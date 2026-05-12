using Bee.Definition;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// Test helper that builds an <see cref="IBeeContext"/> snapshot from the current
    /// <see cref="BackendInfo"/> statics. Use in test fakes or direct-construction tests
    /// once <see cref="Bee.Tests.Shared.GlobalFixture"/> has run.
    /// </summary>
    public static class TestBeeContext
    {
        /// <summary>
        /// Creates a <see cref="BeeContext"/> from the current <see cref="BackendInfo"/>
        /// statics. Returns a fresh instance per call (cheap; just snapshotting refs).
        /// </summary>
        public static IBeeContext Create()
        {
            return new BeeContext
            {
                DefineAccess = BackendInfo.DefineAccess,
                SessionInfoService = BackendInfo.SessionInfoService,
                BoFactory = BackendInfo.BusinessObjectFactory,
                Services = TestServiceProvider.Instance,
            };
        }

        /// <summary>
        /// Minimal <see cref="IServiceProvider"/> for tests; returns BackendInfo statics
        /// for the rare services BO may need (parallels Bee.Business.BackendInfoServiceProvider
        /// but lives in tests for visibility).
        /// </summary>
        private sealed class TestServiceProvider : IServiceProvider
        {
            public static readonly TestServiceProvider Instance = new();

            public object? GetService(Type serviceType)
            {
                if (serviceType == typeof(Bee.Definition.Security.IApiEncryptionKeyProvider)) return BackendInfo.ApiEncryptionKeyProvider;
                if (serviceType == typeof(Bee.Definition.Security.ILoginAttemptTracker))      return BackendInfo.LoginAttemptTracker;
                if (serviceType == typeof(Bee.Definition.Security.IAccessTokenValidator))     return BackendInfo.AccessTokenValidator;
                if (serviceType == typeof(Bee.Definition.IEnterpriseObjectService))  return BackendInfo.EnterpriseObjectService;
                if (serviceType == typeof(Bee.Definition.ICacheDataSourceProvider))  return BackendInfo.CacheDataSourceProvider;
                return null;
            }
        }
    }
}
