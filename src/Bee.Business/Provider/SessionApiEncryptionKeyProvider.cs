﻿using Bee.Base;
using Bee.Cache;
using Bee.Define;
using System;

namespace Bee.Business
{
    /// <summary>
    /// Session Key 提供者，依 AccessToken 取得對應的會話金鑰。
    /// </summary>
    public class SessionApiEncryptionKeyProvider : IApiEncryptionKeyProvider
    {
        /// <summary>
        /// 取得 API 傳輸資料的加密金鑰。
        /// </summary>
        /// <param name="accessToken">AccessToken 或 Guid.Empty。</param>
        /// <returns>64-byte 的合併金鑰資料（AES + HMAC）。</returns>
        public byte[] GetKey(Guid accessToken)
        {
            // 如果 AccessToken 為 Guid.Empty，則拋出未授權異常
            if (BaseFunc.IsEmpty(accessToken))
            {
                throw new UnauthorizedAccessException("Access token is required for session-based key.");
            }

            var sessionInfo = CacheFunc.GetSessionInfo(accessToken);
            return sessionInfo?.ApiEncryptionKey
               ?? throw new UnauthorizedAccessException("Access token is invalid or session key not found.");
        }
    }

}
