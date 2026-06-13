# 計畫：Bee.Northwind 進銷存範例（Avalonia 前端）

**狀態：📝 擬定中（2026-06-13 命名定案 Bee.Northwind、開發於 `apps/` 獨立 app、完成後畢業移獨立 repo）**

> **前置依賴已解除**：`plan-avalonia-lookup-edit`（Avalonia lookup 開窗機制）已於 2026-06-12 全階段完成並封存至 `docs/archive/`。本 plan 各階段不再有 lookup 阻塞，交易單據可全速做。

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 專案骨架：`apps/Bee.Northwind/` 三件套（Define / Server / Avalonia）+ 獨立 slnx + ProjectReference，跑通最小主檔表單 | 📝 待做 |
| 2 | 純主檔基礎資料：Category / Supplier / Customer / Shipper（零 lookup、零 C#） | 📝 待做 |
| 3 | 帶 lookup 的主檔：Employee（自關連）+ Product（雙 lookup）（仍零 C#） | 📝 待做 |
| 4 | 交易單據：Order + Order Details（master-detail + 三 lookup + 明細 InCell，仍零 C#） | 📝 待做 |
| 5 | 應用層業務：OrderBO 覆寫（單據編號、狀態、驗證、金額計算） | 📝 待做 |
| 6 | Avalonia 導航整合：選單式多表單切換、完整操作動線 | 📝 待做 |
| 7 | 教學文件：README + 「30 分鐘加一張 Region 表單」終章 | 📝 待做 |
| 8 | 畢業：發 NuGet 新版 → 整個 `apps/Bee.Northwind/` 移至獨立 repo `bee-northwind-avalonia`、ProjectReference 改 PackageReference | 📝 待做 |

## 背景

Bee.NET 的 `samples/` 有多個前端 demo，每個證明的是「該前端能接上框架」（技術可行性）。`Avalonia.Demo` 在 lookup plan 期間已演進為三 tab（Employee / Department / Project）並含完整 lookup 動線，但它的定位仍是**框架能力的技術驗證**，不是「一套用定義方式建構的完整業務系統」。

對照 .NET 領域競品的說服力來源：XAF 靠 MainDemo / XCRM、ABP 靠 `Acme.BookStore`、Frappe 靠 ERPNext。Bee.NET 需要一個**多表單、相互關聯、含完整業務動線**的展示系統，讓開發者看到 FormSchema 作為定義中樞的網狀效果。

### 為何採 Northwind 業務情境

**Northwind 是 .NET 開發者的「共同語言」** —— 他們腦中已有這套 schema（Customers / Orders / Products / Suppliers…）與業務情境，看 demo 時認知負擔近乎零，全副注意力能放在「bee 的 FormSchema 怎麼表達這些關連」這個真正的賣點上。

Northwind 的關連網**比自擬案例更豐富、且全是真實業務關連**，一次涵蓋 bee 的全部 lookup 能力：

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

### 命名：Bee.Northwind

- **`Northwind`** 是 .NET / 微軟生態公認的示範資料庫名 —— 開發者一看就知道是範例、不是可安裝使用的產品；直指業務案例，不需額外的虛構公司中間層。
- **`Bee` 前綴**對齊套件家族（`Bee.Base` / `Bee.Definition`…）。
- 三件套：`Bee.Northwind.Define` / `Bee.Northwind.Server` / `Bee.Northwind.Avalonia`，namespace 隨之展開。
- **獨立 repo 命名**：`bee-northwind-<前端>`，每個前端一個獨立 repo（本 plan 產出 `bee-northwind-avalonia`；未來 `bee-northwind-maui` 等）。每個 repo 自帶完整三件套（`Define` / `Server` 內容複製）—— 範例 repo 要能單獨 clone 跑起來，**自包含 > DRY**。

### Northwind → bee 模型對應（關鍵技術決策）

Northwind 是關聯式正規化 schema（文字/int PK、複合主鍵、int FK），bee 是 `sys_rowid`(Guid) 關連模型。**只借 Northwind 的業務案例與資料內容；資料結構（鍵、欄位、明細關連）一律以 bee 框架為基準** —— 開發者認得業務、又看到 bee 的標準關連模型，不被 Northwind 的關聯式細節綁架。具體轉換規則：

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

## 依賴策略與搬遷生命週期

本 demo **不放 `samples/`**，而是放 `apps/` 獨立資料夾，以三段式生命週期演進。核心理由：**最終「獨立 repo + PackageReference」的 demo 是最強的銷售證明** —— 它證明「外部開發者拿公開 NuGet 套件就能建出這套系統」，徹底排除「內部有特殊接線」的疑慮；而開發期需要 ProjectReference 同步改框架。

| 階段 | 位置 | 框架依賴 | 為什麼 |
|------|------|---------|--------|
| **開發期**（階段 1–7） | `apps/Bee.Northwind/`（bee-library repo 內） | **ProjectReference** → `../../src/Bee.*` | dogfooding 過程可能改底層架構 —— ProjectReference 讓 demo 與框架**同步演進**，改 src 立即生效、可跨專案 debug / 重構 |
| **發佈** | （同上） | —— | 框架穩定 → 走 `~/.claude/rules/releasing.md` 發 NuGet 新版 |
| **畢業**（階段 8） | 獨立 repo `bee-northwind-avalonia` | **PackageReference** → 發佈的套件版本 | 外部視角的真實範例，只用公開套件 |

