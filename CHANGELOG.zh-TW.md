# 版本變更記錄

[English](CHANGELOG.md)

本檔記錄專案的所有重要變更。

## [4.13.0]

> Bee.NET 仍處 pre-stable 演進階段。本版新增 ERP 級數值層：欄位上的語意 `NumberKind` 驅動顯示格式、捨入策略與小數位數來源 —— **round-then-sum** 合計、逐欄 **多幣別**（SAP CUKY 式，JPY=0 / USD=2 / BHD=3）與 **計量單位**（SAP UNIT 式，KG=3 / PCS=0）位數皆於 runtime 解析，並附 Avalonia `NumericEdit` 編輯器。所有新增皆向後相容（新成員預設空；`CompanyInfo` 尾端加 MessagePack key）。無破壞性變更。[ADR-026](docs/adr/adr-026-numeric-semantics-rounding.md)

📄 詳細變更與設計脈絡：[docs/changelogs/4.13.0.zh-TW.md](docs/changelogs/4.13.0.zh-TW.md)

### 新增

- `Bee.Definition`：`FormField` 與 `LayoutFieldBase` 上的 `NumberKind` 語意（`Quantity` / `Weight` / `Amount` / `Percent` / `UnitPrice` / `Cost` / `ExchangeRate`），驅動顯示格式、捨入策略與位數來源。[ADR-026](docs/adr/adr-026-numeric-semantics-rounding.md)
- `Bee.Definition`：`NumberFormatResolver`（`ResolveDecimals` / `ResolveFormat` / `RoundByKind` / `RoundCash`）與 `NumberFormatApplier.Bake` —— round-then-sum 合計、兩層捨入（幣別/單位自然小數 + 選配現金捨入）、顯示格式 bake 於 per-call schema clone（絕不 mutate 快取）。
- `Bee.Definition`：`CurrencySettings` 幣別主檔（`DefineType.CurrencySettings`，curated ISO 4217，SAP TCURX 式），透過 `FormField.CurrencyField` / `FormSchema.CurrencyField` 逐欄綁定；金額位數跟幣別走。
- `Bee.Definition`：`UnitSettings` 計量單位主檔（`DefineType.UnitSettings`，SAP T006 式），透過 `FormField.UnitField` 逐欄綁定；數量／重量位數跟單位走。
- `Bee.Definition`：`CompanyInfo` 新增 `NumberFormats`、`DefaultCurrency`、`CashRounding`、`AllowedCurrencies`（`[Key(4)]`–`[Key(7)]`），由四個新 `st_company` 欄位承載；空值退框架預設。
- `Bee.UI.Avalonia`：`NumericEdit` 編輯器（`ControlType.NumericEdit`）—— focus 顯完整精度、blur 依 `NumberFormat` 格式化、右對齊、顯示捨入絕不回寫。
- `Bee.UI.Avalonia`：`GridControl` per-cell 幣別／單位感知格式化（逐列解析 `CurrencyField` / `UnitField`）與 `AmountColumnSummary` 混幣／混單位合計 helper。

## [4.12.1]

> Bee.NET 仍處 pre-stable 演進階段。本 patch 在 `Bee.Definition` 內嵌 trimmer descriptor，讓定義型別圖在 full trim / AOT 下保留，補完 4.12.0 起步的 Avalonia **iOS** / **Android** Release 打包路徑（4.12.0 讓同一批型別可於 reflection-only XmlSerializer 反序列化）。無破壞性變更。

📄 詳細變更與設計脈絡：[docs/changelogs/4.12.1.zh-TW.md](docs/changelogs/4.12.1.zh-TW.md)

### 修正

- `Bee.Definition`：隨套件內嵌 `ILLink.Descriptors.xml`，在 full trim / AOT 下保留定義型別圖（`Bee.Definition.*` + `Bee.Base.Collections.*`），使裝置端 `XmlCodec.Deserialize<FormSchema>` 路徑在 trimmed iOS / Android Release 建置下不被裁掉。自動套用至所有下游 trimmed / AOT 消費端，呼叫端無需任何改動。

## [4.12.0]

> Bee.NET 仍處 pre-stable 演進階段。本版讓 `Bee.UI.Avalonia` 控件家族在手機／窄視窗下響應式，並讓 `Bee.Definition` 型別可於 AOT reflection-only XmlSerializer 反序列化 —— 兩者合起來讓 Avalonia 的 **iOS** 與 **Android** head 得以成立。無破壞性變更。

