using System.ComponentModel;
using Bee.Definition.Database;
using Bee.Definition.Settings;

namespace Bee.Definition.UnitTests.Storage
{
    /// <summary>
    /// 補強 <see cref="CustomizeOnlyStorage"/> 中尚未覆蓋的兩個只讀保護方法：
    /// <see cref="CustomizeOnlyStorage.SaveDbCategorySettings"/> 與
    /// <see cref="CustomizeOnlyStorage.SaveTableSchema"/>，均應拋 <see cref="NotSupportedException"/>。
    /// </summary>
    public sealed class CustomizeOnlyStorageSaveTests
    {
        private static CustomizeOnlyStorage CreateStorage()
            => new(new CustomizeOnlyPathOptions(Path.GetTempPath(), "test-cust"));

        [Fact]
        [DisplayName("SaveDbCategorySettings 應拋出 NotSupportedException（override 層嚴格只讀）")]
        public void SaveDbCategorySettings_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => CreateStorage().SaveDbCategorySettings(new DbCategorySettings()));
        }

        [Fact]
        [DisplayName("SaveTableSchema 應拋出 NotSupportedException（override 層嚴格只讀）")]
        public void SaveTableSchema_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => CreateStorage().SaveTableSchema("common", new TableSchema()));
        }
    }
}
