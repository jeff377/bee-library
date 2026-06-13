# 計畫：Bee.Northwind 進銷存範例（Avalonia / 先 Desktop head）

**狀態：📝 擬定中（2026-06-13 命名 Bee.Northwind、框架系統表 st_ + 業務表 ft_ 分層、前端 UI + Desktop head、開發於 `apps/`、完成後畢業移獨立 repo）**

> **前置依賴已解除**：`plan-avalonia-lookup-edit`（Avalonia lookup 開窗機制）已於 2026-06-12 全階段完成並封存至 `docs/archive/`。本 plan 各階段不再有 lookup 阻塞。

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 專案骨架：`apps/Bee.Northwind/`（3 專案 Server / UI / Desktop + `Define/` 定義檔目錄）+ 獨立 slnx + ProjectReference，跑通最小主檔表單 | 📝 待做 |
| 2 | 業務純主檔：Category / Supplier / Customer / Shipper（ft_，零 lookup、零 C#）— 最純定義驅動 | 📝 待做 |
| 3 | 框架系統表 + 擴充：Department（複製框架定義）+ Employee（複製框架 st_employee + Northwind 欄位擴充，dept→Department lookup） | 📝 待做 |
| 4 | 業務 lookup 主檔：Product（Supplier + Category 雙 lookup）（仍零 C#） | 📝 待做 |
| 5 | 交易單據：Order + Order Details（master-detail + 三 lookup + 明細 InCell，仍零 C#） | 📝 待做 |
| 6 | 應用層業務：OrderBO 覆寫（單據編號、狀態、驗證、金額計算） | 📝 待做 |
| 7 | Avalonia 導航整合：選單式多表單切換、完整操作動線 | 📝 待做 |
| 8 | 教學文件：README + 「30 分鐘加一張 Region 表單」終章 | 📝 待做 |
| 9 | 畢業：發 NuGet 新版 → 整個 `apps/Bee.Northwind/` 移至獨立 repo `bee-northwind-avalonia`、ProjectReference 改 PackageReference | 📝 待做 |

## 背景

Bee.NET 的 `samples/` 有多個前端 demo，每個證明的是「該前端能接上框架」（技術可行性）。`Avalonia.Demo` 在 lookup plan 期間已含完整 lookup 動線，但定位仍是**框架能力的技術驗證**，不是「一套用定義方式建構的完整業務系統」。

對照 .NET 領域競品的說服力來源：XAF 靠 MainDemo / XCRM、ABP 靠 `Acme.BookStore`、Frappe 靠 ERPNext。Bee.NET 需要一個**多表單、相互關聯、含完整業務動線**的展示系統，讓開發者看到 FormSchema 作為定義中樞的網狀效果，**並呈現真實 ERP 開發的分層：框架自帶組織/權限系統表（`st_`）+ 業務表（`ft_`）擴充其上**。

### 為何採 Northwind 業務情境

**Northwind 是 .NET 開發者的「共同語言」** —— 腦中已有這套 schema 與業務情境，認知負擔近乎零，全副注意力放在「bee 的 FormSchema 怎麼表達關連」。

Northwind 關連網涵蓋 bee 的全部 lookup 能力，且自然對應「框架系統表 + 業務表」分層：

| Northwind 表 | bee ProgId（表） | 層級 | 展示的關連 |
|--------------|-----------------|------|-----------|
| —（框架自帶） | `Department`（`st_department`） | 框架系統表 | 純主檔；Employee 的 dept 來源 |
| Employees | `Employee`（`st_employee`） | 框架系統表 + 擴充 | **框架現成跨表 lookup**：dept → Department（+ 透過部門帶主管，多欄 mapping） |
| Categories | `Category`（`ft_category`） | 業務表 | 純主檔（lookup 來源） |
| Suppliers | `Supplier`（`ft_supplier`） | 業務表 | 純主檔（lookup 來源） |
| Customers | `Customer`（`ft_customer`） | 業務表 | 純主檔（lookup 來源） |
| Shippers | `Shipper`（`ft_shipper`） | 業務表 | 純主檔（lookup 來源） |
| Products | `Product`（`ft_product`） | 業務表 | 主表**雙 lookup**：Supplier + Category |
| Orders | `Order`（`ft_order`） | 業務表（master-detail） | 主表**三 lookup**：Customer + Employee + Shipper |
| Order Details | `Order` 的明細表（`ft_order_detail`） | 業務表 | 明細 **InCell lookup**：每列選 Product |

