using Bee.Api.Core;
using Bee.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bee.Api.AspNetCore
{
    /// <summary>
    /// API 服務控制器基底類別。
    /// </summary>
    [ApiController]
    [Route("api")]
    [Produces("application/json")]
    public abstract class TApiServiceController : ControllerBase
    {
        /// <summary>
        /// 處理 HTTP POST 請求，並執行相應的 API 服務。
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> PostAsync()
        {
            var apiKey = HttpContext.Request.Headers["X-Api-Key"].ToString();
            var authorization = HttpContext.Request.Headers["Authorization"].ToString();

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(authorization))
            {
                return CreateErrorResponse(StatusCodes.Status401Unauthorized, -32600, "Missing or invalid authentication headers");
            }

            Guid accessToken = TryGetAccessToken(authorization);
            if (accessToken == Guid.Empty)
            {
                return CreateErrorResponse(StatusCodes.Status401Unauthorized, -32600, "Invalid access token");
            }

            string json;
            using var reader = new StreamReader(HttpContext.Request.Body);
            json = await reader.ReadToEndAsync();

            TJsonRpcRequest? request;
            try
            {
                request = SerializeFunc.JsonToObject<TJsonRpcRequest>(json);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(StatusCodes.Status400BadRequest, -32700, $"Failed to deserialize request body: {ex.Message}");
            }

            if (request == null)
            {
                return CreateErrorResponse(StatusCodes.Status400BadRequest, -32600, "Invalid request body");
            }

            try
            {
                var result = Execute(accessToken, request);
                return new ContentResult
                {
                    Content = result.ToJson(),
                    ContentType = "application/json",
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(StatusCodes.Status500InternalServerError, -32000, "Internal server error", request.Id, ex.InnerException?.Message ?? ex.Message);
            }
        }


        /// <summary>
        /// 執行 API 方法。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="request">JSON-RPC 請求模型。</param>
        protected virtual TJsonRpcResponse Execute(Guid accessToken, TJsonRpcRequest request)
        {
            var executor = new TJsonRpcExecutor(accessToken);
            return executor.Execute(request);
        }

        /// <summary>
        /// 取得存取令牌。
        /// </summary>
        /// <param name="authorization">由 Header 傳入的 Authorization 值。</param>
        private Guid TryGetAccessToken(string authorization)
        {
            if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer "))
            {
                return Guid.Empty;
            }

            var token = authorization.Substring("Bearer ".Length).Trim();
            return Guid.TryParse(token, out var guid) ? guid : Guid.Empty;
        }

        /// <summary>
        /// 建立統一格式的 JSON-RPC 錯誤回應。
        /// </summary>
        private IActionResult CreateErrorResponse(int httpStatusCode, int code, string message, string? id = null, string? data = null)
        {
            var response = new TJsonRpcResponse
            {
                Id = id,
                Error = new TJsonRpcError
                {
                    Code = code,
                    Message = message,
                    Data = data
                }
            };
            return StatusCode(httpStatusCode, response);
        }

    }
}