📄 詳細變更與設計脈絡：[docs/changelogs/4.12.0.zh-TW.md](docs/changelogs/4.12.0.zh-TW.md)

### 新增

- `Bee.UI.Avalonia`：`FormView` 響應式佈局 —— 主檔欄位於 `CompactWidthThreshold`（預設 600 DIP）以下由多欄重排為單欄、明細 grid 由 `InCell` 切為 `EditForm`。
- `Bee.UI.Avalonia`：`ListView` 窄視窗卡片佈局 —— 以每筆一張卡取代寬欄 grid。
- `Bee.UI.Avalonia`：`RowEditPanel`（EditForm）依宿主寬度 1 ↔ 2 欄重排；`RowEditDialog` 桌面視窗可調整大小。

### 修正

- `Bee.Definition`：定義集合型別可於 AOT reflection-only XmlSerializer 反序列化（單一 public `Add(T)`、無參數建構子）—— 讓 iOS / Android head 得以成立。呼叫端語法與 XML 格式皆不變。[ADR-025](docs/adr/adr-025-define-types-aot-xmlserializer-compat.md)
- `Bee.UI.Avalonia`：`RowEditDialog` 在單視圖宿主（iOS / Android / 瀏覽器）改走 `OverlayLayer`，取代會崩潰的 native `Window`。
- `Bee.UI.Avalonia`：`FormView` 表單本體垂直捲動，窄單欄佈局下方控件仍可觸及。
- `Bee.UI.Avalonia`：`GridControl` lookup 可編輯 cell 顯示開窗放大鏡圖示。

## [4.11.0]

> Bee.NET 仍處 pre-stable 演進階段。本版主軸為「前端↔後端存取全面 async 化」：client 連線生命週期與型別化定義快取卸除 sync-over-async 橋接（`SyncExecutor` 移除），連帶讓單視窗的 Avalonia Browser (WASM) head 可行。本版含**破壞性變更**，範圍限於 `Bee.UI.Core`、`Bee.Api.Client` 與 Avalonia / MAUI head 的 client 建構／連線面，另含 SQLitePCLRaw 的**安全性升級**。

📄 詳細變更與設計脈絡：[docs/changelogs/4.11.0.zh-TW.md](docs/changelogs/4.11.0.zh-TW.md)

### 破壞性變更

- 移除公開的同步 client API，改用 async —— `ClientInfo.Initialize(string)` / `SetEndpoint`、`ApiConnectValidator.Validate`、`IUIViewService.ShowApiConnect`（改用對應 `...Async`）；`SyncExecutor` 移除。
- 型別更名 `RemoteDefineAccess` → `ClientDefineAccess`（移至 `Bee.Api.Client` root）、`LocalDefineAccess` → `CacheDefineAccess`。

### 安全性

- SQLitePCLRaw 升級至 3.x（GHSA-2m69-gcr7-jv3q），取代先前的 NU1903 抑制。

### 新增

- `Bee.UI.Avalonia`：單視窗 host 的對話框疊層路徑（`OverlayLayer`），讓 lookup / 列編輯對話框得以在 Avalonia Browser (WASM) head 運作。
- `Bee.UI.Avalonia`：`FormDataObject` 新增 `RowAdded` / `RowDeleted` / `IsDirtyChanged` 事件。

### 變更

- `Bee.UI.Avalonia`：欄位編輯器改為離開／Enter 才提交，非逐字提交。
- `Bee.UI.Avalonia`：欄位標題統一標示唯讀（括號、僅留底線）與必填（藍色）。
- `Bee.Definition`：`FormLayoutGenerator` 生成的主 section 不再重複表單名。

### 修正

- `Bee.UI.Avalonia`：`GridControl.Bind` 明確綁定時自我初始化編輯狀態。

### 升級指引

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

> Bee.NET 仍處 pre-stable 演進階段。本版主軸為「lookup 關連機制全面落地」：relation 欄自動成為開窗式 lookup 編輯器、複合顯示「編號 - 名稱」、主表 `ButtonEdit` 與明細 InCell 兩種選取入口，搭配後端 `GetLookup` 取數。同時把 Avalonia 單筆/清單拆為 `FormView` / `ListView` 兩個關注點（ERP 慣例的清單／單筆分離），並將 DataForm 持久化收斂到 DataTable 級 `DataAdapter` 路徑（自製 `SqliteDataAdapter` 讓 SQLite 同樣走 adapter）。本版含**數個 breaking change**，範圍限於 `Bee.UI.Avalonia` 與 `Bee.Db` 的建構面。

