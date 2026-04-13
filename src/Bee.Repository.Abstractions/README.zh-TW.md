# Bee.Repository.Abstractions

> 資料存取層的抽象介面程式庫，定義 Repository 與 Provider 契約。

[English](README.md)

## 架構定位

- **層級**：資料存取層（契約）
- **下游**（依賴此專案者）：`Bee.Repository`、`Bee.Business`
- **上游**（此專案依賴）：`Bee.Definition`

## 目標框架

- `netstandard2.0` -- 廣泛相容 .NET Framework 4.6.1+ 與 .NET Core 2.0+
- `net10.0` -- 使用現代執行階段 API 與效能改進

## 主要功能

### Repository 契約

- `ISessionRepository` -- Session 生命週期操作：透過 Access Token 建立、取得與驗證使用者 Session
- `IDatabaseRepository` -- 資料庫管理操作：連線測試與資料表結構升級

### Provider 契約

- `ISystemRepositoryProvider` -- 聚合系統層級的 Repository（`ISessionRepository`、`IDatabaseRepository`）
- `IFormRepositoryProvider` -- 表單層級 Repository 的工廠，依 ProgId 解析 `IDataFormRepository` 與 `IReportFormRepository`

### 表單 Repository 契約

- `IDataFormRepository` -- 資料表單 CRUD 操作的 Repository 介面
- `IReportFormRepository` -- 報表表單查詢操作的 Repository 介面

### 靜態服務定位器

- `RepositoryInfo` -- 靜態進入點，公開 `SystemProvider` 與 `FormProvider`，從 `BackendConfiguration` 自動初始化

## 主要公開 API

| 介面 / 類別 | 用途 |
|-------------|------|
| `ISessionRepository` | Session 建立（`CreateSession`）與取得（`GetSession`） |
| `IDatabaseRepository` | 連線測試（`TestConnection`）與結構遷移（`UpgradeTableSchema`） |
| `ISystemRepositoryProvider` | 將系統 Repository 聚合為單一 Provider |
| `IFormRepositoryProvider` | 依 ProgId 解析表單 Repository 的工廠 |
| `IDataFormRepository` | 資料表單資料存取契約 |
| `IReportFormRepository` | 報表表單資料存取契約 |
| `RepositoryInfo` | Provider 實例的靜態服務定位器 |

## 設計慣例

- **Repository 模式** -- 每個領域關注點（Session、資料庫、表單）擁有專屬的 Repository 介面。
- **Provider / Factory 模式** -- `ISystemRepositoryProvider` 聚合 Repository；`IFormRepositoryProvider` 作為工廠，依 ProgId 解析 Repository。
- **靜態服務定位器** -- `RepositoryInfo` 在靜態初始化時讀取 `BackendConfiguration`，透過反射（`BaseFunc.CreateInstance`）建立 Provider 實例，支援可設定的預設型別回退。
- **組態驅動實例化** -- Provider 型別名稱定義於 `BackendConfiguration.Components`；自訂實作可在不修改程式碼的情況下替換預設值。
- **啟用 Nullable Reference Types**（`<Nullable>enable</Nullable>`）。

## 目錄結構

```
Bee.Repository.Abstractions/
  Form/                # IDataFormRepository、IReportFormRepository
  Provider/            # ISystemRepositoryProvider、IFormRepositoryProvider
  System/              # ISessionRepository、IDatabaseRepository
  RepositoryInfo.cs    # Provider 實例的靜態服務定位器
```
