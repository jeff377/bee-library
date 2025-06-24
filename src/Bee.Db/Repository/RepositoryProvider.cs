using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 資料儲存物件提供者，集中管理所有 Repository 的實例。
    /// 可透過繼承此類別覆寫部分實作。
    /// </summary>
    public class RepositoryProvider : IRepositoryProvider
    {
        /// <summary>
        /// 連線資訊資料存取介面。
        /// </summary>
        public virtual ISessionRepository SessionRepository => new SessionRepository();

        // 可加入更多 Repository 屬性
        // public virtual IUserRepository UserRepository => new TUserRepository();
    }

}
