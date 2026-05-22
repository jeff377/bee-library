# BeeNET 框架專有名詞中英文對照表

[English](terminology.md)

本文件為技術文件撰寫的標準用語參考，確保中英文名稱一致。

---

## 目錄

1. [架構模式與核心概念](#1-架構模式與核心概念)
2. [表單結構定義層（Bee.Definition）](#2-表單結構定義層beedefinition)
3. [資料庫層（Bee.Db）](#3-資料庫層beedb)
4. [業務邏輯層（Bee.Business）](#4-業務邏輯層beebusiness)
5. [Repository 層（Bee.Repository）](#5-repository-層beerepository)
6. [API 層（Bee.Api.Core / Bee.Api.AspNetCore）](#6-api-層beeapicore--beeapiaspnetcore)
7. [快取層（Bee.ObjectCaching）](#7-快取層beeobjectcaching)
8. [連線層（Bee.Api.Client）](#8-連線層beeapiclient)
9. [基礎設施（Bee.Base）](#9-基礎設施beebase)
10. [列舉型別（Enumerations）](#10-列舉型別enumerations)
11. [系統欄位（System Fields）](#11-系統欄位system-fields)
12. [設定檔（Configuration Files）](#12-設定檔configuration-files)
13. [前端層（Bee.UI.* / Bee.Web.Blazor.*）](#13-前端層beeui--beewebblazor)

---

## 1. 架構模式與核心概念

| 英文名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| Definition-Driven Architecture | 定義導向架構 | BeeNET 核心架構模式，以結構定義統一驅動 UI、資料庫與業務邏輯 |
| Single Source of Truth | 唯一定義來源 | `FormSchema` 作為系統唯一結構規格，避免三層重複實作 |
| NoCode | 零程式碼 | 完全由 `FormSchema` 自動產生，無需撰寫程式碼 |
| LowCode | 低程式碼 | 以 `FormSchema` 為基礎，搭配少量覆寫擴充行為 |
| AnyCode | 全程式碼 | 完全由開發者自行實作，不受 `FormSchema` 驅動 |
| Master-Detail Pattern | 主從資料模式 | 一筆主檔（Master）對應多筆明細（Detail）的資料關聯結構 |
| Repository Dual-Track Strategy | Repository 雙軌策略 | CRUD 由 `FormSchema` 驅動；報表 / 批次由 BO 自行實作（AnyCode） |
| FormMap | 表單映射 | Bee.Db 採用的資料庫存取模式，以 `FormSchema` 為單位描述業務實體並動態產生 SQL；與 ORM 平行的設計（[詳見](formmap.zh-TW.md)） |
| N-Tier Architecture | N 層式架構 | 呈現層 → API 層 → 業務邏輯層 → 資料存取層 的分層架構 |
| Clean Architecture | 整潔架構 | 依賴方向由外向內，核心層不依賴外部框架 |
| MVVM | MVVM 模式 | Model-View-ViewModel，用於 UI 層的資料繫結與狀態管理 |

---

## 2. 表單結構定義層（Bee.Definition）

### 核心類別

| 英文名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| `FormSchema` | 表單結構定義 | 定義中樞，同時驅動 UI、資料庫結構與驗證規則 |
| `FormTable` | 表單資料表 | FormSchema 內的主檔或明細資料表定義 |
| `FormField` | 表單欄位 | 表單資料表內的單一欄位，含型別、驗證、控制項資訊 |
| `FormLayout` | 表單版面配置 | FormSchema 的 UI 投影，描述欄位排列方式 |
| `FormTableCollection` | 表單資料表集合 | FormSchema 內所有 FormTable 的集合 |
| `FormLayoutGenerator` | 表單版面配置產生器 | 依據 FormSchema 自動產生 FormLayout |
| `TableSchema` | 資料表結構 | FormSchema 的資料庫投影，對應實體資料表欄位與索引 |
| `DbTableIndex` | 資料表索引 | 資料表的索引定義，含唯一性與主鍵資訊 |
| `DbCategorySettings` | 資料庫類別設定 | 管理所有邏輯資料庫類別（common / company / log）的設定集合 |
| `DbCategory` | 資料庫類別 | 邏輯資料庫類別節點，含 `Id`（"common" / "company" 等）與所屬資料表清單 |
| `PathOptions` | 定義檔案路徑選項 | DI 注入的 options，提供各類定義檔（FormSchema、TableSchema 等）的標準路徑 |
| `SessionInfo` | 連線資訊 | 執行期用戶連線狀態，含 AccessToken、UserId、語系、時區，以及 `CompanyId`（nullable；由 `EnterCompany` 寫入、由 `LeaveCompany` / `Logout` 清除） |
| `CompanyInfo` | 公司資訊 | 描述使用者可進入之公司的中繼資料：`CompanyId`、`CompanyName`、`CompanyDatabaseId`（該公司 session 期間使用的 `company` 類 `DatabaseSettings` id） |
| `DbScope` | 資料庫範疇 | 型別安全 enum，表達 bo repo 的資料庫存取意圖：`Common` / `Company` / `Log`。與 `schema.CategoryId`（XML 字串屬性）**概念脫勾**——值對應一致，但 enum 是傳給 `IRepositoryDatabaseRouter` 的執行時意圖 |
| `SortField` | 排序欄位 | 單一排序欄位，含欄位名稱與方向 |
| `SortFieldCollection` | 排序欄位集合 | 多個 SortField 的集合 |

### 篩選條件

| 英文名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| `FilterCondition` | 篩選條件 | 單一欄位條件（例如 `Name LIKE '%Lee%'`、`Age > 18`） |
| `FilterGroup` | 篩選條件群組 | 多個條件以 AND / OR 組合的條件樹節點 |
| `IFilterNode` | 篩選節點介面 | `FilterCondition` 與 `FilterGroup` 的共同介面 |

### 日誌

| 英文名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| `ILogWriter` | 日誌寫入介面 | 系統日誌輸出的抽象介面 |
| `LogEntry` | 日誌記錄 | 單筆系統日誌事件物件 |
| `LogOptions` | 日誌選項 | 日誌行為的設定參數 |
| `ConsoleLogWriter` | Console 日誌寫入器 | 將日誌輸出至 Console 的實作 |
| `NullLogWriter` | 空日誌寫入器 | 不執行任何操作的預設實作，避免 null 檢查 |
| `DbAccessAnomalyLogOptions` | 資料庫存取異常日誌選項 | 資料庫異常存取行為的日誌設定 |

### 定義存取

| 英文名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| `IDefineAccess` | 定義存取介面 | 讀取與儲存各類定義資料的抽象介面 |
| `IDefineStorage` | 定義儲存介面 | 定義資料持久化的抽象介面 |
| `FileDefineStorage` | 檔案定義儲存 | 以 XML 檔案實作定義資料的讀寫 |

### 其他介面

| 英文名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| `IUIControl` | UI 控制項介面 | 依表單模式控制 UI 元件狀態的介面 |
| `ICacheDataSourceProvider` | 快取資料來源提供者介面 | 取得暫存連線的用戶資料 |
| `IEnterpriseObjectService` | 企業物件服務介面 | 提供組織結構、模組參數等業務物件的統一存取服務 |

---

## 3. 資料庫層（Bee.Db）

| 英文名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| `DbField` | 資料庫欄位 | 資料庫欄位定義，含精確度、小數位數、長度等 |
| `SelectCommandBuilder` | 查詢命令建構器 | 依據 FormSchema 建構 SELECT SQL 命令（`Bee.Db.Dml`） |
| `IFormCommandBuilder` | 表單命令建構器介面 | 依 FormSchema 建構 CRUD 命令的契約（`Bee.Db.Dml`） |
| `SqlFormCommandBuilder` | SQL Server 表單命令建構器 | `IFormCommandBuilder` 的 SQL Server 實作（`Bee.Db.Providers.SqlServer`） |
| `PgFormCommandBuilder` | PostgreSQL 表單命令建構器 | `IFormCommandBuilder` 的 PostgreSQL 實作（`Bee.Db.Providers.PostgreSql`） |
| `MySqlFormCommandBuilder` | MySQL 表單命令建構器 | `IFormCommandBuilder` 的 MySQL 實作（`Bee.Db.Providers.MySql`） |
| `OracleFormCommandBuilder` | Oracle 表單命令建構器 | `IFormCommandBuilder` 的 Oracle 實作（`Bee.Db.Providers.Oracle`） |
| `SqliteFormCommandBuilder` | SQLite 表單命令建構器 | `IFormCommandBuilder` 的 SQLite 實作（`Bee.Db.Providers.Sqlite`） |

---

## 4. 業務邏輯層（Bee.Business）

| 英文名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| `BusinessObject` | 業務邏輯物件 | 所有 BO 的基礎類別，負責業務邏輯，不直接存取資料庫 |
| `DataSet` | 資料集 | 跨層 DTO，承載 Master-Detail 資料，不含業務邏輯 |
| `UnitOfWork` | 工作單元 | 管理跨 Repository 的共享交易 |

---

## 5. Repository 層（Bee.Repository）

| 英文名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| `IDataFormRepository` | 資料表單 Repository 介面 | FormSchema 驅動的 CRUD 操作（自動產生 SQL） |
| `IReportFormRepository` | 報表表單 Repository 介面 | 複雜查詢與報表用，由 BO 自行實作 SQL（AnyCode） |
| `IRepositoryDatabaseRouter` | Repository 資料庫路由介面 | 從 `DbScope` 與 access token 解析出實際 `databaseId`。`Common` / `Log` 對映固定 databaseId；`Company` 透過 `SessionInfo.CompanyId` → `CompanyInfo.CompanyDatabaseId` 解析 |
| `IFormRepositoryFactory` | 表單 Repository 工廠介面 | 建立 form-level repository。`CreateDataFormRepository(progId, accessToken)` 讀取 schema、把 `CategoryId` 轉成 `DbScope`、再委派給 router 解析 databaseId |

---

## 6. API 層（Bee.Api.Core / Bee.Api.AspNetCore）

| 英文名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| `ApiPayload` | API 傳遞資料結構 | 包裝傳輸資料，支援壓縮與加密 |
| `JsonRpcRequest` | JSON-RPC 請求 | JSON-RPC 2.0 協定的請求物件 |
| `JsonRpcResponse` | JSON-RPC 回應 | JSON-RPC 2.0 協定的回應物件 |
| `ExecFuncArgs` | 自訂函式執行參數 | 呼叫自訂業務函式時傳遞的參數物件 |
| `ApiAccessControlAttribute` | API 存取控制屬性 | 宣告 API 端點的保護等級與認證需求 |
| `TraceContext` | 追蹤情境 | 記錄 API 請求的追蹤資訊 |

### 安全性

| 英文名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| `ApiProtectionLevel` | API 保護等級 | API Payload 的保護層級（`Public` / `Encoded` / `Encrypted` / `LocalOnly`），位於 `Bee.Definition.Security` |
| `ApiAccessRequirement` | API 存取授權需求 | API 端點的認證要求（`Anonymous` / `Authenticated`），位於 `Bee.Definition.Security` |
| `IApiPayloadEncryptor` | API Payload 加密介面 | 定義 Payload 加解密行為的介面 |
| `AesCbcHmacCryptor` | AES-CBC-HMAC 加密器 | 使用 AES-256-CBC + HMAC-SHA256 的標準加密實作 |
| `RsaCryptor` | RSA 加密器 | RSA 非對稱加密實作 |
| `NoEncryptionEncryptor` | 無加密器 | 僅測試環境使用，不執行任何加密 |

---

## 7. 快取層（Bee.ObjectCaching）

| 英文名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| `ICacheContainer` | 快取容器介面 | DI 註冊的容器，集中持有所有快取單例（FormSchema、TableSchema、DatabaseSettings、SessionInfo、CompanyInfo 等）；預設實作為 `CacheContainerService` |
| `LocalDefineAccess` | 本機定義存取 | 透過本機快取存取定義資料的實作 |
| `FormSchemaCache` | 表單結構定義快取 | `FormSchema` 物件的快取容器 |
| `KeyObjectCache<T>` | 鍵值物件快取 | 以鍵值為索引的泛型物件快取基礎類別。內含負向快取：`CreateInstance` 回 null 時記入哨兵值並設短 TTL（預設 5 分鐘絕對過期），避免重複查詢同一無效 key 反覆觸發 create 路徑 |
| `ISessionInfoService` | Session 資訊服務介面 | `SessionInfoCache` 的存取包裝；由 `Login` 寫入、`EnterCompany` / `LeaveCompany` 變動、`Logout` 移除 |
| `ICompanyInfoService` | 公司資訊服務介面 | `CompanyInfoCache` 的存取包裝；由 `IRepositoryDatabaseRouter` 消費以解析 `DbScope.Company` |

---

## 8. 連線層（Bee.Api.Client）

| 英文名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| `RemoteDefineAccess` | 遠端定義存取 | 透過遠端 API 存取定義資料的實作 |

---

## 9. 基礎設施（Bee.Base）

| 英文名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| `IKeyObject` | 鍵值物件介面 | 具有唯一識別鍵的物件抽象介面 |
| `XmlCodec` | XML 序列化工具 | XML 序列化與反序列化的靜態工具類別（位於 `Bee.Base.Serialization`） |
| `FileHashValidator` | 檔案雜湊驗證器 | 使用雜湊值驗證檔案完整性 |
| `AesCbcHmacKeyGenerator` | AES-CBC-HMAC 金鑰產生器 | 產生 AES 與 HMAC 所需金鑰的工具類別 |
| `TreeNodeAttribute` | 樹狀節點屬性 | 標記類別在樹狀結構中的顯示名稱 |

---

## 10. 列舉型別（Enumerations）

### 欄位與資料類型

| 英文名稱 | 中文名稱 | 值 |
|----------|----------|----|
| `FieldType` | 欄位種類 | `DbField`（資料庫欄位）、`RelationField`（關聯欄位）、`VirtualField`（虛擬欄位） |
| `FieldDbType` | 欄位資料庫型別 | `String`、`Integer`、`Decimal`、`DateTime`、`Date`、`Boolean` … 等 13 種 |
| `ControlType` | 控制項類型 | `TextEdit`、`DropDownEdit`、`DateEdit`、`CheckBox` … |
| `FormMode` | 表單模式 | `Add`（新增）、`Edit`（編輯）、`View`（檢視） |
| `TableRole` | 資料表角色 | `Master`（主檔）、`Detail`（明細） |

### 查詢與篩選

| 英文名稱 | 中文名稱 | 值 |
|----------|----------|----|
| `ComparisonOperator` | 比較運算子 | `Equals`、`NotEquals`、`GreaterThan`、`LessThan`、`Like`、`In`、`Between` … |
| `LogicalOperator` | 邏輯運算子 | `And`（且）、`Or`（或） |
| `SortDirection` | 排序方向 | `Ascending`（遞增）、`Descending`（遞減） |
| `FilterNodeType` | 篩選節點種類 | `Condition`（條件）、`Group`（群組） |

### API 與安全

| 英文名稱 | 中文名稱 | 值 |
|----------|----------|----|
| `ApiProtectionLevel` | API 保護等級 | `Public`（公開）、`Encoded`（編碼）、`Encrypted`（加密）、`LocalOnly`（本機限定） |
| `ApiAccessRequirement` | API 存取授權需求 | `Anonymous`（不需登入）、`Authenticated`（需登入） |
| `PayloadFormat` | Payload 格式 | `Plain`（明文）、`Encoded`（Base64 編碼）、`Encrypted`（加密） |

### 定義類型

| 英文名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| `DefineType` | 定義資料類別 | `SystemSettings`、`DatabaseSettings`、`DbCategorySettings`、`ProgramSettings`、`TableSchema`、`FormSchema`、`FormLayout` 共 7 個值 |

### 資料庫

| 英文名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| `DatabaseType` | 資料庫類型 | `SqlServer`、`MySql`、`PostgreSql` … |
| `SchemaUpgradeAction` | 結構升級動作 | 資料庫結構變更時的升級策略 |
| `LogEntryType` | 日誌記錄類型 | `Information`（資訊）、`Warning`（警告）、`Error`（錯誤） |

---

## 11. 系統欄位（System Fields）

BeeNET 框架在所有受管理資料表中自動維護以下系統欄位：

| 欄位名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| `sys_no` | 流水號 | 資料列的自動遞增流水號 |
| `sys_rowid` | 唯一識別碼 | 資料列的全域唯一識別碼（GUID） |
| `sys_master_rowid` | 主檔外鍵 | 主檔資料列的 `sys_rowid`（明細表使用） |
| `sys_insert_time` | 建立時間 | 資料列的建立時間戳記 |
| `sys_update_time` | 更新時間 | 資料列的最後更新時間戳記 |
| `sys_valid_date` | 生效日期 | 資料列的生效起始日期 |
| `sys_invalid_date` | 失效日期 | 資料列的生效截止日期 |

---

## 12. 設定檔（Configuration Files）

| 檔案名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| `SystemSettings.xml` | 系統設定檔 | 全域系統參數 |
| `DatabaseSettings.xml` | 資料庫連線設定檔 | 資料庫連線字串與類型 |
| `DbCategorySettings.xml` | 資料庫類別設定檔 | 所有邏輯資料庫類別與其包含的資料表清單 |
| `ProgramSettings.xml` | 程式設定檔 | 功能程式的參數設定 |
| `ClientSettings.xml` | 用戶端設定檔 | 前端 / 用戶端的行為設定 |
| `FormSchema.xml` | 表單結構定義檔 | 各功能程式的 FormSchema 序列化檔 |
| `FormLayout.xml` | 表單版面配置檔 | 各功能程式的 FormLayout 序列化檔 |
| `TableSchema.xml` | 資料表結構檔 | 各資料表的 TableSchema 序列化檔 |

---

## 13. 前端層（Bee.UI.* / Bee.Web.Blazor.*）

### 跨平台 UI 共通層（`Bee.UI.Core`）

| 英文名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| `ClientInfo` | 用戶端資訊 | Static singleton，管理連線狀態（endpoint、AccessToken、UserInfo），提供 `SystemApiConnector` / `CreateFormApiConnector` / `DefineAccess`。設計給「一個 process = 一個使用者」模型（桌面 / MAUI）。**禁止用於 Blazor 環境**，後者一個 process 服務多個 user circuit |
| `IEndpointStorage` | 端點儲存介面 | 抽象 API endpoint（URL / 設定）的用戶端持久化機制；預設實作存於 `{ExeName}.Settings.xml` |
| `IUIViewService` | UI 視圖服務介面 | 由宿主提供的 dialog service，當 `ClientInfo.Initialize` 需要詢問使用者 endpoint 時呼叫（`ShowApiConnect`）；具體實作依 UI 框架而定（MAUI ContentPage / WinForms Form 等） |
| `VersionInfo` | 版本資訊 | 用戶端在與後端建立連線時回報的版本 metadata |
| `SupportedConnectTypes` | 支援連線類型 | 控制 `ClientInfo.Initialize` 允許哪些連線模式（`Local` / `Remote` / `Both`）的 Flags |

### MAUI 控制項套件（`Bee.UI.Maui`）

| 英文名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| `DynamicForm` | 動態表單 | MAUI 控制項，依 FormSchema 在執行時動態渲染表單（master + detail） |
| `FormDataObject` | 表單資料物件 | `DynamicForm` 綁定的資料物件，承載底層 `DataSet` 與表單層級狀態 |

### Web 前端（`Bee.Web.Blazor.Server` / `Bee.Web.Blazor.Wasm`）

兩個套件皆為 Razor Class Library（RCL），對外暴露相同的 Blazor 元件介面（`DynamicForm`、`FormDataObject`），但各自針對所屬宿主模型獨立實作（Blazor Server 以 DI scope 連接器搭配 SignalR circuit；Blazor WASM 強制使用 `RemoteApiProvider` 走 HTTP）。

| 英文名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| `DynamicForm`（Razor 元件） | 動態表單元件 | Blazor 元件，依 FormSchema 動態渲染表單；Server 與 Wasm 套件各自提供一份實作 |
| `FormDataObject` | 表單資料物件 | Blazor `DynamicForm` 綁定的資料物件；各套件依宿主模型分別有獨立版本 |
| `AddBeeWebBlazorServer` | Blazor Server 註冊擴充方法 | `IServiceCollection` 擴充方法，註冊 Blazor Server RCL 所需服務（DI scope 連接器） |
| `AddBeeWebBlazorWasm` | Blazor WASM 註冊擴充方法 | `IServiceCollection` 擴充方法，註冊 Blazor WASM RCL 所需服務（強制 `RemoteApiProvider`） |

### API 連線提供者（`Bee.Api.Client`）

| 英文名稱 | 中文名稱 | 說明 |
|----------|----------|------|
| `IApiProvider` | API 提供者介面 | 抽象連接器如何抵達後端；由宿主在啟動時選擇實作 |
| `LocalApiProvider` | 近端 API 提供者 | In-process 實作，前後端共用同一個 process，直接呼叫 BO 方法（無 HTTP 開銷） |
| `RemoteApiProvider` | 遠端 API 提供者 | 基於 HTTP 的實作，前端透過 JSON-RPC 連到後端（Blazor WASM 必須使用此實作） |
