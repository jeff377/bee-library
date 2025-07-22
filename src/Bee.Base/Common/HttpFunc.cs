using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Bee.Base
{
    /// <summary>
    /// HTTP 相關函式庫。
    /// </summary>
    public static class HttpFunc
    {
        private static readonly ConcurrentDictionary<string, HttpClient> _clientMap = new ConcurrentDictionary<string, HttpClient>();

        /// <summary>
        /// 建立或取得對應網站主機的 <see cref="HttpClient"/> 實例。
        /// </summary>
        /// <param name="fullUrl">API 的完整網址，例如 https://api.example.com/v1/login。</param>
        /// <returns>共用的 <see cref="HttpClient"/> 實例，可重複使用同一連線池。</returns>
        /// <remarks>
        /// 此方法會依據網址的 <c>Schema + Host + Port</c> 建立唯一快取 Key，避免同站建立過多 <c>HttpClient</c> 實例，
        /// 有效解決 Socket Exhaustion 與 DNS 快取問題。
        /// </remarks>
        private static HttpClient GetOrCreateClient(string fullUrl)
        {
            var baseUri = new Uri(fullUrl);
            string cacheKey = $"{baseUri.Scheme}://{baseUri.Host}:{baseUri.Port}";

            return _clientMap.GetOrAdd(cacheKey, _ =>
            {
                return new HttpClient
                {
                    BaseAddress = new Uri($"{baseUri.Scheme}://{baseUri.Host}:{baseUri.Port}/")
                };
            });
        }

        /// <summary>
        /// 判斷是否為 URL。
        /// </summary>
        /// <param name="input">輸入 URL。</param>
        public static bool IsUrl(string input)
        {
            Uri uriResult;
            return Uri.TryCreate(input, UriKind.Absolute, out uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// 非同步發送 POST 請求。
        /// </summary>
        /// <param name="endpoint">服務端點。</param>
        /// <param name="body">在 Body 區傳遞的 JSON 字串。</param>
        /// <param name="headers">自訂標頭。</param>
        public static async Task<string> PostAsync(string endpoint, string body, NameValueCollection headers = null)
        {
            HttpClient client = GetOrCreateClient(endpoint);

            // 發送 POST 請求
            using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                if (headers != null)
                {
                    foreach (string key in headers)
                    {
                        request.Headers.TryAddWithoutValidation(key, headers[key]);
                    }
                }

                using (HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();  // 確認是否成功
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false); // 讀取回應內容
                }
            }
        }

        /// <summary>
        /// 非同步發送 GET 請求。
        /// </summary>
        /// <param name="endpoint">服務端點。</param>
        /// <param name="headers">自訂標頭。</param>
        public static async Task<string> GetAsync(string endpoint, NameValueCollection headers = null)
        {
            HttpClient client = GetOrCreateClient(endpoint);

            // 發送 GET 請求
            using (var request = new HttpRequestMessage(HttpMethod.Get, endpoint))
            {
                if (headers != null)
                {
                    foreach (string key in headers)
                    {
                        request.Headers.TryAddWithoutValidation(key, headers[key]);
                    }
                }

                using (HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();  // 確認是否成功
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);  // 讀取回應內容
                }
            }
        }
    }
}
