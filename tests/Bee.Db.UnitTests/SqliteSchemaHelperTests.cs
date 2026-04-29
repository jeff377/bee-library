using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.Sqlite;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 純語法測試：覆蓋 <see cref="SqliteSchemaHelper"/> 的識別符 quote、字串 escape、
    /// 預設值表達式與 column / AutoIncrement column 定義組裝。
    /// </summary>
    public class SqliteSchemaHelperTests
    {
        #region QuoteName

        [Theory]
        [InlineData("st_user", "\"st_user\"")]
        [InlineData("name", "\"name\"")]
        [InlineData("col\"with quote", "\"col\"\"with quote\"")]
        [DisplayName("SQLite QuoteName：應以雙引號包覆並 escape 內部雙引號")]
        public void QuoteName_VariousIdentifiers_QuotesProperly(string identifier, string expected)
        {
            Assert.Equal(expected, SqliteSchemaHelper.QuoteName(identifier));
        }

        #endregion

        #region EscapeSqlString

        [Theory]
        [InlineData("hello", "hello")]
        [InlineData("O'Brien", "O''Brien")]
        [InlineData("''", "''''")]
        [DisplayName("SQLite EscapeSqlString：單引號應加倍以避免破壞字面量")]
        public void EscapeSqlString_DoublesSingleQuotes(string input, string expected)
        {
            Assert.Equal(expected, SqliteSchemaHelper.EscapeSqlString(input));
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
        [InlineData(FieldDbType.Guid, "(hex(randomblob(16)))")]
        [InlineData(FieldDbType.Binary, "")]
        [InlineData(FieldDbType.AutoIncrement, "")]
        [DisplayName("SQLite GetDefaultValueExpression：各型別應對應正確 SQLite 預設表達式")]
        public void GetDefaultValueExpression_VariousTypes_ReturnsExpected(FieldDbType dbType, string expected)
        {
            Assert.Equal(expected, SqliteSchemaHelper.GetDefaultValueExpression(dbType));
        }

        #endregion

        #region GetDefaultExpression

        [Fact]
        [DisplayName("SQLite GetDefaultExpression：AllowNull 欄位應回傳空字串（無 DEFAULT 子句）")]
        public void GetDefaultExpression_AllowNull_ReturnsEmpty()
        {
            var field = new DbField("v", "V", FieldDbType.Integer) { AllowNull = true };
            Assert.Equal(string.Empty, SqliteSchemaHelper.GetDefaultExpression(field));
        }

        [Fact]
        [DisplayName("SQLite GetDefaultExpression：AutoIncrement 欄位應回傳空字串（PK 內聯不需要 DEFAULT）")]
        public void GetDefaultExpression_AutoIncrement_ReturnsEmpty()
        {
            var field = new DbField("v", "V", FieldDbType.AutoIncrement);
            Assert.Equal(string.Empty, SqliteSchemaHelper.GetDefaultExpression(field));
        }

        [Fact]
        [DisplayName("SQLite GetDefaultExpression：String 無自訂 default 應回傳 ''")]
        public void GetDefaultExpression_StringNoCustom_ReturnsEmptyLiteral()
        {
            var field = new DbField("v", "V", FieldDbType.String) { Length = 50 };
            Assert.Equal("''", SqliteSchemaHelper.GetDefaultExpression(field));
        }

        [Fact]
        [DisplayName("SQLite GetDefaultExpression：String 自訂 default 應正確包單引號並 escape 內部引號")]
        public void GetDefaultExpression_StringCustomWithQuote_EscapesAndWraps()
        {
            var field = new DbField("v", "V", FieldDbType.String)
            {
                Length = 50,
                DefaultValue = "O'Brien"
            };
            Assert.Equal("'O''Brien'", SqliteSchemaHelper.GetDefaultExpression(field));
        }

        [Fact]
        [DisplayName("SQLite GetDefaultExpression：Integer 自訂 default 應原樣輸出")]
        public void GetDefaultExpression_IntegerCustom_ReturnsRaw()
        {
            var field = new DbField("v", "V", FieldDbType.Integer) { DefaultValue = "42" };
            Assert.Equal("42", SqliteSchemaHelper.GetDefaultExpression(field));
        }

        [Fact]
        [DisplayName("SQLite GetDefaultExpression：Integer 無自訂 default 應回傳內建 0")]
        public void GetDefaultExpression_IntegerNoCustom_ReturnsBuiltinZero()
        {
            var field = new DbField("v", "V", FieldDbType.Integer);
            Assert.Equal("0", SqliteSchemaHelper.GetDefaultExpression(field));
        }

        #endregion

        #region GetColumnDefinition

        [Fact]
        [DisplayName("SQLite GetColumnDefinition：String 欄位帶 COLLATE NOCASE 與 DEFAULT")]
        public void GetColumnDefinition_String_IncludesCollateAndDefault()
        {
            var field = new DbField("name", "Name", FieldDbType.String) { Length = 50 };
            var sql = SqliteSchemaHelper.GetColumnDefinition(field);
            Assert.Contains("\"name\" VARCHAR(50) COLLATE NOCASE NOT NULL DEFAULT ''", sql);
        }

        [Fact]
        [DisplayName("SQLite GetColumnDefinition：Integer NOT NULL 應帶 DEFAULT 0")]
        public void GetColumnDefinition_IntegerNotNull_IncludesDefaultZero()
        {
            var field = new DbField("count", "Count", FieldDbType.Integer);
            var sql = SqliteSchemaHelper.GetColumnDefinition(field);
            Assert.Contains("\"count\" INTEGER NOT NULL DEFAULT 0", sql);
        }

        [Fact]
        [DisplayName("SQLite GetColumnDefinition：AllowNull 不應出現 DEFAULT 子句")]
        public void GetColumnDefinition_AllowNull_OmitsDefault()
        {
            var field = new DbField("count", "Count", FieldDbType.Integer) { AllowNull = true };
            var sql = SqliteSchemaHelper.GetColumnDefinition(field);
            Assert.Equal("\"count\" INTEGER NULL", sql);
        }

        #endregion

        #region GetAutoIncrementColumnDefinition

        [Fact]
        [DisplayName("SQLite GetAutoIncrementColumnDefinition：應內聯 INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL")]
        public void GetAutoIncrementColumnDefinition_InlinesPrimaryKey()
        {
            var field = new DbField("sys_no", "Seq", FieldDbType.AutoIncrement);
            var sql = SqliteSchemaHelper.GetAutoIncrementColumnDefinition(field);
            Assert.Equal("\"sys_no\" INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL", sql);
        }

        #endregion
    }
}
