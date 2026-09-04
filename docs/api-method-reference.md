# API Method Reference

[繁體中文](api-method-reference.zh-TW.md) · [← Docs Index](README.md)

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
| **Method** | The JSON-RPC `method` field — `progId.action`. Listed action constants live in `SystemActions` / `FormActions` / `LogActions`. |
| **Protection** | `[ApiAccessControl]` first arg. The values and what each one means are on `ApiProtectionLevel` — see [`src/Bee.Definition/Security/ApiProtectionLevel.cs`](../src/Bee.Definition/Security/ApiProtectionLevel.cs). Note the table below uses `LocalOnly` as well as the transport levels. |
| **Auth** | `[ApiAccessControl]` second arg — see [`src/Bee.Definition/Security/ApiAccessRequirement.cs`](../src/Bee.Definition/Security/ApiAccessRequirement.cs). |
| **Purpose** | One-line summary; see XML doc on the BO method for full detail. |

### Replay protection

These methods additionally declare `ReplayProtection = UniqueSequence`: each call must carry a
sequence number the session has not used before, or the server answers `-32005 ReplayRejected`.
A client that reuses or replays a request frame on one of them will be refused.

- `Delete`
- `EnterCompany`
- `ExecFunc`
- `LeaveCompany`
- `Save`

### Naming convention (Contract / Args / Result derivable from action)

For any `<Action>` listed below, the contract / BO types follow a fixed pattern:

- **Wire contract**: `Bee.Api.Contracts.I<Action>Request` / `I<Action>Response`
- **Wire DTO**: `Bee.Api.Core.Messages.<Axis>.<Action>Request` / `<Action>Response`
- **BO Args / Result**: `Bee.Business.<Axis>.<Action>Args` / `<Action>Result`

E.g. `GetLanguage` → `IGetLanguageRequest` / `IGetLanguageResponse` /
`GetLanguageArgs` / `GetLanguageResult`. Use IDE "Go to symbol" to jump to
any of these from the action name; no need to list them in the tables.

