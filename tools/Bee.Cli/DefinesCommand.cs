using Bee.Definition;

namespace Bee.Cli;

/// <summary>
/// Subcommand group for the <c>defines</c> command: materialise / list framework
/// default define files. Thin shell over <see cref="Defaults"/>.
/// </summary>
internal static class DefinesCommand
{
    public static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp(Console.Out);
            return ExitCodes.Success;
        }

        return args[0] switch
        {
            "materialize" => Materialize(args.AsSpan(1).ToArray()),
            "list" => List(args.AsSpan(1).ToArray()),
            "--help" or "-h" or "help" => HelpSelf(),
            _ => throw new UsageException($"unknown 'defines' subcommand: '{args[0]}'"),
        };
    }

    private static int HelpSelf()
    {
        PrintHelp(Console.Out);
        return ExitCodes.Success;
    }

    public static void PrintHelp(TextWriter writer)
    {
        writer.WriteLine("dotnet bee defines - Manage framework default define files");
        writer.WriteLine();
        writer.WriteLine("Usage: dotnet bee defines <subcommand> [options]");
        writer.WriteLine();
        writer.WriteLine("Subcommands:");
        writer.WriteLine("  materialize   Write embedded framework defaults to a target directory");
        writer.WriteLine("  list          List the relative paths of all embedded defaults");
        writer.WriteLine();
        writer.WriteLine("Run 'dotnet bee defines <subcommand> --help' for subcommand options.");
    }

    private static int Materialize(string[] args)
    {
        string? path = null;
        bool overwrite = false;
        string? filterPrefix = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--path":
                case "-p":
                    if (i + 1 >= args.Length) throw new UsageException("--path requires a value");
                    path = args[++i];
                    break;
                case "--overwrite":
                    overwrite = true;
                    break;
                case "--filter":
                    if (i + 1 >= args.Length) throw new UsageException("--filter requires a value");
                    filterPrefix = args[++i];
                    break;
                case "--help":
                case "-h":
                    PrintMaterializeHelp(Console.Out);
                    return ExitCodes.Success;
                default:
                    throw new UsageException($"unknown option for 'materialize': '{args[i]}'");
            }
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new UsageException("missing required option: --path <directory>");
        }

        var options = new MaterializeOptions
        {
            Overwrite = overwrite,
            Filter = filterPrefix is null
                ? null
                : p => p.StartsWith(filterPrefix, StringComparison.Ordinal),
        };

        var result = Defaults.MaterializeTo(path, options);

        Console.WriteLine($"Materialized {result.WrittenCount} file(s) to {Path.GetFullPath(path)}");
        if (result.WrittenCount > 0)
        {
            foreach (var rel in result.WrittenRelativePaths)
            {
                Console.WriteLine($"  + {rel}");
            }
        }
        if (result.SkippedCount > 0)
        {
            Console.WriteLine($"Skipped {result.SkippedCount} existing file(s). Pass --overwrite to replace them.");
        }
        return ExitCodes.Success;
    }

    private static void PrintMaterializeHelp(TextWriter writer)
    {
        writer.WriteLine("dotnet bee defines materialize - Write embedded framework defaults to a directory");
        writer.WriteLine();
        writer.WriteLine("Usage: dotnet bee defines materialize --path <directory> [options]");
        writer.WriteLine();
        writer.WriteLine("Options:");
        writer.WriteLine("  --path, -p <dir>     Target directory (created if missing). Required.");
        writer.WriteLine("  --overwrite          Overwrite existing files. Default: skip (preserves consumer customisations).");
        writer.WriteLine("  --filter <prefix>    Only materialize files whose relative path starts with this prefix.");
        writer.WriteLine("                       e.g. --filter TableSchema/ to write only schema files.");
        writer.WriteLine("  --help, -h           Print this help text.");
    }

    private static int List(string[] args)
    {
        // 'list' currently has no options; allow --help for symmetry.
        if (args.Length > 0)
        {
            if (args[0] is "--help" or "-h")
            {
                PrintListHelp(Console.Out);
                return ExitCodes.Success;
            }
            throw new UsageException($"unknown option for 'list': '{args[0]}'");
        }

        foreach (var path in Defaults.ListEmbedded())
        {
            Console.WriteLine(path);
        }
        return ExitCodes.Success;
    }

    private static void PrintListHelp(TextWriter writer)
    {
        writer.WriteLine("dotnet bee defines list - List the relative paths of all embedded framework defaults");
        writer.WriteLine();
        writer.WriteLine("Usage: dotnet bee defines list [--help]");
    }
}