📄 詳細變更與設計脈絡：[docs/changelogs/4.10.0.zh-TW.md](docs/changelogs/4.10.0.zh-TW.md)

### 破壞性變更

- `Bee.UI.Avalonia`：移除 `DynamicForm` / `SingleFormBase`；清單職責拆至新 `ListView`、單筆收斂於 `FormView`，兩者移至新命名空間 `Bee.UI.Avalonia.Views`。
- `Bee.UI.Avalonia`：`GridControl`（含 `GridControlBinder` / `GridEditMode`）由 `Bee.UI.Avalonia.Controls.Editors` 移至 `Bee.UI.Avalonia.Controls`。
- `Bee.Db`：移除逐列 `InsertCommandBuilder` / `UpdateCommandBuilder`（`DeleteCommandBuilder` / `SelectCommandBuilder` 仍保留）。

### 新增

- `Bee.Definition` / `Bee.Api` / `Bee.UI.Avalonia`：定義驅動的開窗 lookup 關連機制 — `DisplayField` / `LookupFields`、自動解析的 `ButtonEdit`、後端 `FormBusinessObject.GetLookup`、前端 `LookupPanel` / `LookupDialog` 與 `GridControl` InCell 開窗（[ADR-023](docs/adr/adr-023-lookup-relation-mechanism.md)）。
- `Bee.Definition`：`FormField.ReadOnly`，由 `FormLayoutGenerator` 傳遞到 `LayoutField` / `LayoutColumn`。
- `Bee.Db`：自製 `SqliteDataAdapter`（經 `SqliteProviderFactory`），使 SQLite 走 adapter 路徑。

### 變更

- `Bee.Db`：`DataFormRepository.Save` 改用 DataTable 級 IUD（`DataAdapter.Update`）；無變更的 DataSet 為 no-op 回 0（[ADR-024](docs/adr/adr-024-dataform-save-dataadapter.md)）。
- `Bee.UI.Avalonia`：`FormView` 清單列雙擊開啟唯讀檢視。

### 修正

- `Bee.Db`：SQLite GUID 欄加 `COLLATE NOCASE`（CREATE 與 ALTER ADD）。
- `Bee.Db`：新增資料列依 `FormSchema` 經 `FormRowDefaults` 補非空預設；master 連結以原值 `sys_rowid` 寫入明細 `sys_master_rowid`。
- `GetNewData` 骨架補 `RelationField` 欄位。
- `SelectContextBuilder`：修多關連 JOIN 解析。
- `Bee.UI.Avalonia`：`ListView` 清單捲軸於列數超出可視範圍時可正常捲動。

## [4.9.0]

> Bee.NET 仍處 pre-stable 演進階段。本版主軸為「Avalonia 可編輯表單全面落地」：與 `ControlType` 一一對應的 field editor 控件組、新的 `GridControl`（含 in-cell 與彈窗兩種列編輯）、表單模式生命週期（`SingleFormBase` 向整棵控件樹廣播 `FormMode`），以及定義層 `FormEditModes` 依表單模式的可編輯設定。本版含 **一個 breaking change**，範圍僅限 Avalonia 家族：`Bee.UI.Avalonia` 移除 `DynamicGrid`（Blazor / MAUI 版不受影響）。另含 MessagePack 相依套件的**安全性升級**。

📄 詳細變更與設計脈絡：[docs/changelogs/4.9.0.zh-TW.md](docs/changelogs/4.9.0.zh-TW.md)

### 破壞性變更

- `Bee.UI.Avalonia`：移除 `DynamicGrid`，`FormView` 列表改用 `GridControl` 渲染（`ContentControl` 組合式控件，`DataGrid` 成員改經 `GridControl.InnerGrid`）。Blazor / MAUI `DynamicGrid` 不受影響。

### 安全性

- MessagePack：`3.1.4` → `3.1.7`（GHSA-hv8m-jj95-wg3x）— 修正 LZ4 解壓對惡意輸入拋 `AccessViolationException`（NU1903 高嚴重性）。

### 新增

