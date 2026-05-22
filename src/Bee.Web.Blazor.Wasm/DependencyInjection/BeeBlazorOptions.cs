namespace Bee.Web.Blazor.Wasm.DependencyInjection
{
    /// <summary>
    /// Fluent options used by <see cref="BeeBlazorServiceCollectionExtensions.AddBeeBlazor"/>
    /// to configure how Blazor WebAssembly components talk to the backend.
    /// </summary>
    /// <remarks>
    /// Blazor WebAssembly runs in the browser and has no in-process backend,
    /// so only <see cref="UseRemoteProvider"/> is exposed; the host must supply
    /// the API endpoint URL.
    /// </remarks>
    public sealed class BeeBlazorOptions
    {
        /// <summary>
        /// Gets the remote endpoint URL. Empty until <see cref="UseRemoteProvider"/>
        /// has been called; reading the URL while empty surfaces a clear
        /// <see cref="InvalidOperationException"/> rather than a vague HTTP error
        /// at first call.
        /// </summary>
        public string Endpoint { get; private set; } = string.Empty;

        /// <summary>
        /// Configures the HTTP provider against <paramref name="endpoint"/>.
        /// </summary>
        /// <param name="endpoint">The remote API endpoint URL (must be non-empty).</param>
        public BeeBlazorOptions UseRemoteProvider(string endpoint)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
            Endpoint = endpoint;
            return this;
        }

        internal string RequireEndpoint()
            => string.IsNullOrEmpty(Endpoint)
                ? throw new InvalidOperationException(
                    "Bee Blazor WASM options have no endpoint. Call options.UseRemoteProvider(url) in AddBeeBlazor.")
                : Endpoint;
    }
}
