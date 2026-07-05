# Development Constraints and Anti-Patterns

[繁體中文](development-constraints.zh-TW.md)

> This document lists the framework's design constraints and forbidden practices, as a reference for AI coding tools to avoid generating code that violates framework conventions.
> For security-related rules, see [Security Rules](../.claude/rules/security.md).

## Initialization Order Constraints

The framework registers itself in the standard `IServiceCollection` DI container; framework services are resolved through ctor injection rather than static entry points. Host startup must run the following five steps in order:

1. `var paths = new PathOptions { DefinePath = "..." }` — locate definition files
2. `var settings = SystemSettingsLoader.Load(paths)` — read `SystemSettings.xml` (boot-time only; runtime cached access goes through DI-resolved `IDefineAccess`)
3. `SysInfo.Initialize(settings.CommonConfiguration)` — process-wide debug flag / payload options
4. `services.AddBeeFramework(settings.BackendConfiguration, paths)` — register framework services (extension from `Bee.Hosting`)
5. `services.BuildServiceProvider()` followed by `app.UseBeeFramework()` (ASP.NET Core hosts only — non-web hosts feed the resulting `IServiceProvider` to `ApiClientInfo.LocalServiceProvider` for near-end mode instead)

See [development-cookbook.md § Framework Initialization Order](development-cookbook.md#framework-initialization-order) for the canonical reference.

### Consequences of Violation

- Resolving framework services before `AddBeeFramework` → DI container throws `InvalidOperationException` (service not registered)
- Calling `SystemSettingsLoader.Load` on a path with no `SystemSettings.xml` → throws `FileNotFoundException`
- Constructing `DbAccess` without an `IDbConnectionManager` argument → compile error (every constructor of `DbAccess` requires `IDbConnectionManager`); obtain `DbAccess` instances through DI-injected `IDbAccessFactory.Create(databaseId)`

### Reference Example

`tests/Bee.Tests.Shared/TestProcessBootstrap.cs` demonstrates the correct initialization order for the test process.

## Definition Data Immutability After Init

After framework initialization, **all server-side cached definition data is
read-only and must not be mutated at runtime**. Every session shares the same
in-memory instance through `IDefineAccess` / `ICacheContainer`, and the cache
is process-wide; per-session adjustments leak to every other session, and
concurrent mutations race.

### Scope

Anything reached through `IDefineAccess.GetX(...)` (which is backed by the
`ICacheContainer` slot of the same name):

- `FormSchema`, `FormLayout`, `TableSchema`
- `SystemSettings`, `DatabaseSettings`, `ProgramSettings`, `DbCategorySettings`
- `LanguageResource`
- `SessionInfo` is the deliberate exception — it is a per-session entity, not
  shared definition data, and the cache key is the access token

### Forbidden Patterns

| Pattern | Why bad |
|---------|---------|
| `cachedSchema.Caption = "..."` | Mutates shared instance → cross-session leak / race |
| `XmlCodec.Serialize(cachedInstance)` as a "free" deep-clone | `IObjectSerialize` lifecycle flips `SerializeState` on the source → thread race + `IsSerializeEmpty` mis-behaves under load |
| Storing per-session state in `Tag` / extension properties on a cached object | `Tag` is also process-shared |
| Using `MasterTable` / collection setters to swap children on a cached instance | Same race surface |

### Correct Approach

- **Need a per-session view (e.g. localized schema)?** Clone first, mutate the clone:
  ```csharp
  var customised = cachedSchema.Clone();
  FormSchemaLocalizer.Localize(customised, sessionLang);
  return customised;
  ```
- **Persistent changes** go through `IDefineAccess.SaveX(...)` which:
  1. Writes to backing storage
  2. Invalidates the cache slot so the next `GetX` rebuilds from storage
- **Need a deep copy?** Use the type's `Clone()` method (provided on
  `FormSchema` / `FormTable` / `FormField` / `TableSchema` /
  `DatabaseSettings` etc.). Never substitute `XmlCodec` round-trip — it
  mutates state on the source.

### Why This Matters

Bee.NET is designed for multi-tenant ASP.NET Core / Blazor Server hosts where a
single process serves many concurrent sessions, each potentially in a different
language and tenant context. The cache is a singleton; lifecycle-aware
serialization hooks make even read-only operations like `XmlCodec.Serialize`
non-idempotent against shared state. The invariant **"definition data is
immutable after init"** is the single rule that lets every session safely
share the same cached instances without coordination.

