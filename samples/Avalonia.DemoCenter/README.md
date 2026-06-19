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

## 場景：Data Editors → 全部編輯器

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

## 規劃中的後續階段

見 [plan-avalonia-demo-center.md](../../docs/plans/plan-avalonia-demo-center.md)：

- 階段 2：`IDemoModule` 抽象 + 模組註冊表 + View Source（EmbeddedResource 讀真實 `.cs`）
- 階段 3：Data Editors 場景組（7 個編輯器 × 多場景）
- 階段 4：Grid / Views 場景組
- 階段 5：主題矩陣掃描、定位為 Maui/Blazor 移植對齊基準
