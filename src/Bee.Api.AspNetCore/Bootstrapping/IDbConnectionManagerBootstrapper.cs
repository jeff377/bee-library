using Bee.Db.Manager;
using Bee.Definition;

namespace Bee.Api.AspNetCore.Bootstrapping
{
    /// <summary>
    /// Marker service used to eager-resolve <see cref="DbConnectionManager.Initialize"/>
    /// once during host startup.
    /// </summary>
    public interface IDbConnectionManagerBootstrapper { }

    internal sealed class DbConnectionManagerBootstrapper : IDbConnectionManagerBootstrapper
    {
        public DbConnectionManagerBootstrapper(IDatabaseSettingsProvider provider)
        {
            DbConnectionManager.Initialize(provider);
        }
    }
}
