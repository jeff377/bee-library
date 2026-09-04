# Bee.Definition

[繁體中文](README.zh-TW.md)

Core type library for the definition-driven architecture, describing forms, databases, settings, and layouts as structured definitions.

## Architecture Position

**Layer**: Infrastructure

Bee.Definition sits at the foundation of the BeeNET framework, providing the shared type system that all upper layers depend on. It defines the "language" of the definition-driven architecture — every form, database table, UI layout, and system setting is expressed through types declared here.

It holds no business logic: interfaces, POCOs, enums and attributes. Changes here ripple upward through the entire stack, so the API surface evolves conservatively.

It is **not** free of I/O today, and the difference matters if you are reasoning about layering.
`Storage/` (the file-backed definition storage), `Security/MasterKeyProvider`, `PathOptions` /
`CustomizeOnlyPathOptions` and `Defaults` read and write definition files from disk. Moving them out
is a data migration rather than a refactor — `BackendDefaultTypes.DefineStorage` names those types in
every existing deployment's `SystemSettings.xml`, so relocating them needs a compatibility mapping for
the old type names — and they stay here until that is done.

- **Layer**: foundation — the shared type system every upper layer speaks.
- **Dependencies**: locked to an explicit allowlist by the **BEE9001** build gate. Anything added here
  is inherited by every consumer of the framework, so widening the allowlist is a deliberate decision
  recorded in [ADR-038](../../docs/adr/adr-038-definition-dependency-boundary.md). The current graph
  lives in the [dependency map](../../docs/dependency-map.md) — this file does not restate it, because
  a second copy is a second thing to keep right.

## Target Framework

| Framework | Purpose |
|-----------|---------|
| `net10.0` | Access to latest runtime optimizations and APIs |

## Key Features

- **FormSchema as the definition hub** — a single FormSchema simultaneously drives UI rendering (FormLayout), database projection (TableSchema), and validation rules, eliminating cross-layer specification drift.
- **Structured filter & sort model** — `FilterCondition` and `FilterGroup` compose a tree-based query model with factory methods (`Equal`, `Contains`, `Between`, `In`, etc.) for type-safe query building.
- **Serializable without a transport dependency** — types carry XML annotations for the definition files on disk, and nothing else. Their binding to the API wire lives in `Bee.Api.Core` as hand-written formatters, so the definition layer never takes a dependency on a transport format ([ADR-036](../../docs/adr/adr-036-wire-serialization-externalized.md)).
- **DI-injected runtime services** — interfaces such as `IDefineAccess`, `ISessionInfoService`, `IDatabaseSettingsProvider`, `IApiEncryptionKeyProvider`, and `IAccessTokenValidator` are defined here and registered through `AddBeeFramework` at host startup, decoupling Definition from concrete implementations.
- **Security contracts** — interfaces like `IAccessTokenValidator` and `IApiEncryptionKeyProvider` define security boundaries without imposing implementation details.
- **DefineType-driven CRUD** — the `DefineType` enum and the `DefineTypeExtensions.ToClrType()` extension method map definition categories to CLR types, enabling generic load/save through `IDefineAccess` and `IDefineStorage`.
- **Centralized settings model** — `SystemSettings`, `DatabaseSettings`, `ProgramSettings`, and `MenuSettings` provide a typed configuration surface that replaces ad-hoc key-value lookups. `ProgramSettings` is the framework's type registry: one flat entry per progId, binding it to a business object (`ProgramItem.BusinessObject`) and a repository (`ProgramItem.Repository`); either left empty falls back to the framework default. `MenuSettings` owns the navigation menu, which the registry no longer carries (see [ADR-034](../../docs/adr/adr-034-progid-type-registry.md)).
- **Tenant customization overlay** — `ICustomizeDefineReader` + `CustomizeOnlyStorage` provide a per-tenant read-only override layer over base definitions, for Language / FormLayout / ProgramSettings / MenuSettings only, driven by `SessionInfo.CustomizeId`. The base cache is never mutated; lookups overlay per key / progId / whole-file without merging (see [ADR-016](../../docs/adr/adr-016-multitenant-customization-overlay.md)).

## Key Public APIs

