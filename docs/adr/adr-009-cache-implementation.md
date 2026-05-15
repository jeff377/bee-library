# ADR-009：Bee.ObjectCaching 採用 Microsoft.Extensions.Caching.Memory + IChangeToken

## 狀態

已採納（2026-04-28）

## 背景

`Bee.ObjectCaching` 原本以 `System.Runtime.Caching.MemoryCache`（NuGet 套件 `System.Runtime.Caching`）為底層儲存：

1. Microsoft 官方文件已明示 `System.Runtime.Caching` 不建議用於新專案，新專案應改用 `Microsoft.Extensions.Caching.Memory`。
2. `MemoryCache.Default` 在 Linux 上因效能計數器初始化等因素偶發 `NotImplementedException`，導致本專案 CI 多次出現 `CacheInfo.Provider` static initialization race（下游連鎖：`SystemBusinessObject` 整批 NRE，rerun 即綠的典型 flaky 模式）。
3. `CacheItemPolicy` 物件偏重，寫入路徑包含效能計數器開銷、過期掃描頻率高。
4. `ChangeMonitor` 體系老舊，與現代 .NET DI / `IChangeToken` 體系不相容。

Bee.NET 為純 .NET 10 新框架、未發佈、無相容包袱，適合一次完整遷移到現代套件，徹底拋掉 `System.Runtime.Caching`。

## 決策

採「**完全拋棄 `System.Runtime.Caching`，內部全面改用 `Microsoft.Extensions.Caching.Memory` + `IChangeToken`；對外公開 API（`CacheItemPolicy`、`ICacheProvider`）保留為 Bee.NET 自家抽象，內部實作 mapping 到新底層**」設計原則。

### 三項核心要點

1. **公開層保留 Bee.NET 自家抽象**

   - `CacheItemPolicy`：保留為 Bee.NET 自家定義，仍只暴露 `AbsoluteExpiration` / `SlidingExpiration` / `ChangeMonitorFilePaths` 三個欄位
   - `ICacheProvider`：保留作為儲存抽象，未來仍可換成 Redis、`IDistributedCache` 等實作
   - 不直接暴露 `Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions` 或 `IMemoryCache` 給呼叫端

2. **內部實作改寫為 `Microsoft.Extensions.Caching.Memory`**

   - `MemoryCacheProvider` 內部持有 `Microsoft.Extensions.Caching.Memory.MemoryCache`（不再使用 `MemoryCache.Default`，亦不依賴 `System.Runtime.Caching`）
   - `Set` 內部將 `CacheItemPolicy` mapping 到 `MemoryCacheEntryOptions`：
     - `AbsoluteExpiration` / `SlidingExpiration` 直接映射
     - `ChangeMonitorFilePaths` 改用 `PhysicalFileProvider.Watch(name)` 取得 `IChangeToken`，加進 `MemoryCacheEntryOptions.AddExpirationToken(...)`
   - `MemoryCacheProvider` 實作 `IDisposable`，負責釋放 `MemoryCache` 與所有為 `ChangeMonitorFilePaths` 建立的 `PhysicalFileProvider`

