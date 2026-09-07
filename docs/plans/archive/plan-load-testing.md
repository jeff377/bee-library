# 計畫：建立框架壓測設施

**狀態：✅ 已完成（2026-09-07）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 建 `tools/Bee.LoadTests` 專案骨架 + Local 層（in-process）場景，含快取計數 decorator | ✅ 已完成（2026-09-07） |
| 2 | 加上 Remote 層（HTTP），Local/Remote 對照量出傳輸層成本 | ✅ 已完成（2026-09-07） |
| 3 | 報告輸出（console / Markdown / JSON）與取數規範，寫進 `docs/repo-ops/` | ✅ 已完成（2026-09-07） |

## 背景

repo 目前**沒有任何壓測設施** —— 沒有 BenchmarkDotNet / NBomber / k6 的痕跡，
`build-ci.yml` 不跑效能測試，`docs/repo-ops/future-work.md` 也沒把它列為待辦。
因此「框架建議怎麼壓測」目前是空白，任何人要量都得從零推導一次。

本 plan 的目的是**把那次推導固化下來**：不是為了得到一個漂亮的吞吐數字，
而是讓「量到的東西是真的、且下次量得到同一件事」。

## 框架形狀帶來的五個約束

這五條決定了工具選型與腳本寫法，先立在這裡，實作時逐條對照。

### 1. 通用 HTTP 壓測工具打不到真實路徑

端點是單一 `POST /api`（[ApiServiceController](../../../src/Bee.Api.AspNetCore/Controllers/ApiServiceController.cs)），
method 藏在 JSON-RPC body 裡。真正的問題在 body：`PayloadFormat` 為 `Encoded` / `Encrypted` 時，
body 由請求宣告的 codec 決定拼寫，**未宣告即 MessagePack**（adr-044）。

k6 / JMeter / bombardier 產不出 MessagePack payload，只能餵事先錄好的固定 body；
改用 `Plain` 測，量到的就不是生產路徑（少了序列化 → 壓縮 → 加密整條管線）。

→ **壓測 client 必須是 .NET，走 `Bee.Api.Client` 的
[`ApiConnector`](../../../src/Bee.Api.Client/Connectors/ApiConnector.cs)**，
才會跟真實客戶端走同一條 payload pipeline。**驅動程式自己寫，不引入壓測框架**——理由見下節。

### 2. `ApiSessionContext.Ambient` 是 process 單例，多 VU 必須各自持有

`ApiSessionContext.Ambient` 是 `static` 唯一實例
（[ApiSessionContext.cs](../../../src/Bee.Api.Client/ApiSessionContext.cs)）。
不帶 session 參數的 `ApiConnector` 建構子一律綁到它。

多個 VU 各自登入時若都走預設建構子，**後一次登入的傳輸金鑰會覆蓋前面所有 VU 的**。
症狀不會是明確的錯誤，而是解密失敗或行為錯亂 —— 很容易被誤判成「框架在高併發下不穩」。

→ 每個 VU 建立自己的 `ApiSessionContext` 實例，走
`ApiConnector(endpoint, accessToken, session)` 這組多載。
**這是本 plan 中最容易踩、且最容易誤判成框架 bug 的一條。**

### 3. 兩層要分開量

`LocalApiProvider`（in-process 分派）與 `RemoteApiProvider`（HTTP）是
[`IJsonRpcProvider`](../../../src/Bee.Api.Client/Providers/IJsonRpcProvider.cs) 的兩個實作。

- **Local** 量的是 BO + Repository + DB 這一段
- **Remote** 量的是整條，含 HTTP、序列化、壓縮、加密

兩者相減才說得出瓶頸在哪。混在一起量只會得到一個無法歸因的數字。
階段 1 只做 Local，階段 2 才加 Remote —— 這個順序是刻意的：
Local 跑不動時，Remote 的數字沒有意義。

### 4. 必須 warm-up，否則尾延遲是假的

cache 是 process-wide 的：FormSchema、CompanyInfo、權限、DepartmentTree 這些
第一次請求才載入，之後全命中（見 `.claude/rules/definition.md`）。
冷啟動那幾秒會把 p95 / p99 整個拉歪。

→ 正式取數前先跑一輪並**丟棄**，且 warm-up 時長要顯式寫在設定裡、不用隱含預設。

### 5. 結論綁 DB provider，且**完全不跑 SQLite**

