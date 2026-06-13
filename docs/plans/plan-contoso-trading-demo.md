# 計畫：Contoso.Trading 進銷存範例系統（Northwind 情境 / Avalonia 前端）

**狀態：📝 擬定中（2026-06-13 業務案例定案 Northwind 核心 8 表）**

> **前置依賴已解除**：`plan-avalonia-lookup-edit`（Avalonia lookup 開窗機制）已於 2026-06-12 全階段完成並封存至 `docs/archive/`。本 plan 各階段不再有 lookup 阻塞，交易單據可全速做。

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 專案骨架：Contoso.Trading 三件套（Define / Server / Avalonia）建立並跑通最小主檔表單 | 📝 待做 |
| 2 | 純主檔基礎資料：Category / Supplier / Customer / Shipper（零 lookup、零 C#） | 📝 待做 |
| 3 | 帶 lookup 的主檔：Employee（自關連）+ Product（雙 lookup）（仍零 C#） | 📝 待做 |
| 4 | 交易單據：Order + Order Details（master-detail + 三 lookup + 明細 InCell，仍零 C#） | 📝 待做 |
| 5 | 應用層業務：Order BO 覆寫（單號、狀態、驗證、金額計算） | 📝 待做 |
| 6 | Avalonia 導航整合：選單式多表單切換、完整操作動線 | 📝 待做 |
| 7 | 教學文件：README + 「30 分鐘加一張 Region 表單」終章 | 📝 待做 |

## 背景

Bee.NET 的 `samples/` 有多個前端 demo，每個證明的是「該前端能接上框架」（技術可行性）。`Avalonia.Demo` 在 lookup plan 期間已演進為三 tab（Employee / Department / Project）並含完整 lookup 動線，但它的定位仍是**框架能力的技術驗證**，不是「一套用定義方式建構的完整業務系統」。

對照 .NET 領域競品的說服力來源：XAF 靠 MainDemo / XCRM、ABP 靠 `Acme.BookStore`、Frappe 靠 ERPNext。Bee.NET 需要一個**多表單、相互關聯、含完整業務動線**的展示系統，讓開發者看到 FormSchema 作為定義中樞的網狀效果。本 plan 建立獨立的 `Contoso.Trading.*` 三件套作為這個「銷售範本」。

### 為何採 Northwind 業務情境

**Northwind 是 .NET 開發者的「共同語言」** —— 他們腦中已有這套 schema（Customers / Orders / Products / Suppliers…）與業務情境，看 demo 時認知負擔近乎零，全副注意力能放在「bee 的 FormSchema 怎麼表達這些關連」這個真正的賣點上。Northwind Traders 本身就是貿易公司，與 `Contoso.Trading` 主題契合（Northwind 當「資料案例」，Contoso.Trading 當「專案名」）。

更關鍵：Northwind 的關連網**比自擬案例更豐富、且全是真實業務關連**，一次涵蓋 bee 的全部 lookup 能力：

| Northwind 表 | bee ProgId | 展示的關連 |
|--------------|-----------|-----------|
| Categories | `Category` | 純主檔（lookup 來源） |
| Suppliers | `Supplier` | 純主檔（lookup 來源） |
| Customers | `Customer` | 純主檔（lookup 來源） |
| Shippers | `Shipper` | 純主檔（lookup 來源） |
| Employees | `Employee` | **自關連**：ReportsTo → Employee（選主管，lookup 指向同一 ProgId） |
| Products | `Product` | 主表**雙 lookup**：Supplier + Category |
| Orders | `Order` | 主表**三 lookup**：Customer + Employee + Shipper |
| Order Details | `Order` 的明細表 | 明細 **InCell lookup**：每列選 Product |

比 Avalonia.Demo 現有的 Department/Project 例子強得多 —— 主表單/雙/多 lookup、明細 InCell lookup、自關連全展示到。

### 命名由來

