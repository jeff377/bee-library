# 計畫：KeyObjectCache 負向快取（Negative Caching）

**狀態：✅ 已完成（2026-05-15）**

> **本計畫為 [plan-system-bo-session-lifecycle.md](plan-system-bo-session-lifecycle.md) 的前置計畫。**
> 後者新增的 `CompanyInfoCache` 需要負向快取支援；先完成本 plan 再進入 session lifecycle 的 P2。

## 背景

`KeyObjectCache<T>.Get(string key)` 目前的行為：

```csharp
public virtual T? Get(string key)
{
    string cacheKey = GetCacheKey(key);
    if (CacheInfo.Provider.Get(cacheKey) is T cached)
        return cached;                                                  // 正向命中

    var value = CreateInstance(key);
    if (value != null)
        CacheInfo.Provider.Set(cacheKey, value!, GetPolicy(key));       // 重建成功才寫入
    return value;                                                       // 重建失敗回 null，下次再來照樣穿透
}
```

當 `CreateInstance(key)` 對無效 / 不存在的 key 回 null 時，**不寫入任何快取**——下次同一個 key 再來，又會走一次 `CreateInstance`（多半是檔案 IO 或 DB 查詢）。

業界稱為 **cache penetration**：無效 key 反覆穿透快取直擊資料源。常見觸發：

- 攻擊者刻意送無效 key（cache stampede 放大）
- 程式 bug 用錯誤 key 反覆查
- 上層忘記前置檢查就直接呼叫 cache

影響範圍：所有 `KeyObjectCache<T>` 子類（`SessionInfoCache`、`FormSchemaCache`、`TableSchemaCache`、`DatabaseSettingsCache`、`DbCategorySettingsCache`、`ProgramSettingsCache`、`FormLayoutCache`、`SystemSettingsCache`）。

## 目標

讓 `KeyObjectCache<T>` 對「重建失敗」結果也快取（**negative caching**），用短 TTL 阻擋穿透，但允許資料真正出現後在合理時間內被發現。

## 非目標

- 不處理快取大小上限 / LRU 淘汰（`ICacheProvider` 層的責任，與本計畫正交）
- 不引入 cache stampede 防護（多執行緒同時 miss 同 key 觸發多次 CreateInstance；屬另一主題）
- 不改 `Set` / `Remove` 行為（同一 cacheKey，正向寫入或 Remove 自然覆蓋 / 移除哨兵）

## 已確認的設計決議

### D1：哨兵（sentinel）型別

採 `static readonly object MissMarker = new()` 哨兵：

```csharp
public abstract class KeyObjectCache<T> where T : class
{
    private static readonly object MissMarker = new();
    // ...
}
```

**Why 不用 null 直接存**：避開 `ICacheProvider` 對 null 的歧義解讀（某些 provider 把 null 視為「未設定」）。`object` 哨兵能與任何型別共存且 reference equality 比較成本為零。

### D2：負向 TTL 預設值——絕對過期 5 分鐘

```csharp
protected virtual CacheItemPolicy? GetNegativePolicy(string key)
    => new CacheItemPolicy(CacheTimeKind.AbsoluteTime, 5);
```

| 選擇 | 理由 |
|------|------|
| 5 分鐘 | 比正向 cache 預設 20 分鐘 sliding 短；無效 key 上限不會永久卡 cache；資料真實新增後 5 分鐘內可見 |
| **絕對過期**（非 sliding） | 攻擊者反覆戳同 key 不會延長 sentinel；保證上限固定 |
| `virtual` | 子類可依場景調整（更短 / 更長 / 停用） |

### D3：子類停用機制

`GetNegativePolicy` 回 null 即停用負向快取：

```csharp
else if (GetNegativePolicy(key) is { } negPolicy)
    CacheInfo.Provider.Set(cacheKey, MissMarker, negPolicy);
```

讓某些 cache（`CreateInstance` 永遠回 null 但仍會 Set 注入）可以選擇不快取 miss。

### D4：基底類別 `Get` 修改

```csharp
public virtual T? Get(string key)
{
    string cacheKey = GetCacheKey(key);
    var cached = CacheInfo.Provider.Get(cacheKey);

    if (ReferenceEquals(cached, MissMarker))
        return null;                                                   // 命中負向：直接回 null，不進 CreateInstance

    if (cached is T t)
        return t;                                                       // 命中正向

    var value = CreateInstance(key);
    if (value != null)
    {
        CacheInfo.Provider.Set(cacheKey, value, GetPolicy(key));
    }
    else if (GetNegativePolicy(key) is { } negPolicy)
    {
        CacheInfo.Provider.Set(cacheKey, MissMarker, negPolicy);
    }
    return value;
}
```

判斷順序：**先檢查 MissMarker 再檢查 T**——避免 MissMarker 被 `is T` 匹配（雖然 `object` 不會匹配絕大多數 T，但顯式判斷較安全）。

### D5：`Set` / `Remove` 行為不變

`Set(key, value)` 用同一個 `cacheKey` 覆蓋——自然把 MissMarker 蓋掉。
`Remove(key)` 用同一個 `cacheKey` 移除——自然清掉 MissMarker。

不需要為哨兵做特別處理。

## 子類審視（P2 階段執行）

