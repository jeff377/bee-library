using Bee.Definition.Database;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// Naming conventions used by the test infrastructure to keep multi-database test
    /// configuration mechanically derivable from <see cref="DatabaseType"/>. Adding a
    /// new database type does not require changes here.
    /// </summary>
    public static class TestDbConventions
    {
        /// <summary>
        /// Returns the environment variable name carrying the connection string for
        /// the given database type. Convention: <c>BEE_TEST_CONNSTR_{DBTYPE}</c>
        /// where <c>{DBTYPE}</c> is the uppercase enum value name.
        /// </summary>
        /// <param name="dbType">The database type.</param>
        public static string GetConnectionStringEnvVar(DatabaseType dbType)
            => $"BEE_TEST_CONNSTR_{dbType.ToString().ToUpperInvariant()}";

        /// <summary>
        /// Returns the <see cref="Bee.Definition.Database.DatabaseItem"/>.<c>Id</c> used by
        /// tests for the given database type. Convention: <c>common_{dbtype}</c> where
        /// <c>{dbtype}</c> is the lowercase enum value name. Tests targeting a database
        /// pass this id to <c>new DbAccess(...)</c> / <c>new TableSchemaBuilder(...)</c>.
        /// </summary>
        /// <param name="dbType">The database type.</param>
        public static string GetDatabaseId(DatabaseType dbType)
            => $"common_{dbType.ToString().ToLowerInvariant()}";
    }
}
