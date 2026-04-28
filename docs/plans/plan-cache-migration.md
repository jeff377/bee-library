# 計畫：Bee.ObjectCaching 完全遷移至 Microsoft.Extensions.Caching.Memory

**狀態：✅ 已完成（2026-04-28）**

## 背景

`Bee.ObjectCaching` 目前底層使用 `System.Runtime.Caching.MemoryCache`（NuGet 套件 `System.Runtime.Caching`）。此套件的歷史問題：

1. **官方不推薦新專案使用** — Microsoft 文件明示新專案應使用 `Microsoft.Extensions.Caching.Memory`
2. **跨平台不穩定** — Linux 上 `MemoryCache.Default` 因效能計數器初始化等原因偶發 `NotImplementedException`，導致本專案 CI 多次出現 `CacheInfo.Provider` static initialization race（下游連鎖：`SystemBusinessObject` 整批 NRE，rerun 即綠的 flaky pattern）
3. **API 較笨重** — `CacheItemPolicy` 物件較重、寫入路徑包含 perf counter 開銷、過期掃描頻率高
4. **`ChangeMonitor` 體系老舊** — 與現代 .NET DI / `IChangeToken` 體系不相容

Bee.NET 是純 .NET 10 新框架，**未發佈、無相容包袱**，適合一次完整遷移到現代套件，徹底拋掉 `System.Runtime.Caching`。

### 已先行清理

