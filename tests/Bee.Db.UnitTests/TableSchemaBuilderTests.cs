using System.ComponentModel;
using Bee.Definition;
using Bee.Db.Schema;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class TableSchemaBuilderTests
    {
        [DbFact]
        [DisplayName("TableSchemaBuilder 比對結構一致的資料表應回傳 None")]
        public void Compare_UpToDateTable_ReturnsNoneAction()
        {
            var builder = new TableSchemaBuilder("common");
            var result = builder.Compare("common", "st_user");

            Assert.NotNull(result);
            Assert.Equal(DbUpgradeAction.None, result.UpgradeAction);
        }

        [DbFact]
        [DisplayName("TableSchemaBuilder 取得命令文字應回傳空字串（結構已同步）")]
        public void GetCommandText_UpToDateTable_ReturnsEmpty()
        {
            var builder = new TableSchemaBuilder("common");
            string sql = builder.GetCommandText("common", "st_user");

            Assert.Equal(string.Empty, sql);
        }

        [DbFact]
        [DisplayName("TableSchemaBuilder Execute 結構已同步時應回傳 false")]
        public void Execute_UpToDateTable_ReturnsFalse()
        {
            var builder = new TableSchemaBuilder("common");
            bool upgraded = builder.Execute("common", "st_user");

            Assert.False(upgraded);
        }
    }
}
