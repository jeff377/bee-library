using System.Data.Common;
using Bee.Db.Manager;
using Bee.Db.Schema;
using Bee.Definition.Database;
using Bee.Definition.Storage;
using Bee.LoadTests.Configuration;

namespace Bee.LoadTests.Bootstrap
{
    /// <summary>
    /// Creates the physical databases and tables a run measures against.
    /// </summary>
    /// <remarks>
    /// Tables are built from <c>DbCategorySettings</c>, so which tables exist is decided by the
    /// definitions rather than by a list here — the same loop the demo seeders use.
    /// </remarks>
    public static class SchemaPreparer
    {
        /// <summary>
        /// Creates each category's physical database when it does not already exist.
        /// </summary>
        /// <param name="options">The run configuration.</param>
        /// <param name="defineAccess">Definition access, for the category list.</param>
        /// <param name="connectionStringTemplate">
        /// The connection string as configured, still containing the <c>{@DbName}</c> placeholder.
        /// </param>
        public static void EnsureDatabases(
            LoadTestOptions options, IDefineAccess defineAccess, string connectionStringTemplate)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(defineAccess);
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringTemplate);

            var adminDatabase = GetAdminDatabaseName(options.Database.Provider);
            if (adminDatabase.Length == 0) { return; }

            var categories = defineAccess.GetDbCategorySettings().Categories;
            if (categories is null) { return; }

            var adminConnectionString = connectionStringTemplate.Replace(
                "{@DbName}", adminDatabase, StringComparison.Ordinal);
            var factory = DbProviderRegistry.Get(options.Database.Provider);

            foreach (var category in categories)
            {
                if (category.Tables is null || category.Tables.Count == 0) { continue; }

                var databaseName = options.Database.ResolveDatabaseName(category.Id);
                GuardDatabaseName(databaseName);

                using var connection = factory.CreateConnection()!;
                connection.ConnectionString = adminConnectionString;
                connection.Open();
                CreateDatabaseIfMissing(options.Database.Provider, connection, databaseName);
            }
        }

        /// <summary>
        /// Builds every table registered in <c>DbCategorySettings</c>. Create-if-not-exists, so
        /// running it again is harmless.
        /// </summary>
        /// <param name="defineAccess">Definition access.</param>
        /// <param name="connectionManager">The connection manager.</param>
        /// <returns>The number of tables built or confirmed.</returns>
        public static int EnsureTables(
            IDefineAccess defineAccess, IDbConnectionManager connectionManager)
        {
            ArgumentNullException.ThrowIfNull(defineAccess);
            ArgumentNullException.ThrowIfNull(connectionManager);

            var categories = defineAccess.GetDbCategorySettings().Categories;
            if (categories is null) { return 0; }

            var count = 0;
            foreach (var category in categories)
            {
                if (category.Tables is null) { continue; }

                var builder = new TableSchemaBuilder(category.Id, defineAccess, connectionManager);
                foreach (var table in category.Tables)
                {
                    builder.Execute(category.Id, table.TableName);
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Rejects a database name that cannot be safely concatenated into DDL.
        /// </summary>
        /// <param name="databaseName">The name to check.</param>
        /// <remarks>
        /// A database name cannot be a command parameter, so it is concatenated. The prefix is
        /// already restricted by configuration validation; this repeats the check over the whole
        /// resolved name, because the category half comes from a definition file rather than from
        /// the configuration that was validated.
        /// </remarks>
        private static void GuardDatabaseName(string databaseName)
        {
            foreach (var character in databaseName)
            {
                if (!char.IsAsciiLetterOrDigit(character) && character != '_')
                {
                    throw new InvalidOperationException(
                        $"Refusing to create a database named '{databaseName}': only ASCII " +
                        "letters, digits and underscore are allowed in a name that reaches DDL.");
                }
            }
        }

        private static string GetAdminDatabaseName(DatabaseType provider) => provider switch
        {
            DatabaseType.SQLServer => "master",
            DatabaseType.PostgreSQL => "postgres",
            DatabaseType.MySQL => "mysql",
            // Oracle runs in single-schema mode and has nothing to create here.
            _ => string.Empty
        };

        private static void CreateDatabaseIfMissing(
            DatabaseType provider, DbConnection connection, string databaseName)
        {
            if (provider == DatabaseType.PostgreSQL)
            {
                // PostgreSQL accepts neither IF NOT EXISTS on CREATE DATABASE nor the statement
                // inside a transaction block, so existence is probed separately first.
                using (var probe = connection.CreateCommand())
                {
                    probe.CommandText =
                        $"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'";
                    if (probe.ExecuteScalar() is not null) { return; }
                }

                using var create = connection.CreateCommand();
                create.CommandText = $"CREATE DATABASE \"{databaseName}\"";
                create.ExecuteNonQuery();
                return;
            }

            using var command = connection.CreateCommand();
            command.CommandText = provider switch
            {
                DatabaseType.SQLServer =>
                    $"IF DB_ID(N'{databaseName}') IS NULL CREATE DATABASE [{databaseName}]",
                DatabaseType.MySQL =>
                    $"CREATE DATABASE IF NOT EXISTS `{databaseName}`",
                _ => throw new NotSupportedException(
                    $"Creating a database on {provider} is not implemented in Bee.LoadTests.")
            };
            command.ExecuteNonQuery();
        }
    }
}
