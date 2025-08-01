﻿using System;
using System.Reflection;
using Bee.Define;

namespace Bee.Api.Core
{
    /// <summary>
    /// 提供 API 方法的存取驗證邏輯，支援繼承基底方法的存取條件。
    /// </summary>
    public static class ApiAccessValidator
    {
        /// <summary>
        /// 驗證指定方法是否符合存取條件（近端、編碼、加密），若不符合則擲出例外。
        /// 若方法未標記 <see cref="ApiAccessControlAttribute"/> 則視為不限制。
        /// </summary>
        /// <param name="method">要驗證的 API 方法</param>
        /// <param name="context">目前的 API 呼叫上下文</param>
        public static void ValidateAccess(MethodInfo method, ApiCallContext context)
        {
            var attr = FindAccessAttribute(method);
            if (attr == null)
                return;

            // 近端呼叫允許所有保護等級
            if (context.IsLocalCall)
                return;

            // 依照呼叫端的 Format 判斷是否符合存取等級
            switch (context.Format)
            {
                case PayloadFormat.Encrypted:
                    // 可呼叫任何非 LocalOnly API
                    return;

                case PayloadFormat.Encoded:
                    if (attr.ProtectionLevel > ApiProtectionLevel.Encoded)
                        throw new UnauthorizedAccessException("This API requires encrypted transmission.");
                    return;

                case PayloadFormat.Plain:
                default:
                    if (attr.ProtectionLevel > ApiProtectionLevel.Public)
                        throw new UnauthorizedAccessException("This API requires encoded or encrypted transmission.");
                    return;
            }
        }

        /// <summary>
        /// 嘗試從方法或其基底定義中取得 <see cref="ApiAccessControlAttribute"/>。
        /// </summary>
        /// <param name="method">目標方法</param>
        /// <returns>取得的屬性，若無則為 null</returns>
        private static ApiAccessControlAttribute FindAccessAttribute(MethodInfo method)
        {
            var attr = method.GetCustomAttribute<ApiAccessControlAttribute>();
            if (attr != null)
                return attr;

            var baseMethod = method.GetBaseDefinition();
            return baseMethod != method
                ? baseMethod.GetCustomAttribute<ApiAccessControlAttribute>()
                : null;
        }
    }

}
