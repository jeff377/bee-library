using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// Test helper that builds an <see cref="IBeeContext"/> snapshot from a per-class
    /// <see cref="BeeTestFixture"/>. Use in test fakes or direct-construction tests.
    /// </summary>
    public static class TestBeeContext
    {
        /// <summary>
        /// Creates a <see cref="BeeContext"/> from the supplied fixture's provider.
        /// Returns a fresh instance per call (cheap; just snapshotting refs).
        /// </summary>
        /// <param name="fixture">The per-class fixture supplying the service provider.</param>
        public static IBeeContext Create(BeeTestFixture fixture)
        {
            ArgumentNullException.ThrowIfNull(fixture);
            return CreateFrom(fixture.Provider);
        }

        /// <summary>
        /// Creates a <see cref="BeeContext"/> with one or more service overrides applied on top
        /// of the supplied fixture's provider. Handy for tests that need to inject a fake
        /// <c>ILoginAttemptTracker</c> etc. without rebuilding the container.
        /// </summary>
        public static IBeeContext CreateWithOverrides(BeeTestFixture fixture, params (Type ServiceType, object? Instance)[] overrides)
        {
            ArgumentNullException.ThrowIfNull(fixture);
            ArgumentNullException.ThrowIfNull(overrides);
            var sp = fixture.Provider;
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
        /// while keeping every other service resolved from the supplied fixture.
        /// </summary>
        public static IBeeContext CreateWithDefineAccess(BeeTestFixture fixture, IDefineAccess defineAccess)
        {
            ArgumentNullException.ThrowIfNull(fixture);
            ArgumentNullException.ThrowIfNull(defineAccess);
            var sp = fixture.Provider;
            return new BeeContext
            {
                DefineAccess = defineAccess,
                SessionInfoService = sp.GetRequiredService<ISessionInfoService>(),
                BoFactory = sp.GetRequiredService<IBusinessObjectFactory>(),
                Services = sp,
            };
        }

        private static IBeeContext CreateFrom(IServiceProvider sp)
        {
            return new BeeContext
            {
                DefineAccess = sp.GetRequiredService<IDefineAccess>(),
                SessionInfoService = sp.GetRequiredService<ISessionInfoService>(),
                BoFactory = sp.GetRequiredService<IBusinessObjectFactory>(),
                Services = sp,
            };
        }
    }
}
