using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// <see cref="CustomizeDefineReader"/> 行為測試：客製檔存在→回該檔；不存在→null；
    /// CustomizePath 未設 / customizeId 空→全回 null（第二道防線）；跨租戶隔離。
    /// </summary>
    public sealed class CustomizeDefineReaderTests : IDisposable
    {
        private readonly string _root;
        // Unique per-test customization codes keep the prefixed cache entries isolated across
        // the shared process-wide cache provider, so this class runs in parallel with others.
        private readonly string _customizeId = "cust" + Guid.NewGuid().ToString("N");

        public CustomizeDefineReaderTests()
        {
            _root = Path.Combine(Path.GetTempPath(), $"bee-custreader-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            try { Directory.Delete(_root, recursive: true); } catch (IOException) { /* best effort */ }
        }

        private CustomizeDefineReader CreateReader(string? customizePath = null)
        {
            var paths = new PathOptions { DefinePath = "/tmp/base", CustomizePath = customizePath ?? _root };
            return new CustomizeDefineReader(new CacheContainerProvider(paths), paths);
        }

        private void WriteCustomizeFormLayout(string customizeId, string layoutId)
        {
            var paths = new CustomizeOnlyPathOptions(_root, customizeId);
            XmlCodec.SerializeToFile(new FormLayout { LayoutId = layoutId }, paths.GetFormLayoutFilePath(layoutId));
        }

        private void WriteCustomizeLanguage(string customizeId, string lang, string ns)
        {
            var paths = new CustomizeOnlyPathOptions(_root, customizeId);
            XmlCodec.SerializeToFile(new LanguageResource { Lang = lang, Namespace = ns }, paths.GetLanguageFilePath(lang, ns));
        }

        private void WriteCustomizeProgramSettings(string customizeId)
        {
            var paths = new CustomizeOnlyPathOptions(_root, customizeId);
            XmlCodec.SerializeToFile(new ProgramSettings(), paths.GetProgramSettingsFilePath());
        }

        [Fact]
        [DisplayName("GetCustomizeFormLayout 客製檔存在時應回傳客製物件")]
        public void GetCustomizeFormLayout_FileExists_ReturnsCustomize()
        {
            WriteCustomizeFormLayout(_customizeId, "EmployeeDefault");
            var reader = CreateReader();

            var result = reader.GetCustomizeFormLayout(_customizeId, "EmployeeDefault");

            Assert.NotNull(result);
            Assert.Equal("EmployeeDefault", result!.LayoutId);
        }

        [Fact]
        [DisplayName("GetCustomizeFormLayout 客製檔不存在時應回傳 null")]
        public void GetCustomizeFormLayout_FileMissing_ReturnsNull()
        {
            Assert.Null(CreateReader().GetCustomizeFormLayout(_customizeId, "NonExistent"));
        }

        [Fact]
        [DisplayName("GetCustomizeLanguage 客製檔存在時應回傳客製物件")]
        public void GetCustomizeLanguage_FileExists_ReturnsCustomize()
        {
            WriteCustomizeLanguage(_customizeId, "zh-TW", "Customer");
            var reader = CreateReader();

            var result = reader.GetCustomizeLanguage(_customizeId, "zh-TW", "Customer");

            Assert.NotNull(result);
            Assert.Equal("Customer", result!.Namespace);
        }

        [Fact]
        [DisplayName("GetCustomizeLanguage 客製檔不存在時應回傳 null")]
        public void GetCustomizeLanguage_FileMissing_ReturnsNull()
        {
            Assert.Null(CreateReader().GetCustomizeLanguage(_customizeId, "zh-TW", "NonExistent"));
        }

        [Fact]
        [DisplayName("GetCustomizeProgramSettings 客製檔存在時應回傳客製物件")]
        public void GetCustomizeProgramSettings_FileExists_ReturnsCustomize()
        {
            WriteCustomizeProgramSettings(_customizeId);
            var reader = CreateReader();

            Assert.NotNull(reader.GetCustomizeProgramSettings(_customizeId));
        }

        [Fact]
        [DisplayName("GetCustomizeProgramSettings 客製檔不存在時應回傳 null（不丟例外）")]
        public void GetCustomizeProgramSettings_FileMissing_ReturnsNull()
        {
            Assert.Null(CreateReader().GetCustomizeProgramSettings(_customizeId));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [DisplayName("customizeId 為空時三類皆回 null（短路第二道防線）")]
        public void EmptyCustomizeId_AllReturnNull(string? customizeId)
        {
            WriteCustomizeFormLayout(_customizeId, "EmployeeDefault");
            var reader = CreateReader();

            Assert.Null(reader.GetCustomizeFormLayout(customizeId!, "EmployeeDefault"));
            Assert.Null(reader.GetCustomizeLanguage(customizeId!, "zh-TW", "Customer"));
            Assert.Null(reader.GetCustomizeProgramSettings(customizeId!));
        }

        [Fact]
        [DisplayName("CustomizePath 未設時三類皆回 null（關閉客製、向後相容）")]
        public void EmptyCustomizePath_AllReturnNull()
        {
            WriteCustomizeFormLayout(_customizeId, "EmployeeDefault");
            var reader = CreateReader(customizePath: "");

            Assert.Null(reader.GetCustomizeFormLayout(_customizeId, "EmployeeDefault"));
            Assert.Null(reader.GetCustomizeLanguage(_customizeId, "zh-TW", "Customer"));
            Assert.Null(reader.GetCustomizeProgramSettings(_customizeId));
        }

        [Fact]
        [DisplayName("跨租戶隔離：A 的客製不影響 B 的查找結果")]
        public void CrossTenant_Isolated()
        {
            string custA = "a" + Guid.NewGuid().ToString("N");
            string custB = "b" + Guid.NewGuid().ToString("N");
            WriteCustomizeFormLayout(custA, "EmployeeDefault");
            var reader = CreateReader();

            Assert.NotNull(reader.GetCustomizeFormLayout(custA, "EmployeeDefault"));
            // B 沒有任何客製檔，必須回 null —— A 的客製不得外溢到 B。
            Assert.Null(reader.GetCustomizeFormLayout(custB, "EmployeeDefault"));
        }

        [Fact]
        [DisplayName("連續查找回傳穩定的快取實例 reference（證明未每次重建）")]
        public void RepeatedLookup_ReturnsStableCachedInstance()
        {
            WriteCustomizeFormLayout(_customizeId, "EmployeeDefault");
            var reader = CreateReader();

            var first = reader.GetCustomizeFormLayout(_customizeId, "EmployeeDefault");
            var second = reader.GetCustomizeFormLayout(_customizeId, "EmployeeDefault");

            Assert.Same(first, second);
        }

        [Fact]
        [DisplayName("建構子傳入 null provider 應拋出 ArgumentNullException")]
        public void Constructor_NullProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CustomizeDefineReader(null!, new PathOptions()));
        }
    }
}
