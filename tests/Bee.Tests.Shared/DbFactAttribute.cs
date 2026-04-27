using Bee.Definition.Database;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// Marks a test that requires a connection to a specific database type.
    /// The required connection string is sourced from the environment variable
    /// <c>BEE_TEST_CONNSTR_{DBTYPE}</c> (e.g. <c>BEE_TEST_CONNSTR_SQLSERVER</c>,
    /// <c>BEE_TEST_CONNSTR_POSTGRESQL</c>). When that variable is not set the test is skipped.
    /// </summary>
    public class DbFactAttribute : FactAttribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DbFactAttribute"/> for the specified database type.
        /// </summary>
        /// <param name="dbType">The database type that this test targets.</param>
        public DbFactAttribute(DatabaseType dbType)
        {
            DatabaseType = dbType;
            var envVar = TestDbConventions.GetConnectionStringEnvVar(dbType);
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar)))
                Skip = $"Skipped – requires {envVar} (test database for {dbType})";
        }

        /// <summary>
        /// Gets the database type targeted by this test.
        /// </summary>
        public DatabaseType DatabaseType { get; }
    }
}
