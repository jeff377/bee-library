using Microsoft.Extensions.DependencyInjection;

namespace Bee.Web.Blazor.Wasm.DependencyInjection
{
    /// <summary>
    /// Service-collection extensions for the Blazor WebAssembly component library.
    /// </summary>
    public static class BeeBlazorServiceCollectionExtensions
    {
        /// <summary>
        /// Registers Bee Blazor WebAssembly services: the resolved
        /// <see cref="BeeBlazorOptions"/> and a <see cref="BeeApiConnectorFactory"/>
        /// that hosts inject to build connectors against the configured endpoint.
        /// </summary>
        /// <remarks>
        /// Hosts must call <c>options.UseRemoteProvider(url)</c> in
        /// <paramref name="configure"/>; the browser has no in-process backend,
        /// so the endpoint URL is mandatory.
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration callback.</param>
        public static IServiceCollection AddBeeBlazor(
            this IServiceCollection services,
            Action<BeeBlazorOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);

            var options = new BeeBlazorOptions();
            configure(options);

            services.AddSingleton(options);
            services.AddSingleton<BeeApiConnectorFactory>();
            return services;
        }
    }
}
