using Bee.Base;
using Bee.Db;
using Bee.Define;

namespace Bee.Connect.UnitTests
{
    public class ConnectTest
    {
        static ConnectTest()
        {
            SysInfo.IsDebugMode = true;
            // �]�w�w�q���|
            BackendInfo.DefinePath = @"D:\DefinePath";
            // �]�w��������
            BackendInfo.DefineProvider = new TFileDefineProvider();
            BackendInfo.BusinessObjectProvider = new Bee.Business.TBusinessObjectProvider();
            // ���U��Ʈw���Ѫ�
            DbProviderManager.RegisterProvider(EDatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            // �w�]��Ʈw�s��
            BackendInfo.DatabaseID = "common";
            // .NET 8 �w�]���� BinaryFormatter�A�ݤ�ʱҥ�
            AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);
        }

        [Theory]
        [InlineData("http://localhost/jsonrpc/api")]
        [InlineData("http://localhost/jsonrpc_aspnet/api")]
        public void ApiConnectValidator(string apiUrl)
        {
            var validator = new TApiConnectValidator();
            var connectType = validator.Validate(apiUrl);

            Assert.Equal(EConnectType.Remote, connectType);  // �T�{�s�u�覡�����ݳs�u
        }

        /// <summary>
        /// �z�L SystemConnector ���� Hello ��k�C
        /// </summary>
        [Fact]
        public void SystemConnector_Hello()
        {
            // �]�w ExecFunc ��k�ǤJ�޼�
            var args = new TExecFuncArgs("Hello");
            // �z�L Connector ���� ExecFunc ��k
            Guid accessToken = Guid.NewGuid();
            var connector = new TSystemApiConnector(accessToken);
            var result = connector.ExecFunc(args);
            Assert.NotNull(result);  // �T�{ ExecFunc ��k�ǥX���G���� null
        }

        /// <summary>
        /// �z�L FormConnector ���� Hello ��k�C
        /// </summary>
        [Fact]
        public void FormConnector_Hello()
        {
            // �]�w ExecFunc ��k�ǤJ�޼�
            var args = new TExecFuncArgs("Hello");
            // �z�L Connector ���� ExecFunc ��k
            Guid accessToken = Guid.NewGuid();
            var connector = new TFormApiConnector(accessToken, "demo");
            var result = connector.ExecFunc(args);
            Assert.NotNull(result);  // �T�{ ExecFunc ��k�ǥX���G���� null
        }

        /// <summary>
        /// ���� SystemApiConnector �� CreateSession ��k�C
        /// </summary>
        [Fact]
        public void SystemConnector_CreateSession()
        {
            // Arrange
            string userId = "001";
            int expiresIn = 600;
            bool oneTime = false;

            // ���ͤ@���H�� Guid �@�� accessToken�]�ȥΩ��l�ơACreateSession �|�^�Ƿs�� token�^
            Guid accessToken = Guid.NewGuid();
            var connector = new TSystemApiConnector(accessToken);

            // Act
            Guid newToken = connector.CreateSession(userId, expiresIn, oneTime);

            // Assert
            Assert.NotEqual(Guid.Empty, newToken); // �����o���Ī� accessToken
        }
    }
}