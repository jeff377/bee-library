# 計畫：修復 Bee.Northwind 登入中斷（缺 `st_session` / `st_user`）

**狀態：✅ 已完成（2026-07-30）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | Northwind 建立框架必要的 common 表，登入恢復 | ✅ 已完成（2026-07-30） |
| 2 | common 表清單改為資料驅動 + 啟動 fail-fast 驗證 | ✅ 已完成（2026-07-30） |
| 3 | debug 模式透傳基礎設施例外訊息 | ✅ 已完成（2026-07-30） |

## 背景

### 症狀

`apps/Bee.Northwind` 目前**四個 head 全部無法登入**。server 起得來、`system.Ping` 通、
種子資料也灌得進去（8 分類 / 15 商品 / 5 訂單），但以 `demo` / `demo` 登入一律回：

```
API error: -32000 - Internal server error
```

server 端**沒有任何對應的 log**。唯一的旁證是啟動後每個 tick 都出現的警告：

```
warn: Bee.Hosting.Session.ExpiredSessionCleanupService[0]
      Expired session cleanup failed; will retry on the next tick.
      SQLite Error 1: 'no such table: st_session'.
```

四個 head（Desktop / Browser WASM / iOS / Android）共用 `Bee.Northwind.UI` 的同一條登入路徑，
所以這不是單一平台問題，是整個 demo 進不去。

### 根因

同一天的兩筆框架 commit 各自為 `Login` 新增了一項 common 資料庫依賴，而 Northwind 的建表清單
沒有跟著更新：

| commit | 日期 | 對 `Login` 新增的依賴 |
|--------|------|---------------------|
| `ee4d2fd0` feat(session) | 2026-07-30 | Login 改為寫入 session 重建種子 → `INSERT INTO st_session` |
| `908d6214` feat(identity) | 2026-07-30 | `ApplyUserLocale` 讀使用者的 culture / time_zone → `SELECT FROM st_user` |

[`NorthwindSchemaSeeder.cs`](../../../apps/Bee.Northwind/Bee.Northwind.Server/NorthwindSchemaSeeder.cs)
的 `s_frameworkTables` 只有 `st_cache_notify` 一張表，且該檔案上次修改是 2026-06-14
（`bd6d2321`），趕不上今天的框架異動。兩張表因此從未被建立。

`82333926`（對定義檔掛上框架 analyzer）不是元凶 —— 它只改了 csproj 與 plan 文件。

### 為什麼會沉默失敗

三個因素疊在一起，讓一個「缺表」等級的問題查起來像框架 bug：

1. **Northwind 的建表清單是硬編字串陣列**，框架新增依賴時沒有任何編譯期或啟動期訊號。
2. **Northwind 不在 `Bee.Library.slnx` 與 CI path filter 內**，框架改動不會觸發任何 Northwind 驗證。
3. **基礎設施例外在 server 端完全沒有落點**：[`JsonRpcExecutor.cs`](../../../src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs)
   的 catch 只走 `Tracer` 與 `LogApiFailureAnomaly`，而後者被 `AnomalyEnabled`（稽核選項）閘住。
   Northwind 沒開稽核，所以 `SqliteException` 連同堆疊一起消失，客戶端只拿到遮蔽後的
   `Internal server error`。訊息遮蔽本身是對的（避免洩漏內部細節），缺的是**遮蔽前先記一筆**。

### 已完成的實測驗證

本 plan 的修正方向已在本機端到端驗證過（改動後已還原，工作區乾淨）：

- 把 `st_session` + `st_user` 加進 materialize filter 與 `s_frameworkTables` → 登入成功
  → `Category.GetList` 8 列 / `Product.GetList` 15 列 / `Order.GetList` 5 列
  → `Order.GetData` 讀出 1 筆主檔 + 2 筆明細。`ExpiredSessionCleanupService` 的警告一併消失。
- **只加 `st_session` 不夠**：實測仍然失敗，因為 `ApplyUserLocale` 無條件查 `st_user`。
- `st_user` **建空表即可** —— `UserRepository.GetLocale` 查無列時回 `UserLocale.Empty`
  並 fallback 到部署預設，Northwind「免 `st_user` 認證」的設計不受影響。

