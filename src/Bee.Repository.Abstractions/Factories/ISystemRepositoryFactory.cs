using Bee.Repository.Abstractions.System;

namespace Bee.Repository.Abstractions.Factories
{
    /// <summary>
    /// Factory for creating system-level repositories. Counterpart of
    /// <see cref="IFormRepositoryFactory"/> for system-level data access (sessions, databases,
    /// and future tenant-aware repositories like organizations).
    /// </summary>
    /// <remarks>
    /// Currently exposes stateless system-DB repositories. Future tenant-aware repositories
    /// (e.g. organization, company-settings) will accept an <c>accessToken</c> parameter
    /// so the factory can resolve the tenant database via <c>SessionInfo.DatabaseId</c>.
    /// </remarks>
    public interface ISystemRepositoryFactory
    {
        /// <summary>
        /// Creates an <see cref="IDatabaseRepository"/>.
        /// </summary>
        IDatabaseRepository CreateDatabaseRepository();

        /// <summary>
        /// Creates an <see cref="ISessionRepository"/>.
        /// </summary>
        ISessionRepository CreateSessionRepository();
    }
}
