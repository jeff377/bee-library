# 計畫：迷你進銷存完整 Demo 系統（Avalonia 旗艦前端）

**狀態：📝 擬定中（2026-06-13 對齊 lookup 完成後現況改寫）**

> **前置依賴已解除**：`plan-avalonia-lookup-edit`（Avalonia lookup 開窗機制）已於 2026-06-12 全階段完成並封存至 `docs/archive/`。本 plan 各階段不再有 lookup 阻塞，交易單據可全速做。

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 專案骨架：MiniErp 三件套（Define / Server / Avalonia）建立並跑通最小主檔表單 | 📝 待做 |
| 2 | 基礎資料：Customer / Product / Vendor / Employee 四張表單（純定義檔，零 C# 業務碼） | 📝 待做 |
| 3 | 交易單據：SalesOrder / PurchaseOrder（master-detail + lookup 參照） | 📝 待做 |
| 4 | 應用層業務：單據編號、狀態欄位、必填驗證（BO 覆寫示範） | 📝 待做 |
| 5 | Avalonia 導航整合：選單式多表單切換、完整操作動線 | 📝 待做 |
| 6 | 教學文件：README + 「30 分鐘加一張新表單」終章 | 📝 待做 |

## 背景

Bee.NET 的 `samples/` 有多個前端 demo，每個證明的是「該前端能接上框架」（技術可行性）。`Avalonia.Demo` 在 lookup plan 期間已從單一 Employee 表單演進為三 tab（Employee / Department / Project）並含完整 lookup 動線，但它的定位仍是**框架能力的技術驗證**，不是「一套用定義方式建構的完整業務系統」。

對照 .NET 領域競品的說服力來源：XAF 靠完整展示應用、Frappe 靠 ERPNext。Bee.NET 需要一個**多表單、相互關聯、含完整業務動線**的展示系統，讓開發者看到 FormSchema 作為定義中樞的網狀效果。本 plan 建立獨立的 `MiniErp.*` 三件套作為這個「銷售範本」，與現有 demo 的技術驗證定位分離。

### 能力盤點（2026-06-13，lookup 完成後重新確認）

v4.9 + lookup 機制落地後，本 demo 所需的框架能力**幾乎全部就緒**：

- ✅ **多表 master-detail**：FormSchema 多 `FormTable` + `sys_master_rowid` 關聯，`DynamicForm` 自動渲染明細 grid
- ✅ **lookup 全套（lookup plan 已完成並實戰驗證）**：
  - 定義層 `RelationProgId` + `RelationFieldMappings`、`DisplayFields`（複合顯示「編號 - 名稱」；交易型目標只映射 `sys_id` 時自然只顯示單號）、`FormSchema.LookupFields`（曝險欄位宣告）
  - `FormLayoutGenerator` 涵蓋規則：relation 欄位 `Visible=false` + ButtonEdit 承載複合顯示，被 `DisplayFields` 涵蓋的 `ref_*` 不重複產生
  - 後端 `GetLookup`（權限軸獨立、`GetLookupFilter()` 業務過濾覆寫點、`SearchText` server-side 過濾、預設分頁）
  - Avalonia `LookupDialog` / `LookupPanel`、主表 ButtonEdit 開窗寫回、明細 grid InCell lookup
  - **可直接複用 `samples/Define/Project.FormSchema.xml` 的寫法**（master lookup + 明細 InCell lookup 都有現成範例）
- ✅ **GetNewData 骨架含 `ref_*` 欄位**（lookup 自測時修復）：New 流程的 lookup 寫回顯示值能落地
- ✅ **Avalonia 可編輯表單全套**：`FormView`（清單 + 編輯）、`DynamicForm`、`GridControl`（InCell 與 EditForm 雙模式）、7 種 field editor
- ✅ **QuickStart 模式自動建表 + 種子**：`Bee.Samples.Shared.DemoSchemaSeeder` 以 `TableSchemaBuilder` 建表、`DbCommandSpec` 種子的模式可直接複用
- ⚠️ **框架無、需在 demo 應用層示範**：單據編號生成、單據狀態欄位與轉移檢查、表單層必填驗證（目前僅 SQL NOT NULL）—— 這正是本 demo 要展示的「pro-code 業務邏輯」分工界線

## 目標

1. **核心論證**：開發者照 README 走完，能體會「新增一張表單 = 新增幾個 XML 定義檔」，不需要寫 UI 與 CRUD 程式碼
2. **誠實展示業務碼的位置**：單號、狀態、驗證等業務邏輯放 BO 覆寫，示範「定義驅動」與「pro-code 業務邏輯」的分工界線（這是與低程式碼平台的差異化論述）
3. **Dogfooding**：過程中逼出的框架缺口記錄到「框架回饋清單」，不在本 demo 內擴張框架

