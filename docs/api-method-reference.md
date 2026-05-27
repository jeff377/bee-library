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
| `GetNewData` | Public | Authenticated | Returns a blank `DataSet` skeleton with FormSchema defaults + server-issued `sys_rowid`. |
| `GetData` | Public | Authenticated | Loads one master row (and its details) by `RowId`. |
| `Save` | Public | Authenticated | Persists a `DataSet` by dispatching INSERT / UPDATE / DELETE per row's `RowState`. |
| `Delete` | Public | Authenticated | Deletes one master row directly by `RowId`. |

## See also

- [API Contract & BO Parameter Design](api-bo-contract-design.md) — Layered design rationale for Contract / Args / Result
- [Security rules](../.claude/rules/security.md) — `ApiAccessControl` semantics + payload pipeline
- [bee-add-bo-method skill](../.claude/skills/bee-add-bo-method/SKILL.md) — Step-by-step for adding a new method (includes updating this reference)
