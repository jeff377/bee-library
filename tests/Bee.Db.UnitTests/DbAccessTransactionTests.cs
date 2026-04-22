using System.ComponentModel;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class DbAccessTransactionTests
    {
        [DbFact]
        [DisplayName("Execute 含 DbTransaction 多載 Scalar 種類應成功回傳結果")]
        public void Execute_WithTransaction_Scalar_ReturnsResult()
        {
            var dbAccess = new DbAccess("common");
            using var conn = DbFunc.CreateConnection("common");
            conn.Open();
            using var tran = conn.BeginTransaction();

            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT COUNT(*) FROM st_user WHERE sys_id={0}", "001");
            var result = dbAccess.Execute(spec, tran);
            tran.Rollback();

            Assert.NotNull(result);
            Assert.NotNull(result.Scalar);
        }

        [DbFact]
        [DisplayName("Execute 含 DbTransaction 多載 DataTable 種類應成功回傳資料表")]
        public void Execute_WithTransaction_DataTable_ReturnsTable()
        {
            var dbAccess = new DbAccess("common");
            using var conn = DbFunc.CreateConnection("common");
            conn.Open();
            using var tran = conn.BeginTransaction();

            var spec = new DbCommandSpec(DbCommandKind.DataTable,
                "SELECT sys_id FROM st_user WHERE sys_id={0}", "001");
            var result = dbAccess.Execute(spec, tran);
            tran.Rollback();

            Assert.NotNull(result);
            Assert.NotNull(result.Table);
        }

        [DbFact]
        [DisplayName("ExecuteAsync 含 DbTransaction 多載 Scalar 種類應成功回傳結果")]
        public async Task ExecuteAsync_WithTransaction_Scalar_ReturnsResult()
        {
            var dbAccess = new DbAccess("common");
            using var conn = DbFunc.CreateConnection("common");
            await conn.OpenAsync();
            await using var tran = await conn.BeginTransactionAsync();

            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT COUNT(*) FROM st_user WHERE sys_id={0}", "001");
            var result = await dbAccess.ExecuteAsync(spec, tran);
            await tran.RollbackAsync();

            Assert.NotNull(result);
            Assert.NotNull(result.Scalar);
        }

        [DbFact]
        [DisplayName("ExecuteAsync 含 DbTransaction 多載 DataTable 種類應成功回傳資料表")]
        public async Task ExecuteAsync_WithTransaction_DataTable_ReturnsTable()
        {
            var dbAccess = new DbAccess("common");
            using var conn = DbFunc.CreateConnection("common");
            await conn.OpenAsync();
            await using var tran = await conn.BeginTransactionAsync();

            var spec = new DbCommandSpec(DbCommandKind.DataTable,
                "SELECT sys_id FROM st_user WHERE sys_id={0}", "001");
            var result = await dbAccess.ExecuteAsync(spec, tran);
            await tran.RollbackAsync();

            Assert.NotNull(result);
            Assert.NotNull(result.Table);
        }

        [DbFact]
        [DisplayName("ExecuteBatch 含 DataTable 命令的批次應成功執行")]
        public void ExecuteBatch_WithDataTableCommand_Succeeds()
        {
            var batch = new DbBatchSpec { UseTransaction = false };
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.DataTable,
                "SELECT sys_id FROM st_user WHERE sys_id={0}", "001"));
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT COUNT(*) FROM st_user WHERE sys_id={0}", "001"));

            var dbAccess = new DbAccess("common");
            var result = dbAccess.ExecuteBatch(batch);

            Assert.NotNull(result);
            Assert.Equal(2, result.Results.Count);
            Assert.NotNull(result.Results[0].Table);
        }

        [DbFact]
        [DisplayName("ExecuteBatchAsync 含 DataTable 命令的非同步批次應成功執行")]
        public async Task ExecuteBatchAsync_WithDataTableCommand_Succeeds()
        {
            var batch = new DbBatchSpec { UseTransaction = false };
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.DataTable,
                "SELECT sys_id FROM st_user WHERE sys_id={0}", "001"));
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT COUNT(*) FROM st_user WHERE sys_id={0}", "001"));

            var dbAccess = new DbAccess("common");
            var result = await dbAccess.ExecuteBatchAsync(batch);

            Assert.NotNull(result);
            Assert.Equal(2, result.Results.Count);
            Assert.NotNull(result.Results[0].Table);
        }
    }
}
