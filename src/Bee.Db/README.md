# Bee.Db

> Database abstraction layer providing dynamic SQL generation, parameterized queries, multi-database support, and IL-based object mapping.

[繁體中文](README.zh-TW.md)

## Architecture Position

- **Layer**: Data Access Layer (infrastructure)
- **Downstream** (dependents): `Bee.Repository`
- **Upstream** (dependencies): `Bee.Definition`

## Target Framework

- `net10.0` -- access to modern runtime APIs and performance improvements

## Key Features

### Database Access

- `DbAccess` -- primary entry point for executing queries, batch commands, and DataTable updates
- `DbConnectionScope` -- scoped connection lifetime management
- `DbCommandSpec` -- parameterized command specification supporting positional (`{0}`) and named (`{Name}`) placeholders with automatic conversion
- `DbBatchSpec` -- batch execution with optional transaction wrapping and configurable isolation levels

### Connection & Provider Management

- `DbConnectionManager` -- centralized connection information registry
- `DbProviderManager` -- database provider factory resolution
- `DbConnectionInfo` -- connection metadata (connection string, database type, provider)

### Query Composition

- `SelectCommandBuilder` -- builds SELECT commands from `FormSchema` definitions
- `ISelectBuilder` / `IFromBuilder` / `IWhereBuilder` / `ISortBuilder` -- composable builder interfaces for SELECT, FROM, WHERE, and ORDER BY clauses
- `SelectContext` -- query context tracking field mappings and table joins
- `WhereBuilder` -- filter-to-SQL translation with parameterized output

### Multi-Database Support

The framework routes SQL generation and schema reading by `DatabaseType` through a dialect factory layer:

- `IDialectFactory` -- per-provider factory exposing `IFormCommandBuilder`, `ICreateTableCommandBuilder`, `ITableAlterCommandBuilder`, `ITableRebuildCommandBuilder`, `ITableSchemaProvider`, and `GetDefaultValueExpression(FieldDbType)`
- `DbDialectRegistry` -- maps `DatabaseType` to its `IDialectFactory` (mirrors how `DbProviderManager` maps to ADO.NET `DbProviderFactory`); registration is explicit and performed by the host
- `DbFunc` -- database-aware utilities (parameter prefixes, identifier quoting, type inference) keyed by `DatabaseType`
- Built-in dialect implementations:
  - **SQL Server** (`Providers/SqlServer/`) -- full support: form CRUD, CREATE/ALTER/REBUILD DDL, schema introspection
  - **PostgreSQL** (`Providers/PostgreSql/`) -- full support (since v4.2.x): form SELECT, CREATE/ALTER/REBUILD DDL, schema introspection via `information_schema` + `pg_catalog`
  - MySQL / SQLite / Oracle -- parameter prefix and identifier quoting are pre-registered in `DbFunc` so connection-level operations work; SQL generation classes are not yet implemented

`Bee.Db` itself has zero ADO.NET driver dependencies; the driver lives in the host application.

### Provider Registration

The host application enables a database by registering two things at startup: the ADO.NET `DbProviderFactory` (for connections) and the `IDialectFactory` (for SQL generation). Any combination is allowed; only register what your app actually uses.

```csharp
using Bee.Db.Manager;
using Bee.Db.Providers.SqlServer;
using Bee.Db.Providers.PostgreSql;
using Bee.Definition;
using Microsoft.Data.SqlClient;
using Npgsql;

// SQL Server
DbProviderManager.RegisterProvider(DatabaseType.SQLServer, SqlClientFactory.Instance);
DbDialectRegistry.Register(DatabaseType.SQLServer, new SqlDialectFactory());

// PostgreSQL
DbProviderManager.RegisterProvider(DatabaseType.PostgreSQL, NpgsqlFactory.Instance);
DbDialectRegistry.Register(DatabaseType.PostgreSQL, new PgDialectFactory());

// Configure connection items in DatabaseSettings (typically loaded from XML);
// each item picks its DatabaseType and one of the registered providers.
```

A `DatabaseItem` carries `Id`, `DatabaseType`, and `ConnectionString`. The framework looks up the provider/dialect by `DatabaseType` whenever a `DbAccess` / `TableSchemaBuilder` / `TableUpgradeOrchestrator` is created with that item's `Id`. PostgreSQL connection string template:

```
Host=localhost;Port=5432;Database={@DbName};Username={@UserId};Password={@Password}
```

### Schema Introspection & Upgrade