## Cross-Layer Forbidden Practices

| Forbidden | Reason | Correct Approach |
|-----------|--------|------------------|
| API layer directly references the Repository layer | Violates layered architecture | Access indirectly through a Business Object |
| Business Object directly creates a `DbConnection` | Bypasses connection management and logging | Use the `DbAccess` class |
| BO references `Bee.Db` (`Bee.Business.csproj` has no `ProjectReference` to `Bee.Db`) | BO is a thin shell over business logic; data access belongs to Repository | FormSchema-driven CRUD → `IDataFormRepository`; custom queries → ad-hoc bo repo with `IDbAccessFactory` |
| BO hard-codes a `databaseId` string or reads `SessionInfo.CompanyId` / `CompanyInfo` directly | Couples BO to the routing implementation; breaks when deployments change | Use `BusinessObject.ResolveDatabaseId(DbScope)` (custom bo repo) or `CreateDataFormRepository(progId)` (FormSchema CRUD); the helpers delegate to `IRepositoryDatabaseRouter` which is the single source of truth |
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

`JsonRpcExecutor` forwards the following exception types to the client unchanged, mapping them to `JsonRpcErrorCode.UserMessage` (`-32099`):

- `UserMessageException` (**preferred**)
- `UnauthorizedAccessException`
- `ArgumentException` (including `ArgumentNullException`, `ArgumentOutOfRangeException`)
- `InvalidOperationException`
- `NotSupportedException`
- `FormatException`
- `JsonRpcException`

All other exceptions are masked as `"Internal server error"` in production (mapped to `JsonRpcErrorCode.InternalError` `-32000`) to prevent leakage of internal details.

### When to Use Each Type

| Exception type | Usage |
|----------------|-------|
| `UserMessageException` | **Preferred**: any message intended to be shown to the end user (business-rule violation, validation failure, workflow interruption). |
| `ArgumentException` | API contract violation — the caller passed a bad parameter (null, malformed, out of range). **Note**: kept on the whitelist for now; new code should prefer `UserMessageException`. |
| `InvalidOperationException` | Object state error or call ordering issue. **Note**: kept on the whitelist for now; new code should prefer `UserMessageException`. |
| `UnauthorizedAccessException` | Authentication / authorization failure. |
| `NotSupportedException` | Feature not implemented or not applicable in the current context. |
| `FormatException` | String or data format cannot be parsed. |
| `JsonRpcException` | Protocol-level errors of the API framework itself (HTTP status / JSON-RPC error code). |

### Client-Side Behaviour

`ApiConnector.FinalizeResponse` rebuilds exceptions based on `JsonRpcError.Code`:

- `code == UserMessage` → throws `UserMessageException(message)` with the original message verbatim (no prefix), ready to be shown to the end user
- Other codes → throws `InvalidOperationException($"API error: {code} - {message}")`, preserving the protocol-level debugging info

Recommended client-side catch order:

```csharp
try
{
    var result = await connector.SomeAction(args);
}
catch (UserMessageException ex)
{
    // Business message: show directly to the user
    ShowMessage(ex.Message);
}
catch (Exception ex)
{
    // System error: log and show a generic error page
    LogError(ex);
}
```

### Evolution Direction

The long-term goal is to make `UserMessageException` the **only** channel for user-facing messages, returning BCL exceptions to their BCL semantics (call errors, state errors, program bugs). The whitelist currently keeps BCL exceptions as a **gradual transition**:

- **New code**: always use `UserMessageException` for user-facing messages
- **Old code**: when touching existing code, convert `InvalidOperationException("xxx")` / `ArgumentException("xxx")` to `UserMessageException("xxx")`
- **Whitelist reduction**: once a given BCL exception has zero user-facing usages in production business objects, an independent plan evaluates removing it from the whitelist
- **End state**: the whitelist contains only `UserMessageException` and `JsonRpcException`

### Extension Paths

- Need more properties (e.g. `Code`, `Details`, structured data): add nullable properties on `UserMessageException` (backward compatible)
- Need to classify errors (e.g. `NotFoundException` for HTTP 404): subclass `UserMessageException`
- i18n payload: `JsonRpcError.Data` is reserved as the slot; the overall mechanism is to be designed in an independent plan

### Design Intent

- Prevent leakage of internal implementation details to the client
- Provide an independent channel for business messages, type-separated from "real program errors", to ease future logging / monitoring routing

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
