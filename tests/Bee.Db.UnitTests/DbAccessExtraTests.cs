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
    }
}
