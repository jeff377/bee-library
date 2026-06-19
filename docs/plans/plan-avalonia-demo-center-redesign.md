# 計畫：Avalonia.DemoCenter 重新設計為「概念/功能主題」導向

**狀態：🚧 進行中（2026-06-19）**

> 全域預設主題：**深色（Dark）**（使用者指定；工具列仍可切回 Light）。

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | IA 重構：`IDemoModule` 改 2 層（主題 → 案例）、shell 導覽樹改 2 層、移除舊 per-control / 總覽模組；交付「控件類型」主題作為新結構驗證；預設深色 | ✅ 已完成（2026-06-19） |
| 2 | 主題：資料繫結、唯讀與必填、FormMode 顯示狀態、開窗選資料 | ✅ 已完成（2026-06-19） |
| 3 | 主題：Layout 排版、Grid、Master-Detail | ✅ 已完成（2026-06-19） |
| 4 | 收尾：README（repo 雙語同步）、`.smoke.yaml`、主題/Light·Dark 自測矩陣、對齊基準定位更新 | 📝 待做 |

## 背景

第一版 Demo Center（[plan-avalonia-demo-center.md](plan-avalonia-demo-center.md)，已完成）以**控件**為組織主軸：每個 per-control 模組（`TextEditModule` 等）把「基本綁定 + Metadata + 唯讀」三維度疊在同一視圖。實際使用後發現**雜亂**——一個案例混多個主題，失去 DevExpress Demo Center「每個 demo 只講一件事」的清爽。

**重新設計目標**（已與使用者拍板）：

1. **組織主軸改為概念/功能主題優先** —— 頂層 = 框架概念（資料繫結 / 唯讀與必填 / Layout / 控件類型 / Grid / Master-Detail），每個案例只示範**單一主題**。
2. **全面重做、移除舊模組** —— 現有 7 個 per-control 模組與「總覽·原生 vs 繼承」模組全部移除，內容依新主題重新切分（回歸比對價值改掛在「控件類型」主題下續存）。

## 資訊架構（IA）變更

### 導覽樹：3 層 → 2 層

第一版：`Category（類別）→ ControlName（控件）→ ScenarioTitle（場景）`（3 層）。
重新設計：`Category（主題）→ Title（案例）`（2 層，貼近 DevExpress Module → Demo）。

### `IDemoModule` 介面調整

```csharp
public interface IDemoModule
{
    string Category { get; }   // 主題，如 "資料繫結"
    string Title { get; }      // 案例標題，如 "Ambient 繫結"
    string Description { get; }
    Control BuildView();
    string GetSourceText();
}
```

- 移除 `ControlName`（3 層中層級不再需要）。
- `ScenarioTitle` → `Title`。
- `DemoModuleBase`（EmbeddedResource 讀 source）、`DemoModuleRegistry`（註冊表）、shell 的 Demo/Source 分頁、全域工具列（主題 + FormMode）**全部沿用**。
- `MainWindow.BuildNavTree` 改為只依 `Category` 分組（2 層）。

## 主題與案例規劃

| 主題（Category） | 案例（Title） | 重點 |
|------|------|------|
| **控件類型** | 控件一覽 | 每個 ControlType 對應的繼承控件各一，可編輯、即時值 |
| | 原生 vs 繼承（BindFieldControl） | 欄位級控件（`IBindFieldControl` / `IFieldEditor`：TextEdit / DateEdit / DropDownEdit…）並排原生 vs 繼承，含一般 / 唯讀（由舊 `EditorsComparisonModule` 的欄位編輯器區塊重切） |
| | 原生 vs 繼承（BindTableControl） | 表格級控件（`IBindTableControl`：`GridControl`）並排原生 `DataGrid`，含 Layout / Ambient 綁定、in-cell / EditForm（由舊 `EditorsComparisonModule` 的 grid 區塊重切） |
| **資料繫結** | Ambient 繫結 | `FormScope` 容器設一次，子編輯器自動綁定；即時值 readout |
| | 明確繫結 | `editor.Bind(dataObject, layoutField)` 單欄 |
| | 即時雙向同步 | 兩控件綁同欄位，一改全動 |
| **唯讀與必填** | LayoutField.ReadOnly | master 欄位永久唯讀（去框外觀） |
| | 必填 / 唯讀標示 | `GridControl` 欄位 Required（藍）/ ReadOnly（棕）header（library 內建上色） |
| **FormMode 顯示狀態** | 控件 × FormMode 三態（含 AllowEditModes） | 同一組控件三欄並排，各欄以 `FormScope.SetFormMode` 釘住 View / Add / Edit，一眼比對：①控件每模式呈現（唯讀去框、ButtonEdit 圖示隱藏）②逐欄 `AllowEditModes`（`FormEditModes`：Add / Edit / All / None）的可編輯閘控——例 key 欄只在 Add 可編、稽核欄 None。不依賴頂部工具列（源自封存 plan-layout-form-mode-editability） |
| | Grid × FormMode | `GridControl` 在三態下的編輯能力 / 工具列可見性差異（`AllowEdit` + `AllowEditModes`） |
| **開窗選資料（Lookup）** | ButtonEdit 開窗選資料 | `ButtonEdit` 點圖示開窗、選取後寫回值（本機 picker；生產的 `RelationProgId` → `LookupDialog` 後端查詢流程見 `Avalonia.Demo`） |
| **Layout 排版** | FormLayout 自動產生 | `GetFormLayout()` 產 master 區段 + 明細 |
| | 多欄排版 | `ColumnCount` / `ColumnSpan` 多欄擺放 |
| **Grid** | In-cell 編輯 | 雙擊 cell / popup 編輯器置換 |
| | EditForm 彈窗 | grid 唯讀、彈窗編輯整列 |
| | Ambient grid | 只設 `TableName` 自動綁定 |
| | List mode | 綁獨立 `DataTable`，唯讀清單 |
| **Master-Detail** | 主檔 + 明細 | `FormLayoutRenderer` 渲染 master 區段 + 明細 grid |

