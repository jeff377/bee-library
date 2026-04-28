using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.System;
using Bee.Repository.System;

namespace Bee.Repository.Factories
{
    /// <summary>
    /// Default implementation of <see cref="ISystemRepositoryFactory"/>.
    /// </summary>
    public class SystemRepositoryFactory : ISystemRepositoryFactory
    {
        /// <summary>
        /// Creates an <see cref="IDatabaseRepository"/>.
        /// </summary>
        public IDatabaseRepository CreateDatabaseRepository()
        {
            return new DatabaseRepository();
        }

        /// <summary>
        /// Creates an <see cref="ISessionRepository"/>.
        /// </summary>
        public ISessionRepository CreateSessionRepository()
        {
            return new SessionRepository();
        }
    }
}
