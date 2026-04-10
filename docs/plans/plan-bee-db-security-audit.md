# Bee.Db Security & Performance Audit Plan

## Audit Scope

Full review of `src/Bee.Db/` project — database access patterns, SQL generation, connection management, error handling.

---

## Findings

### Security Issues

#### S1. SQL Injection in `SqlCreateTableCommandBuilder` (HIGH)

**Files:**
- `src/Bee.Db/Providers/SqlServer/SqlCreateTableCommandBuilder.cs`

**Details:**

Multiple methods directly interpolate table names and index names into raw SQL strings without parameterization or proper escaping:

| Method | Line(s) | Problem |
|--------|---------|---------|
| `GetDropTableCommandText` | 97-98 | `tableName` interpolated into `N'{tableName}'` and `EXEC('DROP TABLE {tableName}')` |
| `GetRenameTableCommandText` | 138-141 | Table/index names interpolated into `sp_rename` calls |
| `GetInsertTableCommandText` | 120-121 | Uses `[{tableName}]` but doesn't escape `]` in names |
| `GetCreateTableCommandText` | 162 | `[{dbTableName}]` without `]` escaping |
| `GetPrimaryKeyCommandText` | 309, 312 | `[{field.FieldName}]` without `]` escaping |
| `GetIndexCommandText` | 345, 349 | Same as above |
| `GetFieldCommandText` | 212-214 | `[{field.FieldName}]` without `]` escaping |
| `GetCommandText` | 55-57 | `this.TableName` in SQL comment — low risk but inconsistent |

**Risk Assessment:** These values originate from `TableSchema` (internal configuration), not direct user input. However, schema definitions may come from form configurations that could be admin-controlled. The `EXEC('DROP TABLE ...')` pattern in `GetDropTableCommandText` is especially dangerous as it constructs dynamic SQL.

**Fix:**
- Create a `SanitizeIdentifier` helper that escapes `]` → `]]` (SQL Server).
- Apply it consistently to all identifier interpolation points.
- For `GetDropTableCommandText`, use `QUOTENAME()` in the SQL or parameterize the table name check.

---

#### S2. `QuoteIdentifier` doesn't escape delimiter characters (HIGH)

**File:** `src/Bee.Db/DbFunc.cs:57`

**Details:**

```csharp
{ DatabaseType.SQLServer, s => $"[{s}]" },
{ DatabaseType.MySQL, s => $"`{s}`" },
{ DatabaseType.SQLite, s => $"\"{s}\"" },
{ DatabaseType.Oracle, s => $"\"{s}\"" }
```

