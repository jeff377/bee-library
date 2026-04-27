using System.ComponentModel;
using System.Data;
using System.Globalization;
using Bee.Base;
using Bee.Db.Dml;
using Bee.Definition;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class DbAccessIsolationLevelTests
    {
        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("ExecuteBatch 以指定 IsolationLevel 執行批次交易應成功")]
        public void ExecuteBatch_WithIsolationLevel_Succeeds()
        {
            var batch = new DbBatchSpec
            {
                UseTransaction = true,
                IsolationLevel = IsolationLevel.ReadCommitted
            };
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT COUNT(*) FROM st_user WHERE sys_id = {0}", "001"));

            var dbAccess = new DbAccess("common_sqlserver");
            var result = dbAccess.ExecuteBatch(batch);

            Assert.NotNull(result);
            Assert.Single(result.Results);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("ExecuteBatchAsync 以指定 IsolationLevel 非同步執行批次交易應成功")]
        public async Task ExecuteBatchAsync_WithIsolationLevel_Succeeds()
        {
            var batch = new DbBatchSpec
            {
                UseTransaction = true,
                IsolationLevel = IsolationLevel.ReadCommitted
            };
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT COUNT(*) FROM st_user WHERE sys_id = {0}", "001"));

            var dbAccess = new DbAccess("common_sqlserver");
            var result = await dbAccess.ExecuteBatchAsync(batch);

            Assert.NotNull(result);
            Assert.Single(result.Results);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("ExecuteBatch 批次中含 DataTable 命令應成功回傳資料表")]
        public void ExecuteBatch_DataTableCommand_Succeeds()
        {
            var batch = new DbBatchSpec { UseTransaction = false };
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.DataTable,
                "SELECT sys_id FROM st_user WHERE sys_id = {0}", "001"));

            var dbAccess = new DbAccess("common_sqlserver");
            var result = dbAccess.ExecuteBatch(batch);

            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.NotNull(result.Results[0].Table);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("ExecuteBatchAsync 批次中含 DataTable 命令非同步應成功回傳資料表")]
        public async Task ExecuteBatchAsync_DataTableCommand_Succeeds()
        {
            var batch = new DbBatchSpec { UseTransaction = false };
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.DataTable,
                "SELECT sys_id FROM st_user WHERE sys_id = {0}", "001"));

            var dbAccess = new DbAccess("common_sqlserver");
            var result = await dbAccess.ExecuteBatchAsync(batch);

            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.NotNull(result.Results[0].Table);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("UpdateDataTable 以指定 IsolationLevel 執行更新應成功")]
        public void UpdateDataTable_WithIsolationLevel_Succeeds()
        {
            var dbAccess = new DbAccess("common_sqlserver");

            var cmd = new DbCommandSpec(DbCommandKind.DataTable,
                "SELECT * FROM st_user WHERE sys_id = {0}", "001");
            var table = dbAccess.Execute(cmd).Table;
            Assert.NotNull(table);
            Assert.True(table.Rows.Count > 0, "st_user 中無 sys_id='001' 的資料");

            int rnd = BaseFunc.RndInt(0, 100);
            table.Rows[0]["note"] = rnd.ToString(CultureInfo.InvariantCulture);

            var tableSchema = BackendInfo.DefineAccess.GetTableSchema("common", "st_user");
            var builder = new TableSchemaCommandBuilder(tableSchema);
            var updateSpec = builder.BuildUpdateSpec(table);
            updateSpec.UseTransaction = true;
            updateSpec.IsolationLevel = IsolationLevel.ReadCommitted;

            int affected = dbAccess.UpdateDataTable(updateSpec);
            Assert.True(affected > 0);
        }
    }
}
