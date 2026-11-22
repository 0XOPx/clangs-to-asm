namespace ClangsToAsm;

internal sealed record Target(string Name, string ClangTriple, string? RuntimeIdentifier)
{
    public static readonly IReadOnlyList<Target> All =
    [
        new("x86_64-linux", "x86_64-unknown-linux-gnu", "linux-x64"),
        new("x86_64-windows", "x86_64-pc-windows-msvc", "win-x64"),
        new("x86_64-macos", "x86_64-apple-darwin", "osx-x64"),
        new("i686-linux", "i686-unknown-linux-gnu", null),
        new("i686-windows", "i686-pc-windows-msvc", null),
        new("aarch64-linux", "aarch64-unknown-linux-gnu", "linux-arm64"),
        new("aarch64-macos", "aarch64-apple-darwin", "osx-arm64"),
        new("armv7-linux", "armv7-unknown-linux-gnueabihf", null),
        new("riscv64-linux", "riscv64-unknown-linux-gnu", null),
        new("wasm32", "wasm32-unknown-unknown", null)
    ];

    public static Target? Find(string name) =>
        All.FirstOrDefault(target => string.Equals(target.Name, name, StringComparison.OrdinalIgnoreCase));
}
