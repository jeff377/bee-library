# ADR-023：定義驅動的 lookup 關連機制

## 狀態

已採納（2026-06-15）

## 背景

[ADR-005](adr-005-formschema-driven.md) 確立 FormSchema 為定義中樞；[ADR-001](adr-001-dataset-as-dto.md) 以 DataSet 承載 master-detail 資料、不做投影。FormSchema 早有「關連欄」表達外鍵：relation 欄帶 `RelationProgId`（指向被參照的表單）+ `RelationFieldMappings`（來源欄 → 本地 `ref_*` 目的欄）。但在此之前，關連欄只能**顯示**既有值，沒有「開窗挑一筆關連記錄、把外鍵與顯示欄一併寫回」的 UI 機制。實務 ERP 表單（訂單選客戶、商品選供應商、明細逐列選商品）大量依賴這個動作。

需求拆解：

- **挑哪一筆**：要一個可搜尋、可分頁的關連清單供使用者選取。
- **寫回什麼**：被選記錄的 `sys_rowid`（關連鍵）+ 依 `RelationFieldMappings` 帶出的顯示欄（`ref_*`）。
- **顯示什麼**：關連欄平常要顯示人類可讀的「編號 - 名稱」，而非裸 Guid。
- **誰負責產生編輯器**：layout 產生器要能從「這是 relation 欄」自動決定用開窗式編輯器，定義端不必逐欄手寫控件型別。
- **兩種入口**：主表欄位（單一關連）與明細 grid 每列（InCell 關連）。

## 考慮過的選項

1. **呼叫端自行接 lookup**：每個表單自己寫「開窗 + 寫回」程式碼。最大彈性但完全違背「定義驅動、零 CRUD 程式碼」目標，且每張表單重複同一套樣板。
2. **以獨立的 lookup 定義節點描述**（另立 `<Lookup>` 元素與既有 relation 欄並存）：表達力強，但與既有 `RelationProgId` / `RelationFieldMappings` 語意重疊，造成「兩套關連定義」的認知負擔與不一致風險。
3. **沿用既有 relation 欄語意 + layout 產生器自動解析（採用）**：relation 欄已含「指向哪個表單、帶回哪些欄」的完整資訊，缺的只是「顯示欄」與「開窗 UI」。補上 `DisplayField` / `LookupFields` 兩個定義屬性，由 `FormLayoutGenerator` 把 relation 欄自動產為開窗式編輯器，不新增平行的關連定義。

## 決策

採**選項 3**，分三層：

### 定義層（`Bee.Definition`）

- relation 欄維持 `RelationProgId` + `RelationFieldMappings`（來源 → `ref_*`）作為**單一關連事實來源**。
- 新增 `DisplayField` / `DisplayFields`：lookup 編輯器與清單以「編號 - 名稱」複合顯示（分隔符為「 - 」，避免與含空格的名稱混淆）。
- 新增 `FormSchema.LookupFields`：開窗清單要呈現的欄位集合。
- `FormLayoutGenerator` 涵蓋規則：relation 欄自動解析為 `ButtonEdit`（開窗式編輯器）；對應的 `ref_*` 目的欄由 relation 欄承載顯示、**不另外產生獨立編輯器**（避免同一筆關連在版面上出現兩次）。

### 後端（`Bee.Api` / `Bee.Business`）

- `FormBusinessObject.GetLookup`（含可覆寫的 `GetLookupFilter()`）為「開窗清單」專用取數——本質是 `GetList` 的 lookup 變體，可被 BO 依情境限縮（例如只列啟用中的供應商）。
- 關連欄回存後，`ref_*` 顯示欄由 server reload 時的 JOIN 重算（client 端寫入只為即時顯示，不是權威值）。

### 前端（`Bee.UI.Avalonia`，先 Desktop）

- `LookupPanel`（搜尋 + 清單 + 選取）/ `LookupDialog`（以 `Window.ShowDialog` 開窗包裝）。
- `ButtonEdit` 內建 lookup 流程：顯示綁定、點圖示開窗、選取後寫回 `sys_rowid` + mapped `ref_*`、可清空。
- `GridControl` 明細 InCell：點擊關連 cell 開同一個 `LookupDialog`，逐列選取。

配套修正：`GetNewData` 骨架補上 `RelationField` 欄位——否則新增流程選取後顯示值無欄可落、帶不回。

## 影響

- 一張含多重關連、master-detail、明細逐列選取的表單可**純定義達成**（relation 欄 + `RelationFieldMappings` + `DisplayFields` + `LookupFields`），不寫 UI／CRUD 程式碼；這是 `apps/Bee.Northwind` 訂單範例的基礎。
- `ref_*` 欄為衍生顯示欄、權威值在 server JOIN：client 寫入只保即時呈現，reload 後以 server 結果為準，避免「前端帶錯顯示值落庫」。
- **Desktop-only**：`LookupDialog` 依賴 `Window.ShowDialog`（多視窗）；Avalonia WASM／Mobile 為 single-view、無 `Window`，跨平台需 `Bee.UI.Avalonia` 改 single-view overlay 的 dialog 抽象（`IDialogPresenter` 或採社群 overlay 方案）——屬另立 plan 的框架工作，本決策範圍只到 Desktop。
- relation 欄自動成 `ButtonEdit` 後，純展示（不可挑選）的關連顯示需求需另以唯讀欄表達；目前以 `FormField.ReadOnly`（見 CHANGELOG 4.10.0）涵蓋。
- 與 [ADR-021](adr-021-avalonia-datagrid-editing-strategy.md) 的 in-cell 編輯策略一致：明細關連欄走「點擊置換編輯器」路徑，lookup 開窗不被 DataGrid 編輯管線撕掉。
