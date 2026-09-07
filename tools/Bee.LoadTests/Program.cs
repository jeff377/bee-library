using System.Reflection;

namespace Bee.LoadTests
{
    /// <summary>
    /// Entry point for the load-test driver. Argument parsing is hand-rolled, matching
    /// <c>Bee.Cli</c> — this project deliberately takes no package reference.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Runs the requested command.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Zero on success, non-zero on failure.</returns>
        public static int Main(string[] args)
        {
            ArgumentNullException.ThrowIfNull(args);

            if (args.Length == 0)
            {
                PrintHelp(Console.Out);
                return 0;
            }

            switch (args[0])
            {
                case "--version" or "-v":
                    Console.WriteLine(Assembly.GetExecutingAssembly()
                        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                        ?? "unknown");
                    return 0;

                case "--help" or "-h" or "help":
                    PrintHelp(Console.Out);
                    return 0;

                default:
                    Console.Error.WriteLine($"error: unknown command: '{args[0]}'");
                    Console.Error.WriteLine();
                    PrintHelp(Console.Error);
                    return 2;
            }
        }

        private static void PrintHelp(TextWriter writer)
        {
            writer.WriteLine("Bee.NET load-test driver");
            writer.WriteLine();
            writer.WriteLine("Usage: dotnet run --project tools/Bee.LoadTests -- <command>");
            writer.WriteLine();
            writer.WriteLine("Commands:");
            writer.WriteLine("  --help, -h       Show this help.");
            writer.WriteLine("  --version, -v    Show the version.");
            writer.WriteLine();
            writer.WriteLine("The 'run' command is not wired up yet: driving a scenario needs a");
            writer.WriteLine("non-SQLite backend to run against, and this repository does not ship");
            writer.WriteLine("one today (every host here is SQLite-only, which the plan excludes).");
        }
    }
}
