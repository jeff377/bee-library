namespace Bee.Base.UnitTests
{
    /// <summary>
    /// 序列化所有會改寫 <see cref="SysInfo"/> process-wide 靜態狀態
    /// （<c>Version</c> / <c>IsDebugMode</c> / <c>AllowedTypeNamespaces</c>）的測試類。
    /// </summary>
    /// <remarks>
    /// 此名稱先前已被 <c>[Collection("SysInfoStatic")]</c> 使用但**沒有對應的定義**。
    /// xUnit 的隱式分組讓它仍然運作，但少了定義就沒有編譯期保護：名稱打錯一個字不會編譯錯，
    /// 序列化會靜默失效，變成只在 CI 才重現的 race。
    /// </remarks>
    [CollectionDefinition("SysInfoStatic")]
    public class SysInfoStaticCollection
    {
        // 純 marker，無 fixture
    }
}
