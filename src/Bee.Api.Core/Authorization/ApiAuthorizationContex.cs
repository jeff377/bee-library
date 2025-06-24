namespace Bee.Api.Core
{
    /// <summary>
    /// API 授權驗證內容。
    /// </summary>
    public class TApiAuthorizationContext
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public TApiAuthorizationContext()
        {
        }

        /// <summary>
        /// API 金鑰。
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// 授權標頭。
        /// </summary>
        public string Authorization { get; set; } = string.Empty;

        /// <summary>
        /// JSON-RPC 方法名稱。
        /// </summary>
        public string Method { get; set; } = string.Empty;
    }
}
