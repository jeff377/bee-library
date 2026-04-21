using System.ComponentModel;
using Microsoft.Data.SqlClient;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class DbAccessExtraTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("DbAccess(string) databaseId 為空白應擲 ArgumentException")]
        public void Constructor_EmptyDatabaseId_Throws(string databaseId)
        {
            Assert.Throws<ArgumentException>(() => new DbAccess(databaseId));
        }

        [Fact]
        [DisplayName("DbAccess(string) databaseId 為 null 應擲 ArgumentException")]
        public void Constructor_NullDatabaseId_Throws()
        {
            Assert.Throws<ArgumentException>(() => new DbAccess((string)null!));
        }

        [Fact]
        [DisplayName("DbAccess(DbConnection) 連線為 null 應擲 ArgumentNullException")]
        public void Constructor_NullExternalConnection_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DbAccess((System.Data.Common.DbConnection)null!));
        }

        [Fact]
        [DisplayName("Execute(null) 應擲 ArgumentNullException")]
        public void Execute_NullCommand_Throws()
        {
            using var conn = new SqlConnection();
            var dbAccess = new DbAccess(conn);

            Assert.Throws<ArgumentNullException>(() => dbAccess.Execute(null!));
        }

        [Fact]
        [DisplayName("Execute(spec, null transaction) 應擲 ArgumentNullException")]
        public void Execute_NullTransaction_Throws()
        {
            using var conn = new SqlConnection();
            var dbAccess = new DbAccess(conn);
            var spec = new DbCommandSpec(DbCommandKind.Scalar, "SELECT 1");

            Assert.Throws<ArgumentNullException>(() => dbAccess.Execute(spec, null!));
        }

        [Fact]
        [DisplayName("ExecuteAsync(null) 應擲 ArgumentNullException")]
        public async Task ExecuteAsync_NullCommand_Throws()
        {
            using var conn = new SqlConnection();
            var dbAccess = new DbAccess(conn);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await dbAccess.ExecuteAsync(null!));
        }

        [Fact]
        [DisplayName("ExecuteAsync(spec, null transaction) 應擲 ArgumentNullException")]
        public async Task ExecuteAsync_NullTransaction_Throws()
        {
            using var conn = new SqlConnection();
            var dbAccess = new DbAccess(conn);
            var spec = new DbCommandSpec(DbCommandKind.Scalar, "SELECT 1");

            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await dbAccess.ExecuteAsync(spec, null!));
        }

        [Fact]
        [DisplayName("ExecuteBatch(null) 應擲 ArgumentNullException")]
        public void ExecuteBatch_NullBatch_Throws()
        {
            using var conn = new SqlConnection();
            var dbAccess = new DbAccess(conn);

            Assert.Throws<ArgumentNullException>(() => dbAccess.ExecuteBatch(null!));
        }

        [Fact]
        [DisplayName("ExecuteBatch 空 Commands 應擲 ArgumentException")]
        public void ExecuteBatch_EmptyCommands_Throws()
        {
            using var conn = new SqlConnection();
            var dbAccess = new DbAccess(conn);
            var batch = new DbBatchSpec();
            // Commands 預設為新的空集合

            Assert.Throws<ArgumentException>(() => dbAccess.ExecuteBatch(batch));
        }

        [Fact]
        [DisplayName("ExecuteBatchAsync(null) 應擲 ArgumentNullException")]
        public async Task ExecuteBatchAsync_NullBatch_Throws()
        {
            using var conn = new SqlConnection();
            var dbAccess = new DbAccess(conn);

            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await dbAccess.ExecuteBatchAsync(null!));
        }

        [Fact]
        [DisplayName("ExecuteBatchAsync 空 Commands 應擲 ArgumentException")]
        public async Task ExecuteBatchAsync_EmptyCommands_Throws()
        {
            using var conn = new SqlConnection();
            var dbAccess = new DbAccess(conn);
            var batch = new DbBatchSpec();

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await dbAccess.ExecuteBatchAsync(batch));
        }

        [Fact]
        [DisplayName("UpdateDataTable(null) 應擲 ArgumentNullException")]
        public void UpdateDataTable_NullSpec_Throws()
        {
            using var conn = new SqlConnection();
            var dbAccess = new DbAccess(conn);

            Assert.Throws<ArgumentNullException>(() => dbAccess.UpdateDataTable(null!));
        }

        [Fact]
        [DisplayName("UpdateDataTable spec 全為 null command 應擲 ArgumentException")]
        public void UpdateDataTable_AllNullCommands_Throws()
        {
            using var conn = new SqlConnection();
            var dbAccess = new DbAccess(conn);
            var spec = new DataTableUpdateSpec
            {
                DataTable = new System.Data.DataTable(),
                InsertCommand = null,
                UpdateCommand = null,
                DeleteCommand = null
            };

            Assert.Throws<ArgumentException>(() => dbAccess.UpdateDataTable(spec));
        }

        [Fact]
        [DisplayName("Query(null) 應擲 ArgumentNullException")]
        public void Query_NullCommand_Throws()
        {
            using var conn = new SqlConnection();
            var dbAccess = new DbAccess(conn);

            Assert.Throws<ArgumentNullException>(() => dbAccess.Query<object>(null!));
        }

        [Fact]
        [DisplayName("QueryAsync(null) 應擲 ArgumentNullException")]
        public async Task QueryAsync_NullCommand_Throws()
        {
            using var conn = new SqlConnection();
            var dbAccess = new DbAccess(conn);

            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await dbAccess.QueryAsync<object>(null!));
        }

        [Fact]
        [DisplayName("ToString 應包含 DatabaseType 與 Provider 名稱")]
        public void ToString_ContainsTypeAndProvider()
        {
            using var conn = new SqlConnection();
            var dbAccess = new DbAccess(conn);

            var text = dbAccess.ToString();

            Assert.Contains("DbAccess", text);
            Assert.Contains("DatabaseType", text);
            Assert.Contains("Provider", text);
        }

        [Fact]
        [DisplayName("UpdateDataTable spec.DataTable 為 null 應擲 ArgumentException")]
        public void UpdateDataTable_NullDataTable_ThrowsArgumentException()
        {
            using var conn = new SqlConnection();
            var dbAccess = new DbAccess(conn);
            var spec = new DataTableUpdateSpec
            {
                DataTable = null,
                InsertCommand = new DbCommandSpec(DbCommandKind.NonQuery, "INSERT INTO t (c) VALUES ({0})", 1)
            };

            Assert.Throws<ArgumentException>(() => dbAccess.UpdateDataTable(spec));
        }

        [DbFact]
        [DisplayName("ExecuteNonQuery(string) 字串便捷方法應正確執行並回傳影響列數")]
        public void ExecuteNonQuery_StringOverload_ReturnsRowsAffected()
        {
            var dbAccess = new DbAccess("common");

            int rows = dbAccess.ExecuteNonQuery(
                "UPDATE st_user SET note={0} WHERE sys_id={1}", "test", "001");

            Assert.True(rows >= 0);
        }

        [DbFact]
        [DisplayName("ExecuteScalar(string) 字串便捷方法應回傳非 null 值")]
        public void ExecuteScalar_StringOverload_ReturnsValue()
        {
            var dbAccess = new DbAccess("common");

            object? val = dbAccess.ExecuteScalar("SELECT COUNT(*) FROM st_user");

            Assert.NotNull(val);
        }

        [DbFact]
        [DisplayName("ExecuteDataTable(string) 字串便捷方法應回傳有效 DataTable")]
        public void ExecuteDataTable_StringOverload_ReturnsDataTable()
        {
            var dbAccess = new DbAccess("common");

            var table = dbAccess.ExecuteDataTable("SELECT * FROM st_user");

            Assert.NotNull(table);
        }

        [DbFact]
        [DisplayName("ExecuteNonQueryAsync(string) 非同步字串便捷方法應正確執行")]
        public async Task ExecuteNonQueryAsync_StringOverload_ReturnsRowsAffected()
        {
            var dbAccess = new DbAccess("common");

            int rows = await dbAccess.ExecuteNonQueryAsync(
                "UPDATE st_user SET note={0} WHERE sys_id={1}", "test", "001");

            Assert.True(rows >= 0);
        }

        [DbFact]
        [DisplayName("ExecuteScalarAsync(string) 非同步字串便捷方法應回傳非 null 值")]
        public async Task ExecuteScalarAsync_StringOverload_ReturnsValue()
        {
            var dbAccess = new DbAccess("common");

            object? val = await dbAccess.ExecuteScalarAsync("SELECT COUNT(*) FROM st_user");

            Assert.NotNull(val);
        }

        [DbFact]
        [DisplayName("ExecuteDataTableAsync(string) 非同步字串便捷方法應回傳有效 DataTable")]
        public async Task ExecuteDataTableAsync_StringOverload_ReturnsDataTable()
        {
            var dbAccess = new DbAccess("common");

            var table = await dbAccess.ExecuteDataTableAsync("SELECT * FROM st_user");

            Assert.NotNull(table);
        }

        [DbFact]
        [DisplayName("Execute(command, transaction) 帶交易執行應成功")]
        public void Execute_WithTransaction_ExecutesSuccessfully()
        {
            using var conn = DbFunc.CreateConnection("common");
            conn.Open();
            var dbAccess = new DbAccess(conn);
            using var tran = conn.BeginTransaction();

            var spec = new DbCommandSpec(DbCommandKind.Scalar, "SELECT COUNT(*) FROM st_user");
            var result = dbAccess.Execute(spec, tran);

            tran.Commit();
            Assert.NotNull(result.Scalar);
        }

        [DbFact]
        [DisplayName("ExecuteAsync(command, transaction) 非同步帶交易執行應成功")]
        public async Task ExecuteAsync_WithTransaction_ExecutesSuccessfully()
        {
            using var conn = DbFunc.CreateConnection("common");
            conn.Open();
            var dbAccess = new DbAccess(conn);
            await using var tran = await conn.BeginTransactionAsync();

            var spec = new DbCommandSpec(DbCommandKind.Scalar, "SELECT COUNT(*) FROM st_user");
            var result = await dbAccess.ExecuteAsync(spec, tran);

            await tran.CommitAsync();
            Assert.NotNull(result.Scalar);
        }
    }
}
