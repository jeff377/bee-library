using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Language;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// <see cref="LocalDefineAccess"/> 尚未覆蓋路徑的補強測試：
    /// <c>GetDefine(ProgramSettings)</c>、<c>GetDefine(PermissionModels)</c>、
    /// <c>GetDefine(Language)</c>，以及 <c>SavePermissionModels</c> / <c>SaveLanguage</c>
    /// 和對應的 <c>SaveDefine</c> 委派路徑。
    /// 使用本地暫存目錄，與其他測試完全隔離。
    /// </summary>
    public class LocalDefineAccessMissingCoverageTests
    {
        private static readonly string[] s_languageKeys = { "zh-TW", "Common" };

        private static LocalDefineAccess CreateAccess(PathOptions paths)
            => new LocalDefineAccess(new FileDefineStorage(paths), paths);

        private sealed class TempDir : IDisposable
        {
            public string Path { get; }
            public PathOptions Options { get; }

            private TempDir(string path)
            {
                Path = path;
                Options = new PathOptions { DefinePath = path };
            }

            public static TempDir Create()
            {
                var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"bee-miss-{Guid.NewGuid():N}");
                Directory.CreateDirectory(dir);
                return new TempDir(dir);
            }

            public void Dispose()
            {
                try
                {
                    if (Directory.Exists(Path))
                        Directory.Delete(Path, recursive: true);
                }
                catch (IOException)
                {
                    // best effort
                }
            }
        }

        [Fact]
        [DisplayName("GetDefine(ProgramSettings) 應回傳 ProgramSettings 實例")]
        public void GetDefine_ProgramSettings_ReturnsProgramSettings()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            access.SaveProgramSettings(new ProgramSettings());

            var result = access.GetDefine(DefineType.ProgramSettings);

            Assert.IsType<ProgramSettings>(result);
        }

        [Fact]
        [DisplayName("GetDefine(PermissionModels) 應回傳 PermissionModels 實例")]
        public void GetDefine_PermissionModels_ReturnsPermissionModels()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            access.SavePermissionModels(new PermissionModels());

            var result = access.GetDefine(DefineType.PermissionModels);

            Assert.IsType<PermissionModels>(result);
        }

        [Fact]
        [DisplayName("GetDefine(Language) 帶有效兩個 keys 應回傳 LanguageResource 實例")]
        public void GetDefine_Language_WithValidKeys_ReturnsLanguageResource()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            var resource = new LanguageResource { Lang = "zh-TW", Namespace = "Common" };
            access.SaveLanguage(resource);

            var result = access.GetDefine(DefineType.Language, s_languageKeys);

            Assert.IsType<LanguageResource>(result);
        }

        [Fact]
        [DisplayName("SavePermissionModels 應寫入 PermissionModels.xml")]
        public void SavePermissionModels_WritesFile()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);

            access.SavePermissionModels(new PermissionModels());

            Assert.True(File.Exists(temp.Options.GetPermissionModelsFilePath()));
        }

        [Fact]
        [DisplayName("SaveLanguage 應寫入對應語言目錄下的 LanguageResource xml")]
        public void SaveLanguage_WritesFile()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            var resource = new LanguageResource { Lang = "zh-TW", Namespace = "Common" };

            access.SaveLanguage(resource);

            Assert.True(File.Exists(temp.Options.GetLanguageFilePath("zh-TW", "Common")));
        }

        [Fact]
        [DisplayName("SaveDefine(PermissionModels) 應委派至 SavePermissionModels")]
        public void SaveDefine_PermissionModels_DelegatesToSavePermissionModels()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);

            access.SaveDefine(DefineType.PermissionModels, new PermissionModels());

            Assert.True(File.Exists(temp.Options.GetPermissionModelsFilePath()));
        }

        [Fact]
        [DisplayName("SaveDefine(Language) 應委派至 SaveLanguage")]
        public void SaveDefine_Language_DelegatesToSaveLanguage()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Options);
            var resource = new LanguageResource { Lang = "zh-TW", Namespace = "Common" };

            access.SaveDefine(DefineType.Language, resource);

            Assert.True(File.Exists(temp.Options.GetLanguageFilePath("zh-TW", "Common")));
        }
    }
}
