# Changelog

[繁體中文](CHANGELOG.zh-TW.md)

All notable changes to this project will be documented in this file.

## [4.11.0]

> Bee.NET remains in pre-stable evolution. The theme of this release is "front-end ↔ back-end access goes fully async": the client connection lifecycle and the typed definition cache drop their synchronous-over-asynchronous bridges (`SyncExecutor` is gone), which makes a single-window Avalonia Browser (WASM) head viable. It contains **breaking changes** confined to the client construction / connection surface of `Bee.UI.Core`, `Bee.Api.Client`, and the Avalonia / MAUI heads, plus a **security upgrade** of SQLitePCLRaw.

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

### Breaking Changes

- **`Bee.UI.Avalonia` removes `DynamicForm` and `SingleFormBase`; list duties move to a new `ListView` and single-record duties consolidate in `FormView`, and both move to a new namespace `Bee.UI.Avalonia.Views`** — Aligns with the ERP list/single separation: `ListView` owns the list (load, select, scroll), `FormView` focuses on single-record view/edit (including detail grids). The form/screen-level controls form their own `.Views` namespace, separate from widget-level controls. Code composed via `DynamicForm` / `SingleFormBase` should use `FormView` (single) plus `ListView` (list), with `using Bee.UI.Avalonia.Views;`.
- **`GridControl` moved from namespace `Bee.UI.Avalonia.Controls.Editors` to `Bee.UI.Avalonia.Controls`** — `GridControlBinder` / `GridEditMode` moved with it. The namespace now splits three ways: `.Views` (`FormView` / `ListView`, screen level), `.Controls` (`GridControl`, the composite widget), `.Controls.Editors` (field editors and the lookup / row-edit support UI). Callers that fully-qualified it as `Bee.UI.Avalonia.Controls.Editors.GridControl`, or imported that namespace solely for it, must adjust their usings.
- **`Bee.Db` removes the row-by-row `InsertCommandBuilder` / `UpdateCommandBuilder`** — `DataFormRepository.Save` now goes through DataTable-level `DataAdapter.Update` (see Changed); these "single-row SQL with only changed columns" builders had no remaining production caller and were removed. `DeleteCommandBuilder` / `SelectCommandBuilder` remain (used by `Delete()` / `GetData()`).

### Added

- **Lookup relation mechanism (`Bee.Definition` / `Bee.Api` / `Bee.UI.Avalonia`)** — a definition-driven dialog lookup (design in [ADR-023](docs/adr/adr-023-lookup-relation-mechanism.md)):
  - Definition-layer `DisplayField` / `LookupFields`; relation fields (`RelationProgId` + `RelationFieldMappings`) are auto-resolved by `FormLayoutGenerator` into a `ButtonEdit` with coverage rules (the relation field carries the display, the corresponding `ref_*` fields are not generated twice); `DisplayFields` renders a composite "code - name".
  - Server-side `FormBusinessObject.GetLookup` (with `GetLookupFilter()`) for the dialog list query.
  - Client `LookupPanel` / `LookupDialog` pick components, a built-in `ButtonEdit` lookup flow (display binding, write back mapped fields on pick, clear), and `GridControl` in-cell click-to-open lookup for detail rows.
- **`FormField.ReadOnly` (`Bee.Definition`)** — a read-only property propagated by `FormLayoutGenerator` to `LayoutField` / `LayoutColumn` (the runtime already honored layout-level ReadOnly); computed fields (e.g. amount, total) can be marked read-only.
- **Home-grown `SqliteDataAdapter` (`Bee.Db`)** — Microsoft.Data.Sqlite ships no `DbDataAdapter`; the framework supplies one via `SqliteProviderFactory` so SQLite reads and writes use the adapter path consistently with the other four providers, with no provider-specific fallback.

### Changed

- **`DataFormRepository.Save` now uses DataTable-level IUD (`DataAdapter.Update`)** — each table applies a full-column parameterized Insert/Update/Delete in one pass; a Modified row whose values are unchanged is just a harmless same-value update, eliminating the whole "Modified but no column change → empty SET → `UPDATE would be empty`" class of error (the root cause of re-saving an existing master-detail document). A DataSet with no pending changes is a no-op returning 0 (previously it threw). Design and the SQLite adapter backfill are in [ADR-024](docs/adr/adr-024-dataform-save-dataadapter.md).
- **`FormView` opens a list row read-only on double-click** — double-clicking a list row enters a read-only view rather than going straight to edit; toolbar enablement follows the current mode.

