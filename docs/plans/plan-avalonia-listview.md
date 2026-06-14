# 計畫：Bee.UI.Avalonia 新增 ListView（清單瀏覽控件）

**狀態：✅ 已完成（2026-06-14）**

> 框架最終形：`ListView`（多筆瀏覽）+ `FormView`（單筆主檔+明細，三態 View/Add/Edit）兩個獨立控件；`SingleFormBase` / `DynamicForm` / `RecordView` / 舊合一 `FormView` 已合併進新 `FormView`。切換由 host 編排（demo `FormWorkspace`）。下方「單筆 View 類別合併」節為最終定案；早期「RecordView + 過渡 FormView」段落為過程紀錄。

> 由 `plan-bee-northwind-demo.md` 階段 1 的 dogfooding 逼出的框架缺口：現代 ERP 慣例「清單瀏覽」與「單筆編輯」應分離，但框架目前只有把兩者疊在同頁的 `FormView`。本 plan 在框架層補上可重用的 `ListView`，供所有表單共用。

## 背景

`src/Bee.UI.Avalonia/Controls/FormView.cs` 目前把「工具列 → GridControl 清單 → DynamicForm 單筆」垂直疊在同一頁（`FormView.cs:138-150`）：選清單列 → `FormDataObject.LoadAsync` 載入單筆、同頁顯示。這違反一般 ERP「清單頁／單筆頁分離」的操作慣例。

使用者（ERP 架構師）定方向：**先在 `Bee.UI.Avalonia` 新增一個獨立的「清單」View，讓不同表單共用**，而非在 demo 裡硬接。

## 已定案決策

| 項目 | 決策 |
|------|------|
| 清單控件命名 | **`ListView`**（對應 `FormSchema.ListFields` / `GetListLayout` 的「清單」領域用語；Avalonia 本身無此型別，不衝突） |
| 單筆控件命名 | **`RecordView`**（與 ListView 對稱：多筆/單筆）。涵蓋**瀏覽/新增/修改**三態，用既有 `SingleFormMode`（View/Add/Edit）表達，故**不叫 EditView**（語意太窄）。繼承 `SingleFormBase` |
| 本次範圍 | **`ListView` + `RecordView` 兩控件都做**（含測試）。現有 `FormView` 保留向後相容（Avalonia.Demo 不動） |
| 切換編排位置 | **demo host 編排**：框架只出 `ListView` + `RecordView` 兩個獨立控件；由 demo 的 `FormWorkspace` 在同一區域切換（非另開分頁）。框架不出切換容器 |
| ListView 工具列 | **檢視 / 新增 / 編輯 / 刪除**（View/New/Edit/Delete）。刪除內部處理（刪除後重載、停留清單）；雙擊列＝編輯 |
| RecordView 工具列（隨模式） | 瀏覽（View）→ **返回**（唯讀）；新增/修改（Add/Edit）→ **儲存 / 取消** |
| 事件介面 | `ListView`：`ViewRequested(rowId)` / `EditRequested(rowId)` / `AddRequested` / `ErrorOccurred`，`ReloadAsync()` 公開。`RecordView`：`Saved` / `Closed` / `ErrorOccurred`，公開 `ViewAsync(rowId)` / `EditAsync(rowId)` / `NewAsync()` |

## 設計：`ListView` 控件

### 定位

`ListView` = **某 ProgId 的全部記錄清單瀏覽器**（master list browser）。read-only 清單 + 工具列，不含單筆欄位表單。與 `FormView` 平行（都繼承 `UserControl`，但 `ListView` 不是 `SingleFormBase` —— 它無單筆 form mode）。

### 公開 API（對齊 FormView 慣例）

```csharp
public class ListView : UserControl
{
    // StyledProperty（與 FormView 同款，host 通常只給 ProgId）
    public string ProgId { get; set; }                  // 驅動 schema/connector fallback
    public Guid AccessToken { get; set; }               // 空 → ClientInfo.AccessToken
    public FormSchema? Schema { get; set; }             // 未給 → GetDefineAsync
    public FormApiConnector? FormConnector { get; set; }// 未給 → CreateFormApiConnector

    // 事件：呈現方式交給 host 決定
    public event EventHandler<Guid>? RecordActivated;   // 雙擊列 / Edit 鈕（帶 sys_rowid）
    public event EventHandler? AddRequested;            // New 鈕
    public event EventHandler<Exception>? ErrorOccurred;

    public Task InitializeAsync();                       // attach 時自動觸發
    public Task ReloadAsync();                           // 對外可手動重載
}
```

### 內部組成（直接複用 FormView 已驗證的片段）

- **schema/connector/token 解析**：照 `FormView` 的 `ResolveSystemConnector` / `ResolveFormConnector` / `ResolveAccessToken` 三個 `protected virtual` hook + `TryResolveSchemaAsync` / fallback 流程（測試可覆寫、不碰 `ClientInfo`）
- **清單載入**：照 `FormView.ReloadListAsync` + `ComputeSelectFields`（`sys_rowid` 前綴 + `ListFields`）→ `GetListAsync` → `GridControl.DataTable`
- **GridControl**：`Bind(Schema.GetListLayout(), rows:null)` 先出欄、`ReloadAsync` 灌列；read-only（不進 detail 編輯模式）；`RowSelected` / 雙擊 → `RecordActivated`
- **工具列**：`New`（→ `AddRequested`）、`Edit`（→ `RecordActivated(選取列)`）、`Delete`（確認後 `FormConnector.DeleteAsync` + 重載，內部處理）、`Refresh`（→ `ReloadAsync`）
- **空清單提示**、**錯誤 label**、busy guard：照 FormView 同款