採 ABP 官方教學 `Acme.BookStore` 的「`<虛構公司>.<業務領域>`」模式：

- **`Contoso`** 是微軟生態公認的「示範專用虛構公司」—— 開發者一看 `Contoso.*` 就知道是範例專案、不是可安裝使用的產品，消除舊命名 `MiniErp` 被誤認為「一套可用的小型 ERP」的問題。
- **`Trading`** 點出進銷存（採購 + 銷售）業務領域，不自稱 ERP。
- 三件套：`Contoso.Trading.Define` / `Contoso.Trading.Server` / `Contoso.Trading.Avalonia`。

### Northwind → bee 模型對應（關鍵技術決策）

Northwind 是關聯式正規化 schema（文字/int PK、複合主鍵、int FK），bee 是 `sys_rowid`(Guid) 關連模型。**採 Northwind 的業務情境與關連網，但鍵結構與系統欄位用 bee 慣例** —— 開發者認得業務、又看到 bee 的標準關連模型，不被 Northwind 的關聯式細節綁架：

| Northwind 結構 | bee 慣例對應 |
|---------------|-------------|
| 文字/int 主鍵（`CustomerID='ALFKI'`、`ProductID=17`） | `sys_id`(String 業務代碼) + `sys_rowid`(Guid 關連鍵) + `sys_no`(AutoIncrement 流水) |
| 名稱欄（`CompanyName` / `ProductName`） | `sys_name`；其餘屬性欄（`ContactName` / `UnitPrice`…）保留原意命名 |
| 外鍵（`Orders.CustomerID` → Customers） | `customer_rowid`(Guid) + `RelationProgId="Customer"` + `RelationFieldMappings`（帶出 `ref_customer_id` / `ref_customer_name`） |
| 複合主鍵明細（`Order Details`: OrderID+ProductID） | `sys_rowid` PK + `sys_master_rowid`(→Order) + `product_rowid`(lookup→Product) + qty / price |
| 自關連（`Employees.ReportsTo` → Employees） | `reports_to_rowid`(Guid) + `RelationProgId="Employee"`（指向同一 ProgId） |

### 能力盤點（2026-06-13，lookup 完成後重新確認）

v4.9 + lookup 機制落地後，本 demo 所需的框架能力**幾乎全部就緒**：

- ✅ **多表 master-detail**：FormSchema 多 `FormTable` + `sys_master_rowid`，`DynamicForm` 自動渲染明細 grid
- ✅ **lookup 全套（已實戰驗證）**：`RelationProgId` + `RelationFieldMappings`、`DisplayFields`（複合顯示「編號 - 名稱」）、`FormSchema.LookupFields`、`FormLayoutGenerator` 涵蓋規則、後端 `GetLookup`（含 `GetLookupFilter()` 業務過濾覆寫點）、Avalonia `LookupDialog` + 主表 ButtonEdit + 明細 InCell lookup。**直接複用 `samples/Define/Project.FormSchema.xml` 寫法**
- ✅ **GetNewData 骨架含 `ref_*`**（lookup 自測修復）：New 流程的 lookup 寫回顯示值能落地
- ✅ **Avalonia 可編輯表單全套**：`FormView` / `DynamicForm` / `GridControl`（InCell 與 EditForm 雙模式）/ 7 種 field editor
- ✅ **QuickStart 自動建表 + 種子**：`Bee.Samples.Shared.DemoSchemaSeeder` 的 `TableSchemaBuilder` + `DbCommandSpec` 模式可複用
- ❓ **自關連 lookup（Employee ReportsTo → Employee）尚未在 samples 驗證**：lookup 目前都是跨表（Project→Department）；指向同一 ProgId 理論上同管線，但屬本 demo 首次實戰，列為觀察點
- ⚠️ **框架無、需 demo 應用層示範**：單據編號生成、單據狀態與轉移檢查、表單層必填驗證 —— 正是要展示的「pro-code 業務邏輯」分工界線

