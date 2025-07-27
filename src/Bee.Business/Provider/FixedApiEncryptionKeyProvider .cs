using Bee.Define;
using System;

namespace Bee.Business
{
    /// <summary>
    /// 固定金鑰提供者，永遠回傳後端設定檔的共用金鑰。
    /// </summary>
    public class FixedApiEncryptionKeyProvider : IApiEncryptionKeyProvider
    {
        /// <summary>
        /// 取得 API 傳輸資料的加密金鑰。
        /// </summary>
        /// <param name="accessToken">AccessToken 或 Guid.Empty。</param>
        /// <returns>64-byte 的合併金鑰資料（AES + HMAC）。</returns>
        public byte[] GetKey(Guid accessToken)
        {
            return BackendInfo.ApiEncryptionKey
                ?? throw new InvalidOperationException("BackendInfo.ApiEncryptionKey is not initialized.");
        }

        /// <summary>
        /// 登入時產生一組金鑰，可能是共用或隨機金鑰。
        /// </summary>
        /// <returns>64-byte 合併金鑰（AES + HMAC）。</returns>
        public byte[] GenerateKeyForLogin()
        {
            // 固定金鑰提供者不支援登入時產生金鑰，直接回傳共用金鑰
            return GetKey(Guid.Empty);
        }
    }

}
