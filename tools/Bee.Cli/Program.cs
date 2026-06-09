using System.Reflection;
using Bee.Definition;

namespace Bee.Cli;

/// <summary>
/// Entry point for the <c>dotnet bee</c> CLI. Routes to subcommand groups
/// (<c>defines</c>) plus a few top-level helpers (<c>--version</c>, <c>--help</c>).
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            return Dispatch(args);
        }
        catch (UsageException ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            Console.Error.WriteLine();
            PrintRootHelp(Console.Error);
            return ExitCodes.Usage;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return ExitCodes.Error;
        }
    }

    private static int Dispatch(string[] args)
    {
        if (args.Length == 0)
        {
            PrintRootHelp(Console.Out);
            return ExitCodes.Success;
        }

        return args[0] switch
        {
            "--version" or "-v" => PrintVersion(),
            "--help" or "-h" or "help" => Help(args),
            "defines" => DefinesCommand.Run(args.AsSpan(1).ToArray()),
            _ => throw new UsageException($"unknown command: '{args[0]}'"),
        };
    }

    private static int Help(string[] args)
    {
        if (args.Length >= 2 && args[1] == "defines")
        {
            DefinesCommand.PrintHelp(Console.Out);
        }
        else
        {
            PrintRootHelp(Console.Out);
        }
        return ExitCodes.Success;
    }

    private static int PrintVersion()
    {
        var asm = typeof(Program).Assembly;
        var version = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? asm.GetName().Version?.ToString()
                      ?? "unknown";
        Console.WriteLine($"dotnet-bee {version}");
        Console.WriteLine($"Bee.NET framework CLI");
        return ExitCodes.Success;
    }

    private static void PrintRootHelp(TextWriter writer)
    {
        writer.WriteLine("dotnet bee - Bee.NET framework CLI");
        writer.WriteLine();
        writer.WriteLine("Usage: dotnet bee <command> [options]");
        writer.WriteLine();
        writer.WriteLine("Commands:");
        writer.WriteLine("  defines       Manage framework default define files (materialize / list)");
        writer.WriteLine();
        writer.WriteLine("Options:");
        writer.WriteLine("  --version, -v Print the CLI version and exit");
        writer.WriteLine("  --help, -h    Print this help text");
        writer.WriteLine();
        writer.WriteLine("Run 'dotnet bee help <command>' for command-specific help.");
    }
}

/// <summary>
/// Centralised exit codes so callers (CI scripts) can rely on stable values.
/// </summary>
internal static class ExitCodes
{
    public const int Success = 0;
    public const int Error = 1;
    public const int Usage = 2;
}

/// <summary>
/// Thrown for argument-parsing / usage errors. Caught at the top of <c>Main</c>
/// and translated to a usage message + exit code 2.
/// </summary>
internal sealed class UsageException : Exception
{
    public UsageException(string message) : base(message) { }
}
