# Table Schema Upgrade Guide

[繁體中文](database-schema-upgrade.zh-TW.md)

> This guide explains how a Bee.NET application maintains database table schemas: how definition changes are synchronized to the live database, the upgrade strategy used under the hood, and operational considerations.
> For naming rules see [Database Naming Conventions](database-naming-conventions.md); for the underlying definition-driven philosophy see [ADR-005 FormSchema-Driven](adr/adr-005-formschema-driven.md).

## 1. Core Concepts

Bee.NET adopts a **define-driven schema** model: the table structure is sourced from FormSchema / TableSchema XML definitions as the single source of truth. At application startup or during operations, the framework compares the definitions against the live database and automatically generates and runs the required upgrade statements.

As a developer you only need to:

1. Modify the definition file (add a column, widen a length, add an index)
2. Call the upgrade API
3. Let the framework decide whether to use ALTER or rebuild

### Success Contract

> **After a successful upgrade returns, the database must contain at least every column listed in the definition, and each column's type / length / nullability must match the definition.**

This is the only thing that matters from the application's perspective. Whether the framework chooses `ALTER TABLE` or a full table rebuild internally is a library implementation detail; callers do not need to care.

## 2. Entry-Point APIs

### 2.1 General Use: `IDatabaseRepository.UpgradeTableSchema`

The simplest call, suitable for most scenarios:

```csharp
var repo = RepositoryInfo.Get<IDatabaseRepository>();
bool upgraded = repo.UpgradeTableSchema("common", "myDb", "ft_employee");
```

The return value indicates whether an upgrade was actually performed: `false` means the database already matches the definition and nothing needed to change.

> The interface is defined in [IDatabaseRepository](../src/Bee.Repository.Abstractions/System/IDatabaseRepository.cs); the default implementation is [DatabaseRepository](../src/Bee.Repository/System/DatabaseRepository.cs).

### 2.2 Advanced Use: `TableSchemaBuilder`

When you need finer control (dry-run, `UpgradeOptions`, structured diff), drop down to [TableSchemaBuilder](../src/Bee.Db/Schema/TableSchemaBuilder.cs):

```csharp
var builder = new TableSchemaBuilder("common");

// Get the structured diff (no execution)
TableSchemaDiff diff = builder.CompareToDiff("myDb", "ft_employee");

// Get the SQL that would be executed (no execution)
string sql = builder.GetCommandText("myDb", "ft_employee");

// Run the upgrade (UpgradeOptions is optional)
bool upgraded = builder.Execute("myDb", "ft_employee", new UpgradeOptions
{
    AllowColumnNarrowing = true,
});
```

When to drop down to this layer:

- You want to inspect the SQL before deployment (dry-run)
- You need to enable special options such as "allow column narrowing"
- A maintenance tool needs to enumerate the columns / indexes that will change

## 3. Upgrade Pipeline: Diff → Plan → Execute

Internally the upgrade is split into three stages, each callable in isolation:

```
┌─────────────────────────────┐
│ 1. CompareToDiff            │  Compare definition vs live DB
│    → TableSchemaDiff        │  Pure structure, no SQL
└─────────────────────────────┘
              ↓
┌─────────────────────────────┐
│ 2. Orchestrator.Plan(diff)  │  Decide ALTER vs rebuild
│    → UpgradePlan            │  Staged SQL plus warnings
└─────────────────────────────┘
              ↓
┌─────────────────────────────┐
│ 3. Orchestrator.Execute     │  Run the plan against the DB
└─────────────────────────────┘
```

### TableSchemaDiff (Structured Changes)

[TableSchemaDiff](../src/Bee.Db/Schema/TableSchemaDiff.cs) is a provider-agnostic intermediate result listing each `ITableChange`:

| Change Type | Meaning |
|-------------|---------|
| `AddFieldChange` | Add a new column |
| `AlterFieldChange` | Modify an existing column (type / length / nullable / default) |
| `RenameFieldChange` | Rename a column (requires `DbField.OriginalFieldName`) |
| `AddIndexChange` | Create an index |
| `DropIndexChange` | Drop an index |

It also carries `DescriptionChanges` (MS_Description / extended-property synchronization).

### UpgradePlan (Execution Plan)

