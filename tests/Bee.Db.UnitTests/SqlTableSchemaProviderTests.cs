using System.ComponentModel;
using Bee.Base.Data;
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

        [DbFact]
        [DisplayName("GetTableSchema st_user 應包含主索引與非主索引（涵蓋 ParsePrimaryKey / ParseIndexes）")]
        public void GetTableSchema_StUser_HasPrimaryKeyAndNonPrimaryIndexes()
        {
            var provider = new SqlTableSchemaProvider("common");

            var schema = provider.GetTableSchema("st_user");

            Assert.NotNull(schema);
            Assert.NotNull(schema!.Indexes);
            Assert.True(schema.Indexes!.Count >= 3);
            Assert.True(schema.Indexes.Any(x => x.PrimaryKey));
            Assert.True(schema.Indexes.Any(x => !x.PrimaryKey));
        }

        [DbFact]
        [DisplayName("GetTableSchema st_user String 欄位 sys_id 長度應為 20（NVARCHAR 長度除以 2）")]
        public void GetTableSchema_StUser_StringFieldHasCorrectLength()
        {
            var provider = new SqlTableSchemaProvider("common");

            var schema = provider.GetTableSchema("st_user");

            Assert.NotNull(schema);
            var sysIdField = schema!.Fields!.FirstOrDefault(x => x.FieldName == "sys_id");
            Assert.NotNull(sysIdField);
            Assert.Equal(FieldDbType.String, sysIdField!.DbType);
            Assert.Equal(20, sysIdField.Length);
        }

        [DbFact]
        [DisplayName("GetTableSchema st_user AutoIncrement 欄位 sys_no 型別應為 AutoIncrement")]
        public void GetTableSchema_StUser_AutoIncrementFieldIsRecognized()
        {
            var provider = new SqlTableSchemaProvider("common");

            var schema = provider.GetTableSchema("st_user");

            Assert.NotNull(schema);
            var sysNoField = schema!.Fields!.FirstOrDefault(x => x.FieldName == "sys_no");
            Assert.NotNull(sysNoField);
            Assert.Equal(FieldDbType.AutoIncrement, sysNoField!.DbType);
        }
    }
}