### Fixed

- **SQLite GUID columns get `COLLATE NOCASE`** — SQLite stores GUIDs as case-sensitive TEXT; a lowercase client key drifting from the uppercase seed/provider storage would orphan master-detail rows when `sys_master_rowid` failed to match. GUID columns now carry `COLLATE NOCASE` (CREATE and ALTER ADD alike), making the comparison case-insensitive and aligning SQLite with the other four databases' natural behavior.
- **New rows get non-null defaults from `FormSchema`, and the master link preserves the raw value** — `FormRowDefaults` fills type-appropriate non-null defaults (text→empty string, numeric→0, …) to avoid NOT NULL violations; the master-detail link writes the master's raw `sys_rowid` value (preserving the provider's stored casing) into the detail `sys_master_rowid` rather than round-tripping through `Guid.Parse/ToString`.
- **`GetNewData` skeleton includes `RelationField` columns** — fixes the lookup add flow not bringing back display values.
- **Multi-relation JOIN resolution (`SelectContextBuilder`)** — fixes JOIN generation when one table has multiple relation fields.
- **`ListView` list scrollbar** — scrolls correctly when rows exceed the visible area.

## [4.9.0]

> Bee.NET remains in pre-stable evolution. The theme of this release is "Avalonia editable forms land in full": a field editor suite mapped 1:1 to `ControlType`, a new `GridControl` with in-cell and dialog-based row editing, a form-mode lifecycle (`SingleFormBase` broadcasting `FormMode` to the whole control tree), and a definition-layer `FormEditModes` setting for per-mode editability. The release contains **one breaking change**, confined to the Avalonia family: `DynamicGrid` was removed from `Bee.UI.Avalonia` (its Blazor / MAUI counterparts are unaffected). It also ships a **security upgrade** of the MessagePack dependency.

### Breaking Changes

- **`Bee.UI.Avalonia` `DynamicGrid` removed — `FormView` list rendering moves to `GridControl`** — Row-presentation logic now has a single home in `GridControl`. The Blazor and MAUI `DynamicGrid` controls are unaffected. Note that `GridControl` is a `ContentControl` composite (built-in toolbar + inner grid), not a `DataGrid` subclass: code that used `DataGrid` members directly (`Columns` / `ItemsSource` / `IsReadOnly` / ...) should go through `GridControl.InnerGrid`.

### Security

- **MessagePack 3.1.4 → 3.1.7 (GHSA-hv8m-jj95-wg3x)** — LZ4 decompression could throw an `AccessViolationException` on malicious input (NU1903 high severity). 3.1.7 is the official patched release; all packages that transitively reference MessagePack pick it up.

### Added

