using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.Sqlite;
using Bee.Db.Schema;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class SqliteTableRebuildCommandBuilderTests
    {
        private static TableSchema BuildDefineSchema()
        {
            var schema = new TableSchema { TableName = "st_demo" };
            schema.Fields!.Add("id", "Id", FieldDbType.Guid);
            schema.Fields!.Add("name", "Name", FieldDbType.String, 50);
            schema.Fields!.Add("age", "Age", FieldDbType.Integer);
            schema.Indexes!.AddPrimaryKey("id");
            schema.Indexes!.Add("ix_{0}_name", "name", false);
            return schema;
        }

        private static TableSchema BuildRealSchema(bool withExtraLegacyField = false)
        {
            var schema = new TableSchema { TableName = "st_demo" };
            schema.Fields!.Add("id", "Id", FieldDbType.Guid);
            schema.Fields!.Add("name", "Name", FieldDbType.String, 50);
            if (withExtraLegacyField)
                schema.Fields!.Add("legacy_col", "Legacy", FieldDbType.String, 10);
            var pk = new TableSchemaIndex { Name = "pk_st_demo", PrimaryKey = true, Unique = true };
            pk.IndexFields!.Add("id");
            schema.Indexes!.Add(pk);
            return schema;
        }

        [Fact]
        [DisplayName("SQLite GetCommandText：rebuild 腳本應含 tmp 表建立、INSERT 與 RENAME")]
        public void GetCommandText_BasicRebuild_IncludesTmpCreateInsertAndRename()
        {
            var define = BuildDefineSchema();
            var real = BuildRealSchema();
            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            var sql = new SqliteTableRebuildCommandBuilder().GetCommandText(diff);

            Assert.Contains("tmp_st_demo", sql);
            Assert.Contains("INSERT INTO \"tmp_st_demo\"", sql);
            Assert.Contains("ALTER TABLE \"tmp_st_demo\" RENAME TO \"st_demo\"", sql);
        }

        [Fact]
        [DisplayName("SQLite GetCommandText：DROP TABLE 應使用 IF EXISTS")]
        public void GetCommandText_DropTable_UsesIfExists()
        {
            var define = BuildDefineSchema();
            var real = BuildRealSchema();
            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            var sql = new SqliteTableRebuildCommandBuilder().GetCommandText(diff);

            Assert.Contains("DROP TABLE IF EXISTS \"tmp_st_demo\";", sql);
            Assert.Contains("DROP TABLE IF EXISTS \"st_demo\";", sql);
        }

        [Fact]
        [DisplayName("SQLite GetCommandText：新增欄位不應出現在 INSERT ... SELECT 清單")]
        public void GetCommandText_AddedField_ExcludedFromDataCopy()
        {
            var define = BuildDefineSchema();
            var real = BuildRealSchema();
            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            var sql = new SqliteTableRebuildCommandBuilder().GetCommandText(diff);

            // age 欄位應出現在 tmp 表定義
            Assert.Contains("\"age\"", sql);
            // 但 INSERT 欄位清單不應含 age
            int insertIdx = sql.IndexOf("INSERT INTO \"tmp_st_demo\"", StringComparison.Ordinal);
            int selectIdx = sql.IndexOf("FROM \"st_demo\"", insertIdx, StringComparison.Ordinal);
            string insertSelectSection = sql.Substring(insertIdx, selectIdx - insertIdx);
            Assert.DoesNotContain("\"age\"", insertSelectSection);
        }

        [Fact]
        [DisplayName("SQLite GetCommandText：real-only 欄位（extension field）應保留於 rebuild 結果")]
        public void GetCommandText_ExtensionField_Preserved()
        {
            var define = BuildDefineSchema();
            var real = BuildRealSchema(withExtraLegacyField: true);
            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            var sql = new SqliteTableRebuildCommandBuilder().GetCommandText(diff);

            Assert.Contains("\"legacy_col\"", sql);
        }

        [Fact]
        [DisplayName("SQLite GetCommandText：tmp 表不應預建非 PK 索引（沒有 ALTER INDEX RENAME）")]
        public void GetCommandText_TmpTable_OmitsSecondaryIndexes()
        {
            var define = BuildDefineSchema();
            var real = BuildRealSchema();
            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            var sql = new SqliteTableRebuildCommandBuilder().GetCommandText(diff);

            // 不應出現 ix_tmp_st_demo_name；非 PK 索引在 RENAME 後才以真實名建立
            Assert.DoesNotContain("ix_tmp_st_demo_name", sql);
        }

        [Fact]
        [DisplayName("SQLite GetCommandText：RENAME 後應以真實名重建非 PK 索引")]
        public void GetCommandText_NonPkIndexes_RecreatedWithRealNames()
        {
            var define = BuildDefineSchema();
            var real = BuildRealSchema();
            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            var sql = new SqliteTableRebuildCommandBuilder().GetCommandText(diff);

            int renameIdx = sql.IndexOf("RENAME TO \"st_demo\"", StringComparison.Ordinal);
            int recreateIdx = sql.IndexOf("CREATE INDEX \"ix_st_demo_name\"", StringComparison.Ordinal);

            Assert.True(renameIdx > 0, "RENAME 步驟必須出現");
            Assert.True(recreateIdx > renameIdx, "非 PK 索引必須在 RENAME 之後重建");
            Assert.Contains("ON \"st_demo\" (\"name\" ASC)", sql);
        }

        [Fact]
        [DisplayName("SQLite GetCommandText：腳本中不應出現 ALTER INDEX RENAME（SQLite 不支援）")]
        public void GetCommandText_NeverEmitsAlterIndexRename()
        {
            var define = BuildDefineSchema();
            var real = BuildRealSchema();
            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            var sql = new SqliteTableRebuildCommandBuilder().GetCommandText(diff);

            Assert.DoesNotContain("ALTER INDEX", sql);
        }

        [Fact]
        [DisplayName("SQLite GetCommandText：new-table diff 應 throw（應改走 CREATE 路徑）")]
        public void GetCommandText_NewTableDiff_Throws()
        {
            var define = BuildDefineSchema();
            var diff = new TableSchemaComparer(define, realTable: null).CompareToDiff();

            Assert.Throws<InvalidOperationException>(() => new SqliteTableRebuildCommandBuilder().GetCommandText(diff));
        }
    }
}
