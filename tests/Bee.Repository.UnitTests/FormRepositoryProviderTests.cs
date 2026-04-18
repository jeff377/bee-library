using System.ComponentModel;
using Bee.Repository.Form;
using Bee.Repository.Provider;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// 針對 <see cref="FormRepositoryProvider"/> 工廠方法的純邏輯測試。
    /// </summary>
    public class FormRepositoryProviderTests
    {
        private const string DataProgId = "Employee";
        private const string ReportProgId = "SalesReport";

        [Fact]
        [DisplayName("GetDataFormRepository 應回傳帶有指定 ProgId 的 DataFormRepository")]
        public void GetDataFormRepository_ReturnsDataFormRepositoryWithProgId()
        {
            var provider = new FormRepositoryProvider();
            var repo = provider.GetDataFormRepository(DataProgId);

            var typed = Assert.IsType<DataFormRepository>(repo);
            Assert.Equal(DataProgId, typed.ProgId);
        }

        [Fact]
        [DisplayName("GetReportFormRepository 應回傳帶有指定 ProgId 的 ReportFormRepository")]
        public void GetReportFormRepository_ReturnsReportFormRepositoryWithProgId()
        {
            var provider = new FormRepositoryProvider();
            var repo = provider.GetReportFormRepository(ReportProgId);

            var typed = Assert.IsType<ReportFormRepository>(repo);
            Assert.Equal(ReportProgId, typed.ProgId);
        }

        [Fact]
        [DisplayName("GetDataFormRepository 不同 ProgId 應回傳不同實例")]
        public void GetDataFormRepository_DifferentProgIds_ReturnDifferentInstances()
        {
            var provider = new FormRepositoryProvider();

            var first = provider.GetDataFormRepository("A");
            var second = provider.GetDataFormRepository("B");

            Assert.NotSame(first, second);
            Assert.Equal("A", ((DataFormRepository)first).ProgId);
            Assert.Equal("B", ((DataFormRepository)second).ProgId);
        }

        [Fact]
        [DisplayName("GetDataFormRepository 相同 ProgId 每次呼叫回傳新實例（非快取）")]
        public void GetDataFormRepository_SameProgId_ReturnsNewInstanceEachCall()
        {
            var provider = new FormRepositoryProvider();

            var first = provider.GetDataFormRepository(DataProgId);
            var second = provider.GetDataFormRepository(DataProgId);

            Assert.NotSame(first, second);
        }

        [Fact]
        [DisplayName("GetReportFormRepository 相同 ProgId 每次呼叫回傳新實例（非快取）")]
        public void GetReportFormRepository_SameProgId_ReturnsNewInstanceEachCall()
        {
            var provider = new FormRepositoryProvider();

            var first = provider.GetReportFormRepository(ReportProgId);
            var second = provider.GetReportFormRepository(ReportProgId);

            Assert.NotSame(first, second);
        }
    }
}