- **Field editor suite (`Bee.UI.Avalonia`)** — Seven editors mapped 1:1 to `ControlType` (`TextEdit` / `MemoEdit` / `ButtonEdit` / `DateEdit` / `YearMonthEdit` / `DropDownEdit` / `CheckEdit`), each inheriting the native Avalonia control with `StyleKeyOverride` so the active theme keeps applying. A shared binding state machine lives in `FieldEditorBinder` (explicit `Bind`, ambient auto-wiring, event-driven refresh, echo protection); `FormScope` provides inheritable `DataObject` / `FormMode` attached properties; `FieldEditorFactory` creates editors by `ControlType`. `DynamicForm` now renders through this suite instead of an internal switch-case.
- **`GridControl` — the `LayoutGrid`-driven grid** — A composite control (built-in icon toolbar + inner `DataGrid` exposed as `InnerGrid`) with two explicit binding modes (`FormDataObject` detail table by `TableName`, or a raw `DataTable` for list views) plus `FormScope` ambient binding. Supports in-cell editing per `LayoutColumn.ControlType` (popup-style columns use click-to-swap presentation — see [ADR-021](docs/adr/adr-021-avalonia-datagrid-editing-strategy.md)), add / delete rows via `AllowActions`, and a single `AllowEdit` switch for host control. `DynamicForm` now renders `FormLayout.Details` as caption + bound `GridControl` sections.
- **Row editing: `GridEditMode` (`InCell` / `EditForm`) + `RowEditPanel` / `RowEditDialog`** — A UI-layer edit-mode property (zero definition-layer changes): `EditForm` mode keeps the grid read-only and opens a modal row dialog on double-click or the toolbar Edit button. Backed by a row-edit protocol on `FormDataObject` (`BeginRowEdit` / `CommitRowEdit` / `CancelRowEdit`) with ADO.NET `BeginEdit` semantics pinned by tests.
- **`SingleFormBase` — form-mode owner and broadcaster** — A base class for single-record forms that owns `FormMode` (default `View`) and broadcasts changes to every editor and grid in its subtree. `FormView` now inherits it and gains a mode lifecycle: row selection loads into read-only `View`, the Edit button enters `Edit`, New enters `Add`, and a successful Save / Delete returns to `View`.
- **`FormEditModes` (`Bee.Definition`)** — A `[Flags]` enum (`None` / `Add` / `Edit` / `All`; `View` is never editable so it is not a flag) plus `LayoutField.AllowEditModes` / `LayoutGrid.AllowEditModes` (default `All`). Lets a field express "editable when adding, locked when editing" (e.g. a document-number field) and lets a grid restrict editing to specific form modes. AND-composed with the existing `ReadOnly` / `AllowActions`; defaults are not serialised, so existing layout files are unaffected.
- **`FormDataObject` change-notification events** — `FieldValueChanged` / `DataSetReplaced` events, with an ADO.NET event bridge so every write path (`SetField`, grid cell edits, direct `DataRow` writes) publishes uniformly; plus row-overload `GetField` / `SetField` access.
- **`samples/Avalonia.Editors.Gallery`** — Side-by-side comparison of native controls vs the inherited field editors per `ControlType`, in-cell editing across all column `ControlType`s, and an `EditForm`-mode comparison section; doubles as the usage example for `FormScope` ambient binding.
- **DefineEditor tool improvements** — Switched to the Semi.Avalonia theme, added a VS Code-style Welcome tab, tab dirty markers + tab context menu (close / close others / close right / close saved / close all) + Save All, an unsaved-changes prompt when closing dirty tabs, and macOS menu polish (Open Recent, Close Tab, Hide / Hide Others / Show All).

### Changed

- **`FormView` now loads records in read-only `View` mode** — Previously a loaded record was immediately editable; now the Edit button must be pressed first. Toolbar enablement follows the current mode.

### Fixed

- **`DataTable` deserialization broken by the MessagePack 3.1.5+ built-in blocklist** — MessagePack's new blocklist treats `System.Data.DataTable` as a BinaryFormatter gadget and rejects it during typeless deserialization, which broke `ParameterCollection` wire transfers carrying a `DataTable`. The framework's `DataTable` formatter rebuilds tables column-by-column and never touches BinaryFormatter, so that attack surface does not exist here. `SafeMessagePackSerializerOptions` now lets the fixed framework trust list take precedence; all other types remain double-gated by the built-in blocklist plus the application namespace whitelist.
- **`FormDataObject` async CRUD continuations now resume on the UI thread** — `ConfigureAwait(false)` left `LoadAsync` / `SaveAsync` / `DeleteAsync` / `NewAsync` continuations on thread-pool threads, so the change events drove thread-affine Avalonia controls off the UI thread ("The calling thread cannot access this object"), breaking the post-Save refresh.
- **`DynamicForm` `DateEdit` threw on machines in a non-UTC time zone** — Rendering an initial value built a `DateTimeOffset` with a zero offset from a `Kind=Local` value, failing the whole form render. Caught by the new `Bee.UI.Avalonia.UnitTests` project.
- **`ComboBox` selection box did not display the selected value** — A recycling `FuncDataTemplate` handed the same `TextBlock` instance to both the dropdown item and the selection box; `DropDownEdit` and the in-cell `ComboBox` now use `DisplayMemberBinding`.
- **Grid rows now re-realize after add / delete** — Avalonia's `DataGrid` does not observe `DataView` changes, so a deleted row stayed visible; `GridControl` refreshes rows itself after `AddRow` / `DeleteSelectedRow`.
- **`ButtonEdit` read-only state now disables the embedded lookup button** — Previously only `IsReadOnly` was set and the button stayed clickable (the lookup flow writes mapped fields back). The embedded icon was also restyled to a DatePicker-style chromeless `PathIcon` that adapts to light / dark themes.
- **Demo backend now materializes `st_cache_notify`** — The samples backend creates the table at startup, stopping `CacheNotifyPoller` from logging a warning every polling cycle.

