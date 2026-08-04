# 計畫：ProgramSettings 型別註冊表化與 Repository 取得機制統一

**狀態：🚧 進行中（2026-08-04）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | `ProgramSettings` 定位收斂：選單分離為獨立定義，註冊表只留 progId → 型別綁定 | ✅ 已完成（2026-08-04） |
| 2 | BO 型別解析全面 ProgId 化（`ProgId` 上移基底、`IBoTypeResolver`、保留字 progId 的啟動自我註冊） | ✅ 已完成（2026-08-04） |
| 3 | `IRepositoryFactory` 介面定案，三個既有工廠合併（含 DI 註冊與 `BackendComponents` 契約調整） | ✅ 已完成（2026-08-04） |
| 4 | 消費端遷移至新工廠，移除 `ISystemRepositoryFactory` / `IFormRepositoryFactory` / `IAuditLogRepositoryFactory` | 📝 待做 |
| 5 | `ProgramItem.Repository` 屬性、解析鏈與 fail-fast 建構 | 📝 待做 |
| 6 | 專屬 Repository 介面樣式落地與文件同步 | 📝 待做 |

## 設計淵源

`ProgramSettings` 的參照來源是 **COM+ 的登錄模型**：以機碼註冊 ProgID 與其對應的元件型別，
ProgID 代表一個獨立的功能或程式。框架的目標是延續這個模型 ——
**整個框架都以 ProgId 定義物件型別**。

這個定位帶出三個直接推論：

1. **所有 BO 都應該是註冊表的項目**，包含 `SystemBusinessObject`、`LogBusinessObject`
   以及未來新增的任何 BO —— 而不是只有 form BO。
2. **Repository 比照辦理** —— 同一個 progId 底下綁定它的 BO 與 Repository。
3. **客製化以 ProgId 為單位覆寫型別**，套裝與客製完全隔離、互不影響。

COM+ 的登錄檔只管「ProgID → 型別」，不管「這個程式在功能表的哪個位置」。
本計畫據此把選單職責從 `ProgramSettings` 剝離（階段 1）。

## 背景

討論起於「`ProgramItem` 目前只綁 `BusinessObject`，想再綁 Repository」，延伸出四個彼此相關的題目：

1. **`ProgramSettings` 的定位收斂** —— 它目前身兼型別註冊表與選單來源，兩者讀者不同、生命週期不同。
2. **BO 型別解析的 ProgId 化** —— 讓所有 BO 都從註冊表解析，取代目前的硬編分支。
3. **BO ↔ Repository 的 1:1 配對** —— 自訂 BO 需要自訂資料存取時，目前沒有宣告式接縫，只能在 BO 內自行取用 `DbAccess`。
4. **Repository 的取得機制統一** —— 比照 `IBusinessObjectFactory`，所有 Repository 走同一個工廠取得，簡化 DI 註冊，且新增 Repository 不需再異動介面。

四者共用同一組解析基礎（ProgramSettings + customization overlay + 型別名載入），故合為一份計畫。

## 現況盤點

### `ProgramSettings` 目前身兼二職

| 職責 | 消費者 | 用到的欄位 |
|------|--------|-----------|
| **型別註冊表** | `ProgramSettingsFormBoTypeResolver` → `IBusinessObjectFactory` | `ProgId` + `BusinessObject` |
| **選單來源** | client shell 整份抓回自行建樹（[FormsViewModel.cs:50](../../apps/Bee.Northwind/Bee.Northwind.UI/ViewModels/FormsViewModel.cs)） | `Category.DisplayName` + `Item.DisplayName` |

兩職的讀者、生命週期與敏感度都不同：註冊表只有 server 需要（且含組件限定型別名），
選單只有 client 需要（且需要 i18n、排序、可見性等純呈現屬性）。

**直接後果**：client 建選單時**無條件走訪所有 category 的所有 item**
（[FormsViewModel.cs:68-74](../../apps/Bee.Northwind/Bee.Northwind.UI/ViewModels/FormsViewModel.cs)），
沒有任何過濾。因此一旦把 `System` / `AuditLog` 納入註冊表，它們會直接變成兩個選單項。

### progId 的全域唯一性目前沒有任何一層在保證

註冊表的前提是「progId 是唯一的鍵」，但巢狀結構讓這個保證只存在於分類之內：

- `ProgramItemCollection : KeyCollectionBase<ProgramItem>` 只保證**同一個 category 內**不重複
- `CustomizeOverlay.FindItem` 是**巢狀線性掃描、取第一個命中**（[CustomizeOverlay.cs:96-107](../../src/Bee.Definition/Customization/CustomizeOverlay.cs)）
  → 同一個 progId 出現在兩個分類時，**哪一筆生效取決於 XML 的文件順序**
- DefineEditor 的驗證同樣擋不住：`seenProgIds` 宣告在 category 迴圈**內**
  （[ProgramSettingsDocumentViewModel.cs:166](../../tools/DefineEditor/ViewModels/ProgramSettingsDocumentViewModel.cs)），
  只查分類內重複；`seenCategoryIds` 才是全域的

攤平為單層後，`ProgramItemCollection` 的 key 機制**本身就是**全域唯一性保證，
重複在載入期即被擋下，且查找成為單層 key lookup。

### BO 解析：兩個保留字 progId 繞過了註冊表

`JsonRpcExecutor.CreateBusinessObject` 是三岔硬編分派（[JsonRpcExecutor.cs:361](../../src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs)）：

```csharp
if (progId == SysProgIds.System)        return _boFactory.CreateSystemBusinessObject(...);
else if (progId == SysProgIds.AuditLog) return _boFactory.CreateLogBusinessObject(...);
else                                    return _boFactory.CreateFormBusinessObject(accessToken, progId, ...);
```

`System` 與 `AuditLog` 已經是 progId（定義於 [`SysProgIds`](../../src/Bee.Definition/SysProgIds.cs)），
卻不走 progId 解析機制。**同樣是 progId，兩套待遇**——這是與設計淵源不一致之處。

### 客製化缺口：SystemBO 目前只能整族、跨租戶地替換

[`BackendComponents`](../../src/Bee.Definition/Settings/SystemSettings/BackendComponents.cs) 共 11 個欄位，
其中**沒有 `SystemBusinessObject` 項目**，只有 `BusinessObjectFactory`。因此今天要改變 SystemBO 的行為，
唯一的辦法是**整個換掉工廠**，而且是 process-wide、不分租戶。

反觀 form BO 已能經 [`CustomizeOverlay.FindProgramItem`](../../src/Bee.Definition/Customization/CustomizeOverlay.cs)
做 per-progId、per-tenant 的覆寫。把 SystemBO 納入註冊表**不是新增機制，而是把既有機制套用到本來就該套用的對象**。

### BO 建構形狀：差異只有一個參數

| BO | ctor |
|----|------|
| `FormBusinessObject` | `(ctx, accessToken, progId, isLocalCall)` |
| `SystemBusinessObject` | `(ctx, accessToken, isLocalCall)` |
| `LogBusinessObject` | `(ctx, accessToken, isLocalCall)` |

