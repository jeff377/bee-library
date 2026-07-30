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

**為何用字串而非 `company_rowid`**：它與 session、`ICompanyInfoService`、權限快照用的是同一個
key，過濾時零轉換，也少一個對不上的環節；共用庫裡人眼可讀，支援與除錯直接看得懂。代價是
公司代號變更會牽動資料列——但公司代號變更本來就已牽動 `CompanyInfo` 快取鍵與權限資料，
不是這裡新增的約束。

**為何獨立庫也要帶**：獨立庫裡它是常數、看似浪費一個短字串，換來的是兩種模式**表結構完全一致**，
同一份 `TableSchema` 通用；試用公司畢業到獨立庫時是**純資料搬移，不是 schema 分岔**。
若只有共用庫才加欄位，等於長期維護兩種表形狀，且畢業流程要做 schema 轉換。

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
repository，之後該 repository 的每一條 SELECT / UPDATE / DELETE 都無條件 AND 上租戶條件，
INSERT 一律蓋章。呼叫端沒有「不傳」這個選項。

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

## 待議項目

### D1：租戶條件的注入層級

| 選項 | 說明 | 評估 |
|------|------|------|
| **A. repository 建構繫結**（建議） | 工廠建構時注入 `CompanyId`，repository 內部對每條語句 AND 條件 | 呼叫端無從省略；改動集中在 `DataFormRepository` 與工廠 |
| B. `DbCommandSpec` 建構層 | 更低層，凡是碰到宣告了該欄位的表就注入 | 覆蓋面最廣（含 AnyCode），但要在 SQL 文字層解析目標表，脆弱 |
| C. BO 層組 FilterNode | 與現有 scopeFilter 同形狀 | **不建議**，理由見上節 |

### D2：AnyCode / raw SQL 路徑怎麼收（本 plan 最關鍵的一題）

報表與批次由 BO 自寫 SQL、經 `IDbAccessFactory.Create(databaseId)` 直查，完全繞過 D1 的注入。
共用庫下這正是最會忘記、也最容易外洩的一條路。

| 選項 | 說明 | 代價 |
|------|------|------|
| A. 收斂入口 | 不再直接公開 `IDbAccessFactory` 給 BO，改提供一個「已綁公司」的封裝，取得 `DbAccess` 必須經過它 | 改動既有 BO 呼叫端；封裝仍無法阻止手寫 SQL 漏條件 |
| B. 必填參數 | raw 查詢入口強制傳入公司代號，簽章上就要求 | 仍靠人填對，但「忘記」會變成編譯錯誤 |
| C. 規範 + 掃描 | 文件規範 ＋ 自訂分析器 / 測試掃描偵測未帶條件的查詢 | 最不侵入，但保證最弱 |
| D. 資料庫層 RLS | 交由 DB 的 row-level security | 五種 DB 支援度不一（SQLite 無），與框架跨庫策略衝突 |

**需要你裁決**：安全性與既有 BO 改動量的取捨。我的傾向是 **B ＋ C**——讓漏填變成編譯期錯誤，
再以掃描補手寫 SQL 的那一段；A 的封裝擋不住真正的破口（SQL 文字仍是自己寫的），
成本卻最高。

### D3：既有部署與既有資料的遷移

- 所有既有業務表都要 ALTER ADD `sys_company_id`，且既有列需 backfill。
- 獨立庫（一庫一公司）的 backfill 值可由 `DatabaseSettings` 反查該庫對應的公司代號；
  **一庫多公司在既有部署中不存在**，故 backfill 無歧義。
- 欄位 nullability：字串常態有值，依偏好宣告 NOT NULL；但 Oracle 視 `''` 為 NULL，
  backfill 未覆蓋到的列會違反 NOT NULL。需決定是「先 nullable、backfill 後再收緊」還是
  「ALTER 時即帶 DEFAULT」。

### D4：哪些表要有這個欄位

| 類別 | 例 | 是否需要 |
|------|-----|---------|
| 業務表（`ft_*`，`CategoryId="company"`） | 訂單、員工、部門 | ✅ 必要 |
| 存於 company 庫的系統表 | `st_role`、`st_role_grant`、`st_user_role` | ✅ 共用庫下必要，否則權限跨公司互見 |
| common 庫的系統表 | `st_user`、`st_company`、`st_session` | ❌ 本就跨公司共用；`st_session` 的公司欄位是「目前在哪家」，語意不同但沿用同一命名 |
| log 庫 | 稽核記錄 | 待議：稽核是否需要按公司區隔查詢 |

### D5：畢業流程（試用 → 獨立庫）是否納入本 plan

資料搬移工具、公司代號重複的處理、搬移期間的停機與一致性。傾向**不納入**，
本 plan 只保證「表結構一致，畢業是純資料搬移」這個前提成立，工具另案。

## 風險

| 風險 | 因應 |
|------|------|
| 漏掉過濾條件 → 跨租戶外洩 | D1 建構繫結使呼叫端無從省略；D2 裁決後補齊 raw 路徑 |
| `uk_` 仍為單欄 → 兩家試用公司無法有相同單號 | 階段 2 改複合索引，且需在既有表升級路徑一併處理 |
| `EmployeeContextResolver` 於共用庫跨公司誤中 | 階段 3 補公司條件，並補測試釘住 |
| 既有部署 backfill 不完整 → NOT NULL 違反 | D3 決定 nullable 收緊時機；升級腳本需可重跑 |
| 快取跨租戶污染 | 現有 DB 快取（`RolePermissionService` / `DepartmentTree`）本就以 companyId 為 key，需逐一確認無以 databaseId 為 key 者 |

## 不在範圍

- **資料庫層 RLS**：見 D2 選項 D，跨五種 DB 不可行。
- **共用庫的資源配額 / 限流**：試用帳號的容量上限屬營運議題。
- **`CategoryId` 語意變更**：仍是 DB scope 選擇器，不因本 plan 改變。
