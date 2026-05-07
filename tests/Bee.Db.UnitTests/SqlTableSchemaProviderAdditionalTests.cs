using System.ComponentModel;
using Bee.Db.Providers.SqlServer;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 補充 <see cref="SqlTableSchemaProvider"/> 靜態方法的覆蓋率：
    /// 测試 <see cref="SqlTableSchemaProviderStaticTests"/> 尚未涉及的
    /// MONEY、FLOAT 和預設分支路徑。
    /// </summary>
    public class SqlTableSchemaProviderAdditionalTests
    {
        #region ParseDBDefaultValue 補充型別

        [Theory]
        [InlineData("MONEY", "((0))", "0", "")]
        [InlineData("MONEY", "((500))", "0", "500")]
        [InlineData("FLOAT", "((0))", "0", "")]
        [InlineData("FLOAT", "((3.14))", "0", "3.14")]
        [DisplayName("SQL Server ParseDBDefaultValue MONEY/FLOAT 應剝除 ((...)) 包裹")]
        public void ParseDBDefaultValue_MoneyAndFloat_StripsDoubleParens(
            string dataType, string defaultValue, string originalDefault, string expected)
        {
            var result = SqlTableSchemaProvider.ParseDBDefaultValue(dataType, defaultValue, originalDefault);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("DECIMAL", "((10.5))", "")]
        [InlineData("BIGINT", "((1000))", "")]
        [InlineData("VARBINARY", "0x", "")]
        [DisplayName("SQL Server ParseDBDefaultValue 不支援的型別應回傳空字串")]
        public void ParseDBDefaultValue_UnsupportedTypes_ReturnsEmpty(
            string dataType, string defaultValue, string expected)
        {
            var result = SqlTableSchemaProvider.ParseDBDefaultValue(dataType, defaultValue, string.Empty);

            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("SQL Server ParseDBDefaultValue NVARCHAR 表達式副本應正確解析")]
        public void ParseDBDefaultValue_NvarcharWithSingleQuotePrefix_StripsNPrefix()
        {
            // SQL Server 儲存的 NVARCHAR default 格式為 (N'value')
            var result = SqlTableSchemaProvider.ParseDBDefaultValue("NVARCHAR", "(N'active')", "");

            Assert.Equal("active", result);
        }

        [Fact]
        [DisplayName("SQL Server ParseDBDefaultValue NVARCHAR 表達式副本不含 N 前缀也應正確解析")]
        public void ParseDBDefaultValue_NvarcharWithoutNPrefix_StripsParens()
        {
            // 部分 SQL Server 表達式不含 N 前缀
            var result = SqlTableSchemaProvider.ParseDBDefaultValue("NVARCHAR", "('world')", "");

            Assert.Equal("world", result);
        }

        #endregion
    }
}