差別只在 `progId`，而 `ProgId` 屬性宣告在 `FormBusinessObject` 上、**不在 `BusinessObject` 基底**
（[FormBusinessObject.cs:42](../../src/Bee.Business/Form/FormBusinessObject.cs)）。
上移至基底即可讓三者 ctor 一致，統一以 `Activator.CreateInstance(type, ctx, accessToken, progId, isLocalCall)` 建構。

順帶修掉一個小矛盾：`SystemBusinessObject` 目前不知道自己的 progId，但它明明是被 progId 定址的。

### Repository 建構形狀：目前四種不同簽章

| Repository | ctor |
|------------|------|
| `DataFormRepository` | `(progId, schema, defineAccess, dbAccessFactory, connectionManager, databaseId)` |
| `SessionRepository` / `CompanyRepository` 等 6 個 | `(connectionManager)` |
| `DatabaseRepository` | `(defineAccess, connectionManager)` |
| `ApiKeyRepository` | `(connectionManager, cacheNotify)` |

`DataFormRepository` 的 databaseId 由工廠算好後注入；系統 Repository 則各自在方法內硬寫 scope
（`new DbAccess(DbCategoryIds.Common, _connectionManager)`）。兩種做法並存，衍生自訂 Repository 時要全部 pass-through。

### Repository 已經全數在工廠之後

| 工廠 | 方法數 | 性質 | 會不會隨新增 Repository 長大 |
|------|--------|------|------------------------------|
| [`IFormRepositoryFactory`](../../src/Bee.Repository.Abstractions/Factories/IFormRepositoryFactory.cs) | 2 | **真工廠**：讀 FormSchema → 解析 `CategoryId` → `DbScope` → 經 `IRepositoryDatabaseRouter` 算出 databaseId → 6 個參數建構（[FormRepositoryFactory.cs:46](../../src/Bee.Repository/Factories/FormRepositoryFactory.cs)） | 否。加再多表單仍是 2 個方法 |
| [`ISystemRepositoryFactory`](../../src/Bee.Repository.Abstractions/Factories/ISystemRepositoryFactory.cs) | 9 | 無參數方法，內容皆為 `new XxxRepository(_connectionManager)` | **是。唯一的 churn 來源** |
| [`IAuditLogRepositoryFactory`](../../src/Bee.Repository.Abstractions/Factories/IAuditLogRepositoryFactory.cs) | 1 | log scope 專用 | 否 |

因此問題不是「沒走工廠」，而是 `ISystemRepositoryFactory` 每加一個 Repository 就長一個方法。

### 新增一個系統 Repository 目前要動 4 個檔

| 檔案 | 原因 |
|------|------|
| `ISystemRepositoryFactory` | 加方法 —— 公開 API，需補 `PublicAPI.Unshipped.txt` |
| `SystemRepositoryFactory` | 加實作 |
| `tests/Bee.ObjectCaching.UnitTests/Services/EmployeeContextResolverTests.cs` 的 `FakeSystemRepositoryFactory` | CS0535，手工 stub 必須補新方法 |
| `tests/Bee.ObjectCaching.UnitTests/Services/DeploymentAuthorizationServiceTests.cs` 的 `FakeSystemRepositoryFactory` | 同上 |

其餘約 15 個測試檔只是呼叫工廠、未實作它，不受影響。兩個 fake 特別刺眼：**各自為了用 1 個 Repository 而實作 9 個方法**。

### Repository 軸接不住 progId 的兩類消費者

**BO 軸可以完全 ProgId 化，Repository 軸不行** —— 兩者的差異來自消費者結構，不是實作偏好。

**破口一：`SysProgIds.System` 一個 progId 對應 9 個 Repository**

SystemBO 在單一 progId 底下用掉 9 個 Repository，`CreateRepository(token, "System")` 無從決定回哪一個。
（注意：這**不影響** BO 軸的 ProgId 化 —— 一個 progId 對一個 **BO** 型別完全成立。）

**破口二：部分消費者不在請求脈絡內，也不是 BO**

| 消費者 | 情境 | 為何給不出 progId |
|--------|------|------------------|
| [`ExpiredSessionCleanupService`](../../src/Bee.Hosting/Session/ExpiredSessionCleanupService.cs) | `BackgroundService`，計時器驅動清理過期 session | 沒有請求、沒有 session、沒有 token |
| [`EmployeeContextResolver`](../../src/Bee.ObjectCaching/Services/EmployeeContextResolver.cs) | session 建立 / 進公司時解析員工脈絡 | 在任何 progId 請求**之前**執行；且**單一方法內要用兩個 Repository**（先 `IUserRepository` 再 `IEmployeeRepository`） |

`SessionCompanyBinder`、`DeploymentAuthorizationService`、`CacheDataSourceProvider` 屬同一類。
**這些消費者根本不是 BO**，所以 BO 軸的 ProgId 化對它們毫無影響，也無法涵蓋它們。

### 系統 Repository 為何維持 per-table 介面

| | FormBusinessObject 軌 | SystemBusinessObject |
|---|---|---|
| 資料存取形狀 | 一個 progId 一張主檔（＋明細），FormSchema 驅動 | 9 個彼此無關的系統表 |
| DB scope | 單一，由 `FormSchema.CategoryId` 決定 | 跨 scope：`st_session` / `st_user` / `st_api_key` 在 common，`st_department` / `st_employee` / `st_role_grant` 在 company |
| Repository 的擁有者 | 該 BO 私有 | **跨消費者的共用基礎設施** |

`ISystemRepositoryFactory` 有 **8 個非 SystemBO 的消費者，橫跨 4 個套件**（`Bee.Business` 的 `SessionCompanyBinder` / `CacheDataSourceProvider`、`Bee.ObjectCaching` 的 `EmployeeContextResolver` / `DeploymentAuthorizationService`、`Bee.Hosting` 的 `ExpiredSessionCleanupService` / `AddBeeFramework`）。強行把它們併成單一 `ISystemRepository` 會產出一個橫跨兩個 DB scope、約 40 個方法的 god interface，且迫使這些消費者去依賴某個 BO 的介面。

## 目標形狀

### 定義檔職責切分

```
ProgramSettings.xml   progId → BusinessObject / Repository 型別綁定（server 專用，不上 wire）
MenuSettings.xml      分類 / 排序 / 標題 / 可見性，每個選單項對應一個 progId（client 消費）
```

### BO 軸：註冊表統一解析

```csharp
public interface IBoTypeResolver          // 由 IFormBoTypeResolver 更名擴權
{
    Type Resolve(string customizeId, string progId);
}

// BusinessObjectFactory 三個 Create 方法收斂為一個
object CreateBusinessObject(Guid accessToken, string progId, bool isLocalCall = true);
```

`JsonRpcExecutor.CreateBusinessObject` 的三岔分支整段消失。

### Repository 軸：統一入口，雙軸解析

```csharp
public interface IRepositoryFactory
{
    // progId 軸 —— 型別隨 progId 變動，走 ProgramSettings 解析
    T CreateFormRepository<T>(Guid accessToken, string progId) where T : class, IDataFormRepository;

    // 框架軸 —— 型別固定，無 progId 脈絡
    T Create<T>() where T : class;
}
```

（介面細節於階段 3 定案；上方為形狀示意，非最終簽章。）