## 目標

1. **核心論證**：開發者照 README 走完，體會「新增一張表單 = 新增幾個 XML 定義檔」，不寫 UI 與 CRUD 程式碼
2. **誠實展示業務碼的位置**：單號、狀態、驗證放 BO 覆寫，示範「定義驅動」與「pro-code 業務邏輯」的分工界線（與低程式碼平台的差異化論述）
3. **Dogfooding**：過程中逼出的框架缺口記錄到「框架回饋清單」，不在本 demo 內擴張框架

## 範圍

### 前端：只做 Avalonia

本 plan **只做 Avalonia 前端**（目前 bee-library 最完整的桌面前端，最適合把完整業務動線做扎實）。

> **多前端是框架天生能力，不是本 plan 工項**：`Contoso.Trading.Define`（FormSchema）與 `Contoso.Trading.Server`（JSON-RPC）本就前端無關 —— 未來要加 Blazor / MAUI，是「接上同一份 Define + Server」的接線工作、不需重寫業務邏輯。這個「一份定義餵多前端、各自零 CRUD 頁面」正是 bee 相對 ABP（每個 UI 仍各寫頁面）的差異化賣點，但留待 Avalonia 旗艦成熟後再以獨立 plan 展開。

### 表單清單（Northwind 核心 8 表 + 教學終章 1 張）

| ProgId | Northwind | 類型 | 展示重點 |
|--------|-----------|------|---------|
| `Category` | Categories | 純主檔 | 純定義檔 CRUD，零 C# |
| `Supplier` | Suppliers | 純主檔 | 純定義檔 CRUD |
| `Customer` | Customers | 純主檔 | 純定義檔 CRUD |
| `Shipper` | Shippers | 純主檔 | 純定義檔 CRUD |
| `Employee` | Employees | 主檔 + 自關連 | ReportsTo lookup 指向 Employee 自身 |
| `Product` | Products | 主檔 + 雙 lookup | Supplier + Category lookup |
| `Order` | Orders | 單據（master-detail） | 主表三 lookup（Customer/Employee/Shipper）+ 明細 InCell lookup（Product）+ 單號/狀態 |
| （明細） | Order Details | `Order` 的明細表 | 每列 Product lookup、數量、單價、金額 |
| `Region` | Region | 教學終章 | 讀者自己動手加，僅 XML、不寫 code |

### 明確不做（防止範圍蔓延）

- ❌ 庫存計算 / 庫存異動帳（需要 posting 機制，遠超 demo 範圍）
- ❌ 採購單（Northwind 無此案例；銷售 Order 已足夠展示單據模式）
- ❌ 簽核流程（框架無 workflow 層，單據僅做狀態欄位 + BO 轉移檢查）
- ❌ 報表引擎（查詢用既有 `FilterCondition` GetList 展示）
- ❌ Northwind 周邊表（Region/Territories 多對多、CustomerDemographics）—— Region 留作終章讓讀者加，其餘不做
- ❌ Blazor / MAUI 前端（框架能力，另立 plan）
- ❌ 框架功能擴張（單號等先以應用層示範，驗證後另立 plan 決定是否上收框架）

## 專案結構

新建獨立 sample 三件套，**與現有 `samples/Define`（Avalonia.Demo 的 lookup 技術驗證）完全隔離** —— 兩者定位不同：現有 demo 證明能力、`Contoso.Trading` 證明完整系統。

```
samples/
├── Contoso.Trading.Define/    # FormSchema / FormLayout / TableSchema XML（demo 的「原始碼」）
│   ├── FormSchema/            #   Category, Supplier, Customer, Shipper, Employee, Product, Order
│   ├── FormLayout/            #   主檔 + 含 <Details> grid 的單據版面
│   └── TableSchema/
├── Contoso.Trading.Server/    # JSON-RPC 後端：QuickStart.Server 模式 + 自訂 BO + 專屬 seeder
│   └── BusinessObjects/       #   OrderBO（單號、狀態、驗證、金額）
└── Contoso.Trading.Avalonia/  # 前端：選單導航 + FormView
```

