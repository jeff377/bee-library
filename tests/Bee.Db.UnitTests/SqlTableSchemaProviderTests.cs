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
        [DisplayName("SqlTableSchemaProvider.GetTableSchema 不存在的資料表應回傳 null")]
        public void GetTableSchema_NonExistentTable_ReturnsNull()
        {
            var provider = new SqlTableSchemaProvider("common");

            var result = provider.GetTableSchema("nonexistent_bee_test_table_xyz");

            Assert.Null(result);
        }
    }
}
