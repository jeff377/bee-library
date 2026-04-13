# 各專案 README 撰寫計畫

## 目標

為 `src/` 下 11 個專案各新增獨立的 README，協助 AI Coding 工具快速理解每個專案的職責、邊界與慣例。

## 語言版本

依照 code-style 規範，主 README 已有雙語版本，各專案 README 也採雙語：
- `README.md`（英文）
- `README.zh-TW.md`（繁體中文）
- 兩份文件頂部需有語言切換連結

每個專案產出 2 個檔案，共 22 個檔案。

## README 內容結構

每份 README 統一採用以下結構，精簡但對 AI Coding 最有價值：

```markdown
# Bee.{Module}

> 一句話描述此專案的職責

[繁體中文](README.zh-TW.md) / [English](README.md)

## 架構定位

- 所屬層級（基礎設施 / API / 商業邏輯 / 資料存取）
- 上游（誰依賴我）
- 下游（我依賴誰）

## 目標框架

列出 target frameworks

## 主要功能

依功能區塊列出核心能力，每項 1-2 行說明

## 關鍵公開 API

列出最重要的介面/類別及其用途（不是完整 API 文件，只列進入點）

## 設計慣例

此專案特有的 pattern 或約定（如果有的話）

## 目錄結構

簡要列出子目錄及其用途
```

### 不包含的內容

- 安裝步驟（NuGet 通用流程）
- 完整 API 文件（已有 XML doc）
- 重複架構文件已有的通用說明
- 使用範例（已有 samples 專案）

## 各專案 README 大綱

### 1. Bee.Base

- **一句話**：跨層共用的基礎工具庫，提供型別轉換、加密、序列化、集合、追蹤等基礎設施
- **層級**：基礎設施（最底層）
- **上游**：幾乎所有其他專案
- **下游**：Newtonsoft.Json
- **關鍵 API**：`BaseFunc`、`StrFunc`、`DateTimeFunc`、`AesCbcHmacCryptor`、`PasswordHasher`、`SerializeFunc`、`Tracer`、`BackgroundService`、`DataTable/DataSet Extensions`
- **慣例**：靜態工具類別模式、常數時間比較（安全）、雙框架條件編譯

### 2. Bee.Definition

- **一句話**：定義驅動架構的核心型別庫，描述表單、資料庫、設定與佈局的結構化定義
- **層級**：基礎設施
- **上游**：Bee.Api.Contracts、Bee.Api.Core、Bee.Repository.Abstractions、Bee.Db、Bee.ObjectCaching、Bee.Business
- **下游**：Bee.Base、MessagePack
- **關鍵 API**：`FormSchema`、`TableSchema`、`FormLayout`、`SystemSettings`、`DatabaseSettings`、`FilterCondition`、`IDefineAccess`、`IBusinessObjectProvider`、`BackendInfo`、`SessionInfo`
- **慣例**：MessagePack + XML 雙序列化、Provider Pattern 透過 BackendInfo 注入

### 3. Bee.Api.Contracts

- **一句話**：API 層與商業邏輯層之間的契約介面庫，定義所有 Request/Response 介面
- **層級**：API 層（契約）
- **上游**：Bee.Api.Core、Bee.Business
- **下游**：Bee.Definition
- **關鍵 API**：`ILoginRequest/Response`、`ICreateSessionRequest/Response`、`IPingRequest/Response`、`IGetDefineRequest/Response`、`IExecFuncRequest/Response`、`IGetPackageRequest/Response`
- **慣例**：純介面定義、MessagePack 序列化屬性

### 4. Bee.Api.Core

- **一句話**：API 基礎框架，處理 JSON-RPC 執行、Payload 加解密管線、授權驗證與型別對應
- **層級**：API 層（核心引擎）
- **上游**：Bee.Api.AspNetCore、Bee.Api.Client
- **下游**：Bee.Api.Contracts、Bee.Definition
- **關鍵 API**：`JsonRpcExecutor`、`ApiServiceOptions`、`ApiPayloadTransformer`（Serialize→Compress→Encrypt 管線）、`ApiAccessValidator`、`ApiContractRegistry`、`SafeMessagePackSerializerOptions`
- **慣例**：Strategy Pattern（可插拔序列化/壓縮/加密）、型別白名單防禦反序列化攻擊

### 5. Bee.Api.AspNetCore

- **一句話**：ASP.NET Core 控制器庫，提供統一的 JSON-RPC 2.0 API 端點
- **層級**：API 層（主機託管）
- **上游**：應用程式（使用者繼承）
- **下游**：Bee.Api.Core
- **關鍵 API**：`ApiServiceController`（抽象基底控制器）
- **慣例**：Template Method Pattern、單一 POST 端點、開發/正式環境錯誤訊息差異化
- **備註**：僅 net10.0

### 6. Bee.Api.Client

