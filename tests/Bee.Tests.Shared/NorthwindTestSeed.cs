using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Bee.Base.Data;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Storage;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// Seeds the Northwind business tables (the 7 <c>ft_*</c> tables plus the upgraded
    /// <c>st_department</c> / <c>st_employee</c>) of the per-database <c>company</c> category
    /// from JSON embedded in this assembly. Mirrors the demo's
    /// <c>Bee.Northwind.Server.NorthwindSchemaSeeder</c> insert logic but is dialect-portable:
    /// identifiers are quoted per <see cref="DatabaseType"/> (Oracle folds unquoted names to
    /// upper case) and JSON string values are converted to the CLR type implied by each
    /// column's <see cref="FieldDbType"/> (the seed JSON stores every value as a string,
    /// including numeric-looking PKs like the order code <c>"10248"</c>, so the column type —
    /// not the value — decides the binding).
    /// </summary>
    /// <remarks>
    /// Test assemblies run as parallel processes against the same physical database, so the
    /// whole seed runs inside one transaction. The first table (<c>ft_category</c>) has a
    /// unique <c>sys_id</c>: the winning process commits the complete set atomically, while a
    /// loser blocks on that unique key, then fails and rolls back (its exception is caught and
    /// logged by <c>SharedDatabaseState.EnsureDatabase</c>). Other processes therefore never
    /// observe a half-seeded database — they either see nothing (and seed it themselves) or the
    /// full committed set. Relation columns in the JSON carry the target's <c>sys_id</c>;
    /// forward relations resolve to the target's <c>sys_rowid</c> on insert, and the one
    /// circular relation (<c>st_department.manager_rowid</c> → an employee) is applied in a
    /// second pass within the same transaction.
    /// </remarks>
    internal static class NorthwindTestSeed
    {
        private const string CompanyCategoryId = "company";

        private sealed record SeedTable(
            string Table,
            string File,
            Dictionary<string, string>? Forward = null,
            Dictionary<string, string>? Deferred = null);

        // Insert order: a forward-relation target must precede its dependents.
        private static readonly SeedTable[] s_seeds =
        {
            new("ft_category", "Category.json"),
            new("ft_supplier", "Supplier.json"),
            new("ft_customer", "Customer.json"),
            new("ft_shipper", "Shipper.json"),
            new("ft_product", "Product.json",
                Forward: new() { ["supplier_rowid"] = "ft_supplier", ["category_rowid"] = "ft_category" }),
            // Department.manager_rowid → Employee is circular, so it is deferred; Employee is
            // inserted next with dept_rowid resolved forward to the just-inserted departments.
            new("st_department", "Department.json", Deferred: new() { ["manager_rowid"] = "st_employee" }),
            new("st_employee", "Employee.json", Forward: new() { ["dept_rowid"] = "st_department" }),
            new("ft_order", "Order.json",
                Forward: new()
                {
                    ["customer_rowid"] = "ft_customer",
                    ["employee_rowid"] = "st_employee",
                    ["shipper_rowid"] = "ft_shipper",
                }),
            new("ft_order_detail", "OrderDetail.json",
                Forward: new() { ["sys_master_rowid"] = "ft_order", ["product_rowid"] = "ft_product" }),
        };

        /// <summary>
        /// Seeds all Northwind business tables into the <c>company</c> database of the given
        /// <paramref name="dbType"/>. No-op when the database is already seeded.
        /// </summary>
        public static void Seed(DatabaseType dbType, IDefineAccess access, IDbConnectionManager connectionManager)
        {
            var companyDatabaseId = TestDbConventions.GetDatabaseId(dbType, CompanyCategoryId);
            var dbAccess = new DbAccess(companyDatabaseId, connectionManager);

            // Fast path: a committed seed is visible (ft_category is the first table written and
            // the last barrier a loser fails on, so its presence means the full set committed).
            if (CountRows(dbType, dbAccess, "ft_category", transaction: null) > 0) return;

            using var connection = connectionManager.CreateConnection(companyDatabaseId);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            foreach (var seed in s_seeds)
                InsertRows(dbType, access, dbAccess, transaction, seed);

            foreach (var seed in s_seeds.Where(s => s.Deferred is not null))
                ApplyDeferredRelations(dbType, dbAccess, transaction, seed);

            // On any exception above, `using var transaction` disposes uncommitted and rolls
            // back; the exception propagates to EnsureDatabase, which logs and skips this DB.
            transaction.Commit();
            Console.WriteLine($"SharedDatabaseState: {companyDatabaseId} Northwind seed committed");
        }

        private static int CountRows(DatabaseType dbType, DbAccess dbAccess, string table, DbTransaction? transaction)
        {
            var spec = new DbCommandSpec(DbCommandKind.Scalar, $"SELECT COUNT(*) FROM {dbType.QuoteIdentifier(table)}");
            var result = transaction is null ? dbAccess.Execute(spec) : dbAccess.Execute(spec, transaction);
            return Convert.ToInt32(result.Scalar, CultureInfo.InvariantCulture);
        }

        private static void InsertRows(
            DatabaseType dbType, IDefineAccess access, DbAccess dbAccess, DbTransaction transaction, SeedTable seed)
        {
            string quotedTable = dbType.QuoteIdentifier(seed.Table);
            var fieldTypes = GetFieldTypes(access, seed.Table);

            foreach (var row in ReadRows(seed.File))
            {
                var columns = new List<string> { dbType.QuoteIdentifier("sys_rowid") };
                var values = new List<object> { Guid.NewGuid() };

                foreach (var pair in row)
                {
                    // Deferred columns are written in the second pass once their target exists.
                    if (seed.Deferred?.ContainsKey(pair.Key) == true) continue;

                    string raw = pair.Value.GetString() ?? string.Empty;

                    object? value;
                    if (seed.Forward is not null && seed.Forward.TryGetValue(pair.Key, out var target))
                    {
                        value = ResolveRowId(dbType, dbAccess, transaction, target, raw);
                    }
                    else
                    {
                        // A numeric / date column with an empty seed value is left to the DB
                        // default; an empty string would not parse to the column's CLR type.
                        if (raw.Length == 0 && IsTypedNonString(fieldTypes, pair.Key)) continue;
                        value = ConvertValue(fieldTypes, pair.Key, raw);
                    }

                    columns.Add(dbType.QuoteIdentifier(pair.Key));
                    values.Add(value ?? string.Empty);
                }

                var placeholders = string.Join(",", Enumerable.Range(0, values.Count).Select(i => $"{{{i}}}"));
                var sql = $"INSERT INTO {quotedTable} ({string.Join(",", columns)}) VALUES ({placeholders})";
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, sql, values.ToArray()), transaction);
            }
        }

        private static void ApplyDeferredRelations(
            DatabaseType dbType, DbAccess dbAccess, DbTransaction transaction, SeedTable seed)
        {
            string quotedTable = dbType.QuoteIdentifier(seed.Table);
            string quotedKey = dbType.QuoteIdentifier("sys_id");

            foreach (var row in ReadRows(seed.File))
            {
                if (!row.TryGetValue("sys_id", out var keyElement)) continue;
                var key = keyElement.GetString();
                if (string.IsNullOrEmpty(key)) continue;

                foreach (var (column, target) in seed.Deferred!)
                {
                    if (!row.TryGetValue(column, out var refElement)) continue;
                    var rowId = ResolveRowId(dbType, dbAccess, transaction, target, refElement.GetString());
                    if (rowId == Guid.Empty) continue;

                    var sql = $"UPDATE {quotedTable} SET {dbType.QuoteIdentifier(column)} = {{0}} WHERE {quotedKey} = {{1}}";
                    dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, sql, rowId, key), transaction);
                }
            }
        }

        private static Guid ResolveRowId(
            DatabaseType dbType, DbAccess dbAccess, DbTransaction transaction, string targetTable, string? sysId)
        {
            if (string.IsNullOrEmpty(sysId)) return Guid.Empty;
            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                $"SELECT {dbType.QuoteIdentifier("sys_rowid")} FROM {dbType.QuoteIdentifier(targetTable)} " +
                $"WHERE {dbType.QuoteIdentifier("sys_id")} = {{0}}", sysId);
            return ToGuid(dbAccess.Execute(spec, transaction).Scalar);
        }

        private static Dictionary<string, FieldDbType> GetFieldTypes(IDefineAccess access, string tableName)
        {
            var result = new Dictionary<string, FieldDbType>(StringComparer.OrdinalIgnoreCase);
            var schema = access.GetTableSchema(CompanyCategoryId, tableName);
            if (schema?.Fields == null) return result;
            foreach (var field in schema.Fields)
                result[field.FieldName] = field.DbType;
            return result;
        }

        private static bool IsTypedNonString(Dictionary<string, FieldDbType> fieldTypes, string fieldName)
        {
            if (!fieldTypes.TryGetValue(fieldName, out var dbType)) return false;
            return dbType is not (FieldDbType.String or FieldDbType.Text);
        }

        private static object ConvertValue(Dictionary<string, FieldDbType> fieldTypes, string fieldName, string raw)
        {
            if (!fieldTypes.TryGetValue(fieldName, out var dbType))
                return raw;

            switch (dbType)
            {
                case FieldDbType.Short:
                case FieldDbType.Integer:
                    return int.Parse(raw, CultureInfo.InvariantCulture);
                case FieldDbType.Long:
                    return long.Parse(raw, CultureInfo.InvariantCulture);
                case FieldDbType.Decimal:
                case FieldDbType.Currency:
                    return decimal.Parse(raw, CultureInfo.InvariantCulture);
                case FieldDbType.Boolean:
                    return raw == "1" || string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
                case FieldDbType.Date:
                case FieldDbType.DateTime:
                    // Kind=Utc: Npgsql rejects Unspecified/Local for PostgreSQL `timestamptz`
                    // columns. Seed dates carry no real time zone, so UTC is a safe label and
                    // is accepted by every provider's date/timestamp binding.
                    return DateTime.SpecifyKind(DateTime.Parse(raw, CultureInfo.InvariantCulture), DateTimeKind.Utc);
                case FieldDbType.Guid:
                    return Guid.Parse(raw);
                default:
                    return raw;
            }
        }

        private static List<Dictionary<string, JsonElement>> ReadRows(string file)
        {
            var assembly = typeof(NorthwindTestSeed).Assembly;
            string resourceName = $"{assembly.GetName().Name}.SeedData.{file}";
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded seed resource not found: {resourceName}");
            using var reader = new StreamReader(stream);
            return JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(reader.ReadToEnd())
                ?? [];
        }

        private static Guid ToGuid(object? value) => value switch
        {
            Guid g => g,
            byte[] { Length: 16 } b => new Guid(b),
            string s when Guid.TryParse(s, out var g) => g,
            _ => Guid.Empty,
        };
    }
}
