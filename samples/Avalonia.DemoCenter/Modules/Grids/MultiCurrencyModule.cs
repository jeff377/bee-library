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
    /// Multi-currency amounts: an order-line grid where the <c>amount</c> column (NumberKind Amount)
    /// resolves its decimals per row from that row's <c>sys_currency</c> field via
    /// <c>GridControl.CurrencySettings</c> — USD shows 2 places, JPY 0, BHD 3. The document-currency
    /// toggle rewrites every row's currency and re-renders (same stored values, different decimals).
    /// A manual footer uses <c>AmountColumnSummary</c>: the original-currency total shows only when all
    /// rows share one currency (mixed → no total), while the home-currency total always shows.
    /// </summary>
    public sealed class MultiCurrencyModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "Grid";

        /// <inheritdoc/>
        public override string Title => "多幣別金額";

        /// <inheritdoc/>
        public override string Description =>
            "金額欄依該列幣別欄（sys_currency）解析位數（USD 2 / JPY 0 / BHD 3）；切換單據貨幣 → 同一批金額值即時改位數。原幣欄合計：全同幣才顯、混幣不顯；本幣欄（TWD）恆顯合計。";

        // Curated client currency master (the subset this demo uses).
        private static CurrencySettings BuildCurrencies() =>
        [
            new CurrencyItem("USD", 0.01m, "$", "US Dollar"),
            new CurrencyItem("JPY", 1m, "¥", "Japanese Yen"),
            new CurrencyItem("BHD", 0.001m, "BD", "Bahraini Dinar"),
            new CurrencyItem("TWD", 0.01m, "NT$", "New Taiwan Dollar"),
        ];

        // (currency, home_amount in TWD) seeded per line; amount is the original-currency value.
        private static readonly (string Product, decimal Amount, string Currency, decimal HomeAmount)[] Lines =
        {
            ("Widget", 1234.567m, "USD", 39505m),
            ("Gadget", 5000.4m, "JPY", 1050m),
            ("Gizmo", 250.125m, "BHD", 21260m),
        };

        private static readonly string[] s_documentModes = ["混幣（原樣）", "全部 USD", "全部 JPY"];

        private readonly CurrencySettings _currencies = BuildCurrencies();

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var data = BuildData();
            var table = data.DataSet.Tables["OrderLine"]!;

            var grid = new GridControl
            {
                MinHeight = 200,
                CurrencySettings = _currencies,
            };
            grid.Bind(BuildLayout(), table);

            var originalTotal = new TextBlock();
            var homeTotal = new TextBlock();
            void RefreshTotals()
            {
                originalTotal.Text = "原幣合計：" + FormatOriginalTotal(table);
                homeTotal.Text = "本幣合計（TWD）：" + FormatHomeTotal(table);
            }
            RefreshTotals();

            var modeIndex = 0;
            var toggle = new Button { Content = "單據貨幣：" + s_documentModes[modeIndex] };
            toggle.Click += (_, _) =>
            {
                modeIndex = (modeIndex + 1) % s_documentModes.Length;
                toggle.Content = "單據貨幣：" + s_documentModes[modeIndex];
                ApplyDocumentCurrency(table, modeIndex);
                grid.RefreshRows();
                RefreshTotals();
            };

            return new ScrollViewer
            {
                Content = DataEditorParts.Section(
                    "多幣別金額（金額欄依列幣別解析位數）",
                    "amount 欄綁 sys_currency（CUKY）；切換單據貨幣 → 同批金額值改位數。原幣欄混幣不顯合計、同幣才顯；本幣欄恆顯。",
                    toggle, grid, originalTotal, homeTotal),
            };
        }

        private static void ApplyDocumentCurrency(System.Data.DataTable table, int modeIndex)
        {
            // Mode 0 = original per-row currencies; 1 = all USD; 2 = all JPY.
            for (int i = 0; i < table.Rows.Count; i++)
            {
                table.Rows[i]["sys_currency"] = modeIndex switch
                {
                    1 => "USD",
                    2 => "JPY",
                    _ => Lines[i].Currency,
                };
            }
        }

        private string FormatOriginalTotal(System.Data.DataTable table)
        {
            var cells = table.Rows.Cast<System.Data.DataRow>()
                .Select(r => (ValueUtilities.CDecimal(r["amount"]), ValueUtilities.CStr(r["sys_currency"])));
            var total = AmountColumnSummary.TryComputeTotal(cells);
            if (total is null) { return "— 混幣，不合計"; }

            // All rows share one currency here → format by it.
            string code = ValueUtilities.CStr(table.Rows[0]["sys_currency"]);
            string format = NumberFormatResolver.ResolveFormat(
                NumberKind.Amount, new RoundingContext { CurrencySettings = _currencies }, code);
            return total.Value.ToString(format, CultureInfo.InvariantCulture) + " " + code;
        }

        private string FormatHomeTotal(System.Data.DataTable table)
        {
            var cells = table.Rows.Cast<System.Data.DataRow>()
                .Select(r => (ValueUtilities.CDecimal(r["home_amount"]), "TWD"));
            var total = AmountColumnSummary.TryComputeTotal(cells) ?? 0m;
            string format = NumberFormatResolver.ResolveFormat(
                NumberKind.Amount, new RoundingContext { CurrencySettings = _currencies }, "TWD");
            return total.ToString(format, CultureInfo.InvariantCulture) + " TWD";
        }

        private static LayoutGrid BuildLayout()
        {
            var layout = new LayoutGrid("OrderLine", "訂單明細");
            layout.Columns!.Add(new LayoutColumn("product", "品名", ControlType.TextEdit));
            layout.Columns.Add(new LayoutColumn("amount", "金額(原幣)", ControlType.NumericEdit)
            {
                NumberKind = NumberKind.Amount,
                CurrencyField = "sys_currency",
            });
            layout.Columns.Add(new LayoutColumn("sys_currency", "幣別", ControlType.TextEdit));
            layout.Columns.Add(new LayoutColumn("home_amount", "金額(本幣)", ControlType.NumericEdit)
            {
                NumberKind = NumberKind.Amount,
                CurrencyField = "local_currency",
            });
            layout.Columns.Add(new LayoutColumn("local_currency", "本幣", ControlType.TextEdit));
            return layout;
        }

        private static FormDataObject BuildData()
        {
            var schema = new FormSchema("Order", "訂單");
            schema.Tables!.Add("Order", "訂單").Fields!.Add("order_no", "單號", FieldDbType.String);

            var line = schema.Tables.Add("OrderLine", "明細");
            line.Fields!.Add("product", "品名", FieldDbType.String);
            line.Fields!.Add(new FormField("amount", "金額(原幣)", FieldDbType.Decimal) { NumberKind = NumberKind.Amount, CurrencyField = "sys_currency" });
            line.Fields!.Add("sys_currency", "幣別", FieldDbType.String);
            line.Fields!.Add(new FormField("home_amount", "金額(本幣)", FieldDbType.Decimal) { NumberKind = NumberKind.Amount, CurrencyField = "local_currency" });
            line.Fields!.Add("local_currency", "本幣", FieldDbType.String);

            var data = new FormDataObject(schema);
            data.InitializeNewMaster();
            data.SetField("order_no", "SO-100");

            var lines = data.DataSet.Tables["OrderLine"]!;
            foreach (var (product, amount, currency, homeAmount) in Lines)
                lines.Rows.Add(product, amount, currency, homeAmount, "TWD");
            return data;
        }
    }
}
