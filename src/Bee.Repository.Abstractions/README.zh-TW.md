# Bee.Repository.Abstractions

> 資料存取層的抽象介面程式庫，定義 Repository 與 Provider 契約。

[English](README.md)

## 架構定位

- **層級**：資料存取層（契約）
- **在相依圖中的位置**：見[專案相依性全景圖](../../docs/dependency-map.zh-TW.md)。**此處不逐一列出** —— 權威來源是 csproj，而散落在每份套件 README 的散文拷貝會漂且無人察覺。它們確實漂了：`Bee.Hosting` 抽出後，有四份 README 的下游數個月都沒把它補上。

## 目標框架

- `net10.0` -- 使用現代執行階段 API 與效能改進

## 主要功能

### Repository 契約

- `ISessionRepository` -- Session 生命週期操作：透過 Access Token 建立、取得與驗證使用者 Session
- `IDatabaseRepository` -- 資料庫管理操作：連線測試與資料表結構升級

### 工廠契約

- `IRepositoryFactory` -- 取得 Repository 的唯一入口，涵蓋兩軸：
  `CreateFormRepository<T>(accessToken, progId)` 為 progId 軸（型別隨 progId 變動），
  `Create<T>(accessToken)` 為框架軸（型別固定，以介面指名）。兩者皆為泛型，新增 Repository 不需異動介面。

### 表單 Repository 契約

- `IDataFormRepository` -- 資料表單 CRUD 操作的 Repository 介面

### 資料庫路由契約

- `IRepositoryDatabaseRouter` -- 依邏輯 `DbScope`（`Common` / `Log` / `Company`）與當前 Session 的 Access Token，解析 Repository 應使用的實體 databaseId

## 主要公開 API

| 介面 / 類別 | 用途 |
|-------------|------|
| `ISessionRepository` | Session 持久化：`GetSession` / `InsertSession` / `UpdateSession` / `DeleteSession` / `DeleteExpiredSessions` |
| `IDatabaseRepository` | 連線測試（`TestConnection`）與結構遷移（`UpgradeTableSchema`） |
| `IRepositoryFactory` | 所有 Repository 的唯一入口，涵蓋兩軸 |
| `IDataFormRepository` | 資料表單資料存取契約 |
| `IRepositoryDatabaseRouter` | 依邏輯 `DbScope` 與 Access Token 解析實體 databaseId |

## 設計慣例

- **Repository 模式** -- 每個領域關注點（Session、資料庫、表單）擁有專屬的 Repository 介面。
- **單一工廠、兩軸解析** -- `IRepositoryFactory` 以註冊表解析綁定 progId 的 Repository，並以介面指名框架 Repository。它取代了三個工廠，其中之一每加一張系統表就長一個方法。
- **被動契約、由 DI 注入** -- 本專案只定義契約，沒有靜態 holder 或服務定位器。具體實作在 DI 容器中註冊並注入到需要之處，不再由靜態進入點解析，也不再讀取靜態 `BackendConfiguration`。
- **啟用 Nullable Reference Types**（`<Nullable>enable</Nullable>`）。

## 目錄結構

```
Bee.Repository.Abstractions/
  AuditLog/                      # IAuditLogRepository、IAuditLogWriteRepository
                                 # 與查詢／記錄型別
  Form/                          # IDataFormRepository
  Factories/                     # IRepositoryFactory
  System/                        # ISessionRepository、IDatabaseRepository
  IRepositoryDatabaseRouter.cs   # 資料庫路由契約（DbScope -> databaseId）
```
