# API Method Reference

[繁體中文](api-method-reference.zh-TW.md)

This is the single-page reference of every public BO method exposed through
`JsonRpcExecutor`, grouped by BO axis. Each row lists the method's wire-level
[contract interface](api-bo-contract-design.md), BO-level Args / Result types,
`[ApiAccessControl]` settings, and a one-line purpose summary.

> **Source of truth.** The reference is kept in sync with the BO source files by
> `BoApiSurfaceTests` (in `tests/Bee.Business.UnitTests/`). Adding or modifying
> a method requires updating both this document and the test baseline; the build
> will fail otherwise.

> Looking for which `progId`s are reserved by the framework? See [Framework-Reserved Names](framework-reserved-names.md).

## Reading the columns

| Column | Meaning |
|--------|---------|
| **Method** | The JSON-RPC `method` field — `progId.action`. Listed action constants live in `SystemActions` / `FormActions`. |
| **Protection** | `[ApiAccessControl]` first arg: `Public` / `Encoded` / `Encrypted`. See [security rules](../.claude/rules/security.md). |
| **Auth** | `[ApiAccessControl]` second arg: `Anonymous` / `Authenticated`. |
| **Purpose** | One-line summary; see XML doc on the BO method for full detail. |

### Naming convention (Contract / Args / Result derivable from action)

For any `<Action>` listed below, the contract / BO types follow a fixed pattern:

- **Wire contract**: `Bee.Api.Contracts.I<Action>Request` / `I<Action>Response`
- **Wire DTO**: `Bee.Api.Core.Messages.<Axis>.<Action>Request` / `<Action>Response`
- **BO Args / Result**: `Bee.Business.<Axis>.<Action>Args` / `<Action>Result`

E.g. `GetLanguage` → `IGetLanguageRequest` / `IGetLanguageResponse` /
`GetLanguageArgs` / `GetLanguageResult`. Use IDE "Go to symbol" to jump to
any of these from the action name; no need to list them in the tables.

## Axis: Base (`BusinessObject`)

Methods defined on the base class — every BO axis inherits them.

| Method | Protection | Auth | Purpose |
|--------|------------|------|---------|
| `ExecFunc` | Public | Authenticated | Generic dispatch to a host-defined custom method, by name. |
| `ExecFuncAnonymous` | Public | Anonymous | Same as `ExecFunc` but pre-login (e.g. registration flows). |

## Axis: System (`SystemBusinessObject`)

Singleton system-level BO, accessed as `System.<action>` over the wire.

