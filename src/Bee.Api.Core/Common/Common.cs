
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

    /// <summary>
    /// 定義 JSON-RPC 標準錯誤代碼，用於表示請求處理過程中的錯誤狀況。
    /// </summary>
    public enum JsonRpcErrorCode
    {
        /// <summary>
        /// 無法解析 JSON，通常是格式錯誤或語法不正確（-32700）。
        /// </summary>
        ParseError = -32700,

        /// <summary>
        /// 請求無效，可能是缺少必要欄位或結構錯誤（-32600）。
        /// </summary>
        InvalidRequest = -32600,

        /// <summary>
        /// 找不到指定的方法（-32601）。
        /// </summary>
        MethodNotFound = -32601,

        /// <summary>
        /// 方法的參數無效或格式錯誤（-32602）。
        /// </summary>
        InvalidParams = -32602,

        /// <summary>
        /// 伺服器內部錯誤，無法完成請求（-32000）。
        /// </summary>
        InternalError = -32000,

        /// <summary>
        /// 未授權的存取，通常是憑證驗證失敗（-32001）。
        /// </summary>
        Unauthorized = -32001
    }

}
