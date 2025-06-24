using System.Collections.Generic;
using System.Net.Http;

namespace Bee.Base
{
    /// <summary>
    /// 提供一個簡單的 HttpClient 工廠，用於管理和重用 HttpClient 實例。
    /// </summary>
    public class HttpClientFactory
    {
        /// <summary>
        /// 儲存已創建的 HttpClient 實例，鍵為 HttpClient 的名稱。
        /// </summary>
        private readonly Dictionary<string, HttpClient> _httpClients = new Dictionary<string, HttpClient>();

        /// <summary>
        /// 創建或獲取指定名稱的 HttpClient 實例。
        /// 如果該名稱的 HttpClient 已經存在，則返回現有實例；
        /// 如果尚未創建，則會創建新的 HttpClient 並存入字典，以便後續重用。
        /// </summary>
        /// <param name="name">HttpClient 的唯一名稱。</param>
        /// <returns>對應名稱的 HttpClient 實例。</returns>
        public HttpClient CreateClient(string name)
        {
            if (_httpClients.ContainsKey(name))
            {
                return _httpClients[name];
            }

            var client = new HttpClient();
            _httpClients[name] = client;

            return client;
        }

        /// <summary>
        /// 移除指定名稱的 HttpClient 實例。
        /// </summary>
        /// <param name="name">要移除的 HttpClient 名稱。</param>
        /// <returns>如果成功移除則返回 true，否則返回 false。</returns>
        public bool RemoveClient(string name)
        {
            return _httpClients.Remove(name);
        }

        /// <summary>
        /// 清除所有已創建的 HttpClient 實例。
        /// </summary>
        public void ClearClients()
        {
            _httpClients.Clear();
        }
    }


}
