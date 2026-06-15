# 版本變更記錄

[English](CHANGELOG.md)

本檔記錄專案的所有重要變更。

## [4.10.0]

> Bee.NET 仍處 pre-stable 演進階段。本版主軸為「lookup 關連機制全面落地」：relation 欄自動成為開窗式 lookup 編輯器、複合顯示「編號 - 名稱」、主表 `ButtonEdit` 與明細 InCell 兩種選取入口，搭配後端 `GetLookup` 取數。同時把 Avalonia 單筆/清單拆為 `FormView` / `ListView` 兩個關注點（ERP 慣例的清單／單筆分離），並將 DataForm 持久化收斂到 DataTable 級 `DataAdapter` 路徑（自製 `SqliteDataAdapter` 讓 SQLite 同樣走 adapter）。本版含**數個 breaking change**，範圍限於 `Bee.UI.Avalonia` 與 `Bee.Db` 的建構面。

### 破壞性變更

- **`Bee.UI.Avalonia` 移除 `DynamicForm` 與 `SingleFormBase`，清單職責拆至新 `ListView`、單筆收斂於 `FormView`** — 對齊 ERP 慣例的「清單／單筆」分離：`ListView` 負責列表（載入、選取、捲動），`FormView` 專注單筆檢視／編輯（含明細 grid）。原本經 `DynamicForm` / `SingleFormBase` 組合的用法改用 `FormView`（單筆）+ `ListView`（清單）。
- **`GridControl` 命名空間由 `Bee.UI.Avalonia.Controls.Editors` 移至 `Bee.UI.Avalonia.Controls`** — 與同級高階控件 `FormView` / `ListView` 並列（`GridControlBinder` / `GridEditMode` 一併移動）；`Editors/` 留給 field editor 與 lookup／列編輯支援 UI。以 `Bee.UI.Avalonia.Controls.Editors.GridControl` 完整限定、或僅為它 import 該命名空間的呼叫端需調整 using。
- **`Bee.Db` 移除逐列 `InsertCommandBuilder` / `UpdateCommandBuilder`** — `DataFormRepository.Save` 改走 DataTable 級 `DataAdapter.Update`（見「變更」），這兩個「只組含變更欄位的單列 SQL」builder 在 production 已無使用者而移除。`DeleteCommandBuilder` / `SelectCommandBuilder` 仍保留（`Delete()` / `GetData()` 使用）。

### 新增

- **Lookup 關連機制（`Bee.Definition` / `Bee.Api` / `Bee.UI.Avalonia`）** — 一套定義驅動的開窗 lookup（設計見 [ADR-023](docs/adr/adr-023-lookup-relation-mechanism.md)）：
  - 定義層 `DisplayField` / `LookupFields`；relation 欄（`RelationProgId` + `RelationFieldMappings`）由 `FormLayoutGenerator` 自動解析為 `ButtonEdit` 並套涵蓋規則（relation 欄承載顯示、對應 `ref_*` 欄不重複產生）；`DisplayFields` 複合顯示「編號 - 名稱」。
  - 後端 `FormBusinessObject.GetLookup`（含 `GetLookupFilter()`）為開窗清單專用取數。
  - 前端 `LookupPanel` / `LookupDialog` 選取元件、`ButtonEdit` 內建 lookup 開窗流程（顯示綁定、開窗寫回 mapped 欄、清空）、`GridControl` 明細 InCell 點擊開窗。
- **`FormField.ReadOnly`（`Bee.Definition`）** — 新增唯讀屬性，`FormLayoutGenerator` 傳遞到 `LayoutField` / `LayoutColumn`（runtime 早已尊重 layout 層 ReadOnly）；計算欄（如金額、總額）可標唯讀。
- **自製 `SqliteDataAdapter`（`Bee.Db`）** — Microsoft.Data.Sqlite 未提供 `DbDataAdapter`；框架以 `SqliteProviderFactory` 包裝補上，使 SQLite 的查詢與異動與其餘 4 個 provider 一致走 adapter 路徑，無須 provider 專屬 fallback。

### 變更

- **`DataFormRepository.Save` 改用 DataTable 級 IUD（`DataAdapter.Update`）** — 每張表以「全欄位參數化」Insert/Update/Delete 一次套用；Modified 列即使值未變也只是無害同值更新，徹底消除「Modified 但無欄位變更 → 空 SET → `UPDATE would be empty`」整類錯誤（master-detail 重存既有單據的根因）。整個 DataSet 無變更時為 no-op 回 0（原為丟例外）。設計與 SQLite adapter 補位見 [ADR-024](docs/adr/adr-024-dataform-save-dataadapter.md)。
- **`FormView` 清單列雙擊開啟唯讀 `View`** — 雙擊清單列進入唯讀檢視，不再直接進編輯；工具列啟用狀態隨當前模式。

### 修正

- **SQLite GUID 欄加 `COLLATE NOCASE`** — SQLite 以區分大小寫 TEXT 儲存 GUID；client 端小寫 key 與 seed／provider 大寫儲存值跨大小寫脫鉤，會讓 master-detail 明細因 `sys_master_rowid` 比對不中而成孤兒。GUID 欄改帶 `COLLATE NOCASE`（CREATE 與 ALTER ADD 一致），讓比對大小寫無關，對齊其餘 4 個 DB 的天生行為。
- **新增資料列依 `FormSchema` 補非空預設值，並保留 master 連結原值** — `FormRowDefaults` 對新列依欄型別補非空預設（text→空字串、數值→0…）避免 NOT NULL 違規；master-detail 連結以 master 的 `sys_rowid` 原值（保留 provider 儲存的大小寫）寫入明細 `sys_master_rowid`，不經 `Guid.Parse/ToString` round-trip。
- **`GetNewData` 骨架補 `RelationField` 欄位** — 修 lookup 新增流程顯示值帶不回。
- **多關連 JOIN 解析（`SelectContextBuilder`）** — 修同一張表多個 relation 欄時的 JOIN 產生。
- **`ListView` 清單捲軸** — 列數超出可視範圍時可正常捲動。

