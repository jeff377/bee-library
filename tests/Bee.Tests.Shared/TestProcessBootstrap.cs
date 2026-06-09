using Bee.Base;
using Bee.Definition;
using Bee.Definition.Storage;
using Bee.Hosting;
using Bee.ObjectCaching;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// Process-wide test bootstrap: 一次性 wire up <c>DbConnectionManager</c>、<c>SysInfo</c>、
    /// <see cref="Bee.Api.Client.ApiClientInfo.LocalServiceProvider"/>，以及
    /// <see cref="SharedDatabaseState.EnsureRegistered"/>。
    /// </summary>
    /// <remarks>
    /// PR 5.7 後 ICacheContainer / IDefineAccess 全面由 DI 容器接管，bootstrap 流程不再需要
    /// 預先初始化 <c>CacheContainer</c> 靜態 facade（已移除）。
    /// </remarks>
    public static class TestProcessBootstrap
    {
        private static readonly object _initLock = new();
        private static bool _initialized;
        private static string? _sharedDefinePath;

        /// <summary>
        /// Hard-coded Base64 AES-CBC-HMAC combined key (64 bytes) used by the test
        /// process when <c>BEE_MASTER_KEY</c> is not set in the environment. Kept
        /// independent from <c>DemoCredentials.DemoMasterKey</c> so test runs and
        /// sample runs cannot leak encrypted state into one another.
        /// </summary>
        private const string TestMasterKey =
            "oQGvs51A0u5Rn8RPJPkQ9xqXevf451mDHpsaJR7nN8WCM0X0zskVqTqDQBtSpSq8MdvmfKUPKAulOJShd9KDXg==";

        /// <summary>
        /// Process-wide shared define directory: <c>tests/Define</c> (test-specific
        /// fixtures) merged with <see cref="Bee.Definition.Defaults"/> (framework
        /// defaults embedded in <c>Bee.Definition.dll</c>). Created once on first
        /// <see cref="EnsureInitialized"/>; cleaned up on process exit.
        /// </summary>
        /// <remarks>
        /// Read-only by convention. Tests that need writable define storage opt in
        /// via <see cref="BeeTestFixtureBuilder.UseTempDefinePath"/>, which copies
        /// this directory into a per-class temp directory.
        /// </remarks>
        public static string SharedDefinePath
        {
            get
            {
                EnsureInitialized();
                return _sharedDefinePath!;
            }
        }

        /// <summary>
        /// 首次呼叫時觸發 process-wide 靜態 wire-up；後續呼叫直接 return。
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_initialized) return;
            lock (_initLock)
            {
                if (_initialized) return;
                InitializeOnce();
                _initialized = true;
            }
        }

        private static void InitializeOnce()
        {
            // 確保 BEE_MASTER_KEY 在任何測試 class 構造前完成設定：bootstrap 一開頭就 set，
            // 避免 SystemSettings 預設 MasterKeySource.Type=Environment 但 env var 未設時
            // MasterKeyProvider 拋例外。Production-like 環境會在外部 inject；此 fallback 只
            // 在 test process 內生效。
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BEE_MASTER_KEY")))
            {
                Environment.SetEnvironmentVariable("BEE_MASTER_KEY", TestMasterKey);
            }

            _sharedDefinePath = CreateSharedDefinePath();
            var pathOptions = new PathOptions
            {
                DefinePath = _sharedDefinePath
            };

            // Bootstrap 暫時用一個 DefineAccess 讓 SharedDatabaseState.EnsureRegistered
            // 在 AddBeeFramework 執行前就能寫入 DatabaseSettings.Items。
            // 這個 bootstrap access 只活在 InitializeOnce scope 內，不對外公開；DI 容器內由
            // AddBeeFramework 重新建立正式的 IDefineAccess 實例（用同一份 PathOptions 即可共用 cache）。
            var bootstrapStorage = new FileDefineStorage(pathOptions);
            var bootstrapAccess = new LocalDefineAccess(bootstrapStorage, pathOptions);

            // DB provider / dialect / DatabaseItem 註冊統一交給 SharedDatabaseState。
            SharedDatabaseState.EnsureRegistered(bootstrapAccess);

            // 系統初始化：boot-time 讀檔走 SystemSettingsLoader（不依賴 IDefineAccess）。
            // tests/Define/SystemSettings.xml 已將 MasterKeySource.Type 設為 Environment、
            // Value 設為 BEE_MASTER_KEY，配合本方法開頭的 env var 注入即可解密 payload。
            var settings = SystemSettingsLoader.Load(pathOptions);
            settings.BackendConfiguration.Components.BusinessObjectFactory = BackendDefaultTypes.BusinessObjectFactory;
            SysInfo.Initialize(settings.CommonConfiguration);

            // 用 AddBeeFramework 建 DI 容器。Phase 7 後框架不再有 process-wide 靜態 facade，
            // 所有服務（含 IDbConnectionManager）皆透過 ctor 注入解析。
            var services = new ServiceCollection();
            services.AddBeeFramework(settings.BackendConfiguration, pathOptions, autoCreateMasterKey: true);
            var provider = services.BuildServiceProvider();

            // Bee.Api.Client 近端模式（in-process）透過 ApiClientInfo.LocalServiceProvider 取得後端服務；
            // 測試 fixture 預設指向同一個 process-wide 容器。Phase 4 transitional —
            // 主計畫 §「範圍邊界」說明此 holder 是 Bee.Api.Client 重構前的暫時做法。
            Bee.Api.Client.ApiClientInfo.LocalServiceProvider = provider;
        }

        private static string FindRepoRoot(string startDir)
        {
            var dir = new DirectoryInfo(startDir);
            while (dir != null)
            {
                if (dir.GetDirectories(".git").Length > 0)
                    return dir.FullName;
                dir = dir.Parent;
            }
            throw new InvalidOperationException($"Cannot find repo root from: {startDir}");
        }

        /// <summary>
        /// Creates the process-wide shared define directory: a temp dir populated
        /// first with the contents of <c>tests/Define</c> (test-specific fixtures
        /// win on conflict), then with the framework defaults from
        /// <see cref="Defaults.MaterializeTo"/> (skip-existing). Registers a
        /// process-exit cleanup hook.
        /// </summary>
        private static string CreateSharedDefinePath()
        {
            var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
            var testsDefine = Path.Combine(repoRoot, "tests", "Define");

            var sharedDir = Path.Combine(
                Path.GetTempPath(),
                $"bee-tests-define-{Environment.ProcessId}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(sharedDir);

            // Step 1: copy tests/Define first. Test-specific fixtures (DbCategorySettings
            // with ft_project, Project FormSchema/Layout/Language, PermGateForm,
            // SystemSettings, DatabaseSettings) win on conflict so they shadow any
            // framework default with the same relative path.
            CopyDirectory(testsDefine, sharedDir);

            // Step 2: materialise framework defaults from Bee.Definition.dll's embedded
            // resources. Overwrite=false so step 1's test-specific files (e.g. the
            // extended DbCategorySettings.xml) are preserved.
            Defaults.MaterializeTo(sharedDir, MaterializeOptions.Default);

            // Best-effort cleanup on process exit. xunit's parallel runners do not
            // share processes, so each runner cleans its own dir.
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                try { Directory.Delete(sharedDir, recursive: true); }
                catch (IOException) { /* best effort */ }
                catch (UnauthorizedAccessException) { /* best effort */ }
            };

            return sharedDir;
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