## [4.8.0]

> Bee.NET remains in pre-stable evolution. The theme of this release is "framework default definitions become first-class": the `st_*` system table schemas, framework-shipped `Department` / `Employee` forms, and bootstrap settings templates now ship as embedded resources inside `Bee.Definition.dll`, accessible via the new `Bee.Definition.Defaults` public API. A new `Bee.Cli` dotnet tool (`dotnet bee defines materialize ...`) and a DefineEditor auto-materialise hook turn this into a one-command first-time setup. The release contains **one breaking change**: the framework organisation tables `ft_department` / `ft_employee` were renamed to `st_department` / `st_employee` to align with the rest of the `st_*` namespace.

### Breaking Changes

- **Framework organisation tables `ft_department` / `ft_employee` renamed to `st_department` / `st_employee`** — Aligns the prefix with other framework-owned tables (`st_role` / `st_role_grant` / `st_user_role`) since these tables are required by the framework's organisation / record-scope layer rather than being business data. The `st_` prefix means "framework-owned", orthogonal to which database the table lives in (these two tables still live in the company database). Deployments that already created `ft_department` / `ft_employee` need to `RENAME TABLE` to the new names — see [Table Schema Upgrade Guide §Renaming framework tables](docs/database-schema-upgrade.md) for 4-dialect examples. FormSchema progIds (`Department` / `Employee`), C# type names, and field names are unchanged.

### Added

- **`docs/framework-reserved-names.md`** (bilingual) — Registry of names the framework owns: `st_*` system tables and reserved `progId`s. Naming **rules** still live in [database-naming-conventions.md](docs/database-naming-conventions.md); API method reference still lives in [api-method-reference.md](docs/api-method-reference.md). The new file is the single source of truth for which specific names are reserved.
- **Framework default define files now ship as embedded resources in `Bee.Definition.dll`** — All `st_*` `TableSchema` XMLs (11 files), framework-shipped `Department` / `Employee` `FormSchema` / `FormLayout` / `Language` resources, a minimal `DbCategorySettings.xml` (declaring only the 11 system tables), a `SystemSettings.xml` template with conservative production defaults, and an empty `DatabaseSettings.xml` stub all live under `src/Bee.Definition/Defaults/` and are embedded into the assembly as manifest resources with the `Bee.Definition.Defaults/{relative-path}` naming scheme. Master copies previously lived in `tests/Define/` — only test-specific fixtures (`ft_project`, `PermGateForm`, `Project`, the test-specific `SystemSettings` / `DatabaseSettings` / extended `DbCategorySettings`) remain there.
- **`Bee.Definition.Defaults` API** — Public methods to access the embedded framework defaults: `Defaults.MaterializeTo(path, options)` writes every embedded file into a target directory (skip-existing by default so consumer customisations are never overwritten), `Defaults.ListEmbedded()` enumerates the relative paths, and `Defaults.OpenEmbedded(relativePath)` returns a stream for a single file. Runtime `IDefineStorage` implementations are untouched — they read only what exists on disk under `DefinePath`; the embedded defaults are consulted only via this API (typically once at setup time by a CLI / dev tool).
- **`TestProcessBootstrap.SharedDefinePath`** — Process-wide merged define directory (test-specific fixtures + framework defaults materialised on first call). `BeeTestFixture`'s default `DefinePath` now points at this directory rather than `tests/Define/` directly so tests resolve both layers transparently.
- **`Bee.Cli` dotnet tool (`dotnet bee`)** — Framework-level CLI; ships the `defines` subcommand group for materialising / listing the embedded framework defaults. Install via `dotnet tool install -g Bee.Cli`; usage `dotnet bee defines materialize --path ./Define [--overwrite] [--filter <prefix>]`, `dotnet bee defines list`, `dotnet bee --version`. Lock-stepped to the framework version; the `nuget-publish.yml` workflow packs and publishes it alongside the other packages on tag push. Reserved subcommand groups (`schema`, `tenant`, `samples`) are documented as the naming convention for future framework operations but are not implemented yet.
- **DefineEditor auto-materialises framework defaults on folder open** — When the user opens a `DefinePath` folder, DefineEditor calls `Defaults.MaterializeTo(folder)` (skip-existing, in-process — same code path as the CLI) before scanning the tree. The status bar reports the count when any files were written. New consumers can open an empty folder and immediately see the framework default tree appear.

