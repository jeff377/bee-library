using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Language;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.ObjectCaching;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// 補強 <see cref="LocalDefineAccess"/> 中 PermissionModels 與 Language 存取路徑的覆蓋測試，
    /// 以及 GetDefine(ProgramSettings) 的 dispatch 路徑。
    /// 各測試以本地 temp 目錄隔離 PathOptions 與 CacheContainerService，可與其他 test class 平行執行。
    /// </summary>
    public class LocalDefineAccessMissingCoverageTests
    {
        private static readonly string[] s_zhTwCommonKeys = { "zh-TW", "Common" };

        private sealed class TempContext : IDisposable
        {
            public string Dir { get; }
            public PathOptions Paths { get; }
            public LocalDefineAccess Access { get; }

            public TempContext()
            {
                Dir = Path.Combine(Path.GetTempPath(), $"bee-miss-{Guid.NewGuid():N}");
                Directory.CreateDirectory(Dir);
                Paths = new PathOptions { DefinePath = Dir };
                var storage = new FileDefineStorage(Paths);
                var cache = new CacheContainerService(storage, Paths, Guid.NewGuid().ToString("N"));
                Access = new LocalDefineAccess(storage, Paths, cache, Array.Empty<byte>());
            }

            public void Dispose()
            {
                try { Directory.Delete(Dir, recursive: true); } catch (IOException) { }
            }
        }

        [Fact]
        [DisplayName("SavePermissionModels 應寫入 PermissionModels.xml")]
        public void SavePermissionModels_WritesFile()
        {
            using var ctx = new TempContext();
            ctx.Access.SavePermissionModels(new PermissionModels());
            Assert.True(File.Exists(ctx.Paths.GetPermissionModelsFilePath()));
        }

        [Fact]
        [DisplayName("GetPermissionModels 檔案存在時應回傳 PermissionModels 實例")]
        public void GetPermissionModels_FileExists_ReturnsInstance()
        {
            using var ctx = new TempContext();
            ctx.Access.SavePermissionModels(new PermissionModels());

            var result = ctx.Access.GetPermissionModels();
            Assert.NotNull(result);
        }

        [Fact]
        [DisplayName("GetDefine(PermissionModels) 應回傳 PermissionModels 實例")]
        public void GetDefine_PermissionModels_ReturnsPermissionModels()
        {
            using var ctx = new TempContext();
            ctx.Access.SavePermissionModels(new PermissionModels());

            var result = ctx.Access.GetDefine(DefineType.PermissionModels);
            Assert.IsType<PermissionModels>(result);
        }

        [Fact]
        [DisplayName("SaveDefine(PermissionModels) 應委派至 SavePermissionModels 並寫入檔案")]
        public void SaveDefine_PermissionModels_DelegatesToSavePermissionModels()
        {
            using var ctx = new TempContext();
            ctx.Access.SaveDefine(DefineType.PermissionModels, new PermissionModels());
            Assert.True(File.Exists(ctx.Paths.GetPermissionModelsFilePath()));
        }

        [Fact]
        [DisplayName("SaveLanguage 應寫入對應的 Language XML 檔案")]
        public void SaveLanguage_WritesFile()
        {
            using var ctx = new TempContext();
            var resource = new LanguageResource { Lang = "zh-TW", Namespace = "Common" };
            ctx.Access.SaveLanguage(resource);
            Assert.True(File.Exists(ctx.Paths.GetLanguageFilePath("zh-TW", "Common")));
        }

        [Fact]
        [DisplayName("GetLanguage 儲存後讀取應回傳 LanguageResource 實例")]
        public void GetLanguage_AfterSave_ReturnsInstance()
        {
            using var ctx = new TempContext();
            var resource = new LanguageResource { Lang = "zh-TW", Namespace = "Common" };
            ctx.Access.SaveLanguage(resource);

            var result = ctx.Access.GetLanguage("zh-TW", "Common");
            Assert.NotNull(result);
        }

        [Fact]
        [DisplayName("GetDefine(Language) 帶兩個 keys 應回傳 LanguageResource 實例")]
        public void GetDefine_Language_WithCorrectKeys_ReturnsLanguageResource()
        {
            using var ctx = new TempContext();
            var resource = new LanguageResource { Lang = "zh-TW", Namespace = "Common" };
            ctx.Access.SaveLanguage(resource);

            var result = ctx.Access.GetDefine(DefineType.Language, s_zhTwCommonKeys);
            Assert.IsType<LanguageResource>(result);
        }

        [Fact]
        [DisplayName("SaveDefine(Language) 應委派至 SaveLanguage 並寫入檔案")]
        public void SaveDefine_Language_DelegatesToSaveLanguage()
        {
            using var ctx = new TempContext();
            var resource = new LanguageResource { Lang = "en-US", Namespace = "Test" };
            ctx.Access.SaveDefine(DefineType.Language, resource);
            Assert.True(File.Exists(ctx.Paths.GetLanguageFilePath("en-US", "Test")));
        }

        [Fact]
        [DisplayName("GetDefine(ProgramSettings) 應回傳 ProgramSettings 實例")]
        public void GetDefine_ProgramSettings_ReturnsProgramSettings()
        {
            using var ctx = new TempContext();
            ctx.Access.SaveProgramSettings(new ProgramSettings());

            var result = ctx.Access.GetDefine(DefineType.ProgramSettings);
            Assert.IsType<ProgramSettings>(result);
        }
    }
}
