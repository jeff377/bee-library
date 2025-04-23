using Newtonsoft.Json;

namespace Bee.Api.Core
{
    /// <summary>
    /// JSON-RPC 要求執行方法的引數。
    /// </summary>
    public class TJsonRpcParams
    {
        /// <summary>
        /// 傳入資料。
        /// </summary>
        [JsonProperty("value")]
        public object Value { get; set; }

        /// <summary>
        /// 資料是否加密。
        /// </summary>
        [JsonProperty("encrypted")]
        public bool Encrypted { get; private set; } = false;
    }
}
