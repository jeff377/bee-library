using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Definition.UnitTests.Storage
{
    /// <summary>
    /// <see cref="CustomizeOnlyStorage"/> 嚴格只讀行為測試：客製檔存在→回該檔；不存在→null（不 fallback）；
    /// 非三類方法→<see cref="NotSupportedException"/>。
    /// </summary>
    public sealed class CustomizeOnlyStorageTests : IDisposable
    {
        private readonly string _root;
        private const string CustomizeId = "acme";

        public CustomizeOnlyStorageTests()
        {
            _root = Path.Combine(Path.GetTempPath(), $"bee-custstore-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            try { Directory.Delete(_root, recursive: true); } catch (IOException) { /* best effort */ }
        }

        private CustomizeOnlyStorage CreateStorage()
            => new(new CustomizeOnlyPathOptions(_root, CustomizeId));

        [Fact]
        [DisplayName("GetFormLayout 客製檔存在時應回傳該檔內容")]
        public void GetFormLayout_FileExists_ReturnsLayout()
        {
            var paths = new CustomizeOnlyPathOptions(_root, CustomizeId);
            var layout = new FormLayout { LayoutId = "EmployeeDefault" };
            XmlCodec.SerializeToFile(layout, paths.GetFormLayoutFilePath("EmployeeDefault"));

            var result = CreateStorage().GetFormLayout("EmployeeDefault");

            Assert.NotNull(result);
            Assert.Equal("EmployeeDefault", result!.LayoutId);
        }

        [Fact]
        [DisplayName("GetFormLayout 客製檔不存在時應回傳 null（不 fallback、不丟例外）")]
        public void GetFormLayout_FileMissing_ReturnsNull()
        {
            Assert.Null(CreateStorage().GetFormLayout("NonExistent"));
        }

        [Fact]
        [DisplayName("GetLanguage 客製檔存在時應回傳該檔內容")]
        public void GetLanguage_FileExists_ReturnsResource()
        {
            var paths = new CustomizeOnlyPathOptions(_root, CustomizeId);
            var resource = new LanguageResource { Lang = "zh-TW", Namespace = "Customer" };
            XmlCodec.SerializeToFile(resource, paths.GetLanguageFilePath("zh-TW", "Customer"));

            var result = CreateStorage().GetLanguage("zh-TW", "Customer");

            Assert.NotNull(result);
            Assert.Equal("Customer", result!.Namespace);
        }

        [Fact]
        [DisplayName("GetLanguage 客製檔不存在時應回傳 null")]
        public void GetLanguage_FileMissing_ReturnsNull()
        {
            Assert.Null(CreateStorage().GetLanguage("zh-TW", "NonExistent"));
        }

        [Fact]
        [DisplayName("GetFormSchema 應拋出 NotSupportedException（override 層不服務）")]
        public void GetFormSchema_ThrowsNotSupported()
        {
            Assert.Throws<NotSupportedException>(() => CreateStorage().GetFormSchema("Employee"));
        }

        [Fact]
        [DisplayName("GetTableSchema 應拋出 NotSupportedException")]
        public void GetTableSchema_ThrowsNotSupported()
        {
            Assert.Throws<NotSupportedException>(() => CreateStorage().GetTableSchema("common", "st_user"));
        }

        [Fact]
        [DisplayName("GetDbCategorySettings 應拋出 NotSupportedException")]
        public void GetDbCategorySettings_ThrowsNotSupported()
        {
            Assert.Throws<NotSupportedException>(() => CreateStorage().GetDbCategorySettings());
        }

        [Fact]
        [DisplayName("所有 Save 方法應拋出 NotSupportedException（嚴格只讀）")]
        public void SaveMethods_ThrowNotSupported()
        {
            var storage = CreateStorage();
            Assert.Throws<NotSupportedException>(() => storage.SaveDbCategorySettings(new DbCategorySettings()));
            Assert.Throws<NotSupportedException>(() => storage.SaveTableSchema("common", new TableSchema()));
            Assert.Throws<NotSupportedException>(() => storage.SaveFormLayout(new FormLayout()));
            Assert.Throws<NotSupportedException>(() => storage.SaveLanguage(new LanguageResource()));
            Assert.Throws<NotSupportedException>(() => storage.SaveFormSchema(new FormSchema()));
        }

        [Fact]
        [DisplayName("建構子傳入 null paths 應拋出 ArgumentNullException")]
        public void Constructor_NullPaths_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CustomizeOnlyStorage(null!));
        }
    }
}
