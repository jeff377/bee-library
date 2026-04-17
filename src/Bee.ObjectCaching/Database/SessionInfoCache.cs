using Bee.ObjectCaching;
using Bee.Definition;
using System;

namespace Bee.ObjectCaching.Database
{
    /// <summary>
    /// Session information cache.
    /// </summary>
    internal class SessionInfoCache : KeyObjectCache<SessionInfo>
    {
        /// <summary>
        /// Creates an instance of the session information.
        /// </summary>
        /// <param name="key">The access token.</param>
        protected override SessionInfo? CreateInstance(string key)
        {
            return null; // Loading SessionInfo from the database or other sources is not yet implemented
        }

        /// <summary>
        /// Gets the session information for the specified access token.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public SessionInfo? Get(Guid accessToken)
        {
            return Get(accessToken.ToString());
        }

        /// <summary>
        /// Removes the session information from the cache.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public void Remove(Guid accessToken)
        {
            Remove(accessToken.ToString());
        }
    }
}
