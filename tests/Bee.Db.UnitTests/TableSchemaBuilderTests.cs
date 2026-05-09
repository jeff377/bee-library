using System.ComponentModel;
using Bee.Db.Schema;
using Bee.Tests.Shared;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class TableSchemaBuilderTests
    {
        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("TableSchemaBuilder 比對結構一致的資料表應回傳 None")]
        public void Compare_UpToDateTable_ReturnsNoneAction()
        {
            var builder = new TableSchemaBuilder("common_sqlserver");
            var result = builder.Compare("common", "st_user");

            Assert.NotNull(result);
            Assert.Equal(DbUpgradeAction.None, result.UpgradeAction);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("TableSchemaBuilder 取得命令文字應回傳空字串（結構已同步）")]
        public void GetCommandText_UpToDateTable_ReturnsEmpty()
        {
            var builder = new TableSchemaBuilder("common_sqlserver");
            string sql = builder.GetCommandText("common", "st_user");

            Assert.Equal(string.Empty, sql);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("TableSchemaBuilder Execute 結構已同步時應回傳 false")]
        public void Execute_UpToDateTable_ReturnsFalse()
        {
            var builder = new TableSchemaBuilder("common_sqlserver");
            bool upgraded = builder.Execute("common", "st_user");

            Assert.False(upgraded);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("GetCommandText 資料表不存在時應回傳非空 SQL 字串")]
        public void GetCommandText_TableNotExists_ReturnsNonEmptySql()
        {
            var builder = new TableSchemaBuilder("common_sqlserver");
            // ft_department 定義存在但 DB 中未建立，差異非空
            string sql = builder.GetCommandText("common", "ft_department");
            Assert.False(string.IsNullOrWhiteSpace(sql));
        }
    }
}
