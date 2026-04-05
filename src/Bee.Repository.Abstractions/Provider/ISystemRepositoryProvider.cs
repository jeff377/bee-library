using Bee.Repository.Abstractions.System;

namespace Bee.Repository.Abstractions.Provider
{
    /// <summary>
    /// 系統儲存庫提供者的介面。
    /// </summary>
    public interface ISystemRepositoryProvider
    {
        /// <summary>
        /// 資料庫儲存庫。
        /// </summary>
        IDatabaseRepository DatabaseRepository { get; set; }

        /// <summary>
        /// 連線資訊儲存庫。
        /// </summary>
        ISessionRepository SessionRepository { get; set; }
    }
}
