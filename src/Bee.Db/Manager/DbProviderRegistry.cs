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

        /// <summary>
        /// Registers an ADO.NET provider factory for the specified database type.
        /// Re-registering replaces the previous entry.
        /// </summary>
        /// <param name="type">The database type.</param>
        /// <param name="factory">The provider factory.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="factory"/> is null.</exception>
        public static void Register(DatabaseType type, DbProviderFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory), "DbProviderFactory cannot be null.");

            _factories[type] = factory;
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
        /// Determines whether a provider factory is registered for the specified database type.
        /// </summary>
        /// <param name="type">The database type.</param>
        public static bool IsRegistered(DatabaseType type) => _factories.ContainsKey(type);
    }
}
