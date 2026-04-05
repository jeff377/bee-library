
namespace Bee.Api.Core
{
    /// <summary>
    /// API Header 鍵值。
    /// </summary>
    public class ApiHeaders
    {
        /// <summary>
        /// API KEY。
        /// </summary>
        public const string ApiKey = "X-Api-Key";
        /// <summary>
        /// Authorization，用於身份驗證或授權。
        /// </summary>
        public const string Authorization = "Authorization";
        /// <summary>
        /// Content-Type，指定請求或回應的內容類型。
        /// </summary>
        public const string ContentType = "Content-Type";
    }
}
