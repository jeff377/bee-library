using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// Test helper that builds an <see cref="IBeeContext"/> snapshot from the process-wide
    /// <see cref="BeeTestServices.Provider"/>. Use in test fakes or direct-construction tests
    /// once <see cref="GlobalFixture"/> has run.
    /// </summary>
    public static class TestBeeContext
    {
        /// <summary>
        /// Creates a <see cref="BeeContext"/> from the current <see cref="BeeTestServices"/>
        /// provider. Returns a fresh instance per call (cheap; just snapshotting refs).
        /// </summary>
        public static IBeeContext Create()
        {
            var sp = BeeTestServices.Provider;
            return new BeeContext
            {
                DefineAccess = sp.GetRequiredService<IDefineAccess>(),
                SessionInfoService = sp.GetRequiredService<ISessionInfoService>(),
                BoFactory = sp.GetRequiredService<IBusinessObjectFactory>(),
                Services = sp,
            };
        }

        /// <summary>
        /// Creates a <see cref="BeeContext"/> with one or more service overrides applied on top
        /// of the process-wide provider. Handy for tests that need to inject a fake
        /// <c>ILoginAttemptTracker</c> etc. without rebuilding the container.
        /// </summary>
        public static IBeeContext CreateWithOverrides(params (Type ServiceType, object? Instance)[] overrides)
        {
            var sp = BeeTestServices.Provider;
            return new BeeContext
            {
                DefineAccess = sp.GetRequiredService<IDefineAccess>(),
                SessionInfoService = sp.GetRequiredService<ISessionInfoService>(),
                BoFactory = sp.GetRequiredService<IBusinessObjectFactory>(),
                Services = new TestOverrideServiceProvider(sp, overrides),
            };
        }

        /// <summary>
        /// Creates a <see cref="BeeContext"/> with a custom <see cref="IDefineAccess"/> swapped in.
        /// Used by tests that need to redirect <c>Save*</c> writes to an isolated temp directory
        /// while keeping every other service resolved from the process-wide provider.
        /// </summary>
        public static IBeeContext CreateWithDefineAccess(IDefineAccess defineAccess)
        {
            ArgumentNullException.ThrowIfNull(defineAccess);
            var sp = BeeTestServices.Provider;
            return new BeeContext
            {
                DefineAccess = defineAccess,
                SessionInfoService = sp.GetRequiredService<ISessionInfoService>(),
                BoFactory = sp.GetRequiredService<IBusinessObjectFactory>(),
                Services = sp,
            };
        }
    }
}