- `ITableSchemaProvider` -- per-provider schema reader (SQL Server uses `sys.*`, PostgreSQL uses `information_schema` + `pg_catalog`)
- `TableSchemaBuilder` -- compares the defined schema against the live database and produces or executes the upgrade commands
- `TableSchemaComparer` -- structured diff (`TableSchemaDiff`) listing add/alter/drop changes
- `TableUpgradeOrchestrator` -- ALTER-based upgrade with rebuild fallback when ALTER cannot apply all changes; routes through the dialect factory
- `ITableAlterCommandBuilder` / `ITableRebuildCommandBuilder` -- per-provider DDL generation for in-place ALTER and full table rebuild
- `TableSchemaCommandBuilder` -- generates IUD commands from `TableSchema`

### IL-Based Object Mapping

- `ILMapper<T>` -- high-performance `DbDataReader`-to-object mapping via IL emit
- Automatic column-to-property matching (case-insensitive)
- Per-query-shape delegate caching with `ConcurrentDictionary`
- Supports `List<T>` and `IEnumerable<T>` (deferred) materialization

### Logging & Diagnostics

- `DbAccessLogger` -- command execution logging
- `DbLogContext` -- slow query tracking and diagnostics context

## Key Public APIs

| Class / Interface | Purpose |
|-------------------|---------|
| `DbAccess` | Execute queries, batch commands, and DataTable updates |
| `DbCommandSpec` | Parameterized command specification with placeholder auto-conversion |
| `DbBatchSpec` | Batch command execution with transaction support |
| `SelectCommandBuilder` | FormSchema-driven SELECT command building |
| `IDialectFactory` | Per-provider factory for SQL/schema builders (SQL Server, PostgreSQL) |
| `IFormCommandBuilder` | Provider-specific CRUD generation interface |
| `ITableSchemaProvider` | Provider-specific live-database schema reader |
| `DbDialectRegistry` | `DatabaseType` → `IDialectFactory` registry |
| `DbConnectionManager` | Connection information registry |
| `DbProviderManager` | ADO.NET `DbProviderFactory` resolution |
| `ILMapper<T>` | IL emit-based DataReader-to-object mapping |
| `DbFunc` | Database-aware utility methods |
| `TableSchemaCommandBuilder` | Schema-based IUD command generation |

## Design Conventions

- **Builder Pattern** -- query composition through `ISelectBuilder`, `IFromBuilder`, `IWhereBuilder`, `ISortBuilder` interfaces, each responsible for a single SQL clause.
- **Specification Pattern** -- `DbCommandSpec`, `DbBatchSpec`, and `DataTableUpdateSpec` encapsulate execution intent as data, decoupling command definition from execution.
- **IL Emit Mapping** -- `ILMapper<T>` generates `DynamicMethod` delegates at runtime for zero-reflection DataReader mapping; delegates are cached per query shape.
- **Placeholder Auto-Conversion** -- `DbCommandSpec` accepts both positional (`{0}`, `{1}`) and named (`{Name}`) placeholders, converting them to provider-specific parameter syntax (`@p0`, `:p0`).
- **Provider Pattern** -- database-specific behavior (quoting, parameter prefixes, DDL, schema introspection) is isolated behind provider interfaces; routing is centralized in `DbDialectRegistry`. Hosts register the dialects they actually use; `Bee.Db` does not auto-register any of them.
- **Nullable reference types** enabled (`<Nullable>enable</Nullable>`).

## Directory Structure

```
Bee.Db/
  DbAccess/        # DbAccess, DbCommandSpec, DbBatchSpec, DbConnectionScope,
                   # DbCommandResult, DbParameterSpec, DataTableUpdateSpec
  Manager/         # DbConnectionManager, DbProviderManager, DbConnectionInfo,
                   # DbDialectRegistry
  Providers/       # IDialectFactory, IFormCommandBuilder, ICreateTableCommandBuilder,
                   # ITableAlterCommandBuilder, ITableRebuildCommandBuilder,
                   # ITableSchemaProvider, SelectCommandBuilder
    SqlServer/     # SQL Server-specific implementations
    PostgreSql/    # PostgreSQL-specific implementations
  Query/           # Query component builders
    Context/       # SelectContext, QueryFieldMapping, TableJoin
    From/          # IFromBuilder, FromBuilder
    Select/        # ISelectBuilder, SelectBuilder
    Sort/          # ISortBuilder, SortBuilder
    Where/         # IWhereBuilder, WhereBuilder, IParameterCollector
  Schema/          # TableSchemaBuilder, TableSchemaComparer
  Logging/         # DbAccessLogger, DbLogContext
  *.cs (root)      # DbFunc, ILMapper, DbCommandKind, JoinType,
                   # CommandTextVariable, TableSchemaCommandBuilder
```
