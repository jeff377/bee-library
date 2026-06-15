# 計畫：根治 SQLite GUID 大小寫一致性問題

**狀態：✅ 已完成（2026-06-15，方向 (c) COLLATE NOCASE 已實作並全套件驗證）**

## 背景

SQLite 沒有 GUID 原生型別，本框架把 GUID 欄宣告為 `UUID`（type affinity，實際以 **TEXT** 儲存且**比對區分大小寫**）。專案內 GUID 的大小寫來源不一致：

- seed / 既有資料：**大寫**（`6689B38C-…`）
- `Guid.NewGuid().ToString()`：**小寫**
- Microsoft.Data.Sqlite 綁 `Guid` 參數：**大寫** TEXT
- `GetData` 從 SQLite 讀回 GUID 欄：**String 型**（保留 DB 原大小寫）
- `GetNewData` 依 schema 建新列：**Guid 型**欄

GUID 在語意上本應大小寫無關，但 SQLite 的 TEXT `=` 比對不是 → 任何「client 產生 GUID → 寫進字串欄 → 當 key 比對」都可能跨大小寫脫鉤。

### 已套用的針對性修法（commit 3e329da8，非根治）

master-detail 連結：明細 `sys_master_rowid` 連 master 的**原值**（保留大小寫），不再經 `Guid.Parse/ToString` round-trip。
- `FormRowDefaults.Apply`（[FormRowDefaults.cs:31](src/Bee.Definition/Forms/FormRowDefaults.cs)）的 `masterRowId` 改 `object?` 原樣寫入
- `FormDataObject.ResolveMasterRowId`（[FormDataObject.cs:579](src/Bee.UI.Avalonia/DataObjects/FormDataObject.cs)）回原始值
- `DataFormRepository.GetData`（[DataFormRepository.cs:202](src/Bee.Repository/Form/DataFormRepository.cs)）用 `CoerceToGuid` 隱藏 String vs Guid 差異後再 filter detail

### 殘留脆弱性（本計畫要根治的對象）

1. 既有訂單（master sys_rowid 讀回為 String 大寫）新增的明細，**明細自身 `sys_rowid`** 仍可能以與大寫慣例不一致的形式存入。
2. `GetData`（String 欄）vs `GetNewData`（Guid 欄）型別不一致，逼出 `CoerceToGuid` 這類 defensive 補丁散落各處。
3. 凡 client 產生 GUID 寫字串欄、之後當 key 比對的路徑，都帶同一風險。

完整脈絡見記憶 `sqlite-guid-text-case-sensitivity.md`。

## 現況關鍵程式碼（盤點結果）

| 關注點 | 位置 | 現況 |
|--------|------|------|
| GUID → SQLite 欄型別 | [SqliteTypeMapping.cs:56](src/Bee.Db/Providers/Sqlite/SqliteTypeMapping.cs) | `FieldDbType.Guid → "UUID"`（TEXT affinity） |
| 欄定義（CREATE/ALTER 共用） | [SqliteSchemaSyntax.cs:97-105](src/Bee.Db/Providers/Sqlite/SqliteSchemaSyntax.cs) | text 欄加 `COLLATE NOCASE`，**Guid 欄沒加** |
| CREATE 路徑 | [SqliteCreateTableCommandBuilder.cs:127](src/Bee.Db/Providers/Sqlite/SqliteCreateTableCommandBuilder.cs) | 呼叫 `GetColumnDefinition` |
| ALTER ADD 路徑 | [SqliteTableAlterCommandBuilder.cs:68](src/Bee.Db/Providers/Sqlite/SqliteTableAlterCommandBuilder.cs) | 同呼叫 `GetColumnDefinition`（單點覆蓋兩路徑） |
| 讀取（generic） | [DbAccess.cs:297-321](src/Bee.Db/DbAccess.cs) | `DbDataAdapter.Fill`，SQLite 回 String、其他 DB 回 native |
| 讀取（schema 感知） | [DataFormRepository.cs:202](src/Bee.Repository/Form/DataFormRepository.cs) | 有 `CoerceToGuid` 補丁 |
| 參數綁定正規化 | [DbCommandSpec.cs:148-168](src/Bee.Db/DbCommandSpec.cs) | 僅 Oracle 特例（Guid→byte[]）；其餘原值傳入 |
| 型別對應表 | [DbTypeConverter.cs](src/Bee.Base/Data/DbTypeConverter.cs) | `FieldDbType.Guid ↔ typeof(Guid) ↔ DbType.Guid` |

