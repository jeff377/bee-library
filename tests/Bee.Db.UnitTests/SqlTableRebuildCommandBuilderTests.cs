using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers.SqlServer;
using Bee.Db.Schema;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class SqlTableRebuildCommandBuilderTests
    {
        private static TableSchema BuildDefineSchema()
        {
            var schema = new TableSchema { TableName = "st_demo" };
            schema.Fields!.Add("id", "Id", FieldDbType.Guid);
            schema.Fields!.Add("name", "Name", FieldDbType.String, 50);
            schema.Fields!.Add("age", "Age", FieldDbType.Integer);
            schema.Indexes!.AddPrimaryKey("id");
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
        [DisplayName("GetCommandText：rebuild 腳本應含 tmp 表建立、INSERT 與 rename")]
        public void GetCommandText_BasicRebuild_IncludesTmpCreateInsertAndRename()
        {
            var define = BuildDefineSchema();
            var real = BuildRealSchema();
            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            var sql = SqlTableRebuildCommandBuilder.GetCommandText(diff);

            Assert.Contains("tmp_st_demo", sql);
            Assert.Contains("INSERT INTO [tmp_st_demo]", sql);
            Assert.Contains("sp_rename", sql);
        }

        [Fact]
        [DisplayName("GetCommandText：新增欄位不應出現在 INSERT ... SELECT 清單")]
        public void GetCommandText_AddedField_ExcludedFromDataCopy()
        {
            var define = BuildDefineSchema();
            var real = BuildRealSchema();
            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            var sql = SqlTableRebuildCommandBuilder.GetCommandText(diff);

            // 新欄位 age 應在 tmp 定義中出現
            Assert.Contains("[age]", sql);
            // 但 INSERT 欄位清單不應含 age（只複製既有欄位 id, name）
            int insertIdx = sql.IndexOf("INSERT INTO [tmp_st_demo]", StringComparison.Ordinal);
            int selectIdx = sql.IndexOf("FROM [st_demo]", insertIdx, StringComparison.Ordinal);
            string insertSelectSection = sql.Substring(insertIdx, selectIdx - insertIdx);
            Assert.DoesNotContain("[age]", insertSelectSection);
        }

        [Fact]
        [DisplayName("GetCommandText：real-only 欄位（extension field）應保留於 rebuild 結果")]
        public void GetCommandText_ExtensionField_Preserved()
        {
            var define = BuildDefineSchema();
            var real = BuildRealSchema(withExtraLegacyField: true);
            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            var sql = SqlTableRebuildCommandBuilder.GetCommandText(diff);

            Assert.Contains("[legacy_col]", sql);
        }

        [Fact]
        [DisplayName("GetCommandText：new-table diff 應 throw（應改走 CREATE 路徑）")]
        public void GetCommandText_NewTableDiff_Throws()
        {
            var define = BuildDefineSchema();
            var diff = new TableSchemaComparer(define, realTable: null).CompareToDiff();

            Assert.Throws<InvalidOperationException>(() => SqlTableRebuildCommandBuilder.GetCommandText(diff));
        }
    }
}
