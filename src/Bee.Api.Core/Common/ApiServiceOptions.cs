using System;

namespace Bee.Api.Core
{
    /// <summary>
    /// 提供 JSON-RPC 框架可自訂的元件設定，包含授權驗證、資料轉換與序列化策略。
    /// 使用者可於應用程式啟動時設定替代實作，以符合自訂需求。
    /// </summary>
    public static class ApiServiceOptions
    {
        private static IApiAuthorizationValidator _authorizationValidator = new TApiAuthorizationValidator(); // 預設實作
        private static IApiPayloadTransformer _payloadTransformer = new TApiPayloadTransformer(); // 預設實作
        private static IApiPayloadSerializer _payloadSerializer = new TMessagePackPayloadSerializer(); // 預設實作
        private static IApiPayloadCompressor _payloadCompressor = new TGZipCompressor(); // 預設實作
        private static IApiPayloadEncryptor _payloadEncryptor = new TAesEncryptor(); // 預設實作

        /// <summary>
        /// API 金鑰與授權驗證器。
        /// </summary>
        public static IApiAuthorizationValidator AuthorizationValidator
        {
            get => _authorizationValidator;
            set => _authorizationValidator = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// API 傳輸資料的處理器，提供資料加解密、序列化與壓縮等轉換功能。
        /// </summary>
        public static IApiPayloadTransformer PayloadTransformer
        {
            get => _payloadTransformer;
            set => _payloadTransformer = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// API 傳輸層 payload 專用序列化器。
        /// </summary>
        public static IApiPayloadSerializer PayloadSerializer
        {
            get => _payloadSerializer;
            set => _payloadSerializer = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// API 傳輸層 payload 專用壓縮器。
        /// </summary>
        public static IApiPayloadCompressor PayloadCompressor
        {
            get => _payloadCompressor;
            set => _payloadCompressor = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// API 傳輸層 payload 專用加密器。
        /// </summary>
        public static IApiPayloadEncryptor PayloadEncryptor
        {
            get => _payloadEncryptor;
            set => _payloadEncryptor = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// 取得當前的設定摘要，包含序列化器、壓縮器與加密器的使用狀態。
        /// </summary>
        public static string CurrentSettingsSummary =>
            $"Serializer: {PayloadSerializer.SerializationMethod}, " +
            $"Compressor: {PayloadCompressor.CompressionMethod}, " +
            $"Encryptor: {PayloadEncryptor.EncryptionMethod}";
    }
}