| 目標 | 是否達成 |
|------|---------|
| 單一入口 | 三個工廠併成一個。DI 註冊 3 條變 1 條，`BackendComponents` 兩個欄位變一個 |
| 新增 Repository 零介面異動 | 兩個方法皆為泛型，介面不再長大；兩個手工 fake 不再 CS0535 |
| 與 `IBusinessObjectFactory` 對稱 | `CreateFormRepository(token, progId)` 與 `CreateBusinessObject(token, progId)` 平行；泛型回傳讓自訂 BO 直接取得 `IOrderRepository`，免 cast |
| 一 BO 一 Repository | **在 form 軸上完全成立** —— 這是 1:1 的主場，也是 `ProgramItem.Repository` 的掛點 |

`Create<T>()` 那一軸不是妥協，而是誠實反映「此 Repository 不屬於任何 progId，其消費者也不是 BO」。

## 已定案事項

### 註冊表定位

| 項目 | 決定 | 理由 |
|------|------|------|
| **職責收斂** | `ProgramSettings` **只管 progId 與型別註冊**（BO / Repository），不再兼任選單來源 | 兩職讀者不同、生命週期不同、敏感度不同。COM+ 登錄檔也只管 ProgID → 型別 |
| **選單獨立** | 選單改為獨立定義檔，每個選單項**對應一個 progId** | 選單需要的是排序、i18n、可見性等純呈現屬性，與型別註冊正交 |
| **保留字的選單問題** | 隨選單分離**自動消失** —— 註冊表不再是選單來源，`System` / `AuditLog` 不會出現在選單 | 原本要靠可見性旗標或保留 category 迴避，分離後不需要 |
| **結構攤平** | **移除 `ProgramCategory`**，收斂為 `ProgramSettings` → `ProgramItemCollection` → `ProgramItem` 單層 | progId 全域唯一性由 `KeyCollectionBase` 直接保證（巢狀時只保證分類內）；查找成為單層 key lookup；分類概念只存在於選單定義，不會兩邊不同步 |
| **`ProgramItem.DisplayName`** | **保留** | 人工檢視與除錯訊息需要人可讀標籤。i18n 由選單定義承接，此處是給維護者看的原文標籤 |
| **選單的客製 overlay** | **納入**，語意為**整份取代** | 不同租戶的可見功能與命名本就不同。整份取代比照 `PickFormLayout`——選單是整體版面，per-item 疊加結果難預期 |
| **相容性策略** | **整體採破壞性更新，不保留舊版相容路徑** | 現有消費者都在掌控範圍內，可與框架同步更新。外部使用者的既有定義檔由遷移 CLI ＋ 啟動 fail fast 承接 |

### BO 軸

| 項目 | 決定 | 理由 |
|------|------|------|
| **納入範圍** | 所有 BO 納入 `ProgramSettings` 管理，含 `SystemBusinessObject`、`LogBusinessObject` 及未來新增者 | 延續 COM+ 登錄模型；同時讓客製化能以 ProgId 為單位覆寫任何 BO |
| **建構統一** | `ProgId` 由 `FormBusinessObject` 上移至 `BusinessObject` 基底，三者 ctor 一致，統一以 `Activator.CreateInstance` 建構 | COM+ 登錄檔沒有 kind 欄位，`CoCreateInstance` 是統一啟動——正是統一啟動讓登錄機制成立 |
| **不新增 Kind 屬性** | 不在 `ProgramItem` 加「BO 類型」欄位 | 加了等於把分支從程式碼搬到 XML，分支仍在且打錯字只能執行期發現 |
| **保留字 progId 的預設** | **必須在 `ProgramSettings.xml` 宣告**；後端啟動時**逐筆檢查保留字 progId，缺哪筆補哪筆**並填入框架預設 BO 型別 | 註冊表是唯一來源（語意純粹），啟動自我註冊消除 bootstrap 懸崖。等同 COM+ 元件安裝時自寫登錄檔 |
| **自我註冊的觸發粒度** | **項目層級**，非檔案層級 —— 檔案存在但缺 `System` 項目時同樣補寫 | 既有 host 都已有 `ProgramSettings.xml` 卻沒有 `System` 項目。若以檔案存在與否為觸發條件，升級後不會補寫，`System` 解析不到而登入中斷 |
| **`SystemBusinessObject` 是否拆分** | **先維持單一 progId**，未來視功能分類再拆出去 | 拆分會改動 wire 契約，且對 Repository 軸破口二毫無幫助。不阻擋本計畫 |
| **`BackendComponents.BusinessObjectFactory`** | **移除** | BO 型別改由註冊表決定後，此欄位失去存在理由 |
| **保留字 progId 的解析防護** | **啟動期驗證 + 解析期 fail fast**，並加上 per-progId 預期基底約束。不採用「內建預設兜底」 | 保留字若沿用一般 progId 的 silent fallback，故障會以誤導性的面貌浮現。詳見下節 |

### Repository 軸

| 項目 | 決定 | 理由 |
|------|------|------|
| **綁定方式** | `ProgramItem` 新增 `Repository` 屬性，組件限定型別名，與 `BusinessObject` 對稱 | 沿用既有且已驗證的 progId → 型別名 → 反射載入模式 |
| **建構統一** | Repository **比照 BO 統一建構函式簽章**；單元測試需要時以**建構函式多載**補足 | 統一啟動是註冊機制成立的前提，與 BO 軸同一個理由 |
| **建構函式簽章** | `(IRepositoryContext ctx, Guid accessToken, string progId)` —— 與 `BusinessObject` 同構；框架軸 Repository 忽略 `progId` | 與階段 2 讓 `SystemBusinessObject` 接受但不使用 `progId` 是同一個取捨，框架內部一致。詳見附錄 |
| **`RepositoryBase` 抽象基底** | **引入** —— 承載統一 ctor、`ctx`、scope 宣告與解析後的 `DatabaseId` | 統一簽章若沒有共同基底，九個 Repository 各自複製同一段樣板。基底也是路由邏輯的自然歸屬 |
| **`ICacheNotifyService?` 的位置** | **放進 `IRepositoryContext`**，所有 Repository 都看得到 | 它本就是 nullable、預設不使用。若維持只有 `ApiKeyRepository` 拿得到，就得為它保留特例 ctor，與統一簽章的目的相牴觸。誰真的用得到可 grep 稽核 |
| **路由邏輯歸屬** | **移入 `RepositoryBase`**，於 ctor 內急切解析 `DatabaseId`；保留「呼叫端指定 databaseId」的方法多載 | 基底既已定案，路由放基底最自然；留在工廠則需把 per-call 狀態塞進 ctx，與否決候選 B 的理由衝突。急切解析維持現行失敗時機；多載服務於 session bootstrap 這類「已知目標 DB、尚無可用 token」的路徑 |
| **建構契約** | 必須衍生自 `DataFormRepository`；以 `ActivatorUtilities.CreateInstance` 建構 | 比照 `BusinessObject` 的 assignable 檢查；`ActivatorUtilities` 讓自訂 Repository 可額外注入自己的 DI 相依 |
| **失敗語意** | 型別載入失敗或非 `DataFormRepository` 衍生 → **直接拋，不 fallback** | 刻意與 `BusinessObject` 的 silent fallback 相反。fallback 只是把 crash 推遲到請求當下且更難診斷 |
| **介面形狀** | `IXxxRepository : IDataFormRepository`（擴充，非取代） | `FormBusinessObject` 的 CRUD 與 `SaveContext` / `DeleteContext` 都寫在基底介面上 |
| **多租戶客製** | **納入** —— `Repository` 比照 `BusinessObject` 走 `CustomizeOverlay` 的 per-progId 取代 | BO 與 Repository 都是可能客製的部分；不納入會讓客製 BO 與其 Repository 脫鉤 |
| **1:1 規則範圍** | 僅限定義驅動的表單軌；框架軸維持 per-table 介面 | 見上節「系統 Repository 為何維持 per-table 介面」 |
| **`IFormRepositoryFactory` 是否併入** | 併入 `IRepositoryFactory`，但**保留 progId + accessToken 參數化的專屬方法** | 它是參數化的真工廠，需要 schema / router 的執行期脈絡 |
| **舊三介面的相容策略** | **直接移除**，不保留 `[Obsolete]` 過渡 | 減少過渡期的雙軌維護 |

