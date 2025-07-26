using System;
using Bee.Base;
using Bee.Define;

namespace Bee.Business
{
    /// <summary>
    /// API 加密金鑰提供者，用於取得傳輸資料加解密所需的 AES+HMAC 金鑰。
    /// 根據 AccessToken 決定是否回傳共用金鑰或進階的會話金鑰。
    /// </summary>
    public class ApiEncryptionKeyProvider : IApiEncryptionKeyProvider
    {
        /// <summary>
        /// 根據 AccessToken 取得 API 傳輸資料的加密金鑰。
        /// </summary>
        /// <param name="accessToken">若為 Guid.Empty，則視為共用模式。</param>
        /// <returns>64-byte 的合併金鑰資料（AES + HMAC）。</returns>
        public byte[] GetKey(Guid accessToken)
        {
            if (BaseFunc.IsEmpty(accessToken))
            {
                return BackendInfo.ApiEncryptionKey;
            }
            else
            {
                // TODO: 支援每個 AccessToken 對應的 Session 金鑰查詢
                throw new NotImplementedException("Session-based API key retrieval is not implemented.");
            }
        }
    }

}
