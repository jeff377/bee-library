namespace Bee.Api.Core.Authorization
{
    /// <summary>
    /// Defines the interface for API key and authorization validation.
    /// </summary>
    public interface IApiAuthorizationValidator
    {
        /// <summary>
        /// Validates the API key and authorization information.
        /// </summary>
        ApiAuthorizationResult Validate(ApiAuthorizationContext context);
    }
}
