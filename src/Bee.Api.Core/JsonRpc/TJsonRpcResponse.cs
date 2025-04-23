using System.ComponentModel;
using Bee.Base;
using Newtonsoft.Json;

namespace Bee.Api.Core
{
    /// <summary>
    /// JSON-RPC 回應模型。 
    /// </summary>
    public class TJsonRpcResponse : IObjectSerializeBase
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TJsonRpcResponse()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        public TJsonRpcResponse(TJsonRpcRequest request)
        {
            // 設定請求的唯一識別碼
            Id = request.Id;
        }

        #endregion

        /// <summary>
        /// 指定 JSON-RPC 的版本。
        /// </summary>
        [JsonProperty("jsonrpc", NullValueHandling = NullValueHandling.Include)]
        public string Jsonrpc { get; set; } = "2.0";

        /// <summary>
        /// 方法執行的結果。
        /// </summary>
        [JsonProperty("result")]
        public TJsonRpcResult Result { get; set; }

        /// <summary>
        /// 錯誤訊息。
        /// </summary>
        [JsonProperty("error")]
        public TJsonRpcError Error { get; set; }

        /// <summary>
        /// 請求的唯一識別碼。
        /// </summary>
        [JsonProperty("id", NullValueHandling = NullValueHandling.Include)]
        public object Id { get; set; }

        /// <summary>
        /// 回傳訊息文字。
        /// </summary>
        [DefaultValue("")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 資料進行加密。
        /// </summary>
        public void Encrypt()
        {
            if (Result != null)
            {
                Result.Encrypt();  // 加密資料
            }
        }

        /// <summary>
        /// 資料進行解密。
        /// </summary>
        public void Decrypt()
        {
            if (Result != null)
            {
                Result.Decrypt();  // 解密資料
            }
        }
    }
}
