# ADR-034：ProgramSettings 作為全框架型別註冊表

## 狀態

**已採納（Accepted，2026-08-04）** —— 決策已執行。`ProgramSettings` 收斂為純型別註冊表、
選單分離為 `MenuSettings`、BO 與 Repository 皆以 progId 綁定，並已落地於 `apps/Bee.Northwind`。

本 ADR 記錄三個長效決策：**ProgramSettings 作為全框架型別註冊表**（含 COM+ 淵源）、
**選單與註冊表分離**、**Repository 1:1 規則只適用表單軌**。

## 淵源：COM+ 的登錄模型

`ProgramSettings` 的參照來源是 **COM+ 的登錄模型**：以機碼登錄 ProgID 與其對應的元件型別，
ProgID 代表一個獨立的功能或程式。框架延續這個模型 —— **整個框架都以 ProgId 定義物件型別**。

這個定位帶出三個直接推論：

1. **所有 BO 都是註冊表的項目**，包含 `SystemBusinessObject`、`LogBusinessObject`
   以及未來新增的任何 BO —— 而不是只有 form BO。
2. **Repository 比照辦理** —— 同一個 progId 底下綁定它的 BO 與 Repository。
3. **客製化以 ProgId 為單位覆寫型別**，套裝與客製完全隔離、互不影響。

COM+ 的登錄檔只管「ProgID → 型別」，不管「這個程式在功能表的哪個位置」。第二個決策即據此而來。

## 決策一：ProgramSettings 只管 progId → 型別

`ProgramSettings.xml` 收斂為單層攤平的 `ProgramItem` 清單，每筆帶 `BusinessObject` 與
`Repository` 兩個組件限定型別名。

### 為何攤平

註冊表的前提是「progId 是唯一的鍵」。巢狀（`ProgramCategory` → `ProgramItem`）結構下，
這個保證只存在於分類之內：同一個 progId 出現在兩個分類時，**哪一筆生效取決於 XML 的文件順序**。
攤平後 `ProgramItemCollection` 的 key 機制**本身就是**全域唯一性保證，重複在載入期即被擋下，
且查找成為單層 key lookup。分類概念只存在於選單定義，不會兩邊不同步。

### 為何連保留字 progId 也納入

`System` 與 `AuditLog` 本來就是 progId，卻曾經繞過註冊表走硬編分派 —— 同樣是 progId、兩套待遇，
與淵源不一致。納入後，`JsonRpcExecutor` 的三岔分支整段消失，且 SystemBO 首次獲得
per-progId、per-tenant 的客製能力（先前唯一辦法是整個換掉工廠，且是 process-wide、不分租戶）。

代價是 bootstrap 懸崖：註冊表是唯一來源，缺項就解析不到。解法是**啟動時逐筆檢查保留字、
缺哪筆補哪筆**，且補寫結果**先進記憶體並立即生效**，落檔只是後續的持久化嘗試 ——
唯讀部署因此仍能啟動。

### 保留字的失敗策略比一般 progId 嚴格

| progId | 型別載不到 / 基底不符 |
|--------|---------------------|
| 一般（如 `Order`） | 靜默退回 `FormBusinessObject`（BO 軸） |
| 保留字（`System` / `AuditLog`） | **直接拋**，並加上 per-progId 的預期基底約束 |

理由是**故障的面貌**。`FormBusinessObject` 沒有 `Login`，`System` 若沿用靜默退回，症狀會是
JSON-RPC「找不到方法 Login」，把診斷者導向 API 層或 client，而非真正的成因（註冊表）。
且 `FormBusinessObject` 的 ctor 接受 progId，會成功建構，故障浮現得晚且面貌錯誤。

未採用「內建預設兜底」：服務雖不中斷，但**客製打錯字會靜默失效** —— 而客製化正是納入註冊表的主要動機。

### `Repository` 的失敗策略與 `BusinessObject` 相反

`Repository` 型別載不到或不衍生自 `DataFormRepository`，**一律直接拋，不 fallback**，
即使是一般 progId。

`Order` 的 BO 名稱打錯只是退化成通用 CRUD —— 惱人，不是災難。Repository 名稱打錯卻會讓
這支程式的讀寫**改跑作者刻意替換掉的通用 SQL**。Fallback 不會避免故障，只會把它推遲到
資料已經錯了的時候。資料存取沒有無害的降級模式。

### 僅供 server 端

註冊表承載組件限定型別名，client 端毫無用處。選單分離後 client 不再需要它，因此遠端
`GetDefine` 比照 `SystemSettings` / `DatabaseSettings` 一併擋下 —— 型別名不上 wire。
這是選單分離的直接附帶效益，不是額外加的防護。

## 決策二：選單與註冊表分離

選單改為獨立的 `MenuSettings.xml`，每個 `MenuEntry` 對應一個 progId。

### 為何分家

兩職的**讀者、生命週期與敏感度都不同**：註冊表只有 server 需要（且含組件限定型別名），
選單只有 client 需要（且需要排序、i18n、可見性等純呈現屬性）。COM+ 的登錄檔也只管
ProgID → 型別。

分家還連帶解掉一個具體問題：client 建選單時無條件走訪所有項目、不做過濾，因此一旦把
`System` / `AuditLog` 納入註冊表，它們會直接變成兩個選單項。原本要靠可見性旗標或保留分類迴避，
分離後不需要。

### 結構決策

