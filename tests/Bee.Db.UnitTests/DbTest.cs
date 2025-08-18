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
        /// 磅︽ SQL 琩高眔 DataTable
        /// </summary>
        [Fact]
        public void ExecuteDataTable()
        {
            string sql = "SELECT * FROM ts_user";
            var table = SysDb.ExecuteDataTable("common", sql);
        }

        [Fact]
        public void ExecuteNonQuery()
        {
            int i = BaseFunc.RndInt(0, 100);
            string sql = $"Update ts_user Set note='{i}' Where sys_id = '001'";
            int rows = SysDb.ExecuteNonQuery("common", sql);
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

        /// <summary>
        /// ㄏノ把计Α磅︽ SQL 琩高眔 DataTable
        /// </summary>
        [Fact]
        public void ExecuteDataTable_Parameter()
        {
            var heper = DbFunc.CreateDbCommandHelper();
            heper.AddParameter(SysFields.Id, FieldDbType.String, "001");
            string sql = "SELECT * FROM ts_user WHERE sys_id = @sys_id";
            heper.SetCommandText(sql);
            var table2 = heper.ExecuteDataTable("common");
        }
    }
}