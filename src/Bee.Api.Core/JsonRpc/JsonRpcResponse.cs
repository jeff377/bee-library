using Bee.Base;
using Newtonsoft.Json;

namespace Bee.Api.Core
{
    /// <summary>
    /// JSON-RPC 回應模型。 
    /// </summary>
    public class JsonRpcResponse : IObjectSerialize
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public JsonRpcResponse()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="request">JSON-RPC 請求模型。</param>
        public JsonRpcResponse(JsonRpcRequest request)
        {
            // 設定請求的唯一識別碼
            Id = request.Id;
        }

        #endregion

        #region IObjectSerialize 介面

        /// <summary>
        /// 序列化狀態。
        /// </summary>
        [JsonIgnore]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public virtual void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
            BaseFunc.SetSerializeState(Result, serializeState);
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
        public JsonRpcResult Result { get; set; }

        /// <summary>
        /// 錯誤訊息。
        /// </summary>
        [JsonProperty("error")]
        public JsonRpcError Error { get; set; }

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
            if (Result != null)
            {
                Result.Encode();
            }
        }

        /// <summary>
        /// 將處理過的資料還原為原始物件，例如解密、解壓縮與反序列化。。
        /// </summary>
        public void Decode()
        {
            if (Result != null)
            {
                Result.Decode();
            }
        }
    }
}
