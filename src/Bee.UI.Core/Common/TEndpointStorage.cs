using Bee.Base;

namespace Bee.UI.Core
{
    /// <summary>
    /// 預設服務端點儲存實作。
    /// </summary>
    public class TEndpointStorage : IEndpointStorage
    {

        /// <summary>
        /// 取得服務端點。
        /// </summary>
        public string LoadEndpoint()
        {
            return ClientInfo.ClientSettings.Endpoint;
        }

        /// <summary>
        /// 設定服務端點。
        /// </summary>
        /// <param name="endpoint">服務端點。</param>
        public void SetEndpoint(string endpoint)
        {
            ClientInfo.ClientSettings.Endpoint = endpoint;
        }

        /// <summary>
        /// 設定並儲存服務端點。
        /// </summary>
        public void SaveEndpoint(string endpoint)
        {
            // 儲存用戶端設定
            ClientInfo.ClientSettings.Endpoint = endpoint;
            ClientInfo.ClientSettings.Save();
        }


    }
}
