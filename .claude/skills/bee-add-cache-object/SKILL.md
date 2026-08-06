---
name: bee-add-cache-object
description: bee-library 新增框架快取物件的完整跨檔流程，分兩類——Define 定義快取（來源是定義檔，經 IDefineAccess）與 Database 資料庫相依快取（來源是 DB，經 ICacheDataSourceProvider 自載 + cache-notify 失效）。含 ObjectCache vs KeyObjectCache 決策樹、ICacheContainer + CacheContainerService 兩處同步（漏補必 CS0535）、DI 相依環的延遲解析與 cache-notify 失效鏈。當使用者要「新增快取物件」、「加一個 cache」、「快取某定義 / 資料庫資料」、「KeyObjectCache / ObjectCache」、「cache-notify 失效」、「判權限/查設定要零 DB」之類需求時使用。
---

# bee-library 新增快取物件

bee-library 的快取分**兩類**，來源與失效機制不同，檔案鏈也不同。先用決策樹定位，再照對應路徑走。每條路徑都會動到 `ICacheContainer` + `CacheContainerService` + 兩個 `CacheNotify` 測試 stub——這三點漏一個就 `CS0535` build 失敗（個別專案 build 抓不到，**只有 `dotnet build Bee.Library.slnx` 複現 CI strict build 才會現**）。

> 樣板對照（讀程式碼時對著看）：
> - Define 快取（single）：`PermissionModelsCache`（`ObjectCache<PermissionModels>`）
> - Define 快取（keyed）：`FormSchemaCache`（`KeyObjectCache<FormSchema>`，by progId）
> - Database 快取（keyed）：`CompanyRolePermissionsCache` / `CompanyInfoCache`（`KeyObjectCache<T>`，by id，`CreateInstance` 經 `ICacheDataSourceProvider` 自載）

## 決策樹

### 第一刀：資料來源是什麼？

| 來源 | 類別 | 資料夾 | 失效機制 | 樣板 |
|------|------|--------|---------|------|
| **定義檔**（XML，經 `IDefineAccess`） | **Define 快取** | `Bee.ObjectCaching/Define/` | `CreateInstance` 自載；`SaveDefine` 時清 | `PermissionModelsCache` / `FormSchemaCache` |
| **資料庫**（runtime 資料） | **Database 快取** | `Bee.ObjectCaching/Database/` | `CreateInstance` 經 `ICacheDataSourceProvider` 自載；cache-notify 輪詢清 | `CompanyRolePermissionsCache` / `CompanyInfoCache` |

### 第二刀：single 還是 keyed？（兩類都適用）

| 基類 | 語意 | 範例 |
|------|------|------|
| `ObjectCache<T>` | **整份只有一個物件**（無 key） | `SystemSettings` / `DatabaseSettings` / `ProgramSettings` / `PermissionModels` |
| `KeyObjectCache<T>` | **多個實例，by key**（progId / company id / token） | `FormSchema` / `TableSchema`（by progId）、`CompanyInfo` / `SessionInfo` / `CompanyRolePermissions`（by id） |

- `KeyObjectCache<T>` 要求 `T` 實作 `IKeyObject`（`string GetKey()`）。
- **兩類的 `CreateInstance(key)` 都自載**：Define 快取從 `IDefineAccess` 取，Database 快取經
  `ICacheDataSourceProvider` 取。差別只在資料來源與失效機制，不在載入位置。

> **2026-07-29 起的慣例變更**：Database 快取原本一律 `CreateInstance => null`、由 service 做
> 「`Get` 落空 → repository → `Set` 回填」。該寫法等於在每個 service 手刻一份 read-through，
> 且繞過 base class 已內建的負向快取。現已全數改為自載——`CompanyInfoCache` /
> `CompanyRolePermissionsCache` / `DepartmentTreeCache` 皆是。
> **新增快取請勿再沿用 `=> null` 的舊樣板**（`SessionInfoCache` 仍回 `null`，那是尚未接上
> 持久化的待辦，不是樣板）。

---

## 路徑 A：Define 定義快取

來源是定義檔、經 `IDefineAccess` 取用。新增一個（如線 A 的 `PermissionModels`）跨 **Definition + ObjectCaching** 兩專案。

### 檔案鏈

