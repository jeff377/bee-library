# Bee.ObjectCaching

> 執行階段快取層，快取定義資料與 Session 資訊以減少 I/O 操作。

[English](README.md)

## 架構定位

- **層級**：基礎設施層（快取）
- **下游**（依賴此專案者）：應用程式、`Bee.Business`（間接）
- **上游**（此專案依賴）：`Bee.Definition`、`Microsoft.Extensions.Caching.Memory`

## 目標框架

- `net10.0` -- 使用現代執行階段 API 與效能改進

## 主要功能

### 定義快取

- `SystemSettingsCache` -- 快取系統層級組態
- `DatabaseSettingsCache` -- 快取資料庫連線設定
- `TableSchemaCache` / `FormSchemaCache` -- 快取資料表與表單結構定義
- `FormLayoutCache` -- 快取 UI 佈局中繼資料
- `ProgramSettingsCache` / `DbCategorySettingsCache` -- 快取程式與資料庫類別設定

### Session 快取

- `SessionInfoCache` -- 快取已驗證的 Session 資料，避免重複查詢資料庫

### 快取基礎架構

- `ObjectCache<T>` -- 單一物件快取基底類別，提供範本方法掛鉤（`GetPolicy`、`GetKey`、`CreateInstance`）
- `KeyObjectCache<T>` -- 鍵值快取基底類別，以字串鍵識別物件
- `ICacheProvider` / `MemoryCacheProvider` -- 可插拔的快取儲存提供者
- `CacheItemPolicy` / `CacheTimeKind` -- 到期組態（預設：20 分鐘滑動視窗）

### 服務

- `SessionInfoService` -- 由快取層支援的 Session 生命週期操作
- `EnterpriseObjectService` -- 協調企業範圍的快取物件

### 多租戶客製化覆蓋層

- `ICacheContainerProvider` / `CacheContainerProvider` -- 延遲建立 per-`CustomizeId` 唯讀覆蓋快取容器（`CachePrefix=customizeId`，backing 為 `CustomizeOnlyStorage`），重用既有快取類別、一行不改
- `CustomizeDefineReader` -- `ICustomizeDefineReader` 實作，從 per-租戶覆蓋容器讀取 Language / FormLayout / ProgramSettings；無客製檔回 `null`（見 [ADR-016](../../docs/adr/adr-016-multitenant-customization-overlay.md)）

## 主要公開 API

| 類別 / 介面 | 用途 |
|-------------|------|
| `ICacheContainer` | 由 DI 注入的契約，公開所有快取實例（`SystemSettingsCache`、`FormSchemaCache`、`SessionInfoCache` 等） |
| `CacheContainerService` | `ICacheContainer` 實作，由 `AddBeeFramework` 註冊為 Singleton |
| `ObjectCache<T>` | 單一物件快取基底類別 |
| `KeyObjectCache<T>` | 鍵值快取基底類別 |
| `ICacheProvider` | 快取儲存提供者介面 |
| `CacheDefineAccess` | `IDefineAccess` 實作，從本機快取讀取定義（可選客製化疊加） |
| `ICacheContainerProvider` / `CacheContainerProvider` | per-`CustomizeId` 覆蓋快取容器提供者 |
| `CustomizeDefineReader` | 租戶客製化覆蓋讀取器（`ICustomizeDefineReader`） |
| `CacheItemPolicy` | 到期與清除組態 |
| `CacheInfo` | 快取項目的中繼資料描述 |

## 設計慣例

- **DI 注入** -- 呼叫端以建構子注入 `ICacheContainer`；`CacheContainerService` 實作由 `AddBeeFramework` 註冊為 Singleton，呼叫端透過注入的契約存取各快取類別，不再經由靜態外觀。
- **範本方法模式（Template Method）** -- `ObjectCache<T>` 的子類別覆寫 `GetPolicy`、`GetKey` 與 `CreateInstance`，在不修改基底擷取邏輯的前提下定義快取行為。
- **鍵值正規化** -- 所有快取鍵一律以 `ToLowerInvariant()` 轉為小寫，確保不區分大小寫且不受 culture 影響。
- **底層儲存** -- `MemoryCacheProvider` 包裝 `Microsoft.Extensions.Caching.Memory.IMemoryCache`，公開的 `CacheItemPolicy` 內部映射到 `MemoryCacheEntryOptions`。
- **啟用 Nullable Reference Types**（`<Nullable>enable</Nullable>`）。

## 目錄結構

```
Bee.ObjectCaching/
  Define/      # SystemSettingsCache、DatabaseSettingsCache、TableSchemaCache、
               # FormSchemaCache、FormLayoutCache、ProgramSettingsCache、
               # DbCategorySettingsCache
  Database/    # SessionInfoCache
  Providers/   # ICacheProvider、MemoryCacheProvider
  Services/    # SessionInfoService、EnterpriseObjectService
  *.cs（根目錄）# ICacheContainer、CacheContainerService、ObjectCache、KeyObjectCache、
               # CacheItemPolicy、CacheTimeKind、CacheInfo、
               # CacheDefineAccess、
               # CacheContainerProvider、CustomizeDefineReader
```
