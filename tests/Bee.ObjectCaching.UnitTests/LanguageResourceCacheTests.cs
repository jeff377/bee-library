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
    public class LanguageResourceCacheTests
    {
        private sealed class StubDefineStorage : IDefineStorage
        {
            private readonly LanguageResource? _resource;

            public StubDefineStorage(LanguageResource? resource = null) => _resource = resource;

            public LanguageResource? GetLanguage(string lang, string ns) => _resource;
            public void SaveLanguage(LanguageResource resource) => throw new NotImplementedException();
            public DbCategorySettings? GetDbCategorySettings() => throw new NotImplementedException();
            public void SaveDbCategorySettings(DbCategorySettings settings) => throw new NotImplementedException();
            public TableSchema? GetTableSchema(string categoryId, string tableName) => throw new NotImplementedException();
            public void SaveTableSchema(string categoryId, TableSchema tableSchema) => throw new NotImplementedException();
            public FormSchema? GetFormSchema(string progId) => throw new NotImplementedException();
            public void SaveFormSchema(FormSchema formSchema) => throw new NotImplementedException();
            public FormLayout? GetFormLayout(string layoutId) => throw new NotImplementedException();
            public void SaveFormLayout(FormLayout formLayout) => throw new NotImplementedException();
        }

        private static readonly PathOptions s_emptyPaths = new();

        private sealed class TestableLanguageResourceCache : LanguageResourceCache
        {
            public TestableLanguageResourceCache(IDefineStorage storage, string cachePrefix = "")
                : base(storage, s_emptyPaths, cachePrefix) { }

            public CacheItemPolicy GetCachePolicy(string key) => GetPolicy(key);
        }

        [Fact]
        [DisplayName("建構子傳入 null storage 應拋出 ArgumentNullException")]
        public void Constructor_NullStorage_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new LanguageResourceCache(null!, s_emptyPaths));
        }

        [Fact]
        [DisplayName("建構子傳入 null paths 應拋出 ArgumentNullException")]
        public void Constructor_NullPaths_ThrowsArgumentNullException()
        {
            var stub = new StubDefineStorage();
            Assert.Throws<ArgumentNullException>(() => new LanguageResourceCache(stub, null!));
        }

        [Fact]
        [DisplayName("GetPolicy 使用 FileDefineStorage 時應設定 ChangeMonitorFilePaths")]
        public void GetPolicy_FileDefineStorage_SetsChangeMonitorFilePaths()
        {
            var storage = new FileDefineStorage(new PathOptions());
            var cache = new TestableLanguageResourceCache(storage);

            var policy = cache.GetCachePolicy("zh-TW.Common");

            Assert.NotNull(policy.ChangeMonitorFilePaths);
            Assert.Single(policy.ChangeMonitorFilePaths);
        }

        [Fact]
        [DisplayName("GetPolicy 非 FileDefineStorage 時 ChangeMonitorFilePaths 應為 null")]
        public void GetPolicy_NonFileDefineStorage_NoChangeMonitorFilePaths()
        {
            var stub = new StubDefineStorage();
            var cache = new TestableLanguageResourceCache(stub);

            var policy = cache.GetCachePolicy("zh-TW.Common");

            Assert.Null(policy.ChangeMonitorFilePaths);
        }

        [Fact]
        [DisplayName("Get 應呼叫 storage.GetLanguage 並回傳同一語言資源物件")]
        public void Get_StorageReturnsResource_ReturnsSameResource()
        {
            string prefix = Guid.NewGuid().ToString("N");
            var resource = new LanguageResource { Lang = "zh-TW", Namespace = "Common" };
            var stub = new StubDefineStorage(resource);
            var cache = new LanguageResourceCache(stub, s_emptyPaths, prefix);

            var result = cache.Get("zh-TW", "Common");

            Assert.Same(resource, result);
            cache.Remove("zh-TW", "Common");
        }

        [Fact]
        [DisplayName("Get 當 storage.GetLanguage 回傳 null 時應回傳 null")]
        public void Get_StorageReturnsNull_ReturnsNull()
        {
            string prefix = Guid.NewGuid().ToString("N");
            var stub = new StubDefineStorage();
            var cache = new LanguageResourceCache(stub, s_emptyPaths, prefix);

            var result = cache.Get("en-US", "NonExistent");

            Assert.Null(result);
        }

        [Fact]
        [DisplayName("Remove 應清除語言資源快取且不拋例外")]
        public void Remove_CachedEntry_DoesNotThrow()
        {
            string prefix = Guid.NewGuid().ToString("N");
            var resource = new LanguageResource { Lang = "zh-TW", Namespace = "System" };
            var stub = new StubDefineStorage(resource);
            var cache = new LanguageResourceCache(stub, s_emptyPaths, prefix);

            cache.Get("zh-TW", "System");

            var exception = Record.Exception(() => cache.Remove("zh-TW", "System"));
            Assert.Null(exception);
        }
    }
}
