using Microsoft.Extensions.DependencyInjection;

namespace Bee.Web.Blazor.Server.DependencyInjection
{
    /// <summary>
    /// Service-collection extensions for the Blazor Server component library.
    /// </summary>
    public static class BeeBlazorServiceCollectionExtensions
    {
        /// <summary>
        /// Registers Bee Blazor Server services: the resolved
        /// <see cref="BeeBlazorOptions"/> and a <see cref="BeeApiConnectorFactory"/>
        /// that hosts inject to build connectors with the configured provider.
        /// </summary>
        /// <remarks>
        /// This call deliberately does <em>not</em> bundle <c>AddBeeFramework</c>:
        /// Blazor Server hosts that want in-process backend dispatch must call
        /// <c>AddBeeFramework</c> separately so they keep full control over the
        /// backend composition root (Bee.Hosting stays the single composition
        /// authority). See <c>docs/plans/plan-blazor-web-integration.md</c>
        /// §DI 註冊分工.
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">
        /// Optional configuration callback. When omitted, the default mode is
        /// <see cref="BeeBlazorProviderMode.Local"/> (in-process backend).
        /// </param>
        public static IServiceCollection AddBeeBlazor(
            this IServiceCollection services,
            Action<BeeBlazorOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);

            var options = new BeeBlazorOptions();
            configure?.Invoke(options);

            services.AddSingleton(options);
            services.AddSingleton<BeeApiConnectorFactory>();
            return services;
        }
    }
}
