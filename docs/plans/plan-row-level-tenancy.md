# 計畫：列級租戶隔離（`sys_company_id`）

**狀態：📝 擬定中（2026-07-30）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | `st_session` 的公司欄位化：`sys_company_id` 獨立欄位 ＋ `UpdateCompanyId` 收斂 | 📝 待做 |
| 2 | 定義層：`SysFields.CompanyId`、TableSchema 生成、複合唯一索引 | 📝 待做 |
| 3 | 強制過濾：repository 綁定公司，讀寫一律注入條件；INSERT 蓋章 | 📝 待做 |
| 4 | AnyCode 破口收斂、既有部署遷移、文件 | 📝 待做 |

> 承接 [plan-session-persistence.md](plan-session-persistence.md)。該 plan 已完成，`st_session`
> 的 `CompanyId` 目前存在 `session_user_xml` 內；階段 1 把它提升為獨立欄位並改用本 plan 的命名。

## 背景與目標

框架目前的租戶隔離是**資料庫級**：`FormSchema.CategoryId` 決定 scope（`common` / `company` /
`log`），`RepositoryDatabaseRouter` 由 `session.CompanyId` → `CompanyInfo.CompanyDatabaseId`
決定實際連哪個庫。一間公司一個庫，隔離由「連不到別人的庫」保證。

租賃（SaaS）環境需要第二種模式：**試用公司共用一個 company 資料庫**，以公司編號在列的層級區隔。
理由是為每個試用帳號開一個資料庫，在成本與維運上都不成立。

兩種模式**正交、並存**：

| 維度 | 決定什麼 | 由誰決定 |
|------|---------|---------|
| `CategoryId` → `CompanyDatabaseId` | 連哪一個資料庫 | 既有 router，不變 |
| `sys_company_id` | 該庫裡哪些列屬於哪家公司 | 本 plan |

付費客戶維持獨立庫（siloed），試用客戶共用庫（pooled），差別只在
`CompanyInfo.CompanyDatabaseId` 是否指向同一個 `DatabaseItem`——router 對「多家公司指向同一個庫」
本來就成立，不需改動。

## 已定案決策（2026-07-30）

| 項目 | 決策 |
|------|------|
| 欄位名稱 | `sys_company_id`（snake_case 全小寫，與 `sys_rowid` / `sys_master_rowid` 同族） |
| 值的型別 | **字串業務公司代號**（如 `C001`），即 `SessionInfo.CompanyId` 手上的值，不用 GUID rowid |
| 涵蓋範圍 | **所有部署一律帶此欄位，含獨立庫** |
| 注入層級（D1） | repository 建構繫結，**且僅對宣告了該欄位的表注入**——schema 驅動，不是無條件 AND |
| raw SQL 路徑（D2） | 入口強制傳入租戶範圍參數（漏填＝編譯錯誤）＋ 靜態掃描補手寫 SQL 那一段 |
| 欄位宣告（D3） | `NOT NULL`，預設值空字串——依框架對 string 型別的預設值規範 |
| 欄位長度 | `DbType="String" Length="10"`——公司代號固定 10 碼英文 |
| 涵蓋規則（D4） | `CategoryId="company"` 的表一律宣告；`common` / `log` 視需求 |

**為何用字串而非 `company_rowid`**：它與 session、`ICompanyInfoService`、權限快照用的是同一個
key，過濾時零轉換，也少一個對不上的環節；共用庫裡人眼可讀，支援與除錯直接看得懂。代價是
公司代號變更會牽動資料列——但公司代號變更本來就已牽動 `CompanyInfo` 快取鍵與權限資料，
不是這裡新增的約束。

**為何獨立庫也要帶**：獨立庫裡它是常數、看似浪費一個短字串，換來的是兩種模式**表結構完全一致**，
同一份 `TableSchema` 通用；試用公司畢業到獨立庫時是**純資料搬移，不是 schema 分岔**。
若只有共用庫才加欄位，等於長期維護兩種表形狀，且畢業流程要做 schema 轉換。

### 公司代號格式：固定 10 碼英文

`sys_company_id` 一律宣告為 `DbType="String" Length="10"`。固定長度對本 plan 是實質好處——
它是每張業務表都會多出來的欄位，也會進到每個複合唯一索引的第一段，寬度小且可預期，
索引成本才壓得住。

框架沒有固定長度字串型別（`FieldDbType` 只有變動長度的 `String` / `Text`），
因此「固定 10 碼」由**驗證層**保證，欄位只負責上限。

**兩件連帶要處理的事：**

1. **`st_company.sys_id` 目前是 `Length="20"`**（[st_company.TableSchema.xml](../../src/Bee.Definition/Defaults/TableSchema/common/st_company.TableSchema.xml)）。
   來源欄位比副本寬，代表可以建出一個 12 碼的公司代號、卻在蓋章到業務列時被截斷或退件。
   建議**收窄為 10** 讓約束留在單一事實來源；既有部署需在升級前檢查有無超過 10 碼的公司代號。