| # | 檔案 | 慣例 |
|---|------|------|
| 1 | `src/Bee.Definition/<Area>/<Name>.cs` | POCO 定義類；keyed 版實作 `IKeyObject` |
| 2 | `src/Bee.Definition/DefineType.cs` | 加 enum 值 `<Name>` |
| 3 | `src/Bee.Definition/DefineTypeExtensions.cs` | 映射 `{ DefineType.<Name>, "<full type name>" }` |
| 4 | `src/Bee.Definition/PathOptions.cs` | 加 `Get<Name>FilePath()` |
| 5 | `src/Bee.Definition/Storage/IDefineAccess.cs` | 加 DIM `<Name> Get<Name>() => (<Name>)GetDefine(DefineType.<Name>);` |
| 6 | `src/Bee.ObjectCaching/Define/<Name>Cache.cs` | `: ObjectCache<T>`（single）或 `: KeyObjectCache<T>`（keyed），`CreateInstance` 從 `DefineAccess` 載 |
| 7 | `src/Bee.ObjectCaching/CacheDefineAccess.cs`（伺服端）、`src/Bee.Api.Client/ClientDefineAccess.cs`（用戶端） | 如該定義兩端都要取用。（2026-08-06 覆核：舊的 `LocalDefineAccess` / `RemoteDefineAccess` 已不存在）|
| 8 | `src/Bee.ObjectCaching/ICacheContainer.cs` | 加 `<Name>Cache <Name> { get; }` |
| 9 | `src/Bee.ObjectCaching/CacheContainerService.cs` | **兩處**（見下方共用段） |
| 10 | 兩個測試 stub | **必補**（見下方共用段） |

- DIM（default interface method）讓既有 `IDefineAccess` 實作者免改。
- `DefineTypeExtensions` 映射的 type name 要與 POCO 全名一致（反序列化用）。

---

## 路徑 B：Database 資料庫相依快取

來源是資料庫、runtime 載入、靠 cache-notify 失效。目的通常是「**判定/查詢零 DB**」（如線 B 判權限完全走快取）。新增一個（如 `CompanyRolePermissions`）跨 **Definition + ObjectCaching + Repository + Hosting**。

### 檔案鏈

| # | 檔案 | 慣例 |
|---|------|------|
| 1 | `src/Bee.Definition/<Area>/<Name>.cs` | POCO 實作 `IKeyObject`（`GetKey() => <CacheKey>`）；純資料 + 查詢方法，無 DB |
| 2 | `src/Bee.Definition/ICacheDataSourceProvider.cs` | 加取數方法 `<T>? Get<Name>(string key)`——**必須回傳 `Bee.Definition` 的型別**（見下方相依限制） |
| 3 | `src/Bee.Business/Providers/CacheDataSourceProvider.cs` | 實作該方法：由 `IRepositoryFactory` 取 repository、組裝 POCO |
| 4 | `src/Bee.ObjectCaching/Database/<Name>Cache.cs` | `: KeyObjectCache<T>`，`CreateInstance` 呼叫 provider（見樣板） |
| 5 | `src/Bee.Definition/<Area>/I<Name>Service.cs` | `Get(string key)` / `Remove(string key)`——上層不必相依 `Bee.ObjectCaching` 的分層邊界 |
| 6 | `src/Bee.ObjectCaching/Services/<Name>Service.cs` | **單行委派**到 cache；載入邏輯不在這裡 |
| 7 | `src/Bee.Repository.Abstractions/.../I<X>Repository.cs` + `src/Bee.Repository/.../<X>Repository.cs` | 資料來源（DB 讀取）；**同時在 `IRepositoryFactory` 加對應的 `Create<T>()` 解析** |
| 8 | `src/Bee.ObjectCaching/ICacheContainer.cs` | 加 `<Name>Cache <Name> { get; }` |
| 9 | `src/Bee.ObjectCaching/CacheContainerService.cs` | **兩處**（見下方共用段）；ctor 把 `dataSource` 傳給新 cache |
| 10 | `src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs` | 只註冊 service；**不要**逐一註冊 repository（見下方） |
| 11 | 兩個測試 stub | **必補**（見下方共用段） |
| (12) | cache-notify bump 點 | 寫配置的 BO/Repository 在**同 transaction** `ICacheNotifyService.Touch(cacheKey, tx, dbType)`（見下方） |

### 相依限制：為何取數方法要回傳 domain 型別

`ICacheDataSourceProvider` 位於 `Bee.Definition`，而 `Bee.Repository.Abstractions`
**反向相依** `Bee.Definition`（`ICompanyRepository.GetById` 回傳 `CompanyInfo`）。
若取數方法回傳 repository 型別，`Bee.Definition` 就得引用 `Bee.Repository.Abstractions`
→ **專案循環參考，編譯不過**。

