using System;
using Bee.Base;
using Newtonsoft.Json;

namespace Bee.Api.Core
{
    /// <summary>
    /// JSON-RPC 請求模型。 
    /// </summary>
    [Serializable]
    public class TJsonRpcRequest : IObjectSerializeBase
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TJsonRpcRequest()
        { }

        #endregion

        /// <summary>
        /// 指定 JSON-RPC 的版本。
        /// </summary>
        [JsonProperty("jsonrpc", NullValueHandling = NullValueHandling.Include)]
        public string Jsonrpc { get; set; } = "2.0";

        /// <summary>
        /// 要呼叫的方法名稱。
        /// </summary>
        [JsonProperty("method", NullValueHandling = NullValueHandling.Include)]
        public string Method { get; set; }

        /// <summary>
        /// 方法的引數。
        /// </summary>
        [JsonProperty("params")]
        public TJsonRpcParams Params { get; set; } = new TJsonRpcParams();

        /// <summary>
        /// 請求的唯一識別碼。
        /// </summary>
        [JsonProperty("id", NullValueHandling = NullValueHandling.Include)]
        public string Id { get; set; }

        /// <summary>
        /// 將指定的物件進行轉換處理，例如序列化、壓縮或加密。
        /// </summary>
        public void Encode()
        {
            if (Params != null)
            {
                Params.Encode();
            }
        }

        /// <summary>
        /// 將處理過的資料還原為原始物件，例如解密、解壓縮與反序列化。
        /// </summary>
        public void Decode()
        {
            if (Params != null)
            {
                Params.Decode();
            }
        }
    }
}
