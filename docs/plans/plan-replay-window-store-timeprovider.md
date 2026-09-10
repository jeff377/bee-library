# 計畫：`MemoryReplayWindowStore` 改吃 `TimeProvider`，消除測試裡的 `Thread.Sleep`

**狀態：✅ 已完成（2026-09-10）**

## 背景

2026-09-10 清 SonarCloud issue 的收尾。32 筆 open issue 修掉 30 筆，剩兩筆判斷題，
本 plan 處理其中一筆：

- **`csharpsquid:S2925`（Do not use `Thread.Sleep()` in a test）**，全 repo 唯一一筆：
  [`tests/Bee.Api.Core.UnitTests/JsonRpc/MemoryReplayWindowStoreTests.cs:94`](../../tests/Bee.Api.Core.UnitTests/JsonRpc/MemoryReplayWindowStoreTests.cs)
  的 `SweepIfDue_IdleEntries_AreEvicted`，用 `Thread.Sleep(30)` 等淘汰期過去。
- 另一筆 `S107`（`RepositoryFactory` 建構子 8 參數）不在本 plan 範圍。

受測型別 [`src/Bee.Api.Core/JsonRpc/MemoryReplayWindowStore.cs`](../../src/Bee.Api.Core/JsonRpc/MemoryReplayWindowStore.cs)
直接讀 `Environment.TickCount64` 三處（`GetOrAdd` L33、`SweepIfDue` L45、`Entry` 建構子 L82），
淘汰期由 `ApiServiceOptions.WireFrameTimestampTolerance × 2` 推導（L40）。

> **提醒**：SonarAnalyzer **只在完整模式 CI 跑**（commit message 帶 `[all-db]` 或手動
> dispatch `db_scope=all`），精簡模式與本機 strict build 都不含它。本機重現要暫時加
> `SonarAnalyzer.CSharp` 的 `PackageReference`，見
> [`docs/repo-ops/gotchas/test-ci-release.md`](../repo-ops/gotchas/test-ci-release.md)。

## 先排除三個方向

| 方向 | 為何不走 |
|------|---------|
| `FrameworkClock`（`src/Bee.Base/FrameworkClock.cs`） | 那是**行事曆／時區**時鐘（`Today(timeZoneId)` / `Now(timeZoneId)`），給業務日期用。這裡要的是**單調經過時間**，語意不同，硬套會把時區問題帶進一個與時區無關的機制 |
| `DateTime.UtcNow` | 會被系統時間調整與 NTP 校正拉動。現行 `Environment.TickCount64` 的單調性正是這個機制要的，任何替代方案都必須保留 |
| 只把 `Thread.Sleep(30)` 換成 `await Task.Delay(30)` | S2925 只認 `Thread.Sleep`，換成 `Task.Delay` 規則就不 hit —— 但那是**繞過掃描**，測試依然靠真實時間流逝、依然可能因排程延遲而 flaky，且下面列的四項「測不到的行為」一項也沒解決。不採用 |

## 已實測驗證的前提（不是照 prompt 照做）

以 .NET 10 SDK 在本機（darwin arm64）實測，2026-09-10：

| 驗證項 | 結果 |
|--------|------|
| `TimeProvider.System.GetTimestamp()` 是否即 `Stopwatch.GetTimestamp()` | ✅ 兩者同源，`TimestampFrequency == Stopwatch.Frequency`（本機 1e9），兩次呼叫相差 0.001ms。**單調性與「不受系統時間調整影響」的性質保住** |
| `GetElapsedTime(start, end)` 是否等於實際經過 | ✅ `Thread.Sleep(20)` 量到 30.30ms（真實排程延遲，符合預期） |
| `FakeTimeProvider`（`Microsoft.Extensions.TimeProvider.Testing` 10.10.0）不 `Advance` 時是否凍結 | ✅ 兩次 `GetTimestamp()` 完全相同 |
| `Advance(10min)` 後 `GetElapsedTime` | ✅ 精確等於 `TimeSpan.FromMinutes(10)`，再 `+1ms` 得 `00:10:00.0010000` |
| 邊界解析度 | ✅ `Advance(life)` 後 `elapsed >= life` 為真且 `> life` 為假；再 `Advance(1 tick)` 後 `> life` 為真。**可精確測到 1 tick 的淘汰邊界** |
| 併發 `Advance` 與 `GetTimestamp`（200k 次讀 vs 200 次 Advance） | ✅ 時間戳倒退次數 0 |
| 量級是否有溢位風險 | ✅ 本機 `Stopwatch.GetTimestamp()` ≈ 1.5e13，`long.MaxValue` ≈ 9.2e18。1GHz 頻率下需約 292 年開機時間才溢位 |

