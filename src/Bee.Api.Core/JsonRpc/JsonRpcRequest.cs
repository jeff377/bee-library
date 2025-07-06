using System;
using Bee.Base;
using Newtonsoft.Json;

namespace Bee.Api.Core
{
    /// <summary>
    /// JSON-RPC 請求模型。 
    /// </summary>
    [Serializable]
    public class JsonRpcRequest : IObjectSerialize
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public JsonRpcRequest()
        { }

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
            BaseFunc.SetSerializeState(Params, serializeState);
        }

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
        public JsonRpcParams Params { get; set; } = new JsonRpcParams();

        /// <summary>
        /// 請求的唯一識別碼。
        /// </summary>
        [JsonProperty("id", NullValueHandling = NullValueHandling.Include)]
        public string Id { get; set; }

        /// <summary>
        /// 將指定的物件進行轉換處理，例如序列化、壓縮或加密。
        /// </summary>
        /// <param name="encryptionKey"> 加密金鑰。</param>
        public void Encode(byte[] encryptionKey)
        {
            if (Params != null)
            {
                Params.Encode(encryptionKey);
            }
        }

        /// <summary>
        /// 將處理過的資料還原為原始物件，例如解密、解壓縮與反序列化。
        /// </summary>
        /// <param name="encryptionKey"> 加密金鑰。</param>    
        public void Decode(byte[] encryptionKey)
        {
            if (Params != null)
            {
                Params.Decode(encryptionKey);
            }
        }
    }
}
