namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// 集合定義：將會修改 <c>SysInfo.IsDebugMode</c> 等全域 static 狀態的測試
    /// 串行執行，避免 xUnit 預設的 collection-level parallel 在不同 class 間造成 race。
    /// </summary>
    [CollectionDefinition("SysInfo")]
    public class SysInfoCollection
    {
    }
}