建立時走 `bee-sample-add` skill 的決策樹與 `Bee.Samples.slnx` 整合慣例。

> **lookup 寫法直接抄 Project**：`samples/Define/Project.FormSchema.xml` 已是完整範例（relation 欄 `Visible="false"` + `RelationProgId` + `RelationFieldMappings`、明細 InCell lookup），`Contoso.Trading` 的 Product / Order 定義照搬即可。

## 階段內容

> 敘事主軸：階段 2→4 是**遞進的「零程式碼」里程碑** —— 純 CRUD → lookup 關連 → master-detail 單據，全部只靠 XML 定義；階段 5 才引入業務碼，清楚劃出分工界線。

### 階段 1：專案骨架

- 依 `bee-sample-add` 建立三個專案、接上 `Bee.Samples.slnx`
- `Contoso.Trading.Server` 自帶 seeder（複用 `DemoSchemaSeeder` 模式），首啟自動建 SQLite + system tables + 業務表
- `Contoso.Trading.Define` 先放一張最小主檔（`Category`），Avalonia 端登入（demo/demo）→ CRUD 跑通
- **驗收**：`dotnet run` 兩個指令起整套系統，Category 可新增/編輯/刪除

### 階段 2：純主檔基礎資料（最純的定義驅動）

- `Category` / `Supplier` / `Customer` / `Shipper` 四組 FormSchema + FormLayout + TableSchema
- 此階段**刻意零 C# 業務碼、零 lookup**：四張純主檔只靠預設 `FormBusinessObject` 管線
- 種子：seeder 寫入 Northwind 子集（類別 8 筆、供應商 5 筆、客戶 10 筆、貨運商 3 筆）
- **驗收**：四張表單 CRUD 全通，diff 中只有 XML 與種子資料

### 階段 3：帶 lookup 的主檔（lookup 關連零程式碼）

- `Employee`：含 `reports_to_rowid` **自關連 lookup**（指向 Employee 自身，選主管）
- `Product`：`supplier_rowid` + `category_rowid` **雙 lookup**
- 仍**零 C# 業務碼**：lookup 全靠定義（`RelationProgId` + `RelationFieldMappings` + `Visible="false"`）
- 種子：員工 5 筆（含 ReportsTo 階層）、商品 15 筆
- **驗收**：Product 開窗選 Supplier/Category 顯示「編號 - 名稱」；Employee 開窗選主管（自關連）；存後重載 `ref_*` 由 server JOIN 重算

### 階段 4：交易單據（master-detail + 多 lookup + 明細 InCell）

- `Order`：主表三 lookup（Customer / Employee / Shipper）+ 明細表 Order Details（每列 Product InCell lookup、數量、單價）
- 定義套 `Project.FormSchema.xml` 範式；FormLayout 含 `<Details>` grid
- 仍**零 C# 業務碼** —— 證明「master-detail 單據純定義就能做出來」
- **驗收**：訂單可開窗選客戶/員工/貨運商、明細逐列選商品、master-detail 一次儲存、存後重載正確

### 階段 5：應用層業務（pro-code 分工界線展示）

- `OrderBO` 覆寫示範：
  - **單據編號**：`ORD-yyyyMM-NNN` 格式，於 Save 時產生（含併發注意事項註記）
  - **狀態欄位**：Draft / Confirmed / Shipped enum dropdown，BO 內檢查合法轉移、Confirmed 後鎖明細
  - **必填驗證**：客戶、至少一筆明細、數量 > 0，違規拋帶訊息的業務例外
