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
| **Method** | The JSON-RPC `method` field — `progId.action`. <c>SystemActions</c> / <c>FormActions</c> constants. |
| **Contract** | The `Bee.Api.Contracts.I<Action>Request` / `I<Action>Response` pair (the "single source of truth"); blank if none. |
| **Args / Result** | The BO POCO types (`Bee.Business.<Axis>.<Action>Args` / `<Action>Result`). |
| **Protection** | `[ApiAccessControl]` first arg: `Public` / `Encoded` / `Encrypted`. See [security rules](../.claude/rules/security.md). |
| **Auth** | `[ApiAccessControl]` second arg: `Anonymous` / `Authenticated`. |
| **Purpose** | One-line summary; see XML doc on the BO method for full detail. |

## Axis: Base (`BusinessObject`)

Methods defined on the base class — every BO axis inherits them.

| Method | Contract | Args / Result | Protection | Auth | Purpose |
|--------|----------|---------------|------------|------|---------|
| `ExecFunc` | `IExecFuncRequest` / `IExecFuncResponse` | `ExecFuncArgs` / `ExecFuncResult` | Public | Authenticated | Generic dispatch to a host-defined custom method, by name. |
| `ExecFuncAnonymous` | `IExecFuncRequest` / `IExecFuncResponse` | `ExecFuncArgs` / `ExecFuncResult` | Public | Anonymous | Same as `ExecFunc` but pre-login (e.g. registration flows). |

## Axis: System (`SystemBusinessObject`)

Singleton system-level BO, accessed as `System.<action>` over the wire.

| Method | Contract | Args / Result | Protection | Auth | Purpose |
|--------|----------|---------------|------------|------|---------|
| `Ping` | `IPingRequest` / `IPingResponse` | `PingArgs` / `PingResult` | Public | Anonymous | Liveness probe; round-trips a server timestamp. |
| `GetCommonConfiguration` | `IGetCommonConfigurationRequest` / `IGetCommonConfigurationResponse` | `GetCommonConfigurationArgs` / `GetCommonConfigurationResult` | Public | Anonymous | Returns `CommonConfiguration` (payload options, debug flag, default lang …). |
| `Login` | `ILoginRequest` / `ILoginResponse` | `LoginArgs` / `LoginResult` | Public | Anonymous | Authenticates user; returns access token + dynamic API encryption key. |
| `CreateSession` | `ICreateSessionRequest` / `ICreateSessionResponse` | `CreateSessionArgs` / `CreateSessionResult` | Public | Anonymous | Issues an anonymous session token (no user identity). |
| `EnterCompany` | `IEnterCompanyRequest` / `IEnterCompanyResponse` | `EnterCompanyArgs` / `EnterCompanyResult` | Public | Authenticated | Switches the session to the specified company (multi-tenant scope). |
| `LeaveCompany` | `ILeaveCompanyRequest` / `ILeaveCompanyResponse` | `LeaveCompanyArgs` / `LeaveCompanyResult` | Public | Authenticated | Clears the company context, keeping the session alive. |
| `Logout` | `ILogoutRequest` / `ILogoutResponse` | `LogoutArgs` / `LogoutResult` | Public | Authenticated | Destroys the current session (also clears company context). |
| `GetDefine` | `IGetDefineRequest` / `IGetDefineResponse` | `GetDefineArgs` / `GetDefineResult` | Public | Authenticated | Returns definition data as an XML envelope (universal — .NET clients use this for FormSchema / FormLayout / LanguageResource). |
| `SaveDefine` | `ISaveDefineRequest` / `ISaveDefineResponse` | `SaveDefineArgs` / `SaveDefineResult` | Public | Authenticated | Persists definition data via XML envelope; invalidates the matching cache slot. |
| `GetFormSchema` | `IGetFormSchemaRequest` / `IGetFormSchemaResponse` | `GetFormSchemaArgs` / `GetFormSchemaResult` | Public | Authenticated | **JS-only.** Returns a `FormSchema` as a typed JSON tree (auto-localized using session's `Culture`). |
| `GetFormLayout` | `IGetFormLayoutRequest` / `IGetFormLayoutResponse` | `GetFormLayoutArgs` / `GetFormLayoutResult` | Public | Authenticated | **JS-only.** Returns a `FormLayout` (generated from auto-localized FormSchema). |
| `GetLanguage` | `IGetLanguageRequest` / `IGetLanguageResponse` | `GetLanguageArgs` / `GetLanguageResult` | Public | Authenticated | **JS-only.** Returns a `LanguageResource` for one `(Lang, Namespace)` pair. |
| `CheckPackageUpdate` | `ICheckPackageUpdateRequest` / `ICheckPackageUpdateResponse` | `CheckPackageUpdateArgs` / `CheckPackageUpdateResult` | Encoded | Anonymous | Reports whether a client package upgrade is available. |
| `GetPackage` | `IGetPackageRequest` / `IGetPackageResponse` | `GetPackageArgs` / `GetPackageResult` | Encoded | Anonymous | Streams a client upgrade package binary. |

> **JS-only methods.** `GetFormSchema` / `GetFormLayout` / `GetLanguage` use
> `KeyCollectionBase` internals that don't round-trip through MessagePack
> (the Encoded / Encrypted wire formats). They're meant for JS / TypeScript
> consumers over the Plain JSON wire path. .NET clients should use `GetDefine`
> with the matching `DefineType` instead.

## Axis: Form (`FormBusinessObject`)

Per-program BO instance, accessed as `<progId>.<action>` over the wire
(e.g. `Employee.GetList`, `Order.Save`).

| Method | Contract | Args / Result | Protection | Auth | Purpose |
|--------|----------|---------------|------------|------|---------|
| `GetList` | `IGetListRequest` / `IGetListResponse` | `GetListArgs` / `GetListResult` | Public | Authenticated | Master-table list query; supports `Filter` / `Sort` / `Paging` (callers should always paginate). |
| `GetNewData` | `IGetNewDataRequest` / `IGetNewDataResponse` | `GetNewDataArgs` / `GetNewDataResult` | Public | Authenticated | Returns a blank `DataSet` skeleton with FormSchema defaults + server-issued `sys_rowid`. |
| `GetData` | `IGetDataRequest` / `IGetDataResponse` | `GetDataArgs` / `GetDataResult` | Public | Authenticated | Loads one master row (and its details) by `RowId`. |
| `Save` | `ISaveRequest` / `ISaveResponse` | `SaveArgs` / `SaveResult` | Public | Authenticated | Persists a `DataSet` by dispatching INSERT / UPDATE / DELETE per row's `RowState`. |
| `Delete` | `IDeleteRequest` / `IDeleteResponse` | `DeleteArgs` / `DeleteResult` | Public | Authenticated | Deletes one master row directly by `RowId`. |

## See also

- [API Contract & BO Parameter Design](api-bo-contract-design.md) — Layered design rationale for Contract / Args / Result
- [Security rules](../.claude/rules/security.md) — `ApiAccessControl` semantics + payload pipeline
- [bee-add-bo-method skill](../.claude/skills/bee-add-bo-method/SKILL.md) — Step-by-step for adding a new method (includes updating this reference)