| Type | Role |
|------|------|
| `FormSchema` | Central definition hub — describes a form's tables, fields, and metadata |
| `TableSchema` / `DbField` | Database projection — column types, indices, constraints |
| `FormLayout` / `LayoutSection` / `LayoutField` | UI projection — field arrangement and grouping |
| `FilterCondition` / `FilterGroup` | Composable query filter tree |
| `SortField` / `SortFieldCollection` | Query sort descriptors |
| `SystemSettings` / `DatabaseSettings` / `ProgramSettings` | Configuration definition types |
| `IDatabaseSettingsProvider` | DI service exposing the current `DatabaseSettings` snapshot and lookup helpers |
| `SessionInfo` / `SessionUser` | Session and user context |
| `IDefineAccess` / `IDefineStorage` | Definition load/save contracts |
| `ICustomizeDefineReader` | Tenant customization-override reader (Language / FormLayout / ProgramSettings / MenuSettings) |
| `CustomizeOnlyStorage` / `CustomizeOnlyPathOptions` | Strict read-only storage for the customization layer (`{CustomizePath}/{customizeId}/...`, missing file → null) |
| `IBusinessObjectFactory` | Factory contract for business object creation |
| `DefineTypeExtensions.ToClrType()` | Extension method for DefineType-to-CLR-type resolution |
| `BackendDefaultTypes` | String constants for default provider type names |
| `DefineType` | Enum categorizing all definition kinds (FormSchema, TableSchema, Settings, etc.) |

## Design Conventions

- **XML annotations only** — a serializable property carries `[XmlElement]` / `[XmlAttribute]` and, where a member must stay off the JSON wire, `[JsonIgnore]`. Both are BCL vocabulary. **Do not add MessagePack attributes**: they would put a transport package on the dependency surface of every consumer, which is exactly what BEE9001 refuses.
- **Replaceable services via XML registry** — `BackendComponents` (in `SystemSettings.xml`) declares the concrete type name for each replaceable interface (`IDefineAccess`, `ISessionInfoService`, etc.). `AddBeeFramework` reads the registry at startup and registers the configured types in the DI container; `BackendDefaultTypes` holds the framework-default type-name constants.
- **Factory methods on FilterCondition** — prefer `FilterCondition.Equal(...)` over `new FilterCondition { ... }` for readability and consistency.
- **DefineType enum as dispatch key** — `DefineTypeExtensions.ToClrType()` maps enum values to CLR types, enabling generic definition CRUD without hard-coding type references.
- **XML doc comments in English** — all public APIs carry English XML documentation to ensure IntelliSense readability for NuGet consumers worldwide.
- **Nullable Reference Types enabled** — the project opts into NRT (`<Nullable>enable</Nullable>`) and treats warnings as errors, enforcing null-safety at compile time.

## Directory Structure

```
Bee.Definition/
  Attributes/       Access control attributes (ApiAccessControl, ExecFuncAccessControl)
  Collections/      ListItem, Parameter, PropertyCollection
  Database/         TableSchema, DbField, DbFieldCollection, DbTableIndex,
                    DatabaseType, FieldType, DbAccessAnomalyLogLevel, DbUpgradeAction
  Filters/          FilterCondition, FilterGroup, FilterNode, FilterNodeKind,
                    ComparisonOperator, LogicalOperator
  Forms/            FormSchema, FormField, FormFieldCollection, FormTable
  Identity/         SessionInfo, SessionUser, UserInfo, IUserInfo, ISessionInfoService
  Layouts/          FormLayout, LayoutSection, LayoutField, LayoutGrid, LayoutColumn,
                    ControlType, GridControlAllowActions, SingleFormMode, FormEditModes,
                    IUIControl, IBindFieldControl, IBindTableControl
  Logging/          IAuditLogWriter, AuditEntry, LoginAuditEntry, AccessAuditEntry,
                    ChangeAuditEntry, ApiAnomalyEntry, DbAnomalyEntry, LogOptions
  Security/         IAccessTokenValidator, IApiEncryptionKeyProvider,
                    MasterKeyProvider, MasterKeySourceType,
                    ApiAccessRequirement, ApiProtectionLevel
  Settings/         SystemSettings, DatabaseSettings, ProgramSettings, MenuSettings, DbCategorySettings
  Attributes/       ApiAccessControlAttribute and the other declarative markers
  Collections/      KeyCollection-based collection types (Parameter, Property, ...)
  Customization/    Tenant customization overlay
  Defaults/         The definition files shipped with the framework (embedded resources)
  Language/         ILanguageService, LanguageResource, FormSchemaLocalizer
  Organization/     DepartmentTree, EmployeeContext
  Paging/           PagingInfo and friends
  Sorting/          SortField, SortFieldCollection, SortDirection
  Storage/          IDefineAccess, ICustomizeDefineReader, CustomizeOnlyStorage (and friends)
  (root)            Cross-cutting infrastructure:
                    BackendDefaultTypes, DefineTypeExtensions, DefineType,
                    GlobalEvents, PropertyCategories,
                    SysFields, SysProgIds, SystemActions,
                    PathOptions, CustomizeOnlyPathOptions,
                    IDatabaseSettingsProvider, IBusinessObjectFactory,
                    ICacheDataSourceProvider
```

The namespace layout follows the design principles in [ADR-008](../../docs/adr/adr-008-bee-db-namespace-layout.md):
syntax/model/factory separation; concrete content grouped by domain (`Database`, `Filters`, `Forms`, `Layouts`, etc.); the root layer reserved for cross-cutting infrastructure (system constants, global service-locator interfaces, framework-wide enums).
