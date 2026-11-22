using System.Runtime.InteropServices;

namespace ClangsToAsm;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            var options = Arguments.Parse(args);

            if (options.Help)
            {
                PrintHelp();
                return 0;
            }

            if (options.ListTargets)
            {
                foreach (var target in Targets.All)
                    Console.WriteLine($"{target.Name,-18} {target.ClangTriple}");
                return 0;
            }

            return await Compiler.CompileAsync(options);
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            Console.Error.WriteLine("Use --help for usage.");
            return 1;
        }
        catch (PlatformNotSupportedException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
        ClangsToAsm - C, C++, and C# to native assembly

        Usage:
          clangs-to-asm <input> [options]
          clangs-to-asm --list-targets

        Options:
          --target <name>       Select one target
          --all-targets         Compile for every configured target
          --output-dir, -o      Output directory (default: asm)
          --opt <level>         O0, O1, O2, O3, Os, Oz (default: O2)
          --std <standard>      C/C++ language standard
          --clang <path>        Clang executable
          --clang++ <path>      Clang++ executable
          --objdump <path>      Disassembler for C# NativeAOT output
          --list-targets        List configured targets
          --help, -h            Show this help

        C/C++ assembly is emitted by Clang. C# uses .NET NativeAOT and objdump.
        """);
    }
}
