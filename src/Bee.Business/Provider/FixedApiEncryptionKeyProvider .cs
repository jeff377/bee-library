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
    }

}
