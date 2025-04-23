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
        public object Id { get; set; }

        /// <summary>
        /// 資料進行加密。
        /// </summary>
        public void Encrypt()
        {
            if (Params != null)
            {
                Params.Encrypt();  // 加密資料
            }
        }

        /// <summary>
        /// 資料進行解密。
        /// </summary>
        public void Decrypt()
        {
            if (Params != null)
            {
                Params.Decrypt();  // 解密資料
            }
        }
    }
}
