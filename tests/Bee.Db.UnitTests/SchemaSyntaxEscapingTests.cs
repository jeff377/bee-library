using System.ComponentModel;
using System.Globalization;
using Bee.Definition.Database;
using Bee.Tests.Shared;
using Bee.Db.Providers.MySql;
using Bee.Db.Providers.Oracle;
using Bee.Db.Providers.PostgreSql;
using Bee.Db.Providers.Sqlite;
using Bee.Db.Providers.SqlServer;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 各方言 <c>EscapeSqlString</c> 的字面值逸出規則。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 這些值（欄位 Caption、資料表 DisplayName、DefaultValue）是定義檔裡的自由文字，會被拼進
    /// DDL 的 <c>'...'</c> 字面值。逸出不足不只是注入面 —— <b>以反斜線結尾的正常說明文字</b>
    /// 在 MySQL 上就足以讓產生的 DDL 語法錯誤。
    /// </para>
    /// <para>
    /// MySQL 是唯一需要處理反斜線的方言：SQL Server、Oracle、SQLite，以及預設開著
    /// <c>standard_conforming_strings</c> 的 PostgreSQL，反斜線都是普通字元。這裡把「哪些方言要
    /// 逸出反斜線」釘成測試，免得日後有人「順手統一」而弄壞其中一邊。
    /// </para>
    /// </remarks>
    public class SchemaSyntaxEscapingTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public SchemaSyntaxEscapingTests(SharedDbFixture fx) { _fx = fx; }

        [Theory]
        [InlineData("plain", "plain")]
        [InlineData("O'Brien", "O''Brien")]
        [InlineData("''", "''''")]
        [DisplayName("所有方言都應把單引號加倍")]
        public void AllDialects_DoubleSingleQuotes(string input, string expected)
        {
            Assert.Equal(expected, SqlSchemaSyntax.EscapeSqlString(input));
            Assert.Equal(expected, PgSchemaSyntax.EscapeSqlString(input));
            Assert.Equal(expected, OracleSchemaSyntax.EscapeSqlString(input));
            Assert.Equal(expected, SqliteSchemaSyntax.EscapeSqlString(input));
            Assert.Equal(expected, MySqlSchemaSyntax.EscapeSqlString(input));
        }

        [Theory]
        [InlineData(@"ends with backslash \", @"ends with backslash \\")]
        [InlineData(@"a\' , (SELECT 1) , '", @"a\\'' , (SELECT 1) , ''")]
        [InlineData(@"C:\temp\file", @"C:\\temp\\file")]
        [DisplayName("MySQL 必須額外逸出反斜線（它預設把 \\ 當逸出字元）")]
        public void MySql_EscapesBackslash(string input, string expected)
        {
            Assert.Equal(expected, MySqlSchemaSyntax.EscapeSqlString(input));
        }

        [Theory]
        [InlineData(@"ends with backslash \")]
        [InlineData(@"C:\temp\file")]
        [DisplayName("其餘方言不得逸出反斜線（那裡它是普通字元，動它會改變值）")]
        public void OtherDialects_LeaveBackslashAlone(string input)
        {
            // 反向的錯誤同樣有害：在這些方言上多加一個反斜線會讓存進去的說明文字與原文不符。
            Assert.Equal(input, SqlSchemaSyntax.EscapeSqlString(input));
            Assert.Equal(input, PgSchemaSyntax.EscapeSqlString(input));
            Assert.Equal(input, OracleSchemaSyntax.EscapeSqlString(input));
            Assert.Equal(input, SqliteSchemaSyntax.EscapeSqlString(input));
        }
    
        [DbTheory(DatabaseType.MySQL)]
        [InlineData(@"ends with backslash \")]
        [InlineData(@"a\' , (SELECT 1) , '")]
        [InlineData("O'Brien")]
        [DisplayName("MySQL：帶反斜線／引號的欄位說明應能真的建出表並原值讀回")]
        public void MySql_ColumnComment_WithBackslashOrQuote_SurvivesRealDdl(string caption)
        {
            // 單元測試只驗字串規則，證不了 MySQL 接不接受 —— 這條把它送進真的 DDL。
            var dbAccess = _fx.NewDbAccess(TestDbConventions.GetDatabaseId(DatabaseType.MySQL));
            string table = "tb_esc_" + Guid.NewGuid().ToString("N")[..8];
            string quoted = DatabaseType.MySQL.QuoteIdentifier(table);

            string comment = MySqlSchemaSyntax.EscapeSqlString(caption);
            dbAccess.ExecuteNonQuery(
                $"CREATE TABLE {quoted} (id INT NOT NULL COMMENT '{comment}')");
            try
            {
                var stored = Convert.ToString(dbAccess.ExecuteScalar(
                    "SELECT column_comment FROM information_schema.columns " +
                    $"WHERE table_schema = DATABASE() AND table_name = {{0}} AND column_name = 'id'",
                    table), CultureInfo.InvariantCulture);

                Assert.Equal(caption, stored);
            }
            finally
            {
                dbAccess.ExecuteNonQuery($"DROP TABLE {quoted}");
            }
        }
    }
}
