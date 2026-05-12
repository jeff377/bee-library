using System.ComponentModel;
using Bee.Definition.Storage;
using Bee.ObjectCaching;
using Bee.Tests.Shared;

namespace Bee.Definition.UnitTests
{
    /// <summary>
    /// Smoke tests for <see cref="BeeTestFixture"/>. Verifies the per-class fixture builds
    /// its own <see cref="IServiceProvider"/>, resolves framework services, and (for
    /// <c>UseTempDefinePath</c>) provides a writable per-fixture <see cref="PathOptions"/>.
    /// </summary>
    public class BeeTestFixtureSmokeTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public BeeTestFixtureSmokeTests(BeeTestFixture fx)
        {
            _fx = fx;
        }

        [Fact]
        [DisplayName("BeeTestFixture 預設應指向共享 tests/Define 路徑")]
        public void DefaultFixture_PointsToSharedDefine()
        {
            Assert.NotNull(_fx.PathOptions);
            Assert.False(string.IsNullOrEmpty(_fx.DefinePath));
            Assert.Contains(Path.Combine("tests", "Define"), _fx.DefinePath);
            Assert.True(File.Exists(_fx.PathOptions.GetSystemSettingsFilePath()));
        }

        [Fact]
        [DisplayName("BeeTestFixture.GetRequiredService 應可解析 IDefineAccess")]
        public void GetRequiredService_IDefineAccess_Succeeds()
        {
            var access = _fx.GetRequiredService<IDefineAccess>();
            Assert.NotNull(access);
            // Smoke: 透過 fixture 的 IDefineAccess 讀 SystemSettings 應有 BackendConfiguration
            var settings = access.GetSystemSettings();
            Assert.NotNull(settings.BackendConfiguration);
        }

        [Fact]
        [DisplayName("BeeTestFixture.GetRequiredService 應可解析 PathOptions singleton")]
        public void GetRequiredService_PathOptions_MatchesFixture()
        {
            var paths = _fx.GetRequiredService<PathOptions>();
            Assert.Same(_fx.PathOptions, paths);
        }

        [Fact]
        [DisplayName("BeeTestFixture.GetRequiredService 應可解析 ICacheContainer")]
        public void GetRequiredService_ICacheContainer_Succeeds()
        {
            var cache = _fx.GetRequiredService<ICacheContainer>();
            Assert.NotNull(cache);
            Assert.NotNull(cache.SystemSettings);
            Assert.NotNull(cache.SessionInfo);
        }
    }

    /// <summary>
    /// Verifies <see cref="BeeTestFixtureBuilder.UseTempDefinePath"/> creates an isolated
    /// per-fixture <see cref="PathOptions"/> with a seeded copy of <c>tests/Define</c>.
    /// </summary>
    public class BeeTestFixtureTempDefineSmokeTests : IClassFixture<BeeTestFixtureTempDefineSmokeTests.WritableFixture>
    {
        private readonly WritableFixture _fx;

        public BeeTestFixtureTempDefineSmokeTests(WritableFixture fx)
        {
            _fx = fx;
        }

        public sealed class WritableFixture : BeeTestFixture
        {
            public WritableFixture() : base(b => b.UseTempDefinePath()) { }
        }

        [Fact]
        [DisplayName("UseTempDefinePath 應給每個 fixture 獨立 temp 目錄")]
        public void TempDefine_IsolatedDirectory()
        {
            Assert.NotEqual(string.Empty, _fx.DefinePath);
            Assert.True(Directory.Exists(_fx.DefinePath));
            Assert.DoesNotContain(Path.Combine("tests", "Define"), _fx.DefinePath);
            // Seeded copy 應包含 SystemSettings.xml
            Assert.True(File.Exists(_fx.PathOptions.GetSystemSettingsFilePath()));
        }

        [Fact]
        [DisplayName("UseTempDefinePath fixture 的 IDefineAccess 應可從 DI 解析")]
        public void TempDefine_AccessResolvable()
        {
            // 註：CacheContainer 仍為 process-wide static shim（PR 5.4 後續清理），
            // 所以 IDefineAccess.GetSystemSettings() 走 cache 時仍回傳共享 fixture
            // 的 instance。本測試只驗證 IDefineAccess 與 PathOptions 可從 DI 解析；
            // per-fixture 寫入路徑驗證由 PR 5.4 後續 cache 層解耦後重新引入。
            var access = _fx.GetRequiredService<IDefineAccess>();
            var paths = _fx.GetRequiredService<PathOptions>();
            Assert.NotNull(access);
            Assert.Equal(_fx.DefinePath, paths.DefinePath);
        }
    }
}
