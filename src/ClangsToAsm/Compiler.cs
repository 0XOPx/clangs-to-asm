using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ClangsToAsm;

internal static class Compiler
{
    public static async Task<int> CompileAsync(Arguments options)
    {
        var input = Path.GetFullPath(options.Input!);

        if (!File.Exists(input))
        {
            Console.Error.WriteLine($"Input file not found: {input}");
            return 1;
        }

        var extension = Path.GetExtension(input).ToLowerInvariant();
        if (extension is not (".c" or ".cc" or ".cpp" or ".cxx" or ".cs"))
        {
            Console.Error.WriteLine("Supported source extensions: .c, .cc, .cpp, .cxx, .cs");
            return 1;
        }

        var targets = options.AllTargets
            ? Targets.All
            : new[] { options.TargetName is null ? GetHostTarget() : Targets.Find(options.TargetName)! };

        Directory.CreateDirectory(options.OutputDirectory);
        var failures = 0;

        foreach (var target in targets)
        {
            Console.WriteLine($"[{target.Name}] {Path.GetFileName(input)}");
            var result = extension == ".cs"
                ? await CompileCSharpAsync(input, target, options)
                : await CompileClangAsync(input, target, options, extension);

            if (result != 0)
                failures++;
        }

        Console.WriteLine($"Completed: {targets.Count - failures}/{targets.Count} target(s) succeeded.");
        return failures == 0 ? 0 : 1;
    }

    private static async Task<int> CompileClangAsync(string input, Target target, Arguments options, string extension)
    {
        var compiler = extension == ".c" ? options.Clang : options.ClangPlusPlus;
        var standard = options.Standard ?? (extension == ".c" ? "c17" : "c++20");
        var output = Path.Combine(options.OutputDirectory, $"{Path.GetFileNameWithoutExtension(input)}.{target.Name}.asm");
        var start = new ProcessStartInfo(compiler)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in new[] { "-S", $"-{options.Optimization}", $"--target={target.ClangTriple}", $"-std={standard}", input, "-o", Path.GetFullPath(output) })
            start.ArgumentList.Add(argument);

        return await RunAsync(start, target.Name);
    }

    private static async Task<int> CompileCSharpAsync(string input, Target target, Arguments options)
    {
        if (target.RuntimeIdentifier is null)
        {
            Console.Error.WriteLine($"C# NativeAOT does not have a configured runtime identifier for {target.Name}.");
            return 1;
        }

        var root = Path.Combine(Path.GetTempPath(), "ClangsToAsm", Guid.NewGuid().ToString("N"));
        var project = Path.Combine(root, "NativeInput");
        var publish = Path.Combine(root, "publish");
        Directory.CreateDirectory(project);
        Directory.CreateDirectory(publish);

        try
        {
            var sourceName = Path.GetFileName(input);
            File.Copy(input, Path.Combine(project, sourceName));

            var projectFile = Path.Combine(project, "NativeInput.csproj");
            await File.WriteAllTextAsync(projectFile,
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net8.0</TargetFramework>
                    <PublishAot>true</PublishAot>
                    <InvariantGlobalization>true</InvariantGlobalization>
                    <StripSymbols>false</StripSymbols>
                  </PropertyGroup>
                </Project>
                """);

            var publishStart = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = project
            };

            foreach (var argument in new[]
            {
                "publish", projectFile, "-c", "Release", "-r", target.RuntimeIdentifier,
                "-p:PublishAot=true", "-p:StripSymbols=false", "-o", publish
            })
                publishStart.ArgumentList.Add(argument);

            var publishResult = await RunAsync(publishStart, target.Name);
            if (publishResult != 0)
                return publishResult;

            var executableName = target.RuntimeIdentifier.StartsWith("win-", StringComparison.OrdinalIgnoreCase) ? "NativeInput.exe" : "NativeInput";
            var executable = Path.Combine(publish, executableName);
            if (!File.Exists(executable))
            {
                Console.Error.WriteLine($"NativeAOT output was not found: {executable}");
                return 1;
            }

            var output = Path.Combine(options.OutputDirectory, $"{Path.GetFileNameWithoutExtension(input)}.{target.Name}.asm");
            var disassembler = new ProcessStartInfo(options.ObjDump)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            disassembler.ArgumentList.Add("-d");
            disassembler.ArgumentList.Add("--demangle");
            disassembler.ArgumentList.Add(executable);

            return await RunToFileAsync(disassembler, output, target.Name);
        }
        finally
        {
            try
            {
                Directory.Delete(root, true);
            }
            catch
            {
            }
        }
    }

    private static async Task<int> RunAsync(ProcessStartInfo start, string target)
    {
        try
        {
            using var process = Process.Start(start);
            if (process is null)
                return 1;

            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var output = await stdout;
            var error = await stderr;

            if (!string.IsNullOrWhiteSpace(output))
                Console.Write(output);
            if (!string.IsNullOrWhiteSpace(error))
                Console.Error.Write(error);

            return process.ExitCode;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            Console.Error.WriteLine($"[{target}] Required tool not found: {start.FileName}");
            return 2;
        }
    }

    private static async Task<int> RunToFileAsync(ProcessStartInfo start, string output, string target)
    {
        try
        {
            using var process = Process.Start(start);
            if (process is null)
                return 1;

            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var text = await stdout;
            var error = await stderr;
            await File.WriteAllTextAsync(output, text);

            if (!string.IsNullOrWhiteSpace(error))
                Console.Error.Write(error);

            return process.ExitCode;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            Console.Error.WriteLine($"[{target}] Required tool not found: {start.FileName}");
            return 2;
        }
    }

    private static Target GetHostTarget()
    {
        if (OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.X64)
            return Targets.Find("x86_64-windows")!;
        if (OperatingSystem.IsLinux() && RuntimeInformation.ProcessArchitecture == Architecture.X64)
            return Targets.Find("x86_64-linux")!;
        if (OperatingSystem.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            return Targets.Find("aarch64-macos")!;
        if (OperatingSystem.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.X64)
            return Targets.Find("x86_64-macos")!;
        if (OperatingSystem.IsLinux() && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            return Targets.Find("aarch64-linux")!;

        throw new PlatformNotSupportedException("No default target is configured for this host. Specify --target.");
    }
}
