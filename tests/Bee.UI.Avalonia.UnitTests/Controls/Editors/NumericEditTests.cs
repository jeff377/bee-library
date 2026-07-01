using System.ComponentModel;
using Avalonia.Input;
using Avalonia.Media;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// Behaviour checks for <see cref="NumericEdit"/>: formatted display at rest, full-precision
    /// write-back (the rounded display form is never persisted), tolerance of invalid input, and
    /// right alignment.
    /// </summary>
    public class NumericEditTests
    {
        private static FormDataObject BuildDataObject()
        {
            var schema = new FormSchema("Order", "Order");
            var master = schema.Tables!.Add("Order", "Order");
            master.Fields!.Add("amount", "Amount", FieldDbType.Decimal);
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            return dataObject;
        }

        private static LayoutField AmountField(string numberFormat = "N2")
            => new() { FieldName = "amount", NumberFormat = numberFormat };

        // Commit is exercised via Enter (a KeyDown routed event), matching TextEditTests; the
        // LostFocus routed event carries a FocusChangedEventArgs that cannot be synthesised here.
        private static void Commit(NumericEdit editor)
            => editor.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });

        [Fact]
        [DisplayName("Bind 後依 NumberFormat 顯示格式化值（N2 → 兩位）")]
        public void Bind_DecimalField_DisplaysFormatted()
        {
            var dataObject = BuildDataObject();
            dataObject.SetField("amount", "12.3456");

            var editor = new NumericEdit();
            editor.Bind(dataObject, AmountField("N2"));

            Assert.Equal("12.35", editor.Text);
        }

        [Fact]
        [DisplayName("寫回為解析後的完整精度值，而非顯示的捨入值")]
        public void WriteBack_StoresFullPrecision_NotRoundedDisplay()
        {
            var dataObject = BuildDataObject();
            var editor = new NumericEdit();
            editor.Bind(dataObject, AmountField("N2"));

            // Simulate the user typing a higher-precision value than the display shows.
            editor.Text = "12.3456789";
            Commit(editor);

            // The bound field keeps full precision; the display rounding is never written back.
            Assert.Equal("12.3456789", dataObject.GetField("amount"));
        }

        [Fact]
        [DisplayName("Enter 提交寫回")]
        public void EnterKey_WritesBack()
        {
            var dataObject = BuildDataObject();
            var editor = new NumericEdit();
            editor.Bind(dataObject, AmountField("N2"));

            editor.Text = "50";
            Commit(editor);

            Assert.Equal("50", dataObject.GetField("amount"));
        }

        [Fact]
        [DisplayName("無效輸入保留上一個有效值，不寫回")]
        public void InvalidInput_KeepsPreviousValue()
        {
            var dataObject = BuildDataObject();
            dataObject.SetField("amount", "42.5");
            var editor = new NumericEdit();
            editor.Bind(dataObject, AmountField("N2"));

            editor.Text = "not-a-number";
            Commit(editor);

            // The invalid text is rejected: the field keeps its previous valid value.
            Assert.Equal("42.5", dataObject.GetField("amount"));
        }

        [Fact]
        [DisplayName("無 NumberFormat 時原值顯示（不格式化）")]
        public void Bind_NoNumberFormat_ShowsRawValue()
        {
            var dataObject = BuildDataObject();
            dataObject.SetField("amount", "7.5");
            var editor = new NumericEdit();
            editor.Bind(dataObject, AmountField(numberFormat: string.Empty));

            Assert.Equal("7.5", editor.Text);
        }

        [Fact]
        [DisplayName("NumericEdit 右對齊")]
        public void NumericEdit_IsRightAligned()
        {
            var editor = new NumericEdit();

            Assert.Equal(TextAlignment.Right, editor.TextAlignment);
        }

        // --- 多幣別 runtime 解析 ---

        private static CurrencySettings Currencies() =>
        [
            new CurrencyItem("USD", 0.01m, "$", "US Dollar"),
            new CurrencyItem("JPY", 1m, "¥", "Japanese Yen"),
        ];

        private static LayoutField AmountKindField()
            => new() { FieldName = "amount", NumberKind = NumberKind.Amount };

        [Fact]
        [DisplayName("設 CurrencySettings + 預設幣別 → 金額欄依幣別位數顯示（JPY 0 位）")]
        public void Amount_WithCurrency_FormatsByDefaultCurrency_Jpy()
        {
            var dataObject = BuildDataObject();
            dataObject.SetField("amount", "1234.567");
            var editor = new NumericEdit { CurrencySettings = Currencies(), DefaultCurrencyCode = "JPY" };

            editor.Bind(dataObject, AmountKindField());

            Assert.Equal("1,235", editor.Text); // JPY → 0 位
        }

        [Fact]
        [DisplayName("同資料改幣別 USD → 金額欄改顯 2 位")]
        public void Amount_WithCurrency_FormatsByDefaultCurrency_Usd()
        {
            var dataObject = BuildDataObject();
            dataObject.SetField("amount", "1234.567");
            var editor = new NumericEdit { CurrencySettings = Currencies(), DefaultCurrencyCode = "USD" };

            editor.Bind(dataObject, AmountKindField());

            Assert.Equal("1,234.57", editor.Text); // USD → 2 位
        }

        [Fact]
        [DisplayName("未設 CurrencySettings 時金額欄不做幣別解析（無 baked → 顯原值）")]
        public void Amount_NoCurrencySettings_ShowsRaw()
        {
            var dataObject = BuildDataObject();
            dataObject.SetField("amount", "1234.567");
            var editor = new NumericEdit(); // 未設 CurrencySettings

            editor.Bind(dataObject, AmountKindField());

            Assert.Equal("1234.567", editor.Text);
        }

        // --- 計量單位 runtime 解析 ---

        private static UnitSettings Units() =>
        [
            new UnitItem("PCS", 0, "count", "Pieces"),
            new UnitItem("KG", 3, "weight", "Kilogram"),
        ];

        private static FormDataObject BuildQtyDataObject()
        {
            var schema = new FormSchema("Order", "Order");
            var master = schema.Tables!.Add("Order", "Order");
            master.Fields!.Add("qty", "Qty", FieldDbType.Decimal);
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            return dataObject;
        }

        private static LayoutField QtyKindField()
            => new() { FieldName = "qty", NumberKind = NumberKind.Quantity, UnitField = "qty_uom" };

        [Fact]
        [DisplayName("設 UnitSettings + 預設單位 KG → 數量欄顯 3 位")]
        public void Quantity_WithUnit_FormatsByDefaultUnit_Kg()
        {
            var dataObject = BuildQtyDataObject();
            dataObject.SetField("qty", "12.345");
            var editor = new NumericEdit { UnitSettings = Units(), DefaultUnitCode = "KG" };

            editor.Bind(dataObject, QtyKindField());

            Assert.Equal("12.345", editor.Text); // KG → 3 位
        }

        [Fact]
        [DisplayName("同資料改單位 PCS → 數量欄改顯 0 位")]
        public void Quantity_WithUnit_FormatsByDefaultUnit_Pcs()
        {
            var dataObject = BuildQtyDataObject();
            dataObject.SetField("qty", "12.345");
            var editor = new NumericEdit { UnitSettings = Units(), DefaultUnitCode = "PCS" };

            editor.Bind(dataObject, QtyKindField());

            Assert.Equal("12", editor.Text); // PCS → 0 位
        }

        [Fact]
        [DisplayName("未設 UnitSettings 時數量欄不做單位解析（無 baked → 顯原值）")]
        public void Quantity_NoUnitSettings_ShowsRaw()
        {
            var dataObject = BuildQtyDataObject();
            dataObject.SetField("qty", "12.345");
            var editor = new NumericEdit(); // 未設 UnitSettings

            editor.Bind(dataObject, QtyKindField());

            Assert.Equal("12.345", editor.Text);
        }
    }
}
