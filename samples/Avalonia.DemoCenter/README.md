# Avalonia.DemoCenter

`Bee.UI.Avalonia` 控件展示中心（DevExpress Demo Center 模式）：左側導覽樹（類別 → 控件 → 場景）、右側活的互動控件宿主、頂部全域工具列（主題 Light/Dark 切換、FormMode View/Add/Edit 切換）。

> 由原 `Avalonia.Editors.Gallery` 升級改名而來。Gallery 的「原生 vs 繼承並排」比對價值未丟——遷為本中心的第一個場景（`Data Editors → 全部編輯器`）。

## 定位

| 角色 | 說明 |
|------|------|
| 展示廳 | 外部框架使用者瀏覽控件能力的門面 |
| 活文件 / 對齊基準 | 每個控件的標準行為與外觀契約；日後移植 Maui/Blazor 的對齊範本 |
| 開發驗證面 | 修改控件外觀（如唯讀去框）時的目視回饋場 |

與 `Avalonia.Demo` 區隔：後者示範端到端**應用**（登入 → 連線 → JSON-RPC → 渲染表單）；本 Demo Center 只聚焦**控件層**，不連後端、不涉入 Login / CRUD 流程。

## 前置條件

- .NET 10 SDK
- 無需後端、無需資料庫——資料來源是 in-memory `FormSchema` + `FormDataObject`

## 跑起來

```bash
dotnet run --project samples/Avalonia.DemoCenter/Avalonia.DemoCenter.csproj
```

## 全域工具列

- **主題切換**：右上角 ToggleSwitch 切 Light / Dark（主題沿用 `Semi.Avalonia`）。
- **FormMode 切換**：View / Add / Edit 下拉，驅動 `FormScope.SetFormMode`，即時翻動目前場景內繼承控件的唯讀 / 編輯外觀。

## 導覽結構

- **Data Editors** — 7 個編輯器各一個場景模組（`Modules/DataEditors/`）：`TextEdit` / `MemoEdit` / `ButtonEdit` / `DateEdit` / `YearMonthEdit` / `DropDownEdit` / `CheckEdit`。每個模組視圖以具標題的區塊呈現：**基本綁定 + 即時值**、**Metadata**（MaxLength / ListItems 等）、**唯讀**（`LayoutField.ReadOnly`）。
  - `ButtonEdit`：本中心無後端，以 `ButtonClick` 開本機 picker 寫回值示範；生產的 `RelationProgId` → `LookupDialog` 後端查詢流程見 `Avalonia.Demo`。
- **總覽 → 原生 vs 繼承** — 由原 Gallery 遷入的整批比對模組（`EditorsComparisonModule`）：每個 `ControlType` 左欄原生、右欄繼承，含「一般」與「唯讀 / 停用」、`GridControl` 兩種編輯模式，保留整批回歸價值。

「唯讀」雙軌：每模組含一個永久唯讀欄（`LayoutField.ReadOnly`）；另切工具列 FormMode 至 View 時，ambient 綁定欄整批轉唯讀。

- **Grid**（`Modules/Grids/`）— `GridControl`：Layout 綁定 + in-cell 編輯、ambient（只設 `TableName`）、EditForm 彈窗編輯三種模式（編輯策略見 [ADR-021](../../docs/adr/adr-021-avalonia-datagrid-editing-strategy.md)）。
- **Views**（`Modules/Views/`）— 採「`FormDataObject` 當 VM + 假資料 → 前端繫結」路線（FormView/ListView 為後端耦合控件，本中心無後端）：
  - `FormView`：以 `FormSchema.GetFormLayout()` 產 layout，透過公開 primitive（`FieldEditorFactory` + `GridControl`，與生產 `FormView` 同一套）渲染 master 區段 + 明細 grid；FormMode 三態由工具列驅動。
  - `ListView`：`GridControl` list-mode 綁定獨立 `DataTable`（唯讀、工具列隱藏）。
  - 生產的 FormView/ListView 後端載入/存檔/列事件見 `Avalonia.Demo`。

## 總覽比對 checklist

每個 `ControlType` 一個區塊，左欄原生控件、右欄繼承控件，各含「一般」與「唯讀 / 停用」兩種狀態。比對 checklist：

1. **靜態樣式**：左右兩欄的背景、邊框、圓角、字體、間距應完全一致
2. **互動偽類**：hover / focus / pressed 的視覺反應應一致
3. **Light/Dark**：工具列切換主題，逐區重看
4. **綁定行為**：在右欄輸入，底部「FormDataObject 即時欄位值」應即時更新；
   `TextEdit` 受 `MaxLength=20` 限制、`DropDownEdit` 選項來自 `FormField.ListItems`