## 範圍

### 表單清單（7 張 + 教學終章 1 張）

| ProgId | 名稱 | 類型 | 展示重點 |
|--------|------|------|---------|
| `Customer` | 客戶 | 基礎資料 | 純定義檔 CRUD，零 C# |
| `Product` | 商品 | 基礎資料 | 純定義檔 CRUD，含單價/單位欄位 |
| `Vendor` | 廠商 | 基礎資料 | 純定義檔 CRUD |
| `Employee` | 員工 | 基礎資料 | 純定義檔 CRUD；作為銷售單「業務員」lookup 來源 |
| `SalesOrder` | 銷售訂單 | 單據（master-detail） | lookup 客戶/商品/業務員、明細 grid InCell 編輯、單號、狀態 |
| `PurchaseOrder` | 進貨單 | 單據（master-detail） | lookup 廠商/商品、與 SalesOrder 對照展示模式可複製性 |
| `Warehouse` | 倉庫 | 教學終章 | 讀者自己動手加，僅 XML、不寫 code |

> `Employee` 在 `MiniErp.Define` 內自帶（獨立三件套不共用 `samples/Define`），可參考 `samples/Define/Employee.FormSchema.xml` 但獨立一份。

### 明確不做（防止範圍蔓延）

- ❌ 庫存計算 / 庫存異動帳（需要 posting 機制，遠超 demo 範圍）
- ❌ 簽核流程（框架無 workflow 層，單據僅做狀態欄位 + BO 轉移檢查）
- ❌ 報表引擎（查詢用既有 `FilterCondition` GetList 展示）
- ❌ 多前端完整版（Blazor 等維持現有小 demo；本 plan 只做 Avalonia 旗艦）
- ❌ 框架功能擴張（單號等先以應用層示範，驗證後另立 plan 決定是否上收框架）

## 專案結構

新建獨立 sample 三件套，**與現有 `samples/Define`（Avalonia.Demo 的 lookup 技術驗證）完全隔離** —— 兩者定位不同：現有 demo 證明能力、MiniErp 證明完整系統。

```
samples/
├── MiniErp.Define/          # FormSchema / FormLayout / TableSchema XML（demo 的「原始碼」）
│   ├── FormSchema/          #   Customer, Product, Vendor, Employee, SalesOrder, PurchaseOrder
│   ├── FormLayout/          #   主檔 + 含 <Details> grid 的單據版面
│   └── TableSchema/
├── MiniErp.Server/          # JSON-RPC 後端：QuickStart.Server 模式 + 自訂 BO + 專屬 seeder
│   └── BusinessObjects/     #   SalesOrderBO / PurchaseOrderBO（單號、狀態、驗證）
└── MiniErp.Avalonia/        # 旗艦前端：選單導航 + FormView
```

建立時走 `bee-sample-add` skill 的決策樹與 `Bee.Samples.slnx` 整合慣例。

> **lookup 寫法直接抄 Project**：`samples/Define/Project.FormSchema.xml` 已是完整範例（relation 欄位 `Visible="false"` + `RelationProgId` + `RelationFieldMappings`、明細 InCell lookup），MiniErp 的單據定義照搬即可，不需重新摸索。

## 階段內容

### 階段 1：專案骨架

- 依 `bee-sample-add` 建立三個專案、接上 `Bee.Samples.slnx`
- `MiniErp.Server` 自帶 seeder（複用 `DemoSchemaSeeder` 的 `TableSchemaBuilder` 建表 + `DbCommandSpec` 種子模式），首啟自動建 SQLite + system tables + 業務表
- `MiniErp.Define` 先放一張最小主檔（如 `Customer`），Avalonia 端登入（demo/demo）→ CRUD 跑通
- **驗收**：`dotnet run` 兩個指令起整套系統，最小表單可新增/編輯/刪除

### 階段 2：基礎資料（定義驅動的純度展示）

- `Customer` / `Product` / `Vendor` / `Employee` 四組 FormSchema + FormLayout + TableSchema
- 此階段**刻意零 C# 業務碼**：四張表單只靠預設 `FormBusinessObject` 管線
- 種子資料：seeder 寫入少量 demo 資料（客戶 5 筆、商品 10 筆、廠商 3 筆、員工 3 筆）
- **驗收**：四張表單 CRUD 全通，diff 中只有 XML 與種子資料

### 階段 3：交易單據（master-detail + lookup）