| Method | Protection | Auth | Purpose |
|--------|------------|------|---------|
| `Ping` | Public | Anonymous | Liveness probe; round-trips a server timestamp. |
| `GetCommonConfiguration` | Public | Anonymous | Returns `CommonConfiguration` (payload options, debug flag, default lang …). |
| `Login` | Public | Anonymous | Authenticates user; returns access token + dynamic API encryption key. |
| `CreateSession` | Public | Anonymous | Issues an anonymous session token (no user identity). |
| `EnterCompany` | Public | Authenticated | Switches the session to the specified company (multi-tenant scope). |
| `LeaveCompany` | Public | Authenticated | Clears the company context, keeping the session alive. |
| `Logout` | Public | Authenticated | Destroys the current session (also clears company context). |
| `GetDefine` | Public | Authenticated | Returns definition data as an XML envelope (universal — .NET clients use this for FormSchema / FormLayout / LanguageResource). |
| `SaveDefine` | Public | Authenticated | Persists definition data via XML envelope; invalidates the matching cache slot. |
| `GetFormSchema` | Public | Authenticated | **JS-only.** Returns a `FormSchema` as a typed JSON tree (auto-localized using session's `Culture`). |
| `GetFormLayout` | Public | Authenticated | **JS-only.** Returns a `FormLayout` (generated from auto-localized FormSchema). |
| `GetDepartmentTree` | Public | Authenticated | Returns the current company's department tree (per-company org hierarchy) as a typed object (JSON / MessagePack); `null` when no company is entered. |
| `GetLanguage` | Public | Authenticated | **JS-only.** Returns a `LanguageResource` for one `(Lang, Namespace)` pair. |
| `CheckPackageUpdate` | Encoded | Anonymous | Reports whether a client package upgrade is available. |
| `GetPackage` | Encoded | Anonymous | Streams a client upgrade package binary. |

> **JS-only methods.** `GetFormSchema` / `GetFormLayout` / `GetLanguage` use
> `KeyCollectionBase` internals that don't round-trip through MessagePack
> (the Encoded / Encrypted wire formats). They're meant for JS / TypeScript
> consumers over the Plain JSON wire path. .NET clients should use `GetDefine`
> with the matching `DefineType` instead.

## Axis: Form (`FormBusinessObject`)

Per-program BO instance, accessed as `<progId>.<action>` over the wire
(e.g. `Employee.GetList`, `Order.Save`).

| Method | Protection | Auth | Purpose |
|--------|------------|------|---------|
| `GetList` | Public | Authenticated | Master-table list query; supports `Filter` / `Sort` / `Paging` (callers should always paginate). |
| `GetLookup` | Public | Authenticated | Lookup candidate rows for picker windows; projection is server-resolved from `FormSchema.LookupFields` (fallback `sys_id` / `sys_name`, always prefixed with `sys_rowid`). `SearchText` matches string-typed lookup fields; default paging applies when omitted. Intentionally not gated by the form's `Read` permission. |
| `GetNewData` | Public | Authenticated | Returns a blank `DataSet` skeleton with FormSchema defaults + server-issued `sys_rowid`. |
| `GetData` | Public | Authenticated | Loads one master row (and its details) by `RowId`. |
| `Save` | Public | Authenticated | Persists a `DataSet` by dispatching INSERT / UPDATE / DELETE per row's `RowState`. |
| `Delete` | Public | Authenticated | Deletes one master row directly by `RowId`. |

## Axis: Audit Log (`LogBusinessObject`)

Read-only queries over the `st_log_*` audit tables (the *read* side of the audit trail; the write side is the side-effects below). Dispatched as `AuditLog.<action>`. Every action is gated behind the `AuditLog` permission model (a `Read` grant is required) so a general user cannot read another's trail, and results are scoped to the caller's current company.

The change axis uses a **list / detail** split: the list methods return lightweight event *headers* (paged `DataTable`, no DiffGram); the DiffGram is restored on demand per event via `GetChangeDetail`.

| Method | Protection | Auth | Purpose |
|--------|------------|------|---------|
| `GetRecordHistory` | Encrypted | Authenticated | A page of one record's change-event headers (all `st_log_change` events for a `ProgId` + `RowKey`, newest first). Returns a header `DataTable` + `PagingInfo`. |
| `GetChangeLog` | Encrypted | Authenticated | A filtered, paged list of `st_log_change` event headers across records (typed filter: time range / user / progId / rowKey / change-kind). Returns a header `DataTable` + `PagingInfo`. |
| `GetChangeDetail` | Encrypted | Authenticated | One change event's `changes_xml` DiffGram restored server-side into structured field-level before/after values, keyed by the event's `SysRowId`. |

## Audit side-effects

When the corresponding `AuditLogOptions` category is enabled (opt-in, off by default), these methods write an audit-trail row best-effort — the log write never changes the method result. See [Framework-Reserved Names §1.3](framework-reserved-names.md).

| Method | Log table | Recorded |
|--------|-----------|----------|
| `System.Login` / `System.Logout` | `st_log_login` | Login success / failure / lockout / logout |
| `Form.Save` | `st_log_change` | Data change (DataSet DiffGram before/after) |
| `Form.Delete` | `st_log_change` | Delete with the deleted record's before-image |
| `Form.GetData` | `st_log_access` | Record view (who viewed which record) |
| *any API call* | `st_log_anomaly_api` | API Error / Timeout / Slow |

## See also

- [API Contract & BO Parameter Design](api-bo-contract-design.md) — Layered design rationale for Contract / Args / Result
- [Security rules](../.claude/rules/security.md) — `ApiAccessControl` semantics + payload pipeline
- [bee-add-bo-method skill](../.claude/skills/bee-add-bo-method/SKILL.md) — Step-by-step for adding a new method (includes updating this reference)
