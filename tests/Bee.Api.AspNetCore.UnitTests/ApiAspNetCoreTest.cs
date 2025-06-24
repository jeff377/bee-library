using System.Text;
using Bee.Api.Core;
using Bee.Base;
using Bee.Db;
using Bee.Define;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bee.Api.AspNetCore.UnitTests
{
    public class ApiAspNetCoreTest
    {
        static ApiAspNetCoreTest()
        {
            // �]�w�w�q���|
            BackendInfo.DefinePath = @"D:\DefinePath";
            // ���U��Ʈw���Ѫ�
            DbProviderManager.RegisterProvider(DatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
        }

        /// <summary>
        /// ���եΪ� ApiServiceController ���O�C
        /// </summary>
        public class ApiServiceController : TApiServiceController { }

        /// <summary>
        /// ���o JSON-RPC �ШD�ҫ��� JSON �r��C
        /// </summary>
        /// <param name="progId">�{���N�X�C</param>
        /// <param name="action">����ʧ@�C</param>
        /// <param name="args">�ǤJ��ơC</param>
        private string GetRpcRequestJson(string progId, string action, object args)
        {
            // �]�w JSON-RPC �ШD�ҫ�
            var request = new TJsonRpcRequest()
            {
                Method = $"{progId}.{action}",
                Params = new TJsonRpcParams()
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
        /// <param name="progId">�{���N�X�C</param>
        /// <param name="action">����ʧ@�C</param>
        /// <param name="args">JSON-RPC �ǤJ�ѼơC</param>
        /// <param name="accessToken">�s���v���C</param>
        /// <returns>�ϧǦC�ƫ᪺���浲�G�C</returns>
        private async Task<TResult> ExecuteRpcAsync<TResult>(string progId, string action, object args, Guid? accessToken = null)
        {
            accessToken ??= Guid.NewGuid();

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

            var response = SerializeFunc.JsonToObject<TJsonRpcResponse>(contentResult.Content);
            return (TResult)response.Result.Value;
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
            var result = await ExecuteRpcAsync<PingResult>(SysProgIds.System, "Ping", args);
            Assert.NotNull(result);
            Assert.Equal("ok", result.Status);
            Assert.Equal("001", result.TraceId);
        }

        [Fact]
        public async Task Hello()
        {
            var args = new ExecFuncArgs("Hello");
            var result = await ExecuteRpcAsync<ExecFuncResult>(SysProgIds.System, "ExecFunc", args);
            Assert.NotNull(result);
        }
    }
}