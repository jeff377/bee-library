using System.ComponentModel;
using Bee.LoadTests.Configuration;
using Bee.LoadTests.Scenarios;

namespace Bee.LoadTests.UnitTests
{
    /// <summary>
    /// Tests for <see cref="VirtualUserPool"/>'s up-front sign-in.
    /// </summary>
    /// <remarks>
    /// These run with no backend hosted, so signing in cannot succeed. That is the case under
    /// test: what matters is that the failure is reported as a failed precondition naming the
    /// account, rather than escaping to be counted against whichever scenario asked for a session.
    /// </remarks>
    public class VirtualUserPoolTests
    {
        private static AuthOptions Auth() => new()
        {
            UserIdPrefix = "loadtest_user_",
            UserPoolSize = 4,
            Password = "irrelevant",
        };

        [Fact]
        [DisplayName("登入失敗時擲出的訊息點名虛擬使用者與帳號，而不是讓失敗落到場景上")]
        public async Task SignInAllAsync_SignInFails_MessageNamesTheVirtualUserAndAccount()
        {
            var pool = new VirtualUserPool(Auth());

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => pool.SignInAllAsync(virtualUsers: 1));

            // Virtual user 0 maps onto the first account in the pool. Both halves are asserted
            // because the index alone does not tell an operator which credentials to check.
            Assert.Contains("Virtual user 0", exception.Message, StringComparison.Ordinal);
            Assert.Contains("loadtest_user_0", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("登入失敗的例外保留原始例外，診斷線索不被訊息取代")]
        public async Task SignInAllAsync_SignInFails_PreservesTheUnderlyingException()
        {
            var pool = new VirtualUserPool(Auth());

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => pool.SignInAllAsync(virtualUsers: 1));

            Assert.NotNull(exception.InnerException);
        }

        [Fact]
        [DisplayName("虛擬使用者索引超過帳號池大小時繞回，訊息指向繞回後的帳號")]
        public async Task SignInAllAsync_MoreVirtualUsersThanAccounts_WrapsOntoThePool()
        {
            // The pool holds four accounts, so the fifth virtual user reuses the first. The
            // message must name the account actually used, not the virtual user index — an
            // operator checking "loadtest_user_4" would find no such row.
            var pool = new VirtualUserPool(Auth());

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => pool.SignInAllAsync(virtualUsers: 5));

            Assert.Contains("Virtual user 0", exception.Message, StringComparison.Ordinal);
            Assert.Contains("loadtest_user_0", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("共用權杖策略下所有虛擬使用者都登入同一個帳號")]
        public async Task SignInAllAsync_SharedTokenStrategy_UsesTheFirstAccount()
        {
            var auth = Auth();
            auth.TokenStrategy = TokenStrategy.Shared;
            var pool = new VirtualUserPool(auth);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => pool.SignInAllAsync(virtualUsers: 3));

            Assert.Contains("loadtest_user_0", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("沒有虛擬使用者時不嘗試登入")]
        public async Task SignInAllAsync_ZeroVirtualUsers_DoesNotAttemptSignIn()
        {
            // Guards the loop bound: a zero-user run has nothing to prepare, and reporting a
            // sign-in failure for a run that drives nobody would be a false alarm.
            var pool = new VirtualUserPool(Auth());

            var exception = await Record.ExceptionAsync(() => pool.SignInAllAsync(virtualUsers: 0));

            Assert.Null(exception);
        }
    }
}
