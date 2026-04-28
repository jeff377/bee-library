# Bee.ObjectCaching

> 執行階段快取層，快取定義資料與 Session 資訊以減少 I/O 操作。

[English](README.md)

## 架構定位

- **層級**：基礎設施層（快取）
- **下游**（依賴此專案者）：應用程式、`Bee.Business`（間接）
- **上游**（此專案依賴）：`Bee.Definition`、`Microsoft.Extensions.Caching.Memory`、`Microsoft.Extensions.FileProviders.Physical`

## 目標框架

- `net10.0` -- 使用現代執行階段 API 與效能改進

## 主要功能

### 定義快取

- `SystemSettingsCache` -- 快取系統層級組態
- `DatabaseSettingsCache` -- 快取資料庫連線設定
- `TableSchemaCache` / `FormSchemaCache` -- 快取資料表與表單結構定義
- `FormLayoutCache` -- 快取 UI 佈局中繼資料
- `ProgramSettingsCache` / `DbSchemaSettingsCache` -- 快取程式與資料庫結構設定

### Session 與執行階段快取

- `SessionInfoCache` -- 快取已驗證的 Session 資料，避免重複查詢資料庫
- `ViewStateCache` -- 快取使用者互動期間的暫態檢視狀態

### 快取基礎架構

- `ObjectCache<T>` -- 單一物件快取基底類別，提供範本方法掛鉤（`GetPolicy`、`GetKey`、`CreateInstance`）
- `KeyObjectCache<T>` -- 鍵值快取基底類別，以字串鍵識別物件
- `ICacheProvider` / `MemoryCacheProvider` -- 可插拔的快取儲存提供者
- `CacheItemPolicy` / `CacheTimeKind` -- 到期組態（預設：20 分鐘滑動視窗）

### 快取失效機制

- 檔案型失效 -- 透過 `PhysicalFileProvider` 產生的 `IChangeToken`，在底層定義檔案變更時自動清除快取項目

### 服務

- `SessionInfoService` -- 由快取層支援的 Session 生命週期操作
- `EnterpriseObjectService` -- 協調企業範圍的快取物件

## 主要公開 API

| 類別 / 介面 | 用途 |
|-------------|------|
| `CacheFunc` | 靜態外觀 -- `GetSystemSettings`、`GetFormSchema`、`GetSessionInfo` 等 |
| `CacheContainer` | 延遲初始化單例，管理所有快取實例 |
| `ObjectCache<T>` | 單一物件快取基底類別 |
| `KeyObjectCache<T>` | 鍵值快取基底類別 |
| `ICacheProvider` | 快取儲存提供者介面 |
| `LocalDefineAccess` | `IDefineAccess` 實作，從本機快取讀取定義 |
| `CacheItemPolicy` | 到期與清除組態 |
| `CacheInfo` | 快取項目的中繼資料描述 |

## 設計慣例

- **外觀模式（Facade）** -- `CacheFunc` 公開扁平的靜態 API，對呼叫端隱藏 `CacheContainer` 與各快取類別的複雜度。
- **範本方法模式（Template Method）** -- `ObjectCache<T>` 的子類別覆寫 `GetPolicy`、`GetKey` 與 `CreateInstance`，在不修改基底擷取邏輯的前提下定義快取行為。
- **延遲單例（Lazy Singleton）** -- `CacheContainer` 使用 `Lazy<T>` 延遲至首次存取時才初始化。
- **鍵值正規化** -- 所有快取鍵一律以 `ToLowerInvariant()` 轉為小寫，確保不區分大小寫且不受 culture 影響。
- **檔案型失效機制** -- 透過 `PhysicalFileProvider` 產生的 `IChangeToken`，在來源定義檔案變更時清除快取項目。（資料庫驅動的失效機制規劃中，尚未實作。）
- **底層儲存** -- `MemoryCacheProvider` 包裝 `Microsoft.Extensions.Caching.Memory.IMemoryCache`，公開的 `CacheItemPolicy` 內部映射到 `MemoryCacheEntryOptions`。
- **啟用 Nullable Reference Types**（`<Nullable>enable</Nullable>`）。

## 目錄結構

```
Bee.ObjectCaching/
  Define/      # SystemSettingsCache、DatabaseSettingsCache、TableSchemaCache、
               # FormSchemaCache、FormLayoutCache、ProgramSettingsCache、
               # DbSchemaSettingsCache
  Database/    # SessionInfoCache
  Runtime/     # ViewStateCache
  Providers/   # ICacheProvider、MemoryCacheProvider
  Services/    # SessionInfoService、EnterpriseObjectService
  *.cs（根目錄）# CacheFunc、CacheContainer、ObjectCache、KeyObjectCache、
               # CacheItemPolicy、CacheTimeKind、CacheInfo、
               # LocalDefineAccess
```
