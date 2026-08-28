using System.Globalization;
using System.Text.Json;
using Bee.Base.Security;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Db.Providers;
using Bee.Db.Schema;
using Bee.Definition;
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
    // Three categories, three database ids: framework cross-company tables in common, business
    // data (ft_* + the app's org tables) in company, the audit trail in log. All three resolve to
    // the same SQLite file in this single-company demo (see DatabaseSettings.xml) — the category
    // is what the framework routes on, so splitting them later is a change to that file alone.
    private const string CommonDatabaseId = "common";
    private const string CompanyDatabaseId = "company";

    private sealed record SeedTable(
        string Table,
        string File,
        Dictionary<string, string>? Forward = null,
        Dictionary<string, string>? Deferred = null);

    /// <summary>
    /// Resource-path prefixes of the framework-owned table definitions inside
    /// <see cref="Defaults"/>. Shared with <see cref="NorthwindBackend"/>, which materialises the
    /// same sets into the demo's <c>Define</c> directory.
    /// </summary>
    public static readonly string[] FrameworkTableSchemaPrefixes =
    {
        "TableSchema/common/",
        "TableSchema/log/",
    };

    private const string CommonTableSchemaPrefix = "TableSchema/common/";

    private const string TableSchemaSuffix = ".TableSchema.xml";

    /// <summary>
    /// Names every common-database table the framework ships a definition for, derived from
    /// <see cref="Defaults.ListEmbedded"/> rather than a hand-written list.
    /// </summary>
    /// <remarks>
    /// These tables <em>are</em> registered in <c>DbCategorySettings</c> under the <c>common</c>
    /// category, so building them goes through the same category loop as every other table.
    /// This list is no longer what creates them; it is what checks the registration is complete.
    ///
    /// IMPORTANT: the check exists because a hand-maintained list is exactly what broke sign-in
    /// once already — two framework changes each added a common-table dependency to `Login`, the
    /// list was not updated, and the failure surfaced only as a generic API error at sign-in time.
    /// Moving the list into XML makes "add a table" pure configuration, but it does not make the
    /// list self-maintaining, so <see cref="VerifyCommonRegistration"/> compares the registered
    /// set against what the framework actually ships and fails startup on a gap.
    ///
    /// The trade-off is that tables this demo never uses — <c>st_company</c>,
    /// <c>st_user_company</c>, <c>st_define</c>, <c>st_api_key</c> — are created empty, because it
    /// substitutes its own <c>ICompanyInfoService</c> and does not store definitions in the
    /// database. A few unused empty tables in the demo SQLite file are a cheaper price than
    /// silently breaking every head the next time the framework reaches for a new table.
    /// </remarks>
    public static IReadOnlyList<string> GetFrameworkCommonTables()
    {
        return Defaults.ListEmbedded()
            .Where(rel => rel.StartsWith(CommonTableSchemaPrefix, StringComparison.Ordinal)
                && rel.EndsWith(TableSchemaSuffix, StringComparison.Ordinal))
            .Select(rel => rel[CommonTableSchemaPrefix.Length..^TableSchemaSuffix.Length])
            .ToArray();
    }

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
        // Order header references three lookups; employee_rowid points at the framework
        // system table st_employee (a business table referencing a framework table). All
        // three targets are inserted above, so the relations resolve forward on insert.
        new("ft_order", "Order.json",
            Forward: new()
            {
                ["customer_rowid"] = "ft_customer",
                ["employee_rowid"] = "st_employee",
                ["shipper_rowid"] = "ft_shipper",
            }),
        // Detail rows resolve sys_master_rowid to the just-inserted order's sys_rowid via
        // the same forward mechanism (the seed carries the order's sys_id), and product_rowid
        // to the product. ft_order_detail has no sys_id of its own, which is fine — only the
        // deferred relation pass requires sys_id, and details declare no deferred relations.
        new("ft_order_detail", "OrderDetail.json",
            Forward: new() { ["sys_master_rowid"] = "ft_order", ["product_rowid"] = "ft_product" }),
        // Per-form audit rules. No relations of its own — sys_id is the progId it governs, and a
        // form with no row here inherits the deployment-wide switches in SystemSettings.xml.
        // The three seeded rows exist to make the mechanism visible: this demo has ChangeEnabled
        // on and AccessEnabled off, so Order and Customer demonstrate a rule turning reads ON
        // against that default, and Category demonstrates one turning changes OFF.
        new("st_audit_rule", "AuditRule.json"),
    };

    public static void EnsureSchemaAndSeed(
        IDefineAccess defineAccess, IDbConnectionManager connectionManager, IDbAccessFactory dbAccessFactory)
    {
        ArgumentNullException.ThrowIfNull(defineAccess);
        ArgumentNullException.ThrowIfNull(connectionManager);
        ArgumentNullException.ThrowIfNull(dbAccessFactory);

        VerifyCommonRegistration(defineAccess);
        EnsureSchema(defineAccess, connectionManager);
        SeedDemoUser(dbAccessFactory.Create(CommonDatabaseId));

        // The rest of the seed data is business data, so it lands in the company database.
        var dbAccess = dbAccessFactory.Create(CompanyDatabaseId);
        var seedDir = Path.Combine(AppContext.BaseDirectory, "SeedData");

        foreach (var seed in s_seeds)
            InsertRows(dbAccess, seed, seedDir);

        foreach (var seed in s_seeds.Where(s => s.Deferred is not null))
            ApplyDeferredRelations(dbAccess, seed, seedDir);

        VerifyTablesExist(defineAccess, connectionManager);
    }

    /// <summary>
    /// Fails startup when the <c>common</c> category does not register every common table the
    /// framework ships a definition for, naming the ones that are missing.
    /// </summary>
    /// <remarks>
    /// Registering tables in <c>DbCategorySettings</c> is what makes "add a table" pure
    /// configuration, but a hand-written list does not notice when the framework grows a new
    /// dependency. This check is the part that does notice: it compares the registered set against
    /// <see cref="GetFrameworkCommonTables"/>, which is derived from the embedded defaults rather
    /// than typed out. The failure it prevents is specific and has happened — a framework change
    /// added a common-table dependency to sign-in, nothing here was updated, and the only symptom
    /// was a generic API error at login.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The common category is missing or incomplete.</exception>
    private static void VerifyCommonRegistration(IDefineAccess defineAccess)
    {
        var settings = defineAccess.GetDbCategorySettings();
        var registered = settings.Categories?
            .Where(c => string.Equals(c.Id, CommonDatabaseId, StringComparison.Ordinal))
            .SelectMany(c => c.Tables?.Select(t => t.TableName) ?? Enumerable.Empty<string>())
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var missing = GetFrameworkCommonTables()
            .Where(table => !registered.Contains(table))
            .ToList();

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Northwind startup aborted: DbCategorySettings.xml does not register these framework " +
                "common tables under the \"common\" category — " + string.Join(", ", missing) +
                ". Add a TableItem for each; the framework reaches for them directly and the failure " +
                "would otherwise surface only as a generic API error at sign-in.");
        }
    }

    /// <summary>
    /// Seeds the single demo account into <c>st_user</c> so sign-in runs the framework's own
    /// <c>st_user</c> authentication rather than an application-supplied credential check.
    /// </summary>
    /// <remarks>
    /// The hash is computed here rather than stored as a literal: a literal would silently stop
    /// matching the first time the hashing parameters change, and the symptom would be a correct
    /// password that no longer signs in.
    /// <para>
    /// The row also carries <c>time_zone</c> and <c>culture</c>, which is what makes the session
    /// take its locale from the user rather than from the deployment defaults.
    /// </para>
    /// </remarks>
    private static void SeedDemoUser(DbAccess dbAccess)
    {
        var countSpec = new DbCommandSpec(DbCommandKind.Scalar, "SELECT COUNT(*) FROM st_user");
        if (Convert.ToInt32(dbAccess.Execute(countSpec).Scalar, CultureInfo.InvariantCulture) > 0) { return; }

        const string sql =
            "INSERT INTO st_user (sys_rowid, sys_id, sys_name, password, email, note, time_zone, culture, deployment_admin) " +
            "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8})";

        dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, sql,
            Guid.NewGuid(),
            NorthwindCredentials.UserId,
            NorthwindCredentials.DisplayName,
            PasswordHasher.HashPassword(NorthwindCredentials.Password),
            string.Empty,
            string.Empty,
            NorthwindCredentials.TimeZone,
            NorthwindCredentials.Culture,
            false));
    }

    /// <summary>
    /// Fails startup when any table this seeder was meant to create is absent, naming the ones
    /// that are missing.
    /// </summary>
    /// <remarks>
    /// The point is to convert a silent failure into a loud one. A table that is expected but
    /// missing does not announce itself: nothing breaks until some request happens to touch it,
    /// and by then the symptom is a generic API error a long way from the cause. Checking at
    /// startup means the demo either comes up usable or refuses to come up at all.
    /// </remarks>
    /// <exception cref="InvalidOperationException">One or more expected tables are missing.</exception>
    private static void VerifyTablesExist(IDefineAccess defineAccess, IDbConnectionManager connectionManager)
    {
        var expected = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        var settings = defineAccess.GetDbCategorySettings();
        if (settings.Categories != null)
        {
            foreach (var category in settings.Categories.Where(c => c.Tables != null))
            {
                if (!expected.TryGetValue(category.Id, out var names))
                {
                    names = new List<string>();
                    expected[category.Id] = names;
                }
                names.AddRange(category.Tables!.Select(t => t.TableName));
            }
        }

        var missing = new List<string>();
        foreach (var (databaseId, tables) in expected)
        {
            var connectionInfo = connectionManager.GetConnectionInfo(databaseId);
            var provider = DbDialectRegistry.Get(connectionInfo.DatabaseType)
                .CreateTableSchemaProvider(databaseId, connectionManager);

            // GetTableSchema returns null for a table that does not exist, so absence is an
            // ordinary result here rather than a provider exception to catch.
            missing.AddRange(tables
                .Where(table => provider.GetTableSchema(table) == null)
                .Select(table => $"{databaseId}.{table}"));
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Northwind startup aborted: the following tables were expected but do not exist — " +
                string.Join(", ", missing) +
                ". Delete northwind.db to rebuild the schema from scratch; if a table still fails " +
                "to appear, its TableSchema definition is missing or failed to build.");
        }
    }

    private static void EnsureSchema(IDefineAccess defineAccess, IDbConnectionManager connectionManager)
    {
        // Build every table registered in DbCategorySettings, so adding a new table is pure XML
        // (a TableSchema file + a DbCategorySettings entry) — no edit here. This is what makes
        // the README's "add a Region form in 30 minutes, zero code" walkthrough honest. Each
        // category's id names both the target database and the TableSchema/<id>/ folder.
        var settings = defineAccess.GetDbCategorySettings();
        if (settings.Categories != null)
        {
            foreach (var category in settings.Categories)
            {
                if (category.Tables == null) { continue; }
                var builder = new TableSchemaBuilder(category.Id, defineAccess, connectionManager);
                foreach (var table in category.Tables)
                    builder.Execute(category.Id, table.TableName);
            }
        }

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
