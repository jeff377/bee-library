using System.ComponentModel;
using System.Reflection;
using Bee.Api.Core.JsonRpc;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Logging;
using Bee.Definition.Organization;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.ObjectCaching;
using Bee.Repository.Abstractions;
using Bee.Repository.Abstractions.AuditLog;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// 驗證 AddBeeFramework 所有 DI 服務的 singleton factory lambda 均可正常解析，
    /// 覆蓋 BeeFrameworkServiceCollectionExtensions 中 singleton 工廠委派的未覆蓋行。
    /// </summary>
    public class BeeFrameworkServiceResolutionTests
    {
        [Fact]
        [DisplayName("AddBeeFramework 應預設註冊 ILoginAttemptTracker")]
        public void AddBeeFramework_RegistersLoginAttemptTrackerByDefault()
        {
            // Login 是唯一可匿名觸達的憑證驗證面；先前此服務無預設實作，
            // 導致開箱即用的部署完全沒有帳號鎖定。
            using var sp = BuildProvider(out string tempDir);
            try
            {
                var tracker = sp.GetService<ILoginAttemptTracker>();

                Assert.NotNull(tracker);
                Assert.IsType<Bee.Business.Security.LoginAttemptTracker>(tracker);
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }

        [Fact]
        [DisplayName("host 自訂的 ILoginAttemptTracker 應覆蓋框架預設")]
        public void AddBeeFramework_HostRegisteredTracker_Wins()
        {
            // 註冊採 TryAdd，故 host 於 AddBeeFramework 之前註冊自己的實作時應勝出。
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-fw-tracker-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var services = new ServiceCollection();
                services.AddSingleton<ILoginAttemptTracker, FakeLoginAttemptTracker>();
                services.AddBeeFramework(
                    new BackendConfiguration(),
                    new PathOptions { DefinePath = tempDir },
                    autoCreateMasterKey: true);

                using var sp = services.BuildServiceProvider();

                Assert.IsType<FakeLoginAttemptTracker>(sp.GetRequiredService<ILoginAttemptTracker>());
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }

        private static ServiceProvider BuildProvider(out string tempDir)
        {
            tempDir = Path.Combine(Path.GetTempPath(), $"bee-fw-tracker-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            var services = new ServiceCollection();
            services.AddBeeFramework(
                new BackendConfiguration(),
                new PathOptions { DefinePath = tempDir },
                autoCreateMasterKey: true);
            return services.BuildServiceProvider();
        }

        private sealed class FakeLoginAttemptTracker : ILoginAttemptTracker
        {
            public bool IsLockedOut(string userId) => false;

            public void RecordFailure(string userId) { }

            public void Reset(string userId) { }
        }

        [Fact]
        [DisplayName("啟用稽核記錄時 IAuditLogWriteRepository 應可解析")]
        public void AddBeeFramework_AuditLogEnabled_ResolvesWriteRepository()
        {
            // 這條註冊只在 AuditLogOptions.Enabled 時存在，預設關閉，因此其他測試碰不到它。
            // Repository 統一為 (IRepositoryContext, Guid, string) 之後，容器無法自行建構
            // 具體型別（三個參數都拿不到），只有經工廠才建得起來。
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-fw-audit-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var configuration = new BackendConfiguration();
                configuration.AuditLogOptions.Enabled = true;
                configuration.AuditLogOptions.UseBackgroundWriter = false;

                var services = new ServiceCollection();
                // 稽核鏈上的 sink 需要 ILogger；正式 host 一定有，裸 ServiceCollection 沒有。
                services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
                services.AddBeeFramework(
                    configuration,
                    new PathOptions { DefinePath = tempDir },
                    autoCreateMasterKey: true);

                using var sp = services.BuildServiceProvider();

                Assert.NotNull(sp.GetRequiredService<IAuditLogWriteRepository>());
                Assert.NotNull(sp.GetRequiredService<IAuditLogWriter>());
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }

        [Fact]
        [DisplayName("AddBeeFramework 預設組態應能解析完整 DI 服務鏈（IDbConnectionManager 至 JsonRpcExecutor）")]
        public void AddBeeFramework_DefaultConfig_ResolvesFullServiceChain()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-fw-fullchain-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var services = new ServiceCollection();
                services.AddBeeFramework(
                    new BackendConfiguration(),
                    new PathOptions { DefinePath = tempDir },
                    autoCreateMasterKey: true);

                using var sp = services.BuildServiceProvider();

                Assert.NotNull(sp.GetRequiredService<IDbConnectionManager>());
                Assert.NotNull(sp.GetRequiredService<IDbAccessFactory>());
                Assert.NotNull(sp.GetRequiredService<IAccessTokenValidator>());
                Assert.NotNull(sp.GetRequiredService<ISessionInfoService>());
                Assert.NotNull(sp.GetRequiredService<ICompanyInfoService>());
                // Resolving both of these proves the cache container takes its data source as a
                // deferred factory: resolving it eagerly would close the cycle ICacheContainer →
                // ICacheDataSourceProvider → repositories → IDefineAccess → ICacheContainer.
                Assert.NotNull(sp.GetRequiredService<ICacheContainer>());
                Assert.NotNull(sp.GetRequiredService<ICacheDataSourceProvider>());
                Assert.NotNull(sp.GetRequiredService<IRolePermissionService>());
                Assert.NotNull(sp.GetRequiredService<IDepartmentTreeService>());
                Assert.NotNull(sp.GetRequiredService<IBusinessObjectFactory>());
                Assert.NotNull(sp.GetRequiredService<IRepositoryDatabaseRouter>());
                // Repository 的唯一入口，兩軸皆由它解析。
                Assert.NotNull(sp.GetRequiredService<IRepositoryFactory>());
                // Individual repositories are not DI-registered by design — consumers go
                // through the factory, so resolving one from it is what this asserts.
                Assert.NotNull(sp.GetRequiredService<IRepositoryFactory>().Create<ICompanyRepository>());
                Assert.NotNull(sp.GetRequiredService<IRepositoryFactory>().Create<IUserCompanyRepository>());
                Assert.NotNull(sp.GetRequiredService<JsonRpcExecutor>());
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }

        [Fact]
        [DisplayName("容器建出的 RepositoryFactory 應接到客製 overlay 所需的兩個選用相依")]
        public void AddBeeFramework_RepositoryFactory_ReceivesCustomizationDependencies()
        {
            // 這兩個相依是選用參數（預設 null），ActivatorUtilities 沒填就是靜默停用租戶客製
            // ——progId 一律解析基底綁定，而且不會有任何其他症狀。行為上看不出來，只能直接
            // 檢查欄位；這正是本測試存在的理由。
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-fw-repocust-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var services = new ServiceCollection();
                services.AddBeeFramework(
                    new BackendConfiguration(),
                    new PathOptions { DefinePath = tempDir },
                    autoCreateMasterKey: true);

                using var sp = services.BuildServiceProvider();
                var factory = sp.GetRequiredService<IRepositoryFactory>();

                Assert.NotNull(PrivateField(factory, "_customizeReader"));
                Assert.NotNull(PrivateField(factory, "_sessionInfoService"));
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }

        private static object? PrivateField(object instance, string name)
            => instance.GetType()
                .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(instance);
    }
}
