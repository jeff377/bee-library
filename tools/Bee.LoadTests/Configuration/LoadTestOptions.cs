using System.Text.Json;
using System.Text.Json.Serialization;
using Bee.Definition.Database;

namespace Bee.LoadTests.Configuration
{
    /// <summary>
    /// The whole configuration for one load-test run.
    /// </summary>
    /// <remarks>
    /// A run is driven by a file rather than by code so that changing the load and re-running is
    /// cheap — that repetition is where the value of load testing comes from. The resolved
    /// configuration is written into the report, so a report can say what produced it.
    /// </remarks>
    public sealed class LoadTestOptions
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Gets or sets which backend to drive.
        /// </summary>
        public TargetOptions Target { get; set; } = new();

        /// <summary>
        /// Gets or sets which database to measure.
        /// </summary>
        public DatabaseOptions Database { get; set; } = new();

        /// <summary>
        /// Gets or sets how much load to apply.
        /// </summary>
        public LoadOptions Load { get; set; } = new();

        /// <summary>
        /// Gets or sets how virtual users authenticate.
        /// </summary>
        public AuthOptions Auth { get; set; } = new();

        /// <summary>
        /// Gets or sets the scenarios in the run.
        /// </summary>
        public List<ScenarioOptions> Scenarios { get; set; } = [];

        /// <summary>
        /// Gets or sets which reports to write.
        /// </summary>
        public ReportOptions Report { get; set; } = new();

        /// <summary>
        /// Reads a configuration from a JSON file.
        /// </summary>
        /// <param name="path">Path to the JSON file.</param>
        /// <returns>The parsed configuration.</returns>
        public static LoadTestOptions FromFile(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            var json = File.ReadAllText(path);
            return Parse(json);
        }

        /// <summary>
        /// Parses a configuration from JSON text.
        /// </summary>
        /// <param name="json">The JSON document.</param>
        /// <returns>The parsed configuration.</returns>
        public static LoadTestOptions Parse(string json)
        {
            ArgumentNullException.ThrowIfNull(json);
            return JsonSerializer.Deserialize<LoadTestOptions>(json, s_jsonOptions)
                ?? throw new InvalidOperationException("The configuration document is empty.");
        }

        /// <summary>
        /// Throws when the configuration cannot produce a meaningful run.
        /// </summary>
        /// <exception cref="InvalidOperationException">The configuration is not usable.</exception>
        public void Validate()
        {
            if (Database.Provider == DatabaseType.SQLite)
            {
                throw new InvalidOperationException(
                    "SQLite is not a supported load-test target. It is a single-file embedded " +
                    "engine rather than a server-side option, and its global write lock produces " +
                    "a bottleneck that does not exist in a real deployment. Choose another provider.");
            }

            if (Load.VirtualUsers <= 0)
            {
                throw new InvalidOperationException("load.virtualUsers must be greater than zero.");
            }

            if (Load.DurationSeconds <= 0)
            {
                throw new InvalidOperationException("load.durationSeconds must be greater than zero.");
            }

            if (Load.WarmupSeconds < 0)
            {
                throw new InvalidOperationException("load.warmupSeconds cannot be negative.");
            }

            if (Target.Mode == TargetMode.Remote && string.IsNullOrWhiteSpace(Target.Endpoint))
            {
                throw new InvalidOperationException("target.endpoint is required when target.mode is Remote.");
            }

            if (Scenarios.Count == 0 || !Scenarios.Exists(s => s.Enabled))
            {
                throw new InvalidOperationException("At least one scenario must be enabled.");
            }

            foreach (var scenario in Scenarios)
            {
                if (scenario.Enabled && scenario.Weight <= 0)
                {
                    throw new InvalidOperationException(
                        $"Scenario '{scenario.Name}' is enabled, so its weight must be greater than zero.");
                }
            }

            if (Report.Percentiles.Length == 0)
            {
                throw new InvalidOperationException("report.percentiles cannot be empty.");
            }
        }
    }
}