## 未決事項

**無。** 設計已全數定案，四份設計說明分別為：
〈附錄：選單定義檔設計〉、〈附錄：`IRepositoryContext` 設計〉、
〈保留字 progId 的解析防護〉、〈自我註冊的執行時機與寫檔邊界〉。

## 落地紀錄：與本文件不同之處

實作過程中與上文敘述有出入的決定，記於此處而非改寫原文——原文是當初的判斷，這裡是實際做法。
行為與定案一致，差異都在機制或範圍。

### 階段 1

| 事項 | 本文件 | 實際 |
|------|--------|------|
| 既有型別 | 未提及 | `Bee.Definition.Settings` 已有一組死碼 `MenuSettings` / `MenuFolder` / `MenuItem` 族（45 條 `PublicAPI.Shipped`、無 `DefineType`、無 storage、production 零消費者）。`MenuSettings` / `MenuFolder` 名稱直接衝突，兩套無法並存，故整組取代。附錄以「避開 UI 框架撞名」為由選 `MenuEntry`，但 `MenuItem` 本就在同一 namespace |
| 選單的租戶 overlay | 未指明由誰解析 | server 端於 `GetDefineCore` 以 session 的 customizeId 解析後回傳結果，client 只拿一份。附錄指定的 client API 是 `ClientDefineAccess.GetMenuSettingsAsync()`（`GetDefine` 路徑），該路徑沒有第二層的位置 |
| `MenuEntry.ProgId` 參照完整性 | 「載入期或 CLI 檢查」 | 只在設計期工具（DefineEditor 與 `Validate(registry)` 多載）。storage 讀選單時手上沒有註冊表，載入期檢查會製造載入順序相依 |

### 階段 2

| 事項 | 本文件 | 實際 |
|------|--------|------|
| 「記憶體優先」的落地方式 | 「補寫結果必須參與解析」，未指明機制 | 由 resolver 實作「保留字**缺項** → 框架預設型別」。不可改 mutate cache 內的 `ProgramSettings`（[development-constraints.md](../development-constraints.md) 的 Definition Data Immutability After Init），這是唯一相容的作法。**與被否決的方案 C 不同**：C 是解析**失敗**時退回，此處失敗（型別載不到／基底不符）一律拋，只有「註冊表根本沒提到」才用預設 |
| 自訂 `IBoTypeResolver` | 未提及 | 未處理保留字的自訂 resolver 會被啟動期驗證擋下（QuickStart sample 已同步）。這是設計預期，但構成新的實作義務 |

### 階段 3

| 事項 | 本文件 | 實際 |
|------|--------|------|
| `IRepositoryContext` 位置 | `namespace Bee.Repository.Abstractions` | 改放 `Bee.Repository`。Abstractions 只 reference `Bee.Definition`，而該介面成員都是 `Bee.Db` 型別；放 Abstractions 會把 `Bee.Db` 拉進只 reference Abstractions 的 `Bee.Business` 與 `Bee.ObjectCaching`。消費端只需 `IRepositoryFactory`，它仍在 Abstractions |
| `RepositoryBase.Scope` | `protected abstract DbScope Scope` | 改為建構函式參數且可為 `null`（表示「本身沒有資料庫，每個方法各自指定」）。非 virtual 屬性是為了避免建構期呼叫可覆寫成員（CA2214） |
| per-company Repository 的範圍 | 只點名 `EmployeeContextResolver` 需保留呼叫端指定 databaseId | `RolePermissionRepository` / `DepartmentRepository` / `EmployeeRepository` 的**全部**呼叫端只有 `CacheDataSourceProvider` 與 `EmployeeContextResolver`，**兩者都沒有 accessToken**（前者是以 companyId 為鍵的 cache 回填）。改 token 驅動會讀成「呼叫者的公司」而非「被指定的公司」，是行為錯誤而非不便 |
| `Bee.Repository` 相依 | 未提及 | 新增 `Microsoft.Extensions.DependencyInjection.Abstractions`（僅為 `ActivatorUtilities`，不含容器實作） |

### 交接時列出的待驗證項目：驗證結果

| # | 項目 | 結果 |
|---|------|------|
| 1 | 多型 XML 在 `KeyCollectionBase` 上的行為 | ✅ 逐子型別 `[XmlArrayItem(typeof(T))]` 產生各自元素名、無 `xsi:type`，三層巢狀 round-trip 通過。集合基底不影響此行為 |
| 2 | `ActivatorUtilities` 對衍生型別的注入 | ✅ 框架參數依**型別**而非位置繫結，額外的介面型別 DI 相依可正確注入，參數順序不同亦可。⚠️ **衍生型別不得再宣告第二個 `string` 或 `Guid` 參數**——同型別已被引數佔用，容器會被要求解析 `string` 而失敗 |
| 3 | 三個 per-company Repository 的等價性 | ⚠️ 範圍比預期大，見上表 |
| 4 | 原子 rename 的跨平台行為 | ✅ `File.Move` 三參數多載 Unix 走 `rename(2)`、Windows 走 `MoveFileEx(MOVEFILE_REPLACE_EXISTING)`，同 volume 內原子取代；**兩參數多載會擲例外而非取代**。Windows 上目的檔被他行程開啟且未共享刪除時 rename 會失敗，故寫入失敗一律降級為 warning（Windows 行為依 API 契約推導，未實機驗證） |
| 5 | `IHostedService` 啟動順序 | ✅ repo 未設 `HostOptions.ServicesStartConcurrently`（預設 `false`），依註冊順序循序啟動；自我註冊已排在 `CacheNotifyPoller` 之前 |
| 6 | `ProgramSettingsCache` 是否可能提前載入 | ✅ 全 repo 僅 `ProgramSettingsBoTypeResolver.Resolve` 會讀，且無人在建構期解析 resolver 或工廠，故 `StartAsync` 之前 cache 必為冷 |

## 自我註冊的執行時機與寫檔邊界（已定案）

### 執行時機：`IHostedService`，不放在 `AddBeeFramework` 內

理由：

- `AddBeeFramework` 是 `IServiceCollection` 擴充（[BeeFrameworkServiceCollectionExtensions.cs:54](../../src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs)），
  執行當下 **ServiceProvider 尚未建立**。自我註冊需要 `IDefineAccess` / `IDefineStorage`，
  在裡面取用就得 `BuildServiceProvider()`，那會產生**與最終容器不同的第二個容器**（singleton 各一份，ASP.NET Core 有 ASP0000 警告）。