涵蓋：純主檔、雙/三 lookup、明細 InCell lookup、框架表↔業務表互指（Order → Employee）、框架現成的多欄 mapping（Employee → 部門 → 部門經理）。

### 命名：Bee.Northwind

- **`Northwind`** 是 .NET / 微軟生態公認的示範資料庫名 —— 一看就知是範例、非可安裝產品；直指業務案例。
- **`Bee` 前綴**對齊套件家族（`Bee.Base` / `Bee.Definition`…）。
- **獨立 repo 命名**：`bee-northwind-avalonia`（repo 名已標明 Avalonia 技術棧）。

### 框架系統表 vs 業務表（st_ / ft_）

bee 的資料表前綴**表「誰用」不表「哪個 DB」**：`st_` = 框架/系統層級表（框架自帶、跨應用通用）、`ft_` = 業務表（應用自定義）。本 demo 嚴守此分層：

- **框架系統表（`st_` 前綴）**：`Employee`（`st_employee`）、`Department`（`st_department`）。它們是框架認得的系統表（權限/組織等框架功能依賴其標準欄位），demo 的定義**必須保留框架標準欄位**、再視需要擴充業務欄位。框架 `src/Bee.Definition/Defaults/` 的標準定義是**「開新專案」的 scaffold 來源**（由 resource 載入鋪設），**不是 runtime fallback** —— demo 比照開新專案，把 Employee/Department 定義（以 Defaults 為複製起點 + 擴充）直接放進自己的 `Define/`。
  - 框架 `st_employee` 標準欄位：`sys_no` / `sys_rowid` / `sys_id` / `sys_name` / `dept_rowid`(→Department) / `user_rowid`(→User) —— 全部保留
  - 框架 `Employee.FormSchema` 已有現成 lookup：`dept_rowid → Department`，且**主管透過部門經理帶出**（`ref_supervisor` 來自部門 manager）—— demo 沿用此機制，**不另做員工自關連**
- **業務表（`ft_`，demo 自定義）**：`Category` / `Supplier` / `Customer` / `Shipper` / `Product` / `Order` / `Order Details`。

### Northwind → bee 模型對應（關鍵技術決策）

Northwind 是關聯式正規化 schema，bee 是 `sys_rowid`(Guid) 關連模型。**只借 Northwind 的業務案例與資料內容；資料結構（鍵、欄位、明細關連、表前綴）一律以 bee 框架為基準**：

| Northwind 結構 | bee 慣例對應 |
|---------------|-------------|
| 文字/int 主鍵（`CustomerID='ALFKI'`、`ProductID=17`） | `sys_id`(String 業務代碼) + `sys_rowid`(Guid 關連鍵) + `sys_no`(AutoIncrement 流水) |
| 名稱欄（`CompanyName` / `ProductName`） | `sys_name`；其餘屬性欄保留原意命名 |
| 外鍵（`Orders.CustomerID` → Customers） | `customer_rowid`(Guid) + `RelationProgId="Customer"` + `RelationFieldMappings`（帶出 `ref_customer_id` / `ref_customer_name`） |
| 複合主鍵明細（`Order Details`: OrderID+ProductID） | `sys_rowid` PK + `sys_master_rowid`(→Order) + `product_rowid`(lookup→Product) + qty / price |
| 員工（`Employees`） | **框架 `st_employee`**：沿用框架 6 欄（含 `dept_rowid`→Department / `user_rowid`），擴充 Northwind 純資料欄（`title` 職稱 / `hire_date` 到職日）。**主管 = 框架 dept→部門經理機制，不做 `ReportsTo` 員工自關連** |
| 部門（Northwind 無，框架有） | **框架 `st_department`**：定義從 Defaults 複製進 demo `Define/`，作為 Employee 的 dept lookup 來源 |

