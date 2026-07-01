using System.ComponentModel;
using Bee.UI.Avalonia.Controls;

namespace Bee.UI.Avalonia.UnitTests.Controls
{
    /// <summary>
    /// AmountColumnSummary：原幣欄全同幣才顯合計、混幣不顯（回 null）、本幣欄（欄內同幣）恆可加總。
    /// </summary>
    public class AmountColumnSummaryTests
    {
        [Fact]
        [DisplayName("全欄同幣 → 回合計")]
        public void TryComputeTotal_SameCurrency_ReturnsSum()
        {
            var total = AmountColumnSummary.TryComputeTotal(
            [
                (10.00m, "USD"), (20.50m, "USD"), (5.25m, "USD"),
            ]);

            Assert.Equal(35.75m, total);
        }

        [Fact]
        [DisplayName("混幣（USD+JPY）→ 回 null（不顯合計）")]
        public void TryComputeTotal_MixedCurrency_ReturnsNull()
        {
            var total = AmountColumnSummary.TryComputeTotal(
            [
                (10.00m, "USD"), (1000m, "JPY"),
            ]);

            Assert.Null(total);
        }

        [Fact]
        [DisplayName("本幣欄（欄內恆同幣）→ 恆回合計")]
        public void TryComputeTotal_HomeCurrencyColumn_AlwaysTotals()
        {
            // 本幣欄每列皆公司本幣（如 TWD）→ 單幣 → 恆可加總。
            var total = AmountColumnSummary.TryComputeTotal(
            [
                (300m, "TWD"), (31000m, "TWD"), (1650m, "TWD"),
            ]);

            Assert.Equal(32950m, total);
        }

        [Fact]
        [DisplayName("幣別碼比對不分大小寫")]
        public void TryComputeTotal_CaseInsensitiveCurrency()
        {
            var total = AmountColumnSummary.TryComputeTotal(
            [
                (10m, "usd"), (20m, "USD"),
            ]);

            Assert.Equal(30m, total);
        }

        [Fact]
        [DisplayName("空集合 → 回合計 0")]
        public void TryComputeTotal_Empty_ReturnsZero()
        {
            var total = AmountColumnSummary.TryComputeTotal([]);

            Assert.Equal(0m, total);
        }
    }
}
