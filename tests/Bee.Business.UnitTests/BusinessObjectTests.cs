using System.ComponentModel;
using Bee.Business.UnitTests.Fakes;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="BusinessObject"/> 基底行為測試。
    /// </summary>
    public class BusinessObjectTests
    {
        [Fact]
        [DisplayName("建構子應正確設定 AccessToken 與 IsLocalCall")]
        public void Constructor_SetsProperties()
        {
            var token = Guid.NewGuid();
            var bo = new TestableBusinessObject(token, isLocalCall: false);

            Assert.Equal(token, bo.AccessToken);
            Assert.False(bo.IsLocalCall);
        }

        [Fact]
        [DisplayName("IsLocalCall 預設為 true")]
        public void Constructor_DefaultIsLocalCall_IsTrue()
        {
            var bo = new TestableBusinessObject(Guid.NewGuid());
            Assert.True(bo.IsLocalCall);
        }

        [Fact]
        [DisplayName("ExecFunc 應委派至 DoExecFunc 覆寫")]
        public void ExecFunc_DelegatesToDoExecFunc()
        {
            var bo = new TestableBusinessObject(Guid.NewGuid());
            var args = new ExecFuncArgs("Hello");

            var result = bo.ExecFunc(args);

            Assert.Equal(1, bo.ExecFuncCallCount);
            Assert.Equal(0, bo.ExecFuncAnonymousCallCount);
            Assert.Same(args, bo.LastArgs);
            Assert.Equal("DoExecFunc", result.Parameters.GetValue<string>("Marker"));
        }

        [Fact]
        [DisplayName("ExecFuncAnonymous 應委派至 DoExecFuncAnonymous 覆寫")]
        public void ExecFuncAnonymous_DelegatesToDoExecFuncAnonymous()
        {
            var bo = new TestableBusinessObject(Guid.NewGuid());
            var args = new ExecFuncArgs("Hi");

            var result = bo.ExecFuncAnonymous(args);

            Assert.Equal(0, bo.ExecFuncCallCount);
            Assert.Equal(1, bo.ExecFuncAnonymousCallCount);
            Assert.Same(args, bo.LastArgs);
            Assert.Equal("DoExecFuncAnonymous", result.Parameters.GetValue<string>("Marker"));
        }

        [Fact]
        [DisplayName("未覆寫 DoExecFunc 時 ExecFunc 應回傳空結果不拋例外")]
        public void ExecFunc_WithoutOverride_ReturnsEmptyResult()
        {
            var bo = new BareBusinessObject(Guid.NewGuid());

            var result = bo.ExecFunc(new ExecFuncArgs("Anything"));

            Assert.NotNull(result);
            Assert.Empty(result.Parameters);
        }

        [Fact]
        [DisplayName("未覆寫 DoExecFuncAnonymous 時 ExecFuncAnonymous 應回傳空結果不拋例外")]
        public void ExecFuncAnonymous_WithoutOverride_ReturnsEmptyResult()
        {
            var bo = new BareBusinessObject(Guid.NewGuid());

            var result = bo.ExecFuncAnonymous(new ExecFuncArgs("Anything"));

            Assert.NotNull(result);
            Assert.Empty(result.Parameters);
        }
    }
}