5. **FormMode**：工具列切 View，右欄繼承控件應整批轉唯讀外觀
6. **GridControl 區**：表頭 / 列 hover / 選取列 / 格線與左欄原生 `DataGrid` 一致；
   「Ambient」列驗證只設 `TableName` 即從 `FormScope` 自動綁定明細表
7. **In-cell 編輯**（策略見 [ADR-021](../../docs/adr/adr-021-avalonia-datagrid-editing-strategy.md)）：
   雙擊 cell（或 F2）進入編輯；popup 型編輯器（DropDown / Date / YearMonth）單擊置換
8. **EditForm 模式區**：grid 唯讀，雙擊列或 Edit 圖示開彈窗編輯整列

## 對應 library 元件

| Demo 行為 | library 元件 |
|-----------|--------------|
| 繼承控件沿用 Semi 樣式 | [src/Bee.UI.Avalonia/Controls/Editors/](../../src/Bee.UI.Avalonia/Controls/Editors/)（各控件 `StyleKeyOverride`） |
| ambient 綁定（容器設一次） | [FormScope.cs](../../src/Bee.UI.Avalonia/Controls/Editors/FormScope.cs) |
| FormMode 驅動唯讀 / 編輯 | [FieldEditorBinder.cs](../../src/Bee.UI.Avalonia/Controls/Editors/FieldEditorBinder.cs) 的 `OnFormModeChanged` |
| 欄位值即時刷新 | [FormDataObject.cs](../../src/Bee.UI.Avalonia/DataObjects/FormDataObject.cs) 的 `FieldValueChanged` 事件 |

## 模組架構（IDemoModule）

每個場景是一個 [`IDemoModule`](Modules/IDemoModule.cs)，集中註冊於 [`DemoModuleRegistry`](Modules/DemoModuleRegistry.cs)；導覽樹由註冊表自動生成（依 `Category` → `ControlName` 分組）。新增場景只需實作 [`DemoModuleBase`](Modules/DemoModuleBase.cs) 並在註冊表加一行。

**View Source**：右側 `Demo` / `Source` 分頁。`Source` 顯示模組自身的真實 `.cs`——`DemoModuleBase.GetSourceText()` 從 EmbeddedResource 讀出（csproj 把 `Modules/**/*.cs` 一併嵌入），故顯示內容永不與實際執行的程式碼脫鉤。

## 新增一個場景

1. 在 `Modules/`（或子資料夾）實作 `DemoModuleBase`，覆寫 `Category` / `ControlName` / `ScenarioTitle` / `Description` / `BuildView()`。
2. 在 [`DemoModuleRegistry`](Modules/DemoModuleRegistry.cs) 的 `Modules` 清單加一行。

導覽樹與 View Source 會自動帶出——`Modules/**/*.cs` 已設為 EmbeddedResource，`GetSourceText()` 依型別全名解析資源（資料夾須對映命名空間）。

## 主題 / FormMode 自測矩陣

逐場景目視掃過（程式已驗證可建置、可啟動；外觀一致性需人眼確認）：

| 維度 | 切換點 | 看什麼 |
|------|--------|--------|
| Light / Dark | 右上 ToggleSwitch | 每個場景在兩個 variant 下，繼承控件背景/邊框/字色與原生對齊，無突兀色塊 |
| FormMode 三態 | 工具列下拉 | View → ambient 綁定欄整批轉唯讀（去框）；Add / Edit → 可編輯 |
| 唯讀雙軌 | — | 每模組的 `LayoutField.ReadOnly` 永久唯讀欄，與 FormMode=View 的整批唯讀外觀一致 |
| View Source | Demo / Source 分頁 | 每場景 Source 顯示該模組真實 `.cs`，與 Demo 行為一致 |

> 主題範圍：僅 `Semi.Avalonia` × Light/Dark（不納 Fluent runtime 切換，見 plan 已拍板決議 #2）。

## 作為 Maui / Blazor 移植的對齊基準

`Bee.UI.Avalonia` 是 UI 架構試點：繼承式控件 + View 層先在此定稿，再移植 Maui / Blazor。本 Demo Center 即「對齊範本」——

- 每個控件的**標準行為與外觀契約**（綁定、Metadata、唯讀、FormMode 反應）在此一處可見、可比對。
- 日後在 Maui / Blazor 實作對應控件時，以本中心每個場景的行為為驗收基準：相同 schema / 假資料 / FormMode 下，跨平台應呈現一致的綁定與狀態切換。
- 控件外觀變更（如唯讀去框）先在此目視驗證，再回推其他平台。

## 後續

本 plan（階段 1–5）已完成，見 [plan-avalonia-demo-center.md](../../docs/plans/plan-avalonia-demo-center.md)。
