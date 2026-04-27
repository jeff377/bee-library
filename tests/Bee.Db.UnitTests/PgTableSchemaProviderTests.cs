using System.ComponentModel;
using Bee.Db.Providers.PostgreSql;
using Bee.Definition;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class PgTableSchemaProviderTests
    {
        private const string DatabaseId = "common_postgresql";

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PgTableSchemaProvider 取得資料表結構應成功")]
        public void GetTableSchema_ValidTableName_ReturnsSchema()
        {
            var helper = new PgTableSchemaProvider(DatabaseId);
            var dbTable = helper.GetTableSchema("st_user");
            Assert.NotNull(dbTable);
        }

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PgTableSchemaProvider 取得不存在資料表應回傳 null")]
        public void GetTableSchema_NonExistentTable_ReturnsNull()
        {
            var helper = new PgTableSchemaProvider(DatabaseId);
            var dbTable = helper.GetTableSchema("bee_nonexistent_table_xyz_99999");
            Assert.Null(dbTable);
        }

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PgTableSchemaProvider DatabaseId 應等於建構子傳入的值")]
        public void Constructor_DatabaseId_IsSet()
        {
            var helper = new PgTableSchemaProvider(DatabaseId);
            Assert.Equal(DatabaseId, helper.DatabaseId);
        }

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("GetTableSchema 應從 COMMENT ON TABLE 讀回表層 DisplayName")]
        public void GetTableSchema_WithComment_ReturnsDisplayName()
        {
            string tableName = $"bee_test_desc_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess(DatabaseId);
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE \"{tableName}\" (\"id\" integer NOT NULL);"));
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"COMMENT ON TABLE \"{tableName}\" IS '測試表說明';"));

                var provider = new PgTableSchemaProvider(DatabaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.Equal("測試表說明", schema!.DisplayName);
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DROP TABLE IF EXISTS \"{tableName}\";"));
            }
        }

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("GetTableSchema 無 COMMENT 時 DisplayName 應為空字串")]
        public void GetTableSchema_WithoutComment_ReturnsEmptyDisplayName()
        {
            string tableName = $"bee_test_desc_{Guid.NewGuid():N}";
            var dbAccess = new DbAccess(DatabaseId);
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"CREATE TABLE \"{tableName}\" (\"id\" integer NOT NULL);"));

                var provider = new PgTableSchemaProvider(DatabaseId);
                var schema = provider.GetTableSchema(tableName);

                Assert.NotNull(schema);
                Assert.Equal(string.Empty, schema!.DisplayName);
            }
            finally
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"DROP TABLE IF EXISTS \"{tableName}\";"));
            }
        }
    }
}