If an identifier contains the delimiter character itself (e.g. `]` for SQL Server, `` ` `` for MySQL, `"` for SQLite/Oracle), the quoting is broken and could allow SQL injection. The correct escaping rules:
- SQL Server: `]` → `]]`
- MySQL: `` ` `` → ``` `` ```
- SQLite/Oracle: `"` → `""`

**Impact:** All query builders that use `DbFunc.QuoteIdentifier` (WhereBuilder, SelectBuilder, FromBuilder, TableSchemaCommandBuilder) are potentially affected if identifiers contain special characters.

**Fix:** Update `QuoteIdentifiers` dictionary to include proper escaping:
```csharp
{ DatabaseType.SQLServer, s => $"[{s.Replace("]", "]]")}]" },
{ DatabaseType.MySQL, s => $"`{s.Replace("`", "``")}`" },
{ DatabaseType.SQLite, s => $"\"{s.Replace("\"", "\"\"")}\"" },
{ DatabaseType.Oracle, s => $"\"{s.Replace("\"", "\"\"")}\"" },
```

---

#### S3. DataTable RowFilter injection in `SqlTableSchemaProvider` (MEDIUM)

**File:** `src/Bee.Db/Providers/SqlServer/SqlTableSchemaProvider.cs:151`

**Details:**

```csharp
table.DefaultView.RowFilter = $"Name='{name}'";
```

The `name` variable comes from the same DataTable's rows (database query results), so it's not directly user-controlled. However, if an index name in the database contains a single quote (`'`), the RowFilter expression will break and could either throw an exception or produce incorrect filtering results.

**Fix:** Escape single quotes by doubling them:
```csharp
table.DefaultView.RowFilter = $"Name='{name.Replace("'", "''")}'";
```

---

#### S4. Sensitive information in error logs (MEDIUM)

**File:** `src/Bee.Db/Logging/DbAccessLogger.cs:74-76`

**Details:**

```csharp
sb.Append("Message=").Append(exception.Message).Append("; ");
sb.Append("CommandText=").Append(context.CommandText);
```

Full SQL command text and exception messages are included in error logs. Database exceptions from providers (e.g. SqlException) may contain server names, connection details, or schema information. The CommandText reveals table structures and query patterns.

**Fix:**
- Truncate `CommandText` to a configurable maximum length in logs.
- Avoid logging the full `exception.Message`; log only `exception.GetType().Name` and error codes.
- Same issue in `WriteWarning` (line 102): `ctx.CommandText` is logged.

---

#### S5. Connection string stored in plain text in memory (LOW)

**File:** `src/Bee.Db/Manager/DbConnectionInfo.cs:33`

**Details:**

`DbConnectionInfo.ConnectionString` is a plain `string` property containing the full connection string with embedded credentials. It's cached indefinitely in `DbConnectionManager._cache` (a static `ConcurrentDictionary`). This means credentials remain in process memory for the lifetime of the application.

**Risk:** In a memory dump or debugger scenario, credentials could be extracted. This is standard ADO.NET behavior and is generally accepted, but worth documenting.

**Recommendation:** Document this as a known trade-off. If higher security is needed, consider using `SecureString` or delegating to the connection string builder's `PersistSecurityInfo=false` option.

---

### Performance Issues

#### P1. Unbounded `ILMapper` cache (MEDIUM)

**File:** `src/Bee.Db/ILMapper.cs:17`

**Details:**

```csharp
private static readonly ConcurrentDictionary<(Type, string), Delegate> _cache = ...
```

The cache is static and entries are never evicted. If the application uses many different query shapes (different column orderings or subsets), the cache grows unboundedly. Each entry holds a compiled `DynamicMethod` delegate.

**Fix:**
- Add a `ClearCache()` method for explicit cleanup.
- Consider a bounded cache with LRU eviction for long-running applications.
- At minimum, document the caching behavior.

---

#### P2. `SqlTableSchemaProvider` creates redundant `DbAccess` instances (LOW)

**File:** `src/Bee.Db/Providers/SqlServer/SqlTableSchemaProvider.cs:72-73, 93-94, 183-184`

**Details:**

Three separate methods (`TableExists`, `GetTableIndexes`, `GetColumns`) each create a new `DbAccessObject(DatabaseId)` instance. While the underlying connection pooling mitigates the connection overhead, the repeated object creation and connection open/close cycles are unnecessary.

**Fix:** Create a single `DbAccess` instance in `GetTableSchema` and pass it down, or make it a class field.

---

#### P3. String concatenation with `+=` in loops (LOW)

**File:** `src/Bee.Db/Providers/SqlServer/SqlCreateTableCommandBuilder.cs:110-118, 303-308, 340-345`

**Details:**

Multiple methods build SQL fragments using `string += "..."` inside loops instead of `StringBuilder`:

```csharp
// GetInsertTableCommandText, line 116
fields += $"[{field.FieldName}]";

// GetPrimaryKeyCommandText, line 308
fields += $"[{field.FieldName}] ...";
```

For tables with many columns, this creates O(n^2) string allocations.

**Fix:** Replace with `StringBuilder` (already used in other methods of the same class).

---

#### P4. DataTable loads entire result set into memory (LOW — by design)

**File:** `src/Bee.Db/DbAccess/DbAccess.cs:277-278, 617`

**Details:**

`ExecuteDataTableCore` uses `adapter.Fill(table)` which loads all rows. For large result sets, this causes memory pressure. The `Query<T>()` method provides a streaming alternative via `DbDataReader`, which is good.

**Recommendation:** This is by design (DataTable is inherently in-memory). Document the recommended use of `Query<T>()` for large result sets.

---

## Proposed Fix Priority

| Priority | Issue | Effort |
|----------|-------|--------|
| 1 | S2: `QuoteIdentifier` escape fix | Small |
| 2 | S1: `SqlCreateTableCommandBuilder` SQL injection | Medium |
| 3 | S3: RowFilter injection escape | Small |
| 4 | S4: Sensitive info in error logs | Small |
| 5 | P1: ILMapper cache cleanup method | Small |
| 6 | P2: Redundant DbAccess instances | Small |
| 7 | P3: String concatenation optimization | Small |
| 8 | S5: Connection string in memory (document only) | Minimal |
| 9 | P4: DataTable memory (document only) | Minimal |

## Implementation Notes

- S2 is the highest priority because `QuoteIdentifier` is used across ALL query builders.
- S1 fixes should reuse the fixed `QuoteIdentifier` where possible, plus add SQL-level protections for the `EXEC()` pattern.
- All fixes should include corresponding unit tests per the testing rules in `.claude/rules/testing.md`.