- `AddBeeFramework` 會在**非執行情境**被呼叫：design-time build、單元測試建 `ServiceCollection`、
  各類程式碼產生器。寫檔副作用會在這些情境發生，測試之間互相污染 `DefinePath`。
- 註冊期丟例外＝host 起不來且訊息脈絡差；hosted service 可記 log、降級或讓 host 有序關閉。

repo 內已有三個 hosted service 的先例（`CacheNotifyPoller`、`ExpiredSessionCleanupService`、
`AuditLogWriterService`），沿用同一模式即可，且註冊順序可控——自我註冊排在其餘之前。

### 實作細節

必須在 `IHostedService.StartAsync` 內完成（而非 `BackgroundService.ExecuteAsync`）。
`StartAsync` 在 host 開始接受請求前跑完，`ExecuteAsync` 不阻塞啟動——放錯位置會讓第一個登入請求
比自我註冊先到。

### 寫檔邊界條件


| 條件 | 作法 |
|------|------|
| **唯讀部署** | 自我註冊的結果**先進記憶體並立即生效**，落檔是後續的持久化嘗試。寫入失敗只記 warning，不影響本次執行 |
| **多實例並行啟動** | 寫暫存檔 + 原子 rename；rename 競賽的輸家確認內容已正確後放棄。補寫內容是冪等的（同一組保留字預設），誰贏結果相同 |
| **cache 失效** | 落檔後顯式 invalidate `ProgramSettingsCache` 的對應 slot，沿用 `SaveDefine` 既有的失效路徑 |

> **「記憶體優先」是關鍵，不是細節。** 已定案「保留字必須在註冊表宣告」＋「解析失敗 fail fast」，
> 若自我註冊只寫檔、寫不進去就當沒發生，唯讀部署會直接卡在 fail fast 上起不來。
> 記憶體內的補寫結果必須**參與解析**，落檔僅決定下次啟動能否省下這一步。

## 保留字 progId 的解析防護（已定案）

**決定：啟動期驗證（A）＋ 解析期 fail fast（B），並為保留字加上 per-progId 的預期基底約束。
不採用內建預設兜底（C）。**

### 問題

[`ProgramSettingsFormBoTypeResolver.ResolveCore`](../../src/Bee.Business/ProgramSettingsFormBoTypeResolver.cs)
目前**每一條失敗路徑都回傳 `typeof(FormBusinessObject)`** —— 檔案不存在、progId 未註冊、
`BusinessObject` 留空、型別載不到、型別非 `FormBusinessObject` 衍生，全部靜默退回。
這對一般 progId 是合理的：`Order` 設定打錯只是退化成通用 CRUD，惱人但服務不中斷。

**`System` 走同一條路會產生誤導性的故障**：

- `FormBusinessObject` 沒有 `Login` / `CreateSession` / `GetDefine`
- `JsonRpcExecutor` 以反射派發 `System.Login` → 找不到方法 → JSON-RPC "method not found"
- **症狀是「登入失敗，找不到方法 Login」**，把診斷者導向 API 層或 client，而非真正的成因（註冊表）
- 且 `FormBusinessObject` 的 ctor 接受 progId，會成功建構為 `FormBusinessObject("System")`，
  建構期不拋例外——故障因此浮現得晚且面貌錯誤

### 放寬 assignable 檢查會再開一個缺口

階段 2 把解析目標由「衍生自 `FormBusinessObject`」放寬為「衍生自 `BusinessObject`」後，
把 `System` 綁到某個 `FormBusinessObject` 子類**會通過檢查**卻完全錯誤。
因此保留字需要 **per-progId 的預期基底約束**：

| 保留字 progId | 預期基底 |
|--------------|---------|
| `System` | 必須 assignable to `SystemBusinessObject` |
| `AuditLog` | 必須 assignable to `LogBusinessObject` |

### 方案評估

| 方案 | 行為 | 採用 |
|------|------|------|
| A. 啟動期驗證 + fail fast | 自我註冊後立即驗證每個保留字 progId 解析得到且符合預期基底，不通過則 host 不啟動 | ✅ 抓部署期錯誤，訊息最清楚 |
| B. 解析期 fail fast | `Resolve("System")` 失敗直接拋帶脈絡的例外，不回 `FormBusinessObject` | ✅ 抓啟動後定義被竄改的情況 |
| C. 內建預設兜底 | 解析失敗時退回框架內建的 `SystemBusinessObject` | ❌ 服務雖不中斷，但**客製打錯字會靜默失效**——而客製化正是納入註冊表的主要動機 |

A 與 B 互補：A 在部署期把關，B 涵蓋執行期。一般 progId 的 silent fallback 政策不變。

## 選單分離的附帶效益：wire 外洩問題消失

先前記錄的「`ProgramSettings` 完整內容（含組件限定型別名）會送給每個已登入遠端 client」，
在選單分離後**自然消解**：client 改抓 `MenuSettings`，不再需要 `ProgramSettings`。

因此 `ProgramSettings` 可進一步收緊為 server 專用（比照 `SystemSettings` / `DatabaseSettings`
在 `GetDefine` 的 `IsLocalCall` 閘內），型別名不再上 wire。此舉在階段 1 一併處理。

> 佐證（現況）：`ApiProtectionLevel.LocalOnly` 由 [`ApiAccessValidator`](../../src/Bee.Api.Core/Validator/ApiAccessValidator.cs) 統一把關，全 repo 標註者僅 `SaveDefine`、`CreateSession` 與部署層管理方法三個。`GetDefine` 標的是 `Public` + `Authenticated`（[SystemBusinessObject.Define.cs:47](../../src/Bee.Business/System/SystemBusinessObject.Define.cs)），其 `IsLocalCall` 閘只擋 `SystemSettings` 與 `DatabaseSettings` 兩型；HTTP 請求一律 `executor.IsLocalCall = false`（[ApiServiceController.cs:259](../../src/Bee.Api.AspNetCore/Controllers/ApiServiceController.cs)）。

## 已查核事項

**客製 BO 覆寫不會弄丟 API 保護。** `ApiAccessValidator` 的屬性解析順序是「method → 被覆寫的 method → 宣告型別」（[ApiAccessValidator.cs:20](../../src/Bee.Api.Core/Validator/ApiAccessValidator.cs)），因此客製 `SystemBusinessObject` 子類覆寫 `SaveDefine` 時仍受 `LocalOnly` 保護；子類新增的 public 方法另有 analyzer BEE3001 在 build 期強制標註。開放 SystemBO 客製化**不會**降低 API 存取控制的保證。

## 階段規劃

### 階段 1：`ProgramSettings` 定位收斂與選單分離

依〈附錄：選單定義檔設計〉施作。

- 新增選單定義（`DefineType` 新值、定義類別、cache、`IDefineStorage` / `IDefineAccess` 存取方法）
- 選單項對應 progId；補上註冊表沒有的呈現屬性（排序、可見性、i18n 掛點）
- `ProgramSettings` 移除 `ProgramCategory`，攤平為 `ProgramItemCollection` 單層，收斂為 progId → 型別綁定；
  `CustomizeOverlay.FindItem` 的巢狀掃描改為單層 key lookup；DefineEditor 的 progId 重複驗證改為全域
