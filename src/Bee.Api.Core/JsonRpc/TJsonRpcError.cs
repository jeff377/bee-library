using Bee.Base;
using Newtonsoft.Json;

namespace Bee.Api.Core
{
    /// <summary>
    /// JSON-RPC 錯誤模型。
    /// </summary>
    public class TJsonRpcError
    {
        /// <summary>
        /// 錯誤代碼。
        /// </summary>
        [JsonProperty("code")]  
        public int Code { get; set; }

        /// <summary>
        /// 錯誤的簡短描述。
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