建置面另有一項與本問題無關的環境雷：iOS head 因 .NET for iOS SDK 要求 Xcode 26.5、本機為 26.6
而擋下，加 `-p:ValidateXcodeVersion=false` 後 0 錯誤。不在本 plan 範圍。

## 階段 1：Northwind 建立框架必要的 common 表

讓登入恢復可用。最小、可獨立交付、可立即驗證。

**改動點**

- [`NorthwindBackend.cs`](../../../apps/Bee.Northwind/Bee.Northwind.Server/NorthwindBackend.cs)：
  `Defaults.MaterializeTo` 的 filter 放行 `TableSchema/common/st_session.TableSchema.xml`
  與 `TableSchema/common/st_user.TableSchema.xml`。
- [`NorthwindSchemaSeeder.cs`](../../../apps/Bee.Northwind/Bee.Northwind.Server/NorthwindSchemaSeeder.cs)：
  `s_frameworkTables` 加入 `"st_session"` 與 `"st_user"`，並更新其上方註解
  （目前寫「Currently just st_cache_notify」）說明這兩張表的用途。

**不做**：不灌 `st_user` 的種子列。Northwind 以
`NorthwindAuthenticatingSystemBusinessObject` 認證，空表是刻意的，寫死一列反而讓 demo
的「免 `st_user` 認證」示範失真。

**驗收**：刪除 `northwind.db` 重跑 server → 啟動無 `st_session` 警告 → `demo`/`demo` 登入成功
→ Category / Product / Order 清單與 Order master-detail 皆可讀取。

## 階段 2：common 表清單改為資料驅動 + 啟動 fail-fast 驗證

階段 1 只是把清單補到「今天正確」。這個階段處理「明天框架再加一張表時不會再沉默」。

**兩件事一起做才有效**

1. **清單資料驅動**：`s_frameworkTables` 與 materialize filter 的硬編字串改為列舉
   `Defaults.ListEmbedded()` 中 `TableSchema/common/` 前綴的全部項目。框架未來新增 common 表時，
   Northwind 下次啟動自動建立，不需要有人記得回來改這裡。
2. **啟動 fail-fast**：`UseNorthwindBackend` 在 seeder 之後驗證每張應建的表確實可查詢，
   缺任何一張就 throw 並列出缺失表名。讓「該建卻沒建」在啟動當下就爆，而不是等到某條 API
   路徑第一次踩到才回一句 `-32000`。

**取捨（需 review）**

資料驅動會一併建出 `st_company`、`st_user_company`、`st_define`、`st_api_key` 等 Northwind
目前用不到的空表（它以自訂 `ICompanyInfoService` 繞過公司表）。代價是 demo 的 SQLite 檔多幾張
空表，教學敘事上稍微鬆了「這個 demo 只需要這些表」的說法；換到的是這類斷裂不再復發。

替代方案是維持顯式清單、只做 fail-fast —— 但那樣的檢查是拿清單驗自己，清單漏了就一起漏，
擋不住本次這種情況。**建議採資料驅動**，並在註解寫清楚為何額外的空表是刻意的。

**驗收**：刪除 `northwind.db` 重跑 → 表全數建立、登入正常；手動刪掉其中一張表後重啟 →
啟動即失敗並指名缺哪張表。

## 階段 3：debug 模式透傳基礎設施例外訊息

這是唯一動到 `src/` 的階段，也是讓下次同類問題「五分鐘查完而不是半小時」的關鍵。

**問題**：[`JsonRpcExecutor.cs`](../../../src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs) 的 catch 走
`MapException` 把非使用者面例外壓成 `Internal server error` 後回傳，但除了受稽核選項閘住的
`LogApiFailureAnomaly` 之外沒有其他落點。稽核關閉時 = 完全靜默。
[`ApiServiceController`](../../../src/Bee.Api.AspNetCore/Controllers/ApiServiceController.cs)
雖有 development 模式帶原訊息的相同取捨，但 executor 自己 catch 後回傳錯誤回應，例外從未往上拋，
那層根本沒有機會觸發。

