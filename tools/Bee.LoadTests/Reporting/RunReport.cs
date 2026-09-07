using Bee.LoadTests.Caching;
using Bee.LoadTests.Running;

namespace Bee.LoadTests.Reporting
{
    /// <summary>
    /// One run's complete result: what produced it, what it measured, and what the cache did.
    /// </summary>
    public sealed class RunReport
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="metadata">What produced these numbers.</param>
        /// <param name="scenarios">Per-scenario results.</param>
        /// <param name="cache">Cache counters over the measured window.</param>
        /// <param name="percentiles">The percentiles to report.</param>
        public RunReport(
            RunMetadata metadata,
            IReadOnlyList<ScenarioResult> scenarios,
            CacheCounters cache,
            IReadOnlyList<double> percentiles)
        {
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            Scenarios = scenarios ?? throw new ArgumentNullException(nameof(scenarios));
            Cache = cache;
            Percentiles = percentiles ?? throw new ArgumentNullException(nameof(percentiles));
        }

        /// <summary>Gets the run metadata.</summary>
        public RunMetadata Metadata { get; }

        /// <summary>Gets the per-scenario results.</summary>
        public IReadOnlyList<ScenarioResult> Scenarios { get; }

        /// <summary>Gets the cache counters over the measured window.</summary>
        public CacheCounters Cache { get; }

        /// <summary>Gets the percentiles reported for each scenario.</summary>
        public IReadOnlyList<double> Percentiles { get; }

        /// <summary>
        /// Gets whether any scenario recorded an error.
        /// </summary>
        public bool HasErrors => Scenarios.Any(scenario => scenario.ErrorCount > 0);
    }
}
