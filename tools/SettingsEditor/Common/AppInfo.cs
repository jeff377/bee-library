using Bee.Db;
using Bee.Define;
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
            // 用戶端初始化
            if (!ClientInfo.Initialize(new TUIViewService(), ESupportedConnectTypes.Both, true)) { return false; }

            // 因為發佈為單一執行檔，BackendInfo 無法動態載入物件，需改由預載入方法
            if (FrontendInfo.ConnectType == EConnectType.Local)
            {
                BackendInfo.DefineProvider = new TFileDefineProvider();
                BackendInfo.BusinessObjectProvider = new Bee.Cache.TBusinessObjectProvider();
                BackendInfo.SystemObject = new Bee.Business.TSystemBusinessObject();
                // 註冊資料庫提供者
                DbProviderManager.RegisterProvider(EDatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            }

            return true;
        }
    }
}
