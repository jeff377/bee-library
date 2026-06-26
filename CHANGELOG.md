# Changelog

[繁體中文](CHANGELOG.zh-TW.md)

All notable changes to this project will be documented in this file.

## [4.12.0]

> Bee.NET remains in pre-stable evolution. This release makes the `Bee.UI.Avalonia` control family responsive for phone / narrow viewports and makes the `Bee.Definition` types deserialize under the AOT reflection-only XmlSerializer — together these enable the Avalonia **iOS** and **Android** heads. No breaking changes.

📄 Full notes & design context: [docs/changelogs/4.12.0.md](docs/changelogs/4.12.0.md)

### Added

- `Bee.UI.Avalonia`: `FormView` responsive layout — master fields reflow multi-column → single column and detail grids switch `InCell` → `EditForm` below `CompactWidthThreshold` (default 600 DIP).
- `Bee.UI.Avalonia`: `ListView` card layout on narrow viewports — one card per row instead of the wide column grid.
- `Bee.UI.Avalonia`: `RowEditPanel` (EditForm) reflows 1 ↔ 2 columns by host width; `RowEditDialog` desktop window is resizable.

### Fixed

- `Bee.Definition`: definition collection types deserialize under the AOT reflection-only XmlSerializer (single public `Add(T)`, parameterless constructors) — enables the iOS / Android heads. Call syntax and XML format unchanged. [ADR-025](docs/adr/adr-025-define-types-aot-xmlserializer-compat.md)
- `Bee.UI.Avalonia`: `RowEditDialog` renders through an `OverlayLayer` on single-view hosts (iOS / Android / browser) instead of a native `Window` (which crashed).
- `Bee.UI.Avalonia`: `FormView` body scrolls vertically so controls below the fold stay reachable in a narrow single-column layout.
- `Bee.UI.Avalonia`: `GridControl` lookup cells show the open-dialog magnifier icon in edit state.

## [4.11.0]

> Bee.NET remains in pre-stable evolution. The theme of this release is "front-end ↔ back-end access goes fully async": the client connection lifecycle and the typed definition cache drop their synchronous-over-asynchronous bridges (`SyncExecutor` is gone), which makes a single-window Avalonia Browser (WASM) head viable. It contains **breaking changes** confined to the client construction / connection surface of `Bee.UI.Core`, `Bee.Api.Client`, and the Avalonia / MAUI heads, plus a **security upgrade** of SQLitePCLRaw.

📄 Full notes & design context: [docs/changelogs/4.11.0.md](docs/changelogs/4.11.0.md)

### Breaking Changes

- Remove synchronous client APIs in favor of async — `ClientInfo.Initialize(string)` / `SetEndpoint`, `ApiConnectValidator.Validate`, `IUIViewService.ShowApiConnect` (use the `...Async` counterparts); `SyncExecutor` removed.
- Rename `RemoteDefineAccess` → `ClientDefineAccess` (now at the `Bee.Api.Client` root) and `LocalDefineAccess` → `CacheDefineAccess`.

### Security

- Upgrade SQLitePCLRaw to 3.x (GHSA-2m69-gcr7-jv3q), replacing the NU1903 suppression.

### Added

- `Bee.UI.Avalonia`: dialog overlay path (`OverlayLayer`) for single-window hosts, enabling lookup / row-edit dialogs in the Avalonia Browser (WASM) head.
- `Bee.UI.Avalonia`: `FormDataObject` `RowAdded` / `RowDeleted` / `IsDirtyChanged` events.

### Changed

- `Bee.UI.Avalonia`: field editors commit on leave / Enter instead of per keystroke.
- `Bee.UI.Avalonia`: field captions mark read-only (parenthesized, underline-only) and required (blue) uniformly.
- `Bee.Definition`: `FormLayoutGenerator` no longer repeats the form name on the generated main section.

### Fixed

- `Bee.UI.Avalonia`: `GridControl.Bind` self-initializes edit state on explicit bind.

### Upgrade Guide

```diff
- ClientInfo.Initialize(endpoint);
- ClientInfo.SetEndpoint(endpoint);
+ await ClientInfo.InitializeAsync(endpoint);
+ await ClientInfo.SetEndpointAsync(endpoint);
```
```diff
- RemoteDefineAccess access = ...;   // LocalDefineAccess cache = ...;
+ ClientDefineAccess access = ...;   // CacheDefineAccess  cache = ...;
```