- **一句話**：API 用戶端連接器，提供 Local/Remote 雙模式的商業邏輯呼叫統一介面
- **層級**：前端/用戶端
- **上游**：應用程式（WinForms、Blazor 等）
- **下游**：Bee.Api.Core
- **關鍵 API**：`ApiClientContext`、`SystemApiConnector`（Login/Ping/CreateSession）、`FormApiConnector`、`IJsonRpcProvider`（Local/Remote 策略）、`RemoteDefineAccess`
- **慣例**：Strategy Pattern（Local vs Remote Provider）、雙建構子模式（本地/遠端切換）

### 7. Bee.Repository.Abstractions

- **一句話**：資料存取層的抽象介面庫，定義 Repository 與 Provider 的契約
- **層級**：資料存取層（契約）
- **上游**：Bee.Repository、Bee.Business
- **下游**：Bee.Definition
- **關鍵 API**：`ISessionRepository`、`IDatabaseRepository`、`ISystemRepositoryProvider`、`IFormRepositoryProvider`、`RepositoryInfo`
- **慣例**：Repository Pattern、Provider/Factory Pattern、靜態 Service Locator

### 8. Bee.Repository

- **一句話**：Repository 抽象的預設實作，提供 Session 管理、資料庫操作與表單存取的具體實作
- **層級**：資料存取層（實作）
- **上游**：應用程式（透過 RepositoryInfo 注入）
- **下游**：Bee.Db、Bee.Repository.Abstractions
- **關鍵 API**：`SessionRepository`、`DatabaseRepository`、`DataFormRepository`、`ReportFormRepository`、`SystemRepositoryProvider`、`FormRepositoryProvider`
- **慣例**：st_session 表儲存 Session、XML 序列化 SessionUser、自動清理過期 Session

### 9. Bee.Db

- **一句話**：資料庫抽象層，提供動態 SQL 產生、參數化查詢、多資料庫支援與 IL 物件對應
- **層級**：資料存取層（基礎設施）
- **上游**：Bee.Repository
- **下游**：Bee.Definition
- **關鍵 API**：`DbAccess`（主要進入點）、`DbCommandSpec`、`DbBatchSpec`、`SelectCommandBuilder`、`IFormCommandBuilder`、`DbConnectionManager`、`ILMapper<T>`
- **慣例**：Builder Pattern（Query 組合）、Specification Pattern（Command/Batch Spec）、IL Emit 高效能對應、佔位符自動轉換

### 10. Bee.Business

- **一句話**：商業邏輯層，提供認證、Session 管理、定義存取與自訂函式執行的業務物件框架
- **層級**：商業邏輯層
- **上游**：Bee.Api.Core（透過 Provider 呼叫）
- **下游**：Bee.Api.Contracts、Bee.Definition、Bee.Repository.Abstractions
- **關鍵 API**：`IBusinessObject`、`ISystemBusinessObject`（Login/Ping/CreateSession）、`IFormBusinessObject`、`BusinessObjectProvider`、`LoginAttemptTracker`、`ExecFuncArgs/Result`
- **慣例**：Command Pattern（ExecFunc 反射呼叫）、Factory Pattern（BusinessObjectProvider）、帳號鎖定機制（5 次/15 分鐘）

### 11. Bee.ObjectCaching

- **一句話**：執行時期快取層，快取定義資料與 Session 資訊以減少 I/O 操作
- **層級**：基礎設施（快取）
- **上游**：應用程式、Bee.Business（間接）
- **下游**：Bee.Definition
- **關鍵 API**：`CacheFunc`（靜態 Facade）、`ObjectCache<T>`、`KeyObjectCache<T>`、`ICacheProvider`、`LocalDefineAccess`、`CacheItemPolicy`
- **慣例**：Facade Pattern（CacheFunc）、Template Method（ObjectCache 子類別）、Lazy Singleton（CacheContainer）、滑動過期 20 分鐘預設

## 執行順序

依照相依性由下往上撰寫，確保描述上下游時已有對應的 README：

1. **Bee.Base** — 最底層，無內部相依
2. **Bee.Definition** — 依賴 Bee.Base
3. **Bee.Api.Contracts** — 依賴 Bee.Definition
4. **Bee.Db** — 依賴 Bee.Definition
5. **Bee.Repository.Abstractions** — 依賴 Bee.Definition
6. **Bee.Repository** — 依賴 Bee.Db + Abstractions
7. **Bee.ObjectCaching** — 依賴 Bee.Definition
8. **Bee.Business** — 依賴 Contracts + Definition + Repository.Abstractions
9. **Bee.Api.Core** — 依賴 Contracts + Definition
10. **Bee.Api.AspNetCore** — 依賴 Api.Core
11. **Bee.Api.Client** — 依賴 Api.Core

每個專案同時產出英文版與中文版（2 個檔案），完成後 commit & push。

## 預估產出

- 22 個 Markdown 檔案（11 專案 × 2 語言）
- 每份 README 約 80-150 行
