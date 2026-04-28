using System.Data.Common;
using Bee.Definition.Database;

namespace Bee.Db.Manager
{
    /// <summary>
    /// Registry of <see cref="DbProviderFactory"/> instances keyed by <see cref="DatabaseType"/>.
    /// Mirrors the role of <see cref="DbDialectRegistry"/> (which stores the framework's own
    /// <see cref="Bee.Db.Providers.IDialectFactory"/>) for ADO.NET provider factories.
    /// Registration is explicit and performed by the host application or test fixture; the
    /// framework never auto-registers any provider.
    /// </summary>
    public static class DbProviderRegistry
    {
        private static readonly Dictionary<DatabaseType, DbProviderFactory> _factories = [];
        private static readonly Dictionary<DatabaseType, Action<DbConnection>> _initializers = [];

        /// <summary>
        /// Registers an ADO.NET provider factory for the specified database type.
        /// Re-registering replaces the previous entry and clears any associated connection initializer.
        /// </summary>
        /// <param name="type">The database type.</param>
        /// <param name="factory">The provider factory.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="factory"/> is null.</exception>
        public static void Register(DatabaseType type, DbProviderFactory factory)
            => Register(type, factory, null);

        /// <summary>
        /// Registers an ADO.NET provider factory along with an optional connection initializer
        /// that runs once on every newly opened connection of this database type.
        /// Re-registering replaces the previous entry; passing <c>null</c> for
        /// <paramref name="connectionInitializer"/> clears any previously set initializer.
        /// </summary>
        /// <param name="type">The database type.</param>
        /// <param name="factory">The provider factory.</param>
        /// <param name="connectionInitializer">
        /// Optional action invoked after a freshly created connection is opened. Typical use:
        /// dialect-specific session settings (e.g. Oracle <c>ALTER SESSION SET NLS_COMP=...</c>).
        /// The action runs against an already opened connection and may execute commands directly.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="factory"/> is null.</exception>
        public static void Register(DatabaseType type, DbProviderFactory factory, Action<DbConnection>? connectionInitializer)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory), "DbProviderFactory cannot be null.");

            _factories[type] = factory;
            if (connectionInitializer != null)
                _initializers[type] = connectionInitializer;
            else
                _initializers.Remove(type);
        }

        /// <summary>
        /// Gets the <see cref="DbProviderFactory"/> registered for the specified database type.
        /// </summary>
        /// <param name="type">The database type.</param>
        /// <returns>The registered <see cref="DbProviderFactory"/>.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no factory is registered for <paramref name="type"/>.</exception>
        public static DbProviderFactory Get(DatabaseType type)
        {
            if (_factories.TryGetValue(type, out var factory))
                return factory;
            throw new KeyNotFoundException($"Database provider not registered: {type}");
        }

        /// <summary>
        /// Gets the connection initializer registered for the specified database type, or
        /// <c>null</c> if no initializer was registered.
        /// </summary>
        /// <param name="type">The database type.</param>
        public static Action<DbConnection>? GetConnectionInitializer(DatabaseType type)
            => _initializers.TryGetValue(type, out var initializer) ? initializer : null;

        /// <summary>
        /// Determines whether a provider factory is registered for the specified database type.
        /// </summary>
        /// <param name="type">The database type.</param>
        public static bool IsRegistered(DatabaseType type) => _factories.ContainsKey(type);
    }
}
