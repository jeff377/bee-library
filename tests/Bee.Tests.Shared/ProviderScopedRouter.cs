using Bee.Definition;
using Bee.Definition.Database;
using Bee.Repository.Abstractions;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// Routes <see cref="DbScope.Common"/> and <see cref="DbScope.Log"/> to the test databases of
    /// one specific <see cref="DatabaseType"/>, so a repository declaring either scope reads and
    /// writes the engine the test names rather than the SQL Server default.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The production <c>RepositoryDatabaseRouter</c> resolves those two scopes to the fixed ids
    /// <c>common</c> and <c>log</c>, and the shared fixture binds <c>common</c> to SQL Server. A
    /// repository built with the default test router therefore runs on SQL Server no matter which
    /// database the enclosing <c>[DbFact(DatabaseType.X)]</c> names — the attribute degrades into
    /// nothing more than an environment-variable gate.
    /// </para>
    /// <para>
    /// Substituting the router rather than the repository's constructor keeps the two concerns
    /// apart: what these tests verify is whether the SQL a repository builds runs on a given
    /// engine, while scope-to-databaseId resolution is the router's own job and is covered by
    /// <c>RepositoryDatabaseRouterTests</c>.
    /// </para>
    /// </remarks>
    public sealed class ProviderScopedRouter : IRepositoryDatabaseRouter
    {
        private readonly DatabaseType _databaseType;

        /// <summary>
        /// Initializes a new <see cref="ProviderScopedRouter"/> bound to one database type.
        /// </summary>
        /// <param name="databaseType">The database type whose test databases every scope resolves to.</param>
        public ProviderScopedRouter(DatabaseType databaseType)
        {
            _databaseType = databaseType;
        }

        /// <inheritdoc/>
        public string Resolve(DbScope scope, Guid accessToken) => scope switch
        {
            DbScope.Common => TestDbConventions.GetDatabaseId(_databaseType, DbCategoryIds.Common),
            DbScope.Log => TestDbConventions.GetDatabaseId(_databaseType, DbCategoryIds.Log),
            DbScope.Company => TestDbConventions.GetDatabaseId(_databaseType, DbCategoryIds.Company),
            _ => throw new InvalidOperationException($"Unsupported DbScope value: {scope}."),
        };
    }
}
