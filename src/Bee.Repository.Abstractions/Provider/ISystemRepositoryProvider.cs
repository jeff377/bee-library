using Bee.Repository.Abstractions.System;

namespace Bee.Repository.Abstractions.Provider
{
    /// <summary>
    /// Interface for the system repository provider.
    /// </summary>
    public interface ISystemRepositoryProvider
    {
        /// <summary>
        /// Gets or sets the database repository.
        /// </summary>
        IDatabaseRepository DatabaseRepository { get; set; }

        /// <summary>
        /// Gets or sets the session repository.
        /// </summary>
        ISessionRepository SessionRepository { get; set; }
    }
}
