using Bee.Base.Exceptions;

namespace Bee.Base.UnitTests.Exceptions
{
    public class ForbiddenExceptionTests
    {
        [Fact]
        [DisplayName("單參數 ctor 應設定 Message，InnerException 為 null")]
        public void Ctor_WithMessage_SetsMessage()
        {
            var ex = new ForbiddenException("permission denied");
            Assert.Equal("permission denied", ex.Message);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        [DisplayName("雙參數 ctor 應同時設定 Message 與 InnerException")]
        public void Ctor_WithMessageAndInner_SetsBoth()
        {
            var inner = new InvalidOperationException("root cause");
            var ex = new ForbiddenException("permission denied", inner);
            Assert.Equal("permission denied", ex.Message);
            Assert.Same(inner, ex.InnerException);
        }

        [Fact]
        [DisplayName("ForbiddenException 應可由 catch (Exception) 接住")]
        public void Throw_CanBeCaughtAsException()
        {
            Exception? caught = null;
            try
            {
                throw new ForbiddenException("test");
            }
            catch (Exception ex)
            {
                caught = ex;
            }
            Assert.NotNull(caught);
            Assert.IsType<ForbiddenException>(caught);
        }
    }
}
