using System.ComponentModel;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class DbAccessStringMethodTests
    {
        [DbFact]
        [DisplayName("ExecuteNonQuery 字串多載應回傳影響列數")]
        public void ExecuteNonQuery_ValidSql_ReturnsRowsAffected()
        {
            var dbAccess = new DbAccess("common");
            int affected = dbAccess.ExecuteNonQuery(
                "UPDATE st_user SET note={1} WHERE sys_id={0}", "001", "test-string-overload");
            Assert.True(affected >= 0);
        }

        [DbFact]
        [DisplayName("ExecuteScalar 字串多載應回傳純量值")]
        public void ExecuteScalar_ValidSql_ReturnsScalarValue()
        {
            var dbAccess = new DbAccess("common");
            object? value = dbAccess.ExecuteScalar(
                "SELECT COUNT(*) FROM st_user WHERE sys_id={0}", "001");
            Assert.NotNull(value);
        }

        [DbFact]
        [DisplayName("ExecuteDataTable 字串多載應回傳 DataTable")]
        public void ExecuteDataTable_ValidSql_ReturnsDataTable()
        {
            var dbAccess = new DbAccess("common");
            var table = dbAccess.ExecuteDataTable(
                "SELECT sys_id FROM st_user WHERE sys_id={0}", "001");
            Assert.NotNull(table);
        }

        [DbFact]
        [DisplayName("ExecuteNonQueryAsync 非同步字串多載應回傳影響列數")]
        public async Task ExecuteNonQueryAsync_ValidSql_ReturnsRowsAffected()
        {
            var dbAccess = new DbAccess("common");
            int affected = await dbAccess.ExecuteNonQueryAsync(
                "UPDATE st_user SET note={1} WHERE sys_id={0}", "001", "test-async-overload");
            Assert.True(affected >= 0);
        }

        [DbFact]
        [DisplayName("ExecuteScalarAsync 非同步字串多載應回傳純量值")]
        public async Task ExecuteScalarAsync_ValidSql_ReturnsScalarValue()
        {
            var dbAccess = new DbAccess("common");
            object? value = await dbAccess.ExecuteScalarAsync(
                "SELECT COUNT(*) FROM st_user WHERE sys_id={0}", "001");
            Assert.NotNull(value);
        }

        [DbFact]
        [DisplayName("ExecuteDataTableAsync 非同步字串多載應回傳 DataTable")]
        public async Task ExecuteDataTableAsync_ValidSql_ReturnsDataTable()
        {
            var dbAccess = new DbAccess("common");
            var table = await dbAccess.ExecuteDataTableAsync(
                "SELECT sys_id FROM st_user WHERE sys_id={0}", "001");
            Assert.NotNull(table);
        }

        [DbFact]
        [DisplayName("ExecuteBatch 未啟用交易應成功執行批次命令")]
        public void ExecuteBatch_WithoutTransaction_Succeeds()
        {
            var batch = new DbBatchSpec { UseTransaction = false };
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT COUNT(*) FROM st_user WHERE sys_id={0}", "001"));

            var dbAccess = new DbAccess("common");
            var result = dbAccess.ExecuteBatch(batch);

            Assert.NotNull(result);
            Assert.Single(result.Results);
        }

        [DbFact]
        [DisplayName("ExecuteBatchAsync 未啟用交易應成功執行非同步批次命令")]
        public async Task ExecuteBatchAsync_WithoutTransaction_Succeeds()
        {
            var batch = new DbBatchSpec { UseTransaction = false };
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT COUNT(*) FROM st_user WHERE sys_id={0}", "001"));

            var dbAccess = new DbAccess("common");
            var result = await dbAccess.ExecuteBatchAsync(batch);

            Assert.NotNull(result);
            Assert.Single(result.Results);
        }

        [DbFact]
        [DisplayName("Execute 含 DbTransaction 多載應成功執行命令")]
        public void Execute_WithTransaction_NonQuery_Succeeds()
        {
            var dbAccess = new DbAccess("common");
            using var conn = DbFunc.CreateConnection("common");
            conn.Open();
            using var tran = conn.BeginTransaction();

            var spec = new DbCommandSpec(DbCommandKind.NonQuery,
                "UPDATE st_user SET note={1} WHERE sys_id={0}", "001", "tx-overload");
            var result = dbAccess.Execute(spec, tran);
            tran.Rollback();

            Assert.NotNull(result);
        }

        [DbFact]
        [DisplayName("ExecuteAsync 含 DbTransaction 多載應成功執行非同步命令")]
        public async Task ExecuteAsync_WithTransaction_NonQuery_Succeeds()
        {
            var dbAccess = new DbAccess("common");
            using var conn = DbFunc.CreateConnection("common");
            await conn.OpenAsync();
            await using var tran = await conn.BeginTransactionAsync();

            var spec = new DbCommandSpec(DbCommandKind.NonQuery,
                "UPDATE st_user SET note={1} WHERE sys_id={0}", "001", "tx-async-overload");
            var result = await dbAccess.ExecuteAsync(spec, tran);
            await tran.RollbackAsync();

            Assert.NotNull(result);
        }
    }
}