進入實作時，逐一審視 8 個現有子類的 `CreateInstance` 行為，決定是否 override `GetNegativePolicy` 停用：

| Cache | CreateInstance 行為 | 預設（不 override） | 建議 |
|-------|-----|----------|------|
| `SessionInfoCache` | 永遠回 null（DB 載入未實作；只靠 `Set` 注入） | 會把所有 miss 快取住，導致 Login 後的 `Set` 之前的查詢被卡 5 分鐘 | **override 回 null（停用）** |
| `FormSchemaCache` | 從 Define 檔案載入 | 無效 form 名稱反覆讀檔有放大風險 | **保留預設（開啟）** |
| `TableSchemaCache` | 同上 | 同上 | **保留預設（開啟）** |
| `FormLayoutCache` | 同上 | 同上 | **保留預設（開啟）** |
| `DatabaseSettingsCache` | 從 Define 載入 | 同上 | **保留預設（開啟）** |
| `DbCategorySettingsCache` | 同上 | 同上 | **保留預設（開啟）** |
| `ProgramSettingsCache` | 同上 | 同上 | **保留預設（開啟）** |
| `SystemSettingsCache` | 同上 | 同上 | **保留預設（開啟）** |

> 表中行為描述屬設計推測；實作 P2 時讀過每個 `CreateInstance` 再下最終決定。若某 cache 有特殊情境也標 override + 註解原因。

## 影響檔案清單

| 檔案 | 變更 |
|------|------|
| `src/Bee.ObjectCaching/KeyObjectCache.cs` | 核心改動：加 `MissMarker`、`GetNegativePolicy`、改 `Get` 邏輯 |
| `src/Bee.ObjectCaching/Database/SessionInfoCache.cs` | override `GetNegativePolicy` 回 null 停用 |
| 其他 7 個子類 | P2 審視後依需要 override；預估多數**不需修改** |
| `tests/Bee.ObjectCaching.UnitTests/KeyObjectCacheTests.cs` | 新增負向快取測試集 |

## 測試策略

新增測試（針對 `KeyObjectCache<T>` 基底行為）：

- `Get_FirstMissAfterFailedCreate_CachesNegativeMarker` —— 第一次 miss + CreateInstance 回 null → 應寫入 marker
- `Get_SecondCallWithinNegativeTtl_DoesNotInvokeCreateInstance` —— TTL 內再呼叫應命中 marker，不走 CreateInstance
- `Get_AfterNegativeTtlExpires_InvokesCreateInstanceAgain` —— TTL 過後（用 fake clock 或短 TTL 設定）應重新走 CreateInstance
- `Get_GetNegativePolicyReturnsNull_DoesNotCacheMiss` —— 子類停用時不寫 marker，行為與舊版一致
- `Set_OverwritesNegativeMarker` —— 顯式 `Set` 應蓋掉 marker（之後 `Get` 回真實物件）
- `Remove_ClearsNegativeMarker` —— `Remove` 應清掉 marker（之後 `Get` 重新走 CreateInstance）
- `Get_NegativeMarkerNotReturnedAsT` —— marker 物件不會被誤判為 T 型別

針對 `SessionInfoCache` override：

- `SessionInfoCache_GetNegativePolicy_ReturnsNull` —— 確認停用
- `SessionInfoCache_RepeatedGetMisses_StillInvokeCreateInstance` —— 確認 override 生效，miss 不被快取

## 階段拆分

| Phase | 內容 | 可獨立 commit |
|-------|------|--------------|
| **P1** | 修改 `KeyObjectCache<T>` 基底：加 `MissMarker` / `GetNegativePolicy` / 改 `Get` | ✅ |
| **P2** | 子類審視 + `SessionInfoCache` override（與其他必要 override） | ✅ |
| **P3** | 新增 `KeyObjectCacheTests` 負向快取測試集；既有子類測試確認未壞 | ✅ |
| **P4** | 全 Solution build + test 驗證（所有 cache 在現有流程下行為符合預期） | ✅ |

## 後續銜接

完成本 plan 後，[plan-system-bo-session-lifecycle.md](plan-system-bo-session-lifecycle.md) 進入 P2 時：

- `CompanyInfoCache` 直接套用新基底，**不需 override**（自動享有 5 分鐘負向快取）
- `SessionInfoCache` 已在本 plan 完成 override
- 後續任何新增 `KeyObjectCache<T>` 子類預設享有負向快取，特殊情境自行 override

## 風險與注意事項

1. **行為改變對既有功能的影響**：
   - 既有測試若依賴「重複 Get 都會呼叫 CreateInstance」的副作用會 fail——預期需要少量測試修正（負向快取是有意行為）
   - 業務層若依賴「Get 永遠重試」的行為（譬如等待某個資料即將被建立），會延遲 5 分鐘才看到——P2 審視時注意
2. **記憶體成長**：負向快取仍占用 cache slot，無界 cache provider 配上海量無效 key 可放大記憶體用量。本計畫**不**處理此問題，視為 `ICacheProvider` 的責任（若使用 in-memory provider 已有自身 eviction 機制）
3. **多執行緒 cache stampede**：本計畫**不**處理。負向快取已能減緩穿透，但同一瞬間多執行緒同時 miss 仍會觸發多次 CreateInstance；屬獨立議題
