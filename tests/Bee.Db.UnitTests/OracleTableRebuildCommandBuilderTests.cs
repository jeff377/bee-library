using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.Oracle;
using Bee.Db.Schema;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// Pure-syntax tests for the Oracle rebuild command builder. Verifies the 6-step
    /// rebuild script (drop tmp / create tmp / copy data / drop old / rename / recreate
    /// secondary indexes) emits the expected fragments with double-quote quoting and the
    /// Oracle-specific PL/SQL DROP-IF-EXISTS pattern.
    /// </summary>
    /// <remarks>
    /// The rebuild builder is internal; tests resolve it via
    /// <see cref="OracleDialectFactory.CreateTableRebuildCommandBuilder"/> rather than
    /// instantiating it directly.
    /// </remarks>
    [Collection("Initialize")]
    public class OracleTableRebuildCommandBuilderTests
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
            var rebuilder = new OracleDialectFactory().CreateTableRebuildCommandBuilder();
            return rebuilder.GetCommandText(diff);
        }

        [Fact]
        [DisplayName("Oracle GetCommandText：rebuild 腳本應含 tmp 表建立、INSERT 與 RENAME（雙引號識別符）")]
        public void GetCommandText_BasicRebuild_IncludesTmpCreateInsertAndRename()
        {
            string sql = BuildSql(BuildDefineSchema(), BuildRealSchema());

            Assert.Contains("tmp_st_demo", sql);
            Assert.Contains("INSERT INTO \"tmp_st_demo\"", sql);
            Assert.Contains("ALTER TABLE \"tmp_st_demo\" RENAME TO \"st_demo\"", sql);
        }

        [Fact]
        [DisplayName("Oracle GetCommandText：tmp 表 CREATE 應走 OracleCreateTableCommandBuilder（雙引號識別符 + 無 ENGINE/CHARSET 後綴）")]
        public void GetCommandText_TmpTableCreate_UsesOracleCreateTableShape()
        {
            string sql = BuildSql(BuildDefineSchema(), BuildRealSchema());

            // tmp 表透過 OracleCreateTableCommandBuilder 建立；不應出現 MySQL/SQLite 風格後綴
            Assert.Contains("CREATE TABLE \"tmp_st_demo\"", sql);
            Assert.DoesNotContain("ENGINE=InnoDB", sql);
            Assert.DoesNotContain("COLLATE=", sql);
        }

        [Fact]
        [DisplayName("Oracle GetCommandText：DROP TABLE 應包在 PL/SQL block 內並 swallow ORA-00942（取代 IF EXISTS）")]
        public void GetCommandText_DropTable_UsesPlSqlExceptionSuppression()
        {
            string sql = BuildSql(BuildDefineSchema(), BuildRealSchema());

            // Oracle 無 DROP TABLE IF EXISTS；DDL 包在 anonymous block 中，當 ORA-00942 才忽略
            Assert.Contains("EXECUTE IMMEDIATE 'DROP TABLE \"tmp_st_demo\" CASCADE CONSTRAINTS'", sql);
            Assert.Contains("EXECUTE IMMEDIATE 'DROP TABLE \"st_demo\" CASCADE CONSTRAINTS'", sql);
            Assert.Contains("WHEN OTHERS THEN IF SQLCODE != -942 THEN RAISE", sql);
            Assert.DoesNotContain("DROP TABLE IF EXISTS", sql);
        }

        [Fact]
        [DisplayName("Oracle GetCommandText：新增欄位不應出現在 INSERT ... SELECT 清單")]
        public void GetCommandText_AddedField_ExcludedFromDataCopy()
        {
            string sql = BuildSql(BuildDefineSchema(), BuildRealSchema());

            // age 欄位應出現在 tmp 表定義
            Assert.Contains("\"age\"", sql);
            // 但 INSERT ... SELECT 子句不應含 age
            int insertIdx = sql.IndexOf("INSERT INTO \"tmp_st_demo\"", StringComparison.Ordinal);
            int selectIdx = sql.IndexOf("FROM \"st_demo\"", insertIdx, StringComparison.Ordinal);
            string insertSelectSection = sql.Substring(insertIdx, selectIdx - insertIdx);
            Assert.DoesNotContain("\"age\"", insertSelectSection);
        }

        [Fact]
        [DisplayName("Oracle GetCommandText：real-only 欄位（extension field）應保留於 rebuild 結果")]
        public void GetCommandText_ExtensionField_Preserved()
        {
            string sql = BuildSql(BuildDefineSchema(), BuildRealSchema(withExtraLegacyField: true));

            Assert.Contains("\"legacy_col\"", sql);
        }

        [Fact]
        [DisplayName("Oracle GetCommandText：tmp 表不應預建非 PK 索引")]
        public void GetCommandText_TmpTable_OmitsSecondaryIndexes()
        {
            string sql = BuildSql(BuildDefineSchema(), BuildRealSchema());

            // 不應出現 ix_tmp_st_demo_name；非 PK 索引在 RENAME 後才以真實名建立
            Assert.DoesNotContain("ix_tmp_st_demo_name", sql);
        }

        [Fact]
        [DisplayName("Oracle GetCommandText：RENAME 後應以真實名重建非 PK 索引")]
        public void GetCommandText_NonPkIndexes_RecreatedWithRealNames()
        {
            string sql = BuildSql(BuildDefineSchema(), BuildRealSchema());

            int renameIdx = sql.IndexOf("RENAME TO \"st_demo\"", StringComparison.Ordinal);
            int recreateIdx = sql.IndexOf("CREATE INDEX \"ix_st_demo_name\"", StringComparison.Ordinal);

            Assert.True(renameIdx > 0, "RENAME 步驟必須出現");
            Assert.True(recreateIdx > renameIdx, "非 PK 索引必須在 RENAME 之後重建");
            Assert.Contains("ON \"st_demo\" (\"name\" ASC)", sql);
        }

        [Fact]
        [DisplayName("Oracle GetCommandText：腳本中不應出現 ALTER INDEX RENAME（與其他 dialect 一致）")]
        public void GetCommandText_NeverEmitsAlterIndexRename()
        {
            string sql = BuildSql(BuildDefineSchema(), BuildRealSchema());

            Assert.DoesNotContain("ALTER INDEX", sql);
            Assert.DoesNotContain("RENAME INDEX", sql);
        }

        [Fact]
        [DisplayName("Oracle GetCommandText：new-table diff 應 throw（應改走 CREATE 路徑）")]
        public void GetCommandText_NewTableDiff_Throws()
        {
            var define = BuildDefineSchema();
            var diff = new TableSchemaComparer(define, realTable: null).CompareToDiff();
            var rebuilder = new OracleDialectFactory().CreateTableRebuildCommandBuilder();

            Assert.Throws<InvalidOperationException>(() => rebuilder.GetCommandText(diff));
        }

        [Fact]
        [DisplayName("Oracle GetCommandText：DROP TABLE 應加 CASCADE CONSTRAINTS（移除 referencing FK）")]
        public void GetCommandText_DropTable_IncludesCascadeConstraints()
        {
            string sql = BuildSql(BuildDefineSchema(), BuildRealSchema());

            Assert.Contains("CASCADE CONSTRAINTS", sql);
        }

        [Fact]
        [DisplayName("Oracle GetCommandText：腳本應依序為 drop tmp / create tmp / insert / drop old / rename / recreate index")]
        public void GetCommandText_StepsInExpectedOrder()
        {
            string sql = BuildSql(BuildDefineSchema(), BuildRealSchema());

            int dropTmpIdx = sql.IndexOf("DROP TABLE \"tmp_st_demo\"", StringComparison.Ordinal);
            int createTmpIdx = sql.IndexOf("CREATE TABLE \"tmp_st_demo\"", StringComparison.Ordinal);
            int insertIdx = sql.IndexOf("INSERT INTO \"tmp_st_demo\"", StringComparison.Ordinal);
            int dropOldIdx = sql.IndexOf("DROP TABLE \"st_demo\"", StringComparison.Ordinal);
            int renameIdx = sql.IndexOf("RENAME TO \"st_demo\"", StringComparison.Ordinal);
            int recreateIdx = sql.IndexOf("CREATE INDEX \"ix_st_demo_name\"", StringComparison.Ordinal);

            Assert.True(dropTmpIdx >= 0 && createTmpIdx > dropTmpIdx, "drop tmp → create tmp");
            Assert.True(createTmpIdx > 0 && insertIdx > createTmpIdx, "create tmp → insert");
            Assert.True(insertIdx > 0 && dropOldIdx > insertIdx, "insert → drop old");
            Assert.True(dropOldIdx > 0 && renameIdx > dropOldIdx, "drop old → rename");
            Assert.True(renameIdx > 0 && recreateIdx > renameIdx, "rename → recreate index");
        }
    }
}
