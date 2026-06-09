using System.ComponentModel;
using Bee.Definition.Identity;
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
        [DisplayName("BeeTestFixture 預設應指向 TestProcessBootstrap 的 process-wide shared define path")]
        public void DefaultFixture_PointsToSharedDefine()
        {
            Assert.NotNull(_fx.PathOptions);
            Assert.False(string.IsNullOrEmpty(_fx.DefinePath));
            // Post-migration（framework defaults 搬到 src/Bee.Definition/Defaults/ 後）：
            // 預設 fixture 指向 TestProcessBootstrap.SharedDefinePath（process-wide temp
            // 目錄，內容為 tests/Define + 從 Bee.Definition.dll embedded 物化的框架預設）。
            Assert.Equal(TestProcessBootstrap.SharedDefinePath, _fx.DefinePath);
            Assert.True(File.Exists(_fx.PathOptions.GetSystemSettingsFilePath()));
            // 合併後該路徑同時可以解析 framework 自有檔（如 st_user.TableSchema.xml）
            // 與 tests 自有檔（如 ft_project.TableSchema.xml）。
            Assert.True(File.Exists(Path.Combine(_fx.DefinePath, "TableSchema", "common", "st_user.TableSchema.xml")));
            Assert.True(File.Exists(Path.Combine(_fx.DefinePath, "TableSchema", "company", "ft_project.TableSchema.xml")));
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

    /// <summary>
    /// PR 5.7 後 BeeTestFixture 不再為 ICacheContainer 套用 per-fixture prefix
    /// （cache 改 ctor 注入 PathOptions 後 prefix 會與 SharedDatabaseState 的 bootstrap
    /// 路徑撞 key 導致 DatabaseSettings.Items 不一致）；session 隔離由 production code
    /// 的 Guid AccessToken 隨機性自然保證。本測試組驗證每個 fixture 仍持有獨立
    /// IServiceProvider 與 service instance。
    /// </summary>
    public class BeeTestFixturePerInstanceIsolationTests
    {
        [Fact]
        [DisplayName("兩個 BeeTestFixture 的 ISessionInfoService 應為獨立 instance")]
        public void TwoFixtures_HaveIndependentSessionServices()
        {
            using var fxA = new BeeTestFixture();
            using var fxB = new BeeTestFixture();

            var svcA = fxA.GetRequiredService<ISessionInfoService>();
            var svcB = fxB.GetRequiredService<ISessionInfoService>();

            Assert.NotSame(svcA, svcB);
        }

        [Fact]
        [DisplayName("兩個 BeeTestFixture 的 ICacheContainer 應為獨立 instance")]
        public void TwoFixtures_HaveIndependentCacheContainers()
        {
            using var fxA = new BeeTestFixture();
            using var fxB = new BeeTestFixture();

            var cacheA = fxA.GetRequiredService<ICacheContainer>();
            var cacheB = fxB.GetRequiredService<ICacheContainer>();

            Assert.NotSame(cacheA, cacheB);
            Assert.NotSame(cacheA.SessionInfo, cacheB.SessionInfo);
        }
    }
}
