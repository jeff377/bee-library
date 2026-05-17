using System.ComponentModel;
using Bee.Api.Core.JsonRpc;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.Repository.Abstractions;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.System;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// 驗證 AddBeeFramework 所有 DI 服務的 singleton factory lambda 均可正常解析，
    /// 覆蓋 BeeFrameworkServiceCollectionExtensions 中 singleton 工廠委派的未覆蓋行。
    /// </summary>
    public class BeeFrameworkServiceResolutionTests
    {
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
                Assert.NotNull(sp.GetRequiredService<ICacheDataSourceProvider>());
                Assert.NotNull(sp.GetRequiredService<IBusinessObjectFactory>());
                Assert.NotNull(sp.GetRequiredService<IRepositoryDatabaseRouter>());
                Assert.NotNull(sp.GetRequiredService<ISystemRepositoryFactory>());
                Assert.NotNull(sp.GetRequiredService<IFormRepositoryFactory>());
                Assert.NotNull(sp.GetRequiredService<ICompanyRepository>());
                Assert.NotNull(sp.GetRequiredService<IUserCompanyRepository>());
                Assert.NotNull(sp.GetRequiredService<JsonRpcExecutor>());
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }
    }
}
