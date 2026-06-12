# 計畫：Avalonia Lookup 開窗機制（ButtonEdit 完整管線）

**狀態：🚧 進行中（2026-06-12）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 定義層：`DisplayField`、`LookupFields` 屬性 + `ControlType.Auto` 對 Relation 欄位解析為 ButtonEdit | ✅ 已完成（2026-06-12） |
| 2 | API/BO 層：`GetLookup` 專用方法（wire action + BO + Client connector，含 BO 業務過濾覆寫點） | ✅ 已完成（2026-06-12） |
| 3 | `LookupDialog` 元件：以 `GetLookupAsync` 取數、搜尋、分頁、單選回傳 | ✅ 已完成（2026-06-12） |
| 4 | 主表欄位接線：ButtonClick → 開窗 → mapping 寫回 FormDataObject（含顯示值與清空） | ✅ 已完成（2026-06-12） |
| 5 | 明細 grid EditForm 模式：RowEditDialog / RowEditPanel 內的 lookup | ✅ 已完成（2026-06-12，UI 動線待使用者自測） |
| 6 | 明細 grid InCell 模式：ButtonEdit cell（ADR-021 click-to-swap 管線） | ✅ 已完成（2026-06-12，UI 動線待使用者自測） |
| 7 | 測試補齊 + samples 接線（Project lookup 範例進 samples/Define） | 📝 待做 |

> **本 plan 是 [plan-mini-erp-demo.md](plan-mini-erp-demo.md) 的前置依賴**：迷你進銷存的交易單據（訂單選客戶/商品）需要完整 lookup 管線。

## 背景

迷你進銷存 demo 規劃時盤點發現：lookup 機制只有定義層與後端載入路徑就緒，Avalonia 前端缺整條互動管線，且取數面缺一個適合開窗的專用 API。

**已就緒（不需動）**：

- 定義層：`FormField.RelationProgId` + `RelationFieldMappings`（資料關聯）、`LookupProgId` + `LookupFieldMappings`（開窗目標與寫回映射）— [FormField.cs:148](../../src/Bee.Definition/Forms/FormField.cs)
- Server 端：`SelectContextBuilder` 已在 SELECT 以 JOIN 補 `ref_*` 關聯欄位，載入路徑完整
- 完整定義用例：`tests/Define/FormSchema/Project.FormSchema.xml`（Department / Employee 兩組 relation + 多欄位 mapping）
- 清單基建：`GridControl.Bind(listLayout, rows)`（FormView 清單頁同款，開窗 grid 重用）
- `FilterNode` / `SortFieldCollection` / `PagingOptions` 查詢基建（`GetList` 管線既有，`GetLookup` 內部沿用）

**缺口（本 plan 範圍）**：

- `ButtonEdit` 僅有外觀與 `ButtonClick` 事件，無人訂閱（註解明言 lookup flow 是 caller 責任）— [ButtonEdit.cs](../../src/Bee.UI.Avalonia/Controls/Editors/ButtonEdit.cs)
- 無開窗選擇元件、無 mapping 寫回邏輯、無顯示值處理（直綁 FieldName 會顯示 Guid 字串）
- 取數面只有 `GetList`（管理清單用途，欄位多、權限軸與「選取參照」不同），無開窗專用方法
- `Bee.UI.Avalonia` 與 `Bee.UI.Core` 全域 grep `Lookup` 零命中

## 設計決策（已與使用者確認）

1. **顯示值**：`FormField` 新增 `DisplayField` 屬性（與 `SourceField` / `DestinationField` / `ListFields` 的欄位參照命名同族），明確指定 ButtonEdit 顯示哪個本地欄位（如 `ref_owner_dept_name`）；`FormLayoutGenerator` 在未指定時以慣例補值 —— 取 `RelationFieldMappings` 中 `SourceField == sys_name` 的 `DestinationField`。
2. **InCell 一併做**：明細逐格編輯選商品是 ERP 使用者的自然期待，InCell ButtonEdit cell 納入本 plan（階段 6），走 ADR-021 click-to-swap 編輯管線。
3. **開窗取數走專用方法 `GetLookup`**，不共用 `GetList`。理由：
   - **權限軸分離**：開單據的使用者可能沒有目標主檔的清單查詢權限，但需要能 lookup 選取；獨立 action 才能分開授權
   - **欄位曝險由 server 控**：`GetList` + selectFields 是 client 決定欄位，lookup 可能被用來越權取敏感欄位（信用額度、議價）；專用方法由目標表單定義宣告曝險集
   - **BO 業務過濾覆寫點**：lookup 常要「只列有效客戶 / 可售商品」，這類過濾不應污染主檔管理清單的 `GetList`
   - **搜尋邏輯收斂 server**：`GetLookup(searchText, paging)` 讓 FilterNode 組裝留在 server，各前端不重複實作
