using System.ComponentModel;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Bee.Tests.Shared;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class DbAccessTransactionTests
    {
        /// <summary>
        /// Fake transaction whose connection is always null, for testing null-connection guards.
        /// </summary>
        private sealed class NullConnectionTransaction : DbTransaction
        {
            protected override DbConnection? DbConnection => null;
            public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
            public override void Commit() { }
            public override void Rollback() { }
        }

        // ── null-connection guard (no DB required) ──────────────────────────

        [Fact]
        [DisplayName("Execute(spec, transaction) 交易連線為 null 應擲 InvalidOperationException")]
        public void Execute_WithTransaction_NullConnection_ThrowsInvalidOperation()
        {
            using var conn = new SqlConnection();
            var dbAccess = new DbAccess(conn);
            var spec = new DbCommandSpec(DbCommandKind.Scalar, "SELECT 1");
            using var tran = new NullConnectionTransaction();

            Assert.Throws<InvalidOperationException>(() => dbAccess.Execute(spec, tran));
        }

        [Fact]
        [DisplayName("ExecuteAsync(spec, transaction) 交易連線為 null 應擲 InvalidOperationException")]
        public async Task ExecuteAsync_WithTransaction_NullConnection_ThrowsInvalidOperation()
        {
            using var conn = new SqlConnection();
            var dbAccess = new DbAccess(conn);
            var spec = new DbCommandSpec(DbCommandKind.Scalar, "SELECT 1");
            using var tran = new NullConnectionTransaction();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => dbAccess.ExecuteAsync(spec, tran));
        }

        // ── ExecuteAsync(spec) - Scalar branch (no transaction overload) ────

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("ExecuteAsync(DbCommandSpec) Scalar 類型應回傳純量值")]
        public async Task ExecuteAsync_ScalarKind_ReturnsScalar()
        {
            var dbAccess = new DbAccess("common_sqlserver");
            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT COUNT(*) FROM st_user WHERE sys_id = {0}", "001");

            var result = await dbAccess.ExecuteAsync(spec);

            Assert.NotNull(result);
            Assert.NotNull(result.Scalar);
        }

        // ── Execute(spec, transaction) - Scalar + DataTable branches ────────

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("Execute(spec, transaction) Scalar 類型應回傳純量值")]
        public void Execute_WithTransaction_ScalarKind_ReturnsScalar()
        {
            var dbAccess = new DbAccess("common_sqlserver");
            using var conn = DbFunc.CreateConnection("common_sqlserver");
            conn.Open();
            using var tran = conn.BeginTransaction();

            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT COUNT(*) FROM st_user WHERE sys_id = {0}", "001");
            var result = dbAccess.Execute(spec, tran);
            tran.Rollback();

            Assert.NotNull(result);
            Assert.NotNull(result.Scalar);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("Execute(spec, transaction) DataTable 類型應回傳資料表")]
        public void Execute_WithTransaction_DataTableKind_ReturnsTable()
        {
            var dbAccess = new DbAccess("common_sqlserver");
            using var conn = DbFunc.CreateConnection("common_sqlserver");
            conn.Open();
            using var tran = conn.BeginTransaction();

            var spec = new DbCommandSpec(DbCommandKind.DataTable,
                "SELECT sys_id FROM st_user WHERE sys_id = {0}", "001");
            var result = dbAccess.Execute(spec, tran);
            tran.Rollback();

            Assert.NotNull(result);
            Assert.NotNull(result.Table);
        }

        // ── ExecuteAsync(spec, transaction) - Scalar + DataTable branches ───

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("ExecuteAsync(spec, transaction) Scalar 類型應回傳純量值")]
        public async Task ExecuteAsync_WithTransaction_ScalarKind_ReturnsScalar()
        {
            var dbAccess = new DbAccess("common_sqlserver");
            using var conn = DbFunc.CreateConnection("common_sqlserver");
            await conn.OpenAsync();
            await using var tran = await conn.BeginTransactionAsync();

            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT COUNT(*) FROM st_user WHERE sys_id = {0}", "001");
            var result = await dbAccess.ExecuteAsync(spec, tran);
            await tran.RollbackAsync();

            Assert.NotNull(result);
            Assert.NotNull(result.Scalar);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("ExecuteAsync(spec, transaction) DataTable 類型應回傳資料表")]
        public async Task ExecuteAsync_WithTransaction_DataTableKind_ReturnsTable()
        {
            var dbAccess = new DbAccess("common_sqlserver");
            using var conn = DbFunc.CreateConnection("common_sqlserver");
            await conn.OpenAsync();
            await using var tran = await conn.BeginTransactionAsync();

            var spec = new DbCommandSpec(DbCommandKind.DataTable,
                "SELECT sys_id FROM st_user WHERE sys_id = {0}", "001");
            var result = await dbAccess.ExecuteAsync(spec, tran);
            await tran.RollbackAsync();

            Assert.NotNull(result);
            Assert.NotNull(result.Table);
        }
    }
}