- `Bee.UI.Avalonia`：field editor 控件組 — 七個編輯器與 `ControlType` 一一對應（`TextEdit` / `MemoEdit` / `ButtonEdit` / `DateEdit` / `YearMonthEdit` / `DropDownEdit` / `CheckEdit`），含 `FieldEditorBinder`、`FormScope` attached property、`FieldEditorFactory`；`DynamicForm` 改經此組渲染。
- `Bee.UI.Avalonia`：新增 `GridControl` — 對應 `LayoutGrid` 的組合式 grid（`InnerGrid`），兩種綁定模式 + `FormScope` ambient 綁定、依 `LayoutColumn.ControlType` in-cell 編輯、`AllowActions` 增刪列、`AllowEdit`（[ADR-021](docs/adr/adr-021-avalonia-datagrid-editing-strategy.md)）。
- `Bee.UI.Avalonia`：新增 `GridEditMode`（`InCell` / `EditForm`）+ `RowEditPanel` / `RowEditDialog`，底層為 `FormDataObject` 列編輯協定（`BeginRowEdit` / `CommitRowEdit` / `CancelRowEdit`）。
- `Bee.UI.Avalonia`：新增 `SingleFormBase`，持有並廣播 `FormMode`；`FormView` 繼承並引入 View / Edit / Add 模式生命週期。
- `Bee.Definition`：新增 `FormEditModes` `[Flags]` 列舉 + `LayoutField.AllowEditModes` / `LayoutGrid.AllowEditModes`（預設 `All`）；與 `ReadOnly` / `AllowActions` AND 合成，預設值不落 XML。
- `Bee.UI.Avalonia`：`FormDataObject` 新增 `FieldValueChanged` / `DataSetReplaced` 事件（ADO.NET 橋接），另加列多載 `GetField` / `SetField`。
- `samples/Avalonia.Editors.Gallery`：原生 vs 繼承編輯器比對、in-cell 編輯、`EditForm` 模式比對區。
- DefineEditor：Semi.Avalonia 主題、Welcome tab、tab dirty 標記 + 右鍵選單 + 全部儲存、未儲存提示、macOS 選單補齊。

### 變更

- `Bee.UI.Avalonia`：`FormView` 載入資料改進唯讀 `View` 模式，須按 Edit 鈕才進編輯。

### 修正

- `Bee.Api`：MessagePack 3.1.5+ 黑名單擋掉 `DataTable` 反序列化；`SafeMessagePackSerializerOptions` 改為框架白名單優先放行。
- `Bee.UI.Avalonia`：`FormDataObject` async CRUD continuation 改回 UI 執行緒（移除 `ConfigureAwait(false)`）。
- `Bee.UI.Avalonia`：`DynamicForm` `DateEdit` 在非 UTC 時區不再拋例外。
- `Bee.UI.Avalonia`：`ComboBox` 選取框正常顯示選值；`DropDownEdit` / in-cell `ComboBox` 改用 `DisplayMemberBinding`。
- `Bee.UI.Avalonia`：`GridControl` 在 `AddRow` / `DeleteSelectedRow` 後重新 realize 列。
- `Bee.UI.Avalonia`：`ButtonEdit` 唯讀時停用內嵌 lookup 按鈕；圖示改為 chromeless `PathIcon`。
- Demo 後端啟動時建立 `st_cache_notify`，消除 `CacheNotifyPoller` warning。

## [4.8.0]

> Bee.NET 仍處 pre-stable 演進階段。本版主軸為「框架預設定義升為一等公民」：所有 `st_*` 系統表 schema、框架預設 `Department` / `Employee` 表單、以及 bootstrap 設定 template 全部以 embedded resource 形式 ship 在 `Bee.Definition.dll` 內，透過新公開 API `Bee.Definition.Defaults` 對外存取。新增 `Bee.Cli` dotnet tool（`dotnet bee defines materialize ...`）+ DefineEditor 自動 materialize hook，把首次 setup 縮成一條指令。本版含 **一個 breaking change**：框架組織表 `ft_department` / `ft_employee` 改名為 `st_department` / `st_employee` 對齊既有 `st_*` 命名空間。

📄 詳細變更與設計脈絡：[docs/changelogs/4.8.0.zh-TW.md](docs/changelogs/4.8.0.zh-TW.md)

### 破壞性變更

