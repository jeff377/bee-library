using Avalonia.Controls;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Definition.Identity;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.DataObjects;
using Avalonia.DemoCenter.Modules.DataEditors;

namespace Avalonia.DemoCenter.Modules.Grids
{
    /// <summary>
    /// Numeric formatting: an order-line grid whose numeric columns each carry a
    /// <see cref="NumberKind"/>, so <c>NumberFormatResolver.ResolveFormat</c> gives each its own
    /// decimal places (quantity N0, unit price N4, amount N2, weight N3, discount P2). The company
    /// toggle switches between framework defaults and company overrides, re-resolving the formats;
    /// double-clicking a numeric cell edits it in place with <c>NumericEdit</c> (full precision on
    /// focus, formatted on blur).
    /// </summary>
    public sealed class NumberFormatModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "Grid";

        /// <inheritdoc/>
        public override string Title => "數值格式化";

        /// <inheritdoc/>
        public override string Description =>
            "每欄依 NumberKind 解析顯示位數（數量 N0 / 單價 N4 / 金額 N2 / 重量 N3 / 折扣 P2）；切換公司走公司覆寫位數即時改變；雙擊數值 cell 以 NumericEdit 就地編輯（focus 顯示完整精度、blur 依格式，顯示捨入不回寫）。";

        // Numeric order-line columns: (data field, caption, semantic kind).
        private static readonly (string Field, string Caption, NumberKind Kind)[] NumericColumns =
        {
            ("quantity", "數量", NumberKind.Quantity),
            ("unit_price", "單價", NumberKind.UnitPrice),
            ("amount", "金額", NumberKind.Amount),
            ("gross_weight", "重量(kg)", NumberKind.Weight),
            ("discount_pct", "折扣", NumberKind.Percent),
        };

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var data = BuildData();
            var grid = new GridControl { MinHeight = 240, EditMode = GridEditMode.InCell };
            grid.Bind(data, BuildLayout(company: null));

            var useOverrides = false;
            var toggle = new Button { Content = CompanyLabel(useOverrides) };
            toggle.Click += (_, _) =>
            {
                useOverrides = !useOverrides;
                toggle.Content = CompanyLabel(useOverrides);
                grid.Bind(data, BuildLayout(useOverrides ? CompanyWithOverrides() : null));
            };

            return new ScrollViewer
            {
                Content = DataEditorParts.Section(
                    "數值格式化（NumberKind → 位數）",
                    "同一批資料，每欄依 NumberKind 解析出不同位數。切換公司 → 走公司覆寫位數（單價 4→2、折扣 P2→P4、數量 0→2）即時重算。單價以完整精度保存、顯示捨入不回寫。",
                    toggle, grid),
            };
        }

        private static string CompanyLabel(bool useOverrides)
            => useOverrides ? "公司 B（覆寫位數）— 點此切回 A" : "公司 A（框架預設）— 點此切到 B";

        private static LayoutGrid BuildLayout(CompanyInfo? company)
        {
            var layout = new LayoutGrid("OrderLine", "訂單明細");
            layout.Columns!.Add(new LayoutColumn("product", "品名", ControlType.TextEdit));
            foreach (var (field, caption, kind) in NumericColumns)
            {
                layout.Columns.Add(new LayoutColumn(field, caption, ControlType.NumericEdit)
                {
                    NumberKind = kind,
                    NumberFormat = NumberFormatResolver.ResolveFormat(kind, company),
                });
            }
            return layout;
        }

        private static CompanyInfo CompanyWithOverrides()
        {
            var company = new CompanyInfo { CompanyId = "B", CompanyName = "公司 B" };
            company.NumberFormats.Add(new NumberFormatItem(NumberKind.UnitPrice, 2));
            company.NumberFormats.Add(new NumberFormatItem(NumberKind.Percent, 4));
            company.NumberFormats.Add(new NumberFormatItem(NumberKind.Quantity, 2));
            return company;
        }

        private static FormDataObject BuildData()
        {
            var schema = new FormSchema("Order", "訂單");
            schema.Tables!.Add("Order", "訂單").Fields!.Add("order_no", "單號", FieldDbType.String);

            var line = schema.Tables.Add("OrderLine", "明細");
            line.Fields!.Add("product", "品名", FieldDbType.String);
            foreach (var (field, caption, kind) in NumericColumns)
                line.Fields!.Add(new FormField(field, caption, FieldDbType.Decimal) { NumberKind = kind });

            var data = new FormDataObject(schema);
            data.InitializeNewMaster();
            data.SetField("order_no", "SO-001");

            var lines = data.DataSet.Tables["OrderLine"]!;
            // Columns are appended in schema order: product, quantity, unit_price, amount,
            // gross_weight, discount_pct. Percent is stored as a fraction (0.055 renders "5.50%").
            // unit_price carries more precision than N4 shows, to demonstrate preserve-on-display.
            lines.Rows.Add("Widget", 3m, 12.3456789m, 37.04m, 1.256m, 0.055m);
            lines.Rows.Add("Gadget", 10m, 0.9999m, 10.00m, 0.5m, 0.1275m);
            return data;
        }
    }
}
