﻿using System;
using System.Collections.Generic;

namespace Bee.Api.Core
{
    /// <summary>
    /// 提供預設的 API 金鑰與授權驗證邏輯。
    /// </summary>
    public class ApiAuthorizationValidator : IApiAuthorizationValidator
    {
        /// <summary>
        /// 不需授權的方法清單（大小寫敏感）。
        /// </summary>
        private static readonly HashSet<string> NoAuthMethods = new HashSet<string>
        {
            "System.Ping",
            "System.GetApiPayloadOptions",
            "System.Login"
        };

        /// <summary>
        /// 判斷指定的 JSON-RPC 方法是否需要授權。
        /// </summary>
        /// <param name="method">JSON-RPC 方法名稱（大小寫敏感）。</param>
        /// <returns>需要授權則回傳 true，否則 false。</returns>
        protected virtual bool IsAuthorizationRequired(string method)
        {
            return !NoAuthMethods.Contains(method);
        }

        /// <summary>
        /// 驗證 API 金鑰與授權資訊。
        /// </summary>
        /// <param name="context">API 授權驗證上下文。</param>
        /// <returns>授權驗證結果。</returns>
        public ApiAuthorizationResult Validate(ApiAuthorizationContext context)
        {
            // 驗證輸入參數是否為 null
            if (context == null)
            {
                return ApiAuthorizationResult.Fail(JsonRpcErrorCode.InvalidRequest, "Invalid authorization context.");
            }

            // 驗證是否有 API 金鑰
            if (string.IsNullOrWhiteSpace(context.ApiKey))
            {
                return ApiAuthorizationResult.Fail(JsonRpcErrorCode.InvalidRequest, "Missing or invalid API key.");
            }

            // 若為免授權的方法，直接回傳成功且不附帶 access token
            if (!IsAuthorizationRequired(context.Method))
            {
                return ApiAuthorizationResult.Success(Guid.Empty);
            }

            // 需授權的方法，檢查 Authorization 標頭
            if (string.IsNullOrWhiteSpace(context.Authorization))
            {
                return ApiAuthorizationResult.Fail(JsonRpcErrorCode.InvalidRequest, "Missing Authorization header.");
            }

            // 確認 Authorization 格式為 Bearer Token
            if (!context.Authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return ApiAuthorizationResult.Fail(JsonRpcErrorCode.InvalidRequest, "Invalid Authorization format. Expected 'Bearer <token>'.");
            }

            // 解析 Bearer Token，並驗證為有效的 Guid
            var tokenPart = context.Authorization.Substring("Bearer ".Length).Trim();
            if (!Guid.TryParse(tokenPart, out var accessToken))
            {
                return ApiAuthorizationResult.Fail(JsonRpcErrorCode.InvalidRequest, "Invalid access token.");
            }

            // 此處可擴充驗證邏輯，例如存取資料庫確認 access token 是否有效
            return ApiAuthorizationResult.Success(accessToken);
        }
    }
}
