using System;

namespace Bee.Define
{
    /// <summary>
    /// Interface for a cache data source provider.
    /// </summary>
    public interface ICacheDataSourceProvider
    {
        /// <summary>
        /// Gets the session user data for the specified access token.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        SessionUser GetSessionUser(Guid accessToken);
    }
}
