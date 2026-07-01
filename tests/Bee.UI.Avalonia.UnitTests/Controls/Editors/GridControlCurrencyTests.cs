using System.ComponentModel;
using System.Data;
using System.Reflection;
using Avalonia.Controls;
using Bee.Definition;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.UI.Avalonia.Controls;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// GridControl per-cell 幣別感知：金額欄依該列幣別欄（CUKY）當前值解析位數；
    /// 同欄不同列不同幣別 → 不同位數；列幣別空退 grid 預設幣別；無 CurrencySettings 時退欄級 baked 格式。
    /// </summary>
    public class GridControlCurrencyTests
    {
        private static CurrencySettings Currencies() =>
        [
            new CurrencyItem("USD", 0.01m, "$", "US Dollar"),
            new CurrencyItem("JPY", 1m, "¥", "Japanese Yen"),
            new CurrencyItem("BHD", 0.001m, "BD", "Bahraini Dinar"),
        ];

        // Renders the amount cell for a row via the read-only interactive cell (a TextBlock whose
        // Text is the currency-aware FormatCellForColumn result).
        private static string AmountCellText(GridControl grid, DataTable table, int rowIndex, string bakedFormat = "")
        {
            var column = new LayoutColumn("amount", "金額", ControlType.NumericEdit)
            {
                NumberKind = NumberKind.Amount,
                CurrencyField = "sys_currency",
                NumberFormat = bakedFormat,
            };
            var method = typeof(GridControl).GetMethod(
                "BuildInteractiveCell", BindingFlags.NonPublic | BindingFlags.Instance);
            var cell = (Control)method!.Invoke(grid, new object?[] { table.DefaultView[rowIndex], column })!;
            return Assert.IsType<TextBlock>(cell).Text ?? string.Empty;
        }

        private static DataTable AmountTable(params (decimal Amount, string Currency)[] rows)
        {
            var table = new DataTable("OrderLine");
            table.Columns.Add("amount", typeof(decimal));
            table.Columns.Add("sys_currency", typeof(string));
            foreach (var (amount, currency) in rows)
                table.Rows.Add(amount, currency);
            return table;
        }

        private static GridControl BindGrid(DataTable table, CurrencySettings? currencies, string defaultCode = "")
        {
            var layout = new LayoutGrid("OrderLine", "明細");
            layout.Columns!.Add(new LayoutColumn("amount", "金額", ControlType.NumericEdit)
            {
                NumberKind = NumberKind.Amount,
                CurrencyField = "sys_currency",
            });
            layout.Columns.Add(new LayoutColumn("sys_currency", "幣別", ControlType.TextEdit));

            var grid = new GridControl { CurrencySettings = currencies, DefaultCurrencyCode = defaultCode };
            grid.Bind(layout, table);
            return grid;
        }

        [Fact]
        [DisplayName("同欄 USD/JPY/BHD 三列 → 各列位數依幣別（2/0/3）")]
        public void AmountColumn_PerRowCurrency_DifferentDecimals()
        {
            var table = AmountTable((1234.567m, "USD"), (1234.567m, "JPY"), (1234.567m, "BHD"));
            var grid = BindGrid(table, Currencies());

            Assert.Equal("1,234.57", AmountCellText(grid, table, 0));   // USD → 2 位
            Assert.Equal("1,235", AmountCellText(grid, table, 1));      // JPY → 0 位
            Assert.Equal("1,234.567", AmountCellText(grid, table, 2));  // BHD → 3 位
        }

        [Fact]
        [DisplayName("幣別解析勝過欄級 baked 格式（JPY 列忽略 N2、顯 0 位）")]
        public void AmountColumn_CurrencyResolution_OverridesBakedFormat()
        {
            var table = AmountTable((1234.567m, "JPY"));
            var grid = BindGrid(table, Currencies());

            // baked "N2" 應被幣別解析（JPY 0 位）覆蓋。
            Assert.Equal("1,235", AmountCellText(grid, table, 0, bakedFormat: "N2"));
        }

        [Fact]
        [DisplayName("列幣別欄為空時退 grid 預設幣別（master 文件幣別 / 公司本幣）")]
        public void AmountColumn_EmptyRowCurrency_FallsBackToDefaultCurrencyCode()
        {
            var table = AmountTable((1234.567m, string.Empty));
            var grid = BindGrid(table, Currencies(), defaultCode: "JPY");

            Assert.Equal("1,235", AmountCellText(grid, table, 0)); // 退 JPY → 0 位
        }

        [Fact]
        [DisplayName("未設 CurrencySettings 時金額欄用欄級 baked 格式（幣別感知關閉）")]
        public void AmountColumn_NoCurrencySettings_UsesBakedFormat()
        {
            var table = AmountTable((1234.567m, "JPY"));
            var grid = BindGrid(table, currencies: null);

            // 欄級 baked 格式維持（此處給 N2）；幣別感知關閉。
            Assert.Equal("1,234.57", AmountCellText(grid, table, 0, bakedFormat: "N2"));
        }
    }
}
