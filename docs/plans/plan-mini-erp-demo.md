# 計畫：迷你進銷存完整 Demo 系統（Avalonia 旗艦前端）

**狀態：📝 擬定中**

> **前置依賴**：[plan-avalonia-lookup-edit.md](plan-avalonia-lookup-edit.md) —— Avalonia 端 lookup 開窗機制（ButtonEdit 完整管線）。本 plan 的階段 3（交易單據）起需要 lookup；階段 1–2（骨架與基礎資料）不依賴、可先行。

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 專案骨架：MiniErp 三件套（Define / Server / Avalonia）建立並跑通 Employee 等級的單一表單 | 📝 待做 |
| 2 | 基礎資料：Customer / Product / Vendor 三張表單（純定義檔，零 C# 業務碼） | 📝 待做 |
| 3 | 交易單據：SalesOrder / PurchaseOrder（master-detail + lookup 參照） | 📝 待做 |
| 4 | 應用層業務：單據編號、狀態欄位、必填驗證（BO 覆寫示範） | 📝 待做 |
| 5 | Avalonia 導航整合：選單式多表單切換、完整操作動線 | 📝 待做 |
| 6 | 教學文件：README + 「30 分鐘加一張新表單」終章 | 📝 待做 |

## 背景

Bee.NET 目前的 `samples/` 有 9 個前端 demo，但定義檔只有一張 `Employee` 表單。每個 demo 證明的是「該前端能接上框架」（技術可行性），無法證明「可以用定義方式建構出完整系統」（框架的核心賣點）。

對照 .NET 領域競品的說服力來源：XAF 靠完整展示應用、Frappe 靠 ERPNext。Bee.NET 需要一個**多表單、相互關聯、含完整業務動線**的展示系統，讓開發者看到 FormSchema 作為定義中樞的網狀效果。

能力盤點結論（2026-06-12）：v4.9 框架對此 demo 高度就緒 ——

- ✅ 可直接用：FormSchema 多表 master-detail（`sys_master_rowid` 關聯）、lookup 定義層與後端（`RelationProgId` + `RelationFieldMappings`，server SELECT 自動 JOIN 補 `ref_*` 欄位）、Avalonia 可編輯表單全套（`FormView` / `DynamicForm` / `GridControl` InCell 與 EditForm 雙模式）、QuickStart 模式的 SQLite 自動建表
- ❌ Avalonia 端 lookup 開窗管線不存在（`ButtonEdit` 僅有 `ButtonClick` 事件鉤子）→ 已立前置 [plan-avalonia-lookup-edit.md](plan-avalonia-lookup-edit.md)
- ⚠️ 框架無、需在 demo 應用層示範：單據編號生成、單據狀態欄位與轉移檢查、表單層必填驗證（目前僅 SQL NOT NULL）
- 📌 範例缺口：`samples/Define` 沒有 FormLayout 與 lookup 的實際範例（lookup 範例僅在 `tests/Define/FormSchema/Project.FormSchema.xml`）；本 demo 將首次在 samples 補齊

## 目標

1. **核心論證**：開發者照 README 走完，能體會「新增一張表單 = 新增兩個 XML 定義檔」，不需要寫 UI 與 CRUD 程式碼
2. **誠實展示業務碼的位置**：單號、狀態、驗證等業務邏輯放 BO 覆寫，示範「定義驅動」與「pro-code 業務邏輯」的分工界線（這是與低程式碼平台的差異化論述）
3. **Dogfooding**：過程中逼出的框架缺口記錄成 issue / 後續 plan，不在本 demo 內擴張框架

## 範圍

### 表單清單（6 張 + 教學終章 1 張）

| ProgId | 名稱 | 類型 | 展示重點 |
|--------|------|------|---------|
| `Customer` | 客戶 | 基礎資料 | 純定義檔 CRUD，零 C# |
| `Product` | 商品 | 基礎資料 | 純定義檔 CRUD，含單價/單位欄位 |
| `Vendor` | 廠商 | 基礎資料 | 純定義檔 CRUD |
| `SalesOrder` | 銷售訂單 | 單據（master-detail） | lookup 客戶/商品、明細 grid 編輯、單號、狀態 |
| `PurchaseOrder` | 進貨單 | 單據（master-detail） | lookup 廠商/商品、與 SalesOrder 對照展示模式可複製性 |
| `Employee` | 員工 | 基礎資料 | 沿用既有定義（業務員欄位的 lookup 來源） |
| `Warehouse` | 倉庫 | 教學終章 | 讀者自己動手加，僅 XML、不寫 code |

### 明確不做（防止範圍蔓延）

- ❌ 庫存計算 / 庫存異動帳（需要 posting 機制，遠超 demo 範圍）
- ❌ 簽核流程（框架無 workflow 層，單據僅做狀態欄位 + BO 轉移檢查）
- ❌ 報表引擎（查詢用既有 `FilterCondition` GetList 展示）
- ❌ 多前端完整版（Blazor 等維持現有小 demo；本 plan 只做 Avalonia 旗艦）
- ❌ 框架功能擴張（單號等先以應用層示範，驗證後另立 plan 決定是否上收框架）

## 專案結構

新建獨立 sample 三件套，不污染 QuickStart.Server（其定位是最小入門）：

