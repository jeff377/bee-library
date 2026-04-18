using System.ComponentModel;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="ExecFuncResult"/> 繼承自 <see cref="BusinessResult"/> 的行為測試。
    /// </summary>
    public class ExecFuncResultTests
    {
        [Fact]
        [DisplayName("ExecFuncResult 建構後 Parameters 為 lazy 初始化")]
        public void DefaultConstructor_ParametersLazyInitialized()
        {
            var result = new ExecFuncResult();

            Assert.NotNull(result.Parameters);
            result.Parameters.Add("key", "value");
            Assert.Equal("value", result.Parameters.GetValue<string>("key"));
        }

        [Fact]
        [DisplayName("ExecFuncResult 應為 BusinessResult 的子類")]
        public void ExecFuncResult_IsBusinessResult()
        {
            var result = new ExecFuncResult();
            Assert.IsType<BusinessResult>(result, exactMatch: false);
        }
    }
}