2. **大小寫要統一。** 公司代號是識別碼型字串，框架端比對用 `OrdinalIgnoreCase`
   （快取鍵、`ICompanyInfoService` 查找），但資料庫端相不相等取決於 collation。
   兩邊規則不同時，`C001` 與 `c001` 會出現「快取當同一家、資料庫當兩家」的分裂。
   建議在建立公司時即正規化為大寫，讓兩端一致。

## 核心原則：過濾由建構保證，不是由呼叫端記得

這是**安全邊界**：任何一條漏掉 `sys_company_id` 條件的查詢，就是跨租戶資料外洩。

現有的 record-scope 過濾**不能直接沿用其形狀**。它是 BO 層解析出 `FilterNode` 後，
以**選擇性參數**傳進 repository：

```csharp
// src/Bee.Repository/Form/DataFormRepository.cs
public DataSet? GetData(Guid rowId, FilterNode? scopeFilter = null)
```

預設 `null` 意味著「忘記傳」不會有任何徵兆——對權限縮小範圍而言是可接受的降級，
對租戶隔離而言是資料外洩。租戶條件**不可以是參數**。

正確落點：`IFormRepositoryFactory.CreateDataFormRepository(progId, accessToken)` 建構 repository 時
已握有 accessToken，且已透過 router 解析出資料庫；同一處解析出 `session.CompanyId` 並注入
repository，之後該 repository 的每一條 SELECT / UPDATE / DELETE 都 AND 上租戶條件，
INSERT 一律蓋章。呼叫端沒有「不傳」這個選項。

**但不是每張表都有這個欄位**——`st_user`、`st_company` 這類 common 庫的系統表沒有、
也不該有公司歸屬。因此注入必須**由 schema 驅動**：以目標表的 `TableSchema` 是否宣告
`sys_company_id` 為閘門，宣告了才注入。這同時給出一個好性質：**要不要被租戶隔離，
是資料表定義說了算，不是呼叫端說了算**——漏加欄位會在資料層被看見（欄位不存在），
而不是變成一條靜默少了條件的查詢。

## 現況盤點（動工前已確認）

| 位置 | 現況 | 本 plan 的影響 |
|------|------|--------------|
| `RepositoryDatabaseRouter.Resolve` | 由 session 解析公司再取 `CompanyDatabaseId` | 不變；多家公司指向同一庫本就成立 |
| `IFormRepositoryFactory.CreateDataFormRepository` | 傳入 `progId` + `accessToken` | **注入點**：同時解析並綁定 `CompanyId` |
| `DataFormRepository` 的 `scopeFilter` | 選擇性參數、預設 `null` | 維持（權限用）；租戶條件另走建構繫結，兩者 AND |
| `TableSchemaGenerator.AddIndexes` | `uk_{0}` 對 `sys_id` 單欄唯一 | 需改為 `(sys_company_id, sys_id)` 複合 |
| `EmployeeContextResolver.Resolve(userId, companyDatabaseId)` | 於 company 庫依 user rowid 找員工 | **共用庫下會跨公司誤中**，必須加公司條件 |
| 存於 company 庫的 `st_*` 權限表 | `st_role` / `st_role_grant` / `st_user_role` | 共用庫下同樣需要區隔（見 D4） |
| `IDbAccessFactory` / `DbCommandSpec` 直查 | AnyCode 報表 / 批次自寫 SQL | **最大破口**（見 D2） |

## 決策

### D1：注入落在 repository 建構，且由 schema 決定要不要注入 —— ✅ 已定案

工廠建構 `DataFormRepository` 時注入 `CompanyId`，repository 內部對每條語句加上租戶條件、
INSERT 蓋章；呼叫端沒有省略的選項。

**閘門是目標表的 `TableSchema` 有沒有宣告 `sys_company_id`**：`st_user` 這類 common 庫的
系統表沒有公司歸屬，對它們注入條件是錯的。schema 驅動讓「哪些資料受租戶隔離」成為定義層的
決定，而非呼叫端的自由心證。

否決 `DbCommandSpec` 建構層（要在 SQL 文字層猜目標表，脆弱）與 BO 層 `FilterNode`
（會變成可省略的參數，見上節）。

### D2：raw SQL 入口改為必填租戶範圍 ＋ 靜態掃描 —— ✅ 已定案

報表與批次由 BO 自寫 SQL、經 `IDbAccessFactory.Create(databaseId)` 直查，繞過 D1 的注入；
共用庫下這是最容易外洩的一條路。決議：**讓「沒想過租戶」變成編譯錯誤**。

入口簽章要求一個明確的租戶範圍引數，且不提供預設值：

- `TenantScope.Company(companyId)` —— 查詢限定於該公司
- `TenantScope.NotApplicable` —— 明確宣告此查詢不涉及公司歸屬（如查 `st_user`）

