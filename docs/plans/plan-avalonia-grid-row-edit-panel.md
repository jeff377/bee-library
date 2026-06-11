# 計畫：GridControl 列級編輯面板（進階編輯模式）

**狀態：📝 擬定中**

## 背景

[ADR-021](../adr/adr-021-avalonia-datagrid-editing-strategy.md) 確立 `GridControl` 的 in-cell 編輯採混合策略（文字欄走編輯管線、popup 型欄位常駐編輯器）。in-cell 適合「快速逐格修改」，但對欄位多、有驗證需求、或需要完整輸入體驗的明細表，**列級編輯面板**是更合適的模式：grid 維持唯讀，選列後在 grid 下方（或對話框）以 field editor 控件組編輯整列。

使用者已確認此模式作為進階選項另案實作，與 in-cell 模式並存、由 layout 設定選擇。

## 設計方向（待細化）

1. **編輯模式設定**：`LayoutGrid` 增加編輯模式設定——優先評估用既有 `ExtendedProperties` 承載（不動定義層 schema），確有需要再升級為正式屬性（如 `EditMode: InCell | RowPanel`，預設 `InCell`）。新增正式屬性涉及 `Bee.Definition` 的序列化面，需同步 DefineEditor 的編輯 UI
2. **RowEditPanel 控件**：選列後出現於 grid 下方的編輯區——本質是「單列版 `DynamicForm`」：以 `LayoutColumn` 集合驅動 field editor 產生（重用 `FieldEditorFactory` / `FieldEditorBinder`），綁定目標從 master row 換成明細列
   - 需要 binder 支援「綁到指定 `DataRow`」而非僅 master row（`FieldEditorBinder` 擴充或包一層 row-scoped data context）
3. **操作流**：選列 → 面板載入該列值 → 編輯（含驗證）→ 確認寫回 `DataRow` + `MarkDirty` / 取消還原；新增列 = `AddRow` + 面板開啟空列
4. **`DynamicForm` 整合**：`BuildDetailSection` 依編輯模式決定 grid 可編輯性與是否掛面板
5. **Gallery**：新增 RowPanel 模式比對區

## 不在範圍

- in-cell 模式的行為變更（ADR-021 已定）
- Blazor / MAUI 端的對應模式

## 前置相依

- plan-avalonia-grid-control ✅（GridControl 本體）
- plan-avalonia-field-editors ✅（field editor 控件組）
