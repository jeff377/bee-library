using Bee.Api.Core.JsonRpc;

namespace Bee.Api.Core.Authorization
{
    /// <summary>
    /// Provides default API key and authorization validation logic.
    /// </summary>
    /// <remarks>
    /// WARNING: The default API key check only verifies that the <c>X-Api-Key</c> header is
    /// non-empty; it does NOT validate the key's value. Real authentication is enforced by the
    /// Bearer access token for methods that require authorization. Production hosts that rely on
    /// the API key as an access gate must override this validator (or
    /// <c>ApiServiceOptions.AuthorizationValidator</c>) to compare the key against a configured
    /// set using a constant-time comparison. <c>UseBeeFramework</c> logs a startup warning while
    /// this default validator is still in place.
    /// </remarks>
    public class ApiAuthorizationValidator : IApiAuthorizationValidator
    {
        /// <summary>
        /// The set of methods that do not require authorization (case-sensitive).
        /// </summary>
        private static readonly HashSet<string> NoAuthMethods =
        [
            "System.Ping",
            "System.GetApiPayloadOptions",
            "System.Login"
        ];

        /// <summary>
        /// Determines whether the specified JSON-RPC method requires authorization.
        /// </summary>
        /// <param name="method">The JSON-RPC method name (case-sensitive).</param>
        /// <returns><c>true</c> if authorization is required; otherwise, <c>false</c>.</returns>
        protected virtual bool IsAuthorizationRequired(string method)
        {
            return !NoAuthMethods.Contains(method);
        }

        /// <summary>
        /// Validates the API key and authorization information.
        /// </summary>
        /// <param name="context">The API authorization validation context.</param>
        /// <returns>The authorization validation result.</returns>
        public ApiAuthorizationResult Validate(ApiAuthorizationContext context)
        {
            // Validate that the input context is not null
            if (context == null)
            {
                return ApiAuthorizationResult.Fail(JsonRpcErrorCode.InvalidRequest, "Invalid authorization context.");
            }

            // Validate that an API key is present.
            // NOTE: This checks presence only, not the key's value. See the class remarks — override
            // this validator to compare the key against a configured set for production access control.
            if (string.IsNullOrWhiteSpace(context.ApiKey))
            {
                return ApiAuthorizationResult.Fail(JsonRpcErrorCode.InvalidRequest, "Missing or invalid API key.");
            }

            // For methods that do not require authorization, return success without an access token
            if (!IsAuthorizationRequired(context.Method))
            {
                return ApiAuthorizationResult.Success(Guid.Empty);
            }

            // For methods requiring authorization, validate the Authorization header
            if (string.IsNullOrWhiteSpace(context.Authorization))
            {
                return ApiAuthorizationResult.Fail(JsonRpcErrorCode.InvalidRequest, "Missing Authorization header.");
            }

            // Verify that the Authorization header uses the Bearer token format
            if (!context.Authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return ApiAuthorizationResult.Fail(JsonRpcErrorCode.InvalidRequest, "Invalid Authorization format. Expected 'Bearer <token>'.");
            }

            // Parse the Bearer token and validate it as a valid Guid
            var tokenPart = context.Authorization.Substring("Bearer ".Length).Trim();
            if (!Guid.TryParse(tokenPart, out var accessToken))
            {
                return ApiAuthorizationResult.Fail(JsonRpcErrorCode.InvalidRequest, "Invalid access token.");
            }

            // Additional validation logic can be added here, such as checking the access token against the database
            return ApiAuthorizationResult.Success(accessToken);
        }
    }
}
