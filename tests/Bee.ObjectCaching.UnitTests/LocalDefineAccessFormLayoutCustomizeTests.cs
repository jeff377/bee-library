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
    /// <see cref="LocalDefineAccess.GetFormLayout(string, string)"/> 整檔擇一疊加測試：
    /// cust 檔存在→回 cust；否則回 base；custCode 空 / 無 reader→短路純 base（reader 零呼叫）。
    /// </summary>
    public sealed class LocalDefineAccessFormLayoutCustomizeTests : IDisposable
    {
        private readonly string _baseRoot;
        private const string LayoutId = "EmployeeDefault";

        public LocalDefineAccessFormLayoutCustomizeTests()
        {
            _baseRoot = Path.Combine(Path.GetTempPath(), $"bee-fl-cust-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_baseRoot);
        }

        public void Dispose()
        {
            try { Directory.Delete(_baseRoot, recursive: true); } catch (IOException) { /* best effort */ }
        }

        private LocalDefineAccess CreateAccess(ICustomizeDefineReader? reader)
        {
            var paths = new PathOptions { DefinePath = _baseRoot };
            // Write a base FormLayout so the base lookup returns a known instance.
            XmlCodec.SerializeToFile(new FormLayout { LayoutId = LayoutId }, paths.GetFormLayoutFilePath(LayoutId));
            var storage = new FileDefineStorage(paths);
            // Unique CachePrefix isolates these cache entries from other parallel tests.
            var cache = new CacheContainerService(storage, paths, Guid.NewGuid().ToString("N"));
            return new LocalDefineAccess(storage, paths, cache, Array.Empty<byte>(), reader);
        }

        [Fact]
        [DisplayName("cust 檔存在時應回傳 cust layout（整檔擇一）")]
        public void GetFormLayout_CustExists_ReturnsCust()
        {
            var custLayout = new FormLayout { LayoutId = LayoutId };
            var reader = new SpyCustomizeReader { FormLayout = custLayout };
            var access = CreateAccess(reader);

            var result = access.GetFormLayout("acme", LayoutId);

            Assert.Same(custLayout, result);
        }

        [Fact]
        [DisplayName("cust 檔不存在時應回傳 base layout")]
        public void GetFormLayout_CustMissing_ReturnsBase()
        {
            var reader = new SpyCustomizeReader { FormLayout = null };
            var access = CreateAccess(reader);

            var result = access.GetFormLayout("acme", LayoutId);

            Assert.NotNull(result);
            Assert.Equal(LayoutId, result.LayoutId);
        }

        [Fact]
        [DisplayName("custCode 空時短路純 base，reader 零呼叫")]
        public void GetFormLayout_EmptyCustCode_ShortCircuits_ReaderNotCalled()
        {
            var reader = new SpyCustomizeReader { FormLayout = new FormLayout { LayoutId = LayoutId } };
            var access = CreateAccess(reader);

            var result = access.GetFormLayout("", LayoutId);

            Assert.Equal(LayoutId, result.LayoutId);
            Assert.Equal(0, reader.GetCustomizeFormLayoutCallCount);
        }

        [Fact]
        [DisplayName("無 reader 注入時即使帶 custCode 也走純 base（向後相容）")]
        public void GetFormLayout_NoReader_BehavesAsBase()
        {
            var access = CreateAccess(reader: null);

            var result = access.GetFormLayout("acme", LayoutId);

            Assert.Equal(LayoutId, result.LayoutId);
        }

        private sealed class SpyCustomizeReader : ICustomizeDefineReader
        {
            public FormLayout? FormLayout { get; init; }
            public int GetCustomizeFormLayoutCallCount { get; private set; }

            public FormLayout? GetCustomizeFormLayout(string custCode, string layoutId)
            {
                GetCustomizeFormLayoutCallCount++;
                return FormLayout;
            }

            public LanguageResource? GetCustomizeLanguage(string custCode, string lang, string ns) => null;
            public ProgramSettings? GetCustomizeProgramSettings(string custCode) => null;
        }
    }
}