```
samples/
├── MiniErp.Define/          # FormSchema / FormLayout / TableSchema XML（demo 的「原始碼」）
│   ├── FormSchema/          #   Customer, Product, Vendor, SalesOrder, PurchaseOrder, Employee
│   ├── FormLayout/          #   首次在 samples 提供手寫 FormLayout（含 Details grid）
│   └── TableSchema/
├── MiniErp.Server/          # JSON-RPC 後端：QuickStart.Server 模式 + 自訂 BO
│   └── BusinessObjects/     #   SalesOrderBO / PurchaseOrderBO（單號、狀態、驗證）
└── MiniErp.Avalonia/        # 旗艦前端：選單導航 + FormView
```

建立時走 `bee-sample-add` skill 的決策樹與 `Bee.Samples.slnx` 整合慣例。

## 階段內容

### 階段 1：專案骨架

- 依 `bee-sample-add` 建立三個專案、接上 `Bee.Samples.slnx`
- `MiniErp.Define` 先放入沿用的 `Employee` 定義，Server 啟動自動建 SQLite + system tables
- Avalonia 端登入（demo/demo）→ 開啟 Employee 表單 CRUD 跑通
- **驗收**：`dotnet run` 兩個指令起整套系統，Employee 可新增/編輯/刪除

### 階段 2：基礎資料（定義驅動的純度展示）

- `Customer` / `Product` / `Vendor` 三組 FormSchema + FormLayout + TableSchema
- 此階段**刻意零 C# 業務碼**：三張表單只靠預設 `FormBusinessObject` 管線
- 種子資料：Server 首啟寫入少量 demo 資料（客戶 5 筆、商品 10 筆、廠商 3 筆）
- **驗收**：三張表單 CRUD 全通，diff 中只有 XML 與種子資料

### 階段 3：交易單據（master-detail + lookup）

- `SalesOrder`：主表（單號、日期、客戶 lookup、業務員 lookup、狀態、備註）+ 明細表（商品 lookup、數量、單價、金額）
- `PurchaseOrder`：同模式對廠商，證明模式可複製
- FormLayout 含 `<Details>` grid（samples 首例），明細用 InCell 編輯
- lookup 透過 `RelationProgId` + `RelationFieldMappings` 帶出名稱欄位（參考 `tests/Define/FormSchema/Project.FormSchema.xml` 既有用法）
- **驗收**：訂單可開窗選客戶/商品、明細列增刪、master-detail 一次儲存

### 階段 4：應用層業務（pro-code 分工界線展示）

- `SalesOrderBO` / `PurchaseOrderBO` 覆寫示範：
  - **單據編號**：`SO-yyyyMM-NNN` 格式，於 Save 時產生（含併發注意事項註記）
  - **狀態欄位**：Draft / Confirmed / Closed enum dropdown，BO 內檢查合法轉移（Draft→Confirmed→Closed），Confirmed 後鎖明細
  - **必填驗證**：客戶、至少一筆明細、數量 > 0，違規拋帶訊息的業務例外
- 金額計算：明細 數量×單價，主表總額彙總（BO 內計算，不信任前端值）
- **驗收**：對應的 xUnit 測試（BO 層，不需 UI）+ 前端操作違規時收到可讀錯誤訊息

### 階段 5：Avalonia 導航整合

- MainWindow 左側選單（基礎資料 / 交易單據 分組）切換 FormView
- 操作動線打磨：開啟即列表、雙擊進編輯、工具列一致
- **驗收**：跑 `demo-smoke` 等級的端到端動線（登入 → 建客戶 → 建商品 → 開訂單選入 → 確認狀態 → 關單）

### 階段 6：教學文件

- `samples/MiniErp.*/README.md`（雙語，依文件語言規則）：
  - 系統導覽 + 架構圖（定義檔 → Server → Avalonia 的資料流）
  - **終章「30 分鐘加一張倉庫表單」**：讀者只新增 `Warehouse.FormSchema.xml` + `TableSchema`，重啟即得完整 CRUD —— 這一章是整個 demo 的銷售論證
  - 「哪些是定義、哪些是業務碼」對照表（指向階段 4 的 BO 檔案）
- `samples/README.md` 索引更新
- **驗收**：使用者照終章操作一次走通

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| 單號/狀態做著做著想上收框架，範圍蔓延 | 本 plan 鎖死在應用層；缺口記錄到 plan 末尾「框架回饋清單」，另立 plan 處理 |
| FormLayout `<Details>` 在 samples 無前例，可能踩未知雷 | 階段 3 先以 UnitTests 既有覆蓋為基準，踩雷時優先修框架測試再回 demo |
| Avalonia lookup 開窗為前置 plan 新功能，首次大量實戰 | 階段 1–2 不依賴 lookup 可先行；前置 plan 完成且 Project 範例驗證後才進階段 3 |
| 種子資料與既有 `samples/Define` 衝突 | MiniErp.Define 完全獨立目錄，不共用 QuickStart 的 Define |

## 框架回饋清單（執行中持續累積）

> Demo 過程發現的框架缺口記在這裡，完成後評估是否各自立 plan。

- （預期）單據編號序列生成器是否值得上收為框架服務
- （預期）FormField 驗證規則（Required / Min / Max）定義層支援
- （待發現）……
