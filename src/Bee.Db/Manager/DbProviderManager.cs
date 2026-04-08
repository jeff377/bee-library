using System;
using System.Collections.Generic;
using System.Data.Common;
using Bee.Definition;

namespace Bee.Db.Manager
{
    /// <summary>
    /// Manages <see cref="DbProviderFactory"/> instances for different database types.
    /// </summary>
    public static class DbProviderManager
    {
        /// <summary>
        /// Stores the registered <see cref="DbProviderFactory"/> instances.
        /// </summary>
        private static readonly Dictionary<DatabaseType, DbProviderFactory> _factories = new Dictionary<DatabaseType, DbProviderFactory>();

        /// <summary>
        /// Registers a new database provider factory.
        /// </summary>
        /// <param name="type">The database type.</param>
        /// <param name="factory">The corresponding <see cref="DbProviderFactory"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="factory"/> is null.</exception>
        public static void RegisterProvider(DatabaseType type, DbProviderFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory), "DbProviderFactory cannot be null.");

            _factories[type] = factory;
        }

        /// <summary>
        /// Gets the <see cref="DbProviderFactory"/> registered for the specified database type.
        /// </summary>
        /// <param name="type">The database type.</param>
        /// <returns>The corresponding <see cref="DbProviderFactory"/>.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the specified type has not been registered.</exception>
        public static DbProviderFactory GetFactory(DatabaseType type)
        {
            if (_factories.TryGetValue(type, out var factory))
            {
                return factory;
            }
            throw new KeyNotFoundException($"Database provider not registered: {type}");
        }
    }
}