## [4.7.0]

> Bee.NET remains in pre-stable evolution. The theme of this release is "ERP permissions, i18n, and multi-tenant customisation land in full": three-phase permission model (line-A / line-B / record-scope), localisation infrastructure, multi-tenant customisation overlay, cross-node DB cache invalidation, a DB-backed define storage backend, and a third desktop platform — the new `Bee.UI.Avalonia` package. This release contains **no breaking changes** (existing public API signatures are unchanged). However, the first start-up creates several new system tables (`st_role` / `st_role_grant` / `st_user_role` / `st_cache_notify` / `st_define` / `st_user_company`); deployments that manage DDL out-of-band (instead of letting the framework auto-upgrade the schema) need to add them manually.

### Added

- **New package `Bee.UI.Avalonia`** — Avalonia 12 desktop control layer. Ships `DynamicForm` / `DynamicGrid` / `FormView` controls, a `FormDataObject` data object, and `FileEndpointStorage` (sandbox-friendly endpoint storage that writes under the user's home directory). Includes `samples/Avalonia.Demo` wired to `QuickStart.Server`. See [ADR-020](docs/adr/adr-020-avalonia-datagrid-binding-strategy.md).
- **ERP permission model (line-A + line-B + record-scope)** — A three-phase rollout delivering a complete role / authorisation / row-level permission stack:
    - **line-A — definition layer**: `PermissionModels` registry (register permission models in code), `FormSchema.PermissionModelId` to bind a model to a form, `FormField.ScopeRole` to declare per-field scope roles.
    - **line-B — enforcement layer**: `AuthorizationService.Can` as the unified authorisation check entry point, `st_role` / `st_role_grant` / `st_user_role` role data model plus per-company repositories, `EnterCompany` populating `SessionInfo.Roles`, and a layer-one permission gate inside `FormBusinessObject`.
    - **record-scope — row-level permissions**: user↔employee linkage plus department snapshots, grant per-action scope plus `ScopeResolver`. The FormBO read path filters by scope; the write path performs an authoritative re-query in `Update` / `Delete` against the incoming `sys_rowid` (so callers cannot bypass scope limits by submitting an out-of-scope `sys_rowid`).
    - See [ADR-019](docs/adr/adr-019-permission-authorization-model.md).
- **Localisation infrastructure (i18n)** — `LanguageResource` cross-format resource (XML / JSON / MessagePack), `ILanguageService` + `GetLangText` lookup API, end-to-end automatic `FormSchema` localisation (using `Clone()` to avoid mutating the shared cache), localised `LangEnumName` enum dropdowns, and `SystemBO.GetLanguage` — a JSON-RPC entry point that serves localisation resources to JS frontends.
- **Multi-tenant customisation overlay** — A `CustomizeId` flows through the entire request, the define read path stacks a customise overlay over the base define (base define + customize override), DI integrates the overlay into the `IDefineAccess` resolution pipeline, and `RemoteDefineAccess` clears its cache on tenant switch. See [ADR-016](docs/adr/adr-016-multitenant-customization-overlay.md).
- **DB cache invalidation (cross-node)** — New `st_cache_notify` table plus `ICacheNotifyService.Touch` to publish invalidation signals, a `CacheNotifyPoller` background poller plus a static route registry for convention-based dispatch, and incremental polling by `sys_update_time` to avoid reprocessing rows. In multi-node web deployments, any node mutating a define / `CompanyInfo` / other DB-backed object now invalidates the matching cache entry on every other node within seconds. See [ADR-017](docs/adr/adr-017-db-cache-invalidation.md).
- **DB-backed define storage (`DbDefineStorage`)** — New `st_define` general-purpose define table plus a `DbDefineStorage` implementation plus `ICustomizeDefineReader` wiring for the customisation overlay. `ProgramSettings` and similar defines can now live in the DB (the original XML file path still works; the two storage backends coexist). DI uses lazy resolution to break the construction cycle with `IDbAccessFactory`. See [ADR-018](docs/adr/adr-018-db-define-storage.md).
- **Organisation department tree** — Cross-format `DepartmentTree` (the org hierarchy is serialised nested via `DepartmentNode.Children`), per-company cache, and a `GetDepartmentTree` JSON-RPC API so frontend org pickers can fetch the whole tree in one call.
- **`ProgramItem.BusinessObject`** — A progId entry in `ProgramSettings` can now bind a BO type explicitly, replacing the prior convention-based resolution. Each progId has an unambiguous BO mapping.
- **Define editor tool (`tools/define-editor`)** — An Avalonia desktop tool offering visual editing for nine define types (FormSchema / TableSchema / FormLayout / LanguageResource / ProgramSettings / DbCategorySettings / SystemSettings / UserSettings / DefineRegistry), with a VS Code-style UI plus a native macOS menu. Supports live i18n switching (English / Traditional Chinese), automatic validation, single-file publishing (including the IL3002 workaround), and a double-clickable macOS `.app` bundle (with a hive icon). Non-shipping tool, distributed outside the NuGet packages.

### Changed

- **`DepartmentTree` serialisation shape** — Changed from a flat list to nested via `DepartmentNode.Children`, so the hierarchy is reflected directly instead of being reassembled from `parent_id` on the client.
- **`st_cache_notify` column names** — Removed the `sys_` prefix from non-system columns (the prefix was incorrectly used originally); system columns (`sys_rowid` / `sys_update_time` / etc.) keep the prefix.
- **`CacheNotifyPoller` polling strategy** — Reverted to incremental fetch by `sys_update_time` (it briefly switched to an alternative strategy that regressed multi-tenant performance), so it is now `O(1)` incremental rather than a full-table scan per cycle.

### Fixed

- **MySQL `ALTER ADD Guid NOT NULL DEFAULT (UUID())` is replication-unsafe** — Under MySQL statement-binlog, an `ALTER TABLE ... ADD column UUID NOT NULL DEFAULT (UUID())` on an existing table is replication-unsafe. The dialect now splits it into two statements (`ADD COLUMN` with a constant default, then `ALTER COLUMN SET DEFAULT (UUID())`). The fresh `CREATE TABLE` path was already safe and is unaffected.
- **Oracle `ALTER MODIFY` ORA-01442** — `ALTER TABLE ... MODIFY column NOT NULL` raises ORA-01442 when re-applied to a column that is already NOT NULL. The dialect now emits the hint only when nullability is actually changing.
- **Oracle String / Text columns always built as nullable** — Oracle treats `''` as `NULL`, so a `String NOT NULL` column whose normal value is the empty string fails fresh `CREATE TABLE` with ORA-01400. The dialect now always allows NULL for String / Text columns, regardless of the table-schema declaration.
- **Three MAUI `DynamicForm` fixes** — `SetField` is now idempotent (writing the same value no longer round-trips), `ConvertToColumnValue` got a non-null fallback (so certain binding paths no longer push `null` into a non-nullable column), and `ReloadList` now preserves `sys_rowid` (so row identity isn't lost after a reload).
- **`ObjectCaching` CI race condition** — Replaced `PhysicalFileProvider` with a lazy `FileModificationToken`, eliminating the file-system watcher race that flaked CI (and dropped the now-unused `Microsoft.Extensions.FileProviders.Physical` package reference).
- **`DemoBusinessObjectFactory` missing `ILanguageService` injection** — The samples-side factory missed the new `ILanguageService` dependency introduced by the i18n phase, so demo BOs failed to resolve. Fixed.
- **`RolePermissionRepository` SQL concatenation missing space** — Flagged by SonarCloud S2857. Functionally harmless but made SQL unreadable in logs.

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
