using System.ComponentModel;
using Bee.Business.UnitTests.Fakes;
using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Definition.Security;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="BusinessFunc"/> 測試;涵蓋 <see cref="BusinessFunc.GetDatabaseItem"/>
    /// 與 <see cref="BusinessFunc.InvokeExecFunc"/>。
    /// </summary>
    [Collection("Initialize")] // 串行化：本檔測試會 mutate BackendInfo.DefineAccess（global state），
    // 必須與其他讀取 BackendInfo.DefineAccess 的 [Collection("Initialize")] class 串行，
    // 否則 mutation 會在 concurrent test 中 leak 到 LocalDefineAccess 路徑，
    // 觸發 CacheInfo cctor poison（NotImplementedException → TypeInitializationException）。
    // 詳見 testing.md「全域狀態與平行安全」章節。
    public class BusinessFuncTests
    {
        [Fact]
        [DisplayName("GetDatabaseItem 於 databaseId 為空字串時應拋 ArgumentNullException")]
        public void GetDatabaseItem_EmptyId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => BusinessFunc.GetDatabaseItem(string.Empty));
        }

        [Fact]
        [DisplayName("GetDatabaseItem 於找不到對應項目時應拋 KeyNotFoundException")]
        public void GetDatabaseItem_NotFound_ThrowsKeyNotFoundException()
        {
            var original = BackendInfo.DefineAccess;
            try
            {
                BackendInfo.DefineAccess = new FakeDefineAccess();

                Assert.Throws<KeyNotFoundException>(() => BusinessFunc.GetDatabaseItem("missing"));
            }
            finally
            {
                BackendInfo.DefineAccess = original;
            }
        }

        [Fact]
        [DisplayName("GetDatabaseItem 於存在對應項目時應回傳該 DatabaseItem")]
        public void GetDatabaseItem_Found_ReturnsItem()
        {
            var original = BackendInfo.DefineAccess;
            try
            {
                var fake = new FakeDefineAccess();
                fake.Settings.Items!.Add(new DatabaseItem { Id = "common", DisplayName = "共用" });
                BackendInfo.DefineAccess = fake;

                var item = BusinessFunc.GetDatabaseItem("common");

                Assert.NotNull(item);
                Assert.Equal("common", item.Id);
                Assert.Equal("共用", item.DisplayName);
            }
            finally
            {
                BackendInfo.DefineAccess = original;
            }
        }

        [Fact]
        [DisplayName("InvokeExecFunc 呼叫不存在的方法應拋 MissingMethodException")]
        public void InvokeExecFunc_MethodNotFound_ThrowsMissingMethodException()
        {
            var handler = new FakeExecFuncHandler();
            var args = new ExecFuncArgs("DoesNotExist");
            var result = new ExecFuncResult();

            Assert.Throws<MissingMethodException>(() =>
                BusinessFunc.InvokeExecFunc(handler, ApiAccessRequirement.Authenticated, args, result));
        }

        [Fact]
        [DisplayName("InvokeExecFunc 匿名呼叫需驗證的方法應拋 UnauthorizedAccessException")]
        public void InvokeExecFunc_AnonymousCallsAuthenticated_ThrowsUnauthorized()
        {
            var handler = new FakeExecFuncHandler();
            var args = new ExecFuncArgs(nameof(FakeExecFuncHandler.Authenticated));
            var result = new ExecFuncResult();

            Assert.Throws<UnauthorizedAccessException>(() =>
                BusinessFunc.InvokeExecFunc(handler, ApiAccessRequirement.Anonymous, args, result));
        }

        [Fact]
        [DisplayName("InvokeExecFunc 匿名呼叫匿名方法應成功並填入結果")]
        public void InvokeExecFunc_AnonymousCallsAnonymous_Succeeds()
        {
            var handler = new FakeExecFuncHandler();
            var args = new ExecFuncArgs(nameof(FakeExecFuncHandler.Anonymous));
            var result = new ExecFuncResult();

            BusinessFunc.InvokeExecFunc(handler, ApiAccessRequirement.Anonymous, args, result);

            Assert.Equal("Anonymous", result.Parameters.GetValue<string>("Called"));
            Assert.Equal(nameof(FakeExecFuncHandler.Anonymous), result.Parameters.GetValue<string>("FuncId"));
        }

        [Fact]
        [DisplayName("InvokeExecFunc 已驗證呼叫已驗證方法應成功")]
        public void InvokeExecFunc_AuthenticatedCallsAuthenticated_Succeeds()
        {
            var handler = new FakeExecFuncHandler();
            var args = new ExecFuncArgs(nameof(FakeExecFuncHandler.Authenticated));
            var result = new ExecFuncResult();

            BusinessFunc.InvokeExecFunc(handler, ApiAccessRequirement.Authenticated, args, result);

            Assert.Equal("Authenticated", result.Parameters.GetValue<string>("Called"));
        }

        [Fact]
        [DisplayName("InvokeExecFunc 已驗證呼叫匿名方法應成功（權限足夠）")]
        public void InvokeExecFunc_AuthenticatedCallsAnonymous_Succeeds()
        {
            var handler = new FakeExecFuncHandler();
            var args = new ExecFuncArgs(nameof(FakeExecFuncHandler.Anonymous));
            var result = new ExecFuncResult();

            BusinessFunc.InvokeExecFunc(handler, ApiAccessRequirement.Authenticated, args, result);

            Assert.Equal("Anonymous", result.Parameters.GetValue<string>("Called"));
        }

        [Fact]
        [DisplayName("InvokeExecFunc 未標記 attribute 的方法預設為 Authenticated")]
        public void InvokeExecFunc_NoAttribute_DefaultsToAuthenticated()
        {
            var handler = new FakeExecFuncHandler();
            var args = new ExecFuncArgs(nameof(FakeExecFuncHandler.NoAttribute));
            var result = new ExecFuncResult();

            Assert.Throws<UnauthorizedAccessException>(() =>
                BusinessFunc.InvokeExecFunc(handler, ApiAccessRequirement.Anonymous, args, result));
        }

        [Fact]
        [DisplayName("InvokeExecFunc 未標記 attribute 時 Authenticated 呼叫應成功")]
        public void InvokeExecFunc_NoAttributeAuthenticated_Succeeds()
        {
            var handler = new FakeExecFuncHandler();
            var args = new ExecFuncArgs(nameof(FakeExecFuncHandler.NoAttribute));
            var result = new ExecFuncResult();

            BusinessFunc.InvokeExecFunc(handler, ApiAccessRequirement.Authenticated, args, result);

            Assert.Equal("NoAttribute", result.Parameters.GetValue<string>("Called"));
        }

        [Fact]
        [DisplayName("InvokeExecFunc 被叫方法拋例外應 unwrap 並保留原始型別")]
        public void InvokeExecFunc_TargetThrows_UnwrapsToOriginalException()
        {
            var handler = new FakeExecFuncHandler();
            var args = new ExecFuncArgs(nameof(FakeExecFuncHandler.Throws));
            var result = new ExecFuncResult();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                BusinessFunc.InvokeExecFunc(handler, ApiAccessRequirement.Anonymous, args, result));
            Assert.Equal("fake-inner-exception", ex.Message);
        }
    }
}
