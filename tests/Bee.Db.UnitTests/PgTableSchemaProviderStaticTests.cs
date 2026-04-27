using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.PostgreSql;

namespace Bee.Db.UnitTests
{
    public class PgTableSchemaProviderStaticTests
    {
        #region GetFieldDbType

        [Theory]
        [InlineData("character varying", 0, 0, 50, FieldDbType.String)]
        [InlineData("varchar", 0, 0, 100, FieldDbType.String)]
        [InlineData("character", 0, 0, 10, FieldDbType.String)]
        [InlineData("character varying", 0, 0, 0, FieldDbType.Text)]
        [InlineData("text", 0, 0, 0, FieldDbType.Text)]
        [InlineData("boolean", 0, 0, 0, FieldDbType.Boolean)]
        [InlineData("smallint", 0, 0, 0, FieldDbType.Short)]
        [InlineData("integer", 0, 0, 0, FieldDbType.Integer)]
        [InlineData("bigint", 0, 0, 0, FieldDbType.Long)]
        [InlineData("numeric", 19, 4, 0, FieldDbType.Currency)]
        [InlineData("numeric", 12, 3, 0, FieldDbType.Decimal)]
        [InlineData("decimal", 18, 2, 0, FieldDbType.Decimal)]
        [InlineData("real", 0, 0, 0, FieldDbType.Decimal)]
        [InlineData("double precision", 0, 0, 0, FieldDbType.Decimal)]
        [InlineData("date", 0, 0, 0, FieldDbType.Date)]
        [InlineData("timestamp", 0, 0, 0, FieldDbType.DateTime)]
        [InlineData("timestamp without time zone", 0, 0, 0, FieldDbType.DateTime)]
        [InlineData("timestamp with time zone", 0, 0, 0, FieldDbType.DateTime)]
        [InlineData("uuid", 0, 0, 0, FieldDbType.Guid)]
        [InlineData("bytea", 0, 0, 0, FieldDbType.Binary)]
        [InlineData("json", 0, 0, 0, FieldDbType.Unknown)]
        [DisplayName("PG GetFieldDbType 應正確映射各 PostgreSQL 型別")]
        public void GetFieldDbType_VariousPgTypes_MapsCorrectly(
            string dataType, int precision, int scale, int length, FieldDbType expected)
        {
            var result = PgTableSchemaProvider.GetFieldDbType(dataType, precision, scale, length);

            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("PG GetFieldDbType 應對輸入字串大小寫不敏感")]
        public void GetFieldDbType_CaseInsensitive()
        {
            Assert.Equal(FieldDbType.Integer, PgTableSchemaProvider.GetFieldDbType("INTEGER", 0, 0, 0));
            Assert.Equal(FieldDbType.Boolean, PgTableSchemaProvider.GetFieldDbType("Boolean", 0, 0, 0));
        }

        #endregion

        #region ParseDBDefaultValue

        [Theory]
        [InlineData("character varying", "'hello'::character varying", "", "hello")]
        [InlineData("varchar", "'world'::character varying", "", "world")]
        [InlineData("text", "'foo'::text", "", "foo")]
        [InlineData("integer", "0", "", "0")]
        [InlineData("integer", "42", "", "42")]
        [InlineData("boolean", "true", "", "true")]
        [InlineData("boolean", "false", "", "false")]
        [InlineData("date", "CURRENT_TIMESTAMP", "", "CURRENT_TIMESTAMP")]
        [InlineData("timestamp", "CURRENT_TIMESTAMP", "", "CURRENT_TIMESTAMP")]
        [InlineData("uuid", "gen_random_uuid()", "", "gen_random_uuid()")]
        [DisplayName("PG ParseDBDefaultValue 應依型別剝除 ::cast 與字串引號")]
        public void ParseDBDefaultValue_StripsCastAndQuotes(
            string dataType, string defaultValue, string originalDefault, string expected)
        {
            var result = PgTableSchemaProvider.ParseDBDefaultValue(dataType, defaultValue, originalDefault);

            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("PG ParseDBDefaultValue 與內建預設值相同時應回傳空字串")]
        public void ParseDBDefaultValue_MatchesBuiltinDefault_ReturnsEmpty()
        {
            // integer 預設值通常為 "0"；剝除可能的 cast 後為 "0"，與內建相同 → 空字串
            var result = PgTableSchemaProvider.ParseDBDefaultValue("integer", "0", "0");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("PG ParseDBDefaultValue 字串型別 escape 之雙引號應還原")]
        public void ParseDBDefaultValue_EscapedQuoteInString_Unescaped()
        {
            var result = PgTableSchemaProvider.ParseDBDefaultValue(
                "character varying", "'O''Brien'::character varying", "");

            Assert.Equal("O'Brien", result);
        }

        [Fact]
        [DisplayName("PG ParseDBDefaultValue 空字串輸入應回傳空字串")]
        public void ParseDBDefaultValue_EmptyInput_ReturnsEmpty()
        {
            var result = PgTableSchemaProvider.ParseDBDefaultValue("integer", string.Empty, "0");

            Assert.Equal(string.Empty, result);
        }

        #endregion
    }
}
