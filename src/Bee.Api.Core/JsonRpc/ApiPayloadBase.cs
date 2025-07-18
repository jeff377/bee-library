using System;
using System.ComponentModel;
using Bee.Base;
using Newtonsoft.Json;

namespace Bee.Api.Core
{
    /// <summary>
    /// 提供 API 傳輸資料的標準結構。
    /// </summary>
    public abstract class ApiPayloadBase : IObjectSerialize
    {
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
            if (Value is IObjectSerialize objectSerialize)
            {
                BaseFunc.SetSerializeState(objectSerialize, serializeState);
            }
        }

        #endregion

        /// <summary>
        /// 傳遞資料的格式（原始、編碼或加密）。
        /// </summary>
        [JsonProperty("format")]
        [DefaultValue(PayloadFormat.Plain)]
        public PayloadFormat Format { get; private set; } = PayloadFormat.Plain;

        /// <summary>
        /// 傳遞資料。
        /// </summary>
        [JsonProperty("value")]
        public object Value { get; set; }

        /// <summary>
        /// 資料是否已經進行編碼（例如序列化、壓縮或加密）。
        /// </summary>
        [JsonProperty("isEncoded")]
        [DefaultValue(false)]
        public bool IsEncoded { get; private set; } = false;

        /// <summary>
        /// 編碼資料是否已加密。
        /// </summary>
        [JsonProperty("isEncrypted")]
        [DefaultValue(false)]
        public bool IsEncrypted { get; private set; } = false;

        /// <summary>
        /// 傳遞資料的型別名稱，用於反序列化還原時指定型別。
        /// </summary>
        [JsonProperty("type")]
        [DefaultValue("")]
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// 將傳遞資料進行轉換處理，例如序列化、壓縮或加密。
        /// </summary>
        /// <param name="encryptionKey"> 加密金鑰。</param>
        public void Encode(byte[] encryptionKey)
        {
            // 已經過編碼則離開
            if (this.IsEncoded) { return; }

            // 將指定的物件進行轉換處理，例如序列化、壓縮或加密
            var type = Value.GetType();
            TypeName = $"{type.FullName}, {type.Assembly.GetName().Name}";
            var transformer = ApiServiceOptions.PayloadTransformer;
            Value = transformer.Encode(Value, type, encryptionKey);

            IsEncoded = true;
            IsEncrypted = encryptionKey != null;
        }

        /// <summary>
        /// 將處理過的資料還原為原始物件，例如解密、解壓縮與反序列化。
        /// </summary>
        /// <param name="encryptionKey"> 加密金鑰。</param>
        public void Decode(byte[] encryptionKey)
        {
            // 未經過編碼則離開
            if (!this.IsEncoded) { return; }

            Type type = Type.GetType(TypeName);
            if (type == null)
                throw new InvalidOperationException($"Unable to load type: {TypeName}");

            // 如果資料已加密，則使用提供的金鑰組進行解密
            var useKeySet = IsEncrypted ? encryptionKey : null;    

            // 將處理過的資料還原為原始物件，例如解密、解壓縮與反序列化
            var transformer = ApiServiceOptions.PayloadTransformer;
            Value = transformer.Decode(Value, type, useKeySet);

            IsEncoded = false;
            IsEncrypted = false;
        }
    }
}
