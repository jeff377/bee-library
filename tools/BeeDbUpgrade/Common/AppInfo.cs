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
            // 用戶端初始化
            if (!ClientInfo.Initialize(new UIViewService(), SupportedConnectTypes.Both)) { return false; }

            if (FrontendInfo.ConnectType == ConnectType.Local)
            {
                // 因為發佈為單一執行檔，BackendInfo 無法動態載入物件，需改由預載入方法
                BackendInfo.DefineProvider = new FileDefineProvider();
                BackendInfo.BusinessObjectProvider = new Bee.Business.BusinessObjectProvider();
                // 註冊資料庫提供者
                DbProviderManager.RegisterProvider(DatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
                // 系統設定初始化
                var settings = ClientInfo.DefineAccess.GetSystemSettings();
                settings.Initialize();
            }

            return true;
        }
    }
}
