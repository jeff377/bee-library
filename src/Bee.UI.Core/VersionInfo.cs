using System.Diagnostics;
using System.Reflection;

namespace Bee.UI.Core
{
    /// <summary>
    /// Provides version and product metadata of the currently running application or host assembly.
    /// </summary>
    public static class VersionInfo
    {
        private const string UnknownValue = "Unknown";

        private static Assembly EntryAssembly => Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        /// <summary>
        /// Product name, mapped to the <c>&lt;Product&gt;</c> property in the .csproj.
        /// </summary>
        public static string Product => GetAttribute<AssemblyProductAttribute>()?.Product ?? UnknownValue;

        /// <summary>
        /// Company name, mapped to the <c>&lt;Company&gt;</c> property in the .csproj.
        /// </summary>
        public static string Company => GetAttribute<AssemblyCompanyAttribute>()?.Company ?? UnknownValue;

        /// <summary>
        /// Application description, mapped to the <c>&lt;Description&gt;</c> property in the .csproj.
        /// </summary>
        public static string Description => GetAttribute<AssemblyDescriptionAttribute>()?.Description ?? "";

        /// <summary>
        /// Clean version number (Git hash stripped), mapped to the <c>&lt;Version&gt;</c> property in the .csproj.
        /// </summary>
        public static string Version => InformationalVersion?.Split('+')[0] ?? UnknownValue;

        /// <summary>
        /// File version, mapped to the <c>&lt;FileVersion&gt;</c> property in the .csproj.
        /// </summary>
        public static string FileVersion => FileVerInfo.FileVersion ?? UnknownValue;

        /// <summary>
        /// Assembly version, mapped to the <c>&lt;AssemblyVersion&gt;</c> property; defaults to <see cref="Version"/> + ".0" when absent.
        /// </summary>
        public static string AssemblyVersion => EntryAssembly.GetName().Version?.ToString() ?? UnknownValue;

        /// <summary>
        /// Full informational version (may include Git hash), mapped to <see cref="AssemblyInformationalVersionAttribute"/>.
        /// </summary>
        public static string FullInformationalVersion => InformationalVersion ?? UnknownValue;

        private static string? InformationalVersion =>
            GetAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        private static FileVersionInfo FileVerInfo =>
            FileVersionInfo.GetVersionInfo(EntryAssembly.Location);

        private static T? GetAttribute<T>() where T : Attribute =>
            EntryAssembly.GetCustomAttribute<T>();
    }
}
