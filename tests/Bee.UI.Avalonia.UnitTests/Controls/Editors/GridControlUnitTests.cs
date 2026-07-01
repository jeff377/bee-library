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
    /// GridControl per-cell 單位感知：數量欄依該列單位欄（UNIT）當前值解析位數；
    /// 同欄不同列不同單位 → 不同位數；列單位空退欄級 baked 格式；無 UnitSettings 退 baked。
    /// </summary>
    public class GridControlUnitTests
    {
        private static UnitSettings Units() =>
        [
            new UnitItem("PCS", 0, "count", "Pieces"),
            new UnitItem("KG", 3, "weight", "Kilogram"),
            new UnitItem("M", 2, "length", "Metre"),
        ];

        private static string QtyCellText(GridControl grid, DataTable table, int rowIndex, string bakedFormat = "")
        {
            var column = new LayoutColumn("qty", "數量", ControlType.NumericEdit)
            {
                NumberKind = NumberKind.Quantity,
                UnitField = "qty_uom",
                NumberFormat = bakedFormat,
            };
            var method = typeof(GridControl).GetMethod(
                "BuildInteractiveCell", BindingFlags.NonPublic | BindingFlags.Instance);
            var cell = (Control)method!.Invoke(grid, new object?[] { table.DefaultView[rowIndex], column })!;
            return Assert.IsType<TextBlock>(cell).Text ?? string.Empty;
        }

        private static DataTable QtyTable(params (decimal Qty, string Unit)[] rows)
        {
            var table = new DataTable("OrderLine");
            table.Columns.Add("qty", typeof(decimal));
            table.Columns.Add("qty_uom", typeof(string));
            foreach (var (qty, unit) in rows)
                table.Rows.Add(qty, unit);
            return table;
        }

        private static GridControl BindGrid(DataTable table, UnitSettings? units)
        {
            var layout = new LayoutGrid("OrderLine", "明細");
            layout.Columns!.Add(new LayoutColumn("qty", "數量", ControlType.NumericEdit)
            {
                NumberKind = NumberKind.Quantity,
                UnitField = "qty_uom",
            });
            layout.Columns.Add(new LayoutColumn("qty_uom", "單位", ControlType.TextEdit));

            var grid = new GridControl { UnitSettings = units };
            grid.Bind(layout, table);
            return grid;
        }

        [Fact]
        [DisplayName("同欄 PCS/KG/M 三列 → 各列位數依單位（0/3/2）")]
        public void QtyColumn_PerRowUnit_DifferentDecimals()
        {
            var table = QtyTable((12.345m, "PCS"), (12.345m, "KG"), (12.345m, "M"));
            var grid = BindGrid(table, Units());

            Assert.Equal("12", QtyCellText(grid, table, 0));      // PCS → 0 位
            Assert.Equal("12.345", QtyCellText(grid, table, 1));  // KG → 3 位
            Assert.Equal("12.35", QtyCellText(grid, table, 2));   // M → 2 位（12.345 → 12.35）
        }

        [Fact]
        [DisplayName("列單位空時退欄級 baked 格式（不做單位解析）")]
        public void QtyColumn_EmptyRowUnit_UsesBakedFormat()
        {
            var table = QtyTable((12.345m, string.Empty));
            var grid = BindGrid(table, Units());

            // 列單位空 → 不解析 → 用欄級 baked（此處 N0）。
            Assert.Equal("12", QtyCellText(grid, table, 0, bakedFormat: "N0"));
        }

        [Fact]
        [DisplayName("未設 UnitSettings 時數量欄用欄級 baked 格式（單位感知關閉）")]
        public void QtyColumn_NoUnitSettings_UsesBakedFormat()
        {
            var table = QtyTable((12.345m, "KG"));
            var grid = BindGrid(table, units: null);

            Assert.Equal("12.35", QtyCellText(grid, table, 0, bakedFormat: "N2"));
        }
    }
}
