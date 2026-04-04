using System;

namespace Bee.Define
{
    /// <summary>
    /// 標註 API 方法的存取控制屬性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class ApiAccessControlAttribute : Attribute
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="protectionLevel">存取保護等級。</param>
        /// <param name="accessRequirement">是否需要登入。</param>
        public ApiAccessControlAttribute(
            ApiProtectionLevel protectionLevel,
            ApiAccessRequirement accessRequirement = ApiAccessRequirement.Authenticated)
        {
            ProtectionLevel = protectionLevel;
            AccessRequirement = accessRequirement;
        }

        /// <summary>
        /// 存取保護等級（編碼與加密需求）。
        /// </summary>
        public ApiProtectionLevel ProtectionLevel { get; }

        /// <summary>
        /// 存取授權需求（是否需要登入）。
        /// </summary>
        public ApiAccessRequirement AccessRequirement { get; }
    }

}
