using Bee.Base;
using Bee.Define;
using Newtonsoft.Json;

namespace Bee.Api.Core
{
    /// <summary>
    /// JSON-RPC 執行方法的傳出結果。
    /// </summary>
    public class TJsonRpcResult
    {
        /// <summary>
        /// 傳出資料。
        /// </summary>
        [JsonProperty("value")]
        public object Value { get; set; }

        /// <summary>
        /// 資料是否已經進行編碼（例如加密或壓縮）。
        /// </summary>
        [JsonProperty("isEncoded")]
        public bool IsEncoded { get; private set; } = false;

        /// <summary>
        /// 資料進行加密。
        /// </summary>
        public void Encrypt()
        {
            // 已加密離開
            if (this.IsEncoded) { return; }

            byte[] bytes = SerializeFunc.ObjectToBinary(Value);  // 序列化
            var encryption = SysFunc.CreateApiServiceEncryption();
            Value = encryption.Encrypt(bytes);  // 加密
            IsEncoded = true;
        }

        /// <summary>
        /// 資料進行解密。
        /// </summary>
        public void Decrypt()
        {
            // 未加密則離開
            if (!this.IsEncoded) { return; }

            var encryption = SysFunc.CreateApiServiceEncryption();
            byte[] bytes = encryption.Decrypt(Value as byte[]);  // 解密
            Value = SerializeFunc.BinaryToObject(bytes);  // 反序列化
            IsEncoded = false;
        }
    }
}
