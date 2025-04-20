using Bee.Db;
using Bee.Define;

namespace Bee.Api.Core.UnitTests
{
    public class ApiCoreTest
    {
        static ApiCoreTest()
        {
            // �]�w�w�q���|
            BackendInfo.DefinePath = @"D:\Bee\src\DefinePath";
            // �]�w��������
            BackendInfo.DefineProvider = new TFileDefineProvider();
            BackendInfo.BusinessObjectProvider = new Bee.Cache.TBusinessObjectProvider();
            BackendInfo.SystemObject = new Bee.Business.TSystemObject();
            // ���U��Ʈw���Ѫ�
            DbProviderManager.RegisterProvider(EDatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
        }

        /// <summary>
        /// ���� API ��k�C
        /// </summary>
        /// <param name="progID">�{���N�X�C</param>
        /// <param name="action">����ʧ@�C</param>
        /// <param name="value">�ǤJ��ơC</param>
        private T ApiExecute<T>(string progID, string action, object value)
        {
            // �]�w API ��k�ǤJ�޼�
            var args = new TApiServiceArgs()
            {
                ProgID = SysProgIDs.System,
                Action = action,
                Value = value
            };
            Guid accessToken = Guid.NewGuid();
            var executor = new TApiServiceExecutor(accessToken);
            var result = executor.Execute(args);
            return (T)result.Value;
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

        [Fact]
        public void TestDatabaseId()
        {
            var args = new TExecFuncArgs("TestDatabaseId");
            args.Parameters.Add("DatabaseId", "common");
            var result = ApiExecute<TExecFuncResult>(SysProgIDs.System, "ExecFunc", args);
            Assert.NotNull(result);
        }
    }
}