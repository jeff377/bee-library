using System.ComponentModel;
using Bee.LoadTests.Configuration;
using Bee.LoadTests.Scenarios;

namespace Bee.LoadTests.UnitTests
{
    /// <summary>
    /// Tests for <see cref="LoginScenario"/>'s account selection.
    /// </summary>
    public class LoginScenarioTests
    {
        private static AuthOptions Auth(TokenStrategy strategy, int poolSize) => new()
        {
            TokenStrategy = strategy,
            UserPoolSize = poolSize,
            UserIdPrefix = "loadtest_user_"
        };

        [Fact]
        [DisplayName("PerUser 策略下每個 VU 用自己的帳號")]
        public void ResolveUserId_PerUser_GivesEachVirtualUserItsOwn()
        {
            var scenario = new LoginScenario(Auth(TokenStrategy.PerUser, 10));

            Assert.Equal("loadtest_user_0", scenario.ResolveUserId(0));
            Assert.Equal("loadtest_user_3", scenario.ResolveUserId(3));
        }

        [Fact]
        [DisplayName("VU 數超過帳號池時繞回，run 仍可執行")]
        public void ResolveUserId_MoreUsersThanAccounts_WrapsAround()
        {
            var scenario = new LoginScenario(Auth(TokenStrategy.PerUser, 3));

            Assert.Equal("loadtest_user_0", scenario.ResolveUserId(3));
            Assert.Equal("loadtest_user_1", scenario.ResolveUserId(7));
        }

        [Fact]
        [DisplayName("Shared 策略下所有 VU 共用第一個帳號")]
        public void ResolveUserId_Shared_AlwaysFirstAccount()
        {
            var scenario = new LoginScenario(Auth(TokenStrategy.Shared, 10));

            Assert.Equal("loadtest_user_0", scenario.ResolveUserId(0));
            Assert.Equal("loadtest_user_0", scenario.ResolveUserId(9));
        }

        [Fact]
        [DisplayName("帳號池為 0 時不擲除零例外")]
        public void ResolveUserId_ZeroPoolSize_DoesNotDivideByZero()
        {
            var scenario = new LoginScenario(Auth(TokenStrategy.PerUser, 0));

            Assert.Equal("loadtest_user_0", scenario.ResolveUserId(5));
        }

        [Fact]
        [DisplayName("場景名稱與設定檔中的名稱一致")]
        public void Name_MatchesConfigurationKey()
        {
            Assert.Equal("Login", new LoginScenario(Auth(TokenStrategy.PerUser, 1)).Name);
        }
    }
}
