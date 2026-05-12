using Bee.Api.AspNetCore;
using Bee.Base;
using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Definition.Security;
using Bee.Definition.Storage;
using Bee.ObjectCaching;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// Process-wide test bootstrap. Initialises shared statics
    /// (<see cref="DefinePathInfo"/>, <c>CacheContainer</c>, <c>DbConnectionManager</c>,
    /// <c>SysInfo</c>, DB provider registry) once per process, then builds and exposes
    /// the legacy shared <see cref="IServiceProvider"/> via <see cref="BeeTestServices"/>.
    /// </summary>
    /// <remarks>
    /// Phase 5 PR 5.4b extracted DB provider/dialect/item registration into
    /// <see cref="SharedDatabaseState"/>; <see cref="BeeTestFixture"/> shares the same
    /// helper. <see cref="GlobalFixture"/> itself remains as the entrypoint that legacy
    /// <c>[Collection("Initialize")]</c> tests bind to until PR 5.4d retires it.
    /// </remarks>
    public class GlobalFixture : IDisposable
    {
        // VS Code Test Explorer 走 single-host 模式時,9 個 test collection 各自會新建一個
        // GlobalFixture/DbGlobalFixture instance,並行進入 ctor。BeeTestServices.Provider /
        // DbProviderRegistry 等都是 process-wide static,並行 init 會造成 KeyedCollection 重複 Add 等
        // 問題,連帶讓 [Collection("Initialize")] 測試大量失敗。
        // terminal 的 dotnet test 對每個 assembly 開獨立 process,沒有此 race。
        // 用 lock + once flag 確保整個 process 內只執行一次 init,後續 fixture instance 直接 return。
        private static readonly object _initLock = new();
        private static bool _initialized;

        /// <summary>
        /// 建構函式,於第一次進入 Collection 測試時執行初始化。
        /// 在 single-host 模式下,後續 fixture instance 會 short-circuit 直接 return。
        /// </summary>
        public GlobalFixture()
        {
            lock (_initLock)
            {
                if (_initialized) return;
                InitializeOnce();
                _initialized = true;
            }
        }

        private static void InitializeOnce()
        {
            // 全域初始化邏輯,例如載入設定檔、建立資料庫、啟動 API
            // 設定定義路徑（相對於測試輸出目錄往上找 tests/Bee.Tests.Shared/Define）
            var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
            var pathOptions = new PathOptions
            {
                DefinePath = Path.Combine(repoRoot, "tests", "Define")
            };
            // DefinePathInfo 仍由 cache 層讀取 (Phase 5 PR 5.3 才會解耦)；同步設定避免 race。
            DefinePathInfo.Initialize(pathOptions);

            // Bootstrap 暫時用一個 DefineAccess 讓 SharedDatabaseState.EnsureRegistered
            // 在 AddBeeFramework 執行前就能寫入 DatabaseSettings.Items；CacheContainer 必須先初始化
            // 才能讓 LocalDefineAccess 透過快取讀寫 DatabaseSettings。
            // ⚠ 這個 bootstrap access 只活在 InitializeOnce scope 內，不對外公開；DI 容器內由
            // AddBeeFramework 重新建立正式的 IDefineAccess 實例。
            var bootstrapStorage = new FileDefineStorage(pathOptions);
            CacheContainer.Initialize(bootstrapStorage);
            var bootstrapAccess = new LocalDefineAccess(bootstrapStorage, pathOptions);

            // DB provider / dialect / DatabaseItem 註冊統一交給 SharedDatabaseState（PR 5.4b 抽出）。
            // 必須先於 AddBeeFramework：startup 過程的 DatabaseSettings 驗證會檢查 Id='common' 存在。
            SharedDatabaseState.EnsureRegistered(bootstrapAccess);

            // 系統初始化：boot-time 讀檔走 SystemSettingsLoader（不依賴 IDefineAccess）。
            // 與 runtime cache-backed 路徑分工，詳見 plan-backendinfo-di-phase0-systemsettings-loader.md。
            var settings = SystemSettingsLoader.Load(pathOptions);
            settings.BackendConfiguration.Components.BusinessObjectFactory = BackendDefaultTypes.BusinessObjectFactory;
            // CI 環境改用環境變數作為 MasterKey 來源,避免在 tests/Define/ 下建立 Master.key
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

            // 用 AddBeeFramework 建 DI 容器，取代原本的 BackendInfo.Initialize。
            var services = new ServiceCollection();
            services.AddBeeFramework(settings.BackendConfiguration, pathOptions, autoCreateMasterKey: true);
            var provider = services.BuildServiceProvider();
            // 顯式 eager-resolve bootstrappers（等同 production app.UseBeeFramework() 的效果），
            // 觸發 CacheContainer / DbConnectionManager 兩個 process-wide 靜態 wire-up。
            // RepositoryInfo bootstrapper 已於 PR 5.3a 隨 RepositoryInfo 靜態類別一起刪除。
            provider.GetRequiredService<Bee.Api.AspNetCore.Bootstrapping.ICacheBootstrapper>();
            provider.GetRequiredService<Bee.Api.AspNetCore.Bootstrapping.IDbConnectionManagerBootstrapper>();

            BeeTestServices.Initialize(provider);
            // Bee.Api.Client 近端模式（in-process）需要透過 ApiClientInfo.LocalServiceProvider
            // 取得後端服務；測試 fixture 預設指向同一個 process-wide 容器。Phase 4 transitional —
            // 主計畫 §「範圍邊界」說明此 holder 是 Bee.Api.Client 重構前的暫時做法。
            Bee.Api.Client.ApiClientInfo.LocalServiceProvider = provider;
            Console.WriteLine("GlobalFixture Initialized");
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
        /// 測試完成後釋放資源。
        /// </summary>
        /// <remarks>
        /// 注意:在 single-host 模式下多個 fixture instance 會分別 dispose,
        /// 不可在此關閉 <c>SharedDatabaseState</c> 的 SQLite keep-alive
        /// （process-wide singleton，提早關閉會讓其他並行 collection 的 in-memory db 消失）。
        /// OS 會在 process exit 時自動回收連線。
        /// </remarks>
        public void Dispose()
        {
            Console.WriteLine("GlobalFixture Disposed");
            GC.SuppressFinalize(this);
        }
    }
}
