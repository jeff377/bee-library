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

        [Fact]
        [DisplayName("TableSchemaBuilder 建構子傳入 null 應擲 ArgumentNullException")]
        public void Constructor_NullId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TableSchemaBuilder(null!));
        }

        [Fact]
        [DisplayName("TableSchemaBuilder 建構子傳入空字串應擲 ArgumentException")]
        public void Constructor_EmptyId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new TableSchemaBuilder(""));
        }

        [Fact]
        [DisplayName("TableSchemaBuilder 建構子傳入空白字串應擲 ArgumentException")]
        public void Constructor_WhitespaceId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new TableSchemaBuilder("   "));
        }

        [Fact]
        [DisplayName("TableSchemaBuilder 建構子傳入未登錄的資料庫 ID 應擲 KeyNotFoundException")]
        public void Constructor_UnknownId_ThrowsKeyNotFoundException()
        {
            Assert.Throws<KeyNotFoundException>(() => new TableSchemaBuilder("__nonexistent_db__"));
        }
    }
}
