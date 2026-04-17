using System.ComponentModel;
using Bee.Business.BusinessObjects;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="FormBusinessObject"/> 行為測試，透過內部 <c>FormExecFuncHandler.Hello</c> 驗證反射派發。
    /// </summary>
    public class FormBusinessObjectTests
    {
        [Fact]
        [DisplayName("建構子應設定 AccessToken、ProgId、IsLocalCall")]
        public void Constructor_SetsProperties()
        {
            var token = Guid.NewGuid();

            var bo = new FormBusinessObject(token, "prog01", isLocalCall: false);

            Assert.Equal(token, bo.AccessToken);
            Assert.Equal("prog01", bo.ProgId);
            Assert.False(bo.IsLocalCall);
        }

        [Fact]
        [DisplayName("ExecFunc Hello 應填入預設訊息")]
        public void ExecFunc_Hello_FillsExpectedMessage()
        {
            var bo = new FormBusinessObject(Guid.NewGuid(), "prog01");

            var result = bo.ExecFunc(new ExecFuncArgs("Hello"));

            Assert.Equal("Hello form-level BusinessObject", result.Parameters.GetValue<string>("Hello"));
        }

        [Fact]
        [DisplayName("ExecFuncAnonymous 呼叫未標 attribute 的方法應拋 UnauthorizedAccessException")]
        public void ExecFuncAnonymous_HelloIsDefaultAuthenticated_ThrowsUnauthorized()
        {
            // FormExecFuncHandler.Hello 未標 ExecFuncAccessControl，預設視為 Authenticated，
            // 因此匿名呼叫時應被 InvokeExecFunc 阻擋。
            var bo = new FormBusinessObject(Guid.NewGuid(), "prog01");

            Assert.Throws<UnauthorizedAccessException>(() =>
                bo.ExecFuncAnonymous(new ExecFuncArgs("Hello")));
        }

        [Fact]
        [DisplayName("ExecFunc 呼叫不存在的方法應拋 MissingMethodException")]
        public void ExecFunc_UnknownMethod_ThrowsMissingMethod()
        {
            var bo = new FormBusinessObject(Guid.NewGuid(), "prog01");

            Assert.Throws<MissingMethodException>(() =>
                bo.ExecFunc(new ExecFuncArgs("DoesNotExist")));
        }
    }
}