- 框架組織表 `ft_department` / `ft_employee` 改名為 `st_department` / `st_employee`；已落地部署需自行 `RENAME TABLE`——範例見 [資料表結構升級指南 §框架表改名](docs/database-schema-upgrade.zh-TW.md)。FormSchema progId、C# 型別名、欄位名皆未變動。

### 新增

- `docs/framework-reserved-names.md`（雙語）：框架保留命名 registry（`st_*` 系統表、保留 `progId`）。
- `Bee.Definition`：框架預設定義檔（11 個 `st_*` `TableSchema` XML、`Department` / `Employee` 的 `FormSchema` / `FormLayout` / `Language`、精簡 `DbCategorySettings.xml`、`SystemSettings.xml` template、空殼 `DatabaseSettings.xml`）改以 embedded resource 形式 ship，naming 為 `Bee.Definition.Defaults/{相對路徑}`。
- `Bee.Definition.Defaults` API：`Defaults.MaterializeTo(path, options)`（skip-existing）、`Defaults.ListEmbedded()`、`Defaults.OpenEmbedded(relativePath)`；runtime `IDefineStorage` 不變。
- `TestProcessBootstrap.SharedDefinePath`：process-wide 合併後 define 目錄；`BeeTestFixture` 預設 `DefinePath` 改指向此處。
- `Bee.Cli` dotnet tool（`dotnet bee`）：`defines materialize --path ./Define [--overwrite] [--filter <prefix>]`、`defines list`、`--version`；版本與框架 lock-step，經 `nuget-publish.yml` 發佈。保留 subcommand group（`schema` / `tenant` / `samples`）本版尚未實作。
- DefineEditor 開啟資料夾時自動 materialize 框架預設（`Defaults.MaterializeTo`，skip-existing）；有寫入時 status bar 顯示物化檔數。

## [4.7.0]

> Bee.NET 仍處 pre-stable 演進階段。本版主軸為「ERP 權限機制、i18n 與多租戶客製化全面落地」：新增權限線 A/B/record-scope 三段式機制、多國語系基礎建設、多租戶客製化覆蓋層、跨節點 DB 快取失效機制與「定義存 DB」儲存後端，並加開第三個桌面平台支援 — 新增 `Bee.UI.Avalonia` 套件。本版無 breaking change（既有公開 API 簽章未動）；但首次啟動會自動建立多張新系統表（`st_role` / `st_role_grant` / `st_user_role` / `st_cache_notify` / `st_define` / `st_user_company` 等），如以 framework 自動 schema 升級之外另自管 DDL 的部署需手動補建。

📄 詳細變更與設計脈絡：[docs/changelogs/4.7.0.zh-TW.md](docs/changelogs/4.7.0.zh-TW.md)

### 新增

- `Bee.UI.Avalonia`：新 Avalonia 12 桌面控制項套件 — `DynamicForm` / `DynamicGrid` / `FormView`、`FormDataObject`、`FileEndpointStorage`；附範例 `samples/Avalonia.Demo`。對應 [ADR-020](docs/adr/adr-020-avalonia-datagrid-binding-strategy.md)。
- ERP 權限機制（line-A + line-B + record-scope）：`PermissionModels` registry、`FormSchema.PermissionModelId`、`FormField.ScopeRole`、`AuthorizationService.Can`、`st_role` / `st_role_grant` / `st_user_role` 資料模型、`EnterCompany` 填充 `SessionInfo.Roles`、FormBO 權限 gate，以及 `ScopeResolver` 列級過濾 + `Update` / `Delete` 對 `sys_rowid` 權威 re-query。對應 [ADR-019](docs/adr/adr-019-permission-authorization-model.md)。
- i18n：`LanguageResource`（XML / JSON / MessagePack）、`ILanguageService` + `GetLangText`、`FormSchema` 自動本地化、`LangEnumName` 列舉下拉本地化、`SystemBO.GetLanguage` JSON-RPC 入口。
- 多租戶客製化覆蓋層：`CustomizeId` 全程隨身傳遞，定義讀取端疊加覆蓋（base define + customize override），整合至 `IDefineAccess`，`RemoteDefineAccess` 切換租戶時清快取。對應 [ADR-016](docs/adr/adr-016-multitenant-customization-overlay.md)。
- DB 快取失效（跨節點）：`st_cache_notify` 表 + `ICacheNotifyService.Touch`、`CacheNotifyPoller` 背景輪詢 + 靜態路由 registry、以 `sys_update_time` 增量抓取。對應 [ADR-017](docs/adr/adr-017-db-cache-invalidation.md)。
- `DbDefineStorage`：`st_define` 表 + `DbDefineStorage` + `ICustomizeDefineReader`；定義可改存 DB（原 XML 路徑仍可用），DI 延遲解析打破與 `IDbAccessFactory` 的建構循環。對應 [ADR-018](docs/adr/adr-018-db-define-storage.md)。
- 組織部門樹：三棲 `DepartmentTree`（以 `DepartmentNode.Children` 巢狀）+ per-company 快取 + `GetDepartmentTree` JSON-RPC API。
- `ProgramItem.BusinessObject`：progId 可綁定 BO 型別，取代慣例命名解析。
- `tools/define-editor`：Avalonia 桌面工具，9 種定義型別視覺化編輯，支援 i18n live switch、自動驗證、單檔發佈、macOS `.app` bundle。non-shipping tool。

