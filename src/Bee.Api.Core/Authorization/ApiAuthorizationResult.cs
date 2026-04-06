using System;
using Bee.Api.Core.JsonRpc;

namespace Bee.Api.Core.Authorization
{
    /// <summary>
    /// API authorization validation result.
    /// </summary>
    public class ApiAuthorizationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether validation succeeded.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        public JsonRpcErrorCode Code { get; set; }

        /// <summary>
        /// Gets or sets the error message when validation fails.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the access token returned upon successful validation.
        /// </summary>
        public Guid AccessToken { get; set; } = Guid.Empty;

        /// <summary>
        /// Creates a successful authorization result.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public static ApiAuthorizationResult Success(Guid accessToken)
        {
            return new ApiAuthorizationResult
            {
                IsValid = true,
                AccessToken = accessToken
            };
        }

        /// <summary>
        /// Creates a failed authorization result.
        /// </summary>
        /// <param name="code">The error code.</param>
        /// <param name="errorMessage">The error message.</param>
        public static ApiAuthorizationResult Fail(JsonRpcErrorCode code,  string errorMessage)
        {
            return new ApiAuthorizationResult
            {
                IsValid = false,
                Code = code,
                ErrorMessage = errorMessage
            };
        }
    }
}
