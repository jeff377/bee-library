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
        /// Gets or sets how many rows to plant into each listed table.
        /// </summary>
        public int RowCount { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the tables to plant rows into, named as they appear in the definitions.
        /// </summary>
        /// <remarks>
        /// NOTE: rows are planted independently per table — relation columns get generated values
        /// rather than keys resolved against another table. That is enough for read scenarios,
        /// which measure the query rather than what the rows mean; a scenario that needs a
        /// master and its details to line up has to seed them itself.
        /// </remarks>
        public List<string> Tables { get; set; } = ["ft_customer"];
    }
}
