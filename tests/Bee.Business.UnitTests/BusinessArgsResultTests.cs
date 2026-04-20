using System.ComponentModel;
using Bee.Definition.Collections;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="BusinessArgs"/> 與 <see cref="BusinessResult"/> 的 <c>Parameters</c>
    /// lazy 初始化與 setter 行為測試（透過 <see cref="ExecFuncArgs"/>、<see cref="ExecFuncResult"/> 子類驗證）。
    /// </summary>
    public class BusinessArgsResultTests
    {
        [Fact]
        [DisplayName("BusinessArgs.Parameters 首次存取應 lazy 建立非 null 集合")]
        public void BusinessArgs_Parameters_LazyInitialized()
        {
            var args = new ExecFuncArgs();
            var parameters = args.Parameters;

            Assert.NotNull(parameters);
            Assert.Empty(parameters);
        }

        [Fact]
        [DisplayName("BusinessArgs.Parameters setter 應覆寫既有集合")]
        public void BusinessArgs_Parameters_Setter_Overrides()
        {
            var args = new ExecFuncArgs();
            args.Parameters.Add("old", "value");

            var replacement = new ParameterCollection
            {
                { "new", "value" }
            };
            args.Parameters = replacement;

            Assert.Same(replacement, args.Parameters);
            Assert.False(args.Parameters.Contains("old"));
            Assert.True(args.Parameters.Contains("new"));
        }

        [Fact]
        [DisplayName("BusinessResult.Parameters 行為與 BusinessArgs 相同")]
        public void BusinessResult_Parameters_LazyAndSettable()
        {
            var result = new ExecFuncResult();

            var parameters = result.Parameters;
            Assert.NotNull(parameters);

            var replacement = new ParameterCollection
            {
                { "k", 42 }
            };
            result.Parameters = replacement;

            Assert.Same(replacement, result.Parameters);
            Assert.Equal(42, result.Parameters.GetValue<int>("k"));
        }
    }
}
