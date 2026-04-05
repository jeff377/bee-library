using Bee.Repository.Abstractions.Provider;
using Bee.Repository.Abstractions.System;
using Bee.Repository.System;

namespace Bee.Repository.Provider
{
    /// <summary>
    /// 系統儲存庫提供者。
    /// </summary>
    public class SystemRepositoryProvider : ISystemRepositoryProvider
    {
        /// <summary>
        /// 資料庫儲存庫。
        /// </summary>
        public IDatabaseRepository DatabaseRepository { get; set; } = new DatabaseRepository();

        /// <summary>
        /// 連線資訊儲存庫。
        /// </summary>
        public ISessionRepository SessionRepository { get; set; } = new SessionRepository();
    }
}
