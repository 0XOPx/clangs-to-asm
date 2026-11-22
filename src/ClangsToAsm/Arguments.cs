namespace ClangsToAsm;

internal sealed class Arguments
{
    public string? Input { get; private set; }
    public string OutputDirectory { get; private set; } = "asm";
    public string? TargetName { get; private set; }
    public bool AllTargets { get; private set; }
    public bool ListTargets { get; private set; }
    public string Optimization { get; private set; } = "O2";
    public string? Standard { get; private set; }
    public string Clang { get; private set; } = "clang";
    public string ClangPlusPlus { get; private set; } = "clang++";
    public string ObjDump { get; private set; } = "llvm-objdump";
    public bool Help { get; private set; }

    public static Arguments Parse(string[] args)
    {
        var result = new Arguments();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            string Next()
            {
                if (++i >= args.Length)
                    throw new ArgumentException($"Missing value for {arg}.");
                return args[i];
            }

            switch (arg)
            {
                case "-h":
                case "--help":
                    result.Help = true;
                    break;
                case "--list-targets":
                    result.ListTargets = true;
                    break;
                case "--all-targets":
                    result.AllTargets = true;
                    break;
                case "--target":
                    result.TargetName = Next();
                    break;
                case "--output-dir":
                case "-o":
                    result.OutputDirectory = Next();
                    break;
                case "--opt":
                    result.Optimization = Next();
                    break;
                case "--std":
                    result.Standard = Next();
                    break;
                case "--clang":
                    result.Clang = Next();
                    break;
                case "--clang++":
                    result.ClangPlusPlus = Next();
                    break;
                case "--objdump":
                    result.ObjDump = Next();
                    break;
                default:
                    if (arg.StartsWith('-'))
                        throw new ArgumentException($"Unknown option: {arg}");
                    if (result.Input is not null)
                        throw new ArgumentException("Only one input source file can be compiled per invocation.");
                    result.Input = arg;
                    break;
            }
        }

        if (result.Optimization is not ("O0" or "O1" or "O2" or "O3" or "Os" or "Oz"))
            throw new ArgumentException("Optimization must be O0, O1, O2, O3, Os, or Oz.");

        if (result.TargetName is not null && result.AllTargets)
            throw new ArgumentException("Use either --target or --all-targets, not both.");

        if (!result.Help && !result.ListTargets && result.Input is null)
            throw new ArgumentException("An input file is required.");

        if (result.TargetName is not null && Targets.Find(result.TargetName) is null)
            throw new ArgumentException($"Unknown target: {result.TargetName}. Use --list-targets.");

        return result;
    }
}