provider 之間行為差異夠大（這正是 `NormalizeDbType` 存在的理由，見 `.claude/rules/database.md`），
所以每次取數都要記錄 provider。

**SQLite 直接排除在壓測之外**，兩個理由：

1. 它有全域寫入鎖，併發寫量到的是假性瓶頸。
2. 框架自己把它**定位在「檔案式單機與嵌入式情境」**
   （[src/Bee.Db/README.zh-TW.md](../../../src/Bee.Db/README.zh-TW.md)），
   不是伺服端選項；repo 內引用它的只有 `samples/Bee.Samples.Shared` 與
   `apps/Bee.Northwind/Bee.Northwind.Server` 這兩個 demo 用途。**這是主要理由。**

→ 正式數字一律在真實伺服端 provider 上取，預設 SQL Server（`./test.sh` 本來就會起容器，
零設定的優勢並不專屬於 SQLite）。留一條 SQLite 路徑的唯一效果是誘使人在上面取數然後誤用。

> **不要用「SQLite 走不同程式碼路徑」當理由。** 它獨有的差異
> （ALTER 一律 rebuild、無 `COMMENT ON`、type affinity）集中在 **DDL / schema 層**，
> 而壓測打的是 **DML 熱路徑**——在那條路徑上 SQLite 是被**刻意拉齊**的：
> [`SqliteProviderFactory`](../../../src/Bee.Db/Providers/Sqlite/SqliteProviderFactory.cs)
> 補上 `Microsoft.Data.Sqlite` 缺的 `DbDataAdapter`，讓它與其他 provider 共用同一條
> adapter-based 讀寫路徑。排除 SQLite 的理由是**它不是伺服端選項**，不是路徑不同。

## 外部套件：不引入壓測框架

**不引用任何壓測框架**，只以 `ProjectReference` 取用 repo 內的 `Bee.Api.Client` 等組件。
並行排程與統計自己寫。

**唯一的例外是 ADO.NET driver**（`Microsoft.Data.SqlClient` 等）。`Bee.Db` 本身刻意不引用
任何 driver —— 那是 host 的責任，而壓測工具在 Local 模式下**就是** host。這不是破例，
是承擔 host 本來就該承擔的東西：每個要跑起來的 backend 都得引一個。

### 為什麼不用 NBomber

它是這類任務的第一順位人選，但**授權不合這個 repo**：NBomber 自 6.x 起是
**商業/專有授權** —— 個人與 hobby 專案免費，**任何組織（公司、非營利、政府）使用都需要
購買 Commercial Subscription 並取得 Activation Key**（2026-09-07 查 nuget.org 的
授權頁確認）。

bee-library 是 **public 的 MIT repo**。把壓測建立在商業授權套件上，等於任何 clone 下來
想跑壓測的組織都得先買授權 —— 而這份 plan 的產出正是要當成「框架建議的壓測方法」，
建議一個帶授權負擔的工具並不合適。

### 其他評估過的選項

| 選項 | 授權 | 為何不用 |
|------|------|---------|
| BenchmarkDotNet | MIT | 是**微基準**工具，量單次呼叫的統計分佈，不是併發負載；不合用途 |
| k6 / JMeter | 開源 | 產不出 MessagePack payload（約束 1），打不到真實路徑 |
| dotnet/crank | MIT | 偏基礎設施級，為了本 plan 的範圍設定成本過高 |

### 自己寫需要多少東西

範圍比想像的窄，因為 client 已經現成（`ApiConnector`），只缺「排程 + 計時 + 統計」：

- **並行驅動** —— N 個 worker 各自跑迴圈，各持有自己的 `ApiSessionContext`（約束 2）
- **warm-up 隔離** —— 兩段計時，第一段的樣本丟棄
- **統計** —— 收集每次的耗時，排序後取 p50 / p95 / p99，加總算 RPS

沒有外部相依，純 BCL（`Task`、`Stopwatch`、`Array.Sort`）就夠。`tools/Directory.Build.props`
本來就設 `IsPackable=false`，這個專案不會發布。

### 自己寫要自己注意的一件事：封閉模型的偏誤

「N 個 worker 做完一次再做下一次」是**封閉模型**：系統變慢時送出速率會自動降低，
因此它**不會**暴露出開放模型（固定到達率）能看到的尾延遲惡化。

這不是必須修掉的缺陷 —— ERP 的真實使用者確實是等回應才做下一個動作，封閉模型反而貼近實情。
但**取數時要知道自己量的是哪一種**，並記進結果（階段 3）。要看飽和點時再加開放模型的驅動方式。