- `SalesOrder`：主表（單號、日期、客戶 lookup、業務員 lookup、狀態、備註）+ 明細表（商品 lookup、數量、單價、金額）
- `PurchaseOrder`：同模式對廠商，證明模式可複製
- lookup 定義直接套 `Project.FormSchema.xml` 的範式：rowid 欄 `Visible="false"` + `RelationProgId` + `RelationFieldMappings`（帶 `sys_id` / `sys_name` → `ref_*`）；FormLayout 含 `<Details>` grid，明細商品欄走 InCell lookup
- **驗收**：訂單可開窗選客戶/商品/業務員（顯示「編號 - 名稱」）、明細列增刪、master-detail 一次儲存、存後重載 `ref_*` 由 server JOIN 重算

### 階段 4：應用層業務（pro-code 分工界線展示）

- `SalesOrderBO` / `PurchaseOrderBO` 覆寫示範：
  - **單據編號**：`SO-yyyyMM-NNN` 格式，於 Save 時產生（含併發注意事項註記）
  - **狀態欄位**：Draft / Confirmed / Closed enum dropdown，BO 內檢查合法轉移（Draft→Confirmed→Closed），Confirmed 後鎖明細
  - **必填驗證**：客戶、至少一筆明細、數量 > 0，違規拋帶訊息的業務例外
- 金額計算：明細 數量×單價，主表總額彙總（BO 內計算，不信任前端值）
- BO 跨層新增走 `bee-add-bo-method` skill 流程（若需新 action）；單純覆寫 `Save` / `GetNewData` 則直接 override
- **驗收**：對應的 xUnit 測試（BO 層，不需 UI）+ 前端操作違規時收到可讀錯誤訊息

### 階段 5：Avalonia 導航整合

- MainWindow 左側選單（基礎資料 / 交易單據 分組）切換 FormView（比 Avalonia.Demo 現有的 TabControl 更接近真實 ERP 導航）
- 操作動線打磨：開啟即列表、雙擊進編輯、工具列一致
- **驗收**：跑 `demo-smoke` 等級的端到端動線（登入 → 建客戶 → 建商品 → 開訂單選入 → 確認狀態 → 關單）

### 階段 6：教學文件

- `samples/MiniErp.*/README.md`（雙語，依文件語言規則）：
  - 系統導覽 + 架構圖（定義檔 → Server → Avalonia 的資料流）
  - **終章「30 分鐘加一張倉庫表單」**：讀者只新增 `Warehouse.FormSchema.xml` + `FormLayout` + `TableSchema`，重啟即得完整 CRUD —— 這一章是整個 demo 的銷售論證
  - 「哪些是定義、哪些是業務碼」對照表（指向階段 4 的 BO 檔案）
- `samples/README.md` 索引更新
- **驗收**：使用者照終章操作一次走通

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| 單號/狀態做著做著想上收框架，範圍蔓延 | 本 plan 鎖死在應用層；缺口記錄到「框架回饋清單」，另立 plan 處理 |
| `MiniErp.Define` 與 `samples/Define` 混淆（兩處都有 lookup demo） | 完全獨立目錄、獨立 server、獨立 SQLite 檔；README 明述兩者定位差異（技術驗證 vs 完整系統） |
| seeder 與 lookup 新增流程的 `ref_*` 欄位互動 | GetNewData 骨架已含 `ref_*`（lookup 自測修復）；種子直接寫業務表即可，`ref_*` 由查詢時 JOIN 補 |
| 單據明細 InCell lookup 的 UI 動線 | 已在 Avalonia.Demo Project 表單實戰驗證；編譯過即交付使用者自測（依既有協作慣例） |

## 框架回饋清單（執行中持續累積）

> Demo 過程發現的框架缺口記在這裡，完成後評估是否各自立 plan。

- （預期）單據編號序列生成器是否值得上收為框架服務
- （預期）FormField 驗證規則（Required / Min / Max）定義層支援
- （待發現）……

## 給執行 session 的 handoff 摘要

> 本 plan 預計**另起新 session 執行**。新 session 接手時的關鍵脈絡：

1. **lookup 機制已完整落地於 main**（見 `docs/archive/plan-avalonia-lookup-edit.md`）：`DisplayFields` 複合顯示、`GetLookup` API、`LookupDialog`、主表 + 明細 InCell lookup 全可用
2. **現成範例**：`samples/Define/Project.FormSchema.xml`（lookup 定義）、`samples/Avalonia.Demo/`（前端接線）、`samples/Bee.Samples.Shared/DemoSchemaSeeder.cs`（建表 + 種子）
3. **相關 skill**：`bee-sample-add`（建三件套）、`bee-scaffold-from-formschema`（從 FormSchema 產 FormLayout / TableSchema / Language sidecar）、`bee-add-bo-method`（若單據需新 BO action）、`demo-smoke`（端到端冒煙）
4. **協作慣例**：本機可 build/test → 直接改 main；UI 編譯過即交付使用者自測；每階段 commit 前 `./test.sh` 全綠