> **runtime 只讀 demo `Define/`、無覆蓋問題**：框架 `Defaults` 僅在「開新專案」時由 resource 鋪設初始定義，**runtime 不參與**。本 demo 比照開新專案 —— Employee/Department 的定義（從 `src/Bee.Definition/Defaults/` 複製標準結構為起點，Employee 再加 `title` / `hire_date`）直接放進 demo `Define/`，後端 `LocalDefineAccess` 只讀這份。前端 Avalonia 走 `RemoteDefineAccess`（帶快取、MVVM 友善）消費 Server 結果。沒有「demo 覆蓋 Defaults」「載入優先序」的問題。

### 能力盤點（2026-06-13，lookup 完成後重新確認）

- ✅ **多表 master-detail**：FormSchema 多 `FormTable` + `sys_master_rowid`，`DynamicForm` 自動渲染明細 grid
- ✅ **lookup 全套（已實戰驗證，桌面）**：`RelationProgId` + `RelationFieldMappings`、`DisplayFields`（複合顯示「編號 - 名稱」）、`FormSchema.LookupFields`、`FormLayoutGenerator` 涵蓋規則、後端 `GetLookup`（含 `GetLookupFilter()`）、Avalonia `LookupDialog` + 主表 ButtonEdit + 明細 InCell lookup。**直接複用 `samples/Define/Project.FormSchema.xml` 寫法**
- ✅ **框架系統表定義現成可抄**：`src/Bee.Definition/Defaults/` 有 `Employee` / `Department` 標準定義（含 Employee→Department lookup + 部門經理多欄 mapping），demo **複製進自己的 `Define/`** 為起點 + 擴充，不重新發明（Defaults 是 scaffold 來源、非 runtime）
- ✅ **GetNewData 骨架含 `ref_*`**（lookup 自測修復）：New 流程的 lookup 寫回顯示值能落地
- ✅ **Avalonia 可編輯表單全套**：`FormView` / `DynamicForm` / `GridControl`（InCell 與 EditForm 雙模式）/ 7 種 field editor
- ✅ **QuickStart 自動建表 + 種子**：`Bee.Samples.Shared.DemoSchemaSeeder` 的 `TableSchemaBuilder` + `DbCommandSpec` 模式可複用
- ⛔ **Avalonia 整合僅桌面驗證**：`LookupDialog` / `RowEditDialog` 用 `Window.ShowDialog`（多視窗）—— WASM / Mobile 無多視窗，跨平台需 `Bee.UI.Avalonia` 改 dialog 抽象。**故本 plan 只做 Desktop head**
- ⚠️ **框架無、需 demo 應用層示範**：單據編號生成、單據狀態與轉移檢查、表單層必填驗證 —— 正是要展示的「pro-code 業務邏輯」分工界線

## 依賴策略與搬遷生命週期

本 demo **不放 `samples/`**，而是放 `apps/` 獨立資料夾，以三段式生命週期演進。核心理由：**最終「獨立 repo + PackageReference」的 demo 是最強的銷售證明** —— 證明「外部開發者拿公開 NuGet 套件就能建出這套系統」；開發期則需要 ProjectReference 同步改框架。

| 階段 | 位置 | 框架依賴 | 為什麼 |
|------|------|---------|--------|
| **開發期**（階段 1–8） | `apps/Bee.Northwind/`（bee-library repo 內） | **ProjectReference** → `../../src/Bee.*` | dogfooding 過程可能改底層架構 —— ProjectReference 讓 demo 與框架同步演進 |
| **發佈** | （同上） | —— | 框架穩定 → 走 `~/.claude/rules/releasing.md` 發 NuGet 新版 |
| **畢業**（階段 9） | 獨立 repo `bee-northwind-avalonia` | **PackageReference** → 發佈的套件版本 | 外部視角的真實範例，只用公開套件 |

