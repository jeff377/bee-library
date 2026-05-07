using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.MySql;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// Static-only tests for <see cref="MySqlTableSchemaProvider"/>: validates the type-
    /// mapping and default-value parsing helpers that don't require a live MySQL connection.
    /// The full integration coverage (live INFORMATION_SCHEMA reads, GetTableSchema
    /// round-trip) is gated by <c>BEE_TEST_CONNSTR_MYSQL</c> and runs in MySqlIntegrationTests.
    /// </summary>
    public class MySqlTableSchemaProviderStaticTests
    {
        #region GetFieldDbType

        [Theory]
        [InlineData("char", 0, 0, 36, FieldDbType.Guid)]
        [InlineData("char", 0, 0, 10, FieldDbType.String)]
        [InlineData("CHAR", 0, 0, 36, FieldDbType.Guid)]
        [InlineData("varchar", 0, 0, 100, FieldDbType.String)]
        [InlineData("VARCHAR", 0, 0, 50, FieldDbType.String)]
        [InlineData("text", 0, 0, 0, FieldDbType.Text)]
        [InlineData("tinytext", 0, 0, 0, FieldDbType.Text)]
        [InlineData("mediumtext", 0, 0, 0, FieldDbType.Text)]
        [InlineData("longtext", 0, 0, 0, FieldDbType.Text)]
        [InlineData("tinyint", 0, 0, 0, FieldDbType.Boolean)]
        [InlineData("TINYINT", 0, 0, 0, FieldDbType.Boolean)]
        [InlineData("smallint", 0, 0, 0, FieldDbType.Short)]
        [InlineData("int", 0, 0, 0, FieldDbType.Integer)]
        [InlineData("integer", 0, 0, 0, FieldDbType.Integer)]
        [InlineData("mediumint", 0, 0, 0, FieldDbType.Integer)]
        [InlineData("bigint", 0, 0, 0, FieldDbType.Long)]
        [InlineData("decimal", 19, 4, 0, FieldDbType.Currency)]
        [InlineData("decimal", 12, 3, 0, FieldDbType.Decimal)]
        [InlineData("numeric", 19, 4, 0, FieldDbType.Currency)]
        [InlineData("numeric", 10, 2, 0, FieldDbType.Decimal)]
        [InlineData("float", 0, 0, 0, FieldDbType.Decimal)]
        [InlineData("double", 0, 0, 0, FieldDbType.Decimal)]
        [InlineData("real", 0, 0, 0, FieldDbType.Decimal)]
        [InlineData("date", 0, 0, 0, FieldDbType.Date)]
        [InlineData("datetime", 0, 0, 0, FieldDbType.DateTime)]
        [InlineData("timestamp", 0, 0, 0, FieldDbType.DateTime)]
        [InlineData("binary", 0, 0, 0, FieldDbType.Binary)]
        [InlineData("varbinary", 0, 0, 0, FieldDbType.Binary)]
        [InlineData("blob", 0, 0, 0, FieldDbType.Binary)]
        [InlineData("tinyblob", 0, 0, 0, FieldDbType.Binary)]
        [InlineData("mediumblob", 0, 0, 0, FieldDbType.Binary)]
        [InlineData("longblob", 0, 0, 0, FieldDbType.Binary)]
        [InlineData("json", 0, 0, 0, FieldDbType.Unknown)]
        [DisplayName("MySQL GetFieldDbType 應正確映射各 MySQL 型別")]
        public void GetFieldDbType_VariousMySqlTypes_MapsCorrectly(
            string dataType, int precision, int scale, int length, FieldDbType expected)
        {
            var result = MySqlTableSchemaProvider.GetFieldDbType(dataType, precision, scale, length);

            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("MySQL GetFieldDbType 應對輸入字串大小寫不敏感")]
        public void GetFieldDbType_CaseInsensitive()
        {
            Assert.Equal(FieldDbType.Integer, MySqlTableSchemaProvider.GetFieldDbType("INT", 0, 0, 0));
            Assert.Equal(FieldDbType.Long, MySqlTableSchemaProvider.GetFieldDbType("BIGINT", 0, 0, 0));
            Assert.Equal(FieldDbType.Text, MySqlTableSchemaProvider.GetFieldDbType("TEXT", 0, 0, 0));
        }

        [Fact]
        [DisplayName("MySQL GetFieldDbType CHAR(36) 應映射為 Guid，其他長度映射為 String")]
        public void GetFieldDbType_CharLength36_IsGuid_OtherLengthIsString()
        {
            Assert.Equal(FieldDbType.Guid, MySqlTableSchemaProvider.GetFieldDbType("char", 0, 0, 36));
            Assert.Equal(FieldDbType.String, MySqlTableSchemaProvider.GetFieldDbType("char", 0, 0, 35));
            Assert.Equal(FieldDbType.String, MySqlTableSchemaProvider.GetFieldDbType("char", 0, 0, 10));
        }

        [Fact]
        [DisplayName("MySQL GetFieldDbType NULL 或空字串應回傳 Unknown")]
        public void GetFieldDbType_NullOrEmpty_ReturnsUnknown()
        {
            Assert.Equal(FieldDbType.Unknown, MySqlTableSchemaProvider.GetFieldDbType(null!, 0, 0, 0));
            Assert.Equal(FieldDbType.Unknown, MySqlTableSchemaProvider.GetFieldDbType(string.Empty, 0, 0, 0));
        }

        [Fact]
        [DisplayName("MySQL GetFieldDbType DECIMAL(19,4) 應映射為 Currency")]
        public void GetFieldDbType_Decimal19_4_IsCurrency()
        {
            Assert.Equal(FieldDbType.Currency, MySqlTableSchemaProvider.GetFieldDbType("decimal", 19, 4, 0));
        }

        #endregion

        #region ParseDBDefaultValue

        [Theory]
        [InlineData("int", "0", "0", "")]
        [InlineData("int", "42", "0", "42")]
        [InlineData("varchar", "hello", "", "hello")]
        [InlineData("varchar", "  world  ", "", "world")]
        [InlineData("datetime", "CURRENT_TIMESTAMP(6)", "CURRENT_TIMESTAMP(6)", "")]
        [InlineData("char", "uuid()", "(UUID())", "")]
        [InlineData("char", "UUID()", "(UUID())", "")]
        [InlineData("bigint", "100", "0", "100")]
        [InlineData("tinyint", "1", "0", "1")]
        [DisplayName("MySQL ParseDBDefaultValue 應 trim 空白並與內建預設比對")]
        public void ParseDBDefaultValue_VariousCases_ReturnsExpected(
            string dataType, string defaultValue, string originalDefault, string expected)
        {
            var result = MySqlTableSchemaProvider.ParseDBDefaultValue(dataType, defaultValue, originalDefault);

            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("MySQL ParseDBDefaultValue 空字串輸入應回傳空字串")]
        public void ParseDBDefaultValue_EmptyInput_ReturnsEmpty()
        {
            var result = MySqlTableSchemaProvider.ParseDBDefaultValue("int", string.Empty, "0");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("MySQL ParseDBDefaultValue 與內建預設值大小寫不同也視為相同（uuid）")]
        public void ParseDBDefaultValue_MatchesBuiltinDefaultCaseInsensitive_ReturnsEmpty()
        {
            // MySQL 將 (UUID()) 正規化為 uuid()（小寫、無外層括號）
            var result = MySqlTableSchemaProvider.ParseDBDefaultValue("char", "uuid()", "(UUID())");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("MySQL ParseDBDefaultValue 有外層括號的內建預設值應剝除括號後比較")]
        public void ParseDBDefaultValue_OuterParensInOriginal_StrippedBeforeCompare()
        {
            // 框架原始為 (UUID())，MySQL INFORMATION_SCHEMA 回傳 uuid()（已剝括號）
            // StripOuterParens((UUID())) → UUID() → 與 uuid() case-insensitive 相等 → 空字串
            var result = MySqlTableSchemaProvider.ParseDBDefaultValue("char", "uuid()", "(UUID())");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("MySQL ParseDBDefaultValue 與內建預設值不同時應回傳 trimmed 值")]
        public void ParseDBDefaultValue_DifferentFromBuiltin_ReturnsTrimmedValue()
        {
            var result = MySqlTableSchemaProvider.ParseDBDefaultValue("varchar", "  active  ", string.Empty);

            Assert.Equal("active", result);
        }

        [Fact]
        [DisplayName("MySQL ParseDBDefaultValue 數值型預設為 0 不同時應回傳自訂值")]
        public void ParseDBDefaultValue_NumericCustomDefault_ReturnsValue()
        {
            var result = MySqlTableSchemaProvider.ParseDBDefaultValue("int", "99", "0");

            Assert.Equal("99", result);
        }

        #endregion
    }
}
