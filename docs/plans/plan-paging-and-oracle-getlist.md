# 計畫：深分頁成本與 Oracle GetList 失效

**狀態：🚧 進行中（2026-09-08）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| A | Oracle 的 `GetList` 失效（ORA-00932）—— 功能問題 | ✅ 已完成（2026-09-08） |
| B | 深分頁（`OFFSET`）成本 —— 效能問題 | 📝 待決策 |

兩者互相獨立，優先序不同：**A 是壞的，B 是慢的**。A 應先處理，B 需要先決定值不值得動。

## 背景

`tools/Bee.LoadTests` 建立後，第一次用它跨 provider 跑同一組場景，量出兩件先前沒有數據的事。
量測條件一致：Local、Encrypted、20 VU、warm-up 30s、量測 120s、`ft_customer` 100,000 筆、
`pageSize` 50，淺頁自第 1 頁起、深頁自第 1,900 頁起（`OFFSET 94,950`），各走 10 頁。

## 階段 A：Oracle 的 `GetList` 完全失效

### 現象

Oracle 上**兩個場景都是 0 成功**，不限深分頁：

```
ORA-00932: 表示式 (:1) 為 TIMESTAMP 資料類型, 與預期的資料類型 BINARY 不相容
```

其餘三家（SQL Server / PostgreSQL / MySQL）在同一組定義、同一份 seed 資料下皆 0 錯誤，
所以**問題在 Oracle 這條路徑本身**，不是定義或資料。

錯誤訊息在 `IsDebugMode=false` 時會被伺服端收斂成 `Internal server error`；本次是暫時開啟
debug 才取得的。

### 為什麼沒被測試抓到

`DataFormRepository` / `GetList` 相關的測試檔**沒有任何一個標記 Oracle**
（`grep -c 'DatabaseType.Oracle'` 全數為 0）。Oracle 在測試套件裡有可觀的涵蓋，
但集中在 framework repository（`UserRepository`、`CompanyRepository` 等），
**FormSchema 驅動的 CRUD 查詢路徑不在其中**。

這是測試矩陣的缺口，不是個別測試漏寫：那條路徑在 Oracle 上從未被驗證過。

### 查證結果

**不在 `GetList` 的 SELECT 上，也與型別推斷無關。** 壓測的 list 場景不帶 filter，
產生的 SELECT 一個 bind 變數都沒有；`GetList` 之所以 100% 失敗，是因為
`VirtualUserPool` 的登入在第一次就擲例外並把 faulted task 快取起來，之後每個 iteration
都立刻重擲同一個例外（5 秒內 115 萬筆「錯誤」即為此，不是 115 萬次資料庫往返）。

真因有兩個，彼此獨立：

#### A1：Oracle 預設以「位置」而非「名稱」綁定參數

`Oracle.ManagedDataAccess` 的 `OracleCommand.BindByName` 預設為 `false`，
於是 SQL 裡第 n 個 bind 變數拿到的是**參數集合的第 n 筆**，與名稱無關。
其餘四家都以名稱綁定 —— 而 `{0}` / `{Name}` 佔位符 API 的語意正是以名稱對應
（`DbAccessTests` 早就有 `"Update st_user Set note={1} Where sys_id = {0}"` 這種寫法）。

登入時 [`SessionRepository.UpdateSession`](../../src/Bee.Repository/System/SessionRepository.cs)
先設兩個欄位再以 access token 比對，佔位符順序是 `{1} {2} {0}`；在位置綁定下
`DateTime` 被送進 `access_token`（`RAW(16)`）欄位，即 `ORA-00932`。

**修法**：[`DbCommandSpec.CreateCommand`](../../src/Bee.Db/DbCommandSpec.cs) 對 Oracle 的
text command 設 `BindByName = true`（以反射設定，因為 `Bee.Db` 不參考任何 ADO.NET driver）。
逐句改寫 SQL 不是解 —— 那要求每個寫 SQL 的人都記得一條沒有任何機制檢查的 Oracle 專屬規則。

#### A2：Oracle 的 `RAW(16)` 讀回來是 `byte[]`，不是 `Guid`

Oracle 沒有 UUID 型別，框架把 `FieldDbType.Guid` 對映為 `RAW(16)`。寫入端早已處理
（`DbCommandSpec.NormalizeParameterValue` 轉 `byte[]`），**讀取端沒有**：

- `DataFormRepository.TryCoerceToGuid` 只認 `Guid` 與 `string`，於是 `GetData` / `Save`
  在 Oracle 上擲 `Cannot coerce value of type 'System.Byte[]' into Guid`。
- 更廣的一層是回傳的 `DataTable` 本身：`sys_rowid` 欄位宣告為 Guid、實際裝 `byte[]`，
  每個以 `is Guid` 判斷的消費端（`FormDataGuard`、各 UI head 的 grid）都會在 Oracle 上走錯分支。

**修法**：`MarkFromSchema` 就地把「schema 宣告為 Guid、provider 卻給 `byte[]`」的欄位
換成真正的 Guid 欄位，`TryCoerceToGuid` 同步補上 `byte[]` 分支。
`ValueUtilities.CGuid(object)` **本來就處理了 `byte[]`** —— 會漏是因為
`DataFormRepository` 自帶了一份平行實作。

### 已完成的事

1. 以 [`ParameterBindingOrderTests`](../../tests/Bee.Db.UnitTests/ParameterBindingOrderTests.cs)
   在五家 provider 上覆蓋佔位符的綁定合約（非遞增順序、同一佔位符出現兩次），
   Oracle 那支在修正前擲出與壓測完全相同的 `ORA-00932`。