開發期隔離原則：

- **不進 `Bee.Samples.slnx`**：`apps/Bee.Northwind/` 自帶 `Bee.Northwind.slnx`（ProjectReference 指 `../../src/Bee.*`）。移走時 slnx 一起帶，只需把 ProjectReference 改 PackageReference。
- **CI 天然不涵蓋**：`build-ci.yml` 只在 `src/ tests/ slnx props sonar yml` 異動觸發 —— `apps/` 不在這些路徑。demo 正確性靠本機 build + `demo-smoke`；改 src 底層時 CI 仍建 src + tests 確保框架不壞。
- **版控但過渡**：納入 bee-library git；畢業時 `git rm` 移除、整個資料夾搬到新 repo。

## 資料庫與種子資料

- **資料庫：SQLite** —— 嵌入式檔案型、零安裝、跨平台，`QuickStart.Server` 已驗證。範例最高優先是「clone → `dotnet run` 即跑」。
- **`.db` 檔 gitignore、不進 repo**：二進位不該版控、資料應可重建。確認 repo 根 `.gitignore` 已含 `*.db`。
- **首啟自動建 + 灌**：`Bee.Northwind.Server` 首次跑時 seeder 從 `TableSchema` 建表（`st_` + `ft_`）+ 灌 Northwind 子集（沿用 `DemoSchemaSeeder` 模式）。
- **種子形式：JSON 資源檔 + seeder 讀取**：Northwind 子集放 `Bee.Northwind.Server/SeedData/*.json`（保留 Beverages / ALFKI… 真實內容），seeder 映射成 bee 結構 insert。
- **關連種子用 `sys_id` 引用**：JSON 關連欄填目標 `sys_id`，seeder **先建主檔拿 `sys_rowid`、再建關連表/明細時把 `sys_id` 解析成 `sys_rowid` 填入** —— 與 bee Guid 關連一致、JSON 仍人類可讀。
- **換 DB 是賣點**：`TableSchema` 驅動，換 SQL Server / PostgreSQL 只改 `DatabaseSettings`。README 順帶展示（demo 預設 SQLite）。

## 目標

1. **核心論證**：開發者照 README 走完，體會「新增一張表單 = 新增幾個 XML 定義檔」，不寫 UI 與 CRUD 程式碼
2. **真實 ERP 分層**：框架系統表（`st_`，沿用 + 擴充）與業務表（`ft_`，自定義）並存，呈現真實開發樣貌
3. **誠實展示業務碼的位置**：單號、狀態、驗證放 BO 覆寫，示範「定義驅動」與「pro-code 業務邏輯」的分工界線
4. **Dogfooding**：過程中逼出的框架缺口記錄到「框架回饋清單」，視情況改 src 底層

## 範圍

### 前端：Avalonia UI，先做 Desktop head

Avalonia 是**跨平台 UI 技術**（一份 UI + 各平台 head），非單一平台。本 plan 做 `Bee.Northwind.UI`（共用 UI）+ `Bee.Northwind.Desktop`（桌面 head）。

| 專案 | 角色 |
|------|------|
| `Define/`（定義檔目錄，**非專案**） | FormSchema / FormLayout / TableSchema XML（含擴充版 Employee）；Server 啟動以 `PathOptions.DefinePath` 指向它 |
| `Bee.Northwind.Server` | JSON-RPC 後端 + OrderBO + seeder（前端無關） |
| `Bee.Northwind.UI` | Avalonia 共用 UI：Views / ViewModels / App.axaml / 導航 |
| `Bee.Northwind.Desktop` | 桌面 head（薄進入點）—— **本 plan 實作** |
| `Bee.Northwind.Browser` / `Bee.Northwind.Mobile` | Web / App head —— **結構預留、本 plan 不實作** |

