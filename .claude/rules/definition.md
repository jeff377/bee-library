# 定義層（Bee.Definition）規範（骨幹）

> 定義型別的設計規範（集合屬性繼承基底、欄位參照命名、`Defaults/` 的定位）
> → `src/Bee.Definition/CLAUDE.md`（觸及該專案時自動載入）。
> BO 介面的開設判準 → `src/Bee.Business/CLAUDE.md`（那些介面在 Business，不在 Definition）。

## Cache 內的物件 init 後不可異動

**這條留常駐，因為違反者是所有 cache 消費端** —— BO、Repository、快取層、
各 UI head 都可能拿到 cache 實例，不只定義層自己。

**凡從 process-wide `ICacheContainer` 拿到的實例，每個 session 都是同一個 reference，
載入後不可在 runtime mutate**，否則跨 session 洩漏 / race。這條的成立理由是
**cache 為共用**，與資料從哪載入無關，因此兩類都適用：

| 類別 | 載入管道 | 失效管道 |
|------|---------|---------|
| **定義檔快取**（FormSchema、FormLayout、各 `*Settings`…） | `IDefineAccess.GetX(...)` | `IDefineAccess.SaveX(...)` |
| **資料庫相依快取**（CompanyInfo、CompanyRolePermissions、DepartmentTree、CompanyAuditRules、ApiKeyInfo、ApiKeyGateState） | `ICacheDataSourceProvider` | 共用 cache-notify 表（**沒有** `SaveX`） |

`SessionInfo` 是例外（本來就 per-session，cache key 即 access token）。
**完整型別清單只在 `docs/development-constraints.md`，本檔不複寫**。

- 需要 per-session 變動 → 先 `cached.Clone()` 再 mutate。
- 定義資料的持久化變更走 `IDefineAccess.SaveX(...)`（寫 storage + invalidate cache slot）；
  資料庫相依資料走所屬 repository + 一筆 cache-notify 記錄，**漏了 notify 就全 process 拿舊值**。
- 新加 Define 系列類別時記得補 `Clone()`。**資料庫相依快取型別刻意不提供 `Clone()`** ——
  它們是拿來讀的快照，需要變體就自己複製值，別補 `Clone()` 開後門。
- **`XmlCodec.Serialize(cachedInstance)` 不能當免費 deep-clone** —— 它透過
  `IObjectSerialize.SetSerializeState` 在**來源**上 mutate state，並行下 `IsSerializeEmpty` 等
  以 state 為條件的邏輯會錯亂（已踩過，fix commit `aa843f71`）。
- code review 看到「對 cache 取出的 instance 直接 mutate」或拿 `XmlCodec.Serialize(cached)`
  當克隆，必須擋下。

完整規範見 `docs/development-constraints.md` § Cached Data Immutability After Init。
