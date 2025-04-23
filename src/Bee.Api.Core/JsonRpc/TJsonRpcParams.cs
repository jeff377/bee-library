using Bee.Base;
using Bee.Define;
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