## [4.9.0]

> Bee.NET 仍處 pre-stable 演進階段。本版主軸為「Avalonia 可編輯表單全面落地」：與 `ControlType` 一一對應的 field editor 控件組、新的 `GridControl`（含 in-cell 與彈窗兩種列編輯）、表單模式生命週期（`SingleFormBase` 向整棵控件樹廣播 `FormMode`），以及定義層 `FormEditModes` 依表單模式的可編輯設定。本版含 **一個 breaking change**，範圍僅限 Avalonia 家族：`Bee.UI.Avalonia` 移除 `DynamicGrid`（Blazor / MAUI 版不受影響）。另含 MessagePack 相依套件的**安全性升級**。

### 破壞性變更

- **`Bee.UI.Avalonia` 移除 `DynamicGrid` — `FormView` 列表改用 `GridControl` 渲染** — 列呈現邏輯收斂至 `GridControl` 單一來源。Blazor 與 MAUI 的 `DynamicGrid` 控件不受影響。注意 `GridControl` 為 `ContentControl` 組合式控件（內建工具列 + 內部 grid），不是 `DataGrid` 子類：原本直接取用 `DataGrid` 成員（`Columns` / `ItemsSource` / `IsReadOnly` 等）的程式碼改經 `GridControl.InnerGrid`。

### 安全性

- **MessagePack 3.1.4 → 3.1.7（GHSA-hv8m-jj95-wg3x）** — LZ4 解壓對惡意輸入可能拋 `AccessViolationException`（NU1903 高嚴重性）。3.1.7 為官方修補版；所有間接引用 MessagePack 的套件一併升級。

### 新增

- **Field editor 控件組（`Bee.UI.Avalonia`）** — 七個編輯器與 `ControlType` 一一對應（`TextEdit` / `MemoEdit` / `ButtonEdit` / `DateEdit` / `YearMonthEdit` / `DropDownEdit` / `CheckEdit`），各繼承原生 Avalonia 控件並以 `StyleKeyOverride` 沿用現行主題樣式。共用綁定狀態機集中於 `FieldEditorBinder`（顯式 `Bind`、ambient 自動接線、事件驅動刷新、echo 防護）；`FormScope` 提供可繼承的 `DataObject` / `FormMode` attached property；`FieldEditorFactory` 依 `ControlType` 建立編輯器。`DynamicForm` 改經此控件組渲染，不再走內部 switch-case。
- **`GridControl` — 對應 `LayoutGrid` 的 grid 控件** — 組合式控件（內建圖示工具列 + 公開為 `InnerGrid` 的內部 `DataGrid`），支援兩種顯式綁定（`FormDataObject` 明細表依 `TableName` 解析、或列表模式直接給 `DataTable`）與 `FormScope` ambient 綁定。依 `LayoutColumn.ControlType` 提供 in-cell 編輯（popup 型欄位採點擊置換呈現——見 [ADR-021](docs/adr/adr-021-avalonia-datagrid-editing-strategy.md)）、`AllowActions` 增刪列、host 端以單一 `AllowEdit` 屬性控制。`DynamicForm` 補上 `FormLayout.Details` 渲染（每個 `LayoutGrid` 一組 caption + 綁定 `GridControl`）。
- **列編輯：`GridEditMode`（`InCell` / `EditForm`）+ `RowEditPanel` / `RowEditDialog`** — UI 層編輯模式屬性（定義層零變更）：`EditForm` 模式 grid 恆唯讀，雙擊列或工具列 Edit 鈕開 modal 彈窗編輯。底層為 `FormDataObject` 的列編輯協定（`BeginRowEdit` / `CommitRowEdit` / `CancelRowEdit`），ADO.NET `BeginEdit` 行為以測試釘住。
- **`SingleFormBase` — 表單模式擁有者與廣播者** — 單筆資料表單基底類別，持有 `FormMode`（預設 `View`）並向子樹所有編輯器與 grid 廣播變更。`FormView` 改繼承此類別並引入模式生命週期：選列載入進唯讀 `View`、Edit 鈕進 `Edit`、New 進 `Add`、Save / Delete 成功回 `View`。
- **`FormEditModes`（`Bee.Definition`）** — `[Flags]` 列舉（`None` / `Add` / `Edit` / `All`；`View` 永不可編輯故不在旗標內）+ `LayoutField.AllowEditModes` / `LayoutGrid.AllowEditModes`（預設 `All`）。欄位可表達「新增可編輯、修改鎖定」（如單號欄），grid 可依表單模式限縮編輯動作；與既有 `ReadOnly` / `AllowActions` 為 AND 合成。預設值不落 XML，既有 layout 定義檔不受影響。
- **`FormDataObject` 變更通知事件** — 新增 `FieldValueChanged` / `DataSetReplaced` 事件，並以 ADO.NET 事件橋接讓任何寫入路徑（`SetField`、grid cell 編輯、直接寫 `DataRow`）一律統一發布；另加列多載的 `GetField` / `SetField` 存取。
- **`samples/Avalonia.Editors.Gallery`** — 每個 `ControlType` 左右並排原生控件 vs 繼承編輯器的樣式比對、全部 column `ControlType` 的 in-cell 編輯、以及 `EditForm` 模式比對區；兼作 `FormScope` ambient 綁定的用法範例。
- **DefineEditor 工具改進** — 改用 Semi.Avalonia 主題、新增仿 VS Code 的 Welcome tab、tab dirty 標記 + tab 右鍵選單（關閉 / 關閉其他 / 關閉右側 / 關閉已儲存 / 全部關閉）+ 全部儲存、關閉 dirty tab 時的未儲存提示，以及 macOS 選單補齊（Open Recent、Close Tab、Hide / Hide Others / Show All）。

