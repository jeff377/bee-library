# Bee.Db

> Database abstraction layer providing dynamic SQL generation, parameterized queries, multi-database support, and IL-based object mapping.

[繁體中文](README.zh-TW.md)

## Architecture Position

- **Layer**: Data Access Layer (infrastructure)
- **Downstream** (dependents): `Bee.Repository`
- **Upstream** (dependencies): `Bee.Definition`, `System.Reflection.Emit.Lightweight`

## Target Frameworks

- `netstandard2.0` -- broad compatibility with .NET Framework 4.6.1+ and .NET Core 2.0+
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

- `IFormCommandBuilder` -- provider-specific CRUD command generation (INSERT, UPDATE, DELETE, SELECT)
- `ICreateTableCommandBuilder` -- provider-specific DDL generation
- `DbFunc` -- database-aware utilities (parameter prefixes, identifier quoting, type inference)
- Built-in providers: SQL Server (with MySQL, SQLite, Oracle parameter/quoting support)

### Schema Introspection

- `TableSchemaBuilder` -- table schema discovery from live database
- `TableSchemaComparer` -- schema diff detection for migration scenarios
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
| `IFormCommandBuilder` | Provider-specific CRUD generation interface |
| `DbConnectionManager` | Connection information registry |
| `DbProviderManager` | Database provider factory resolution |
| `ILMapper<T>` | IL emit-based DataReader-to-object mapping |
| `DbFunc` | Database-aware utility methods |
| `TableSchemaCommandBuilder` | Schema-based IUD command generation |

## Design Conventions

- **Builder Pattern** -- query composition through `ISelectBuilder`, `IFromBuilder`, `IWhereBuilder`, `ISortBuilder` interfaces, each responsible for a single SQL clause.
- **Specification Pattern** -- `DbCommandSpec`, `DbBatchSpec`, and `DataTableUpdateSpec` encapsulate execution intent as data, decoupling command definition from execution.
- **IL Emit Mapping** -- `ILMapper<T>` generates `DynamicMethod` delegates at runtime for zero-reflection DataReader mapping; delegates are cached per query shape.
- **Placeholder Auto-Conversion** -- `DbCommandSpec` accepts both positional (`{0}`, `{1}`) and named (`{Name}`) placeholders, converting them to provider-specific parameter syntax (`@p0`, `:p0`).
- **Provider Pattern** -- database-specific behavior (quoting, parameter prefixes, DDL) is isolated behind provider interfaces, enabling multi-database support.
- **Nullable reference types** enabled (`<Nullable>enable</Nullable>`).

## Directory Structure

```
Bee.Db/
  DbAccess/        # DbAccess, DbCommandSpec, DbBatchSpec, DbConnectionScope,
                   # DbCommandResult, DbParameterSpec, DataTableUpdateSpec
  Manager/         # DbConnectionManager, DbProviderManager, DbConnectionInfo
  Providers/       # IFormCommandBuilder, ICreateTableCommandBuilder,
                   # SelectCommandBuilder
    SqlServer/     # SQL Server-specific implementations
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
