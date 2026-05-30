using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Bee.ObjectCaching;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// 驗證 AddBeeFramework 對租戶客製化覆蓋層的接線（階段 4）：
    /// provider / reader 可解析；三個消費端注入 reader 後仍可解析（無循環依賴）；
    /// CustomizePath 未設→純 base；CustomizePath 設定→經 DI 的 overlay 端到端生效。
    /// </summary>
    public sealed class CustomizationWiringTests : IDisposable
    {
        private readonly string _defineDir;
        private readonly string _customizeDir;

        public CustomizationWiringTests()
        {
            _defineDir = Path.Combine(Path.GetTempPath(), $"bee-wire-def-{Guid.NewGuid():N}");
            _customizeDir = Path.Combine(Path.GetTempPath(), $"bee-wire-cust-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_defineDir);
            Directory.CreateDirectory(_customizeDir);
        }

        public void Dispose()
        {
            foreach (var dir in new[] { _defineDir, _customizeDir })
            {
                try { Directory.Delete(dir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }

        private ServiceProvider BuildProvider(string customizePath)
        {
            var services = new ServiceCollection();
            services.AddBeeFramework(
                new BackendConfiguration(),
                new PathOptions { DefinePath = _defineDir, CustomizePath = customizePath },
                autoCreateMasterKey: true);
            return services.BuildServiceProvider();
        }

        [Fact]
        [DisplayName("AddBeeFramework 應註冊並解析 ICacheContainerProvider 與 ICustomizeDefineReader")]
        public void AddBeeFramework_ResolvesProviderAndReader()
        {
            using var sp = BuildProvider(_customizeDir);

            Assert.NotNull(sp.GetRequiredService<ICacheContainerProvider>());
            Assert.NotNull(sp.GetRequiredService<ICustomizeDefineReader>());
        }

        [Fact]
        [DisplayName("注入 reader 後三個消費端仍可解析（證明注入鏈無循環依賴）")]
        public void AddBeeFramework_ConsumersWithReader_Resolve()
        {
            using var sp = BuildProvider(_customizeDir);

            Assert.NotNull(sp.GetRequiredService<Bee.Definition.Language.ILanguageService>());
            Assert.NotNull(sp.GetRequiredService<Bee.Business.IFormBoTypeResolver>());
            Assert.NotNull(sp.GetRequiredService<IDefineAccess>());
        }

        [Fact]
        [DisplayName("CustomizePath 未設時 reader 三類皆回 null（退化純 base）")]
        public void AddBeeFramework_EmptyCustomizePath_ReaderReturnsNull()
        {
            using var sp = BuildProvider(customizePath: string.Empty);
            var reader = sp.GetRequiredService<ICustomizeDefineReader>();

            Assert.Null(reader.GetCustomizeFormLayout("acme", "EmployeeDefault"));
            Assert.Null(reader.GetCustomizeLanguage("acme", "zh-TW", "Common"));
            Assert.Null(reader.GetCustomizeProgramSettings("acme"));
        }

        [Fact]
        [DisplayName("CustomizePath 設定時 IDefineAccess.GetFormLayout 經 DI 注入的 reader 端到端回傳客製 layout")]
        public void AddBeeFramework_CustomizePathSet_FormLayoutOverlayWorksEndToEnd()
        {
            const string customizeId = "acme";
            const string layoutId = "EmployeeDefault";
            // 寫一份客製 FormLayout 到 {CustomizePath}/{customizeId}/FormLayout/...
            var custPaths = new CustomizeOnlyPathOptions(_customizeDir, customizeId);
            XmlCodec.SerializeToFile(new FormLayout { LayoutId = layoutId }, custPaths.GetFormLayoutFilePath(layoutId));

            using var sp = BuildProvider(_customizeDir);
            var access = sp.GetRequiredService<IDefineAccess>();

            // 整檔擇一：custCode 非空且客製檔存在 → 回客製 layout（不碰 base）。
            var result = access.GetFormLayout(customizeId, layoutId);

            Assert.NotNull(result);
            Assert.Equal(layoutId, result.LayoutId);
        }
    }
}
