using System.ComponentModel;
using Bee.Db.Providers.SqlServer;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class SqlTableSchemaProviderTests
    {
        [DbFact]
        [DisplayName("SqlTableSchemaProvider 取得資料表結構應成功")]
        public void GetTableSchema_ValidTableName_ReturnsSchema()
        {
            var helper = new SqlTableSchemaProvider("common");
            var dbTable = helper.GetTableSchema("st_user");
            Assert.NotNull(dbTable);
        }

        [DbFact]
        [DisplayName("SqlTableSchemaProvider 取得不存在資料表應回傳 null")]
        public void GetTableSchema_NonExistentTable_ReturnsNull()
        {
            var helper = new SqlTableSchemaProvider("common");
            var dbTable = helper.GetTableSchema("bee_nonexistent_table_xyz_99999");
            Assert.Null(dbTable);
        }

        [DbFact]
        [DisplayName("SqlTableSchemaProvider DatabaseId 應等於建構子傳入的值")]
        public void Constructor_DatabaseId_IsSet()
        {
            var helper = new SqlTableSchemaProvider("common");
            Assert.Equal("common", helper.DatabaseId);
        }
    }
}
