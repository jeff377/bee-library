---
name: bee-add-cache-object
description: bee-library 新增框架快取物件的完整跨檔流程，分兩類——Define 定義快取（來源是定義檔，經 IDefineAccess）與 Database 資料庫相依快取（來源是 DB，經 service + repository + cache-notify 失效）。含 ObjectCache vs KeyObjectCache 決策樹、ICacheContainer + CacheContainerService 三處同步、兩個 CacheNotify 測試 stub（漏補必 CS0535）、DI 註冊與 cache-notify 失效鏈。當使用者要「新增快取物件」、「加一個 cache」、「快取某定義 / 資料庫資料」、「KeyObjectCache / ObjectCache」、「cache-notify 失效」、「判權限/查設定要零 DB」之類需求時使用。
---

# bee-library 新增快取物件

bee-library 的快取分**兩類**，來源與失效機制不同，檔案鏈也不同。先用決策樹定位，再照對應路徑走。每條路徑都會動到 `ICacheContainer` + `CacheContainerService` + 兩個 `CacheNotify` 測試 stub——這三點漏一個就 `CS0535` build 失敗（個別專案 build 抓不到，**只有 `dotnet build Bee.Library.slnx` 複現 CI strict build 才會現**）。

> 樣板對照（讀程式碼時對著看）：
> - Define 快取（single）：`PermissionModelsCache`（`ObjectCache<PermissionModels>`）
> - Define 快取（keyed）：`FormSchemaCache`（`KeyObjectCache<FormSchema>`，by progId）
> - Database 快取（keyed）：`CompanyRolePermissionsCache` / `CompanyInfoCache`（`KeyObjectCache<T>`，by id，`CreateInstance => null`）

## 決策樹

### 第一刀：資料來源是什麼？

| 來源 | 類別 | 資料夾 | 失效機制 | 樣板 |
|------|------|--------|---------|------|
| **定義檔**（XML，經 `IDefineAccess`） | **Define 快取** | `Bee.ObjectCaching/Define/` | `CreateInstance` 自載；`SaveDefine` 時清 | `PermissionModelsCache` / `FormSchemaCache` |
| **資料庫**（runtime 資料） | **Database 快取** | `Bee.ObjectCaching/Database/` | service 載入 + `Set`；cache-notify 輪詢清 | `CompanyRolePermissionsCache` / `CompanyInfoCache` |

### 第二刀：single 還是 keyed？（兩類都適用）

| 基類 | 語意 | 範例 |
|------|------|------|
| `ObjectCache<T>` | **整份只有一個物件**（無 key） | `SystemSettings` / `DatabaseSettings` / `ProgramSettings` / `PermissionModels` |
| `KeyObjectCache<T>` | **多個實例，by key**（progId / company id / token） | `FormSchema` / `TableSchema`（by progId）、`CompanyInfo` / `SessionInfo` / `CompanyRolePermissions`（by id） |

- `KeyObjectCache<T>` 要求 `T` 實作 `IKeyObject`（`string GetKey()`）。
- Define 快取的 `CreateInstance(key)` **自載**（從 `IDefineAccess` 取）；Database 快取的 `CreateInstance(key)` **一律 `=> null`**（negative caching，資料由 service 載入後 `Set`，cache 不自己碰 DB）。

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
| 7 | `src/Bee.ObjectCaching/Define/LocalDefineAccess.cs`、`RemoteDefineAccess.cs` | 如該定義需 Local/Remote 兩路取用 |
| 8 | `src/Bee.ObjectCaching/ICacheContainer.cs` | 加 `<Name>Cache <Name> { get; }` |
| 9 | `src/Bee.ObjectCaching/CacheContainerService.cs` | **三處**（見下方共用段） |
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
| 2 | `src/Bee.ObjectCaching/Database/<Name>Cache.cs` | `: KeyObjectCache<T>`，`CreateInstance(key) => null`（由 service 載入） |
| 3 | `src/Bee.Definition/<Area>/I<Name>Service.cs` | `Get(string key)` / `Remove(string key)` |
| 4 | `src/Bee.ObjectCaching/Services/<Name>Service.cs` | cache miss → 解析來源（如 company DB）→ repository 載入 → `new <Name>(...)` → `cache.Set` |
| 5 | `src/Bee.Repository.Abstractions/.../I<X>Repository.cs` + `src/Bee.Repository/.../<X>Repository.cs` | 資料來源（DB 讀取）；遵守 `bee-add-bo-method` 規則 1（Repository 抽象） |
| 6 | `src/Bee.ObjectCaching/ICacheContainer.cs` | 加 `<Name>Cache <Name> { get; }` |
| 7 | `src/Bee.ObjectCaching/CacheContainerService.cs` | **三處**（見下方共用段） |
| 8 | `src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs` | DI 註冊 repository + service（見下方） |
| 9 | 兩個測試 stub | **必補**（見下方共用段） |
| (10) | cache-notify bump 點 | 寫配置的 BO/Repository 在**同 transaction** `ICacheNotifyService.Touch(cacheKey, tx, dbType)`（見下方） |

