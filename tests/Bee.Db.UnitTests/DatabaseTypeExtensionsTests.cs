using System.ComponentModel;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    public class DatabaseTypeExtensionsTests
    {
        #region QuoteIdentifier 跳脫測試

        [Theory]
        [InlineData(DatabaseType.SQLServer, "Name", "[Name]")]
        [InlineData(DatabaseType.SQLServer, "Col]umn", "[Col]]umn]")]
        [InlineData(DatabaseType.SQLServer, "A]]B", "[A]]]]B]")]
        [DisplayName("QuoteIdentifier SQL Server 應正確跳脫 ] 字元")]
        public void QuoteIdentifier_SqlServer_EscapesBracket(DatabaseType dbType, string identifier, string expected)
        {
            var result = dbType.QuoteIdentifier(identifier);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(DatabaseType.MySQL, "Name", "`Name`")]
        [InlineData(DatabaseType.MySQL, "Col`umn", "`Col``umn`")]
        [DisplayName("QuoteIdentifier MySQL 應正確跳脫 ` 字元")]
        public void QuoteIdentifier_MySql_EscapesBacktick(DatabaseType dbType, string identifier, string expected)
        {
            var result = dbType.QuoteIdentifier(identifier);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(DatabaseType.SQLite, "Name", "\"Name\"")]
        [InlineData(DatabaseType.SQLite, "Col\"umn", "\"Col\"\"umn\"")]
        [InlineData(DatabaseType.PostgreSQL, "Name", "\"Name\"")]
        [InlineData(DatabaseType.PostgreSQL, "Col\"umn", "\"Col\"\"umn\"")]
        [DisplayName("QuoteIdentifier SQLite/PostgreSQL 應正確跳脫雙引號（保留原大小寫）")]
        public void QuoteIdentifier_SqlitePg_EscapesDoubleQuote(DatabaseType dbType, string identifier, string expected)
        {
            var result = dbType.QuoteIdentifier(identifier);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("Name", "\"NAME\"")]
        [InlineData("col", "\"COL\"")]
        [InlineData("Col\"umn", "\"COL\"\"UMN\"")]
        [DisplayName("QuoteIdentifier Oracle 應正確跳脫雙引號並 UPPERCASE 化（adapter 邊界策略）")]
        public void QuoteIdentifier_Oracle_UppercasesAndEscapes(string identifier, string expected)
        {
            // Oracle 採 quoted-UPPERCASE 策略：framework 對 Oracle 識別符在 emit 階段 UPPER 化
            // 後加引號，與 Oracle 內部「unquoted fold to UPPER」自然儲存對齊。SQL Server / MySQL
            // 不分大小寫，不需此處理；PostgreSQL / SQLite 保留 case-sensitive 原樣存放。
            var result = DatabaseType.Oracle.QuoteIdentifier(identifier);
            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("QuoteIdentifier 不支援的資料庫類型應擲出 NotSupportedException")]
        public void QuoteIdentifier_UnsupportedType_Throws()
        {
            Assert.Throws<NotSupportedException>(() =>
                ((DatabaseType)999).QuoteIdentifier("Test"));
        }

        #endregion

        #region GetParameterPrefix 測試

        [Theory]
        [InlineData(DatabaseType.SQLServer, "@")]
        [InlineData(DatabaseType.MySQL, "@")]
        [InlineData(DatabaseType.SQLite, "@")]
        [InlineData(DatabaseType.Oracle, ":")]
        [InlineData(DatabaseType.PostgreSQL, "@")]
        [DisplayName("GetParameterPrefix 應回傳對應資料庫的參數前綴")]
        public void GetParameterPrefix_ReturnsCorrectPrefix(DatabaseType dbType, string expected)
        {
            var result = dbType.GetParameterPrefix();
            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("GetParameterPrefix 不支援的資料庫類型應擲出 NotSupportedException")]
        public void GetParameterPrefix_UnsupportedType_Throws()
        {
            Assert.Throws<NotSupportedException>(() =>
                ((DatabaseType)999).GetParameterPrefix());
        }

        #endregion

        #region GetParameterName 測試

        [Theory]
        [InlineData(DatabaseType.SQLServer, "Id", "@Id")]
        [InlineData(DatabaseType.MySQL, "Id", "@Id")]
        [InlineData(DatabaseType.SQLite, "Id", "@Id")]
        [InlineData(DatabaseType.Oracle, "Id", ":Id")]
        [InlineData(DatabaseType.PostgreSQL, "Id", "@Id")]
        [DisplayName("GetParameterName 應依資料庫類型加上對應前綴")]
        public void GetParameterName_AppendsPrefix(DatabaseType dbType, string name, string expected)
        {
            var result = dbType.GetParameterName(name);
            Assert.Equal(expected, result);
        }

        #endregion
    }
}
