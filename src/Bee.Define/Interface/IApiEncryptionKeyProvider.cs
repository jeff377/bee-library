using System;

namespace Bee.Define
{
    /// <summary>
    /// API 加密金鑰提供者介面。
    /// 支援靜態共用金鑰或每次登入個別產生的 Session 金鑰。
    /// </summary>
    public interface IApiEncryptionKeyProvider
    {
        /// <summary>
        /// 取得 API 傳輸資料的加密金鑰。
        /// </summary>
        /// <param name="accessToken">AccessToken 或 Guid.Empty。</param>
        /// <returns>64-byte 的合併金鑰資料（AES + HMAC）。</returns>
        byte[] GetKey(Guid accessToken);

        /// <summary>
        /// 登入時產生一組金鑰，可能是共用或隨機金鑰。
        /// </summary>
        /// <returns>64-byte 合併金鑰（AES + HMAC）。</returns>
        byte[] GenerateKeyForLogin();
    }
}
