using System;

namespace Bee.Define
{
    /// <summary>
    /// API 存取控管屬性。
    /// 用於定義方法是否限制為近端呼叫，或是否要求資料傳輸須經過編碼（壓縮/加密）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class ApiAccessControlAttribute : Attribute
    {
        /// <summary>
        /// 是否僅允許近端呼叫（例如工具程式）。
        /// 預設為 false。
        /// </summary>
        public bool LocalOnly { get; set; } = false;

        /// <summary>
        /// 是否要求資料傳輸需經過編碼（序列化/壓縮/加密），等同限制僅供內部系統存取。
        /// 預設為 false。
        /// </summary>
        public bool RequireEncoding { get; set; } = false;

        /// <summary>
        /// 建構函式。
        /// </summary>
        public ApiAccessControlAttribute() { }

        /// <summary>
        /// 建構函式，可同時指定是否為近端限定與是否需編碼。
        /// </summary>
        /// <param name="localOnly">是否僅限近端呼叫</param>
        /// <param name="requireEncoding">是否需編碼傳輸</param>
        public ApiAccessControlAttribute(bool localOnly, bool requireEncoding)
        {
            LocalOnly = localOnly;
            RequireEncoding = requireEncoding;
        }
    }
}
