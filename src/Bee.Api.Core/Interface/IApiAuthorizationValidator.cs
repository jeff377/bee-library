namespace Bee.Api.Core
{
    /// <summary>
    /// 提供 API 金鑰與授權驗證的擴充介面。
    /// </summary>
    public interface IApiAuthorizationValidator
    {
        /// <summary>
        /// 驗證 API 金鑰與授權。
        /// </summary>
        ApiAuthorizationResult Validate(ApiAuthorizationContext context);
    }

}
