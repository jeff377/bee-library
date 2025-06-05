using Bee.Base;

namespace Bee.UI.Core
{
    /// <summary>
    /// 預設服務端點儲存實作。
    /// </summary>
    public class TEndpointStorage : IEndpointStorage
    {
        /// <summary>
        /// 儲存服務端點。
        /// </summary>
        public void SaveEndpoint(string endpoint)
        {
            // 儲存用戶端設定
            ClientInfo.ClientSettings.Endpoint = endpoint;
            ClientInfo.ClientSettings.Save();
        }

        /// <summary>
        /// 取得服務端點。
        /// </summary>
        public string LoadEndpoint()
        {
            return ClientInfo.ClientSettings.Endpoint;
        }
    }
}
