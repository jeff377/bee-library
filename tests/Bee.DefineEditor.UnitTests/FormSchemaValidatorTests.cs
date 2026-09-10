using System.ComponentModel;
using Bee.Base.Data;
using Bee.DefineEditor.Models;
using Bee.DefineEditor.Services;
using Bee.Definition;
using Bee.Definition.Forms;

namespace Bee.DefineEditor.UnitTests
{
    /// <summary>
    /// FormSchemaValidator 的單位綁定檢查：數量／重量欄必須綁 UnitField，
    /// 且 UnitField 必須是同一張表、大小寫相同的欄（執行期從同一列以宣告欄名查找）。
    /// </summary>
    public class FormSchemaValidatorTests
    {
        private static FormSchema SchemaWith(params FormField[] fields)
        {
            var schema = new FormSchema("Order", "訂單");
            var table = schema.Tables!.Add("OrderLine", "明細");
            foreach (var field in fields)
                table.Fields!.Add(field);
            return schema;
        }

        private static List<ValidationIssue> UnitFieldErrors(FormSchema schema)
            => FormSchemaValidator.Validate(schema, SolutionContext.Empty)
                .Where(issue => issue.Severity == ValidationSeverity.Error
                    && issue.Path.EndsWith(".UnitField", StringComparison.Ordinal))
                .ToList();

        [Theory]
        [InlineData(NumberKind.Quantity)]
        [InlineData(NumberKind.Weight)]
        [DisplayName("數量／重量欄沒綁 UnitField → 報 Error")]
        public void Validate_UnitKindWithoutUnitField_ReportsError(NumberKind kind)
        {
            var schema = SchemaWith(new FormField("qty", "數量", FieldDbType.Decimal) { NumberKind = kind });

            var issue = Assert.Single(UnitFieldErrors(schema));
            Assert.Equal("OrderLine.qty.UnitField", issue.Path);
        }

        [Fact]
        [DisplayName("UnitField 指到同一張表不存在的欄 → 報 Error")]
        public void Validate_UnitFieldNotOnTable_ReportsError()
        {
            var schema = SchemaWith(
                new FormField("qty", "數量", FieldDbType.Decimal) { NumberKind = NumberKind.Quantity, UnitField = "qty_uom" });

            var issue = Assert.Single(UnitFieldErrors(schema));
            Assert.Contains("qty_uom", issue.Message);
        }

        [Fact]
        [DisplayName("UnitField 指到別張表的欄（例如表頭）→ 報 Error：執行期只讀同一列")]
        public void Validate_UnitFieldOnAnotherTable_ReportsError()
        {
            var schema = new FormSchema("Order", "訂單");
            schema.Tables!.Add("Order", "訂單").Fields!.Add("uom", "單位", FieldDbType.String);
            schema.Tables.Add("OrderLine", "明細").Fields!.Add(
                new FormField("qty", "數量", FieldDbType.Decimal) { NumberKind = NumberKind.Quantity, UnitField = "uom" });

            var issue = Assert.Single(UnitFieldErrors(schema));
            Assert.Equal("OrderLine.qty.UnitField", issue.Path);
        }

        [Fact]
        [DisplayName("UnitField 只差大小寫 → 報 Error：執行期以宣告欄名、區分大小寫查找")]
        public void Validate_UnitFieldCaseMismatch_ReportsError()
        {
            var schema = SchemaWith(
                new FormField("qty", "數量", FieldDbType.Decimal) { NumberKind = NumberKind.Quantity, UnitField = "QTY_UOM" },
                new FormField("qty_uom", "單位", FieldDbType.String));

            Assert.Single(UnitFieldErrors(schema));
        }

        [Fact]
        [DisplayName("數量欄綁了同一張表、大小寫相同的單位欄 → 不報單位 Error")]
        public void Validate_UnitFieldOnSameTable_NoError()
        {
            var schema = SchemaWith(
                new FormField("qty", "數量", FieldDbType.Decimal) { NumberKind = NumberKind.Quantity, UnitField = "qty_uom" },
                new FormField("qty_uom", "單位", FieldDbType.String));

            Assert.Empty(UnitFieldErrors(schema));
        }

        [Theory]
        [InlineData(NumberKind.None)]
        [InlineData(NumberKind.Amount)]
        [InlineData(NumberKind.UnitPrice)]
        [InlineData(NumberKind.Percent)]
        [DisplayName("非數量／重量的欄不要求 UnitField")]
        public void Validate_NonUnitKind_NoUnitFieldRequired(NumberKind kind)
        {
            var schema = SchemaWith(new FormField("value", "數值", FieldDbType.Decimal) { NumberKind = kind });

            Assert.Empty(UnitFieldErrors(schema));
        }
    }
}
