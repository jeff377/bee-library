---
name: bee-load-test
description: 用 tools/Bee.LoadTests 對 bee-library 框架跑壓測並判讀結果——前置檢查（容器、連線字串、prepare）、Local / Remote 兩種模式、以及「數字什麼時候不能信」的判讀紀律（參數過小、錯誤數被忽略、封閉模型的尾延遲盲點、Remote 量不到快取）。當使用者要「跑壓測」、「壓力測試」、「load test」、「量效能」、「效能測試」、「這樣改會不會變慢」、「量一下吞吐 / 延遲」之類需求時使用。**只負責執行與判讀，不負責調優**；也不為了讓數字好看而改參數或原始碼。
---

# bee-library 壓測執行

**操作與規範的權威來源是 `docs/repo-ops/load-testing.md`**，設定項清單是
`tools/Bee.LoadTests/loadtest.sample.json`（每項都有註解），旗標是 `--help`。
本檔不複寫那些，只放**執行時的判斷與紀律**——也就是 agent 最容易做錯的部分。

---

## 1. 前置檢查（照順序，不可跳）

### Docker daemon

```bash
docker ps
```

失敗時**告知使用者啟動 Docker Desktop，不要自行 `open -a Docker`**。
容器存在但 stopped 也不要自行 `docker run` 創新的——image 版本 / port / volume 都有約束。

### 連線字串

console app **不讀 `.runsettings`**，必須用環境變數：

```bash
export BEE_TEST_CONNSTR_SQLSERVER='...'
```

值可從 `.runsettings` 取。缺少時工具的錯誤訊息會寫明變數名與補救方式，照它做即可。

### prepare

第一次、或改了 `seed.rowCount` / `auth.userPoolSize` 後要跑：

```bash
dotnet run --project tools/Bee.LoadTests -c Release -- prepare
```

它是冪等的，重跑安全。

## 2. 執行

Local（量 BO + Repository + DB）：

```bash
dotnet run --project tools/Bee.LoadTests -c Release -- run --vu 20 --duration 120
```

Remote 需要兩個終端機，先 `serve` 再 `run --mode Remote --endpoint ...`；
完整命令見 `docs/repo-ops/load-testing.md`。

## 3. 判讀紀律

**這一節是本 skill 存在的理由。** 前面兩節照文件做就好，這一節是拿到數字之後的事。

### 先看錯誤欄，再看延遲

一份半數呼叫快速失敗的報告，延遲會非常漂亮。**回報結果時錯誤數與延遲必須並列**，
不可只摘延遲。實際發生過：Login 全數失敗那一輪的延遲欄全是 0。

錯誤型別名不足以診斷時，報告的 `ErrorSamples` 每型別留了一則訊息，先讀那個。

### 參數過小的數字不能下結論

`--vu 4 --duration 5` 是驗證「跑得動」用的，不是拿來下結論的。
要回答「這樣改會不會變慢」，VU 與時長都要足以讓數字穩定，並且**同一組參數跑對照組**。

**單獨一次 run 的絕對數字幾乎沒有意義**——有意義的是同機器、同參數、改動前後的對照。

### 封閉模型看不到尾延遲崩潰

預設每個 VU 等前一次回來才發下一次，送出速率會隨系統變慢而下降。
因此**它不會顯示開放模型找得到的飽和點**。要找飽和點得換模型，不要拿封閉模型的
p99 宣稱「系統在這個負載下沒問題」。

### Remote 量不到快取

計數 provider 在驅動程式的 process，被操作的快取在伺服端。報告會標 `Not observed`，
**那是「沒量到」不是「命中率 0%」**。要量快取行為改用 Local 模式。

### 報告要連中繼資料一起保存

報告寫到已 gitignore 的 `artifacts/loadtest/`。值得留的複製到 `docs/repo-ops/`，
**連中繼資料區一起複製**——只摘延遲數字的話，之後沒人知道那是什麼條件下量的。

寫法一律是「當時量到什麼」，**不得寫成「本框架吞吐為 X」**（複寫必漂，見
`~/.claude/rules/single-source.md`）。

## 4. 常見失敗與處理

| 症狀 | 原因與處理 |
|------|-----------|
| `Environment variable 'BEE_TEST_CONNSTR_*' is not set` | 沒 export；照訊息設定 |
| 大量 `HttpRequestException` 401 | Remote 模式缺 `X-Api-Key`；設定的 `target.apiKey` 沒帶到 |
| 場景全數失敗且訊息指向 DI 解析 | backend 起不來，先跑 `verify` 隔離問題 |
| `Unknown scenario 'X'` | 設定檔場景名打錯。**這是刻意不跳過的**——靜默略過會產出看起來完整的報告 |
| 容器沒起來 | 見前置檢查；**不要改測試或原始碼讓它「過」** |

`verify` 指令會起 backend、解析服務、讀一份 FormSchema 再拆掉，用來把
「backend 有問題」與「場景有問題」分開。

## 5. 不做什麼

- **不為了讓數字好看而調參數或改原始碼。** 數字難看是訊號，不是要消除的東西。
- **不把壓測放進 CI。** runner 噪音太大，當閘門只會製造 flaky。
- **不跑 SQLite。** 設定驗證會直接拒絕，理由見文件；不要為了「能跑」而繞過。
- **不負責調優。** 量出瓶頸後要不要調、怎麼調，是後續決策，回報給使用者判斷。

## 相關

- `docs/repo-ops/load-testing.md` —— 操作與規範的權威來源，含已知限制
- `tools/Bee.LoadTests/loadtest.sample.json` —— 設定項的權威來源
- `.claude/rules/testing.md` —— 單元測試規範（與壓測無關，勿混用）
