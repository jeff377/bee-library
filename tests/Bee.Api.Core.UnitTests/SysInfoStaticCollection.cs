namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 序列化本組件內所有讀寫 <c>SysInfo.IsDebugMode</c> 的測試類。
    /// </summary>
    /// <remarks>
    /// <c>JsonRpcExecutor.MapException</c> 在 debug 模式下改為透傳基礎設施例外的原訊息，
    /// 因此「斷言遮蔽訊息」與「切到 debug 模式驗證透傳」這兩類測試共用同一個 process-wide
    /// 靜態。xUnit 預設不同 test class 平行執行，不序列化就會互相污染 ——
    /// 症狀是斷言遮蔽訊息的測試偶發拿到原始例外訊息。
    ///
    /// collection 不跨組件，故 <c>Bee.Base.UnitTests</c> 的同名定義在此不適用，需各自宣告。
    /// </remarks>
    [CollectionDefinition("SysInfoStatic")]
    public class SysInfoStaticCollection
    {
        // 純 marker，無 fixture
    }
}
