# 計畫：深分頁成本與跨引擎壓測的隔離

**狀態：🚧 進行中（2026-09-08）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| A | Oracle 的 FormSchema 請求全數失敗（ORA-00932） | ✅ 已完成（2026-09-08） |
| B | 深分頁（`OFFSET`）成本 —— 是否處理 | 📝 待決策 |
| C | Oracle 的深分頁數據 —— 需先備妥專用 schema | 📝 待做 |
| D | 壓測工具：VU pool 的登入失敗會歸錯場景 | 📝 待做 |

## 背景

`tools/Bee.LoadTests` 建立後首次跨引擎跑同一組場景，量出兩件先前沒有數據的事，並在過程中
暴露出工具本身的兩個缺陷。量測條件：Local、Encrypted、20 VU、warm-up 30s、量測 120s、
`ft_customer` 100,000 筆、`pageSize` 50，淺頁自第 1 頁起、深頁自第 1,900 頁起
（`OFFSET 94,950`），各走 10 頁。

## 階段 A：Oracle 的 FormSchema 請求全數失敗（已修）

修正於 commit `c53bc0e3`。**根因與外顯完全不同，值得記下來。**

外顯是「`GetList` 100% 失敗，ORA-00932」，但錯不在 `GetList` 的 SELECT：

1. **`Oracle.ManagedDataAccess` 的 `BindByName` 預設為 `false`**，於是 SQL 裡第 n 個 bind
   變數拿到參數集合的第 n 筆，與名稱無關。其餘四家皆以名稱綁定，而 `{0}` / `{Name}` 佔位符
   API 的語意本來就是名稱對應。登入路徑上 `SessionRepository.UpdateSession` 的佔位符順序是
   `{1} {2} {0}`，於是 `DateTime` 被送進 `access_token`（`RAW(16)`）——**登入先炸**。
2. Oracle 沒有 UUID 型別，`FieldDbType.Guid` 對映 `RAW(16)`，讀回是 `byte[]`。

修法是在 `DbCommandSpec.CreateCommand` 對 Oracle 的 text command 設 `BindByName`（以反射，
因為 `Bee.Db` 不參考任何 ADO.NET driver）。逐句改寫 SQL 遷就位置綁定不是解：那要求每個寫
SQL 的人記住一條沒有機制檢查的規則，而型別相容時的錯位根本不會報錯。

### 這次的教訓

**壓測報告指出的場景，未必是壞掉的地方。** 報告寫著 `GetList` 失敗，真因在登入——因為
VU pool 快取了 faulted task（見階段 D）。診斷時應先確認前置步驟是否成立，而不是從報告
指名的場景開始查。

另外，`IsDebugMode=false` 會把 ORA 代碼收斂成泛用的 `Internal server error`，
沒有開 debug 就完全看不到線索。

## 階段 B：深分頁的 `OFFSET` 成本

### 數據（兩輪獨立執行）

| Provider | 淺頁 p50 | 深頁 p50 | 倍數 | 首輪倍數 | 深頁 p99 |
|---|---:|---:|---:|---:|---:|
| SQL Server | 1.2 | 18.5 | 15.4× | 14.5× | 51.1 |
| PostgreSQL | 0.68 | 11.16 | 16.4× | 15.6× | 26.0 |
| MySQL | 1.17 | 22.39 | 19.1× | 20.1× | 43.2 |
| Oracle | — | — | — | — | 見階段 C |

兩輪倍數相差都在 1 以內，**結論穩固**：三家皆重現，沒有哪一家的 dialect 實作躲得掉。

這是 `OFFSET` 分頁的固有行為，不是框架缺陷：引擎必須走過並丟棄偏移量之前的所有列，成本隨
偏移量成長，而第一頁的查詢完全感覺不到——這也是為什麼資料量從 1,000 筆拉到 100,000 筆時，
只取前 10 頁的場景數字紋風不動。

### 決策點：值不值得處理

**先回答這個，再談怎麼做。** 判準是實務上使用者會不會深翻：

- 若 ERP 畫面都有篩選、結果集通常在數十頁內 → **不必處理**，記錄為已知特性即可。
- 若存在「全表瀏覽」「匯出前翻到底」這類用法 → 值得處理。

框架端答不了，取決於實際部署的使用型態。

### 若要處理，三個選項

