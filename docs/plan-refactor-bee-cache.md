# Bee.Cache 命名空間重構計畫

## 目標

將 Bee.Cache 所有 `.cs` 檔案的命名空間由統一的 `Bee.Cache` 調整為與資料夾結構對應，
同時移除語意不明的 `Common/`、`Info/`、`DefineAccess/` 及冗餘的 `Cache/` 層級資料夾，
並將 `Provider/`、`Service/` 改名以符合命名空間慣例。

---

## 一、資料夾與命名空間對應

| 資料夾 | 命名空間 |
|--------|---------|
| 專案根目錄 | `Bee.Cache` |
| `Define/` | `Bee.Cache.Define` |
| `Database/` | `Bee.Cache.Database` |
| `Runtime/` | `Bee.Cache.Runtime` |
| `Providers/` | `Bee.Cache.Providers` |
| `Services/` | `Bee.Cache.Services` |

---

## 二、各資料夾異動明細

### 2.1 專案根目錄（`Bee.Cache`）

**從 `Common/` 移入（namespace 不變）：**

| 原始路徑 | 異動 |
|---------|------|
| `Common/CacheContainer.cs` | 移至根目錄，namespace 不變 |
| `Common/CacheFunc.cs` | 移至根目錄，namespace 不變 |
| `Common/CacheItemPolicy .cs` | 移至根目錄，**同步修正檔名為 `CacheItemPolicy.cs`**（移除多餘空格），namespace 不變 |
| `Common/DbChangeMonitor.cs` | 移至根目錄，namespace 不變 |

**從 `Common/Common.cs` 拆出（新建檔案）：**

| 型別 | 新檔名 |
|------|--------|
| `CacheTimeKind` | `CacheTimeKind.cs` |

**從 `Info/` 移入（namespace 不變）：**

| 原始路徑 | 異動 |
|---------|------|
| `Info/CacheInfo.cs` | 移至根目錄，namespace 不變 |

**從 `DefineAccess/` 移入（namespace 不變）：**

| 原始路徑 | 異動 |
|---------|------|
| `DefineAccess/LocalDefineAccess.cs` | 移至根目錄，namespace 不變 |

**從 `Cache/` 移入（namespace 不變）：**

| 原始路徑 | 異動 |
|---------|------|
| `Cache/KeyObjectCache.cs` | 移至根目錄，namespace 不變 |
| `Cache/ObjectCache.cs` | 移至根目錄，namespace 不變 |

---

### 2.2 `Define/`（`Bee.Cache.Define`，從 `Cache/Define/` 提升）

| 原始路徑 | 異動 |
|---------|------|
| `Cache/Define/DatabaseSettingsCache.cs` | 移至 `Define/`，namespace 改為 `Bee.Cache.Define` |
| `Cache/Define/DbSchemaSettingsCache.cs` | 移至 `Define/`，namespace 改為 `Bee.Cache.Define` |
| `Cache/Define/DbTableCache.cs` | 移至 `Define/`，namespace 改為 `Bee.Cache.Define` |
| `Cache/Define/FormDefineCache.cs` | 移至 `Define/`，namespace 改為 `Bee.Cache.Define` |
| `Cache/Define/FormLayoutCache.cs` | 移至 `Define/`，namespace 改為 `Bee.Cache.Define` |
| `Cache/Define/ProgramSettingsCache.cs` | 移至 `Define/`，namespace 改為 `Bee.Cache.Define` |
| `Cache/Define/SystemSettingsCache.cs` | 移至 `Define/`，namespace 改為 `Bee.Cache.Define` |

---

### 2.3 `Database/`（`Bee.Cache.Database`，從 `Cache/Db/` 提升並改名）

| 原始路徑 | 異動 |
|---------|------|
| `Cache/Db/SessionInfoCache.cs` | 移至 `Database/`，namespace 改為 `Bee.Cache.Database` |

---

### 2.4 `Runtime/`（`Bee.Cache.Runtime`，從 `Cache/Runtime/` 提升）

| 原始路徑 | 異動 |
|---------|------|
| `Cache/Runtime/ViewStateCache.cs` | 移至 `Runtime/`，namespace 改為 `Bee.Cache.Runtime` |

---

### 2.5 `Providers/`（`Bee.Cache.Providers`，資料夾從 `Provider/` 改名）

| 檔案 | 異動 |
|------|------|
| `Providers/ICacheProvider.cs` | namespace 改為 `Bee.Cache.Providers` |
| `Providers/MemoryCacheProvider.cs` | namespace 改為 `Bee.Cache.Providers`，補上 `using Bee.Cache;`（使用 `CacheFunc`、`CacheItemPolicy`）|

---

### 2.6 `Services/`（`Bee.Cache.Services`，資料夾從 `Service/` 改名）

| 檔案 | 異動 |
|------|------|
| `Services/EnterpriseObjectService.cs` | namespace 改為 `Bee.Cache.Services` |
| `Services/SessionInfoService.cs` | namespace 改為 `Bee.Cache.Services`，補上 `using Bee.Cache;`（使用 `CacheFunc`）|