### 變更

- `DepartmentTree`：序列化由扁平 list 改為以 `DepartmentNode.Children` 巢狀。
- `st_cache_notify`：去除非系統欄的 `sys_` 前綴，系統欄保留。
- `CacheNotifyPoller`：改回以 `sys_update_time` 的 `O(1)` 增量抓取。

### 修正

- MySQL：statement-binlog 下 `ALTER ADD Guid NOT NULL DEFAULT (UUID())` replication-unsafe；dialect 拆為 `ADD COLUMN`（常數預設）+ `ALTER COLUMN SET DEFAULT (UUID())`。
- Oracle：`ALTER MODIFY ... NOT NULL` 對既已 NOT NULL 欄重發拋 ORA-01442；僅在 nullability 改變時才下 hint。
- Oracle：String / Text 欄一律建 nullable（`''` 視為 `NULL`，fresh `CREATE TABLE` 掛 ORA-01400）。
- MAUI `DynamicForm`：`SetField` 改為 idempotent、`ConvertToColumnValue` 補非 null fallback、`ReloadList` 保留 `sys_rowid`。
- `ObjectCaching`：以 lazy `FileModificationToken` 取代 `PhysicalFileProvider` 修正 CI 競爭（移除 `Microsoft.Extensions.FileProviders.Physical` 參考）。
- `DemoBusinessObjectFactory`：補上漏注入的 `ILanguageService`。
- `RolePermissionRepository`：SQL 串接補空格（SonarCloud S2857）。

## [4.6.0]

> Bee.NET 仍處 pre-stable 演進階段。本版主軸為「開放 JSON-RPC 給 JS 前端」：FormBO / SystemBO 共 7 個 CRUD / Session 方法 `ProtectionLevel` 降為 `Public`、新增兩個 JSON-native 取得方法（`GetFormSchema` / `GetFormLayout`），並修正 Plain 路徑 DataSet 反序列化與 Blazor WebAssembly RSA 相關阻塞問題。`MasterKeySource` 預設值改為 `Environment`，依嚴格 SemVer 觀點屬 major，pre-stable 政策下以 minor 發佈。

📄 詳細變更與設計脈絡：[docs/changelogs/4.6.0.zh-TW.md](docs/changelogs/4.6.0.zh-TW.md)

### 新增

- `Bee.Business`：`SystemBO.GetFormSchema` / `GetFormLayout` — JSON-native 取得方法，回傳 `FormSchema` / `FormLayout`；`.NET` 對應 `SystemApiConnector.GetFormSchemaAsync` / `GetFormLayoutAsync`；皆為 `Public + Authenticated`。對應決策：[ADR-014](docs/adr/adr-014-jsonrpc-plain-public-default.md)。
- `docs`：新增中英雙語 [`docs/jsonrpc-frontend-integration.md`](docs/jsonrpc-frontend-integration.md) — wire format、headers、認證流程、可呼叫方法清單、`JsonRpcErrorCode` 對應表、TypeScript wrapper。

### 變更

- `Bee.Definition`：`MasterKeySource` 預設改為 `Environment`（從 `$BEE_MASTER_KEY` 讀取，不再產生 `Master.key`）（**breaking**）；已明確設定 `<Type>File</Type>` 的 host 不受影響。對應決策：[ADR-015](docs/adr/adr-015-master-key-environment-default.md)。
- `Bee.Business`：7 個 BO 方法 `ProtectionLevel` 降為 `Public` — `FormBO.GetNewData` / `GetData` / `Save` / `Delete` 與 `SystemBO.EnterCompany` / `LeaveCompany` / `Logout`（`Encrypted` → `Public`，仍 `Authenticated`）；向下相容。對應決策：[ADR-014](docs/adr/adr-014-jsonrpc-plain-public-default.md)。
- `Bee.Definition`：`FormSchema.MasterTable` 加 `[JsonIgnore]`（XML / MessagePack 不受影響）；JS / TS 客戶端改從 `tables[0]` 讀取，取代 `masterTable`。

