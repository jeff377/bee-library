namespace Bee.Web.Blazor.Server.DependencyInjection
{
    /// <summary>
    /// Fluent options used by <see cref="BeeBlazorServiceCollectionExtensions.AddBeeBlazor"/>
    /// to decide whether Blazor Server components talk to the backend in-process
    /// (<see cref="BeeBlazorProviderMode.Local"/>) or over HTTP
    /// (<see cref="BeeBlazorProviderMode.Remote"/>).
    /// </summary>
    /// <remarks>
    /// Blazor Server hosts typically share a process with the backend
    /// (<see cref="UseLocalProvider"/>); Server deployments that talk to a
    /// separate API host can still opt into <see cref="UseRemoteProvider"/>.
    /// </remarks>
    public sealed class BeeBlazorOptions
    {
        /// <summary>
        /// Gets the resolved provider mode (<c>Local</c> by default for Blazor Server).
        /// </summary>
        public BeeBlazorProviderMode Mode { get; private set; } = BeeBlazorProviderMode.Local;

        /// <summary>
        /// Gets the remote endpoint URL; empty when <see cref="Mode"/> is
        /// <see cref="BeeBlazorProviderMode.Local"/>.
        /// </summary>
        public string Endpoint { get; private set; } = string.Empty;

        /// <summary>
        /// Configures the in-process (<see cref="BeeBlazorProviderMode.Local"/>)
        /// provider. The host must also call <c>AddBeeFramework</c> and assign
        /// <c>ApiClientInfo.LocalServiceProvider</c> so connector calls can be
        /// dispatched in process.
        /// </summary>
        public BeeBlazorOptions UseLocalProvider()
        {
            Mode = BeeBlazorProviderMode.Local;
            Endpoint = string.Empty;
            return this;
        }

        /// <summary>
        /// Configures the HTTP (<see cref="BeeBlazorProviderMode.Remote"/>)
        /// provider. Connector calls are dispatched to <paramref name="endpoint"/>.
        /// </summary>
        /// <param name="endpoint">The remote API endpoint URL (must be non-empty).</param>
        public BeeBlazorOptions UseRemoteProvider(string endpoint)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
            Mode = BeeBlazorProviderMode.Remote;
            Endpoint = endpoint;
            return this;
        }
    }
}
