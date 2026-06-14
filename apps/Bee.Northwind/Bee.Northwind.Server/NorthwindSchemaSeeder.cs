using System.Globalization;
using System.Text.Json;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Db.Schema;
using Bee.Definition.Storage;

namespace Bee.Northwind.Server;

/// <summary>
/// Process-once helper that materializes the demo's table schema from the
/// <c>TableSchema</c> definitions and seeds each business master from a JSON file in
/// <c>SeedData/</c>. Idempotent: schema build is create-if-not-exists, and each table is
/// seeded only when empty.
/// </summary>
/// <remarks>
/// The seed JSON carries a Northwind subset (real category / supplier / customer / shipper
/// content). Each row's columns are the object's keys; <c>sys_rowid</c> is generated per row.
/// Table and column identifiers come from in-repo definition / seed files (not user input);
/// all values are passed as parameters.
/// </remarks>
public static class NorthwindSchemaSeeder
{
    private const string DatabaseId = "common";

    private static readonly string[] s_tables =
    {
        "ft_category",
        "ft_supplier",
        "ft_customer",
        "ft_shipper",
        // Framework table polled by CacheNotifyPoller; its schema is materialized from
        // Bee.Definition embedded defaults by NorthwindBackend.AddNorthwindBackend.
        "st_cache_notify",
    };

    // table → seed JSON file (under SeedData/). Tables without an entry are schema-only.
    private static readonly (string Table, string File)[] s_seeds =
    {
        ("ft_category", "Category.json"),
        ("ft_supplier", "Supplier.json"),
        ("ft_customer", "Customer.json"),
        ("ft_shipper", "Shipper.json"),
    };

    public static void EnsureSchemaAndSeed(
        IDefineAccess defineAccess, IDbConnectionManager connectionManager, IDbAccessFactory dbAccessFactory)
    {
        ArgumentNullException.ThrowIfNull(defineAccess);
        ArgumentNullException.ThrowIfNull(connectionManager);
        ArgumentNullException.ThrowIfNull(dbAccessFactory);

        EnsureSchema(defineAccess, connectionManager);

        var seedDir = Path.Combine(AppContext.BaseDirectory, "SeedData");
        foreach (var (table, file) in s_seeds)
            SeedFromJson(dbAccessFactory, table, Path.Combine(seedDir, file));
    }

    private static void EnsureSchema(IDefineAccess defineAccess, IDbConnectionManager connectionManager)
    {
        var builder = new TableSchemaBuilder(DatabaseId, defineAccess, connectionManager);
        foreach (var table in s_tables)
            builder.Execute(DatabaseId, table);
    }

    private static void SeedFromJson(IDbAccessFactory dbAccessFactory, string table, string jsonPath)
    {
        var dbAccess = dbAccessFactory.Create(DatabaseId);

        var countSpec = new DbCommandSpec(DbCommandKind.Scalar, $"SELECT COUNT(*) FROM {table}");
        var count = Convert.ToInt32(dbAccess.Execute(countSpec).Scalar, CultureInfo.InvariantCulture);
        if (count > 0) return;

        var rows = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(File.ReadAllText(jsonPath))
                   ?? new List<Dictionary<string, JsonElement>>();

        foreach (var row in rows)
        {
            var columns = new List<string> { "sys_rowid" };
            var values = new List<object> { Guid.NewGuid() };
            foreach (var pair in row)
            {
                columns.Add(pair.Key);
                values.Add(pair.Value.GetString() ?? string.Empty);
            }

            var placeholders = string.Join(",", Enumerable.Range(0, values.Count).Select(i => $"{{{i}}}"));
            var sql = $"INSERT INTO {table} ({string.Join(",", columns)}) VALUES ({placeholders})";
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, sql, values.ToArray()));
        }
    }
}