兩者都要求開發者**當下作答**；差別在於後者是一個看得見、可被 review 與掃描盯上的宣告，
而不是「什麼都沒寫」。第二層是靜態掃描：對 `TenantScope.Company` 的呼叫檢查 SQL 文字是否
帶有該欄位條件，補上「參數傳了但 SQL 忘了加」的缺口。

否決「收斂入口／封裝 `DbAccess`」——SQL 文字仍是自己寫的，封裝擋不住真正的破口，
卻要改動所有既有 BO 呼叫端；也否決資料庫層 RLS（五種 DB 支援度不一，SQLite 無）。

### D3：`NOT NULL` ＋ 預設值空字串 —— ✅ 已定案

依框架對 string 型別的預設值規範宣告：`NOT NULL`，`DefaultValue` 為空字串。不採 nullable
——讓「沒蓋到章的列」成為合法狀態，等於把資料完整性問題延後。

**升級路徑另有一道 Oracle 的坎**（宣告面不受影響，但 ALTER 順序要對）：Oracle 視 `''` 為
NULL，對已有資料列的表直接 `ALTER ADD ... NOT NULL DEFAULT ''` 會擲 ORA-01400。既有表的
加欄位必須是三步：**ALTER ADD（可為空）→ 以實際公司代號 backfill → 收緊為 NOT NULL**。
這是升級腳本的步驟，不是欄位宣告的讓步——新建的表一律直接是 `NOT NULL`。

backfill 值的來源：獨立庫（一庫一公司）可由 `DatabaseSettings` 反查該庫對應的公司代號；
**一庫多公司在既有部署中不存在**（那是本 plan 才引入的模式），故 backfill 無歧義。

### D4：以 scope 為準 —— company 庫全部要，common / log 視需求 —— ✅ 已定案

規則不列舉個別表，而是綁在 scope 上：

| Scope | 規則 |
|-------|------|
| `company` | **該庫裡的表一律宣告 `sys_company_id`**，不分 `ft_` 業務表或 `st_` 系統表（`st_role` / `st_role_grant` / `st_user_role` 亦然，否則共用庫下權限會跨公司互見） |
| `common` | **視需求**，預設不宣告。這些表本就跨公司共用（`st_user`、`st_company`） |
| `log` | **視需求**，預設不宣告。稽核是否需要按公司區隔查詢，由該表自己決定 |

以 scope 為準而非逐表列舉，好處是可以用測試把它變成不變式：
**所有 `CategoryId="company"` 的 `TableSchema` 必須宣告 `sys_company_id`**——新增表時漏加會
直接紅，而不是等到共用庫上線才發現某張表沒被隔離。

**common / log 宣告此欄位時的注意事項**：D1 的注入閘門只看「表有沒有宣告該欄位」，
所以一張 common 表一旦宣告了它，將來若被 FormSchema 納入 CRUD，就會被自動加上
「等於目前公司」的條件。`st_session` 正是這種情況——它的欄位語意是「這個 session **目前在**
哪家公司」，不是「這一列**屬於**哪家公司」。實務上它只經 `SessionRepository` 的手寫 SQL 存取，
D1 的閘門碰不到它；但 common / log 要宣告此欄位前，須先確認語意確實是「歸屬」而非別的意思。

### D5：畢業流程（試用 → 獨立庫）不納入本 plan

資料搬移工具、公司代號重複的處理、搬移期間的停機與一致性。傾向**不納入**，
本 plan 只保證「表結構一致，畢業是純資料搬移」這個前提成立，工具另案。

## 風險

| 風險 | 因應 |
|------|------|
| 漏掉過濾條件 → 跨租戶外洩 | D1 建構繫結使呼叫端無從省略；D2 讓 raw 路徑漏填變編譯錯誤 |
| 表忘了宣告 `sys_company_id` → 該表完全不受隔離 | schema 驅動的代價：以測試釘住 D4 的不變式（`CategoryId="company"` 的表必須宣告此欄位） |
| `uk_` 仍為單欄 → 兩家試用公司無法有相同單號 | 階段 2 改複合索引，且需在既有表升級路徑一併處理 |
| `EmployeeContextResolver` 於共用庫跨公司誤中 | 階段 3 補公司條件，並補測試釘住 |
| 既有部署 backfill 不完整 → NOT NULL 違反 | D3 決定 nullable 收緊時機；升級腳本需可重跑 |
| 公司代號大小寫不一致 → 快取與 DB 判斷分裂 | 建立公司時正規化為大寫；比對一律 `OrdinalIgnoreCase` |
| 快取跨租戶污染 | 現有 DB 快取（`RolePermissionService` / `DepartmentTree`）本就以 companyId 為 key，需逐一確認無以 databaseId 為 key 者 |

## 不在範圍

- **資料庫層 RLS**：見 D2 選項 D，跨五種 DB 不可行。
- **共用庫的資源配額 / 限流**：試用帳號的容量上限屬營運議題。
- **`CategoryId` 語意變更**：仍是 DB scope 選擇器，不因本 plan 改變。
