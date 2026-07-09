# 計畫：DataSet 欄名全小寫（定義 / 資料 / UI 三者一致，消除大小寫例外）

**狀態：🚧 進行中（2026-07-09）— 程式遷移完成並經 SQLite 真 DB 驗證；剩多 DB provider 容器全回歸 + 發佈時 CHANGELOG breaking 條目**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 0 | 探勘稽核：鎖定所有「大寫來源」與「依賴大寫的消費端」 | ✅ 已完成（2026-07-09）— C# 端 0 處大寫字面比較（皆走大小寫無關 DataColumnCollection）；消費端僅第一方 JS |
| 1 | 決策定案：策略、wire 相容、版本邊界 | ✅ 已完成 — 由 [ADR-029](../adr/adr-029-lowercase-field-names.md) 拍板全小寫、breaking wire 變更 |
| 2 | 核心切換：`AddColumn` / `DbAccess` 正規化改小寫 + `UppercaseColumnNames` → `LowercaseColumnNames` | ✅ 已完成（2026-07-09） |
| 3 | wire 與序列化對齊 + JS/TS 前端同步 | ✅ 已完成 — JSON/MessagePack converter 直出 `ColumnName` 故自動小寫；`Web.Js.Demo/app.js` 已改小寫 key |
| 4 | 持久化相容 + 全 repo 回歸 | 🚧 SQLite 真 DB 驗證通過（Bee.Db 209 / Bee.Repository 10）；audit 歷史以「讀取端大小寫無關」相容；**待**：SQL Server/PG/MySQL/Oracle 容器全回歸 + 發佈時 CHANGELOG 標 breaking |

> **本計畫不含實作**，是動工前的評估與規劃。當前的「運算式欄名大小寫」bug **已於 commit 96821c04 以解耦方式修好**（運算式綁 `FormField.FieldName`，與 DataSet 儲存大小寫無關），**與本遷移相互獨立**——本計畫是為了根治「大小寫敏感的名稱比對」這一類反覆咬人的問題，讓定義 / 資料 / UI 三者欄名天生一致。

## 背景與目標

### 為何反覆踩大小寫

DataSet 欄名目前**恆為大寫**，這是刻意的正規化，最初動機是**繞過 UI 控制項欄位繫結區分大小寫**（讓繫結一致）。但「大小寫敏感的名稱比對」是同一類問題，會在不同子系統反覆出現：

- **UI 繫結**（最初）：控制項欄位繫結區分大小寫 → 全大寫繞過。
- **運算式引擎**（2026-07-09）：DynamicExpresso 識別字區分大小寫；`BuildVariables` 曾用大寫 `ColumnName` 當變數 key，但運算式引用小寫宣告欄名 → `UnknownIdentifierException`（已解耦修好）。

### 目標

讓**定義層（`FormField.FieldName`）、資料層（DB 欄名 / DataSet `ColumnName`）、UI 層（控制項繫結）三者欄名一律小寫 `snake_case`**，使「欄名」在全系統只有一種寫法，從此不再需要在任何子系統處理大小寫落差。

## 現況探勘結論（2026-07-09，動筆前已確認）

### A. 大寫的「產生來源」（要改的點）

| 來源 | 位置 | 說明 |
|------|------|------|
| DB 讀取正規化 | `src/Bee.Db/DbAccess.cs:409`、`:814` | `adapter.Fill` / `table.Load` 後緊接 `table.UppercaseColumnNames()` —— **所有 DB 讀取結果強制大寫**。跨 provider 差異（Oracle 回大寫、PostgreSQL 回小寫…）在此被統一。這是大寫慣例的**主錨點**。 |
| 程式建表 | `src/Bee.Base/Data/DataTableExtensions.cs:22` | `new DataColumn(fieldName.ToUpper(), ...)` —— `GetNewData` / client 空 DataSet 的欄名來源。 |
| 顯式工具 | `src/Bee.Base/Data/DataTableExtensions.cs:132` `UppercaseColumnNames()` | 對既有 DataTable 逐欄 `ColumnName.ToUpper()`。 |

### B. 「依賴大寫」的消費端（會被 breaking 的點）

| 消費端 | 位置 | 風險 |
|--------|------|------|
| **JSON wire** | `DataTableJsonConverter.cs:39/169`、`DataSetJsonConverter.cs:50/55` | 直出 `col.ColumnName`（大寫）→ JSON payload 欄名為大寫。 |
| **MessagePack wire** | `Bee.Api.Core/MessagePack/DataSetFormatter.cs` / `DataTableFormatter`（`MessagePackCodec.cs:27-28` 註冊） | 同樣以 `ColumnName` 編碼 → MessagePack payload 欄名為大寫。 |
| **JS/TS 前端（外部 API surface）** | `samples/Web.Js.Demo/app.js:90/142/143/189` | 直接讀大寫 key：`r.current.SYS_ROWID`、`row.current.SYS_ID/SYS_NAME`。**改小寫 → 全部 `undefined`**。外部框架使用者的前端亦同。 |
| **持久化：變更稽核歷史** | `FormBusinessObject.cs:436` `SerializeDiffGram` → `changes.WriteXml(DiffGram)` | audit log 存 DataSet DiffGram XML，欄名為 XML 元素名（大寫）。**既有歷史資料欄名是大寫**，遷移後新舊不一致（讀舊記錄的解析需相容兩種大小寫）。 |

### C. 不依賴大寫（好消息）

