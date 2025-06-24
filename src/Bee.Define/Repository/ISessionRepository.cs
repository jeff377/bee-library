using System;

namespace Bee.Define
{
    /// <summary>
    /// 連線資訊資料存取介面。
    /// </summary>
    public interface ISessionRepository
    {
        /// <summary>
        /// 取得連線資訊。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        SessionUser GetSession(Guid accessToken);

        /// <summary>
        /// 建立一組用戶連線。
        /// </summary>
        /// <param name="userID">用戶帳號。</param>
        /// <param name="expiresIn">到期秒數。</param>
        /// <param name="oneTime">一次性有效。</param>
        SessionUser CreateSession(string userID, int expiresIn = 3600, bool oneTime = false);
    }
}