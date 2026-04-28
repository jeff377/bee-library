using Bee.Definition.Settings;
using Bee.Base;
using Bee.Definition;
using Bee.Repository.Abstractions.Providers;

namespace Bee.Repository.Abstractions
{
    /// <summary>
    /// Provides static access to the system repository and form repository providers.
    /// </summary>
    public static class RepositoryInfo
    {
        /// <summary>
        /// Initializes static members of the <see cref="RepositoryInfo"/> class.
        /// </summary>
        static RepositoryInfo()
        {
            if (SysInfo.IsSingleFile) { return; }
            if (BackendInfo.DefineAccess == null) { return; }

            var settings = BackendInfo.DefineAccess.GetSystemSettings();
            Initialize(settings.BackendConfiguration);
        }

        /// <summary>
        /// Gets or sets the system repository provider.
        /// </summary>
        public static ISystemRepositoryProvider? SystemProvider { get; set; }

        /// <summary>
        /// Gets or sets the form repository provider.
        /// </summary>
        public static IFormRepositoryProvider? FormProvider { get; set; }

        /// <summary>
        /// Initializes the repository providers from the given backend configuration.
        /// </summary>
        private static void Initialize(BackendConfiguration configuration)
        {
            var components = configuration.Components;
            // Set the system repository provider
            SystemProvider = CreateOrDefault<ISystemRepositoryProvider>
                (components.SystemRepositoryProvider, BackendDefaultTypes.SystemRepositoryProvider);
            // Set the form repository provider
            FormProvider = CreateOrDefault<IFormRepositoryProvider>
                (components.FormRepositoryProvider, BackendDefaultTypes.FormRepositoryProvider);
        }

        /// <summary>
        /// Creates an instance of the specified type, falling back to <paramref name="fallback"/> when <paramref name="configured"/> is empty.
        /// </summary>
        /// <param name="configured">The type name specified in configuration.</param>
        /// <param name="fallback">The default type name.</param>
        private static T? CreateOrDefault<T>(string configured, string fallback) where T : class
        {
            var typeName = string.IsNullOrWhiteSpace(configured) ? fallback : configured;
            return BaseFunc.CreateInstance(typeName) as T;
        }
    }
}
