using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// 補強 <see cref="LocalDefineAccess"/> 中尚未被覆蓋的 Get/Save 路徑：
    /// <c>ProgramSettings</c>、<c>PermissionModels</c>、<c>FormLayout</c>（單引數 overload）、
    /// <c>Language</c>（GetDefine 路徑 + 直接方法 + Save 路徑）。
    /// 每個測試使用獨立暫存目錄，可與其他 test class 平行執行。
    /// </summary>
    public class LocalDefineAccessGetRemainingTests
    {
        private static readonly string[] s_formLayoutKey = { "RL_Test" };
        private static readonly string[] s_languageKeys = { "zh-TW", "Common" };

        private sealed class TempDir : IDisposable
        {
            public string Root { get; }
            public PathOptions Paths { get; }

            private TempDir(string root)
            {
                Root = root;
                Paths = new PathOptions { DefinePath = root };
            }

            public static TempDir Create()
            {
                var dir = Path.Combine(Path.GetTempPath(), $"bee-get-{Guid.NewGuid():N}");
                Directory.CreateDirectory(dir);
                return new TempDir(dir);
            }

            public void Dispose()
            {
                try
                {
                    if (Directory.Exists(Root))
                        Directory.Delete(Root, recursive: true);
                }
                catch (IOException) { }
            }
        }

        private static LocalDefineAccess CreateAccess(PathOptions paths)
            => new LocalDefineAccess(new FileDefineStorage(paths), paths);

        [Fact]
        [DisplayName("GetDefine(ProgramSettings) 應回傳 ProgramSettings 實例")]
        public void GetDefine_ProgramSettings_ReturnsProgramSettings()
        {
            using var temp = TempDir.Create();
            XmlCodec.SerializeToFile(new ProgramSettings(), temp.Paths.GetProgramSettingsFilePath());
            var access = CreateAccess(temp.Paths);

            var result = access.GetDefine(DefineType.ProgramSettings);

            Assert.IsType<ProgramSettings>(result);
        }

        [Fact]
        [DisplayName("GetProgramSettings 直接呼叫應回傳 ProgramSettings 實例")]
        public void GetProgramSettings_DirectCall_ReturnsProgramSettings()
        {
            using var temp = TempDir.Create();
            XmlCodec.SerializeToFile(new ProgramSettings(), temp.Paths.GetProgramSettingsFilePath());
            var access = CreateAccess(temp.Paths);

            var result = access.GetProgramSettings();

            Assert.IsType<ProgramSettings>(result);
        }

        [Fact]
        [DisplayName("GetDefine(PermissionModels) 應回傳 PermissionModels 實例")]
        public void GetDefine_PermissionModels_ReturnsPermissionModels()
        {
            using var temp = TempDir.Create();
            XmlCodec.SerializeToFile(new PermissionModels(), temp.Paths.GetPermissionModelsFilePath());
            var access = CreateAccess(temp.Paths);

            var result = access.GetDefine(DefineType.PermissionModels);

            Assert.IsType<PermissionModels>(result);
        }

        [Fact]
        [DisplayName("GetPermissionModels 直接呼叫應回傳 PermissionModels 實例")]
        public void GetPermissionModels_DirectCall_ReturnsPermissionModels()
        {
            using var temp = TempDir.Create();
            XmlCodec.SerializeToFile(new PermissionModels(), temp.Paths.GetPermissionModelsFilePath());
            var access = CreateAccess(temp.Paths);

            var result = access.GetPermissionModels();

            Assert.IsType<PermissionModels>(result);
        }

        [Fact]
        [DisplayName("GetDefine(FormLayout) 帶有效 key 應回傳 FormLayout 實例")]
        public void GetDefine_FormLayout_ValidKey_ReturnsFormLayout()
        {
            using var temp = TempDir.Create();
            XmlCodec.SerializeToFile(
                new FormLayout { LayoutId = "RL_Test" },
                temp.Paths.GetFormLayoutFilePath("RL_Test"));
            var access = CreateAccess(temp.Paths);

            var result = access.GetDefine(DefineType.FormLayout, s_formLayoutKey);

            Assert.IsType<FormLayout>(result);
        }

        [Fact]
        [DisplayName("GetDefine(Language) 帶有效 keys 應回傳 LanguageResource 實例")]
        public void GetDefine_Language_ValidKeys_ReturnsLanguageResource()
        {
            using var temp = TempDir.Create();
            XmlCodec.SerializeToFile(
                new LanguageResource { Lang = "zh-TW", Namespace = "Common" },
                temp.Paths.GetLanguageFilePath("zh-TW", "Common"));
            var access = CreateAccess(temp.Paths);

            var result = access.GetDefine(DefineType.Language, s_languageKeys);

            Assert.IsType<LanguageResource>(result);
        }

        [Fact]
        [DisplayName("GetDefine(Language) keys 為 null 應拋 ArgumentException")]
        public void GetDefine_Language_NullKeys_ThrowsArgumentException()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Paths);

            Assert.Throws<ArgumentException>(() => access.GetDefine(DefineType.Language, null));
        }

        [Fact]
        [DisplayName("GetLanguage 直接呼叫應回傳 LanguageResource 實例")]
        public void GetLanguage_DirectCall_ReturnsLanguageResource()
        {
            using var temp = TempDir.Create();
            XmlCodec.SerializeToFile(
                new LanguageResource { Lang = "zh-TW", Namespace = "Common" },
                temp.Paths.GetLanguageFilePath("zh-TW", "Common"));
            var access = CreateAccess(temp.Paths);

            var result = access.GetLanguage("zh-TW", "Common");

            Assert.IsType<LanguageResource>(result);
        }

        [Fact]
        [DisplayName("SaveDefine(PermissionModels) 應委派至 SavePermissionModels 並寫入檔案")]
        public void SaveDefine_PermissionModels_WritesFile()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Paths);

            access.SaveDefine(DefineType.PermissionModels, new PermissionModels());

            Assert.True(File.Exists(temp.Paths.GetPermissionModelsFilePath()));
        }

        [Fact]
        [DisplayName("SavePermissionModels 直接呼叫應寫入 PermissionModels.xml")]
        public void SavePermissionModels_DirectCall_WritesFile()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Paths);

            access.SavePermissionModels(new PermissionModels());

            Assert.True(File.Exists(temp.Paths.GetPermissionModelsFilePath()));
        }

        [Fact]
        [DisplayName("SaveDefine(Language) 應委派至 SaveLanguage 並寫入檔案")]
        public void SaveDefine_Language_WritesFile()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Paths);
            var resource = new LanguageResource { Lang = "zh-TW", Namespace = "Common" };

            access.SaveDefine(DefineType.Language, resource);

            Assert.True(File.Exists(temp.Paths.GetLanguageFilePath("zh-TW", "Common")));
        }

        [Fact]
        [DisplayName("SaveLanguage 直接呼叫應寫入語言資源 xml 檔案")]
        public void SaveLanguage_DirectCall_WritesFile()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Paths);
            var resource = new LanguageResource { Lang = "zh-TW", Namespace = "Common" };

            access.SaveLanguage(resource);

            Assert.True(File.Exists(temp.Paths.GetLanguageFilePath("zh-TW", "Common")));
        }

        [Fact]
        [DisplayName("SaveLanguage null 輸入應拋 ArgumentNullException")]
        public void SaveLanguage_NullInput_ThrowsArgumentNullException()
        {
            using var temp = TempDir.Create();
            var access = CreateAccess(temp.Paths);

            Assert.Throws<ArgumentNullException>(() => access.SaveLanguage(null!));
        }
    }
}
