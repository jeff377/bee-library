using Microsoft.AspNetCore.Builder;

namespace Bee.Api.AspNetCore
{
    /// <summary>
    /// Host-side activation for the Bee.NET framework. Pair with
    /// <see cref="Bee.Hosting.BeeFrameworkServiceCollectionExtensions.AddBeeFramework"/>.
    /// </summary>
    /// <remarks>
    /// As of Phase 7 this extension performs no bootstrap work — all framework
    /// services are resolved through ctor injection. The method is retained as an
    /// API hook for future middleware registration without breaking existing host
    /// startup code.
    /// </remarks>
    public static class BeeFrameworkApplicationBuilderExtensions
    {
        /// <summary>
        /// No-op activation hook reserved for future framework middleware.
        /// </summary>
        public static IApplicationBuilder UseBeeFramework(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);
            return app;
        }
    }
}
