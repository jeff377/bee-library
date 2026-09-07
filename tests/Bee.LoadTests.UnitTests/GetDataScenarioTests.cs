using System.ComponentModel;
using Bee.LoadTests.Configuration;
using Bee.LoadTests.Scenarios;

namespace Bee.LoadTests.UnitTests
{
    /// <summary>
    /// Tests for <see cref="GetDataScenario"/>.
    /// </summary>
    public class GetDataScenarioTests
    {
        private static VirtualUserPool Pool() => new(new AuthOptions());

        [Fact]
        [DisplayName("場景名稱與設定檔中的名稱一致")]
        public void Name_MatchesConfigurationKey()
        {
            Assert.Equal("GetData", new GetDataScenario(Pool(), "Customer", [Guid.NewGuid()]).Name);
        }

        [Fact]
        [DisplayName("沒有可用的鍵時拒絕建構，訊息指出補救方式")]
        public void Constructor_NoRowIds_Throws()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => new GetDataScenario(Pool(), "Customer", []));

            Assert.Contains("prepare", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("未指定 progId 時採預設")]
        public void Constructor_EmptyProgId_FallsBackToDefault()
        {
            var scenario = new GetDataScenario(Pool(), string.Empty, [Guid.NewGuid()]);

            Assert.Equal("GetData", scenario.Name);
        }
    }
}
