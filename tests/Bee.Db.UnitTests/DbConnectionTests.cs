using System.ComponentModel;
using Bee.Db;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    [Collection("Initialize")]
    public class DbConnectionTests
    {
        [DbFact]
        [DisplayName("OpenConnection 使用環境變數連線字串應成功連線")]
        public void OpenConnection_WithEnvConnStr_Succeeds()
        {
            using var conn = DbFunc.CreateConnection("common");
            conn.Open();
            Assert.Equal(System.Data.ConnectionState.Open, conn.State);
        }
    }
}
