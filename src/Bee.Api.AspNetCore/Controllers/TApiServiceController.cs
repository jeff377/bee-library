using Bee.Api.Core;
using Bee.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bee.Api.AspNetCore
{
    /// <summary>
    /// 提供 JSON-RPC API 處理的控制器基底類別，適用於 ASP.NET Core。
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
            if (!HttpContext.Request.ContentType?.Contains("application/json") ?? true)
            {
                return CreateErrorResponse(StatusCodes.Status415UnsupportedMediaType,
                    EJsonRpcErrorCode.InvalidRequest, "Unsupported media type");
            }

            using var reader = new StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(json))
            {
                return CreateErrorResponse(StatusCodes.Status400BadRequest,
                    EJsonRpcErrorCode.InvalidRequest, "Empty request body");
            }

            TJsonRpcRequest? request;
            try
            {
                request = SerializeFunc.JsonToObject<TJsonRpcRequest>(json);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(StatusCodes.Status400BadRequest,
                    EJsonRpcErrorCode.ParseError, $"Invalid JSON format: {ex.Message}");
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Method))
            {
                return CreateErrorResponse(StatusCodes.Status400BadRequest,
                    EJsonRpcErrorCode.InvalidRequest, "Missing method");
            }

            // 授權驗證統一交由 Validator 處理
            var apiKey = HttpContext.Request.Headers["X-Api-Key"].ToString();
            var authorization = HttpContext.Request.Headers["Authorization"].ToString();
            var context = new TApiAuthorizationContext
            {
                ApiKey = apiKey,
                Authorization = authorization,
                Method = request.Method
            };
            var validator = ApiAuthorizationValidatorProvider.GetValidator();
            var result = validator.Validate(context);
            if (!result.IsValid)
            {
                return CreateErrorResponse(StatusCodes.Status401Unauthorized, result.Code, result.ErrorMessage, request.Id);
            }

            // 執行相應的 API 方法
            return HandleRequest(result.AccessToken, request);
        }

        /// <summary>
        /// 處理 JSON-RPC 請求，並執行相應的 API 方法。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="request">JSON-RPC 請求模型。</param>
        private IActionResult HandleRequest(Guid accessToken, TJsonRpcRequest request)
        {
            try
            {
                var executor = new TJsonRpcExecutor(accessToken);
                var result = executor.Execute(request);
                return new ContentResult
                {
                    Content = result.ToJson(),
                    ContentType = "application/json",
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(StatusCodes.Status500InternalServerError, EJsonRpcErrorCode.InternalError,
                    "Internal server error", request.Id, ex.InnerException?.Message ?? ex.Message);
            }
        }

        /// <summary>
        /// 建立統一格式的 JSON-RPC 錯誤回應。
        /// </summary>
        private IActionResult CreateErrorResponse(int httpStatusCode, EJsonRpcErrorCode code, string message, string? id = null, string? data = null)
        {
            var response = new TJsonRpcResponse
            {
                Id = id,
                Error = new TJsonRpcError
                {
                    Code = (int)code,
                    Message = message,
                    Data = data
                }
            };
            return StatusCode(httpStatusCode, response);
        }

    }
}
