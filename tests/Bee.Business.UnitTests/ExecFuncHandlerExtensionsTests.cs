using System.ComponentModel;
using Bee.Business.UnitTests.Fakes;
using Bee.Definition.Security;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="ExecFuncHandlerExtensions"/> 的 InvokeExecFunc 多載測試。
    /// </summary>
    public class ExecFuncHandlerExtensionsTests
    {
        [Fact]
        [DisplayName("InvokeExecFunc 呼叫不存在的方法應拋 MissingMethodException")]
        public void InvokeExecFunc_MethodNotFound_ThrowsMissingMethodException()
        {
            var handler = new FakeExecFuncHandler();
            var args = new ExecFuncArgs("DoesNotExist");
            var result = new ExecFuncResult();

            Assert.Throws<MissingMethodException>(() =>
                handler.InvokeExecFunc(ApiAccessRequirement.Authenticated, args, result));
        }

        [Fact]
        [DisplayName("InvokeExecFunc 匿名呼叫需驗證的方法應拋 UnauthorizedAccessException")]
        public void InvokeExecFunc_AnonymousCallsAuthenticated_ThrowsUnauthorized()
        {
            var handler = new FakeExecFuncHandler();
            var args = new ExecFuncArgs(nameof(FakeExecFuncHandler.Authenticated));
            var result = new ExecFuncResult();

            Assert.Throws<UnauthorizedAccessException>(() =>
                handler.InvokeExecFunc(ApiAccessRequirement.Anonymous, args, result));
        }

        [Fact]
        [DisplayName("InvokeExecFunc 匿名呼叫匿名方法應成功並填入結果")]
        public void InvokeExecFunc_AnonymousCallsAnonymous_Succeeds()
        {
            var handler = new FakeExecFuncHandler();
            var args = new ExecFuncArgs(nameof(FakeExecFuncHandler.Anonymous));
            var result = new ExecFuncResult();

            handler.InvokeExecFunc(ApiAccessRequirement.Anonymous, args, result);

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

            handler.InvokeExecFunc(ApiAccessRequirement.Authenticated, args, result);

            Assert.Equal("Authenticated", result.Parameters.GetValue<string>("Called"));
        }

        [Fact]
        [DisplayName("InvokeExecFunc 已驗證呼叫匿名方法應成功（權限足夠）")]
        public void InvokeExecFunc_AuthenticatedCallsAnonymous_Succeeds()
        {
            var handler = new FakeExecFuncHandler();
            var args = new ExecFuncArgs(nameof(FakeExecFuncHandler.Anonymous));
            var result = new ExecFuncResult();

            handler.InvokeExecFunc(ApiAccessRequirement.Authenticated, args, result);

            Assert.Equal("Anonymous", result.Parameters.GetValue<string>("Called"));
        }

        [Fact]
        [DisplayName("InvokeExecFunc 未標記 attribute 的方法匿名呼叫應拒絕")]
        public void InvokeExecFunc_NoAttributeAnonymous_ThrowsUnauthorized()
        {
            var handler = new FakeExecFuncHandler();
            var args = new ExecFuncArgs(nameof(FakeExecFuncHandler.NoAttribute));
            var result = new ExecFuncResult();

            Assert.Throws<UnauthorizedAccessException>(() =>
                handler.InvokeExecFunc(ApiAccessRequirement.Anonymous, args, result));
        }

        [Fact]
        [DisplayName("InvokeExecFunc 未標記 attribute 的方法即使已驗證也應拒絕（fail-closed）")]
        public void InvokeExecFunc_NoAttributeAuthenticated_ThrowsUnauthorized()
        {
            var handler = new FakeExecFuncHandler();
            var args = new ExecFuncArgs(nameof(FakeExecFuncHandler.NoAttribute));
            var result = new ExecFuncResult();

            var ex = Assert.Throws<UnauthorizedAccessException>(() =>
                handler.InvokeExecFunc(ApiAccessRequirement.Authenticated, args, result));
            Assert.Contains("does not declare", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("InvokeExecFunc LocalOnly 方法遠端呼叫應拒絕")]
        public void InvokeExecFunc_LocalOnlyRemoteCall_ThrowsUnauthorized()
        {
            var handler = new FakeExecFuncHandler();
            var args = new ExecFuncArgs(nameof(FakeExecFuncHandler.LocalOnly));
            var result = new ExecFuncResult();

            var ex = Assert.Throws<UnauthorizedAccessException>(() =>
                handler.InvokeExecFunc(ApiAccessRequirement.Authenticated, isLocalCall: false, args, result));
            Assert.Contains("local calls only", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("InvokeExecFunc LocalOnly 方法本機呼叫應成功")]
        public void InvokeExecFunc_LocalOnlyLocalCall_Succeeds()
        {
            var handler = new FakeExecFuncHandler();
            var args = new ExecFuncArgs(nameof(FakeExecFuncHandler.LocalOnly));
            var result = new ExecFuncResult();

            handler.InvokeExecFunc(ApiAccessRequirement.Authenticated, isLocalCall: true, args, result);

            Assert.Equal("LocalOnly", result.Parameters.GetValue<string>("Called"));
        }

        [Fact]
        [DisplayName("InvokeExecFunc 舊多載視同遠端呼叫，LocalOnly 方法應拒絕")]
        public void InvokeExecFunc_LegacyOverload_TreatsCallAsRemote()
        {
            var handler = new FakeExecFuncHandler();
            var args = new ExecFuncArgs(nameof(FakeExecFuncHandler.LocalOnly));
            var result = new ExecFuncResult();

            Assert.Throws<UnauthorizedAccessException>(() =>
                handler.InvokeExecFunc(ApiAccessRequirement.Authenticated, args, result));
        }

        [Fact]
        [DisplayName("InvokeExecFunc 被叫方法拋例外應 unwrap 並保留原始型別")]
        public void InvokeExecFunc_TargetThrows_UnwrapsToOriginalException()
        {
            var handler = new FakeExecFuncHandler();
            var args = new ExecFuncArgs(nameof(FakeExecFuncHandler.Throws));
            var result = new ExecFuncResult();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                handler.InvokeExecFunc(ApiAccessRequirement.Anonymous, args, result));
            Assert.Equal("fake-inner-exception", ex.Message);
        }
    }
}
