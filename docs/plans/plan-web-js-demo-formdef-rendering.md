# 計畫：Web.Js.Demo 加 FormDefinition-driven 渲染區塊

**狀態：✅ 已完成（2026-05-26）**

## 背景

[plan-jsonrpc-formschema-formlayout.md](plan-jsonrpc-formschema-formlayout.md)
落地後，JS 前端已能透過 `System.GetFormSchema` + `System.GetFormLayout` 拿到
schema-driven 渲染所需的完整 JSON。本 plan 在 `Web.Js.Demo` 加一個區塊，
驗證「拿 FormDefinition → 動態渲染 Employee 表單 → 串 GetData / Save / Delete」
的端到端流程。

驗證目的：
1. 證明 JS 端真的能用拿到的 layout JSON 渲染出可操作表單，不是紙上談兵
2. 把 GetFormSchema / GetFormLayout 在實務上的整合 pattern 留成 reference
3. 給未來的 JS 框架整合（React / Vue / Angular）開發者一份「最小可運作」起點

## 設計選擇

### 維持 Web.Js.Demo 核心原則

- 純 ES modules，無 npm / build step
- 控制項用原生 HTML elements（`<input>` / `<textarea>` / `<select>` / `<input type=checkbox>`）
- 排版用 CSS Grid（`grid-row: span N` / `grid-column: span N` 直接對映
  `LayoutField.rowSpan` / `columnSpan`）
- 樣式延用既有 vanilla CSS

### 範圍：master + 一個 detail grid 渲染 + 三種 CRUD 操作

**做：**
- Master section（一個或多個）渲染為 CSS Grid form
- Detail（如 EmployeePhone）渲染為簡易 table
- Load FormDefinition（GetFormSchema + GetFormLayout 並行）
- New（GetNewData → 灌入 form）
- Load by RowId（GetData → 灌入 form）
- Save（收回 form 變更 → 標 RowState → 呼叫 `Employee.Save`）

**不做：**
- 客戶端驗證規則引擎（依賴 FormSchema 內的 validation rules，另一塊大題目；
  本 plan 把驗證留給 server，Save 失敗時拿 RpcError 顯示 message）
- relations / lookup（FormField.RelationProgId / LookupProgId 在 schema 內，
  但實作 lookup 涉及多 progId 查詢，超出本 plan）
- Detail row 的編輯（detail 僅唯讀渲染為 table 證明結構通；新增 / 修改 detail
  row 留待後續）
- 排版響應式 fallback（窄螢幕折行等細節）

## 變更清單

### 1. 新增 `samples/Web.Js.Demo/form-renderer.js`

新 ES module，與 `bee-api-client.js` 同層級。匯出：

- `renderFormLayout(layout, container)` — 主入口，把 `FormLayout` JSON
  渲染到指定 `<div>` 內，回傳一個 controller 物件給外部呼叫
- 內部 helper：`renderSection` / `renderField` / `renderDetailGrid`
- ControlType → HTML element factory：
  - `TextEdit` → `<input type=text>`
  - `DateEdit` → `<input type=date>`
  - `YearMonthEdit` → `<input type=month>`
  - `CheckEdit` → `<input type=checkbox>`
  - `MemoEdit` → `<textarea>`
  - `DropDownEdit` → `<select>`（dropdown 選項本 plan 不串資料源，先 placeholder）
  - `ButtonEdit` → `<input type=text>` + `<button>`（按鈕本 plan 無功能）
- `bindDataSetToForm(controller, dataSet)` — 把 GetData / GetNewData 的
  `DataSet` 灌進 form
- `collectFormIntoDataSet(controller)` — 把 form 變更收回，根據與原值比對
  決定 RowState（unchanged → Unchanged、有改 → Modified、新增 row → Added）

### 2. `samples/Web.Js.Demo/index.html` 加區塊

在既有「4. Employee CRUD」與「5. Logout」之間插入：

**「6. FormDefinition-driven 渲染（端到端 demo）」** 區塊：
- 「Load Form Definition (Employee)」按鈕：併行呼叫 GetFormSchema + GetFormLayout，
  渲染 Employee form
- form 容器 `<div id="rendered-form">`（空狀態 placeholder「(尚未載入)」）
- 「New (Empty)」按鈕：GetNewData → bindDataSetToForm
- 「Load by RowId」按鈕：用上方 RowId 輸入呼叫 GetData → bindDataSetToForm
- 「Save Form」按鈕：collectFormIntoDataSet → Save → 結果區顯示 affectedRows

Logout 區段順移為「7.」。

### 3. `samples/Web.Js.Demo/app.js` 加事件綁定

延用既有的 `run(label, fn)` helper + RpcError 處理 pattern；新增的事件
handler 與既有 CRUD 同風格。

### 4. README 中英版加說明

「FormDefinition-driven 渲染」段落：簡述設計、提到「不做客戶端驗證」決策、
給「想 port 到 React / Vue 怎麼起手」一個指引。

### 5. `.smoke.yaml` 加期望文字

`flow` 的 `expect_text` 加區塊標題字串（如 `"FormDefinition"` / `"Load Form Definition"`），
確認新區塊有渲染（瀏覽器 tier "read" 限制仍在，只能 load-level 驗證、無法點擊）。

## 驗證方式

1. `cd samples/QuickStart.Server && dotnet run` 起後端
2. `cd samples/Web.Js.Demo && python3 -m http.server 8080` 起 static server
3. 瀏覽器 http://localhost:8080，依序：
   - Login（demo / demo）
   - 點「Load Form Definition」→ 確認 Employee form 完整渲染
     （員工編號 / 員工姓名 / 到職日 / 在職中 4 個欄位 + EmployeePhone detail table）
   - 點「New (Empty)」→ form 清空
   - 填欄位 → 點「Save Form」→ 結果區出現 `affectedRows: 1`
   - 上方 GetList 拿一個 RowId → 貼到輸入框 → 點「Load by RowId」→ form 填入該筆員工資料
   - 改一個欄位 → 點「Save Form」→ server 接受 Modified row

## 不在本 plan 範圍

- React / Vue / Angular 端的 framework integration（屬於外部框架專案）
- 客戶端 schema 驗證引擎（依賴 FormSchema 的 validation rules，另一塊大題目）
- TypeScript 版 renderer（docs 已有 TS interface snippet，TS 專案可自行 port）
- Detail row 的新增 / 修改 / 刪除（本 plan 只渲染唯讀 table）
- Lookup / relation 控制項實作

## 相關連結

- [plan-jsonrpc-formschema-formlayout.md](plan-jsonrpc-formschema-formlayout.md)
  — 前置 plan（提供 GetFormSchema / GetFormLayout endpoints）
- [docs/jsonrpc-frontend-integration.md](../jsonrpc-frontend-integration.md)
  — JS 整合指引（含 TypeScript wrapper 範例）
- [samples/Web.Js.Demo](../../samples/Web.Js.Demo/) — 本 plan 工作對象