### 變更

- **`FormView` 載入資料改進唯讀 `View` 模式** — 過去載入即可編輯；現在須按 Edit 鈕才進編輯。工具列啟用狀態隨當前模式。

### 修正

- **MessagePack 3.1.5+ 內建黑名單擋掉 `DataTable` 反序列化** — MessagePack 新版 blocklist 將 `System.Data.DataTable` 視為 BinaryFormatter gadget 而在 typeless 反序列化時擋下，導致 `ParameterCollection` 帶 `DataTable` 的 wire 傳遞失敗。本框架的 `DataTable` formatter 逐欄重建、完全不經 BinaryFormatter，該攻擊面不存在。`SafeMessagePackSerializerOptions` 改為固定框架白名單優先放行；其餘型別維持內建黑名單 + 應用層命名空間白名單雙重把關。
- **`FormDataObject` async CRUD continuation 改回 UI 執行緒** — `ConfigureAwait(false)` 讓 `LoadAsync` / `SaveAsync` / `DeleteAsync` / `NewAsync` 的 continuation 落在 thread-pool 執行緒，變更事件在非 UI 執行緒驅動 thread-affine 的 Avalonia 控件（"The calling thread cannot access this object"），中斷 Save 後的列表刷新。
- **`DynamicForm` `DateEdit` 在非 UTC 時區機器上拋例外** — 渲染初始值時以 `Kind=Local` 的值建構 zero-offset `DateTimeOffset`，導致整個表單渲染失敗。由新增的 `Bee.UI.Avalonia.UnitTests` 測試專案抓出。
- **`ComboBox` 選取框不顯示選值** — 回收式 `FuncDataTemplate` 把同一個 `TextBlock` 實例同時發給下拉清單項與選取框；`DropDownEdit` 與 in-cell `ComboBox` 一律改用 `DisplayMemberBinding`。
- **增刪列後 grid 重新 realize** — Avalonia `DataGrid` 不觀察 `DataView` 變化，刪除的列仍停留在畫面上；`GridControl` 在 `AddRow` / `DeleteSelectedRow` 後自行重 realize。
- **`ButtonEdit` 唯讀時同步停用內嵌 lookup 按鈕** — 原本只設 `IsReadOnly`，內嵌按鈕仍可點擊（lookup 流程會寫回 mapped fields）。內嵌圖示同時改為 DatePicker 風格的 chromeless `PathIcon`，適配 light / dark 主題。
- **Demo 後端啟動時建立 `st_cache_notify`** — samples 後端啟動時自動建表，`CacheNotifyPoller` 不再每輪輪詢都記一筆 warning。

## [4.8.0]

> Bee.NET 仍處 pre-stable 演進階段。本版主軸為「框架預設定義升為一等公民」：所有 `st_*` 系統表 schema、框架預設 `Department` / `Employee` 表單、以及 bootstrap 設定 template 全部以 embedded resource 形式 ship 在 `Bee.Definition.dll` 內，透過新公開 API `Bee.Definition.Defaults` 對外存取。新增 `Bee.Cli` dotnet tool（`dotnet bee defines materialize ...`）+ DefineEditor 自動 materialize hook，把首次 setup 縮成一條指令。本版含 **一個 breaking change**：框架組織表 `ft_department` / `ft_employee` 改名為 `st_department` / `st_employee` 對齊既有 `st_*` 命名空間。

### 破壞性變更

- **框架組織表 `ft_department` / `ft_employee` 改名為 `st_department` / `st_employee`** — 與其他框架自有系統表（`st_role` / `st_role_grant` / `st_user_role`）前綴對齊：這兩張表為框架組織／record-scope 機制所需，非業務資料。`st_` 前綴語意為「框架所有」，與資料表所在資料庫位置正交（這兩張表仍住在 company database）。已落地的部署需自行 `RENAME TABLE` 至新名稱——4 種 dialect 範例見 [資料表結構升級指南 §框架表改名](docs/database-schema-upgrade.zh-TW.md)。FormSchema progId（`Department` / `Employee`）、C# 型別名、欄位名皆未變動。

### 新增

