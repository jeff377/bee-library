using System;
using Bee.Define;

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
        private static IApiPayloadCompressor _payloadCompressor = new TGZipPayloadCompressor(); // 預設實作
        private static IApiPayloadEncryptor _payloadEncryptor = new TAesPayloadEncryptor(); // 預設實作
        private static IBusinessObjectResolver _businessObjectResolver = new TBusinessObjectResolver(); // 預設實作

        /// <summary>
        /// 初始化 API 服務選項，設定序列化器、壓縮器與加密器的實作。
        /// </summary>
        /// <param name="payloadOptions">提供 API Payload 處理相關選項，例如序列化、壓縮與加密。</param>
        public static void Initialize(TApiPayloadOptions payloadOptions)
        {
            PayloadSerializer = ApiPayloadOptionsFactory.CreateSerializer(payloadOptions.Serializer);
            PayloadCompressor = ApiPayloadOptionsFactory.CreateCompressor(payloadOptions.Compressor);
            PayloadEncryptor = ApiPayloadOptionsFactory.CreateEncryptor(payloadOptions.Encryptor);
        }

        /// <summary>
        /// 初始化 API Payload 編碼元件，直接指定序列化器、壓縮器與加密器的實作。
        /// 此方法可取代使用 Factory 的預設建立方式，適合進階自訂場景。
        /// </summary>
        /// <param name="serializer">自訂序列化器。</param>
        /// <param name="compressor">自訂壓縮器。</param>
        /// <param name="encryptor">自訂加密器。</param>
        public static void Initialize(
            IApiPayloadSerializer serializer,
            IApiPayloadCompressor compressor,
            IApiPayloadEncryptor encryptor)
        {
            PayloadSerializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            PayloadCompressor = compressor ?? throw new ArgumentNullException(nameof(compressor));
            PayloadEncryptor = encryptor ?? throw new ArgumentNullException(nameof(encryptor));
        }


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
        /// 商業物件建立解析器，負責依照 progID 建立對應物件實例。
        /// </summary>
        public static IBusinessObjectResolver BusinessObjectResolver
        {
            get => _businessObjectResolver;
            set => _businessObjectResolver = value ?? throw new ArgumentNullException(nameof(value));
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
