# Bee.Repository

> Repository 抽象的預設實作，提供 Session 管理、資料庫操作與表單資料存取。

[English](README.md)

## 架構定位

- **層級**：資料存取層（實作）
- **下游**（依賴此專案者）：應用程式（透過 `RepositoryInfo` 注入）
- **上游**（此專案依賴）：`Bee.Db`、`Bee.Repository.Abstractions`

## 目標框架

- `net10.0` -- 使用現代執行階段 API 與效能改進

## 主要功能

### Session 管理

- `SessionRepository` -- 將 Session 持久化於 `st_session` 資料表，以 XML 序列化 `SessionUser` 資料
- 產生 GUID 格式的 Access Token，確保不可預測的 Session 識別碼
- 支援一次性 Session，取得後自動刪除
- 存取時自動清理過期 Session，使用 UTC 時間比較

### 資料庫操作

- `DatabaseRepository` -- 連線測試，支援參數替換（`{@DbName}`、`{@UserId}`、`{@Password}`）
- 透過 `TableSchemaBuilder` 進行結構升級，配合 FormSchema 驅動的資料表管理

### 表單資料存取

- `DataFormRepository` -- `IDataFormRepository` 的預設實作，依 ProgId 解析，處理資料表單 CRUD
- `ReportFormRepository` -- `IReportFormRepository` 的預設實作，依 ProgId 解析，處理報表表單查詢

### Provider 實作

- `SystemRepositoryFactory` -- 將 `SessionRepository` 與 `DatabaseRepository` 組裝為單一 Provider
- `FormRepositoryFactory` -- 依 ProgId 建立表單 Repository 實例的工廠

## 主要公開 API

| 類別 | 用途 |
|------|------|
| `SessionRepository` | 針對 `st_session` / `st_user` 資料表的 Session CRUD |
| `DatabaseRepository` | 連線測試與結構遷移 |
| `DataFormRepository` | 資料表單資料存取實作 |
| `ReportFormRepository` | 報表表單資料存取實作 |
| `SystemRepositoryFactory` | 預設 `ISystemRepositoryFactory` 實作 |
| `FormRepositoryFactory` | 預設 `IFormRepositoryFactory` 實作 |

## 設計慣例

- **Session 的 XML 序列化** -- `SessionUser` 序列化為 XML 並儲存於 `st_session.session_user_xml`；取得時反序列化還原。
- **連線字串參數替換** -- `DatabaseRepository.TestConnection` 在開啟連線前，替換 `{@DbName}`、`{@UserId}`、`{@Password}` 預留位置。
- **一次性 Session 自動刪除** -- 當 `SessionUser.OneTime` 為 true 時，`GetSession` 回傳後立即刪除該 Session 記錄。
- **過期 Session 清理** -- `GetSession` 比較 `sys_invalid_time` 與 `DateTime.UtcNow`，透明地刪除過時記錄。
- **參數化查詢** -- 所有 SQL 使用 `DbCommandSpec` 搭配位置參數，防止 SQL 注入。
- **啟用 Nullable Reference Types**（`<Nullable>enable</Nullable>`）。

## 目錄結構

```
Bee.Repository/
  Form/       # DataFormRepository、ReportFormRepository
  Factories/   # SystemRepositoryFactory、FormRepositoryFactory
  System/     # SessionRepository、DatabaseRepository
```
