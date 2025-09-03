using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Bee.Base;
using Bee.Cache;
using Bee.Define;
using Microsoft.Data.SqlClient;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class DbTest
    {
        static DbTest()
        {
        }


        /// <summary>
        /// 執行 SQL 查詢，並取得 DataTable。
        /// </summary>
        [Fact]
        public void ExecuteDataTable()
        {
            string sql = "SELECT * FROM ts_user";
            var table = SysDb.ExecuteDataTable("common", sql);

            var helper = DbFunc.CreateDbCommandHelper();
            helper.SetCommandText(sql);
            var dbAccess = new DbAccess("common");
            table = dbAccess.ExecuteDataTable(helper.DbCommand);

            // 由 DbAccess 管理連線
            var command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            dbAccess = new DbAccess("common");
            var result = dbAccess.Execute(command);
            Assert.NotNull(result.Table);

            // 由外部管理連線
            using (var conn = DbFunc.CreateConnection("common"))
            {
                dbAccess = new DbAccess(conn);
                result = dbAccess.Execute(command);
                Assert.NotNull(result.Table);
            }

            sql = "SELECT * FROM ts_user WHERE sys_id = {0} OR sys_id = {1} ";
            command = new DbCommandSpec(DbCommandKind.DataTable, sql);
            command.Parameters.Add("p1", "001");
            command.Parameters.Add("p2", "002");
            dbAccess = new DbAccess("common");
            result = dbAccess.Execute(command);
            Assert.NotNull(result.Table);
        }

        /// <summary>
        /// 非同步執行 SQL 查詢，並取得 DataTable。
        /// </summary>
        [Fact]
        public async Task ExecuteDataTableAsync()
        {
            string sql = "SELECT * FROM ts_user";
            var table = await SysDb.ExecuteDataTableAsync("common", sql);
            Assert.NotNull(table);
            Assert.True(table.Rows.Count > 0);
        }

        [Fact]
        public void ExecuteNonQuery()
        {
            int i = BaseFunc.RndInt(0, 100);
            string sql = $"Update ts_user Set note='{i}' Where sys_id = '001'";
            int rows = SysDb.ExecuteNonQuery("common", sql);
        }

        [Fact]
        public void ExecuteReader()
        {
            string sql = "SELECT sys_id, sys_name FROM ts_user WHERE sys_id = '001'";
            var helper = new DbCommandHelper(DatabaseType.SQLServer);
            helper.SetCommandFormatText(sql);
            using (var reader = SysDb.ExecuteReader("common", helper.DbCommand))
            {
                Assert.True(reader.Read());
                Assert.Equal("001", reader["sys_id"].ToString());
                // 可依需求驗證其他欄位
            }
        }

        [Fact]
        public async Task ExecuteReaderAsync()
        {
            string sql = "SELECT sys_id, sys_name FROM ts_user WHERE sys_id = '001'";
            var helper = new DbCommandHelper(DatabaseType.SQLServer);
            helper.SetCommandFormatText(sql);
            using (var reader = await SysDb.ExecuteReaderAsync("common", helper.DbCommand))
            {
                Assert.True(await reader.ReadAsync());
                Assert.Equal("001", reader["sys_id"].ToString());
                // 可依需求驗證其他欄位
            }
        }

        [Fact]
        public async Task ExecuteNonQueryAsync()
        {
            int i = BaseFunc.RndInt(0, 100);
            string sql = $"Update ts_user Set note='{i}' Where sys_id = '001'";
            int rows = await SysDb.ExecuteNonQueryAsync("common", sql);
            int rows2 = await SysDb.ExecuteNonQueryAsync("common", sql);
        }

        [Fact]
        public void ExecuteScalar()
        {
            string sql = $"Select note From ts_user Where sys_id = '001'";
            var value = SysDb.ExecuteScalar("common", sql);
        }

        [Fact]
        public async Task ExecuteScalarAsync()
        {
            string sql = $"Select note From ts_user Where sys_id = '001'";
            var value = await SysDb.ExecuteScalarAsync("common", sql);
            var value2 = await SysDb.ExecuteScalarAsync("common", sql);
        }

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

        [Fact]
        public void Query()
        {
            string sql = "SELECT sys_id AS userID, sys_name AS UserName, sys_insert_time AS InsertTime FROM ts_user";
            var list = SysDb.Query<User>("common", sql).ToList();
            var list3 = SysDb.Query<User2>("common", sql).ToList();
        }

        [Fact]
        public async Task QueryAsync()
        {
            string sql = "SELECT sys_id AS userID, sys_name AS UserName, sys_insert_time AS InsertTime FROM ts_user";
            var list = await SysDb.QueryAsync<User>("common", sql);
            Assert.NotNull(list);
            Assert.True(list.Count > 0);

            var list2 = await SysDb.QueryAsync<User2>("common", sql);
            Assert.NotNull(list2);
        }

        [Fact]
        public async Task QueryStreamAsync()
        {
            string sql = "SELECT sys_id AS userID, sys_name AS UserName, sys_insert_time AS InsertTime FROM ts_user";
            int count = 0;
            await foreach (var user in SysDb.QueryStreamAsync<User>("common", sql))
            {
                Assert.NotNull(user);
                Assert.False(string.IsNullOrEmpty(user.UserID));
                count++;
            }
            Assert.True(count > 0);
        }

        /// <summary>
        /// 使用參數式執行 SQL 查詢，並取得 DataTable。
        /// </summary>
        [Fact]
        public void ExecuteDataTable_Parameters()
        {
            var helper = DbFunc.CreateDbCommandHelper();
            helper.AddParameter(SysFields.Id, FieldDbType.String, "001");
            string sql = "SELECT * FROM ts_user WHERE sys_id = @sys_id";
            helper.SetCommandText(sql);
            using (var command = helper.DbCommand)
            {
                var table = SysDb.ExecuteDataTable("common", helper.DbCommand);
            }
        }
    }
}