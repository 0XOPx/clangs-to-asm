# ClangsToAsm

ClangsToAsm compiles C, C++, and C# source files into native assembly listings for selectable target architectures.

- C and C++: Clang/LLVM assembly output via `-S`.
- C#: .NET NativeAOT compilation followed by disassembly with LLVM objdump or GNU objdump.
- Multiple targets: compile once per selected target and write separate output files.
- Compiler processes receive argument lists directly; no shell command strings are constructed.

## Requirements

- .NET 8 SDK to build and run ClangsToAsm.
- Clang and Clang++ for C and C++.
- For C# native output: a .NET SDK with NativeAOT support and the platform's NativeAOT prerequisites.
- `llvm-objdump` or `objdump` to turn the NativeAOT executable into an assembly listing.

Clang can emit assembly without linking. Cross-target headers, libraries, sysroots, and target toolchains may still be required by the source being compiled. C# NativeAOT cross-compilation is limited by the .NET toolchain and host/target support.

## Build

```powershell
dotnet build -c Release
```

## Usage

Compile one file for the host target:

```powershell
dotnet run --project src/ClangsToAsm -- program.c
```

Choose a target:

```powershell
dotnet run --project src/ClangsToAsm -- program.cpp --target x86_64-linux
```

Compile for every configured target:

```powershell
dotnet run --project src/ClangsToAsm -- program.c --all-targets
```

Choose the output directory:

```powershell
dotnet run --project src/ClangsToAsm -- program.c --all-targets --output-dir asm
```

List target identifiers:

```powershell
dotnet run --project src/ClangsToAsm -- --list-targets
```

Set optimization and language standard:

```powershell
dotnet run --project src/ClangsToAsm -- program.cpp --target x86_64-linux --opt O2 --std c++20
dotnet run --project src/ClangsToAsm -- program.c --target aarch64-linux --opt O3 --std c17
```

Select compiler executables:

```powershell
dotnet run --project src/ClangsToAsm -- program.c --clang clang
dotnet run --project src/ClangsToAsm -- program.cpp --clang++ clang++
dotnet run --project src/ClangsToAsm -- program.cs --objdump llvm-objdump
```

## Configured targets

| Identifier | Clang target |
| --- | --- |
| `x86_64-linux` | x86-64 Linux GNU |
| `x86_64-windows` | x86-64 Windows MSVC |
| `x86_64-macos` | x86-64 macOS |
| `i686-linux` | 32-bit x86 Linux GNU |
| `i686-windows` | 32-bit x86 Windows MSVC |
| `aarch64-linux` | ARM64 Linux GNU |
| `aarch64-macos` | ARM64 macOS |
| `armv7-linux` | ARMv7 Linux hard-float |
| `riscv64-linux` | RISC-V 64-bit Linux GNU |
| `wasm32` | WebAssembly 32-bit |

The target list describes Clang code-generation targets. A target may still require an installed Clang backend, target sysroot, SDK, or external toolchain.

C# currently maps to NativeAOT runtime identifiers for `win-x64`, `linux-x64`, `linux-arm64`, `osx-x64`, and `osx-arm64`. The selected target must be supported by the host SDK and its NativeAOT toolchain. Targets without a configured NativeAOT runtime identifier are reported as unsupported for C#.

## Output

Each output is named `<input-name>.<target>.asm`. C/C++ output is assembly emitted directly by Clang. C# output is a disassembly of the NativeAOT-produced native executable.

## Exit codes

- `0`: all requested compilations succeeded.
- `1`: invalid arguments or compilation failure.
- `2`: required compiler/tool was not found.

## License

MIT
