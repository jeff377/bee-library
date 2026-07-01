using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Identity;

namespace Bee.Definition.UnitTests.Forms
{
    /// <summary>
    /// NumberFormatApplier.Bake：依公司位數對 FormSchema 數值欄 bake 顯示格式；
    /// explicit NumberFormat 優先、None 略過、company 位數反映於格式。
    /// </summary>
    public class NumberFormatApplierTests
    {
        private static FormSchema SchemaWith(params FormField[] fields)
        {
            var schema = new FormSchema("Order", "訂單");
            var table = schema.Tables!.Add("Order", "訂單");
            foreach (var f in fields)
                table.Fields!.Add(f);
            return schema;
        }

        private static CompanyInfo CompanyWith(params NumberFormatItem[] overrides)
        {
            var company = new CompanyInfo { CompanyId = "C001" };
            foreach (var item in overrides)
                company.NumberFormats.Add(item);
            return company;
        }

        [Fact]
        [DisplayName("Bake 對 Company/SystemFixed 欄套用框架預設格式；Currency（金額）不 bake")]
        public void Bake_NullCompany_FrameworkFormat()
        {
            var schema = SchemaWith(
                new FormField("disc", "折扣", FieldDbType.Decimal) { NumberKind = NumberKind.Percent },
                new FormField("rate", "匯率", FieldDbType.Decimal) { NumberKind = NumberKind.ExchangeRate },
                new FormField("amount", "金額", FieldDbType.Decimal) { NumberKind = NumberKind.Amount });

            NumberFormatApplier.Bake(schema, null);

            Assert.Equal("P2", schema.Tables!["Order"].Fields!["disc"].NumberFormat);
            Assert.Equal("N5", schema.Tables!["Order"].Fields!["rate"].NumberFormat);
            // Currency amounts resolve at runtime by their currency, so they are not baked.
            Assert.Equal(string.Empty, schema.Tables!["Order"].Fields!["amount"].NumberFormat);
        }

        [Fact]
        [DisplayName("Bake 金額欄不 bake，且無 CurrencyField 時繼承主檔幣別欄")]
        public void Bake_AmountField_NotBaked_InheritsMasterCurrencyField()
        {
            var schema = SchemaWith(new FormField("amount", "金額", FieldDbType.Decimal) { NumberKind = NumberKind.Amount });
            schema.CurrencyField = "sys_currency";

            NumberFormatApplier.Bake(schema, null);

            var field = schema.Tables!["Order"].Fields!["amount"];
            Assert.Equal(string.Empty, field.NumberFormat);
            Assert.Equal("sys_currency", field.CurrencyField);
        }

        [Fact]
        [DisplayName("Bake 金額欄已指定 CurrencyField 時不被主檔幣別欄覆蓋")]
        public void Bake_AmountField_ExplicitCurrencyField_NotOverwritten()
        {
            var schema = SchemaWith(new FormField("home_amount", "本幣金額", FieldDbType.Decimal)
            {
                NumberKind = NumberKind.Amount,
                CurrencyField = "local_currency",
            });
            schema.CurrencyField = "sys_currency";

            NumberFormatApplier.Bake(schema, null);

            Assert.Equal("local_currency", schema.Tables!["Order"].Fields!["home_amount"].CurrencyField);
        }

        [Fact]
        [DisplayName("Bake 主檔無幣別欄時金額欄 CurrencyField 維持空")]
        public void Bake_AmountField_NoMasterCurrencyField_LeavesEmpty()
        {
            var schema = SchemaWith(new FormField("amount", "金額", FieldDbType.Decimal) { NumberKind = NumberKind.Amount });

            NumberFormatApplier.Bake(schema, null);

            Assert.Equal(string.Empty, schema.Tables!["Order"].Fields!["amount"].CurrencyField);
        }

