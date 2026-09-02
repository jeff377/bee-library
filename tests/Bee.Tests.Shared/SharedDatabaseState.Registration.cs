using Bee.Base;
using Bee.Db.Manager;
using Bee.Db.Providers.MySql;
using Bee.Db.Providers.Oracle;
using Bee.Db.Providers.PostgreSql;
using Bee.Db.Providers.Sqlite;
using Bee.Db.Providers.SqlServer;
using Bee.Definition.Database;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Microsoft.Data.Sqlite;

namespace Bee.Tests.Shared
{
    /// <content>
    /// Provider / dialect registration and the <c>DatabaseSettings</c> entries the test
    /// databases are addressed by. Separate from the build steps because it answers a different
    /// question — which engines exist and how to reach them, not what lives inside them.
    /// </content>
    public static partial class SharedDatabaseState
    {
        // What EnsureRegistered wrote into DatabaseSettings, kept so it can be written again.
        // The settings object is a process-wide cache slot that any test can invalidate — a
        // Save* call on an unrelated temp directory is enough — and the reload comes from
        // DatabaseSettings.xml, which knows nothing about these runtime-registered entries.
        private static readonly List<RegisteredServer> s_registeredServers = [];
        private static readonly List<RegisteredItem> s_registeredItems = [];

        private sealed record RegisteredServer(string Id, DatabaseType DatabaseType, string ConnectionString);

        private sealed record RegisteredItem(
            string Id, string CategoryId, DatabaseType DatabaseType, string ServerId, string DbName);

        // SQLite in-memory shared-cache databases live only as long as at least one
        // connection is open; hold one open per category for the lifetime of the process.
        private static readonly List<SqliteConnection> s_sqliteKeepAlive = [];

        private static List<string> GetCategoryIds(IDefineAccess access)
        {
            var settings = access.GetDbCategorySettings();
            if (settings?.Categories == null || settings.Categories.Count == 0)
            {
                // Fallback so registration still produces a usable "common" DatabaseItem
                // even when DbCategorySettings.xml is missing/empty.
                return ["common"];
            }
            return settings.Categories.Select(c => c.Id).ToList();
        }

        // Oracle 不走實體 DB 區隔（保持單一 testuser schema 容納所有 category 的表），
        // 其他 DB 由 {@DbName} 把 CategoryId 代換為實體 DB 名。
        private static string ResolveDbName(DatabaseType dbType, string categoryId)
            => dbType == DatabaseType.Oracle ? string.Empty : categoryId;

        private static void EnsureFallbackCommonDatabaseItem(IDefineAccess bootstrapAccess)
        {
            if (bootstrapAccess.GetDatabaseSettings().Items!.Contains("common")) return;
            AddDatabaseItemIfMissing(
                bootstrapAccess, id: "common", categoryId: "common",
                dbType: DatabaseType.SQLServer, serverId: string.Empty, dbName: "common");
        }

