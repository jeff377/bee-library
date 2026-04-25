using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.PostgreSql;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    public class PgSchemaHelperTests
    {
        private static DbField MakeField(
            FieldDbType dbType,
            string fieldName = "col",
            bool allowNull = false,
            string defaultValue = "",
            int length = 50,
            int precision = 18,
            int scale = 0)
        {
            var field = new DbField(fieldName, "Col", dbType);
            field.AllowNull = allowNull;
            field.DefaultValue = defaultValue;
            field.Length = length;
            field.Precision = precision;
            field.Scale = scale;
            return field;
        }

        #region QuoteName

        [Theory]
        [InlineData("column_name", "\"column_name\"")]
        [InlineData("my\"col", "\"my\"\"col\"")]
        [DisplayName("QuoteName 應用雙引號包裹識別碼並逸出內部雙引號")]
        public void QuoteName_VariousIdentifiers_ReturnsQuoted(string identifier, string expected)
        {
            var result = PgSchemaHelper.QuoteName(identifier);
            Assert.Equal(expected, result);
        }

        #endregion

        #region EscapeSqlString

        [Theory]
        [InlineData("hello", "hello")]
        [InlineData("it's", "it''s")]
        [InlineData("a'b'c", "a''b''c")]
        [DisplayName("EscapeSqlString 應將單引號加倍逸出")]
        public void EscapeSqlString_VariousInputs_EscapesSingleQuotes(string value, string expected)
        {
            var result = PgSchemaHelper.EscapeSqlString(value);
            Assert.Equal(expected, result);
        }

        #endregion

        #region GetDefaultValueExpression

        [Theory]
        [InlineData(FieldDbType.String, "")]
        [InlineData(FieldDbType.Text, "")]
        [InlineData(FieldDbType.Boolean, "0")]
        [InlineData(FieldDbType.Short, "0")]
        [InlineData(FieldDbType.Integer, "0")]
        [InlineData(FieldDbType.Long, "0")]
        [InlineData(FieldDbType.Decimal, "0")]
        [InlineData(FieldDbType.Currency, "0")]
        [InlineData(FieldDbType.Date, "CURRENT_TIMESTAMP")]
        [InlineData(FieldDbType.DateTime, "CURRENT_TIMESTAMP")]
        [InlineData(FieldDbType.Guid, "gen_random_uuid()")]
        [InlineData(FieldDbType.Unknown, "")]
        [DisplayName("GetDefaultValueExpression 應為各 FieldDbType 回傳對應的 PostgreSQL 預設值運算式")]
        public void GetDefaultValueExpression_VariousTypes_ReturnsCorrectExpression(
            FieldDbType dbType, string expected)
        {
            var result = PgSchemaHelper.GetDefaultValueExpression(dbType);
            Assert.Equal(expected, result);
        }

        #endregion

        #region GetDefaultExpression

        [Fact]
        [DisplayName("GetDefaultExpression AllowNull 欄位應回傳空字串")]
        public void GetDefaultExpression_AllowNull_ReturnsEmpty()
        {
            var field = MakeField(FieldDbType.Integer, allowNull: true);
            var result = PgSchemaHelper.GetDefaultExpression(field);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("GetDefaultExpression AutoIncrement 欄位應回傳空字串")]
        public void GetDefaultExpression_AutoIncrement_ReturnsEmpty()
        {
            var field = MakeField(FieldDbType.AutoIncrement);
            var result = PgSchemaHelper.GetDefaultExpression(field);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("GetDefaultExpression Integer 無自訂預設值應回傳內建預設值 0")]
        public void GetDefaultExpression_IntegerNoCustomDefault_ReturnsBuiltinDefault()
        {
            var field = MakeField(FieldDbType.Integer);
            var result = PgSchemaHelper.GetDefaultExpression(field);
            Assert.Equal("0", result);
        }

        [Fact]
        [DisplayName("GetDefaultExpression Integer 有自訂預設值應回傳自訂值")]
        public void GetDefaultExpression_IntegerWithCustomDefault_ReturnsCustomValue()
        {
            var field = MakeField(FieldDbType.Integer, defaultValue: "42");
            var result = PgSchemaHelper.GetDefaultExpression(field);
            Assert.Equal("42", result);
        }

        [Fact]
        [DisplayName("GetDefaultExpression Guid 無自訂預設值應回傳 gen_random_uuid()")]
        public void GetDefaultExpression_Guid_ReturnsGenRandomUuid()
        {
            var field = MakeField(FieldDbType.Guid);
            var result = PgSchemaHelper.GetDefaultExpression(field);
            Assert.Equal("gen_random_uuid()", result);
        }

        [Fact]
        [DisplayName("GetDefaultExpression String 無自訂預設值應回傳 ''")]
        public void GetDefaultExpression_StringNoCustomDefault_ReturnsEmptyStringLiteral()
        {
            var field = MakeField(FieldDbType.String);
            var result = PgSchemaHelper.GetDefaultExpression(field);
            Assert.Equal("''", result);
        }

        [Fact]
        [DisplayName("GetDefaultExpression String 有自訂預設值應以單引號包裹並逸出特殊字元")]
        public void GetDefaultExpression_StringWithCustomDefault_ReturnsEscapedLiteral()
        {
            var field = MakeField(FieldDbType.String, defaultValue: "O'Brien");
            var result = PgSchemaHelper.GetDefaultExpression(field);
            Assert.Equal("'O''Brien'", result);
        }

        #endregion

        #region GetColumnDefinition

        [Fact]
        [DisplayName("GetColumnDefinition NOT NULL Integer 欄位應包含型別、條件約束與預設值")]
        public void GetColumnDefinition_NotNullInteger_ContainsExpectedParts()
        {
            var field = MakeField(FieldDbType.Integer, fieldName: "age");
            var result = PgSchemaHelper.GetColumnDefinition(field);
            Assert.Contains("\"age\"", result);
            Assert.Contains("integer", result);
            Assert.Contains("NOT NULL", result);
            Assert.Contains("DEFAULT 0", result);
        }

        [Fact]
        [DisplayName("GetColumnDefinition NULL 欄位應不含 DEFAULT 子句")]
        public void GetColumnDefinition_NullableField_ExcludesDefaultClause()
        {
            var field = MakeField(FieldDbType.String, fieldName: "note", allowNull: true);
            var result = PgSchemaHelper.GetColumnDefinition(field);
            Assert.Contains("\"note\"", result);
            Assert.Contains("NULL", result);
            Assert.DoesNotContain("DEFAULT", result);
        }

        #endregion
    }
}