## [4.10.0]

> Bee.NET remains in pre-stable evolution. The theme of this release is "the lookup relation mechanism lands in full": relation fields automatically become dialog-based lookup editors with composite "code - name" display, with two pick entry points (master `ButtonEdit` and detail in-cell), backed by a server-side `GetLookup`. It also splits the Avalonia single-record and list concerns into `FormView` / `ListView` (the ERP list/single separation), and consolidates DataForm persistence onto a DataTable-level `DataAdapter` path (a home-grown `SqliteDataAdapter` lets SQLite use the adapter path too). The release contains **several breaking changes**, confined to the construction surface of `Bee.UI.Avalonia` and `Bee.Db`.

📄 Full notes & design context: [docs/changelogs/4.10.0.md](docs/changelogs/4.10.0.md)

### Breaking Changes

- `Bee.UI.Avalonia`: remove `DynamicForm` / `SingleFormBase`; list duties move to new `ListView`, single-record duties consolidate in `FormView`, both move to new namespace `Bee.UI.Avalonia.Views`.
- `Bee.UI.Avalonia`: `GridControl` (with `GridControlBinder` / `GridEditMode`) moved from `Bee.UI.Avalonia.Controls.Editors` to `Bee.UI.Avalonia.Controls`.
- `Bee.Db`: remove row-by-row `InsertCommandBuilder` / `UpdateCommandBuilder` (`DeleteCommandBuilder` / `SelectCommandBuilder` remain).

### Added

- `Bee.Definition` / `Bee.Api` / `Bee.UI.Avalonia`: definition-driven dialog lookup relation mechanism — `DisplayField` / `LookupFields`, auto-resolved `ButtonEdit`, server-side `FormBusinessObject.GetLookup`, client `LookupPanel` / `LookupDialog` and `GridControl` in-cell lookup ([ADR-023](docs/adr/adr-023-lookup-relation-mechanism.md)).
- `Bee.Definition`: `FormField.ReadOnly` propagated by `FormLayoutGenerator` to `LayoutField` / `LayoutColumn`.
- `Bee.Db`: home-grown `SqliteDataAdapter` via `SqliteProviderFactory` so SQLite uses the adapter path.

### Changed

- `Bee.Db`: `DataFormRepository.Save` now uses DataTable-level IUD (`DataAdapter.Update`); no-change DataSet is a no-op returning 0 ([ADR-024](docs/adr/adr-024-dataform-save-dataadapter.md)).
- `Bee.UI.Avalonia`: `FormView` opens a list row read-only on double-click.

### Fixed

- `Bee.Db`: SQLite GUID columns get `COLLATE NOCASE` (CREATE and ALTER ADD).
- `Bee.Db`: new rows get non-null defaults from `FormSchema` via `FormRowDefaults`; master link writes the raw `sys_rowid` into detail `sys_master_rowid`.
- `GetNewData` skeleton includes `RelationField` columns.
- `SelectContextBuilder`: fix multi-relation JOIN resolution.
- `Bee.UI.Avalonia`: `ListView` list scrollbar scrolls correctly when rows exceed the visible area.

## [4.9.0]

> Bee.NET remains in pre-stable evolution. The theme of this release is "Avalonia editable forms land in full": a field editor suite mapped 1:1 to `ControlType`, a new `GridControl` with in-cell and dialog-based row editing, a form-mode lifecycle (`SingleFormBase` broadcasting `FormMode` to the whole control tree), and a definition-layer `FormEditModes` setting for per-mode editability. The release contains **one breaking change**, confined to the Avalonia family: `DynamicGrid` was removed from `Bee.UI.Avalonia` (its Blazor / MAUI counterparts are unaffected). It also ships a **security upgrade** of the MessagePack dependency.

📄 Full notes & design context: [docs/changelogs/4.9.0.md](docs/changelogs/4.9.0.md)

### Breaking Changes

- `Bee.UI.Avalonia`: removes `DynamicGrid`; `FormView` list rendering moves to `GridControl` (a `ContentControl` composite — use `GridControl.InnerGrid` for `DataGrid` members). Blazor / MAUI `DynamicGrid` unaffected.

