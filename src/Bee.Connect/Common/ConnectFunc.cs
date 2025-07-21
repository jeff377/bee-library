using System;
using System.Collections.Generic;
using System.Text;
using Bee.Define;

namespace Bee.Connect
{
    /// <summary>
    /// API 服務連結共用函式庫。
    /// </summary>
    public static class ConnectFunc
    {
        /// <summary>
        /// 設置連線方式異動的相關靜態屬性。
        /// </summary>
        /// <param name="connectType">服務連線方式。</param>
        /// <param name="endpoint">服端端點，遠端連線為網址，近端連線為本地路徑。</param>
        public static void SetConnectType(ConnectType connectType, string endpoint)
        {
            if (connectType == ConnectType.Local)
            {
                // 設定近端連線相關屬性
                FrontendInfo.ConnectType = ConnectType.Local;
                FrontendInfo.Endpoint = string.Empty;
                BackendInfo.DefinePath = endpoint;
            }
            else
            {
                // 設定遠端連線相關屬性
                FrontendInfo.ConnectType = ConnectType.Remote;
                FrontendInfo.Endpoint = endpoint;
                BackendInfo.DefinePath = string.Empty;
            }
            // 設定存取權杖令牌為空，因為連線方式變更後需要重新登入
            FrontendInfo.AccessToken = Guid.Empty;
        }
    }
}
