using System;
using System.IO;
using System.Web;
using Bee.Api.Core;
using Bee.Base;

namespace Bee.Api.AspNet
{
    /// <summary>
    /// 提供 JSON-RPC API 處理的 HTTP 模組，適用於 ASP.NET。
    /// </summary>
    public class ApiServiceModule : IHttpModule
    {
        /// <summary>
        /// 初始化模組，註冊請求攔截事件
        /// </summary>
        public void Init(HttpApplication context)
        {
            context.BeginRequest += OnBeginRequest;
        }

        /// <summary>
        /// 處理 HTTP 請求的開始事件。
        /// </summary>
        private void OnBeginRequest(object sender, EventArgs e)
        {
            var app = (HttpApplication)sender;
            var context = app.Context;

            // 判斷是否為 API 路徑
            string executionPath = app.Request.AppRelativeCurrentExecutionFilePath.TrimEnd('/');
            if (!StrFunc.IsEquals(executionPath, "~/api")) { return; }

            // 讀取並解析 JSON-RPC 請求
            JsonRpcRequest request;
            try
            {
                request = ReadRequest(context);
            }
            catch (JsonRpcException ex)
            {
                WriteErrorResponse(context, ex.HttpStatusCode, (int)ex.ErrorCode, ex.RpcMessage);
                return;
            }

            // 驗證 API 金鑰與授權
            var result = ValidateAuthorization(context, request);
            if (!result.IsValid)
            {
                WriteErrorResponse(context, 401, (int)result.Code, result.ErrorMessage, request.Id);
                return;
            }

            // 執行相應的 API 方法
            HandleRequest(context, request, result);
        }

        /// <summary>
        /// 讀取並解析 JSON-RPC 請求。
        /// </summary>
        /// <exception cref="JsonRpcException">當內容為空或格式錯誤時拋出。</exception>
        private JsonRpcRequest ReadRequest(HttpContext context)
        {
            if (context.Request.HttpMethod != "POST" ||
                !context.Request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
            {
                throw new JsonRpcException(415, JsonRpcErrorCode.InvalidRequest, "Unsupported media type");
            }

            // 將 InputStream 指回開頭位置，避免被其他模組先讀取過而導致讀取失敗
            context.Request.InputStream.Seek(0, SeekOrigin.Begin);

            string json;
            using (var reader = new StreamReader(context.Request.InputStream))
                json = reader.ReadToEnd();

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new JsonRpcException(400, JsonRpcErrorCode.InvalidRequest, "Empty request body");
            }

            try
            {
                var request = SerializeFunc.JsonToObject<JsonRpcRequest>(json);
                if (request == null || string.IsNullOrWhiteSpace(request.Method))
                {
                    throw new JsonRpcException(400, JsonRpcErrorCode.InvalidRequest, "Missing method");
                }
                return request;
            }
            catch (JsonRpcException) { throw; }
            catch (Exception ex)
            {
                throw new JsonRpcException(400, JsonRpcErrorCode.ParseError, $"Invalid JSON format: {ex.Message}");
            }
        }

        /// <summary>
        /// 驗證 API 授權資訊。
        /// </summary>
        /// <param name="context">HTTP 請求的上下文物件。</param>
        /// <param name="request">JSON-RPC 請求。</param>
        /// <returns>驗證結果。</returns>
        private ApiAuthorizationResult ValidateAuthorization(HttpContext context, JsonRpcRequest request)
        {
            var apiKey = context.Request.Headers[ApiHeaders.ApiKey] ?? string.Empty;
            var authorization = context.Request.Headers[ApiHeaders.Authorization] ?? string.Empty;

            var authContext = new ApiAuthorizationContext
            {
                ApiKey = apiKey,
                Authorization = authorization,
                Method = request.Method
            };

            var validator = ApiServiceOptions.AuthorizationValidator;
            return validator.Validate(authContext);
        }

        /// <summary>
        /// 處理 JSON-RPC 請求，並執行相應的 API 方法。
        /// </summary>
        /// <param name="context">HTTP 請求的上下文物件。</param>
        /// <param name="request">JSON-RPC 請求物件。</param>
        /// <param name="authResult">授權驗證的結果物件。</param>
        private void HandleRequest(HttpContext context, JsonRpcRequest request, ApiAuthorizationResult authResult)
        {
            try
            {
                var executor = new JsonRpcExecutor(authResult.AccessToken);
                var response = executor.Execute(request);

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = 200;
                context.Response.Write(response.ToJson());
                context.ApplicationInstance.CompleteRequest();  // 取代 Response.End 方法，不會丟出例外錯誤
            }
            catch (Exception ex)
            {
                WriteErrorResponse(context, 500, (int)JsonRpcErrorCode.InternalError,
                    "Internal server error", request.Id, ex.InnerException?.Message ?? ex.Message);
            }
        }


        /// <summary>
        /// 建立 JSON-RPC 格式的錯誤回應，並寫入 HTTP 回應。
        /// </summary>
        /// <param name="context">HTTP 請求的上下文物件。</param>
        /// <param name="httpStatusCode">HTTP 狀態碼，例如 400、401、500。</param>
        /// <param name="code">JSON-RPC 錯誤代碼，使用 <see cref="JsonRpcErrorCode"/> 列舉。</param>
        /// <param name="message">錯誤訊息內容。</param>
        /// <param name="id">對應的 JSON-RPC 請求識別碼，可為 null。</param>
        /// <param name="data">額外的錯誤資料，例如例外訊息，可為 null。</param>
        private void WriteErrorResponse(HttpContext context, int httpStatusCode, int code, string message, string id = null, string data = null)
        {
            var response = new JsonRpcResponse
            {
                Id = id,
                Error = new JsonRpcError
                {
                    Code = code,
                    Message = message,
                    Data = data
                }
            };

            context.Response.StatusCode = httpStatusCode;
            context.Response.ContentType = "application/json";
            context.Response.Write(response.ToJson());
            context.ApplicationInstance.CompleteRequest();  // 取代 Response.End 方法，不會丟出例外錯誤
        }

        /// <summary>
        /// 釋放資源。
        /// </summary>
        public void Dispose() { }
    }
}
