# 計畫：ERP 資料軌跡 / 日誌功能（log 資料庫分類）— 母計畫

**狀態：🚧 進行中（2026-07-05）**

> 本文件是**母計畫 / 索引**：承載研究藍本（§1–§3）與跨項共用架構（§4–§7）。實際實作已**依六軸拆成獨立工作項**，逐項討論定案再執行——見下方拆解表，各項有自己的 plan 檔與狀態。

## 工作項拆解與排程

| 項 | 工作項 | 對應軸 | 資料表 | plan | 狀態 |
|----|--------|--------|--------|------|------|
| 0 | 日誌基礎設施 | —（前置） | outbox（於項 2） | [plan-audit-0-foundation.md](plan-audit-0-foundation.md) | ✅ 已完成 |
| 1 | 登入記錄 | ① | `st_log_login` | [plan-audit-1-login.md](plan-audit-1-login.md) | ✅ 已完成 |
| 2 | 異動記錄（含安全⑥） | ③(+⑥) | `st_log_change`（DiffGram `changes_xml` 單表）；EAV `st_log_change_field` 選配 | [plan-audit-2-change.md](plan-audit-2-change.md) | ✅ 已完成 |
| 3 | 檢視記錄 | ② | `st_log_access` | [plan-audit-3-access.md](plan-audit-3-access.md) | ✅ 已完成 |
| 4 | 系統/錯誤（選配） | ⑤ | `st_log_trace` | *(定案後建立)* | 📝 待做 |
| 5 | 執行記錄（範圍/必要性待決定） | ④ | `st_log_exec` | *(待決定)* | 🅿️ 暫緩（最後決定） |

- 六軸→收斂成「基礎設施 + 5 項」（軸⑥安全按 D3 併入項 2、以旗標區分）。
- 排程：**0 ✅ → 1 登入 ✅ → 2 異動 ✅ →** 3 檢視 → 4 系統選配 →（5 執行記錄：暫緩，最後決定）。每項流程固定：**出細部 plan → 逐點定案 → 執行**。
- **執行記錄（項 5）暫緩原因**：全記每次 `JsonRpcExecutor` 呼叫量體過大，且與項 1（登入）/項 2（異動）重複。SAP（SAL 選擇性 filter、STAD 短期統計、SLG1 opt-in）與 Odoo（ir.logging opt-in、OCA per-rule、read 昂貴）都**不全記**。若日後做，方向為**選擇性/可設定**（例：只記非 CRUD 業務動作 + 錯誤，排除讀取與已覆蓋的 Save/Delete/Login），對齊框架既有 `DbAccessAnomalyLogOptions` 只記異常的哲學。範圍待最後決定。
- **未來增強：per-form 稽核規則（涵蓋項 2 異動 + 項 3 檢視）**：目前異動/檢視皆「全記所有表單」。未來加一份 admin 執行期規則（「哪些 progId 要做異動/檢視記錄」，per-form、per-操作），對齊 **Odoo `auditlog.rule`**（管理員挑表單、不改程式）。SAP 對照：Change Documents 開發期旗標 / RAL 管理員設定。預設維持全記以相容。
- 完成一項回頭把本表狀態更新為 ✅；全部完成後補 [framework-reserved-names.md](../framework-reserved-names.md) §1 的 log 分類。

> §1–§2 的 SAP / Odoo 研究可獨立作為設計依據；§4 之後為 Bee.NET 落地共用設計，各工作項 plan 會回引本文件對應章節。

---

## 背景

前一輪盤點確認：框架的 `log` 邏輯資料庫分類**目前是空的**——`DbCategorySettings` 有 `<DbCategory Id="log"><Tables /></DbCategory>`，但沒有任何 `st_*` 表（見 [`src/Bee.Definition/Defaults/DbCategorySettings.xml`](../../src/Bee.Definition/Defaults/DbCategorySettings.xml)）。[framework-reserved-names.md](../framework-reserved-names.md) §1 也只列了 common（6 表）與 company（5 表）。

同時框架**已有的**是「診斷用」日誌，而非「業務資料軌跡」：

