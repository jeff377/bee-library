using Bee.Base;
using Bee.Cache;
using Bee.Define;
using System;

namespace Bee.Business
{
    /// <summary>
    /// AccessToken 驗證提供者，用於驗證 AccessToken 的有效性。
    /// </summary>
    public class AccessTokenValidationProvider : IAccessTokenValidationProvider
    {
        /// <summary>
        /// 驗證指定的 AccessToken 是否有效。
        /// </summary>
        /// <param name="accessToken">用於驗證的存取權杖。</param>
        /// <returns>若 AccessToken 有效則為 true，否則為 false。</returns>
        public bool ValidateAccessToken(Guid accessToken)
        {
            // 如果 AccessToken 為 Guid.Empty，則拋出未授權異常
            if (BaseFunc.IsEmpty(accessToken))
            {
                throw new UnauthorizedAccessException("Access token is required.");
            }

            var sessionInfo = CacheFunc.GetSessionInfo(accessToken);
            if (sessionInfo == null) 
                throw new UnauthorizedAccessException("Session key not found or expired.");

            return sessionInfo.AccessToken == accessToken;
        }
    }
}
