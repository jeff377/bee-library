using Bee.Definition.Settings;
using Bee.Base;
using Bee.Definition;
using Bee.Repository.Abstractions.Factories;

namespace Bee.Repository.Abstractions
{
    /// <summary>
    /// Provides static access to the system and form repository factories.
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
        /// Gets or sets the system repository factory.
        /// </summary>
        public static ISystemRepositoryFactory? SystemFactory { get; set; }

        /// <summary>
        /// Gets or sets the form repository factory.
        /// </summary>
        public static IFormRepositoryFactory? FormFactory { get; set; }

        /// <summary>
        /// Initializes the repository factories from the given backend configuration.
        /// </summary>
        private static void Initialize(BackendConfiguration configuration)
        {
            var components = configuration.Components;
            // Set the system repository factory
            SystemFactory = CreateOrDefault<ISystemRepositoryFactory>
                (components.SystemRepositoryFactory, BackendDefaultTypes.SystemRepositoryFactory);
            // Set the form repository factory
            FormFactory = CreateOrDefault<IFormRepositoryFactory>
                (components.FormRepositoryFactory, BackendDefaultTypes.FormRepositoryFactory);
        }

        /// <summary>
        /// Creates an instance of the specified type, falling back to <paramref name="fallback"/> when <paramref name="configured"/> is empty.
        /// </summary>
        /// <param name="configured">The type name specified in configuration.</param>
        /// <param name="fallback">The default type name.</param>
        private static T? CreateOrDefault<T>(string configured, string fallback) where T : class
        {
            var typeName = string.IsNullOrWhiteSpace(configured) ? fallback : configured;
            return AssemblyLoader.CreateInstance(typeName) as T;
        }
    }
}
