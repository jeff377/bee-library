using Bee.Base;
using Bee.Define;
using Newtonsoft.Json;

namespace Bee.Api.Core
{
    /// <summary>
    /// JSON-RPC 執行方法的傳入引數。
    /// </summary>
    public class TJsonRpcParams
    {
        /// <summary>
        /// 傳入資料。
        /// </summary>
        [JsonProperty("value")]
        public object Value { get; set; }

        /// <summary>
        /// 資料是否已經進行編碼（例如加密或壓縮）。
        /// </summary>
        [JsonProperty("isEncoded")]
        public bool IsEncoded { get; private set; } = false;

        /// <summary>
        /// 將傳入資料進行轉換處理，例如序列化、壓縮或加密。
        /// </summary>
        public void Encode()
        {
            // 已經過編碼則離開
            if (this.IsEncoded) { return; }

            // 將指定的物件進行轉換處理，例如序列化、壓縮或加密
            var transformer = ApiServiceOptions.PayloadTransformer;
            Value = transformer.Encode(Value);

            IsEncoded = true;
        }

        /// <summary>
        /// 將處理過的資料還原為原始物件，例如解密、解壓縮與反序列化。
        /// </summary>
        public void Decode()
        {
            // 未經過編碼則離開
            if (!this.IsEncoded) { return; }

            // 將處理過的資料還原為原始物件，例如解密、解壓縮與反序列化
            var transformer = ApiServiceOptions.PayloadTransformer;
            Value = transformer.Decode(Value);

            IsEncoded = false;
        }
    }
}
