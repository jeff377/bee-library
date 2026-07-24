# 計畫：快取失效模型統一（檔案相依 + DB 相依皆進 CacheItemPolicy）

**狀態：📝 擬定中（2026-07-24）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | storage 回答檔案相依，消除 `is FileDefineStorage` 型別判斷 | ✅ 已完成（2026-07-24） |
| 2 | DB 相依進 `CacheItemPolicy`（notify token 模型） | 📝 待做 |
| 3 | 退役 group 註冊表（`_evictableByGroup` / `TryEvict` / `CacheGroup`） | 📝 待裁決（breaking） |

## 背景

框架體檢（[plan-framework-review.md](plan-framework-review.md)）指出 `Bee.ObjectCaching` 有 8 個定義快取以 `if (_storage is FileDefineStorage)` 做執行期具象型別判斷，屬能力洩漏。追查後發現真正的問題更深一層：**`CacheItemPolicy` 只表達了檔案相依，DB 相依完全在快取類之外**，兩套失效機制性質不同、且靠字串約定隱性對齊。

本計畫把兩種相依統一進 `CacheItemPolicy`，讓「這筆快取如何失效」在單一處完整可讀。

## 現況（2026-07-24 實測）

### 兩套機制並存且不對稱

| | 檔案相依 | DB 相依 |
|---|---|---|
| 性質 | 宣告式／拉取 | 命令式／推送 |
| 宣告位置 | 快取類的 `GetPolicy()` | **快取類之外**（容器註冊表 + 寫入端 Touch） |
| 傳遞載體 | `CacheItemPolicy.ChangeMonitorFilePaths` | 無（不經 policy） |
| 生效路徑 | `MemoryCacheProvider` → `FileModificationToken : IChangeToken` | `DbDefineStorage.Write` → `Touch` → notify 表 → `CacheNotifyPoller` → `ICacheContainer.TryEvict` → `_evictableByGroup[group].Evict(entity)` |

`CacheItemPolicy` 目前僅三個欄位：`AbsoluteExpiration`、`SlidingExpiration`、`ChangeMonitorFilePaths`。

### 已驗證：DB 這條鏈目前是正確的

`DbDefineStorage.Write<T>` 以 `typeof(T).Name` 組 notify key（`$"{typeof(T).Name}:{defineKey}"`），而 `_evictableByGroup` 以 `cache.CacheGroup`（同為 `typeof(T).Name`）建索引 —— **兩端由同一型別推導，因此對齊成立，不是 bug**。

> 審查過程中一度懷疑 `DefineType.Language` 與快取型別 `LanguageResource` 不符會導致失效失敗，經查 `Write<T>` 根本未使用 `DefineType` 列舉，該疑慮不成立。此處記錄以免後人重複懷疑。

### 三個結構性問題

1. **快取類對具象型別相依**：8 個 `is FileDefineStorage`。這些快取的 `PathOptions` 也只為算監控路徑而存在（欄位＋建構子＋`GetPolicy` 共 3 處），而 `FileDefineStorage` 內部本就持有 `PathOptions` 並自行解析每個定義的路徑 —— **路徑知識重複**。
2. **DB 相依無編譯期保障**：`typeof(T).Name` 在寫入端與容器註冊表兩處各自成立，任一端改變命名即靜默失效，無編譯錯誤、無測試把關。
3. **`_evictableByGroup` 硬編快取清單**：`CacheContainerService` 以一個手寫陣列（12 個快取）建立 group→cache 索引，新增快取必須記得加入。

### 附帶風險（非本計畫範圍，但應知悉）

`Write<T>` 的 `typeof(T).Name` **同時**作為 `st_define.define_type` 欄位值與 notify group。重新命名任何定義類別（如 `LanguageResource`）會一併改變資料庫既有資料的比對值 —— 屬資料遷移問題。建議至少於 `Write<T>` 補註解標明此雙重用途。

## 設計

核心洞察：**`MemoryCacheProvider` 已經把檔案相依轉成 `IChangeToken`**（`options.AddExpirationToken(new FileModificationToken(path))`），且 `CacheNotifyPollSession` **已經在維護 `cacheKey → version` 的鏡射表**。DB 相依只需鏡射同一條路即可，不需發明新機制。

### 1. `CacheItemPolicy` 新增對稱欄位

```csharp
/// <summary>檔案相依：這些檔案異動時失效。</summary>
public string[]? ChangeMonitorFilePaths { get; set; } = null;

/// <summary>DB 相依：cache-notify 版本異動時失效。格式 "{group}:{entity}"。</summary>
public string? ChangeNotifyKey { get; set; } = null;
```