## 各 DB 的 GUID 欄型別與大小寫天性

| DB | GUID 欄型別 | 比對是否大小寫敏感 | 需處理？ |
|----|------------|------------------|---------|
| **SQLite** | `UUID`（實為 TEXT） | **是**（區分大小寫） | **是 ← 本案焦點** |
| SQL Server | `uniqueidentifier` | 否（native 二進制比對） | 否 |
| PostgreSQL | `uuid` | 否（native） | 否 |
| MySQL | `CHAR(36)` | 視欄位 collation；預設 `_ci` 不敏感 | 大致否（建議驗證 collation） |
| Oracle | `RAW(16)` | 否（二進制；綁定已轉 byte[]） | 否 |

**結論：大小寫脫鉤是 SQLite 獨有問題**，其他 4 DB 天生大小寫無關。這直接影響三個方向的取捨。

---

## 三個根治方向評估

### 方向 (a) — 框架統一 GUID 大小寫正規化

**作法**：選定 canonical 大小寫（建議大寫，對齊 seed 慣例與 Microsoft.Data.Sqlite 預設），在所有「GUID 變成字串 key」的邊界強制正規化（參數綁定、讀回後）。

| 面向 | 評估 |
|------|------|
| 改動點 | `DbCommandSpec.NormalizeParameterValue`（綁定）、讀回路徑 |
| Blast radius | 中 |
| 各 DB 影響 | 僅 SQLite 需要；但邏輯放在共用綁定層 |
| **致命缺陷** | 綁定層是 **value-driven 非 schema-driven** —— 無法在綁定當下判斷「這個 string 是 GUID 還是一般文字」。盲目把所有 string 大寫會**破壞真實文字資料**。要正確套用反而得引入 schema 感知，複雜度等同方向 (b) |
| 其他 | 不修正**已存入**的混合大小寫資料 |

**判定：不建議**。在現有 value-driven 綁定架構下難以正確界定範圍。

### 方向 (b) — GUID 欄讀回為 Guid 型（typed GetData）

**作法**：讓讀取路徑依 schema 把 SQLite GUID 欄轉成 `System.Guid`，使 DataTable 欄為 Guid 型，記憶體內徹底消除字串大小寫（Guid 相等比對本就大小寫無關）。

| 面向 | 評估 |
|------|------|
| 改動點 | 讀取路徑需 schema 驅動建 DataColumn 型別（`DataTable` 一旦有資料不能改 `DataType`，須先建 schema 再 Fill，或建新欄複製） |
| Blast radius | **大** |
| **主要風險 1** | `DbAccess.ExecuteDataTable` 是**通用**路徑，執行任意 SQL（報表、AnyCode），**並非總是 schema 感知** → 無法全域判斷哪些欄是 GUID。只能在 schema 驅動的 `DataFormRepository` CRUD 路徑套用 |
| **主要風險 2** | 改變欄型別會波及 **wire 合約**與既有「預期 String」的消費端；`CoerceToGuid` 正是因為呼叫端拿到 String 才存在，反轉預設值會連鎖 |
| 各 DB 影響 | 可順帶統一 MySQL `CHAR(36)`（也回 string）；SQLServer/PG 已 native；Oracle 已特例。架構上最「純」 |
| 正面 | 治本——記憶體內不再有 GUID 字串，`CoerceToGuid` 補丁可逐步退場 |

**判定：架構最純但 blast radius 最大**。全域套用風險高；**限縮在 FormSchema 驅動 CRUD 路徑**則可行，可作為 (c) 之上的中長期強化。

### 方向 (c) — GUID 欄加 `COLLATE NOCASE`（SQLite only）★建議

