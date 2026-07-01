using System.Globalization;
using Avalonia.Controls;
using Bee.Base;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.DataObjects;
using Avalonia.DemoCenter.Modules.DataEditors;

namespace Avalonia.DemoCenter.Modules.Grids
{
    /// <summary>
    /// Multi-unit quantities: an order-line grid where the <c>qty</c> column (NumberKind Quantity)
    /// resolves its decimals per row from that row's <c>qty_uom</c> unit field via
    /// <c>GridControl.UnitSettings</c> — PCS shows 0 places, KG 3, M 2. Same stored values, different
    /// decimals per row. A manual footer uses <c>AmountColumnSummary</c>: the quantity total shows only
    /// when all rows share one unit (mixed → no total), mirroring the mixed-currency rule.
    /// </summary>
    public sealed class MultiUnitModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "Grid";

        /// <inheritdoc/>
        public override string Title => "多單位數量";

        /// <inheritdoc/>
        public override string Description =>
            "數量欄依該列單位欄（qty_uom）解析位數（PCS 0 / KG 3 / M 2）；切換單位 → 同一批數量值即時改位數。數量合計：全同單位才顯、混單位不顯。";

        // Curated client unit master (the subset this demo uses).
        private static UnitSettings BuildUnits() =>
        [
            new UnitItem("PCS", 0, "count", "Pieces"),
            new UnitItem("KG", 3, "weight", "Kilogram"),
            new UnitItem("M", 2, "length", "Metre"),
        ];

        private static readonly (string Product, decimal Qty, string Unit)[] Lines =
        {
            ("Bolt", 12.345m, "PCS"),
            ("Steel", 12.345m, "KG"),
            ("Cable", 12.345m, "M"),
        };

        private static readonly string[] s_unitModes = ["混單位（原樣）", "全部 KG", "全部 PCS"];

        private readonly UnitSettings _units = BuildUnits();

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var data = BuildData();
            var table = data.DataSet.Tables["OrderLine"]!;

            var grid = new GridControl
            {
                MinHeight = 200,
                UnitSettings = _units,
            };
            grid.Bind(BuildLayout(), table);

            var qtyTotal = new TextBlock();
            void RefreshTotal() => qtyTotal.Text = "數量合計：" + FormatQtyTotal(table);
            RefreshTotal();

            var modeIndex = 0;
            var toggle = new Button { Content = "計量單位：" + s_unitModes[modeIndex] };
            toggle.Click += (_, _) =>
            {
                modeIndex = (modeIndex + 1) % s_unitModes.Length;
                toggle.Content = "計量單位：" + s_unitModes[modeIndex];
                ApplyUnit(table, modeIndex);
                grid.RefreshRows();
                RefreshTotal();
            };

            return new ScrollViewer
            {
                Content = DataEditorParts.Section(
                    "多單位數量（數量欄依列單位解析位數）",
                    "qty 欄綁 qty_uom（UNIT）；切換單位 → 同批數量值改位數。混單位不顯合計、同單位才顯。",
                    toggle, grid, qtyTotal),
            };
        }

        private static void ApplyUnit(System.Data.DataTable table, int modeIndex)
        {
            for (int i = 0; i < table.Rows.Count; i++)
            {
                table.Rows[i]["qty_uom"] = modeIndex switch
                {
                    1 => "KG",
                    2 => "PCS",
                    _ => Lines[i].Unit,
                };
            }
        }

        private string FormatQtyTotal(System.Data.DataTable table)
        {
            var cells = table.Rows.Cast<System.Data.DataRow>()
                .Select(r => (ValueUtilities.CDecimal(r["qty"]), ValueUtilities.CStr(r["qty_uom"])));
            var total = AmountColumnSummary.TryComputeTotal(cells);
            if (total is null) { return "— 混單位，不合計"; }

            string code = ValueUtilities.CStr(table.Rows[0]["qty_uom"]);
            string format = NumberFormatResolver.ResolveFormat(
                NumberKind.Quantity, new RoundingContext { UnitSettings = _units }, code);
            return total.Value.ToString(format, CultureInfo.InvariantCulture) + " " + code;
        }

        private static LayoutGrid BuildLayout()
        {
            var layout = new LayoutGrid("OrderLine", "訂單明細");
            layout.Columns!.Add(new LayoutColumn("product", "品名", ControlType.TextEdit));
            layout.Columns.Add(new LayoutColumn("qty", "數量", ControlType.NumericEdit)
            {
                NumberKind = NumberKind.Quantity,
                UnitField = "qty_uom",
            });
            layout.Columns.Add(new LayoutColumn("qty_uom", "單位", ControlType.TextEdit));
            return layout;
        }

        private static FormDataObject BuildData()
        {
            var schema = new FormSchema("Order", "訂單");
            schema.Tables!.Add("Order", "訂單").Fields!.Add("order_no", "單號", FieldDbType.String);

            var line = schema.Tables.Add("OrderLine", "明細");
            line.Fields!.Add("product", "品名", FieldDbType.String);
            line.Fields!.Add(new FormField("qty", "數量", FieldDbType.Decimal) { NumberKind = NumberKind.Quantity, UnitField = "qty_uom" });
            line.Fields!.Add("qty_uom", "單位", FieldDbType.String);

            var data = new FormDataObject(schema);
            data.InitializeNewMaster();
            data.SetField("order_no", "SO-200");

            var lines = data.DataSet.Tables["OrderLine"]!;
            foreach (var (product, qty, unit) in Lines)
                lines.Rows.Add(product, qty, unit);
            return data;
        }
    }
}
