using System.ComponentModel;
using Bee.Business;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="BusinessObject"/> 受保護屬性與 <see cref="BusinessObject.SessionInfo"/> 覆蓋測試。
    /// </summary>
    public class BusinessObjectPropertyTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public BusinessObjectPropertyTests(BeeTestFixture fx) { _fx = fx; }

        /// <summary>
        /// 將受保護的屬性與方法暴露為 public，供測試驗證。
        /// </summary>
        private sealed class ExposedBusinessObject : BusinessObject
        {
            public ExposedBusinessObject(IBeeContext ctx, Guid accessToken)
                : base(ctx, accessToken) { }

            public IDefineAccess ExposedDefineAccess => DefineAccess;
            public ISessionInfoService ExposedSessionInfoService => SessionInfoService;
            public IBusinessObjectFactory ExposedBoFactory => BoFactory;
            public IServiceProvider ExposedServices => Services;
            public string InvokeResolveDatabaseId(DbScope scope) => ResolveDatabaseId(scope);
        }

        [Fact]
        [DisplayName("SessionInfo 屬性預設應回傳 null（基底類別不設定）")]
        public void SessionInfo_DefaultValue_ReturnsNull()
        {
            var bo = new ExposedBusinessObject(TestBeeContext.Create(_fx), Guid.NewGuid());
            Assert.Null(bo.SessionInfo);
        }

        [Fact]
        [DisplayName("DefineAccess 屬性應轉發 Context 中的 IDefineAccess 實例")]
        public void DefineAccess_Property_ForwardsContextDefineAccess()
        {
            var ctx = TestBeeContext.Create(_fx);
            var bo = new ExposedBusinessObject(ctx, Guid.NewGuid());
            Assert.Same(ctx.DefineAccess, bo.ExposedDefineAccess);
        }

        [Fact]
        [DisplayName("SessionInfoService 屬性應轉發 Context 中的 ISessionInfoService 實例")]
        public void SessionInfoService_Property_ForwardsContextSessionInfoService()
        {
            var ctx = TestBeeContext.Create(_fx);
            var bo = new ExposedBusinessObject(ctx, Guid.NewGuid());
            Assert.Same(ctx.SessionInfoService, bo.ExposedSessionInfoService);
        }

        [Fact]
        [DisplayName("BoFactory 屬性應轉發 Context 中的 IBusinessObjectFactory 實例")]
        public void BoFactory_Property_ForwardsContextBoFactory()
        {
            var ctx = TestBeeContext.Create(_fx);
            var bo = new ExposedBusinessObject(ctx, Guid.NewGuid());
            Assert.Same(ctx.BoFactory, bo.ExposedBoFactory);
        }

        [Fact]
        [DisplayName("Services 屬性應轉發 Context 中的 IServiceProvider 實例")]
        public void Services_Property_ForwardsContextServices()
        {
            var ctx = TestBeeContext.Create(_fx);
            var bo = new ExposedBusinessObject(ctx, Guid.NewGuid());
            Assert.Same(ctx.Services, bo.ExposedServices);
        }

        [Fact]
        [DisplayName("ResolveDatabaseId(Common) 應回傳非空的 databaseId 字串")]
        public void ResolveDatabaseId_CommonScope_ReturnsNonEmptyDatabaseId()
        {
            var bo = new ExposedBusinessObject(TestBeeContext.Create(_fx), Guid.NewGuid());
            var databaseId = bo.InvokeResolveDatabaseId(DbScope.Common);
            Assert.NotEmpty(databaseId);
        }
    }
}