開發期的隔離原則：

- **不進 `Bee.Samples.slnx`**：`apps/Bee.Northwind/` 自帶 `Bee.Northwind.slnx`（ProjectReference 指 `../../src/Bee.*`），與 samples 的建置/CI 隔離。移走時 slnx 一起帶，只需把 ProjectReference 改 PackageReference。
- **CI 天然不涵蓋**：`build-ci.yml` 只在 `src/ tests/ slnx props sonar yml` 異動觸發 —— `apps/` 不在這些路徑，CI 不會建它（大型 demo app 不該拖慢框架 CI）。demo 正確性靠本機 build + `demo-smoke`；改 src 底層時 CI 仍建 src + tests 確保框架本身不壞。
- **版控但過渡**：`apps/Bee.Northwind/` 納入 bee-library git（保留開發歷史、可 push 備份）；畢業時 `git rm` 從 bee-library 移除、整個資料夾搬到新 repo。

## 目標

1. **核心論證**：開發者照 README 走完，體會「新增一張表單 = 新增幾個 XML 定義檔」，不寫 UI 與 CRUD 程式碼
2. **誠實展示業務碼的位置**：單號、狀態、驗證放 BO 覆寫，示範「定義驅動」與「pro-code 業務邏輯」的分工界線（與低程式碼平台的差異化論述）
3. **Dogfooding**：過程中逼出的框架缺口記錄到「框架回饋清單」，視情況改 src 底層（這正是開發期放 repo 內用 ProjectReference 的目的）

## 範圍

### 前端：只做 Avalonia

本 plan **只做 Avalonia 前端**（目前 bee-library 最完整的桌面前端，最適合把完整業務動線做扎實）。

> **多前端是框架天生能力，不是本 plan 工項**：`Bee.Northwind.Define`（FormSchema）與 `Bee.Northwind.Server`（JSON-RPC）本就前端無關 —— 未來 `bee-northwind-maui` 等是「複製 Define + Server、接上新前端」的接線工作、不需重寫業務邏輯。這個「一份定義餵多前端、各自零 CRUD 頁面」正是 bee 相對 ABP（每個 UI 仍各寫頁面）的差異化賣點，但每前端一獨立 repo、留待 Avalonia 版畢業後再展開。

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
- ❌ Blazor / MAUI 前端（框架能力，各自獨立 repo，另立 plan）
- ❌ 框架功能擴張（單號等先以應用層示範，驗證後另立 plan 決定是否上收框架）

## 專案結構

```
apps/
└── Bee.Northwind/                      # 開發期借住 bee-library；畢業移至 repo bee-northwind-avalonia
    ├── Bee.Northwind.slnx              # 獨立 slnx；ProjectReference → ../../src/Bee.*（不進 Bee.Samples.slnx）
    ├── Bee.Northwind.Define/           # FormSchema / FormLayout / TableSchema XML（demo 的「原始碼」）
    │   ├── FormSchema/                 #   Category, Supplier, Customer, Shipper, Employee, Product, Order
    │   ├── FormLayout/                 #   主檔 + 含 <Details> grid 的單據版面
    │   └── TableSchema/
    ├── Bee.Northwind.Server/           # JSON-RPC 後端：QuickStart.Server 模式 + 自訂 BO + 專屬 seeder
    │   └── BusinessObjects/            #   OrderBO（單號、狀態、驗證、金額）
    └── Bee.Northwind.Avalonia/         # 前端：選單導航 + FormView
```

建立時走 `bee-sample-add` skill 的決策樹（但目標路徑放 `apps/`、不接 `Bee.Samples.slnx`，改建自己的 `Bee.Northwind.slnx`）。

> **lookup 寫法直接抄 Project**：`samples/Define/Project.FormSchema.xml` 已是完整範例（relation 欄 `Visible="false"` + `RelationProgId` + `RelationFieldMappings`、明細 InCell lookup），`Bee.Northwind` 的 Product / Order 定義照搬即可。

## 階段內容

> 敘事主軸：階段 2→4 是**遞進的「零程式碼」里程碑** —— 純 CRUD → lookup 關連 → master-detail 單據，全部只靠 XML 定義；階段 5 才引入業務碼，清楚劃出分工界線。

### 階段 1：專案骨架

- 建立 `apps/Bee.Northwind/` 三件套 + `Bee.Northwind.slnx`（ProjectReference 指 `../../src/Bee.*`，不接 `Bee.Samples.slnx`）
- `Bee.Northwind.Server` 自帶 seeder（複用 `DemoSchemaSeeder` 模式），首啟自動建 SQLite + system tables + 業務表
- `Bee.Northwind.Define` 先放一張最小主檔（`Category`），Avalonia 端登入（demo/demo）→ CRUD 跑通
- **驗收**：`dotnet run` 兩個指令起整套系統，Category 可新增/編輯/刪除