結論：**`TimeProvider` 是對的接縫**，且 `GetTimestamp()` / `GetElapsedTime()`
（不是 `GetUtcNow()`）是必須用的那一組。

## Q1：這值不值得做？—— 值得，但理由不是「免掉一次 `Thread.Sleep`」

免掉 30ms 與一個 flaky 來源只是附帶。真正的收穫是**四項目前完全測不到的行為變成可測**，
外加**一項規範違反被消除**：

### 現在測不到、換接縫後可測

1. **淘汰邊界的嚴格不等式。** 現行判定是 `LastTouchedMs < cutoff`，即
   「閒置**超過**淘汰期才淘汰、恰好等於淘汰期要留著」。現行測試把 tolerance 壓到 1ms、
   睡 30ms，離邊界十萬八千里，這條語意等於沒測。換 fake clock 後可精確釘：
   `Advance(lifetime)` 後 entry 仍在、再 `Advance(1 tick)` 後才消失。
2. **sweep 節流 `_nextSweepAtMs`。** 現行測試的註解自己寫了「不寫死數字以免對 sweep 的內部
   節流過度耦合」—— 那是**放棄測它**的委婉說法。節流是記憶體有界性的一半（另一半是淘汰）：
   它保證 sweep 每個 lifetime 最多跑一次，也保證有存取時至少跑一次。fake clock 下這兩面都能
   直接斷言（`Advance(lifetime - 1 tick)` 後存取，閒置 entry 仍在；補到 `lifetime` 後才消失）。
3. **「使用中的 session 永不被淘汰」。** `GetOrAdd` 命中既有 token 時會刷新 `LastTouchedMs`，
   所以一個持續有流量的 session 不論行程跑多久都不該被掃掉。這條**目前零測試**，而它與
   已修過的 CON-5 是**同一類 bug**：淘汰掉使用中的 window，等於該 session 的重放防護靜默重置
   （見 [`docs/changelogs/4.28.0.zh-TW.md`](../changelogs/4.28.0.zh-TW.md)）。這是安全控制，
   值得有閘門。
4. **sweep 先於 GetOrAdd 的順序。** `GetOrAdd` 是先 `SweepIfDue()` 再取 entry，因此本次要拿的
   token 是在 sweep **之後**才蓋時間戳，不可能被自己這一次呼叫掃掉。fake clock 下可直接驗。

### 換接縫**買不到**的（誠實說明）

- **`Interlocked` 單一 sweeper 的競爭測不到。** 併發與時間正交，換時鐘不改變這點；
  `_sweeping` 仍然沒有確定性的測法。
- **`Entry` 預設 0 的 race 仍測不到。** 該 race 需要「sweep 與 entry 插入交錯」，fake clock
  只凍結時間、不排程執行緒。現行那筆反射結構性斷言（`Entry_IsStampedAtConstruction_NotAfterwards`）
  的存在理由不變，只是形式要跟著調整（見 Q3）。

### 額外收穫：消掉一筆規範違反

現行測試為了把淘汰期壓到毫秒級，**改寫 production static**
`ApiServiceOptions.WireFrameTimestampTolerance`，因而整個測試類別必須掛
`[Collection("ApiServiceOptionsState")]`（`.claude/rules/testing.md` 第 3 條所限制的事）。

fake clock 下不必再壓 tolerance —— 直接 `Advance` 十分鐘即可，時間是假的、不花任何真實秒數。
於是：**該類別不再改寫任何 production static，`[Collection]` 標記可以拿掉**。
（本組件已整體關閉平行，所以這不是平行度的收穫，是規範面的收穫；詳見下方測試調整那節的註。）

### 成本

