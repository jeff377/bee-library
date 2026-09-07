namespace Bee.LoadTests.Statistics
{
    /// <summary>
    /// Computes latency percentiles over a set of recorded durations.
    /// </summary>
    /// <remarks>
    /// Percentiles use the nearest-rank method: the p-th percentile is the value at
    /// rank <c>ceil(p / 100 * N)</c> in the sorted sample, so every reported figure is an
    /// observed measurement rather than an interpolation between two of them. The choice is
    /// recorded here because a report comparing runs is only meaningful when both used the
    /// same definition, and the two common definitions disagree on small samples.
    /// </remarks>
    public sealed class LatencyStatistics
    {
        private readonly double[] _sortedMilliseconds;

        private LatencyStatistics(double[] sortedMilliseconds)
            => _sortedMilliseconds = sortedMilliseconds;

        /// <summary>
        /// Gets the number of samples.
        /// </summary>
        public int Count => _sortedMilliseconds.Length;

        /// <summary>
        /// Gets the smallest recorded duration in milliseconds, or zero when there are no samples.
        /// </summary>
        public double Min => Count == 0 ? 0 : _sortedMilliseconds[0];

        /// <summary>
        /// Gets the largest recorded duration in milliseconds, or zero when there are no samples.
        /// </summary>
        public double Max => Count == 0 ? 0 : _sortedMilliseconds[Count - 1];

        /// <summary>
        /// Gets the arithmetic mean in milliseconds, or zero when there are no samples.
        /// </summary>
        public double Mean
        {
            get
            {
                if (Count == 0) { return 0; }
                double total = 0;
                foreach (var value in _sortedMilliseconds) { total += value; }
                return total / Count;
            }
        }

        /// <summary>
        /// Creates a statistics view over the given durations. The input is copied and sorted,
        /// so the caller's array is left untouched.
        /// </summary>
        /// <param name="milliseconds">The recorded durations, in any order.</param>
        /// <returns>A statistics view over a sorted copy of <paramref name="milliseconds"/>.</returns>
        public static LatencyStatistics FromMilliseconds(IReadOnlyCollection<double> milliseconds)
        {
            ArgumentNullException.ThrowIfNull(milliseconds);
            var sorted = milliseconds.ToArray();
            Array.Sort(sorted);
            return new LatencyStatistics(sorted);
        }

        /// <summary>
        /// Gets the value at the given percentile, in milliseconds.
        /// </summary>
        /// <param name="percentile">The percentile to read, in the range 0 to 100 exclusive of 0.</param>
        /// <returns>The observed value at that percentile, or zero when there are no samples.</returns>
        public double Percentile(double percentile)
        {
            if (percentile <= 0 || percentile > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(percentile), percentile,
                    "Percentile must be greater than 0 and at most 100.");
            }

            if (Count == 0) { return 0; }

            var rank = (int)Math.Ceiling(percentile / 100d * Count);
            var index = Math.Clamp(rank - 1, 0, Count - 1);
            return _sortedMilliseconds[index];
        }
    }
}
