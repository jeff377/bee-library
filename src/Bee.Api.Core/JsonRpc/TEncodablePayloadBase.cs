using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Bee.Api.Core
{
    /// <summary>
    /// 提供編碼與解碼的共用實作。
    /// </summary>
    public abstract  class TEncodablePayloadBase : IEncodablePayload
    {
        /// <summary>
        /// 傳遞資料。
        /// </summary>
        [JsonProperty("value")]
        public object Value { get; set; }

        /// <summary>
        /// 資料是否已經進行編碼（例如加密或壓縮）。
        /// </summary>
        [JsonProperty("isEncoded")]
        [DefaultValue(false)]
        public bool IsEncoded { get; private set; } = false;

        /// <summary>
        /// 傳遞資料的型別名稱，用於反序列化還原時指定型別。
        /// </summary>
        [JsonProperty("type")]
        [DefaultValue("")]
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// 將傳遞資料進行轉換處理，例如序列化、壓縮或加密。
        /// </summary>
        public void Encode()
        {
            // 已經過編碼則離開
            if (this.IsEncoded) { return; }

            // 將指定的物件進行轉換處理，例如序列化、壓縮或加密
            var type = Value.GetType();
            TypeName = $"{type.FullName}, {type.Assembly.GetName().Name}";
            var transformer = ApiServiceOptions.PayloadTransformer;
            Value = transformer.Encode(Value, type);

            IsEncoded = true;
        }

        /// <summary>
        /// 將處理過的資料還原為原始物件，例如解密、解壓縮與反序列化。
        /// </summary>
        public void Decode()
        {
            // 未經過編碼則離開
            if (!this.IsEncoded) { return; }

            Type type = Type.GetType(TypeName);
            if (type == null)
                throw new InvalidOperationException($"Unable to load type: {TypeName}");

            // 將處理過的資料還原為原始物件，例如解密、解壓縮與反序列化
            var transformer = ApiServiceOptions.PayloadTransformer;
            Value = transformer.Decode(Value, type);

            IsEncoded = false;
        }
    }
}
