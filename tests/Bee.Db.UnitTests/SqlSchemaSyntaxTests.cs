using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.SqlServer;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 純語法測試：覆蓋 <see cref="SqlSchemaSyntax"/> 的識別符 quote、字串 escape、
    /// 資料型轉換、預設值表達式與 column 定義組裝。
    /// </summary>
    public class SqlSchemaSyntaxTests
    {
        #region QuoteName

        [Theory]
        [InlineData("st_user", "[st_user]")]
        [InlineData("name", "[name]")]
        [InlineData("col]with bracket", "[col]]with bracket]")]
        [InlineData("", "[]")]
        [DisplayName("SQL Server QuoteName 應以方括號包覆並 escape 內部 ]")]
        public void QuoteName_VariousIdentifiers_QuotesProperly(string identifier, string expected)
        {
            Assert.Equal(expected, SqlSchemaSyntax.QuoteName(identifier));
        }

        #endregion

        #region EscapeSqlString

        [Theory]
        [InlineData("hello", "hello")]
        [InlineData("O'Brien", "O''Brien")]
        [InlineData("it's a 'test'", "it''s a ''test''")]
        [InlineData("", "")]
        [DisplayName("SQL Server EscapeSqlString 單引號應加倍以避免破壞字面量")]
        public void EscapeSqlString_DoublesSingleQuotes(string input, string expected)
        {
            Assert.Equal(expected, SqlSchemaSyntax.EscapeSqlString(input));
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
        [InlineData(FieldDbType.DateTime, "[datetime2](7)")]
        [InlineData(FieldDbType.Guid, "[uniqueidentifier]")]
        [InlineData(FieldDbType.Binary, "[varbinary](max)")]
        [DisplayName("SQL Server ConvertDbType 應回傳各型別對應的 SQL Server 型別字串")]
        public void ConvertDbType_VariousTypes_ReturnsExpectedSql(FieldDbType dbType, string expected)
        {
            var field = new DbField("f", "F", dbType);
            Assert.Equal(expected, SqlSchemaSyntax.ConvertDbType(field));
        }

        [Fact]
        [DisplayName("SQL Server ConvertDbType String 應含 Length")]
        public void ConvertDbType_String_IncludesLength()
        {
            var field = new DbField("name", "Name", FieldDbType.String) { Length = 50 };
            Assert.Equal("[nvarchar](50)", SqlSchemaSyntax.ConvertDbType(field));
        }

        [Fact]
        [DisplayName("SQL Server ConvertDbType Decimal 預設精度 18,0")]
        public void ConvertDbType_DecimalDefault_ReturnsDecimal18_0()
        {
            var field = new DbField("f", "F", FieldDbType.Decimal);
            Assert.Equal("[decimal](18,0)", SqlSchemaSyntax.ConvertDbType(field));
        }

        [Fact]
        [DisplayName("SQL Server ConvertDbType Decimal 自訂精度應正確輸出")]
        public void ConvertDbType_DecimalCustom_ReturnsCorrectPrecision()
        {
            var field = new DbField("f", "F", FieldDbType.Decimal) { Precision = 12, Scale = 3 };
            Assert.Equal("[decimal](12,3)", SqlSchemaSyntax.ConvertDbType(field));
        }

        [Fact]
        [DisplayName("SQL Server ConvertDbType Unknown 應丟出 InvalidOperationException")]
        public void ConvertDbType_UnknownType_ThrowsInvalidOperationException()
        {
            var field = new DbField("f", "F", FieldDbType.Unknown);
            Assert.Throws<InvalidOperationException>(() => SqlSchemaSyntax.ConvertDbType(field));
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
        [InlineData(FieldDbType.AutoIncrement, "")]
        [InlineData(FieldDbType.Binary, "")]
        [InlineData(FieldDbType.Unknown, "")]
        [DisplayName("SQL Server GetDefaultValueExpression 各型別應對應正確 SQL Server 預設表達式")]
        public void GetDefaultValueExpression_VariousTypes_ReturnsExpected(FieldDbType dbType, string expected)
        {
            Assert.Equal(expected, SqlSchemaSyntax.GetDefaultValueExpression(dbType));
        }

        #endregion

        #region GetDefaultExpression

        [Fact]
        [DisplayName("SQL Server GetDefaultExpression AllowNull 欄位應回傳空字串（無 DEFAULT 子句）")]
        public void GetDefaultExpression_AllowNull_ReturnsEmpty()
        {
            var field = new DbField("f", "F", FieldDbType.Integer) { AllowNull = true };
            Assert.Equal(string.Empty, SqlSchemaSyntax.GetDefaultExpression(field));
        }

        [Fact]
        [DisplayName("SQL Server GetDefaultExpression AutoIncrement 欄位應回傳空字串")]
        public void GetDefaultExpression_AutoIncrement_ReturnsEmpty()
        {
            var field = new DbField("pk", "PK", FieldDbType.AutoIncrement);
            Assert.Equal(string.Empty, SqlSchemaSyntax.GetDefaultExpression(field));
        }

        [Fact]
        [DisplayName("SQL Server GetDefaultExpression String 無自訂預設應回傳 N''")]
        public void GetDefaultExpression_StringNoCustom_ReturnsEmptyNLiteral()
        {
            var field = new DbField("name", "Name", FieldDbType.String) { Length = 50 };
            Assert.Equal("N''", SqlSchemaSyntax.GetDefaultExpression(field));
        }

        [Fact]
        [DisplayName("SQL Server GetDefaultExpression String 自訂預設應包 N'...' 後回傳")]
        public void GetDefaultExpression_StringCustom_ReturnsNLiteralWrapped()
        {
            var field = new DbField("name", "Name", FieldDbType.String) { Length = 50, DefaultValue = "hello" };
            Assert.Equal("N'hello'", SqlSchemaSyntax.GetDefaultExpression(field));
        }

        [Fact]
        [DisplayName("SQL Server GetDefaultExpression Integer 無自訂預設應回傳內建預設 0")]
        public void GetDefaultExpression_IntegerNoCustom_ReturnsBuiltinZero()
        {
            var field = new DbField("age", "Age", FieldDbType.Integer);
            Assert.Equal("0", SqlSchemaSyntax.GetDefaultExpression(field));
        }

        [Fact]
        [DisplayName("SQL Server GetDefaultExpression Integer 自訂預設應原樣輸出")]
        public void GetDefaultExpression_IntegerCustom_ReturnsRaw()
        {
            var field = new DbField("age", "Age", FieldDbType.Integer) { DefaultValue = "42" };
            Assert.Equal("42", SqlSchemaSyntax.GetDefaultExpression(field));
        }

        [Fact]
        [DisplayName("SQL Server GetDefaultExpression DateTime 無自訂應回傳 getdate()")]
        public void GetDefaultExpression_DateTimeNoCustom_ReturnsGetDate()
        {
            var field = new DbField("created_at", "Created", FieldDbType.DateTime);
            Assert.Equal("getdate()", SqlSchemaSyntax.GetDefaultExpression(field));
        }

        [Fact]
        [DisplayName("SQL Server GetDefaultExpression Guid 無自訂應回傳 newid()")]
        public void GetDefaultExpression_GuidNoCustom_ReturnsNewId()
        {
            var field = new DbField("sys_rowid", "Row ID", FieldDbType.Guid);
            Assert.Equal("newid()", SqlSchemaSyntax.GetDefaultExpression(field));
        }

        [Fact]
        [DisplayName("SQL Server GetDefaultExpression Text 無自訂預設應回傳 N''")]
        public void GetDefaultExpression_TextNoCustom_ReturnsEmptyNLiteral()
        {
            var field = new DbField("content", "Content", FieldDbType.Text);
            Assert.Equal("N''", SqlSchemaSyntax.GetDefaultExpression(field));
        }

        #endregion

        #region GetColumnDefinition

        [Fact]
        [DisplayName("SQL Server GetColumnDefinition String NOT NULL 應包含型別、非空和 DEFAULT")]
        public void GetColumnDefinition_StringNotNull_IncludesTypeNullabilityAndDefault()
        {
            var field = new DbField("name", "Name", FieldDbType.String) { Length = 50 };
            var sql = SqlSchemaSyntax.GetColumnDefinition(field);
            Assert.Contains("[name]", sql);
            Assert.Contains("[nvarchar](50)", sql);
            Assert.Contains("NOT NULL", sql);
            Assert.Contains("DEFAULT (N'')", sql);
        }

        [Fact]
        [DisplayName("SQL Server GetColumnDefinition Integer NOT NULL 應包含 DEFAULT 0")]
        public void GetColumnDefinition_IntegerNotNull_IncludesDefaultZero()
        {
            var field = new DbField("count", "Count", FieldDbType.Integer);
            var sql = SqlSchemaSyntax.GetColumnDefinition(field);
            Assert.Equal("[count] [int] NOT NULL DEFAULT (0)", sql);
        }

        [Fact]
        [DisplayName("SQL Server GetColumnDefinition AllowNull 不應出現 DEFAULT 子句")]
        public void GetColumnDefinition_AllowNull_OmitsDefaultClause()
        {
            var field = new DbField("remark", "Remark", FieldDbType.Integer) { AllowNull = true };
            var sql = SqlSchemaSyntax.GetColumnDefinition(field);
            Assert.Equal("[remark] [int] NULL", sql);
        }

        [Fact]
        [DisplayName("SQL Server GetColumnDefinition Guid NOT NULL 應包含 DEFAULT (newid())")]
        public void GetColumnDefinition_GuidNotNull_IncludesNewId()
        {
            var field = new DbField("sys_rowid", "Row ID", FieldDbType.Guid);
            var sql = SqlSchemaSyntax.GetColumnDefinition(field);
            Assert.Equal("[sys_rowid] [uniqueidentifier] NOT NULL DEFAULT (newid())", sql);
        }

        [Fact]
        [DisplayName("SQL Server GetColumnDefinition 含 ] 的欄位名應正確 escape")]
        public void GetColumnDefinition_IdentifierWithBracket_EscapesProperly()
        {
            var field = new DbField("col]name", "Col", FieldDbType.Integer);
            var sql = SqlSchemaSyntax.GetColumnDefinition(field);
            Assert.StartsWith("[col]]name]", sql);
        }

        #endregion
    }
}
