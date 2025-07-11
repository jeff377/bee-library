using System;
using System.Diagnostics;
using System.Reflection;

namespace Bee.UI.Core
{
    /// <summary>
    /// 提供目前執行中的應用程式或主程式的版本與產品資訊。
    /// </summary>
    public static class VersionInfo
    {
        /// <summary>
        /// 取得應用程式的主要組件（入口點），若為 null 則回退至目前執行組件。
        /// </summary>
        private static Assembly EntryAssembly => Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        /// <summary>
        /// 取得產品名稱，對應於 .csproj 的 &lt;Product&gt; 屬性。
        /// </summary>
        public static string Product => GetAttribute<AssemblyProductAttribute>()?.Product ?? "Unknown";

        /// <summary>
        /// 取得公司名稱，對應於 .csproj 的 &lt;Company&gt; 屬性。
        /// </summary>
        public static string Company => GetAttribute<AssemblyCompanyAttribute>()?.Company ?? "Unknown";

        /// <summary>
        /// 取得應用程式描述資訊，對應於 .csproj 的 &lt;Description&gt; 屬性。
        /// </summary>
        public static string Description => GetAttribute<AssemblyDescriptionAttribute>()?.Description ?? "";

        /// <summary>
        /// 取得純淨版本號（移除 Git hash），對應於 .csproj 的 &lt;Version&gt; 屬性。
        /// </summary>
        public static string Version => InformationalVersion?.Split('+')[0] ?? "Unknown";

        /// <summary>
        /// 取得檔案版本，對應於 .csproj 的 &lt;FileVersion&gt; 屬性。
        /// </summary>
        public static string FileVersion => FileVerInfo.FileVersion;

        /// <summary>
        /// 取得組件版本，對應於 .csproj 的 &lt;AssemblyVersion&gt; 或預設為 Version + .0。
        /// </summary>
        public static string AssemblyVersion => EntryAssembly.GetName().Version?.ToString() ?? "Unknown";

        /// <summary>
        /// 取得完整資訊版本（可能包含 Git hash），對應於 AssemblyInformationalVersionAttribute。
        /// </summary>
        public static string FullInformationalVersion => InformationalVersion ?? "Unknown";

        /// <summary>
        /// 取得 AssemblyInformationalVersionAttribute 的原始值。
        /// </summary>
        private static string InformationalVersion =>
            GetAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        /// <summary>
        /// 取得目前組件的 FileVersionInfo。
        /// </summary>
        private static FileVersionInfo FileVerInfo =>
            FileVersionInfo.GetVersionInfo(EntryAssembly.Location);

        /// <summary>
        /// 泛型方法，用來取得指定的組件屬性類型。
        /// </summary>
        /// <typeparam name="T">要取得的屬性類型</typeparam>
        /// <returns>指定屬性的實例，若無則為 null</returns>
        private static T GetAttribute<T>() where T : Attribute =>
            EntryAssembly.GetCustomAttribute<T>();
    }
}
