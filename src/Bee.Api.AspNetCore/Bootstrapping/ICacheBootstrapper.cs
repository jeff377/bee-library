using Bee.Definition.Settings;
using Bee.ObjectCaching;

namespace Bee.Api.AspNetCore.Bootstrapping
{
    /// <summary>
    /// Marker service used to eager-resolve the <see cref="Bee.ObjectCaching"/>
    /// static initialization once during host startup. Installs the DI-resolved
    /// <see cref="ICacheContainer"/> on the legacy <see cref="CacheContainer"/> static
    /// shim until PR 5.4 retires the shim itself.
    /// </summary>
    public interface ICacheBootstrapper { }

    internal sealed class CacheBootstrapper : ICacheBootstrapper
    {
        public CacheBootstrapper(ICacheContainer cacheContainer, BackendConfiguration configuration)
        {
            CacheContainer.Initialize(cacheContainer);
            CacheInfo.Initialize(configuration);
        }
    }
}
