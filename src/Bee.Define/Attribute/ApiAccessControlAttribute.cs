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
        /// <param name="level">API 存取保護等級。</param>
        public ApiAccessControlAttribute(ApiProtectionLevel level)
        {
            ProtectionLevel = level;
        }

        /// <summary>
        /// 存取保護等級
        /// </summary>
        public ApiProtectionLevel ProtectionLevel { get; }
    }

}