### Cache 樣板（path B）

```csharp
namespace Bee.ObjectCaching.Database
{
    public class <Name>Cache : KeyObjectCache<<T>>
    {
        public <Name>Cache(string cachePrefix = "") : base(cachePrefix) { }

        // 一律 null：資料由 <Name>Service 載入後 Set，cache 不自碰 DB（negative caching）。
        protected override <T>? CreateInstance(string key) => null;
    }
}
```

### Service 樣板（path B）

```csharp
public class <Name>Service : I<Name>Service
{
    private readonly ICacheContainer _cache;
    // + 解析來源所需服務（如 ICompanyInfoService）、I<X>Repository

    public <T>? Get(string key)
    {
        var cached = _cache.<Name>.Get(key);
        if (cached != null) { return cached; }
        // 解析來源 DB → repository 載入 → 組裝 → Set
        var snapshot = new <T>(...);
        _cache.<Name>.Set(snapshot);
        return snapshot;
    }

    public void Remove(string key) => _cache.<Name>.Remove(key);
}
```

### DI 註冊（path B，`BeeFrameworkServiceCollectionExtensions.cs`）

```csharp
// repository：由 factory 建立但暴露給 DI（ActivatorUtilities / ctor inject 用）
services.AddSingleton<I<X>Repository>(sp =>
    sp.GetRequiredService<ISystemRepositoryFactory>().Create<X>Repository());

// service：直接 new（依賴從 sp 解析）
services.AddSingleton<I<Name>Service>(sp =>
    new <Name>Service(
        sp.GetRequiredService<ICacheContainer>(),
        /* ...其他依賴... */));
```

### cache-notify 失效鏈（path B）

失效**基礎設施已備好**，新增 cache 自動掛上（靠 `CacheGroup` 路由）：

1. `KeyObjectCache<T>` 的 `GetCacheKey(key)` = `cachePrefix + CacheGroup + ":" + key`；`CacheGroup` 預設 `typeof(T).Name`。
2. `CacheContainerService.TryEvict(cacheKey)` 在 ctor 把每個 owned cache 依 `CacheGroup` 索引進 `_evictableByGroup`；收到 notify key 時 split on 第一個 `':'` → `cacheGroup` + `entity` → 找對應 cache 清掉 entity。
3. poller 輪詢 common 的 cache-notify 表，對 bumped key 呼叫 `TryEvict`。

**你要補的只有 bump 點**：寫該資料庫資料的 BO/Repository，在**同一個 transaction** 內呼叫 `ICacheNotifyService.Touch("<CacheGroup>:<key>", transaction, dbType)`，下一輪 poller 才會清。沒有寫配置的管理介面時，bump 點留待該管理 BO 建立時補（線 B 的 `CompanyRolePermissions` 即此狀態）。

---

## 共用段：三處 + 兩 stub（兩條路徑都要）

### `CacheContainerService.cs` 三處（缺一即 ICacheContainer 未完整實作 → CS0535）

```csharp
// (1) ctor 內初始化
<Name> = new <Name>Cache(CachePrefix);

// (2) 收集所有 cache 供 cache-notify 路由的陣列 — 把 <Name> 加進去
//     （這個陣列建出 _evictableByGroup；漏加 → 該 cache 收不到 notify 失效）
new IEvictableCache[] { ..., <Name> }

// (3) 屬性宣告
public <Name>Cache <Name> { get; }
```

### 兩個 CacheNotify 測試 stub（**最容易漏、CI 必擋**）

加 `ICacheContainer` 屬性後，這兩個 stub 的 `StubCacheContainer` 必須同步補屬性，否則 `CS0535`（介面未完整實作）：

- `tests/Bee.Hosting.UnitTests/CacheNotifyPollerUnitTests.cs`
- `tests/Bee.Hosting.UnitTests/CacheNotifyPollSessionUnitTests.cs`

```csharp
public <Name>Cache <Name> => throw new NotImplementedException();
```

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