- 既有 `ProgramSettings.xml` 的遷移路徑落地
- client 選單來源切換（`FormsViewModel` 等）
- `ProgramSettings` 收緊為 server 專用（`GetDefine` 的 `IsLocalCall` 閘納入此型別）
- DefineEditor 支援新定義檔
- 選單的客製 overlay（若定案納入）

**驗收**：Northwind 選單行為與改版前一致；`ProgramSettings` 不再經由遠端 `GetDefine` 取得；
既有定義檔的遷移路徑有測試覆蓋；重複 progId 在載入期即被擋下（含遷移時浮現的跨分類重複）。

### 階段 2：BO 型別解析全面 ProgId 化

- `ProgId` 屬性由 `FormBusinessObject` 上移至 `BusinessObject` 基底；`SystemBusinessObject` /
  `LogBusinessObject` 的 ctor 補上 `progId` 參數，三者簽章一致
- `IFormBoTypeResolver` → `IBoTypeResolver`，`ProgramSettingsFormBoTypeResolver` 對應更名；
  解析目標由「衍生自 `FormBusinessObject`」放寬為「衍生自 `BusinessObject`」
- `BusinessObjectFactory` 的三個 Create 方法收斂為 `CreateBusinessObject(accessToken, progId, isLocalCall)`
- `JsonRpcExecutor.CreateBusinessObject` 的三岔分支移除
- `BackendComponents.BusinessObjectFactory` 移除
- 保留字 progId 的啟動自我註冊：逐筆檢查、缺哪筆補哪筆（時機與寫檔邊界見〈自我註冊的執行時機與寫檔邊界〉）
- 保留字 progId 的解析防護：啟動期驗證 + 解析期 fail fast + per-progId 預期基底約束（見〈保留字 progId 的解析防護〉）

**驗收**：`dotnet build -c Release` 零警告；既有測試全綠；新增測試涵蓋
「全新 DefinePath 啟動」「既有 `ProgramSettings.xml` 缺 `System` 項目啟動」「客製層覆寫 `System` BO」三條路徑。

### 階段 3：`IRepositoryFactory` 介面定案與三工廠合併

- 依〈附錄：`IRepositoryContext` 設計〉實作；`IRepositoryFactory` 簽章於本階段定案
- 所有 Repository 收斂為統一建構函式；測試需要處補建構函式多載
- 實作 `RepositoryFactory`，內含框架軸的型別對應（`IXxxRepository` → `XxxRepository`）
- DI 註冊由 3 條併為 1 條（[BeeFrameworkServiceCollectionExtensions.cs:242-252](../../src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs)）
- `BackendComponents` 的 `SystemRepositoryFactory` / `FormRepositoryFactory` 兩欄併為單一 `RepositoryFactory`（[BackendComponents.cs:91-100](../../src/Bee.Definition/Settings/SystemSettings/BackendComponents.cs)），`BackendDefaultTypes` 同步
- 舊三介面暫時保留、以新工廠實作，讓階段 4 可漸進遷移

**驗收**：`dotnet build -c Release` 零警告；既有測試全綠；`BeeFrameworkServiceResolutionTests` 能解析新工廠。

### 階段 4：消費端遷移與舊介面移除

- SystemBO 五個 partial 檔改用新工廠
- 8 個非 BO 消費者遷移（`SessionCompanyBinder`、`CacheDataSourceProvider`、`EmployeeContextResolver`、`DeploymentAuthorizationService`、`ExpiredSessionCleanupService`、`AddBeeFramework` 等）
- 兩個手工 `FakeSystemRepositoryFactory` 縮成泛型單一方法
- **直接移除** `ISystemRepositoryFactory` / `IFormRepositoryFactory` / `IAuditLogRepositoryFactory`（不留 `[Obsolete]` 過渡），`PublicAPI.Shipped.txt` 對應處理

**驗收**：全 repo 無舊介面殘留；測試全綠；公開 API 變更於 commit message 說明二進位相容性判定。

### 階段 5：`ProgramItem.Repository` 綁定

- `ProgramItem` 新增 `Repository` 屬性（`[XmlAttribute]`、`[DefaultValue("")]`），比照 `BusinessObject` 的 XML doc 寫法
- `CustomizeOverlay` 納入 `Repository` 的 per-progId 取代
- 工廠的 form 軸解析：`ProgramItem.Repository` 有值 → `AssemblyLoader.GetType` → 檢查衍生自 `DataFormRepository` → `ActivatorUtilities.CreateInstance`；任一步失敗**直接拋**
- 無值 → 沿用框架預設 `DataFormRepository`
- 保留字 progId（`System` / `AuditLog`）的 `Repository` 留空

**驗收**：有值 / 無值 / 型別找不到 / 型別非衍生 四條解析路徑皆有測試；客製 overlay 的取代行為有測試；
fail-fast 的例外訊息足以定位是哪個 progId 的哪個型別名。

### 階段 6：專屬介面樣式與文件

- 於 `apps/Bee.Northwind` 落地一個 `IOrderRepository : IDataFormRepository` 的實例，作為樣式範本
- 更新 [`docs/definition-files-overview.md`](../definition-files-overview.md) 與 `.zh-TW` 版（雙語同步）——
  其「ProgramSettings 身兼二職」一節需改寫為「型別註冊表」＋新增選單定義一節
- 修正 [`docs/terminology.zh-TW.md`](../terminology.zh-TW.md) 對 `ProgramSettings.xml` 的描述 —— 現寫「功能程式的參數設定」，
  但其內容是 registry，無任何 per-program 參數，屬文件漂移
- 升格一份 ADR 記錄三個長效決策：**ProgramSettings 作為全框架型別註冊表**（含 COM+ 淵源）、
  **選單與註冊表分離**、**Repository 1:1 規則只適用表單軌**

**驗收**：公開文件雙語同步；`rules/public-docs.md` 的落地檢查指令無新增違規。

## 附錄：選單定義檔設計（已定案）

目標是把選單職責自 `ProgramSettings` 剝離，讓註冊表回歸純粹的 progId → 型別綁定。

### 檔名與型別

`MenuSettings.xml`，型別 `MenuSettings`，與 `SystemSettings` / `DatabaseSettings` /
`DbCategorySettings` 的命名一致。新增 `DefineType.MenuSettings`。

### 結構：Folder / Item 分型，共同基底

```xml
<MenuSettings>
  <Items>
    <MenuFolder Id="transactions" Caption="交易" Order="10">
      <Items>
        <MenuEntry  Id="customer" ProgId="Customer" Caption="客戶" Order="10" />
        <MenuFolder Id="sales" Caption="銷售" Order="20">
          <Items>
            <MenuEntry Id="sales-order"  ProgId="Order" Caption="訂單"   Order="10" />
            <MenuEntry Id="sales-return" ProgId="Order" Caption="退貨單" Order="20" />
          </Items>
        </MenuFolder>
      </Items>
    </MenuFolder>
    <MenuEntry Id="dashboard" ProgId="Dashboard" Caption="儀表板" Order="20" />
  </Items>
</MenuSettings>
```

