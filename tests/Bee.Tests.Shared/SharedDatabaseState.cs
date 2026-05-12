using Bee.Base;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Db.Providers.MySql;
using Bee.Db.Providers.Oracle;
using Bee.Db.Providers.PostgreSql;
using Bee.Db.Providers.Sqlite;
using Bee.Db.Providers.SqlServer;
using Bee.Db.Schema;
using Bee.Definition.Database;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Microsoft.Data.Sqlite;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// Process-wide test DB infrastructure: registers ADO.NET <c>DbProviderFactory</c>
    /// + framework <c>IDbDialectFactory</c> per <see cref="DatabaseType"/>, seeds the
    /// matching <see cref="DatabaseItem"/> into <c>DatabaseSettings</c> when the
    /// corresponding <c>BEE_TEST_CONNSTR_*</c> env var is set, and (on opt-in)
    /// creates / upgrades the shared <c>st_user</c>/<c>st_session</c> schemas and
    /// inserts a seed user. All operations are idempotent and guarded by a single
    /// process-wide lock so concurrent fixture ctors don't race.
    /// </summary>
    /// <remarks>
    /// Phase 5 PR 5.4b extracted this helper from <c>GlobalFixture</c> /
    /// <c>DbGlobalFixture</c> so the new <see cref="BeeTestFixture"/> can opt into
    /// shared DB setup without depending on the legacy fixtures.
    /// </remarks>
    public static class SharedDatabaseState
    {
        private static readonly object _registerLock = new();
        private static bool _registered;

        private static readonly object _schemaLock = new();
        private static bool _schemaInitialised;

        // SQLite in-memory shared-cache databases live only as long as at least one
        // connection is open; hold one open for the lifetime of the process.
        private static SqliteConnection? _sqliteKeepAlive;

        /// <summary>
        /// Registers DB providers / dialect factories and seeds <see cref="DatabaseItem"/>
        /// values for every <see cref="DatabaseType"/> whose connection string env var
        /// is set. Idempotent across the process.
        /// </summary>
        /// <param name="bootstrapAccess">
        /// A <c>LocalDefineAccess</c> backed by the same <c>CacheContainer</c> the rest
        /// of the framework will read from; new items are added to its <c>DatabaseSettings</c>.
        /// </param>
        public static void EnsureRegistered(IDefineAccess bootstrapAccess)
        {
            ArgumentNullException.ThrowIfNull(bootstrapAccess);
            lock (_registerLock)
            {
                if (_registered) return;

                RegisterSqlServer(bootstrapAccess);
                RegisterPostgreSql(bootstrapAccess);
                RegisterSqlite(bootstrapAccess);
                RegisterMySql(bootstrapAccess);
                RegisterOracle(bootstrapAccess);
                EnsureFallbackCommonDatabaseItem(bootstrapAccess);

                _registered = true;
            }
        }

        /// <summary>
        /// Verifies connectivity, creates / upgrades required schemas, and inserts seed
        /// data for every registered database. Skips any DB whose env var is unset or
        /// whose connection fails. Idempotent across the process.
        /// </summary>
        /// <param name="access">An <see cref="IDefineAccess"/> resolving the same
        /// <c>DatabaseSettings</c> that <see cref="EnsureRegistered"/> populated.</param>
        public static void EnsureSchemaAndSeed(IDefineAccess access)
        {
            ArgumentNullException.ThrowIfNull(access);
            lock (_schemaLock)
            {
                if (_schemaInitialised) return;

                EnsureDatabase(DatabaseType.SQLServer, access);
                EnsureDatabase(DatabaseType.PostgreSQL, access);
                EnsureDatabase(DatabaseType.SQLite, access);
                EnsureDatabase(DatabaseType.MySQL, access);
                EnsureDatabase(DatabaseType.Oracle, access);

                _schemaInitialised = true;
            }
        }

        private static void EnsureFallbackCommonDatabaseItem(IDefineAccess bootstrapAccess)
        {
            var dbSettings = bootstrapAccess.GetDatabaseSettings();
            if (dbSettings.Items!.Contains("common")) return;
            dbSettings.Items.Add(new DatabaseItem
            {
                Id = "common",
                CategoryId = "common",
                DatabaseType = DatabaseType.SQLServer,
                ConnectionString = string.Empty
            });
        }

        private static void RegisterSqlServer(IDefineAccess bootstrapAccess)
        {
            DbProviderRegistry.Register(DatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            DbDialectRegistry.Register(DatabaseType.SQLServer, new SqlDialectFactory());

            var connStr = Environment.GetEnvironmentVariable(TestDbConventions.GetConnectionStringEnvVar(DatabaseType.SQLServer));
            if (string.IsNullOrEmpty(connStr)) return;

            AddDatabaseItemIfMissing(bootstrapAccess, "common", "common", DatabaseType.SQLServer, connStr);
            AddDatabaseItemIfMissing(bootstrapAccess, TestDbConventions.GetDatabaseId(DatabaseType.SQLServer), "common", DatabaseType.SQLServer, connStr);
        }

        private static void RegisterPostgreSql(IDefineAccess bootstrapAccess)
        {
            DbProviderRegistry.Register(DatabaseType.PostgreSQL, Npgsql.NpgsqlFactory.Instance);
            DbDialectRegistry.Register(DatabaseType.PostgreSQL, new PgDialectFactory());

            var connStr = Environment.GetEnvironmentVariable(TestDbConventions.GetConnectionStringEnvVar(DatabaseType.PostgreSQL));
            if (string.IsNullOrEmpty(connStr)) return;

            AddDatabaseItemIfMissing(bootstrapAccess, TestDbConventions.GetDatabaseId(DatabaseType.PostgreSQL), "common", DatabaseType.PostgreSQL, connStr);
        }

        private static void RegisterSqlite(IDefineAccess bootstrapAccess)
        {
            DbProviderRegistry.Register(DatabaseType.SQLite, SqliteFactory.Instance);
            DbDialectRegistry.Register(DatabaseType.SQLite, new SqliteDialectFactory());

            var connStr = Environment.GetEnvironmentVariable(TestDbConventions.GetConnectionStringEnvVar(DatabaseType.SQLite));
            if (string.IsNullOrEmpty(connStr)) return;

            AddDatabaseItemIfMissing(bootstrapAccess, TestDbConventions.GetDatabaseId(DatabaseType.SQLite), "common", DatabaseType.SQLite, connStr);

            // Hold one open connection for the lifetime of the process so the in-memory
            // shared-cache database stays alive even when all per-test connections close.
            if (_sqliteKeepAlive == null)
            {
                _sqliteKeepAlive = new SqliteConnection(connStr);
                _sqliteKeepAlive.Open();
            }
        }

        private static void RegisterMySql(IDefineAccess bootstrapAccess)
        {
            DbProviderRegistry.Register(DatabaseType.MySQL, MySqlConnector.MySqlConnectorFactory.Instance);
            DbDialectRegistry.Register(DatabaseType.MySQL, new MySqlDialectFactory());

            var connStr = Environment.GetEnvironmentVariable(TestDbConventions.GetConnectionStringEnvVar(DatabaseType.MySQL));
            if (string.IsNullOrEmpty(connStr)) return;

            AddDatabaseItemIfMissing(bootstrapAccess, TestDbConventions.GetDatabaseId(DatabaseType.MySQL), "common", DatabaseType.MySQL, connStr);
        }

        private static void RegisterOracle(IDefineAccess bootstrapAccess)
        {
            DbProviderRegistry.Register(
                DatabaseType.Oracle,
                global::Oracle.ManagedDataAccess.Client.OracleClientFactory.Instance,
                ApplyOracleSessionSettings);
            DbDialectRegistry.Register(DatabaseType.Oracle, new OracleDialectFactory());

            var connStr = Environment.GetEnvironmentVariable(TestDbConventions.GetConnectionStringEnvVar(DatabaseType.Oracle));
            if (string.IsNullOrEmpty(connStr)) return;

            AddDatabaseItemIfMissing(bootstrapAccess, TestDbConventions.GetDatabaseId(DatabaseType.Oracle), "common", DatabaseType.Oracle, connStr);
        }

        private static void ApplyOracleSessionSettings(System.Data.Common.DbConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER SESSION SET NLS_COMP='LINGUISTIC' NLS_SORT='BINARY_CI'";
            cmd.ExecuteNonQuery();
        }

        private static void AddDatabaseItemIfMissing(IDefineAccess bootstrapAccess, string id, string categoryId, DatabaseType dbType, string connStr)
        {
            var dbSettings = bootstrapAccess.GetDatabaseSettings();
            if (dbSettings.Items!.Contains(id)) return;
            dbSettings.Items.Add(new DatabaseItem
            {
                Id = id,
                CategoryId = categoryId,
                DatabaseType = dbType,
                ConnectionString = connStr
            });
        }

        private static void EnsureDatabase(DatabaseType dbType, IDefineAccess access)
        {
            var envVar = TestDbConventions.GetConnectionStringEnvVar(dbType);
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar))) return;

            var databaseId = TestDbConventions.GetDatabaseId(dbType);
            try
            {
                VerifyConnection(databaseId);
                EnsureSchema(databaseId, access);
                EnsureSeedData(dbType, databaseId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SharedDatabaseState: {dbType} setup skipped — {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }

        private static void VerifyConnection(string databaseId)
        {
            using var conn = DbConnectionManager.CreateConnection(databaseId);
            conn.Open();
            Console.WriteLine($"SharedDatabaseState: {databaseId} connection verified (State={conn.State})");
        }

        private static void EnsureSchema(string databaseId, IDefineAccess access)
        {
            var builder = new TableSchemaBuilder(databaseId, access);

            bool created = builder.Execute("common", "st_user");
            Console.WriteLine($"SharedDatabaseState: {databaseId} st_user schema — {(created ? "created/upgraded" : "up-to-date")}");

            created = builder.Execute("common", "st_session");
            Console.WriteLine($"SharedDatabaseState: {databaseId} st_session schema — {(created ? "created/upgraded" : "up-to-date")}");
        }

        private static void EnsureSeedData(DatabaseType dbType, string databaseId)
        {
            var dbAccess = new DbAccess(databaseId);

            // 表名與欄位名一律 dialect-quote：Oracle 對 unquoted 識別符自動轉 UPPERCASE，
            // 而 framework CREATE TABLE 是 quoted lowercase 形式，unquoted SELECT/INSERT
            // 會找不到 ST_USER。對其他 DB（quoted 後仍為原大小寫）行為一致。
            string tbl = dbType.QuoteIdentifier("st_user");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colId = dbType.QuoteIdentifier("sys_id");
            string colName = dbType.QuoteIdentifier("sys_name");
            string colPwd = dbType.QuoteIdentifier("password");
            string colEmail = dbType.QuoteIdentifier("email");
            string colNote = dbType.QuoteIdentifier("note");
            string colInsTime = dbType.QuoteIdentifier("sys_insert_time");

            var check = new DbCommandSpec(DbCommandKind.Scalar,
                $"SELECT COUNT(*) FROM {tbl} WHERE {colId} = {{0}}", "001");
            var result = dbAccess.Execute(check);

            if (ValueUtilities.CInt(result.Scalar!) == 0)
            {
                var (uuid, now) = GetSeedExpressions(dbType);
                // password/email/note 使用單空白字元而非空字串：Oracle 將 empty string 視為
                // NULL，會違反 NOT NULL constraint；其他 DB 仍視為一字元字串。如此 5 DB 行為一致。
                var insert = new DbCommandSpec(DbCommandKind.NonQuery,
                    $"INSERT INTO {tbl} ({colRowId}, {colId}, {colName}, {colPwd}, {colEmail}, {colNote}, {colInsTime}) " +
                    $"VALUES ({uuid}, {{0}}, {{1}}, ' ', ' ', ' ', {now})",
                    "001", "測試管理員");
                dbAccess.Execute(insert);
                Console.WriteLine($"SharedDatabaseState: {databaseId} seed user '001' inserted");
            }
            else
            {
                Console.WriteLine($"SharedDatabaseState: {databaseId} seed user '001' already exists");
            }
        }

        private static (string Uuid, string Now) GetSeedExpressions(DatabaseType dbType)
        {
            switch (dbType)
            {
                case DatabaseType.SQLServer:
                    return ("NEWID()", "GETDATE()");
                case DatabaseType.PostgreSQL:
                    return ("gen_random_uuid()", "CURRENT_TIMESTAMP");
                case DatabaseType.SQLite:
                    // SQLite has no native UUID generator; hex(randomblob(16)) is unique enough
                    // for seed data even though it isn't a v4 UUID.
                    return ("hex(randomblob(16))", "CURRENT_TIMESTAMP");
                case DatabaseType.MySQL:
                    return ("UUID()", "CURRENT_TIMESTAMP(6)");
                case DatabaseType.Oracle:
                    return ("SYS_GUID()", "SYSTIMESTAMP");
                default:
                    // NOTE: when adding a new DatabaseType, add a case here as well —
                    // otherwise SharedDatabaseState will throw at fixture init time
                    // once a connection string for the new DB is provided.
                    throw new NotSupportedException($"Seed expressions are not defined for {dbType}.");
            }
        }
    }
}
