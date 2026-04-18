# Bee.Definition

[English](README.md)

定義驅動架構的核心型別庫，以結構化定義描述表單、資料庫、設定與畫面佈局。

## 架構定位

**層級**：基礎設施層

Bee.Definition 位於 BeeNET 框架的最底層，提供所有上層共用的型別系統。它定義了定義驅動架構的「語言」——每一個表單、資料庫表、UI 佈局與系統設定，都透過此處宣告的型別來表達。

在 BeeNET 的相依關係圖中，此套件**不包含商業邏輯，也不執行 I/O**。它是純粹的型別與合約庫：介面、POCO、列舉與屬性標籤。這使其保持穩定且輕量——此處的 API 異動會向上波及整個技術堆疊，因此 API 表面採保守演進策略。

- **上游相依**：Bee.Base、MessagePack
- **下游消費者**：Bee.Api.Contracts、Bee.Api.Core、Bee.Repository.Abstractions、Bee.Db、Bee.ObjectCaching、Bee.Business

## 目標框架

| 框架 | 用途 |
|------|------|
| `net10.0` | 使用最新執行時期的最佳化與 API |

## 主要功能

- **FormSchema 作為定義中樞** — 單一 FormSchema 同時驅動 UI 渲染（FormLayout）、資料庫投影（TableSchema）與驗證規則，消除跨層規格不一致的問題。
- **結構化篩選與排序模型** — `FilterCondition` 與 `FilterGroup` 組成樹狀查詢模型，並提供工廠方法（`Equal`、`Contains`、`Between`、`In` 等），實現型別安全的查詢建構。
- **雙軌序列化支援** — 型別同時標註 MessagePack（高效能二進位）與 XML 序列化屬性，兼顧 API 傳輸效率與人類可讀的組態檔案。
- **Provider 模式與 BackendInfo** — 靜態註冊表（`BackendInfo`）持有執行時期的各項 Provider（加密、快取、日誌、Session），以慣例解析，使 Definition 層與具體實作解耦。
- **安全合約** — `IAccessTokenValidationProvider`、`IApiEncryptionKeyProvider` 等介面定義安全邊界，不強制綁定實作細節。
- **DefineType 驅動的 CRUD** — `DefineType` 列舉與 `DefineFunc` 工具類別將定義類別對應至 CLR 型別，透過 `IDefineAccess` 與 `IDefineStorage` 實現泛型載入/儲存。
- **集中式設定模型** — `SystemSettings`、`DatabaseSettings`、`ProgramSettings` 與 `MenuSettings` 提供具型別的組態介面，取代零散的鍵值查詢。

## 主要公開 API

| 型別 | 角色 |
|------|------|
| `FormSchema` | 定義中樞——描述表單的資料表、欄位與中繼資料 |
| `TableSchema` / `DbField` | 資料庫投影——欄位型別、索引、約束條件 |
| `FormLayout` / `LayoutGroup` / `LayoutItem` | UI 投影——欄位排列與分組 |
| `FilterCondition` / `FilterGroup` | 可組合的查詢篩選樹 |
| `SortField` / `SortFieldCollection` | 查詢排序描述 |
| `SystemSettings` / `DatabaseSettings` / `ProgramSettings` | 組態定義型別 |
| `BackendInfo` | 執行時期服務的靜態 Provider 註冊表 |
| `SessionInfo` / `SessionUser` | Session 與使用者上下文 |
| `IDefineAccess` / `IDefineStorage` | 定義載入/儲存合約 |
| `IBusinessObjectProvider` | 商業物件建立的工廠合約 |
| `DefineFunc` | DefineType 至 CLR 型別的解析工具 |
| `BackendDefaultTypes` | 預設 Provider 型別名稱的字串常數 |
| `DefineType` | 列舉所有定義種類（FormSchema、TableSchema、Settings 等） |

## 設計慣例

- **MessagePack `[Key]` + XML `[XmlElement]` 雙重標註** — 每個可序列化屬性同時攜帶兩種屬性標籤，以支援二進位與 XML 兩種通道。
- **Provider 模式** — `BackendInfo` 以介面型別（如 `ILogWriter`、`IApiEncryptionKeyProvider`）公開靜態屬性，具體型別在啟動時透過 `BackendDefaultTypes` 常數註冊。
- **FilterCondition 的工廠方法** — 偏好使用 `FilterCondition.Equal(...)` 而非 `new FilterCondition { ... }`，以提升可讀性與一致性。
- **DefineType 列舉作為分派鍵** — `DefineFunc.GetDefineType()` 將列舉值對應至 CLR 型別，實現泛型定義 CRUD，無需硬編碼型別參考。
- **不可變預設值** — `BackendInfo` 屬性初始化為安全預設值（`NullLogWriter`、空陣列），確保系統不會遇到 null Provider。
- **XML 文件註解使用英文** — 所有公開 API 皆附帶英文 XML 文件，確保 NuGet 使用者在 IntelliSense 中的可讀性。
- **啟用 Nullable Reference Types** — 專案啟用 NRT（`<Nullable>enable</Nullable>`）並將警告視為錯誤，在編譯時期強制 null 安全性。

## 目錄結構

```
Bee.Definition/
  Attributes/       存取控制屬性（ApiAccessControl、ExecFuncAccessControl）
  Collections/      ListItem、Parameter、PropertyCollection
  Database/         TableSchema、DbField、DbFieldCollection、TableSchemaIndex
  Filters/          FilterCondition、FilterGroup、ComparisonOperator、SortField
  Forms/            FormSchema、FormField、FormFieldCollection、FormTable
  Layouts/          FormLayout、LayoutGroup、LayoutItem
  Logging/          ILogWriter、LogEntry、LogOptions
  Security/         IAccessTokenValidationProvider、IApiEncryptionKeyProvider
  Serialization/    自訂 MessagePack 格式化器
  Settings/         SystemSettings、DatabaseSettings、ProgramSettings、MenuSettings、DbSchemaSettings
  Storage/          IDefineStorage
  （根目錄）         BackendInfo、SessionInfo、IDefineAccess、IBusinessObjectProvider、
                    DefineFunc、Common（列舉）、UserInfo
```
