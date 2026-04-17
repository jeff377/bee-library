using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.SqlServer;
using Bee.Definition;

namespace Bee.Db.UnitTests
{
    public class SqlTableSchemaProviderStaticTests
    {
        #region GetFieldDbType

        [Theory]
        [InlineData("NCHAR", 0, 0, 0, FieldDbType.String)]
        [InlineData("NVARCHAR", 0, 0, 50, FieldDbType.String)]
        [InlineData("NVARCHAR", 0, 0, -1, FieldDbType.Text)]
        [InlineData("BIT", 0, 0, 0, FieldDbType.Boolean)]
        [InlineData("SMALLINT", 0, 0, 0, FieldDbType.Short)]
        [InlineData("INT", 0, 0, 0, FieldDbType.Integer)]
        [InlineData("BIGINT", 0, 0, 0, FieldDbType.Long)]
        [InlineData("FLOAT", 0, 0, 0, FieldDbType.Decimal)]
        [InlineData("DECIMAL", 19, 4, 0, FieldDbType.Currency)]
        [InlineData("DECIMAL", 12, 3, 0, FieldDbType.Decimal)]
        [InlineData("DATE", 0, 0, 0, FieldDbType.Date)]
        [InlineData("DATETIME", 0, 0, 0, FieldDbType.DateTime)]
        [InlineData("UNIQUEIDENTIFIER", 0, 0, 0, FieldDbType.Guid)]
        [InlineData("VARBINARY", 0, 0, 0, FieldDbType.Binary)]
        [InlineData("XML", 0, 0, 0, FieldDbType.Unknown)]
        [DisplayName("GetFieldDbType 應正確映射各 SQL Server 型別")]
        public void GetFieldDbType_VariousSqlTypes_MapsCorrectly(
            string dataType, int precision, int scale, int length, FieldDbType expected)
        {
            var result = SqlTableSchemaProvider.GetFieldDbType(dataType, precision, scale, length);

            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("GetFieldDbType 應對輸入字串大小寫不敏感")]
        public void GetFieldDbType_CaseInsensitive()
        {
            Assert.Equal(FieldDbType.Integer, SqlTableSchemaProvider.GetFieldDbType("int", 0, 0, 0));
            Assert.Equal(FieldDbType.Boolean, SqlTableSchemaProvider.GetFieldDbType("Bit", 0, 0, 0));
        }

        #endregion

        #region ParseDBDefaultValue

        [Theory]
        [InlineData("VARCHAR", "('hello')", "", "hello")]
        [InlineData("CHAR", "('A')", "", "A")]
        [InlineData("NVARCHAR", "(N'world')", "", "world")]
        [InlineData("NCHAR", "(N'X')", "", "X")]
        [InlineData("INT", "((42))", "", "42")]
        [InlineData("BIT", "((1))", "", "1")]
        [InlineData("DATE", "(getdate())", "", "getdate()")]
        [InlineData("DATETIME", "(getdate())", "", "getdate()")]
        [InlineData("UNIQUEIDENTIFIER", "(newid())", "", "newid()")]
        [DisplayName("ParseDBDefaultValue 應依型別剝除外層括號或前綴")]
        public void ParseDBDefaultValue_StripsWrappers(
            string dataType, string defaultValue, string originalDefault, string expected)
        {
            var result = SqlTableSchemaProvider.ParseDBDefaultValue(dataType, defaultValue, originalDefault);

            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("ParseDBDefaultValue 與內建預設值相同時應回傳空字串")]
        public void ParseDBDefaultValue_MatchesBuiltinDefault_ReturnsEmpty()
        {
            // INT 預設值通常為 "0"；((0)) 剝層後變成 "0"，與內建預設值相同
            var result = SqlTableSchemaProvider.ParseDBDefaultValue("INT", "((0))", "0");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("ParseDBDefaultValue 不支援的型別應回傳空字串")]
        public void ParseDBDefaultValue_UnknownType_ReturnsEmpty()
        {
            var result = SqlTableSchemaProvider.ParseDBDefaultValue("XML", "<root/>", "");

            Assert.Equal(string.Empty, result);
        }

        #endregion
    }
}
