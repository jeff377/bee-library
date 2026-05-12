using Bee.ObjectCaching.Database;
using Bee.ObjectCaching.Define;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Exposes the eight cache instances managed by the framework. Phase 5 PR 5.3c
    /// added this interface alongside the existing <see cref="CacheContainer"/> static
    /// facade. New consumers should ctor-inject <see cref="ICacheContainer"/>; the
    /// static class remains as a transitional shim for legacy callers and is removed
    /// once the test fixture rewrite (PR 5.4) finishes migrating them.
    /// </summary>
    public interface ICacheContainer
    {
        /// <summary>The system settings cache.</summary>
        SystemSettingsCache SystemSettings { get; }

        /// <summary>The database settings cache.</summary>
        DatabaseSettingsCache DatabaseSettings { get; }

        /// <summary>The program settings cache.</summary>
        ProgramSettingsCache ProgramSettings { get; }

        /// <summary>The database category settings cache.</summary>
        DbCategorySettingsCache DbCategorySettings { get; }

        /// <summary>The table schema cache, keyed by category id and table name.</summary>
        TableSchemaCache TableSchema { get; }

        /// <summary>The form schema cache, keyed by program identifier.</summary>
        FormSchemaCache FormSchema { get; }

        /// <summary>The form layout cache, keyed by layout identifier.</summary>
        FormLayoutCache FormLayout { get; }

        /// <summary>The session information cache, keyed by access token.</summary>
        SessionInfoCache SessionInfo { get; }
    }
}
