using Bee.Db;
using Bee.Define;

namespace Bee.Connect.UnitTests
{
    public class ConnectTest
    {
        static ConnectTest()
        {
            // �]�w�w�q���|
            BackendInfo.DefinePath = @"D:\Bee\src\DefinePath";
            // �]�w��������
            BackendInfo.DefineProvider = new TFileDefineProvider();
            BackendInfo.BusinessObjectProvider = new Bee.Cache.TBusinessObjectProvider();
            BackendInfo.SystemObject = new Bee.Business.TSystemObject();
            // ���U��Ʈw���Ѫ�
            DbProviderManager.RegisterProvider(EDatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            // .NET 8 �w�]���� BinaryFormatter�A�ݤ�ʱҥ�
            AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);
        }

        [Fact]
        public void ApiConnectValidator()
        {
            var validator = new TApiConnectValidator();
            var connectType = validator.Validate("http://localhost/jsonrpc_aspnet/api");
            Assert.Equal(EConnectType.Remote, connectType);  // �T�{�s�u�覡�����ݳs�u
        }

        /// <summary>
        /// �z�L Connect ���� Hello ��k�C
        /// </summary>
        [Fact]
        public void Hello()
        {
            // �]�w ExecFunc ��k�ǤJ�޼�
            var args = new TExecFuncArgs("Hello");
            // �z�L Connect ���� ExecFunc ��k
            Guid accessToken = Guid.NewGuid();
            var connector = new TSystemConnector(accessToken);
            var result = connector.ExecFunc(args);
            Assert.NotNull(result);  // �T�{ ExecFunc ��k�ǥX���G���� null
        }
    }
}