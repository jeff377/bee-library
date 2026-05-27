using System.ComponentModel;
using Bee.Definition.Language;
using Bee.Definition.Storage;

namespace Bee.Definition.UnitTests.Language
{
    /// <summary>
    /// <see cref="FileDefineStorage"/> 對 <see cref="LanguageResource"/> 的讀寫整合測試。
    /// 使用獨立 temp 目錄,不污染共享 fixture 資料。
    /// </summary>
    public class LanguageStorageRoundTripTests
    {
        [Fact]
        [DisplayName("FileDefineStorage SaveLanguage 應寫檔至 Language/{lang}/{ns}.Language.xml")]
        public void SaveLanguage_WritesFileAtExpectedPath()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"bee-lang-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var paths = new PathOptions { DefinePath = tempDir };
                var storage = new FileDefineStorage(paths);
                var resource = new LanguageResource
                {
                    Namespace = "Common",
                    Lang = "zh-TW"
                };
                resource.Items.Add("OK", "確定");

                storage.SaveLanguage(resource);

                var expectedPath = paths.GetLanguageFilePath("zh-TW", "Common");
                Assert.True(File.Exists(expectedPath), $"File not found at {expectedPath}");
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }

        [Fact]
        [DisplayName("FileDefineStorage GetLanguage 應反序列化 Save 寫入的內容")]
        public void GetLanguage_ReadsBackSavedContent()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"bee-lang-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var paths = new PathOptions { DefinePath = tempDir };
                var storage = new FileDefineStorage(paths);
                var saved = new LanguageResource
                {
                    Namespace = "Customer",
                    Lang = "en-US"
                };
                saved.Items.Add("Field.Name.Caption", "Customer Name");
                saved.Items.Add("SaveFailed", "Save failed.");
                var orderStatus = new LanguageEnum { Name = "OrderStatus" };
                orderStatus.Entries.Add("0", "Pending");
                orderStatus.Entries.Add("1", "Processing");
                saved.Enums.Add(orderStatus);
                storage.SaveLanguage(saved);

                var loaded = storage.GetLanguage("en-US", "Customer");

                Assert.NotNull(loaded);
                Assert.Equal("Customer", loaded!.Namespace);
                Assert.Equal("en-US", loaded.Lang);
                Assert.Equal(2, loaded.Items.Count);
                Assert.Equal("Customer Name", loaded.GetText("Field.Name.Caption"));
                Assert.Equal("Save failed.", loaded.GetText("SaveFailed"));
                var loadedEnum = loaded.GetEnum("OrderStatus");
                Assert.NotNull(loadedEnum);
                Assert.Equal("Pending", loadedEnum!.GetText("0"));
                Assert.Equal("Processing", loadedEnum.GetText("1"));
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }

        [Fact]
        [DisplayName("FileDefineStorage GetLanguage 對不存在檔案應回傳 null（缺譯為正常情境，可 negative-cache）")]
        public void GetLanguage_MissingFile_ReturnsNull()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"bee-lang-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var paths = new PathOptions { DefinePath = tempDir };
                var storage = new FileDefineStorage(paths);

                var result = storage.GetLanguage("zh-TW", "Nonexistent");

                Assert.Null(result);
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }
    }
}
