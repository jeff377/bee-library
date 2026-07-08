# 框架保留命名

[English](framework-reserved-names.md)

> 列出 **bee-library** 框架所擁有的命名：哪些 `st_*` 系統表存在、框架保留了哪些 `progId`。
>
> - 命名**規則**（`st_` vs `ft_` 前綴、欄位／索引慣例）見 [資料庫命名規範](database-naming-conventions.zh-TW.md)。
> - 完整 **API 方法參考**（action 簽章、`[ApiAccessControl]`、用途）見 [API 方法參考](api-method-reference.zh-TW.md)。
> - 本文件是「**哪些具體名稱**被框架保留」的唯一來源。擴充或整合框架時，請勿與此處列出的任何名稱衝突。

---

## 1. 系統表（`st_*`）

`st_` 前綴語意為「框架所有的表」，**與該表所在的資料庫位置正交**：`st_*` 表可以位於 common 資料庫、per-company 資料庫，也可以位於 log 資料庫——重點是「誰擁有」而不是「住哪裡」。

### 1.1 Common 資料庫（全域共用）

| 表名 | 用途 |
|------|------|
| `st_user` | 全域使用者主檔（登入帳號、密碼雜湊、個資）。 |
| `st_company` | 公司清單（per-tenant 根目錄）。 |
| `st_user_company` | 哪些使用者可進入哪些公司。 |
| `st_session` | Session / Access Token。 |
| `st_define` | DB-backed 定義儲存（FormSchema / TableSchema 等，非 XML 檔案版本）。 |
| `st_cache_notify` | 跨節點 cache 失效通道（[ADR-017](adr/adr-017-db-cache-invalidation.md)）。 |

### 1.2 公司資料庫（per-tenant）

| 表名 | 用途 |
|------|------|
| `st_role` | 角色定義（[ADR-019](adr/adr-019-permission-authorization-model.md)）。 |
| `st_role_grant` | 角色↔資源授權（per model / action）。 |
| `st_user_role` | 使用者↔角色綁定。 |
| `st_department` | 組織部門。 |
| `st_employee` | 員工（連結 common DB 的 `st_user` 至 per-company 的組織位置）。 |

> `st_department` / `st_employee` 雖位於公司資料庫，但仍是 **框架所有**（record-scope 與組織樹功能所需），不是業務資料。Per-company 業務表請使用 `ft_` 前綴。

### 1.3 Log 資料庫（資料軌跡）

| 表名 | 用途 |
|------|------|
| `st_log_login` | 登入事件（成功 / 失敗 / 鎖定 / 登出）。 |
| `st_log_change` | 異動記錄——一次 Save / Delete 一列，`changes_xml` 承載 DataSet DiffGram 新舊值。 |
| `st_log_access` | 檢視記錄（誰看了哪筆記錄）。 |
| `st_log_anomaly_api` | API 層異常（Error / Timeout / Slow）——哪個動作偏離正常。 |
| `st_log_anomaly_db` | DB 層異常（Error / Timeout / Slow / 大量列數）——哪個資料庫 + 指令偏離正常。 |

> Log 表**預設關閉（opt-in）**且自足：去正規化觸發者的 user / company，查詢不需跨資料庫 join（log 資料庫實體分離）。log 資料庫可依年份分庫（`log_2024`、`log_2025`…），當年度可寫、歷史唯讀。設計理由見 [ADR-027](adr/adr-027-audit-trail.md)。

---

## 2. 保留 `progId`

### 2.1 系統軸

- **`System`**（formalize 為 `Bee.Definition.SysProgIds.System`）—— 系統層級 business object（`SystemBusinessObject`）的單例 entry point。所有與特定 form 無關的框架層級 action（登入、ping、取定義等）皆由此 dispatch。

完整 action 清單見 [API 方法參考 §軸：System](api-method-reference.zh-TW.md)。

### 2.2 框架預設 form

框架隨組織／record-scope 功能 ship 出下列預設 form：

| progId | 對應表 | 用途 |
|--------|-------|------|
| `Department` | `st_department` | 部門維護表單。 |
| `Employee` | `st_employee` | 員工維護表單。 |

每個 form progId 共通繼承的 FormBO action 清單見 [API 方法參考 §軸：Form](api-method-reference.zh-TW.md)。

---

## 3. 消費者命名守則

擴充 bee-library 或在其上建立應用時：

- **自家業務表用 `ft_` 前綴**，不要用 `st_`（保留給框架）。詳見 [資料庫命名規範](database-naming-conventions.zh-TW.md)。
- **避開保留的 `progId`**——`System`、`Department`、`Employee` 已被框架使用。自家 progId 請取不同名稱；慣例為 `PascalCase`，常以模組縮寫前綴。
- **要擴充框架表**（例如為 `st_employee` 加自訂欄位）：在應用程式的 `DefinePath` 中放一份同名 `.TableSchema.xml`。runtime 框架只讀 `DefinePath`，這份檔就是框架實際看到的唯一來源。框架內 embedded 預設 runtime 不會參與——只供下方 API 一次性匯出使用。
- **取得 base XML 起手**——三種途徑，按一般偏好排序：
    - **程式碼層 API**（canonical）：`Bee.Definition.Defaults.MaterializeTo("./Define")` 把所有 embedded 框架預設 XML 寫入指定目錄。預設 skip-existing，重複跑安全、不會覆蓋你的客製。詳見 [`src/Bee.Definition/Defaults.cs`](../src/Bee.Definition/Defaults.cs)。
    - **CLI**（CI / setup 腳本首選）：一次性安裝 `dotnet tool install -g Bee.Cli`（之後升版用 `dotnet tool update -g Bee.Cli`），後續 `dotnet bee defines materialize --path ./Define`——同一份 API 的 thin shell。`dotnet bee defines list` 列出所有 embedded 檔、`dotnet bee defines materialize --filter TableSchema/` 只 materialize 子集。
    - **GitHub 瀏覽**：所有 embedded 預設都活在 repo 的 [`src/Bee.Definition/Defaults/`](../src/Bee.Definition/Defaults/)——打開你要的檔，內容複製到自家 `DefinePath`。
- **框架升版若異動 `st_*` 表結構**，會在 [CHANGELOG](../CHANGELOG.zh-TW.md) 中標示為 breaking change。改名類異動需手動執行 `RENAME TABLE`——範例見 [資料表結構升級指南 §框架表改名](database-schema-upgrade.zh-TW.md)。

---

## 延伸閱讀

- [資料庫命名規範](database-naming-conventions.zh-TW.md)——`st_` / `ft_` 區分背後的命名規則。
- [API 方法參考](api-method-reference.zh-TW.md)——完整 BO 方法目錄。
- [架構總覽](architecture-overview.zh-TW.md)——`st_*` 表在整體 N-tier + clean architecture 中的位置。
- [ADR-019：權限授權模型](adr/adr-019-permission-authorization-model.md)——為何 `st_role` / `st_user_role` / `st_employee` 是框架所有。
