namespace Bee.Define
{
    /// <summary>
    /// 資料儲存物件提供者介面，定義所有 Repository 的取得方式。
    /// </summary>
    public interface IRepositoryProvider
    {
        /// <summary>
        /// 連線資訊資料存取介面。
        /// </summary>
        ISessionRepository SessionRepository { get; }
    }
}
