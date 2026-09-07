namespace Bee.LoadTests.Configuration
{
    /// <summary>
    /// One scenario in the run.
    /// </summary>
    public sealed class ScenarioOptions
    {
        /// <summary>
        /// Gets or sets the scenario name, matched against the registered scenarios.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether this scenario takes part in the run.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets how often this scenario is chosen relative to the others. A scenario
        /// weighted 5 runs five times as often as one weighted 1.
        /// </summary>
        public int Weight { get; set; } = 1;
    }
}