- `ILogWriter` / `LogEntry` / `ConsoleLogWriter` / `NullLogWriter`——系統診斷輸出（terminology §日誌）。
- `TraceContext` / `Tracer` / `ITraceListener` / `ITraceWriter`——請求層級追蹤，**記憶體/UI 導向，未持久化到 DB**（[`src/Bee.Base/Tracing/`](../../src/Bee.Base/Tracing/)）。
- `DbScope.Log` enum + `IRepositoryDatabaseRouter`——已備好「路由寫入到 log DB」的機制（[`src/Bee.Repository/RepositoryDatabaseRouter.cs`](../../src/Bee.Repository/RepositoryDatabaseRouter.cs)）。

**缺口**：沒有持久化的業務資料軌跡（誰在何時登入、看了哪筆敏感資料、把哪個欄位從什麼改成什麼、執行了哪個動作、成功或失敗）。本 plan 目標即補上此缺口，並讓它落在既有的 log 分類。

**目標**

1. 定義一套「通用 ERP 日誌分類」，對齊 SAP / Odoo 的成熟做法（§1–§2）。
2. 在 log 分類下設計對應的 `st_log_*` 框架表（§4）。
3. 利用框架既有的單一收斂點做低侵入掛勾（§5）。
4. 處理欄位級 before/after、量體/保留/分區、不可竄改等關鍵議題（§6–§7）。
5. 分階段交付（§8），先落高槓桿的「執行 + 登入」，再逐步補「異動 / 檢視 / 進階」。

**非目標**：不改寫既有 `ILogWriter` / `Tracer` 診斷管線（兩者互補，見 §5.5）；不做即時 SIEM 串接（僅預留匯出點）。

---

## 1. SAP 的日誌 / 稽核分類（藍本）

SAP 沒有單一 audit log，而是依用途拆成多個獨立機制：

| 機制 | 交易碼 / 表 | 記錄什麼 | 對應軸線 |
|------|-------------|----------|----------|
| **Security Audit Log（SAL）** | `SM19`/`SM20`→`RSAU_CONFIG`/`RSAU_READ_LOG`；預設寫**本機稽核檔**非 DB | 成功/失敗登入、交易/報表啟動、RFC、使用者主檔異動、授權失敗 | ① 登入 / ⑥ 安全 |
| **Change Documents** | `CDHDR`（表頭）+ `CDPOS`（欄位明細）；`SCDO` 定義 | **業務物件欄位級變更**（`VALUE_OLD` / `VALUE_NEW`、`CHNGIND` I/U/D） | ③ 異動 |
| **System Log** | `SM21` | 系統層錯誤/警告/dump/rollback | ⑤ 系統 |
| **Table Logging** | `DBTABLOG`；需 `rec/client` + 表 `SE13` 勾選；分析 `SCU3` | **表級**異動（多用於 config 表，含 before/after） | ③ 異動 / ⑥ 組態 |
| **Read Access Logging（RAL）** | `SRALMANAGER` / `SRALMONITOR` | **敏感資料被讀取**（GDPR）；以 log domain + purpose + channel **選擇性**記錄 | ② 檢視 |
| **Application Log** | `SLG1`；`BALHDR` / `BALDAT`；以 Object/Sub-object 分類 | 應用自訂處理訊息（批次/過帳流程 log） | ④ 執行 |
| **Job / Batch** | `SM37`；`TBTCO` / `TBTCP` / `TBTCS` | 背景作業排程與執行狀態、job log | ④ 執行 |
| **Workflow / STAD / SUIM** | `SWI1` / `STAD` / `SUIM` | 工作流流轉 / 交易統計 / 權限查詢 | ④ / ⑤ / ⑥ |

**兩個關鍵區分**：
- **Change Documents（業務物件級）vs Table Logging（資料庫表級）**——都是 before/after，但前者以「業務物件」為單位、後者以「表」為單位（多用於低頻 config 表；對交易主表開啟會嚴重拖慢並灌爆 `DBTABLOG`）。
- **SAL 記「事件發生」，Change Documents 記「欄位改了什麼」**——登入稽核與異動稽核是兩套表、兩種量體特性。

---

## 2. Odoo 的日誌 / 稽核分類（藍本）

Odoo 較輕量，以 ORM mixin 為主，正式 CRUD 稽核靠 OCA 模組補足：