### 階段 2：純主檔基礎資料（最純的定義驅動）

- `Category` / `Supplier` / `Customer` / `Shipper` 四組 FormSchema + FormLayout + TableSchema
- 此階段**刻意零 C# 業務碼、零 lookup**：四張純主檔只靠預設 `FormBusinessObject` 管線
- 種子：取 **Northwind 真實資料內容**子集（如類別 Beverages / Condiments…、客戶 ALFKI Alfreds Futterkiste…），欄位映射到 bee 結構 —— 讓熟悉 Northwind 的開發者一眼認出資料；約類別 8 筆、供應商 5 筆、客戶 10 筆、貨運商 3 筆
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

- `apps/Bee.Northwind/README.md`（雙語，依文件語言規則）：
  - 系統導覽 + 架構圖（定義檔 → Server → Avalonia 的資料流）+ Northwind → bee 模型對應表
  - **終章「30 分鐘加一張 Region 表單」**：讀者只新增 `Region.FormSchema.xml` + `FormLayout` + `TableSchema`，重啟即得完整 CRUD —— 這一章是整個 demo 的銷售論證
  - 「哪些是定義、哪些是業務碼」對照表（指向階段 5 的 OrderBO）
- **驗收**：使用者照終章操作一次走通

### 階段 8：畢業（移至獨立 repo）

- 確認框架穩定，依 `~/.claude/rules/releasing.md` 發 NuGet 新版（含本 demo 期間對 src 的所有底層改動）
- 建立獨立 repo `bee-northwind-avalonia`，整個 `apps/Bee.Northwind/` 搬入
- `Bee.Northwind.*.csproj` 的 ProjectReference（`../../src/Bee.*`）改為 PackageReference（指發佈版本）
- bee-library 內 `git rm apps/Bee.Northwind/`
- 新 repo README 補「需要的 Bee.* 套件版本」與 `dotnet restore` 即可跑的說明
- **驗收**：新 repo clone 下來、`dotnet restore` + `dotnet run` 起得來（純 NuGet 套件、無任何 ProjectReference）

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| Northwind schema 與 bee 模型落差（int FK / 複合鍵） | 採 bee 慣例鍵（見「Northwind → bee 模型對應」表）；不照搬原 schema |
| 自關連 lookup（Employee ReportsTo → Employee）首次實戰 | 理論上同 lookup 管線；階段 3 獨立驗證，踩雷則記框架回饋清單 |
| 單號/狀態做著做著想上收框架，範圍蔓延 | 本 plan 鎖死在應用層；缺口記錄到「框架回饋清單」，另立 plan 處理 |
| 開發期改 src 底層、與框架測試/其他 sample 衝突 | 改 src 時 CI 仍建 src + tests + 既有 samples（在觸發路徑內）把關；demo 本身本機驗證 |
| 畢業時 ProjectReference → PackageReference 漏改、殘留內部依賴 | 階段 8 驗收明訂「無任何 ProjectReference、純 NuGet 即可跑」 |
| 單據明細 InCell lookup 的 UI 動線 | 已在 Avalonia.Demo Project 表單實戰驗證；編譯過即交付使用者自測 |

## 框架回饋清單（執行中持續累積）

> Demo 過程發現的框架缺口記在這裡，完成後評估是否各自立 plan / 直接改 src（開發期 ProjectReference 即時生效）。

- （預期）單據編號序列生成器是否值得上收為框架服務
- （預期）FormField 驗證規則（Required / Min / Max）定義層支援
- （觀察）自關連 lookup 是否需要框架層特別處理（避免無限遞迴開窗等）
- （待發現）……

## 給執行 session 的 handoff 摘要

> 本 plan 預計**另起新 session 執行**。新 session 接手時的關鍵脈絡：

1. **位置與依賴**：demo 放 `apps/Bee.Northwind/`（不進 samples）、自帶 `Bee.Northwind.slnx`、ProjectReference 指 `../../src/Bee.*`、不進 CI；完成後（階段 8）發版 + 移至獨立 repo `bee-northwind-avalonia` 改 PackageReference
2. **業務案例 = Northwind 核心 8 表**，但鍵結構用 bee 慣例（見「Northwind → bee 模型對應」表）
3. **lookup 機制已完整落地於 main**（見 `docs/archive/plan-avalonia-lookup-edit.md`）：`DisplayFields` 複合顯示、`GetLookup` API、`LookupDialog`、主表 + 明細 InCell lookup 全可用
4. **現成範例**：`samples/Define/Project.FormSchema.xml`（lookup 定義）、`samples/Avalonia.Demo/`（前端接線）、`samples/Bee.Samples.Shared/DemoSchemaSeeder.cs`（建表 + 種子）
5. **相關 skill**：`bee-sample-add`（建三件套，注意目標放 apps/、自建 slnx）、`bee-scaffold-from-formschema`（從 FormSchema 產 sidecar）、`bee-add-bo-method`（OrderBO 若需新 action）、`demo-smoke`（端到端冒煙）
6. **協作慣例**：本機可 build/test → 直接改 main；UI 編譯過即交付使用者自測；每階段 commit 前 `./test.sh` 全綠（改了 src 底層更要全綠）