**作法**：SQLite 的 GUID(`UUID`) 欄在 CREATE/ALTER 時加 `COLLATE NOCASE`，使該欄 `=` 比對大小寫無關——讓 SQLite 對齊其他 4 DB 的天生行為。

| 面向 | 評估 |
|------|------|
| 改動點 | **單點**：[SqliteSchemaSyntax.cs:100](src/Bee.Db/Providers/Sqlite/SqliteSchemaSyntax.cs) 把 Guid 納入加 COLLATE 的條件。CREATE 與 ALTER ADD 共用 `GetColumnDefinition`，一改兩路徑齊覆蓋 |
| Blast radius | **小**，SQLite-scoped |
| 技術正確性 | SQLite `NOCASE` 折疊 ASCII A–Z；GUID hex 為 `0-9A-F` 全 ASCII → **完整覆蓋** GUID 大小寫。`col = value`／`JOIN` 以欄位 collation 為準 → orphan-detail-on-reload 與任何 GUID key 比對都被修正 |
| 各 DB 影響 | 僅 SQLite；其他 DB 不動（天生不敏感） |
| 既有 DB 遷移 | 既有 SQLite 表的欄 collation 需重建才生效；**測試每次重建 schema → 自動套用**；正式環境 SQLite 需 migration（或重建）才吃到 |
| 限制 | 只讓**比對**大小寫無關，**不正規化儲存值**（混合大小寫仍存著，但比對已不在意）。不改讀回型別 → `CoerceToGuid` 等補丁仍保留（無害） |

**判定：建議作為主方案**。最小、最精準、blast radius 最低，直擊「SQLite TEXT 比對區分大小寫」根因，且不碰通用讀取／綁定／wire 合約。

---

## 建議

**主方案採方向 (c)**：SQLite GUID 欄加 `COLLATE NOCASE`，一個 dialect 單點改動讓 SQLite 與其餘 4 DB 的 GUID 比對行為一致。

**中長期可選強化（方向 b 限縮版）**：若日後仍想消除記憶體內 GUID 字串、退場 `CoerceToGuid`，再在 **FormSchema 驅動的 `DataFormRepository` CRUD 路徑**（schema 已知）把 GUID 欄 typed 成 `Guid`，**不**動通用 `DbAccess` 報表/AnyCode 路徑與 wire 合約。此項非必要、可獨立另案。

**不採方向 (a)**：value-driven 綁定層無法安全界定「哪些 string 是 GUID」。

## 實作範圍（待選定 (c) 後）

| # | 動作 | 檔案 |
|---|------|------|
| 1 | `GetColumnDefinition` 的 COLLATE 條件納入 `FieldDbType.Guid`（與 String/Text 並列），更新對應 XML 註解說明 GUID 也加 NOCASE 的理由 | [SqliteSchemaSyntax.cs:97-115](src/Bee.Db/Providers/Sqlite/SqliteSchemaSyntax.cs) |
| 2 | 新增/擴充測試：驗證 SQLite GUID 欄產生的 DDL 含 `COLLATE NOCASE`；以混合大小寫 GUID 做 `WHERE guid_col = '<相反大小寫>'` 命中測試（master-detail reload 跨大小寫不再 orphan） | `tests/Bee.Db.UnitTests/`（SQLite 對應） |
| 3 | 回歸：`./test.sh` 全套件綠（特別 master-detail 持久化、`DataFormRepository` 相關） | — |
| 4 | 更新記憶 `sqlite-guid-text-case-sensitivity.md`：標記殘留脆弱性已由 COLLATE NOCASE 根治、(b) 為可選未來強化 | memory |

## 驗證

- 桌面可本機驗證 → 直接改 main：先 `dotnet build -c Release` + `./test.sh`（含 SQLite `[DbFact]`），綠了再 commit/push。
- 重點確認 commit 3e329da8 修過的 master-detail 場景在 (c) 下仍綠（兩者正交、互補）。

## 不在範圍

- 不動 SQLServer/PG/MySQL/Oracle 的 GUID dialect（天生大小寫無關）。
- 不在本案做方向 (b)（typed read path）—— 列為可選未來另案。
- 不回填既有 SQLite 資料的大小寫（COLLATE 讓比對無關，無需回填）。