| 機制 | Model / Module | 記錄什麼 | 對應軸線 |
|------|----------------|----------|----------|
| **Chatter / 欄位追蹤** | `mail.thread`；`mail.message`；**`mail.tracking.value`** | `tracking=True` 欄位變更 → chatter 可讀時間軸；**依型別分欄**存舊/新值（`old/new_value_char`、`_integer`、`_float`、`_monetary`、`_datetime`） | ③ 異動 |
| **OCA `auditlog`** | `auditlog.rule` / `auditlog.log` / `auditlog.log.line` | 對指定 model 的 create/write/unlink（可選 **read**）；欄位級 before/after | ③ 異動 / ② 檢視 |
| **`ir.logging`** | `ir.logging`（`level`、`message`、`path`、`func`、`line`） | 技術/伺服器層 log、server action `log()` | ④ 執行 / ⑤ 系統 |
| **登入記錄** | `res.users.login_date`、**`res.users.log`** | 最後登入、線上狀態、連線；登入失敗多進 server log | ① 登入 |
| **Automated / Server Actions** | `base.automation` / `ir.actions.server` | 觸發式自訂寫 log / 發訊息 | ④ 執行 / 客製稽核 |

**設計啟示**：
- `auditlog` 有 **Fast log（不逐欄 diff，省）vs Full log（逐欄 + 可含 read，貴）** 兩檔位——直接對應本 plan 要提供的「輕量 / 完整」兩模式。
- Odoo 官方明示 **read 全記成本高**，預設不建議對整個 model 開——與 SAP RAL「只針對敏感欄位選擇性記」殊途同歸。
- `mail.tracking.value` **依型別分欄**存值（型別正確、可排序/國際化）；SAP `CDPOS` 用**單一字串欄**（通用、簡單但型別資訊遺失）——兩種取捨見 §6.1。

---

## 3. 綜合分類藍圖（六軸）

| 軸線 | SAP | Odoo | 關鍵欄位（who/when/what/where/before-after/result） | 量體 / 保留 |
|------|-----|------|--------------------------------------------------|-------------|
| **① 登入** | SAL | `res.users.log` | who=user, when, where=IP/session, result=成功/失敗/鎖定 | 中；失敗登入留久 |
| **② 檢視** | RAL（選擇性） | auditlog read 模式 | who, when, what=筆/欄, where=入口, result=讀取；**無 before/after** | **最大痛點**：讀 ≫ 寫，必須 opt-in + 敏感度驅動 |
| **③ 異動** | CDHDR/CDPOS、DBTABLOG | tracking.value、auditlog | who, when, what=物件+key+欄, where=method, **before/after**, result=I/U/D | 交易表全記量體大；one-row-per-field；依年分區 |
| **④ 執行** | SM37、SLG1、STAD | ir.logging、automation | who, when=起訖, what=動作+參數, result=成功/失敗/訊息, 耗時 | 批次/呼叫量大；設層級門檻 |
| **⑤ 系統** | SM21 | ir.logging(ERROR) | when, what=元件, result=錯誤碼/訊息 | 高頻短保留；與業務稽核分離 |
| **⑥ 安全/組態** | DBTABLOG on config、SAL、SUIM | auditlog on ACL/groups | who, when, what=設定/權限, before/after | 低頻高敏感、長保留、需不可竄改 |

**共通最小欄位模型**：`who`（user）/ `when`（UTC）/ `what`（物件+key+欄位 或 動作名）/ `where`（method/channel/IP/session）/ `before-after`（僅異動）/ `result`（成功失敗+訊息+影響筆數）。

---

## 4. Bee.NET 資料表設計（log 分類）

### 4.1 命名與歸屬

- 前綴用 `st_log_*`：`st_` 表示框架所有（對齊 [framework-reserved-names.md](../framework-reserved-names.md) §1「`st_` 正交於 DB 位置」），`log_` 讓所有日誌表在 log 分類下視覺成群。
- 全部歸 `DbCategory Id="log"`，透過 `DbScope.Log` → `RepositoryDatabaseRouter.Resolve` 落到固定 `databaseId = "log"`（可再按年份分片，見 §7）。
- 需在三處 `DbCategorySettings.xml` 的 `<DbCategory Id="log">` 註冊表清單：Defaults、tests/Define、（若 app 需要）apps/Bee.Northwind/Define。TableSchema 檔放 `Define/TableSchema/log/`（目前無此資料夾，需新建）。
- **日誌獨立性（設計鐵則）**：log 表**自足**，查詢**不 join 其他表**。log DB 與 common/company 是**實體分離**的資料庫，跨 DB join 根本不可行；故 who / company 等一律存**去正規化的顯示值**（`user_name` / `company_name`），而非只存需要 join 才有意義的 rowid。widith 換來查詢獨立與可長期封存。

