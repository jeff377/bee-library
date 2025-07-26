using Bee.Cache;
using Bee.Db;
using Bee.Define;
using System;
using Xunit;

namespace Bee.Api.Core.UnitTests
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
            // 設定定義路徑
            BackendInfo.DefinePath = @"D:\DefinePath";
            // 系統初始化
            var settings = CacheFunc.GetSystemSettings();
            settings.Initialize();
            // 註冊資料庫提供者
            DbProviderManager.RegisterProvider(DatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            // .NET 8 預設停用 BinaryFormatter，需手動啟用
            AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);

            Console.WriteLine("GlobalFixture Initialized");
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
