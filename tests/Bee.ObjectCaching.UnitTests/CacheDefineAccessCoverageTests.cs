using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// <see cref="CacheDefineAccess"/> 補漏覆蓋：建構子 null 守衛、CurrencySettings /
    /// UnitSettings 的 Get / Save / GetDefine / SaveDefine dispatch 路徑。
    /// 各測試以獨立 TempDir + 唯一 cache prefix 隔離，可平行執行。
    /// </summary>
    public sealed class CacheDefineAccessCoverageTests
    {
        private sealed class TempDir : IDisposable
        {
            public PathOptions Options { get; }
            private readonly string _path;

            public TempDir()
            {
                _path = Path.Combine(Path.GetTempPath(), $"bee-cov-{Guid.NewGuid():N}");
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
            var cache = new CacheContainerService(storage, paths, "cov_" + Guid.NewGuid().ToString("N"));
            return new CacheDefineAccess(storage, paths, cache, Array.Empty<byte>());
        }

        // ── 建構子 null 守衛 ──────────────────────────────────────────────────

        [Fact]
        [DisplayName("建構子 storage 為 null 應拋 ArgumentNullException")]
        public void Ctor_NullStorage_ThrowsArgumentNullException()
        {
            using var temp = new TempDir();
            var storage = new FileDefineStorage(temp.Options);
            var cache = new CacheContainerService(storage, temp.Options, "cov_" + Guid.NewGuid().ToString("N"));

            Assert.Throws<ArgumentNullException>(() =>
                new CacheDefineAccess(null!, temp.Options, cache, Array.Empty<byte>()));
        }

        [Fact]
        [DisplayName("建構子 paths 為 null 應拋 ArgumentNullException")]
        public void Ctor_NullPaths_ThrowsArgumentNullException()
        {
            using var temp = new TempDir();
            var storage = new FileDefineStorage(temp.Options);
            var cache = new CacheContainerService(storage, temp.Options, "cov_" + Guid.NewGuid().ToString("N"));

            Assert.Throws<ArgumentNullException>(() =>
                new CacheDefineAccess(storage, null!, cache, Array.Empty<byte>()));
        }

        [Fact]
        [DisplayName("建構子 cache 為 null 應拋 ArgumentNullException")]
        public void Ctor_NullCache_ThrowsArgumentNullException()
        {
            using var temp = new TempDir();
            var storage = new FileDefineStorage(temp.Options);

            Assert.Throws<ArgumentNullException>(() =>
                new CacheDefineAccess(storage, temp.Options, null!, Array.Empty<byte>()));
        }

        [Fact]
        [DisplayName("建構子 configEncryptionKey 為 null 應以空陣列容錯不拋例外")]
        public void Ctor_NullConfigEncryptionKey_DoesNotThrow()
        {
            using var temp = new TempDir();
            var storage = new FileDefineStorage(temp.Options);
            var cache = new CacheContainerService(storage, temp.Options, "cov_" + Guid.NewGuid().ToString("N"));

            var exception = Record.Exception(() =>
                new CacheDefineAccess(storage, temp.Options, cache, null!));

            Assert.Null(exception);
        }

        // ── CurrencySettings ──────────────────────────────────────────────────

        [Fact]
        [DisplayName("SaveCurrencySettings 應透過 DefineStorage 寫入 CurrencySettings.xml")]
        public void SaveCurrencySettings_WritesFile()
        {
            using var temp = new TempDir();
            var access = CreateAccess(temp.Options);

            access.SaveCurrencySettings([]);

            Assert.True(File.Exists(temp.Options.GetCurrencySettingsFilePath()));
        }

        [Fact]
        [DisplayName("GetCurrencySettings 先存後取應回傳 CurrencySettings 實例")]
        public void GetCurrencySettings_AfterSave_ReturnsInstance()
        {
            using var temp = new TempDir();
            var access = CreateAccess(temp.Options);
            access.SaveCurrencySettings([]);

            var result = access.GetCurrencySettings();

            Assert.NotNull(result);
        }

        [Fact]
        [DisplayName("GetDefine(CurrencySettings) 應委派至 GetCurrencySettings 並回傳 CurrencySettings")]
        public void GetDefine_CurrencySettings_ReturnsCurrencySettings()
        {
            using var temp = new TempDir();
            var access = CreateAccess(temp.Options);
            access.SaveCurrencySettings([]);

            var result = access.GetDefine(DefineType.CurrencySettings);

            Assert.IsType<CurrencySettings>(result);
        }

        [Fact]
        [DisplayName("SaveDefine(CurrencySettings) 應委派至 SaveCurrencySettings 並寫入檔案")]
        public void SaveDefine_CurrencySettings_DelegatesToSaveCurrencySettings()
        {
            using var temp = new TempDir();
            var access = CreateAccess(temp.Options);

            access.SaveDefine(DefineType.CurrencySettings, new CurrencySettings());

            Assert.True(File.Exists(temp.Options.GetCurrencySettingsFilePath()));
        }

        // ── UnitSettings ──────────────────────────────────────────────────────

        [Fact]
        [DisplayName("SaveUnitSettings 應透過 DefineStorage 寫入 UnitSettings.xml")]
        public void SaveUnitSettings_WritesFile()
        {
            using var temp = new TempDir();
            var access = CreateAccess(temp.Options);

            access.SaveUnitSettings([]);

            Assert.True(File.Exists(temp.Options.GetUnitSettingsFilePath()));
        }

        [Fact]
        [DisplayName("GetUnitSettings 先存後取應回傳 UnitSettings 實例")]
        public void GetUnitSettings_AfterSave_ReturnsInstance()
        {
            using var temp = new TempDir();
            var access = CreateAccess(temp.Options);
            access.SaveUnitSettings([]);

            var result = access.GetUnitSettings();

            Assert.NotNull(result);
        }

        [Fact]
        [DisplayName("GetDefine(UnitSettings) 應委派至 GetUnitSettings 並回傳 UnitSettings")]
        public void GetDefine_UnitSettings_ReturnsUnitSettings()
        {
            using var temp = new TempDir();
            var access = CreateAccess(temp.Options);
            access.SaveUnitSettings([]);

            var result = access.GetDefine(DefineType.UnitSettings);

            Assert.IsType<UnitSettings>(result);
        }

        [Fact]
        [DisplayName("SaveDefine(UnitSettings) 應委派至 SaveUnitSettings 並寫入檔案")]
        public void SaveDefine_UnitSettings_DelegatesToSaveUnitSettings()
        {
            using var temp = new TempDir();
            var access = CreateAccess(temp.Options);

            access.SaveDefine(DefineType.UnitSettings, new UnitSettings());

            Assert.True(File.Exists(temp.Options.GetUnitSettingsFilePath()));
        }
    }
}