### 4.2 共通欄位（每張日誌表都有）

| 欄位 | 型別 | 說明 |
|------|------|------|
| `sys_no` | Long/Identity | 序號（寫入順序，兼作 hash-chain 排序鍵） |
| `sys_rowid` | Guid | 主鍵 |
| `log_time` | DateTime(UTC) | **事件發生時刻**（非寫入時刻；非同步下兩者可能不同） |
| `user_id` | String, null | 觸發者帳號（登入 id，**自足**；對齊 SAP `USERNAME`） |
| `user_name` | String, null | 觸發者顯示名稱（**去正規化**，免 join common `st_user`） |
| `company_id` | String, null | 租戶代碼（自足；登入前事件為 null） |
| `company_name` | String, null | 公司顯示名稱（**去正規化**，免跨 DB join common `st_company`） |
| `access_token` | Guid, null | Session 關聯 |
| `trace_id` | Guid, null | **同一次 API 呼叫的關聯 id**（串起 exec ↔ change ↔ access，見 §5.4） |
| `client_ip` | String, null | 來源 IP |
| `source` | String, null | 來源標記：`"ProgId.Action"` / channel / tcode 對應 |

> 沿用框架 `sys_rowid` / `sys_no` 主鍵慣例（[`src/Bee.Definition/SysFields.cs`](../../src/Bee.Definition/SysFields.cs)）；日誌專屬欄用 `log_` / 語意命名，不硬套 `sys_insert_time`（事件時刻語意不同）。**不存 `user_rowid`**：純 rowid 需 join 才有意義，違反日誌獨立性——改存 `user_id` + `user_name`。

### 4.3 各表 schema

**① `st_log_login`（登入記錄）** — 軸①（**已實作**，見 [plan-audit-1-login.md](plan-audit-1-login.md)）
- 共通欄 +
- `event`（Integer/enum `LoginEvent`）：`LoginSucceeded` / `LoginFailed` / `LockedOut` / `Logout`——**列舉自帶結果，不另設 result 欄**
- `fail_reason`（String, null）：密碼錯誤 / 帳號鎖定 …（**不含明文密碼**，見 security.md）

**② `st_log_exec`（執行記錄）** — 軸④（API 呼叫層）
- 共通欄 +
- `method`（String）：`"ProgId.Action"`
- `payload_format`（Integer）：Plain / Encoded / Encrypted
- `is_local`（Boolean）：本機呼叫 vs 遠端
- `result`（Integer）：Ok / Error
- `error_code`（String, null）+ `error_message`（String, null）：**已消毒**（禁 stack trace / 內部路徑，見 scanning.md §敏感資訊外洩）
- `elapsed_ms`（Integer）：耗時（來自 `TraceContext.Stopwatch`）

**③ 異動記錄** — 軸③（**預設：DataSet DiffGram 單表**）
- **`st_log_change`（單表，一次 Save = 一列）**：共通欄 + `prog_id`（業務物件）+ `table_name`（主表）+ `row_key`（master `sys_rowid`）+ `change_kind`（Integer：Insert/Update/Delete）+ `is_sensitive`（Boolean，軸⑥用）+ **`changes_xml`（Text/CLOB）**
  - `changes_xml` = `dataSet.GetChanges()` 以 **DiffGram** 序列化（`WriteXml(XmlWriteMode.DiffGram)` 或框架保留 original 的等效路徑）。一個 blob 完整承載 master+detail、多列、多欄、新舊值。顯示時 XML→DataSet 還原、重用既有 UI 呈現新舊值。
  - **擷取時序**：於 `DataFormRepository.Save` 進入點先 `GetChanges()`（副本），套用後才寫 log——ADO.NET DataAdapter 更新成功會 `AcceptChanges` 沖掉 RowState/Original。
  - who/when/prog_id/table/**row_key**/change_kind 為實體欄位（可查、可索引）；欄位級差異在 XML 內（換取「一次全記、零自訂 diff、可還原顯示」）。
