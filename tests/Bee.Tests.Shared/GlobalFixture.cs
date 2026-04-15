using Bee.Base;
using Bee.ObjectCaching;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Settings;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// 提供測試所需的全域初始化邏輯，例如資料庫、API 環境的啟動與釋放資源。
    /// </summary>
    public class GlobalFixture : IDisposable
    {
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
            SysInfo.Initialize(settings.CommonConfiguration);
            BackendInfo.Initialize(settings.BackendConfiguration, autoCreateMasterKey: true);
            // 註冊資料庫提供者
            DbProviderManager.RegisterProvider(DatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            // 從環境變數載入測試資料庫連線字串
            var connStr = Environment.GetEnvironmentVariable("BEE_TEST_DB_CONNSTR");
            if (!string.IsNullOrEmpty(connStr))
            {
                var dbSettings = BackendInfo.DefineAccess.GetDatabaseSettings();
                dbSettings.Items.Add(new DatabaseItem
                {
                    Id = "common",
                    DatabaseType = DatabaseType.SQLServer,
                    ConnectionString = connStr
                });
            }
            Console.WriteLine("GlobalFixture Initialized");
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
            Console.WriteLine("GlobalFixture Disposed");
        }
    }

}
