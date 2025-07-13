using Bee.Base;
using Bee.Cache;
using Bee.Db;
using Bee.Define;

namespace Bee.Api.Core.UnitTests
{
    public class ApiCoreTest
    {
        static ApiCoreTest()
        {
            // �]�w�w�q���|
            BackendInfo.DefinePath = @"D:\DefinePath";
            // ��l�ƪ��_
            var settings = CacheFunc.GetSystemSettings();
            settings.BackendConfiguration.InitializeSecurityKeys();
            // ���U��Ʈw���Ѫ�
            DbProviderManager.RegisterProvider(DatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            // .NET 8 �w�]���� BinaryFormatter�A�ݤ�ʱҥ�
            AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);
        }

        /// <summary>
        /// JSON-RPC �ШD�ҫ��ǦC�ơC
        /// </summary>
        [Fact]
        public void JsonRpcRequestSerialize()
        {
            var request = new JsonRpcRequest()
            {
                Method = $"{SysProgIds.System}.ExecFunc",
                Params = new JsonRpcParams()
                {
                    Value = new ExecFuncArgs("Hello")
                },
                Id = Guid.NewGuid().ToString()
            };
            string json = request.ToJson();
            Assert.NotEmpty(json);

            // ���սs�X
            request.Encode(null);
            string encodedJson = request.ToJson();
            Assert.NotEmpty(encodedJson);

            // ���ոѽX
            request.Decode(null);
            string decodedJson = request.ToJson();
            Assert.NotEmpty(decodedJson);
        }

        /// <summary>
        /// ���� API ��k�C
        /// </summary>
        /// <param name="progId">�{���N�X�C</param>
        /// <param name="action">����ʧ@�C</param>
        /// <param name="value">�ǤJ��ơC</param>
        private T ApiExecute<T>(string progId, string action, object value)
        {
            // �]�w JSON-RPC �ШD�ҫ�
            var request = new JsonRpcRequest()
            {
                Method = $"{progId}.{action}",
                Params = new JsonRpcParams()
                {
                    Value = value
                },
                Id = Guid.NewGuid().ToString()
            };
            Guid accessToken = Guid.NewGuid();
            var executor = new JsonRpcExecutor(accessToken);
            var response = executor.Execute(request);
            return (T)response.Result.Value;
        }

        /// <summary>
        /// �z�L API ���� Ping ��k�C
        /// </summary>
        [Fact]
        public void Ping()
        {
            var args = new PingArgs()
            {
                ClientName = "TestClient",
                TraceId = "001",
            };
            var result = ApiExecute<PingResult>(SysProgIds.System, "Ping", args);
            Assert.NotNull(result);
            Assert.Equal("ok", result.Status);
            Assert.Equal("001", result.TraceId);
        }

        /// <summary>
        /// ���� GetApiPayloadOptions ��k�C
        /// </summary>
        [Fact]
        public void GetApiPayloadOptions()
        {
            var args = new GetApiPayloadOptionsArgs();
            var result = ApiExecute<GetApiPayloadOptionsResult>(SysProgIds.System, "GetApiPayloadOptions", args);
            Assert.NotNull(result);
            //Assert.Equal("messagepack", result.Serializer);
        }

        /// <summary>
        /// �z�L API ���� Hello ��k�C
        /// </summary>
        [Fact]
        public void Hello()
        {
            // �]�w ExecFunc ��k�ǤJ�޼�
            Guid accessToken = Guid.NewGuid();
            // �]�w �]�w JSON-RPC �ШD�ҫ�
            var request = new JsonRpcRequest()
            {
                Method = $"{SysProgIds.System}.ExecFunc",
                Params = new JsonRpcParams()
                {
                    Value = new ExecFuncArgs("Hello")
                },
                Id = Guid.NewGuid().ToString()
            };

            string json = request.ToJson();
            // ���� API ��k
            var executor = new JsonRpcExecutor(accessToken);
            var response = executor.Execute(request);
            // ���o ExecFunc ��k�ǥX���G
            var execFuncResult = response.Result.Value as ExecFuncResult;
            Assert.NotNull(execFuncResult);  // �T�{ ExecFunc ��k�ǥX���G���� null
        }

        [Fact]
        public void TestDatabaseId()
        {
            var args = new ExecFuncArgs("TestDatabaseId");
            args.Parameters.Add("DatabaseId", "common");
            var result = ApiExecute<ExecFuncResult>(SysProgIds.System, "ExecFunc", args);
            Assert.NotNull(result);
        }
    }
}