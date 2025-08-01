using Bee.Base;
using Bee.Cache;
using Bee.Db;
using Bee.Define;
using Bee.UI.Core;
using Bee.UI.WinForms;

namespace DbUpgrade
{
    /// <summary>
    /// 應用程式資訊。
    /// </summary>
    internal static class AppInfo
    {
        /// <summary>
        /// 應用程式初始化。
        /// </summary>
        public static bool Initialize()
        {
            // 設為工具程式模式
            SysInfo.IsToolMode = true;
            // 因為發佈為單一執行檔，無法動態載入物件，需由程式碼建立
            BackendInfo.DefineProvider = new FileDefineProvider();
            BackendInfo.BusinessObjectProvider = new Bee.Business.BusinessObjectProvider();
            // 用戶端初始化
            if (!ClientInfo.Initialize(new UIViewService(), SupportedConnectTypes.Local)) { return false; }
            // 註冊資料庫提供者
            DbProviderManager.RegisterProvider(DatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            // 初始化金鑰
            var settings = CacheFunc.GetSystemSettings();
            settings.BackendConfiguration.InitializeSecurityKeys(true);
            // 設定資料庫類型和編號
            BackendInfo.DatabaseType = settings.BackendConfiguration.DatabaseType;
            BackendInfo.DatabaseId = settings.BackendConfiguration.DatabaseId;
            // 設定為非偵錯模式
            SysInfo.IsDebugMode = false;
            return true;
        }
    }
}