---

## 三、移除的資料夾

| 資料夾 | 原因 |
|--------|------|
| `Common/` | 語意不明，內容已分散至根目錄，`Common.cs` 刪除（內容拆出） |
| `Info/` | 僅單一類別，不值得獨立層級，`CacheInfo.cs` 移至根目錄 |
| `DefineAccess/` | 僅單一類別，`LocalDefineAccess.cs` 移至根目錄 |
| `Cache/` | 冗餘層級，`ObjectCache.cs`、`KeyObjectCache.cs` 移至根目錄，子資料夾提升為頂層 |

**改名資料夾：**

| 原名 | 新名 | 原因 |
|------|------|------|
| `Cache/Define/` | `Define/` | 提升為頂層，與命名空間 `Bee.Cache.Define` 對齊 |
| `Cache/Db/` | `Database/` | 提升為頂層，與 `Bee.Define.Database` 慣例一致 |
| `Cache/Runtime/` | `Runtime/` | 提升為頂層，與命名空間 `Bee.Cache.Runtime` 對齊 |
| `Provider/` | `Providers/` | 與命名空間 `Bee.Cache.Providers` 對齊 |
| `Service/` | `Services/` | 與命名空間 `Bee.Cache.Services` 對齊 |

---

## 四、影響範圍

### Bee.Cache 內部

- 命名空間變更：11 個 `.cs`（Define/ 7 個、Database/ 1 個、Runtime/ 1 個、Providers/ 2 個、Services/ 2 個）
- 新建檔案：1 個（`CacheTimeKind.cs`）
- 刪除檔案：1 個（`Common/Common.cs`）
- 修正檔名：1 個（`CacheItemPolicy .cs` → `CacheItemPolicy.cs`）
- 純移動（namespace 不變）：8 個

### 下游專案（需更新型別字串）

`Bee.Cache.Providers` 與 `Bee.Cache.Services` 的具象型別透過反射載入，型別全名字串定義於 `src/Bee.Define/Common.cs`，需同步更新：

| 常數 | 原值 | 新值 |
|------|------|------|
| `BackendDefaultTypes.CacheProvider` | `"Bee.Cache.MemoryCacheProvider, Bee.Cache"` | `"Bee.Cache.Providers.MemoryCacheProvider, Bee.Cache"` |
| `BackendDefaultTypes.SessionInfoService` | `"Bee.Cache.SessionInfoService, Bee.Cache"` | `"Bee.Cache.Services.SessionInfoService, Bee.Cache"` |
| `BackendDefaultTypes.EnterpriseObjectService` | `"Bee.Cache.EnterpriseObjectService, Bee.Cache"` | `"Bee.Cache.Services.EnterpriseObjectService, Bee.Cache"` |

### 下游專案（無需修改 `using`）

下列專案使用 `using Bee.Cache;` 存取的型別（`LocalDefineAccess`、`CacheFunc`）仍留在根命名空間，**不需補 using**：

| 專案 | 使用型別 |
|------|---------|
| `tools/BeeSettingsEditor` | `LocalDefineAccess` |
| `tools/BeeDbUpgrade` | `LocalDefineAccess` |
| `tests/Bee.Tests.Shared` | `LocalDefineAccess` |
| `samples/JsonRpcServer` | `LocalDefineAccess` |
| `samples/JsonRpcServerAspNet` | `CacheFunc` |
| `tests/Bee.Cache.UnitTests` | `CacheFunc`（透過父命名空間隱式存取） |

---

## 五、執行順序

1. 將 `CacheTimeKind` 從 `Common/Common.cs` 拆出為 `CacheTimeKind.cs`，刪除 `Common/Common.cs`
2. 將 `Common/`、`Info/`、`DefineAccess/`、`Cache/` 中的檔案移至根目錄（修正 `CacheItemPolicy .cs` 檔名）
3. 建立 `Define/`，移入 `Cache/Define/` 所有檔案，namespace 改為 `Bee.Cache.Define`
4. 建立 `Database/`，移入 `Cache/Db/SessionInfoCache.cs`，namespace 改為 `Bee.Cache.Database`
5. 建立 `Runtime/`，移入 `Cache/Runtime/ViewStateCache.cs`，namespace 改為 `Bee.Cache.Runtime`
6. 將 `Provider/` 改名為 `Providers/`，更新 namespace 為 `Bee.Cache.Providers`
7. 將 `Service/` 改名為 `Services/`，更新 namespace 為 `Bee.Cache.Services`
8. 執行 `dotnet build src/Bee.Cache/Bee.Cache.csproj` 確認專案本身無錯誤
9. 更新 `src/Bee.Define/Common.cs` 中的 3 個型別字串常數
10. 執行 `dotnet build` 全方案建置確認
11. 執行 `dotnet test` 確認測試全數通過
