namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// 序列化會碰到 process-wide 狀態的測試類別。
    /// </summary>
    /// <remarks>
    /// 這個組件先前既無 <c>[Collection]</c> 也無 <c>DisableTestParallelization</c>，卻同時有三種
    /// 互相衝突的行為：
    /// <list type="number">
    /// <item><c>MasterKeyProviderTests</c> 會把 <c>BEE_MASTER_KEY</c> 環境變數設為 <c>null</c>
    /// 再於 <c>finally</c> 還原。</item>
    /// <item><c>BeeTestFixtureSmokeTests</c> 在**測試 body 內**建立 DI 容器。落在上面那個 null
    /// 視窗內的容器會走 <c>MasterKeyProvider</c> 的 <c>autoCreate</c> 分支產生一把新金鑰並寫回
    /// 環境變數，接著被 <c>finally</c> 蓋掉 —— 該容器就此持有一把跟其他東西對不上的 master key，
    /// 之後以「解密／HMAC 失敗」現形，而錯誤完全不指向真因。</item>
    /// <item><c>GlobalEventsTests</c> 斷言全域事件的觸發次數，而每建一個容器就會經
    /// <c>DatabaseSettingsCache</c> 觸發一次。</item>
    /// </list>
    /// <para>
    /// WARNING: 判準是「**碰到**同一份 process-wide 狀態」，不是「修改它」。讀取端落在寫入端的
    /// try/finally 視窗裡，行為一樣會錯 —— 這正是本 repo 先前踩過的形狀（見
    /// <c>rules/testing.md</c>）。新增會建立 DI 容器、或會動環境變數／全域事件的測試類別時，
    /// 一併加入這個 collection。
    /// </para>
    /// </remarks>
    [CollectionDefinition(Name)]
    public static class ProcessWideStateCollection
    {
        /// <summary>Collection 名稱。</summary>
        public const string Name = "ProcessWideState";
    }
}
