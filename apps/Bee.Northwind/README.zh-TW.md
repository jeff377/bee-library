# Bee.Northwind

[English](README.md)

一個示範範例 —— 經典的 **Northwind** 進銷存業務案例 —— 建構於 [Bee.NET](../../README.zh-TW.md) 框架之上，展示如何以定義組裝出一套應用。它存在的目的，是把一個論點變得具體：

> **一張具備完整新增／查詢／修改／刪除、清單瀏覽、跨表 lookup 的畫面，就是幾個 XML 定義檔 —— 不是 UI 程式、不是 CRUD 程式、不是 SQL。**

九張表單、含三個 lookup 的 master-detail 訂單、框架組織表，以及恰好一個手寫的業務物件（訂單規則）—— 其餘全是定義。終章帶你在大約三十分鐘內加上第十張表單，**一行程式碼都不寫**。

## 展示了什麼

- **定義驅動的 CRUD** —— `FormSchema` 是唯一真實來源，同時驅動 UI 表單、清單檢視、資料庫表與驗證面。
- **零程式碼的跨表 lookup** —— XML 裡一個關連欄位加上欄位對應，就得到挑選對話框、外鍵、以及反正規化的顯示欄（重載時由 server JOIN 重算）。
- **Master-detail 單據** —— 訂單帶一個明細表格，每列可挑選商品，整筆一次儲存、一次重載。
- **誠實畫出 pro-code 界線** —— 單據編號、狀態轉移、必填驗證、金額計算是全應用**唯一**的 C#，集中在一個 `OrderBO`。下方對照表精確標示哪些行為是定義、哪些是框架、哪些是應用程式碼。
- **框架系統表（`st_`）與業務表（`ft_`）並存** —— `Employee` / `Department` 是應用沿用並擴充的框架表；`Customer` / `Product` / `Order` 是應用自定義的業務表。

## 執行 demo

需要 **.NET 10 SDK**。資料庫為 SQLite，首次執行時自動建立並灌入種子 —— 免安裝。

### 從 VS Code（推薦）

開啟此 repository，在「執行與偵錯」下拉選 **Run Bee.Northwind (Server + Desktop)**，按 <kbd>F5</kbd>。它會一併建置並啟動 JSON-RPC 後端與桌面前端。

### 從命令列

在 repository 根目錄開兩個終端機：

```bash
# 1. 後端（JSON-RPC，http://localhost:5100）
dotnet run --project apps/Bee.Northwind/Bee.Northwind.Server

# 2. 桌面前端
dotnet run --project apps/Bee.Northwind/Bee.Northwind.Desktop
```

接著在 app 中：**Connect**（endpoint 已預填）→ 以 `demo` / `demo` **Sign in**。

> 首次執行 server 會在 server 專案旁建立 `northwind.db` 並灌入 Northwind 子集。刪除該檔即可重新建表灌種子。

## 表單清單

| 選單 | ProgId | 資料表 | 層級 | 重點 |
|------|--------|--------|------|------|
| Categories | `Category` | `ft_category` | 業務 | 純主檔，零程式碼 |
| Suppliers | `Supplier` | `ft_supplier` | 業務 | 純主檔 |
| Customers | `Customer` | `ft_customer` | 業務 | 純主檔 |
| Shippers | `Shipper` | `ft_shipper` | 業務 | 純主檔 |
| Products | `Product` | `ft_product` | 業務 | **雙 lookup**（Supplier + Category） |
| Departments | `Department` | `st_department` | 框架系統 | 沿用的框架表 |
| Employees | `Employee` | `st_employee` | 框架系統 + 擴充 | 框架欄位 + `title` / `hire_date`；`dept` lookup 一併帶出部門經理作為主管 |
| Orders | `Order` | `ft_order` + `ft_order_detail` | 業務（master-detail） | **主表三 lookup**（Customer / Employee / Shipper）+ 每列**商品 lookup**；唯一的 `OrderBO` |

## 框架系統表 vs 業務表（`st_` / `ft_`）