        [Fact]
        [DisplayName("Bake 公司 A vs B 位數不同 → 格式字串不同")]
        public void Bake_DifferentCompanies_DifferentFormats()
        {
            var companyA = CompanyWith(new NumberFormatItem(NumberKind.Percent, 2));
            var companyB = CompanyWith(new NumberFormatItem(NumberKind.Percent, 4));

            var schemaA = SchemaWith(new FormField("disc", "折扣", FieldDbType.Decimal) { NumberKind = NumberKind.Percent });
            var schemaB = SchemaWith(new FormField("disc", "折扣", FieldDbType.Decimal) { NumberKind = NumberKind.Percent });

            NumberFormatApplier.Bake(schemaA, companyA);
            NumberFormatApplier.Bake(schemaB, companyB);

            Assert.Equal("P2", schemaA.Tables!["Order"].Fields!["disc"].NumberFormat);
            Assert.Equal("P4", schemaB.Tables!["Order"].Fields!["disc"].NumberFormat);
        }

        [Fact]
        [DisplayName("Bake explicit NumberFormat 優先，不被覆蓋")]
        public void Bake_ExplicitFormat_Preserved()
        {
            var field = new FormField("amount", "金額", FieldDbType.Decimal)
            {
                NumberKind = NumberKind.Amount,
                NumberFormat = "C2",
            };
            var schema = SchemaWith(field);

            NumberFormatApplier.Bake(schema, null);

            Assert.Equal("C2", schema.Tables!["Order"].Fields!["amount"].NumberFormat);
        }

        [Fact]
        [DisplayName("Bake 綁 UnitField 的數量欄不 bake（runtime 依單位解析）")]
        public void Bake_QuantityWithUnitField_NotBaked()
        {
            var field = new FormField("order_qty", "數量", FieldDbType.Decimal)
            {
                NumberKind = NumberKind.Quantity,
                UnitField = "qty_uom",
            };
            var schema = SchemaWith(field);

            NumberFormatApplier.Bake(schema, null);

            Assert.Equal(string.Empty, schema.Tables!["Order"].Fields!["order_qty"].NumberFormat);
        }

        [Fact]
        [DisplayName("Bake 未綁 UnitField 的數量欄退公司位數並 bake（框架 Quantity N0）")]
        public void Bake_QuantityWithoutUnitField_BakedFromCompany()
        {
            var field = new FormField("order_qty", "數量", FieldDbType.Decimal) { NumberKind = NumberKind.Quantity };
            var schema = SchemaWith(field);

            NumberFormatApplier.Bake(schema, null);

            Assert.Equal("N0", schema.Tables!["Order"].Fields!["order_qty"].NumberFormat);
        }

        [Fact]
        [DisplayName("Bake NumberKind=None 欄位略過，不套格式")]
        public void Bake_NoneKind_Skipped()
        {
            var field = new FormField("memo", "備註", FieldDbType.String);
            var schema = SchemaWith(field);

            NumberFormatApplier.Bake(schema, null);

            Assert.Equal(string.Empty, schema.Tables!["Order"].Fields!["memo"].NumberFormat);
        }

        [Fact]
        [DisplayName("HasNumericField 有 NumberKind 欄回 true、全 None 回 false")]
        public void HasNumericField_DetectsNumericFields()
        {
            var numeric = SchemaWith(new FormField("amount", "金額", FieldDbType.Decimal) { NumberKind = NumberKind.Amount });
            var plain = SchemaWith(new FormField("memo", "備註", FieldDbType.String));

            Assert.True(NumberFormatApplier.HasNumericField(numeric));
            Assert.False(NumberFormatApplier.HasNumericField(plain));
        }

        [Fact]
        [DisplayName("Bake 只動傳入實例，來源 schema（模擬快取）不受污染")]
        public void Bake_DoesNotAffectSourceSchema()
        {
            // 用 Percent（Company 來源、會 bake）驗證污染隔離；Amount 不 bake 不適合此測試。
            var source = SchemaWith(new FormField("disc", "折扣", FieldDbType.Decimal) { NumberKind = NumberKind.Percent });

            // 模擬交付流程：clone 後才 bake，來源保持空 NumberFormat
            var clone = source.Clone();
            NumberFormatApplier.Bake(clone, null);

            Assert.Equal(string.Empty, source.Tables!["Order"].Fields!["disc"].NumberFormat);
            Assert.Equal("P2", clone.Tables!["Order"].Fields!["disc"].NumberFormat);
        }
    }
}
