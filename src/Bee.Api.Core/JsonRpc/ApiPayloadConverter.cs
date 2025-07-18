using System;

namespace Bee.Api.Core
{
    /// <summary>
    /// 處理 <see cref="ApiPayload"/> 的格式轉換（序列化、壓縮與加密）工具類別。
    /// </summary>
    public static class ApiPayloadConverter
    {
        /// <summary>
        /// 將指定的 Payload 物件轉換為目標格式（編碼或加密）。
        /// </summary>
        /// <param name="payload">要轉換的 Payload 物件。</param>
        /// <param name="targetFormat">欲轉換的目標格式，例如 Encoded 或 Encrypted。</param>
        /// <param name="encryptionKey">加密金鑰，僅當 <paramref name="targetFormat"/> 為 Encrypted 時必須指定。</param>
        /// <exception cref="InvalidOperationException">
        /// 當 <paramref name="targetFormat"/> 為 Encrypted 但未提供金鑰，或 Payload.Value 為 null 時發生。
        /// </exception>
        public static void TransformTo(ApiPayload payload, PayloadFormat targetFormat, byte[] encryptionKey = null)
        {
            if (targetFormat == PayloadFormat.Plain)
            {
                payload.Format = PayloadFormat.Plain;
                return;
            }

            if (payload.Value == null)
                throw new InvalidOperationException("Payload.Value cannot be null.");

            var type = payload.Value.GetType();
            payload.TypeName = type.FullName + ", " + type.Assembly.GetName().Name;

            var transformer = ApiServiceOptions.PayloadTransformer;
            var bytes = transformer.Encode(payload.Value, type);

            if (targetFormat == PayloadFormat.Encrypted)
            {
                if (encryptionKey == null || encryptionKey.Length == 0)
                    throw new InvalidOperationException("Encryption key is required for encrypted payload.");

                bytes = transformer.Encrypt(bytes, encryptionKey);
            }
            else if (encryptionKey != null && encryptionKey.Length > 0)
            {
                throw new InvalidOperationException("Encryption key should not be provided for non-encrypted format.");
            }

            payload.Value = bytes;
            payload.Format = targetFormat;
        }

        /// <summary>
        /// 將指定格式的 Payload 還原為原始物件（解密與解碼）。
        /// </summary>
        /// <param name="payload">要還原的 Payload 物件。</param>
        /// <param name="sourceFormat">來源格式，應為 Encoded 或 Encrypted。</param>
        /// <param name="encryptionKey">解密所需的金鑰，僅當 <paramref name="sourceFormat"/> 為 Encrypted 時必須指定。</param>
        /// <exception cref="InvalidOperationException">
        /// 當 <paramref name="sourceFormat"/> 為 Encrypted 但未提供金鑰，或無法載入 TypeName 時發生。
        /// </exception>
        /// <exception cref="InvalidCastException">當 Payload.Value 不是 byte[] 類型時發生。</exception>
        public static void RestoreFrom(ApiPayload payload, PayloadFormat sourceFormat, byte[] encryptionKey = null)
        {
            if (sourceFormat == PayloadFormat.Plain)
            {
                payload.Format = PayloadFormat.Plain;
                return;
            }

            if (string.IsNullOrEmpty(payload.TypeName))
                throw new InvalidOperationException("TypeName is missing for deserialization.");

            var type = Type.GetType(payload.TypeName);
            if (type == null)
                throw new InvalidOperationException("Unable to load type: " + payload.TypeName);

            var bytes = payload.Value as byte[];
            if (bytes == null)
                throw new InvalidCastException("Payload.Value must be byte[].");

            var transformer = ApiServiceOptions.PayloadTransformer;

            if (sourceFormat == PayloadFormat.Encrypted)
            {
                if (encryptionKey == null || encryptionKey.Length == 0)
                    throw new InvalidOperationException("Missing encryption key for encrypted payload.");

                bytes = transformer.Decrypt(bytes, encryptionKey);
            }

            payload.Value = transformer.Decode(bytes, type);
            payload.Format = PayloadFormat.Plain;
        }
    }

}
