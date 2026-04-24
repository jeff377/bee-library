using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Providers;
using Bee.Db.Providers.SqlServer;
using Bee.Db.Schema;
using Bee.Db.Schema.Changes;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class TableUpgradeOrchestratorTests
    {
        private static readonly IDialectFactory s_dialect = new SqlDialectFactory();

        private static TableSchema BuildDefineSchema(string tableName = "st_demo")
        {
            var schema = new TableSchema { TableName = tableName };
            schema.Fields!.Add("id", "Id", FieldDbType.Guid);
            schema.Fields!.Add("name", "Name", FieldDbType.String, 50);
            schema.Indexes!.AddPrimaryKey("id");
            return schema;
        }

        private static TableSchema BuildRealSchema(string tableName = "st_demo")
        {
            var schema = new TableSchema { TableName = tableName };
            schema.Fields!.Add("id", "Id", FieldDbType.Guid);
            schema.Fields!.Add("name", "Name", FieldDbType.String, 50);
            var pk = new TableSchemaIndex { Name = $"pk_{tableName}", PrimaryKey = true, Unique = true };
            pk.IndexFields!.Add("id");
            schema.Indexes!.Add(pk);
            return schema;
        }

        private static UpgradePlan PlanFor(TableSchema define, TableSchema? real, UpgradeOptions? options = null)
        {
            var diff = new TableSchemaComparer(define, real).CompareToDiff();
            return new TableUpgradeOrchestrator(s_dialect).Plan(diff, options);
        }

        [Fact]
        [DisplayName("Plan：新表應為 Create 模式，單一 CreateTable stage")]
        public void Plan_NewTable_ReturnsCreateMode()
        {
            var plan = PlanFor(BuildDefineSchema(), real: null);

            Assert.Equal(UpgradeExecutionMode.Create, plan.Mode);
            Assert.False(plan.IsEmpty);
            var stage = Assert.Single(plan.Stages);
            Assert.Equal(UpgradeStageKind.CreateTable, stage.Kind);
        }

        [Fact]
        [DisplayName("Plan：結構完全相同應為 NoChange 模式")]
        public void Plan_IdenticalSchemas_ReturnsNoChange()
        {
            var plan = PlanFor(BuildDefineSchema(), BuildRealSchema());

            Assert.Equal(UpgradeExecutionMode.NoChange, plan.Mode);
            Assert.True(plan.IsEmpty);
            Assert.Empty(plan.Stages);
        }

        [Fact]
        [DisplayName("Plan：全部 ALTER 可處理時應為 Alter 模式")]
        public void Plan_AlterCapableChanges_ReturnsAlterMode()
        {
            var define = BuildDefineSchema();
            define.Fields!.Add("age", "Age", FieldDbType.Integer);
            var plan = PlanFor(define, BuildRealSchema());

            Assert.Equal(UpgradeExecutionMode.Alter, plan.Mode);
            Assert.NotEmpty(plan.Stages);
        }

        [Fact]
        [DisplayName("Plan：含 Rebuild 變化時整表應 fallback 為 Rebuild 模式")]
        public void Plan_AnyRebuildChange_FallsBackToRebuildMode()
        {
            // 將 name 從 String 改為 Integer（跨 family）→ Rebuild
            var define = BuildDefineSchema();
            define.Fields!["name"].DbType = FieldDbType.Integer;
            define.Fields!["name"].Length = 0;

            var plan = PlanFor(define, BuildRealSchema());

            Assert.Equal(UpgradeExecutionMode.Rebuild, plan.Mode);
            var stage = Assert.Single(plan.Stages);
            Assert.Equal(UpgradeStageKind.Rebuild, stage.Kind);
        }

        [Fact]
        [DisplayName("Plan：Rebuild 同時帶 rename 應擲例外")]
        public void Plan_RebuildWithRename_Throws()
        {
            var define = BuildDefineSchema();
            define.Fields!["name"].FieldName = "display_name";
            define.Fields!["display_name"].OriginalFieldName = "name";
            // 同時把新欄位類型改為跨 family（觸發 rebuild）
            define.Fields!["display_name"].DbType = FieldDbType.Integer;
            define.Fields!["display_name"].Length = 0;

            var diff = new TableSchemaComparer(define, BuildRealSchema()).CompareToDiff();
            var orchestrator = new TableUpgradeOrchestrator(s_dialect);

            Assert.Throws<InvalidOperationException>(() => orchestrator.Plan(diff));
        }

        [Fact]
        [DisplayName("Plan：ALTER stages 順序應為 DropIndexes → AlterColumns → AddColumns → CreateIndexes → SyncDescriptions")]
        public void Plan_AlterMode_StagesAreOrdered()
        {
            var define = BuildDefineSchema();
            // 現有 pk 索引定義不同（Unique 假） → 會產生 drop + add index
            define.Indexes!["pk_{0}"].Name = "pk_{0}"; // 保持 PK
            define.Fields!["name"].Length = 100;       // 觸發 alter column
            define.Fields!.Add("age", "Age", FieldDbType.Integer); // 觸發 add column
            define.Indexes!.Add("ix_{0}_name", "name", false); // 觸發 create index
            define.DisplayName = "示範";                // 觸發 description sync
            define.Fields!["name"].Caption = "新名稱";

            var real = BuildRealSchema();
            // 改動 real pk 的 Unique 以觸發 DropIndex + AddIndex 對 PK
            real.Indexes!["pk_st_demo"].Unique = false;

            var plan = PlanFor(define, real);

            Assert.Equal(UpgradeExecutionMode.Alter, plan.Mode);
            var kinds = plan.Stages.Select(s => s.Kind).ToList();
            // 必含以下 stages；檢查相對順序
            int dropIdx = kinds.IndexOf(UpgradeStageKind.DropIndexes);
            int alterIdx = kinds.IndexOf(UpgradeStageKind.AlterColumns);
            int addIdx = kinds.IndexOf(UpgradeStageKind.AddColumns);
            int createIdx = kinds.IndexOf(UpgradeStageKind.CreateIndexes);
            int syncIdx = kinds.IndexOf(UpgradeStageKind.SyncDescriptions);
            Assert.True(dropIdx >= 0);
            Assert.True(alterIdx > dropIdx);
            Assert.True(addIdx > alterIdx);
            Assert.True(createIdx > addIdx);
            Assert.True(syncIdx > createIdx);
        }

        [Fact]
        [DisplayName("Plan：純描述差異應僅產生 SyncDescriptions stage")]
        public void Plan_DescriptionOnly_EmitsOnlySyncDescriptionsStage()
        {
            var define = BuildDefineSchema();
            define.DisplayName = "示範";
            var plan = PlanFor(define, BuildRealSchema());

            Assert.Equal(UpgradeExecutionMode.Alter, plan.Mode);
            var stage = Assert.Single(plan.Stages);
            Assert.Equal(UpgradeStageKind.SyncDescriptions, stage.Kind);
        }

        [Fact]
        [DisplayName("Plan：narrowing 變化在預設 options 下應擲例外")]
        public void Plan_NarrowingDisallowed_Throws()
        {
            var define = BuildDefineSchema();
            define.Fields!["name"].Length = 30; // 由 real 50 縮為 30

            var diff = new TableSchemaComparer(define, BuildRealSchema()).CompareToDiff();
            var orchestrator = new TableUpgradeOrchestrator(s_dialect);

            Assert.Throws<InvalidOperationException>(() => orchestrator.Plan(diff));
        }

        [Fact]
        [DisplayName("Plan：narrowing 變化在 AllowColumnNarrowing=true 下應允許並產生 warning")]
        public void Plan_NarrowingAllowed_ReturnsPlanWithWarning()
        {
            var define = BuildDefineSchema();
            define.Fields!["name"].Length = 30;

            var diff = new TableSchemaComparer(define, BuildRealSchema()).CompareToDiff();
            var options = new UpgradeOptions { AllowColumnNarrowing = true };
            var plan = new TableUpgradeOrchestrator(s_dialect).Plan(diff, options);

            Assert.Equal(UpgradeExecutionMode.Alter, plan.Mode);
            Assert.NotEmpty(plan.Warnings);
            Assert.Contains(plan.Warnings, w => w.Contains("Narrowing"));
        }

        [Fact]
        [DisplayName("Plan：NotSupported 變化應擲例外")]
        public void Plan_NotSupportedChange_Throws()
        {
            var orchestrator = new TableUpgradeOrchestrator(new NotSupportedDialect());
            var define = BuildDefineSchema();
            define.Fields!.Add("age", "Age", FieldDbType.Integer);
            var diff = new TableSchemaComparer(define, BuildRealSchema()).CompareToDiff();

            Assert.Throws<InvalidOperationException>(() => orchestrator.Plan(diff));
        }

        // 測試用 dialect：alter builder 一律回傳 NotSupported，其餘委派給 SqlDialectFactory。
        private sealed class NotSupportedDialect : IDialectFactory
        {
            private readonly SqlDialectFactory _inner = new();
            public ITableSchemaProvider CreateTableSchemaProvider(string databaseId) => _inner.CreateTableSchemaProvider(databaseId);
            public ICreateTableCommandBuilder CreateCreateTableCommandBuilder() => _inner.CreateCreateTableCommandBuilder();
            public ITableAlterCommandBuilder CreateTableAlterCommandBuilder() => new NotSupportedBuilder();
            public ITableRebuildCommandBuilder CreateTableRebuildCommandBuilder() => _inner.CreateTableRebuildCommandBuilder();
            public IFormCommandBuilder CreateFormCommandBuilder(string progId) => _inner.CreateFormCommandBuilder(progId);
            public string GetDefaultValueExpression(FieldDbType dbType) => _inner.GetDefaultValueExpression(dbType);
        }

        // 自訂 alter builder 讓 GetExecutionKind 永遠回傳 NotSupported，用於驗證 orchestrator 的拒絕行為
        private sealed class NotSupportedBuilder : ITableAlterCommandBuilder
        {
            public ChangeExecutionKind GetExecutionKind(TableChange change) => ChangeExecutionKind.NotSupported;
            public bool IsNarrowingChange(TableChange change) => false;
            public IReadOnlyList<string> GetStatements(string tableName, TableChange change) => [];
        }
    }
}
