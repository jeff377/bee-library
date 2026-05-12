using Bee.Definition.Storage;
using Bee.ObjectCaching.Database;
using Bee.ObjectCaching.Define;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Transitional static facade over <see cref="ICacheContainer"/>. Phase 5 PR 5.3c
    /// moved the canonical implementation to <see cref="CacheContainerService"/>; this
    /// shim delegates to a process-wide instance installed via
    /// <see cref="Initialize(ICacheContainer)"/> (the framework bootstrapper wires the DI
    /// singleton at startup). Removed in PR 5.4 once test fixtures migrate to
    /// ctor-injected <see cref="ICacheContainer"/>.
    /// </summary>
    public static class CacheContainer
    {
        private static ICacheContainer? _instance;

        /// <summary>
        /// Installs the process-wide <see cref="ICacheContainer"/> instance.
        /// </summary>
        /// <param name="instance">The instance to install.</param>
        public static void Initialize(ICacheContainer instance)
        {
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        /// <summary>
        /// Installs a process-wide instance backed by a fresh <see cref="CacheContainerService"/>
        /// constructed from the supplied storage. Convenience overload that preserves the
        /// pre-PR-5.3c API shape used by tests.
        /// </summary>
        /// <param name="storage">The define storage shared by storage-backed caches.</param>
        public static void Initialize(IDefineStorage storage)
        {
            Initialize(new CacheContainerService(storage));
        }

        private static ICacheContainer Instance
            => _instance ?? throw new InvalidOperationException(
                "CacheContainer accessed before CacheContainer.Initialize was called.");

        /// <summary>Gets the system settings cache.</summary>
        public static SystemSettingsCache SystemSettings => Instance.SystemSettings;

        /// <summary>Gets the database settings cache.</summary>
        public static DatabaseSettingsCache DatabaseSettings => Instance.DatabaseSettings;

        /// <summary>Gets the program settings cache.</summary>
        public static ProgramSettingsCache ProgramSettings => Instance.ProgramSettings;

        /// <summary>Gets the database category settings cache.</summary>
        public static DbCategorySettingsCache DbCategorySettings => Instance.DbCategorySettings;

        /// <summary>Gets the table schema cache, keyed by category id and table name.</summary>
        public static TableSchemaCache TableSchema => Instance.TableSchema;

        /// <summary>Gets the form schema cache, keyed by program identifier.</summary>
        public static FormSchemaCache FormSchema => Instance.FormSchema;

        /// <summary>Gets the form layout cache, keyed by layout identifier.</summary>
        public static FormLayoutCache FormLayout => Instance.FormLayout;

        /// <summary>Gets the session information cache, keyed by access token.</summary>
        public static SessionInfoCache SessionInfo => Instance.SessionInfo;
    }
}
