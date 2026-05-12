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
        /// Eager-resolves the bootstrap markers registered by <c>AddBeeFramework</c> so the
        /// underlying static initialization (<c>CacheContainer</c>, <c>DbConnectionManager</c>,
        /// <c>RepositoryInfo</c>) runs before any request is handled.
        /// </summary>
        public static IApplicationBuilder UseBeeFramework(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);
            app.ApplicationServices.GetRequiredService<ICacheBootstrapper>();
            app.ApplicationServices.GetRequiredService<IDbConnectionManagerBootstrapper>();
            app.ApplicationServices.GetRequiredService<IRepositoryInfoBootstrapper>();
            return app;
        }
    }
}
