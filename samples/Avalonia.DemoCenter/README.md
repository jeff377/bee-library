# Avalonia.DemoCenter

`Bee.UI.Avalonia` 控件展示中心（DevExpress Demo Center 模式，**主題/功能導向**）：左側導覽樹（主題 → 案例）、右側 `Demo` / `Source` 分頁、頂部全域工具列（主題 Light/Dark）。**預設深色**。FormMode 切換不在全域工具列，收在「FormMode 顯示狀態」主題內，避免驅動不相關的範例。

每個案例只示範**單一主題**（資料繫結、唯讀、FormMode…），對應 DevExpress「每個 demo 只講一件事」的清爽。

## 定位

| 角色 | 說明 |
|------|------|
| 展示廳 | 外部框架使用者瀏覽控件能力的門面 |
| 活文件 / 對齊基準 | 每個控件 / 概念的標準行為與外觀契約；日後移植 Maui/Blazor 的對齊範本 |
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

- **主題切換**：右上角 ToggleSwitch 切 Light / Dark（沿用 `Semi.Avalonia`；預設 Dark）。

> FormMode（View/Add/Edit）切換**不**在全域工具列——它只屬於「FormMode 顯示狀態」主題（互動切換案例 + 三欄釘住比對），不該驅動其他不相關範例。其餘案例預設 Edit 模式（可編輯）。

## 主題與案例

導覽樹為兩層：**主題（Category）→ 案例（Title）**。

| 主題 | 案例 | 重點 |
|------|------|------|
| **控件類型** | 控件一覽 | 每個 ControlType 的繼承控件各一，可編輯、即時值 |
| | 原生 vs 繼承（BindFieldControl） | 欄位級控件（`IBindFieldControl` / `IFieldEditor`）並排原生，含一般 / 唯讀 |
| | 原生 vs 繼承（BindTableControl） | 表格級控件（`IBindTableControl`：`GridControl`）並排原生 `DataGrid` |
| **資料繫結** | Ambient 繫結 | 容器設一次 `FormScope.SetDataObject`，子編輯器只給 `FieldName` 自動綁定 |
| | 明確繫結 | `editor.Bind(dataObject, layoutField)` 不靠 ambient |
| | 雙向同步 | 兩控件綁同欄位，輸入離開（或 Enter）提交後同步（FormDataObject 為單一來源） |
| | DataObject 事件 | `FieldValueChanged` / `RowAdded` / `RowDeleted` / `IsDirtyChanged` / `DataSetReplaced` 即時記錄 |
| **唯讀與必填** | LayoutField.ReadOnly | 永久唯讀去框留底線；CheckEdit 灰框留字 |
| | 必填 / 唯讀標示 | `GridControl` 表頭色：唯讀棕、必填藍（library 內建上色） |
| **FormMode 顯示狀態** | 互動切換 | FormMode 下拉即時驅動一組控件 + 明細 grid 的唯讀/編輯（FormMode 切換唯一的所在） |
| | 控件 × FormMode 三態（含 AllowEditModes） | 三欄釘 View/Add/Edit；欄位帶不同 `AllowEditModes`（All / Add / Edit / None），看控件呈現 + 逐欄可編輯閘控 |
| | Grid × FormMode | GridControl 三態下編輯能力 / 工具列可見性差異 |
| **開窗選資料** | ButtonEdit 開窗選資料 | 點圖示開本機 picker 寫回值（生產 `RelationProgId`→`LookupDialog` 後端流程見 `Avalonia.Demo`） |
| **Layout 排版** | FormLayout 自動產生 | `GetFormLayout()` 由 schema 自動產生區段 + 欄位擺放 |
| | 多欄排版 | `ColumnCount=2` + 欄位 `ColumnSpan` 跨欄擺放 |
| **Grid** | In-cell 編輯 | 雙擊 cell / popup 編輯器置換（策略見 [ADR-021](../../docs/adr/adr-021-avalonia-datagrid-editing-strategy.md)） |
| | EditForm 彈窗 | grid 唯讀、彈窗編輯整列 |
| | Ambient 綁定 | 只設 `TableName` 自動綁定、欄位自動產生 |
| | List mode | 綁獨立 `DataTable`，唯讀清單、工具列隱藏 |
| **Master-Detail** | 主檔 + 明細 | `FormLayoutRenderer` 渲染 master 區段 + 明細 grid |

> **Views（FormView/ListView）路線**：兩者為後端耦合控件，本中心無後端，故以「`FormDataObject` 當 VM + 假資料 → 前端繫結」示範——用與生產 `FormView` 同一套公開 primitive（`FieldEditorFactory` + `GridControl`）渲染（見 Master-Detail / Layout / Grid 主題）。後端載入/存檔/列事件見 `Avalonia.Demo`。

