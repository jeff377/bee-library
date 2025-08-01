﻿using System.Net.Http.Headers;
using Bee.Api.Core;
using Bee.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bee.Api.AspNetCore
{
    /// <summary>
    /// 提供 JSON-RPC API 處理的控制器基底類別，適用於 ASP.NET Core。
    /// </summary>
    [ApiController]
    [Route("api")]
    [Produces("application/json")]
    public abstract class ApiServiceController : ControllerBase
    {
        /// <summary>
        /// 判斷當前環境是否為開發環境。
        /// </summary>
        protected bool IsDevelopment =>
            HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment();

        /// <summary>
        /// 處理 HTTP POST 請求，並執行相應的 API 服務。
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> PostAsync()
        {
            // 讀取並解析 JSON-RPC 請求
            JsonRpcRequest request;
            try
            {
                request = await ReadRequestAsync();
            }
            catch (JsonRpcException ex)
            {
                return CreateErrorResponse(ex.HttpStatusCode, ex.ErrorCode, ex.RpcMessage);
            }

            // 驗證 API 金鑰與授權
            var result = ValidateAuthorization(request);
            if (!result.IsValid)
            {
                return CreateErrorResponse(StatusCodes.Status401Unauthorized, result.Code, result.ErrorMessage, request.Id);
            }

            // 執行相應的 API 方法
            return await HandleRequestAsync(result.AccessToken, request);
        }

        /// <summary>
        /// 讀取並解析 JSON-RPC 請求。
        /// </summary>
        /// <returns>成功解析的 <see cref="JsonRpcRequest"/> 實例。</returns>
        /// <exception cref="JsonRpcException">當內容為空或格式錯誤時拋出。</exception>
        protected virtual async Task<JsonRpcRequest> ReadRequestAsync()
        {
            if (!MediaTypeHeaderValue.TryParse(HttpContext.Request.ContentType, out var mediaType) ||
                mediaType?.MediaType == null || // Ensure mediaType and MediaType are not null
                !mediaType.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
            {
                throw new JsonRpcException(StatusCodes.Status415UnsupportedMediaType,
                    JsonRpcErrorCode.InvalidRequest, "Unsupported media type");
            }

            Request.EnableBuffering();
            Request.Body.Position = 0;

            using var reader = new StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new JsonRpcException(StatusCodes.Status400BadRequest,
                    JsonRpcErrorCode.InvalidRequest, "Empty request body");
            }

            try
            {
                var request = SerializeFunc.JsonToObject<JsonRpcRequest>(json);
                if (request == null || string.IsNullOrWhiteSpace(request.Method))
                {
                    throw new JsonRpcException(StatusCodes.Status400BadRequest,
                        JsonRpcErrorCode.InvalidRequest, "Missing method");
                }

                return request;
            }
            catch (JsonRpcException) { throw; }
            catch (Exception ex)
            {
                throw new JsonRpcException(StatusCodes.Status400BadRequest,
                    JsonRpcErrorCode.ParseError, $"Invalid JSON format: {ex.Message}");
            }
        }

        /// <summary>
        /// 驗證 API 授權資訊。
        /// </summary>
        /// <param name="request">JSON-RPC 請求。</param>
        /// <returns>驗證結果。</returns>
        protected virtual ApiAuthorizationResult ValidateAuthorization(JsonRpcRequest request)
        {
            var apiKey = HttpContext.Request.Headers[ApiHeaders.ApiKey].ToString();
            var authorization = HttpContext.Request.Headers[ApiHeaders.Authorization].ToString();

            var context = new ApiAuthorizationContext
            {
                ApiKey = apiKey,
                Authorization = authorization,
                Method = request.Method
            };

            var validator = ApiServiceOptions.AuthorizationValidator;
            return validator.Validate(context);
        }

        /// <summary>
        /// 處理 JSON-RPC 請求，並執行相應的 API 方法。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="request">JSON-RPC 請求模型。</param>
        protected virtual async Task<IActionResult> HandleRequestAsync(Guid accessToken, JsonRpcRequest request)
        {
            try
            {
                var executor = new JsonRpcExecutor(accessToken);
                var result = await executor.ExecuteAsync(request);
                return new ContentResult
                {
                    Content = result.ToJson(),
                    ContentType = "application/json",
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                var message = IsDevelopment
                    ? ex.InnerException?.Message ?? ex.Message
                    : string.Empty;

                return CreateErrorResponse(StatusCodes.Status500InternalServerError, JsonRpcErrorCode.InternalError,
                    "Internal server error", request.Id, message);
            }
        }

        /// <summary>
        /// 建立 JSON-RPC 格式的錯誤回應物件。
        /// </summary>
        /// <param name="httpStatusCode">HTTP 狀態碼。</param>
        /// <param name="code">JSON-RPC 錯誤代碼。</param>
        /// <param name="message">錯誤訊息。</param>
        /// <param name="id">對應的請求 ID，可為 null。</param>
        /// <param name="data">額外錯誤資料，可為 null。</param>
        /// <returns>回傳帶有錯誤資訊的 IActionResult。</returns>
        protected virtual IActionResult CreateErrorResponse(int httpStatusCode, JsonRpcErrorCode code, string message, string? id = null, string? data = null)
        {
            var response = new JsonRpcResponse
            {
                Id = id,
                Error = new JsonRpcError
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