## 階段 1：專案骨架 + Local 層

**位置**：`tools/Bee.LoadTests`（console 專案）

放 `tools/` 而非 `tests/` 的理由：它不是 xUnit 測試、不由 `./test.sh` 執行、不進 CI，
放進 `tests/` 會讓人以為 `dotnet test` 會跑到它。
需同步註冊到 `Bee.Tools.slnx`（不是 `Bee.Library.slnx`）。

**場景**（方法名取自
[`FormApiConnector`](../../../src/Bee.Api.Client/Connectors/FormApiConnector.cs)）：

| 場景 | 打什麼 | 為何需要 |
|------|--------|---------|
| `Login` | session 建立，會寫 `st_session` | 唯一會建 session 的路徑，且每個 VU 各跑一次 |
| `GetListAsync` / `GetDataAsync` | 資料讀取 | 最常見的請求形狀 |
| `SaveAsync` | 業務資料寫入 | 唯一壓得到交易與寫入鎖的路徑 |
| 定義類讀取 | `IDefineAccess` 的 Get 系列，經 `CacheDefineAccess` | 量的是快取行為，不是 DB —— 見下節 |

**定義類只壓讀取，不壓寫入。** 定義寫入（`IDefineAccess.SaveX(...)`）只在維護者改定義時
發生，呼叫次數與資料面差好幾個數量級，壓它得不到有用的結論。

**token 策略必須在腳本裡明講**（兩種都合法，但量到的是不同的東西）：

- 共用一個 token → 測的是「單一使用者連打」
- 各 VU 各自登入 → 才壓得到 session 查表與 `st_session` 寫入

後者需要先 seed 一批測試帳號。**預設走後者**，因為它才是多使用者情境；
共用 token 那組留作對照，用來把 session 查表的成本隔離出來。

### 定義類：要量的是快取命中，不是延遲

定義資料經 `CacheDefineAccess` 走 process-wide cache，穩態下幾乎不碰 DB。
因此這條場景的延遲數字本身沒什麼資訊量，**真正要回答的是兩個問題**：

1. 穩態下命中率是不是接近全中？沒有的話，是哪個 slot 一直 miss？
2. 併發同時 miss 同一把 key 時，
   [`CacheSingleFlight`](../../../src/Bee.ObjectCaching/CacheSingleFlight.cs)
   有沒有把它們收斂成單次建立？

第 2 點特別值得壓 —— 它是**併發專屬行為**，單元測試不容易涵蓋到真實併發下的表現，
而它擋的正是 cache stampede。

**現況：目前量不到。** `src/Bee.ObjectCaching/` 沒有任何命中 / 未命中計數
（`ObjectCache` / `KeyObjectCache` 都只做取值，沒有統計面）。

**做法已定案：包一層 `ICacheProvider` decorator，框架零改動。**

[`CacheInfo.Provider`](../../../src/Bee.ObjectCaching/CacheInfo.cs) 是 public 可設定的
static 屬性（預設 `MemoryCacheProvider`，且本來就支援從設定檔換成其他實作）——
**它本身就是框架設計好的替換點**，壓測啟動時包一層即可：

```csharp
CacheInfo.Provider = new CountingCacheProvider(CacheInfo.Provider);
```

[`ICacheProvider`](../../../src/Bee.ObjectCaching/Providers/ICacheProvider.cs) 只有五個方法，
decorator 很薄。它能回答本節開頭的兩個問題：

- **命中率** —— `Get` 回 `null` 即 miss，計數相除即得。
- **single-flight 收斂** —— `CacheSingleFlight` 位於 `KeyObjectCache` / `ObjectCache` 層，
  **在 provider 之上**，收斂掉的是工廠呼叫，而工廠內才寫 provider。因此 **`Set` 次數就是
  實際建立次數**：N 個 VU 併發打同一把 key 若只看到一次 `Set`，收斂就成立。
  這是直接證據，不是推論。

**已知盲點**：decorator 只看得到 provider 層，看不到 provider 之上的 negative-cache
short-circuit（[KeyObjectCache.cs:109](../../../src/Bee.ObjectCaching/KeyObjectCache.cs)）。
那條路徑根本不會下到 provider，所以它的命中不會被計入。取數時要知道分母是什麼。

