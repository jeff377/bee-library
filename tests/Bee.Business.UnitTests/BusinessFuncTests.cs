using System.ComponentModel;
using Bee.Business.UnitTests.Fakes;
using Bee.Definition;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="BusinessFunc.InvokeExecFunc"/> 反射派發與權限檢查測試。
    /// </summary>
    public class BusinessFuncTests
    {
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
