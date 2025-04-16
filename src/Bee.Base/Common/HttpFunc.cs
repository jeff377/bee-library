using System;
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
            StringContent oContent;
            HttpResponseMessage oResponse;
            string sResponseBody;

            using (HttpClient client = new HttpClient())
            {
                oContent = new StringContent(body, Encoding.UTF8, "application/json");
                if (headers != null)
                {
                    for (int N1 = 0; N1 < headers.Count; N1++)
                        client.DefaultRequestHeaders.Add(headers.GetKey(N1), headers.Get(N1));
                }
                // 發送 POST 請求
                oResponse = await client.PostAsync(endpoint, oContent).ConfigureAwait(false);
                // 確認是否成功
                oResponse.EnsureSuccessStatusCode();
                // 讀取回應內容
                sResponseBody = await oResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                return sResponseBody;
            }
        }

        /// <summary>
        /// 非同步發送 GET 請求。
        /// </summary>
        /// <param name="endpoint">服務端點。</param>
        /// <param name="headers">自訂標頭。</param>
        public static async Task<string> GetAsync(string endpoint, NameValueCollection headers = null)
        {
            HttpResponseMessage oResponse;
            string sResponseBody;

            using (HttpClient client = new HttpClient())
            {
                if (headers != null)
                {
                    for (int N1 = 0; N1 < headers.Count; N1++)
                        client.DefaultRequestHeaders.Add(headers.GetKey(N1), headers.Get(N1));
                }
                // 發送 GET 請求
                oResponse = await client.GetAsync(endpoint).ConfigureAwait(false);
                // 確認是否成功
                oResponse.EnsureSuccessStatusCode();
                // 讀取回應內容
                sResponseBody = await oResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                return sResponseBody;
            }
        }

        /// <summary>
        /// 發送 GET 請求取得回應內容。
        /// </summary>
        /// <param name="endpoint">服務端點。</param>
        /// <param name="headers">自訂標頭。</param>
        public static async Task<HttpResponseMessage> GetResponse(string endpoint, NameValueCollection headers = null)
        {
            HttpResponseMessage oResponse;

            using (HttpClient client = new HttpClient())
            {
                if (headers != null)
                {
                    for (int N1 = 0; N1 < headers.Count; N1++)
                        client.DefaultRequestHeaders.Add(headers.GetKey(N1), headers.Get(N1));
                }
                // 發送 GET 請求
                oResponse = await client.GetAsync(endpoint).ConfigureAwait(false);
                // 確認是否成功
                oResponse.EnsureSuccessStatusCode();
                // 讀取回應內容
                return oResponse;
            }
        }

    }
}