## 模組架構（IDemoModule）

每個案例是一個 [`IDemoModule`](Modules/IDemoModule.cs)（`Category` / `Title` / `Description` / `BuildView()` / `GetSourceText()`），集中註冊於 [`DemoModuleRegistry`](Modules/DemoModuleRegistry.cs)；導覽樹由註冊表自動生成（依 `Category` 分組成兩層）。

**View Source**：右側 `Demo` / `Source` 分頁。`Source` 顯示模組自身的真實 `.cs`——[`DemoModuleBase`](Modules/DemoModuleBase.cs) `GetSourceText()` 從 EmbeddedResource 讀出（csproj 把 `Modules/**/*.cs` 一併嵌入），故顯示內容永不與實際執行的程式碼脫鉤。

## 新增一個案例

1. 在 `Modules/<主題資料夾>/` 實作 `DemoModuleBase`，覆寫 `Category` / `Title` / `Description` / `BuildView()`。
2. 在 [`DemoModuleRegistry`](Modules/DemoModuleRegistry.cs) 的 `Modules` 清單加一行。

導覽樹與 View Source 會自動帶出——`Modules/**/*.cs` 已設為 EmbeddedResource，`GetSourceText()` 依型別全名解析資源（**資料夾須對映命名空間**）。常用 helper：`DataEditorParts`（單欄物件 / 區塊卡 / 即時值 / Compose）、`SampleFormData`（Employee + Phones 假資料）、`FormLayoutRenderer`（公開 primitive 渲染 layout）。

## 對應 library 元件

| Demo 行為 | library 元件 |
|-----------|--------------|
| 繼承控件沿用 Semi 樣式 | [src/Bee.UI.Avalonia/Controls/Editors/](../../src/Bee.UI.Avalonia/Controls/Editors/)（各控件 `StyleKeyOverride`） |
| ambient 綁定（容器設一次） | [FormScope.cs](../../src/Bee.UI.Avalonia/Controls/Editors/FormScope.cs) |
| FormMode / AllowEditModes 驅動唯讀 | [FieldEditorBinder.cs](../../src/Bee.UI.Avalonia/Controls/Editors/FieldEditorBinder.cs) 的 `AllowsEdit` / `OnFormModeChanged` |
| 欄位值即時刷新 | [FormDataObject.cs](../../src/Bee.UI.Avalonia/DataObjects/FormDataObject.cs) 的 `FieldValueChanged` 事件 |

## 主題 / FormMode 自測矩陣

逐案例目視掃過（程式已驗證可建置、可啟動；外觀一致性需人眼確認）：

| 維度 | 切換點 | 看什麼 |
|------|--------|--------|
| Light / Dark | 右上 ToggleSwitch | 每個案例在兩個 variant 下，繼承控件背景/邊框/字色與原生對齊，無突兀色塊 |
| FormMode 三態 | FormMode 顯示狀態 → 互動切換 | 切 View → 去框唯讀、ButtonEdit 圖示隱藏、grid 唯讀；Add / Edit → 可編輯 |
| AllowEditModes | FormMode 顯示狀態 → 控件 × 三態 | 三欄釘 View/Add/Edit，逐欄依 `AllowEditModes` 啟用/停用 |
| View Source | Demo / Source 分頁 | 每案例 Source 顯示該模組真實 `.cs`，與 Demo 行為一致 |

> 主題範圍：僅 `Semi.Avalonia` × Light/Dark（不納 Fluent runtime 切換，見 plan 拍板決議 #2）。

## 作為 Maui / Blazor 移植的對齊基準

`Bee.UI.Avalonia` 是 UI 架構試點：繼承式控件 + View 層先在此定稿，再移植 Maui / Blazor。本 Demo Center 即「對齊範本」——

- 每個控件 / 概念的**標準行為與外觀契約**（綁定、唯讀、必填、FormMode、AllowEditModes、Layout、Grid）在此一處可見、可比對。
- 日後在 Maui / Blazor 實作對應控件時，以本中心每個案例的行為為驗收基準：相同 schema / 假資料 / FormMode 下，跨平台應呈現一致的綁定與狀態切換。
- 控件外觀變更（如唯讀去框）先在此目視驗證，再回推其他平台。

## Plan

- 重新設計（主題導向 IA + 8 主題）：[plan-avalonia-demo-center-redesign.md](../../docs/plans/plan-avalonia-demo-center-redesign.md)
- 第一版（建立 Demo Center shell）：[plan-avalonia-demo-center.md](../../docs/plans/plan-avalonia-demo-center.md)
