using Bee.Base;
using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Repository.Abstractions.Factories;

namespace Bee.Repository.Abstractions
{
    /// <summary>
    /// Provides static access to the system and form repository factories.
    /// </summary>
    /// <remarks>
    /// Installed via <see cref="Initialize(BackendConfiguration)"/> at host startup;
    /// <c>BackendInfo.Initialize</c> invokes this via reflection (avoids reverse
    /// dependency from <c>Bee.Definition</c> into <c>Bee.Repository.Abstractions</c>).
    /// </remarks>
    public static class RepositoryInfo
    {
        /// <summary>
        /// Gets or sets the system repository factory.
        /// </summary>
        public static ISystemRepositoryFactory? SystemFactory { get; set; }

        /// <summary>
        /// Gets or sets the form repository factory.
        /// </summary>
        public static IFormRepositoryFactory? FormFactory { get; set; }

        /// <summary>
        /// Installs the repository factories from the given backend configuration.
        /// Must be called once at host startup; typically invoked by
        /// <c>BackendInfo.Initialize</c>.
        /// </summary>
        /// <param name="configuration">The backend configuration.</param>
        public static void Initialize(BackendConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            var components = configuration.Components;
            SystemFactory = CreateOrDefault<ISystemRepositoryFactory>
                (components.SystemRepositoryFactory, BackendDefaultTypes.SystemRepositoryFactory);
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
