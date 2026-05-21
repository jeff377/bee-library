# Changelog

[繁體中文](CHANGELOG.zh-TW.md)

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
- **`UserMessageException` (`Bee.Base.Exceptions`)** — Dedicated type for "business flow interruption signal", replacing the practice of using BCL exceptions (`InvalidOperationException` / `ArgumentException` …) to deliver user-facing messages. New code should use this type for any message intended for the end user.
- **`JsonRpcErrorCode.UserMessage = -32099`** — Catch-all container for user-facing business messages, placed at the tail of the server-defined error range to separate it from system errors (`InternalError = -32000`). `JsonRpcExecutor` writes this code for all user-facing exceptions, enabling the client `ApiConnector` to reconstruct the exception type precisely.
- **`Bee.Base.Exceptions` namespace** — Consolidates exception-related types; the existing `ExceptionExtensions` is moved into this namespace.

### Changed

- **`IFormRepositoryFactory.CreateDataFormRepository`** — Now takes an additional `Guid accessToken` parameter, routed through `IRepositoryDatabaseRouter`. BO code should use the new `BusinessObject.CreateDataFormRepository(progId)` helper, which threads the token automatically.
- **`IDataFormRepository.GetList`** — Now returns `DataFormListResult` (carrying `Table` + `Paging`) and accepts an optional `PagingOptions? paging`.
- **`CompanyInfo.LogDatabaseId` removed** — `DbScope.Log` now resolves to the fixed `databaseId = "log"` (pre-EnterCompany methods can write audit logs). Cross-company log isolation is handled by upcoming `sys_company_rowid` row-level partitioning, not separate physical log DBs per company.
- **`SelectCommandBuilder` behaviour on unknown table name** — Throws `InvalidOperationException` (was `KeyNotFoundException`), aligning with the Insert / Update / Delete builders.
- **`JsonRpcExecutor` error code alignment** (behaviour change) — No longer hardcodes `code = -1` when catching exceptions; instead writes the corresponding `JsonRpcErrorCode` based on exception type (whitelisted exceptions → `UserMessage` `-32099`, others → `InternalError` `-32000`). Clients that depended on `code == -1` to detect errors must migrate to the new enum.
- **`ApiConnector.FinalizeResponse` reconstructs exceptions by code** (behaviour change) — `code == UserMessage` throws `UserMessageException(message)` (clean message, no prefix); other codes keep the previous `InvalidOperationException($"API error: {code} - {message}")` wrapping. Existing `catch (Exception)` still catches `UserMessageException`; callers are encouraged to migrate to `catch (UserMessageException)` + `catch (Exception)` for explicit routing.
- **`ExceptionExtensions` namespace** — Moved from `Bee.Base` to `Bee.Base.Exceptions`; call sites need to add `using Bee.Base.Exceptions;`.

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
