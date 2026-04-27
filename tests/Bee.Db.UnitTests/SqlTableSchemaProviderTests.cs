using System.ComponentModel;
using Bee.Db.Providers.SqlServer;
using Bee.Definition;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class SqlTableSchemaProviderTests
    {
        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SqlTableSchemaProvider 取得資料表結構應成功")]
        public void GetTableSchema_ValidTableName_ReturnsSchema()
        {
            var helper = new SqlTableSchemaProvider("common_sqlserver");
            var dbTable = helper.GetTableSchema("st_user");
            Assert.NotNull(dbTable);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SqlTableSchemaProvider 取得不存在資料表應回傳 null")]
        public void GetTableSchema_NonExistentTable_ReturnsNull()
        {
            var helper = new SqlTableSchemaProvider("common_sqlserver");
            var dbTable = helper.GetTableSchema("bee_nonexistent_table_xyz_99999");
            Assert.Null(dbTable);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SqlTableSchemaProvider DatabaseId 應等於建構子傳入的值")]
        public void Constructor_DatabaseId_IsSet()
        {
            var helper = new SqlTableSchemaProvider("common_sqlserver");
            Assert.Equal("common_sqlserver", helper.DatabaseId);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("GetTableSchema 應從 extended property 讀回表層 DisplayName")]
        public void GetTableSchema_WithExtendedProperty_ReturnsDisplayName()
        {
            string tableName = $"bee_test_desc_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess("common_sqlserver");
            try
            {
                // 建立只有一個欄位的測試表
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE [{tableName}] ([id] [int] NOT NULL);"));
                // 寫入表層 extended property
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    "EXEC sp_addextendedproperty @name=N'MS_Description', @value=N'測試表說明'," +
                    $" @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'{tableName}';"));

                var provider = new SqlTableSchemaProvider("common_sqlserver");
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.Equal("測試表說明", schema!.DisplayName);
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"IF (SELECT COUNT(*) FROM sys.tables WHERE name=N'{tableName}')>0 DROP TABLE [{tableName}];"));
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("GetTableSchema 無 extended property 時 DisplayName 應為空字串")]
        public void GetTableSchema_WithoutExtendedProperty_ReturnsEmptyDisplayName()
        {
            string tableName = $"bee_test_desc_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess("common_sqlserver");
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE [{tableName}] ([id] [int] NOT NULL);"));

                var provider = new SqlTableSchemaProvider("common_sqlserver");
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.Equal(string.Empty, schema!.DisplayName);
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"IF (SELECT COUNT(*) FROM sys.tables WHERE name=N'{tableName}')>0 DROP TABLE [{tableName}];"));
            }
        }
    }
}
