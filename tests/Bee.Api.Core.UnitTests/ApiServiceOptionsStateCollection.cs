namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 序列化所有會改寫 <see cref="ApiServiceOptions"/> process-wide 靜態元件
    /// （<c>PayloadSerializer</c> / <c>PayloadCompressor</c> / <c>PayloadEncryptor</c>）的測試類。
    /// </summary>
    /// <remarks>
    /// 這些測試以 try/finally 快照還原靜態值，但那只在**串行執行**下成立。xUnit 預設
    /// collection-per-class 會讓不同測試類平行跑，而 <c>ApiPayloadTransformer</c> 直接讀取
    /// 這些靜態值，且它位於 <c>JsonRpcExecutor</c> 加密／編碼 payload 的必經路徑上。
    /// 本機多核排程鬆不一定觸發；CI 2-core 才會紅，且失敗訊息會是
    /// <c>NoEncryptionEncryptor is only permitted in debug/development mode</c>
    /// —— 看起來像 production 安全 bug，實為測試互相污染。
    /// 根治方式是把這三個元件 DI 化；在那之前以此 collection 序列化。
    /// <para>
    /// <b>2026-08-07 補強</b>：本 collection 只涵蓋**寫入端**，而**讀取端**（約 19 個走 payload
    /// 管線的 round-trip 測試類）一樣會踩，CI build #31169045420 即因此紅。讀取端會隨新測試
    /// 持續增加，逐類補 <c>[Collection]</c> 必然遺漏，故改以 <c>AssemblyInfo.cs</c> 的
    /// <c>DisableTestParallelization</c> 整體序列化本組件。本 collection 保留作為「哪些類別會
    /// 改寫靜態元件」的標記。
    /// </para>
    /// </remarks>
    [CollectionDefinition("ApiServiceOptionsState")]
    public class ApiServiceOptionsStateCollection
    {
        // 純 marker，無 fixture
    }
}
