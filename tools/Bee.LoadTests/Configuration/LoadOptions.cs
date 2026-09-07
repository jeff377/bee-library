namespace Bee.LoadTests.Configuration
{
    /// <summary>
    /// How much load to apply and for how long.
    /// </summary>
    public sealed class LoadOptions
    {
        /// <summary>
        /// Gets or sets the number of concurrent virtual users.
        /// </summary>
        public int VirtualUsers { get; set; } = 10;

        /// <summary>
        /// Gets or sets how long to run before measurement starts. Samples from this period are
        /// discarded: the caches load on first use, so without a warm-up the percentiles describe
        /// a cold start rather than steady state.
        /// </summary>
        public int WarmupSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets how long to measure for.
        /// </summary>
        public int DurationSeconds { get; set; } = 120;

        /// <summary>
        /// Gets or sets how requests are paced.
        /// </summary>
        public LoadModel Model { get; set; } = LoadModel.Closed;
    }
}
