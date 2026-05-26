using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.ObjectCaching.Define;

namespace Bee.ObjectCaching.UnitTests
{
    public class FormLayoutCacheTests
    {
        private sealed class StubDefineStorage : IDefineStorage
        {
            private readonly FormLayout? _layout;

            public StubDefineStorage(FormLayout? layout = null) => _layout = layout;

            public FormLayout? GetFormLayout(string layoutId) => _layout;
            public DbCategorySettings? GetDbCategorySettings() => throw new NotImplementedException();
            public void SaveDbCategorySettings(DbCategorySettings settings) => throw new NotImplementedException();
            public TableSchema? GetTableSchema(string categoryId, string tableName) => throw new NotImplementedException();
            public void SaveTableSchema(string categoryId, TableSchema tableSchema) => throw new NotImplementedException();
            public FormSchema? GetFormSchema(string progId) => throw new NotImplementedException();
            public void SaveFormSchema(FormSchema formSchema) => throw new NotImplementedException();
            public void SaveFormLayout(FormLayout formLayout) => throw new NotImplementedException();
            public LanguageResource? GetLanguage(string lang, string ns) => throw new NotImplementedException();
            public void SaveLanguage(LanguageResource resource) => throw new NotImplementedException();
        }

        private static readonly PathOptions s_emptyPaths = new();

        private sealed class TestableFormLayoutCache : FormLayoutCache
        {
            public TestableFormLayoutCache(IDefineStorage storage, string cachePrefix = "")
                : base(storage, s_emptyPaths, cachePrefix) { }

            public CacheItemPolicy GetCachePolicy(string key) => GetPolicy(key);
        }

        [Fact]
        [DisplayName("建構子傳入 null storage 應拋出 ArgumentNullException")]
        public void Constructor_NullStorage_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new FormLayoutCache(null!, s_emptyPaths));
        }

        [Fact]
        [DisplayName("GetPolicy 使用 FileDefineStorage 時應設定 ChangeMonitorFilePaths")]
        public void GetPolicy_FileDefineStorage_SetsChangeMonitorFilePaths()
        {
            var storage = new FileDefineStorage(new PathOptions());
            var cache = new TestableFormLayoutCache(storage);

            var policy = cache.GetCachePolicy("Employee");

            Assert.NotNull(policy.ChangeMonitorFilePaths);
            Assert.Single(policy.ChangeMonitorFilePaths);
        }

        [Fact]
        [DisplayName("GetPolicy 非 FileDefineStorage 時 ChangeMonitorFilePaths 應為 null")]
        public void GetPolicy_NonFileDefineStorage_NoChangeMonitorFilePaths()
        {
            var stub = new StubDefineStorage();
            var cache = new TestableFormLayoutCache(stub);

            var policy = cache.GetCachePolicy("Employee");

            Assert.Null(policy.ChangeMonitorFilePaths);
        }

        [Fact]
        [DisplayName("Get 應呼叫 storage.GetFormLayout 並回傳結果")]
        public void Get_StorageReturnsLayout_ReturnsSameLayout()
        {
            string prefix = Guid.NewGuid().ToString("N");
            var layout = new FormLayout();
            var stub = new StubDefineStorage(layout);
            var cache = new FormLayoutCache(stub, s_emptyPaths, prefix);

            var result = cache.Get("TestLayout");

            Assert.Same(layout, result);
            cache.Remove("TestLayout");
        }

        [Fact]
        [DisplayName("Get 當 storage.GetFormLayout 回傳 null 時應回傳 null")]
        public void Get_StorageReturnsNull_ReturnsNull()
        {
            string prefix = Guid.NewGuid().ToString("N");
            var stub = new StubDefineStorage();
            var cache = new FormLayoutCache(stub, s_emptyPaths, prefix);

            var result = cache.Get("NonExistent");

            Assert.Null(result);
        }
    }
}
