using Bee.Db.Manager;
using Bee.Db.Schema;
using Bee.Definition.Storage;

namespace Bee.Northwind.Server;

/// <summary>
/// Process-once helper that materializes the demo's table schema from the
/// <c>TableSchema</c> definitions on first run. Idempotent: <see cref="TableSchemaBuilder"/>
/// is create-if-not-exists, so a second invocation is a no-op.
/// </summary>
/// <remarks>
/// Stage 1 creates only <c>ft_category</c> (plus the framework's <c>st_cache_notify</c>,
/// materialized by <see cref="NorthwindBackend"/>). Later stages add the remaining
/// <c>ft_*</c> business tables and JSON-driven seed rows.
/// </remarks>
public static class NorthwindSchemaSeeder
{
    private const string DatabaseId = "common";

    private static readonly string[] s_tables =
    {
        "ft_category",
        // Framework table polled by CacheNotifyPoller; its schema is materialized from
        // Bee.Definition embedded defaults by NorthwindBackend.AddNorthwindBackend.
        "st_cache_notify",
    };

    public static void EnsureSchema(IDefineAccess defineAccess, IDbConnectionManager connectionManager)
    {
        ArgumentNullException.ThrowIfNull(defineAccess);
        ArgumentNullException.ThrowIfNull(connectionManager);

        var builder = new TableSchemaBuilder(DatabaseId, defineAccess, connectionManager);
        foreach (var table in s_tables)
        {
            builder.Execute(DatabaseId, table);
        }
    }
}
