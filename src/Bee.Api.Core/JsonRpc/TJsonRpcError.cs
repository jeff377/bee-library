using Newtonsoft.Json;

namespace Bee.Api.Core
{
    /// <summary>
    /// JSON-RPC 錯誤模型。
    /// </summary>
    public class TJsonRpcError
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public TJsonRpcError()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="code">錯誤代碼。</param>
        /// <param name="message">錯誤訊息。</param>
        /// <param name="data">用於提供附加的錯誤訊息。</param>
        public TJsonRpcError(int code, string message, object data = null)
        {
            Code = code;
            Message = message;
            Data = data;
        }

        /// <summary>
        /// 錯誤代碼。
        /// </summary>
        [JsonProperty("code")]  
        public int Code { get; set; }

        /// <summary>
        /// 錯誤訊息。
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// 用於提供附加的錯誤訊息。
        /// </summary>
        [JsonProperty("data")]
        public object Data { get; set; }
    }
}
