using System.Text;
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

        [Fact]
        public async Task Hello()
        {
            // Arrange
            var controller = new ApiServiceController();

            var validJson = "{\"ProgID\":\"System\",\"Action\":\"ExecFunc\",\"Value\":{\"$type\":\"Bee.Define.TExecFuncArgs, Bee.Define\",\"FuncID\":\"Hello\",\"Parameters\":[]}}";
            var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(validJson));

            var context = new DefaultHttpContext();
            context.Request.Headers["X-Api-Key"] = "valid-api-key";
            context.Request.Headers["Authorization"] = "Bearer valid-token";
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