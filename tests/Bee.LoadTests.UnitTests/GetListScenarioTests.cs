using System.ComponentModel;
using Bee.LoadTests.Configuration;
using Bee.LoadTests.Scenarios;

namespace Bee.LoadTests.UnitTests
{
    /// <summary>
    /// Tests for <see cref="GetListScenario"/>'s configuration handling.
    /// </summary>
    public class GetListScenarioTests
    {
        private static VirtualUserPool Pool() => new(new AuthOptions());

        [Fact]
        [DisplayName("場景名稱與設定檔中的名稱一致")]
        public void Name_MatchesConfigurationKey()
        {
            Assert.Equal("GetList", new GetListScenario(Pool(), "Customer", 50).Name);
        }

        [Fact]
        [DisplayName("未指定 progId 時採預設，不會用空字串去打伺服器")]
        public void Constructor_EmptyProgId_FallsBackToDefault()
        {
            // Constructing must not throw on an unset progId; the default keeps the run usable
            // when the configuration omits it.
            var scenario = new GetListScenario(Pool(), string.Empty, 50);

            Assert.Equal("GetList", scenario.Name);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [DisplayName("pageSize 非正值時採預設，避免退化成未分頁查詢")]
        public void Constructor_NonPositivePageSize_FallsBackToDefault(int pageSize)
        {
            // A page size of zero must not reach the server: an unpaged list query materialises
            // the whole table on both ends, which measures something else entirely.
            var scenario = new GetListScenario(Pool(), "Customer", pageSize);

            Assert.Equal("GetList", scenario.Name);
        }

        [Fact]
        [DisplayName("pool 為 null 時拒絕建構")]
        public void Constructor_NullPool_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GetListScenario(null!, "Customer", 50));
        }
    }
}