- **選配「可查詢模式」`st_log_change_field`（EAV，one row per changed field）**：`sys_rowid` + `sys_master_rowid`（→ 表頭）+ `field_name` + `field_type` + `old_value` + `new_value`。**僅在需要欄位級 SQL 報表/統計時**，對指定表額外攤開（對齊 SAP `CDPOS` / Odoo `mail.tracking.value`）。預設不開。

**④ `st_log_access`（檢視記錄，opt-in）** — 軸②（**已實作**，見 [plan-audit-3-access.md](plan-audit-3-access.md)）
- 共通欄 + `prog_id` + `row_key`——**record-level，不記欄位**（一次 `GetData` 明細檢視＝一列：誰看了哪筆）。
- **預設關閉**（`AccessEnabled`）；只掛 `GetData`，`GetList` 不掛。目前全記所有表單，未來加 per-form 稽核規則（見頂部索引註記）。

**（軸⑤ 系統 / 軸⑥ 安全）先不建獨立表**：
- ⑤ 系統/錯誤：Phase 4 再評估把 `ITraceWriter` 導一份到 `st_log_trace`；目前 `error_message` 已進 `st_log_exec`。
- ⑥ 安全/組態：本質是對 `st_role` / `st_role_grant` / `st_user_role` / `st_user` 的異動，**由 `st_log_change` 自然涵蓋**；只需在表頭加 `is_sensitive` 旗標或用 `prog_id` 過濾即可，暫不獨立建表（待決策 D3）。

---

## 5. 掛勾接點（低侵入，對齊既有收斂點）

| 日誌 | 掛勾位置 | 檔案 | 為何理想 |
|------|----------|------|----------|
| **執行** | `JsonRpcExecutor.ExecuteAsyncCore()`（`Tracer.Start`/`End` 之間） | [`src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs`](../../src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs) | **所有 Remote+Local API 的唯一收斂點**，try-finally 已內建成功/失敗與耗時 |
| **登入** | `SystemBusinessObject.Login()`（`AuthenticateUser` / `LoginTracker` 成敗分支）+ `SessionRepository.Insert()` | [`src/Bee.Business/System/SystemBusinessObject.cs`](../../src/Bee.Business/System/SystemBusinessObject.cs) | 成功/失敗/鎖定判定已集中，直接對映 `event` |
| **異動** | `DataFormRepository.Save()`（DataRow `RowState` + `Original`/`Current`）→ 於 `DbAccess.UpdateDataTables()` 交易內擷取 | [`src/Bee.Repository/Form/DataFormRepository.cs`](../../src/Bee.Repository/Form/DataFormRepository.cs)、[`src/Bee.Db/DbAccess.cs`](../../src/Bee.Db/DbAccess.cs) | ADO.NET `DataRowVersion.Original`/`Current` 即 before/after 天然來源 |
| **檢視** | 讀取路徑 + 敏感欄位標記過濾（opt-in） | Repository 讀取 / FormBO GetList | 需敏感度驅動，避免熱路徑爆量 |
| **落表路由** | `RepositoryDatabaseRouter.Resolve(DbScope.Log, token)` → `"log"` | [`src/Bee.Repository/RepositoryDatabaseRouter.cs`](../../src/Bee.Repository/RepositoryDatabaseRouter.cs) | 固定路由，已存在 |

### 5.4 關聯 id（trace_id）
一次 API 呼叫可能產生 1 筆 exec-log + N 筆 change-log + M 筆 access-log。在 `JsonRpcExecutor` 進入點產生一個 `trace_id`（或重用 `TraceContext`），透過 `SessionInfo`/ambient context 傳遞，讓所有子日誌共用同一 `trace_id`，便於「以一次操作為單位」回溯。

### 5.5 與既有診斷日誌的關係
- `ILogWriter` / `Tracer`：**技術診斷**（console、trace、效能），不改。
- 本 plan 的 `IAuditLogWriter`：**業務資料軌跡**（合規、可回溯、落 log DB）。
- 兩者可共用 `TraceContext`（耗時、trace_id 來源），但寫入目標與生命週期分離。

---

## 6. 欄位級 before/after 與檢視量體