所以：POCO 放 `Bee.Definition`，provider 回傳該 POCO，`Bee.ObjectCaching` 只看得到介面。

### Cache 樣板（path B）

```csharp
namespace Bee.ObjectCaching.Database
{
    public class <Name>Cache : KeyObjectCache<<T>>
    {
        private readonly Func<ICacheDataSourceProvider>? _dataSource;

        /// <param name="cachePrefix">Per-owner cache namespace.</param>
        public <Name>Cache(string cachePrefix = "") : this(null, cachePrefix) { }

        /// <remarks>
        /// WARNING: dataSource must stay a factory — see CompanyInfoCache for the cycle it avoids.
        /// </remarks>
        internal <Name>Cache(Func<ICacheDataSourceProvider>? dataSource, string cachePrefix)
            : base(cachePrefix)
        {
            _dataSource = dataSource;
        }

        protected override <T>? CreateInstance(string key)
            => _dataSource?.Invoke().Get<Name>(key);
    }
}
```

**兩個容易踩的形狀約束**：

- **帶 `dataSource` 的建構式必須 `internal`**。若做成 public，`RS0026` / `RS0027` 會擋下——
  不允許兩個 public 多載都帶選擇性參數，且帶選擇性參數者必須是參數最多的多載。
  `CacheContainerService` 同組件，`internal` 即足夠，且公開表面不變動。
- **`dataSource` 必須是 `Func<T>` 而非實例**（見下方 DI 段的相依環）。

### Service 樣板（path B）

載入邏輯已在 cache，service 收斂為分層邊界上的單行委派：

```csharp
public class <Name>Service : I<Name>Service
{
    private readonly ICacheContainer _cache;

    public <Name>Service(ICacheContainer cache)
        => _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    public <T>? Get(string key) => _cache.<Name>.Get(key);
    public void Remove(string key) => _cache.<Name>.Remove(key);
}
```

> service 看似純 facade，但介面定義在 `Bee.Definition`，讓 `Bee.Business` / `Bee.Repository`
> 等上層不必相依 `Bee.ObjectCaching`——是分層邊界，不是 code-style 要消除的 1-line wrapper。

### DI 註冊（path B，`BeeFrameworkServiceCollectionExtensions.cs`）

```csharp
// service：只吃 ICacheContainer
services.AddSingleton<I<Name>Service>(sp =>
    new <Name>Service(sp.GetRequiredService<ICacheContainer>()));
```

**不要**為新 repository 加 `services.AddSingleton<I<X>Repository>(...)`——消費端一律經
`IRepositoryFactory` 按需取得（與 `IRepositoryFactory.CreateFormRepository<T>` 用 progId 產生表單 repository
同一慣例）。逐一註冊會讓每個新系統表變成「工廠方法 + DI 註冊 + 消費端 ctor 參數」三處編輯。

**相依環（務必理解，否則 `AddBeeFramework` 解析即死結）**：

```
ICacheContainer → ICacheDataSourceProvider → IRepositoryFactory → IDefineAccess → ICacheContainer
```

`CacheDefineAccess` 吃 `ICacheContainer`，環因此閉合。解法是容器**以 method group 傳入延遲工廠**，
第一次 cache miss 才解析：

```csharp
services.AddSingleton<ICacheContainer>(sp =>
    new CacheContainerService(
        sp.GetRequiredService<IDefineStorage>(),
        sp.GetRequiredService<PathOptions>(),
        string.Empty,
        sp.GetRequiredService<ICacheDataSourceProvider>));   // 注意：無括號，不即時解析
```

### cache-notify 失效鏈（path B）

失效**基礎設施已備好**，新增 cache 自動掛上——**不需要把 cache 註冊進任何地方**：

1. `KeyObjectCache<T>` 的 `GetCacheKey(key)` = `cachePrefix + CacheGroup + ":" + key`；`CacheGroup` 預設 `typeof(T).Name`。
2. poller 輪詢 common 的 cache-notify 表，把觀察到的版本號寫進 `CacheInfo.NotifyVersions`
   （`CacheNotifyPollSession` → `SetVersion(cacheKey, version)`）。**poller 不持有任何 cache 參考。**
3. 每個 cache entry 在建立時記下自己 `ChangeNotifyKey` 當下的版本號
   （`MemoryCacheProvider`），之後每次讀取比對——版本變了就視為已失效、重新載入。

