# BeeNET Framework Terminology Reference (English ↔ Chinese)

[繁體中文](terminology.zh-TW.md)

This document provides a standard term reference for technical writing, ensuring consistency between English and Chinese names.

---

## Table of Contents

1. [Architecture Patterns and Core Concepts](#1-architecture-patterns-and-core-concepts)
2. [Form Definition Layer (Bee.Definition)](#2-form-definition-layer-beedefinition)
3. [Database Layer (Bee.Db)](#3-database-layer-beedb)
4. [Business Logic Layer (Bee.Business)](#4-business-logic-layer-beebusiness)
5. [Repository Layer (Bee.Repository)](#5-repository-layer-beerepository)
6. [API Layer (Bee.Api.Core / Bee.Api.AspNetCore)](#6-api-layer-beeapicore--beeapiaspnetcore)
7. [Caching Layer (Bee.ObjectCaching)](#7-caching-layer-beeobjectcaching)
8. [Connector Layer (Bee.Api.Client)](#8-connector-layer-beeapiclient)
9. [Infrastructure (Bee.Base)](#9-infrastructure-beebase)
10. [Enumerations](#10-enumerations)
11. [System Fields](#11-system-fields)
12. [Configuration Files](#12-configuration-files)
13. [Frontend Layer (Bee.UI.* / Bee.Web.Blazor.*)](#13-frontend-layer-beeui--beewebblazor)

---

## 1. Architecture Patterns and Core Concepts

| English | 中文 | Description |
|---------|------|-------------|
| Definition-Driven Architecture | 定義導向架構 | BeeNET's core architectural pattern, using structural definitions to uniformly drive UI, database, and business logic |
| Single Source of Truth | 唯一定義來源 | `FormSchema` as the system's only structural specification, avoiding duplicate implementations across three layers |
| NoCode | 零程式碼 | Fully generated automatically from `FormSchema`; no code required |
| LowCode | 低程式碼 | Built on `FormSchema` with small overrides extending behavior |
| AnyCode | 全程式碼 | Fully implemented by the developer, not driven by `FormSchema` |
| Master-Detail Pattern | 主從資料模式 | A master record (Master) associated with multiple detail records (Detail) |
| Repository Dual-Track Strategy | Repository 雙軌策略 | CRUD driven by `FormSchema`; reports / batches implemented by BO (AnyCode) |
| FormMap | 表單映射 | Bee.Db's data access pattern, describing business entities by `FormSchema` and dynamically generating SQL; a parallel design to ORM ([details](formmap.md)) |
| N-Tier Architecture | N 層式架構 | Presentation → API → Business Logic → Data Access layered architecture |
| Clean Architecture | 整潔架構 | Dependency direction from outside in; the core layer does not depend on external frameworks |
| MVVM | MVVM 模式 | Model-View-ViewModel, used at the UI layer for data binding and state management |

---

## 2. Form Definition Layer (Bee.Definition)

### Core Classes

| English | 中文 | Description |
|---------|------|-------------|
| `FormSchema` | 表單結構定義 | The definition hub, simultaneously driving UI, database structure, and validation rules |
| `FormTable` | 表單資料表 | Master or detail table definition inside a FormSchema |
| `FormField` | 表單欄位 | A single field inside a form table, with type, validation, and control information |
| `FormLayout` | 表單版面配置 | The UI projection of a FormSchema, describing field arrangement |
| `FormTableCollection` | 表單資料表集合 | A collection of all FormTables in a FormSchema |
| `FormLayoutGenerator` | 表單版面配置產生器 | Automatically generates a FormLayout from a FormSchema |
| `TableSchema` | 資料表結構 | The database projection of a FormSchema, mapping to physical table columns and indexes |
| `DbTableIndex` | 資料表索引 | Table index definition, including uniqueness and primary key information |
| `DbCategorySettings` | 資料庫類別設定 | A collection managing all logical database categories (common / company / log) |
| `DbCategory` | 資料庫類別 | A logical database category node, with `Id` ("common" / "company", etc.) and the list of tables it owns |
| `PathOptions` | 定義檔案路徑選項 | DI-injected options that provide standardized paths for definition files (FormSchema, TableSchema, etc.) |
| `SessionInfo` | 連線資訊 | Runtime user session state, including AccessToken, UserId, locale, time zone, and `CompanyId` (nullable; set by `EnterCompany`, cleared by `LeaveCompany` / `Logout`) |
| `CompanyInfo` | 公司資訊 | Metadata describing a company the user may enter for a session: `CompanyId`, `CompanyName`, `CompanyDatabaseId` (the `DatabaseSettings` id used for the `company` category during this session) |
| `DbScope` | 資料庫範疇 | Type-safe enum representing a bo repo's database access intent: `Common` / `Company` / `Log`. Decoupled from `schema.CategoryId` (the string XML attribute) — same three values but the enum is the runtime intent passed to `IRepositoryDatabaseRouter` |
| `SortField` | 排序欄位 | A single sort field, with field name and direction |
| `SortFieldCollection` | 排序欄位集合 | A collection of multiple SortFields |

### Filter Conditions

| English | 中文 | Description |
|---------|------|-------------|
| `FilterCondition` | 篩選條件 | A single column condition (e.g. `Name LIKE '%Lee%'`, `Age > 18`) |
| `FilterGroup` | 篩選條件群組 | A condition tree node combining multiple conditions with AND / OR |
| `IFilterNode` | 篩選節點介面 | The common interface of `FilterCondition` and `FilterGroup` |

### Logging

| English | 中文 | Description |
|---------|------|-------------|
| `ILogWriter` | 日誌寫入介面 | The abstract interface for system log output |
| `LogEntry` | 日誌記錄 | A single system log event object |
| `LogOptions` | 日誌選項 | Configuration parameters for logging behavior |
| `ConsoleLogWriter` | Console 日誌寫入器 | Implementation that writes logs to the console |
| `NullLogWriter` | 空日誌寫入器 | Default no-op implementation, avoiding null checks |
| `DbAccessAnomalyLogOptions` | 資料庫存取異常日誌選項 | Logging configuration for database access anomalies |

### Definition Access

| English | 中文 | Description |
|---------|------|-------------|
| `IDefineAccess` | 定義存取介面 | Abstract interface for reading and storing all kinds of definition data |
| `IDefineStorage` | 定義儲存介面 | Abstract interface for definition data persistence |
| `FileDefineStorage` | 檔案定義儲存 | XML-file-based implementation of definition data read / write |

### Other Interfaces

| English | 中文 | Description |
|---------|------|-------------|
| `IUIControl` | UI 控制項介面 | Interface that controls UI component state by form mode |
| `ICacheDataSourceProvider` | 快取資料來源提供者介面 | Provides cached user data for transient sessions |
| `IEnterpriseObjectService` | 企業物件服務介面 | Unified access service for enterprise business objects (organization, module parameters, etc.) |

---

## 3. Database Layer (Bee.Db)

| English | 中文 | Description |
|---------|------|-------------|
| `DbField` | 資料庫欄位 | Database column definition, including precision, scale, length, etc. |
| `SelectCommandBuilder` | 查詢命令建構器 | Builds SELECT SQL commands from a FormSchema (`Bee.Db.Dml`) |
| `IFormCommandBuilder` | 表單命令建構器介面 | Contract for building CRUD commands from a FormSchema (`Bee.Db.Dml`) |
| `SqlFormCommandBuilder` | SQL Server 表單命令建構器 | SQL Server implementation of `IFormCommandBuilder` (`Bee.Db.Providers.SqlServer`) |
| `PgFormCommandBuilder` | PostgreSQL 表單命令建構器 | PostgreSQL implementation of `IFormCommandBuilder` (`Bee.Db.Providers.PostgreSql`) |
| `MySqlFormCommandBuilder` | MySQL 表單命令建構器 | MySQL implementation of `IFormCommandBuilder` (`Bee.Db.Providers.MySql`) |
| `OracleFormCommandBuilder` | Oracle 表單命令建構器 | Oracle implementation of `IFormCommandBuilder` (`Bee.Db.Providers.Oracle`) |
| `SqliteFormCommandBuilder` | SQLite 表單命令建構器 | SQLite implementation of `IFormCommandBuilder` (`Bee.Db.Providers.Sqlite`) |

---

## 4. Business Logic Layer (Bee.Business)

| English | 中文 | Description |
|---------|------|-------------|
| `BusinessObject` | 業務邏輯物件 | Base class for all BOs; handles business logic, does not access the database directly |
| `DataSet` | 資料集 | Cross-layer DTO carrying Master-Detail data, with no business logic |
| `UnitOfWork` | 工作單元 | Manages shared transactions across Repositories |

---

## 5. Repository Layer (Bee.Repository)

| English | 中文 | Description |
|---------|------|-------------|
| `IDataFormRepository` | 資料表單 Repository 介面 | FormSchema-driven CRUD operations (auto-generated SQL) |
| `IReportFormRepository` | 報表表單 Repository 介面 | For complex queries and reporting; SQL implemented by BO (AnyCode) |
| `IRepositoryDatabaseRouter` | Repository 資料庫路由介面 | Resolves the physical `databaseId` for a given `DbScope` and access token. `Common` / `Log` map to fixed databaseIds; `Company` resolves via `SessionInfo.CompanyId` → `CompanyInfo.CompanyDatabaseId` |
| `IFormRepositoryFactory` | 表單 Repository 工廠介面 | Creates form-level repositories. `CreateDataFormRepository(progId, accessToken)` reads the form schema, converts `CategoryId` to `DbScope`, and delegates routing to the router |

---

## 6. API Layer (Bee.Api.Core / Bee.Api.AspNetCore)

| English | 中文 | Description |
|---------|------|-------------|
| `ApiPayload` | API 傳遞資料結構 | Wraps transmission data, supporting compression and encryption |
| `JsonRpcRequest` | JSON-RPC 請求 | JSON-RPC 2.0 request object |
| `JsonRpcResponse` | JSON-RPC 回應 | JSON-RPC 2.0 response object |
| `ExecFuncArgs` | 自訂函式執行參數 | Parameter object passed when invoking custom business functions |
| `ApiAccessControlAttribute` | API 存取控制屬性 | Declares the protection level and authentication requirement of API endpoints |
| `TraceContext` | 追蹤情境 | Records tracing information for API requests |

### Security

| English | 中文 | Description |
|---------|------|-------------|
| `ApiProtectionLevel` | API 保護等級 | API Payload protection level (`Public` / `Encoded` / `Encrypted` / `LocalOnly`), located in `Bee.Definition.Security` |
| `ApiAccessRequirement` | API 存取授權需求 | Authentication requirement for API endpoints (`Anonymous` / `Authenticated`), located in `Bee.Definition.Security` |
| `IApiPayloadEncryptor` | API Payload 加密介面 | Defines payload encryption / decryption behavior |
| `AesCbcHmacCryptor` | AES-CBC-HMAC 加密器 | Standard encryption implementation using AES-256-CBC + HMAC-SHA256 |
| `RsaCryptor` | RSA 加密器 | RSA asymmetric encryption implementation |
| `NoEncryptionEncryptor` | 無加密器 | No-encryption implementation, for test environments only |

---

## 7. Caching Layer (Bee.ObjectCaching)

| English | 中文 | Description |
|---------|------|-------------|
| `ICacheContainer` | 快取容器介面 | DI-registered container that centrally holds all cache singletons (FormSchema, TableSchema, DatabaseSettings, SessionInfo, CompanyInfo, etc.); default implementation `CacheContainerService` |
| `LocalDefineAccess` | 本機定義存取 | Implementation that accesses definition data via local cache |
| `FormSchemaCache` | 表單結構定義快取 | Cache container for `FormSchema` objects |
| `KeyObjectCache<T>` | 鍵值物件快取 | Generic base class for object caches indexed by key. Includes negative caching: `CreateInstance` returning null is recorded as a sentinel for a short TTL (default 5 min absolute), so repeated lookups of unknown keys do not re-invoke the create path |
| `ISessionInfoService` | Session 資訊服務介面 | Access wrapper around `SessionInfoCache`; populated by `Login`, mutated by `EnterCompany` / `LeaveCompany`, removed by `Logout` |
| `ICompanyInfoService` | 公司資訊服務介面 | Access wrapper around `CompanyInfoCache`; consumed by `IRepositoryDatabaseRouter` to resolve `DbScope.Company` |

---

## 8. Connector Layer (Bee.Api.Client)

| English | 中文 | Description |
|---------|------|-------------|
| `RemoteDefineAccess` | 遠端定義存取 | Implementation that accesses definition data via the remote API |

---

## 9. Infrastructure (Bee.Base)

| English | 中文 | Description |
|---------|------|-------------|
| `IKeyObject` | 鍵值物件介面 | Abstract interface for objects with a unique identifying key |
| `XmlCodec` | XML 序列化工具 | Static utility class for XML serialization / deserialization (`Bee.Base.Serialization`) |
| `FileHashValidator` | 檔案雜湊驗證器 | Validates file integrity using hash values |
| `AesCbcHmacKeyGenerator` | AES-CBC-HMAC 金鑰產生器 | Utility class for generating AES and HMAC keys |
| `TreeNodeAttribute` | 樹狀節點屬性 | Marks the display name of a class within a tree structure |

---

## 10. Enumerations

### Field and Data Types

| English | 中文 | Values |
|---------|------|--------|
| `FieldType` | 欄位種類 | `DbField` (database field), `RelationField` (relation field), `VirtualField` (virtual field) |
| `FieldDbType` | 欄位資料庫型別 | `String`, `Integer`, `Decimal`, `DateTime`, `Date`, `Boolean`, ... 13 in total |
| `ControlType` | 控制項類型 | `TextEdit`, `DropDownEdit`, `DateEdit`, `CheckBox`, ... |
| `FormMode` | 表單模式 | `Add`, `Edit`, `View` |
| `TableRole` | 資料表角色 | `Master`, `Detail` |

### Query and Filter

| English | 中文 | Values |
|---------|------|--------|
| `ComparisonOperator` | 比較運算子 | `Equals`, `NotEquals`, `GreaterThan`, `LessThan`, `Like`, `In`, `Between`, ... |
| `LogicalOperator` | 邏輯運算子 | `And`, `Or` |
| `SortDirection` | 排序方向 | `Ascending`, `Descending` |
| `FilterNodeType` | 篩選節點種類 | `Condition`, `Group` |

### API and Security

| English | 中文 | Values |
|---------|------|--------|
| `ApiProtectionLevel` | API 保護等級 | `Public`, `Encoded`, `Encrypted`, `LocalOnly` |
| `ApiAccessRequirement` | API 存取授權需求 | `Anonymous` (no login), `Authenticated` (login required) |
| `PayloadFormat` | Payload 格式 | `Plain`, `Encoded` (Base64), `Encrypted` |

### Definition Type

| English | 中文 | Description |
|---------|------|-------------|
| `DefineType` | 定義資料類別 | `SystemSettings`, `DatabaseSettings`, `DbCategorySettings`, `ProgramSettings`, `TableSchema`, `FormSchema`, `FormLayout` — 7 values total |

### Database

| English | 中文 | Description |
|---------|------|-------------|
| `DatabaseType` | 資料庫類型 | `SqlServer`, `MySql`, `PostgreSql`, ... |
| `SchemaUpgradeAction` | 結構升級動作 | Upgrade strategy for database structural changes |
| `LogEntryType` | 日誌記錄類型 | `Information`, `Warning`, `Error` |

---

## 11. System Fields

The BeeNET framework automatically maintains the following system fields in all managed tables:

| Field Name | 中文 | Description |
|------------|------|-------------|
| `sys_no` | 流水號 | Auto-incremented sequential number for the row |
| `sys_rowid` | 唯一識別碼 | Globally unique identifier for the row (GUID) |
| `sys_master_rowid` | 主檔外鍵 | The master row's `sys_rowid` (used by detail tables) |
| `sys_insert_time` | 建立時間 | Row creation timestamp |
| `sys_update_time` | 更新時間 | Row last update timestamp |
| `sys_valid_date` | 生效日期 | Effective start date of the row |
| `sys_invalid_date` | 失效日期 | Expiry date of the row |

---

## 12. Configuration Files

| File Name | 中文 | Description |
|-----------|------|-------------|
| `SystemSettings.xml` | 系統設定檔 | Global system parameters |
| `DatabaseSettings.xml` | 資料庫連線設定檔 | Database connection strings and types |
| `DbCategorySettings.xml` | 資料庫類別設定檔 | All logical database categories and the tables they contain |
| `ProgramSettings.xml` | 程式設定檔 | Parameters for functional programs |
| `ClientSettings.xml` | 用戶端設定檔 | Front-end / client behavior settings |
| `FormSchema.xml` | 表單結構定義檔 | Serialized FormSchema files for each functional program |
| `FormLayout.xml` | 表單版面配置檔 | Serialized FormLayout files for each functional program |
| `TableSchema.xml` | 資料表結構檔 | Serialized TableSchema files for each table |

---

## 13. Frontend Layer (Bee.UI.* / Bee.Web.Blazor.*)

### Cross-Platform UI Common (`Bee.UI.Core`)

| English | 中文 | Description |
|---------|------|-------------|
| `ClientInfo` | 用戶端資訊 | Static singleton that manages connection state (endpoint, AccessToken, UserInfo) and exposes `SystemApiConnector` / `CreateFormApiConnector` / `DefineAccess`. Designed for the "one process = one user" model (desktop / MAUI). **Must not be used in Blazor environments**, where multiple user circuits share a process |
| `IEndpointStorage` | 端點儲存介面 | Abstraction for persisting the API endpoint (URL / settings) on the client side; default implementation stores in `{ExeName}.Settings.xml` |
| `IUIViewService` | UI 視圖服務介面 | Host-supplied dialog service called when `ClientInfo.Initialize` needs to ask the user for the endpoint (`ShowApiConnect`); concrete implementation depends on the UI framework (MAUI ContentPage / WinForms Form, etc.) |
| `VersionInfo` | 版本資訊 | Version metadata reported by the client to the backend during handshake |
| `SupportedConnectTypes` | 支援連線類型 | Flags controlling which connection modes (`Local` / `Remote` / `Both`) the host allows during `ClientInfo.Initialize` |

### MAUI Control Library (`Bee.UI.Maui`)

| English | 中文 | Description |
|---------|------|-------------|
| `DynamicForm` | 動態表單 | MAUI control that renders a FormSchema-driven form (master + detail) at runtime |
| `FormDataObject` | 表單資料物件 | The data-binding object bound by `DynamicForm`, carrying the underlying `DataSet` and form-level state |

### Web Frontend (`Bee.Web.Blazor.Server` / `Bee.Web.Blazor.Wasm`)

Both packages are Razor Class Libraries (RCLs) that expose the same Blazor component surface — `DynamicForm` and `FormDataObject` — but each one is implemented independently for its own hosting model (Blazor Server uses DI-scoped connectors per SignalR circuit; Blazor WASM is forced to `RemoteApiProvider` over HTTP).

| English | 中文 | Description |
|---------|------|-------------|
| `DynamicForm` (Razor component) | 動態表單元件 | Blazor component that renders a FormSchema-driven form; the Server and Wasm packages each ship their own implementation |
| `FormDataObject` | 表單資料物件 | Data-binding object bound by the Blazor `DynamicForm`; each package has its own version tailored to its hosting model |
| `AddBeeWebBlazorServer` | Blazor Server 註冊擴充方法 | `IServiceCollection` extension that registers the Blazor Server RCL services (DI-scoped connectors) |
| `AddBeeWebBlazorWasm` | Blazor WASM 註冊擴充方法 | `IServiceCollection` extension that registers the Blazor WASM RCL services (forces `RemoteApiProvider`) |

### Api Client Providers (`Bee.Api.Client`)

| English | 中文 | Description |
|---------|------|-------------|
| `IApiProvider` | API 提供者介面 | Abstracts how a connector reaches the backend; chosen by the host at startup |
| `LocalApiProvider` | 近端 API 提供者 | In-process implementation; the frontend and backend share the same process, invoking BO methods directly (no HTTP) |
| `RemoteApiProvider` | 遠端 API 提供者 | HTTP-based implementation; the frontend reaches the backend over JSON-RPC (required for Blazor WASM) |
