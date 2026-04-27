using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.Schema;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class TableUpgradeOrchestratorIntegrationTests
    {
        private const string DatabaseId = "common_sqlserver";

        private static TableSchema BuildSchema(string tableName, int nameLength = 50)
        {
            var schema = new TableSchema { TableName = tableName };
            schema.Fields!.Add("sys_rowid", "Row ID", FieldDbType.Guid);
            schema.Fields!.Add("name", "Name", FieldDbType.String, nameLength);
            schema.Indexes!.AddPrimaryKey("sys_rowid");
            return schema;
        }

        private static void DropIfExists(string tableName)
        {
            var dbAccess = new DbAccess(DatabaseId);
            var sql = $"IF OBJECT_ID(N'{tableName}', N'U') IS NOT NULL DROP TABLE [{tableName}];";
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, sql));
        }

        private static bool TableExists(string tableName)
        {
            var dbAccess = new DbAccess(DatabaseId);
            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT COUNT(*) FROM sys.tables WHERE name = {0}", tableName);
            var result = dbAccess.Execute(spec);
            return Convert.ToInt32(result.Scalar!, System.Globalization.CultureInfo.InvariantCulture) > 0;
        }

        private static bool ColumnExists(string tableName, string columnName)
        {
            var dbAccess = new DbAccess(DatabaseId);
            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID({0}) AND name = {1}",
                tableName, columnName);
            var result = dbAccess.Execute(spec);
            return Convert.ToInt32(result.Scalar!, System.Globalization.CultureInfo.InvariantCulture) > 0;
        }

        private static int GetColumnLength(string tableName, string columnName)
        {
            var dbAccess = new DbAccess(DatabaseId);
            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT max_length FROM sys.columns WHERE object_id = OBJECT_ID({0}) AND name = {1}",
                tableName, columnName);
            var result = dbAccess.Execute(spec);
            // nvarchar max_length is bytes (2 per char)
            return Convert.ToInt32(result.Scalar!, System.Globalization.CultureInfo.InvariantCulture) / 2;
        }

        private static int CountRows(string tableName)
        {
            var dbAccess = new DbAccess(DatabaseId);
            var spec = new DbCommandSpec(DbCommandKind.Scalar, $"SELECT COUNT(*) FROM [{tableName}]");
            var result = dbAccess.Execute(spec);
            return Convert.ToInt32(result.Scalar!, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static UpgradePlan PlanFor(string tableName, TableSchema define, UpgradeOptions? options = null)
        {
            var provider = new Providers.SqlServer.SqlTableSchemaProvider(DatabaseId);
            var real = provider.GetTableSchema(tableName);
            var diff = new TableSchemaComparer(define, real).CompareToDiff();
            return new TableUpgradeOrchestrator(DatabaseId).Plan(diff, options);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("整合：空 plan 應回傳 false")]
        public void Execute_EmptyPlan_ReturnsFalse()
        {
            var plan = new UpgradePlan(UpgradeExecutionMode.NoChange);
            var executed = TableUpgradeOrchestrator.Execute(plan, DatabaseId);
            Assert.False(executed);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("整合：新表應透過 Create 模式建立於 DB")]
        public void Execute_NewTable_CreatesTable()
        {
            const string tableName = "st_orch_create_test";
            DropIfExists(tableName);
            try
            {
                var plan = PlanFor(tableName, BuildSchema(tableName));

                Assert.Equal(UpgradeExecutionMode.Create, plan.Mode);
                var executed = TableUpgradeOrchestrator.Execute(plan, DatabaseId);

                Assert.True(executed);
                Assert.True(TableExists(tableName));
            }
            finally
            {
                DropIfExists(tableName);
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("整合：新增欄位走 ALTER 路徑，既有資料保留")]
        public void Execute_AddColumn_PreservesExistingData()
        {
            const string tableName = "st_orch_addcol_test";
            DropIfExists(tableName);
            try
            {
                // 先建表並塞資料
                var initial = BuildSchema(tableName);
                TableUpgradeOrchestrator.Execute(PlanFor(tableName, initial), DatabaseId);
                var dbAccess = new DbAccess(DatabaseId);
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"INSERT INTO [{tableName}] (sys_rowid, name) VALUES (NEWID(), {{0}})", "Alice"));

                // define 新增欄位 age
                var updated = BuildSchema(tableName);
                updated.Fields!.Add("age", "Age", FieldDbType.Integer);
                var plan = PlanFor(tableName, updated);

                Assert.Equal(UpgradeExecutionMode.Alter, plan.Mode);
                TableUpgradeOrchestrator.Execute(plan, DatabaseId);

                Assert.True(ColumnExists(tableName, "age"));
                Assert.Equal(1, CountRows(tableName));
            }
            finally
            {
                DropIfExists(tableName);
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("整合：放大欄位長度走 ALTER 路徑")]
        public void Execute_WidenColumnLength_UsesAlterPath()
        {
            const string tableName = "st_orch_widen_test";
            DropIfExists(tableName);
            try
            {
                var initial = BuildSchema(tableName, nameLength: 50);
                TableUpgradeOrchestrator.Execute(PlanFor(tableName, initial), DatabaseId);

                var widened = BuildSchema(tableName, nameLength: 100);
                var plan = PlanFor(tableName, widened);

                Assert.Equal(UpgradeExecutionMode.Alter, plan.Mode);
                TableUpgradeOrchestrator.Execute(plan, DatabaseId);

                Assert.Equal(100, GetColumnLength(tableName, "name"));
            }
            finally
            {
                DropIfExists(tableName);
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("整合：跨 family 型別變更應走 Rebuild 路徑（資料可丟失視資料而定）")]
        public void Execute_CrossFamilyTypeChange_UsesRebuildPath()
        {
            const string tableName = "st_orch_rebuild_test";
            DropIfExists(tableName);
            try
            {
                var initial = BuildSchema(tableName);
                TableUpgradeOrchestrator.Execute(PlanFor(tableName, initial), DatabaseId);

                // 插入數值格式字串，rebuild 時 CAST 仍能成功
                var dbAccess = new DbAccess(DatabaseId);
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"INSERT INTO [{tableName}] (sys_rowid, name) VALUES (NEWID(), '42')"));

                // 將 name 改為 Integer（跨 family）
                var updated = BuildSchema(tableName);
                updated.Fields!["name"].DbType = FieldDbType.Integer;
                updated.Fields!["name"].Length = 0;

                var plan = PlanFor(tableName, updated);
                Assert.Equal(UpgradeExecutionMode.Rebuild, plan.Mode);

                TableUpgradeOrchestrator.Execute(plan, DatabaseId);

                // 驗證資料列仍存在（值轉型成功）
                Assert.Equal(1, CountRows(tableName));
            }
            finally
            {
                DropIfExists(tableName);
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("整合：同一定義重跑應為 NoChange（冪等）")]
        public void Execute_SameSchemaTwice_IsIdempotent()
        {
            const string tableName = "st_orch_idempotent_test";
            DropIfExists(tableName);
            try
            {
                var define = BuildSchema(tableName);
                TableUpgradeOrchestrator.Execute(PlanFor(tableName, define), DatabaseId);

                var plan2 = PlanFor(tableName, define);

                Assert.Equal(UpgradeExecutionMode.NoChange, plan2.Mode);
                Assert.False(TableUpgradeOrchestrator.Execute(plan2, DatabaseId));
            }
            finally
            {
                DropIfExists(tableName);
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("整合：real-only 欄位（extension）在 rebuild 後仍保留")]
        public void Execute_RebuildPath_PreservesExtensionField()
        {
            const string tableName = "st_orch_extfield_test";
            DropIfExists(tableName);
            try
            {
                // 建表後手動加入「第三方」欄位 legacy_col
                var initial = BuildSchema(tableName);
                TableUpgradeOrchestrator.Execute(PlanFor(tableName, initial), DatabaseId);
                var dbAccess = new DbAccess(DatabaseId);
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    $"ALTER TABLE [{tableName}] ADD [legacy_col] [nvarchar](10) NULL;"));

                // 觸發 rebuild（跨 family 變更）
                var updated = BuildSchema(tableName);
                updated.Fields!["name"].DbType = FieldDbType.Integer;
                updated.Fields!["name"].Length = 0;

                var plan = PlanFor(tableName, updated);
                Assert.Equal(UpgradeExecutionMode.Rebuild, plan.Mode);

                TableUpgradeOrchestrator.Execute(plan, DatabaseId);

                // rebuild 後 legacy_col 應仍存在
                Assert.True(ColumnExists(tableName, "legacy_col"));
            }
            finally
            {
                DropIfExists(tableName);
            }
        }
    }
}
