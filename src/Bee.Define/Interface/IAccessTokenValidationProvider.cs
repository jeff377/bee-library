using System;

namespace Bee.Define
{
    /// <summary>
    /// AccessToken 驗證提供者介面，用於驗證 AccessToken 的有效性。
    /// </summary>
    public interface IAccessTokenValidationProvider
    {
        /// <summary>
        /// 驗證指定的 AccessToken 是否有效。
        /// </summary>
        /// <param name="accessToken">用於驗證的存取權杖。</param>
        /// <returns>若 AccessToken 有效則為 true，否則為 false。</returns>
        bool ValidateAccessToken(Guid accessToken);
    }
}
