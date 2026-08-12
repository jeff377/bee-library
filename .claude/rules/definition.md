# 定義層（Bee.Definition）規範（骨幹）

> 定義型別的設計規範（集合屬性繼承基底、欄位參照命名、`Defaults/` 的定位）
> → `src/Bee.Definition/CLAUDE.md`（觸及該專案時自動載入）。
> BO 介面的開設判準 → `src/Bee.Business/CLAUDE.md`（那些介面在 Business，不在 Definition）。

## Cache 內的定義資料 init 後不可異動

**這條留常駐，因為違反者是所有 `IDefineAccess` 消費端** —— BO、Repository、快取層、
各 UI head 都可能拿到 cache 實例，不只定義層自己。

所有透過 `IDefineAccess.GetX(...)` 取得的物件（FormSchema、FormLayout、TableSchema、
SystemSettings、DatabaseSettings、ProgramSettings、DbCategorySettings、LanguageResource）
都是 **process-wide cache 共用實例**，每個 session 拿到同一個 reference。
**init 完成後不可在 runtime mutate**，否則跨 session 洩漏 / race。
`SessionInfo` 是例外（本來就 per-session）。

- 需要 per-session 變動 → 先 `cached.Clone()` 再 mutate。
- 持久化變更走 `IDefineAccess.SaveX(...)`（寫 storage + invalidate cache slot）。
- 新加 Define 系列類別時記得補 `Clone()`。
- **`XmlCodec.Serialize(cachedInstance)` 不能當免費 deep-clone** —— 它透過
  `IObjectSerialize.SetSerializeState` 在**來源**上 mutate state，並行下 `IsSerializeEmpty` 等
  以 state 為條件的邏輯會錯亂（已踩過，fix commit `aa843f71`）。
- code review 看到「對 cache 取出的 instance 直接 mutate」或拿 `XmlCodec.Serialize(cached)`
  當克隆，必須擋下。

完整規範見 `docs/development-constraints.md` § Definition Data Immutability After Init。
