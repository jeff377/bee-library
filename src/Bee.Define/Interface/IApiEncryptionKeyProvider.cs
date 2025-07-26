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
        /// 根據 AccessToken 取得 API 傳輸資料的加密金鑰。
        /// </summary>
        /// <param name="accessToken">若 AccessToken 為空，則視為共用模式。</param>
        byte[] GetKey(Guid accessToken);
    }
}
