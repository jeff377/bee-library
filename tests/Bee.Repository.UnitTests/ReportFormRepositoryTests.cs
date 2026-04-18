using System.ComponentModel;
using Bee.Repository.Form;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// 針對 <see cref="ReportFormRepository"/> 建構子屬性賦值的純邏輯測試。
    /// </summary>
    public class ReportFormRepositoryTests
    {
        [Theory]
        [InlineData("SalesReport")]
        [InlineData("InventoryReport")]
        [InlineData("CustomerLedger")]
        [DisplayName("ReportFormRepository 建構子應正確設定 ProgId")]
        public void Constructor_SetsProgId(string progId)
        {
            var repo = new ReportFormRepository(progId);
            Assert.Equal(progId, repo.ProgId);
        }

        [Fact]
        [DisplayName("ReportFormRepository 建構子目前未防護空白 ProgId，依現況原樣儲存")]
        public void Constructor_EmptyProgId_StoresAsIs()
        {
            var repo = new ReportFormRepository(string.Empty);
            Assert.Equal(string.Empty, repo.ProgId);
        }
    }
}