資料表前綴表示**誰擁有這張表，而非它落在哪個資料庫**：

- **`st_` —— 框架／系統表。** 由框架提供、跨應用共用、被框架功能（權限、組織）依賴。`Employee`（`st_employee`）與 `Department`（`st_department`）是框架表。應用把它們的定義從框架預設複製進自己的 `Define/`（與開新專案 scaffold 的方式相同），保留標準欄位，再**擴充** —— `Employee` 加上 `title` 與 `hire_date`。
- **`ft_` —— 業務表。** 由本應用定義：`Category`、`Supplier`、`Customer`、`Shipper`、`Product`、`Order`、`Order Details`。

`Order → Employee` 是有趣的跨層連線：業務表（`ft_order`）指向框架系統表（`st_employee`）—— 訂單上的業務員就是框架的員工。

## Northwind → bee 模型對應

Northwind 是正規化的關聯式 schema；bee 是 `sys_rowid`（Guid）關連模型。本 demo 借用 Northwind 的業務案例與資料，但鍵與關連一律遵循 bee 慣例：

| Northwind | bee 慣例 |
|-----------|----------|
| 文字／int 主鍵（`CustomerID='ALFKI'`、`ProductID=17`） | `sys_id`（字串業務代碼）+ `sys_rowid`（Guid 關連鍵）+ `sys_no`（流水號） |
| 名稱欄（`CompanyName`、`ProductName`） | `sys_name` |
| 外鍵（`Orders.CustomerID`） | `customer_rowid`（Guid）+ `RelationProgId="Customer"` + 欄位對應，帶出 `ref_customer_id` / `ref_customer_name` |
| 複合主鍵明細（`Order Details`：OrderID+ProductID） | `sys_rowid` 主鍵 + `sys_master_rowid`（→ Order）+ `product_rowid`（lookup → Product）+ 數量／單價 |
| 員工 | 框架 `st_employee`：框架欄位 + Northwind 資料欄；主管來自部門，而非 `ReportsTo` 員工自關連 |

## 哪些是定義、哪些是框架、哪些是應用程式碼

整個論點濃縮成一張表。

| 行為 | 來源 | 位置 |
|------|------|------|
| 表單版面、欄位編輯器、標籤 | **定義** | `FormSchema`（版面由框架自動產生） |
| 清單欄位與瀏覽 | **定義** | `FormSchema.ListFields` |
| 資料庫表 + 索引 | **定義** | `TableSchema` |
| 新增／修改／刪除分派 | **框架** | `FormBusinessObject` + repository |
| Lookup 對話框、外鍵寫回、JOIN 重載 | **定義 + 框架** | 關連欄位 + `RelationFieldMappings`；框架 `GetLookup` |
| Master-detail 整筆一次儲存 | **框架** | repository，由多表 `FormSchema` 驅動 |
| 導航選單（分組表單清單） | **定義** | `ProgramSettings.xml` |
| 登入／工作階段／加密 | **框架** | `SystemBusinessObject`、API 管線 |
| **單據編號、狀態轉移、驗證、金額** | **應用程式碼** | `OrderBO`（全應用唯一的業務邏輯） |

唯一的 C# 業務物件 [`OrderBO`](Bee.Northwind.Server/BusinessObjects/OrderBO.cs) 覆寫 `Save` / `GetNewData`，補上一般表單無法表達的規則。其純規則（[`OrderRules`](Bee.Northwind.Server/BusinessObjects/OrderRules.cs)、[`OrderDataSet`](Bee.Northwind.Server/BusinessObjects/OrderDataSet.cs)）在 [`Bee.Northwind.Server.Tests`](Bee.Northwind.Server.Tests) 有單元測試。

## 終章：三十分鐘加一張 Region 表單，零程式碼

Northwind 有一張 `Region` 表，demo 刻意沒做 —— 留給你自己加。你會寫**三個 XML 檔加一行選單，全是定義、零程式碼**，重啟後就得到一張完整可用的 CRUD 畫面。

