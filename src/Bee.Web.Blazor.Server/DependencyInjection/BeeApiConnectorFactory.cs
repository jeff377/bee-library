using Bee.Api.Client;
using Bee.Api.Client.Connectors;

namespace Bee.Web.Blazor.Server.DependencyInjection
{
    /// <summary>
    /// Builds <see cref="FormApiConnector"/> / <see cref="SystemApiConnector"/>
    /// instances honouring the <see cref="BeeBlazorOptions"/> chosen at startup.
    /// </summary>
    /// <remarks>
    /// Registered as <b>scoped</b> by
    /// <see cref="BeeBlazorServiceCollectionExtensions.AddBeeBlazor"/> — one per circuit — and
    /// connectors are cheap to allocate per call so callers should construct one per logical
    /// operation rather than caching.
    /// <para>
    /// WARNING: The scope is what makes <see cref="ApiSessionContext"/> per-user. It was a singleton
    /// before, which meant every circuit shared one transmission key: in Remote mode the last login
    /// overwrote the rest, and the earlier users' encrypted requests failed to decrypt until they
    /// signed in again. Do not register this as a singleton.
    /// </para>
    /// </remarks>
    public class BeeApiConnectorFactory
    {
        private readonly BeeBlazorOptions _options;
        private readonly ApiSessionContext _session;

        /// <summary>
        /// Initializes a new instance of <see cref="BeeApiConnectorFactory"/> sharing the process-wide
        /// session state.
        /// </summary>
        /// <param name="options">The resolved Blazor options.</param>
        /// <remarks>
        /// Retained for source compatibility. A host serving several users from one process should use
        /// the overload that takes an <see cref="ApiSessionContext"/>.
        /// </remarks>
        public BeeApiConnectorFactory(BeeBlazorOptions options)
            : this(options, ApiSessionContext.Ambient)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="BeeApiConnectorFactory"/> for one session.
        /// </summary>
        /// <param name="options">The resolved Blazor options.</param>
        /// <param name="session">The per-circuit session state handed to every connector it creates.</param>
        public BeeApiConnectorFactory(BeeBlazorOptions options, ApiSessionContext session)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(session);
            _options = options;
            _session = session;
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
        /// (the BO method must declare <see cref="Bee.Definition.Security.ApiAccessRequirement.Anonymous"/>).
        /// </param>
        /// <param name="progId">The program identifier (e.g. "Employee").</param>
        public virtual FormApiConnector CreateFormConnector(Guid accessToken, string progId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(progId);
            return _options.Mode == BeeBlazorProviderMode.Local
                ? new FormApiConnector(accessToken, progId, _session)
                : new FormApiConnector(_options.Endpoint, accessToken, progId, _session);
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
                ? new SystemApiConnector(accessToken, _session)
                : new SystemApiConnector(_options.Endpoint, accessToken, _session);
        }
    }
}