- `DbChangeMonitor`（placeholder，從未實作 DB 變更追蹤）已於 [`8099d03`](https://github.com/jeff377/bee-library/commit/8099d03) 移除

## 目標

1. `Bee.ObjectCaching` 不再依賴 `System.Runtime.Caching`，純 `Microsoft.Extensions.Caching.Memory` 實作
2. 介面瘦身：移除無生產 caller 的方法，簡化抽象層
3. 解決 CI flakiness 根因（`MemoryCache.Default` 在 Linux 的初始化問題）
4. 現代化：使用 `IChangeToken` 替代 `ChangeMonitor`、`PhysicalFileProvider` 替代 `HostFileChangeMonitor`
5. 不異動套件版本號（沿用 `4.0.x`）

### 不涵蓋

- DI 容器整合（不引入 `IServiceCollection.AddMemoryCache()`，維持 Bee.NET 原本的 service-locator 模式）
- DB 驅動的快取失效（DbChangeMonitor 已移除；未來實作另議）
- 跨 process 分散式快取（`IDistributedCache`）
- 公開 API 行為變更（`CacheItemPolicy`、`ICacheProvider` 等公開類別本身保留，僅內部實作改寫）

## 設計原則

> **完全拋棄 `System.Runtime.Caching`，內部全面改用 `Microsoft.Extensions.Caching.Memory` + `IChangeToken`。對外公開 API（`CacheItemPolicy`、`ICacheProvider`）保留為 Bee.NET 自家抽象，內部實作 mapping 到新底層。**

亦即：
- **公開層**：`CacheItemPolicy`（Bee.ObjectCaching 自家）— 不變或微調
- **內部層**：mapping 到 `MemoryCacheEntryOptions` + `IChangeToken`（Microsoft.Extensions）
- **抽象介面**：`ICacheProvider` 維持，但移除無 caller 的方法

## 變更清單

### A. NuGet 套件變更

**`src/Bee.ObjectCaching/Bee.ObjectCaching.csproj`**

| 動作 | 套件 |
|------|------|
| **移除** | `System.Runtime.Caching` |
| **新增** | `Microsoft.Extensions.Caching.Memory`（10.x） |
| **新增** | `Microsoft.Extensions.FileProviders.Physical`（10.x，用於檔案 watcher） |

`Microsoft.Extensions.Primitives`（含 `IChangeToken`）為其他兩者的 transitive dependency，不需顯式加。

### B. `ICacheProvider` 介面瘦身

[`src/Bee.ObjectCaching/Providers/ICacheProvider.cs`](src/Bee.ObjectCaching/Providers/ICacheProvider.cs)

| 方法 | 動作 | 理由 |
|------|------|------|
| `bool Contains(string key)` | 保留 | 維持 |
| `void Set(string key, object value, CacheItemPolicy policy)` | 保留 | 維持 |
| `object Get(string key)` | 改為 `object? Get(string key)` | nullable 標註精準（cache miss 回傳 null） |
| `object Remove(string key)` | 改為 `void Remove(string key)` | 生產零 caller 使用回傳值；新底層也是 void |
| `long Trim(int percent)` | **移除** | 生產零使用；新底層 `Compact` 介面也不暴露於 `IMemoryCache` |
| `long GetCount()` | 保留 | 仍可呼叫（測試與未來監控使用） |
| `IEnumerable<string> GetAllKeys()` | **移除** | 生產零使用；新底層需 reflection 取 internal `_entries`，代價高無收益 |

### C. `MemoryCacheProvider` 實作改寫

[`src/Bee.ObjectCaching/Providers/MemoryCacheProvider.cs`](src/Bee.ObjectCaching/Providers/MemoryCacheProvider.cs)

| 動作 | 細節 |
|------|------|
| 持有 `Microsoft.Extensions.Caching.Memory.MemoryCache`（取代 `System.Runtime.Caching.MemoryCache`） | 構造傳 `new MemoryCacheOptions()`；不再用 `MemoryCache.Default` |
| 實作 `IDisposable` | 新 `MemoryCache` 是 disposable resource |
| `Set` 內部 mapping | `CacheItemPolicy` → `MemoryCacheEntryOptions`：絕對／滑動到期照映；`ChangeMonitorFilePaths` → 每個檔案建一個 `PhysicalFileProvider.Watch(name)` 取 `IChangeToken`，加進 `options.AddExpirationToken(...)` |
| `GetCount` | 改用 `MemoryCache.Count` 屬性（concrete class 屬性，非介面方法） |
| 大小寫不敏感 key 邏輯 | **改為 `key.ToLowerInvariant()`**（取代 `StrFunc.ToUpper`，原本是 culture-dependent 有 Turkish-I 風險；同時對齊現代 .NET / HTTP / REST 等 lowercase 慣例） |

### D. `CacheFunc.CreateCachePolicy` 移除

[`src/Bee.ObjectCaching/CacheFunc.cs`](src/Bee.ObjectCaching/CacheFunc.cs)

此 internal method 目前的職責是「Bee.ObjectCaching.CacheItemPolicy → System.Runtime.Caching.CacheItemPolicy」轉換。遷移後該轉換邏輯內聚到 `MemoryCacheProvider.Set` 內，`CacheFunc.CreateCachePolicy` **移除**。

### D'. Cache key 正規化方式變更（uppercase → lowercase + invariant）

[`src/Bee.ObjectCaching/Providers/MemoryCacheProvider.cs`](src/Bee.ObjectCaching/Providers/MemoryCacheProvider.cs) 的 `GetCacheKey`：

| 面向 | 舊 | 新 |
|------|-----|-----|
| Case 方向 | UPPER | lower |
| Culture 處理 | `s.ToUpper()`（**culture-dependent**） | `s.ToLowerInvariant()`（**culture-invariant**） |

**為何要改 invariant**：技術用途的 key normalization 必須避開 Turkish-I、German ß 等 locale-specific 行為；`ToLowerInvariant` 是 .NET 對技術 key 的標準寫法。

**為何選 lowercase 不選 uppercase**：與現代 .NET / HTTP / REST 慣例對齊（ASP.NET Core route templates、HTTP/2 headers spec、JSON keys 都是 lowercase）。

**對 Bee.NET 其他常數（`SysProgIds` / `SysFuncIDs` 等）的影響**：無。那些是「值」的命名慣例（如 `"SYS001"`），與「cache key 內部正規化方向」是兩件不同的事。

**對外行為的影響**：`ICacheProvider` 對外 API 不變（key 仍 case-insensitive）；只是內部 storage key 的呈現換邊。**不影響任何 caller**。

### E. `CacheItemPolicy.ChangeMonitorDbKeys` 移除

[`src/Bee.ObjectCaching/CacheItemPolicy.cs`](src/Bee.ObjectCaching/CacheItemPolicy.cs)

此屬性原本搭配 `DbChangeMonitor` 使用，但 DbChangeMonitor 已移除且該屬性僅在 `CacheItemPolicyTests` 自我測試中設值，從未實際接到任何 monitor。**移除屬性**，留空白為未來 IChangeToken-based DB invalidation 留空間。

### F. 測試重寫

[`tests/Bee.ObjectCaching.UnitTests/MemoryCacheProviderTests.cs`](tests/Bee.ObjectCaching.UnitTests/MemoryCacheProviderTests.cs)

| 測試 | 動作 |
|------|------|
| `Set_then_Get` 系列 | 保留 |
| `Contains_*` | 保留 |
| `Set_with_AbsoluteExpiration_*` | 保留 |
| `Set_with_SlidingExpiration_*` | 保留 |
| `Remove_*_ReturnsValue` | 改為 `Remove_*_RemovesEntry`（不再 assert 回傳值） |
| `Trim_*` | **移除**（介面方法已移除） |
| `GetCount_GetAllKeys_ReflectCurrentCache` | 改為 `GetCount_ReflectsCurrentCache`（去掉 GetAllKeys 部分） |
| `Set_ChangeMonitorFilePaths_*` | 保留並可加強：實際觸發 file watcher 變更，驗證 entry 被驅逐 |

[`tests/Bee.ObjectCaching.UnitTests/CacheItemPolicyTests.cs`](tests/Bee.ObjectCaching.UnitTests/CacheItemPolicyTests.cs)

- 移除 `ChangeMonitorDbKeys` 相關 assertion

### G. README 更新

[`src/Bee.ObjectCaching/README.md`](src/Bee.ObjectCaching/README.md) 與 zh-TW 版

- 「Cache Invalidation」段：保留 file-based、移除其他過時提及
- 「Key Public APIs」表：移除 `GetAllKeys`、`Trim`
- Directory Structure 不變（檔案結構未動）
- 補一條「底層實作」說明：「使用 `Microsoft.Extensions.Caching.Memory.IMemoryCache` + `IChangeToken`」

### H. ADR-009（新增）

[`docs/adr/adr-009-cache-implementation.md`](docs/adr/adr-009-cache-implementation.md)

記錄此遷移決策：
- 選擇 `Microsoft.Extensions.Caching.Memory` 而非保留 `System.Runtime.Caching` 的理由
- 公開 `CacheItemPolicy` 保留 Bee.NET 自家定義的理由（不直接暴露 `MemoryCacheEntryOptions`）
- `IChangeToken` 取代 `ChangeMonitor` 的設計

## 影響面

### 檔案變更統計

- 移動／改寫 src 檔案：**3 份**（`MemoryCacheProvider.cs`、`CacheFunc.cs`、`CacheItemPolicy.cs`、`Providers/ICacheProvider.cs`）
- 測試重寫：**2 份**（`MemoryCacheProviderTests.cs`、`CacheItemPolicyTests.cs`）
- csproj 變更：**1 份**
- 文件：README × 2 + 新 ADR-009
- **總計約 8–10 份檔案**

### 對外 API 變更

| 對象 | 變更 |
|------|------|
| `ICacheProvider` 公開介面 | 移除 `Trim` 與 `GetAllKeys`；`Remove` 回傳改 void；`Get` 變 `object?` |
| `CacheItemPolicy.ChangeMonitorDbKeys` 屬性 | 移除（無 caller） |
| `CacheItemPolicy.AbsoluteExpiration` / `SlidingExpiration` / `ChangeMonitorFilePaths` | 不變 |
| 內部 helper `CacheFunc.CreateCachePolicy` | 移除（internal method） |
| `MemoryCacheProvider` 公開類別 | 維持，內部實作改寫；新增 `IDisposable` |
| `CacheInfo.Provider` 等其他類別 | 不變 |

### 外部使用者影響

- 若有自訂 `ICacheProvider` 實作（如 Redis），需更新介面實作（`Trim`/`GetAllKeys` 不再實作；`Remove` 改 void）
- 若有 `Bee.ObjectCaching.MemoryCacheProvider` 直接 `Trim()` 呼叫，需移除（生產零使用，預期外部影響小）

不升版號（沿用 4.0.x），release notes 列出對應表。

## 執行步驟

1. **A 階段：套件切換**
   1. `Bee.ObjectCaching.csproj`：移除 `System.Runtime.Caching`、新增 `Microsoft.Extensions.Caching.Memory` + `Microsoft.Extensions.FileProviders.Physical`
   2. `dotnet restore`

2. **B 階段：介面瘦身**
   1. 修改 `ICacheProvider.cs`：移除 `Trim` / `GetAllKeys`、`Remove` 改 void、`Get` 加 `?`
   2. 此時 build 必失敗（`MemoryCacheProvider` 尚未配合）

3. **C 階段：MemoryCacheProvider 改寫**
   1. 改用 `Microsoft.Extensions.Caching.Memory.MemoryCache`
   2. 實作 `IDisposable`
   3. `Set` 內部 mapping 到 `MemoryCacheEntryOptions` + `PhysicalFileProvider.Watch(...)`
   4. 移除 `using System.Runtime.Caching`
   5. build 應通過 `Bee.ObjectCaching` 專案

4. **D 階段：CacheFunc 與 CacheItemPolicy 清理**
   1. `CacheFunc.cs`：移除 `CreateCachePolicy` 與相關 `using System.Runtime.Caching`
   2. `CacheItemPolicy.cs`：移除 `ChangeMonitorDbKeys` 屬性
   3. `dotnet build` 全 solution 應通過

5. **E 階段：測試重寫**
   1. `MemoryCacheProviderTests.cs`：移除 `Trim`/`GetAllKeys` 測試、`Remove` 不 assert 回傳值、強化 `ChangeMonitorFilePaths` 測試（實際觸發 file change）
   2. `CacheItemPolicyTests.cs`：移除 `ChangeMonitorDbKeys` 測試
   3. `./test.sh` 全綠

6. **驗證**
   1. `dotnet build --configuration Release` 0 警告 0 錯誤
   2. `./test.sh` 全綠（含 DB 整合測試）
   3. **連續 push 3 次觀察 CI 是否仍 flaky**（驗證根因解決）

7. **文件**
   1. README × 2 同步
   2. 撰寫 `docs/adr/adr-009-cache-implementation.md`

## 風險與權衡

| 風險 | 緩解 |
|------|------|
| `PhysicalFileProvider` 持有 file handle，需正確 Dispose | `MemoryCacheEntryOptions.AddExpirationToken` 接收後由 cache 管理；entry eviction 時自動釋放 token；但 PhysicalFileProvider 本身需在 MemoryCacheProvider.Dispose 中清理 |
| 多個 entry 觀察同一檔案會產生多個 `PhysicalFileProvider`，重複資源 | 接受（見 D2）— 首版每個 entry 各自 new；OS handle 成本低、entry 數量在 dozens 級。需要共用時可後續單獨加 |
| 行為微差（過期掃描頻率、eviction 演算法） | 無正確性問題；測試覆蓋過期/驅逐行為即可 |
| 若使用者有自定 `ICacheProvider` 實作，介面改變會破壞 | 接受；release notes 詳列；專案未發佈，影響範圍可控 |
| `MemoryCache` 改 `Microsoft.Extensions.Caching.Memory.MemoryCache` 後，`Count` 是屬性不是方法，呼叫端需改 | 由 `ICacheProvider.GetCount()` 抽象隔離，外部不感知 |
| 切換時 CI 仍會 flaky（理論上不該，但保守觀察） | 連續 3 次 push 驗證；若仍 flaky 則需深入調查並非完全因 System.Runtime.Caching 引起 |

### 不採用的替代方案

1. **保持 `System.Runtime.Caching` 並 work around CI flakiness**（如選項 1：`MemoryCache.Default` 改 `new MemoryCache(name)`）
   - 短期可行但長期仍背負過時套件包袱、與現代 .NET 體系脫節；既然新框架就一次到位
2. **直接暴露 `Microsoft.Extensions.Caching.Memory.IMemoryCache`，移除 `ICacheProvider` 抽象**
   - 失去未來換 Redis、Distributed cache 的擴充空間；保留抽象成本低收益高
3. **同時維護 `System.Runtime.Caching` 與 `Microsoft.Extensions.Caching.Memory` 兩種 provider**
   - 引入 dual-stack 維護負擔；違反 Bee.NET 純 .NET 10 的設計取向
4. **直接導入 DI（`IServiceCollection.AddMemoryCache()`）**
   - 與 Bee.NET 既有 service-locator 模式不一致；屬另一個重構議題

## 驗收標準

- [ ] `Bee.ObjectCaching.csproj` 不含 `System.Runtime.Caching`
- [ ] `Bee.ObjectCaching.csproj` 含 `Microsoft.Extensions.Caching.Memory`
- [ ] 全 repo 不再 `using System.Runtime.Caching`
- [ ] `dotnet build --configuration Release` 0 警告 0 錯誤
- [ ] `./test.sh` 本機全綠（剩 2 個 pre-existing 環境依賴失敗無關）
- [ ] CI 連續 3 次 push 通過（驗證 flakiness 解決）
- [ ] `ICacheProvider` 不再含 `Trim` / `GetAllKeys`，`Remove` 為 void、`Get` 為 `object?`
- [ ] `CacheItemPolicy` 不再含 `ChangeMonitorDbKeys`
- [ ] `CacheFunc.CreateCachePolicy` 已移除
- [ ] `MemoryCacheProvider` 實作 `IDisposable`
- [ ] `MemoryCacheProvider.GetCacheKey` 使用 `key.ToLowerInvariant()`（取代 `StrFunc.ToUpper`）
- [ ] `src/Bee.ObjectCaching/README.md` 與 `README.zh-TW.md` 與實際對齊
- [ ] `docs/adr/adr-009-cache-implementation.md` 已撰寫
- [ ] 套件版本號未變動（仍為 4.0.x）

## 已決定事項（執行時依此辦理）

以下決議已確認，執行時無需再問：

### D1. `MemoryCacheOptions` 不預設值

執行 `new MemoryCache(new MemoryCacheOptions())`，**所有選項用預設值**：
- `SizeLimit`：不設（無上限） — Bee.NET 的 cache 對象（SystemSettings、FormSchemas 等）數量小（dozens 級），不需要 size-based eviction。設了反而會強迫每個 entry 帶 `Size`，徒增複雜
- `CompactionPercentage`：用預設 0.05（5%） — 只在有 SizeLimit 時相關
- `ExpirationScanFrequency`：用預設 1 分鐘 — 對非 high-throughput 場景足夠

**未來有效能瓶頸時再加配置，現在不做。**

### D2. File provider 不預先共用

**每個 cache entry 為其每個監控檔案各自 `new PhysicalFileProvider(directory)`**，不做依 directory 共用的 cache。

理由：
- OS file handle 成本低
- Bee.NET cache entries 數量在 dozens 級
- 共用機制要寫 `static Dictionary<directory, PhysicalFileProvider>` + 生命週期管理 + 引用計數，引入額外複雜度與資源洩漏風險
- 真遇到 handle 耗盡或數量爆炸再做（單獨 commit 可後續加）

### D3. `MemoryCacheProvider` 不加 `Reset()` API

只實作 `IDisposable.Dispose()`，**不額外提供 `Reset()` 或 `Clear()`**。理由：YAGNI；目前無 caller 需求。

需要時可由消費者：
- 呼叫 `((MemoryCache)_cache).Compact(1.0)` 清空（但 _cache 是 private，要再加 internal accessor）
- 或重建 `MemoryCacheProvider` instance

**有實際需求再加。**

### D4. ADR-009 與本 plan 一起執行

ADR-009 在本 plan 的 commit 中一併產出（不要拆兩個 commit）。執行步驟 7「文件」階段就完成。
