using System;

namespace Bee.Define
{
    /// <summary>
    /// 快取資料來源提供者介面。
    /// </summary>
    public interface ICacheDataSourceProvider
    {
        /// <summary>
        /// 取得暫存連線的用戶資料。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        TSessionUser GetSessionUser(Guid accessToken);
    }
}
