using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.PostgreSql;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 純語法測試：覆蓋 <see cref="PgSchemaSyntax"/> 對 Boolean default 的跨方言翻譯。
    /// 規範形式統一為 "1"/"0"，PG 拼接層翻譯為 TRUE/FALSE。
    /// </summary>
    public class PgSchemaSyntaxTests
    {
        [Fact]
        [DisplayName("PG GetDefaultExpression Boolean DefaultValue=1 應回傳 TRUE")]
        public void GetDefaultExpression_BooleanTrue_ReturnsTrue()
        {
            var field = new DbField("enabled", "Enabled", FieldDbType.Boolean) { DefaultValue = "1" };
            Assert.Equal("TRUE", PgSchemaSyntax.GetDefaultExpression(field));
        }

        [Fact]
        [DisplayName("PG GetDefaultExpression Boolean DefaultValue=0 應回傳 FALSE")]
        public void GetDefaultExpression_BooleanFalse_ReturnsFalse()
        {
            var field = new DbField("enabled", "Enabled", FieldDbType.Boolean) { DefaultValue = "0" };
            Assert.Equal("FALSE", PgSchemaSyntax.GetDefaultExpression(field));
        }

        [Fact]
        [DisplayName("PG GetDefaultExpression Boolean 無自訂預設應回傳 FALSE（內建 0 → FALSE）")]
        public void GetDefaultExpression_BooleanNoCustom_ReturnsFalse()
        {
            var field = new DbField("enabled", "Enabled", FieldDbType.Boolean);
            Assert.Equal("FALSE", PgSchemaSyntax.GetDefaultExpression(field));
        }

        [Fact]
        [DisplayName("PG GetDefaultExpression Boolean AllowNull 應回傳空字串（無 DEFAULT 子句）")]
        public void GetDefaultExpression_BooleanAllowNull_ReturnsEmpty()
        {
            var field = new DbField("enabled", "Enabled", FieldDbType.Boolean) { AllowNull = true };
            Assert.Equal(string.Empty, PgSchemaSyntax.GetDefaultExpression(field));
        }

        [Fact]
        [DisplayName("PG GetColumnDefinition Boolean DefaultValue=1 應產出 boolean NOT NULL DEFAULT TRUE")]
        public void GetColumnDefinition_BooleanTrue_IncludesDefaultTrue()
        {
            var field = new DbField("enabled", "Enabled", FieldDbType.Boolean) { DefaultValue = "1" };
            var sql = PgSchemaSyntax.GetColumnDefinition(field);
            Assert.Equal("\"enabled\" boolean NOT NULL DEFAULT TRUE", sql);
        }

        [Fact]
        [DisplayName("PG GetColumnDefinition Integer DefaultValue 既有行為不受影響（regression guard）")]
        public void GetColumnDefinition_IntegerCustomDefault_RawNumber()
        {
            var field = new DbField("age", "Age", FieldDbType.Integer) { DefaultValue = "42" };
            var sql = PgSchemaSyntax.GetColumnDefinition(field);
            Assert.Equal("\"age\" integer NOT NULL DEFAULT 42", sql);
        }
    }
}
