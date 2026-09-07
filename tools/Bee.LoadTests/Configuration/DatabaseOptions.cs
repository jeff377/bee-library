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

        /// <summary>
        /// Gets or sets the prefix put in front of each category when resolving a database name.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: this is what keeps a run out of the test databases. The test harness creates
        /// catalogs named after the categories themselves — <c>common</c>, <c>company</c>,
        /// <c>log</c> — from the same connection string, so an unprefixed run would seed rows into
        /// the databases the unit tests depend on. That damage is quiet: tests start failing later,
        /// somewhere else, for reasons that do not point back here.
        /// <para>
        /// Only letters, digits and underscore are accepted, because the name reaches a
        /// <c>CREATE DATABASE</c> statement, where it cannot be a parameter.
        /// </para>
        /// </remarks>
        public string DatabaseNamePrefix { get; set; } = "loadtest_";

        /// <summary>
        /// Gets the database name for a category, with the prefix applied.
        /// </summary>
        /// <param name="categoryId">The category id.</param>
        /// <returns>The database name to connect to.</returns>
        public string ResolveDatabaseName(string categoryId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(categoryId);
            return DatabaseNamePrefix + categoryId;
        }
    }
}
