# 計畫：Avalonia Field Editor 控件組 — ControlType 對應與定義層深度綁定

**狀態：🚧 進行中（2026-06-11）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | `FormDataObject` 變更通知事件 | ✅ 已完成（2026-06-11） |
| 2 | Field Editor 控件組 + `FormScope` ambient 綁定 | ✅ 已完成（2026-06-11） |
| 3 | `Avalonia.Editors.Gallery` 樣式比對 sample（Semi.Avalonia） | 📝 待做 |
| 4 | `DynamicForm` 重構改用控件組 | 📝 待做 |

## 背景

[FormLayout](../../src/Bee.Definition/Layouts/FormLayout.cs) 以 `LayoutField.ControlType`（`TextEdit` / `ButtonEdit` / `DateEdit` / `YearMonthEdit` / `DropDownEdit` / `MemoEdit` / `CheckEdit`，另有 `Auto`）定義表單欄位控件。目前 Avalonia 端的 [DynamicForm](../../src/Bee.UI.Avalonia/Controls/DynamicForm.cs) 以 switch-case 直接建構**原生控件**並手動掛事件寫回 `FormDataObject`：

- 接線邏輯（取值、寫回、ReadOnly、選項載入）散落在 `BuildInputControl` 各 case，無法重用
- `FormField` metadata（`MaxLength`、`DisplayFormat`、`NumberFormat`）大多未套用
- 手寫 axaml 表單無法重用這些行為，必須在 code-behind 重新接線
- 定義層既有的綁定契約 [IBindFieldControl](../../src/Bee.Definition/Layouts/IBindFieldControl.cs) / [IUIControl](../../src/Bee.Definition/Layouts/IUIControl.cs)（WinForms 時代）在本 repo 無任何實作

本計畫在 `Bee.UI.Avalonia` 建立一組**與 `ControlType` 一一對應、繼承原生 Avalonia 控件**的編輯器控件，實作定義層綁定介面，與 `FormLayout` / `FormSchema` metadata 深度綁定，簡化 UI 設計。

已確認的設計決策：

1. **命名與 `ControlType` 同名**（`TextEdit`、`DateEdit`…），以 namespace `Bee.UI.Avalonia.Controls.Editors` 區隔
2. **Ambient 作用域 + 顯式 Bind 雙軌**：attached inherited property 讓手寫 axaml 只在容器設一次 `DataObject`，子控件憑 `FieldName` 自動接線；`DynamicForm` 動態產生走顯式 `Bind`
3. **`FormDataObject` 加變更通知事件**，控件在 `LoadAsync` / `NewAsync` 後自動刷新，不再依賴整棵重建

## 目標使用體驗

```xml
<!-- 手寫 axaml：容器設一次 DataObject，子控件自動接線並套用 FormField metadata -->
<StackPanel ed:FormScope.DataObject="{Binding Data}">
    <ed:TextEdit FieldName="emp_name" />
    <ed:DateEdit FieldName="hire_date" />
    <ed:DropDownEdit FieldName="dept_id" />
    <ed:CheckEdit FieldName="is_active" />
</StackPanel>
```

```csharp
// DynamicForm（或使用端程式碼）：顯式綁定，帶入 LayoutField 版面屬性
var editor = FieldEditorFactory.Create(field.ControlType);
editor.Bind(dataObject, field);
```

---

## 階段 1：`FormDataObject` 變更通知事件

修改 [FormDataObject.cs](../../src/Bee.UI.Avalonia/DataObjects/FormDataObject.cs)：

1. 新增事件：
   - `event EventHandler<FieldValueChangedEventArgs>? FieldValueChanged` — `SetField` 實際寫入（通過 echo 防護）後觸發，攜帶 `FieldName` 與新值
   - `event EventHandler? DataSetReplaced` — `LoadAsync` / `SaveAsync` / `DeleteAsync` / `NewAsync` / `InitializeNewMaster` 置換或重置 `DataSet` 後觸發
2. 新增 `FieldValueChangedEventArgs`（`DataObjects/FieldValueChangedEventArgs.cs`）
3. 既有 echo 防護（`SetField` 先比對再寫）天然避免事件迴圈：值相同不寫入、不觸發