- 金額計算：明細 數量×單價（套 Discount）、主表總額彙總（BO 內算，不信任前端值）
- BO 跨層新增走 `bee-add-bo-method` skill（若需新 action）；單純覆寫 `Save` / `GetNewData` 則直接 override
- **驗收**：對應的 xUnit 測試（BO 層，不需 UI）+ 前端操作違規時收到可讀錯誤訊息

### 階段 6：Avalonia 導航整合

- MainWindow 左側選單（基礎資料 / 交易單據 分組）切換 FormView（比 Avalonia.Demo 的 TabControl 更接近真實 ERP 導航）
- 操作動線打磨：開啟即列表、雙擊進編輯、工具列一致
- **驗收**：跑 `demo-smoke` 等級的端到端動線（登入 → 建客戶 → 建商品 → 開訂單選入 → 確認狀態 → 出貨）

### 階段 7：教學文件

- `samples/Contoso.Trading.*/README.md`（雙語，依文件語言規則）：
  - 系統導覽 + 架構圖（定義檔 → Server → Avalonia 的資料流）+ Northwind → bee 模型對應表
  - **終章「30 分鐘加一張 Region 表單」**：讀者只新增 `Region.FormSchema.xml` + `FormLayout` + `TableSchema`，重啟即得完整 CRUD —— 這一章是整個 demo 的銷售論證
  - 「哪些是定義、哪些是業務碼」對照表（指向階段 5 的 OrderBO）
- `samples/README.md` 索引更新
- **驗收**：使用者照終章操作一次走通

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| Northwind schema 與 bee 模型落差（int FK / 複合鍵） | 採 bee 慣例鍵（見「Northwind → bee 模型對應」表）；不照搬原 schema |
| 自關連 lookup（Employee ReportsTo → Employee）首次實戰 | 理論上同 lookup 管線；階段 3 獨立驗證，踩雷則記框架回饋清單 |
| 單號/狀態做著做著想上收框架，範圍蔓延 | 本 plan 鎖死在應用層；缺口記錄到「框架回饋清單」，另立 plan 處理 |
| `Contoso.Trading.Define` 與 `samples/Define` 混淆（兩處都有 lookup demo） | 完全獨立目錄、獨立 server、獨立 SQLite 檔；README 明述兩者定位差異 |
| 單據明細 InCell lookup 的 UI 動線 | 已在 Avalonia.Demo Project 表單實戰驗證；編譯過即交付使用者自測 |

## 框架回饋清單（執行中持續累積）

> Demo 過程發現的框架缺口記在這裡，完成後評估是否各自立 plan。

- （預期）單據編號序列生成器是否值得上收為框架服務
- （預期）FormField 驗證規則（Required / Min / Max）定義層支援
- （觀察）自關連 lookup 是否需要框架層特別處理（避免無限遞迴開窗等）
- （待發現）……

## 給執行 session 的 handoff 摘要

> 本 plan 預計**另起新 session 執行**。新 session 接手時的關鍵脈絡：

1. **業務案例 = Northwind 核心 8 表**，但鍵結構用 bee 慣例（見「Northwind → bee 模型對應」表）
2. **lookup 機制已完整落地於 main**（見 `docs/archive/plan-avalonia-lookup-edit.md`）：`DisplayFields` 複合顯示、`GetLookup` API、`LookupDialog`、主表 + 明細 InCell lookup 全可用
3. **現成範例**：`samples/Define/Project.FormSchema.xml`（lookup 定義）、`samples/Avalonia.Demo/`（前端接線）、`samples/Bee.Samples.Shared/DemoSchemaSeeder.cs`（建表 + 種子）
4. **相關 skill**：`bee-sample-add`（建三件套）、`bee-scaffold-from-formschema`（從 FormSchema 產 FormLayout / TableSchema / Language sidecar）、`bee-add-bo-method`（OrderBO 若需新 action）、`demo-smoke`（端到端冒煙）
5. **協作慣例**：本機可 build/test → 直接改 main；UI 編譯過即交付使用者自測；每階段 commit 前 `./test.sh` 全綠
