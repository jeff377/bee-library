# 計畫：深分頁成本與跨引擎壓測的隔離

**狀態：✅ 已完成（2026-09-08）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| A | Oracle 的 FormSchema 請求全數失敗（ORA-00932） | ✅ 已完成（2026-09-08） |
| B | 深分頁（`OFFSET`）成本 —— 是否處理 | ✅ 已完成（2026-09-08，採選項 1） |
| C | Oracle 的深分頁數據 —— 需先備妥專用 schema | ✅ 已完成（2026-09-08） |
| D | 壓測工具：VU pool 的登入失敗會歸錯場景 | ✅ 已完成（2026-09-08） |

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

### 數據（前三家各兩輪、Oracle 三輪，皆獨立執行）

| Provider | 淺頁 p50 | 深頁 p50 | 倍數 | 首輪倍數 | 深頁 p99 |
|---|---:|---:|---:|---:|---:|
| SQL Server | 1.2 | 18.5 | 15.4× | 14.5× | 51.1 |
| PostgreSQL | 0.68 | 11.16 | 16.4× | 15.6× | 26.0 |
| MySQL | 1.17 | 22.39 | 19.1× | 20.1× | 43.2 |
| Oracle | 0.7 | 19.3 ~ 24.6 | 27.6 ~ 35.1× | 三輪，見下 | ⚠ 非分頁成本，見下 |

前三家兩輪倍數相差都在 1 以內，**結論穩固**：四家皆重現，沒有哪一家的 dialect 實作躲得掉。

**Oracle 那列讀法不同，兩點要一起看：**

- **倍數更高但更不穩**：三輪深頁 p50 為 19.3 / 23.3 / 24.6ms，倍數 27.6× / 33.3× / 35.1×
  （淺頁 p50 三輪都是 0.7ms）。逐輪遞增而非上下震盪，前三家沒有這個現象。
- **深頁 p99 那欄不是 `OFFSET` 成本**：Oracle 兩個場景**都**帶著約 0.8～1.9s 的 p99 與
  約 2.1～2.4s 的 max，而**淺頁也一樣**——淺頁 p99 三輪為 1,257 / 1,731 / 805ms。
  只掃 50 列的第一頁不可能因偏移量而慢，所以那條尾巴是這個容器的週期性停頓，
  與分頁無關，**不要拿它跟前三家的深頁 p99 對比**。成因未查明。

因此 Oracle 這列**只有 p50 與倍數可以跟前三家並排讀**。吞吐（約 155～161 rps，
前三家 755 上下）同樣不可比——見文末「數據的侷限」。三輪錯誤數皆為 0。

這是 `OFFSET` 分頁的固有行為，不是框架缺陷：引擎必須走過並丟棄偏移量之前的所有列，成本隨
偏移量成長，而第一頁的查詢完全感覺不到——這也是為什麼資料量從 1,000 筆拉到 100,000 筆時，
只取前 10 頁的場景數字紋風不動。

### 決策：採選項 1（不處理，記錄為已知特性）

**2026-09-08 由使用者拍板。** 判準是實務上使用者會不會深翻：ERP 畫面都有篩選、
結果集通常在數十頁內，因此深分頁的成本不值得用產品行為（跳頁）去換。

落地兩件事，皆已完成：

- 這組數字與其成因記進
  [`docs/repo-ops/gotchas/database.md`](../../repo-ops/gotchas/database.md)，
  下次有人問「為什麼翻到後面變慢」時查得到。
- [`PagingOptions.Page`](../../../src/Bee.Definition/Paging/PagingOptions.cs) 的 XML doc
  補上「成本隨頁碼成長」，讓呼叫端在 IntelliSense 就看得到，而不必先量過才知道。

**這個決定是可逆的**：選項 2 / 3 日後仍可實作，本次沒有做任何會擋住它們的事。

### 當時評估過的三個選項（保留，供日後翻案時參考）

| 選項 | 做法 | 代價 |
|------|------|------|
| **1. 不處理** | 記進 `docs/repo-ops/gotchas/database.md`，讓下次有人問時查得到這組數字 | 深翻的畫面就是慢；成本為零 |
| **2. Keyset pagination** | 以上一頁最後一筆的鍵當游標（`WHERE key > @last`），成本與偏移量無關 | 動到 [`PagingOptions`](../../../src/Bee.Definition/Paging/PagingOptions.cs)、wire contract 與所有 UI head；且**不支援跳頁**，是產品行為的改變 |
| **3. 混合** | 淺頁維持 `OFFSET`，超過門檻改走 keyset | 兩套路徑要維護，門檻是另一個要調的參數 |

選項 2 的「不支援跳頁」是關鍵取捨：keyset 換得的效能，代價是使用者不能直接跳到第 500 頁。
若現行 UI 有頁碼跳轉，這是產品決策而非技術決策。


