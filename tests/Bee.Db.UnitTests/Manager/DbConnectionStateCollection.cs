namespace Bee.Db.UnitTests.Manager
{
    /// <summary>
    /// 序列化 <see cref="Bee.Db.Manager.DbConnectionManager"/> 與相關 process-wide cache 的測試類別。
    /// </summary>
    /// <remarks>
    /// <see cref="DbConnectionManagerTests"/> 與 <see cref="DbAccessFactoryTests"/> 都會 mutate 共享的
    /// <c>IDefineAccess.GetDatabaseSettings().Items</c> 並呼叫 <c>DbConnectionManager.Remove/Clear</c>
    /// 等清空操作。並行執行時，一支測試的 <c>Clear()</c> 會抹掉另一支剛快取的條目並使其
    /// Count/Contains 斷言失準，因此用此 collection 強制序列化。
    /// PR 5.7 將 <c>DbConnectionManager</c> 改為 DI 注入後可全面取消序列化。
    /// </remarks>
    [CollectionDefinition("DbConnectionState")]
    public class DbConnectionStateCollection
    {
        // 純 marker，無 fixture。
    }
}