> FormMode 既由全域工具列即時切換，也獨立成「FormMode 顯示狀態」主題——以三欄釘住 View/Add/Edit 並排比對，比單純切工具列更直觀。

### 既有 helper 沿用

- `SampleFormData`（Employee + Phones 假資料）→ Layout / Grid / Master-Detail 主題。
- `FormLayoutRenderer`（公開 primitive 渲染 layout）→ Layout / Master-Detail 主題；**需小幅升級**支援 `ColumnCount` 多欄（鏡像 `FormView.BuildFieldGrid`）以支援「多欄排版」案例。
- `DataEditorParts`（單欄物件 / 區塊卡 / 即時值 / Compose）→ 資料繫結 / 唯讀主題。

## 移除清單（階段 1）

- `Modules/DataEditors/`：`TextEditModule`、`MemoEditModule`、`ButtonEditModule`、`DateEditModule`、`YearMonthEditModule`、`DropDownEditModule`、`CheckEditModule`（內容重切到新主題；`ButtonEdit` 的本機 picker 移到「控件類型 → 控件一覽」或獨立案例）。
- `Modules/EditorsComparisonModule.cs`（重切為「控件類型 → 原生 vs 繼承 比對」）。
- `DataEditorParts` 視重用情況保留或併入新 helper。

## 階段細節

### 階段 1：IA 重構 + 控件類型主題
- 改 `IDemoModule`（2 層）、`DemoModuleBase` 不動、`DemoModuleRegistry` 改填新模組、`MainWindow` 樹改 2 層。
- 移除舊模組，新增「控件類型」主題兩案例，確保新結構可建置、可啟動、View Source 正常。

### 階段 2：資料繫結 + 唯讀與必填 + FormMode 顯示狀態 + 開窗選資料
- 各主題聚焦單一概念。
- FormMode 顯示狀態：三欄並排，各以 `FormScope.SetFormMode` 釘住一個 mode（不依賴工具列）。
- 開窗選資料：`ButtonEdit` 本機 picker 開窗寫回（沿用第一版 `ButtonEditModule` 的 picker 邏輯）。

### 階段 3：Layout + Grid + Master-Detail
- 升級 `FormLayoutRenderer` 支援多欄。
- Grid 四案例、Master-Detail 一案例。

### 階段 4：收尾
- README 改寫（主題導向導覽說明）、repo 雙語文件如需同步、`.smoke.yaml` expect_text 更新為新主題、自測矩陣、對齊基準定位更新。

## 風險

- **2 層 vs 既有 3 層程式**：`IDemoModule` 改介面會牽動 shell 與所有模組；階段 1 一次到位、保持每階段可 run。
- **案例顆粒度**：每案例只一個主題，避免又回到「疊多維度」的雜亂；審查時以「這個 case 是否只講一件事」為準。
- **`FieldCaptionStyle` internal**：master 欄位 caption 無法上色，故「必填 / 唯讀標示」案例以 `GridControl` 欄位 header（library 內建上色）示範，非 master 表單 caption。
- **範圍**：8 主題約 19 案例，嚴格依階段交付，每階段可獨立 run。
