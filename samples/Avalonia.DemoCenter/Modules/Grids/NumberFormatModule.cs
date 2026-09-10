using Avalonia.Controls;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Definition.Identity;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.DataObjects;
using Avalonia.DemoCenter.Modules.DataEditors;

namespace Avalonia.DemoCenter.Modules.Grids
{
    /// <summary>
    /// Numeric formatting: an order-line grid whose numeric columns each carry a
    /// <see cref="NumberKind"/>, so each resolves its own decimal places (unit price N4, amount N2,
    /// discount P2). Quantity and weight bind a unit field and resolve per row from that unit through
    /// <c>GridControl.UnitSettings</c> (PCS 0, KG 3), never from the company. The company toggle
    /// switches between framework defaults and company overrides, re-resolving the company-sourced
    /// formats; double-clicking a numeric cell edits it in place with <c>NumericEdit</c> (full precision
    /// on focus, formatted on blur).
    /// </summary>
    public sealed class NumberFormatModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "Grid";

        /// <inheritdoc/>
        public override string Title => "數值格式化";

        /// <inheritdoc/>
        public override string Description =>
            "每欄依 NumberKind 解析顯示位數（單價 N4 / 金額 N2 / 折扣 P2）；數量與重量綁單位欄、依該列單位解析（PCS 0 / KG 3），不隨公司變；切換公司走公司覆寫位數即時改變；雙擊數值 cell 以 NumericEdit 就地編輯（focus 顯示完整精度、blur 依格式，顯示捨入不回寫）。";

        // Numeric order-line columns: (data field, caption, semantic kind, unit field). Quantity and
        // weight must bind a unit field; the other kinds take none.
        private static readonly (string Field, string Caption, NumberKind Kind, string UnitField)[] NumericColumns =
        {
            ("quantity", "數量", NumberKind.Quantity, "quantity_uom"),
            ("unit_price", "單價", NumberKind.UnitPrice, ""),
            ("amount", "金額", NumberKind.Amount, ""),
            ("gross_weight", "重量", NumberKind.Weight, "weight_uom"),
            ("discount_pct", "折扣", NumberKind.Percent, ""),
        };

        // Curated client unit master (the subset this demo uses).
        private static UnitSettings BuildUnits() =>
        [
            new UnitItem("PCS", 0, "count", "Pieces"),
            new UnitItem("KG", 3, "weight", "Kilogram"),
        ];

        private readonly UnitSettings _units = BuildUnits();

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var data = BuildData();
            var grid = new GridControl { MinHeight = 240, EditMode = GridEditMode.InCell, UnitSettings = _units };
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
                    "同一批資料，每欄依 NumberKind 解析出不同位數。切換公司 → 走公司覆寫位數（單價 4→2、折扣 P2→P4）即時重算；數量與重量跟該列單位走，不受公司影響。單價以完整精度保存、顯示捨入不回寫。",
                    toggle, grid),
            };
        }

        private static string CompanyLabel(bool useOverrides)
            => useOverrides ? "公司 B（覆寫位數）— 點此切回 A" : "公司 A（框架預設）— 點此切到 B";

        private static LayoutGrid BuildLayout(CompanyInfo? company)
        {
            var layout = new LayoutGrid("OrderLine", "訂單明細");
            layout.Columns!.Add(new LayoutColumn("product", "品名", ControlType.TextEdit));
            foreach (var (field, caption, kind, unitField) in NumericColumns)
            {
                layout.Columns.Add(new LayoutColumn(field, caption, ControlType.NumericEdit)
                {
                    NumberKind = kind,
                    UnitField = unitField,
                    NumberFormat = NumberFormatResolver.ResolveFormat(kind, company),
                });
                if (!string.IsNullOrEmpty(unitField))
                    layout.Columns.Add(new LayoutColumn(unitField, "單位", ControlType.TextEdit));
            }
            return layout;
        }

        private static CompanyInfo CompanyWithOverrides()
        {
            var company = new CompanyInfo { CompanyId = "B", CompanyName = "公司 B", DefaultCurrency = "USD" };
            company.NumberFormats.Add(new NumberFormatItem(NumberKind.UnitPrice, 2));
            company.NumberFormats.Add(new NumberFormatItem(NumberKind.Percent, 4));
            return company;
        }

        private static FormDataObject BuildData()
        {
            var schema = new FormSchema("Order", "訂單");
            schema.Tables!.Add("Order", "訂單").Fields!.Add("order_no", "單號", FieldDbType.String);

            var line = schema.Tables.Add("OrderLine", "明細");
            line.Fields!.Add("product", "品名", FieldDbType.String);
            foreach (var (field, caption, kind, unitField) in NumericColumns)
                line.Fields!.Add(new FormField(field, caption, FieldDbType.Decimal) { NumberKind = kind, UnitField = unitField });
            line.Fields!.Add("quantity_uom", "數量單位", FieldDbType.String);
            line.Fields!.Add("weight_uom", "重量單位", FieldDbType.String);

            var data = new FormDataObject(schema);
            data.InitializeNewMaster();
            data.SetField("order_no", "SO-001");

            var lines = data.DataSet.Tables["OrderLine"]!;
            // Columns are appended in schema order: product, quantity, unit_price, amount,
            // gross_weight, discount_pct, quantity_uom, weight_uom. Percent is stored as a fraction
            // (0.055 renders "5.50%"). unit_price carries more precision than N4 shows, to demonstrate
            // preserve-on-display.
            lines.Rows.Add("Widget", 3m, 12.3456789m, 37.04m, 1.256m, 0.055m, "PCS", "KG");
            lines.Rows.Add("Gadget", 10m, 0.9999m, 10.00m, 0.5m, 0.1275m, "PCS", "KG");
            return data;
        }
    }
}
