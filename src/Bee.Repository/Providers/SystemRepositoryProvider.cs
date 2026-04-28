using Bee.Repository.Abstractions.Providers;
using Bee.Repository.Abstractions.System;
using Bee.Repository.System;

namespace Bee.Repository.Providers
{
    /// <summary>
    /// Default implementation of the system repository provider.
    /// </summary>
    public class SystemRepositoryProvider : ISystemRepositoryProvider
    {
        /// <summary>
        /// Gets or sets the database repository.
        /// </summary>
        public IDatabaseRepository DatabaseRepository { get; set; } = new DatabaseRepository();

        /// <summary>
        /// Gets or sets the session repository.
        /// </summary>
        public ISessionRepository SessionRepository { get; set; } = new SessionRepository();
    }
}