| 選項 | 做法 | 代價 |
|------|------|------|
| **1. 不處理** | 記進 `docs/repo-ops/gotchas/database.md`，讓下次有人問時查得到這組數字 | 深翻的畫面就是慢；成本為零 |
| **2. Keyset pagination** | 以上一頁最後一筆的鍵當游標（`WHERE key > @last`），成本與偏移量無關 | 動到 [`PagingOptions`](../../src/Bee.Definition/Paging/PagingOptions.cs)、wire contract 與所有 UI head；且**不支援跳頁**，是產品行為的改變 |
| **3. 混合** | 淺頁維持 `OFFSET`，超過門檻改走 keyset | 兩套路徑要維護，門檻是另一個要調的參數 |

選項 2 的「不支援跳頁」是關鍵取捨：keyset 換得的效能，代價是使用者不能直接跳到第 500 頁。
若現行 UI 有頁碼跳轉，這是產品決策而非技術決策。

### 若採選項 1，仍建議做的事

`PagingOptions` 的 XML doc 目前只提醒「未分頁會載入全部」，沒有提醒「深分頁的成本隨偏移量
成長」。那是讓呼叫端做出正確設計選擇的資訊。

## 階段 C：Oracle 的深分頁數據

尚未取得，因為 Oracle 無法與測試套件共用同一個 schema 而不互相破壞。

### 已修正的隔離漏洞

壓測以 `database.databaseNamePrefix`（預設 `loadtest_`）隔離，做法是替換連線字串裡的
`{@DbName}`。**Oracle 的連線字串指的是服務而非資料庫，沒有那個佔位符**，於是前綴無從施力，
每個 category 都解析到連線字串已經指向的地方——也就是單元測試自己的 schema。

實測後果：壓測建的 25 張表與 100,000 筆 `ft_customer` 全部落在測試用的 `testuser` schema，
而測試套件清掉了壓測植入的帳號，兩邊互相踩。

已修正：連線字串若不含 `{@DbName}` 則直接拒絕執行，並指示改設
`BEE_LOADTEST_CONNSTR_{DBTYPE}` 指向專用 schema；該變數存在時優先採用且不做此檢查
（等於操作者明講「這個歸壓測寫」）。

> **先前宣稱「隔離已驗證」是不成立的**：當時只查了 SQL Server 的 `sys.databases`，
> 沒有驗證 Oracle 這條路徑。而 Oracle 在框架裡本就是單一 schema 模式，
> category 級隔離對它不存在——`SharedDatabaseState` 早已處理過這件事（對 Oracle 用空字串
> categoryId），當時沒有跟著想。

### 待辦

1. 備妥一個保留給壓測的 Oracle schema（需要 `CREATE USER` 權限）。
2. 以 `BEE_LOADTEST_CONNSTR_ORACLE` 指向它，重跑淺／深分頁對照補齊上表。
3. **清理已污染的 `testuser` schema** —— 25 張表與 100,000 筆資料仍在裡面。清理前需要先
   分辨哪些表是壓測建的、哪些是測試本來就需要的。

## 階段 D：VU pool 的登入失敗會歸錯場景

`VirtualUserPool` 以 `Lazy<Task<VirtualUser>>` 快取每個 VU 的登入結果。登入失敗時 faulted
task 被快取，之後每個 iteration 立即重擲同一個例外——於是報告顯示「`GetList` 失敗
2,100 萬次」，而真相是登入失敗一次。

兩個問題：

- **歸因錯誤**：錯誤記在使用 pool 的場景上，而不是登入。
- **次數失真**：失敗極快，錯誤計數暴增到與工作量無關的數量級。

可能的處理方向（未定案）：登入失敗時讓整個 run 快速中止並明確報告是前置步驟失敗，
而不是讓它變成每個場景的錯誤計數。

## 不在本 plan 範圍

- **跨 provider 的效能比較**：本次數據顯示 PostgreSQL 在這台機器上吞吐約為 SQL Server 的
  兩倍，但那是單機、單次、開發環境的觀察，**不足以支持任何選型結論**。

## 附註：數據的侷限

上表是**當時在該台機器量到的**，不是框架的效能規格：開發用 macOS、client 與 server 同機、
封閉模型（送出速率隨系統變慢而下降，因此不顯示飽和點）、資料量 100,000 筆（對這四家都仍屬
小表，皆可完全載入記憶體）。跨 provider 的絕對值受各容器設定影響，**倍數關係才是本 plan
引用的部分**。