**測試**（`tests/Bee.UI.Avalonia.UnitTests/DataObjects/`）：`SetField` 觸發一次事件、相同值不觸發、`InitializeNewMaster` 觸發 `DataSetReplaced`。

## 階段 2：Field Editor 控件組

新增資料夾 `src/Bee.UI.Avalonia/Controls/Editors/`（namespace `Bee.UI.Avalonia.Controls.Editors`，符合 IDE0130 資料夾對映）。

### 2.1 控件清單（七個，皆含 `StyleKeyOverride` 指向原生基底）

| ControlType | 控件 | 繼承 | 專屬行為 |
|-------------|------|------|---------|
| `TextEdit` | `TextEdit` | `TextBox` | 套 `FormField.MaxLength`；`TextChanged` 寫回 |
| `MemoEdit` | `MemoEdit` | `TextEdit` | `AcceptsReturn` + `TextWrapping.Wrap` + `MinHeight=60` |
| `ButtonEdit` | `ButtonEdit` | `TextBox` | `InnerRightContent` 內嵌按鈕；公開 `ButtonClick` 事件（lookup 開窗預留掛載點） |
| `DateEdit` | `DateEdit` | `DatePicker` | ISO `yyyy-MM-dd` 寫回（沿用現行 DateTimeKind 處理） |
| `YearMonthEdit` | `YearMonthEdit` | `DateEdit` | `DayVisible=false`；寫回 `yyyy-MM` |
| `DropDownEdit` | `DropDownEdit` | `ComboBox` | 自動載入 `FormField.ListItems`；以 `ListItem.Value` 寫回 |
| `CheckEdit` | `CheckEdit` | `CheckBox` | `bool.TrueString` round-trip |

`ControlType.Auto` 不建控件：工廠 fallback 至 `TextEdit`（沿用現行 default case 行為）。

### 2.2 共用契約與基礎設施

各控件繼承基底不同，無法共用 base class，共用邏輯走「介面 + helper」：

- **`IFieldEditor`**（`Controls/Editors/IFieldEditor.cs`）：
  ```csharp
  public interface IFieldEditor : IBindFieldControl, IUIControl
  {
      void Bind(FormDataObject dataObject, LayoutField field);
      void Bind(FormDataObject dataObject, string fieldName);   // ambient 路徑用，無 LayoutField
      void Unbind();
  }
  ```
  實作定義層既有 `IBindFieldControl`（`FieldName` / `FieldValue`）與 `IUIControl`（`SetControlState(SingleFormMode)`：`View` → 唯讀/停用；`Add` / `Edit` → 依 `LayoutField.ReadOnly` 決定）。

- **`FieldEditorBinder`**（每控件持有一個 instance helper）封裝共用接線：
  - 初值拉取（`GetField`）與 metadata 套用（`MaxLength` / `ListItems` / `ReadOnly` / 日期格式）
  - 訂閱 `DataSetReplaced` → 重拉值；`FieldValueChanged`（同欄位、非自身發出）→ 重拉值，支援跨欄位聯動
  - 寫回 `SetField`，以 suppression flag 防自身 echo
  - `Unbind` 解除事件訂閱（控件離開視覺樹時由 `OnDetachedFromLogicalTree` 呼叫，避免洩漏）

- **`FormScope`**（`Controls/Editors/FormScope.cs`）attached inherited properties：
  - `FormScope.DataObject`（`FormDataObject?`，`Inherits=true`）— 控件 attach 到 logical tree 或屬性變更時，若有 `FieldName` 且尚未顯式綁定，自動 `Bind(ambient, FieldName)`
  - `FormScope.FormMode`（`SingleFormMode`，`Inherits=true`，預設 `Edit`）— 變更時對作用域內編輯器呼叫 `SetControlState`

- **`FieldEditorFactory`**（`Controls/Editors/FieldEditorFactory.cs`）：`ControlType` → 對應編輯器實例。