新增屬性屬 additive，**非 breaking**。

### 2. 版本表抽為共享服務

把 `CacheNotifyPollSession` 私有的 `_mirror` 提升為 `Bee.ObjectCaching` 的服務：

```csharp
public interface ICacheNotifyVersionStore
{
    long GetVersion(string notifyKey);
    void SetVersion(string notifyKey, long version);
}
```

poller 由「呼叫 `TryEvict`」改為「`SetVersion`」。分層方向無礙（`Bee.Hosting → Bee.ObjectCaching` 相依已存在）。

### 3. 惰性 notify token（與 `FileModificationToken` 同風格）

```csharp
private sealed class CacheNotifyToken : IChangeToken
{
    private readonly ICacheNotifyVersionStore _versions;
    private readonly string _key;
    private readonly long _initialVersion;
    private volatile bool _hasChanged;

    public bool HasChanged
    {
        get
        {
            if (_hasChanged) return true;
            _hasChanged = _versions.GetVersion(_key) != _initialVersion;
            return _hasChanged;
        }
    }

    public bool ActiveChangeCallbacks => false;
    public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
        => CancellationToken.None.Register(callback!, state);
}
```

刻意沿用惰性設計：`FileModificationToken` 的註解已說明「不用背景計時器可避免計時器搶在讀取前逐出」的競態，DB 版沒有理由背離。

### 4. 基底類自動填 `ChangeNotifyKey`

`KeyObjectCache<T>` / `ObjectCache<T>` 在建立 policy 時自動帶入 `CacheGroup + ":" + key`，**個別快取類不需宣告 DB 相依**，新增快取自動具備。

### 5. storage 報告自己的變更訊號

`IDefineStorage` 加預設介面方法，回傳**中性描述子**而非檔案路徑：

```csharp
// Bee.Definition.Storage
public readonly record struct DefineChangeSource
{
    public string[]? FilePaths { get; init; }   // 檔案背後
    public string? NotifyKey { get; init; }     // DB 背後
    public static DefineChangeSource None => default;
}

// IDefineStorage
DefineChangeSource GetChangeSource(DefineType defineType, params string[] keys)
    => DefineChangeSource.None;
```

| 實作 | 回報 |
|------|------|
| `FileDefineStorage` | `FilePaths`（沿用其內部既有的 `_paths.GetXxxFilePath()`） |
| `DbDefineStorage` | `NotifyKey`（階段 2） |
| `CustomizeOnlyStorage` | `FilePaths`（若決定補監控，見下） |

快取端翻譯成 policy：

```csharp
policy.ChangeMonitorFilePaths = _storage.GetChangeSource(DefineType.FormSchema, key).FilePaths;
```

**為何是描述子而非直接傳 `CacheItemPolicy`**：`CacheItemPolicy` 屬 `Bee.ObjectCaching`，而 `IDefineStorage` 屬 `Bee.Definition`，現有方向是 `Bee.ObjectCaching → Bee.Definition`。把 policy 傳進介面等於要求反向相依而成環。跨層傳遞的應是「這個定義怎麼知道它變了」（storage 對自身儲存方式的知識），而非「快取政策怎麼設」（快取層的決策）——型別命名刻意避開 `Cache` 字眼即為守住此界線。

**為何是單一描述子而非多個方法**：若階段 2 另加 `GetChangeNotifyKey`，則每個實作都會對其中一個方法回報「沒有」，不對稱加倍。單一描述子讓每個實作只填自己那一格。

採預設介面方法而非新介面，是因為專案已有此用法（`IFormBoTypeResolver.Resolve(customizeId, progId) => Resolve(progId)`），且對既有實作者非 breaking。

## 階段拆解

### 階段 1 — 消除 `is FileDefineStorage`（非 breaking，獨立有價值）

- `IDefineStorage` 加 `GetChangeMonitorPaths` 預設方法；`FileDefineStorage` 覆寫。
- 8 個快取類的 `GetPolicy` 改用之。
- **保留** `PathOptions` 建構子參數不動（移除屬 breaking ctor 變更，另案評估）。
- 驗收：`grep "is FileDefineStorage"` 為 0；既有測試全過。

**執行結果（2026-07-24）**：8 處型別判斷歸零，全 16 個測試專案通過、既有測試一行未改、建置 0 警告。

實作補充兩點：

