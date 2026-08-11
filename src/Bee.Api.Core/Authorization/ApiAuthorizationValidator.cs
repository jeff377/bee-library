using Bee.Api.Core.JsonRpc;
using Bee.Definition.Security;

namespace Bee.Api.Core.Authorization
{
    /// <summary>
    /// Provides default API key and authorization validation logic.
    /// </summary>
    /// <remarks>
    /// The strength of the API key check depends on whether the deployment has issued any keys.
    /// Once <c>st_api_key</c> holds an enabled key, <see cref="IApiKeyValidator"/> has already
    /// compared the supplied key against its stored hash and this validator enforces the verdict.
    /// Until then the historical behaviour applies — a non-empty <c>X-Api-Key</c> passes — so
    /// upgrading the framework does not lock existing deployments out, and <c>UseBeeFramework</c>
    /// logs a startup warning pointing at key management.
    /// <para>
    /// Either way, user authentication is the Bearer access token's job: the API key identifies the
    /// calling application, not the user.
    /// </para>
    /// </remarks>
    public class ApiAuthorizationValidator : IApiAuthorizationValidator
    {
        /// <summary>
        /// The single message returned for every API key rejection.
        /// </summary>
        /// <remarks>
        /// WARNING: keep this identical for missing, malformed, unknown, disabled and expired keys.
        /// Distinguishing them would let a caller use the error surface to discover which keys
        /// exist; the reasons are separated in the audit record instead.
        /// </remarks>
        private const string ApiKeyRejectedMessage = "Missing or invalid API key.";

        /// <summary>
        /// The set of methods that do not require authorization (case-sensitive).
        /// </summary>
        private static readonly HashSet<string> s_noAuthMethods =
        [
            "System.Ping",
            "System.GetApiPayloadOptions",
            "System.Login"
        ];

        /// <summary>
        /// The set of methods that do not require an API key (case-sensitive).
        /// </summary>
        /// <remarks>
        /// WARNING: two separate axes — do not merge this with <see cref="s_noAuthMethods"/>. That one
        /// exempts methods from the Bearer token; this one exempts them from application identity.
        /// Merging would give away two methods that must keep requiring a key: <c>System.Login</c>
        /// is exactly where "which application attempted a sign-in" needs recording, and
        /// <c>System.GetApiPayloadOptions</c> discloses payload and encryption negotiation settings
        /// rather than answering a connectivity question.
        /// <para>
        /// <c>System.Ping</c> is exempt because a health check must still answer when the database
        /// is unavailable — the key lookup cannot be consulted then, and every other method fails
        /// closed in that state.
        /// </para>
        /// </remarks>
        private static readonly HashSet<string> s_noApiKeyMethods =
        [
            "System.Ping"
        ];

        /// <summary>
        /// Determines whether the specified JSON-RPC method requires authorization.
        /// </summary>
        /// <param name="method">The JSON-RPC method name (case-sensitive).</param>
        /// <returns><c>true</c> if authorization is required; otherwise, <c>false</c>.</returns>
        protected virtual bool IsAuthorizationRequired(string method)
        {
            return !s_noAuthMethods.Contains(method);
        }

        /// <summary>
        /// Determines whether the specified JSON-RPC method requires a valid API key.
        /// </summary>
        /// <param name="method">The JSON-RPC method name (case-sensitive).</param>
        /// <returns><c>true</c> if an API key is required; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Override to exempt a deployment's own health-check methods. Keep the list minimal: every
        /// exempt method is reachable without knowing which application is calling.
        /// </remarks>
        protected virtual bool IsApiKeyRequired(string method)
        {
            return !s_noApiKeyMethods.Contains(method);
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

            // Validate the API key, unless this method is exempt from application identity.
            if (IsApiKeyRequired(context.Method) && !IsApiKeyAccepted(context))
            {
                return ApiAuthorizationResult.Fail(JsonRpcErrorCode.InvalidRequest, ApiKeyRejectedMessage);
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

        /// <summary>
        /// Applies the API key verdict carried on the context.
        /// </summary>
        /// <param name="context">The API authorization validation context.</param>
        /// <returns><c>true</c> when the call may proceed past the key gate.</returns>
        /// <remarks>
        /// The gate is only in force once the deployment has issued a key. Before that
        /// (<see cref="ApiKeyStatus.NotConfigured"/>), and for in-process calls that never carry a
        /// header (<see cref="ApiKeyStatus.NotChecked"/>), the historical presence-only check
        /// applies so existing deployments keep working across the upgrade.
        /// </remarks>
        private static bool IsApiKeyAccepted(ApiAuthorizationContext context)
        {
            var validation = context.ApiKeyValidation ?? ApiKeyValidationResult.NotChecked;
            return validation.Status switch
            {
                ApiKeyStatus.Valid => true,
                ApiKeyStatus.NotConfigured or ApiKeyStatus.NotChecked
                    => !string.IsNullOrWhiteSpace(context.ApiKey),
                _ => false,
            };
        }
    }
}
