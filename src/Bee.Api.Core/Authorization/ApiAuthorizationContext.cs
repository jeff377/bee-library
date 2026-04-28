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
    }
}