| 項目 | 決定 | 理由 |
|------|------|------|
| **層數** | 多層遞迴，不設固定層數 | ERP 選單三層以上常見；事後改為公開定義檔是破壞性變更 |
| **節點分型** | `MenuFolder` / `MenuEntry` 分兩型，共同基底 `MenuNodeBase` | 屬性本就不一致。分型後「功能項不得有子節點」由**型別保證**，不需執行期驗證 |
| **葉節點命名** | `MenuEntry`，**不用 `MenuItem`** | `MenuItem` 幾乎被每個 UI 框架佔用（WPF / WinForms / Avalonia / DevExpress）。定義型別會被**所有** UI head 消費，且撞名處恰是「依定義建選單」那段程式碼 —— 衝突是必然而非偶然 |
| **key** | 獨立的 `Id`，`ProgId` 是另一個屬性，且 `Id` **全樹唯一** | 允許同一支程式出現在選單多處（訂單與退貨單可共用同一個 BO），並讓節點可被穩定參照（深層連結、最近使用） |
| **客製 overlay** | **整份取代** | 選單是整體版面，per-item 疊加會產生難以預期的混合結果 |

### 連帶影響

**`ProgId` → 選單節點是 1:N。** 需要「目前開啟的表單對應哪個選單項」（麵包屑、選單高亮）時，
必須以 `Id` 而非 `ProgId` 追蹤，client 導覽狀態應攜帶 `Id`。

**`Visible` 不是權限機制。** 它是設計期開關，對每個使用者都一樣；逐使用者的可見性屬
[權限與授權](adr-019-permission-authorization-model.md) 的職責。**client 目前對選單不做任何權限過濾。**

## 決策三：Repository 的 1:1 規則只適用表單軌

「一個 progId 一個 BO 一個 Repository」在**表單軌完全成立**，在框架軌不成立。
`IRepositoryFactory` 因此有兩個方法而非一個：

```csharp
T CreateFormRepository<T>(Guid accessToken, string progId) where T : class, IDataFormRepository;
T Create<T>(Guid accessToken = default) where T : class;
```

這不是妥協，而是誠實反映消費者結構。**BO 軸可以完全 ProgId 化，Repository 軸不行**，
原因有二：

**破口一：`System` 一個 progId 對應多個 Repository。** SystemBO 在單一 progId 底下用掉
session / user / company / api-key 等多個系統表的 Repository，`CreateFormRepository(token, "System")`
無從決定回哪一個。（這**不影響** BO 軸 —— 一個 progId 對一個 **BO** 型別完全成立。）

**破口二：部分消費者不在請求脈絡內，也不是 BO。**

| 消費者 | 情境 | 為何給不出 progId |
|--------|------|------------------|
| `ExpiredSessionCleanupService` | `BackgroundService`，計時器驅動清理過期 session | 沒有請求、沒有 session、沒有 token |
| `EmployeeContextResolver` | session 建立 / 進公司時解析員工脈絡 | 在任何 progId 請求**之前**執行，且單一方法內要用兩個 Repository |

`SessionCompanyBinder`、`DeploymentAuthorizationService`、`CacheDataSourceProvider` 屬同一類。
**這些消費者根本不是 BO**，BO 軸的 ProgId 化對它們毫無影響，也無法涵蓋它們。

### 為何系統 Repository 維持 per-table 介面

| | 表單軌 | 框架軌 |
|---|---|---|
| 資料存取形狀 | 一個 progId 一張主檔（＋明細），FormSchema 驅動 | 多張彼此無關的系統表 |
| DB scope | 單一，由 `FormSchema.CategoryId` 決定 | 跨 scope：session / user / api-key 在 common，department / employee / role-grant 在 company |
| Repository 的擁有者 | 該 BO 私有 | **跨消費者的共用基礎設施** |

強行併成單一 `ISystemRepository` 會產出一個橫跨兩個 DB scope、數十個方法的 god interface，
且迫使這些非 BO 的消費者去依賴某個 BO 的介面。

### 兩個方法皆為泛型

新增 Repository 因此**不需異動介面**。這正是本決策要解決的問題：被取代的
`ISystemRepositoryFactory` 每加一張系統表就長一個方法，且兩個手工測試 fake 各為了用
一個 Repository 而實作九個方法。

### 表單軌的專屬介面樣式

`IXxxRepository : IDataFormRepository`（**擴充，非取代**）。`FormBusinessObject` 的 CRUD 與
`SaveContext` / `DeleteContext` 都寫在基底介面上，取代等於仍要實作每個成員、只是不說出口。
BO 端以自己的介面取得它，免 cast：

```csharp
private IOrderRepository Repository() => CreateFormRepository<IOrderRepository>();
```

實例見 `apps/Bee.Northwind/Bee.Northwind.Server/Repositories/IOrderRepository.cs` 與
同目錄的 `OrderRepository.cs`。

## 相關

- [ADR-007](adr-007-convention-based-type-resolution.md) —— 約定式型別解析
- [ADR-016](adr-016-multitenant-customization-overlay.md) —— 多租戶客製 overlay，本 ADR 的 per-progId 取代沿用其機制
- [ADR-010](adr-010-logical-database-category.md) —— 邏輯資料庫分類，決定表單軌 Repository 的路由目標
- [定義檔總覽](../definition-files-overview.zh-TW.md) —— 兩份定義檔的使用說明
