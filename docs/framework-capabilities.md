# Framework Capabilities

[繁體中文](framework-capabilities.zh-TW.md) · [← Docs Index](README.md)

> A single-page catalogue of the mechanisms Bee.NET provides out of the box, grouped by area. Each row is one sentence — enough to know whether the framework already covers a need, and where to read further.
>
> This page answers **"what is provided"**. It does not explain **how to use** any single mechanism; every section links to the document that does.

---

## 1. Definition Layer

Thirteen definition types under `DefinePath` drive the whole application. See [Definition Files Overview](definition-files-overview.md) and [Architecture Overview](architecture-overview.md).

| Mechanism | What it provides |
|-----------|------------------|
| **FormSchema** | The definition hub. One schema drives the UI, the generated SQL and the validation rules at once, so ordinary CRUD needs no code |
| **TableSchema** | The physical table: columns, types, lengths, nullability, indexes |
| **FormLayout** | How a form is arranged on screen, laying out the fields FormSchema declares |
| **`IDefineAccess`** | One interface for reading and writing all definition types, shared by server and client (the client goes through the API) |
| **ProgramSettings** | Type registry mapping each `progId` to its business object and repository. Server-side only |
| **MenuSettings** | Navigation menu: groups, ordering, captions and visibility, each item pointing at a `progId` |
| **Startup trio** | SystemSettings → DatabaseSettings → DbCategorySettings, loaded in a fixed order because the master key from the first decrypts the connection strings in the second |
| **FormRule** | Declarative pre-save / pre-delete validation, written inside the FormSchema |
| **Expression engine** | DynamicExpresso evaluates computed fields and rules; `IExpressionEvaluator` keeps the engine swappable. See [Expressions and Rules](expression-rules.md) |
| **Master-detail** | Child tables linked through `sys_master_rowid`, written together in a single `Save` |
| **Lookup relations** | A field declares its relation target and field mappings; the picker query and value propagation come for free |
| **PluginSettings** | Which business plugins are attached to each `progId`, executed in declaration order |

## 2. Data Access

See [FormSchema-Driven Database Access](formschema-data-access.md), [Database Settings Guide](database-settings-guide.md) and [Database Dialect Differences](database-dialect-differences.md).

| Mechanism | What it provides |
|-----------|------------------|
| **DbAccess** | The data access core: sync and async execution, batches, DataTable updates |
| **DbCommandSpec** | `{0}` placeholder statements parameterised by the framework, so no call site concatenates SQL |
| **FormSchema-driven SQL** | SQL generated at runtime from the FormSchema — no ORM, no generated entity classes |
| **Five dialects** | SQL Server, PostgreSQL, MySQL, Oracle and SQLite providers, each with its own DDL and parameter rules |
| **Category routing** | `common` / `company` / `log` scopes decide which physical database a table lands in |
| **Connection string encryption** | Connection strings are stored encrypted in DatabaseSettings and decrypted with the master key |
| **Schema upgrade** | diff → plan → execute pipeline with automatic ALTER-vs-rebuild decisions and dry runs. See [Database Schema Upgrade](database-schema-upgrade.md) |
| **Paging, sorting, filtering** | `PagingInfo`, `SortField` and a `FilterNode` condition tree supporting nested AND / OR groups |
| **Numeric rounding policy** | Round-then-sum: each detail row is rounded to its field scale before totalling, so details always add up to the total |
| **Connection scope** | `DbConnectionScope` owns connection and transaction lifetime across a unit of work |
| **Anomaly detection** | `DbAccess` records suspicious access patterns to the anomaly log |
| **Two-track repositories** | CRUD comes from `DataFormRepository` driven by the FormSchema; reports and batch work go to a hand-written repository |

## 3. Business Layer

See [End-to-End Development Cookbook](development-cookbook.md) and [API ↔ BO Contract Design](api-bo-contract-design.md).

| Mechanism | What it provides |
|-----------|------------------|
| **Three BO axes** | System (framework-wide), Form (one instance per `progId`) and Log (audit queries) |
| **`FormBusinessObject`** | Default CRUD surface: `GetList`, `GetData`, `GetNewData`, `Save`, `Delete`, `GetLookup` |
| **`IBusinessObjectFactory`** | Resolves a business object by `progId`, falling back to the framework default when none is registered |
| **ExecFunc** | Generic dispatch for host-defined methods called by name, with an anonymous variant for flows such as self-registration |
| **`FormBusinessPlugin`** | Hook points around the save and delete pipeline, chained in declaration order |
| **GlobalEvents** | Framework-level event hooks for cross-cutting host behaviour |

## 4. API and Transport

See [JSON-RPC Frontend Integration](jsonrpc-frontend-integration.md) and [API Method Reference](api-method-reference.md).

| Mechanism | What it provides |
|-----------|------------------|
| **JSON-RPC 2.0** | A single POST endpoint; the `method` field is `progId.action` |
| **PayloadFormat** | Plain, Encoded and Encrypted payload modes selected per method |
| **Payload pipeline** | Serialize → compress → encrypt (MessagePack + Gzip + AES-CBC-HMAC), in that order, reversed on the way back |
| **Connectors** | `SystemApiConnector`, `FormApiConnector` and `LogApiConnector` are the client-side entry points |
| **Connect types** | The same client code runs against an in-process backend or a remote HTTP one; call sites do not change |
| **Three-tier contracts** | Contract interface, wire DTO and BO args/result, all derivable from the action name |
| **Wire contracts** | Wire types register their MessagePack formatters explicitly, so payloads work on runtimes without dynamic code (iOS AOT) |
| **Time zone at the wire** | Conversion happens at the payload boundary; storage stays UTC. See [Time Zone Handling](datetime-timezone.md) |
| **JS frontend surface** | Non-.NET frontends use Plain JSON, with typed variants of `GetFormSchema` / `GetFormLayout` / `GetLanguage` |