### 修正

- `Bee.Base`：`RsaCryptor` 改用 PEM（SPKI / PKCS#1）取代 XML key 格式，並加 `OperatingSystem.IsBrowser()` fallback — 解封 Blazor WebAssembly 登入。
- `Bee.Api.Core`：`ApiInputConverter` 補齊 Plain 路徑的 `DataTableJsonConverter` / `DataSetJsonConverter` / `JsonStringEnumConverter`，修正 `DataSet` rows 為空與 `Save` 一律回 "DataSet has no pending changes"。
- `Bee.UI.Maui`：`DynamicForm` 新增 public `Refresh()` 驅動 `Rebuild()`，使 in-place `DataSet` mutation 後（New / Save / Delete）正確重繪。

### 升級指引

```diff
- const masterTable = formSchema.masterTable;
+ const masterTable = formSchema.tables[0];
```

## [4.5.0]

> Bee.NET 仍處於 pre-stable 演進階段。本次新增三層前端套件（`Bee.UI.Core` 跨平台共通層、`Bee.UI.Maui` MAUI 行動／桌面控制項、`Bee.Web.Blazor.Server` / `Bee.Web.Blazor.Wasm` 兩個 Blazor RCL），並把 API connector 介面整批轉為 async-only。介面簽名變動由嚴格 SemVer 觀點屬 major，pre-stable 政策下以 minor 發佈。

📄 詳細變更與設計脈絡：[docs/changelogs/4.5.0.zh-TW.md](docs/changelogs/4.5.0.zh-TW.md)

### 新增

- `Bee.UI.Core`：新增跨平台 UI 共通層（共用 ViewModel、`FormDataObject`、`SystemApiConnector`、`ClientInfo`），由 `bee-ui-core` 併入。對應 [ADR-013](docs/adr/adr-013-frontend-api-connection-strategy.md)。
- `Bee.UI.Maui`：新增 MAUI 控制項層，提供 `DynamicForm` / `DynamicGrid` / `FormPage` 與 `MauiPreferenceEndpointStorage`；預設 `net10.0`，平台 TFM 透過 `-p:BeeUiMauiFullPlatforms=true` 開啟。
- `Bee.Web.Blazor.Server` / `Bee.Web.Blazor.Wasm`：新增 Blazor RCL，提供 `DynamicForm` / `DynamicGrid` / `FormPage`、`BeeAccessTokenProvider`、`BeeLoginPanel`、`AddBeeBlazor`。
- `UserMessageException` + `JsonRpcErrorCode.UserMessage`：後端 throw 由 `ApiConnector` 重建為 client 端 `UserMessageException`，可直接以 `.Message` 呈現。
- `FormBusinessObject`：新增 `GetNewData` / `GetData` / `Save` / `Delete`，使 `IFormBusinessObject` 涵蓋完整單筆 CRUD。
- `samples/`：新增 demo 家族 — `QuickStart.Server` + `QuickStart.Console`、`Blazor.Server.Demo` + `Blazor.Wasm.Demo`、`Maui.Demo`；共用 `Bee.Samples.Shared` 並備有 `.smoke.yaml`。

### 變更

- `IApiConnector` / `IFormApiConnector` / `ISystemApiConnector`：轉為 async-only，移除同步方法，改用 `*Async` 版本。
- `ExceptionExtensions`：由 `Bee.Base` 搬至 `Bee.Base.Exceptions`。
- `ClientInfo`：改為 static class，`ClientInfo.SystemApiConnector.Initialize()` 改為 async。見 [ADR-013](docs/adr/adr-013-frontend-api-connection-strategy.md)。

### 升級指引

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

> Bee.NET 仍處於 pre-stable 演進階段；對外公開 API 表面尚無外部消費者，minor 版本允許包含 API 搬遷與少量 breaking change。本次包含介面簽名變動（`IFormRepositoryFactory.CreateDataFormRepository`、`IDataFormRepository.GetList`）與屬性移除（`CompanyInfo.LogDatabaseId`），嚴格 SemVer 觀點屬 major，pre-stable 政策下以 minor 發佈。

