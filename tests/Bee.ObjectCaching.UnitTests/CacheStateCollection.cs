namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// 序列化會操弄 process-wide cache（<see cref="CacheContainer"/>） 與
    /// <see cref="Bee.Definition.DefinePathInfo"/> 全域狀態的測試類別。
    /// </summary>
    /// <remarks>
    /// 5 個 class：CacheContainerTests / CacheTests / DatabaseSettingsCacheTests /
    /// LocalDefineAccessTests / SystemSettingsCacheTests。它們在 cache miss 時走
    /// process-wide static 路徑、或主動透過 <c>TempDefinePath</c> 暫時切換
    /// <c>DefinePathInfo.CurrentOptions</c>，並行執行會 race。
    /// PR 5.7 將 cache 改為接 <see cref="Bee.Definition.PathOptions"/> 注入後可移除此 collection。
    /// </remarks>
    [CollectionDefinition("CacheState")]
    public class CacheStateCollection
    {
        // 純 marker，無 fixture。
    }
}
