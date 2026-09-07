using Bee.Definition.Database;

namespace Bee.LoadTests.Configuration
{
    /// <summary>
    /// Which database the run measures.
    /// </summary>
    /// <remarks>
    /// The connection string is deliberately absent: it comes from the
    /// <c>BEE_TEST_CONNSTR_{DBTYPE}</c> environment variable, matching <c>test.sh</c>. Load-test
    /// configuration gets pasted into reports and issues, and a credential does not belong there.
    /// </remarks>
    public sealed class DatabaseOptions
    {
        /// <summary>
        /// Gets or sets the database engine to run against.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: <see cref="DatabaseType.SQLite"/> is rejected. It is a single-file
        /// embedded engine, not a server-side option, and its global write lock turns concurrent
        /// writes into a bottleneck that says nothing about a real deployment.
        /// </remarks>
        public DatabaseType Provider { get; set; } = DatabaseType.SQLServer;

        /// <summary>
        /// Gets or sets the category the scenarios read and write through.
        /// </summary>
        public string CategoryId { get; set; } = "company";
    }
}