- **C# 端**：全 repo **無** `ColumnName ==` / `.Equals` 對大寫字面的比較（唯一命中是 `ApiAuthorizationValidator` 的 `"Bearer "`，無關）。C# 存取一律走 `row["sys_id"]` / `Columns.Contains(...)` / `Columns["..."]`——`DataColumnCollection` 查找**大小寫無關**，故 C# 側切換零改動。
- **Avalonia UI 讀寫**：`FormDataObject.GetField/SetField` 走 `row[fieldName]` / `Columns[fieldName]`（大小寫無關）→ Avalonia head 應不依賴大寫（**Phase 0 須逐一驗證 WinForms / Blazor / MAUI head 的繫結是否同樣大小寫無關**，因為大寫最初正是為某個 UI head 的區分大小寫繫結而設）。

## 待定案決策（Phase 1）

### 決策 1：遷移策略——「全小寫」 vs 「全面 case-insensitive」

兩種都能根治「大小寫例外」，但哲學不同：

- **(A) 全小寫正規化（使用者傾向）**：`AddColumn` / `DbAccess` 改產小寫，wire、UI、定義四者天生一致。**優點**：所見即一致（含 wire 上 `row.current.sys_rowid` 對齊 DB/schema，對 JS 消費端長期更直覺）。**代價**：wire 破壞 + audit 歷史不一致，須協調所有 client。
- **(B) 全面 case-insensitive**：不動儲存大小寫，改讓**每個做名稱比對的子系統**都大小寫無關（DataColumnCollection 已是；運算式已解耦；UI 繫結改 case-insensitive）。**優點**：零 wire 破壞。**缺點**：沒達成使用者要的「三者欄名字面一致」，且「哪些地方仍大小寫敏感」需持續盯防。

> **已由 [ADR-029](../adr/adr-029-lowercase-field-names.md) 拍板為 (A) 全小寫**（2026-07-09，理由：讓定義/資料/UI 三者欄名一致，從源頭消除大小寫例外）。以下切分以 (A) 為主軸；(B) 僅列為對照記錄。

### 決策 2：wire 相容——沒有「不破壞」的小寫路徑

任何讓 wire 欄名變小寫的做法（改儲存、或在 converter 層 lowercase）都會**破壞現有讀大寫 key 的 JS/TS 消費端**。故 (A) 必然是 **breaking change**，須：

- 在 **major 版本邊界**釋出，並同版更新所有第一方前端（`Web.Js.Demo`、`bee-api-client.js`…）。
- 於 CHANGELOG / 遷移指南明確標 breaking：「DataSet JSON/MessagePack 欄名由大寫改為小寫（對齊 DB/FormField）」。
- 評估是否提供過渡期相容旗標（例如 converter 可選輸出大寫，供舊前端緩衝）——**傾向不做**（雙軌會讓大小寫問題延續，違背本計畫初衷）。

### 決策 3：持久化歷史相容

change-audit 既有 DiffGram XML 欄名為大寫。遷移後：

- **讀取路徑**須容忍「歷史大寫 / 新小寫」兩種——DiffGram 還原本就經 `ReadXml` 建 DataTable，欄名照 XML 元素名還原；下游若以欄名比對需大小寫無關（多半已是）。
- **不回填改寫既有 audit 資料**（audit 應為不可變歷史）。以「解析端相容」而非「資料遷移」處理。

## 交付切分（Phase）

| Phase | 範圍 | 主要檔案 / 動作 |
|-------|------|----------------|
| **0 稽核** | 確認 B 表消費端全集：逐一驗證 **各 UI head 繫結大小寫**（WinForms / Blazor / Avalonia / MAUI）、掃描第一方 JS/TS 讀大寫 key 的處、確認 MessagePack DataSet formatter 欄名路徑、確認 audit 讀取端大小寫無關 | 無程式改動，產出稽核清單 |
| **1 決策** | 依 Phase 0 拍板策略 (A)/(B)、版本邊界、過渡旗標與否、CHANGELOG breaking 條目草稿 | 本文件更新 |
| **2 核心切換** | `AddColumn` `ToUpper`→`ToLower`；`DbAccess` ×2 與 `UppercaseColumnNames`→`LowercaseColumnNames`（含既有測試 `DataExtensionsTests`）；掃描並修正任何 C# 大寫字面依賴（目前為 0） | `DataTableExtensions.cs`、`DbAccess.cs` |
| **3 wire + 前端** | 確認 JSON/MessagePack converter 隨 `ColumnName` 自動變小寫（多半無需改，因直出 ColumnName）；同步 `Web.Js.Demo` / `bee-api-client.js` 等第一方前端讀小寫 key；更新 wire 相關測試 | `samples/Web.Js.Demo/*`、序列化測試 |
| **4 相容 + 回歸** | audit 讀取端大小寫相容;全 repo build+test（5 DB provider × 各 UI head × wire 兩格式 round-trip）;CHANGELOG breaking + 遷移指南 | 全面回歸 |

## 風險

- **外部 API surface 破壞**：最大風險，須 major 版 + 遷移指南。第三方前端不在我方掌控，需明確公告。
- **測試盲區**：現有測試多以小寫欄名手建 DataTable（正是本次運算式 bug 漏網的原因）；反過來，改小寫後要確保仍有**覆蓋 wire round-trip 實際欄名**的測試，避免又一個大小寫盲區。
- **UI head 差異**：若某 head 繫結確實區分大小寫（大寫的原始動機），改小寫後該 head 繫結會壞——Phase 0 必須先證實各 head 皆大小寫無關，否則 (A) 不成立、須退回 (B) 或該 head 內部正規化。

## 對照：不做本遷移的現況

當前 bug 已用解耦修好（運算式綁 `FormField.FieldName`）。若最終決定不遷移，現況可接受——只是「DataSet 內部大寫」這個實作細節仍透過 wire 洩漏，且未來新子系統若做大小寫敏感的欄名比對仍需各自解耦。本計畫的價值在於**一次根治**，代價是 breaking change。