- 一個 `internal` 建構子多載 + 一個 `TimeProvider` 欄位（見 Q2）。
- 測試專案多一個套件相依 `Microsoft.Extensions.TimeProvider.Testing`（Microsoft 第一方）。
  `Bee.Api.Core` 本身**不新增任何套件**（`TimeProvider` 是 BCL）。
- 一次完整模式 CI 才看得到 S2925 關閉。

**判定：做。** 四項可測行為裡有兩項（第 1、3 點）直接關乎「重放防護不會被靜默削弱」，
不是為了讓掃描變綠。

> 若使用者判定不做：在 SonarCloud UI 標 **Won't Fix**，理由寫
> 「淘汰為時間驅動，受測型別直接讀 `Environment.TickCount64`；在未引入時鐘接縫前，
>  等待真實時間流逝是唯一測法。已評估 `TimeProvider` 接縫（見本 plan），決定暫不引入。」
> —— 這是有效結論，只是會把上述四項留在測不到的狀態。

## Q2：接縫放哪一層 —— 只放 `MemoryReplayWindowStore`，不動 `IReplayWindowStore`

`IReplayWindowStore` 的契約是「給我這個 session 的 window」，**時間是實作細節**：

- 共享式實作（Redis / 資料庫）會用 server 端 TTL 或既有的過期機制，收一個 `TimeProvider`
  對它毫無意義，等於在契約上塞進一個只有記憶體實作用得到的參數。
- `IReplayWindowStore` 已在 `PublicAPI.Shipped.txt`（L55-56）且**有外部實作者**
  （介面存在的理由就是讓多節點部署換掉它，見
  [`docs/adr/adr-042-api-replay-protection.md`](../adr/adr-042-api-replay-protection.md)）。
  改介面是破壞性變更。

**結論：接縫只開在 `MemoryReplayWindowStore` 自己身上。**

### 公開 vs internal：建議走 `internal`

`MemoryReplayWindowStore() -> void` 已在 `PublicAPI.Shipped.txt` L126。對既有 public 建構子
**加 optional 參數是二進位破壞性變更**（`.claude/rules/commit-verification.md`），所以只能加多載。
但多載該不該是 public，有兩個選項：

| 選項 | 內容 | 評估 |
|------|------|------|
| **A（建議）** `internal MemoryReplayWindowStore(TimeProvider clock)` | 靠 `Bee.Api.Core.csproj` **已宣告**的 `InternalsVisibleTo("Bee.Api.Core.UnitTests")` | 拿到 100% 的可測性，**公開 API 表面零成長**、`PublicAPI.*.txt` 完全不動、沒有永久契約要維護。外部消費者不需要注入時鐘 —— 他們要換行為是換掉整個 `IReplayWindowStore` |
| B `public MemoryReplayWindowStore(TimeProvider clock)` | 同時申報到 `PublicAPI.Unshipped.txt` | 把「可注入時鐘」當成出貨功能。多一條永久契約，但目前找不到會用它的外部情境 |

**建議 A。** 若使用者偏好 B，則需在 `PublicAPI.Unshipped.txt` 補
`Bee.Api.Core.JsonRpc.MemoryReplayWindowStore.MemoryReplayWindowStore(System.TimeProvider! clock) -> void`，
且 commit message 說明「新增多載、既有無參數建構子簽章不變 → 二進位相容」。

無參數建構子一律委派：`public MemoryReplayWindowStore() : this(TimeProvider.System) { }`。

## 實作範圍

### 1. `src/Bee.Api.Core/JsonRpc/MemoryReplayWindowStore.cs`

| 現行 | 改為 |
|------|------|
| `private long _nextSweepAtMs`（絕對毫秒，初值 0） | `private long _lastSweepAtTimestamp`（絕對時間戳），**建構時初始化為 `clock.GetTimestamp()`** |
| `private static long LifetimeMs => tolerance.TotalMilliseconds * 2` | `private static TimeSpan Lifetime => tolerance * 2`（仍是 static property，**每次 sweep 現讀**，不快取 —— tolerance 是可變 static） |
| `Environment.TickCount64`（三處） | `_clock.GetTimestamp()` |
| `Entry.LastTouchedMs` | `Entry.LastTouchedTimestamp`（型別仍是 `long`，只換單位） |
| `LastTouchedMs < cutoff` | `_clock.GetElapsedTime(last, now) > Lifetime` |
| `now < _nextSweepAtMs` → return | `_clock.GetElapsedTime(_lastSweepAt, now) < Lifetime` → return |
| `public Entry() => LastTouchedMs = Environment.TickCount64;` | `Entry(long touchedAt)` —— 見下 |

