# Changelog

[繁體中文](CHANGELOG.zh-TW.md)

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [4.6.0]

> Bee.NET remains in pre-stable evolution. The theme of this release is "open up JSON-RPC to JavaScript frontends": seven FormBO / SystemBO CRUD / Session methods now ship with `ProtectionLevel = Public`, two new JSON-native getters (`GetFormSchema` / `GetFormLayout`) are introduced, and Plain-path `DataSet` deserialization plus a Blazor WebAssembly RSA blocker are fixed. The `MasterKeySource` default flips to `Environment`; under strict SemVer this would be a major bump, but under the pre-stable policy it ships as a minor.

### Added

- **`SystemBO.GetFormSchema` / `GetFormLayout`** — Two JSON-friendly getters that let JS / TypeScript frontends pull strongly-typed `FormSchema` / `FormLayout` directly as JSON via the Plain wire format and render schema-driven UIs. `.NET` clients get matching `SystemApiConnector.GetFormSchemaAsync` / `GetFormLayoutAsync`; the existing `GetDefineAsync<FormSchema>` (XML-string intermediary) still works. Both methods are `Public + Authenticated`. See [ADR-014](docs/adr/adr-014-jsonrpc-plain-public-default.md).
- **JSON-RPC frontend integration guide** — New [`docs/jsonrpc-frontend-integration.md`](docs/jsonrpc-frontend-integration.md) (bilingual) covering wire format, headers, auth flow, callable method catalog, `JsonRpcErrorCode` mapping, and a copy-paste TypeScript wrapper. Intended for React / Vue / Angular / vanilla JS consumers.

### Changed

- **`MasterKeySource` default changed to `Environment`** (**breaking**) — new installations now default to reading the master key from `$BEE_MASTER_KEY` instead of a `Master.key` file under `DefinePath`. Aligns with the 12-factor "config in env" principle so container / Kubernetes / cloud-function hosts can inject the key directly without mounting a secret volume. Existing deployments with an explicit `<Type>File</Type>` in `SystemSettings.xml` are unaffected. To migrate an existing host: set `BEE_MASTER_KEY` to the Base64 content of the current `Master.key`, then update `SystemSettings.xml` to `<Type>Environment</Type><Value>BEE_MASTER_KEY</Value>`. See [ADR-015](docs/adr/adr-015-master-key-environment-default.md).
- **Seven BO methods downgraded to `ProtectionLevel = Public`** — `FormBO.GetNewData` / `GetData` / `Save` / `Delete` and `SystemBO.EnterCompany` / `LeaveCompany` / `Logout` move from `Encrypted + Authenticated` to `Public + Authenticated`, allowing JS frontends to call them with native JSON over HTTPS via `PayloadFormat.Plain`. `Authenticated` still gates identity, and application-layer authorisation checks are unchanged. **Backward compatible**: existing `Encrypted`-format `.NET` clients keep working (`ApiAccessValidator` allows a higher-protection format to call a lower-protection method). See [ADR-014](docs/adr/adr-014-jsonrpc-plain-public-default.md).
- **`FormSchema.MasterTable` is now `[JsonIgnore]`** — The `MasterTable` property is no longer serialised to JSON (its value is always equal to `Tables[ProgId]`, previously costing ~30% of the Plain wire-format payload). XML serialisation and MessagePack paths are **unaffected** (no `XmlIgnore` was added; `.NET` clients keep their existing path). JS / TS clients that used to read `masterTable` from the JSON response must read `tables[0]` instead.

### Fixed

