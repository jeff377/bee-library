using System.Text;
using Bee.Api.Core;
using Bee.Base;
using Bee.Base.Serialization;
using Bee.Api.Contracts;
using Bee.Api.Contracts.System;
using Bee.Define;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bee.Api.AspNetCore.UnitTests
{
    [Collection("Initialize")]
    public class ApiAspNetCoreTest
    {
        private Guid _accessToken;

        static ApiAspNetCoreTest()
        {
        }

        /// <summary>
        /// ���եΪ� ApiServiceController ���O�C
        /// </summary>
        public class ApiServiceController : AspNetCore.ApiServiceController { }

        /// <summary>
        /// ���o JSON-RPC �ШD�ҫ��� JSON �r��C
        /// </summary>
        /// <param name="progId">�{���N�X�C</param>
        /// <param name="action">����ʧ@�C</param>
        /// <param name="args">�ǤJ��ơC</param>
        private string GetRpcRequestJson(string progId, string action, object args)
        {
            // �]�w JSON-RPC �ШD�ҫ�
            var request = new JsonRpcRequest()
            {
                Method = $"{progId}.{action}",
                Params = new JsonRpcParams()
                {
                    Value = args
                },
                Id = Guid.NewGuid().ToString()
            };
            return request.ToJson();
        }

        /// <summary>
        /// ���� ApiServiceController �öǦ^�ϧǦC�Ƶ��G�C
        /// </summary>
        /// <typeparam name="TResult">�^�ǫ��O�C</typeparam>
        /// <param name="accessToken">�s���O�P�C</param>
        /// <param name="progId">�{���N�X�C</param>
        /// <param name="action">����ʧ@�C</param>
        /// <param name="args">JSON-RPC �ǤJ�ѼơC</param>
        /// <returns>�ϧǦC�ƫ᪺���浲�G�C</returns>
        private async Task<TResult> ExecuteRpcAsync<TResult>(Guid accessToken, string progId, string action, object args)
        {
            // �إ� JSON-RPC �ШD���e
            string json = GetRpcRequestJson(progId, action, args);

            var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var context = new DefaultHttpContext();
            context.Request.Headers["X-Api-Key"] = "valid-api-key";
            context.Request.Headers["Authorization"] = $"Bearer {accessToken}";
            context.Request.Headers["Content-Type"] = "application/json";
            context.Request.Body = requestBody;

            var controller = new ApiServiceController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = context
                }
            };

            // ���� API
            var result = await controller.PostAsync();
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.False(string.IsNullOrWhiteSpace(contentResult.Content));

            var response = SerializeFunc.JsonToObject<JsonRpcResponse>(contentResult.Content);
            return (TResult)response.Result.Value;
        }

        /// <summary>
        /// �����n�J�è��o AccessToken�C
        /// </summary>
        /// <returns></returns>
        private async Task<Guid> GetAccessTokenAsync()
        {
            if (_accessToken == Guid.Empty)
            {
                // �����n�J�A��ڱ��p���q API �n�J���o AccessToken
                var args = new LoginArgs()
                {
                    UserId = "demo",    
                    Password = "1234"
                };
                var result = await ExecuteRpcAsync<LoginResult>(Guid.Empty, SysProgIds.System, "Login", args);
                _accessToken = result.AccessToken;
            }
            return _accessToken;
        }

        /// <summary>
        /// ���� Ping ��k�C
        /// </summary>
        [Fact]
        public async Task Ping()
        {
            var args = new PingArgs()
            {
                ClientName = "TestClient",
                TraceId = "001",
            };
            var result = await ExecuteRpcAsync<PingResult>(Guid.Empty, SysProgIds.System, "Ping", args);
            Assert.NotNull(result);
            Assert.Equal("ok", result.Status);
            Assert.Equal("001", result.TraceId);
        }

        [Fact]
        public async Task Hello()
        {
            Guid accessToken = await GetAccessTokenAsync();
            var args = new ExecFuncArgs("Hello");
            var result = await ExecuteRpcAsync<ExecFuncResult>(accessToken, SysProgIds.System, "ExecFunc", args);
            Assert.NotNull(result);
        }
    }
}