**與「禁止修改 production static」規則的關係**：`.claude/rules/testing.md` 第 3 條禁止測試
修改 production 的 `static`，理由是 xUnit 不同 test class 平行執行必然 race。壓測專案是
**獨立的 console process、不是 xUnit**，那個理由不適用。這個判定寫在這裡，是因為
下一個 session 讀到 `CacheInfo.Provider = ...` 時會合理地懷疑它違規。

### 為什麼不用 System.Diagnostics.Metrics

評估過，技術上可行且**不需要新增任何 PackageReference** —— `Microsoft.Extensions.Caching.Memory`
已經把 `System.Diagnostics.DiagnosticSource` 傳遞帶進 `Bee.ObjectCaching`
（在該專案加一個用 `Meter` / `Counter<long>` 的探針檔，Release build 0 錯誤 0 警告，
2026-09-07 實測）。`Bee.ObjectCaching` 也不在 `BEE9001` 的受管清單裡（那條只鎖
`Bee.Base` / `Bee.Definition`），所以不存在相依邊界問題 —— 我先前把這一點寫成障礙是錯的。

不採用的理由與相依無關，是**範圍**：

- 它是 production 級的長期遙測能力，本 plan 只需要壓測期間的量測。
- 靠傳遞相依取得的型別是脆弱的（`Caching.Memory` 哪天不再帶 `DiagnosticSource` 就會斷）；
  要穩就得顯式 `PackageReference`，那才會讓 nuspec 多一筆，而那是個
  **獨立於壓測的決策**，不該由這份 plan 順手決定。

→ 若日後要做 production 級的快取遙測，另開議題處理，不綁在本 plan。

**驗收**：場景在 SQL Server 容器上跑得完，warm-up 與正式期分離，
輸出含 p50 / p95 / p99 與 RPS；定義類場景另外輸出命中率與 single-flight 收斂結果。

## 階段 2：Remote 層與保護等級對照

加上 `RemoteApiProvider` 路徑，對 `samples/QuickStart.Server` 或
`apps/Bee.Northwind/Bee.Northwind.Server` 起的 host 打。

`ApiProtectionLevel` 至少跑 `Public` 與 `Encrypted` 各一輪
（[ApiProtectionLevel.cs](../../../src/Bee.Definition/Security/ApiProtectionLevel.cs)），
加密的 CPU 成本才顯示得出來。`LocalOnly` 不適用於這一層。

**驗收**：能講出「HTTP + 序列化佔多少、加密再加多少」，而不只是一個總數。

## 設定：參數與報告都由設定檔驅動

**壓測參數不寫死在程式碼裡** —— 換一次負載強度就要改 code 重編譯，會讓人懶得多跑幾組，
而壓測的價值恰恰來自「換參數再跑一次」。

**設定檔為主、命令列可覆寫**最常調的幾個（`--vu` / `--duration` / `--config`）。
解析方式沿用 [`Bee.Cli`](../../../tools/Bee.Cli/Program.cs) 的既有慣例：**手寫 args 解析、
不引入 `System.CommandLine`**，與上一節一致。設定檔用 JSON
（`System.Text.Json` 是 BCL）。

```jsonc
{
  "target": {
    "mode": "Remote",                    // Local（in-process）| Remote（HTTP）
    "endpoint": "http://localhost:5000/api",
    "protectionLevel": "Encrypted",      // Public | Encoded | Encrypted
    "codec": "messagepack"
  },
  "database": {
    "provider": "SqlServer",             // 不接受 SQLite（約束 5）
    "categoryId": "company"
  },
  "load": {
    "virtualUsers": 50,
    "warmupSeconds": 30,
    "durationSeconds": 120,
    "model": "Closed"                    // Closed | Open
  },
  "auth": {
    "tokenStrategy": "PerUser",          // PerUser | Shared（見階段 1 的 token 策略）
    "userPoolSize": 50
  },
  "scenarios": [
    { "name": "Login",    "enabled": true,  "weight": 1 },
    { "name": "GetList",  "enabled": true,  "weight": 5 },
    { "name": "GetData",  "enabled": true,  "weight": 5 },
    { "name": "Save",     "enabled": true,  "weight": 1 },
    { "name": "DefineRead", "enabled": true, "weight": 3 }
  ],
  "report": {
    "console": true,
    "markdown": true,
    "json": true,
    "outputDirectory": "artifacts/loadtest",
    "percentiles": [50, 95, 99]
  }
}
```

