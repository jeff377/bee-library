using Bee.Api.AspNetCore;
using Bee.Base;
using Bee.Definition;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
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
    internal static class TestProcessBootstrap
    {
        private static readonly object _initLock = new();
        private static bool _initialized;

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
            var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
            var pathOptions = new PathOptions
            {
                DefinePath = Path.Combine(repoRoot, "tests", "Define")
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
            var settings = SystemSettingsLoader.Load(pathOptions);
            settings.BackendConfiguration.Components.BusinessObjectFactory = BackendDefaultTypes.BusinessObjectFactory;
            // CI 環境改用環境變數作為 MasterKey 來源，避免在 tests/Define/ 下建立 Master.key
            // 汙染 MasterKeyProviderTests.GetMasterKey_EmptyFilePath_UsesDefaultFileName 等
            // 預期「DefinePath 下無 Master.key」的測試。
            if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
            {
                settings.BackendConfiguration.SecurityKeySettings.MasterKeySource = new MasterKeySource
                {
                    Type = MasterKeySourceType.Environment,
                    Value = "BEE_TEST_FIXTURE_MASTER_KEY"
                };
            }
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
    }
}
