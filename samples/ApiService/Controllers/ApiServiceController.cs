using Bee.Api.Core;
using Bee.Base;
using Bee.Define;
using Microsoft.AspNetCore.Mvc;

namespace ApiService.Controllers
{
    [ApiController]
    [Route("api")]
    [Produces("application/json")]
    public class ApiServiceController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> PostAsync()
        {
            var apiKey = HttpContext.Request.Headers["X-Api-Key"].ToString();
            var authorization = HttpContext.Request.Headers["Authorization"].ToString();

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(authorization))
            {
                return Unauthorized(new TApiServiceResult
                {
                    Message = "Missing or invalid authentication headers"
                });
            }

            Guid accessToken = TryGetAccessToken(authorization);

            string json;
            using var reader = new StreamReader(HttpContext.Request.Body);
            json = await reader.ReadToEndAsync();

            TApiServiceArgs args;
            try
            {
                args = SerializeFunc.JsonToObject<TApiServiceArgs>(json);
            }
            catch (Exception ex)
            {
                return BadRequest(new TApiServiceResult
                {
                    Message = $"Failed to deserialize request body: {ex.Message}"
                });
            }

            if (args == null)
            {
                return BadRequest(new TApiServiceResult
                {
                    Message = "Invalid request body"
                });
            }

            try
            {
                var result = Execute(accessToken, args);
                return new ContentResult
                {
                    Content = result.ToJson(),
                    ContentType = "application/json",
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                var result = new TApiServiceResult(args)
                {
                    Message = ex.Message
                };
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }
        }

        /// <summary>
        /// 執行 API 服務。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="args">呼叫 API 服務傳入引數。</param>
        public TApiServiceResult Execute(Guid accessToken, TApiServiceArgs args)
        {
            bool encrypted = args.Encrypted;
            // 傳入引數有加密，則進行解密
            if (encrypted) { args.Decrypt(); }
            // 執行指定方法
            var executor = new TApiServiceExecutor(accessToken);
            var result = executor.Execute(args);
            // 若傳入引數有加密，回傳結果也要加密
            if (encrypted) { result.Encrypt(); }
            return result;
        }

        private Guid TryGetAccessToken(string authorization)
        {
            if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer "))
            {
                return Guid.Empty;
            }

            var token = authorization.Substring("Bearer ".Length).Trim();
            return Guid.TryParse(token, out var guid) ? guid : Guid.Empty;
        }


    }

}
