using Bee.Api.Client.Connectors;

namespace Bee.Web.Blazor.Server.DependencyInjection
{
    /// <summary>
    /// Builds <see cref="FormApiConnector"/> / <see cref="SystemApiConnector"/>
    /// instances honouring the <see cref="BeeBlazorOptions"/> chosen at startup.
    /// </summary>
    /// <remarks>
    /// Registered as a singleton by
    /// <see cref="BeeBlazorServiceCollectionExtensions.AddBeeBlazor"/>; the
    /// factory itself is stateless beyond <see cref="BeeBlazorOptions"/>, and
    /// connectors are cheap to allocate per call so callers should construct one
    /// per logical operation rather than caching.
    /// </remarks>
    public class BeeApiConnectorFactory
    {
        private readonly BeeBlazorOptions _options;

        /// <summary>
        /// Initializes a new instance of <see cref="BeeApiConnectorFactory"/>.
        /// </summary>
        /// <param name="options">The resolved Blazor options.</param>
        public BeeApiConnectorFactory(BeeBlazorOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            _options = options;
        }

        /// <summary>
        /// Gets the resolved provider mode.
        /// </summary>
        public BeeBlazorProviderMode Mode => _options.Mode;

        /// <summary>
        /// Creates a <see cref="FormApiConnector"/> for the given progId and access token.
        /// </summary>
        /// <param name="accessToken">
        /// The session access token; pass <see cref="Guid.Empty"/> for anonymous calls
        /// (the BO method must declare <c>ApiAccessRequirement.Anonymous</c>).
        /// </param>
        /// <param name="progId">The program identifier (e.g. "Employee").</param>
        public virtual FormApiConnector CreateFormConnector(Guid accessToken, string progId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(progId);
            return _options.Mode == BeeBlazorProviderMode.Local
                ? new FormApiConnector(accessToken, progId)
                : new FormApiConnector(_options.Endpoint, accessToken, progId);
        }

        /// <summary>
        /// Creates a <see cref="SystemApiConnector"/> for the given access token.
        /// </summary>
        /// <param name="accessToken">
        /// The session access token; pass <see cref="Guid.Empty"/> for anonymous calls.
        /// </param>
        public virtual SystemApiConnector CreateSystemConnector(Guid accessToken)
        {
            return _options.Mode == BeeBlazorProviderMode.Local
                ? new SystemApiConnector(accessToken)
                : new SystemApiConnector(_options.Endpoint, accessToken);
        }
    }
}
