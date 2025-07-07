using Bee.Base;

namespace Bee.Define.UnitTests
{
    /// <summary>
    /// 測試 SecurityKeys 初始化與解密功能。
    /// </summary>
    public class SecurityKeysTests
    {
        /// <summary>
        /// 驗證 SecurityKeys 可以正確解密 API 與 Cookie 金錀。
        /// </summary>
        [Fact]
        public void Should_InitializeSecurityKeys_Correctly()
        {
            // 建立模擬金錀
            byte[] masterKey = AesCbcHmacKeyGenerator.GenerateCombinedKey();
            byte[] apiKey = AesCbcHmacKeyGenerator.GenerateCombinedKey();
            byte[] cookieKey = AesCbcHmacKeyGenerator.GenerateCombinedKey();

            AesCbcHmacKeyGenerator.FromCombinedKey(masterKey, out byte[] aesKey, out byte[] hmacKey);

            // 加密後產生 base64 字串
            string apiEncrypted = Convert.ToBase64String(AesCbcHmacCryptor.Encrypt(apiKey, aesKey, hmacKey));
            string cookieEncrypted = Convert.ToBase64String(AesCbcHmacCryptor.Encrypt(cookieKey, aesKey, hmacKey));

            // 寫入模擬 master.key 檔案
            string filePath = SaveTempMasterKey(masterKey);

            var settings = new SecurityKeySettings
            {
                MasterKeySource = new MasterKeySource
                {
                    Type = MasterKeySourceType.File,
                    Value = filePath
                },
                ApiEncryptionKey = apiEncrypted,
                CookieEncryptionKey = cookieEncrypted
            };

            LoadSecurityKey(settings, out byte[] apiKey2, out byte[] cookieKey2);

            Assert.Equal(apiKey, apiKey2);
            Assert.Equal(cookieKey, cookieKey2);
        }

        /// <summary>
        /// 載入金錀設定。
        /// </summary>
        /// <param name="settings">金錀設定。</param>
        private static void LoadSecurityKey(SecurityKeySettings settings, out byte[] apiKey, out byte[] cookieKey)
        {
            byte[] masterKey = MasterKeyProvider.GetMasterKey(settings.MasterKeySource);
            AesCbcHmacKeyGenerator.FromCombinedKey(masterKey, out var aesKey, out var hmacKey);

            apiKey = Array.Empty<byte>();
            cookieKey = Array.Empty<byte>();    
            // 解密 API 金錀，如果設定中有提供。
            if (StrFunc.IsNotEmpty(settings.ApiEncryptionKey))
            {
                byte[] bytes = Convert.FromBase64String(settings.ApiEncryptionKey);
                apiKey = AesCbcHmacCryptor.Decrypt(bytes, aesKey, hmacKey);
            }

            // 解密 Cookie 金錀，如果設定中有提供。
            if (StrFunc.IsNotEmpty(settings.CookieEncryptionKey))
            {
                byte[] bytes = Convert.FromBase64String(settings.CookieEncryptionKey);
                cookieKey = AesCbcHmacCryptor.Decrypt(bytes, aesKey, hmacKey);
            }
        }

        /// <summary>
        /// 將 master key 寫入暫存檔，回傳檔案路徑。
        /// </summary>
        /// <param name="key">金錀位元組。</param>
        /// <returns>檔案路徑。</returns>
        private string SaveTempMasterKey(byte[] key)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp-master.key");
            File.WriteAllText(path, Convert.ToBase64String(key));
            return path;
        }
    }
}
