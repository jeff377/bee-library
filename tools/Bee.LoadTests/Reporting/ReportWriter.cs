using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bee.LoadTests.Reporting
{
    /// <summary>
    /// Writes a run's report to disk, for people and for later comparison.
    /// </summary>
    /// <remarks>
    /// The output directory defaults under the git-ignored <c>artifacts/</c> tree: most runs are
    /// exploratory and are not worth keeping, so the ones that are get copied out by hand rather
    /// than every run accumulating in version control.
    /// </remarks>
    public static class ReportWriter
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        };

        /// <summary>
        /// Writes the Markdown report.
        /// </summary>
        /// <param name="report">The run report.</param>
        /// <param name="path">Destination file path.</param>
        public static void WriteMarkdown(RunReport report, string path)
        {
            ArgumentNullException.ThrowIfNull(report);
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            var text = new StringBuilder();
            var meta = report.Metadata;

            text.Append("# Load test ").AppendLine(
                meta.CompletedAt.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture));
            text.AppendLine();

            text.AppendLine("| Setting | Value |");
            text.AppendLine("|---|---|");
            AppendRow(text, "Version", meta.Version);
            AppendRow(text, "Provider", meta.Provider);
            AppendRow(text, "Mode", $"{meta.Mode}, {meta.ProtectionLevel}");
            AppendRow(text, "Machine", $"{meta.OperatingSystem}, {meta.ProcessorCount} logical cores");
            AppendRow(text, "Load",
                $"{meta.VirtualUsers} VU, warm-up {meta.WarmupSeconds}s, measure {meta.DurationSeconds}s, {meta.LoadModel} model");
            AppendRow(text, "Auth", meta.TokenStrategy);
            AppendRow(text, "Seeded rows", meta.SeededRows.ToString(CultureInfo.InvariantCulture));
            if (meta.DroppedBindings.Count > 0)
            {
                AppendRow(text, "Dropped bindings", string.Join(", ", meta.DroppedBindings));
            }
            text.AppendLine();

            text.AppendLine("## Results");
            text.AppendLine();
            text.Append("| Scenario | calls | errors | rps |");
            foreach (var percentile in report.Percentiles)
            {
                text.Append(" p").Append(percentile.ToString("0.##", CultureInfo.InvariantCulture)).Append(" |");
            }
            text.AppendLine(" max |");

            text.Append("|---|---:|---:|---:|");
            foreach (var _ in report.Percentiles) { text.Append("---:|"); }
            text.AppendLine("---:|");

            foreach (var scenario in report.Scenarios)
            {
                text.Append("| ").Append(scenario.Name)
                    .Append(" | ").Append(scenario.SuccessCount.ToString(CultureInfo.InvariantCulture))
                    .Append(" | ").Append(scenario.ErrorCount.ToString(CultureInfo.InvariantCulture))
                    .Append(" | ").Append(scenario.RequestsPerSecond.ToString("F1", CultureInfo.InvariantCulture));
                foreach (var percentile in report.Percentiles)
                {
                    text.Append(" | ").Append(
                        scenario.Latencies.Percentile(percentile).ToString("F1", CultureInfo.InvariantCulture));
                }
                text.Append(" | ").Append(scenario.Latencies.Max.ToString("F1", CultureInfo.InvariantCulture))
                    .AppendLine(" |");
            }
            text.AppendLine();
            text.AppendLine("Latencies are in milliseconds, over the successful calls only.");
            text.AppendLine();

            if (report.HasErrors)
            {
                text.AppendLine("## Errors");
                text.AppendLine();
                foreach (var scenario in report.Scenarios.Where(s => s.ErrorCount > 0))
                {
                    text.Append("### ").AppendLine(scenario.Name);
                    text.AppendLine();
                    text.AppendLine("| Count | Type | Example message |");
                    text.AppendLine("|---:|---|---|");
                    foreach (var pair in scenario.Errors.OrderByDescending(p => p.Value))
                    {
                        var sample = scenario.ErrorSamples.TryGetValue(pair.Key, out var message)
                            ? message.Replace("|", "\\|", StringComparison.Ordinal)
                            : string.Empty;
                        text.Append("| ").Append(pair.Value.ToString(CultureInfo.InvariantCulture))
                            .Append(" | ").Append(pair.Key)
                            .Append(" | ").Append(sample).AppendLine(" |");
                    }
                    text.AppendLine();
                }
            }

            text.AppendLine("## Cache");
            text.AppendLine();

            if (!report.CacheObserved)
            {
                text.AppendLine(
                    "Not observed. The counting provider runs in the driver's process, and a " +
                    "Remote run exercises the cache in the server's — the counters below would " +
                    "read zero, which is absence of measurement rather than a zero hit rate. Run " +
                    "in Local mode to measure cache behaviour.");
            }
            else
            {
                text.AppendLine("| Metric | Value |");
                text.AppendLine("|---|---:|");
                AppendRow(text, "Reads", report.Cache.Reads.ToString(CultureInfo.InvariantCulture));
                AppendRow(text, "Hit rate", report.Cache.HitRate.ToString("P1", CultureInfo.InvariantCulture));
                AppendRow(text, "Writes", report.Cache.Writes.ToString(CultureInfo.InvariantCulture));
                text.AppendLine();
                text.AppendLine(
                    "Writes are creations that reached the provider. Concurrent misses on one key " +
                    "are collapsed above it, so a write count far below the virtual user count is " +
                    "that collapsing working.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(path, text.ToString());
        }

        /// <summary>
        /// Writes the JSON report, which is what a later run can be compared against.
        /// </summary>
        /// <param name="report">The run report.</param>
        /// <param name="path">Destination file path.</param>
        public static void WriteJson(RunReport report, string path)
        {
            ArgumentNullException.ThrowIfNull(report);
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            var document = new
            {
                report.Metadata,
                CacheObserved = report.CacheObserved,
                Cache = new
                {
                    report.Cache.Reads,
                    report.Cache.Hits,
                    report.Cache.Misses,
                    report.Cache.Writes,
                    report.Cache.HitRate,
                },
                Scenarios = report.Scenarios.Select(scenario => new
                {
                    scenario.Name,
                    scenario.SuccessCount,
                    scenario.ErrorCount,
                    scenario.RequestsPerSecond,
                    DurationSeconds = scenario.Duration.TotalSeconds,
                    Latencies = new
                    {
                        scenario.Latencies.Count,
                        scenario.Latencies.Min,
                        scenario.Latencies.Mean,
                        scenario.Latencies.Max,
                        Percentiles = report.Percentiles.ToDictionary(
                            percentile => percentile.ToString("0.##", CultureInfo.InvariantCulture),
                            scenario.Latencies.Percentile),
                    },
                    scenario.Errors,
                    scenario.ErrorSamples,
                }),
            };

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(path, JsonSerializer.Serialize(document, s_jsonOptions));
        }

        private static void AppendRow(StringBuilder text, string name, string value)
            => text.Append("| ").Append(name).Append(" | ").Append(value).AppendLine(" |");
    }
}
