using Bee.Cache;
using Bee.Define;
using System;

namespace Bee.Cache.Database
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
        protected override SessionInfo CreateInstance(string key)
        {
            return null; // Loading SessionInfo from the database or other sources is not yet implemented

            //var accessToken = BaseFunc.CGuid(key);
            //if (BaseFunc.IsEmpty(accessToken)) { return null; }

            //// 取得暫存連線的用戶資料
            //var user = BackendInfo.CacheDataSourceProvider.GetSessionUser(accessToken);
            //if (user == null) { return null; }

            //// 傳回連線資訊
            //return new SessionInfo()
            //{
            //    AccessToken = accessToken,
            //    UserID = user.UserID,
            //    UserName = user.UserName
            //};
        }

        /// <summary>
        /// Gets the session information for the specified access token.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public SessionInfo Get(Guid accessToken)
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