4. **回傳欄位集**：`FormSchema` 新增 `LookupFields`（逗號分隔，同 `ListFields` 風格）；未設定時預設取 `sys_rowid, sys_id, sys_name`（主檔慣例識別欄位，固定可預測；欄位不存在於 schema 時略過）；回應一律附 `sys_rowid`。**契約**：呼叫端 mapping 的 `SourceField` 必須 ⊆ 目標表單的 lookup 欄位集（未來可在定義載入時跨 schema 驗證）。

## 行為規格

### GetLookup API

- 新增 `FormActions.GetLookup`；`GetLookupRequest { SearchText, Paging }`、`GetLookupResponse { Table }`
- Server 流程：解析欄位集（`LookupFields` → 未設定預設 `sys_id, sys_name`，一律 prepend `sys_rowid`）→ `SearchText` 對字串型 lookup 欄位組 LIKE/OR `FilterNode` → 套 BO 業務過濾 → 走既有 `GetList` repository 管線取數（不需新 repository 方法）
- BO 覆寫點：`FormBusinessObject` 提供 virtual hook（如 `GetLookupFilter()`）讓 domain BO 附加業務條件（只列有效、可售等）
- 未帶 `Paging` 時 server 套預設分頁上限，防大表全載
- 跨層新增依 `bee-add-bo-method` 流程走 contract / wire / BO / Client connector + 兩層 round-trip 測試

### 開窗目標與資料

- 目標 ProgId：`LookupProgId` 優先，未設定 fallback `RelationProgId`
- 開窗 grid 欄位：依 lookup 欄位集 + 目標 FormSchema 的 `Caption` 產生（`sys_rowid` 不顯示）
- 搜尋框：`SearchText` 送 server，過濾邏輯不在 client 組

### 選取寫回

- 寫 `sys_rowid` 至 lookup 欄位本身（`FieldName`，Guid）
- mapping 寫回：`LookupFieldMappings` 優先，未設定 fallback `RelationFieldMappings`；逐筆把選取列的 `SourceField` 值寫入本地 `DestinationField`
- 寫回走 `FormDataObject`（觸發 dirty 與 UI 同步），不直接操作控件 —— 控件事件對程式設值不觸發，須走 property-changed 管線（既有雷區）
- client 端寫回的 `ref_*` 值僅供即時顯示；存檔後重新載入仍以 server JOIN 結果為準（單一真相在 server）

### 顯示與清空

- ButtonEdit 文字框顯示 `DisplayField` 欄位值、**唯讀**（不可手動輸入，v1 不做「鍵入代碼直接解析」，列入未來延伸）
- 清空互動：提供清除途徑（具體形式於階段 4 定案，傾向 clear icon 或 Delete 鍵），清空時 rowid 設 `Guid.Empty`、mapping 的 `DestinationField` 一併清空
- View 模式 / read-only layout 下按鈕已自動停用（ButtonEdit 既有行為，沿用）

## 階段內容

### 階段 1：定義層與 Auto 解析

- `FormField.DisplayField` 屬性、`FormSchema.LookupFields` 屬性（XmlAttribute、三棲序列化、Clone 同步）
- `FormLayoutGenerator`：`ControlType.Auto` 且 `RelationProgId` 非空 → `ButtonEdit`；`DisplayField` 未設時依慣例補
- 單元測試：generator 解析、序列化 round-trip
- **驗收**：Project.FormSchema 經 generator 產出的 layout，relation 欄位為 ButtonEdit 且帶正確顯示欄位

### 階段 2：GetLookup 方法（API/BO 層）

- 依 `bee-add-bo-method` 流程跨層新增：`FormActions.GetLookup`、Request/Response、`FormBusinessObject.GetLookup`（欄位集解析 + SearchText 過濾 + 預設分頁）、`FormApiConnector.GetLookupAsync`
- BO 業務過濾 virtual hook + 單元測試（含 `LookupFields` 未設定的預設欄位集、`sys_rowid` prepend、SearchText FilterNode 組裝）
- **驗收**：兩層 round-trip 測試綠；以 Project → Department 案例驗證回傳欄位集正確且不含未宣告欄位

