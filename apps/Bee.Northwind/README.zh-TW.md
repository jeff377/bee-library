# Bee.Northwind

[English](README.md)

一個示範範例 —— 經典的 **Northwind** 進銷存業務案例 —— 建構於 [Bee.NET](../../README.zh-TW.md) 框架之上，展示如何以定義組裝出一套應用。它存在的目的，是把一個論點變得具體：

> **一張具備完整新增／查詢／修改／刪除、清單瀏覽、跨表 lookup 的畫面，就是幾個 XML 定義檔 —— 不是 UI 程式、不是 CRUD 程式、不是 SQL。**

八張表單、含三個 lookup 的 master-detail 訂單、框架組織表，以及恰好一個手寫的業務物件（訂單規則）—— 其餘全是定義。

## 展示了什麼

- **定義驅動的 CRUD** —— `FormSchema` 是唯一真實來源，同時驅動 UI 表單、清單檢視、資料庫表與驗證面。
- **零程式碼的跨表 lookup** —— XML 裡一個關連欄位加上欄位對應，就得到挑選對話框、外鍵、以及反正規化的顯示欄（重載時由 server JOIN 重算）。
- **Master-detail 單據** —— 訂單帶一個明細表格，每列可挑選商品，整筆一次儲存、一次重載。
- **自訂業務邏輯物件** —— 單據編號、狀態轉移、必填驗證、金額計算是全應用**唯一**的 C#，集中在一個 `OrderBO`；下方對照表精確標示哪些行為屬定義、哪些屬框架、哪些屬應用程式碼。
- **框架系統表（`st_`）與業務表（`ft_`）並存** —— `Employee` / `Department` 是應用沿用並擴充的框架表；`Customer` / `Product` / `Order` 是應用自定義的業務表。
- **在地化標題與租戶客製層** —— 訂單表單的 zh-TW 標題來自語系資源，客製層再改掉其中兩個、其餘照樣繼承。兩者都是定義檔，都不是程式碼。

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

### 網頁前端（Avalonia WASM）

同一套 UI 也能透過 **Avalonia Browser** head 在瀏覽器執行 —— 相同的 `App`、view model、view
編譯成 WebAssembly。需要 `wasm-tools` workload（`sudo dotnet workload install wasm-tools`）與上面
執行中的 server，然後：

```bash
# 網頁前端 dev server（Avalonia WASM，http://localhost:5200）
dotnet run --project apps/Bee.Northwind/Bee.Northwind.Browser
```

開啟 <http://localhost:5200/>，以相同方式連線 / 登入。WASM 專屬接線（localStorage endpoint、
async 連線、overlay 對話框、publish 注意事項）見
[`Bee.Northwind.Browser/README.md`](Bee.Northwind.Browser/README.md)。

### 行動前端（Avalonia iOS / Android）

同一套 UI 也能以 Avalonia single-view head 在 iOS 與 Android 上執行，並連同上面執行中的 server。
下方預設用 **Debug** 跑（免簽章、迭代快）。Release 的 trim/AOT 序列化相容性**已解並驗證** ——
`Bee.Definition` 內隨套件附上 `ILLink.Descriptors.xml`，在 full trim 下保留定義型別圖，已在
Android 模擬器（full trim）與 iOS 模擬器（強制 reflection-only path，等同 device AOT）驗證通過；
iOS 上實機則另需 Apple Developer 簽章憑證。畫面會響應式重排 —— 窄螢幕下表單
單欄、清單卡片化 —— 且 Android 硬體 / 手勢返回鍵會先退記錄 → 關分頁，才退出 app。

```bash
# iOS 模擬器（需 ios workload + Xcode；先啟動一個模擬器）
dotnet build apps/Bee.Northwind/Bee.Northwind.iOS -t:Run -f net10.0-ios -c Debug

# Android 模擬器（需 Android SDK + JDK 17；先啟動一個 AVD）
dotnet build apps/Bee.Northwind/Bee.Northwind.Android -t:Run -f net10.0-android -c Debug
```

在 **Android 模擬器**，主機要用 `10.0.2.2`（非 `localhost`），endpoint 填 `http://10.0.2.2:5100/api`；
manifest 已開 dev 明文 HTTP。在 **iOS 模擬器**則用 `http://localhost:5100/api`（ATS 於 dev 允許任意連線）。

> 首次執行 server 會在 server 專案旁建立 `northwind.db` 並灌入 Northwind 子集。刪除該檔即可重新建表灌種子。

## 執行畫面

四個 Avalonia head 渲染同一張訂單表單 —— 同一份定義、同一套控件，差別只在最外層的平台殼。

**桌面與 Browser（WASM）：**