📄 詳細變更與設計脈絡：[docs/changelogs/4.4.0.zh-TW.md](docs/changelogs/4.4.0.zh-TW.md)

### 新增

- `Bee.Business`：`FormBO.GetList` 統一查詢入口，透過 `IDataFormRepository` 並支援 `PagingOptions`/`PagingInfo`；`FormApiConnector.GetList`/`GetListAsync` 用戶端入口。
- `Bee.Business`：`SystemBO` 新增 `EnterCompany`/`LeaveCompany`/`Logout`；`SessionInfo` 加 nullable `CompanyId`；`Login` 補入 `ISystemBusinessObject`；新增 `CompanyInfo` 與 `ICompanyInfoService`。對應 [ADR-012](docs/adr/adr-012-session-company-context.md)。
- `Bee.Business`：新增 `DbScope` enum（`Common`/`Company`/`Log`）與 `IRepositoryDatabaseRouter`；`BusinessObject` 新增 `ResolveDatabaseId(DbScope)`、`CreateDataFormRepository(progId)` protected helper。對應 [ADR-010](docs/adr/adr-010-logical-database-category.md)。
- `Bee.Db`：`SelectCommandBuilder` 跨 5 dialect 分頁（`OFFSET/FETCH` 或 `LIMIT/OFFSET`）+ 新增 `BuildCount`。
- `Bee.ObjectCaching`：`KeyObjectCache<T>` 負向快取（預設 5 分鐘絕對過期，virtual `GetNegativePolicy` 可覆寫/停用）。對應 [ADR-009](docs/adr/adr-009-cache-implementation.md)。
- `Bee.Business`：`IBusinessObjectFactory` typed wrapper `CreateFormBO(token, progId)`／`CreateSystemBO(token)`。
- `Bee.Repository`：新增 `st_company`/`st_user_company` 系統表 + `ICompanyRepository`/`IUserCompanyRepository`；預設 common `DbCategorySettings` 已含此兩表。
- `JsonRpcErrorCode`：新增 `CompanyNotEntered` (-32002, HTTP 409)、`CompanyAccessDenied` (-32003, HTTP 403)。

### 變更

- `IFormRepositoryFactory.CreateDataFormRepository`：加入 `Guid accessToken` 參數，配合 `IRepositoryDatabaseRouter` 路由。
- `IDataFormRepository.GetList`：回傳 `DataFormListResult`（`Table` + `Paging`），加入 `PagingOptions? paging` default 參數。
- `CompanyInfo.LogDatabaseId` 移除：`DbScope.Log` 改為固定 `databaseId = "log"`。
- `SelectCommandBuilder`：未知表名改拋 `InvalidOperationException`（原 `KeyNotFoundException`）。

### 升級指引

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
+ var logDbId = "log";  // 框架固定路由；跨公司隔離改用 sys_company_rowid 列級分區
```

## [4.3.0]

> Bee.NET 仍處於 pre-stable 演進階段；對外公開 API 表面尚無外部消費者，minor 版本允許包含命名空間搬遷。本次調整以嚴格 SemVer 觀點屬 major，pre-stable 政策下以 minor 發佈。

📄 詳細變更與設計脈絡：[docs/changelogs/4.3.0.zh-TW.md](docs/changelogs/4.3.0.zh-TW.md)

### 新增

- `Bee.Hosting`：新套件 — 框架 composition root，將所有後端服務（`IDefineAccess`、`IDbAccessFactory`、`IBusinessObjectFactory`、`JsonRpcExecutor` 等）註冊到任意 `IServiceCollection`，不依賴 ASP.NET Core。

### 變更

- `Bee.Hosting`：`BeeFrameworkServiceCollectionExtensions.AddBeeFramework` 由 `Bee.Api.AspNetCore` 搬入（命名空間 `Bee.Api.AspNetCore` → `Bee.Hosting`）。
- `Bee.Api.AspNetCore`：現在僅包含 ASP.NET Core 整合（`UseBeeFramework` + `ApiServiceController`）；原有 4 個 ProjectReference 全部合併至 `Bee.Hosting`。

### 升級指引

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

## [4.2.0] 與更早版本

見 git 歷史（`git log --oneline`）。
