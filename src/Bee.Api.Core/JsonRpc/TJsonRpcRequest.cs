using System;
using System.ComponentModel;
using Bee.Base;
using Bee.Define;
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
        /// 傳入資料。
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// 資料是否加密。
        /// </summary>
        [DefaultValue(false)]
        public bool Encrypted { get; private set; } = false;

        /// <summary>
        /// 資料進行加密。
        /// </summary>
        public void Encrypt()
        {
            // 已加密離開
            if (this.Encrypted) { return; }

            byte[] bytes = SerializeFunc.ObjectToBinary(Value);  // 序列化
            var encryption = SysFunc.CreateApiServiceEncryption();
            Value = encryption.Encrypt(bytes);  // 加密
            Encrypted = true;
        }

        /// <summary>
        /// 資料進行解密。
        /// </summary>
        public void Decrypt()
        {
            // 未加密則離開
            if (!this.Encrypted) { return; }

            var encryption = SysFunc.CreateApiServiceEncryption();
            byte[] bytes = encryption.Decrypt(Value as byte[]);  // 解密
            Value = SerializeFunc.BinaryToObject(bytes);  // 反序列化
            Encrypted = false;
        }
    }
}
