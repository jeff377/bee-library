using System;

namespace Bee.Api.Core
{
    /// <summary>
    /// 提供依據設定值建立 API Payload 編碼元件的工廠類別。
    /// </summary>
    public static class ApiPayloadOptionsFactory
    {
        /// <summary>
        /// 建立指定名稱的序列化元件。
        /// </summary>
        /// <param name="name">序列化器名稱，例如 "messagepack"、"binaryformatter"。</param>
        /// <returns>序列化元件。</returns>
        /// <exception cref="NotSupportedException">不支援的序列化器名稱。</exception>
        public static IApiPayloadSerializer CreateSerializer(string name)
        {
            switch (name)
            {
                case "messagepack":
                    return new MessagePackPayloadSerializer();
                case "binaryformatter":
                    return new BinaryFormatterPayloadSerializer();
                default:
                    throw new NotSupportedException($"Unsupported serializer: {name}");
            }
        }

        /// <summary>
        /// 建立指定名稱的壓縮元件。
        /// </summary>
        /// <param name="name">壓縮器名稱，例如 "gzip"，或 "none" 表示不壓縮。</param>
        /// <returns>壓縮元件。</returns>
        /// <exception cref="NotSupportedException">不支援的壓縮器名稱。</exception>
        public static IApiPayloadCompressor CreateCompressor(string name)
        {
            switch (name)
            {
                case "gzip":
                    return new GZipPayloadCompressor();
                case "none":
                case "":
                    return new NoCompressionCompressor();
                default:
                    throw new NotSupportedException($"Unsupported compressor: {name}");
            }
        }

        /// <summary>
        /// 建立指定名稱的加密元件。
        /// </summary>
        /// <param name="name">加密器名稱，例如 "aes256"，或 "none" 表示不加密。</param>
        /// <returns>加密元件。</returns>
        /// <exception cref="NotSupportedException">不支援的加密器名稱。</exception>
        public static IApiPayloadEncryptor CreateEncryptor(string name)
        {
            switch (name)
            {
                case "aes256":
                    return new AesPayloadEncryptor();
                case "none":
                case "":
                    return new NoEncryptionEncryptor();
                default:
                    throw new NotSupportedException($"Unsupported encryptor: {name}");
            }
        }
    }
}
