using Bee.Db;
using Bee.Define;

namespace Bee.Api.Core.UnitTests
{
    public class ApiCoreTest
    {
        static ApiCoreTest()
        {
            // �]�w��������
            BackendInfo.DefineProvider = new TFileDefineProvider();
            BackendInfo.BusinessObjectProvider = new Bee.Cache.TBusinessObjectProvider();
            BackendInfo.SystemObject = new Bee.Business.TSystemObject();
            // ���U��Ʈw���Ѫ�
            DbProviderManager.RegisterProvider(EDatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
        }

        /// <summary>
        /// �z�L API ���� Hello ��k�C
        /// </summary>
        [Fact]
        public void Hello()
        {
            // �]�w ExecFunc ��k�ǤJ�޼�
            Guid accessToken = Guid.NewGuid();
            var execFuncArgs = new TExecFuncArgs("Hello");
            execFuncArgs.Parameters.Add("Name", "World");
            execFuncArgs.Parameters.Add("Age", 18);
            // �]�w API ��k�ǤJ�޼�
            var args = new TApiServiceArgs()
            {
                ProgID = SysProgIDs.System,
                Action = "ExecFunc",
                Value = execFuncArgs
            };
            // ���� API ��k
            var executor = new TApiServiceExecutor(accessToken);
            var result = executor.Execute(args);
            // ���o ExecFunc ��k�ǥX���G
            var execFuncResult = result.Value as TExecFuncResult;
            Assert.NotNull(execFuncResult);  // �T�{ ExecFunc ��k�ǥX���G���� null
        }
    }
}