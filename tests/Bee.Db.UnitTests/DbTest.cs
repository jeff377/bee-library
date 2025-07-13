using Bee.Base;
using Bee.Cache;
using Bee.Define;

namespace Bee.Db.UnitTests
{
    public class DbTest
    {
        static DbTest()
        {
            // �]�w�w�q���|
            BackendInfo.DefinePath = @"D:\DefinePath";
            // ��l�ƪ��_
            var settings = CacheFunc.GetSystemSettings();
            settings.BackendConfiguration.InitializeSecurityKeys();

            BackendInfo.DatabaseType = DatabaseType.SQLServer;
            // ���U��Ʈw���Ѫ�
            DbProviderManager.RegisterProvider(DatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
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