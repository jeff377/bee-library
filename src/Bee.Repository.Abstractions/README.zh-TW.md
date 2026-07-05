# Bee.Repository.Abstractions

> 資料存取層的抽象介面程式庫，定義 Repository 與 Provider 契約。

[English](README.md)

## 架構定位

- **層級**：資料存取層（契約）
- **下游**（依賴此專案者）：`Bee.Repository`、`Bee.Business`
- **上游**（此專案依賴）：`Bee.Definition`

## 目標框架

- `net10.0` -- 使用現代執行階段 API 與效能改進

## 主要功能

### Repository 契約

- `ISessionRepository` -- Session 生命週期操作：透過 Access Token 建立、取得與驗證使用者 Session
- `IDatabaseRepository` -- 資料庫管理操作：連線測試與資料表結構升級

### Provider 契約

- `ISystemRepositoryFactory` -- 聚合系統層級的 Repository（`ISessionRepository`、`IDatabaseRepository`）
- `IFormRepositoryFactory` -- 表單層級 Repository 的工廠，依 ProgId 解析 `IDataFormRepository` 與 `IReportFormRepository`

### 表單 Repository 契約

- `IDataFormRepository` -- 資料表單 CRUD 操作的 Repository 介面
- `IReportFormRepository` -- 報表表單查詢操作的 Repository 介面

### 資料庫路由契約

- `IRepositoryDatabaseRouter` -- 依邏輯 `DbScope`（`Common` / `Log` / `Company`）與當前 Session 的 Access Token，解析 Repository 應使用的實體 databaseId

## 主要公開 API

| 介面 / 類別 | 用途 |
|-------------|------|
| `ISessionRepository` | Session 建立（`CreateSession`）與取得（`GetSession`） |
| `IDatabaseRepository` | 連線測試（`TestConnection`）與結構遷移（`UpgradeTableSchema`） |
| `ISystemRepositoryFactory` | 將系統 Repository 聚合為單一 Provider |
| `IFormRepositoryFactory` | 依 ProgId 解析表單 Repository 的工廠 |
| `IDataFormRepository` | 資料表單資料存取契約 |
| `IReportFormRepository` | 報表表單資料存取契約 |
| `IRepositoryDatabaseRouter` | 依邏輯 `DbScope` 與 Access Token 解析實體 databaseId |

## 設計慣例

- **Repository 模式** -- 每個領域關注點（Session、資料庫、表單）擁有專屬的 Repository 介面。
- **Provider / Factory 模式** -- `ISystemRepositoryFactory` 聚合 Repository；`IFormRepositoryFactory` 作為工廠，依 ProgId 解析 Repository。
- **被動契約、由 DI 注入** -- 本專案只定義契約，沒有靜態 holder 或服務定位器。具體實作在 DI 容器中註冊並注入到需要之處，不再由靜態進入點解析，也不再讀取靜態 `BackendConfiguration`。
- **啟用 Nullable Reference Types**（`<Nullable>enable</Nullable>`）。

## 目錄結構

```
Bee.Repository.Abstractions/
  Form/                          # IDataFormRepository、IReportFormRepository
  Factories/                     # ISystemRepositoryFactory、IFormRepositoryFactory
  System/                        # ISessionRepository、IDatabaseRepository
  IRepositoryDatabaseRouter.cs   # 資料庫路由契約（DbScope -> databaseId）
```
