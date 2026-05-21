using System.ComponentModel;
using Bee.Base.Exceptions;

namespace Bee.Base.UnitTests.Exceptions
{
    /// <summary>
    /// Tests for <see cref="UserMessageException"/> covering constructor overloads,
    /// inheritance behaviour, and catch semantics.
    /// </summary>
    public class UserMessageExceptionTests
    {
        [Fact]
        [DisplayName("單參數 ctor 應設定 Message,InnerException 為 null")]
        public void Ctor_WithMessage_SetsMessage()
        {
            var ex = new UserMessageException("test message");

            Assert.Equal("test message", ex.Message);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        [DisplayName("雙參數 ctor 應同時設定 Message 與 InnerException")]
        public void Ctor_WithMessageAndInner_SetsBoth()
        {
            var inner = new InvalidOperationException("inner cause");
            var ex = new UserMessageException("test message", inner);

            Assert.Equal("test message", ex.Message);
            Assert.Same(inner, ex.InnerException);
        }

        [Fact]
        [DisplayName("UserMessageException 應可由 catch (Exception) 接住")]
        public void Throw_CanBeCaughtAsException()
        {
            Exception? caught = null;
            try
            {
                throw new UserMessageException("test");
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            Assert.NotNull(caught);
            Assert.IsType<UserMessageException>(caught);
        }

        [Fact]
        [DisplayName("UserMessageException 不應被 catch (InvalidOperationException) 接住(驗證型別獨立)")]
        public void Throw_NotCaughtAsInvalidOperationException()
        {
            UserMessageException? rethrown = null;
            try
            {
                try
                {
                    throw new UserMessageException("test");
                }
                catch (InvalidOperationException)
                {
                    // Should not reach here.
                    Assert.Fail("UserMessageException should not be caught as InvalidOperationException.");
                }
            }
            catch (UserMessageException ex)
            {
                rethrown = ex;
            }

            Assert.NotNull(rethrown);
        }

        [Fact]
        [DisplayName("UserMessageException 不應被 catch (ArgumentException) 接住(驗證型別獨立)")]
        public void Throw_NotCaughtAsArgumentException()
        {
            UserMessageException? rethrown = null;
            try
            {
                try
                {
                    throw new UserMessageException("test");
                }
                catch (ArgumentException)
                {
                    // Should not reach here.
                    Assert.Fail("UserMessageException should not be caught as ArgumentException.");
                }
            }
            catch (UserMessageException ex)
            {
                rethrown = ex;
            }

            Assert.NotNull(rethrown);
        }
    }
}