- **`docs/framework-reserved-names.md`**（雙語）— 框架保留命名 registry：列出 `st_*` 系統表與保留 `progId`。命名**規則**仍位於 [database-naming-conventions.md](docs/database-naming-conventions.zh-TW.md)；API 方法參考仍位於 [api-method-reference.md](docs/api-method-reference.zh-TW.md)。新檔為「哪些具體名稱被保留」的唯一來源。
- **框架預設定義檔已以 embedded resource 形式 ship 在 `Bee.Definition.dll` 內**——所有 `st_*` `TableSchema` XML（11 檔）、框架預設 `Department` / `Employee` 的 `FormSchema` / `FormLayout` / `Language` 資源、精簡版 `DbCategorySettings.xml`（只宣告 11 張系統表）、保守 production 預設的 `SystemSettings.xml` template、空殼 `DatabaseSettings.xml` 一律住在 `src/Bee.Definition/Defaults/` 並以 `Bee.Definition.Defaults/{相對路徑}` manifest naming 嵌入 assembly。Master 副本從 `tests/Define/` 搬出——後者只保留測試專屬 fixture（`ft_project`、`PermGateForm`、`Project`、測試專用的 `SystemSettings` / `DatabaseSettings` / 擴展版 `DbCategorySettings`）。
- **`Bee.Definition.Defaults` API**——存取 embedded 框架預設的公開方法：`Defaults.MaterializeTo(path, options)` 把所有 embedded 檔寫入指定目錄（預設 skip-existing，消費者客製不會被覆蓋）、`Defaults.ListEmbedded()` 列出相對路徑、`Defaults.OpenEmbedded(relativePath)` 取單一檔的 stream。Runtime `IDefineStorage` 不變——仍只讀 `DefinePath` 內的檔；embedded 預設只透過此 API 一次性匯出使用（通常由 CLI / 開發工具在 setup 階段呼叫）。
- **`TestProcessBootstrap.SharedDefinePath`**——process-wide 合併後的 define 目錄（test-specific fixture + 首次呼叫時物化的框架預設）。`BeeTestFixture` 預設的 `DefinePath` 改指向這個目錄而非 `tests/Define/`，讓測試能透明地解析兩層內容。
- **`Bee.Cli` dotnet tool（`dotnet bee`）**——框架級 CLI；本版 ship 出 `defines` subcommand group，用於 materialize / list embedded 框架預設。安裝：`dotnet tool install -g Bee.Cli`；用法：`dotnet bee defines materialize --path ./Define [--overwrite] [--filter <prefix>]`、`dotnet bee defines list`、`dotnet bee --version`。版本與框架 lock-step，`nuget-publish.yml` workflow 在 tag push 時連同其他套件一起 pack + push。保留 subcommand group（`schema` / `tenant` / `samples`）作為未來框架操作的命名 convention，本版尚未實作。
- **DefineEditor 開啟資料夾時自動 materialize 框架預設**——使用者開啟 `DefinePath` 資料夾時，DefineEditor 在掃描 tree 之前呼叫 `Defaults.MaterializeTo(folder)`（skip-existing、in-process——與 CLI 走同一份程式碼）。有寫入時 status bar 顯示物化檔數。新消費者可直接開啟空資料夾即看到框架預設樹自動出現。

## [4.7.0]

> Bee.NET 仍處 pre-stable 演進階段。本版主軸為「ERP 權限機制、i18n 與多租戶客製化全面落地」：新增權限線 A/B/record-scope 三段式機制、多國語系基礎建設、多租戶客製化覆蓋層、跨節點 DB 快取失效機制與「定義存 DB」儲存後端，並加開第三個桌面平台支援 — 新增 `Bee.UI.Avalonia` 套件。本版無 breaking change(既有公開 API 簽章未動);但首次啟動會自動建立多張新系統表(`st_role` / `st_role_grant` / `st_user_role` / `st_cache_notify` / `st_define` / `st_user_company` 等),如以 framework 自動 schema 升級之外另自管 DDL 的部署需手動補建。

### 新增

- **新套件 `Bee.UI.Avalonia`** — Avalonia 12 桌面控制項層,提供 `DynamicForm` / `DynamicGrid` / `FormView` 控制項、`FormDataObject` 資料物件,以及 `FileEndpointStorage`(寫入使用者 home 的 sandbox-friendly endpoint 儲存)。同步附範例 `samples/Avalonia.Demo` 接 `QuickStart.Server`。對應決策:[ADR-020](docs/adr/adr-020-avalonia-datagrid-binding-strategy.md)。
- **ERP 權限機制(line-A + line-B + record-scope)** — 三段式落地完整可用的角色/授權/列級權限體系:
    - **line-A 定義/設定層**:`PermissionModels` registry(以程式碼註冊權限模型)、`FormSchema.PermissionModelId` 綁定權限模型、`FormField.ScopeRole` 宣告欄位 scope。
    - **line-B 執行層**:`AuthorizationService.Can` 統一授權判斷入口、`st_role` / `st_role_grant` / `st_user_role` 角色資料模型 + per-company Repository、`EnterCompany` 填充 `SessionInfo.Roles`,並在 `FormBusinessObject` 層一加上權限 gate。
    - **record-scope 列級權限**:user↔employee 連結 + 部門快照、grant per-action scope + `ScopeResolver`,FormBO 讀取端依 scope 過濾、寫入端在 `Update` / `Delete` 對 sys_rowid 做權威 re-query 把關(防止透過任意 sys_rowid 繞過 scope 範圍)。
    - 對應決策:[ADR-019](docs/adr/adr-019-permission-authorization-model.md)。
- **多國語系基礎建設(i18n)** — `LanguageResource` 三棲多國語系資源(XML / JSON / MessagePack)、`ILanguageService` + `GetLangText` 取詞 API、`FormSchema` 端到端自動本地化(以 `Clone()` 避免動到快取共享物件)、`LangEnumName` 列舉下拉清單本地化、`SystemBO.GetLanguage` 給 JS 前端的多語系資源 JSON-RPC 入口。
- **多租戶客製化覆蓋層** — 透過 `CustomizeId` 在 request 全程隨身傳遞客製身分,定義讀取端疊加覆蓋(base define + customize override),DI 接線將覆蓋層整合至 `IDefineAccess` 解析管線,並讓 `RemoteDefineAccess` 在切換租戶時清快取。對應決策:[ADR-016](docs/adr/adr-016-multitenant-customization-overlay.md)。
- **DB 快取相依機制(跨節點失效)** — 新增 `st_cache_notify` 通知表 + `ICacheNotifyService.Touch` 寫入失效訊號、`CacheNotifyPoller` 背景輪詢 + 靜態路由 registry 慣例式失效分派、以 `sys_update_time` 增量抓取避免重複處理;多 web node 部署下任一節點對定義 / `CompanyInfo` 等 DB-backed 物件的變更,可在數秒內讓其他節點的快取失效並重新讀取。對應決策:[ADR-017](docs/adr/adr-017-db-cache-invalidation.md)。
- **定義存 DB(`DbDefineStorage`)** — 新增 `st_define` 通用定義儲存表 + `DbDefineStorage` 實作 + `ICustomizeDefineReader` 串接客製化覆蓋層,將 `ProgramSettings` 等定義改由 DB 儲存(原 XML 檔案路徑仍可繼續使用,二者並存);DI 採延遲解析打破與 `IDbAccessFactory` 的建構循環。對應決策:[ADR-018](docs/adr/adr-018-db-define-storage.md)。
- **組織部門樹** — 三棲 `DepartmentTree`(以 `DepartmentNode.Children` 巢狀序列化呈現組織階層) + per-company 快取 + `GetDepartmentTree` JSON-RPC API,供前端組織選擇器一次取得整棵樹。
- **`ProgramItem.BusinessObject`** — `ProgramSettings` 內 progId 可綁定 BO 型別,取代既有以慣例命名解析 BO 類別的方式,讓單一 progId 對應的 BO 更明確。
- **定義檔編輯器工具(`tools/define-editor`)** — Avalonia 桌面工具,涵蓋 9 種定義型別(FormSchema / TableSchema / FormLayout / LanguageResource / ProgramSettings / DbCategorySettings / SystemSettings / UserSettings / DefineRegistry)的視覺化編輯,以 VS Code 風 UI + macOS 原生 menu 呈現,支援 i18n(英 / 繁中)live switch、自動驗證、單檔發佈(含 IL3002 修正)、雙擊執行的 macOS `.app` bundle(含蜂巢圖示)。non-shipping tool,獨立於 NuGet 套件之外。

