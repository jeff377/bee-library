# 計畫：FormDataObject 事件橋接 — DataTable 變更統一發布 FieldValueChanged

**狀態：✅ 已完成（2026-06-11）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 事件橋接層 + args 擴充（`FormDataObject` 本體） | ✅ 已完成（2026-06-11） |
| 2 | 訂閱端收斂（`FieldEditorBinder` / `GridControl`）+ 拆除手動 dirty | ✅ 已完成（2026-06-11，與階段 1 同 commit——MarkDirty 移除使兩階段編譯耦合） |

## 背景

`FormDataObject` 的定位是 MVVM 的 ViewModel，主檔與明細的欄位異動都應該對 UI 發布 `FieldValueChanged`。現況的缺口與成因：

- **明細沒有事件**：`GridControl` 的 in-cell 編輯直接寫 `DataRow` + 手動 `MarkDirty()`，不會引發 `FieldValueChanged`
- **成因是模式問題，不是漏寫**：事件目前由「寫入者」引發（`SetField` 手動 raise），意味著每個寫入路徑都要記得引發——未來 lookup 帶回多欄、BO 規則直接改 `DataRow`，都會是同類缺口

評估後採**做法 B：DataTable 事件橋接**（兩做法的完整比較見對話記錄，結論摘要）：

- `DataSet` 是 model 的單一事實來源；ADO.NET `DataTable` 本身就有 `ColumnChanged` / `RowChanged` / `RowDeleted` 事件，**任何**寫入路徑都會觸發，不存在「忘記呼叫」的 bug 類別
- 把「引發事件」的責任從 N 個寫入者收斂到 1 個橋接層，`SetField` 與 `GridControl` 反而瘦身
- dirty 追蹤同步統一由橋接層判定，`MarkDirty()` 公開 API 可移除（本 session 新增、尚未隨版發布，無相容性負擔）

## 階段 1：事件橋接層

修改 [FormDataObject.cs](../../src/Bee.UI.Avalonia/DataObjects/FormDataObject.cs)：

1. **`ReplaceDataSet(DataSet)` 私有方法**：`DataSet` 的所有指派點（ctor、`LoadAsync`、`SaveAsync`、`DeleteAsync`、`NewAsync`）收斂到這裡——退訂舊表事件 → 指派 → 訂閱新表事件 → raise `DataSetReplaced`。訂閱在資料填充完成之後，載入過程不發事件
2. **橋接 handlers**：
   - `ColumnChanged` → raise `FieldValueChanged(TableName, FieldName, FormatForBinding(e.ProposedValue), e.Row)` + `IsDirty = true`；**`e.Row.RowState == Detached` 時跳過**（`AddRow` 的 NOT NULL 種子寫入發生在 attach 前，保持安靜）
   - `RowChanged`：`e.Action == Add` → `IsDirty = true`；`Commit`（AcceptChanges）等框架動作不弄髒
   - `RowDeleted` → `IsDirty = true`
3. **`SetField` 瘦身**：保留 compare-first 防護（擋同值寫入的事件雜訊）與型別轉換，移除手動 raise 與 `IsDirty = true`（橋接代勞）
4. **移除 `MarkDirty()`**：職責由橋接層接手
5. **[FieldValueChangedEventArgs](../../src/Bee.UI.Avalonia/DataObjects/FieldValueChangedEventArgs.cs) 擴充**：`(FieldName, Value)` → `(TableName, FieldName, Value, Row)`；`Row` 供明細訂閱者定位列，主檔訂閱者忽略
6. `InitializeNewMaster` 等「會觸發橋接但語意上不算髒」的方法，維持最後 `IsDirty = false` 的順序即可，不需特殊處理

**測試**（`FormDataObjectTests` 擴充）：
- `SetField` 仍 raise 一次、同值不 raise（行為不變，來源換橋接）
- **明細 `DataRow` 直接寫入 raise `FieldValueChanged`**（含 TableName / Row）並標 dirty——本計畫的核心驗收
- detached row 寫入不 raise（順便釘住 ADO.NET 的 detached 行為——若實測 detached 本來就不觸發 `ColumnChanged`，guard 屬防禦性，測試照寫）
- `Rows.Add` / `Row.Delete()` 標 dirty；`AcceptChanges` 不標
- `NewAsync` 置換 `DataSet` 後，**新表的寫入仍會 raise**（重訂閱驗證）、舊表寫入不再 raise（退訂驗證）

## 階段 2：訂閱端收斂

1. **[FieldEditorBinder.cs](../../src/Bee.UI.Avalonia/Controls/Editors/FieldEditorBinder.cs)**：`OnFieldValueChanged` 過濾條件從「FieldName 相符」改為「`TableName == 主表` 且 FieldName 相符」（field editors 綁的是 master row）；echo suppression 機制不變
2. **[GridControl.cs](../../src/Bee.UI.Avalonia/Controls/Editors/GridControl.cs)**：拆除 `WriteCell` / `AddRow` / `DeleteSelectedRow` 內的 `MarkDirty()` 呼叫（寫入即發布）；`WriteCell` 的 compare-first 保留
3. 編輯器 / 測試中所有 `FieldValueChangedEventArgs` 建構處同步改簽章

**明確不做（記入計畫避免 scope creep）**：

- `GridControl` 訂閱同表 `FieldValueChanged` 去刷新已 realize 的明細 cell（外部寫入的畫面同步）——需要與 in-cell 編輯的 suppression 協調（否則每次按鍵會觸發列重 realize、撕掉編輯中的控件），與列級編輯面板（plan-avalonia-grid-row-edit-panel）一併設計
- Blazor / MAUI 家族的同等橋接——待 Avalonia 端驗證後鏡像

## 相容性

`FieldValueChangedEventArgs` 簽章變更與 `MarkDirty()` 移除均為破壞性，但兩者皆本 session 新增、尚未隨任何 NuGet 版本發布（上一版 tag 之後才進 main），無外部呼叫端，直接改不留過渡。

## 驗證

1. `dotnet build src/Bee.UI.Avalonia/Bee.UI.Avalonia.csproj --configuration Release`
2. `./test.sh tests/Bee.UI.Avalonia.UnitTests/Bee.UI.Avalonia.UnitTests.csproj`
3. Gallery 冒煙：主檔 field editor 互打（同欄位雙控件聯動）、明細 in-cell 編輯後底部 dirty 行為、`DataSetReplaced` 後編輯器刷新——行為應與現狀完全一致（本計畫是內部重構，UI 行為不變）
4. push main 後 `build-ci.yml` 全套驗證

## 工作流

桌面環境本機可驗證 → 依階段直接 commit `main`，每階段本機 build + test 通過後 push。
