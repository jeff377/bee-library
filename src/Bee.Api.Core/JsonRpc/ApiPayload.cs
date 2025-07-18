using System;
using System.ComponentModel;
using Bee.Base;
using Newtonsoft.Json;

namespace Bee.Api.Core
{
    /// <summary>
    /// 表示 API 傳遞的標準資料結構，支援序列化、壓縮與加密處理。
    /// </summary>
    public abstract class ApiPayload : IObjectSerialize
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
        public PayloadFormat Format { get; internal set; } = PayloadFormat.Plain;

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
        /// 將傳遞資料進行編碼處理（序列化與壓縮，視需要加密）。
        /// </summary>
        /// <param name="encryptionKey">可選的加密金鑰。</param>
        public void Encode(byte[] encryptionKey)
        {
            if (Format != PayloadFormat.Plain) return;

            var type = Value.GetType();
            TypeName = $"{type.FullName}, {type.Assembly.GetName().Name}";

            var transformer = ApiServiceOptions.PayloadTransformer;
            var encoded = transformer.Encode(Value, type);

            if (encryptionKey != null && encryptionKey.Length > 0)
            {
                encoded = transformer.Encrypt((byte[])encoded, encryptionKey);
                Format = PayloadFormat.Encrypted;
            }
            else
            {
                Format = PayloadFormat.Encoded;
            }

            Value = encoded;
        }

        /// <summary>
        /// 將處理過的資料還原為原始物件。
        /// </summary>
        /// <param name="encryptionKey">加密金鑰。</param>
        public void Decode(byte[] encryptionKey)
        {
            if (Format == PayloadFormat.Plain) return;

            Type type = Type.GetType(TypeName);
            if (type == null)
                throw new InvalidOperationException($"Unable to load type: {TypeName}");

            var transformer = ApiServiceOptions.PayloadTransformer;
            byte[] bytes = Value as byte[];

            if (bytes == null)
                throw new InvalidCastException("Invalid value type. Must be byte[].");

            if (Format == PayloadFormat.Encrypted)
            {
                if (encryptionKey == null || encryptionKey.Length == 0)
                    throw new InvalidOperationException("Missing encryption key for encrypted payload.");

                bytes = transformer.Decrypt(bytes, encryptionKey);
            }

            Value = transformer.Decode(bytes, type);
            Format = PayloadFormat.Plain;
        }
    }
}
