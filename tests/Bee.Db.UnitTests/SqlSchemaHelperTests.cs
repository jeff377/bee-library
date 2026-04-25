using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.SqlServer;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    public class SqlSchemaHelperTests
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
        [InlineData("column_name", "[column_name]")]
        [InlineData("my]col", "[my]]col]")]
        [DisplayName("QuoteName 應用方括號包裹識別碼並逸出內部方括號")]
        public void QuoteName_VariousIdentifiers_ReturnsQuoted(string identifier, string expected)
        {
            var result = SqlSchemaHelper.QuoteName(identifier);
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
            var result = SqlSchemaHelper.EscapeSqlString(value);
            Assert.Equal(expected, result);
        }

        #endregion

        #region ConvertDbType

        [Theory]
        [InlineData(FieldDbType.Text, "[nvarchar](max)")]
        [InlineData(FieldDbType.Boolean, "[bit]")]
        [InlineData(FieldDbType.AutoIncrement, "[int] IDENTITY(1,1)")]
        [InlineData(FieldDbType.Short, "[smallint]")]
        [InlineData(FieldDbType.Integer, "[int]")]
        [InlineData(FieldDbType.Long, "[bigint]")]
        [InlineData(FieldDbType.Currency, "[decimal](19,4)")]
        [InlineData(FieldDbType.Date, "[date]")]
        [InlineData(FieldDbType.DateTime, "[datetime]")]
        [InlineData(FieldDbType.Guid, "[uniqueidentifier]")]
        [InlineData(FieldDbType.Binary, "[varbinary](max)")]
        [DisplayName("ConvertDbType 應為各 FieldDbType 回傳正確的 SQL Server 型別字串")]
        public void ConvertDbType_VariousTypes_ReturnsCorrectSqlType(FieldDbType dbType, string expected)
        {
            var field = MakeField(dbType);
            var result = SqlSchemaHelper.ConvertDbType(field);
            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("ConvertDbType String 應使用指定的欄位長度")]
        public void ConvertDbType_String_UsesFieldLength()
        {
            var field = MakeField(FieldDbType.String, length: 100);
            var result = SqlSchemaHelper.ConvertDbType(field);
            Assert.Equal("[nvarchar](100)", result);
        }

        [Fact]
        [DisplayName("ConvertDbType Decimal 應使用指定的 Precision/Scale")]
        public void ConvertDbType_Decimal_UsesPrecisionAndScale()
        {
            var field = MakeField(FieldDbType.Decimal, precision: 12, scale: 3);
            var result = SqlSchemaHelper.ConvertDbType(field);
            Assert.Equal("[decimal](12,3)", result);
        }

        [Fact]
        [DisplayName("ConvertDbType Decimal Precision/Scale 為 0 時應套用預設值 18/0")]
        public void ConvertDbType_Decimal_ZeroPrecision_UsesDefault()
        {
            var field = MakeField(FieldDbType.Decimal, precision: 0, scale: 0);
            var result = SqlSchemaHelper.ConvertDbType(field);
            Assert.Equal("[decimal](18,0)", result);
        }

        [Fact]
        [DisplayName("ConvertDbType 不支援的 FieldDbType 應擲出 InvalidOperationException")]
        public void ConvertDbType_UnknownDbType_ThrowsInvalidOperationException()
        {
            var field = MakeField(FieldDbType.Unknown);
            Assert.Throws<InvalidOperationException>(() => SqlSchemaHelper.ConvertDbType(field));
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
        [InlineData(FieldDbType.Date, "getdate()")]
        [InlineData(FieldDbType.DateTime, "getdate()")]
        [InlineData(FieldDbType.Guid, "newid()")]
        [InlineData(FieldDbType.Unknown, "")]
        [DisplayName("GetDefaultValueExpression 應為各 FieldDbType 回傳對應的 SQL Server 預設值運算式")]
        public void GetDefaultValueExpression_VariousTypes_ReturnsCorrectExpression(
            FieldDbType dbType, string expected)
        {
            var result = SqlSchemaHelper.GetDefaultValueExpression(dbType);
            Assert.Equal(expected, result);
        }

        #endregion

        #region GetDefaultExpression

        [Fact]
        [DisplayName("GetDefaultExpression AllowNull 欄位應回傳空字串")]
        public void GetDefaultExpression_AllowNull_ReturnsEmpty()
        {
            var field = MakeField(FieldDbType.Integer, allowNull: true);
            var result = SqlSchemaHelper.GetDefaultExpression(field);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("GetDefaultExpression AutoIncrement 欄位應回傳空字串")]
        public void GetDefaultExpression_AutoIncrement_ReturnsEmpty()
        {
            var field = MakeField(FieldDbType.AutoIncrement);
            var result = SqlSchemaHelper.GetDefaultExpression(field);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("GetDefaultExpression Integer 無自訂預設值應回傳內建預設值 0")]
        public void GetDefaultExpression_IntegerNoCustomDefault_ReturnsBuiltinDefault()
        {
            var field = MakeField(FieldDbType.Integer);
            var result = SqlSchemaHelper.GetDefaultExpression(field);
            Assert.Equal("0", result);
        }

        [Fact]
        [DisplayName("GetDefaultExpression Integer 有自訂預設值應回傳自訂值")]
        public void GetDefaultExpression_IntegerWithCustomDefault_ReturnsCustomValue()
        {
            var field = MakeField(FieldDbType.Integer, defaultValue: "99");
            var result = SqlSchemaHelper.GetDefaultExpression(field);
            Assert.Equal("99", result);
        }

        [Fact]
        [DisplayName("GetDefaultExpression String 無自訂預設值應回傳 N''")]
        public void GetDefaultExpression_StringNoCustomDefault_ReturnsEmptyNStringLiteral()
        {
            var field = MakeField(FieldDbType.String);
            var result = SqlSchemaHelper.GetDefaultExpression(field);
            Assert.Equal("N''", result);
        }

        [Fact]
        [DisplayName("GetDefaultExpression String 有自訂預設值應以 N'...' 格式回傳")]
        public void GetDefaultExpression_StringWithCustomDefault_ReturnsNStringLiteral()
        {
            var field = MakeField(FieldDbType.String, defaultValue: "test");
            var result = SqlSchemaHelper.GetDefaultExpression(field);
            Assert.Equal("N'test'", result);
        }

        [Fact]
        [DisplayName("GetDefaultExpression Guid 無自訂預設值應回傳 newid()")]
        public void GetDefaultExpression_Guid_ReturnsNewId()
        {
            var field = MakeField(FieldDbType.Guid);
            var result = SqlSchemaHelper.GetDefaultExpression(field);
            Assert.Equal("newid()", result);
        }

        #endregion

        #region GetColumnDefinition

        [Fact]
        [DisplayName("GetColumnDefinition NOT NULL Integer 欄位應包含型別、條件約束與 DEFAULT 子句")]
        public void GetColumnDefinition_NotNullInteger_ContainsExpectedParts()
        {
            var field = MakeField(FieldDbType.Integer, fieldName: "age");
            var result = SqlSchemaHelper.GetColumnDefinition(field);
            Assert.Contains("[age]", result);
            Assert.Contains("[int]", result);
            Assert.Contains("NOT NULL", result);
            Assert.Contains("DEFAULT (0)", result);
        }

        [Fact]
        [DisplayName("GetColumnDefinition NULL 欄位應不含 DEFAULT 子句")]
        public void GetColumnDefinition_NullableField_ExcludesDefaultClause()
        {
            var field = MakeField(FieldDbType.String, fieldName: "note", allowNull: true);
            var result = SqlSchemaHelper.GetColumnDefinition(field);
            Assert.Contains("[note]", result);
            Assert.Contains("NULL", result);
            Assert.DoesNotContain("DEFAULT", result);
        }

        #endregion
    }
}
