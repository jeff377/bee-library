using Bee.Api.Core;
using Bee.Api.Core.JsonRpc;
using Bee.Base;
using Bee.Base.Serialization;
using Bee.Api.Contracts;
using Bee.Api.Contracts.System;
using Bee.Define;

namespace Bee.Api.Core.UnitTests
{
    [Collection("Initialize")]
    public class ApiCoreTest
    {
        private Guid _accessToken;

        static ApiCoreTest()
        {

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
            ApiPayloadConverter.TransformTo(request.Params, PayloadFormat.Encoded);
            string encodedJson = request.ToJson();
            Assert.NotEmpty(encodedJson);

            // ���ոѽX
            ApiPayloadConverter.RestoreFrom(request.Params, PayloadFormat.Encoded);
            string decodedJson = request.ToJson();
            Assert.NotEmpty(decodedJson);
        }

        /// <summary>
        /// ���� API ��k�C
        /// </summary>
        /// <param name="accessToken">�s���O�P�C</param>
        /// <param name="progId">�{���N�X�C</param>
        /// <param name="action">����ʧ@�C</param>
        /// <param name="value">�ǤJ��ơC</param>
        private T ApiExecute<T>(Guid accessToken, string progId, string action, object value)
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

            var executor = new JsonRpcExecutor(accessToken);
            var response = executor.Execute(request);
            return (T)response.Result.Value;
        }

        /// <summary>
        /// �����n�J�è��o AccessToken�C
        /// </summary>
        /// <returns></returns>
        private Guid GetAccessToken()
        {
            if (_accessToken == Guid.Empty)
            {
                // �����n�J�A��ڱ��p���q API �n�J���o AccessToken
                var args = new LoginArgs()
                {
                    UserId = "demo",
                    Password = "1234"
                };
                var result = ApiExecute<LoginResult>(Guid.Empty, SysProgIds.System, "Login", args);
                _accessToken = result.AccessToken;
            }
            return _accessToken;
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
            var result = ApiExecute<PingResult>(Guid.Empty, SysProgIds.System, "Ping", args);
            Assert.NotNull(result);
            Assert.Equal("ok", result.Status);
            Assert.Equal("001", result.TraceId);
        }

        /// <summary>
        /// ���� GetCommonConfiguration ��k�C
        /// </summary>
        [Fact]
        public void GetCommonConfiguration()
        {
            var args = new GetCommonConfigurationArgs();
            var result = ApiExecute<GetCommonConfigurationResult>(Guid.Empty, SysProgIds.System, SystemActions.GetCommonConfiguration, args);
            Assert.NotNull(result);
            //Assert.Equal("messagepack", result.Serializer);
        }

        /// <summary>
        /// �z�L API ���� Hello ��k�C
        /// </summary>
        [Fact]
        public void Hello()
        {
            // ���o AccessToken
            Guid accessToken = GetAccessToken();

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

    }
}