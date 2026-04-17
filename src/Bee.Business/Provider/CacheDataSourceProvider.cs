using System;
using Bee.Definition;
using Bee.Repository.Abstractions;

namespace Bee.Business.Provider
{
    /// <summary>
    /// Cache data source provider.
    /// </summary>
    public class CacheDataSourceProvider : ICacheDataSourceProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheDataSourceProvider"/> class.
        /// </summary>
        public CacheDataSourceProvider()
        { }

        /// <summary>
        /// Gets the session user data for the specified access token.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public SessionUser? GetSessionUser(Guid accessToken)
        {
            var repo = RepositoryInfo.SystemProvider!.SessionRepository;
            return repo.GetSession(accessToken);
        }
    }
}