> 不重構 FormView、不抽共用基底（這輪求穩、避免動到既有 `Avalonia.Demo` / FormView 測試）。少量解析邏輯複製到 ListView；待下輪做 EditView 時再評估是否把三方共用片段上收為 `FormViewBase` 或 helper。

### 測試（`tests/Bee.UI.Avalonia.UnitTests/Controls/ListViewTests.cs`）

照 `FormViewTests` 模式：`TestListView` 子類覆寫 `Resolve*` + 既有 `FakeFormApiConnector`，驅動 `InitializeAsync`、不需視覺樹。涵蓋：

- 公開屬性 ↔ StyledProperty 對應
- `InitializeAsync` 後 `GetListAsync` 被呼叫、`GridControl` 收到列
- `ComputeSelectFields` 含 `sys_rowid` 前綴
- 雙擊列 / Edit → `RecordActivated` 帶正確 rowId
- New → `AddRequested`
- Delete → `DeleteAsync` 被呼叫 + 重載
- 錯誤 → `ErrorOccurred` + error label

## Demo 端整合（過渡）

`apps/Bee.Northwind`（不在本 plan 的 src 範圍，但一起調整以驗收）：

- `FormsView` 左選單點選 → 開 **`ListView`** 分頁（取代目前直接開 `FormView`）
- `ListView.RecordActivated` / `AddRequested` → 開一個 **`FormView`** 分頁作為過渡編輯器（沿用現有去重邏輯：同 ProgId 已開則切換）
  - 過渡期 `FormView` 仍是「清單＋表單」合一，故編輯分頁會有冗餘清單 —— **明確標記為過渡**，下輪 `EditView`（可接 rowId 直開單筆）落地後移除

> 註：本輪 `FormView` 無「依 rowId 預載單筆」的 API，故過渡編輯分頁是合一視圖；不為過渡硬加 FormView API（待 EditView 一次做對）。

## 驗收

1. `src/Bee.UI.Avalonia` build 綠 + `ListViewTests` 全過
2. `./test.sh` 全綠（本輪動 src → CI 會建 src + tests）
3. demo：左選單 → ListView 清單分頁（read-only、工具列）；雙擊列 / New 開 FormView 編輯分頁；Delete/Refresh 正常
4. 既有 `Avalonia.Demo` / `Avalonia.Editors.Gallery` 不受影響（FormView 未改）

## 單筆 View 類別合併（2026-06-14 定案）

使用者 review 後指出單筆相關類別命名混亂。決議：`src/Bee.UI.Avalonia` 顯示「單筆資料」的四個類別 **`SingleFormBase` + `DynamicForm` + `RecordView` + 舊合一 `FormView`** 合併為**單一 `FormView`**（單筆＝主檔+明細，對應 BO `GetData` / `GetFormLayout`）。與 `ListView`（多筆，對應 `GetList` / `GetListLayout`）對稱。

- **命名**：單筆 View = `FormView`（與 FormSchema/FormLayout/FormDataObject/FormApiConnector 家族一致；`GetFormLayout`↔`GetListLayout`、`ListView`↔`FormView` 對稱）。**不用 `DataView`**（與 `System.Data.DataView` 撞名）、不用 `RecordView`（Form 家族外名詞）。
- **BO 方法不動**：`GetList` / `GetData` 保留（改名 blast radius 大、效益低；View 命名跟版面家族已一致）。
- **合併內容**：新 `FormView` = `RecordView` 的 CRUD/三態（View/Add/Edit，工具列 瀏覽→返回、新增/修改→儲存/取消）+ `DynamicForm` 的渲染（sections + 明細 grid，經 `FieldEditorFactory`）+ `SingleFormBase` 的 `FormMode`/FormScope 廣播。刪除 `SingleFormBase` / `DynamicForm` / `RecordView` / 舊合一 `FormView`。
- **切換仍 host 編排**：demo `FormWorkspace` 改用 `ListView` + 新 `FormView`。

### 影響面（執行範圍）

| 檔案 | 動作 |
|------|------|
| `src/Bee.UI.Avalonia/Controls/FormView.cs` | 重寫為合併後單筆 View |
| `…/Controls/{RecordView,SingleFormBase,DynamicForm}.cs` | 刪除 |
| `…/Controls/Editors/{LookupPanel,IFieldEditor}.cs` | 僅註解提及 DynamicForm/FormView，更新文字 |
| `tests/…/Controls/{FormViewTests,FormViewAdditionalTests,RecordViewTests,SingleFormBaseTests,DynamicFormTests}.cs` | 合併重寫為新 `FormViewTests`（+ 渲染覆蓋）|
| `tests/…/AvaloniaRegistryWarmup.cs` | 移除 DynamicForm/RecordView 預熱、改 `new FormView()` |
| `apps/Bee.Northwind/…/FormWorkspace.cs` | `RecordView` → `FormView` |
| `samples/Avalonia.Demo` | 舊用合一 `FormView`（3 tab）→ 改 ListView+FormView 切換 |
| `samples/Avalonia.Editors.Gallery` | `MainWindow` 用 `DynamicForm` demo EditForm → 改用 GridControl |

> Blazor / MAUI 的 `DynamicForm` 是各 UI 棧獨立型別，**不受影響**。
