# Development Constraints and Anti-Patterns

[繁體中文](development-constraints.zh-TW.md)

> This document lists the framework's design constraints and forbidden practices, as a reference for AI coding tools to avoid generating code that violates framework conventions.
> For security-related rules, see [Security Rules](../.claude/rules/security.md).

## Initialization Order Constraints

The framework registers itself in the standard `IServiceCollection` DI container; framework services are resolved through ctor injection rather than static entry points. Host startup must run the following four steps in order:

1. `var paths = new PathOptions { DefinePath = "..." }` — locate definition files
2. `var settings = SystemSettingsLoader.Load(paths)` — read `SystemSettings.xml` (boot-time only; runtime cached access goes through DI-resolved `IDefineAccess`)
3. `SysInfo.Initialize(settings.CommonConfiguration)` — process-wide debug flag / payload options
4. `services.AddBeeFramework(settings.BackendConfiguration, paths)` — register framework services
5. `services.BuildServiceProvider()` followed by `app.UseBeeFramework()` (ASP.NET only)

See [development-cookbook.md § Framework Initialization Order](development-cookbook.md#framework-initialization-order) for the canonical reference.

### Consequences of Violation

- Resolving framework services before `AddBeeFramework` → DI container throws `InvalidOperationException` (service not registered)
- Calling `SystemSettingsLoader.Load` on a path with no `SystemSettings.xml` → throws `FileNotFoundException`
- Constructing `DbAccess` without an `IDbConnectionManager` argument (the single-arg legacy ctor was removed in Phase 7) → compile error; obtain `DbAccess` instances through DI-injected `IDbAccessFactory.Create(databaseId)`

### Reference Example

`tests/Bee.Tests.Shared/TestProcessBootstrap.cs` demonstrates the correct initialization order for the test process.

## Cross-Layer Forbidden Practices

| Forbidden | Reason | Correct Approach |
|-----------|--------|------------------|
| API layer directly references the Repository layer | Violates layered architecture | Access indirectly through a Business Object |
| Business Object directly creates a `DbConnection` | Bypasses connection management and logging | Use the `DbAccess` class |
| Client side resolves Repository services from a DI container | Server-only | Call the API via `ApiConnector` |
| Skipping the Payload Pipeline order | Breaks encryption / decryption consistency | Maintain Serialize → Compress → Encrypt |
| BO returns API types directly | BO must not depend on API serialization formats | Return BO types; `ApiOutputConverter` maps them automatically by naming convention |

## ExecFunc Development Constraints

### Method Signature Rules

ExecFunc handler methods must follow these rules:

- **Must** be `public` methods (reflection invocation requires it)
- **Must** be non-generic (`GetMethod()` does not support generic resolution)
- **Fixed signature**: `void MethodName(ExecFuncArgs args, ExecFuncResult result)`
- **FuncId maps to method name**, case-sensitive
- Methods without `[ExecFuncAccessControl]` default to `Authenticated`

### Access Control Declaration

```csharp
// Anonymous access
[ExecFuncAccessControl(ApiAccessRequirement.Anonymous)]
public void PublicMethod(ExecFuncArgs args, ExecFuncResult result) { }

// Login required (default behavior; the attribute can be omitted)
[ExecFuncAccessControl(ApiAccessRequirement.Authenticated)]
public void SecureMethod(ExecFuncArgs args, ExecFuncResult result) { }
```

## Exception Handling Rules

### Client-Visible Exception Types

`JsonRpcExecutor` only forwards the following exception types to the client unchanged:

- `UnauthorizedAccessException`
- `ArgumentException` (including `ArgumentNullException`, `ArgumentOutOfRangeException`)
- `InvalidOperationException`
- `NotSupportedException`
- `FormatException`
- `JsonRpcException`

All other exceptions are converted to `"Internal server error"` in production. In development (`IsDevelopment`), the full error message is returned.

### Design Intent

- Prevent leakage of internal implementation details to the client
- For specific error messages, use the types listed above or a custom `JsonRpcException`

## FormSchema Design Constraints

- FormSchema is **read-only** at runtime; fields cannot be added dynamically
- `IFormCommandBuilder` (in `Bee.Db.Dml`) is the contract for CRUD command construction; the 5 DB providers each implement it independently (`SqlFormCommandBuilder` / `PgFormCommandBuilder` / `MySqlFormCommandBuilder` / `OracleFormCommandBuilder` / `SqliteFormCommandBuilder`), with no common base class
- Manually adjusted parts of TableSchema (precision, indexes, default values) are preserved when FormSchema is updated
- `FormTable.DbTableName`: optional field; when empty, `FormTable.TableName` is used as the physical table name. Naming should follow the [Database Naming Conventions](database-naming-conventions.md) (lowercase + snake_case)

## Type Safety Constraints

### MessagePack Type Whitelist

`SafeTypelessFormatter` and `SafeMessagePackSerializerOptions` enforce a type whitelist:

- Only registered types can be deserialized
- Unregistered types throw `MessagePackSerializationException`
- New API types must be registered in `ApiContractRegistry`

### API Contract Naming Convention (Mandatory)

API Request / Response and BO Args / Result types must follow naming conventions so that `ApiOutputConverter` can automatically map BO return values to API types (see [ADR-007](adr/adr-007-convention-based-type-resolution.md)):

| Layer | Input | Output |
|-------|-------|--------|
| BO (`Bee.Business`) | `{Action}Args` | `{Action}Result` |
| API (`Bee.Api.Core`) | `{Action}Request` | `{Action}Response` |
| Contract (`Bee.Api.Contracts`) | `I{Action}Request` | `I{Action}Response` |

- Types deviating from the naming convention will not be auto-converted; BO return values will pass through to the client and cause type errors
- `ApiContractRegistry` is still used as a MessagePack Typeless serialization whitelist for Encoded / Encrypted formats, but **manual `Register` calls are no longer required** to set up response mapping

## Account Security Constraints

- `LoginAttemptTracker` default policy: lock the account for 15 minutes after 5 consecutive failed login attempts
- During lockout, all login attempts are rejected directly without checking the password
- Successful login resets the failure counter

## Database Schema Constraints

The framework's schema definition (`TableSchema`) and upgrade mechanism (`TableUpgradeOrchestrator`) **deliberately do not support** the following database-level elements:

- **Foreign Key constraints**
- **Triggers**
- **Views**

### Design Principle

Referential integrity, business rules, and derived data are handled by **the application code (Business Object layer)**; the schema definition only describes table structure (columns, indexes, primary keys).

### Rationale

- Database-layer dependencies make cross-provider support and schema upgrades extremely costly
- In real-world ERP scenarios, the BO layer can fully express business rules; pushing them down to the DB is unnecessary
- Upgrade flows (adding / removing columns, changing types) avoid cascading concerns such as FK suspension, trigger rebuilds, or view refreshes

### If You Truly Need FK / Trigger / View

Maintain them with project-specific migration scripts outside the framework. The upgrade pipeline will not produce corresponding DDL and provides no compatibility guarantees.
