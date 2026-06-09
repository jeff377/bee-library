using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// Builder for <see cref="BeeTestFixture"/>. Customises the <see cref="PathOptions"/>
    /// resolution and the underlying <see cref="BackendConfiguration"/> before the per-fixture
    /// <see cref="IServiceProvider"/> is built.
    /// </summary>
    public sealed class BeeTestFixtureBuilder
    {
        private bool _useTempDefinePath;
        private bool _useSharedDatabases;
        private Action<BackendConfiguration>? _configureBackend;

        /// <summary>
        /// Redirects the fixture's <see cref="PathOptions.DefinePath"/> to a per-fixture
        /// temporary directory; the directory is seeded with a copy of the shared
        /// <c>tests/Define</c> fixture (so <c>SystemSettings.xml</c> etc. resolve correctly)
        /// and deleted when the fixture is disposed.
        /// </summary>
        public BeeTestFixtureBuilder UseTempDefinePath()
        {
            _useTempDefinePath = true;
            return this;
        }

        /// <summary>
        /// Opts the fixture into the process-wide shared database setup: registers
        /// ADO.NET providers + dialect factories per <c>DatabaseType</c>, seeds the
        /// matching <c>DatabaseItem</c> entries (when <c>BEE_TEST_CONNSTR_*</c> env
        /// vars are set), and creates/upgrades the shared <c>st_user</c>/<c>st_session</c>
        /// schemas plus seed user. Idempotent across the process — use for fixtures
        /// driving <c>[DbFact]</c> integration tests.
        /// </summary>
        public BeeTestFixtureBuilder UseSharedDatabases()
        {
            _useSharedDatabases = true;
            return this;
        }

        /// <summary>
        /// Applies a callback to the loaded <see cref="BackendConfiguration"/> before the
        /// service provider is built. Useful for switching encryption-key sources, swapping
        /// component types, etc.
        /// </summary>
        /// <param name="configure">The configuration callback.</param>
        public BeeTestFixtureBuilder ConfigureBackend(Action<BackendConfiguration> configure)
        {
            _configureBackend = configure ?? throw new ArgumentNullException(nameof(configure));
            return this;
        }

        internal PathOptions BuildPathOptions(out string? tempDir)
        {
            // SharedDefinePath: process-wide merged dir (tests/Define + framework
            // defaults materialised from Bee.Definition.Defaults). Built once by
            // TestProcessBootstrap.EnsureInitialized() before any fixture ctor runs.
            var sharedDefine = TestProcessBootstrap.SharedDefinePath;

            if (!_useTempDefinePath)
            {
                tempDir = null;
                return new PathOptions { DefinePath = sharedDefine };
            }

            tempDir = Path.Combine(Path.GetTempPath(), $"bee-fixture-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            CopyDirectory(sharedDefine, tempDir);
            return new PathOptions { DefinePath = tempDir };
        }

        internal ServiceProvider BuildServiceProvider(PathOptions paths)
        {
            var settings = SystemSettingsLoader.Load(paths);
            settings.BackendConfiguration.Components.BusinessObjectFactory = BackendDefaultTypes.BusinessObjectFactory;

            // tests/Define/SystemSettings.xml 預設 MasterKeySource.Type=Environment、
            // Value=BEE_MASTER_KEY。TestProcessBootstrap 已於 process 啟動時為缺值的
            // BEE_MASTER_KEY 注入 hardcoded TestMasterKey,所以 fixture 不需要再額外
            // 覆寫 MasterKeySource。

            _configureBackend?.Invoke(settings.BackendConfiguration);

            var services = new ServiceCollection();
            services.AddBeeFramework(settings.BackendConfiguration, paths, autoCreateMasterKey: true);

            // Cache key prefix 設計（PR 5.4d 引入）已於 PR 5.7 撤除：cache 改為 ctor 注入
            // PathOptions 後，bootstrap 與 fixture 的 ICacheContainer instance 不同，但底層
            // CacheInfo.Provider 仍共享；保留 prefix 會讓 SharedDatabaseState 對 bootstrap
            // 的 DatabaseSettings.Items mutation 在 fixture-prefixed cache 看不到。
            // session-isolation 需求由 production code 的 Guid AccessToken 隨機性自然保證。

            var provider = services.BuildServiceProvider();

            if (_useSharedDatabases)
            {
                // Schema + seed are process-wide (idempotent); resolved IDefineAccess
                // shares the same DatabaseSettings cache that SharedDatabaseState
                // populated via GlobalFixture's bootstrap path.
                SharedDatabaseState.EnsureSchemaAndSeed(
                    provider.GetRequiredService<Bee.Definition.Storage.IDefineAccess>(),
                    provider.GetRequiredService<IDbConnectionManager>());
            }

            return provider;
        }

        private static void CopyDirectory(string source, string dest)
        {
            foreach (var subdir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(source, subdir);
                Directory.CreateDirectory(Path.Combine(dest, rel));
            }
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(source, file);
                File.Copy(file, Path.Combine(dest, rel), overwrite: true);
            }
        }
    }
}
