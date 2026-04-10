# Review: Database Connection Handling

## Overview

Review of the database connection lifecycle management in `Bee.Db`, covering
`DbConnectionScope`, `DbAccess`, and `DbConnectionManager`.

## Assessment Summary

Overall the connection handling is **well-designed**. The `DbConnectionScope`
ownership pattern, consistent `using` statements, and thread-safe caching are
solid. Two concrete issues were found.

---

## Strengths

| Area | Detail |
|------|--------|
| **Ownership semantics** | `DbConnectionScope` cleanly separates owned vs. external connections via `_ownsConnection` flag. External connections are never disposed by the scope. |
| **Deterministic cleanup** | Every `Execute`, `ExecuteBatch`, `Query`, `UpdateDataTable` (and async counterparts) wrap `DbConnectionScope` in `using` — connections are always released. |
| **Transaction safety** | `ExecuteBatch` / `ExecuteBatchAsync` use `TryRollbackQuiet` in catch blocks, preventing rollback exceptions from masking the original error. Transactions are disposed in `finally`. |
| **Broken-connection recovery** | `EnsureOpenSync` / `EnsureOpenAsync` reopen `Closed` or `Broken` connections before reuse. |
| **Async best practices** | `ConfigureAwait(false)` used throughout; `CancellationToken` propagated; .NET 8+ `BeginTransactionAsync` with .NET Standard 2.0 fallback. |
| **Thread-safe caching** | `DbConnectionManager` uses `ConcurrentDictionary.GetOrAdd` and responds to `GlobalEvents.DatabaseSettingsChanged` to invalidate cache. |
| **Connection pooling** | Relies on the underlying ADO.NET provider's built-in pooling (e.g. `SqlConnection`), which is the correct approach — no need to reinvent it. |

---

## Issue 1: Connection leak when `Open()` fails

**Severity:** Medium
**Files:** `src/Bee.Db/DbAccess/DbConnectionScope.cs` lines 45-49, 73-77

### Problem

In both `Create` and `CreateAsync`, a new `DbConnection` is created from the
factory but **not wrapped in a try/catch**. If `conn.Open()` (or `OpenAsync`)
throws (e.g. bad connection string, server unreachable, timeout), the
connection object is never disposed:

```csharp
// Current code (sync) — lines 45-49
var conn = factory.CreateConnection()
           ?? throw new InvalidOperationException("...");
conn.ConnectionString = connectionString;
conn.Open();                                    // <-- throws here
return new DbConnectionScope(conn, true);       // <-- never reached
```

Although ADO.NET pooling will eventually reclaim the underlying socket, the
managed `DbConnection` object and its unmanaged handle remain unreleased until
GC finalizes them. Under high concurrency or repeated connection failures this
can exhaust the connection pool.

### Suggested Fix

```csharp
// Sync
var conn = factory.CreateConnection()
           ?? throw new InvalidOperationException("...");
conn.ConnectionString = connectionString;
try
{
    conn.Open();
}
catch
{
    conn.Dispose();
    throw;
}
return new DbConnectionScope(conn, true);

// Async — same pattern with OpenAsync
```

---

## Issue 2: `UpdateDataTable` rollback can mask original exception

**Severity:** Low
**File:** `src/Bee.Db/DbAccess/DbAccess.cs` lines 364-368

### Problem

`UpdateDataTable` uses a bare `tran?.Rollback()` in the `catch` block:

```csharp
catch
{
    tran?.Rollback();   // If Rollback() throws, original exception is lost
    throw;
}
```

If `Rollback()` itself throws (e.g. connection already broken), the rollback
exception propagates and the **original exception is lost**. This is
inconsistent with `ExecuteBatch` (line 202) and `ExecuteBatchAsync` (line 542),
which correctly use `TryRollbackQuiet(tran)`.

### Suggested Fix

```csharp
catch
{
    TryRollbackQuiet(tran);
    throw;
}
```

---

## Items Reviewed (No Issues Found)

| Area | Notes |
|------|-------|
| **DbAccess constructors** | `databaseId` constructor validates input and retrieves cached info; `DbConnection` constructor validates non-null and resolves provider. Both are correct. |
| **DbCommand lifecycle** | All `CreateCommand` results are wrapped in `using` inside Core methods (`ExecuteNonQueryCore`, `ExecuteScalarCore`, etc.). |
| **DataAdapter lifecycle** | `UpdateDataTable` disposes adapter via `using`, and disposes insert/update/delete commands in `finally`. |
| **DbConnectionManager caching** | `GetOrAdd` is idempotent; `CreateConnectionInfo` is a pure factory. Cache invalidation on settings change is correct. |
| **Connection string security** | Placeholder substitution (`{@DbName}`, `{@UserId}`, `{@Password}`) avoids hardcoded credentials. Connection strings are not logged. |
| **Repository usage** | `SessionRepository` creates a new `DbAccess` per operation — no connection reuse across calls, but no leaks either. Acceptable for its usage pattern. |
| **SqlTableSchemaProvider** | Holds a `DbAccess` instance field and reuses it for multiple queries. Each call still creates/disposes its own scope — correct. |
| **External connection pattern** | Used in tests with proper `using` on the caller side. `DbAccess` does not dispose external connections — by design. |