> **Web / Mobile head 不實作**：`LookupDialog` / `RowEditDialog` 用 `Window.ShowDialog`，**WASM 裡 `Window` 型別根本不存在**（single-view 模式），需 `Bee.UI.Avalonia` 改 single-view overlay（方向：抽 `IDialogPresenter`，或採 `DialogHost.Avalonia` / `Ursa` `OverlayDialog` / `FluentAvalonia` `ContentDialog`）。屬框架層工作、另立 plan。**注意**：Avalonia web 是 Skia 繪 canvas（非 HTML DOM），`FormView` / `DynamicForm` / `GridControl` 等 UI 在 web 原樣可跑、不需重寫，唯一卡點就是 dialog。

### 表單清單（框架表 2 + 業務表 7 + 教學終章 1）

| ProgId | 表（前綴） | 來源 | 展示重點 |
|--------|-----------|------|---------|
| `Department` | `st_department` | 複製框架 Defaults 進 `Define/` | 純主檔；Employee 的 dept 來源 |
| `Employee` | `st_employee` | 複製框架 Defaults + Northwind 擴充 | 保留框架 6 欄；擴充 `title` / `hire_date`；dept→Department lookup（部門經理帶主管） |
| `Category` | `ft_category` | 業務（自定義） | 純定義檔 CRUD，零 C# |
| `Supplier` | `ft_supplier` | 業務 | 純定義檔 CRUD |
| `Customer` | `ft_customer` | 業務 | 純定義檔 CRUD |
| `Shipper` | `ft_shipper` | 業務 | 純定義檔 CRUD |
| `Product` | `ft_product` | 業務 | 雙 lookup：Supplier + Category |
| `Order` | `ft_order` | 業務（master-detail） | 三 lookup（Customer/Employee/Shipper）+ 明細 InCell（Product）+ 單號/狀態 |
| （明細） | `ft_order_detail` | `Order` 明細 | 每列 Product lookup、數量、單價、金額 |
| `Region` | `ft_region` | 教學終章 | 讀者自己動手加，僅 XML、不寫 code |

### 明確不做（防止範圍蔓延）

- ❌ 庫存計算 / 庫存異動帳（需要 posting 機制，遠超 demo 範圍）
- ❌ 採購單（Northwind 無此案例；銷售 Order 已足夠展示單據模式）
- ❌ 員工自關連 `ReportsTo`（尊重框架模型，主管 = dept→部門經理）
- ❌ 簽核流程（框架無 workflow 層，單據僅做狀態欄位 + BO 轉移檢查）
- ❌ 報表引擎（查詢用既有 `FilterCondition` GetList 展示）
- ❌ Web(WASM) / Mobile head（結構預留；需先做 `Bee.UI.Avalonia` dialog 跨平台抽象，另立 plan）
- ❌ 框架功能擴張（單號等先以應用層示範，驗證後另立 plan 決定是否上收框架）

## 專案結構

```
apps/
└── Bee.Northwind/                      # 開發期借住 bee-library；畢業移至 repo bee-northwind-avalonia
    ├── Bee.Northwind.slnx              # 獨立 slnx（只含 3 個 .csproj）；ProjectReference → ../../src/Bee.*
    ├── Define/                         # 定義檔目錄（非專案，不進 slnx）；Server 以 PathOptions.DefinePath 指向
    │   ├── FormSchema/                 #   Employee(擴充), Category, Supplier, Customer, Shipper, Product, Order
    │   │                               #   Department/Employee 從框架 Defaults 複製進來（Employee 加擴充欄）
    │   ├── FormLayout/                 #   主檔 + 含 <Details> grid 的單據版面
    │   └── TableSchema/                #   st_employee(擴充) + ft_*（業務表）
    ├── Bee.Northwind.Server/           # JSON-RPC 後端（.csproj）
    │   ├── BusinessObjects/            #   OrderBO（單號、狀態、驗證、金額）
    │   └── SeedData/                   #   Northwind 子集 JSON（seeder 讀取灌入）
    ├── Bee.Northwind.UI/               # Avalonia 共用 UI：Views / ViewModels / App.axaml / 導航
    └── Bee.Northwind.Desktop/          # 桌面 head（薄進入點）— 本 plan 實作
        # （未來）Bee.Northwind.Browser / Bee.Northwind.Mobile — 結構預留、另立 plan
```

