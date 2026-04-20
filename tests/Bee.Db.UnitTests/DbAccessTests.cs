using System.ComponentModel;
using System.Globalization;
using Bee.Base;
using Bee.Definition;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class DbAccessTests
    {
        public class User
        {
            public string? UserID { get; set; }
            public string? UserName { get; set; }
            public DateTime InsertTime { get; set; }
        }

        public class User2
        {
            public string? UserID { get; set; }
            public string? UserName { get; set; }
            public string? AccessToken { get; set; }
        }

        /// <summary>
        /// 執行 SQL 查詢，並取得 DataTable。
        /// </summary>
        [DbFact]
        [DisplayName("ExecuteDataTable 執行多種參數化查詢應回傳有效 DataTable")]
        public void ExecuteDataTable_VariousParameterFormats_ReturnsDataTable()
        {
            // 由 DbAccess 管理連線
            string sql = "SELECT * FROM st_user";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            var dbAccess = new DbAccess("common");
            var result = dbAccess.Execute(command);
            Assert.NotNull(result.Table);

            // 由外部管理連線
            using (var conn = DbFunc.CreateConnection("common"))
            {
                dbAccess = new DbAccess(conn);
                result = dbAccess.Execute(command);
                Assert.NotNull(result.Table);
            }

            sql = "SELECT * FROM st_user WHERE sys_id = {0} OR sys_id = {1} ";
            command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            command.Parameters.Add("p1", "001");
            command.Parameters.Add("p2", "002");
            dbAccess = new DbAccess("common");
            result = dbAccess.Execute(command);
            Assert.NotNull(result.Table);

            command = new DbCommandSpec(DbCommandKind.DataTable, sql, "001", "002");
            dbAccess = new DbAccess("common");
            result = dbAccess.Execute(command);
            Assert.NotNull(result.Table);

            var parameters = new Dictionary<string, object>
            {
                { "p1", "001" },
                { "p2", "002" }
            };
            sql = "SELECT * FROM st_user WHERE sys_id = {p1} OR sys_id = {p2} ";
            command = new DbCommandSpec(DbCommandKind.DataTable, sql, parameters);
            result = dbAccess.Execute(command);
            Assert.NotNull(result.Table);
        }

        /// <summary>
        /// 非同步執行 SQL 查詢，並取得 DataTable。
        /// </summary>
        [DbFact]
        [DisplayName("ExecuteDataTableAsync 非同步查詢應回傳含資料列的 DataTable")]
        public async Task ExecuteDataTableAsync_ValidQuery_ReturnsNonEmptyDataTable()
        {
            string sql = "SELECT * FROM st_user";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            var dbAccess = new DbAccess("common");
            var reulst = await dbAccess.ExecuteAsync(command);
            var table = reulst.Table;
            Assert.NotNull(table);
            Assert.True(table.Rows.Count > 0);
        }

        [DbFact]
        [DisplayName("ExecuteNonQuery 更新資料應成功執行")]
        public void ExecuteNonQuery_UpdateRow_Executes()
        {
            int i = BaseFunc.RndInt(0, 100);
            string sql = "Update st_user Set note={1} Where sys_id = {0}";
            var command = new DbCommandSpec(DbCommandKind.NonQuery, sql, "001", i);
            var dbAccess = new DbAccess("common");
            var result = dbAccess.Execute(command);
            Assert.NotNull(result);
            Assert.True(result.RowsAffected >= 0);
        }

        [DbFact]
        [DisplayName("ExecuteNonQueryAsync 非同步更新資料應成功執行")]
        public async Task ExecuteNonQueryAsync_UpdateRow_Executes()
        {
            int i = BaseFunc.RndInt(0, 100);
            string sql = "Update st_user Set note={1} Where sys_id = {0}";
            var command = new DbCommandSpec(DbCommandKind.NonQuery, sql, "001", i);
            var dbAccess = new DbAccess("common");
            var result = await dbAccess.ExecuteAsync(command);
            Assert.NotNull(result);
            Assert.True(result.RowsAffected >= 0);
        }

        [DbFact]
        [DisplayName("ExecuteScalar 查詢單一值應成功執行")]
        public void ExecuteScalar_SelectSingleValue_ReturnsScalar()
        {
            string sql = "Select note From st_user Where sys_id = {0}";
            var command = new DbCommandSpec(DbCommandKind.Scalar, sql, "001");
            var dbAccess = new DbAccess("common");
            var result = dbAccess.Execute(command);
            Assert.NotNull(result);
        }

        [DbFact]
        [DisplayName("Query 查詢應回傳強型別物件清單")]
        public void Query_ValidSql_ReturnsMappedObjects()
        {
            string sql = "SELECT sys_id AS userID, sys_name AS UserName, sys_insert_time AS InsertTime FROM st_user";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            var dbAccess = new DbAccess("common");
            var list = dbAccess.Query<User>(command);
            var list3 = dbAccess.Query<User2>(command);
            Assert.NotNull(list);
            Assert.NotNull(list3);
        }

        [DbFact]
        [DisplayName("QueryAsync 非同步查詢應回傳強型別物件清單")]
        public async Task QueryAsync_ValidSql_ReturnsMappedObjects()
        {
            string sql = "SELECT sys_id AS userID, sys_name AS UserName, sys_insert_time AS InsertTime FROM st_user";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            var dbAccess = new DbAccess("common");
            var list = await dbAccess.QueryAsync<User>(command);
            var list2 = await dbAccess.QueryAsync<User2>(command);
            Assert.NotNull(list);
            Assert.NotNull(list2);
        }

        [DbFact]
        [DisplayName("UpdateDataTable 修改資料列後更新應影響至少一筆資料")]
        public void UpdateDataTable_ModifiedRow_AffectsRows()
        {
            var dbAccess = new DbAccess("common");

            // 1.查詢 st_user 所有資料
            string sql = "SELECT * FROM st_user";
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            var result = dbAccess.Execute(command);
            var table = result.Table;
            Assert.NotNull(table);
            Assert.True(table.Rows.Count > 0, "st_user 無任何資料");

            // 2. 修改第一筆資料
            int i = BaseFunc.RndInt(0, 100);
            var row = table.Rows[0];
            row["note"] = i.ToString(CultureInfo.InvariantCulture);

            // 3. 用 DbTableCommandBuilder 建立 DataTableUpdateSpec
            var tableSchema = BackendInfo.DefineAccess.GetTableSchema("common", "st_user");
            var builder = new TableSchemaCommandBuilder(tableSchema);
            var updateSpec = builder.BuildUpdateSpec(table);

            // 4. 執行 UpdateDataTable
            int affected = dbAccess.UpdateDataTable(updateSpec);

            Assert.True(affected > 0, "沒有資料被更新");
        }

        [DbFact]
        [DisplayName("ExecuteBatch 批次執行含交易的多個命令應成功")]
        public void ExecuteBatch_WithTransaction_Succeeds()
        {
            var batch = new DbBatchSpec();
            batch.UseTransaction = true;
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.Scalar,
                    "SELECT COUNT(*) FROM st_user WHERE sys_id = {0}", "001"));
            int i = BaseFunc.RndInt(0, 100);
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.NonQuery,
                     "UPDATE st_user SET note={1} WHERE sys_id = {0}", "001", i));

            var dbAccess = new DbAccess("common");
            var result = dbAccess.ExecuteBatch(batch);
            Assert.NotNull(result);
        }

        [DbFact]
        [DisplayName("ExecuteBatchAsync 非同步批次執行含交易的多個命令應成功")]
        public async Task ExecuteBatchAsync_WithTransaction_Succeeds()
        {
            var batch = new DbBatchSpec();
            batch.UseTransaction = true;
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.Scalar,
                    "SELECT COUNT(*) FROM st_user WHERE sys_id = {0}", "001"));
            int i = BaseFunc.RndInt(0, 100);
            batch.Commands.Add(new DbCommandSpec(DbCommandKind.NonQuery,
                     "UPDATE st_user SET note={1} WHERE sys_id = {0}", "001", i));

            var dbAccess = new DbAccess("common");
            var result = await dbAccess.ExecuteBatchAsync(batch);
            Assert.NotNull(result);
        }
    }
}