1. **漏補兩個 CacheNotify stub → CS0535**：加 `ICacheContainer` 屬性後一定要補（線 A 踩過一次）。
2. **只 build 個別專案、沒跑 slnx**：stub 的 CS0535 在 `dotnet build tests/Bee.Hosting.UnitTests` 才現；**一律 `dotnet build Bee.Library.slnx -c Release` 複現 CI strict build**。
3. **`CacheContainerService` 三處只改一兩處**：init 漏 → NRE；eviction 陣列漏 → cache-notify 失效收不到（沉默 bug）；屬性宣告漏 → CS0535。
4. **Database 快取 `CreateInstance` 寫成自載**：應 `=> null`（由 service 載入）。自載會讓 cache 直接碰 DB，破壞「判定零 DB」設計。
5. **single vs keyed 選錯**：整份一個物件用 `ObjectCache<T>`；多實例用 `KeyObjectCache<T>` 且 `T : IKeyObject`。
6. **`IDE0028` 集合初始化**：`new List<string>()` 當欄位/區域初始化會被要求改 collection expression `[]`（net10 + strict build）。
7. **POCO 放進 cache 後被 mutate**：cache 內容共享、不可變動（見 memory `definition-immutability`）；per-session 變動先 `Clone()`。
8. **cache-notify bump 點忘了同 transaction**：`Touch` 必須與寫配置同一 transaction，否則寫成功但 notify 沒進、或 notify 進了但寫 rollback。

## 完整 checklist

**定位**：
- [ ] 來源：定義檔（path A）還是資料庫（path B）
- [ ] 形態：single（`ObjectCache<T>`）還是 keyed（`KeyObjectCache<T>` + `IKeyObject`）

**Path A（Define 快取）**：
- [ ] POCO 定義類 + `DefineType` enum 值 + `DefineTypeExtensions` 映射 + `PathOptions.Get<Name>FilePath`
- [ ] `IDefineAccess` DIM `Get<Name>()`
- [ ] `Define/<Name>Cache.cs`（`CreateInstance` 自載）

**Path B（Database 快取）**：
- [ ] POCO 實作 `IKeyObject`，判定/查詢邏輯放 POCO 方法
- [ ] `Database/<Name>Cache.cs`（`CreateInstance => null`）
- [ ] `I<Name>Service` + `<Name>Service`（cache miss → repository → `Set`）
- [ ] repository 抽象 + 實作（DB 來源）
- [ ] DI 註冊 repository + service
- [ ] cache-notify bump 點（寫配置時 `Touch` 同 transaction；無管理介面則留待）

**共用（兩條都要）**：
- [ ] `ICacheContainer` 加屬性
- [ ] `CacheContainerService` 三處（init + eviction 陣列 + 屬性宣告）
- [ ] 兩個 CacheNotify stub 補屬性
- [ ] 對應測試（POCO 純單元 / service fake / repository `[DbFact]`）
- [ ] **`dotnet build Bee.Library.slnx -c Release` 0w/0e**，再跑測試

## 參考檔案（讀程式碼對著看）

| 用途 | 檔案 |
|------|------|
| Define 快取（single）樣板 | `src/Bee.ObjectCaching/Define/PermissionModelsCache.cs` |
| Define 快取（keyed）樣板 | `src/Bee.ObjectCaching/Define/FormSchemaCache.cs` |
| Database 快取樣板 | `src/Bee.ObjectCaching/Database/CompanyRolePermissionsCache.cs` / `CompanyInfoCache.cs` |
| Cache 基類 | `src/Bee.ObjectCaching/ObjectCache.cs` / `KeyObjectCache.cs` |
| Service 樣板 | `src/Bee.ObjectCaching/Services/RolePermissionService.cs`（cache miss → repo → Set） |
| POCO + 判定邏輯樣板 | `src/Bee.Definition/Identity/CompanyRolePermissions.cs`（`GetAllowed` / `GetKey`） |
| 三處同步點 | `src/Bee.ObjectCaching/CacheContainerService.cs`（init / eviction 陣列 / 屬性 / `TryEvict`） |
| ICacheContainer | `src/Bee.ObjectCaching/ICacheContainer.cs` |
| 測試 stub（必補） | `tests/Bee.Hosting.UnitTests/CacheNotifyPollerUnitTests.cs` / `CacheNotifyPollSessionUnitTests.cs` |
| DI 註冊 | `src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs`（repository + service） |
| POCO 純單元測試樣板 | `tests/Bee.Definition.UnitTests/Identity/CompanyRolePermissionsTests.cs` |