| 桌面 | Browser |
|---|---|
| ![桌面 — 訂單單筆](https://raw.githubusercontent.com/jeff377/blog-images/main/avalonia-mobile-frontend-desktop-order-detail.png) | ![Browser — 訂單單筆](https://raw.githubusercontent.com/jeff377/blog-images/main/avalonia-mobile-frontend-browser-order-detail.png) |

**iOS 與 Android：**

| | iOS | Android |
|---|---|---|
| **訂單清單** | ![iOS — 訂單清單](https://raw.githubusercontent.com/jeff377/blog-images/main/avalonia-mobile-frontend-ios-order-list.png) | ![Android — 訂單清單](https://raw.githubusercontent.com/jeff377/blog-images/main/avalonia-mobile-frontend-android-order-list.png) |
| **訂單單筆** | ![iOS — 訂單單筆](https://raw.githubusercontent.com/jeff377/blog-images/main/avalonia-mobile-frontend-ios-order-detail.png) | ![Android — 訂單單筆](https://raw.githubusercontent.com/jeff377/blog-images/main/avalonia-mobile-frontend-android-order-detail.png) |

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

### 落在哪個資料庫（`common` / `company` / `log`）

前綴表示誰**擁有**一張表；另一個獨立的維度表示它落在哪個**資料庫**。`FormSchema` 的 `CategoryId` 選擇資料庫 scope，只有三個值：

- **`company`** —— 各公司獨立的業務資料：`ft_` 表，以及組織表 `st_department` / `st_employee`（一家應用的員工屬於該公司）。router 透過 session 的公司解析到公司資料庫。
- **`common`** —— 跨公司共用的框架表：使用者（`st_user`）、工作階段、cache-notify 訊號、定義儲存、公司與 API 金鑰。非應用資料。
- **`log`** —— 稽核軌跡：登入、資料異動、檢視、API 與資料庫異常各一張表。它在正式部署通常獨立一個資料庫，因為成長曲線與業務資料不同、讀它的人也不同。

**兩個維度是正交的**：`st_department` / `st_employee` 是框架擁有的表（`st_` 前綴），卻落在 **company** 資料庫，因為一家公司的員工就是那家公司的資料。把「誰擁有」和「放哪裡」綁成同一件事，在多公司部署上會撞牆。

三個分類都登錄在 `Define/DbCategorySettings.xml`，seeder 照著它逐張建表 —— 加一張表因此是純 XML。

本 demo 是單公司，所以三個資料庫都指向同一個 `northwind.db` 檔。**分類是框架路由的依據，所以日後要把稽核或某家公司拆到獨立資料庫，改的只有 `DatabaseSettings.xml` 一個檔，一份表單定義都不必動。**

### 登入是兩步，而本示範兩步都走

登入要回答兩個問題，框架把它們分開問。`Login` 回答**你是誰**；`EnterCompany` 回答**你在哪一家公司**，並填上那半個必須先知道公司才推導得出來的 session —— 客製化代碼、角色、以及 record-scope 的列識別。本示範只有一家公司，所以登入成功後直接自動進入；有好幾家的部署在兩次呼叫之間插一個選單，其餘一律不變。

**兩步都跑在框架程式碼上。** 應用沒有替換任何服務，也沒有覆寫任何方法：

- **認證**走框架自己的 `st_user` 檢查。seeder 在首次啟動時把 `demo` 帳號寫進 `st_user`，密碼以 `PasswordHasher` 現算雜湊存入（不是寫死的雜湊值，否則換一次雜湊參數就對不上）。比對帳號密碼在每個部署都一樣，所以那件事屬於框架。
- **進公司**走框架自己的 `EnterCompany`。seeder 寫入對應的 `st_company` 與 `st_user_company` 兩列，該呼叫接著驗公司存在且啟用、查使用者的存取權，再把角色與員工脈絡快照到 session 上。

`st_user` 那一列同時帶著 `time_zone` 與 `culture`，於是 session 的時區取自**使用者**而不是伺服器或部署預設值。

> **只有一家公司不是跳過第二步的理由。** 本示範早期版本走過那條捷徑 —— 在覆寫的 `Login` 裡直接蓋上 `SessionInfo.CompanyId`，不呼叫 `EnterCompany` —— 而它付出的比省下的多，且兩筆代價都是無聲的。其一，以公司為鍵的查找（例如下面的 per-form 稽核規則）是透過 `st_company` 解出公司的，那張表沒有列就一律回「沒有規則」，每一條規則因此都不生效。其二，`EnterCompany` 會把公司寫進 `st_session` 的種子，而**應用自己做不到這件事**；少了它，公司只活在快取裡，伺服器一重啟，客戶端拿回的就是一個認證得過、卻連一張公司分類表單都打不開的 session。

### 稽核：零應用程式碼

稽核在 `SystemSettings.xml` 開啟之後，**每一次登入成功、失敗與鎖定都會自動落進 `st_log_login`**，同樣零應用程式碼：框架的 `Login` 本身就在三個分支各寫一次。demo 把 `UseBackgroundWriter` 設為 `false`，記錄在登入回傳的當下就看得到；正式部署維持預設的批次寫入。

**要稽核哪些表單是逐張決定的，不是一個開關管全部。** `SystemSettings.xml` 訂部署層預設——本示範記異動、不記檢視——而 `st_audit_rule` 逐張覆寫它。seeder 植入三筆規則讓機制看得見：`Order` 與 `Customer` 把檢視記錄**打開**（壓過部署預設）並標為敏感；`Category` 把異動記錄**關掉**，因為參考資料的異動在稽核軌跡裡只是雜訊。其餘表單沒有規則列，直接沿用預設。規則存在公司資料庫，由 Administration 下的 **Audit Rules** 表單維護。

> 本示範的 `AuditRule.FormSchema.xml` 拿掉了框架版宣告的 `PermissionModelId`，因為 Northwind 完全沒有權限基礎設施，而 enforcement 是 fail-closed——留著會讓這張表單誰都打不開。**真實部署應保留它**，並把該模型授權給管理者角色：能編輯這些規則的人，等於能決定什麼被記錄、什麼不被記錄。

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
| progId 對業務物件與 Repository 的綁定 | **定義** | `ProgramSettings.xml`（型別註冊表） |
| 導航選單（分組表單清單） | **定義** | `MenuSettings.xml` |
| 在地化標題與顯示名稱 | **定義** | `Define/Language/{lang}/{progId}.Language.xml` |
| 各租戶的標題覆寫 | **定義** | `Customize/{customizeId}/Language/…`，逐 key 決定 |
| 登入／工作階段／加密 | **框架** | `SystemBusinessObject`、API 管線 |
| **單據編號、狀態轉移、驗證、金額** | **應用程式碼** | `OrderBO`（全應用唯一的業務邏輯） |

唯一的 C# 業務物件 [`OrderBO`](Bee.Northwind.Server/BusinessObjects/OrderBO.cs) 覆寫 `Save` / `GetNewData`，補上一般表單無法表達的規則。其純規則拆到 [`OrderRules`](Bee.Northwind.Server/BusinessObjects/OrderRules.cs) 與 [`OrderDataSet`](Bee.Northwind.Server/BusinessObjects/OrderDataSet.cs)，不依賴資料庫、與協調流程分離。

它的兩個資料庫查詢放在 [`IOrderRepository`](Bee.Northwind.Server/Repositories/IOrderRepository.cs) / [`OrderRepository`](Bee.Northwind.Server/Repositories/OrderRepository.cs)，與業務物件綁在**同一筆**註冊表項目上 —— 一支程式、一個業務物件、一個 Repository。這是「表單需要產生式 CRUD 以外的資料存取」時的樣式範本：**擴充** `IDataFormRepository` 而非取代它、衍生自 `DataFormRepository`，BO 端以介面取得（`CreateFormRepository<IOrderRepository>()`）。把 SQL 移出業務物件，也正是這兩個查詢得以路由到訂單自己的公司資料庫、而非業務物件當初隨手指名那個資料庫的原因。

## 在地化與租戶客製層

訂單表單的標題有兩套來源。英文那套內嵌在 `FormSchema` 的 `Caption`，所以英文根本不需要語系檔
—— 查不到 key 時 schema 自己的字原樣留著。zh-TW 那套來自
`Define/Language/zh-TW/Order.Language.xml`，key 的慣例到處都一樣（`Schema.DisplayName`、
`Table.{表}.DisplayName`、`Field.{欄位}.Caption`）。

客製層疊在它上面。demo 公司指名了一個客製化代碼（`NorthwindCredentials.CustomizeId`），
登入時被抄進 session，之後每次定義查詢都會**先**看 `Customize/{customizeId}/`、再看套裝的
`Define/`。這個租戶把客戶叫做「經銷商」，所以它的資源只宣告兩個 key：

```xml
<LanguageItem Key="Field.customer_rowid.Caption" Value="經銷商" />
<LanguageItem Key="Field.ref_customer_name.Caption" Value="經銷商名稱" />
```

表單上其餘欄位照樣解析到套裝資源 —— **語系文字是逐 key 覆蓋，不是整檔取代** ——
所以套裝日後新增的標題，不必動客製檔就會傳到這個租戶。版面與選單則相反（整檔取代），
因為視覺編排做局部合併沒有直覺上的正解。

有兩個彼此獨立的開關管著這一層，**清掉任一個就回到純套裝部署、其餘行為完全不變**：
session 的客製化代碼，以及 [`NorthwindBackend`](Bee.Northwind.Server/NorthwindBackend.cs) 裡的
`PathOptions.CustomizePath`。公司對客製化代碼是**多對一**，所以「多家公司共用一份客製」才是
常態，demo 只是剛好各一。

把這些組裝起來是 client 的工作、不是 server 的：API 一律把定義原樣送出，由
`FormDefinitionLoader` 取回兩層、套用疊加，再把在地化後的 schema 交給畫面。這也是
[`FormWorkspace`](Bee.Northwind.UI/Controls/FormWorkspace.cs) 兩個畫面都要給 loader 的原因
—— 沒有 loader 的畫面只會拿到原樣的 schema、英文標題、以及自動產生的版面。

## 終章：三十分鐘加一張 Region 表單，零程式碼

Northwind 有一張 `Region` 表，demo 刻意沒做 —— 留給你自己加。你會寫**三個 XML 檔加一行選單，全是定義、零程式碼**，重啟後就得到一張完整可用的 CRUD 畫面。

### 1. 資料表 —— `Define/TableSchema/company/ft_region.TableSchema.xml`

Region 是業務資料,所以放在 **company** 分類(`TableSchema/company/`),與其他 `ft_` 表一起。

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
<FormSchema xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" ProgId="Region" DisplayName="Region" CategoryId="company" ListFields="sys_id,sys_name">
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

同時要寫對應的 `FormLayout/ft_region.FormLayout.xml`。執行階段渲染的就是這個檔，**檔案不存在會失敗** —— 框架不再從 `FormSchema` 推導版面。`FormLayoutGenerator` 可在設計階段依 schema 產生一份初稿，產出後存檔，之後就跟其他定義檔一樣編輯。

### 3. 註冊資料表 —— 加到 `Define/DbCategorySettings.xml` 的 company 分類

```xml
<TableItem TableName="ft_region" DisplayName="Region" />
```

這讓 seeder 在下次啟動時建立此表（它會把此處註冊的每一張表，建到該分類對應的資料庫）。

### 4. 註冊程式 —— 加到 `Define/ProgramSettings.xml`

```xml
<ProgramItem ProgId="Region" DisplayName="Regions" />
```

`ProgramSettings.xml` 是型別註冊表：把 progId 對應到綁定於它的型別 —— 一個商業物件與一個 Repository。（兩個屬性都沒有，代表使用框架預設 CRUD；本專案除 `Order` 外皆是如此。）

### 5. 放上選單 —— 加到 `Define/MenuSettings.xml`

```xml
<MenuEntry Id="region" Caption="Regions" Order="60" ProgId="Region" />
```

放進 `master-data` 資料夾內。`Id` 是節點的 key、需全樹唯一；它與 `ProgId` 分離，因此同一支程式可以出現在選單的多個位置。

### 6. 重啟

重啟 server（它會建立 `ft_region`）與桌面前端。**Regions** 現在出現在左側選單的 Master Data 之下，具備可用的清單、新增、修改、刪除，以及來自 `uk_` 索引的唯一代碼檢查 —— 全部來自五處定義修改，不編譯你自己的任何程式碼。

## 專案結構

```
apps/Bee.Northwind/
├── Define/                       定義 —— 真實來源（非專案，由 server 讀取）
│   ├── FormSchema/               每張表單一個檔
│   ├── TableSchema/{common,company,log}/  一個分類一個資料夾
│   ├── DatabaseSettings.xml      common + company + log 三個資料庫
│   ├── DbCategorySettings.xml    各分類有哪些表（驅動建表）
│   ├── ProgramSettings.xml       型別註冊表（progId 對業務物件 + Repository）
│   ├── MenuSettings.xml          導航選單（資料夾、排序、標題）
│   └── Language/{lang}/          在地化標題，每個 progId 一個檔
├── Customize/{customizeId}/      租戶客製層（結構與 Define/ 相同）
├── Bee.Northwind.Server/         JSON-RPC 後端、OrderBO、JSON 種子資料
├── Bee.Northwind.UI/             Avalonia 共用 UI（views、view models、導航）
├── Bee.Northwind.Desktop/        桌面進入點（Avalonia.Desktop）
├── Bee.Northwind.Browser/        網頁進入點（Avalonia WASM）
├── Bee.Northwind.iOS/            iOS 進入點（Avalonia.iOS，Release trim 已驗證）
└── Bee.Northwind.Android/        Android 進入點（Avalonia.Android，Release trim 已驗證）
```
