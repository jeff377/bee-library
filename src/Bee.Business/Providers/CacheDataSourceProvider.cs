using Bee.Definition;
using Bee.Repository.Abstractions.Factories;
using Bee.Definition.Identity;

namespace Bee.Business.Providers
{
    /// <summary>
    /// Cache data source provider.
    /// </summary>
    public class CacheDataSourceProvider : ICacheDataSourceProvider
    {
        private readonly ISystemRepositoryFactory _systemFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheDataSourceProvider"/> class.
        /// </summary>
        /// <param name="systemFactory">Factory that builds system-level repositories on demand.</param>
        public CacheDataSourceProvider(ISystemRepositoryFactory systemFactory)
        {
            _systemFactory = systemFactory ?? throw new ArgumentNullException(nameof(systemFactory));
        }

        /// <summary>
        /// Gets the session user data for the specified access token.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public SessionUser? GetSessionUser(Guid accessToken)
        {
            var repo = _systemFactory.CreateSessionRepository();
            return repo.GetSession(accessToken);
        }
    }
}