> **lookup 寫法直接抄 Project**：`samples/Define/Project.FormSchema.xml` 已是完整範例（relation 欄 `Visible="false"` + `RelationProgId` + `RelationFieldMappings`、明細 InCell lookup）。**框架 Employee/Department 定義抄 `src/Bee.Definition/Defaults/`**。

## 階段內容

> 敘事主軸：階段 2→5 是**遞進的「零程式碼」里程碑** —— 業務純主檔 → 框架系統表沿用+擴充 → 業務 lookup 主檔 → master-detail 單據，全靠 XML 定義；階段 6 才引入業務碼。

### 階段 1：專案骨架

- 建立 `apps/Bee.Northwind/` 的 **3 個 .csproj 專案**（Server / UI / Desktop head）+ `Define/` 定義檔目錄（非專案、不進 slnx）+ `Bee.Northwind.slnx`（ProjectReference 指 `../../src/Bee.*`，不接 `Bee.Samples.slnx`）
- Server runtime 只讀 demo `Define/`（含從框架 Defaults 複製進來的系統表定義 + 業務表）；seeder 首啟建表（`st_` + `ft_`）+ 種子。框架 `Defaults` 僅供「開新專案」scaffold、不在 runtime 路徑
- `Define/` 先放一張最小業務主檔（`Category`）的 FormSchema/FormLayout/TableSchema，Desktop 登入（demo/demo）→ CRUD 跑通
- **驗收**：`dotnet run`（Server + Desktop）起整套系統，Category 可新增/編輯/刪除

### 階段 2：業務純主檔（最純的定義驅動）

- `Category` / `Supplier` / `Customer` / `Shipper`（`ft_`）四組 FormSchema + FormLayout + TableSchema
- **刻意零 C# 業務碼、零 lookup**：四張純主檔只靠預設 `FormBusinessObject` 管線
- 種子：JSON 資源檔取 Northwind 真實內容子集（類別 8 / 供應商 5 / 客戶 10 / 貨運商 3）
- **驗收**：四張表單 CRUD 全通，diff 中只有 XML 與種子資料

### 階段 3：框架系統表沿用 + 擴充（框架自帶 + 業務擴充）

- `Department`：從框架 `Defaults` 複製定義進 demo `Define/`（保留框架結構）
- `Employee`：從框架 `Defaults` 複製為起點，**保留框架 6 欄**（`sys_*` / `dept_rowid`→Department / `user_rowid`）+ **擴充** Northwind 純資料欄（`title` 職稱 / `hire_date` 到職日）；框架原有的 `dept_rowid → Department` lookup（部門經理帶主管，多欄 mapping）一併帶入
- 仍**零 C# 業務碼**
- 種子：部門 4 筆、員工 5 筆（含部門歸屬）
- **驗收**：Employee 表單顯示框架欄 + 擴充欄；dept 開窗選 Department 帶出部門 + 部門經理；展示「框架自帶系統表 + 業務擴充」

### 階段 4：業務 lookup 主檔（lookup 關連零程式碼）

- `Product`（`ft_product`）：`supplier_rowid` + `category_rowid` **雙 lookup**
- 仍**零 C# 業務碼**：lookup 全靠定義（`RelationProgId` + `RelationFieldMappings` + `Visible="false"`）
- 種子：商品 15 筆
- **驗收**：Product 開窗選 Supplier/Category 顯示「編號 - 名稱」；存後重載 `ref_*` 由 server JOIN 重算

### 階段 5：交易單據（master-detail + 多 lookup + 明細 InCell）

