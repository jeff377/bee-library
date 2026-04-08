using Bee.Core;
using Bee.ObjectCaching.Providers;
using Bee.Definition;
using Bee.Definition.Settings;
using System;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Provides a static interface for accessing the cache provider.
    /// </summary>
    /// <remarks>
    /// This class is responsible for initializing and managing the cache provider instance,
    /// and exposes a static <c>Provider</c> property for accessing it.
    /// The cache provider is determined based on the backend configuration; if none is specified,
    /// the default <c>MemoryCacheProvider</c> is used.
    /// </remarks>
    public static class CacheInfo
    {
        /// <summary>
        /// Static constructor that initializes the cache provider.
        /// </summary>
        static CacheInfo()
        {
            if (SysInfo.IsSingleFile) { return; }
            if (BackendInfo.DefineAccess == null)
                throw new InvalidOperationException("BackendInfo.DefineAccess cannot be null. Please ensure the backend configuration is properly initialized.");

            var settings = BackendInfo.DefineAccess.GetSystemSettings();
            Initialize(settings.BackendConfiguration);
        }

        /// <summary>
        /// Gets or sets the cache provider instance.
        /// </summary>
        /// <value>
        /// Defaults to <c>MemoryCacheProvider</c>, but can be overridden based on the backend configuration.
        /// </value>
        public static ICacheProvider Provider { get; set; } = new MemoryCacheProvider();

        /// <summary>
        /// Initializes the cache provider based on the backend configuration.
        /// </summary>
        /// <param name="configuration">The backend configuration.</param>
        private static void Initialize(BackendConfiguration configuration)
        {
            var components = configuration.Components;
            // Create the cache provider from configuration or fall back to the default
            Provider = CreateOrDefault<ICacheProvider>
                (components.CacheProvider, BackendDefaultTypes.CacheProvider);
        }

        /// <summary>
        /// Creates an instance of the specified type, using <paramref name="fallback"/> if <paramref name="configured"/> is empty.
        /// </summary>
        /// <typeparam name="T">The type of instance to create.</typeparam>
        /// <param name="configured">The type name specified in the configuration.</param>
        /// <param name="fallback">The default type name to use when no type is configured.</param>
        /// <returns>The created instance, or null if the type cannot be instantiated.</returns>
        private static T CreateOrDefault<T>(string configured, string fallback) where T : class
        {
            var typeName = string.IsNullOrWhiteSpace(configured) ? fallback : configured;
            return BaseFunc.CreateInstance(typeName) as T;
        }
    }
}
