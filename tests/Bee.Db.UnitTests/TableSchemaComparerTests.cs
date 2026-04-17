using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Schema;
using Bee.Definition;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class TableSchemaComparerTests
    {
        private static TableSchema BuildBaseSchema(string tableName = "st_demo")
        {
            var schema = new TableSchema { TableName = tableName };
            schema.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            schema.Fields!.Add("name", "Name", FieldDbType.String, 50);
            schema.Indexes!.AddPrimaryKey(SysFields.RowId);
            return schema;
        }

        // 模擬從資料庫讀回的真實資料表結構，索引名稱已經是格式化後的字串（非樣板）
        private static TableSchema BuildRealSchema(string tableName = "st_demo")
        {
            var schema = new TableSchema { TableName = tableName };
            schema.Fields!.Add(SysFields.RowId, "Row ID", FieldDbType.Guid);
            schema.Fields!.Add("name", "Name", FieldDbType.String, 50);
            var pk = new TableSchemaIndex
            {
                Name = $"pk_{tableName}",
                PrimaryKey = true,
                Unique = true
            };
            pk.IndexFields!.Add(SysFields.RowId);
            schema.Indexes!.Add(pk);
            return schema;
        }

        [Fact]
        [DisplayName("RealTable 為 null 時整張表應標記為 New")]
        public void Compare_NullRealTable_MarksTableAsNew()
        {
            var define = BuildBaseSchema();
            var comparer = new TableSchemaComparer(define, null);

            var result = comparer.Compare();

            Assert.Equal(DbUpgradeAction.New, result.UpgradeAction);
        }

        [Fact]
        [DisplayName("結構完全相同時 UpgradeAction 應為 None")]
        public void Compare_IdenticalSchemas_ReturnsNone()
        {
            var define = BuildBaseSchema();
            var real = BuildRealSchema();
            var comparer = new TableSchemaComparer(define, real);

            var result = comparer.Compare();

            Assert.Equal(DbUpgradeAction.None, result.UpgradeAction);
        }

        [Fact]
        [DisplayName("實際表缺少欄位時應將欄位標記為 New 並升級表")]
        public void Compare_MissingField_MarksFieldAsNewAndUpgradesTable()
        {
            var define = BuildBaseSchema();
            define.Fields!.Add("age", "Age", FieldDbType.Integer);

            var real = BuildRealSchema();
            var comparer = new TableSchemaComparer(define, real);

            var result = comparer.Compare();

            Assert.Equal(DbUpgradeAction.Upgrade, result.UpgradeAction);
            Assert.Equal(DbUpgradeAction.New, result.Fields!["age"].UpgradeAction);
        }

        [Fact]
        [DisplayName("欄位定義不同時應將欄位標記為 Upgrade 並升級表")]
        public void Compare_DifferentField_MarksFieldAsUpgrade()
        {
            var define = BuildBaseSchema();
            var real = BuildRealSchema();
            real.Fields!["name"].Length = 30;  // 與 define 的 50 不同

            var comparer = new TableSchemaComparer(define, real);
            var result = comparer.Compare();

            Assert.Equal(DbUpgradeAction.Upgrade, result.UpgradeAction);
            Assert.Equal(DbUpgradeAction.Upgrade, result.Fields!["name"].UpgradeAction);
        }

        [Fact]
        [DisplayName("實際表缺少索引時應將索引標記為 New 並升級表")]
        public void Compare_MissingIndex_MarksIndexAsNew()
        {
            var define = BuildBaseSchema();
            define.Indexes!.Add("ix_{0}_name", "name", false);
            var real = BuildRealSchema();

            var comparer = new TableSchemaComparer(define, real);
            var result = comparer.Compare();

            Assert.Equal(DbUpgradeAction.Upgrade, result.UpgradeAction);
            var idx = result.Indexes!["ix_{0}_name"];
            Assert.Equal(DbUpgradeAction.New, idx.UpgradeAction);
        }

        [Fact]
        [DisplayName("索引定義不同時應將索引標記為 Upgrade")]
        public void Compare_DifferentIndex_MarksIndexAsUpgrade()
        {
            var define = BuildBaseSchema();
            define.Indexes!.Add("ix_{0}_name", "name", true);

            var real = BuildRealSchema();
            // 真實表的索引名稱已格式化為 "ix_st_demo_name"，且 unique 標記不同
            real.Indexes!.Add("ix_st_demo_name", "name", false);

            var comparer = new TableSchemaComparer(define, real);
            var result = comparer.Compare();

            Assert.Equal(DbUpgradeAction.Upgrade, result.UpgradeAction);
            Assert.Equal(DbUpgradeAction.Upgrade, result.Indexes!["ix_{0}_name"].UpgradeAction);
        }

        [Fact]
        [DisplayName("實際表多出的欄位應追加到比較結果中")]
        public void Compare_ExtraFieldInRealTable_AppendsExtensionField()
        {
            var define = BuildBaseSchema();
            // 觸發 Upgrade 才會走 AddExtensionFields
            define.Fields!.Add("age", "Age", FieldDbType.Integer);

            var real = BuildRealSchema();
            real.Fields!.Add("legacy_col", "Legacy", FieldDbType.String, 10);

            var comparer = new TableSchemaComparer(define, real);
            var result = comparer.Compare();

            Assert.Equal(DbUpgradeAction.Upgrade, result.UpgradeAction);
            Assert.True(result.Fields!.Contains("legacy_col"));
        }

        [Fact]
        [DisplayName("DefineTable 與 RealTable 屬性應正確暴露於 Comparer")]
        public void Properties_ExposeInputs()
        {
            var define = BuildBaseSchema();
            var real = BuildRealSchema();
            var comparer = new TableSchemaComparer(define, real);

            Assert.Same(define, comparer.DefineTable);
            Assert.Same(real, comparer.RealTable);
        }
    }
}
