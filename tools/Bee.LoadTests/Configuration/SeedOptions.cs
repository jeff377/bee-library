namespace Bee.LoadTests.Configuration
{
    /// <summary>
    /// How much data to plant before measuring.
    /// </summary>
    /// <remarks>
    /// Row count is configuration rather than a constant because it changes the answer: a list
    /// query over a hundred rows and one over a hundred thousand are different measurements, and
    /// comparing runs is only meaningful when both state how much data they ran against.
    /// </remarks>
    public sealed class SeedOptions
    {
        /// <summary>
        /// Gets or sets whether to create the schema and plant data before the run. Turn this off
        /// to measure against a database that is already populated.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets how many rows to plant into each business table.
        /// </summary>
        public int RowCount { get; set; } = 1000;
    }
}