        private static void RegisterSqlServer(IDefineAccess bootstrapAccess, List<string> categoryIds)
        {
            DbProviderRegistry.Register(DatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            DbDialectRegistry.Register(DatabaseType.SQLServer, new SqlDialectFactory());

            var connStr = Environment.GetEnvironmentVariable(TestDbConventions.GetConnectionStringEnvVar(DatabaseType.SQLServer));
            if (string.IsNullOrEmpty(connStr)) return;

            RegisterServerAndItems(bootstrapAccess, DatabaseType.SQLServer, connStr, categoryIds);
            // Backward-compat: framework convention historically uses the bare "common"
            // DatabaseItem.Id for the SQL Server default. Bind it to the same server with
            // DbName="common" so the {@DbName} placeholder resolves identically to common_sqlserver.
            AddDatabaseItemIfMissing(
                bootstrapAccess,
                id: "common",
                categoryId: "common",
                dbType: DatabaseType.SQLServer,
                serverId: TestDbConventions.GetServerId(DatabaseType.SQLServer),
                dbName: "common");
        }

        private static void RegisterPostgreSql(IDefineAccess bootstrapAccess, List<string> categoryIds)
        {
            DbProviderRegistry.Register(DatabaseType.PostgreSQL, Npgsql.NpgsqlFactory.Instance);
            DbDialectRegistry.Register(DatabaseType.PostgreSQL, new PgDialectFactory());

            var connStr = Environment.GetEnvironmentVariable(TestDbConventions.GetConnectionStringEnvVar(DatabaseType.PostgreSQL));
            if (string.IsNullOrEmpty(connStr)) return;

            RegisterServerAndItems(bootstrapAccess, DatabaseType.PostgreSQL, connStr, categoryIds);
        }

        private static void RegisterSqlite(IDefineAccess bootstrapAccess, List<string> categoryIds)
        {
            DbProviderRegistry.Register(DatabaseType.SQLite, new SqliteProviderFactory(SqliteFactory.Instance));
            DbDialectRegistry.Register(DatabaseType.SQLite, new SqliteDialectFactory());

            var connStr = Environment.GetEnvironmentVariable(TestDbConventions.GetConnectionStringEnvVar(DatabaseType.SQLite));
            if (string.IsNullOrEmpty(connStr)) return;

            RegisterServerAndItems(bootstrapAccess, DatabaseType.SQLite, connStr, categoryIds);

            // Keep one open connection per category — each {@DbName} substitution maps
            // to an independent in-memory DB, and the underlying shared-cache store is
            // reclaimed once the last open connection closes.
            if (s_sqliteKeepAlive.Count == 0)
            {
                foreach (var categoryId in categoryIds)
                {
                    var resolvedConnStr = StringUtilities.Replace(connStr, "{@DbName}", categoryId);
                    var conn = new SqliteConnection(resolvedConnStr);
                    conn.Open();
                    s_sqliteKeepAlive.Add(conn);
                }
            }
        }

        private static void RegisterMySql(IDefineAccess bootstrapAccess, List<string> categoryIds)
        {
            DbProviderRegistry.Register(DatabaseType.MySQL, MySqlConnector.MySqlConnectorFactory.Instance);
            DbDialectRegistry.Register(DatabaseType.MySQL, new MySqlDialectFactory());

            var connStr = Environment.GetEnvironmentVariable(TestDbConventions.GetConnectionStringEnvVar(DatabaseType.MySQL));
            if (string.IsNullOrEmpty(connStr)) return;

            RegisterServerAndItems(bootstrapAccess, DatabaseType.MySQL, connStr, categoryIds);
        }

        private static void RegisterOracle(IDefineAccess bootstrapAccess, List<string> categoryIds)
        {
            DbProviderRegistry.Register(
                DatabaseType.Oracle,
                global::Oracle.ManagedDataAccess.Client.OracleClientFactory.Instance,
                ApplyOracleSessionSettings);
            DbDialectRegistry.Register(DatabaseType.Oracle, new OracleDialectFactory());

            var connStr = Environment.GetEnvironmentVariable(TestDbConventions.GetConnectionStringEnvVar(DatabaseType.Oracle));
            if (string.IsNullOrEmpty(connStr)) return;

            RegisterServerAndItems(bootstrapAccess, DatabaseType.Oracle, connStr, categoryIds);
        }

        private static void ApplyOracleSessionSettings(System.Data.Common.DbConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER SESSION SET NLS_COMP='LINGUISTIC' NLS_SORT='BINARY_CI'";
            cmd.ExecuteNonQuery();
        }

        private static void RegisterServerAndItems(
            IDefineAccess bootstrapAccess,
            DatabaseType dbType,
            string connStr,
            List<string> categoryIds)
        {
            var serverId = TestDbConventions.GetServerId(dbType);
            AddDatabaseServerIfMissing(bootstrapAccess, serverId, dbType, connStr);
            foreach (var categoryId in categoryIds)
            {
                var id = TestDbConventions.GetDatabaseId(dbType, categoryId);
                AddDatabaseItemIfMissing(bootstrapAccess, id, categoryId, dbType, serverId, ResolveDbName(dbType, categoryId));
            }
        }

        private static void AddDatabaseServerIfMissing(IDefineAccess bootstrapAccess, string serverId, DatabaseType dbType, string connStr)
        {
            s_registeredServers.Add(new RegisteredServer(serverId, dbType, connStr));
            var dbSettings = bootstrapAccess.GetDatabaseSettings();
            if (dbSettings.Servers!.Contains(serverId)) return;
            dbSettings.Servers.Add(NewServer(s_registeredServers[^1]));
        }

        private static DatabaseServer NewServer(RegisteredServer server) => new()
        {
            Id = server.Id,
            DatabaseType = server.DatabaseType,
            ConnectionString = server.ConnectionString
        };

        private static void AddDatabaseItemIfMissing(IDefineAccess bootstrapAccess, string id, string categoryId, DatabaseType dbType, string serverId, string dbName)
        {
            s_registeredItems.Add(new RegisteredItem(id, categoryId, dbType, serverId, dbName));
            var dbSettings = bootstrapAccess.GetDatabaseSettings();
            if (dbSettings.Items!.Contains(id)) return;
            dbSettings.Items.Add(NewItem(s_registeredItems[^1]));
        }

        // A fresh instance per application: the same entry is written to more than one
        // DatabaseSettings object over a process's life, and collection members are not shareable.
        private static DatabaseItem NewItem(RegisteredItem item) => new()
        {
            Id = item.Id,
            CategoryId = item.CategoryId,
            DatabaseType = item.DatabaseType,
            ServerId = item.ServerId,
            DbName = item.DbName,
            ConnectionString = string.Empty
        };
    }
}
