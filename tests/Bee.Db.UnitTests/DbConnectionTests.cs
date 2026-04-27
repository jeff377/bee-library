using System.ComponentModel;
using Bee.Definition;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class DbConnectionTests
    {
        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("OpenConnection 使用環境變數連線字串應成功連線")]
        public void OpenConnection_WithEnvConnStr_Succeeds()
        {
            using var conn = DbFunc.CreateConnection("common_sqlserver");
            conn.Open();
            Assert.Equal(System.Data.ConnectionState.Open, conn.State);
        }
    }
}
