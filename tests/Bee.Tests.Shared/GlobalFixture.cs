using Bee.Api.AspNetCore;
using Bee.Base;
using Bee.ObjectCaching;
using Bee.Db.Manager;
using Bee.Db.Providers.MySql;
using Bee.Db.Providers.Oracle;
using Bee.Db.Providers.PostgreSql;
using Bee.Db.Providers.Sqlite;
using Bee.Db.Providers.SqlServer;
using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Definition.Database;
using Bee.Definition.Security;
using Bee.Definition.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// 提供測試所需的全域初始化邏輯。除了載入定義路徑與系統設定外，依環境變數逐一註冊
    /// 各 <see cref="DatabaseType"/> 對應的 ADO.NET <c>DbProviderFactory</c> 與框架 dialect factory，
    /// 並建立 <see cref="DatabaseItem"/>（Id 命名見 <see cref="TestDbConventions.GetDatabaseId"/>）。
    /// </summary>
    public class GlobalFixture : IDisposable
    {
        // VS Code Test Explorer 走 single-host 模式時,9 個 test collection 各自會新建一個
        // GlobalFixture/DbGlobalFixture instance,並行進入 ctor。BeeTestServices.Provider /
        // DbProviderRegistry 等都是 process-wide static,並行 init 會造成 KeyedCollection 重複 Add 等
        // 問題,連帶讓 [Collection("Initialize")] 測試大量失敗。
        // terminal 的 dotnet test 對每個 assembly 開獨立 process,沒有此 race。
        // 用 lock + once flag 確保整個 process 內只執行一次 init,後續 fixture instance 直接 return。
        private static readonly object _initLock = new();
        private static bool _initialized;

        // SQLite in-memory shared-cache databases live only as long as at least one
        // connection is open; once the last connection closes the database disappears.
        // Static + never disposed in fixture: in single-host mode the first fixture would
        // dispose first while other parallel fixtures still need the in-memory schema;
        // OS reclaims the connection at process exit anyway.
        private static Microsoft.Data.Sqlite.SqliteConnection? _sqliteKeepAlive;

        /// <summary>
        /// 建構函式,於第一次進入 Collection 測試時執行初始化。
        /// 在 single-host 模式下,後續 fixture instance 會 short-circuit 直接 return。
        /// </summary>
        public GlobalFixture()
        {
            lock (_initLock)
            {
                if (_initialized) return;
                InitializeOnce();
                _initialized = true;
            }
        }

        private static void InitializeOnce()
        {
            // 全域初始化邏輯,例如載入設定檔、建立資料庫、啟動 API
            // 設定定義路徑（相對於測試輸出目錄往上找 tests/Bee.Tests.Shared/Define）
            var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
            var pathOptions = new PathOptions
            {
                DefinePath = Path.Combine(repoRoot, "tests", "Define")
            };
            // DefinePathInfo 仍由 cache 層讀取 (Phase 5 PR 5.3 才會解耦)；同步設定避免 race。
            DefinePathInfo.Initialize(pathOptions);

            // Bootstrap 暫時用一個 DefineAccess 讓 RegisterXxx() / EnsureFallbackCommonDatabaseItem
            // 在 AddBeeFramework 執行前就能寫入 DatabaseSettings.Items；CacheContainer 必須先初始化
            // 才能讓 LocalDefineAccess 透過快取讀寫 DatabaseSettings。
            // ⚠ 這個 bootstrap access 只活在 InitializeOnce scope 內，不對外公開；DI 容器內由
            // AddBeeFramework 重新建立正式的 IDefineAccess 實例。
            var bootstrapStorage = new FileDefineStorage(pathOptions);
            CacheContainer.Initialize(bootstrapStorage);
            var bootstrapAccess = new LocalDefineAccess(bootstrapStorage, pathOptions);
            // 註冊各 DB 的 ADO.NET provider + dialect factory + DatabaseItem（依環境變數）；
            // 必須先於 AddBeeFramework，因為 startup 過程的 DatabaseSettings 驗證會檢查
            // Id='common' 的 DatabaseItem 是否存在。
            RegisterSqlServer(bootstrapAccess);
            RegisterPostgreSql(bootstrapAccess);
            RegisterSqlite(bootstrapAccess);
            RegisterMySql(bootstrapAccess);
            RegisterOracle(bootstrapAccess);
            // Fallback：若上述 DB 環境變數都未設（無 DB 整合測試的情境），
            // 仍需確保 Id='common' 存在,以通過 startup 驗證。
            EnsureFallbackCommonDatabaseItem(bootstrapAccess);
            // 系統初始化：boot-time 讀檔走 SystemSettingsLoader（不依賴 IDefineAccess）。
            // 與 runtime cache-backed 路徑分工，詳見 plan-backendinfo-di-phase0-systemsettings-loader.md。
            var settings = SystemSettingsLoader.Load(pathOptions);
            settings.BackendConfiguration.Components.BusinessObjectFactory = BackendDefaultTypes.BusinessObjectFactory;
            // CI 環境改用環境變數作為 MasterKey 來源,避免在 tests/Define/ 下建立 Master.key
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

            // 用 AddBeeFramework 建 DI 容器，取代原本的 BackendInfo.Initialize。
            var services = new ServiceCollection();
            services.AddBeeFramework(settings.BackendConfiguration, pathOptions, autoCreateMasterKey: true);
            var provider = services.BuildServiceProvider();
            // 顯式 eager-resolve bootstrappers（等同 production app.UseBeeFramework() 的效果），
            // 觸發 CacheContainer / DbConnectionManager 兩個 process-wide 靜態 wire-up。
            // RepositoryInfo bootstrapper 已於 PR 5.3a 隨 RepositoryInfo 靜態類別一起刪除。
            provider.GetRequiredService<Bee.Api.AspNetCore.Bootstrapping.ICacheBootstrapper>();
            provider.GetRequiredService<Bee.Api.AspNetCore.Bootstrapping.IDbConnectionManagerBootstrapper>();

            BeeTestServices.Initialize(provider);
            // Bee.Api.Client 近端模式（in-process）需要透過 ApiClientInfo.LocalServiceProvider
            // 取得後端服務；測試 fixture 預設指向同一個 process-wide 容器。Phase 4 transitional —
            // 主計畫 §「範圍邊界」說明此 holder 是 Bee.Api.Client 重構前的暫時做法。
            Bee.Api.Client.ApiClientInfo.LocalServiceProvider = provider;
            Console.WriteLine("GlobalFixture Initialized");
        }

        /// <summary>
        /// 確保 DatabaseSettings 含有 Id='common' 的 DatabaseItem,作為 startup 驗證的後備.
        /// 純單元測試（無 DB 整合測試的情境）下,連線字串保持空字串,任何試圖實際連線的測試
        /// 會自然失敗——但純邏輯/序列化測試只關心 startup 通過,不需實際連線。
        /// </summary>
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

        /// <summary>
        /// 註冊 SQL Server 的 ADO.NET provider 與 dialect factory，並依環境變數建立 <see cref="DatabaseItem"/>。
        /// SQL Server 是測試 fixture 的「邏輯預設」，所以同時註冊兩個 Id 指向同一個 SQL Server 連線：
        /// <list type="bullet">
        /// <item><c>common</c>：對應 framework 慣例 <c>DbCategoryIds.Common</c>，prod code 路徑使用
        /// （例如 <c>SessionRepository.GetSession</c> 與 <c>CacheContainer.TableSchema.Get(categoryId, tableName)</c>）。</item>
        /// <item><c>common_sqlserver</c>：用於 <c>[DbFact(DatabaseType.SQLServer)]</c> 明確 DB 類型測試。</item>
        /// </list>
        /// </summary>
        private static void RegisterSqlServer(IDefineAccess bootstrapAccess)
        {
            DbProviderRegistry.Register(DatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            DbDialectRegistry.Register(DatabaseType.SQLServer, new SqlDialectFactory());

            var connStr = Environment.GetEnvironmentVariable(TestDbConventions.GetConnectionStringEnvVar(DatabaseType.SQLServer));
            if (string.IsNullOrEmpty(connStr)) return;

            AddDatabaseItemIfMissing(bootstrapAccess, "common", "common", DatabaseType.SQLServer, connStr);
            AddDatabaseItemIfMissing(bootstrapAccess, TestDbConventions.GetDatabaseId(DatabaseType.SQLServer), "common", DatabaseType.SQLServer, connStr);
        }

        /// <summary>
        /// 註冊 PostgreSQL 的 ADO.NET provider 與 dialect factory，並依環境變數建立 <see cref="DatabaseItem"/>。
        /// 與 SQL Server 不同，PG 並非 fixture 的「邏輯預設」（framework 慣例 <c>DbCategoryIds.Common</c>
        /// 對應 SQL Server），所以只註冊一個明確的 Id（<c>common_postgresql</c>）。
        /// </summary>
        private static void RegisterPostgreSql(IDefineAccess bootstrapAccess)
        {
            DbProviderRegistry.Register(DatabaseType.PostgreSQL, Npgsql.NpgsqlFactory.Instance);
            DbDialectRegistry.Register(DatabaseType.PostgreSQL, new PgDialectFactory());

            var connStr = Environment.GetEnvironmentVariable(TestDbConventions.GetConnectionStringEnvVar(DatabaseType.PostgreSQL));
            if (string.IsNullOrEmpty(connStr)) return;

            AddDatabaseItemIfMissing(bootstrapAccess, TestDbConventions.GetDatabaseId(DatabaseType.PostgreSQL), "common", DatabaseType.PostgreSQL, connStr);
        }

        /// <summary>
        /// 註冊 SQLite 的 ADO.NET provider 與 dialect factory，並依環境變數建立 <see cref="DatabaseItem"/>。
        /// 推薦的測試連線字串是 <c>Data Source=file:bee_test_sqlite?mode=memory&amp;cache=shared</c>
        /// （in-memory + shared cache，零 IO，CI 與本機行為一致）。同時保留一條長生命週期連線
        /// 防止 in-memory database 在所有測試連線關閉時被釋放。
        /// </summary>
        private static void RegisterSqlite(IDefineAccess bootstrapAccess)
        {
            DbProviderRegistry.Register(DatabaseType.SQLite, Microsoft.Data.Sqlite.SqliteFactory.Instance);
            DbDialectRegistry.Register(DatabaseType.SQLite, new SqliteDialectFactory());

            var connStr = Environment.GetEnvironmentVariable(TestDbConventions.GetConnectionStringEnvVar(DatabaseType.SQLite));
            if (string.IsNullOrEmpty(connStr)) return;

            AddDatabaseItemIfMissing(bootstrapAccess, TestDbConventions.GetDatabaseId(DatabaseType.SQLite), "common", DatabaseType.SQLite, connStr);

            // Hold one open connection for the entire process lifetime — see field comment.
            if (_sqliteKeepAlive == null)
            {
                _sqliteKeepAlive = new Microsoft.Data.Sqlite.SqliteConnection(connStr);
                _sqliteKeepAlive.Open();
            }
        }

        /// <summary>
        /// 註冊 MySQL 的 ADO.NET provider 與 dialect factory，並依環境變數建立 <see cref="DatabaseItem"/>。
        /// MySQL 並非 fixture 的「邏輯預設」（framework 慣例 <c>DbCategoryIds.Common</c>
        /// 對應 SQL Server），所以只註冊一個明確的 Id（<c>common_mysql</c>）。
        /// 本機未設 <c>BEE_TEST_CONNSTR_MYSQL</c>（如未跑 MySQL container）則僅完成 dialect 註冊，
        /// 後續以 <c>[DbFact(DatabaseType.MySQL)]</c> 標記的整合測試會自動跳過。
        /// </summary>
        private static void RegisterMySql(IDefineAccess bootstrapAccess)
        {
            DbProviderRegistry.Register(DatabaseType.MySQL, MySqlConnector.MySqlConnectorFactory.Instance);
            DbDialectRegistry.Register(DatabaseType.MySQL, new MySqlDialectFactory());

            var connStr = Environment.GetEnvironmentVariable(TestDbConventions.GetConnectionStringEnvVar(DatabaseType.MySQL));
            if (string.IsNullOrEmpty(connStr)) return;

            AddDatabaseItemIfMissing(bootstrapAccess, TestDbConventions.GetDatabaseId(DatabaseType.MySQL), "common", DatabaseType.MySQL, connStr);
        }

        /// <summary>
        /// 註冊 Oracle 的 ADO.NET provider 與 dialect factory，並依環境變數建立 <see cref="DatabaseItem"/>。
        /// Oracle 並非 fixture 的「邏輯預設」（framework 慣例 <c>DbCategoryIds.Common</c>
        /// 對應 SQL Server），所以只註冊一個明確的 Id（<c>common_oracle</c>）。
        /// 同時掛上 connection-open hook：每次新連線開啟後執行
        /// <c>ALTER SESSION SET NLS_COMP='LINGUISTIC' NLS_SORT='BINARY_CI'</c>，讓字串比對在
        /// session 範圍內 case-insensitive，符合 ERP 預設行為（詳見 plan-oracle-support.md）。
        /// 本機未設 <c>BEE_TEST_CONNSTR_ORACLE</c> 則僅完成 dialect 註冊，後續以
        /// <c>[DbFact(DatabaseType.Oracle)]</c> 標記的整合測試會自動跳過。
        /// </summary>
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

        /// <summary>
        /// 套用 Oracle session 級設定：linguistic comparison + case-insensitive sort。
        /// 由 <see cref="DbProviderRegistry"/> 的 connection-open hook 在每次新連線開啟後呼叫。
        /// </summary>
        private static void ApplyOracleSessionSettings(System.Data.Common.DbConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER SESSION SET NLS_COMP='LINGUISTIC' NLS_SORT='BINARY_CI'";
            cmd.ExecuteNonQuery();
        }

        // Idempotent helper：當同一個 process 載入多個 test assembly 時（例如 VS Code Test Explorer
        // 走 single-host 模式），各 assembly 都會新建 GlobalFixture，但 DatabaseSettings.Items 是
        // process-wide static — 若直接 Add 已存在的 Id，KeyedCollection 會丟 ArgumentException
        // 拖垮整個 fixture，連帶讓所有 [Collection("Initialize")] 測試失敗。
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
        /// <remarks>
        /// 注意:在 single-host 模式下多個 fixture instance 會分別 dispose,
        /// 不可在此關閉 <c>_sqliteKeepAlive</c>(它是 process-wide singleton,
        /// 提早關閉會讓其他並行 collection 的 in-memory db 消失)。OS 會在
        /// process exit 時自動回收連線。
        /// </remarks>
        public void Dispose()
        {
            Console.WriteLine("GlobalFixture Disposed");
            GC.SuppressFinalize(this);
        }
    }

}
