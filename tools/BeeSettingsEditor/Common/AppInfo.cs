using Bee.Base;
using Bee.Cache;
using Bee.Connect;
using Bee.Db;
using Bee.Define;
using Bee.Repository.Abstractions;
using Bee.UI.Core;
using Bee.UI.WinForms;

namespace SettingsEditor
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
            // 發佈為單一執行檔
            SysInfo.IsSingleFile = true;
            // 因為發佈為單一執行檔，無法動態載入物件，需由程式碼建立            
            BackendInfo.DefineStorage = new FileDefineStorage();
            BackendInfo.DefineAccess = new LocalDefineAccess();
            BackendInfo.BusinessObjectProvider = new Bee.Business.BusinessObjectProvider();            
            RepositoryInfo.SystemProvider = new Bee.Repository.SystemRepositoryProvider();
            // 允許產生設定檔
            ClientInfo.AllowGenerateSettings = true;  
            // 用戶端初始化
            if (!ClientInfo.Initialize(new UIViewService(), SupportedConnectTypes.Local)) { return false; }
            // 註冊資料庫提供者
            DbProviderManager.RegisterProvider(DatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            // 後端初始化
            var settings = BackendInfo.DefineAccess.GetSystemSettings();
            BackendInfo.Initialize(settings.BackendConfiguration, true);
            // 設定為非偵錯模式
            SysInfo.IsDebugMode = false; 
            return true;
        }
    }
}