**測試**（`tests/Bee.UI.Avalonia.UnitTests/Controls/Editors/`，沿用既有純 xUnit 直接建構模式）：
- 各控件 `StyleKeyOverride` 指向正確基底（reflection 驗證，防「隱形控件」回歸）
- `Bind` 後初值正確、控件變更寫回 `FormDataObject`、`FieldValueChanged` 觸發刷新
- `DropDownEdit` 載入 `ListItems`；`TextEdit` 套 `MaxLength`
- `SetControlState` 三種 mode 的啟用狀態
- `FieldEditorFactory` 的 `ControlType` 對應完整性（含 `Auto` fallback）

## 階段 3：`Avalonia.Editors.Gallery` 樣式比對 sample

繼承控件的樣式正確性（`StyleKeyOverride` 是否生效、與原生控件視覺是否一致）無法用單元測試把關，需要可視化比對工具。新增獨立 sample（依 `bee-sample-add` skill 流程建檔）：

- **路徑**：`samples/Avalonia.Editors.Gallery/`
- **主題**：**Semi.Avalonia 12.0.3**（對齊 DefineEditor 實際使用情境；Avalonia.Demo 維持 Fluent 與既有定位不動）
- **csproj**：比照 `tools/DefineEditor`（Avalonia 12.0.4 + Avalonia.Desktop + Semi.Avalonia + Fonts.Inter，Debug 跑）
- **內容**：
  - 每個 `ControlType` 一列，左右並排「**原生控件** vs **繼承控件**」，各含 normal / 帶值 / disabled / readonly 四種狀態
  - 標題列放 Light/Dark 切換（`RequestedThemeVariant`），深淺色都驗
  - 繼承控件側以 in-memory `FormSchema` + `FormDataObject`（`InitializeNewMaster`，不需後端）透過 `FormScope.DataObject` ambient 綁定驅動——**同時兼作 ambient 綁定模式的使用範例**
  - 底部顯示目前 `FormDataObject` 欄位值，目視驗證寫回
- 加入 `Bee.Samples.slnx`；附 README（跑法 + 比對 checklist）
- `samples/` 不觸發 build-ci.yml，無 CI 影響

## 階段 4：`DynamicForm` 重構

- `BuildInputControl` switch-case 移除，改為 `FieldEditorFactory.Create(field.ControlType)` + `editor.Bind(DataObject, field)`
- `BuildFieldCell` 的 caption `TextBlock` 維持容器職責（編輯器不含 label）
- `Refresh()` 保留（API 相容），但 `DataSet` 置換後的值刷新已由事件驅動，重建只剩版面結構變更時需要
- 更新 `tests/Bee.UI.Avalonia.UnitTests/Controls/DynamicFormTests.cs`：dispatch 斷言改驗證回傳的編輯器型別

## 不在本次範圍

- `DynamicGrid` / `LayoutColumn`（`IBindTableControl`）的對應控件化 — 另案
- `ButtonEdit` 的 lookup 開窗流程（`LookupProgId` / `FieldMappings`）— 只預留 `ButtonClick` 事件
- `DisplayFormat` / `NumberFormat` 的完整格式化引擎 — 第一版僅套日期格式
- MAUI / Blazor 端的同等重構 — 待 Avalonia 端模式驗證後再鏡像

## 驗證

1. `dotnet build src/Bee.UI.Avalonia/Bee.UI.Avalonia.csproj --configuration Release`
2. `./test.sh tests/Bee.UI.Avalonia.UnitTests/Bee.UI.Avalonia.UnitTests.csproj`（純邏輯測試，無 DB 依賴）
3. **樣式一致性**：`samples/Avalonia.Editors.Gallery` 以 Debug 跑，逐列比對原生 vs 繼承控件在 Light/Dark 下的視覺（編譯過即交付，視覺比對由使用者自測）
4. `samples/Avalonia.Demo` 以 Debug 跑 `FormView` 冒煙：欄位渲染、輸入寫回、Load 後自動刷新
5. push main 後 `build-ci.yml` 全套驗證

## 工作流

桌面環境可本機驗證 → 依階段直接 commit `main`，每階段本機 build + test 通過後 push。