- `Order`（`ft_order`）：主表三 lookup（Customer / Employee / Shipper）+ 明細表 `ft_order_detail`（每列 Product InCell lookup、數量、單價）
- 注意 `Order → Employee` 是**業務表指向框架表**的 lookup（業務員 = 框架員工）
- 定義套 `Project.FormSchema.xml` 範式；FormLayout 含 `<Details>` grid
- 仍**零 C# 業務碼**
- **驗收**：訂單可開窗選客戶/員工/貨運商、明細逐列選商品、master-detail 一次儲存、存後重載正確

### 階段 6：應用層業務（pro-code 分工界線展示）

- `OrderBO` 覆寫示範：
  - **單據編號**：`ORD-yyyyMM-NNN`，於 Save 時產生（含併發注意事項註記）
  - **狀態欄位**：Draft / Confirmed / Shipped enum dropdown，BO 內檢查合法轉移、Confirmed 後鎖明細
  - **必填驗證**：客戶、至少一筆明細、數量 > 0，違規拋帶訊息的業務例外
- 金額計算：明細 數量×單價（套 Discount）、主表總額彙總（BO 內算，不信任前端值）
- BO 跨層新增走 `bee-add-bo-method` skill（若需新 action）；單純覆寫 `Save` / `GetNewData` 則直接 override
- **驗收**：BO 層 xUnit 測試 + 前端操作違規時收到可讀錯誤訊息

### 階段 7：Avalonia 導航整合

- `Bee.Northwind.UI` 的 MainWindow 左側選單（基礎資料 / 組織 / 交易單據 分組）切換 FormView
- 操作動線打磨：開啟即列表、雙擊進編輯、工具列一致
- **驗收**：跑 `demo-smoke` 等級的端到端動線（登入 → 建客戶 → 建商品 → 開訂單選入 → 確認狀態 → 出貨）

### 階段 8：教學文件

- `apps/Bee.Northwind/README.md`（雙語）：
  - 系統導覽 + 架構圖（定義檔 → Server → UI/Desktop）+ Northwind → bee 模型對應表 + 框架表/業務表分層說明
  - **終章「30 分鐘加一張 Region 表單」**：讀者只新增 `Region.FormSchema.xml` + `FormLayout` + `TableSchema`，重啟即得完整 CRUD —— 整個 demo 的銷售論證
  - 「哪些是定義、哪些是業務碼、哪些是框架自帶」對照表
- **驗收**：使用者照終章操作一次走通

### 階段 9：畢業（移至獨立 repo）

- 確認框架穩定，依 `~/.claude/rules/releasing.md` 發 NuGet 新版（含本 demo 期間對 src 的所有底層改動）
- 建立獨立 repo `bee-northwind-avalonia`，整個 `apps/Bee.Northwind/` 搬入
- ProjectReference（`../../src/Bee.*`）改為 PackageReference（指發佈版本）
- bee-library 內 `git rm apps/Bee.Northwind/`
- 新 repo README 補套件版本與 `dotnet restore` 即可跑的說明
- **驗收**：新 repo clone 下來 `dotnet restore` + `dotnet run`（Desktop head）起得來（純 NuGet、無任何 ProjectReference）

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| Northwind schema 與 bee 模型落差（int FK / 複合鍵 / 表前綴） | 採 bee 慣例（見「Northwind → bee 模型對應」表）；框架表 st_（複製 Defaults 進 Define + 擴充）、業務表 ft_ 自定義 |
| 誤把 Web/Mobile head 拉進範圍 → dialog 跨平台工程拖垮 demo | 明確只做 Desktop head；Web/Mobile head 待 `Bee.UI.Avalonia` dialog 抽象另立 plan |
| 單號/狀態做著做著想上收框架，範圍蔓延 | 本 plan 鎖死在應用層；缺口記錄到「框架回饋清單」，另立 plan 處理 |
| 開發期改 src 底層、與框架測試/其他 sample 衝突 | 改 src 時 CI 仍建 src + tests + 既有 samples 把關；demo 本身本機驗證 |
| 畢業時 ProjectReference → PackageReference 漏改 | 階段 9 驗收明訂「無任何 ProjectReference、純 NuGet 即可跑」 |

## 框架回饋清單（執行中持續累積）