3. **介面瘦身與 key 正規化現代化**

   - `ICacheProvider` 移除無生產 caller 的方法：`Trim(int percent)` 與 `GetAllKeys()`
   - `Remove` 從 `object Remove(string key)` 改為 `void Remove(string key)`（生產零 caller 使用回傳值；新底層也是 void）
   - `Get` 標註為 `object? Get(string key)`，明確表達 cache miss 時回傳 null
   - `MemoryCacheProvider.GetCacheKey` 改用 `key.ToLowerInvariant()`：
     - 從 culture-dependent 的 `ToUpper` 改為 culture-invariant，避開 Turkish-I、German ß 等 locale-specific 行為
     - 與現代 .NET / HTTP / REST 慣例（lowercase）對齊
   - `CacheItemPolicy.ChangeMonitorDbKeys` 屬性移除（搭配的 `DbChangeMonitor` 在 [`8099d03`](https://github.com/jeff377/bee-library/commit/8099d03) 已移除，原本就只有測試用，無實際 monitor 接收）

## 結果

### 採納後的依賴關係

| 套件 | 動作 |
|------|------|
| `System.Runtime.Caching` | **移除** |
| `Microsoft.Extensions.Caching.Memory` | **新增**（10.x） |
| `Microsoft.Extensions.FileProviders.Physical` | **新增**（10.x，提供 `PhysicalFileProvider`） |

`Microsoft.Extensions.Primitives`（含 `IChangeToken`）為其他兩者的 transitive dependency，不需顯式加。

### 對外 API 變更

| 對象 | 變更 |
|------|------|
| `ICacheProvider.Trim` | 移除 |
| `ICacheProvider.GetAllKeys` | 移除 |
| `ICacheProvider.Remove` | 回傳改為 `void` |
| `ICacheProvider.Get` | 改為 `object? Get(string key)` |
| `CacheItemPolicy.ChangeMonitorDbKeys` | 移除 |
| `MemoryCacheProvider` | 維持公開類別；新增 `IDisposable` 實作 |
| `CacheFunc.CreateCachePolicy` | 移除（internal method） |
| `CacheItemPolicy.AbsoluteExpiration` / `SlidingExpiration` / `ChangeMonitorFilePaths` | 不變 |
| `CacheInfo.Provider` 等其他類別 | 不變 |

不另升版號（沿用 `4.0.x`），release notes 列出對應表即可。

### 預設組態

- `new MemoryCache(new MemoryCacheOptions())`：所有選項採預設
  - `SizeLimit` 不設（無上限）— Bee.NET 的快取對象（SystemSettings、FormSchemas 等）數量在 dozens 級，不需要 size-based eviction
  - `ExpirationScanFrequency` 採預設 1 分鐘 — 對非 high-throughput 場景足夠
- `PhysicalFileProvider` 不預先共用：每個 cache entry 為其每個監控檔案各自 `new PhysicalFileProvider(directory)`
  - OS file handle 成本低、entry 數量在 dozens 級；共用機制要寫 `static Dictionary<directory, PhysicalFileProvider>` + 生命週期管理 + 引用計數，引入額外複雜度與資源洩漏風險
  - 真遇到 handle 耗盡或數量爆炸再做（單獨 commit 可後續加）
- `MemoryCacheProvider` 不加 `Reset()` / `Clear()` API（YAGNI；目前無 caller 需求）

## 替代方案（已評估後不採納）

1. **保持 `System.Runtime.Caching` 並 work around CI flakiness**（如 `MemoryCache.Default` 改 `new MemoryCache(name)`）
   - 拒絕原因：短期可行但長期仍背負過時套件包袱、與現代 .NET 體系脫節；既然新框架就一次到位

2. **直接暴露 `Microsoft.Extensions.Caching.Memory.IMemoryCache`，移除 `ICacheProvider` 抽象**
   - 拒絕原因：失去未來換 Redis、`IDistributedCache` 的擴充空間；保留抽象成本低收益高

3. **同時維護 `System.Runtime.Caching` 與 `Microsoft.Extensions.Caching.Memory` 兩種 provider**
   - 拒絕原因：引入 dual-stack 維護負擔；違反 Bee.NET 純 .NET 10 的設計取向

4. **直接導入 DI（`IServiceCollection.AddMemoryCache()`）**
   - 拒絕原因：與 Bee.NET 既有 service-locator 模式（`CacheInfo.Provider`）不一致；屬另一個重構議題

## 後續延伸：負向快取（2026-05-15）

`KeyObjectCache<T>.Get` 原本對「`CreateInstance` 回 null」的結果**不寫入**快取——下次同一個 key 再來會穿透到資料源（檔案 IO / DB 查詢）。攻擊者送無效 key、程式 bug 用錯誤 key、上層忘記前置檢查都會放大這個 cache penetration 問題。

[`plan-keyobjectcache-negative-cache`](../plans/plan-keyobjectcache-negative-cache.md) 引入負向快取：

### 設計要點

- **`MissMarker` 哨兵**：`KeyObjectCacheSentinel.MissMarker` 為單一 process-wide `object` 實例（非泛型 static 避免 [S2743]：每個 closed type 都建立獨立哨兵的浪費）。Cache miss 後若 `CreateInstance` 回 null，寫入此哨兵；`Get` 命中哨兵時直接回 null，不再呼叫 `CreateInstance`
- **`GetNegativePolicy(key)` virtual 方法**：預設 5 分鐘**絕對**過期（比正向快取 20 分鐘 sliding 短；絕對過期確保攻擊者反覆戳同 key 不會延長 TTL）。子類 override 回 null 即停用負向快取
- **`Set` / `Remove` 行為不變**：同一個 cacheKey 寫入正向值或 `Remove` 自然覆蓋 / 清除哨兵，不需特別處理

### `SessionInfoCache` 例外停用

`SessionInfoCache.CreateInstance` 永遠回 null（session 入 cache 只走 `Login` 的 `Set` 路徑、不從 backing store 重建）。若啟用負向快取，匿名流量會用任意 access token 灌出大量 marker entry 但無實際保護價值——session lookup 對未知 token 本來就 fast-return null。`SessionInfoCache` override `GetNegativePolicy` 回 null 停用。

其他 `KeyObjectCache<T>` 子類（`FormSchemaCache` / `TableSchemaCache` / `FormLayoutCache`）的 `CreateInstance` 會讀檔，反覆讀無效檔名是真實放大風險，**保留預設**負向快取。

### 對外 API 變更

| 對象 | 變更 |
|------|------|
| `KeyObjectCache<T>.GetNegativePolicy(string key)` | **新增** virtual method，預設回 5 分鐘 absolute TTL；回 null 停用負向快取 |
| `KeyObjectCacheSentinel`（internal） | **新增** static class 持有單一 `MissMarker` 實例 |
| `KeyObjectCache<T>.Get(string key)` | 行為變更：cache miss + `CreateInstance` 回 null 時，依 `GetNegativePolicy` 決定是否寫入哨兵；命中哨兵直接回 null |
| `Set` / `Remove` | 行為不變 |

### 已知影響

- 第二次查詢已知不存在的 key 不再觸發 `CreateInstance`，預設 5 分鐘內穩定回 null
- 既有測試若依賴「`CreateInstance` 每次都被呼叫」的副作用會 fail；本次落地時順帶修正 `KeyObjectCacheTests` 的相關預期

## 相關文件

- 計畫：[`plan-cache-migration.md`](../plans/plan-cache-migration.md)
- 計畫：[`plan-keyobjectcache-negative-cache.md`](../plans/plan-keyobjectcache-negative-cache.md)
- 套件 README：[`src/Bee.ObjectCaching/README.md`](../../src/Bee.ObjectCaching/README.md)
- 相關 commit：[`8099d03`](https://github.com/jeff377/bee-library/commit/8099d03)（移除 `DbChangeMonitor` placeholder）、[`715c159e`](https://github.com/jeff377/bee-library/commit/715c159e)（負向快取）