換句話說，失效是 **entry 自己拉**（pull），不是容器被推（push）。這是為什麼新增 cache
不必登錄進任何陣列。

**你要補的只有 bump 點**：寫該資料庫資料的 BO/Repository，在**同一個 transaction** 內呼叫 `ICacheNotifyService.Touch("<CacheGroup>:<key>", transaction, dbType)`，下一輪 poller 才會清。沒有寫配置的管理介面時，bump 點留待該管理 BO 建立時補（線 B 的 `CompanyRolePermissions` 即此狀態）。

---

## 共用段：兩處（兩條路徑都要）

### `ICacheContainer.cs` + `CacheContainerService.cs`

```csharp
// (1) ICacheContainer 加屬性宣告
<Name>Cache <Name> { get; }

// (2) CacheContainerService ctor 內初始化
//     Define 快取：new <Name>Cache(storage, paths, CachePrefix)
//     Database 快取：把 dataSource 傳進去 —— new <Name>Cache(dataSource, CachePrefix)
<Name> = new <Name>Cache(CachePrefix);

// (3) CacheContainerService 加對應的 public 屬性（`/// <inheritdoc/>`）
public <Name>Cache <Name> { get; }
```

漏掉介面屬性或實作屬性 → `CS0535`（介面未完整實作）；漏掉 ctor 初始化 → NRE。

> **2026-08-06 覆核：先前寫的「第三處：eviction 陣列」已不存在。** `CacheContainerService`
> 曾維護一個 `IEvictableCache[]` 供 cache-notify 路由（`TryEvict` / `_evictableByGroup`），
> 現行機制改為 **poller 只發布觀察到的版本號到 `CacheInfo.NotifyVersions`，由帶有相符
> `ChangeNotifyKey` 的 cache entry 自行失效**——新增 cache 不需要註冊進任何陣列。
>
> 同一次覆核也確認：**`tests/Bee.Hosting.UnitTests` 的 CacheNotify 測試已不再實作
> `ICacheContainer`**（poller 不再持有 cache 參考），因此不存在「兩個必補的 stub」。
> 全 repo 實作 `ICacheContainer` 的只有 `CacheContainerService` 一個。

---

## 測試

| 類別 | 測什麼 | 怎麼測 |
|------|--------|--------|
| Define 快取 | 取用回正確物件、`SaveDefine` 後失效 | 經 `BeeTestFixture` 取 `IDefineAccess.Get<Name>()` |
| Database 快取 POCO | 純查詢邏輯（如多角色 OR 合併） | 純單元，合成資料建 POCO 直接斷言（**不需 DB**） |
| Database service | cache miss 載入 + cache hit 短路 | fake repository + fake 來源 service，驗證 `Get` 兩次只載一次 |
| Repository | DB round-trip | `[DbFact]` 5 DB，`IClassFixture<SharedDbFixture>` |

判定/查詢邏輯儘量放在 **POCO 的方法**（如 `CompanyRolePermissions.GetAllowed`），這樣核心邏輯能用合成資料純單元測試、不綁 DB。

## 容易踩的坑

1. **漏補 `CacheContainerService` 的屬性宣告 → CS0535**：`ICacheContainer` 加了屬性就要有實作。
2. **只 build 個別專案、沒跑 slnx**：stub 的 CS0535 在 `dotnet build tests/Bee.Hosting.UnitTests` 才現；**一律 `dotnet build Bee.Library.slnx -c Release` 複現 CI strict build**。
3. **`CacheContainerService` 兩處只改一處**：ctor 初始化漏 → NRE；屬性宣告漏 → CS0535。
4. **沿用舊的 `CreateInstance => null` 樣板**（2026-07-29 前的慣例）：Database 快取現在**應自載**，
   經 `ICacheDataSourceProvider`。回 `null` 等於把 read-through 手刻進 service，並繞過 base class
   已內建的負向快取。順帶澄清：「判定零 DB」講的是**快取命中**零 DB，而 miss 無論由 service 或
   `CreateInstance` 去撈都要碰 DB——自載不破壞該設計。
5. **`dataSource` 寫成實例而非 `Func<T>`**：`AddBeeFramework` 解析 `ICacheContainer` 時即死結
   （相依環見 path B 的 DI 段）。DI 註冊處傳 method group、不要加括號。
6. **為新 repository 加個別 DI 註冊**：一律經 `IRepositoryFactory` 取得，不逐一註冊。
7. **single vs keyed 選錯**：整份一個物件用 `ObjectCache<T>`；多實例用 `KeyObjectCache<T>` 且 `T : IKeyObject`。
8. **`IDE0028` 集合初始化**：`new List<string>()` 當欄位/區域初始化會被要求改 collection expression `[]`（net10 + strict build）。
9. **POCO 放進 cache 後被 mutate**：cache 內容共享、不可變動（見 memory `definition-immutability`）；per-session 變動先 `Clone()`。
10. **cache-notify bump 點忘了同 transaction**：`Touch` 必須與寫配置同一 transaction，否則寫成功但 notify 沒進、或 notify 進了但寫 rollback。

## 完整 checklist

**定位**：
- [ ] 來源：定義檔（path A）還是資料庫（path B）
- [ ] 形態：single（`ObjectCache<T>`）還是 keyed（`KeyObjectCache<T>` + `IKeyObject`）

**Path A（Define 快取）**：
- [ ] POCO 定義類 + `DefineType` enum 值 + `DefineTypeExtensions` 映射 + `PathOptions.Get<Name>FilePath`
- [ ] `IDefineAccess` DIM `Get<Name>()`
- [ ] `Define/<Name>Cache.cs`（`CreateInstance` 自載）

**Path B（Database 快取）**：
- [ ] POCO 實作 `IKeyObject`（放 `Bee.Definition`），判定/查詢邏輯放 POCO 方法
- [ ] `ICacheDataSourceProvider` 加取數方法（**回傳 `Bee.Definition` 型別**）
- [ ] `CacheDataSourceProvider` 實作（經 `IRepositoryFactory` 取 repository、組裝 POCO）
- [ ] `Database/<Name>Cache.cs`（`CreateInstance` 呼叫 provider；帶 `dataSource` 的建構式 `internal`）
- [ ] `I<Name>Service` + `<Name>Service`（**單行委派**，不含載入邏輯）
- [ ] repository 抽象 + 實作 + `IRepositoryFactory` 加對應的 `Create<T>()` 解析
- [ ] DI 只註冊 service（**不**逐一註冊 repository）
- [ ] `CacheContainerService` ctor 把 `dataSource` 傳進新 cache
- [ ] cache-notify bump 點（寫配置時 `Touch` 同 transaction；無管理介面則留待）

**共用（兩條都要）**：
- [ ] `ICacheContainer` 加屬性
- [ ] `CacheContainerService` 兩處（ctor 初始化 + 屬性宣告）
- [ ] 對應測試（POCO 純單元 / service fake / repository `[DbFact]`）
- [ ] **`dotnet build Bee.Library.slnx -c Release` 0w/0e**，再跑測試

## 參考檔案（讀程式碼對著看）

| 用途 | 檔案 |
|------|------|
| Define 快取（single）樣板 | `src/Bee.ObjectCaching/Define/PermissionModelsCache.cs` |
| Define 快取（keyed）樣板 | `src/Bee.ObjectCaching/Define/FormSchemaCache.cs` |
| Database 快取樣板 | `src/Bee.ObjectCaching/Database/CompanyInfoCache.cs`（含 `Func<T>` 相依環的 WARNING 註解） |
| Database 快取（需解析來源 DB） | `src/Bee.ObjectCaching/Database/DepartmentTreeCache.cs` / `CompanyRolePermissionsCache.cs` |
| 取數接縫 | `src/Bee.Definition/ICacheDataSourceProvider.cs` + `src/Bee.Business/Providers/CacheDataSourceProvider.cs` |
| Cache 基類 | `src/Bee.ObjectCaching/ObjectCache.cs` / `KeyObjectCache.cs` |
| Service 樣板 | `src/Bee.ObjectCaching/Services/DepartmentTreeService.cs`（單行委派） |
| POCO + 判定邏輯樣板 | `src/Bee.Definition/Identity/CompanyRolePermissions.cs`（`GetAllowed` / `GetKey`） |
| 兩處同步點 | `src/Bee.ObjectCaching/CacheContainerService.cs`（ctor 初始化 / 屬性宣告） |
| ICacheContainer | `src/Bee.ObjectCaching/ICacheContainer.cs` |
| DI 註冊 | `src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs`（service；`ICacheContainer` 傳延遲工廠） |
| POCO 純單元測試樣板 | `tests/Bee.Definition.UnitTests/Identity/CompanyRolePermissionsTests.cs` |
