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

        /// <summary>
        /// Gets or sets the program this scenario acts on. Empty leaves the scenario's own
        /// default in place; sign-in scenarios ignore it.
        /// </summary>
        public string ProgId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the page size for list queries.
        /// </summary>
        /// <remarks>
        /// Paging is applied on purpose. An unpaged list query loads every matching row into
        /// memory on both ends, so it would measure the cost of materialising the whole seeded
        /// table rather than the query a screen actually issues.
        /// </remarks>
        public int PageSize { get; set; } = 50;
    }
}
