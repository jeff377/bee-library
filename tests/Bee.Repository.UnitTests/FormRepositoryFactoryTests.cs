using System.ComponentModel;
using Bee.Repository.Form;
using Bee.Repository.Factories;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// 針對 <see cref="FormRepositoryFactory"/> 工廠方法的純邏輯測試。
    /// </summary>
    public class FormRepositoryFactoryTests
    {
        private const string DataProgId = "Employee";
        private const string ReportProgId = "SalesReport";

        [Fact]
        [DisplayName("CreateDataFormRepository 應回傳帶有指定 ProgId 的 DataFormRepository")]
        public void CreateDataFormRepository_ReturnsDataFormRepositoryWithProgId()
        {
            var provider = new FormRepositoryFactory();
            var repo = provider.CreateDataFormRepository(DataProgId);

            var typed = Assert.IsType<DataFormRepository>(repo);
            Assert.Equal(DataProgId, typed.ProgId);
        }

        [Fact]
        [DisplayName("CreateReportFormRepository 應回傳帶有指定 ProgId 的 ReportFormRepository")]
        public void CreateReportFormRepository_ReturnsReportFormRepositoryWithProgId()
        {
            var provider = new FormRepositoryFactory();
            var repo = provider.CreateReportFormRepository(ReportProgId);

            var typed = Assert.IsType<ReportFormRepository>(repo);
            Assert.Equal(ReportProgId, typed.ProgId);
        }

        [Fact]
        [DisplayName("CreateDataFormRepository 不同 ProgId 應回傳不同實例")]
        public void CreateDataFormRepository_DifferentProgIds_ReturnDifferentInstances()
        {
            var provider = new FormRepositoryFactory();

            var first = provider.CreateDataFormRepository("A");
            var second = provider.CreateDataFormRepository("B");

            Assert.NotSame(first, second);
            Assert.Equal("A", ((DataFormRepository)first).ProgId);
            Assert.Equal("B", ((DataFormRepository)second).ProgId);
        }

        [Fact]
        [DisplayName("CreateDataFormRepository 相同 ProgId 每次呼叫回傳新實例（非快取）")]
        public void CreateDataFormRepository_SameProgId_ReturnsNewInstanceEachCall()
        {
            var provider = new FormRepositoryFactory();

            var first = provider.CreateDataFormRepository(DataProgId);
            var second = provider.CreateDataFormRepository(DataProgId);

            Assert.NotSame(first, second);
        }

        [Fact]
        [DisplayName("CreateReportFormRepository 相同 ProgId 每次呼叫回傳新實例（非快取）")]
        public void CreateReportFormRepository_SameProgId_ReturnsNewInstanceEachCall()
        {
            var provider = new FormRepositoryFactory();

            var first = provider.CreateReportFormRepository(ReportProgId);
            var second = provider.CreateReportFormRepository(ReportProgId);

            Assert.NotSame(first, second);
        }
    }
}