**The request side always follows this pattern; the response side does not.**
Where several actions return the same shape, they share one response type rather
than each declaring an identical copy. On the audit-log axis every action does:
the five list queries return `LogListResponse` / `LogListResult` and the three
aggregates return `LogAggregateResponse` / `LogAggregateResult`, so only
`GetChangeDetail` has a response type named after its action. Derive request
types from the action name; look the response type up from the BO method
signature.

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
| `CreateSession` | LocalOnly | Anonymous | Issues a session token for a given user id **without checking credentials** — that is what separates it from `Login`. Trusted-caller operation, so remote calls are rejected. |
| `EnterCompany` | Public | Authenticated | Switches the session to the specified company (multi-tenant scope). |
| `LeaveCompany` | Public | Authenticated | Clears the company context, keeping the session alive. |
| `Logout` | Public | Authenticated | Destroys the current session (also clears company context). |
| `GetDefine` | Public | Authenticated | Returns definition data as an XML envelope (universal — .NET clients use this for FormSchema / FormLayout / LanguageResource). |
| `SaveDefine` | LocalOnly | Authenticated | Persists definition data via XML envelope; invalidates the matching cache slot. Writing definitions is a deployment-time operation, so remote callers are rejected outright — read them with `GetDefine`. |
| `GetCustomizePluginSettings` | LocalOnly | Authenticated | Reads one tenant's business plugin bindings as XML; an empty string when the tenant declares none. Local-only for the same reason as `SaveCustomizePluginSettings`. |
| `SaveCustomizePluginSettings` | LocalOnly | Authenticated | Stores one tenant's business plugin bindings, replacing them outright. Every bound type is validated before anything is written — it must load, derive from `FormBusinessPlugin`, and override at least one stage — and one bad entry rejects the whole definition. These bindings decide which code runs inside the save and delete pipelines, so remote callers are rejected outright. |
| `CreateApiKey` | Encrypted | Authenticated | Issues an API key and returns the complete plaintext key **once** — only a hash is stored, so it cannot be shown again. A key belongs to the installation, not to a company, so a remote caller must be a deployment administrator (`st_user.deployment_admin`); being merely authenticated is not enough. Local calls pass without one, so a deployment with no administrator yet can still mint its first key on the host. |
| `ListApiKeys` | Encrypted | Authenticated | Lists the issued API keys, enabled and disabled alike, as summaries carrying **no credential material** — the stored hash never leaves the server. Same gate as `CreateApiKey`. |
| `SetApiKeyEnabled` | Encrypted | Authenticated | Enables or disables an issued key. Disabling is the revocation path and takes effect **immediately** across every server process, not when a cache lapses. Same gate as `CreateApiKey`. |
| `SetApiKeyExpiry` | Encrypted | Authenticated | Sets or clears a key's expiry. A past time is accepted here (retiring a live key), unlike on `CreateApiKey`. Same gate as `CreateApiKey`. |
| `SetDeploymentAdmin` | LocalOnly | Authenticated | Grants or revokes a user's deployment administrator flag (`st_user.deployment_admin`), which governs installation-wide assets rather than any company's data. Appointing an administrator is a deployment-time operation, so remote callers are rejected; this is also the only write path to the column. |
| `GetFormSchema` | Public | Authenticated | **JS-only.** Returns a `FormSchema` as a typed JSON tree (auto-localized using session's `Culture`). |
| `GetFormLayout` | Public | Authenticated | **JS-only.** Returns the base-layer `FormLayout` definition exactly as stored; empty when none is stored. |
| `GetDepartmentTree` | Public | Authenticated | Returns the current company's department tree (per-company org hierarchy) as a typed object (JSON / MessagePack); `null` when no company is entered. |
| `GetLanguage` | Public | Authenticated | **JS-only.** Returns a `LanguageResource` for one `(Lang, Namespace)` pair. |
| `GetCustomizeFormLayout` | Public | Authenticated | Returns the session tenant's `FormLayout` override as XML, or an empty string when the tenant declares none. The customize code comes from the session, never from the caller. |
| `GetCustomizeLanguage` | Public | Authenticated | Returns the session tenant's `LanguageResource` override as XML, or an empty string when the tenant declares none. |
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

The change axis uses a **list / detail** split: `GetChangeLog` returns lightweight event *headers* (paged `DataTable`, no DiffGram); the DiffGram is restored on demand per event via `GetChangeDetail`.

| Method | Protection | Auth | Purpose |
|--------|------------|------|---------|
| `GetChangeLog` | Encrypted | Authenticated | A filtered, paged list of `st_log_change` event headers (typed filter: time range / user / progId / rowKey / change-kind). Typical uses: a form's changes over a period (`ProgId` + time range), a user's changes over a period (`UserId` + time range), or one record's history (`ProgId` + `RowKey`). Returns a header `DataTable` + `PagingInfo`. |
| `GetChangeDetail` | Encrypted | Authenticated | One change event's `changes_xml` DiffGram restored server-side into structured field-level before/after values, keyed by the event's `SysRowId`. |
| `GetLoginLog` | Encrypted | Authenticated | Filtered, paged list of `st_log_login` event headers (time / user / event). Returns a header `DataTable` + `PagingInfo`. |
| `GetAccessLog` | Encrypted | Authenticated | Filtered, paged list of `st_log_access` record-view headers (time / user / progId / rowKey). Returns a header `DataTable` + `PagingInfo`. |
| `GetApiAnomalyLog` | Encrypted | Authenticated | Filtered, paged list of `st_log_anomaly_api` headers (time / user / method / anomaly-kind). Returns a header `DataTable` + `PagingInfo`. |
| `GetDbAnomalyLog` | Encrypted | Authenticated | Filtered, paged list of `st_log_anomaly_db` headers (time / databaseId / anomaly-kind). A cross-company infrastructure view (`st_log_anomaly_db` has no company). Returns a header `DataTable` + `PagingInfo`. |
| `GetApiAnomalySummary` | Encrypted | Authenticated | API-anomaly counts grouped by `anomaly_kind` over an optional time window (monitoring summary). Returns an aggregate `DataTable` (`anomaly_kind` / `event_count`), unpaged. |
| `GetDbAnomalySummary` | Encrypted | Authenticated | DB-anomaly counts grouped by `anomaly_kind` over an optional time window. Cross-company infrastructure summary. Returns an aggregate `DataTable` (`anomaly_kind` / `event_count`), unpaged. |
| `GetTopApiMethods` | Encrypted | Authenticated | Busiest API methods by anomaly count over an optional time window (monitoring hot-spots). Returns an aggregate `DataTable` (`method` / `event_count` / `max_elapsed_ms`), top-N. |

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
- [Permission & Authorization](permission-authorization.md) — what each `[ApiAccessControl]` requirement means at run time
- [ADR-004](adr/adr-004-messagepack-payload.md) and [ADR-044](adr/adr-044-payload-codec-negotiation.md) — the payload pipeline and per-request codec negotiation
