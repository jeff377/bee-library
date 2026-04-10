using System.ComponentModel;
using Bee.Definition;

namespace Bee.Db.UnitTests
{
    public class DbFuncTests
    {
        #region QuoteIdentifier 跳脫測試

        [Theory]
        [InlineData(DatabaseType.SQLServer, "Name", "[Name]")]
        [InlineData(DatabaseType.SQLServer, "Col]umn", "[Col]]umn]")]
        [InlineData(DatabaseType.SQLServer, "A]]B", "[A]]]]B]")]
        [DisplayName("QuoteIdentifier SQL Server 應正確跳脫 ] 字元")]
        public void QuoteIdentifier_SqlServer_EscapesBracket(DatabaseType dbType, string identifier, string expected)
        {
            var result = DbFunc.QuoteIdentifier(dbType, identifier);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(DatabaseType.MySQL, "Name", "`Name`")]
        [InlineData(DatabaseType.MySQL, "Col`umn", "`Col``umn`")]
        [DisplayName("QuoteIdentifier MySQL 應正確跳脫 ` 字元")]
        public void QuoteIdentifier_MySql_EscapesBacktick(DatabaseType dbType, string identifier, string expected)
        {
            var result = DbFunc.QuoteIdentifier(dbType, identifier);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(DatabaseType.SQLite, "Name", "\"Name\"")]
        [InlineData(DatabaseType.SQLite, "Col\"umn", "\"Col\"\"umn\"")]
        [InlineData(DatabaseType.Oracle, "Name", "\"Name\"")]
        [InlineData(DatabaseType.Oracle, "Col\"umn", "\"Col\"\"umn\"")]
        [DisplayName("QuoteIdentifier SQLite/Oracle 應正確跳脫雙引號")]
        public void QuoteIdentifier_SqliteOracle_EscapesDoubleQuote(DatabaseType dbType, string identifier, string expected)
        {
            var result = DbFunc.QuoteIdentifier(dbType, identifier);
            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("QuoteIdentifier 不支援的資料庫類型應擲出 NotSupportedException")]
        public void QuoteIdentifier_UnsupportedType_Throws()
        {
            Assert.Throws<NotSupportedException>(() =>
                DbFunc.QuoteIdentifier((DatabaseType)999, "Test"));
        }

        #endregion

        #region GetParameterPrefix 測試

        [Theory]
        [InlineData(DatabaseType.SQLServer, "@")]
        [InlineData(DatabaseType.MySQL, "@")]
        [InlineData(DatabaseType.SQLite, "@")]
        [InlineData(DatabaseType.Oracle, ":")]
        [DisplayName("GetParameterPrefix 應回傳對應資料庫的參數前綴")]
        public void GetParameterPrefix_ReturnsCorrectPrefix(DatabaseType dbType, string expected)
        {
            var result = DbFunc.GetParameterPrefix(dbType);
            Assert.Equal(expected, result);
        }

        #endregion
    }
}
