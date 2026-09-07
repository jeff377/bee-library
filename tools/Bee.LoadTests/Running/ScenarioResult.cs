using Bee.LoadTests.Statistics;

namespace Bee.LoadTests.Running
{
    /// <summary>
    /// What one scenario produced over the measured window.
    /// </summary>
    public sealed class ScenarioResult
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="name">The scenario name.</param>
        /// <param name="latencies">Latency statistics over the successful calls.</param>
        /// <param name="successCount">How many calls completed.</param>
        /// <param name="errors">Error counts by exception type name.</param>
        /// <param name="duration">The measured window.</param>
        public ScenarioResult(
            string name,
            LatencyStatistics latencies,
            long successCount,
            IReadOnlyDictionary<string, long> errors,
            TimeSpan duration)
        {
            Name = name;
            Latencies = latencies;
            SuccessCount = successCount;
            Errors = errors;
            Duration = duration;
        }

        /// <summary>Gets the scenario name.</summary>
        public string Name { get; }

        /// <summary>Gets latency statistics over the successful calls.</summary>
        public LatencyStatistics Latencies { get; }

        /// <summary>Gets how many calls completed without throwing.</summary>
        public long SuccessCount { get; }

        /// <summary>
        /// Gets how many calls threw, by exception type name.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: report this next to the latency figures, never instead of them. A run where
        /// half the calls failed fast looks excellent measured on latency alone.
        /// </remarks>
        public IReadOnlyDictionary<string, long> Errors { get; }

        /// <summary>Gets the measured window.</summary>
        public TimeSpan Duration { get; }

        /// <summary>Gets the total number of calls, successful or not.</summary>
        public long TotalCount => SuccessCount + ErrorCount;

        /// <summary>Gets how many calls threw.</summary>
        public long ErrorCount => Errors.Values.Sum();

        /// <summary>
        /// Gets completed calls per second over the measured window.
        /// </summary>
        public double RequestsPerSecond => Duration.TotalSeconds <= 0
            ? 0
            : SuccessCount / Duration.TotalSeconds;
    }
}