| 型別 | 成員 | 說明 |
|------|------|------|
| `MenuNodeBase`（abstract） | `Id`（key）/ `Caption` / `Order` / `Icon` / `Visible` | 共同基底，繼承 `KeyCollectionItem` |
| `MenuFolder` | ＋`Items` | 分組節點，可含子節點 |
| `MenuEntry` | ＋`ProgId` | 功能項，參照註冊表的一筆 progId |
| `MenuNodeCollection` | `KeyCollectionBase<MenuNodeBase>` | 兩型別共用同一個 key 空間 |

**分型的關鍵好處：把驗證規則變成型別保證。** 單一遞迴型別下需要一條執行期規則
「`ProgId` 有值的節點不得有子節點」；分型後這件事在結構上就不可能發生。

### 多型 XML：照 `FilterNode` 家族的既有作法

repo 已有同構的多型定義階層 —— `FilterNode`（abstract）＋ `FilterCondition` / `FilterGroup`，
且 `FilterGroup` 自身遞迴含 `FilterNodeCollection`。兩個難點它都解掉了：

```csharp
// 基底：宣告所有子型別（FilterNode.cs:13-14）
[XmlInclude(typeof(FilterCondition))]
[XmlInclude(typeof(FilterGroup))]
public abstract class FilterNode : MessagePackCollectionItem { }

// 遞迴屬性：逐子型別標註，XmlSerializer 因此輸出「各自的元素名」而非 xsi:type 判別碼
[XmlArrayItem(typeof(FilterCondition))]
[XmlArrayItem(typeof(FilterGroup))]
public FilterNodeCollection Nodes { get; set; }

public bool ShouldSerializeNodes() => Nodes != null && Nodes.Count > 0;
```

- `[XmlArrayItem(typeof(T))]` 逐型別標註是**可讀 XML 的關鍵**——沒有它會退化成
  `xsi:type="MenuFolder"` 判別碼，人工檢視體驗大幅變差
- `ShouldSerialize{Property}()` 讓空集合不輸出，維持定義檔簡潔
- **MessagePack 的 `[Union]` 不需要**：選單經 `GetDefine` 以 XML 字串上 wire，不走物件序列化。
  比 `FilterNode` 單純
- 行動端 AOT 無虞：`KeyCollectionBase<MenuNodeBase>` 仍只有一個 public `Add`，
  且 `ILLink.Descriptors.xml` 的 `Bee.Definition.*` wildcard 已涵蓋子型別

回歸測試比照 `FilterGroupTests.FilterGroup_DeepNested_XmlRoundtrip_PreservesStructure`
（[FilterGroupTests.cs:160](../../tests/Bee.Definition.UnitTests/Filters/FilterGroupTests.cs)），
以三層巢狀驗證結構還原。

### 已定案的設計決策

| 項目 | 決定 | 說明 |
|------|------|------|
| **層數** | **多層遞迴**，不設固定層數 | 保留彈性；ERP 選單三層以上常見，事後改為公開定義檔的破壞性變更 |
| **節點分型** | **`MenuFolder` / `MenuEntry` 分兩型**，共同基底 `MenuNodeBase` | 屬性本就不一致（Folder 有子集合、Entry 有 `ProgId`）。分型後「功能項不得有子節點」由型別保證，不需執行期驗證 |
| **葉節點命名** | **`MenuEntry`**，不用 `MenuItem` | `MenuItem` 幾乎被每個 UI 框架佔用（WPF `System.Windows.Controls.MenuItem`、WinForms `ToolStripMenuItem`、`Avalonia.Controls.MenuItem`、DevExpress 等）。定義型別會被**所有** UI head 消費，且撞名處恰是「依定義建選單」那段程式碼——衝突是必然而非偶然。公開定義型別日後改名成本高，故一開始就避開 |
| **key** | **獨立的 `Id`**，`ProgId` 是另一個屬性 | 允許同一支程式出現在選單多處（例如訂單與退貨單共用同一個 BO） |
| **`Id` 唯一性** | **全樹唯一**，非僅同層 | 讓節點可被穩定參照（深層連結、使用者的最近使用 / 我的最愛） |
| **客製 overlay** | **整份取代**（比照 `PickFormLayout`） | 選單是整體版面，per-item 疊加會產生難以預期的混合結果 |
| **遷移** | **CLI 一次性命令 + 啟動時偵測舊格式 fail fast** | 自動改寫使用者定義檔具侵入性，且重複 progId 需人為判斷 |
| **`Caption` 的 i18n** | 走 `LanguageResource`，namespace `Menu`，sub-key `Folder.{id}.Caption` / `Entry.{id}.Caption` | 比照 `FormSchemaLocalizer` 的 sub-key 慣例。用 `Id` 而非 `ProgId`——同一 progId 可能出現多處且標題不同 |

### 需要額外驗證的一點

`KeyCollectionBase` 只保證**同層**不重複，遞迴結構因此仍需補一項樹狀驗證：

- **`Id` 全樹唯一** —— 載入期走訪整棵樹檢查（權威），DefineEditor 同步實作（即時回饋）。
  `MenuFolder` 與 `MenuEntry` 共用同一個 key 空間。

（原先單一型別下所需的「`ProgId` 有值的節點不得有子節點」已由分型消除。）

空的 `MenuFolder` 建議列為驗證**警告**而非錯誤——編輯過程中的中間狀態合理。

### 連帶影響

- **`ProgId` → 選單節點是 1:N**。需要「目前開啟的表單對應哪個選單項」（麵包屑、選單高亮）時，
  必須以 `Id` 而非 `ProgId` 追蹤。client 導覽狀態應攜帶 `Id`。
- **`ProgId` 參照的完整性**：選單引用了註冊表中不存在的 progId 應為驗證錯誤（載入期或 CLI 檢查）。

### 遷移

既有 `ProgramSettings.xml` 是巢狀且身兼二職，需拆為攤平的註冊表＋選單兩份。

- **由 CLI 一次性命令執行**（如 `dotnet bee defines split-menu`），不在啟動時自動改寫定義檔
- **啟動時偵測舊格式並 fail fast**，訊息明確指向該命令
- **跨分類重複的 progId：中止並列出全部，要求人工處理。** 現況允許重複且由文件順序決定勝負
  （見〈現況盤點〉），自動挑一筆等於把既有的不確定性固化下來
- 遷移產生的 `MenuEntry.Id` 由 CLI 依原 `ProgId` 產生（衝突時加序號），維護者可事後改為有意義的值

### 本計畫不處理的既有缺口

**client 目前對選單不做任何權限過濾、全部顯示。** 逐使用者的可見性屬權限職責（`PermissionModels`），
與 `Visible` 這個設計期開關是兩回事。此缺口需在文件中明確載明，避免 `Visible` 被誤用為權限機制。

### 連帶要補的基礎設施

`DefineType` 新值、`MenuSettingsCache`、`IDefineStorage` / `IDefineAccess` 存取方法、
`PathOptions.GetMenuSettingsFilePath()`、`CustomizeDefineReader` 對應方法、
`CustomizeOnlyStorage` / `CustomizeOnlyPathOptions` 對應、DefineEditor 的 document view 與驗證、
client 端存取（`ClientDefineAccess.GetMenuSettingsAsync`）。

## 附錄：`IRepositoryContext` 設計（已定案）

