using Bee.Api.AspNetCore.Bootstrapping;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Api.AspNetCore
{
    /// <summary>
    /// Host-side activation for the Bee.NET framework. Pair with
    /// <see cref="BeeFrameworkServiceCollectionExtensions.AddBeeFramework"/>.
    /// </summary>
    public static class BeeFrameworkApplicationBuilderExtensions
    {
        /// <summary>
        /// Eager-resolves the <see cref="IDbConnectionManagerBootstrapper"/> marker so the
        /// legacy <c>DbConnectionManager</c> static facade is wired before any request is
        /// handled. The <see cref="Bee.ObjectCaching.ICacheContainer"/> is fully DI-driven
        /// (PR 5.7) and no longer requires a bootstrap step.
        /// </summary>
        public static IApplicationBuilder UseBeeFramework(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);
            app.ApplicationServices.GetRequiredService<IDbConnectionManagerBootstrapper>();
            return app;
        }
    }
}