### 變更

- **`DepartmentTree` 序列化結構** — 由原本扁平 list 改為以 `DepartmentNode.Children` 巢狀呈現,直接反映階層而不再仰賴 parent_id 重組。
- **`st_cache_notify` 表欄名** — 去除非系統欄的 `sys_` 前綴(原本誤用),系統欄(`sys_rowid` / `sys_update_time` 等)保留前綴。
- **`CacheNotifyPoller` 抓取策略** — 改回以 `sys_update_time` 增量抓取(原一度改為其他策略後因多租戶效能退步又回滾),`O(1)` 增量處理而非全表掃描。

### 修正

- **MySQL `ALTER ADD Guid NOT NULL DEFAULT (UUID())` replication-unsafe** — MySQL 在 statement-binlog 下,既有表 `ALTER TABLE ... ADD column UUID NOT NULL DEFAULT (UUID())` 為 replication-unsafe;dialect 拆兩段執行(先以常數預設 `ADD COLUMN`、再 `ALTER COLUMN SET DEFAULT (UUID())`),fresh `CREATE TABLE` 路徑本就安全不受影響。
- **Oracle `ALTER MODIFY` ORA-01442** — Oracle `ALTER TABLE ... MODIFY column NOT NULL` 對既已 NOT NULL 的欄重發會拋 ORA-01442;dialect 修正為僅在 nullability 真正改變時才下 hint。
- **Oracle String / Text 欄一律建 nullable** — Oracle 視 `''` 為 `NULL`,使「常態為空字串」的 `String NOT NULL` 欄在 fresh `CREATE TABLE` 時掛 ORA-01400;dialect 修正為 String / Text 欄一律允許 NULL,不再隨表結構宣告 NOT NULL。
- **MAUI `DynamicForm` 三項修補** — `SetField` 改為 idempotent(同值寫入不再 round-trip)、`ConvertToColumnValue` 補非 null fallback(避免某些 binding 路徑塞 null 進非 nullable 欄)、`ReloadList` 補 `sys_rowid` 欄保留(避免重載後筆列識別丟失)。
- **`ObjectCaching` CI 競爭條件** — 以 lazy `FileModificationToken` 取代 `PhysicalFileProvider`,消除 CI 上偶發的 file-system watcher 競爭(同時移除已棄用的 `Microsoft.Extensions.FileProviders.Physical` 套件參考)。
- **`DemoBusinessObjectFactory` 漏注入 `ILanguageService`** — sample 端工廠補上 i18n 階段引入的 `ILanguageService` 相依,讓 demo BO 可正常解析。
- **`RolePermissionRepository` SQL 串接補空格** — SonarCloud S2857 指出的字串串接缺空格(原本不影響功能,但會導致 log 內 SQL 難讀)。

## [4.6.0]

> Bee.NET 仍處 pre-stable 演進階段。本版主軸為「開放 JSON-RPC 給 JS 前端」：FormBO / SystemBO 共 7 個 CRUD / Session 方法 `ProtectionLevel` 降為 `Public`、新增兩個 JSON-native 取得方法（`GetFormSchema` / `GetFormLayout`），並修正 Plain 路徑 DataSet 反序列化與 Blazor WebAssembly RSA 相關阻塞問題。`MasterKeySource` 預設值改為 `Environment`，依嚴格 SemVer 觀點屬 major，pre-stable 政策下以 minor 發佈。

### 新增

- **`SystemBO.GetFormSchema` / `GetFormLayout`** — 新增兩個 JSON-friendly 取得方法，給 JS / TypeScript 前端走 Plain wire format 直接拿到強型別 `FormSchema` / `FormLayout` 的 JSON tree，做 schema-driven 渲染。`.NET` client 端對應 `SystemApiConnector.GetFormSchemaAsync` / `GetFormLayoutAsync`；既有 `GetDefineAsync<FormSchema>`（XML 字串中介）仍可繼續使用。兩個方法皆為 `Public + Authenticated`。對應決策：[ADR-014](docs/adr/adr-014-jsonrpc-plain-public-default.md)。
- **JSON-RPC 前端整合指引** — 新增 [`docs/jsonrpc-frontend-integration.md`](docs/jsonrpc-frontend-integration.md)（中英雙語），涵蓋 wire format、headers、認證流程、可呼叫方法清單、`JsonRpcErrorCode` 對應表與 TypeScript wrapper snippet，供 React / Vue / Angular / vanilla JS 前端參照。