目標是讓所有 Repository 的建構函式統一，作法比照 BO 的 `IBeeContext`。

### 介面

```csharp
namespace Bee.Repository.Abstractions
{
    /// <summary>
    /// Construction-time context handed to every repository. The data-access counterpart of
    /// <see cref="Bee.Definition.IBeeContext"/>: it aggregates the cross-cutting services a
    /// repository needs so that every repository shares one constructor signature.
    /// </summary>
    public interface IRepositoryContext
    {
        /// <summary>The definition data access service (FormSchema / TableSchema lookups).</summary>
        IDefineAccess DefineAccess { get; }

        /// <summary>The connection manager (dialect + connection resolution).</summary>
        IDbConnectionManager ConnectionManager { get; }

        /// <summary>The database access factory.</summary>
        IDbAccessFactory DbAccessFactory { get; }

        /// <summary>Resolves a logical scope to a physical database id.</summary>
        IRepositoryDatabaseRouter Router { get; }

        /// <summary>
        /// Cross-process cache invalidation channel; <c>null</c> when the host does not poll it.
        /// </summary>
        ICacheNotifyService? CacheNotify { get; }

        /// <summary>Escape hatch for services not in the typed core members. Use sparingly.</summary>
        IServiceProvider Services { get; }
    }
}
```

### 建構函式簽章（已定案）

```csharp
protected RepositoryBase(IRepositoryContext ctx, Guid accessToken, string progId)
```

- 與 `BusinessObject(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall)` 同構
- 框架軸 Repository 忽略 `progId`（工廠傳 `string.Empty`）——與階段 2 讓
  `SystemBusinessObject` 接受但不使用 `progId` 是**同一個取捨**，框架內部一致
- `accessToken` 供需要 company scope 的 Repository 自行經 `Router` 解析；
  `Guid.Empty` 代表無 session（背景服務情境）

> **未採用的替代方案**：把 `accessToken` / `progId` 併入 `IRepositoryContext`，簽章收斂為單一參數。
> `ActivatorUtilities` 較單純，但會讓應用生命週期的服務與 per-call 狀態混在同一個物件，
> 且與 BO 分歧（`BeeContext` 刻意把 `accessToken` 留在 ctor 參數）。

### 路由邏輯的歸屬（已定案：選項一）

統一簽章 `(ctx, accessToken, progId)` **沒有 `databaseId` 的位置**，因此必須決定它從哪來。

#### 現況有三種並存的作法

| 作法 | 誰在用 | 形式 |
|------|--------|------|
| (a) 建構期注入 | `DataFormRepository` | 工廠算好 `databaseId` 後從 ctor 傳入，烘進實例 |
| (b) 方法內硬寫 scope | `SessionRepository` / `CompanyRepository` / `ApiKeyRepository` 等 | 每個方法 `new DbAccess(DbCategoryIds.Common, _connectionManager)` |
| (c) 呼叫端逐次指定 | `EmployeeRepository.GetByUserRowId(databaseId, ...)`、`DepartmentRepository` | `databaseId` 是方法參數，由呼叫端提供 |

#### 兩個選項

**選項一：路由移入 `RepositoryBase`**

基底宣告 scope，於 ctor 內以 `ctx.Router.Resolve(Scope, accessToken)` 解析出 `DatabaseId`；
form repository 的 scope 由 `FormSchema.CategoryId` 推導，框架 repository 以 `protected abstract DbScope Scope` 宣告。
三種作法收斂為一種。

- 好處：工廠退回純粹的「解析型別 → 建構」；scope 從散落在方法內的字面值變成型別上的顯式宣告，可稽核
- 代價見下方「必須先解決的問題」

**選項二：路由留在工廠，`databaseId` 經 `IRepositoryContext` 傳入**

工廠仍負責解析，把結果放進 per-call 建構的 ctx。

- 好處：改動最小，失敗時機與錯誤訊息完全不變
- 代價：`IRepositoryContext` 從「橫切服務集合」變成「服務＋已解析的呼叫狀態」，
  與先前否決候選 B 的理由（不把 per-call 狀態混進 context）自相矛盾

#### 選項一必須先解決的問題

1. **`EmployeeContextResolver` 手上沒有可用的 accessToken。**
   它的簽章是 `Resolve(userId, databaseId)`（[EmployeeContextResolver.cs:30](../../src/Bee.ObjectCaching/Services/EmployeeContextResolver.cs)），
   在 session 建立 / 進公司的過程中被呼叫——**此時 session 尚未能解析出公司**，所以它是拿著
   `databaseId` 而非 token。若路由改為 token 驅動，這個呼叫端會斷。
   → 解法：保留 (c) 的「呼叫端指定 databaseId」方法多載，定位為
   「我已經知道目標 DB」的 bootstrap 路徑，而非殘留的技術債。

2. **失敗時機。** 目前壞掉的 `CategoryId` 在工廠就拋（BO 方法一開始、尚未動任何資料）。
   若移入 Repository 且**延遲**解析，故障會挪到第一次存取資料時、可能在操作中途。
   → 解法：在 ctor 內**急切**解析，維持現行時機。

3. **定義驗證的職責。** `FormRepositoryFactory.ParseCategoryId` 拋的
   `Unknown schema.CategoryId '{categoryId}'` 是定義錯誤。移入後，定義驗證會發生在資料存取物件內。
   （可接受——搬移後正是 Repository 在讀 schema。）

4. **per-company Repository 的行為變更。** `RolePermissionRepository` / `DepartmentRepository` /
   `EmployeeRepository` 目前由呼叫端給 databaseId，改為 token 驅動後路由來源改變，需逐一確認等價。

5. **測試建構成本。** 目前可直接以 `databaseId` 建 `DataFormRepository` 繞過路由；
   移入後測試需要可運作的 `Router`（或走已定案的測試用建構函式多載）。

#### 定案

**選項一，並保留 (c) 的方法多載。** 理由：`RepositoryBase` 既已定案，路由放基底是最自然的歸屬，
否則九個 Repository 各自重複；選項二會讓 `IRepositoryContext` 承載 per-call 狀態，
與否決候選 B 的理由衝突。問題 1 的多載不是妥協——session bootstrap 本來就處於
「已知目標 DB、尚無可用 token」的狀態，明確保留一條路徑比硬湊 token 誠實。

### 測試用建構函式多載

統一簽章後，只需要少數相依的測試不必組出完整 ctx，以**額外多載**提供輕量入口
（比照既有 `SystemRepositoryFactory` 的雙 ctor 作法，
[SystemRepositoryFactory.cs:32](../../src/Bee.Repository/Factories/SystemRepositoryFactory.cs)）。
用多載而非可選參數，是為了讓「測試用」與「正式」兩條建構路徑在簽章上就分得開。

### review 時待確認

無。介面、建構函式簽章、`RepositoryBase`、`ICacheNotifyService?` 位置與路由歸屬均已定案，
見〈已定案事項 → Repository 軸〉。本附錄的設計於階段 3 落地時據以實作。

## 附帶觀察（不在本計畫範圍）

- `ProgramItem.DisplayName` 目前是硬編單語字串，不走 `LanguageResource`。選單分離後，
  i18n 應由新的選單定義承接（見〈附錄：選單定義檔設計〉），這個不對稱隨之解決。
