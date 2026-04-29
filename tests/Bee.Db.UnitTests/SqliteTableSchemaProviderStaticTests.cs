using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.Sqlite;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 純語法測試：覆蓋 <see cref="SqliteTableSchemaProvider"/> 的靜態解析方法
    /// （<c>MapToFieldDbType</c> 與 <c>ParseDefaultValue</c>），與 <see cref="PgTableSchemaProviderStaticTests"/>
    /// 對稱。
    /// </summary>
    public class SqliteTableSchemaProviderStaticTests
    {
        #region MapToFieldDbType

        [Theory]
        [InlineData("VARCHAR", false, FieldDbType.String)]
        [InlineData("CHAR", false, FieldDbType.String)]
        [InlineData("CHARACTER", false, FieldDbType.String)]
        [InlineData("NVARCHAR", false, FieldDbType.String)]
        [InlineData("TEXT", false, FieldDbType.Text)]
        [InlineData("CLOB", false, FieldDbType.Text)]
        [InlineData("BOOLEAN", false, FieldDbType.Boolean)]
        [InlineData("BOOL", false, FieldDbType.Boolean)]
        [InlineData("SMALLINT", false, FieldDbType.Short)]
        [InlineData("INT2", false, FieldDbType.Short)]
        [InlineData("INTEGER", false, FieldDbType.Integer)]
        [InlineData("INT", false, FieldDbType.Integer)]
        [InlineData("INT4", false, FieldDbType.Integer)]
        [InlineData("BIGINT", false, FieldDbType.Long)]
        [InlineData("INT8", false, FieldDbType.Long)]
        [InlineData("NUMERIC", false, FieldDbType.Decimal)]
        [InlineData("DECIMAL", false, FieldDbType.Decimal)]
        [InlineData("REAL", false, FieldDbType.Decimal)]
        [InlineData("DOUBLE", false, FieldDbType.Decimal)]
        [InlineData("FLOAT", false, FieldDbType.Decimal)]
        [InlineData("DATE", false, FieldDbType.Date)]
        [InlineData("DATETIME", false, FieldDbType.DateTime)]
        [InlineData("TIMESTAMP", false, FieldDbType.DateTime)]
        [InlineData("UUID", false, FieldDbType.Guid)]
        [InlineData("BLOB", false, FieldDbType.Binary)]
        [InlineData("BINARY", false, FieldDbType.Binary)]
        [InlineData("JSON", false, FieldDbType.Unknown)]
        [DisplayName("SQLite MapToFieldDbType 應正確映射各 SQLite 型別")]
        public void MapToFieldDbType_VariousTypes_MapsCorrectly(string baseType, bool isPrimaryKey, FieldDbType expected)
        {
            Assert.Equal(expected, SqliteTableSchemaProvider.MapToFieldDbType(baseType, isPrimaryKey));
        }

        [Fact]
        [DisplayName("SQLite MapToFieldDbType：INTEGER 且為 PK 應映射為 AutoIncrement（rowid alias）")]
        public void MapToFieldDbType_IntegerPrimaryKey_MapsToAutoIncrement()
        {
            Assert.Equal(FieldDbType.AutoIncrement,
                SqliteTableSchemaProvider.MapToFieldDbType("INTEGER", isPrimaryKey: true));
        }

        [Fact]
        [DisplayName("SQLite MapToFieldDbType：INTEGER 非 PK 應映射為 Integer")]
        public void MapToFieldDbType_IntegerNonPrimaryKey_MapsToInteger()
        {
            Assert.Equal(FieldDbType.Integer,
                SqliteTableSchemaProvider.MapToFieldDbType("INTEGER", isPrimaryKey: false));
        }

        [Fact]
        [DisplayName("SQLite MapToFieldDbType：對輸入字串大小寫不敏感")]
        public void MapToFieldDbType_CaseInsensitive()
        {
            Assert.Equal(FieldDbType.Boolean, SqliteTableSchemaProvider.MapToFieldDbType("boolean", false));
            Assert.Equal(FieldDbType.Decimal, SqliteTableSchemaProvider.MapToFieldDbType("Numeric", false));
            Assert.Equal(FieldDbType.Date, SqliteTableSchemaProvider.MapToFieldDbType("date", false));
        }

        [Fact]
        [DisplayName("SQLite MapToFieldDbType：null/空字串應映射為 Unknown")]
        public void MapToFieldDbType_NullOrEmpty_ReturnsUnknown()
        {
            Assert.Equal(FieldDbType.Unknown, SqliteTableSchemaProvider.MapToFieldDbType(null!, false));
            Assert.Equal(FieldDbType.Unknown, SqliteTableSchemaProvider.MapToFieldDbType(string.Empty, false));
        }

        #endregion

        #region ParseDefaultValue

        [Theory]
        [InlineData("0", FieldDbType.Integer, "0", "")]
        [InlineData("'hello'", FieldDbType.String, "", "hello")]
        [InlineData("'world'", FieldDbType.Text, "", "world")]
        [InlineData("CURRENT_TIMESTAMP", FieldDbType.DateTime, "CURRENT_TIMESTAMP", "")]
        [InlineData("42", FieldDbType.Integer, "0", "42")]
        [DisplayName("SQLite ParseDefaultValue 應依型別剝除引號與外層括號")]
        public void ParseDefaultValue_StripsQuotesAndParens(
            string raw, FieldDbType dbType, string original, string expected)
        {
            Assert.Equal(expected, SqliteTableSchemaProvider.ParseDefaultValue(raw, dbType, original));
        }

        [Fact]
        [DisplayName("SQLite ParseDefaultValue：函式預設外層括號應被剝除（hex(randomblob(16)) round-trip）")]
        public void ParseDefaultValue_FunctionDefaultWithOuterParens_StripsOnce()
        {
            // SQLite 在 DEFAULT 子句裡通常會以 (...) 包覆函式表達式，
            // ParseDefaultValue 只剝外層一層；compare 用的 originalDefault 仍含括號，
            // 因此比對不相等，回傳剝完外層括號的形式。
            var result = SqliteTableSchemaProvider.ParseDefaultValue(
                "(hex(randomblob(16)))", FieldDbType.Guid, "(hex(randomblob(16)))");
            Assert.Equal("hex(randomblob(16))", result);
        }

        [Fact]
        [DisplayName("SQLite ParseDefaultValue：字串型別應 unescape 雙引號")]
        public void ParseDefaultValue_EscapedQuoteInString_Unescaped()
        {
            var result = SqliteTableSchemaProvider.ParseDefaultValue("'O''Brien'", FieldDbType.String, string.Empty);
            Assert.Equal("O'Brien", result);
        }

        [Fact]
        [DisplayName("SQLite ParseDefaultValue：與內建預設值相同應回傳空字串")]
        public void ParseDefaultValue_MatchesBuiltinDefault_ReturnsEmpty()
        {
            var result = SqliteTableSchemaProvider.ParseDefaultValue("0", FieldDbType.Integer, "0");
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("SQLite ParseDefaultValue：空輸入應回傳空字串")]
        public void ParseDefaultValue_EmptyInput_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, SqliteTableSchemaProvider.ParseDefaultValue(string.Empty, FieldDbType.Integer, "0"));
        }

        [Fact]
        [DisplayName("SQLite ParseDefaultValue：外層括號被剝除（hex(randomblob) 一般 wrap 形式）")]
        public void ParseDefaultValue_OuterParens_AreStripped()
        {
            // SQLite 對函式呼叫式的 default 通常會以 (...) 包覆儲存，需剝除外層才能還原原始表達式。
            var result = SqliteTableSchemaProvider.ParseDefaultValue("(CURRENT_TIMESTAMP)", FieldDbType.DateTime, "CURRENT_TIMESTAMP");
            Assert.Equal(string.Empty, result);
        }

        #endregion
    }
}
