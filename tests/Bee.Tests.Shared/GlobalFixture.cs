using Bee.Base;
using Bee.ObjectCaching;
using Bee.Db.Manager;
using Bee.Db.Providers.PostgreSql;
using Bee.Db.Providers.Sqlite;
using Bee.Db.Providers.SqlServer;
using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Definition.Database;
using Bee.Definition.Security;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// 提供測試所需的全域初始化邏輯。除了載入定義路徑與系統設定外，依環境變數逐一註冊
    /// 各 <see cref="DatabaseType"/> 對應的 ADO.NET <c>DbProviderFactory</c> 與框架 dialect factory，
    /// 並建立 <see cref="DatabaseItem"/>（Id 命名見 <see cref="TestDbConventions.GetDatabaseId"/>）。
    /// </summary>
    public class GlobalFixture : IDisposable
    {
        // SQLite in-memory shared-cache databases live only as long as at least one
        // connection is open; once the last connection closes the database disappears.
        // Keep one connection alive for the fixture's lifetime so subsequent test
        // connections see the schema/data populated by EnsureSchema/EnsureSeedData.
        private Microsoft.Data.Sqlite.SqliteConnection? _sqliteKeepAlive;

        /// <summary>
        /// 建構函式，於第一次進入 Collection 測試時執行初始化。
        /// </summary>
        public GlobalFixture()
        {
            // 全域初始化邏輯，例如載入設定檔、建立資料庫、啟動 API
            // 設定定義路徑（相對於測試輸出目錄往上找 tests/Bee.Tests.Shared/Define）
            var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
            BackendInfo.DefinePath = Path.Combine(repoRoot, "tests", "Define");
            BackendInfo.DefineAccess = new LocalDefineAccess();
            // 系統初始化
            var settings = BackendInfo.DefineAccess.GetSystemSettings();
            settings.BackendConfiguration.Components.BusinessObjectProvider = BackendDefaultTypes.BusinessObjectProvider;
            // CI 環境改用環境變數作為 MasterKey 來源，避免在 tests/Define/ 下建立 Master.key
            // 汙染 MasterKeyProviderTests.GetMasterKey_EmptyFilePath_UsesDefaultFileName 等
            // 預期「DefinePath 下無 Master.key」的測試。
            if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
            {
                settings.BackendConfiguration.SecurityKeySettings.MasterKeySource = new MasterKeySource
                {
                    Type = MasterKeySourceType.Environment,
                    Value = "BEE_TEST_FIXTURE_MASTER_KEY"
                };
            }
            SysInfo.Initialize(settings.CommonConfiguration);
            BackendInfo.Initialize(settings.BackendConfiguration, autoCreateMasterKey: true);
            // 註冊各 DB 的 ADO.NET provider + dialect factory；每個 DB 各自獨立、未設環境變數則跳過。
            RegisterSqlServer();
            RegisterPostgreSql();
            RegisterSqlite();
            // 未來新增 MySQL / Oracle 在此擴增。
            Console.WriteLine("GlobalFixture Initialized");
        }

        /// <summary>
        /// 註冊 SQL Server 的 ADO.NET provider 與 dialect factory，並依環境變數建立 <see cref="DatabaseItem"/>。
        /// SQL Server 是測試 fixture 的「邏輯預設」（<c>BackendConfiguration.DatabaseId="common"</c>
        /// 在 SystemSettings.xml 中），所以同時註冊兩個 Id 指向同一個 SQL Server 連線：
        /// <list type="bullet">
        /// <item><c>common</c>：用於 prod code 路徑（<see cref="BackendInfo.DatabaseId"/> 預設值），
        /// 例如 <c>SessionRepository.GetSession</c> 與 <c>CacheFunc.GetTableSchema(tableName)</c>。</item>
        /// <item><c>common_sqlserver</c>：用於 <c>[DbFact(DatabaseType.SQLServer)]</c> 明確 DB 類型測試。</item>
        /// </list>
        /// </summary>
        private static void RegisterSqlServer()
        {
            DbProviderRegistry.Register(DatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            DbDialectRegistry.Register(DatabaseType.SQLServer, new SqlDialectFactory());

            var connStr = Environment.GetEnvironmentVariable(TestDbConventions.GetConnectionStringEnvVar(DatabaseType.SQLServer));
            if (string.IsNullOrEmpty(connStr)) return;

            var dbSettings = BackendInfo.DefineAccess.GetDatabaseSettings();
            dbSettings.Items!.Add(new DatabaseItem
            {
                Id = "common",
                DatabaseType = DatabaseType.SQLServer,
                ConnectionString = connStr
            });
            dbSettings.Items!.Add(new DatabaseItem
            {
                Id = TestDbConventions.GetDatabaseId(DatabaseType.SQLServer),
                DatabaseType = DatabaseType.SQLServer,
                ConnectionString = connStr
            });
        }

        /// <summary>
        /// 註冊 PostgreSQL 的 ADO.NET provider 與 dialect factory，並依環境變數建立 <see cref="DatabaseItem"/>。
        /// 與 SQL Server 不同，PG 並非 fixture 的「邏輯預設」（<see cref="BackendInfo.DatabaseId"/> 仍為 "common"
        /// 對應 SQL Server），所以只註冊一個明確的 Id（<c>common_postgresql</c>）。
        /// </summary>
        private static void RegisterPostgreSql()
        {
            DbProviderRegistry.Register(DatabaseType.PostgreSQL, Npgsql.NpgsqlFactory.Instance);
            DbDialectRegistry.Register(DatabaseType.PostgreSQL, new PgDialectFactory());

            var connStr = Environment.GetEnvironmentVariable(TestDbConventions.GetConnectionStringEnvVar(DatabaseType.PostgreSQL));
            if (string.IsNullOrEmpty(connStr)) return;

            var dbSettings = BackendInfo.DefineAccess.GetDatabaseSettings();
            dbSettings.Items!.Add(new DatabaseItem
            {
                Id = TestDbConventions.GetDatabaseId(DatabaseType.PostgreSQL),
                DatabaseType = DatabaseType.PostgreSQL,
                ConnectionString = connStr
            });
        }

        /// <summary>
        /// 註冊 SQLite 的 ADO.NET provider 與 dialect factory，並依環境變數建立 <see cref="DatabaseItem"/>。
        /// 推薦的測試連線字串是 <c>Data Source=file:bee_test_sqlite?mode=memory&amp;cache=shared</c>
        /// （in-memory + shared cache，零 IO，CI 與本機行為一致）。同時保留一條長生命週期連線
        /// 防止 in-memory database 在所有測試連線關閉時被釋放。
        /// </summary>
        private void RegisterSqlite()
        {
            DbProviderRegistry.Register(DatabaseType.SQLite, Microsoft.Data.Sqlite.SqliteFactory.Instance);
            DbDialectRegistry.Register(DatabaseType.SQLite, new SqliteDialectFactory());

            var connStr = Environment.GetEnvironmentVariable(TestDbConventions.GetConnectionStringEnvVar(DatabaseType.SQLite));
            if (string.IsNullOrEmpty(connStr)) return;

            var dbSettings = BackendInfo.DefineAccess.GetDatabaseSettings();
            dbSettings.Items!.Add(new DatabaseItem
            {
                Id = TestDbConventions.GetDatabaseId(DatabaseType.SQLite),
                DatabaseType = DatabaseType.SQLite,
                ConnectionString = connStr
            });

            // Hold one open connection for the fixture's lifetime — see field comment.
            _sqliteKeepAlive = new Microsoft.Data.Sqlite.SqliteConnection(connStr);
            _sqliteKeepAlive.Open();
        }

        private static string FindRepoRoot(string startDir)
        {
            var dir = new DirectoryInfo(startDir);
            while (dir != null)
            {
                if (dir.GetDirectories(".git").Length > 0)
                    return dir.FullName;
                dir = dir.Parent;
            }
            throw new InvalidOperationException($"Cannot find repo root from: {startDir}");
        }

        /// <summary>
        /// 測試完成後釋放資源。
        /// </summary>
        public void Dispose()
        {
            // 清理動作，例如刪除暫存資料庫、停止模擬 API Server
            _sqliteKeepAlive?.Dispose();
            _sqliteKeepAlive = null;
            Console.WriteLine("GlobalFixture Disposed");
            GC.SuppressFinalize(this);
        }
    }

}