### Security

- MessagePack: `3.1.4` → `3.1.7` (GHSA-hv8m-jj95-wg3x) — fixes LZ4 `AccessViolationException` on malicious input (NU1903 high).

### Added

- `Bee.UI.Avalonia`: field editor suite — seven editors mapped 1:1 to `ControlType` (`TextEdit` / `MemoEdit` / `ButtonEdit` / `DateEdit` / `YearMonthEdit` / `DropDownEdit` / `CheckEdit`), with `FieldEditorBinder`, `FormScope` attached properties, and `FieldEditorFactory`; `DynamicForm` renders through it.
- `Bee.UI.Avalonia`: adds `GridControl` — `LayoutGrid`-driven composite grid (`InnerGrid`) with two binding modes, `FormScope` ambient binding, in-cell editing per `LayoutColumn.ControlType`, `AllowActions` add/delete, and `AllowEdit` ([ADR-021](docs/adr/adr-021-avalonia-datagrid-editing-strategy.md)).
- `Bee.UI.Avalonia`: adds `GridEditMode` (`InCell` / `EditForm`) + `RowEditPanel` / `RowEditDialog`, backed by `FormDataObject` row-edit protocol (`BeginRowEdit` / `CommitRowEdit` / `CancelRowEdit`).
- `Bee.UI.Avalonia`: adds `SingleFormBase` owning and broadcasting `FormMode`; `FormView` inherits it with a View/Edit/Add mode lifecycle.
- `Bee.Definition`: adds `FormEditModes` `[Flags]` enum + `LayoutField.AllowEditModes` / `LayoutGrid.AllowEditModes` (default `All`); AND-composed with `ReadOnly` / `AllowActions`, defaults not serialised.
- `Bee.UI.Avalonia`: `FormDataObject` adds `FieldValueChanged` / `DataSetReplaced` events with ADO.NET bridge, plus row-overload `GetField` / `SetField`.
- `samples/Avalonia.Editors.Gallery`: native vs inherited editor comparison, in-cell editing, and `EditForm`-mode section.
- DefineEditor: Semi.Avalonia theme, Welcome tab, tab dirty markers + context menu + Save All, unsaved-changes prompt, macOS menu polish.

### Changed

- `Bee.UI.Avalonia`: `FormView` now loads records in read-only `View` mode; Edit button required to edit.

### Fixed

- `Bee.Api`: `DataTable` deserialization broken by MessagePack 3.1.5+ blocklist; `SafeMessagePackSerializerOptions` now lets the framework trust list take precedence.
- `Bee.UI.Avalonia`: `FormDataObject` async CRUD continuations now resume on the UI thread (removed `ConfigureAwait(false)`).
- `Bee.UI.Avalonia`: `DynamicForm` `DateEdit` no longer throws on non-UTC time zones.
- `Bee.UI.Avalonia`: `ComboBox` selection box now shows the selected value; `DropDownEdit` / in-cell `ComboBox` use `DisplayMemberBinding`.
- `Bee.UI.Avalonia`: `GridControl` re-realizes rows after `AddRow` / `DeleteSelectedRow`.
- `Bee.UI.Avalonia`: `ButtonEdit` read-only state now disables the embedded lookup button; icon restyled to chromeless `PathIcon`.
- Demo backend now materializes `st_cache_notify`, stopping the `CacheNotifyPoller` warning.

## [4.8.0]

> Bee.NET remains in pre-stable evolution. The theme of this release is "framework default definitions become first-class": the `st_*` system table schemas, framework-shipped `Department` / `Employee` forms, and bootstrap settings templates now ship as embedded resources inside `Bee.Definition.dll`, accessible via the new `Bee.Definition.Defaults` public API. A new `Bee.Cli` dotnet tool (`dotnet bee defines materialize ...`) and a DefineEditor auto-materialise hook turn this into a one-command first-time setup. The release contains **one breaking change**: the framework organisation tables `ft_department` / `ft_employee` were renamed to `st_department` / `st_employee` to align with the rest of the `st_*` namespace.

📄 Full notes & design context: [docs/changelogs/4.8.0.md](docs/changelogs/4.8.0.md)

### Breaking Changes

