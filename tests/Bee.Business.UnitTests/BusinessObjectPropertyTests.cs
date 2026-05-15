using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// 驗證 <see cref="BusinessObject"/> 受保護屬性正確委派至 <see cref="IBeeContext"/>。
    /// </summary>
    public class BusinessObjectPropertyTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public BusinessObjectPropertyTests(SharedDbFixture fx) { _fx = fx; }

        private sealed class ExposingBusinessObject : BusinessObject
        {
            public ExposingBusinessObject(IBeeContext ctx, Guid token) : base(ctx, token) { }
            public IDefineAccess GetDefineAccess() => DefineAccess;
            public ISessionInfoService GetSessionInfoService() => SessionInfoService;
            public IBusinessObjectFactory GetBoFactory() => BoFactory;
            public IServiceProvider GetServices() => Services;
        }

        [Fact]
        [DisplayName("DefineAccess 屬性應回傳 IBeeContext 中的 DefineAccess 實例")]
        public void DefineAccess_ReturnsContextDefineAccess()
        {
            var ctx = TestBeeContext.Create(_fx);
            var bo = new ExposingBusinessObject(ctx, Guid.NewGuid());
            var result = bo.GetDefineAccess();
            Assert.NotNull(result);
            Assert.Same(ctx.DefineAccess, result);
        }

        [Fact]
        [DisplayName("SessionInfoService 屬性應回傳 IBeeContext 中的 SessionInfoService 實例")]
        public void SessionInfoService_ReturnsContextSessionInfoService()
        {
            var ctx = TestBeeContext.Create(_fx);
            var bo = new ExposingBusinessObject(ctx, Guid.NewGuid());
            var result = bo.GetSessionInfoService();
            Assert.NotNull(result);
            Assert.Same(ctx.SessionInfoService, result);
        }

        [Fact]
        [DisplayName("BoFactory 屬性應回傳 IBeeContext 中的 BoFactory 實例")]
        public void BoFactory_ReturnsContextBoFactory()
        {
            var ctx = TestBeeContext.Create(_fx);
            var bo = new ExposingBusinessObject(ctx, Guid.NewGuid());
            var result = bo.GetBoFactory();
            Assert.NotNull(result);
            Assert.Same(ctx.BoFactory, result);
        }

        [Fact]
        [DisplayName("Services 屬性應回傳 IBeeContext 中的 Services 實例")]
        public void Services_ReturnsContextServices()
        {
            var ctx = TestBeeContext.Create(_fx);
            var bo = new ExposingBusinessObject(ctx, Guid.NewGuid());
            var result = bo.GetServices();
            Assert.NotNull(result);
            Assert.Same(ctx.Services, result);
        }
    }
}
