using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.Oracle;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 補充 <see cref="OracleTableSchemaProvider"/> 靜態方法的覆蓋率：
    /// 测試 <see cref="OracleTableSchemaProviderStaticTests"/> 尚未涉及的
    /// NCHAR、NVARCHAR2、CHAR、NCLOB 型別路徑。
    /// </summary>
    public class OracleTableSchemaProviderAdditionalTests
    {
        #region GetFieldDbType 補充型別

        [Fact]
        [DisplayName("Oracle GetFieldDbType NCHAR 應映射為 String")]
        public void GetFieldDbType_Nchar_ReturnsString()
        {
            Assert.Equal(FieldDbType.String, OracleTableSchemaProvider.GetFieldDbType("NCHAR", 0, 0, 10));
            Assert.Equal(FieldDbType.String, OracleTableSchemaProvider.GetFieldDbType("nchar", 0, 0, 5));
        }

        [Theory]
        [InlineData("NVARCHAR2", 0, 0, 100, FieldDbType.String)]
        [InlineData("nvarchar2", 0, 0, 50, FieldDbType.String)]
        [InlineData("NCHAR", 0, 0, 10, FieldDbType.String)]
        [InlineData("NCLOB", 0, 0, 0, FieldDbType.Text)]
        [DisplayName("Oracle GetFieldDbType N-prefix 型別應正確映射")]
        public void GetFieldDbType_NPrefixTypes_MapsCorrectly(
            string dataType, int precision, int scale, int length, FieldDbType expected)
        {
            var result = OracleTableSchemaProvider.GetFieldDbType(dataType, precision, scale, length);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("NUMBER", 0, 1, 0, FieldDbType.Decimal)]
        [InlineData("NUMBER", 0, 0, 0, FieldDbType.Decimal)]
        [InlineData("NUMBER", 18, 2, 0, FieldDbType.Decimal)]
        [DisplayName("Oracle GetFieldDbType NUMBER 途徑補充：精度/小數不符合已知對映時應回傳 Decimal")]
        public void GetFieldDbType_NumberUncoveredBranches_ReturnsDecimal(
            string dataType, int precision, int scale, int length, FieldDbType expected)
        {
            var result = OracleTableSchemaProvider.GetFieldDbType(dataType, precision, scale, length);

            Assert.Equal(expected, result);
        }

        #endregion

        #region ParseDBDefaultValue 補充型別

        [Theory]
        [InlineData("NVARCHAR2", "'hello'", "", "hello")]
        [InlineData("CHAR", "'A'", "", "A")]
        [InlineData("NCHAR", "'X'", "", "X")]
        [InlineData("NCLOB", "'foo'", "", "foo")]
        [DisplayName("Oracle ParseDBDefaultValue N-prefix 型別應剔除字串引號")]
        public void ParseDBDefaultValue_NPrefixTypes_StripsStringLiteral(
            string dataType, string defaultValue, string originalDefault, string expected)
        {
            var result = OracleTableSchemaProvider.ParseDBDefaultValue(dataType, defaultValue, originalDefault);

            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("Oracle ParseDBDefaultValue NVARCHAR2 預設與內建預設相同應回傳空字串")]
        public void ParseDBDefaultValue_Nvarchar2MatchesBuiltin_ReturnsEmpty()
        {
            var result = OracleTableSchemaProvider.ParseDBDefaultValue("NVARCHAR2", "''", "");

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        [DisplayName("Oracle ParseDBDefaultValue 非引號包裹的字串型預設應原樣輸出")]
        public void ParseDBDefaultValue_StringTypeNotQuoted_ReturnsAsIs()
        {
            // Oracle DATA_DEFAULT 如果不是引號包裹（如引用式 default），StripStringLiteral 不處理直接回傳
            var result = OracleTableSchemaProvider.ParseDBDefaultValue("NVARCHAR2", "CURRENT_TIMESTAMP", "");

            Assert.Equal("CURRENT_TIMESTAMP", result);
        }

        [Fact]
        [DisplayName("Oracle ParseDBDefaultValue NCLOB 與內建預設不同時應回傳副本內容")]
        public void ParseDBDefaultValue_NclobCustomDefault_ReturnsStrippedValue()
        {
            var result = OracleTableSchemaProvider.ParseDBDefaultValue("NCLOB", "'my default'", "");

            Assert.Equal("my default", result);
        }

        #endregion
    }
}
