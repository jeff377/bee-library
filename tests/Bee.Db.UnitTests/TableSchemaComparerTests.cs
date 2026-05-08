using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Schema;
using Bee.Db.Schema.Changes;
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

        [Fact]
        [DisplayName("只有 DisplayName 差異時 UpgradeAction=None 且 DescriptionChanges 含表層異動")]
        public void Compare_OnlyTableDisplayNameDiffers_NoUpgradeButDescriptionChanged()
        {
            var define = BuildBaseSchema();
            define.DisplayName = "示範資料表";
            var real = BuildRealSchema();
            real.DisplayName = string.Empty; // DB 尚未寫入

            var comparer = new TableSchemaComparer(define, real);
            var result = comparer.Compare();

            Assert.Equal(DbUpgradeAction.None, result.UpgradeAction);
            var change = Assert.Single(comparer.DescriptionChanges);
            Assert.Equal(DescriptionLevel.Table, change.Level);
            Assert.Equal("示範資料表", change.NewValue);
            Assert.True(change.IsNew);
        }

        [Fact]
        [DisplayName("只有欄位 Caption 差異時應產生 Column 層 DescriptionChange（更新模式）")]
        public void Compare_OnlyFieldCaptionDiffers_DescriptionChangeIsUpdate()
        {
            var define = BuildBaseSchema();
            define.Fields!["name"].Caption = "新名稱";
            var real = BuildRealSchema();
            real.Fields!["name"].Caption = "舊名稱"; // DB 已存在不同值

            var comparer = new TableSchemaComparer(define, real);
            var result = comparer.Compare();

            Assert.Equal(DbUpgradeAction.None, result.UpgradeAction);
            var change = Assert.Single(comparer.DescriptionChanges);
            Assert.Equal(DescriptionLevel.Column, change.Level);
            Assert.Equal("name", change.FieldName);
            Assert.Equal("新名稱", change.NewValue);
            Assert.False(change.IsNew);
        }

        [Fact]
        [DisplayName("Define 註解為空時採保守策略不產生 DescriptionChange")]
        public void Compare_EmptyDefineDescription_NoChangeGenerated()
        {
            var define = BuildBaseSchema();
            define.DisplayName = string.Empty;
            define.Fields!["name"].Caption = string.Empty;
            var real = BuildRealSchema();
            real.DisplayName = "DB 既有表說明";
            real.Fields!["name"].Caption = "DB 既有欄位說明";

            var comparer = new TableSchemaComparer(define, real);
            var result = comparer.Compare();

            Assert.Equal(DbUpgradeAction.None, result.UpgradeAction);
            Assert.Empty(comparer.DescriptionChanges);
        }

        [Fact]
        [DisplayName("結構與註解皆有差異時 UpgradeAction=Upgrade 且 DescriptionChanges 仍會填充（供上層判斷）")]
        public void Compare_SchemaAndDescriptionDiffer_UpgradePopulatesBoth()
        {
            var define = BuildBaseSchema();
            define.DisplayName = "新表說明";
            var real = BuildRealSchema();
            real.Fields!["name"].Length = 30; // 觸發 schema Upgrade

            var comparer = new TableSchemaComparer(define, real);
            var result = comparer.Compare();

            Assert.Equal(DbUpgradeAction.Upgrade, result.UpgradeAction);
            // DescriptionChanges 仍會產生，由上層決定是否消費
            Assert.Contains(comparer.DescriptionChanges, c => c.Level == DescriptionLevel.Table);
        }

        [Fact]
        [DisplayName("RealTable 為 null 時 DescriptionChanges 應為空（由 schema CREATE 路徑處理）")]
        public void Compare_NullRealTable_DescriptionChangesEmpty()
        {
            var define = BuildBaseSchema();
            define.DisplayName = "示範資料表";

            var comparer = new TableSchemaComparer(define, null);
            comparer.Compare();

            Assert.Empty(comparer.DescriptionChanges);
        }

        [Fact]
        [DisplayName("僅存在於 Real 的欄位不產生 DescriptionChange")]
        public void Compare_ExtraFieldInRealTable_NoColumnDescriptionChange()
        {
            var define = BuildBaseSchema();
            var real = BuildRealSchema();
            real.Fields!.Add("legacy_col", "舊欄位說明", FieldDbType.String, 10);

            var comparer = new TableSchemaComparer(define, real);
            comparer.Compare();

            // legacy_col 不在 define 中，不應產生對應的 Column DescriptionChange
            Assert.DoesNotContain(comparer.DescriptionChanges, c => c.FieldName == "legacy_col");
        }

        // ---- CompareToDiff ----

        [Fact]
        [DisplayName("CompareToDiff：RealTable 為 null 時 IsNewTable=true 且無 Changes")]
        public void CompareToDiff_NullRealTable_ReturnsNewTableDiffWithNoChanges()
        {
            var define = BuildBaseSchema();
            var comparer = new TableSchemaComparer(define, null);

            var diff = comparer.CompareToDiff();

            Assert.True(diff.IsNewTable);
            Assert.Empty(diff.Changes);
        }

        [Fact]
        [DisplayName("CompareToDiff：結構完全相同時 Changes 應為空")]
        public void CompareToDiff_IdenticalSchemas_ReturnsNoChanges()
        {
            var define = BuildBaseSchema();
            var real = BuildRealSchema();
            var comparer = new TableSchemaComparer(define, real);

            var diff = comparer.CompareToDiff();

            Assert.Empty(diff.Changes);
            Assert.True(diff.IsEmpty);
        }

        [Fact]
        [DisplayName("CompareToDiff：實際表缺少欄位時應產生 AddFieldChange")]
        public void CompareToDiff_MissingField_EmitsAddFieldChange()
        {
            var define = BuildBaseSchema();
            define.Fields!.Add("age", "Age", FieldDbType.Integer);
            var real = BuildRealSchema();

            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            var addChange = Assert.Single(diff.Changes.OfType<AddFieldChange>());
            Assert.Equal("age", addChange.Field.FieldName);
            Assert.Equal(FieldDbType.Integer, addChange.Field.DbType);
        }

        [Fact]
        [DisplayName("CompareToDiff：欄位定義不同時應產生 AlterFieldChange（含 Old/New）")]
        public void CompareToDiff_DifferentField_EmitsAlterFieldChange()
        {
            var define = BuildBaseSchema();
            var real = BuildRealSchema();
            real.Fields!["name"].Length = 30;

            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            var alterChange = Assert.Single(diff.Changes.OfType<AlterFieldChange>());
            Assert.Equal("name", alterChange.NewField.FieldName);
            Assert.Equal(50, alterChange.NewField.Length);
            Assert.Equal(30, alterChange.OldField.Length);
        }

        [Fact]
        [DisplayName("CompareToDiff：實際表缺少索引時應產生 AddIndexChange")]
        public void CompareToDiff_MissingIndex_EmitsAddIndexChange()
        {
            var define = BuildBaseSchema();
            define.Indexes!.Add("ix_{0}_name", "name", false);
            var real = BuildRealSchema();

            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            var addIndex = Assert.Single(diff.Changes.OfType<AddIndexChange>());
            Assert.Equal("ix_{0}_name", addIndex.Index.Name);
            Assert.Empty(diff.Changes.OfType<DropIndexChange>());
        }

        [Fact]
        [DisplayName("CompareToDiff：索引定義不同時應同時產生 Drop + Add")]
        public void CompareToDiff_DifferentIndex_EmitsDropThenAdd()
        {
            var define = BuildBaseSchema();
            define.Indexes!.Add("ix_{0}_name", "name", true);
            var real = BuildRealSchema();
            real.Indexes!.Add("ix_st_demo_name", "name", false);

            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            var drop = Assert.Single(diff.Changes.OfType<DropIndexChange>());
            var add = Assert.Single(diff.Changes.OfType<AddIndexChange>());
            Assert.Equal("ix_st_demo_name", drop.Index.Name);
            Assert.Equal("ix_{0}_name", add.Index.Name);
        }

        [Fact]
        [DisplayName("CompareToDiff：僅存在於 Real 的欄位不產生 Change（保留政策）")]
        public void CompareToDiff_ExtraFieldInRealTable_EmitsNoChangeForExtensionField()
        {
            var define = BuildBaseSchema();
            var real = BuildRealSchema();
            real.Fields!.Add("legacy_col", "Legacy", FieldDbType.String, 10);

            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            Assert.Empty(diff.Changes);
        }

        [Fact]
        [DisplayName("CompareToDiff：僅存在於 Real 的索引不產生 Change（保留政策）")]
        public void CompareToDiff_ExtraIndexInRealTable_EmitsNoChangeForExtensionIndex()
        {
            var define = BuildBaseSchema();
            var real = BuildRealSchema();
            real.Indexes!.Add("ix_st_demo_legacy", "name", false);

            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            Assert.Empty(diff.Changes);
        }

        [Fact]
        [DisplayName("CompareToDiff：描述差異應填入 diff.DescriptionChanges")]
        public void CompareToDiff_DescriptionDiff_PopulatesDiffDescriptionChanges()
        {
            var define = BuildBaseSchema();
            define.DisplayName = "示範資料表";
            var real = BuildRealSchema();

            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            var change = Assert.Single(diff.DescriptionChanges);
            Assert.Equal(DescriptionLevel.Table, change.Level);
            Assert.Equal("示範資料表", change.NewValue);
            Assert.True(change.IsNew);
        }

        [Fact]
        [DisplayName("CompareToDiff：Define 註解為空採保守策略（不產生 DescriptionChange）")]
        public void CompareToDiff_EmptyDefineDescription_NoDescriptionChange()
        {
            var define = BuildBaseSchema();
            define.DisplayName = string.Empty;
            var real = BuildRealSchema();
            real.DisplayName = "DB 既有表說明";

            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            Assert.Empty(diff.DescriptionChanges);
        }

        [Fact]
        [DisplayName("CompareToDiff：不應影響 define 或 real 的 UpgradeAction（不 mutate）")]
        public void CompareToDiff_DoesNotMutateUpgradeAction()
        {
            var define = BuildBaseSchema();
            define.Fields!.Add("age", "Age", FieldDbType.Integer);
            var real = BuildRealSchema();
            real.Fields!["name"].Length = 30;

            new TableSchemaComparer(define, real).CompareToDiff();

            // define 上未被設定 UpgradeAction
            Assert.Equal(DbUpgradeAction.None, define.UpgradeAction);
            Assert.Equal(DbUpgradeAction.None, define.Fields!["age"].UpgradeAction);
            Assert.Equal(DbUpgradeAction.None, define.Fields!["name"].UpgradeAction);
        }

        [Fact]
        [DisplayName("CompareToDiff 不影響舊 Compare() 行為（獨立 DescriptionChanges 來源）")]
        public void CompareToDiff_DoesNotPopulateLegacyDescriptionChanges()
        {
            var define = BuildBaseSchema();
            define.DisplayName = "示範";
            var real = BuildRealSchema();
            var comparer = new TableSchemaComparer(define, real);

            comparer.CompareToDiff();

            // 舊 API 的 DescriptionChanges 不應被 CompareToDiff 動到
            Assert.Empty(comparer.DescriptionChanges);
        }

        // ---- Rename detection via OriginalFieldName ----

        [Fact]
        [DisplayName("CompareToDiff：舊名存在、新名不存在時應產生 RenameFieldChange")]
        public void CompareToDiff_RenameHint_OldNameExists_EmitsRenameFieldChange()
        {
            var define = BuildBaseSchema();
            define.Fields!["name"].FieldName = "display_name";
            define.Fields!["display_name"].OriginalFieldName = "name";
            var real = BuildRealSchema();

            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            var rename = Assert.Single(diff.Changes.OfType<RenameFieldChange>());
            Assert.Equal("name", rename.OldFieldName);
            Assert.Equal("display_name", rename.NewField.FieldName);
            // 型別相同 (String 50 ↔ String 50)，不應額外產生 AlterFieldChange
            Assert.Empty(diff.Changes.OfType<AlterFieldChange>());
        }

        [Fact]
        [DisplayName("CompareToDiff：rename 合併型別變更時應同時產生 Rename + Alter")]
        public void CompareToDiff_RenameWithTypeChange_EmitsRenameAndAlter()
        {
            var define = BuildBaseSchema();
            define.Fields!["name"].FieldName = "display_name";
            define.Fields!["display_name"].OriginalFieldName = "name";
            define.Fields!["display_name"].Length = 100; // 與 real 的 50 不同
            var real = BuildRealSchema();

            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            Assert.Single(diff.Changes.OfType<RenameFieldChange>());
            var alter = Assert.Single(diff.Changes.OfType<AlterFieldChange>());
            // Alter 的 old 是 post-rename 投影（新名，但舊定義）
            Assert.Equal("display_name", alter.OldField.FieldName);
            Assert.Equal(50, alter.OldField.Length);
            Assert.Equal(100, alter.NewField.Length);
        }

        [Fact]
        [DisplayName("CompareToDiff：stale rename hint（新名已存在於 DB）應視為已完成，不再產生 Rename")]
        public void CompareToDiff_RenameHint_StaleHint_NoRenameEmitted()
        {
            var define = BuildBaseSchema();
            define.Fields!["name"].FieldName = "display_name";
            define.Fields!["display_name"].OriginalFieldName = "name";
            var real = BuildRealSchema();
            // DB 已完成 rename：存在 display_name，不存在 name
            real.Fields!.Remove("name");
            real.Fields!.Add("display_name", "Display Name", FieldDbType.String, 50);

            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            Assert.Empty(diff.Changes.OfType<RenameFieldChange>());
            Assert.Empty(diff.Changes.OfType<AddFieldChange>());
        }

        [Fact]
        [DisplayName("CompareToDiff：舊名與新名皆不存在時應降級為 AddFieldChange（警告情境）")]
        public void CompareToDiff_RenameHint_NeitherNameInRealTable_FallsBackToAddField()
        {
            var define = BuildBaseSchema();
            define.Fields!.Add("new_col", "New", FieldDbType.String, 20);
            define.Fields!["new_col"].OriginalFieldName = "ghost_col"; // ghost 舊名不存在於 DB
            var real = BuildRealSchema();

            var diff = new TableSchemaComparer(define, real).CompareToDiff();

            Assert.Empty(diff.Changes.OfType<RenameFieldChange>());
            var add = Assert.Single(diff.Changes.OfType<AddFieldChange>());
            Assert.Equal("new_col", add.Field.FieldName);
        }
    }
}
