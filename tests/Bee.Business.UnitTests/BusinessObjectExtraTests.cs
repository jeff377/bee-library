using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// 補強 <see cref="BusinessObject"/> 中 null-guard、SessionInfo 屬性、
    /// ResolveDatabaseId 與 CreateDataFormRepository 的覆蓋率測試。
    /// </summary>
    public class BusinessObjectExtraTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public BusinessObjectExtraTests(SharedDbFixture fx) { _fx = fx; }

        /// <summary>
        /// 測試用子類別，將 BusinessObject 的 protected 方法暴露為 public，
        /// 以便驗證其執行路徑而不需要 FormBusinessObject 的完整資料層依賴。
        /// </summary>
        private sealed class InspectableBusinessObject : BusinessObject
        {
            public InspectableBusinessObject(IBeeContext ctx, Guid accessToken)
                : base(ctx, accessToken) { }

            public string CallResolveDatabaseId(DbScope scope) => ResolveDatabaseId(scope);

            public void InvokeCreateDataFormRepository(string progId)
            {
                CreateDataFormRepository(progId);
            }
        }

        [Fact]
        [DisplayName("建構子傳入 null ctx 應拋 ArgumentNullException（null-guard 分支）")]
        public void Constructor_NullContext_ThrowsArgumentNullException()
        {
            var ex = Record.Exception(() => new InspectableBusinessObject(null!, Guid.NewGuid()));
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        [DisplayName("SessionInfo 屬性在未設定 Session 時應為 null")]
        public void SessionInfo_WithoutSession_IsNull()
        {
            var bo = new InspectableBusinessObject(TestBeeContext.Create(_fx), Guid.NewGuid());
            Assert.Null(bo.SessionInfo);
        }

        [Fact]
        [DisplayName("ResolveDatabaseId(Common) 應回傳 \"common\" 不需 Session")]
        public void ResolveDatabaseId_Common_ReturnsCommonDatabaseId()
        {
            var bo = new InspectableBusinessObject(TestBeeContext.Create(_fx), Guid.Empty);
            var result = bo.CallResolveDatabaseId(DbScope.Common);
            Assert.Equal(DbCategoryIds.Common, result);
        }

        [Fact]
        [DisplayName("CreateDataFormRepository 無有效 Session 時應拋 UnauthorizedAccessException")]
        public void CreateDataFormRepository_NoSession_ThrowsUnauthorized()
        {
            // Company scope 需要 Session 中的 CompanyId，Guid.Empty 無對應 Session → UnauthorizedAccessException
            var bo = new InspectableBusinessObject(TestBeeContext.Create(_fx), Guid.Empty);
            var ex = Record.Exception(() => bo.InvokeCreateDataFormRepository("Employee"));
            Assert.IsType<UnauthorizedAccessException>(ex);
        }
    }
}