### 階段 3：LookupDialog 元件

- `Bee.UI.Avalonia` 新增 lookup 開窗（Window 或 overlay dialog，與 RowEditDialog 風格一致）
- 內容：搜尋框 + GridControl 清單 + 確定/取消；雙擊列 = 選取確定
- 資料管線：解析目標 ProgId → 取 FormSchema（經 SystemApiConnector，含 client 快取）→ `GetLookupAsync(searchText, paging)`
- 回傳：選取列的完整 DataRow（含 lookup 欄位集全部欄位）
- **驗收**：元件可獨立以 ProgId 開窗、搜尋、回傳選取列

### 階段 4：主表欄位接線

- `DynamicForm` / `FieldEditorBinder`：ButtonEdit 建立時若欄位帶 RelationProgId，訂閱 `ButtonClick` → 開 LookupDialog → 寫回
- 顯示值綁定：text 綁 `DisplayField` 欄位、value 為 `FieldName`（binder 雙欄位綁定）
- 清空互動實作
- **驗收**：Project 表單（Department / Employee lookup）在 Avalonia 端可開窗選取、ref 欄位即時帶出、存檔 round-trip 正確

### 階段 5：Grid EditForm 模式

- RowEditPanel / RowEditDialog 重用階段 4 的接線（editors 來自同一 factory，理論上接近免費，需處理 dialog 疊 dialog 的 owner/focus）
- **驗收**：明細列以彈窗編輯時 lookup 欄位可開窗選取

### 階段 6：Grid InCell 模式

- ButtonEdit 作為 cell editor 進 InCell 編輯管線（ADR-021 click-to-swap；DataGrid 編輯管線與 popup 編輯器互動是已知雷區，預留打磨時間）
- 顯示 cell 呈現 `DisplayField` 值；編輯態進 ButtonEdit、開窗選取後 commit 寫回該列
- **驗收**：明細 grid 逐格點擊商品欄 → 開窗 → 選取 → 該列 mapping 欄位同步更新

### 階段 7：測試補齊 + samples 接線

- `samples/Define` 加入 Department + Project（或等價）lookup 範例（目前 samples 無任何 lookup 用例），含 `LookupFields` 宣告示範
- Avalonia.Demo 加一頁展示 lookup 表單；編譯通過後交使用者自測 UI（依既有協作慣例）
- **驗收**：`./test.sh` 全綠；Avalonia.Demo 可手動走通 lookup 動線

## 風險與雷區

| 風險 | 緩解 |
|------|------|
| InCell ButtonEdit 與 DataGrid 編輯管線衝突（popup 編輯器雷區） | 走 ADR-021 click-to-swap 既有路徑；階段 6 獨立、不阻塞 1–5 交付 |
| 跨執行緒 UI 存取（async 取數後更新控件） | UI 路徑不用 `ConfigureAwait(false)`（FormDataObject CRUD 已有前例修正 a36be26e） |
| 程式設值不觸發控件事件 | 寫回一律走 FormDataObject property-changed 管線，不直接 set 控件 |
| mapping `SourceField` 不在目標 lookup 欄位集 → 寫回拿不到值 | `GetLookup` 回應缺欄位時寫回端拋明確錯誤（不沉默跳過）；定義載入期跨 schema 驗證列未來延伸 |
| LookupDialog 大表全載 | server 端預設分頁上限；搜尋走 server-side FilterNode |
| `DisplayField` 慣例補值猜錯（mapping 無 sys_name） | 慣例落空時 fallback 顯示空字串並於 generator 出 debug log；定義端可顯式指定 |

## 未來延伸（不在本 plan）

- ButtonEdit 鍵入代碼直接解析（輸入 `A001` 自動帶出，免開窗）
- 呼叫端情境過濾（如明細選商品時依主表倉庫帶 `FilterNode` 條件）
- 定義載入期跨 schema 驗證：mapping `SourceField` ⊆ 目標表單 lookup 欄位集
- 多選 lookup（一次選多列產生多筆明細）
- 最近使用 / 常用項目快捷
- `GetLookup` 結果的 client 端快取（單位、類別等低變動主檔）