## 階段 C：Oracle 的深分頁數據（已完成）

數據已補進上表。取得的前提是先把 Oracle 從測試套件的 schema 裡分出來——在那之前
兩邊共用同一個 schema，必然互相破壞。

### 已修正的隔離漏洞

壓測以 `database.databaseNamePrefix`（預設 `loadtest_`）隔離，做法是替換連線字串裡的
`{@DbName}`。**Oracle 的連線字串指的是服務而非資料庫，沒有那個佔位符**，於是前綴無從施力，
每個 category 都解析到連線字串已經指向的地方——也就是單元測試自己的 schema。

實測後果：壓測建的 25 張表與 100,000 筆 `ft_customer` 全部落在測試用的 `testuser` schema。

> **更正**：先前寫「測試套件清掉了壓測植入的帳號」，這句是錯的。`tests/` 裡唯一會刪
> `st_user` 的是 `TestUsers.Delete`，它以完整 `sys_id` 逐列刪，刪不到 `loadtest_user_*`；
> `SharedDatabaseState` 全程沒有任何 `DELETE` / `TRUNCATE` / `DROP`。
>
> 真正刪掉它們的是**維護者手動執行 `gotchas/database.md` 記載的復原 SQL**——那段
> 正是逐條刪 `loadtest_user_%` 與 `loadtest` 公司列。證據是 `testuser.st_user.password`
> 目前為 `VARCHAR2(40)`：復原 SQL 的最後一行就是把它從壓測定義的 200 改回 40，
> 而壓測跑過之後它必然是 200。**是人照文件做的，不是測試套件的副作用。**

已修正：連線字串若不含 `{@DbName}` 則直接拒絕執行，並指示改設
`BEE_LOADTEST_CONNSTR_{DBTYPE}` 指向專用 schema；該變數存在時優先採用且不做此檢查
（等於操作者明講「這個歸壓測寫」）。

> **先前宣稱「隔離已驗證」是不成立的**：當時只查了 SQL Server 的 `sys.databases`，
> 沒有驗證 Oracle 這條路徑。而 Oracle 在框架裡本就是單一 schema 模式，
> category 級隔離對它不存在——`SharedDatabaseState` 早已處理過這件事（對 Oracle 用空字串
> categoryId），當時沒有跟著想。

### 專用 schema（已備妥）

Oracle 容器裡新開了 `loadtest` user，`BEE_LOADTEST_CONNSTR_ORACLE` 指向它。
`prepare` 之後 25 張表與 100,000 列都建在 `loadtest`，`testuser` 的 `ft_customer`
仍停在 10 列——隔離這次有驗證。建立步驟寫進了
[`docs/repo-ops/load-testing.md`](../../repo-ops/load-testing.md)，本檔不複寫。

### 污染盤點與清理（已完成）

清理前先盤點，結論是**沒有任何一張表是壓測獨有的、需要 DROP 的**：

| 來源 | 內容 | 處置 |
|------|------|------|
| 壓測植入 | `ft_customer` 99,990 列（`sys_id` 形如 `sys_id-<index>`，`DataSeeder` 產生） | 已刪 |
| 測試套件需要 | 其餘 25 張表與全部資料 | 保留 |
| 更早的改名殘留 | 空表 `ft_department` / `ft_employee` | 未動，見下 |

**表清單完全重疊，所以不能按表清。** 壓測用的 `apps/Bee.Northwind/Define` 註冊 25 張表，
測試用的 `tests/Define` 註冊 26 張（多一張 `ft_project`），前者是後者的子集。
`DataSeeder.EnsureRows` 只補足差額、從不刪列，所以它是在測試種子的 10 列之上補到 100,000，
**判準因此是列而不是表**：其餘每張表的列數都與 `tests/Bee.Tests.Shared/SeedData/*.json`
完全相符（`ft_category` 8、`ft_order` 5、`ft_order_detail` 12、`ft_product` 15…），
只有 `ft_customer` 是 100,000 而非 10。

刪除前確認過 `ft_order` 沒有任何一列指向要刪的客戶（0 列），刪後 `ft_customer` 回到 10 列。

**驗證**：清理前後各跑一次完整 `./test.sh`，兩次結果**逐專案完全相同**——
17 個測試專案全綠、6,113 通過、1 略過、0 失敗。也就是說這批髒資料當時並沒有讓任何測試變紅，
清掉是為了回到已知狀態，不是為了修紅燈。

### 順帶發現（未處理，不屬本 plan）

- **`ft_department` / `ft_employee` 兩張空表**是 commit `6e1340c1a` 把框架組織表改名為
  `st_department` / `st_employee` 之前留下的，現行定義與程式碼都不再引用它們。兩張都是 0 列。
  持久容器不會自己淘汰改名前的表，**這類殘留在四個容器裡可能都有**。
