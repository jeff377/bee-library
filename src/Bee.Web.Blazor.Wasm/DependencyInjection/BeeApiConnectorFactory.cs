using Bee.Api.Client.Connectors;

namespace Bee.Web.Blazor.Wasm.DependencyInjection
{
    /// <summary>
    /// Builds <see cref="FormApiConnector"/> / <see cref="SystemApiConnector"/>
    /// instances pointing at the endpoint configured in <see cref="BeeBlazorOptions"/>.
    /// </summary>
    /// <remarks>
    /// Blazor WebAssembly always uses <c>RemoteApiProvider</c> — the browser
    /// has no in-process backend so the factory has only one code path.
    /// Registered as a singleton by
    /// <see cref="BeeBlazorServiceCollectionExtensions.AddBeeBlazor"/>.
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
            return new FormApiConnector(_options.RequireEndpoint(), accessToken, progId);
        }

        /// <summary>
        /// Creates a <see cref="SystemApiConnector"/> for the given access token.
        /// </summary>
        /// <param name="accessToken">
        /// The session access token; pass <see cref="Guid.Empty"/> for anonymous calls.
        /// </param>
        public virtual SystemApiConnector CreateSystemConnector(Guid accessToken)
            => new(_options.RequireEndpoint(), accessToken);
    }
}
