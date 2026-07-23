using Bee.Api.Core;
using Bee.Api.Core.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bee.Api.AspNetCore
{
    /// <summary>
    /// Host-side activation for the Bee.NET framework. Pair with
    /// <see cref="Bee.Hosting.BeeFrameworkServiceCollectionExtensions.AddBeeFramework"/>.
    /// </summary>
    public static class BeeFrameworkApplicationBuilderExtensions
    {
        /// <summary>
        /// Activates host-side framework startup checks. Currently emits a warning when the
        /// default <see cref="ApiAuthorizationValidator"/> is still in place.
        /// </summary>
        public static IApplicationBuilder UseBeeFramework(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            // Warn when the default authorization validator is still in place: it only checks that
            // the X-Api-Key header is non-empty (not its value), so a host that treats the API key
            // as an access gate must override it before production. Real authentication still runs
            // via the Bearer access token; this warning only flags the API-key gap.
            if (ApiServiceOptions.AuthorizationValidator is ApiAuthorizationValidator)
            {
                var logger = app.ApplicationServices
                    .GetService<ILoggerFactory>()?.CreateLogger("Bee.Api.AspNetCore");
                logger?.LogWarning(
                    "The default ApiAuthorizationValidator only verifies that the X-Api-Key header is " +
                    "non-empty, not its value. Override ApiServiceOptions.AuthorizationValidator with a " +
                    "constant-time key check before deploying to production.");
            }

            return app;
        }
    }
}