- Framework organisation tables `ft_department` / `ft_employee` renamed to `st_department` / `st_employee`; deployments must `RENAME TABLE` — see [Table Schema Upgrade Guide §Renaming framework tables](docs/database-schema-upgrade.md). FormSchema progIds, C# type names, and field names unchanged.

### Added

- `docs/framework-reserved-names.md` (bilingual): registry of framework-reserved names (`st_*` system tables, reserved `progId`s).
- `Bee.Definition`: framework default define files (11 `st_*` `TableSchema` XMLs, `Department` / `Employee` `FormSchema` / `FormLayout` / `Language`, minimal `DbCategorySettings.xml`, `SystemSettings.xml` template, empty `DatabaseSettings.xml`) now ship as embedded resources under `Bee.Definition.Defaults/{relative-path}`.
- `Bee.Definition.Defaults` API: `Defaults.MaterializeTo(path, options)` (skip-existing), `Defaults.ListEmbedded()`, `Defaults.OpenEmbedded(relativePath)`; runtime `IDefineStorage` untouched.
- `TestProcessBootstrap.SharedDefinePath`: process-wide merged define directory; `BeeTestFixture` default `DefinePath` now points here.
- `Bee.Cli` dotnet tool (`dotnet bee`): `defines materialize --path ./Define [--overwrite] [--filter <prefix>]`, `defines list`, `--version`; lock-stepped to framework version, published via `nuget-publish.yml`. Reserved subcommand groups (`schema`, `tenant`, `samples`) not yet implemented.
- DefineEditor auto-materialises framework defaults (`Defaults.MaterializeTo`, skip-existing) on folder open; status bar reports written count.

## [4.7.0]

> Bee.NET remains in pre-stable evolution. The theme of this release is "ERP permissions, i18n, and multi-tenant customisation land in full": three-phase permission model (line-A / line-B / record-scope), localisation infrastructure, multi-tenant customisation overlay, cross-node DB cache invalidation, a DB-backed define storage backend, and a third desktop platform — the new `Bee.UI.Avalonia` package. This release contains **no breaking changes** (existing public API signatures are unchanged). However, the first start-up creates several new system tables (`st_role` / `st_role_grant` / `st_user_role` / `st_cache_notify` / `st_define` / `st_user_company`); deployments that manage DDL out-of-band (instead of letting the framework auto-upgrade the schema) need to add them manually.

📄 Full notes & design context: [docs/changelogs/4.7.0.md](docs/changelogs/4.7.0.md)

### Added

- `Bee.UI.Avalonia`: new Avalonia 12 desktop control package — `DynamicForm` / `DynamicGrid` / `FormView`, `FormDataObject`, `FileEndpointStorage`; `samples/Avalonia.Demo` included. See [ADR-020](docs/adr/adr-020-avalonia-datagrid-binding-strategy.md).
- ERP permission model (line-A + line-B + record-scope): `PermissionModels` registry, `FormSchema.PermissionModelId`, `FormField.ScopeRole`, `AuthorizationService.Can`, `st_role` / `st_role_grant` / `st_user_role` data model, `EnterCompany` populating `SessionInfo.Roles`, FormBO permission gate, and `ScopeResolver` row-level filtering with authoritative `sys_rowid` re-query in `Update` / `Delete`. See [ADR-019](docs/adr/adr-019-permission-authorization-model.md).
- i18n: `LanguageResource` (XML / JSON / MessagePack), `ILanguageService` + `GetLangText`, automatic `FormSchema` localisation, `LangEnumName` enum dropdowns, and `SystemBO.GetLanguage` JSON-RPC entry point.
- Multi-tenant customisation overlay: `CustomizeId` flows through the request, define read path stacks customise overlay over base define, integrated into `IDefineAccess`, `RemoteDefineAccess` clears cache on tenant switch. See [ADR-016](docs/adr/adr-016-multitenant-customization-overlay.md).
- DB cache invalidation (cross-node): `st_cache_notify` table + `ICacheNotifyService.Touch`, `CacheNotifyPoller` background poller with static route registry, incremental polling by `sys_update_time`. See [ADR-017](docs/adr/adr-017-db-cache-invalidation.md).
- `DbDefineStorage`: `st_define` table + `DbDefineStorage` + `ICustomizeDefineReader`; defines can live in DB (XML path still works), lazy DI resolution to break the `IDbAccessFactory` cycle. See [ADR-018](docs/adr/adr-018-db-define-storage.md).
- Organisation department tree: cross-format `DepartmentTree` (nested via `DepartmentNode.Children`), per-company cache, `GetDepartmentTree` JSON-RPC API.
- `ProgramItem.BusinessObject`: a progId can bind a BO type explicitly, replacing convention-based resolution.
- `tools/define-editor`: Avalonia desktop tool for visual editing of nine define types, with live i18n, validation, single-file publishing, and a macOS `.app` bundle. Non-shipping tool.

