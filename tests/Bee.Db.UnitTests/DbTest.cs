using Bee.Base;
using Bee.Cache;
using Bee.Define;

namespace Bee.Db.UnitTests
{
    public class DbTest
    {
        static DbTest()
        {
            // 設定定義路徑
            BackendInfo.DefinePath = @"D:\DefinePath";
            // 初始化金鑰
            var settings = CacheFunc.GetSystemSettings();
            settings.BackendConfiguration.InitializeSecurityKeys();

            BackendInfo.DatabaseType = DatabaseType.SQLServer;
            // 註冊資料庫提供者
            DbProviderManager.RegisterProvider(DatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
        }

        /// <summary>
        /// 執行 SQL 查詢，並取得 DataTable。
        /// </summary>
        [Fact]
        public void ExecuteDataTable()
        {
            string sql = "SELECT * FROM ts_user";
            var table = SysDb.ExecuteDataTable("common", sql);
        }

        /// <summary>
        /// 使用參數式執行 SQL 查詢，並取得 DataTable。
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