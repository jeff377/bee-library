using Bee.Db.Providers;
using Bee.Definition.Database;

namespace Bee.Db.Manager
{
    /// <summary>
    /// Registry of <see cref="IDialectFactory"/> implementations keyed by <see cref="DatabaseType"/>.
    /// Mirrors the role of <see cref="DbProviderRegistry"/> (which stores ADO.NET <c>DbProviderFactory</c>)
    /// for the framework's own SQL-generation and schema-reading builders.
    /// Registration is explicit and performed by the host application or test fixture; the framework
    /// never auto-registers any dialect.
    /// </summary>
    public static class DbDialectRegistry
    {
        private static readonly Dictionary<DatabaseType, IDialectFactory> _factories = [];

        /// <summary>
        /// Registers a dialect factory for the specified database type.
        /// Re-registering replaces the previous entry.
        /// </summary>
        /// <param name="type">The database type.</param>
        /// <param name="factory">The dialect factory.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="factory"/> is null.</exception>
        public static void Register(DatabaseType type, IDialectFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory), "IDialectFactory cannot be null.");
            _factories[type] = factory;
        }

        /// <summary>
        /// Gets the dialect factory registered for the specified database type.
        /// </summary>
        /// <param name="type">The database type.</param>
        /// <returns>The registered <see cref="IDialectFactory"/>.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no factory is registered for <paramref name="type"/>.</exception>
        public static IDialectFactory Get(DatabaseType type)
        {
            if (_factories.TryGetValue(type, out var factory))
                return factory;
            throw new KeyNotFoundException($"Dialect factory not registered: {type}");
        }

        /// <summary>
        /// Indicates whether a factory is registered for the specified database type.
        /// </summary>
        /// <param name="type">The database type.</param>
        public static bool IsRegistered(DatabaseType type) => _factories.ContainsKey(type);
    }
}