**必須保住的語意等價**：現行 `last < now - Lifetime` 等價於 `now - last > Lifetime`，
所以判定式是 `elapsed > Lifetime`（**大於**，不是 `>=`）。閒置時間恰好等於 lifetime 的 entry
留著。寫成 `>=` 會是無聲的行為變更。

**`_lastSweepAtTimestamp` 初值**：現行 `_nextSweepAtMs` 初值 0 使**第一次存取必定 sweep**，
但那次 sweep 面對的是空字典，是純 no-op。改為建構時蓋上 `clock.GetTimestamp()`（等價於
「剛掃過」），行為上完全等價且避開一個 0 sentinel。這是刻意的小調整，會寫進註解。

**`Entry` 建構子**：改成 `private sealed class Entry(long touchedAt)`。
如此「不得預設為 0」的不變式從**執行期靠人記得**升級為**編譯期強制** ——
沒有無參數建構子可用，就沒有留 0 的寫法。L71-80 那段 `WARNING:` 註解要改寫成
「此不變式現由建構子簽章強制」，不能留著描述一個已不存在的形狀。
（替代寫法：`Entry(TimeProvider clock)` 自己蓋章，可讓現行反射測試形狀維持不變，
但把時鐘欄位塞進每個 entry，且不變式回到執行期。不建議。）

**效能**：sweep 迴圈裡每筆多一次 `GetElapsedTime`（一次 64 位元乘除）。相對於
`ConcurrentDictionary` 列舉可忽略；換取的是不必自行做 frequency → 毫秒的換算
（本機頻率是 1e9、Windows 是 1e7，手算容易出錯）。

### 2. `tests/Bee.Api.Core.UnitTests/Bee.Api.Core.UnitTests.csproj`

加 `<PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" Version="10.10.0" />`。

（替代：在 `tests/Bee.Tests.Shared/` 手寫一個 ~15 行的 `TestTimeProvider : TimeProvider`
只覆寫 `GetTimestamp` / `TimestampFrequency` / `GetUtcNow`，免掉套件相依。但
`FakeTimeProvider` 是 Microsoft 第一方、行為已在上表實測、零維護成本，且本 repo 已用
多個 `Microsoft.Extensions.*` 10.x 套件。建議用套件。）

### 3. `tests/Bee.Api.Core.UnitTests/JsonRpc/MemoryReplayWindowStoreTests.cs`

| 測試 | 處置 |
|------|------|
| `GetOrAdd_SameToken_ReturnsSameWindow` | 不動（不涉時間） |
| `GetOrAdd_DifferentTokens_AreIsolated` | 不動 |
| `Entry_IsStampedAtConstruction_NotAfterwards` | **改寫斷言**：`Entry` 不得有無參數建構子（反射確認 `GetConstructor(nonPublic, Type.EmptyTypes)` 為 null），並在 `<remarks>` 說明不變式已升級為編譯期強制、原本的執行期斷言隨之退場。原註解裡「為什麼用反射而不是行為測試」那段保留 —— 那個判斷仍然成立 |
| `SweepIfDue_IdleEntries_AreEvicted` | **重寫**：`FakeTimeProvider` + `Advance(lifetime + 1 tick)`，**移除 `Thread.Sleep`、移除對 `WireFrameTimestampTolerance` 的改寫與 try/finally** |
| 新增 `SweepIfDue_IdleExactlyLifetime_IsKept` | 淘汰邊界（`Advance(lifetime)` 後仍在） |
| 新增 `SweepIfDue_BeforeThrottleWindow_DoesNotSweep` | 節流：`Advance(lifetime - 1 tick)` 後存取，閒置 entry 仍在 |
| 新增 `GetOrAdd_ActiveToken_IsNeverEvicted` | 反覆 `Advance(lifetime × 0.9)` + `GetOrAdd(同一 token)` 數輪，該 token 的 window 始終是同一個實例（`Assert.Same`）且歷史未失（`TryAccept` 重複序號仍被拒） |
| 類別層 `[Collection("ApiServiceOptionsState")]` | **移除**（不再改寫該 static），類別 `<remarks>` 裡對應的說明一併改寫 |

