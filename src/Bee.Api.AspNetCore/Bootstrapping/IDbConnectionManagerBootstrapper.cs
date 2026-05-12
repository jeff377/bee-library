using Bee.Db.Manager;

namespace Bee.Api.AspNetCore.Bootstrapping
{
    /// <summary>
    /// Marker service used to eager-resolve the DI-registered
    /// <see cref="IDbConnectionManager"/> and install it on the legacy
    /// <see cref="DbConnectionManager"/> static shim during host startup. Removed in
    /// PR 5.4 once the static shim itself is retired.
    /// </summary>
    public interface IDbConnectionManagerBootstrapper { }

    internal sealed class DbConnectionManagerBootstrapper : IDbConnectionManagerBootstrapper
    {
        public DbConnectionManagerBootstrapper(IDbConnectionManager manager)
        {
            DbConnectionManager.Initialize(manager);
        }
    }
}
