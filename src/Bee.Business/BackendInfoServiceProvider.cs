using Bee.Definition;

namespace Bee.Business
{
    /// <summary>
    /// Phase 3 transitional <see cref="IServiceProvider"/> implementation that
    /// resolves a fixed set of services from <see cref="BackendInfo"/> statics.
    /// </summary>
    /// <remarks>
    /// Used as <c>IBeeContext.Services</c> until Phase 4 replaces the impl with
    /// the real DI scope. Only services that <c>BackendInfo</c> currently
    /// exposes and that BO methods may need are mapped; new mappings can be
    /// added as new BO needs arise. Returning <c>null</c> for unmapped types
    /// matches <see cref="IServiceProvider"/> semantics.
    /// </remarks>
    internal sealed class BackendInfoServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(Bee.Definition.Security.IApiEncryptionKeyProvider)) return BackendInfo.ApiEncryptionKeyProvider;
            if (serviceType == typeof(Bee.Definition.Security.ILoginAttemptTracker))      return BackendInfo.LoginAttemptTracker;
            if (serviceType == typeof(Bee.Definition.Security.IAccessTokenValidator))     return BackendInfo.AccessTokenValidator;
            if (serviceType == typeof(IEnterpriseObjectService))  return BackendInfo.EnterpriseObjectService;
            if (serviceType == typeof(ICacheDataSourceProvider))  return BackendInfo.CacheDataSourceProvider;
            return null;
        }
    }
}
