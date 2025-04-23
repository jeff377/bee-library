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
            BackendInfo.DefinePath = @"D:\Bee\src\DefinePath";
            // ���U��Ʈw���Ѫ�
            DbProviderManager.RegisterProvider(EDatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
        }

        /// <summary>
        /// ���եΪ� ApiServiceController ���O�C
        /// </summary>
        public class ApiServiceController : TApiServiceController { }

        /// <summary>
        /// ���o JSON-RPC �ШD�ҫ��� JSON �r��C
        /// </summary>
        /// <param name="progID">�{���N�X�C</param>
        /// <param name="action">����ʧ@�C</param>
        /// <param name="value">�ǤJ��ơC</param>
        private string GetRpcRequestJson(string progID, string action, object value)
        {
            // �]�w JSON-RPC �ШD�ҫ�
            var request = new TJsonRpcRequest()
            {
                Method = $"{progID}.{action}",
                Params = new TJsonRpcParams()
                {
                    Value = value
                },
                Id = Guid.NewGuid()
            };
            return request.ToJson();
        }

        [Fact]
        public async Task Hello()
        {
            // �]�w ExecFunc ��k�ǤJ�޼�
            Guid accessToken = Guid.NewGuid();
            var args = new TExecFuncArgs("Hello");
            // ���o JSON-RPC �ШD�ҫ��� JSON �r��
            string json =GetRpcRequestJson(SysProgIDs.System, "ExecFunc", args);

            // Arrange
            var controller = new ApiServiceController();
            var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(json));

            var context = new DefaultHttpContext();
            context.Request.Headers["X-Api-Key"] = "valid-api-key";
            context.Request.Headers["Authorization"] = $"Bearer {accessToken}";
            context.Request.Body = requestBody;

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = context
            };

            // Act
            var result = await controller.PostAsync();

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
            Assert.Equal("application/json", contentResult.ContentType);
        }
    }
}