**連線字串不進設定檔** —— 走既有的 `BEE_TEST_CONNSTR_{DBTYPE}` 環境變數慣例
（與 `./test.sh` 相同）。設定檔會被貼進 issue 或報告裡，機密不該在其中
（見 `.claude/rules/security.md`）。

一份 `loadtest.sample.json` 入版控當範例；實際使用的設定檔不入版控。

**設定內容原樣寫進報告的中繼資料區** —— 報告要能自證是用什麼參數跑出來的，
否則兩份報告放在一起無法比較。

## 階段 3：報告與取數規範

### 報告：三層輸出

| 輸出 | 給誰 | 位置 |
|------|------|------|
| Console 摘要 | 跑的人當下看 | stdout |
| Markdown 報告 | 人讀、貼進 issue / 文件 | `artifacts/loadtest/<時間戳>.md` |
| JSON | 程式讀，供日後比較 | `artifacts/loadtest/<時間戳>.json` |

`artifacts/` 已在 `.gitignore` 內，所以**報告預設不入版控** —— 探索性的跑動佔多數，
不值得每次都留。**要保留的那幾份手動複製到 `docs/repo-ops/`**（維運文件，非公開文件）。

`System.Text.Json` 是 BCL，JSON 那層不會引入外部套件。

### 報告一定要帶的中繼資料

**沒有這些，數字不能解讀，報告等於廢紙。** 這是報告設計裡唯一不可妥協的部分：

- **時間**：執行日期時間
- **版本**：框架版號（`Version.props`）、git commit
- **環境**：provider + DB 版本、OS、CPU 核數、RAM
- **模式**：Local / Remote、`ApiProtectionLevel`、body codec
- **負載參數**：VU 數、warm-up 時長、正式期時長、**封閉或開放模型**
- **結果**：p50 / p95 / p99 / max、RPS、錯誤數與錯誤類型分佈
- **快取**：命中率、`Set` 次數（single-flight 收斂證據）

錯誤數**必須一起報**：一份延遲很漂亮但半數請求失敗的報告，只看延遲會得到相反的結論。

### 報告樣板

```markdown
# 壓測報告 2026-09-07 14:30

| 項目 | 值 |
|------|-----|
| 框架版本 | 4.29.0 (commit 112af43c) |
| 環境 | SQL Server 2022 / macOS 15.6 / 10 core / 32 GB |
| 模式 | Remote, Encrypted, MessagePack |
| 負載 | 50 VU, warm-up 30s, 量測 120s, 封閉模型 |

## 結果

| 場景 | p50 | p95 | p99 | max | RPS | 錯誤 |
|------|-----|-----|-----|-----|-----|------|
| Login | … | … | … | … | … | 0 |

## 快取

| 指標 | 值 |
|------|-----|
| 命中率 | … |
| Set 次數 / VU 數 | … / 50（single-flight 收斂則遠小於 VU 數） |
```

### 取數規範

寫進 `docs/repo-ops/`：取數前置條件（哪個 provider、哪台機器、warm-up 多久、VU 數）、
報告保留原則、以及明確排除項 —— **不進 CI**。

### 為什麼不進 CI

壓測數字在 CI runner 上噪音太大，當閘門只會製造 flaky，而 flaky 閘門的下場是被忽略或被關掉。
手動觸發、結果記進文件即可。

### 數字怎麼寫才不會漂

結果一律記成「**當時量到什麼**」（附環境、版本、provider、日期），
不寫成「本框架吞吐為 X」。後者是複寫，必漂，且沒有任何機制會發現它過期
（見 `~/.claude/rules/single-source.md`）。

公開文件（README / ADR / `docs/` 下對外文件）**不引用本 plan**，
需要對外交代效能特性時另行升格成 ADR 或對外文件。

## 不在本 plan 範圍

- **微效能基準（BenchmarkDotNet）** —— 那是另一件事（量單一方法的 ns 級成本），
  與端到端壓測的目的、工具、取數方式都不同。要做另開 plan。
- **調優** —— 本 plan 只建立「量得準」的能力，不承諾改善任何數字。
  量出瓶頸後要不要調、怎麼調，是後續決策。
- **跨次比較與回歸偵測** —— 自動比對本次與 baseline、超標就報警，需要固定的 baseline
  檔與入版控策略。JSON 輸出已為它預留，但本 plan 只做到「產出可比較的資料」，
  不做比較機制本身。
