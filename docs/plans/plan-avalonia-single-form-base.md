# 計畫：資料表單基底類別 SingleFormBase（FormMode 擁有者與廣播）

**狀態：✅ 已完成（2026-06-12）**

> 本計畫為 [plan-layout-form-mode-editability.md](plan-layout-form-mode-editability.md) 的**第二個前置作業**
> （第一個為 [plan-avalonia-gridcontrol-toolbar.md](plan-avalonia-gridcontrol-toolbar.md)，已完成）。
> 落地後 formMode 計畫的階段 2/3 測試可直接走「表單切模式 → ambient 廣播 → 子樹控件切換」的真實管線。

## 背景

資料表單有三種模式（View 瀏覽 / Add 新增 / Edit 修改），表單切換模式時要通知表單內所有控制項
切換為合適狀態。Avalonia 端目前是「有廣播通道、沒有廣播者」：

1. `FormScope.FormModeProperty`（ambient attached property）與各控件的 class handler 都就緒，
   但 repo 內**沒有任何地方設定它**——FormMode 的生命週期（載入→瀏覽、New→新增、Save 成功→瀏覽）
   是表單層級的狀態機，目前無處安放。
2. `FormView` 載入資料後永遠可編輯（吃 ambient 預設值 `Edit`），沒有瀏覽模式，
   不符合 ERP 資料表單慣例。
3. 測試全部直接呼叫各控件的 `SetControlState`，ambient 廣播管線零覆蓋。

依架構約定：**只有單筆資料的基底表單才有 FormMode 屬性**，其他表單（列表等）沒有。

## 設計

### `SingleFormBase`（src/Bee.UI.Avalonia/Controls/SingleFormBase.cs）

```csharp
public abstract class SingleFormBase : UserControl
{
    public static readonly StyledProperty<SingleFormMode> FormModeProperty =
        AvaloniaProperty.Register<SingleFormBase, SingleFormMode>(
            nameof(FormMode), SingleFormMode.View);   // 資料表單初始為瀏覽

    static SingleFormBase()
    {
        FormModeProperty.Changed.AddClassHandler<SingleFormBase>((o, e) =>
        {
            var mode = (SingleFormMode)e.NewValue!;
            FormScope.SetFormMode(o, mode);
            o.OnFormModeChanged(mode);
        });
    }

    protected SingleFormBase()
    {
        // FormScope 的 ambient 預設為 Edit（讓無表單 scope 的獨立編輯器可編輯）；
        // 資料表單擁有模式，建構時即把 scope 釘到初始 View，子樹一掛上就是正確狀態。
        FormScope.SetFormMode(this, FormMode);
    }

    public SingleFormMode FormMode { get; set; }   // styled accessor

    protected virtual void OnFormModeChanged(SingleFormMode formMode) { }
}
```

職責刻意維持最小：**持有 FormMode、轉發到 `FormScope`、提供 virtual hook**。
模式轉換的時機（何時進 Add、何時回 View）綁定 CRUD 流程，屬 derived form 的職責。

### `FormView` 改繼承 + 模式切換

`FormView : SingleFormBase`，引入完整模式生命週期：

| 動作 | 轉換 |
|------|------|
| 初始 / `InitializeAsync` | `View`（基底預設） |
| 選列載入成功（`OnRowSelectedAsync`） | → `View`（唯讀瀏覽） |
| 工具列 **Edit 鈕**（新增） | → `Edit`（master 已載入且目前為 View 才可按） |
| New 成功 | → `Add` |
| Save 成功 | → `View` |
| Delete 成功 | → `View` |
| Save / Load 失敗 | 模式不變（停在原模式） |

- 轉換寫在各 CRUD handler 的成功路徑內（`RunGuardedAsync` 的 lambda 中、await 之後），
  失敗丟例外時不會執行到。
- 工具列 enable 規則隨模式：
  - New：`initialized && !busy`
  - Edit：`initialized && !busy && hasMaster && FormMode == View`
  - Save：`initialized && !busy && hasMaster && FormMode != View`
  - Delete：`initialized && !busy && hasMaster && FormMode == View`
- `OnFormModeChanged` override → `UpdateToolbarState()`。
- **行為升級**：FormView 過去載入即可編輯；改版後載入進瀏覽、按 Edit 才進編輯。
- 列表 `GridControl` 也在 FormView 子樹內會收到廣播，但 list-mode 綁定（無 `FormDataObject`）
  的防護讓它任何模式都唯讀、工具列隱藏——前置 plan 已驗證。

### 後續（不在本次範圍）

- Edit/Add 模式的 **Cancel**（放棄編輯回 View + rollback）——需要 FormDataObject 的取消語意配合，另案。
- Edit 模式中切換選列的 dirty 確認（目前直接重載）。
- MAUI / Blazor 端對等的基底類別。

## 測試

### `SingleFormBaseTests`（新檔）——ambient 廣播管線

以 derived test form 驗證真實管線（property 繼承 + class handler），而非逐控件呼叫 `SetControlState`：

- 預設 `View`：掛入子樹的綁定編輯器 `IsReadOnly == true`、detail 綁定 GridControl `AllowEdit == false`
  （對照：同一編輯器在無表單 scope 下吃 ambient 預設 `Edit` 可編輯）。
- `form.FormMode = Edit` → 子樹編輯器可編輯、GridControl `AllowEdit == true`。
- 切回 `View` → 全部回唯讀。
- `OnFormModeChanged` hook 被呼叫。

### `FormViewTests` 更新

- 型別測試：`FormView` 為 `SingleFormBase` 子類。
- `InitializeAsync` 後 `FormMode == View`。
- 選列載入 → `View`；`OnNewClickedAsync` → `Add`；Save 成功 → `View`；Save 失敗 → 停在 `Add`。

## 執行順序

1. `SingleFormBase` 基底類別。
2. `FormView` 改繼承 + Edit 鈕 + 模式轉換 + 工具列規則。
3. `SingleFormBaseTests` 新增、`FormViewTests` 更新。
4. Release build 0 警告 + 全測試通過 → commit main。