### Changed

- `DepartmentTree`: serialisation changed from flat list to nested via `DepartmentNode.Children`.
- `st_cache_notify`: removed the `sys_` prefix from non-system columns; system columns keep it.
- `CacheNotifyPoller`: reverted to `O(1)` incremental fetch by `sys_update_time`.

### Fixed

- MySQL: `ALTER ADD Guid NOT NULL DEFAULT (UUID())` is replication-unsafe under statement-binlog; dialect now splits into `ADD COLUMN` (constant default) + `ALTER COLUMN SET DEFAULT (UUID())`.
- Oracle: `ALTER MODIFY ... NOT NULL` raised ORA-01442 on already-NOT-NULL columns; hint now emitted only when nullability changes.
- Oracle: String / Text columns now always built nullable (`''` = `NULL`, ORA-01400 on fresh `CREATE TABLE`).
- MAUI `DynamicForm`: `SetField` now idempotent, `ConvertToColumnValue` got a non-null fallback, `ReloadList` preserves `sys_rowid`.
- `ObjectCaching`: replaced `PhysicalFileProvider` with lazy `FileModificationToken` to fix a CI race (dropped `Microsoft.Extensions.FileProviders.Physical` reference).
- `DemoBusinessObjectFactory`: added missing `ILanguageService` injection.
- `RolePermissionRepository`: SQL concatenation missing space (SonarCloud S2857).

## [4.6.0]

> Bee.NET remains in pre-stable evolution. The theme of this release is "open up JSON-RPC to JavaScript frontends": seven FormBO / SystemBO CRUD / Session methods now ship with `ProtectionLevel = Public`, two new JSON-native getters (`GetFormSchema` / `GetFormLayout`) are introduced, and Plain-path `DataSet` deserialization plus a Blazor WebAssembly RSA blocker are fixed. The `MasterKeySource` default flips to `Environment`; under strict SemVer this would be a major bump, but under the pre-stable policy it ships as a minor.

📄 Full notes & design context: [docs/changelogs/4.6.0.md](docs/changelogs/4.6.0.md)

### Added

- `Bee.Business`: `SystemBO.GetFormSchema` / `GetFormLayout` — JSON-native getters returning `FormSchema` / `FormLayout`; `.NET` adds `SystemApiConnector.GetFormSchemaAsync` / `GetFormLayoutAsync`; both `Public + Authenticated`. See [ADR-014](docs/adr/adr-014-jsonrpc-plain-public-default.md).
- `docs`: new bilingual [`docs/jsonrpc-frontend-integration.md`](docs/jsonrpc-frontend-integration.md) — wire format, headers, auth flow, method catalog, `JsonRpcErrorCode` mapping, TypeScript wrapper.

### Changed

- `Bee.Definition`: `MasterKeySource` default changed to `Environment` (reads `$BEE_MASTER_KEY` instead of `Master.key`) (**breaking**); explicit `<Type>File</Type>` hosts unaffected. See [ADR-015](docs/adr/adr-015-master-key-environment-default.md).
- `Bee.Business`: seven BO methods downgraded to `ProtectionLevel = Public` — `FormBO.GetNewData` / `GetData` / `Save` / `Delete`, `SystemBO.EnterCompany` / `LeaveCompany` / `Logout` (`Encrypted` → `Public`, still `Authenticated`); backward compatible. See [ADR-014](docs/adr/adr-014-jsonrpc-plain-public-default.md).
- `Bee.Definition`: `FormSchema.MasterTable` is now `[JsonIgnore]` (XML / MessagePack unaffected); JS / TS clients read `tables[0]` instead of `masterTable`.

### Fixed

