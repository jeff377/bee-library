using System;
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
        /// 驗證指定方法是否符合存取條件（近端、編碼），若不符合則擲出例外。
        /// 若方法未標記 <see cref="ApiAccessControlAttribute"/> 則視為不限制。
        /// </summary>
        /// <param name="method">要驗證的 API 方法</param>
        /// <param name="context">目前的 API 呼叫上下文</param>
        public static void ValidateAccess(MethodInfo method, TApiCallContext context)
        {
            var attr = FindAccessAttribute(method);
            if (attr == null)
                return;

            if (attr.LocalOnly && !context.IsLocalCall)
                throw new UnauthorizedAccessException("This method is only allowed for local calls.");

            if (attr.RequireEncoding && !context.IsEncoded)
                throw new UnauthorizedAccessException("This method is only allowed for internal system calls (encryption/compression encoding required).");
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
