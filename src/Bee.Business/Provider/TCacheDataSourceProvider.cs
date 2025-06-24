using System;
using Bee.Db;
using Bee.Define;

namespace Bee.Business
{
    /// <summary>
    /// 快取資料來源提供者。
    /// </summary>
    public class TCacheDataSourceProvider : ICacheDataSourceProvider
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public TCacheDataSourceProvider()
        { }

        /// <summary>
        /// 取得暫存連線的用戶資料。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public SessionUser GetSessionUser(Guid accessToken)
        {
            var repo = BackendInfo.RepositoryProvider.SessionRepository;
            return repo.GetSession(accessToken);
        }
    }
}
