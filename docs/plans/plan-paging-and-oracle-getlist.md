# 計畫：深分頁成本與 Oracle GetList 失效

**狀態：📝 擬定中（2026-09-08）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| A | Oracle 的 `GetList` 失效（ORA-00932）—— 功能問題 | 📝 待做 |
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

### 待查

- `:1` 是哪個參數。`ORA-00932` 指參數被推斷為 `TIMESTAMP` 而欄位期望 `BINARY`(RAW)，
  依 `.claude/rules/database.md`，Oracle 的 `Guid → Binary` 轉換在
  [`DbCommandSpec.NormalizeDbType`](../../src/Bee.Db/DbCommandSpec.cs)；需確認查詢路徑
  是否走到它，或參數在別處就被推斷成別的型別。
- 是否只影響 `GetList`，還是 `GetData` / `Save` 同樣失效（本次場景設定只跑了兩個 list 場景）。
- 是否與壓測植入的資料有關（`DataSeeder` 對 `Guid` 欄寫入 `Guid`、對 `DateTime` 欄寫入
  `DateTime`）——但 seed 本身在 Oracle 上成功，且查詢才失敗，指向查詢側。

### 建議範圍

1. 先以一支針對 Oracle 的 `DataFormRepository.GetList` 測試重現（**先讓它紅**）。
2. 修正型別推斷。
3. **把 FormSchema 驅動的查詢路徑納入 Oracle 測試矩陣** —— 否則修好了也擋不住下次。

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
