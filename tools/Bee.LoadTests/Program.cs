using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Client;
using Bee.Db;
using Bee.Definition.Security;
using Bee.Db.Manager;
using Bee.Definition.Storage;
using Bee.LoadTests.Bootstrap;
using Bee.LoadTests.Configuration;
using Bee.LoadTests.Reporting;
using Bee.LoadTests.Running;
using Bee.LoadTests.Scenarios;
using Bee.LoadTests.Serving;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.LoadTests
{
    /// <summary>
    /// Entry point for the load-test driver. Argument parsing is hand-rolled, matching
    /// <c>Bee.Cli</c> — this project takes no package reference beyond an ADO.NET driver.
    /// </summary>
    public static class Program
    {
        private const int ExitSuccess = 0;
        private const int ExitFailure = 1;
        private const int ExitUsage = 2;

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
                return ExitSuccess;
            }

            try
            {
                return args[0] switch
                {
                    "--version" or "-v" => PrintVersion(),
                    "--help" or "-h" or "help" => Help(Console.Out),
                    "verify" => Verify(args.AsSpan(1).ToArray()),
                    "prepare" => Prepare(args.AsSpan(1).ToArray()),
                    "run" => RunAsync(args.AsSpan(1).ToArray()).GetAwaiter().GetResult(),
                    "serve" => Serve(args.AsSpan(1).ToArray()),
                    _ => UnknownCommand(args[0]),
                };
            }
            catch (Exception ex) when (ex is InvalidOperationException
                                        or NotSupportedException
                                        or IOException
                                        or ArgumentException)
            {
                // These are the failures a misconfigured run produces, and their messages are
                // written to be actionable on their own. Anything else is a defect and should
                // surface with its stack trace rather than be flattened to one line here.
                Console.Error.WriteLine($"error: {ex.Message}");
                return ExitFailure;
            }
        }

        private static int Verify(string[] args)
        {
            var options = LoadConfiguration(args);
            options.Validate();

            Console.WriteLine($"Provider     : {options.Database.Provider}");
            Console.WriteLine($"Connection   : {DefineWorkspace.GetConnectionStringVariable(options.Database.Provider)}");

            using var host = LoadTestHost.Start(options);

            Console.WriteLine($"Definitions  : {host.DefinePath}");
            if (host.DroppedBindings.Count > 0)
            {
                Console.WriteLine(
                    $"Dropped      : {string.Join(", ", host.DroppedBindings)} " +
                    "(assembly not loadable; falls back to the framework implementation)");
            }

            // Resolving these proves the chain a Local call actually walks: the executor is what
            // LocalApiProvider reaches for, and it is useless without definitions and database
            // access behind it.
            var executor = host.Services.GetRequiredService<JsonRpcExecutor>();
            var defineAccess = host.Services.GetRequiredService<IDefineAccess>();
            host.Services.GetRequiredService<IDbAccessFactory>();
            Console.WriteLine($"Executor     : {executor.GetType().Name}");

            // Reading a schema proves the definition copy is not merely present but usable.
            var schema = defineAccess.GetFormSchema("Order");
            Console.WriteLine($"Form schema  : {schema.ProgId} (category: {schema.CategoryId})");

            // Read it again. The first read populates the cache, so the second must be served from
            // it — which is what proves the counters observe hits at all. A run reporting a hit
            // rate of zero would otherwise be indistinguishable from a decorator that was
            // installed too early and silently overwritten.
            var before = host.CacheCounters.Snapshot();
            defineAccess.GetFormSchema("Order");
            var after = host.CacheCounters.Snapshot();

            var hitsObserved = after.Hits - before.Hits;
            Console.WriteLine(
                $"Cache        : {after.Reads} reads, {after.Hits} hits, {after.Writes} writes");
            Console.WriteLine(
                $"Cache re-read: {hitsObserved} hit(s), {after.Writes - before.Writes} write(s)");

            if (hitsObserved == 0)
            {
                Console.Error.WriteLine(
                    "warning: re-reading a cached definition produced no hit. The counting " +
                    "provider may have been installed before AddBeeFramework and overwritten.");
                return ExitFailure;
            }

            Console.WriteLine();
            Console.WriteLine("Backend started and torn down successfully.");
            return ExitSuccess;
        }

        private static int Prepare(string[] args)
        {
            var options = LoadConfiguration(args);
            options.Validate();

            Console.WriteLine($"Provider     : {options.Database.Provider}");
            Console.WriteLine($"Databases    : {options.Database.DatabaseNamePrefix}<category>");

            using var host = LoadTestHost.Start(options);
            var defineAccess = host.Services.GetRequiredService<IDefineAccess>();
            var connectionManager = host.Services.GetRequiredService<IDbConnectionManager>();

            SchemaPreparer.EnsureDatabases(options, defineAccess, host.ConnectionStringTemplate);
            Console.WriteLine("Databases    : ensured");

            var tables = SchemaPreparer.EnsureTables(defineAccess, connectionManager);
            Console.WriteLine($"Tables       : {tables} built or confirmed");

            if (options.Seed.Enabled)
            {
                var dbAccessFactory = host.Services.GetRequiredService<IDbAccessFactory>();

                // Accounts live in the common category; the business tables do not.
                var accounts = AccountSeeder.EnsureAccounts(
                    dbAccessFactory.Create("common"), options.Auth, options.Database.CategoryId);
                Console.WriteLine(accounts == 0
                    ? $"Accounts     : {options.Auth.UserPoolSize} already present"
                    : $"Accounts     : +{accounts} of {options.Auth.UserPoolSize}");

                var dbAccess = dbAccessFactory.Create(options.Database.CategoryId);

                foreach (var table in options.Seed.Tables)
                {
                    var started = Stopwatch.GetTimestamp();
                    var inserted = DataSeeder.EnsureRows(
                        defineAccess, dbAccess, options.Database.CategoryId,
                        table, options.Seed.RowCount);
                    var elapsed = Stopwatch.GetElapsedTime(started);
                    var total = DataSeeder.CountRows(dbAccess, table);

                    Console.WriteLine(inserted == 0
                        ? $"Seed         : {table} already holds {total} row(s)"
                        : $"Seed         : {table} +{inserted} row(s) in {elapsed.TotalSeconds:F1}s (now {total})");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Schema is ready.");
            return ExitSuccess;
        }

        private static async Task<int> RunAsync(string[] args)
        {
            var options = LoadConfiguration(args);
            ApplyOverrides(options, args);
            options.Validate();

            var enabled = options.Scenarios.Where(scenario => scenario.Enabled).ToArray();
            var weights = enabled.ToDictionary(
                scenario => scenario.Name, scenario => scenario.Weight, StringComparer.Ordinal);

            Console.WriteLine($"Provider     : {options.Database.Provider}");
            Console.WriteLine($"Mode         : {options.Target.Mode}, {options.Target.ProtectionLevel}");
            Console.WriteLine(
                $"Load         : {options.Load.VirtualUsers} VU, warm-up {options.Load.WarmupSeconds}s, " +
                $"measure {options.Load.DurationSeconds}s, {options.Load.Model} model");

            using var host = LoadTestHost.Start(options);
            if (host.DroppedBindings.Count > 0)
            {
                Console.WriteLine($"Dropped      : {string.Join(", ", host.DroppedBindings)}");
            }

            if (options.Target.Mode == TargetMode.Remote)
            {
                // Sent as X-Api-Key on every remote call; the server rejects a request without it
                // before any of this reaches a business object.
                ApiClientInfo.ApiKey = options.Target.ApiKey;
            }

            var pool = new VirtualUserPool(options.Auth,
                options.Target.Mode == TargetMode.Remote ? options.Target.Endpoint : null);

            // Keys for the read-by-key scenario are collected up front. Doing it inside the
            // scenario would fold the lookup into every sample.
            var rowIds = enabled.Any(scenario => scenario.Name is "GetData" or "Save")
                ? CollectRowIds(host, options)
                : [];

            var scenarios = enabled
                .Select(scenario => CreateScenario(scenario, options, pool, rowIds))
                .ToArray();

            Console.WriteLine($"Scenarios    : {string.Join(", ", scenarios.Select(s => s.Name))}");
            Console.WriteLine();
            Console.WriteLine("Running...");

            var results = await LoadRunner.RunAsync(
                options.Load, scenarios, weights,
                onWarmupComplete: host.CacheCounters.Reset).ConfigureAwait(false);

            var report = new RunReport(
                RunMetadata.Capture(options, host.DroppedBindings),
                results,
                host.CacheCounters.Snapshot(),
                options.Report.Percentiles);

            if (options.Report.Console) { PrintResults(report); }
            WriteReports(report, options.Report);

            return results.Any(result => result.SuccessCount == 0) ? ExitFailure : ExitSuccess;
        }

        private static int Serve(string[] args)
        {
            var options = LoadConfiguration(args);
            options.Validate();

            var url = ReadOption(args, "--url") ?? "http://localhost:5199";

            Console.WriteLine($"Provider     : {options.Database.Provider}");
            LoadTestServer.RunAsync(options, url).GetAwaiter().GetResult();
            return ExitSuccess;
        }

        private static IReadOnlyList<Guid> CollectRowIds(LoadTestHost host, LoadTestOptions options)
        {
            var table = options.Seed.Tables.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(table))
            {
                throw new InvalidOperationException(
                    "The GetData and Save scenarios work by key, but seed.tables is empty so " +
                    "there is no table to take keys from.");
            }

            var dbAccess = host.Services.GetRequiredService<IDbAccessFactory>()
                .Create(options.Database.CategoryId);
            return DataSeeder.ReadRowIds(dbAccess, table, count: 100);
        }

        private static IScenario CreateScenario(
            ScenarioOptions scenario,
            LoadTestOptions options,
            VirtualUserPool pool,
            IReadOnlyList<Guid> rowIds)
        {
            var endpoint = options.Target.Mode == TargetMode.Remote ? options.Target.Endpoint : null;

            return scenario.Name switch
            {
                "Login" => new LoginScenario(options.Auth, endpoint),
                "GetList" or "GetListDeep" => new GetListScenario(
                    pool, scenario.Name, scenario.ProgId, scenario.PageSize, scenario.StartPage),
                "GetData" => new GetDataScenario(pool, scenario.ProgId, rowIds),
                "Save" => new SaveScenario(pool, scenario.ProgId, rowIds),
                _ => throw new InvalidOperationException(
                    $"Unknown scenario '{scenario.Name}'. A misspelled name is rejected rather " +
                    "than skipped, because a run silently missing a scenario would still produce " +
                    "a report that looks complete.")
            };
        }

        private static void PrintResults(RunReport report)
        {
            Console.WriteLine();
            Console.Write($"{"Scenario",-16}{"calls",8}{"errors",8}{"rps",10}");
            foreach (var percentile in report.Percentiles)
            {
                Console.Write($"{"p" + percentile.ToString("0.##", CultureInfo.InvariantCulture),10}");
            }
            Console.WriteLine($"{"max",10}");

            foreach (var result in report.Scenarios)
            {
                Console.Write(
                    $"{result.Name,-16}{result.SuccessCount,8}{result.ErrorCount,8}" +
                    $"{result.RequestsPerSecond,10:F1}");
                foreach (var percentile in report.Percentiles)
                {
                    Console.Write($"{result.Latencies.Percentile(percentile),10:F1}");
                }
                Console.WriteLine($"{result.Latencies.Max,10:F1}");
            }

            foreach (var result in report.Scenarios.Where(r => r.ErrorCount > 0))
            {
                Console.WriteLine();
                Console.WriteLine($"Errors in {result.Name}:");
                foreach (var pair in result.Errors.OrderByDescending(p => p.Value))
                {
                    var sample = result.ErrorSamples.TryGetValue(pair.Key, out var message)
                        ? $" — {message}"
                        : string.Empty;
                    Console.WriteLine($"  {pair.Value,8}  {pair.Key}{sample}");
                }
            }

            Console.WriteLine();
            Console.WriteLine(report.CacheObserved
                ? $"Cache        : {report.Cache.Reads} reads, {report.Cache.HitRate:P1} hit rate, " +
                  $"{report.Cache.Writes} writes"
                : "Cache        : not observed (the cache being exercised is in the server process)");
            Console.WriteLine($"Rows seeded  : {report.Metadata.SeededRows} per table");
        }

        private static void WriteReports(RunReport report, ReportOptions options)
        {
            if (!options.Markdown && !options.Json) { return; }

            var stem = Path.Combine(options.OutputDirectory, report.Metadata.ToFileStem());
            Console.WriteLine();

            if (options.Markdown)
            {
                var path = stem + ".md";
                ReportWriter.WriteMarkdown(report, path);
                Console.WriteLine($"Report       : {Path.GetFullPath(path)}");
            }

            if (options.Json)
            {
                var path = stem + ".json";
                ReportWriter.WriteJson(report, path);
                Console.WriteLine($"Report       : {Path.GetFullPath(path)}");
            }
        }

        private static void ApplyOverrides(LoadTestOptions options, string[] args)
        {
            var users = ReadOption(args, "--vu");
            if (users is not null) { options.Load.VirtualUsers = int.Parse(users, CultureInfo.InvariantCulture); }

            var duration = ReadOption(args, "--duration");
            if (duration is not null) { options.Load.DurationSeconds = int.Parse(duration, CultureInfo.InvariantCulture); }

            var warmup = ReadOption(args, "--warmup");
            if (warmup is not null) { options.Load.WarmupSeconds = int.Parse(warmup, CultureInfo.InvariantCulture); }

            var mode = ReadOption(args, "--mode");
            if (mode is not null) { options.Target.Mode = Enum.Parse<TargetMode>(mode, ignoreCase: true); }

            var endpoint = ReadOption(args, "--endpoint");
            if (endpoint is not null) { options.Target.Endpoint = endpoint; }

            var protection = ReadOption(args, "--protection");
            if (protection is not null)
            {
                options.Target.ProtectionLevel =
                    Enum.Parse<ApiProtectionLevel>(protection, ignoreCase: true);
            }
        }

        private static LoadTestOptions LoadConfiguration(string[] args)
        {
            var path = ReadOption(args, "--config");
            if (path is null)
            {
                var fallback = Path.Combine(AppContext.BaseDirectory, "loadtest.sample.json");
                if (!File.Exists(fallback))
                {
                    throw new InvalidOperationException(
                        "No configuration given and the bundled sample could not be found. " +
                        "Pass --config <path>.");
                }
                Console.WriteLine($"Config       : {fallback} (bundled sample)");
                return LoadTestOptions.FromFile(fallback);
            }

            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"Configuration file not found: '{path}'.");
            }

            Console.WriteLine($"Config       : {path}");
            return LoadTestOptions.FromFile(path);
        }

        private static string? ReadOption(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.Ordinal))
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        private static int UnknownCommand(string command)
        {
            Console.Error.WriteLine($"error: unknown command: '{command}'");
            Console.Error.WriteLine();
            PrintHelp(Console.Error);
            return ExitUsage;
        }

        private static int PrintVersion()
        {
            Console.WriteLine(Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? "unknown");
            return ExitSuccess;
        }

        private static int Help(TextWriter writer)
        {
            PrintHelp(writer);
            return ExitSuccess;
        }

        private static void PrintHelp(TextWriter writer)
        {
            writer.WriteLine("Bee.NET load-test driver");
            writer.WriteLine();
            writer.WriteLine("Usage: dotnet run --project tools/Bee.LoadTests -- <command> [options]");
            writer.WriteLine();
            writer.WriteLine("Commands:");
            writer.WriteLine("  verify           Start the backend, resolve its services, tear it down.");
            writer.WriteLine("  prepare          Create the databases and tables a run measures against.");
            writer.WriteLine("  run              Run the load test and report the result.");
            writer.WriteLine("  serve            Host the JSON-RPC endpoint for a Remote run.");
            writer.WriteLine("  --help, -h       Show this help.");
            writer.WriteLine("  --version, -v    Show the version.");
            writer.WriteLine();
            writer.WriteLine("Options:");
            writer.WriteLine("  --config <path>  Configuration file. Defaults to the bundled sample.");
            writer.WriteLine("  --vu <n>         Override the virtual user count.");
            writer.WriteLine("  --duration <s>   Override the measured window, in seconds.");
            writer.WriteLine("  --warmup <s>     Override the warm-up window, in seconds.");
            writer.WriteLine("  --url <url>      Listen address for 'serve'. Default http://localhost:5199.");
            writer.WriteLine("  --mode <m>       Local or Remote.");
            writer.WriteLine("  --endpoint <url> Server to measure in Remote mode.");
            writer.WriteLine("  --protection <p> Public, Encoded or Encrypted.");
            writer.WriteLine();
            writer.WriteLine("The connection string comes from BEE_TEST_CONNSTR_<PROVIDER>, the same");
            writer.WriteLine("variable ./test.sh uses. Run 'prepare' once before the first 'run'.");
        }
    }
}
