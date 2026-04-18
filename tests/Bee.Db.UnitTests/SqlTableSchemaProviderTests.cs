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
    }
}
