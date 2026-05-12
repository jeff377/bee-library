using Bee.Definition.Storage;
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
        private readonly IDefineAccess _defineAccess;

        /// <summary>
        /// Initializes a new <see cref="SystemRepositoryFactory"/>.
        /// </summary>
        /// <param name="defineAccess">The define access service used by repositories that need to read
        /// the defined table schema (e.g., schema upgrade).</param>
        public SystemRepositoryFactory(IDefineAccess defineAccess)
        {
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
        }

        /// <summary>
        /// Creates an <see cref="IDatabaseRepository"/>.
        /// </summary>
        public IDatabaseRepository CreateDatabaseRepository()
        {
            return new DatabaseRepository(_defineAccess);
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
