using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// <see cref="CacheDefineAccess"/> 尚未覆蓋的路徑：
    /// ProgramSettings / PermissionModels / Language / FormLayout（GetDefine dispatch）
    /// 與 SavePermissionModels / SaveLanguage 的寫入路徑。
    /// 各測試透過獨立 TempDir + 唯一 cache prefix 隔離，可平行執行。
    /// </summary>
    public class CacheDefineAccessMissingCoverageTests
    {
        private static readonly string[] s_zhTwCommonKeys = { "zh-TW", "Common" };
        private static readonly string[] s_testLayoutKey = { "TestLayout" };

        private sealed class TempDir : IDisposable
        {
            public PathOptions Options { get; }
            private readonly string _path;

            public TempDir()
            {
                _path = Path.Combine(Path.GetTempPath(), $"bee-mcov-{Guid.NewGuid():N}");
                Directory.CreateDirectory(_path);
                Options = new PathOptions { DefinePath = _path };
            }

            public void Dispose()
            {
                try { Directory.Delete(_path, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }

        private static CacheDefineAccess CreateAccess(PathOptions paths)
        {
            var storage = new FileDefineStorage(paths);
            var cache = new CacheContainerService(storage, paths, "mcov_" + Guid.NewGuid().ToString("N"));
            return new CacheDefineAccess(storage, paths, cache, Array.Empty<byte>());
        }

        // ── ProgramSettings ──────────────────────────────────────────────────

        [Fact]
        [DisplayName("GetProgramSettings 先存後取應回傳 ProgramSettings 實例")]
        public void GetProgramSettings_AfterSave_ReturnsInstance()
        {
            using var temp = new TempDir();
            var access = CreateAccess(temp.Options);
            access.SaveProgramSettings(new ProgramSettings());

            var result = access.GetProgramSettings();

            Assert.NotNull(result);
        }

        [Fact]
        [DisplayName("GetDefine(ProgramSettings) 應委派至 GetProgramSettings 並回傳 ProgramSettings")]
        public void GetDefine_ProgramSettings_ReturnsProgramSettings()
        {
            using var temp = new TempDir();
            var access = CreateAccess(temp.Options);
            access.SaveProgramSettings(new ProgramSettings());

            var result = access.GetDefine(DefineType.ProgramSettings);

            Assert.IsType<ProgramSettings>(result);
        }

        // ── PermissionModels ─────────────────────────────────────────────────

        [Fact]
        [DisplayName("SavePermissionModels 應寫入 PermissionModels.xml")]
        public void SavePermissionModels_WritesFile()
        {
            using var temp = new TempDir();
            var access = CreateAccess(temp.Options);

            access.SavePermissionModels(new PermissionModels());

            Assert.True(File.Exists(temp.Options.GetPermissionModelsFilePath()));
        }

        [Fact]
        [DisplayName("GetPermissionModels 先存後取應回傳 PermissionModels 實例")]
        public void GetPermissionModels_AfterSave_ReturnsInstance()
        {
            using var temp = new TempDir();
            var access = CreateAccess(temp.Options);
            access.SavePermissionModels(new PermissionModels());

            var result = access.GetPermissionModels();

            Assert.NotNull(result);
        }

        [Fact]
        [DisplayName("GetDefine(PermissionModels) 應委派至 GetPermissionModels 並回傳 PermissionModels")]
        public void GetDefine_PermissionModels_ReturnsPermissionModels()
        {
            using var temp = new TempDir();
            var access = CreateAccess(temp.Options);
            access.SavePermissionModels(new PermissionModels());

            var result = access.GetDefine(DefineType.PermissionModels);

            Assert.IsType<PermissionModels>(result);
        }

        [Fact]
        [DisplayName("SaveDefine(PermissionModels) 應委派至 SavePermissionModels 並寫入檔案")]
        public void SaveDefine_PermissionModels_DelegatesToSavePermissionModels()
        {
            using var temp = new TempDir();
            var access = CreateAccess(temp.Options);

            access.SaveDefine(DefineType.PermissionModels, new PermissionModels());

            Assert.True(File.Exists(temp.Options.GetPermissionModelsFilePath()));
        }

        // ── Language ─────────────────────────────────────────────────────────

        [Fact]
        [DisplayName("SaveLanguage 應寫入對應語系資料夾下的 Language xml")]
        public void SaveLanguage_WritesFile()
        {
            using var temp = new TempDir();
            var access = CreateAccess(temp.Options);
            var resource = new LanguageResource { Lang = "zh-TW", Namespace = "Common" };

            access.SaveLanguage(resource);

            Assert.True(File.Exists(temp.Options.GetLanguageFilePath("zh-TW", "Common")));
        }

        [Fact]
        [DisplayName("GetLanguage 先存後取應回傳 LanguageResource 實例")]
        public void GetLanguage_AfterSave_ReturnsLanguageResource()
        {
            using var temp = new TempDir();
            var access = CreateAccess(temp.Options);
            var resource = new LanguageResource { Lang = "zh-TW", Namespace = "Common" };
            access.SaveLanguage(resource);

            var result = access.GetLanguage("zh-TW", "Common");

            Assert.NotNull(result);
        }

        [Fact]
        [DisplayName("GetDefine(Language) 帶有效 keys 應委派至 GetLanguage 並回傳 LanguageResource")]
        public void GetDefine_Language_WithValidKeys_ReturnsLanguageResource()
        {
            using var temp = new TempDir();
            var access = CreateAccess(temp.Options);
            var resource = new LanguageResource { Lang = "zh-TW", Namespace = "Common" };
            access.SaveLanguage(resource);

            var result = access.GetDefine(DefineType.Language, s_zhTwCommonKeys);

            Assert.IsType<LanguageResource>(result);
        }

        [Fact]
        [DisplayName("SaveDefine(Language) 應委派至 SaveLanguage 並寫入語系 xml")]
        public void SaveDefine_Language_DelegatesToSaveLanguage()
        {
            using var temp = new TempDir();
            var access = CreateAccess(temp.Options);
            var resource = new LanguageResource { Lang = "en-US", Namespace = "Sys" };

            access.SaveDefine(DefineType.Language, resource);

            Assert.True(File.Exists(temp.Options.GetLanguageFilePath("en-US", "Sys")));
        }

        // ── FormLayout dispatch ───────────────────────────────────────────────

        [Fact]
        [DisplayName("GetDefine(FormLayout) 帶有效 key 應委派至 GetFormLayout 並回傳 FormLayout")]
        public void GetDefine_FormLayout_WithValidKey_ReturnsFormLayout()
        {
            using var temp = new TempDir();
            var access = CreateAccess(temp.Options);
            var layout = new FormLayout { LayoutId = "TestLayout" };
            access.SaveFormLayout(layout);

            var result = access.GetDefine(DefineType.FormLayout, s_testLayoutKey);

            Assert.IsType<FormLayout>(result);
        }
    }
}
