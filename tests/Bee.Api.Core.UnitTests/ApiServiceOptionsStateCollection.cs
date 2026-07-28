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
    /// </remarks>
    [CollectionDefinition("ApiServiceOptionsState")]
    public class ApiServiceOptionsStateCollection
    {
        // 純 marker，無 fixture
    }
}