### 6.1 before/after 儲存法
| 方案 | 對照 | 優點 | 缺點 |
|------|------|------|------|
| **★ D. DataSet DiffGram 單欄（採用預設）** | 框架原生 DataSet | 一次全記 master+detail/多列/多欄/新舊值、零自訂 diff、XML→DataSet 還原直接顯示、dogfood | 欄位級無法直接 SQL 查/統計（需解析 XML） |
| A. text + type tag | SAP `CDPOS` | 簡單、schema 窄 | 型別資訊靠 tag、排序/比較弱 |
| B. typed 分欄 | Odoo `mail.tracking.value` | 型別正確、可排序/國際化 | schema 寬、取值需分型別 |
| C. JSON diff 單欄 | auditlog fast log | 寫入快、彈性 | 同 D 的查詢弱點，且非框架原生 |

**採用（D，2026-07-05）**：異動 log 預設用 **DataSet DiffGram 單欄**（`changes_xml`）——框架 DataSet 原生 `GetChanges()` + DiffGram 已同時保留新舊值（[`DataTableJsonConverter`](../../src/Bee.Base/Serialization/DataTableJsonConverter.cs) 亦證實 wire round-trip 保留 `original`/`current`）。查詢需求靠**表頭實體欄位**（who/when/prog_id/row_key…）滿足；只有「跨紀錄的欄位級查詢/統計」需要時，才對指定表加開**選配 EAV 模式**（方案 A/`st_log_change_field`）當 full 檔位。等同 fast（XML，預設）/ full（EAV，選配）兩檔位，對齊 Odoo auditlog。

> **鐵則**：序列化須用 **DiffGram**（含 before 區塊）；普通 `WriteXml` 只寫 current、舊值遺失。擷取須在 `Save` 套用（AcceptChanges）**之前**。

### 6.2 檢視記錄量體取捨（軸②的核心難題）
讀 ≫ 寫，全記必爆量並拖慢查詢。採 SAP RAL / Odoo 的共識做法：
1. **預設關閉**，opt-in 啟用。
2. **敏感度驅動**：只對 `DbField` / `FormField` 標記為敏感（新增 `IsSensitive` 或 log-domain 標記）的欄位記錄。
3. **限定入口**：只在指定 ProgId / 動作記，不是每次 ORM 讀取。
4. 合規場景通常要求「敏感資料全記」，故取樣只用於行為分析、不用於合規舉證。

---

## 7. 寫入架構、保留、分區、不可竄改

### 7.1 寫入架構
> **註（項 2 定案更新）**：下方 transactional outbox 為原設計；異動記錄實作時重評為 **best-effort 非同步**（見 D2 / [plan-audit-2-change.md](plan-audit-2-change.md) §3）。outbox 保留為「零漏失需求」時的升級路徑。
- 新增 `IAuditLogWriter` 抽象 + 預設實作（落 log DB via `DbAccess(DbScope.Log)`）。
- **非同步 + 批次**：exec / login / access 走背景 channel/queue 批次 INSERT，避免阻塞主業務流程。
- **交易邊界 → 採 transactional outbox（已定 D2）**：change-log 的一致性與業務資料強相關，故：
  - **業務交易內**先寫入 outbox 列（與業務資料同一交易 commit → 強一致，且因與業務資料同庫故無跨 DB 交易問題）。
  - **背景 flush**：獨立 worker 把 outbox 列搬到 log DB（`DbScope.Log`）後標記/刪除，達成與 log DB 的解耦與批次。
  - 兼顧「不漏記」（同交易保證）與「不阻塞、可分庫」（背景搬運），避開同交易跨庫（分散式交易）與 best-effort 遺失窗口兩者的缺點。
  - outbox 表本身歸屬：放業務資料所在 scope（company/common）以確保同交易；欄位含 flush 狀態旗標與重試計數。

### 7.2 保留與分區
- 每個日誌類別**可獨立設定保留期**（登入失敗、財務異動、敏感讀取的合規要求不同：SOX 常 7 年、GDPR 依最短必要）。
- **依年分庫（解決無限成長，未來設計）**：`log` 分類支援一 CategoryId 對多 DatabaseItem（`log_2024` / `log_2025` / `log_2026`，見 [database-settings-guide.md](../database-settings-guide.md) 情境 4）。
  - **當年度 log DB 可讀可寫**；**歷史年度 log DB 唯讀（只供查詢）**——每顆實體 DB 因此有界，不會無限成長。歷史唯讀由 DB 權限層強制（亦強化 §7.3 append-only / 不可竄改）。
  - **寫入目標解析（未來 seam）**：目前 sink 寫固定 `DbCategoryIds.Log`（"log"），為「單一 log DB」退化情形。分年落地時，把「當年可寫 log DB」抽成小解析器（預設回 `"log"`；分年版回 `"log_{year}"`），sink 改用之即可——**entry 模型與表 schema 不變、向前相容**。
  - **查詢跨年**：歷史查詢由應用/報表層聚合多個 `log_YYYY` DatabaseItem（讀取端，不影響 writer）。此正是 §4 日誌獨立性（每列自足、不 join）的另一理由——跨年、跨庫聚合只能靠自足的列。
  - 或用 DB 原生 partition by `log_time`；清理以 drop partition / detach 舊庫為單位（遠快於 delete）。
