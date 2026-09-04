using System.Data.Common;
using Bee.Definition.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        /// Activates host-side framework startup checks. Currently emits a warning while no API key
        /// has been issued, which leaves the <c>X-Api-Key</c> header checked for presence only.
        /// </summary>
        public static IApplicationBuilder UseBeeFramework(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            var logger = app.ApplicationServices
                .GetService<ILoggerFactory>()?.CreateLogger("Bee.Api.AspNetCore");
            WarnWhenApiKeyGateIsNotInForce(app.ApplicationServices, logger);

            return app;
        }

        /// <summary>
        /// Warns when <c>st_api_key</c> holds no enabled key, so the framework is still applying the
        /// pre-gate behaviour of accepting any non-empty <c>X-Api-Key</c>.
        /// </summary>
        /// <param name="services">The application service provider.</param>
        /// <param name="logger">The logger, or <c>null</c> when logging is unavailable.</param>
        /// <remarks>
        /// The remedy this points at is issuing a key, not writing code: the gate closes by itself as
        /// soon as one enabled key exists. That is the practical difference from the previous warning,
        /// which could only ask hosts to replace the validator.
        /// <para>
        /// NOTE: a store that cannot be reached is reported separately rather than as "no keys".
        /// Startup must not fail over this, but nor should an unreachable database read as an open
        /// gate — at run time the same condition rejects calls.
        /// </para>
        /// </remarks>
        private static void WarnWhenApiKeyGateIsNotInForce(IServiceProvider services, ILogger? logger)
        {
            if (logger == null) { return; }

            // Asked through the definition-layer seam rather than ICacheContainer: this layer has no
            // business knowing the answer is cached, let alone by which assembly.
            var gateState = services.GetService<IApiKeyGateStateProvider>();
            if (gateState == null) { return; }

            try
            {
                var gate = gateState.GetState();
                if (gate is { InForce: true }) { return; }

                // Error rather than Warning: this is the difference between having an API key gate
                // and not having one, and a Warning is the level operators filter out. Startup is
                // deliberately not failed — `IsApiKeyAccepted` documents the presence-only fallback
                // as the path that keeps existing deployments working across an upgrade, and
                // turning that into a hard failure in a minor release would stop them booting.
                var environment = services.GetService<IHostEnvironment>();
                if (environment?.IsDevelopment() == true)
                {
                    logger.LogWarning(
                        "No enabled API key exists, so the X-Api-Key header is only checked for presence. " +
                        "That is expected in Development; issue an API key before deploying.");
                    return;
                }

                logger.LogError(
                    "No enabled API key exists, so the X-Api-Key header is only checked for presence, " +
                    "not for its value — this deployment has no working API key gate. Issue an API key " +
                    "to turn it into a real one; no code change is required, and existing callers keep " +
                    "working until the first key exists.");
            }
            catch (DbException ex)
            {
                logger.LogWarning(ex, "Could not determine whether any API key is enabled; API calls will be rejected while the store is unreachable.");
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Could not determine whether any API key is enabled; API calls will be rejected while the store is unreachable.");
            }
        }
    }
}
