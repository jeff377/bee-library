# Bee.Definition

[繁體中文](README.zh-TW.md)

Core type library for the definition-driven architecture, describing forms, databases, settings, and layouts as structured definitions.

## Architecture Position

**Layer**: Infrastructure

Bee.Definition sits at the foundation of the BeeNET framework, providing the shared type system that all upper layers depend on. It defines the "language" of the definition-driven architecture — every form, database table, UI layout, and system setting is expressed through types declared here.

In the BeeNET dependency graph, this package contains **no business logic and no I/O**. It is a pure type + contract library: interfaces, POCOs, enums, and attributes. This keeps it stable and lightweight — changes here ripple upward through the entire stack, so the API surface evolves conservatively.

- **Upstream dependencies**: Bee.Base, MessagePack
- **Downstream consumers**: Bee.Api.Contracts, Bee.Api.Core, Bee.Repository.Abstractions, Bee.Db, Bee.ObjectCaching, Bee.Business

## Target Framework

| Framework | Purpose |
|-----------|---------|
| `net10.0` | Access to latest runtime optimizations and APIs |

## Key Features

- **FormSchema as the definition hub** — a single FormSchema simultaneously drives UI rendering (FormLayout), database projection (TableSchema), and validation rules, eliminating cross-layer specification drift.
- **Structured filter & sort model** — `FilterCondition` and `FilterGroup` compose a tree-based query model with factory methods (`Equal`, `Contains`, `Between`, `In`, etc.) for type-safe query building.
- **Dual serialization support** — types are annotated for both MessagePack (high-performance binary) and XML serialization, enabling efficient API transport and human-readable configuration files.
- **Provider pattern via BackendInfo** — a static registry (`BackendInfo`) holds runtime providers (encryption, caching, logging, session) resolved by convention, decoupling Definition from concrete implementations.
- **Security contracts** — interfaces like `IAccessTokenValidationProvider` and `IApiEncryptionKeyProvider` define security boundaries without imposing implementation details.
- **DefineType-driven CRUD** — the `DefineType` enum and `DefineFunc` utility map definition categories to CLR types, enabling generic load/save through `IDefineAccess` and `IDefineStorage`.
- **Centralized settings model** — `SystemSettings`, `DatabaseSettings`, `ProgramSettings`, and `MenuSettings` provide a typed configuration surface that replaces ad-hoc key-value lookups.

## Key Public APIs

| Type | Role |
|------|------|
| `FormSchema` | Central definition hub — describes a form's tables, fields, and metadata |
| `TableSchema` / `DbField` | Database projection — column types, indices, constraints |
| `FormLayout` / `LayoutGroup` / `LayoutItem` | UI projection — field arrangement and grouping |
| `FilterCondition` / `FilterGroup` | Composable query filter tree |
| `SortField` / `SortFieldCollection` | Query sort descriptors |
| `SystemSettings` / `DatabaseSettings` / `ProgramSettings` | Configuration definition types |
| `BackendInfo` | Static provider registry for runtime services |
| `SessionInfo` / `SessionUser` | Session and user context |
| `IDefineAccess` / `IDefineStorage` | Definition load/save contracts |
| `IBusinessObjectProvider` | Factory contract for business object creation |
| `DefineFunc` | Utility for DefineType-to-CLR-type resolution |
| `BackendDefaultTypes` | String constants for default provider type names |
| `DefineType` | Enum categorizing all definition kinds (FormSchema, TableSchema, Settings, etc.) |

## Design Conventions

- **MessagePack `[Key]` + XML `[XmlElement]` dual annotation** — every serializable property carries both attributes to support binary and XML channels.
- **Provider Pattern** — `BackendInfo` exposes static properties typed to interfaces (e.g., `ILogWriter`, `IApiEncryptionKeyProvider`). Concrete types are registered at startup via `BackendDefaultTypes` constants.
- **Factory methods on FilterCondition** — prefer `FilterCondition.Equal(...)` over `new FilterCondition { ... }` for readability and consistency.
- **DefineType enum as dispatch key** — `DefineFunc.GetDefineType()` maps enum values to CLR types, enabling generic definition CRUD without hard-coding type references.
- **Immutable defaults** — `BackendInfo` properties initialize to safe defaults (`NullLogWriter`, empty arrays) so the system never encounters null providers.
- **XML doc comments in English** — all public APIs carry English XML documentation to ensure IntelliSense readability for NuGet consumers worldwide.
- **Nullable Reference Types enabled** — the project opts into NRT (`<Nullable>enable</Nullable>`) and treats warnings as errors, enforcing null-safety at compile time.

## Directory Structure

```
Bee.Definition/
  Attributes/       Access control attributes (ApiAccessControl, ExecFuncAccessControl)
  Collections/      ListItem, Parameter, PropertyCollection
  Database/         TableSchema, DbField, DbFieldCollection, TableSchemaIndex,
                    DatabaseType, FieldType, DbAccessAnomalyLogLevel, DbUpgradeAction
  Documents/        IExcelHelper (document-format helper abstraction)
  Filters/          FilterCondition, FilterGroup, FilterNode, FilterNodeKind,
                    ComparisonOperator, LogicalOperator
  Forms/            FormSchema, FormField, FormFieldCollection, FormTable
  Identity/         SessionInfo, SessionUser, UserInfo, IUserInfo, ISessionInfoService
  Layouts/          FormLayout, LayoutGroup, LayoutItem,
                    ControlType, ColumnControlType, GridControlAllowActions, SingleFormMode,
                    IUIControl, IBindFieldControl, IBindTableControl
  Logging/          ILogWriter, LogEntry, LogEntryType, LogOptions
  Security/         IAccessTokenValidationProvider, IApiEncryptionKeyProvider,
                    MasterKeyProvider, MasterKeySourceType,
                    ApiAccessRequirement, ApiProtectionLevel
  Serialization/    Custom MessagePack formatters
  Settings/         SystemSettings, DatabaseSettings, ProgramSettings, MenuSettings, DbSchemaSettings
  Sorting/          SortField, SortFieldCollection, SortDirection
  Storage/          IDefineAccess (and friends)
  (root)            Cross-cutting infrastructure:
                    BackendInfo, BackendDefaultTypes, DefineFunc, DefinePathInfo,
                    DefineType, GlobalEvents, PropertyCategories,
                    SysFields, SysFuncIDs, SysProgIds, SystemActions,
                    ApplicationType, InitializeOptions,
                    IBusinessObjectProvider, ICacheDataSourceProvider, IEnterpriseObjectService
```

The namespace layout follows the design principles in [ADR-008](../../docs/adr/adr-008-bee-db-namespace-layout.md):
syntax/model/factory separation; concrete content grouped by domain (`Database`, `Filters`, `Forms`, `Layouts`, etc.); the root layer reserved for cross-cutting infrastructure (system constants, global service-locator interfaces, framework-wide enums).