- 清理/歸檔做成可排程作業（對齊 SAP SARA 歸檔概念）。

### 7.3 不可竄改（tamper-evidence）
- **Append-only**：log 表僅允許 INSERT；DB 權限層禁 UPDATE/DELETE。
- **職責分離**：操作業務資料的帳號 ≠ 管理稽核記錄的帳號；稽核員唯讀。
- **hash-chain（Phase 4，選配）**：每筆存 `entry_hash = hash(prev_hash + 本筆內容)`，斷鏈即可偵測竄改；區段 hash 可定期簽章/外送 WORM。
- 清理只能透過「歸檔」流程，不直接 delete。

---

## 8. 交付與排程

交付以本文件頂部的**工作項拆解表**為準（項 0–5，逐項獨立 plan、逐項定案再執行）。共用收尾要求：

- 每張 `st_log_*` 表補對應單元測試（TableSchema round-trip + 寫入路由 `[DbFact]`）。
- 全部完成後回頭更新 [framework-reserved-names.md](../framework-reserved-names.md) §1，補上 **log 分類**表清單（呼應「log 目前為空」的缺口）。
- 分區/保留排程作業、hash-chain 不可竄改（§7.2–§7.3）歸屬進階，於各表落地後再排。

---

## 9. 決策紀錄

**已定（2026-07-05）**

- **D1 命名 → `st_log_*`**：`st_log_login` / `st_log_exec` / `st_log_change` / `st_log_change_field` / `st_log_access`（log 表在 log 分類下成群）。
- **D2 異動 log 交易邊界 → ~~transactional outbox~~ 改採 best-effort 非同步**（項 2 定案時重評）：落到實作，outbox 需 per-company-DB outbox 表 + 多租戶跨庫 flush + repository 簽章改動，負擔過重；改由 BO 於 commit 後走項 0 `IAuditLogWriter`（可對 change 強制同步寫縮小漏失窗口）。零漏失需求出現時再升級 outbox（additive）。詳見 [plan-audit-2-change.md](plan-audit-2-change.md) §3。
- **D3 安全/組態（軸⑥）→ 併入 `st_log_change`**：以 `is_sensitive` 旗標 / `prog_id` 過濾區分，不另建表（§4.3）。
- **D4 執行模式 → 逐項獨立 plan、定案再執行**（見頂部拆解表）。項 0 基礎設施已完成；現行進度以拆解表狀態為準。

**仍待決（實作啟動前再定）**

- **D5 before/after 儲存**：text + type tag（推薦起手，對齊 SAP `CDPOS`）/ typed 分欄（Odoo `mail.tracking.value`）/ JSON diff（auditlog fast log）。見 §6.1。
- **D6 SAP/Odoo 研究落點**：§1–§2 是否另抽一份 `docs/` 參考（ADR 或 `docs/audit-trail-design.md`），或僅留本 plan。

---

## 參考

- 掛勾接點盤點：`JsonRpcExecutor` / `ApiAccessValidator` / `SystemBusinessObject.Login` / `DataFormRepository.Save` / `DbAccess.UpdateDataTables` / `RepositoryDatabaseRouter`（見 §5）。
- 部署與分片：[database-settings-guide.md](../database-settings-guide.md)（common/company/log 四種部署情境）。
- 系統表登記：[framework-reserved-names.md](../framework-reserved-names.md)（`st_*` 命名與所屬 DB）。
- 相關 ADR：[adr-017 DB 快取失效](../adr/adr-017-db-cache-invalidation.md)、[adr-018 DB 定義儲存](../adr/adr-018-db-define-storage.md)、[adr-019 權限授權模型](../adr/adr-019-permission-authorization-model.md)。
