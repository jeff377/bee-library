using Bee.Definition.Security;

namespace Bee.Api.Core.Authorization
{
    /// <summary>
    /// API authorization validation context.
    /// </summary>
    public class ApiAuthorizationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiAuthorizationContext"/> class.
        /// </summary>
        public ApiAuthorizationContext()
        {
        }

        /// <summary>
        /// Gets or sets the API key.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Authorization header value.
        /// </summary>
        public string Authorization { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the JSON-RPC method name.
        /// </summary>
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the verdict already reached on <see cref="ApiKey"/>, or
        /// <see cref="ApiKeyValidationResult.NotChecked"/> when the key gate did not run.
        /// </summary>
        /// <remarks>
        /// Deliberately a verdict rather than an <see cref="IApiKeyValidator"/>: this type is a data
        /// carrier, and holding a service here would make it a service locator and put the
        /// authorization validator — which only decides — in charge of running the check.
        /// The transport layer validates once and the verdict is then read by the authorization
        /// decision, the connectivity probe's response and the audit record alike.
        /// </remarks>
        public ApiKeyValidationResult ApiKeyValidation { get; set; } = ApiKeyValidationResult.NotChecked;
    }
}
