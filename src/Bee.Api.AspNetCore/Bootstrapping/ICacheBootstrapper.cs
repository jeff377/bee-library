using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.ObjectCaching;

namespace Bee.Api.AspNetCore.Bootstrapping
{
    /// <summary>
    /// Marker service used to eager-resolve the <see cref="Bee.ObjectCaching"/>
    /// static initialization once during host startup.
    /// </summary>
    public interface ICacheBootstrapper { }

    internal sealed class CacheBootstrapper : ICacheBootstrapper
    {
        public CacheBootstrapper(IDefineStorage storage, BackendConfiguration configuration)
        {
            CacheContainer.Initialize(storage);
            CacheInfo.Initialize(configuration);
        }
    }
}