- `Bee.Base`: `RsaCryptor` exports keys in PEM (SPKI / PKCS#1) instead of XML, plus `OperatingSystem.IsBrowser()` fallback — unblocks Blazor WebAssembly login.
- `Bee.Api.Core`: `ApiInputConverter` registers Plain-path `DataTableJsonConverter` / `DataSetJsonConverter` / `JsonStringEnumConverter`, fixing empty-rows `DataSet` and "DataSet has no pending changes" on `Save`.
- `Bee.UI.Maui`: `DynamicForm` exposes public `Refresh()` driving `Rebuild()`, so the form rebuilds after in-place `DataSet` mutation (New / Save / Delete).

### Upgrade Guide

```diff
- const masterTable = formSchema.masterTable;
+ const masterTable = formSchema.tables[0];
```

## [4.5.0]

> Bee.NET remains in pre-stable evolution. This release introduces three frontend package layers (`Bee.UI.Core` cross-platform shared layer, `Bee.UI.Maui` MAUI mobile/desktop controls, `Bee.Web.Blazor.Server` / `Bee.Web.Blazor.Wasm` Blazor RCLs) and flips the API connector interfaces to async-only. Under strict SemVer the signature changes would be a major bump; under the pre-stable policy it ships as a minor.

📄 Full notes & design context: [docs/changelogs/4.5.0.md](docs/changelogs/4.5.0.md)

### Added

- `Bee.UI.Core`: new cross-platform UI shared layer (shared view models, `FormDataObject`, `SystemApiConnector`, `ClientInfo`), merged from `bee-ui-core`. See [ADR-013](docs/adr/adr-013-frontend-api-connection-strategy.md).
- `Bee.UI.Maui`: new MAUI control layer with `DynamicForm` / `DynamicGrid` / `FormPage` and `MauiPreferenceEndpointStorage`; defaults to `net10.0`, platform TFMs via `-p:BeeUiMauiFullPlatforms=true`.
- `Bee.Web.Blazor.Server` / `Bee.Web.Blazor.Wasm`: new Blazor RCLs shipping `DynamicForm` / `DynamicGrid` / `FormPage`, `BeeAccessTokenProvider`, `BeeLoginPanel`, and `AddBeeBlazor`.
- `UserMessageException` + `JsonRpcErrorCode.UserMessage`: server throws rehydrated by `ApiConnector` into client-side `UserMessageException` for direct `.Message` display.
- `FormBusinessObject`: added `GetNewData` / `GetData` / `Save` / `Delete`, completing single-row CRUD on `IFormBusinessObject`.
- `samples/`: new demo family — `QuickStart.Server` + `QuickStart.Console`, `Blazor.Server.Demo` + `Blazor.Wasm.Demo`, `Maui.Demo`; share `Bee.Samples.Shared` and ship `.smoke.yaml`.

### Changed

- `IApiConnector` / `IFormApiConnector` / `ISystemApiConnector`: now async-only; sync methods removed, use `*Async` variants.
- `ExceptionExtensions`: moved from `Bee.Base` to `Bee.Base.Exceptions`.
- `ClientInfo`: now a static class; `ClientInfo.SystemApiConnector.Initialize()` is async. See [ADR-013](docs/adr/adr-013-frontend-api-connection-strategy.md).

### Upgrade Guide

```diff
- var data = connector.GetData(progId, formData);
+ var data = await connector.GetDataAsync(progId, formData);
```

```diff
  using Bee.Base;
+ using Bee.Base.Exceptions;

  ex.Unwrap();
```

## [4.4.0]

> Bee.NET remains in pre-stable evolution; the public API surface has no external consumers yet, so minor releases are allowed to carry API moves and limited breaking changes. This release includes interface signature changes (`IFormRepositoryFactory.CreateDataFormRepository`, `IDataFormRepository.GetList`) and a property removal (`CompanyInfo.LogDatabaseId`). Under strict SemVer this would be a major bump; under the pre-stable policy it ships as a minor.

📄 Full notes & design context: [docs/changelogs/4.4.0.md](docs/changelogs/4.4.0.md)

### Added

- `Bee.Business`: `FormBO.GetList` unified list query via `IDataFormRepository` with `PagingOptions`/`PagingInfo`; `FormApiConnector.GetList`/`GetListAsync` client entry.
- `Bee.Business`: `SystemBO` adds `EnterCompany`/`LeaveCompany`/`Logout`; `SessionInfo` gains nullable `CompanyId`; `Login` declared on `ISystemBusinessObject`; new `CompanyInfo` + `ICompanyInfoService`. See [ADR-012](docs/adr/adr-012-session-company-context.md).
- `Bee.Business`: `DbScope` enum (`Common`/`Company`/`Log`) + `IRepositoryDatabaseRouter`; `BusinessObject.ResolveDatabaseId(DbScope)` and `CreateDataFormRepository(progId)` protected helpers. See [ADR-010](docs/adr/adr-010-logical-database-category.md).
- `Bee.Db`: `SelectCommandBuilder` paging (`OFFSET/FETCH` or `LIMIT/OFFSET`) across 5 dialects + new `BuildCount`.
- `Bee.ObjectCaching`: `KeyObjectCache<T>` negative caching (default 5-min absolute expiration, virtual `GetNegativePolicy` to override/disable). See [ADR-009](docs/adr/adr-009-cache-implementation.md).
- `Bee.Business`: `IBusinessObjectFactory` typed wrappers `CreateFormBO(token, progId)` / `CreateSystemBO(token)`.
- `Bee.Repository`: `st_company`/`st_user_company` system tables + `ICompanyRepository`/`IUserCompanyRepository`; default common `DbCategorySettings` includes both.
- `JsonRpcErrorCode`: new `CompanyNotEntered` (-32002, HTTP 409) and `CompanyAccessDenied` (-32003, HTTP 403).

### Changed

- `IFormRepositoryFactory.CreateDataFormRepository`: adds `Guid accessToken` parameter, routed via `IRepositoryDatabaseRouter`.
- `IDataFormRepository.GetList`: returns `DataFormListResult` (`Table` + `Paging`) and accepts optional `PagingOptions? paging`.
- `CompanyInfo.LogDatabaseId` removed: `DbScope.Log` resolves to fixed `databaseId = "log"`.
- `SelectCommandBuilder`: unknown table name now throws `InvalidOperationException` (was `KeyNotFoundException`).

### Upgrade Guide

```diff
- var repo = factory.CreateDataFormRepository("Employee");
+ var repo = factory.CreateDataFormRepository("Employee", accessToken);
```

```diff
- DataTable table = repo.GetList(filter, sortFields, fields);
+ DataFormListResult result = repo.GetList(filter, sortFields, fields, paging: null);
+ DataTable table = result.Table;
```

```diff
- var logDbId = companyInfo.LogDatabaseId;
+ var logDbId = "log";  // Fixed framework routing; cross-company isolation via row-level partitioning
```

## [4.3.0]

> Bee.NET is in pre-stable evolution; minor releases may include namespace moves while the public surface still has no external consumers. This release moves `AddBeeFramework` to a dedicated package — strictly a SemVer-major change, but treated as minor under the pre-stable policy.

📄 Full notes & design context: [docs/changelogs/4.3.0.md](docs/changelogs/4.3.0.md)

### Added

- `Bee.Hosting`: new package — framework composition root registering all backend services (`IDefineAccess`, `IDbAccessFactory`, `IBusinessObjectFactory`, `JsonRpcExecutor`, etc.) into any `IServiceCollection` without depending on ASP.NET Core.

### Changed

- `Bee.Hosting`: `BeeFrameworkServiceCollectionExtensions.AddBeeFramework` moved here from `Bee.Api.AspNetCore` (namespace `Bee.Api.AspNetCore` → `Bee.Hosting`).
- `Bee.Api.AspNetCore`: now contains only ASP.NET Core integration (`UseBeeFramework` + `ApiServiceController`); 4 previous project references consolidated under `Bee.Hosting`.

### Upgrade Guide

```diff
+ using Bee.Hosting;
  using Bee.Api.AspNetCore;

  var settings = SystemSettingsLoader.Load(pathOptions);
  services.AddBeeFramework(settings.BackendConfiguration, pathOptions);
  app.UseBeeFramework();
```

```diff
  <!-- *.csproj -->
- <PackageReference Include="Bee.Api.AspNetCore" Version="4.2.*" />
+ <PackageReference Include="Bee.Hosting" Version="4.3.*" />
```

## [4.2.0] and earlier

See git history (`git log --oneline`).