### 變更

- **`MasterKeySource` 預設改為 `Environment`**（**breaking**）— 新部署一律從環境變數 `$BEE_MASTER_KEY` 讀取 master key,不再於 `DefinePath` 下產生 `Master.key` 檔案。對齊 12-factor「config in env」原則,使 container / Kubernetes / cloud function 等 host 可直接注入金鑰,不需額外 mount secret volume。既有部署若 `SystemSettings.xml` 已明確設定 `<Type>File</Type>` 不受影響;要遷移既有 host:把現有 `Master.key` 的 Base64 內容 set 給 `BEE_MASTER_KEY`,並把 `SystemSettings.xml` 改為 `<Type>Environment</Type><Value>BEE_MASTER_KEY</Value>`。對應決策：[ADR-015](docs/adr/adr-015-master-key-environment-default.md)。
- **7 個 BO 方法 `ProtectionLevel` 降為 `Public`** — `FormBO.GetNewData` / `GetData` / `Save` / `Delete` 與 `SystemBO.EnterCompany` / `LeaveCompany` / `Logout` 由 `Encrypted + Authenticated` 降為 `Public + Authenticated`，讓 JS 前端可透過 `PayloadFormat.Plain` 以原生 JSON over HTTPS 直接呼叫。`Authenticated` 仍守身分門檻、application-layer 業務權限檢查不變。**向下相容**：既有 `Encrypted` 格式的 `.NET` client 仍可正常呼叫（`ApiAccessValidator` 允許高保護格式呼叫低保護方法）。對應決策：[ADR-014](docs/adr/adr-014-jsonrpc-plain-public-default.md)。
- **`FormSchema.MasterTable` 加 `[JsonIgnore]`** — JSON 序列化時 `MasterTable` 屬性不再輸出（其值永遠等於 `Tables[ProgId]`，原本對 JS Plain wire format 是 ~30% 無效 payload）。XML 序列化與 MessagePack 路徑**不受影響**（未加 `XmlIgnore`、`.NET` client 仍走原路徑）。JS / TS 客戶端若曾從 JSON 內 `masterTable` 取 master schema，需改從 `tables` 陣列第一個元素讀。

### 修正