- 各快取的 `PathOptions` 欄位已移除（改為僅於建構子驗證非 null），因為它在階段 1 後唯一用途消失，留著會觸發 IDE0052。建構子簽章維持不變，非 breaking。
- `ChangeMonitorFilePaths` **維持 `null` 語意**（無監控路徑時為 null 而非空陣列）。最初版本直接指派空陣列，雖與 provider 行為等價，卻使既有測試 `GetPolicy_NonFileDefineStorage_NoChangeMonitorFilePaths` 失敗——該測試斷言 null。改測試以遷就實作違反本階段「零行為變更」的驗收標準，故保留 null；改用 `DefineChangeSource` 後 `FilePaths` 本身可為 null，此語意自然成立、不需額外判斷。
- 介面初版為 `string[] GetChangeMonitorPaths(...)`，於同日調整為回傳 `DefineChangeSource`。原因：檔案形狀的簽章會讓 DB 實作永遠回報空值，且階段 2 勢必要再改介面。因該方法尚未隨任何版本發布（`v4.15.0` tag 早於此變更），趁未發版調整形狀零成本。

### 待決：`CustomizeOnlyStorage` 目前沒有檔案監控

`CustomizeOnlyStorage`（租戶客製覆蓋層）**不繼承** `FileDefineStorage`，因此舊的 `is FileDefineStorage` 對它一律為 false ——
**租戶客製層的定義快取從未有過檔案監控，只靠 20 分鐘滑動過期**。它同樣是檔案背後的 storage，此缺口在階段 1 之前就存在。

階段 1 刻意**不改變**此行為（維持零行為變更）：`CustomizeOnlyStorage` 未覆寫 `GetChangeMonitorPaths`，繼承預設回傳空。

若要補上，只需讓它覆寫該方法（以其 `CustomizeOnlyPathOptions` 解析路徑）——但那會讓租戶快取**開始**監控檔案，屬行為變更，需獨立評估其對檔案控制代碼數與效能的影響。

### 階段 2 — DB 相依進 policy（核心，additive）

- 新增 `CacheItemPolicy.ChangeNotifyKey`、`ICacheNotifyVersionStore` 與記憶體實作。
- `MemoryCacheProvider` 加掛 `CacheNotifyToken`。
- `CacheNotifyPollSession` 改寫版本表（**暫時同時保留 `TryEvict` 呼叫**，兩條路並行以降風險）。
- 快取基底自動填 `ChangeNotifyKey`。
- 驗收：新增測試驗「版本遞增後下次讀取取得新值」；既有 cache-notify 測試全過。

### 階段 3 — 退役 group 註冊表（breaking，需裁決）

- 移除 `_evictableByGroup` 硬編陣列、`IEvictableCache.Evict`、`ICacheContainer.TryEvict`、`CacheGroup`（或保留 `CacheGroup` 僅供組 notify key）。
- `ICacheContainer.TryEvict` 為公開 API，**需先標 `[Obsolete]` 一個版本**再移除。
- 此階段才真正兌現「消除 `typeof(T).Name` 字串約定耦合」與「新增快取不必改註冊陣列」。

## 相容性

| 變更 | 性質 |
|------|------|
| `CacheItemPolicy.ChangeNotifyKey` 新增屬性 | additive，非 breaking |
| `IDefineStorage.GetChangeMonitorPaths` 預設介面方法 | 非 breaking（既有實作者繼承預設） |
| `ICacheNotifyVersionStore` 新型別 | additive |
| 移除 `ICacheContainer.TryEvict` / `CacheGroup`（階段 3） | **breaking**，需 `[Obsolete]` 過渡 + major 版號 |

## 風險與限制

- **語意不變**：仍受 poll 間隔限制（最多晚一個週期失效），與現況相同 —— 本計畫不改善時效性，只改善模型完整性。
- **`MemoryCacheProvider` 是目前唯一 `ICacheProvider` 實作**。若日後出現分散式 provider，版本表需跨行程共享；屆時 `ICacheNotifyVersionStore` 正是替換點（設計上已預留）。
- 階段 2 兩條路並行期間，同一次通知會同時觸發 token 失效與 `TryEvict`，行為等價、無副作用，但需確認測試不依賴「只被逐出一次」。

## 待裁決事項

- **階段 3 是否執行**：退役 group 註冊表是本計畫的最大結構收益，但唯一的 breaking 來源。可選擇長期維持階段 2 的並行狀態。
- **`CacheGroup` 去留**：階段 3 後仍需要一個 group 字串來組 notify key，可保留 `CacheGroup` 僅此用途，或改由基底以 `typeof(T).Name` 直接推導。
- 若執行階段 3，需確認 `[Obsolete]` 過渡版本與移除版本（現行 4.15.0）。
