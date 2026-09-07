# 壓測取數規範

`tools/Bee.LoadTests` 的使用方式與取數紀律。**維運文件、非公開文件**——讀者是 bee-library
的維護者，不是框架使用者。

設定項的完整清單與各項意義**不在本檔**：以 `tools/Bee.LoadTests/loadtest.sample.json`
（每一項都有註解）與各設定型別的 XML doc 為準。本檔只寫「怎麼跑」與「怎麼讀數字」。

---

## 前置條件

1. **資料庫容器**在跑（`./test.sh` 用的那組，容器名見 `test.sh` 檔頭）。
2. **連線字串**以 `BEE_TEST_CONNSTR_{DBTYPE}` 提供，與 `./test.sh` 同一個變數。
3. **`prepare` 跑過一次**：建立壓測專屬資料庫、建表、植入帳號與資料。

```bash
export BEE_TEST_CONNSTR_SQLSERVER='...'
dotnet run --project tools/Bee.LoadTests -c Release -- prepare
dotnet run --project tools/Bee.LoadTests -c Release -- run
```

Remote 模式另需先起伺服端（另一個終端機）：

```bash
dotnet run --project tools/Bee.LoadTests -c Release -- serve
dotnet run --project tools/Bee.LoadTests -c Release -- run --mode Remote --endpoint http://localhost:5199/api
```

`--vu` / `--duration` / `--warmup` / `--mode` / `--endpoint` / `--protection` 可覆寫設定檔；
完整旗標見 `--help`。

## 兩條硬性排除

### 不跑 SQLite

它是檔案式單機／嵌入式定位，不是伺服端選項，且全域寫入鎖會讓併發寫呈現實際上不存在的瓶頸。
設定驗證會直接拒絕，訊息說明原因。

> **不要用「SQLite 走不同程式碼路徑」當理由。** 它獨有的差異集中在 DDL 層，而壓測打的是
> DML 熱路徑——在那條路徑上 `SqliteProviderFactory` 刻意把它與其他 provider 拉齊了。

### 不進 CI

壓測數字在 CI runner 上噪音太大，當閘門只會製造 flaky，而 flaky 閘門的下場是被忽略或被關掉。
手動觸發、結果記進文件即可。

## 取數紀律

- **warm-up 不可省。** 快取在首次使用才載入，沒有 warm-up 的百分位描述的是冷啟動。
- **正式數字在真實伺服端 provider 上取**，預設 SQL Server。
- **錯誤數與延遲一起讀。** 一份半數呼叫快速失敗的報告，只看延遲會非常漂亮——實作上曾經
  出現過整輪 0 成功而延遲欄全是 0 的情形。
- **知道自己量的是哪一種模型。** 預設封閉模型（每個 VU 等前一次回來才發下一次），送出速率
  會隨系統變慢而下降，因此**不會**顯示開放模型找得到的尾延遲崩潰。它貼近人使用商業應用的
  樣子，但要找飽和點得換模型。

## 報告

三層輸出：console、Markdown、JSON，預設寫到 `artifacts/loadtest/`（已 gitignore）。

**值得留的手動複製到本目錄**，其餘不必保存——多數跑動是探索性的。

### 數字怎麼寫才不會漂

結果一律記成「**當時量到什麼**」。報告的中繼資料區已帶齊版本（含 commit）、provider、模式、
機器與負載形狀，複製過來時**連同中繼資料一起**，不要只摘延遲數字。

**不要寫成「本框架吞吐為 X」**——那是複寫，必漂，且沒有任何機制會發現它過期
（見 `~/.claude/rules/single-source.md`）。需要對外交代效能特性時另行升格成 ADR 或公開文件。

## 已知限制

判讀報告時需要知道的三件事：

| 限制 | 影響 |
|------|------|
| **Remote run 量不到快取** | 計數 provider 在驅動程式的 process，被操作的快取在伺服端。報告會標示 `Not observed`，JSON 帶 `CacheObserved: false`。要量快取行為得用 Local 模式。 |
| **`Order` 的 BO 綁定會被清掉** | 那組定義把 `Order` 綁到 demo 伺服端組件，驅動程式不引用它（引用等於把應用的商業邏輯摺進「量框架」的數字）。該程式因此退回框架自身實作，報告的 `Dropped bindings` 會列出。 |
| **植入的關聯欄位不是真外鍵** | 每張表獨立植入，關聯欄拿到的是生成值。對讀取場景足夠——量的是查詢本身；需要主檔與明細對得起來的場景得自己植入。 |

## 相關

- `docs/plans/plan-load-testing.md` —— 設計決策與推導過程（階段性文件）
- `tools/Bee.LoadTests/loadtest.sample.json` —— 設定項的權威來源
- `.claude/rules/testing.md` —— 單元測試規範（與本檔無關，勿混用）