2. 修正 A1 與 A2。
3. **把 FormSchema 驅動的路徑納入 Oracle 測試矩陣**：`GetList`（含分頁與 fallback sort）、
   `GetData`、`Save`、`Delete`、CRUD flow，另加兩支明確斷言 `sys_rowid` 欄位型別為 `Guid` 的測試。
4. 修正壓測工具本身兩處同樣的 `is Guid` 誤判（`AccountSeeder.ResolveRowId`、
   `DataSeeder.ReadRowIds`）—— 前者讓第二次 `prepare` 重插既有帳號而違反唯一鍵，
   後者讓 Oracle 收不到任何 rowId、`GetData` / `Save` 場景直接無法啟動。

驗證：Oracle 上 Login / GetList / GetData / Save 四個場景 0 錯誤。

### 仍未處理（不在本次範圍）

- **框架軸 repository 的 Oracle 覆蓋是名義上的。** `DbScope.Common` 固定解析為
  databaseId `"common"`，而測試 fixture 把 `"common"` 註冊成 SQL Server；
  `UserRepositoryTests` / `ApiKeyRepositoryTests` 上的 `[DbFact(DatabaseType.Oracle)]`
  實際打的是 SQL Server（`RunRoundTrip(DatabaseType _)` 直接丟棄該參數即為徵狀）。
  這也是 A1 藏了這麼久的原因之一。
- **壓測工具在 Oracle 上無法與單元測試隔離。** `databaseNamePrefix` 對 Oracle 無效
  （單一 `testuser` schema 容納所有 category），因此 `prepare --provider Oracle` 會把
  Northwind 的 `st_user`（`password` 長度 200）套到單元測試共用的表上，之後單元測試
  fixture 會以 `Change narrows a column` 整組失敗。

## 階段 B：深分頁的 `OFFSET` 成本

### 數據

三家皆重現，倍數相近：

| Provider | 淺頁 p50 | 深頁 p50 | 倍數 | 淺頁 p99 | 深頁 p99 | 場景 rps |
|---|---:|---:|---:|---:|---:|---:|
| SQL Server | 1.3 | 18.8 | **14.5×** | 8.9 | 51.4 | 745.8 |
| PostgreSQL | 0.7 | 10.9 | **15.6×** | 4.3 | 24.8 | 1548.4 |
| MySQL | 1.0 | 20.1 | **20.1×** | 5.9 | 40.8 | 871.0 |
| Oracle | — | — | — | — | — | 見階段 A |

**這是 `OFFSET` 分頁的固有行為，不是框架缺陷**：引擎必須走過並丟棄偏移量之前的所有列。
成本隨偏移量成長，而第一頁的查詢完全感覺不到——這也是為什麼把資料量從 1,000 筆拉到
100,000 筆時，只取前 10 頁的場景數字紋風不動。

### 決策點：值不值得處理

**先回答這個，再談怎麼做。** 判準是實務上使用者會不會深翻：

- 若 ERP 畫面都有篩選、結果集通常在數十頁內 → **不必處理**，記錄為已知特性即可。
- 若存在「全表瀏覽」「匯出前翻到底」這類用法 → 值得處理。

這個問題**框架端答不了**，取決於實際部署的使用型態。

### 若要處理，三個選項

| 選項 | 做法 | 代價 |
|------|------|------|
| **1. 不處理** | 記進 `docs/repo-ops/gotchas/database.md`，讓下次有人問時查得到這組數字 | 深翻的畫面就是慢；但成本為零 |
| **2. Keyset pagination** | 以上一頁最後一筆的鍵當游標（`WHERE key > @last`），成本與偏移量無關 | 動到 [`PagingOptions`](../../src/Bee.Definition/Paging/PagingOptions.cs) 的介面、wire contract 與所有 UI head；且**不支援跳頁**（只能上一頁 / 下一頁），是產品行為的改變 |
| **3. 混合** | 淺頁維持 `OFFSET`，超過門檻改走 keyset | 兩套路徑要維護，且門檻是另一個要調的參數 |

選項 2 的「不支援跳頁」是關鍵取捨：keyset 換得的效能，代價是使用者不能直接跳到第 500 頁。
若現行 UI 有頁碼跳轉，這是產品決策而非技術決策。

### 若採選項 1，仍建議做的事

即使不改實作，`PagingOptions` 的 XML doc 可以說明這個成本特性——現行 doc 只提醒了
「未分頁會載入全部」，沒有提醒「深分頁的成本隨偏移量成長」。那是讓呼叫端做出正確設計
選擇的資訊。

## 不在本 plan 範圍

- **壓測工具本身的改動**已完成（深分頁場景、四家 driver、`--provider` 覆寫），
  不需要再動。
- **調優 SQL Server 以外的 provider**：本次數據顯示 PostgreSQL 在這台機器上吞吐約為
  SQL Server 的兩倍，但那是單機、單次、開發環境的觀察，**不足以支持任何選型結論**，
  也不是本 plan 要回答的問題。

## 附註：數據的侷限

上表是**當時在該台機器量到的**，不是框架的效能規格：開發用 macOS、client 與 server 同機、
封閉模型（送出速率隨系統變慢而下降，因此不顯示飽和點）、資料量 100,000 筆（對這四家
都仍屬小表，皆可完全載入記憶體）。跨 provider 的絕對值受各容器設定影響，**倍數關係才是
本 plan 引用的部分**。