> Demo 過程發現的框架缺口記在這裡，完成後評估是否各自立 plan / 直接改 src（開發期 ProjectReference 即時生效）。

- （已知）`Bee.UI.Avalonia` 的 `LookupDialog` / `RowEditDialog` 用 `Window.ShowDialog`。WASM 裡 `Window` 型別根本不存在（single-view），跨平台需 dialog 改 single-view overlay（抽 `IDialogPresenter` 或採 `DialogHost.Avalonia` / `Ursa` `OverlayDialog` / `FluentAvalonia` `ContentDialog`）。本 demo 只做 Desktop 不觸及，Web/Mobile head 前置 plan 須先處理
- （觀察）「開新專案」從 `Defaults` 複製系統表定義到 `Define/` 的流程是否順手 —— 是否值得有個 skill / CLI 一鍵把指定框架系統表 scaffold 進專案的 `Define/`
- （預期）單據編號序列生成器是否值得上收為框架服務
- （預期）FormField 驗證規則（Required / Min / Max）定義層支援
- （待發現）……

## 給執行 session 的 handoff 摘要

> 本 plan 預計**另起新 session 執行**。新 session 接手時的關鍵脈絡：

1. **位置與依賴**：demo 放 `apps/Bee.Northwind/`（不進 samples）、自帶 `Bee.Northwind.slnx`、ProjectReference 指 `../../src/Bee.*`、不進 CI；完成後（階段 9）發版 + 移至獨立 repo `bee-northwind-avalonia` 改 PackageReference
2. **前端結構**：Avalonia UI + head —— `Bee.Northwind.UI`（共用）+ `Bee.Northwind.Desktop`（本 plan 唯一實作的 head）；Web/Mobile head 結構預留、實作另立 plan（卡在 dialog 跨平台抽象）
3. **資料庫 = SQLite**（`.db` gitignore、首啟 seeder 自動建表 + 灌）；種子 = `Bee.Northwind.Server/SeedData/*.json`，關連欄用 `sys_id` 引用、seeder 解析成 `sys_rowid`
4. **框架表 vs 業務表分層**：`Employee`(`st_employee`) / `Department`(`st_department`) 是**框架系統表**（`st_` 前綴、保留框架標準欄位）。**框架 `Defaults` 只是「開新專案」的 scaffold 來源（resource 載入）、runtime 不參與** —— demo 把 Employee/Department 定義從 `src/Bee.Definition/Defaults/` **複製進自己的 `Define/`**（Employee 加 `title` / `hire_date`），後端只讀 demo `Define/`、無覆蓋/優先序問題。主管 = 框架 dept→部門經理（**不做員工自關連**）。業務表 `ft_`（Category/Supplier/Customer/Shipper/Product/Order/OrderDetails）由 demo 自定義
5. **業務案例 = Northwind**，鍵結構用 bee 慣例（見「Northwind → bee 模型對應」表）
6. **lookup 機制已完整落地於 main**（見 `docs/archive/plan-avalonia-lookup-edit.md`）：`DisplayFields` 複合顯示、`GetLookup` API、`LookupDialog`、主表 + 明細 InCell lookup 全可用（桌面）
7. **現成範例**：`samples/Define/Project.FormSchema.xml`（lookup 定義）、`src/Bee.Definition/Defaults/`（框架 Employee/Department 定義）、`samples/Avalonia.Demo/`（前端接線）、`samples/Bee.Samples.Shared/DemoSchemaSeeder.cs`（建表 + 種子）
8. **相關 skill**：`bee-sample-add`（建專案，注意目標放 apps/、自建 slnx、UI+head 結構）、`bee-scaffold-from-formschema`（從 FormSchema 產 sidecar）、`bee-add-bo-method`（OrderBO 若需新 action）、`demo-smoke`（端到端冒煙）
9. **協作慣例**：本機可 build/test → 直接改 main；UI 編譯過即交付使用者自測；每階段 commit 前 `./test.sh` 全綠（改了 src 底層更要全綠）