**採用做法**：`MapException` 在 `SysInfo.IsDebugMode` 為真時透傳原始例外訊息，錯誤碼維持
`-32000`。`SysInfo` 屬 `Bee.Base`、由各 host 於啟動時以 `CommonConfiguration` 灌入，
`Bee.Api.Core` 本來就依賴它 —— **零新增套件相依、零公開表面變更**。

> 原先評估的是注入 `ILogger<JsonRpcExecutor>`，但那需要為 `Bee.Api.Core` 新增
> `Microsoft.Extensions.Logging.Abstractions`，且在公開 ctor 加選擇性參數會移除已 shipped 的
> 簽章（PublicAPI baseline 直接以 RS0017 擋下）—— 對一個診斷性改善而言代價過高。改用既有的
> debug 旗標後兩項代價都消失。差別是：這解的是**開發期**可見性；正式環境的基礎設施例外仍然
> 只在稽核開啟時才有記錄，那是另一個獨立議題。

**守住的界線**：只在 debug 模式透傳，且只傳 message、不含堆疊 —— 對應 `rules/scanning.md`
的「敏感資訊外洩」條款。正式環境行為完全不變。

**測試連帶**：`MapException` 的結果自此取決於 `SysInfo.IsDebugMode` 這個 process-wide 靜態。
原本兩條斷言遮蔽訊息的測試沿用環境值，而測試 fixture 本身跑在 debug 模式（`tests/Define` 的
`SystemSettings`）—— 改動後立刻紅，正確地暴露了「斷言依賴環境」這件事。兩條改為明確設定
`IsDebugMode = false`（要驗的是 production 行為），並補上 debug 模式的對應測試；
本組件新增 `SysInfoStaticCollection` 序列化這兩個測試類。

**驗收**：全套測試 15 個專案 0 失敗；Northwind 於執行期 drop 掉 `st_session` 後登入，
客戶端直接收到 `-32000 - SQLite Error 1: 'no such table: st_session'.`
（本次事件原本要靠逐張建表反推才問得出的那句話）。

## 未納入本次，但已知的缺口

**Northwind 沒有任何自動化回歸防護。** 它不在 `Bee.Library.slnx` 內，也不在 `build-ci.yml`
的 path filter 內 —— 框架端任何改動都不會觸發 Northwind 的建置或執行驗證。本次事件正是這個缺口
的具體代價：框架加了兩項 DB 依賴，唯一的 dogfooding 應用當天就壞掉，而 CI 全綠。

本次刻意不處理，理由是它會把一個 4 行的修復擴張成 CI 改造。階段 2 的 fail-fast 是這期間的
替代防護 —— 它把「沉默失敗」降級為「啟動即失敗」，但仍需要有人真的去跑 Northwind 才會看到。

後續若要補，方向是把本次驗證用的流程（connect → login → GetList → GetData）做成自動化 smoke
test，並讓框架改動觸發它；屆時需一併決定 slnx 與 path filter 的調整範圍。

## 風險

| 風險 | 評估 |
|------|------|
| 階段 1 改錯範圍 | 低。改動限於 Northwind 兩個檔案，已實測驗證過完整路徑。 |
| 階段 2 多建空表造成困惑 | 低但真實。已以 `GetFrameworkCommonTables` 的 remarks 說明取捨。 |
| ~~階段 3 新增套件相依~~ | 已消除 —— 改用既有的 `SysInfo.IsDebugMode`，不新增相依、不動公開表面。 |
| 階段 3 誤放寬錯誤訊息 | 已控制。透傳僅限 debug 模式且只含 message；正式環境路徑有明確測試覆蓋。 |

## 執行結果

三個階段均已落地，全套測試 15 個專案 0 失敗（1 skipped，屬既有的 RSA 條件測試）。
公開文件同步更新：`development-constraints`、`jsonrpc-frontend-integration` 與 CHANGELOG（皆雙語）。

階段 3 的最終做法與原案不同（見該節），是 review 時提出的更省做法。
