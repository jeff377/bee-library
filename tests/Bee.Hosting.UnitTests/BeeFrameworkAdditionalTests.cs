using System.ComponentModel;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Security;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions.Factories;
using Bee.Tests.Shared;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// 補強 <see cref="BeeFrameworkServiceCollectionExtensions"/> 的 DI singleton 工廠延遲解析路徑。
    /// </summary>
    public class BeeFrameworkAdditionalTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public BeeFrameworkAdditionalTests(BeeTestFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("DI 容器解析 IDefineAccess 應回傳非 null 實例（涵蓋 CreateDefineStorage 與 ResolveDefineAccess 路徑）")]
        public void ResolveDefineAccess_FromDI_ReturnsNonNull()
        {
            var access = _fx.GetRequiredService<IDefineAccess>();
            Assert.NotNull(access);
        }

        [Fact]
        [DisplayName("DI 容器解析 IDbConnectionManager 應回傳非 null 實例")]
        public void ResolveDbConnectionManager_FromDI_ReturnsNonNull()
        {
            var manager = _fx.GetRequiredService<IDbConnectionManager>();
            Assert.NotNull(manager);
        }

        [Fact]
        [DisplayName("DI 容器解析 IDbAccessFactory 應回傳非 null 實例")]
        public void ResolveDbAccessFactory_FromDI_ReturnsNonNull()
        {
            var factory = _fx.GetRequiredService<IDbAccessFactory>();
            Assert.NotNull(factory);
        }

        [Fact]
        [DisplayName("DI 容器解析 IFormRepositoryFactory 應回傳非 null 實例（涵蓋 CreateConfigurableService 路徑）")]
        public void ResolveFormRepositoryFactory_FromDI_ReturnsNonNull()
        {
            var factory = _fx.GetRequiredService<IFormRepositoryFactory>();
            Assert.NotNull(factory);
        }

        [Fact]
        [DisplayName("DI 容器解析 IBusinessObjectFactory 應回傳非 null 實例（涵蓋 CreateBusinessObjectFactory 路徑）")]
        public void ResolveBusinessObjectFactory_FromDI_ReturnsNonNull()
        {
            var factory = _fx.GetRequiredService<IBusinessObjectFactory>();
            Assert.NotNull(factory);
        }

        [Fact]
        [DisplayName("DI 容器以預設設定解析 IApiEncryptionKeyProvider 應回傳非 null 實例（涵蓋 DynamicApiEncryptionKeyProvider 路徑）")]
        public void ResolveApiEncryptionKeyProvider_DefaultConfig_ReturnsNonNull()
        {
            var provider = _fx.GetRequiredService<IApiEncryptionKeyProvider>();
            Assert.NotNull(provider);
        }
    }
}