[UpgradePlan](../src/Bee.Db/Schema/UpgradePlan.cs) holds the `Mode` (`NoChange` / `Create` / `Alter` / `Rebuild`), the `Stages` (staged SQL), and `Warnings`. You can print the SQL directly:

```csharp
var diff = builder.CompareToDiff("myDb", "ft_employee");
var plan = new TableUpgradeOrchestrator("common").Plan(diff);

Console.WriteLine($"Mode: {plan.Mode}");
foreach (var sql in plan.AllStatements)
    Console.WriteLine(sql);
```

## 4. ALTER vs Rebuild: Which Path Is Taken

### Changes that take the ALTER path (sub-second)

The following changes are handled with `ALTER TABLE` in seconds, without copying data:

- Adding a column
- Type changes within the same family (e.g. `String(50) → String(100)`, `Integer → Long`)
- Toggling nullable, changing default
- Adding / dropping an index
- Narrowing a column length (requires `AllowColumnNarrowing = true`)

### Two cases that trigger a rebuild (SQL Server)

| Category | Examples |
|----------|----------|
| **Cross-family type change** | `String → Integer`, numeric → `Date`, `Boolean → anything else`, `Binary → non-Binary`, `Guid ↔ String` |
| **AutoIncrement state toggle** | Plain column ↔ IDENTITY column (SQL Server `ALTER COLUMN` cannot toggle IDENTITY) |

**Rebuild mechanism**: create a temporary table → `INSERT INTO tmp SELECT FROM original` → drop the old table → rename. For large tables (tens of millions of rows) this can take from minutes to hours and holds a table-level lock for the duration.

### Automatic decision — no developer choice

The orchestrator inspects every change in the `TableSchemaDiff`:

- All changes ALTER-capable → ALTER path
- Any change requires rebuild → the whole table goes through rebuild
- Any change unsupported by the provider → throws and aborts

> The design **intentionally exposes no Strategy option**: callers should not need to decide "ALTER or rebuild this time" — the choice is fully determined by the diff content.

## 5. UpgradeOptions

There is currently a single option:

```csharp
public class UpgradeOptions
{
    /// <summary>
    /// Allow ALTER COLUMN with reduced length / precision (may truncate data).
    /// Default false: narrowing is rejected to avoid silent data loss.
    /// </summary>
    public bool AllowColumnNarrowing { get; set; } = false;
}
```

### What `AllowColumnNarrowing` controls

When the definition specifies a length / precision smaller than the current column in the database:

- Default: **rejected with an exception** to avoid silent truncation
- Enabled: truncation is explicitly accepted and recorded under the plan's `Warnings`

```csharp
var options = new UpgradeOptions { AllowColumnNarrowing = true };
builder.Execute("myDb", "ft_employee", options);
```

> **When to enable**: you have already verified that existing data fits the new length (e.g. `SELECT MAX(LEN(col))`), or the column is brand-new with no data yet. **When not to enable**: for narrowing on a live business table, clean up the data first, then upgrade the schema.

## 6. Renaming a Column (`DbField.OriginalFieldName`)

By default Bee.NET will not infer "this column in the database that has no matching definition is a rename" — it is left in place as an extension column. To rename, mark the previous name **explicitly** in the definition:

```xml
<DbField FieldName="employee_no" OriginalFieldName="emp_no" Caption="Employee No." />
```

The comparer detects the `emp_no → employee_no` rename intent during upgrade and emits a `RenameFieldChange`, executed via `sp_rename` (data preserved).

### Rules

| Scenario | Behaviour |
|----------|-----------|
| DB has the old name `emp_no`, no new name | Run `sp_rename` |
| DB already has the new name `employee_no` | No-op (idempotent, safe to re-run) |
| DB has neither old nor new name | Warning + falls back to adding a new column |
| Multiple cumulative renames across versions | **Not supported**: clear `OriginalFieldName` after each release |

### When to use it

- ✅ Iterating on a brand-new module during development
- ❌ Cumulative renames across multiple already-deployed versions — version skipping is not guaranteed

> Remove `OriginalFieldName` **in the next release** after the rename has shipped, to avoid stale metadata.

## 7. Stages and Transaction Behaviour

On the ALTER path the orchestrator splits the SQL into ordered stages, each running in its own transaction:

```
Stage 1: DropIndexes        Drop indexes whose definition will change
Stage 2: AlterColumns       Rename / retype / resize / re-nullable existing columns
Stage 3: AddColumns         Add new columns
Stage 4: CreateIndexes      Create new indexes; recreate ones dropped in Stage 1
Stage 5: SyncDescriptions   Sync extended properties (MS_Description)
```

### Failure behaviour

- A stage failure → that stage's transaction rolls back, subsequent stages do not run, the exception propagates
- Stages that already committed are **not rolled back**
- The comparer is **idempotent**: after fixing the cause and re-running, completed stages produce no diff and are silently skipped

> The design intentionally avoids a single overall transaction: most databases do not provide reliable rollback for DDL across multiple statements; per-stage transactions plus an idempotent re-run is the more practical strategy.

## 8. Unsupported Scenarios

The following are **outside the scope of automatic upgrade** and must be handled manually:

| Unsupported | Reason / Workaround |
|-------------|---------------------|
| **Drop column** | Continues the extension-field preservation policy; third-party integrations may rely on columns not listed in the definition. Run the SQL manually if you really need to drop |
| **Foreign key** | Framework principle: referential integrity belongs in business objects, not the database layer |
| **Trigger / View** | Same principle — business rules do not live in the database |
| **Multiple renames across versions** | Only single-step rename is guaranteed idempotent; deploy each rename individually |
| **Cross-database schemas** | Only the default schema is supported |
| **Online schema change** | E.g. SQL Server `ALTER INDEX ... WITH (ONLINE = ON)` — run manually |

## 9. Dry-Run and Deployment Practices

### Inspect the SQL before deployment

For large tables (tens of millions of rows), **strongly recommend a dry-run** to confirm the execution mode:

```csharp
var diff = builder.CompareToDiff("myDb", "ft_orders");
var plan = new TableUpgradeOrchestrator("common").Plan(diff);

if (plan.Mode == UpgradeExecutionMode.Rebuild)
{
    // This run will rebuild — schedule a maintenance window
    Console.WriteLine("Rebuild will be triggered:");
    Console.WriteLine(builder.GetCommandText("myDb", "ft_orders"));
}
```

### When to schedule a maintenance window

| Plan.Mode | Impact | Action |
|-----------|--------|--------|
| `NoChange` | None | No action |
| `Create` | New table, no data | Run anytime |
| `Alter` | Sub-second; brief per-column lock at most | Safe in normal hours |
| `Rebuild` | Full table copy, table-level lock | **Schedule a maintenance window** and estimate the copy time first |

### Standard procedure for column narrowing

Do not flip `AllowColumnNarrowing = true` on a live table. Recommended flow:

1. Dry-run to identify which columns will narrow
2. `SELECT COUNT(*) FROM table WHERE LEN(col) > <newLength>` to check for existing data
3. If any rows exceed → clean up the data first (trim, move to a new column, coordinate with the business)
4. Once all data fits → enable the option and upgrade

### Alternatives to rebuild on large tables

If the dry-run shows a rebuild but downtime is unacceptable, consider:

- Split the change: turn a cross-family type change into a multi-step deploy — add a new column → dual-write from the application → backfill → cut over → drop the old column
- Manual online schema change: run provider-specific SQL yourself, bypassing the Bee.NET upgrade
- Run side by side: leave the old table untouched, build new functionality on a new table, retire the old one over time

## 10. References

### Source files
- [TableSchemaBuilder](../src/Bee.Db/Schema/TableSchemaBuilder.cs) — public entry point
- [TableUpgradeOrchestrator](../src/Bee.Db/Schema/TableUpgradeOrchestrator.cs) — Plan / Execute
- [TableSchemaDiff](../src/Bee.Db/Schema/TableSchemaDiff.cs) / [UpgradePlan](../src/Bee.Db/Schema/UpgradePlan.cs)
- [UpgradeOptions](../src/Bee.Db/Schema/UpgradeOptions.cs)
- [DbField.OriginalFieldName](../src/Bee.Definition/Database/DbField.cs)

### Related documents
- [Database Naming Conventions](database-naming-conventions.md)
- [Architecture Overview](architecture-overview.md)
- [Development Cookbook](development-cookbook.md)
- [Development Constraints](development-constraints.md)
- [ADR-005: FormSchema-Driven](adr/adr-005-formschema-driven.md)
