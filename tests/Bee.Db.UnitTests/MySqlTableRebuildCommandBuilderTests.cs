using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.MySql;
using Bee.Db.Schema;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// Pure-syntax tests for the MySQL rebuild command builder. Verifies the 6-step
    /// rebuild script (drop tmp / create tmp / copy data / drop old / rename / recreate
    /// secondary indexes) emits the expected fragments with backtick quoting and the
    /// MySQL CREATE TABLE table suffix.
    /// </summary>
    /// <remarks>
    /// The rebuild builder is internal; tests resolve it via
    /// <see cref="MySqlDialectFactory.CreateTableRebuildCommandBuilder"/> rather than
    /// instantiating it directly.
    /// </remarks>
    [Collection("Initialize")]
    public class MySqlTableRebuildCommandBuilderTests
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

        private static string BuildSql(TableSchema define, TableSchema? real)
        {
            var diff = new TableSchemaComparer(define, real).CompareToDiff();
            var rebuilder = new MySqlDialectFactory().CreateTableRebuildCommandBuilder();
            return rebuilder.GetCommandText(diff);
        }

        [Fact]
        [DisplayName("MySQL GetCommandText：rebuild 腳本應含 tmp 表建立、INSERT 與 RENAME（backtick 識別符）")]
        public void GetCommandText_BasicRebuild_IncludesTmpCreateInsertAndRename()
        {
            string sql = BuildSql(BuildDefineSchema(), BuildRealSchema());

            Assert.Contains("tmp_st_demo", sql);
            Assert.Contains("INSERT INTO `tmp_st_demo`", sql);
            Assert.Contains("ALTER TABLE `tmp_st_demo` RENAME TO `st_demo`", sql);
        }

        [Fact]
        [DisplayName("MySQL GetCommandText：tmp 表 CREATE 應帶 ENGINE=InnoDB 與 utf8mb4_0900_ai_ci collation")]
        public void GetCommandText_TmpTableCreate_IncludesMySqlTableSuffix()
        {
            string sql = BuildSql(BuildDefineSchema(), BuildRealSchema());

            // tmp 表透過 MySqlCreateTableCommandBuilder 建立，必然帶 MySQL 的 table suffix；
            // CI collation day-1 內建確保 rebuild 後表行為與原表一致。
            Assert.Contains("ENGINE=InnoDB", sql);
            Assert.Contains("COLLATE=utf8mb4_0900_ai_ci", sql);
        }

        [Fact]
        [DisplayName("MySQL GetCommandText：DROP TABLE 應使用 IF EXISTS")]
        public void GetCommandText_DropTable_UsesIfExists()
        {
            string sql = BuildSql(BuildDefineSchema(), BuildRealSchema());

            Assert.Contains("DROP TABLE IF EXISTS `tmp_st_demo`;", sql);
            Assert.Contains("DROP TABLE IF EXISTS `st_demo`;", sql);
        }

        [Fact]
        [DisplayName("MySQL GetCommandText：新增欄位不應出現在 INSERT ... SELECT 清單")]
        public void GetCommandText_AddedField_ExcludedFromDataCopy()
        {
            string sql = BuildSql(BuildDefineSchema(), BuildRealSchema());

            // age 欄位應出現在 tmp 表定義
            Assert.Contains("`age`", sql);
            // 但 INSERT ... SELECT 子句不應含 age
            int insertIdx = sql.IndexOf("INSERT INTO `tmp_st_demo`", StringComparison.Ordinal);
            int selectIdx = sql.IndexOf("FROM `st_demo`", insertIdx, StringComparison.Ordinal);
            string insertSelectSection = sql.Substring(insertIdx, selectIdx - insertIdx);
            Assert.DoesNotContain("`age`", insertSelectSection);
        }

        [Fact]
        [DisplayName("MySQL GetCommandText：real-only 欄位（extension field）應保留於 rebuild 結果")]
        public void GetCommandText_ExtensionField_Preserved()
        {
            string sql = BuildSql(BuildDefineSchema(), BuildRealSchema(withExtraLegacyField: true));

            Assert.Contains("`legacy_col`", sql);
        }

        [Fact]
        [DisplayName("MySQL GetCommandText：tmp 表不應預建非 PK 索引")]
        public void GetCommandText_TmpTable_OmitsSecondaryIndexes()
        {
            string sql = BuildSql(BuildDefineSchema(), BuildRealSchema());

            // 不應出現 ix_tmp_st_demo_name；非 PK 索引在 RENAME 後才以真實名建立
            Assert.DoesNotContain("ix_tmp_st_demo_name", sql);
        }

        [Fact]
        [DisplayName("MySQL GetCommandText：RENAME 後應以真實名重建非 PK 索引")]
        public void GetCommandText_NonPkIndexes_RecreatedWithRealNames()
        {
            string sql = BuildSql(BuildDefineSchema(), BuildRealSchema());

            int renameIdx = sql.IndexOf("RENAME TO `st_demo`", StringComparison.Ordinal);
            int recreateIdx = sql.IndexOf("CREATE INDEX `ix_st_demo_name`", StringComparison.Ordinal);

            Assert.True(renameIdx > 0, "RENAME 步驟必須出現");
            Assert.True(recreateIdx > renameIdx, "非 PK 索引必須在 RENAME 之後重建");
            Assert.Contains("ON `st_demo` (`name` ASC)", sql);
        }

        [Fact]
        [DisplayName("MySQL GetCommandText：腳本中不應出現 ALTER INDEX RENAME（與 SQLite/PG pattern 一致，避免方言差異）")]
        public void GetCommandText_NeverEmitsAlterIndexRename()
        {
            string sql = BuildSql(BuildDefineSchema(), BuildRealSchema());

            Assert.DoesNotContain("ALTER INDEX", sql);
            Assert.DoesNotContain("RENAME INDEX", sql);
        }

        [Fact]
        [DisplayName("MySQL GetCommandText：new-table diff 應 throw（應改走 CREATE 路徑）")]
        public void GetCommandText_NewTableDiff_Throws()
        {
            var define = BuildDefineSchema();
            var diff = new TableSchemaComparer(define, realTable: null).CompareToDiff();
            var rebuilder = new MySqlDialectFactory().CreateTableRebuildCommandBuilder();

            Assert.Throws<InvalidOperationException>(() => rebuilder.GetCommandText(diff));
        }
    }
}
