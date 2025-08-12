using Bee.Base;
using Bee.Cache;
using Bee.Define;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class DbTest
    {
        static DbTest()
        {
        }

        /// <summary>
        /// ���� SQL �d�ߡA�è��o DataTable�C
        /// </summary>
        [Fact]
        public void ExecuteDataTable()
        {
            string sql = "SELECT * FROM ts_user";
            var table = SysDb.ExecuteDataTable("common", sql);
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
            var helper = DbFunc.CreateDbCommandHelper();
            string sql = "SELECT sys_id AS userID, sys_name AS UserName, sys_insert_time AS InsertTime FROM ts_user";
            helper.SetCommandText(sql);
            var list = helper.Query<User>("common").ToList();
            var list2 = helper.Query<User>("common").ToList();
            var list3 = helper.Query<User2>("common").ToList();
        }

        /// <summary>
        /// �ϥΰѼƦ����� SQL �d�ߡA�è��o DataTable�C
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