- **Blazor WebAssembly 登入解封** — `RsaCryptor` 改用 PEM (SPKI public / PKCS#1 private) 取代 XML key 格式，移除對 Windows-only `RSA.ToXmlString` / `FromXmlString` 的依賴。WASM 平台再透過 `OperatingSystem.IsBrowser()` fallback：client 送空 `ClientPublicKey`（server 端短路加密、後續 `Encrypted` 請求自動降級為 `Encoded`），`HttpClient` 使用預設實例以走 `BrowserHttpHandler`。端到端驗證 Wasm demo（Sign in → Employee CRUD）通過，`Blazor.Server.Demo` 與 `QuickStart.Console` 無 regression。
- **`ApiInputConverter` 補齊 Plain 路徑反序列化的 converters** — 原本只設 `PropertyNameCaseInsensitive`，缺 `DataTableJsonConverter` / `DataSetJsonConverter` / `JsonStringEnumConverter`，導致 Plain 格式的 `SaveArgs` 進來後 DataTable rows 全部為空，`Save` 一律回 "DataSet has no pending changes"。對齊 write 端 `JsonCodec` 的註冊；任何 Plain 路徑承載 `DataSet` 的呼叫現皆可正常運作。
- **MAUI `DynamicForm` 在 in-place `DataSet` mutation 後正確重繪** — `FormDataObject` 在 New / Load / Save / Delete 流程內就地改寫同一個 `DataSet` 參考，但 `BindableProperty` 只在 reference change 時觸發 `propertyChanged`，導致 `FormPage.RefreshFormView` 看似無效（按 New 後 form 不清空、Save / Delete 按鈕不啟用）。`DynamicForm` 新增 public `Refresh()` 入口直接驅動既有 `Rebuild()`。Blazor 不受影響（Razor 每次 event handler 完成都會 re-render）。

### 升級指引

**`MasterKeySource` 遷移既有 host（File → Environment）：**

```bash
# 1. 把現有 Master.key 的 Base64 內容設為環境變數
export BEE_MASTER_KEY="$(cat $DEFINE_PATH/Master.key)"
```

```xml
<!-- 2. SystemSettings.xml 內把 MasterKeySource 改為 Environment -->
<MasterKeySource>
  <Type>Environment</Type>
  <Value>BEE_MASTER_KEY</Value>
</MasterKeySource>
```

新 host 直接 `export BEE_MASTER_KEY=<base64>` 即可，預設 `SystemSettings.xml` 已是 `Environment`。

**JS / TS 客戶端取 `FormSchema` master schema：**

```diff
- const masterTable = formSchema.masterTable;
+ const masterTable = formSchema.tables[0];
```

`.NET` 客戶端（MessagePack / XML）不需修改。

## [4.5.0]

> Bee.NET 仍處於 pre-stable 演進階段。本次新增三層前端套件（`Bee.UI.Core` 跨平台共通層、`Bee.UI.Maui` MAUI 行動／桌面控制項、`Bee.Web.Blazor.Server` / `Bee.Web.Blazor.Wasm` 兩個 Blazor RCL），並把 API connector 介面整批轉為 async-only。介面簽名變動由嚴格 SemVer 觀點屬 major，pre-stable 政策下以 minor 發佈。

### 新增

- **新套件 `Bee.UI.Core`** — 跨平台 UI 共通層（Web / MAUI 共用 ViewModel、`FormDataObject`、`SystemApiConnector`、`ClientInfo`）。前身為獨立 repo `bee-ui-core`，本版併入 monorepo 統一發佈。對應決策：[ADR-013](docs/adr/adr-013-frontend-api-connection-strategy.md)。
- **新套件 `Bee.UI.Maui`** — .NET MAUI 跨平台控制項層（iOS / Android / macOS / Windows）；提供 `DynamicForm` / `DynamicGrid` / `FormPage` 與 sandbox-friendly `MauiPreferenceEndpointStorage`。同步附範例 `samples/Maui.Demo` 接 `QuickStart.Server`。預設 TFM `net10.0`（library 端不需 MAUI workload 即可 build／consume），平台 TFM 透過 `-p:BeeUiMauiFullPlatforms=true` 開啟。
- **新套件 `Bee.Web.Blazor.Server` / `Bee.Web.Blazor.Wasm`** — Blazor Razor Class Library 雙端對稱實作；提供 `DynamicForm` / `DynamicGrid` / `FormPage` 控制項、`BeeAccessTokenProvider` token 注入、`BeeLoginPanel` 登入面板、`AddBeeBlazor` 服務註冊。
- **`UserMessageException` + `JsonRpcErrorCode.UserMessage`** — 統一「需要顯示給使用者的訊息」傳遞管道；後端 throw 後 `ApiConnector` 依 error code 重建為 client 端 `UserMessageException`，呼叫端可直接以訊息呈現。
- **`FormBO` 補齊 CRUD action** — `FormBusinessObject` 新增 `GetNewData` / `GetData` / `Save` / `Delete` 四個 action,使 `IFormBusinessObject` 涵蓋完整單筆 CRUD 流程(`GetList` 已於 v4.4.0 引入)。
- **`samples/` 範例專案家族** — 新增三組 demo:`QuickStart.Server` + `QuickStart.Console`(P0,本地 + JSON-RPC 雙模式)、`Blazor.Server.Demo` + `Blazor.Wasm.Demo`(P1)、`Maui.Demo`(P2)。各 demo 共用 `Bee.Samples.Shared` 的 define seed 與認證常數,並備有 `.smoke.yaml` 供端到端冒煙測試。

### 變更

- **API connector 介面轉為 async-only** — `IApiConnector` / `IFormApiConnector` / `ISystemApiConnector` 移除全部同步方法,所有對外呼叫一律走 `*Async` 版本。呼叫端需把 `connector.GetData(...)` 改為 `await connector.GetDataAsync(...)`。
- **`ExceptionExtensions` 命名空間搬遷** — 由 `Bee.Base` 改至 `Bee.Base.Exceptions`;呼叫端需新增 `using Bee.Base.Exceptions;`。
- **`ClientInfo` 改為 static class** — `ClientInfo.SystemApiConnector.Initialize()` 改為 async;此變更明確化「單一前端使用者」假設(多用戶 web 情境應改由 per-request DI 提供 token,見 [ADR-013](docs/adr/adr-013-frontend-api-connection-strategy.md) 後續延伸段落)。

### 升級指引

**API connector 呼叫端(同步 → async):**

```diff
- var data = connector.GetData(progId, formData);
+ var data = await connector.GetDataAsync(progId, formData);
```

**`ExceptionExtensions` 命名空間:**

```diff
  using Bee.Base;
+ using Bee.Base.Exceptions;

  ex.Unwrap();
```

**`UserMessageException` 處理(推薦):**

```csharp
try
{
    await connector.SaveAsync(progId, dataSet);
}
catch (UserMessageException ex)
{
    // 訊息已格式化為「給使用者看」的字串;直接顯示
    ShowToast(ex.Message);
}
```

## [4.4.0]

> Bee.NET 仍處於 pre-stable 演進階段；對外公開 API 表面尚無外部消費者，minor 版本允許包含 API 搬遷與少量 breaking change。本次包含介面簽名變動（`IFormRepositoryFactory.CreateDataFormRepository`、`IDataFormRepository.GetList`）與屬性移除（`CompanyInfo.LogDatabaseId`），嚴格 SemVer 觀點屬 major，pre-stable 政策下以 minor 發佈。

### 新增

- **`FormBO.GetList` 統一查詢入口** — `IFormBusinessObject` 宣告 `GetList` 簽名；`FormBusinessObject` 透過 `IDataFormRepository` 實作並支援 `PagingOptions` / `PagingInfo` 分頁；`FormApiConnector.GetList` / `GetListAsync` 提供用戶端入口。跨 5 種 dialect（SQL Server / PostgreSQL / SQLite / MySQL / Oracle）皆已整合測試驗證。
- **`SystemBO` Session Lifecycle 完整化** — 新增 `EnterCompany` / `LeaveCompany` / `Logout` 三個方法，搭配既有 `Login` 構成兩對對稱方法（`Login` ↔ `Logout`、`EnterCompany` ↔ `LeaveCompany`）；`SessionInfo` 加 `CompanyId`（nullable），`Login` 簽名補入 `ISystemBusinessObject`。新增 `CompanyInfo` 型別與 `ICompanyInfoService` 快取服務。對應決策：[ADR-012](docs/adr/adr-012-session-company-context.md)。
- **bo repo DB 路由** — 新增 `DbScope` enum（`Common` / `Company` / `Log`）與 `IRepositoryDatabaseRouter`，BO 不再手寫 databaseId 字串；`BusinessObject` 新增 `ResolveDatabaseId(DbScope)` 與 `CreateDataFormRepository(string progId)` 兩個 protected helper。對應決策：[ADR-010](docs/adr/adr-010-logical-database-category.md)(後續延伸段落)。
- **`SelectCommandBuilder` 分頁與 COUNT** — 跨 5 dialect 支援 `OFFSET/FETCH` 或 `LIMIT/OFFSET`；新增 `BuildCount` 方法產出獨立 `SELECT COUNT(*)`，可獨立於 SELECT 流程使用。
- **`KeyObjectCache<T>` 負向快取** — 預設啟用 5 分鐘絕對過期負向快取以避免 cache penetration；可透過 virtual `GetNegativePolicy` 覆寫或停用（`SessionInfoCache` 已 override 停用，避免匿名流量灌爆快取）。對應決策:[ADR-009](docs/adr/adr-009-cache-implementation.md)(後續延伸段落)。
- **`IBusinessObjectFactory` typed wrapper** — `Bee.Business` 新增 `CreateFormBO(token, progId)` / `CreateSystemBO(token)` 擴充方法，消除呼叫端手動 cast 的噪音。
- **`st_company` / `st_user_company` 兩張系統表** — 落於 common 庫，搭配 `ICompanyRepository` / `IUserCompanyRepository`，讓 `EnterCompany` 對「公司不存在 / 公司停用 / 未授權」一律回 `CompanyAccessDenied`。`DbCategorySettings` 預設 common 分類已包含此兩表。
- **JsonRpcErrorCode 新增兩碼** — `CompanyNotEntered` (-32002, HTTP 409)、`CompanyAccessDenied` (-32003, HTTP 403)。後者刻意合併「無權限」與「不存在」以防止 user enumeration。

### 變更

- **`IFormRepositoryFactory.CreateDataFormRepository`** — 簽名加入 `Guid accessToken` 參數，配合 `IRepositoryDatabaseRouter` 自動路由 databaseId。BO 端建議改用 `BusinessObject.CreateDataFormRepository(progId)` helper，自動帶 token。
- **`IDataFormRepository.GetList`** — 回傳型別改為 `DataFormListResult`（含 `Table` + `Paging`），並加入 `PagingOptions? paging` default 參數。
- **`CompanyInfo.LogDatabaseId` 移除** — `DbScope.Log` 改為固定路由 `databaseId = "log"`（pre-EnterCompany 方法可寫 audit log）。跨公司 log 隔離由後續的 `sys_company_rowid` 列級分區處理，不再需要每家公司獨立 log DB。
- **`SelectCommandBuilder` 未知表名行為一致化** — 由 `KeyNotFoundException` 改為 `InvalidOperationException`，與 Insert / Update / Delete builder 對齊。

### 升級指引

**`IFormRepositoryFactory` 呼叫端：**

```diff
- var repo = factory.CreateDataFormRepository("Employee");
+ var repo = factory.CreateDataFormRepository("Employee", accessToken);
```

> BO 內部建議改用 `BusinessObject.CreateDataFormRepository(progId)` helper，自動帶 token，無需手動傳遞。

**`IDataFormRepository.GetList` 呼叫端：**

```diff
- DataTable table = repo.GetList(filter, sortFields, fields);
+ DataFormListResult result = repo.GetList(filter, sortFields, fields, paging: null);
+ DataTable table = result.Table;
```

**`CompanyInfo.LogDatabaseId` 引用點：**

```diff
- var logDbId = companyInfo.LogDatabaseId;
+ var logDbId = "log";  // 框架固定路由；跨公司隔離改用 sys_company_rowid 列級分區
```

或透過 `BusinessObject.ResolveDatabaseId(DbScope.Log)` 取得。

## [4.3.0]

> Bee.NET 仍處於 pre-stable 演進階段；對外公開 API 表面尚無外部消費者，minor 版本允許包含命名空間搬遷。本次調整以嚴格 SemVer 觀點屬 major，pre-stable 政策下以 minor 發佈。

### 新增

- **新套件 `Bee.Hosting`** — Bee.NET 框架的 composition root。將所有後端服務（`IDefineAccess`、`IDbAccessFactory`、`IBusinessObjectFactory`、`JsonRpcExecutor` 等）註冊到任意 `IServiceCollection`，不依賴 ASP.NET Core。非 ASP.NET Core 宿主（WinForms、WPF、Console、Worker Service、整合測試）現在可以註冊框架而不必拖入 `Microsoft.AspNetCore.App`。

### 變更

- **`BeeFrameworkServiceCollectionExtensions.AddBeeFramework` 從 `Bee.Api.AspNetCore` 搬至 `Bee.Hosting`。**
  - 命名空間從 `Bee.Api.AspNetCore` 改為 `Bee.Hosting`
  - ASP.NET Core 宿主：`Bee.Api.AspNetCore` 已改為引用 `Bee.Hosting`，會以遞移方式帶入。啟動程式需在既有 `using Bee.Api.AspNetCore;` 旁加上 `using Bee.Hosting;`
  - 非 ASP.NET Core 宿主：直接引用 `Bee.Hosting` 取代 `Bee.Api.AspNetCore`，不再透過遞移帶入 `Microsoft.AspNetCore.App`
- `Bee.Api.AspNetCore` 現在僅包含 ASP.NET Core 整合（`UseBeeFramework` middleware hook + `ApiServiceController`）；原有 4 個 ProjectReference（`Bee.Api.Core`、`Bee.Business`、`Bee.Db`、`Bee.ObjectCaching`、`Bee.Repository`）全部合併至 `Bee.Hosting`

### 升級指引

**ASP.NET Core web host：**

```diff
+ using Bee.Hosting;
  using Bee.Api.AspNetCore;

  var settings = SystemSettingsLoader.Load(pathOptions);
  services.AddBeeFramework(settings.BackendConfiguration, pathOptions);
  app.UseBeeFramework();
```

**非 ASP.NET Core 宿主（WinForms / WPF / Console / Worker / 整合測試）：**

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

// 把後端 service provider 注入給 UI 層作為近端連線來源。
ApiClientInfo.LocalServiceProvider = sp;
ApiClientInfo.ConnectType = ConnectType.Local;
```

## [4.2.0] 與更早版本

見 git 歷史（`git log --oneline`）。
