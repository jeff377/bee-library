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
        /// Returns the <see cref="Bee.Definition.Settings.DatabaseItem"/>.<c>Id</c> used by
        /// tests for the given database type, defaulting to the <c>common</c> category.
        /// Convention: <c>common_{dbtype}</c> where <c>{dbtype}</c> is the lowercase enum
        /// value name. Tests targeting a database pass this id to <c>new DbAccess(...)</c>
        /// / <c>new TableSchemaBuilder(...)</c>.
        /// </summary>
        /// <param name="dbType">The database type.</param>
        public static string GetDatabaseId(DatabaseType dbType)
            => GetDatabaseId(dbType, "common");

        /// <summary>
        /// Returns the <see cref="Bee.Definition.Settings.DatabaseItem"/>.<c>Id</c> used by
        /// tests for the given database type + category. Convention:
        /// <c>{categoryId}_{dbtype}</c> (e.g. <c>company_oracle</c>).
        /// </summary>
        /// <param name="dbType">The database type.</param>
        /// <param name="categoryId">The logical category id (e.g. <c>common</c>, <c>company</c>, <c>log</c>).</param>
        public static string GetDatabaseId(DatabaseType dbType, string categoryId)
            => $"{categoryId}_{dbType.ToString().ToLowerInvariant()}";

        /// <summary>
        /// Returns the <see cref="Bee.Definition.Settings.DatabaseServer"/>.<c>Id</c> used by
        /// tests for the given database type. Convention: <c>server_{dbtype}</c>. A single
        /// server per <see cref="DatabaseType"/> backs every category-scoped
        /// <see cref="Bee.Definition.Settings.DatabaseItem"/> via <c>ServerId</c>.
        /// </summary>
        /// <param name="dbType">The database type.</param>
        public static string GetServerId(DatabaseType dbType)
            => $"server_{dbType.ToString().ToLowerInvariant()}";
    }
}
