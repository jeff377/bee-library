namespace Bee.Tests.Shared
{
    /// <summary>
    /// 定義全域測試集合，與 <see cref="GlobalFixture"/> 綁定，
    /// 讓多個測試類別共用相同的初始化邏輯。
    /// </summary>
    [CollectionDefinition("Initialize")]
    public class GlobalCollection : ICollectionFixture<GlobalFixture>
    {
        // 不需要任何程式碼，僅作為集合定義
    }

}