> **平行度不會因此提升**：`tests/Bee.Api.Core.UnitTests/AssemblyInfo.cs` 已宣告組件層
> `[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]`（該檔檔頭記著
> CI build #31169045420 的污染事故），整個組件本來就串行。移除 `[Collection]` 的收穫
> **只有**「這個類別不再改寫 production static」這一項 —— 但那正是
> `.claude/rules/testing.md` 第 3 條在意的事。上面「回到可平行執行」的說法不適用於本組件。

## Q4：併發正確性 —— 既有保證一項都不弱化

| 既有保證 | 換時間來源後 |
|---------|-------------|
| `Volatile.Write/Read(ref entry.LastTouchedMs)` | **原樣保留**。欄位型別仍是 `long`（只換單位），32 位元平台上 64 位元讀寫的原子性仍需 volatile，不得改成裸讀寫 |
| `Volatile.Write/Read(ref _nextSweepAtMs)` | **原樣保留**，改名為 `_lastSweepAtTimestamp`，volatile 存取不變 |
| `Interlocked.Exchange(ref _sweeping, 1)` 單一 sweeper + `finally` 歸零 | **完全不動** |
| 時間來源本身執行緒安全 | `TimeProvider.System.GetTimestamp()` 即 `Stopwatch.GetTimestamp()`，無狀態、執行緒安全。`FakeTimeProvider` 在併發 `Advance` 下實測 200k 次讀取零倒退（見上表） |
| 時鐘欄位的安全發佈 | `private readonly TimeProvider _clock`，建構子指派，`readonly` 保證安全發佈 |
| `Lifetime` 每次現讀可變 static | **維持現行行為**，不在建構子快取 —— 快取會讓 runtime 調整 tolerance 對既存 store 無效，那是行為變更 |

**唯一新增的併發面**：sweep 迴圈裡 `now` 在迴圈外取一次（現行也是），迴圈中每筆用同一個
`now` 比對。這點不變。

## 執行步驟

1. 改 `MemoryReplayWindowStore.cs`（含註解改寫）。
2. 決定 Q2 的 A/B；若 B 則補 `PublicAPI.Unshipped.txt`。
3. 測試專案加套件。
4. 改寫 / 新增測試共六筆，移除 `[Collection]`。
5. `dotnet build --configuration Release`（strict，警告即失敗）。
6. `./test.sh tests/Bee.Api.Core.UnitTests/Bee.Api.Core.UnitTests.csproj`。
7. **本機確認 S2925 已消失**：依
   [`docs/repo-ops/gotchas/test-ci-release.md`](../repo-ops/gotchas/test-ci-release.md)
   暫時加 `SonarAnalyzer.CSharp` 的 `PackageReference` 建一次，確認後**移除**該暫時參考。
8. commit。**push 前先問使用者要不要跑完整模式**（`.claude/rules/testing.md`）：
   本次改動不碰 DB / SQL 產生邏輯，正確性用精簡模式即足；但 **SonarAnalyzer 只在完整模式跑**，
   要看到 S2925 真的關閉就需要一次 `[all-db]`。建議帶 `[all-db]`。

## 驗收標準

- [ ] `MemoryReplayWindowStoreTests.cs` 不再出現 `Thread.Sleep`。

      > 全 repo 掃過：`tests/` 下另有三處 `Thread.Sleep`，**都不是 S2925 的對象**，本 plan 不動 ——
      > `CacheSingleFlightTests.cs:37,62` 在測試用的 `SlowObjectCache` / `SlowKeyObjectCache`
      > 覆寫的 `CreateInstance` 裡（S2925 只認帶測試屬性的方法本體）、
      > `Bee.Tests.Shared/CrossProcessLock.cs:59` 是共用 helper 的輪詢、
      > `MasterKeyProviderRetryTests.cs:16` 只是註解文字。
- [ ] `MemoryReplayWindowStoreTests` 六筆全綠，且**不含任何 `Thread.Sleep` / `Task.Delay` /
      真實等待**，整個類別執行時間為毫秒級。