- **`st_user_company` 有一列孤兒**（`user_rowid` 指向 `001`，`company_rowid` 指向已不存在的
  公司列），`sys_insert_time` 為 2026-06-04，比壓測早三個月，與壓測無關。
- **`st_cache_notify` 單調成長**：目前 5,662 列，每跑一次完整測試約 +10 列，沒有任何回收機制。

## 階段 D：VU pool 的登入失敗會歸錯場景（已修）

`VirtualUserPool` 以 `Lazy<Task<VirtualUser>>` 快取每個 VU 的登入結果。登入失敗時 faulted
task 被快取，之後每個 iteration 立即重擲同一個例外——於是報告顯示「`GetList` 失敗
2,100 萬次」，而真相是登入失敗一次。

兩個問題：

- **歸因錯誤**：錯誤記在使用 pool 的場景上，而不是登入。
- **次數失真**：失敗極快，錯誤計數暴增到與工作量無關的數量級。

### 修法：把登入提前成明確的前置步驟

`VirtualUserPool.SignInAllAsync` 在任何場景執行**之前**把該輪要用的每個 VU 登入完畢，
由 `Program.RunAsync` 在印出 `Scenarios` 之後、`Running...` 之前呼叫。失敗時擲
`InvalidOperationException`，訊息點名**是哪個 VU、用哪個帳號、底層錯誤是什麼**，
`Main` 既有的 handler 印一行後以非零碼結束。

**兩個問題一起消失**，因為它們本來就是同一個成因的兩面：登入失敗不再有機會進入
`LoadRunner.WorkerAsync` 的 catch-all，所以既不會記到場景頭上，也不會被那個
「失敗極快 → 迴圈全速空轉」的迴圈放大。

觸發條件用**場景物件的型別**（`scenario is not LoginScenario`）而非名稱字串：日後新增
會用 pool 的場景自動被涵蓋，不必有人記得去補一份名單；判斷錯的代價也只是多登入一次。
`LoginScenario` 刻意不走 pool——登入正是它要量的東西，預先登入會把它要量的東西消掉。

### 驗證

- **失敗路徑**：故意給錯密碼跑 20 VU，**1 秒內中止**、exit code 1、
  訊息為「Virtual user 0 could not sign in as 'loadtest_user_0' … (UserMessageException:
  Invalid username or password.)」，且**不產出報告**——修正前這裡會跑滿整個量測窗、
  產出一份看起來完整但數字全錯的報告。
- **正常路徑**：同設定改回正確密碼，多印一行 `Sign-in : 4 virtual user(s) ready`，
  兩個場景零錯誤。
- **只跑 `Login` 的設定**：不觸發預先登入（無 `Sign-in` 那行），`Login` 場景照常量到
  499 次呼叫、零錯誤——確認沒有把它要量的東西提前掉。
- `Bee.LoadTests.UnitTests` 新增 5 支 `VirtualUserPoolTests`（120 → 125）。

### 未處理的殘餘

**「失敗極快 → 錯誤計數暴增」這個放大器本身還在。** 本次是把登入這個成因移出迴圈，
不是修掉迴圈：任何**其他**瞬間失敗的場景（例如打錯 `progId`）依然會在封閉模型下全速空轉，
把錯誤數推到與工作量無關的數量級。

之所以沒一併處理：要修就得引入「錯誤率超過門檻即中止」這類機制，而門檻是另一個要調的參數，
且會讓「刻意量測失敗路徑」變得不可能。登入是**唯一**已知會整輪失敗的前置步驟，先解它。
真的再踩到別的成因時再評估。

## 不在本 plan 範圍

- **跨 provider 的效能比較**：本次數據顯示 PostgreSQL 在這台機器上吞吐約為 SQL Server 的
  兩倍，但那是單機、單次、開發環境的觀察，**不足以支持任何選型結論**。

## 附註：數據的侷限

上表是**當時在該台機器量到的**，不是框架的效能規格：開發用 macOS、client 與 server 同機、
封閉模型（送出速率隨系統變慢而下降，因此不顯示飽和點）、資料量 100,000 筆（對這四家都仍屬
小表，皆可完全載入記憶體）。跨 provider 的絕對值受各容器設定影響，**倍數關係才是本 plan
引用的部分**。

Oracle 又比其他三家多一層侷限：它跑在預設設定的 `oracle23ai` 容器裡，吞吐約為前三家的
五分之一，且兩個場景共有一條約 2 秒的尾巴。**那條尾巴出現在淺頁上，所以它不是分頁造成的**，
但成因未查——在查明之前，Oracle 的絕對值連「這台機器上的 Oracle 有多快」都不足以說明，
只能支持「深分頁在 Oracle 上同樣變慢，且倍數不低於其他三家」這一句。