## 5. Session and Authentication

| Mechanism | What it provides |
|-----------|------------------|
| **Session and access token** | GUID tokens with an expiry, one-time tokens supported, session state persisted in `st_session` |
| **Login** | Credential verification returning an access token and the dynamic API encryption key |
| **CreateSession** | Issues a token for a given user *without* verifying credentials, for trusted background jobs; local calls only |
| **Login attempt tracking** | `ILoginAttemptTracker` counts failures so a host can implement lockout |
| **API keys** | Identify the calling application, not the user: only the hash is stored, the plaintext is returned once, and keys can be disabled or expired. See [API Key Management](api-key-management.md) |
| **Deployment admin** | A deployment-wide administrator flag governing installation assets, granted separately from any company permission |

## 6. Security and Cryptography

| Mechanism | What it provides |
|-----------|------------------|
| **`[ApiAccessControl]`** | Declares protection level and authentication requirement per method, plus a local-only tier |
| **Master key provider** | Pluggable master key sources, used to decrypt connection strings and other protected settings |
| **AES-CBC-HMAC** | AES-256-CBC with HMAC-SHA256, a fresh random IV per operation and constant-time comparison |
| **Password hashing** | `PasswordHasher` for credential storage and verification |
| **API encryption key providers** | Static, dynamic and derived strategies decide which payload key a client receives |
| **Sensitive fields** | `SensitiveCategory` and reserved protected fields control what is returned and what is masked in logs |
| **File integrity** | `FileHashValidator` verifies delivered files against their hash |

## 7. Permission and Authorization

See [Permission & Authorization](permission-authorization.md).

| Mechanism | What it provides |
|-----------|------------------|
| **Two-layer model** | An action gate decides whether an operation is allowed; record scope decides which rows are visible |
| **PermissionModels** | Registry of permission models, their actions and their record-scope strategies |
| **Role and grant tables** | `st_role`, `st_role_grant` and `st_user_role` carry roles and their grants |
| **`FormField.ScopeRole`** | Marks the scoping column; reads are filtered automatically and writes re-query authoritatively on the server |
| **Field capability resolution** | `ElementCapabilityResolver` turns permissions into per-field readable / writable / hidden states for the UI |

## 8. Multi-Tenancy and Customization

See [Tenant Customization](customization.md).

| Mechanism | What it provides |
|-----------|------------------|
| **Company scope** | Each company gets its own database and settings; `EnterCompany` and `LeaveCompany` switch the session's scope |
| **Customize overlay** | Per-tenant overrides for FormLayout, LanguageResource and PluginSettings, without forking the base definitions |
| **Department tree** | Per-company organisational hierarchy exposed as a typed tree |
| **Employee context** | The department and employee record behind the current user, available to permissions and default values |

## 9. Localization and Formatting

| Mechanism | What it provides |
|-----------|------------------|
| **LanguageResource** | One file per (language × namespace) carrying captions and messages |
| **`FormSchemaLocalizer`** | Localizes form and field captions against the session culture |
| **LanguageEnum** | Localized enumeration entries backing drop-down lists |
| **`BeeStringLocalizer`** | An `IStringLocalizer` implementation so application code and UI read localized text directly |
| **Time zone** | Stored as UTC, converted at the boundary, with a per-user time zone setting. See [Time Zone Handling](datetime-timezone.md) |
| **Currency and unit masters** | `CurrencySettings` and `UnitSettings` define decimal places per currency and per unit of measure |
| **Number format resolution** | Decimal places per numeric kind resolved at company level, applied when values are written and displayed |
| **Cash rounding** | Rounds to the smallest natural denomination of the currency |

## 10. Caching

| Mechanism | What it provides |
|-----------|------------------|
| **ObjectCache / KeyObjectCache** | Single-object and keyed caches for definitions and database-backed data |
| **`CacheDefineAccess`** | Caches definition files process-wide; cached instances are shared and must not be mutated |
| **Cache notify** | Invalidation broadcast through `st_cache_notify` reaches every process without waiting for expiry |
| **Single flight** | Concurrent loads of the same key collapse into one, avoiding cache stampedes |
| **`ICacheDataSourceProvider`** | The seam for caches whose source is a database query rather than a definition file |

## 11. Audit, Diagnostics and Tooling

| Mechanism | What it provides |
|-----------|------------------|
| **Four audit streams** | Login events, API access, data changes (before/after diffgram) and anomalies (API and database) |
| **Log business object** | Query API over the audit streams, with both detail listings and aggregates |
| **Tracer** | Layered tracing with categories and pluggable listeners |
| **Bee.Analyzers** | Build-time diagnostics shipped with the packages, so convention violations fail the build. See [Analyzer Rules](analyzer-rules.md) |
| **UI control families** | Avalonia (schema-driven native control subclasses, grid, lookup dialogs) and Blazor Server |
| **`ClientDefineAccess`** | Clients read definitions through the API and cache them, never touching the file system |
| **Client storage seams** | `IEndpointStorage` and `IApiKeyStorage` let each head persist endpoints and keys the way its platform allows |
