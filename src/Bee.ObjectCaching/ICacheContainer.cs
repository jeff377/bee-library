using Bee.ObjectCaching.Database;
using Bee.ObjectCaching.Define;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Exposes the cache instances managed by the framework. Consumers ctor-inject
    /// <see cref="ICacheContainer"/>; the implementation is supplied by
    /// <see cref="CacheContainerService"/> registered as a Singleton by
    /// <c>AddBeeFramework</c>.
    /// </summary>
    public interface ICacheContainer
    {
        /// <summary>The system settings cache.</summary>
        SystemSettingsCache SystemSettings { get; }

        /// <summary>The database settings cache.</summary>
        DatabaseSettingsCache DatabaseSettings { get; }

        /// <summary>The program settings cache.</summary>
        ProgramSettingsCache ProgramSettings { get; }

        /// <summary>The permission model registry cache.</summary>
        PermissionModelsCache PermissionModels { get; }

        /// <summary>The database category settings cache.</summary>
        DbCategorySettingsCache DbCategorySettings { get; }

        /// <summary>The system-level currency master cache.</summary>
        CurrencySettingsCache CurrencySettings { get; }

        /// <summary>The system-level unit-of-measure master cache.</summary>
        UnitSettingsCache UnitSettings { get; }

        /// <summary>The table schema cache, keyed by category id and table name.</summary>
        TableSchemaCache TableSchema { get; }

        /// <summary>The form schema cache, keyed by program identifier.</summary>
        FormSchemaCache FormSchema { get; }

        /// <summary>The form layout cache, keyed by layout identifier.</summary>
        FormLayoutCache FormLayout { get; }

        /// <summary>The language resource cache, keyed by language code and namespace.</summary>
        LanguageResourceCache LanguageResource { get; }

        /// <summary>The session information cache, keyed by access token.</summary>
        SessionInfoCache SessionInfo { get; }

        /// <summary>The company information cache, keyed by company id.</summary>
        CompanyInfoCache CompanyInfo { get; }

        /// <summary>The per-company role-permission snapshot cache, keyed by company id.</summary>
        CompanyRolePermissionsCache CompanyRolePermissions { get; }

        /// <summary>The per-company department-tree snapshot cache, keyed by company id.</summary>
        DepartmentTreeCache DepartmentTree { get; }

        /// <summary>
        /// Evicts the cache entry named by a <c>"group:entity"</c> cache key, dispatching to the
        /// owned cache whose <see cref="IEvictableCache.CacheGroup"/> matches the key's group
        /// (case-insensitive). Used by the cache-notify poller; returns <c>false</c> when no cache
        /// owns the group (the bump is ignored).
        /// </summary>
        /// <param name="cacheKey">The full <c>"group:entity"</c> cache key from the notification table.</param>
        /// <returns><c>true</c> when a cache matched the group and was evicted; otherwise <c>false</c>.</returns>
        bool TryEvict(string cacheKey);
    }
}