- [ ] 該測試類別不再改寫任何 production static，`[Collection]` 已移除。
- [ ] `Release` strict build 零警告。
- [ ] 淘汰語意未變：閒置**恰好等於** lifetime 留著、**超過**才淘汰（由新測試釘住）。
- [ ] 完整模式 CI 的 SonarCloud 上 `csharpsquid:S2925` 歸零。

## 相關

- [`src/Bee.Api.Core/JsonRpc/MemoryReplayWindowStore.cs`](../../src/Bee.Api.Core/JsonRpc/MemoryReplayWindowStore.cs)
- [`src/Bee.Api.Core/JsonRpc/IReplayWindowStore.cs`](../../src/Bee.Api.Core/JsonRpc/IReplayWindowStore.cs)
- [`docs/adr/adr-042-api-replay-protection.md`](../adr/adr-042-api-replay-protection.md)
- [`docs/changelogs/4.28.0.zh-TW.md`](../changelogs/4.28.0.zh-TW.md) —— `Entry.LastTouchedMs` 那筆修正
- [`docs/repo-ops/gotchas/test-ci-release.md`](../repo-ops/gotchas/test-ci-release.md) —— 本機重現 SonarAnalyzer

## 執行結果（2026-09-10）

採 **Q2 選項 A**：`internal MemoryReplayWindowStore(TimeProvider clock)`，
`PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` **一個字都沒動**，公開 API 表面零成長。

實際改動四個檔，與動筆前宣告一致，無範圍外異動：

| 檔案 | 改動 |
|------|------|
| `src/Bee.Api.Core/JsonRpc/MemoryReplayWindowStore.cs` | `TimeProvider` 接縫、`_lastSweepAtTimestamp`、`Entry(long touchedAt)` |
| `tests/Bee.Api.Core.UnitTests/Bee.Api.Core.UnitTests.csproj` | `Microsoft.Extensions.TimeProvider.Testing` 10.10.0 |
| `tests/Bee.Api.Core.UnitTests/JsonRpc/MemoryReplayWindowStoreTests.cs` | 4 筆 → 7 筆，移除 `[Collection]` |
| `docs/plans/plan-replay-window-store-timeprovider.md` | 本節 |

### 變異測試：確認新測試不是空轉

新增的三筆斷言各自對應一個能讓它變紅的變異，**各只有一筆測試紅**：

| 變異 | 變紅的測試 |
|------|-----------|
| 淘汰判定 `> lifetime` 改成 `>= lifetime` | `SweepIfDue_IdleExactlyLifetime_AreKept` |
| 拿掉 `SweepIfDue` 的節流 early return | `SweepIfDue_WithinThrottleWindow_DoesNotSweep` |
| 拿掉 `GetOrAdd` 的 `Volatile.Write` 時間戳刷新 | `GetOrAdd_ActiveToken_IsNeverEvicted` |

> **節流測試第一版是空轉的**，變異測試當場抓到：原本只 `Advance(lifetime - 1 tick)` 就斷言，
> 但那一刻 entry 的閒置時間還沒超過淘汰期，掃不掃結果都一樣。正解是先走滿一個淘汰期觸發
> 一次 sweep 把節流原點推到「此刻」，**再**走「差 1 tick」—— 此時 entry 已閒置近兩倍淘汰期、
> 早該被掃掉，留著它的就只剩節流。**這正是為什麼新增的閘門要跑一次變異測試**：
> 一個恆綠的測試比沒有測試更糟。

### 驗證

- clean Release build（全 solution，`--no-incremental`）：0 警告 0 錯誤。
- `./test.sh` 全綠：17 個測試專案、0 失敗（1 筆既有 skip），四個 DB 容器皆在跑。
- 本類別 7 筆耗時 **7~10 ms**，無任何真實等待。
- 本機加 `SonarAnalyzer.CSharp` 10.34.0 重現：**S2925 已消失**，且改動後的
  `MemoryReplayWindowStore.cs` 零 Sonar 警告。暫時參考已移除。
  - 順帶量到該測試專案有 **282 筆 S8969**（多餘的 `!`）等既有樣態，**不在 SonarCloud 的
    quality profile 內**（否則上一輪清理不會只剩兩筆），非本次引入，未處理。
