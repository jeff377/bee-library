using System;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 系統執行階段的加密金錀集合（靜態存取）。
    /// </summary>
    public static class SecurityKeys
    {
        /// <summary>
        /// 主金錀。
        /// </summary>
        public static byte[] MasterKey { get; private set; } = Array.Empty<byte>();

        /// <summary>
        /// API 傳輸金錀。
        /// </summary>
        public static byte[] ApiKey { get; private set; } = Array.Empty<byte>();

        /// <summary>
        /// Cookie 金錀。
        /// </summary>
        public static byte[] CookieKey { get; private set; } = Array.Empty<byte>();

        private static bool _isInitialized;

        /// <summary>
        /// 是否已初始化。
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// 初始化金錀（僅允許一次）。
        /// </summary>
        /// <param name="settings">金錀設定。</param>
        public static void Initialize(SecurityKeySettings settings)
        {
            if (_isInitialized)
                throw new InvalidOperationException("SecurityKeys is already initialized.");

            MasterKey = MasterKeyProvider.GetMasterKey(settings.MasterKeySource);
            AesCbcHmacKeyGenerator.FromCombinedKey(MasterKey, out var aesKey, out var hmacKey);

            // 解密 API 金錀，如果設定中有提供。
            if (StrFunc.IsNotEmpty(settings.ApiEncryptionKey))
            {
                byte[] bytes = Convert.FromBase64String(settings.ApiEncryptionKey);
                ApiKey = AesCbcHmacCryptor.Decrypt(bytes, aesKey, hmacKey);
            }

            // 解密 Cookie 金錀，如果設定中有提供。
            if (StrFunc.IsNotEmpty(settings.CookieEncryptionKey))
            {
                byte[] bytes = Convert.FromBase64String(settings.CookieEncryptionKey);
                CookieKey = AesCbcHmacCryptor.Decrypt(bytes, aesKey, hmacKey);
            }

            _isInitialized = true;
        }

        /// <summary>
        /// 清除金錀（用於測試或重新初始化）。
        /// </summary>
        public static void Clear()
        {
            MasterKey = Array.Empty<byte>();
            ApiKey = Array.Empty<byte>();
            CookieKey = Array.Empty<byte>();
            _isInitialized = false;
        }
    }

}
