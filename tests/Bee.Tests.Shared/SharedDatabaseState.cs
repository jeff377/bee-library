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
    /// + framework <c>IDbDialectFactory</c> per <see cref="DatabaseType"/>, seeds one
    /// <see cref="DatabaseServer"/> plus one <see cref="DatabaseItem"/> per category
    /// declared in <c>DbCategorySettings.xml</c> when the corresponding
    /// <c>BEE_TEST_CONNSTR_*</c> env var is set, and (on opt-in) creates / upgrades the
    /// physical schema for every <c>(category, table)</c> in <c>DbCategorySettings.xml</c>
    /// plus inserts a seed user. All operations are idempotent and guarded by a single
    /// process-wide lock so concurrent fixture ctors don't race.
    /// </summary>
    /// <remarks>
    /// Each <see cref="DatabaseItem"/> carries <c>DbName = CategoryId</c> so the server's
    /// <c>{@DbName}</c> placeholder substitution produces a per-category physical database
    /// (e.g. SQL Server <c>common</c> + <c>company</c>). Oracle is the only exception:
    /// per-category DbName is left empty and all 5 tables live in the same testuser schema.
    /// SQLite uses <c>{@DbName}</c> as the in-memory shared-cache filename, producing one
    /// independent in-memory database per category.
    /// </remarks>
    public static class SharedDatabaseState
    {
        private static readonly Lock _registerLock = new();
        private static bool _registered;

        private static readonly Lock _schemaLock = new();
        private static bool _schemaInitialised;

        // SQLite in-memory shared-cache databases live only as long as at least one
        // connection is open; hold one open per category for the lifetime of the process.
        private static readonly List<SqliteConnection> _sqliteKeepAlive = [];

        /// <summary>
        /// Registers DB providers / dialect factories and seeds <see cref="DatabaseServer"/>
        /// + per-category <see cref="DatabaseItem"/> values for every <see cref="DatabaseType"/>
        /// whose connection string env var is set. Idempotent across the process.
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

                var categoryIds = GetCategoryIds(bootstrapAccess);
                RegisterSqlServer(bootstrapAccess, categoryIds);
                RegisterPostgreSql(bootstrapAccess, categoryIds);
                RegisterSqlite(bootstrapAccess, categoryIds);
                RegisterMySql(bootstrapAccess, categoryIds);
                RegisterOracle(bootstrapAccess, categoryIds);
                EnsureFallbackCommonDatabaseItem(bootstrapAccess);

                _registered = true;
            }
        }

        /// <summary>
        /// Verifies connectivity, creates / upgrades schemas for every
        /// <c>(category, table)</c> declared in <c>DbCategorySettings.xml</c>, and inserts
        /// seed data for every registered database. Skips any DB whose env var is unset or
        /// whose connection fails. Idempotent across the process.
        /// </summary>
        /// <param name="access">An <see cref="IDefineAccess"/> resolving the same
        /// <c>DatabaseSettings</c> that <see cref="EnsureRegistered"/> populated.</param>
        /// <param name="connectionManager">The DI-resolved connection manager.</param>
        public static void EnsureSchemaAndSeed(IDefineAccess access, IDbConnectionManager connectionManager)
        {
            ArgumentNullException.ThrowIfNull(access);
            ArgumentNullException.ThrowIfNull(connectionManager);
            lock (_schemaLock)
            {
                if (_schemaInitialised) return;

                EnsureDatabase(DatabaseType.SQLServer, access, connectionManager);
                EnsureDatabase(DatabaseType.PostgreSQL, access, connectionManager);
                EnsureDatabase(DatabaseType.SQLite, access, connectionManager);
                EnsureDatabase(DatabaseType.MySQL, access, connectionManager);
                EnsureDatabase(DatabaseType.Oracle, access, connectionManager);

                _schemaInitialised = true;
            }
        }

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
            var dbSettings = bootstrapAccess.GetDatabaseSettings();
            if (dbSettings.Items!.Contains("common")) return;
            dbSettings.Items.Add(new DatabaseItem
            {
                Id = "common",
                CategoryId = "common",
                DatabaseType = DatabaseType.SQLServer,
                DbName = "common",
                ConnectionString = string.Empty
            });
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
            if (_sqliteKeepAlive.Count == 0)
            {
                foreach (var categoryId in categoryIds)
                {
                    var resolvedConnStr = StringUtilities.Replace(connStr, "{@DbName}", categoryId);
                    var conn = new SqliteConnection(resolvedConnStr);
                    conn.Open();
                    _sqliteKeepAlive.Add(conn);
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
            var dbSettings = bootstrapAccess.GetDatabaseSettings();
            if (dbSettings.Servers!.Contains(serverId)) return;
            dbSettings.Servers.Add(new DatabaseServer
            {
                Id = serverId,
                DatabaseType = dbType,
                ConnectionString = connStr
            });
        }

        private static void AddDatabaseItemIfMissing(IDefineAccess bootstrapAccess, string id, string categoryId, DatabaseType dbType, string serverId, string dbName)
        {
            var dbSettings = bootstrapAccess.GetDatabaseSettings();
            if (dbSettings.Items!.Contains(id)) return;
            dbSettings.Items.Add(new DatabaseItem
            {
                Id = id,
                CategoryId = categoryId,
                DatabaseType = dbType,
                ServerId = serverId,
                DbName = dbName,
                ConnectionString = string.Empty
            });
        }

        private static void EnsureDatabase(DatabaseType dbType, IDefineAccess access, IDbConnectionManager connectionManager)
        {
            var envVar = TestDbConventions.GetConnectionStringEnvVar(dbType);
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar))) return;

            var commonDatabaseId = TestDbConventions.GetDatabaseId(dbType);
            try
            {
                EnsurePhysicalDatabasesExist(dbType, access);
                VerifyConnection(commonDatabaseId, connectionManager);
                EnsureSchema(dbType, access, connectionManager);
                EnsureSeedData(dbType, commonDatabaseId, connectionManager);
                // Business-table seed lives in the company database. Idempotent (per-table
                // empty check); only runs when the company category is registered.
                NorthwindTestSeed.Seed(dbType, access, connectionManager);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SharedDatabaseState: {dbType} setup skipped — {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }

        // Auto-creates the per-category physical database (e.g. company on SQL Server,
        // PostgreSQL, MySQL) when missing. Best-effort: connects to the engine's admin
        // database and runs a CREATE DATABASE statement; on permission failure the
        // exception is logged and the schema-build step will raise a clearer error.
        // Oracle and SQLite are skipped (Oracle uses single-schema mode; SQLite in-memory
        // DBs come into being on first connection).
        private static void EnsurePhysicalDatabasesExist(DatabaseType dbType, IDefineAccess access)
        {
            if (dbType == DatabaseType.Oracle || dbType == DatabaseType.SQLite) return;

            var categorySettings = access.GetDbCategorySettings();
            if (categorySettings?.Categories == null) return;

            var serverId = TestDbConventions.GetServerId(dbType);
            var dbSettings = access.GetDatabaseSettings();
            if (dbSettings.Servers == null || !dbSettings.Servers.Contains(serverId)) return;
            var serverConnStr = dbSettings.Servers[serverId].ConnectionString;

            var adminDbName = GetAdminDatabaseName(dbType);
            var adminConnStr = StringUtilities.Replace(serverConnStr, "{@DbName}", adminDbName);
            var providerFactory = DbProviderRegistry.Get(dbType);

            foreach (var category in categorySettings.Categories)
            {
                if (category.Tables == null || category.Tables.Count == 0) continue;
                var dbName = category.Id;
                try
                {
                    using var conn = providerFactory.CreateConnection()!;
                    conn.ConnectionString = adminConnStr;
                    conn.Open();
                    CreateDatabaseIfMissing(dbType, conn, dbName);
                    Console.WriteLine($"SharedDatabaseState: {dbType} physical database '{dbName}' ensured");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SharedDatabaseState: {dbType} CREATE DATABASE '{dbName}' failed (may need manual setup + grant) — {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        private static string GetAdminDatabaseName(DatabaseType dbType) => dbType switch
        {
            DatabaseType.SQLServer => "master",
            DatabaseType.PostgreSQL => "postgres",
            // MySQL: an empty initial database is valid; pick the always-present "mysql"
            // schema to avoid driver-specific edge cases when DB=empty.
            DatabaseType.MySQL => "mysql",
            _ => string.Empty
        };

        private static void CreateDatabaseIfMissing(DatabaseType dbType, System.Data.Common.DbConnection conn, string dbName)
        {
            switch (dbType)
            {
                case DatabaseType.SQLServer:
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"IF DB_ID(N'{dbName}') IS NULL CREATE DATABASE [{dbName}]";
                    cmd.ExecuteNonQuery();
                    break;
                }
                case DatabaseType.PostgreSQL:
                {
                    // PG does not support IF NOT EXISTS on CREATE DATABASE and CREATE DATABASE
                    // cannot run inside a transaction block; do an explicit existence probe first.
                    using (var probe = conn.CreateCommand())
                    {
                        probe.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{dbName}'";
                        if (probe.ExecuteScalar() != null) return;
                    }
                    using var create = conn.CreateCommand();
                    create.CommandText = $"CREATE DATABASE \"{dbName}\"";
                    create.ExecuteNonQuery();
                    break;
                }
                case DatabaseType.MySQL:
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{dbName}`";
                    cmd.ExecuteNonQuery();
                    break;
                }
            }
        }

        private static void VerifyConnection(string databaseId, IDbConnectionManager connectionManager)
        {
            using var conn = connectionManager.CreateConnection(databaseId);
            conn.Open();
            Console.WriteLine($"SharedDatabaseState: {databaseId} connection verified (State={conn.State})");
        }

        private static void EnsureSchema(DatabaseType dbType, IDefineAccess access, IDbConnectionManager connectionManager)
        {
            var settings = access.GetDbCategorySettings();
            if (settings?.Categories == null) return;

            foreach (var category in settings.Categories)
            {
                if (category.Tables == null || category.Tables.Count == 0) continue;

                var databaseId = TestDbConventions.GetDatabaseId(dbType, category.Id);
                var builder = new TableSchemaBuilder(databaseId, access, connectionManager);

                foreach (var table in category.Tables)
                {
                    bool created = builder.Execute(category.Id, table.TableName);
                    Console.WriteLine($"SharedDatabaseState: {databaseId} {table.TableName} schema — {(created ? "created/upgraded" : "up-to-date")}");
                }
            }
        }

        private static void EnsureSeedData(DatabaseType dbType, string databaseId, IDbConnectionManager connectionManager)
        {
            var dbAccess = new DbAccess(databaseId, connectionManager);

            var userRowId = EnsureSeedUser(dbType, databaseId, dbAccess);
            var companyRowId = EnsureSeedCompany(dbType, databaseId, dbAccess);
            EnsureSeedUserCompany(dbType, databaseId, dbAccess, userRowId, companyRowId);
        }

        // 表名與欄位名一律 dialect-quote：Oracle 對 unquoted 識別符自動轉 UPPERCASE，
        // 而 framework CREATE TABLE 是 quoted lowercase 形式，unquoted SELECT/INSERT
        // 會找不到 ST_USER。對其他 DB（quoted 後仍為原大小寫）行為一致。
        private static Guid EnsureSeedUser(DatabaseType dbType, string databaseId, DbAccess dbAccess)
        {
            string tbl = dbType.QuoteIdentifier("st_user");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colId = dbType.QuoteIdentifier("sys_id");
            string colName = dbType.QuoteIdentifier("sys_name");
            string colPwd = dbType.QuoteIdentifier("password");
            string colEmail = dbType.QuoteIdentifier("email");
            string colNote = dbType.QuoteIdentifier("note");
            string colInsTime = dbType.QuoteIdentifier("sys_insert_time");

            var existing = LookupRowId(dbType, dbAccess, tbl, colRowId, colId, "001");
            if (existing != Guid.Empty)
            {
                Console.WriteLine($"SharedDatabaseState: {databaseId} seed user '001' already exists (rowid={existing})");
                return existing;
            }

            var (_, now) = GetSeedExpressions(dbType);
            var newRowId = Guid.NewGuid();
            // password/email/note 使用單空白字元而非空字串：Oracle 將 empty string 視為
            // NULL，會違反 NOT NULL constraint；其他 DB 仍視為一字元字串。如此 5 DB 行為一致。
            var insert = new DbCommandSpec(DbCommandKind.NonQuery,
                $"INSERT INTO {tbl} ({colRowId}, {colId}, {colName}, {colPwd}, {colEmail}, {colNote}, {colInsTime}) " +
                $"VALUES ({{0}}, {{1}}, {{2}}, ' ', ' ', ' ', {now})",
                newRowId, "001", "測試管理員");
            dbAccess.Execute(insert);
            Console.WriteLine($"SharedDatabaseState: {databaseId} seed user '001' inserted (rowid={newRowId})");
            return newRowId;
        }

        private static Guid EnsureSeedCompany(DatabaseType dbType, string databaseId, DbAccess dbAccess)
        {
            string tbl = dbType.QuoteIdentifier("st_company");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colId = dbType.QuoteIdentifier("sys_id");
            string colName = dbType.QuoteIdentifier("sys_name");
            string colDbId = dbType.QuoteIdentifier("company_database_id");
            string colEnabled = dbType.QuoteIdentifier("enabled");
            string colInsTime = dbType.QuoteIdentifier("sys_insert_time");

            var existing = LookupRowId(dbType, dbAccess, tbl, colRowId, colId, "C001");
            if (existing != Guid.Empty)
            {
                Console.WriteLine($"SharedDatabaseState: {databaseId} seed company 'C001' already exists (rowid={existing})");
                return existing;
            }

            var (_, now) = GetSeedExpressions(dbType);
            var newRowId = Guid.NewGuid();
            // company_database_id 指向該 company 的資料庫（company-category DatabaseItem.Id），
            // 也就是 permission 表（st_role_grant / st_user_role）實際所在的庫 —— EnterCompany 會用
            // 此值載入角色權限快照。測試環境下即 company_sqlserver / company_postgresql / ...。
            var companyDbId = TestDbConventions.GetDatabaseId(dbType, "company");
            // 各方言 boolean literal：SQL Server/SQLite/MySQL/Oracle 用 1，PG 用 TRUE。
            string enabledLiteral = dbType == DatabaseType.PostgreSQL ? "TRUE" : "1";
            var insert = new DbCommandSpec(DbCommandKind.NonQuery,
                $"INSERT INTO {tbl} ({colRowId}, {colId}, {colName}, {colDbId}, {colEnabled}, {colInsTime}) " +
                $"VALUES ({{0}}, {{1}}, {{2}}, {{3}}, {enabledLiteral}, {now})",
                newRowId, "C001", "測試公司", companyDbId);
            dbAccess.Execute(insert);
            Console.WriteLine($"SharedDatabaseState: {databaseId} seed company 'C001' inserted (rowid={newRowId})");
            return newRowId;
        }

        private static void EnsureSeedUserCompany(
            DatabaseType dbType, string databaseId, DbAccess dbAccess, Guid userRowId, Guid companyRowId)
        {
            string tbl = dbType.QuoteIdentifier("st_user_company");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colUserRowId = dbType.QuoteIdentifier("user_rowid");
            string colCompanyRowId = dbType.QuoteIdentifier("company_rowid");
            string colInsTime = dbType.QuoteIdentifier("sys_insert_time");

            var check = new DbCommandSpec(DbCommandKind.Scalar,
                $"SELECT COUNT(*) FROM {tbl} WHERE {colUserRowId} = {{0}} AND {colCompanyRowId} = {{1}}",
                userRowId, companyRowId);
            var result = dbAccess.Execute(check);
            if (ValueUtilities.CInt(result.Scalar!) > 0)
            {
                Console.WriteLine($"SharedDatabaseState: {databaseId} seed user-company link already exists");
                return;
            }

            var (_, now) = GetSeedExpressions(dbType);
            var insert = new DbCommandSpec(DbCommandKind.NonQuery,
                $"INSERT INTO {tbl} ({colRowId}, {colUserRowId}, {colCompanyRowId}, {colInsTime}) " +
                $"VALUES ({{0}}, {{1}}, {{2}}, {now})",
                Guid.NewGuid(), userRowId, companyRowId);
            dbAccess.Execute(insert);
            Console.WriteLine($"SharedDatabaseState: {databaseId} seed user-company link inserted ('001' ↔ 'C001')");
        }

        // SELECT sys_rowid by business key; returns Guid.Empty if not found.
        // Handles Oracle RAW(16) (returned as byte[]) and string-storage (SQLite) alongside native Guid.
        private static Guid LookupRowId(
            DatabaseType dbType, DbAccess dbAccess, string tbl, string colRowId, string colBusinessKey, string businessKey)
        {
            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                $"SELECT {colRowId} FROM {tbl} WHERE {colBusinessKey} = {{0}}", businessKey);
            var result = dbAccess.Execute(spec);
            return ToGuid(result.Scalar);
        }

        private static Guid ToGuid(object? value)
        {
            if (value is null || value is DBNull) return Guid.Empty;
            if (value is Guid g) return g;
            if (value is byte[] b && b.Length == 16) return new Guid(b);
            if (value is string s && Guid.TryParse(s, out var parsed)) return parsed;
            return Guid.Empty;
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
