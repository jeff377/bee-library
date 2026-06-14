using System.Globalization;
using System.Text.Json;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Db.Schema;
using Bee.Definition.Storage;

namespace Bee.Northwind.Server;

/// <summary>
/// Process-once helper that materializes the demo's table schema from the
/// <c>TableSchema</c> definitions and seeds each table from a JSON file in <c>SeedData/</c>.
/// Idempotent: schema build is create-if-not-exists, each table is seeded only when empty,
/// and the deferred relation pass re-applies the same UPDATEs harmlessly.
/// </summary>
/// <remarks>
/// Relation columns in the seed JSON carry the <em>target's</em> <c>sys_id</c> (human
/// readable); the seeder resolves it to the target's <c>sys_rowid</c>. Forward relations are
/// resolved inline on insert (target already seeded). Circular relations — Department.manager
/// references an Employee while Employee.dept references a Department — are listed as deferred
/// and resolved in a second pass after every table is inserted. Table / column identifiers
/// come from in-repo definition / seed files (not user input); all values are parameters.
/// </remarks>
public static class NorthwindSchemaSeeder
{
    private const string DatabaseId = "common";

    private sealed record SeedTable(
        string Table,
        string File,
        Dictionary<string, string>? Forward = null,
        Dictionary<string, string>? Deferred = null);

    private static readonly string[] s_schemaTables =
    {
        "ft_category", "ft_supplier", "ft_customer", "ft_shipper", "ft_product",
        "st_department", "st_employee",
        // Framework table polled by CacheNotifyPoller; schema materialized from
        // Bee.Definition embedded defaults by NorthwindBackend.AddNorthwindBackend.
        "st_cache_notify",
    };

    // Insert order: a forward-relation target must precede its dependents.
    private static readonly SeedTable[] s_seeds =
    {
        new("ft_category", "Category.json"),
        new("ft_supplier", "Supplier.json"),
        new("ft_customer", "Customer.json"),
        new("ft_shipper", "Shipper.json"),
        new("ft_product", "Product.json",
            Forward: new() { ["supplier_rowid"] = "ft_supplier", ["category_rowid"] = "ft_category" }),
        // Department.manager_rowid -> Employee is circular, so it is deferred; Employee is
        // inserted next with dept_rowid resolved forward to the just-inserted departments.
        new("st_department", "Department.json", Deferred: new() { ["manager_rowid"] = "st_employee" }),
        new("st_employee", "Employee.json", Forward: new() { ["dept_rowid"] = "st_department" }),
    };

    public static void EnsureSchemaAndSeed(
        IDefineAccess defineAccess, IDbConnectionManager connectionManager, IDbAccessFactory dbAccessFactory)
    {
        ArgumentNullException.ThrowIfNull(defineAccess);
        ArgumentNullException.ThrowIfNull(connectionManager);
        ArgumentNullException.ThrowIfNull(dbAccessFactory);

        EnsureSchema(defineAccess, connectionManager);

        var dbAccess = dbAccessFactory.Create(DatabaseId);
        var seedDir = Path.Combine(AppContext.BaseDirectory, "SeedData");

        foreach (var seed in s_seeds)
            InsertRows(dbAccess, seed, seedDir);

        foreach (var seed in s_seeds.Where(s => s.Deferred is not null))
            ApplyDeferredRelations(dbAccess, seed, seedDir);
    }

    private static void EnsureSchema(IDefineAccess defineAccess, IDbConnectionManager connectionManager)
    {
        var builder = new TableSchemaBuilder(DatabaseId, defineAccess, connectionManager);
        foreach (var table in s_schemaTables)
            builder.Execute(DatabaseId, table);
    }

    private static void InsertRows(DbAccess dbAccess, SeedTable seed, string seedDir)
    {
        var countSpec = new DbCommandSpec(DbCommandKind.Scalar, $"SELECT COUNT(*) FROM {seed.Table}");
        if (Convert.ToInt32(dbAccess.Execute(countSpec).Scalar, CultureInfo.InvariantCulture) > 0) return;

        foreach (var row in ReadRows(seedDir, seed.File))
        {
            var columns = new List<string> { "sys_rowid" };
            var values = new List<object> { Guid.NewGuid() };

            foreach (var pair in row)
            {
                // Deferred columns are written in the second pass once their target exists.
                if (seed.Deferred?.ContainsKey(pair.Key) == true) continue;

                columns.Add(pair.Key);
                if (seed.Forward is not null && seed.Forward.TryGetValue(pair.Key, out var target))
                    values.Add(ResolveRowId(dbAccess, target, pair.Value.GetString()));
                else
                    values.Add(pair.Value.GetString() ?? string.Empty);
            }

            var placeholders = string.Join(",", Enumerable.Range(0, values.Count).Select(i => $"{{{i}}}"));
            var sql = $"INSERT INTO {seed.Table} ({string.Join(",", columns)}) VALUES ({placeholders})";
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, sql, values.ToArray()));
        }
    }

    private static void ApplyDeferredRelations(DbAccess dbAccess, SeedTable seed, string seedDir)
    {
        foreach (var row in ReadRows(seedDir, seed.File))
        {
            if (!row.TryGetValue("sys_id", out var keyElement)) continue;
            var key = keyElement.GetString();
            if (string.IsNullOrEmpty(key)) continue;

            foreach (var (column, target) in seed.Deferred!)
            {
                if (!row.TryGetValue(column, out var refElement)) continue;
                var rowId = ResolveRowId(dbAccess, target, refElement.GetString());
                if (rowId == Guid.Empty) continue;

                var sql = $"UPDATE {seed.Table} SET {column} = {{0}} WHERE sys_id = {{1}}";
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, sql, rowId, key));
            }
        }
    }

    private static Guid ResolveRowId(DbAccess dbAccess, string targetTable, string? sysId)
    {
        if (string.IsNullOrEmpty(sysId)) return Guid.Empty;
        var spec = new DbCommandSpec(DbCommandKind.Scalar, $"SELECT sys_rowid FROM {targetTable} WHERE sys_id = {{0}}", sysId);
        return ToGuid(dbAccess.Execute(spec).Scalar);
    }

    private static List<Dictionary<string, JsonElement>> ReadRows(string seedDir, string file)
        => JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(
               File.ReadAllText(Path.Combine(seedDir, file)))
           ?? new List<Dictionary<string, JsonElement>>();

    private static Guid ToGuid(object? value) => value switch
    {
        Guid g => g,
        string s when Guid.TryParse(s, out var g) => g,
        byte[] { Length: 16 } b => new Guid(b),
        _ => Guid.Empty,
    };
}
