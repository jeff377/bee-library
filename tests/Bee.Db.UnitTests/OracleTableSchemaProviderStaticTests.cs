using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.Oracle;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// Static-only tests for <see cref="OracleTableSchemaProvider"/>: validates the type-
    /// mapping and default-value parsing helpers that don't require a live Oracle connection.
    /// The full integration coverage (live <c>USER_*</c> dictionary reads, GetTableSchema
    /// round-trip) is gated by <c>BEE_TEST_CONNSTR_ORACLE</c> and runs in Phase D.
    /// </summary>
    public class OracleTableSchemaProviderStaticTests
    {
        #region GetFieldDbType

        [Theory]
        [InlineData("VARCHAR2", 0, 0, 50, FieldDbType.String)]
        [InlineData("varchar2", 0, 0, 100, FieldDbType.String)]
        [InlineData("NVARCHAR2", 0, 0, 50, FieldDbType.String)]
        [InlineData("CHAR", 0, 0, 10, FieldDbType.String)]
        [InlineData("CLOB", 0, 0, 0, FieldDbType.Text)]
        [InlineData("NCLOB", 0, 0, 0, FieldDbType.Text)]
        [InlineData("LONG", 0, 0, 0, FieldDbType.Text)]
        [InlineData("NUMBER", 1, 0, 0, FieldDbType.Boolean)]
        [InlineData("NUMBER", 5, 0, 0, FieldDbType.Short)]
        [InlineData("NUMBER", 10, 0, 0, FieldDbType.Integer)]
        [InlineData("NUMBER", 19, 0, 0, FieldDbType.Long)]
        [InlineData("NUMBER", 19, 4, 0, FieldDbType.Currency)]
        [InlineData("NUMBER", 12, 3, 0, FieldDbType.Decimal)]
        [InlineData("NUMBER", 18, 2, 0, FieldDbType.Decimal)]
        [InlineData("FLOAT", 0, 0, 0, FieldDbType.Decimal)]
        [InlineData("BINARY_FLOAT", 0, 0, 0, FieldDbType.Decimal)]
        [InlineData("BINARY_DOUBLE", 0, 0, 0, FieldDbType.Decimal)]
        [InlineData("DATE", 0, 0, 0, FieldDbType.Date)]
        [InlineData("TIMESTAMP(6)", 0, 0, 0, FieldDbType.DateTime)]
        [InlineData("TIMESTAMP", 0, 0, 0, FieldDbType.DateTime)]
        [InlineData("RAW", 0, 0, 16, FieldDbType.Guid)]
        [InlineData("RAW", 0, 0, 100, FieldDbType.Binary)]
        [InlineData("BLOB", 0, 0, 0, FieldDbType.Binary)]
        [InlineData("LONG RAW", 0, 0, 0, FieldDbType.Binary)]
        [InlineData("XMLTYPE", 0, 0, 0, FieldDbType.Unknown)]
        [DisplayName("Oracle GetFieldDbType 應正確映射各 Oracle 型別")]
        public void GetFieldDbType_VariousOracleTypes_MapsCorrectly(
            string dataType, int precision, int scale, int length, FieldDbType expected)
        {
            var result = OracleTableSchemaProvider.GetFieldDbType(dataType, precision, scale, length);

            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("Oracle GetFieldDbType 應對輸入字串大小寫不敏感")]
        public void GetFieldDbType_CaseInsensitive()
        {
            Assert.Equal(FieldDbType.String, OracleTableSchemaProvider.GetFieldDbType("varchar2", 0, 0, 50));
            Assert.Equal(FieldDbType.Integer, OracleTableSchemaProvider.GetFieldDbType("Number", 10, 0, 0));
            Assert.Equal(FieldDbType.Guid, OracleTableSchemaProvider.GetFieldDbType("Raw", 0, 0, 16));
        }

        [Fact]
        [DisplayName("Oracle GetFieldDbType TIMESTAMP 帶括號精度（如 TIMESTAMP(6) WITH TIME ZONE）也應辨識為 DateTime")]
        public void GetFieldDbType_TimestampWithQualifier_StillMapsToDateTime()
        {
            Assert.Equal(FieldDbType.DateTime, OracleTableSchemaProvider.GetFieldDbType("TIMESTAMP(6)", 0, 0, 0));
        }

        #endregion

        #region ParseDBDefaultValue

        [Theory]
        [InlineData("VARCHAR2", "'hello'", "", "hello")]
        [InlineData("VARCHAR2", "'world' ", "", "world")] // 帶 trailing space（Oracle LONG 慣例）
        [InlineData("CLOB", "'foo'", "", "foo")]
        [InlineData("NUMBER", "0", "", "0")]
        [InlineData("NUMBER", "42", "", "42")]
        [InlineData("DATE", "SYSTIMESTAMP", "", "SYSTIMESTAMP")]
        [InlineData("TIMESTAMP(6)", "SYSTIMESTAMP", "", "SYSTIMESTAMP")]
        [InlineData("RAW", "SYS_GUID()", "", "SYS_GUID()")]
        [DisplayName("Oracle ParseDBDefaultValue 應依型別剝除字串引號並 trim 空白")]
        public void ParseDBDefaultValue_StripsQuotesAndTrims(
            string dataType, string defaultValue, string originalDefault, string expected)
        {
            var result = OracleTableSchemaProvider.ParseDBDefaultValue(dataType, defaultValue, originalDefault);

            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("Oracle ParseDBDefaultValue 與內建預設值相同時應回傳空字串")]
        public void ParseDBDefaultValue_MatchesBuiltinDefault_ReturnsEmpty()
        {
            // NUMBER 預設值通常為 "0"；trim 後與 builtin 相同 → 空字串
            var result = OracleTableSchemaProvider.ParseDBDefaultValue("NUMBER", "0", "0");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("Oracle ParseDBDefaultValue 與內建預設值大小寫不同也應視為相同（SYS_GUID 等函式名）")]
        public void ParseDBDefaultValue_MatchesBuiltinDefaultCaseInsensitive_ReturnsEmpty()
        {
            // Oracle data dictionary 可能回傳 lower-case 函式名稱
            var result = OracleTableSchemaProvider.ParseDBDefaultValue("RAW", "sys_guid()", "SYS_GUID()");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("Oracle ParseDBDefaultValue 字串型別 escape 之雙引號應還原（'O''Brien' → O'Brien）")]
        public void ParseDBDefaultValue_EscapedQuoteInString_Unescaped()
        {
            var result = OracleTableSchemaProvider.ParseDBDefaultValue("VARCHAR2", "'O''Brien'", "");

            Assert.Equal("O'Brien", result);
        }

        [Fact]
        [DisplayName("Oracle ParseDBDefaultValue 空字串輸入應回傳空字串")]
        public void ParseDBDefaultValue_EmptyInput_ReturnsEmpty()
        {
            var result = OracleTableSchemaProvider.ParseDBDefaultValue("NUMBER", string.Empty, "0");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("Oracle ParseDBDefaultValue 字串空字面 '' 應還原為空字串（與 builtin 不同時保留）")]
        public void ParseDBDefaultValue_EmptyStringLiteral_ParsesToEmpty()
        {
            // 字串 column 的內建預設為 string.Empty；DB 實際儲存 '' → 解析也是 ""，
            // 與 originalDefaultValue (string.Empty) 比對為 equal → 回傳 string.Empty
            var result = OracleTableSchemaProvider.ParseDBDefaultValue("VARCHAR2", "''", "");

            Assert.Equal(string.Empty, result);
        }

        #endregion
    }
}
