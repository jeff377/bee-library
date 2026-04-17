using System.ComponentModel;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="ExecFuncArgs"/> 建構子、屬性與 <see cref="BusinessArgs.Parameters"/> lazy 初始化測試。
    /// </summary>
    public class ExecFuncArgsTests
    {
        [Fact]
        [DisplayName("預設建構子 FuncId 應為空字串")]
        public void DefaultConstructor_FuncIdIsEmpty()
        {
            var args = new ExecFuncArgs();
            Assert.Equal(string.Empty, args.FuncId);
        }

        [Fact]
        [DisplayName("傳入 funcID 的建構子應設定 FuncId")]
        public void Constructor_WithFuncId_SetsFuncId()
        {
            var args = new ExecFuncArgs("SayHello");
            Assert.Equal("SayHello", args.FuncId);
        }

        [Fact]
        [DisplayName("FuncId 可重新設定")]
        public void FuncId_IsSettable()
        {
            var args = new ExecFuncArgs("First");
            args.FuncId = "Second";
            Assert.Equal("Second", args.FuncId);
        }

        [Fact]
        [DisplayName("Parameters 應為 lazy 初始化且可重複存取")]
        public void Parameters_LazyInitialized_ReturnsSameInstance()
        {
            var args = new ExecFuncArgs();

            var first = args.Parameters;
            var second = args.Parameters;

            Assert.NotNull(first);
            Assert.Same(first, second);
        }
    }
}
