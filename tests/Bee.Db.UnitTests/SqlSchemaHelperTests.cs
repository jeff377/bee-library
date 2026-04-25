using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.SqlServer;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    public class SqlSchemaHelperTests
    {
        private static DbField MakeField(FieldDbType dbType, int length = 0,
            int precision = 18, int scale = 0, bool allowNull = false, string defaultValue = "")
        {
            return new DbField("col", "Col", dbType)
            {
                Length = length,
                Precision = precision,
                Scale = scale,
                AllowNull = allowNull,
                DefaultValue = defaultValue
            };
        }

        #region QuoteName

        [Fact]
        [DisplayName("QuoteName 普通識別碼應以方括號包裹")]
        public void QuoteName_SimpleIdentifier_WrapsInBrackets()
        {
            var result = SqlSchemaHelper.QuoteName("col_name");
            Assert.Equal("[col_name]", result);
        }

        [Fact]
        [DisplayName("QuoteName 識別碼含 ] 時應跳脫為 ]]")]
        public void QuoteName_IdentifierWithClosingBracket_EscapesBracket()
        {
            var result = SqlSchemaHelper.QuoteName("col]name");
            Assert.Equal("[col]]name]", result);
        }

        #endregion

        #region EscapeSqlString

        [Fact]
        [DisplayName("EscapeSqlString 無單引號時應原樣回傳")]
        public void EscapeSqlString_NoSingleQuotes_ReturnsUnchanged()
        {
            var result = SqlSchemaHelper.EscapeSqlString("hello world");
            Assert.Equal("hello world", result);
        }

        [Fact]
        [DisplayName("EscapeSqlString 含單引號時應跳脫為兩個單引號")]
        public void EscapeSqlString_WithSingleQuotes_EscapesCorrectly()
        {
            var result = SqlSchemaHelper.EscapeSqlString("it's a test");
            Assert.Equal("it''s a test", result);
        }

        #endregion

        #region ConvertDbType

        [Theory]
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
        [InlineData(FieldDbType.Text, "[nvarchar](max)")]
        [DisplayName("ConvertDbType 應正確映射各 FieldDbType 至 SQL Server 型別")]
        public void ConvertDbType_VariousTypes_MapsCorrectly(FieldDbType dbType, string expected)
        {
            var field = MakeField(dbType);
            var result = SqlSchemaHelper.ConvertDbType(field);
            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("ConvertDbType String 應使用指定長度")]
        public void ConvertDbType_String_UsesLength()
        {
            var field = MakeField(FieldDbType.String, length: 50);
            var result = SqlSchemaHelper.ConvertDbType(field);
            Assert.Equal("[nvarchar](50)", result);
        }

        [Fact]
        [DisplayName("ConvertDbType Decimal 應使用指定 precision/scale")]
        public void ConvertDbType_Decimal_UsesSpecifiedPrecisionScale()
        {
            var field = MakeField(FieldDbType.Decimal, precision: 12, scale: 3);
            var result = SqlSchemaHelper.ConvertDbType(field);
            Assert.Equal("[decimal](12,3)", result);
        }

        [Fact]
        [DisplayName("ConvertDbType Decimal precision/scale 為 0 時應使用預設 [decimal](18,0)")]
        public void ConvertDbType_Decimal_ZeroPrecisionScale_UsesDefaults()
        {
            var field = MakeField(FieldDbType.Decimal, precision: 0, scale: 0);
            var result = SqlSchemaHelper.ConvertDbType(field);
            Assert.Equal("[decimal](18,0)", result);
        }

        [Fact]
        [DisplayName("ConvertDbType 不支援的 FieldDbType 應擲出 InvalidOperationException")]
        public void ConvertDbType_UnknownDbType_ThrowsInvalidOperation()
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
        [DisplayName("GetDefaultValueExpression 應回傳各型別的 SQL Server 內建預設值")]
        public void GetDefaultValueExpression_VariousTypes_ReturnsExpected(FieldDbType dbType, string expected)
        {
            var result = SqlSchemaHelper.GetDefaultValueExpression(dbType);
            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("GetDefaultValueExpression 未對應型別應回傳空字串")]
        public void GetDefaultValueExpression_UnknownType_ReturnsEmpty()
        {
            var result = SqlSchemaHelper.GetDefaultValueExpression(FieldDbType.Unknown);
            Assert.Equal(string.Empty, result);
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
            var field = MakeField(FieldDbType.AutoIncrement, allowNull: false);
            var result = SqlSchemaHelper.GetDefaultExpression(field);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("GetDefaultExpression String 空 DefaultValue 應回傳 N''")]
        public void GetDefaultExpression_StringEmptyDefault_ReturnsNEmptyLiteral()
        {
            var field = MakeField(FieldDbType.String, allowNull: false, defaultValue: "");
            var result = SqlSchemaHelper.GetDefaultExpression(field);
            Assert.Equal("N''", result);
        }

        [Fact]
        [DisplayName("GetDefaultExpression String 自訂 DefaultValue 應以 N'...' 包裹")]
        public void GetDefaultExpression_StringCustomDefault_WrapsInNLiteral()
        {
            var field = MakeField(FieldDbType.String, allowNull: false, defaultValue: "hello");
            var result = SqlSchemaHelper.GetDefaultExpression(field);
            Assert.Equal("N'hello'", result);
        }

        [Fact]
        [DisplayName("GetDefaultExpression Text 空 DefaultValue 應回傳 N''")]
        public void GetDefaultExpression_TextEmptyDefault_ReturnsNEmptyLiteral()
        {
            var field = MakeField(FieldDbType.Text, allowNull: false, defaultValue: "");
            var result = SqlSchemaHelper.GetDefaultExpression(field);
            Assert.Equal("N''", result);
        }

        [Fact]
        [DisplayName("GetDefaultExpression Integer 空 DefaultValue 應回傳 0")]
        public void GetDefaultExpression_IntegerEmptyDefault_ReturnsZero()
        {
            var field = MakeField(FieldDbType.Integer, allowNull: false, defaultValue: "");
            var result = SqlSchemaHelper.GetDefaultExpression(field);
            Assert.Equal("0", result);
        }

        [Fact]
        [DisplayName("GetDefaultExpression Integer 自訂 DefaultValue 應使用自訂值")]
        public void GetDefaultExpression_IntegerCustomDefault_ReturnsCustomValue()
        {
            var field = MakeField(FieldDbType.Integer, allowNull: false, defaultValue: "42");
            var result = SqlSchemaHelper.GetDefaultExpression(field);
            Assert.Equal("42", result);
        }

        [Fact]
        [DisplayName("GetDefaultExpression DateTime 欄位應回傳 getdate()")]
        public void GetDefaultExpression_DateTime_ReturnsGetdate()
        {
            var field = MakeField(FieldDbType.DateTime, allowNull: false);
            var result = SqlSchemaHelper.GetDefaultExpression(field);
            Assert.Equal("getdate()", result);
        }

        #endregion

        #region GetColumnDefinition

        [Fact]
        [DisplayName("GetColumnDefinition NOT NULL Integer 應包含型別、NULL 標記與 DEFAULT")]
        public void GetColumnDefinition_NotNullInteger_GeneratesFullDefinition()
        {
            var field = MakeField(FieldDbType.Integer, allowNull: false);
            var result = SqlSchemaHelper.GetColumnDefinition(field);
            Assert.Equal("[col] [int] NOT NULL DEFAULT (0)", result);
        }

        [Fact]
        [DisplayName("GetColumnDefinition NULL 欄位應不包含 DEFAULT 子句")]
        public void GetColumnDefinition_NullableField_NoDefaultClause()
        {
            var field = MakeField(FieldDbType.Integer, allowNull: true);
            var result = SqlSchemaHelper.GetColumnDefinition(field);
            Assert.Equal("[col] [int] NULL", result);
        }

        [Fact]
        [DisplayName("GetColumnDefinition String NOT NULL 應使用 N'' 預設值")]
        public void GetColumnDefinition_NotNullString_UsesNEmptyDefault()
        {
            var field = MakeField(FieldDbType.String, length: 50, allowNull: false);
            var result = SqlSchemaHelper.GetColumnDefinition(field);
            Assert.Equal("[col] [nvarchar](50) NOT NULL DEFAULT (N'')", result);
        }

        #endregion
    }
}
