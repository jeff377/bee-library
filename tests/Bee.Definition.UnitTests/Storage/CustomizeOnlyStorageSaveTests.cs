using System.ComponentModel;
using Bee.Definition.Database;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Definition.UnitTests.Storage
{
    /// <summary>
    /// 補強 <see cref="CustomizeOnlyStorage"/> 中尚未被覆蓋的兩個 Save 方法：
    /// <c>SaveDbCategorySettings</c> 與 <c>SaveTableSchema</c>，均應拋出
    /// <see cref="NotSupportedException"/>（覆蓋層嚴格只讀）。
    /// </summary>
    public sealed class CustomizeOnlyStorageSaveTests : IDisposable
    {
        private readonly string _root;
        private const string CustomizeId = "acme";

        public CustomizeOnlyStorageSaveTests()
        {
            _root = Path.Combine(Path.GetTempPath(), $"bee-custsave-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            try { Directory.Delete(_root, recursive: true); } catch (IOException) { /* best effort */ }
        }

        private CustomizeOnlyStorage CreateStorage()
            => new(new CustomizeOnlyPathOptions(_root, CustomizeId));

        [Fact]
        [DisplayName("SaveDbCategorySettings 應拋出 NotSupportedException（嚴格只讀）")]
        public void SaveDbCategorySettings_ThrowsNotSupported()
        {
            Assert.Throws<NotSupportedException>(() => CreateStorage().SaveDbCategorySettings(new DbCategorySettings()));
        }

        [Fact]
        [DisplayName("SaveTableSchema 應拋出 NotSupportedException（嚴格只讀）")]
        public void SaveTableSchema_ThrowsNotSupported()
        {
            Assert.Throws<NotSupportedException>(() => CreateStorage().SaveTableSchema("common", new TableSchema()));
        }
    }
}
