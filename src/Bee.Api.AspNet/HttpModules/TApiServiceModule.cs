using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using Bee.Api.Core;
using Bee.Base;

namespace Bee.Api.AspNet
{
    /// <summary>
    /// 提供 JSON-RPC API 處理的 HTTP 模組，適用於 ASP.NET。
    /// </summary>
    public class TApiServiceModule : IHttpModule
    {
        /// <summary>
        /// 初始化模組，註冊請求攔截事件。
        /// </summary>
        public void Init(HttpApplication context)
        {
            context.BeginRequest += OnBeginRequest;
        }

        private void OnBeginRequest(object sender, EventArgs e)
        {
            var app = (HttpApplication)sender;
            var context = app.Context;

            if (!context.Request.Path.Equals("/api", StringComparison.OrdinalIgnoreCase))
                return;

            if (context.Request.HttpMethod != "POST" ||
                !context.Request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 400;
                context.Response.StatusDescription = "Invalid HTTP method or content type.";
                context.Response.End();
                return;
            }

            string apiKey = context.Request.Headers["X-Api-Key"] ?? string.Empty;
            string authorization = context.Request.Headers["Authorization"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                WriteErrorResponse(context, 401, -32600, "Missing or invalid X-Api-Key header");
                return;
            }

            string json;
            using (var reader = new StreamReader(context.Request.InputStream))
                json = reader.ReadToEnd();

            TJsonRpcRequest request = null;
            try
            {
                request = SerializeFunc.JsonToObject<TJsonRpcRequest>(json);
            }
            catch (Exception ex)
            {
                WriteErrorResponse(context, 400, -32700, $"Failed to deserialize request body: {ex.Message}");
                return;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Method))
            {
                WriteErrorResponse(context, 400, -32600, "Invalid request body or missing method");
                return;
            }

            var accessToken = Guid.Empty;
            if (IsAuthorizationRequired(request.Method))
            {
                accessToken = TryGetAccessToken(authorization);
                if (accessToken == Guid.Empty)
                {
                    WriteErrorResponse(context, 401, -32600, "Missing or invalid Authorization header", request.Id);
                    return;
                }
            }

            try
            {
                var executor = new TJsonRpcExecutor(accessToken);
                var result = executor.Execute(request);

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = 200;
                context.Response.Write(result.ToJson());
                context.ApplicationInstance.CompleteRequest();  // 取代 Response.End 方法，不會丟出例外錯誤
            }
            catch (Exception ex)
            {
                WriteErrorResponse(context, 500, -32000, "Internal server error", request.Id, ex.InnerException?.Message ?? ex.Message);
            }
        }

        private Guid TryGetAccessToken(string authorization)
        {
            if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer "))
                return Guid.Empty;

            var token = authorization.Substring("Bearer ".Length).Trim();
            return Guid.TryParse(token, out var guid) ? guid : Guid.Empty;
        }

        private bool IsAuthorizationRequired(string method)
        {
            var noAuthMethods = new HashSet<string> { "System.Login", "System.Ping" };
            return !noAuthMethods.Contains(method);
        }

        private void WriteErrorResponse(HttpContext context, int httpStatusCode, int code, string message, string id = null, string data = null)
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

            context.Response.StatusCode = httpStatusCode;
            context.Response.ContentType = "application/json";
            context.Response.Write(response.ToJson());
            context.Response.End();
        }

        /// <summary>
        /// 模組資源釋放（不需實作）。
        /// </summary>
        public void Dispose() { }
    }
}
