using System.ComponentModel;
using Bee.Business;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Security;
using Bee.Repository.Abstractions.Factories;
using Bee.Tests.Shared;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// 驗證 AddBeeFramework 注冊的服務可從 DI 容器正常解析，
    /// 同時涵蓋各 singleton lambda 中的 CreateConfigurableService 等私有方法路徑。
    /// </summary>
    public class BeeFrameworkServiceResolutionTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public BeeFrameworkServiceResolutionTests(BeeTestFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("AddBeeFramework 建立的容器應能解析 IFormRepositoryFactory")]
        public void AddBeeFramework_CanResolve_IFormRepositoryFactory()
        {
            var service = _fx.GetRequiredService<IFormRepositoryFactory>();
            Assert.NotNull(service);
        }

        [Fact]
        [DisplayName("AddBeeFramework 建立的容器應能解析 ISystemRepositoryFactory")]
        public void AddBeeFramework_CanResolve_ISystemRepositoryFactory()
        {
            var service = _fx.GetRequiredService<ISystemRepositoryFactory>();
            Assert.NotNull(service);
        }

        [Fact]
        [DisplayName("AddBeeFramework 建立的容器應能解析 IBusinessObjectFactory")]
        public void AddBeeFramework_CanResolve_IBusinessObjectFactory()
        {
            var service = _fx.GetRequiredService<IBusinessObjectFactory>();
            Assert.NotNull(service);
        }

        [Fact]
        [DisplayName("AddBeeFramework 建立的容器應能解析 ISessionInfoService")]
        public void AddBeeFramework_CanResolve_ISessionInfoService()
        {
            var service = _fx.GetRequiredService<ISessionInfoService>();
            Assert.NotNull(service);
        }

        [Fact]
        [DisplayName("AddBeeFramework 建立的容器應能解析 IAccessTokenValidator")]
        public void AddBeeFramework_CanResolve_IAccessTokenValidator()
        {
            var service = _fx.GetRequiredService<IAccessTokenValidator>();
            Assert.NotNull(service);
        }

        [Fact]
        [DisplayName("AddBeeFramework 建立的容器應能解析 IFormBoTypeResolver")]
        public void AddBeeFramework_CanResolve_IFormBoTypeResolver()
        {
            var service = _fx.GetRequiredService<IFormBoTypeResolver>();
            Assert.NotNull(service);
        }

        [Fact]
        [DisplayName("AddBeeFramework 建立的容器應能解析 IEnterpriseObjectService")]
        public void AddBeeFramework_CanResolve_IEnterpriseObjectService()
        {
            var service = _fx.GetRequiredService<IEnterpriseObjectService>();
            Assert.NotNull(service);
        }
    }
}