### 1. 資料表 —— `Define/TableSchema/common/ft_region.TableSchema.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<TableSchema xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" TableName="ft_region" DisplayName="Region">
  <Fields>
    <DbField FieldName="sys_no" Caption="Sequence" DbType="AutoIncrement" />
    <DbField FieldName="sys_rowid" Caption="Row ID" DbType="Guid" />
    <DbField FieldName="sys_id" Caption="Region Code" DbType="String" Length="20" />
    <DbField FieldName="sys_name" Caption="Region Name" DbType="String" Length="50" />
  </Fields>
  <Indexes>
    <DbTableIndex Name="pk_{0}" Unique="true" PrimaryKey="true">
      <IndexFields><IndexField FieldName="sys_no" /></IndexFields>
    </DbTableIndex>
    <DbTableIndex Name="rx_{0}" Unique="true">
      <IndexFields><IndexField FieldName="sys_rowid" /></IndexFields>
    </DbTableIndex>
    <DbTableIndex Name="uk_{0}" Unique="true">
      <IndexFields><IndexField FieldName="sys_id" /></IndexFields>
    </DbTableIndex>
  </Indexes>
</TableSchema>
```

### 2. 表單 —— `Define/FormSchema/Region.FormSchema.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<FormSchema xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" ProgId="Region" DisplayName="Region" CategoryId="common" ListFields="sys_id,sys_name">
  <Tables>
    <FormTable TableName="Region" DbTableName="ft_region" DisplayName="Region">
      <Fields>
        <FormField FieldName="sys_no" Caption="Sequence" DbType="AutoIncrement" Visible="false" />
        <FormField FieldName="sys_rowid" Caption="Row ID" DbType="Guid" Visible="false" />
        <FormField FieldName="sys_id" Caption="Region Code" DbType="String" />
        <FormField FieldName="sys_name" Caption="Region Name" DbType="String" />
      </Fields>
    </FormTable>
  </Tables>
</FormSchema>
```

不需要寫 `FormLayout` 檔 —— 框架會在交付時從 `FormSchema` 自動產生版面。

### 3. 註冊資料表 —— 加到 `Define/DbCategorySettings.xml`

```xml
<TableItem TableName="ft_region" DisplayName="Region" />
```

這讓 seeder 在下次啟動時建立此表（它會建立此處註冊的每一張表）。

### 4. 放上選單 —— 加到 `Define/ProgramSettings.xml`

```xml
<ProgramItem ProgId="Region" DisplayName="Regions" />
```

`ProgramSettings.xml` **就是**應用的程式清單 —— 導航選單由它建立，新項目會出現在左側選單。（沒有 `BusinessObject` 屬性，代表使用框架預設 CRUD。）

### 5. 重啟

重啟 server（它會建立 `ft_region`）與桌面前端。**Regions** 現在出現在左側選單的 Master Data 之下，具備可用的清單、新增、修改、刪除，以及來自 `uk_` 索引的唯一代碼檢查 —— 全部來自四處定義修改，不編譯你自己的任何程式碼。

## 專案結構

```
apps/Bee.Northwind/
├── Define/                       定義 —— 真實來源（非專案，由 server 讀取）
│   ├── FormSchema/               每張表單一個檔
│   ├── TableSchema/common/       每張表一個檔
│   ├── DbCategorySettings.xml    有哪些表（驅動建表）
│   └── ProgramSettings.xml       程式清單（驅動選單 + BO 綁定）
├── Bee.Northwind.Server/         JSON-RPC 後端、OrderBO、JSON 種子資料
├── Bee.Northwind.Server.Tests/   訂單規則的 xUnit 測試
├── Bee.Northwind.UI/             Avalonia 共用 UI（views、view models、導航）
└── Bee.Northwind.Desktop/        桌面進入點
```

詳細計畫與持續累積的框架回饋清單見 [`docs/plans/plan-bee-northwind-demo.md`](../../docs/plans/plan-bee-northwind-demo.md)。