- **Blazor WebAssembly Login unblocked** — `RsaCryptor` now exports keys in PEM (SPKI public / PKCS#1 private) instead of XML, removing the dependency on the Windows-only `RSA.ToXmlString` / `FromXmlString`. WASM further falls back via `OperatingSystem.IsBrowser()`: clients send an empty `ClientPublicKey` (the server short-circuits encryption when the key is empty, and subsequent `Encrypted` requests auto-downgrade to `Encoded`), and the default `HttpClient` is used so it is backed by `BrowserHttpHandler`. Verified end-to-end on the Wasm demo (Sign in → Employee CRUD); `Blazor.Server.Demo` and `QuickStart.Console` regress cleanly.
- **`ApiInputConverter` registers Plain-path converters for `DataSet` / `RowState`** — Previously only `PropertyNameCaseInsensitive` was set, but `DataTableJsonConverter` / `DataSetJsonConverter` / `JsonStringEnumConverter` were missing on the read side, so `SaveArgs` arriving over Plain came in with empty rows and `Save` always returned "DataSet has no pending changes". Aligned with the write-side `JsonCodec` registration; any Plain-path call carrying a `DataSet` now works.
- **MAUI `DynamicForm` rebuilds correctly after in-place `DataSet` mutation** — `FormDataObject` mutates the same `DataSet` reference across New / Load / Save / Delete, but `BindableProperty` only fires `propertyChanged` on reference changes, so `FormPage.RefreshFormView` was a no-op (New looked unresponsive, Save / Delete stayed disabled). `DynamicForm` now exposes a public `Refresh()` entry that drives the existing `Rebuild()`. Blazor is unaffected (Razor re-renders the entire component tree on every event handler completion).

### Migration

**`MasterKeySource` (File → Environment) for an existing host:**

```bash
# 1. Set the existing Master.key Base64 content as an environment variable
export BEE_MASTER_KEY="$(cat $DEFINE_PATH/Master.key)"
```

```xml
<!-- 2. Update SystemSettings.xml to use Environment -->
<MasterKeySource>
  <Type>Environment</Type>
  <Value>BEE_MASTER_KEY</Value>
</MasterKeySource>
```

New hosts only need `export BEE_MASTER_KEY=<base64>` — the default `SystemSettings.xml` already uses `Environment`.

**JS / TS clients reading the master `FormSchema` table:**

```diff
- const masterTable = formSchema.masterTable;
+ const masterTable = formSchema.tables[0];
```

`.NET` clients (MessagePack / XML) need no changes.

## [4.5.0]

> Bee.NET remains in pre-stable evolution. This release introduces three frontend package layers (`Bee.UI.Core` cross-platform shared layer, `Bee.UI.Maui` MAUI mobile/desktop controls, `Bee.Web.Blazor.Server` / `Bee.Web.Blazor.Wasm` Blazor RCLs) and flips the API connector interfaces to async-only. Under strict SemVer the signature changes would be a major bump; under the pre-stable policy it ships as a minor.

### Added

- **New package `Bee.UI.Core`** — Cross-platform UI shared layer (Web / MAUI shared view models, `FormDataObject`, `SystemApiConnector`, `ClientInfo`). Previously lived in a separate repo `bee-ui-core`; merged into this monorepo for unified releases. See [ADR-013](docs/adr/adr-013-frontend-api-connection-strategy.md).
- **New package `Bee.UI.Maui`** — .NET MAUI cross-platform control layer (iOS / Android / macOS / Windows). Ships `DynamicForm` / `DynamicGrid` / `FormPage` controls and the sandbox-friendly `MauiPreferenceEndpointStorage`, plus `samples/Maui.Demo` wired to `QuickStart.Server`. The library defaults to `net10.0` (no MAUI workload required for consumers); opt into platform TFMs with `-p:BeeUiMauiFullPlatforms=true`.
- **New packages `Bee.Web.Blazor.Server` / `Bee.Web.Blazor.Wasm`** — Symmetric Blazor Razor Class Libraries for both render modes. Both ship `DynamicForm` / `DynamicGrid` / `FormPage` controls, `BeeAccessTokenProvider` for token injection, `BeeLoginPanel`, and the `AddBeeBlazor` service-registration extension.
- **`UserMessageException` + `JsonRpcErrorCode.UserMessage`** — Unified channel for "messages intended for the end user". Server-side throws are rehydrated by `ApiConnector` into a client-side `UserMessageException`, so callers can display `.Message` directly.
- **`FormBO` CRUD actions completed** — `FormBusinessObject` now declares `GetNewData` / `GetData` / `Save` / `Delete`, completing the single-row CRUD surface on `IFormBusinessObject` (`GetList` was added in v4.4.0).
- **`samples/` project family** — Three demo sets: `QuickStart.Server` + `QuickStart.Console` (P0, local + JSON-RPC dual-mode); `Blazor.Server.Demo` + `Blazor.Wasm.Demo` (P1); `Maui.Demo` (P2). All share define seeds and credential constants via `Bee.Samples.Shared`, and each ships a `.smoke.yaml` for end-to-end smoke testing.

### Changed

- **API connector interfaces are now async-only** — `IApiConnector` / `IFormApiConnector` / `ISystemApiConnector` no longer expose synchronous methods; all calls must go through the `*Async` variants. Migrate `connector.GetData(...)` to `await connector.GetDataAsync(...)`.
- **`ExceptionExtensions` namespace moved** — From `Bee.Base` to `Bee.Base.Exceptions`. Callers must add `using Bee.Base.Exceptions;`.
- **`ClientInfo` is now a static class** — `ClientInfo.SystemApiConnector.Initialize()` is now async; this codifies the "single-user frontend" assumption (multi-user web hosts should inject the token per-request via DI — see [ADR-013](docs/adr/adr-013-frontend-api-connection-strategy.md) extension section).

### Migration

**API connector callers (sync → async):**

```diff
- var data = connector.GetData(progId, formData);
+ var data = await connector.GetDataAsync(progId, formData);
```

**`ExceptionExtensions` namespace:**

```diff
  using Bee.Base;
+ using Bee.Base.Exceptions;

  ex.Unwrap();
```

**`UserMessageException` handling (recommended):**

```csharp
try
{
    await connector.SaveAsync(progId, dataSet);
}
catch (UserMessageException ex)
{
    // Already formatted as a user-facing message; show directly.
    ShowToast(ex.Message);
}
```

## [4.4.0]

> Bee.NET remains in pre-stable evolution; the public API surface has no external consumers yet, so minor releases are allowed to carry API moves and limited breaking changes. This release includes interface signature changes (`IFormRepositoryFactory.CreateDataFormRepository`, `IDataFormRepository.GetList`) and a property removal (`CompanyInfo.LogDatabaseId`). Under strict SemVer this would be a major bump; under the pre-stable policy it ships as a minor.

### Added

- **`FormBO.GetList` unified list query** — `IFormBusinessObject` declares the `GetList` signature; `FormBusinessObject` implements it through `IDataFormRepository` with `PagingOptions` / `PagingInfo` paging support; `FormApiConnector.GetList` / `GetListAsync` expose the client entry point. Integration-tested across all 5 dialects (SQL Server / PostgreSQL / SQLite / MySQL / Oracle).
- **Complete `SystemBO` session lifecycle** — Add `EnterCompany` / `LeaveCompany` / `Logout`, completing the two symmetric pairs (`Login` ↔ `Logout`, `EnterCompany` ↔ `LeaveCompany`); `SessionInfo` gains a nullable `CompanyId`, and `Login` is now declared on `ISystemBusinessObject`. Adds `CompanyInfo` type and `ICompanyInfoService` cache service. See [ADR-012](docs/adr/adr-012-session-company-context.md).
- **bo repo DB routing** — Add `DbScope` enum (`Common` / `Company` / `Log`) and `IRepositoryDatabaseRouter` so BO code no longer writes databaseId literals; `BusinessObject` exposes `ResolveDatabaseId(DbScope)` and `CreateDataFormRepository(string progId)` as protected helpers. See [ADR-010](docs/adr/adr-010-logical-database-category.md) (extension section).
- **`SelectCommandBuilder` paging and COUNT** — `OFFSET/FETCH` or `LIMIT/OFFSET` across all 5 dialects; new `BuildCount` method produces a standalone `SELECT COUNT(*)` usable independently of the SELECT pipeline.
- **`KeyObjectCache<T>` negative caching** — Enabled by default with a 5-minute absolute expiration to prevent cache penetration; override the virtual `GetNegativePolicy` to customise or disable (`SessionInfoCache` disables it to prevent anonymous traffic from filling the cache). See [ADR-009](docs/adr/adr-009-cache-implementation.md) (extension section).
- **Typed `IBusinessObjectFactory` wrappers** — `Bee.Business` adds `CreateFormBO(token, progId)` and `CreateSystemBO(token)` extension methods to remove manual casts at the call site.
- **`st_company` / `st_user_company` system tables** — In the common database, backed by `ICompanyRepository` / `IUserCompanyRepository`, so `EnterCompany` returns `CompanyAccessDenied` for "company missing / company disabled / unauthorised" alike. Default `DbCategorySettings` for the common category now includes these two tables.
- **Two new `JsonRpcErrorCode` values** — `CompanyNotEntered` (-32002, HTTP 409) and `CompanyAccessDenied` (-32003, HTTP 403). The latter deliberately merges "no permission" and "not found" to prevent user enumeration.

### Changed

- **`IFormRepositoryFactory.CreateDataFormRepository`** — Now takes an additional `Guid accessToken` parameter, routed through `IRepositoryDatabaseRouter`. BO code should use the new `BusinessObject.CreateDataFormRepository(progId)` helper, which threads the token automatically.
- **`IDataFormRepository.GetList`** — Now returns `DataFormListResult` (carrying `Table` + `Paging`) and accepts an optional `PagingOptions? paging`.
- **`CompanyInfo.LogDatabaseId` removed** — `DbScope.Log` now resolves to the fixed `databaseId = "log"` (pre-EnterCompany methods can write audit logs). Cross-company log isolation is handled by upcoming `sys_company_rowid` row-level partitioning, not separate physical log DBs per company.
- **`SelectCommandBuilder` behaviour on unknown table name** — Throws `InvalidOperationException` (was `KeyNotFoundException`), aligning with the Insert / Update / Delete builders.

### Migration

**`IFormRepositoryFactory` callers:**

```diff
- var repo = factory.CreateDataFormRepository("Employee");
+ var repo = factory.CreateDataFormRepository("Employee", accessToken);
```

> Inside BO code, prefer `BusinessObject.CreateDataFormRepository(progId)` — it threads the token for you.

**`IDataFormRepository.GetList` callers:**

```diff
- DataTable table = repo.GetList(filter, sortFields, fields);
+ DataFormListResult result = repo.GetList(filter, sortFields, fields, paging: null);
+ DataTable table = result.Table;
```

**Replace references to `CompanyInfo.LogDatabaseId`:**

```diff
- var logDbId = companyInfo.LogDatabaseId;
+ var logDbId = "log";  // Fixed framework routing; cross-company isolation via row-level partitioning
```

Or call `BusinessObject.ResolveDatabaseId(DbScope.Log)`.

## [4.3.0]

> Bee.NET is in pre-stable evolution; minor releases may include namespace moves while the public surface still has no external consumers. This release moves `AddBeeFramework` to a dedicated package — strictly a SemVer-major change, but treated as minor under the pre-stable policy.

### Added

- **New package `Bee.Hosting`** — composition root for the Bee.NET framework. Registers all backend services (`IDefineAccess`, `IDbAccessFactory`, `IBusinessObjectFactory`, `JsonRpcExecutor`, etc.) into any `IServiceCollection` without depending on ASP.NET Core. Non-ASP.NET Core hosts (WinForms, WPF, Console, Worker Service, integration tests) can now register the framework without pulling in `Microsoft.AspNetCore.App`.

### Changed

- **`BeeFrameworkServiceCollectionExtensions.AddBeeFramework` moved from `Bee.Api.AspNetCore` to `Bee.Hosting`.**
  - Namespace changed from `Bee.Api.AspNetCore` to `Bee.Hosting`.
  - ASP.NET Core hosts: `Bee.Api.AspNetCore` now references `Bee.Hosting`, so the package is brought in transitively. Add `using Bee.Hosting;` next to the existing `using Bee.Api.AspNetCore;`.
  - Non-ASP.NET Core hosts: reference `Bee.Hosting` directly instead of `Bee.Api.AspNetCore`. No more transitive `Microsoft.AspNetCore.App` dependency.
- `Bee.Api.AspNetCore` now only contains ASP.NET Core integration (`UseBeeFramework` middleware hook + `ApiServiceController`); its 4 previous project references (`Bee.Api.Core`, `Bee.Business`, `Bee.Db`, `Bee.ObjectCaching`, `Bee.Repository`) are now consolidated under `Bee.Hosting`.

### Migration

**ASP.NET Core web host:**

```diff
+ using Bee.Hosting;
  using Bee.Api.AspNetCore;

  var settings = SystemSettingsLoader.Load(pathOptions);
  services.AddBeeFramework(settings.BackendConfiguration, pathOptions);
  app.UseBeeFramework();
```

**Non-ASP.NET Core host (WinForms / WPF / Console / Worker / integration test):**

```diff
  <!-- *.csproj -->
- <PackageReference Include="Bee.Api.AspNetCore" Version="4.2.*" />
+ <PackageReference Include="Bee.Hosting" Version="4.3.*" />
```

```csharp
using Bee.Hosting;
using Bee.Api.Client;

var services = new ServiceCollection();
var settings = SystemSettingsLoader.Load(pathOptions);
services.AddBeeFramework(settings.BackendConfiguration, pathOptions);
var sp = services.BuildServiceProvider();

// Feed the backend provider to the UI tier's local-connection adapter.
ApiClientInfo.LocalServiceProvider = sp;
ApiClientInfo.ConnectType = ConnectType.Local;
```

## [4.2.0] and earlier

See git history (`git log --oneline`).
