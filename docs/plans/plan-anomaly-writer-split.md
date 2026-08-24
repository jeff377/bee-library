# 計畫：稽核寫入與異常寫入拆成兩個介面

**狀態：✅ 已完成（2026-08-24）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 型別與接線一次到位：`AnomalyEntry` 基底 ＋ `IAnomalyLogWriter` 介面、實作類同時實作兩個介面、DI 註冊兩次、三支公開建構子換參數型別、消費端與測試替身跟著換（**破壞性變更全部落在這一階段**） | ✅ 已完成（2026-08-24） |
| 2 | ADR、術語表、公開文件、CHANGELOG、`Bee.Definition` 相依閉包驗證 | ✅ 已完成（2026-08-24） |

> **為什麼不切成「純加法」與「換簽章」兩階段**：純加法那半做完的中間狀態是
> 「兩個介面指向同一個實作，但沒有任何消費端用新介面」—— 不會單獨發版、下游也受益不到，
> 只是多一次 commit。切兩階段唯一有意義的情境是**先發一版、對舊參數掛 `[Obsolete]`
> 讓下游適應**，而本 plan 沒有選那條過渡路線（見「版號」）。

## 背景

`IAuditLogWriter`（`src/Bee.Definition/Logging/IAuditLogWriter.cs`）目前同時承載兩件性質不同的事：

- **業務稽核軌跡**：誰在什麼時候對哪一筆做了什麼（登入、異動、檢視）
- **執行異常記錄**：哪一次執行偏離了正常（API 層與 DB 層的 Error / Timeout / Slow / 大量列數 / 金鑰被拒）

而 [adr-040](../adr/adr-040-audit-trail-taxonomy.md) 決策二自己就把後者判成 observability：
「系統 / 錯誤本質是 observability（維運 / 除錯），歸 `ILogWriter` / host `ILogger`⋯**與業務稽核分離**」。
實作把兩者合在同一個介面上，是因為**寫入管線共用**（有上限佇列、批次、退路檔案、log 資料庫的
`DbAccess` 不做偵測），不是因為它們回答同一種問題。

### 觸發這件事的觀察

**七個消費端沿稽核／異常這條線乾淨二分，沒有任何一個同時寫兩種：**

| 只寫稽核 | entry 型別 | 所在組件 |
|---|---|---|
| `FormBusinessObject.Audit.cs:106` | `ChangeAuditEntry` | `Bee.Business` |
| `FormBusinessObject.Audit.cs:138` | `AccessAuditEntry` | `Bee.Business` |
| `SystemBusinessObject.Audit.cs:55` | `ChangeAuditEntry` | `Bee.Business` |
| `SystemBusinessObject.Session.cs:280` | `LoginAuditEntry` | `Bee.Business` |

| 只寫異常 | entry 型別 | 所在組件 |
|---|---|---|
| `DbAccess.Anomaly.cs:81` | `DbAnomalyEntry` | `Bee.Db` |
| `JsonRpcExecutor.cs:189` | `ApiAnomalyEntry` | `Bee.Api.Core` |
| `ApiServiceController.cs:234` | `ApiAnomalyEntry` | `Bee.Api.AspNetCore` |

**最強的一條證據是命名**：異常那三個消費端的欄位與參數**自己已經叫 `_anomalyWriter` /
`anomalyWriterFactory`** —— 程式碼在用命名補一個型別系統沒有表達的區分。

其餘三條：

1. **分層錯位**：介面住 `Bee.Definition.Logging`，而異常那條線的消費端是 `Bee.Db` 與 `Bee.Api.*`，
   它們拿到的是「整個稽核寫入器」卻只用得到異常那半。
2. **開關已經是分開的**：`AuditLogOptions.Enabled` 與 `AnomalyEnabled` 兩個，
   而 `SystemBusinessObject.DeploymentAuditEnabled()` 還刻意只看 `Enabled` 不看 `ChangeEnabled`，
   條件組合已經在三處各自長出來。
3. **`DbAnomalyEntry` 繼承了一個它整組不要的基底**：它覆寫 `AddCommonColumns` 成空的，
   因為那一層沒有 session。`AuditEntry` 的契約是「有 who / company / session」。

## 決策

### D1：兩個介面，各收自己的 entry 基底

```csharp
// Bee.Definition/Logging/IAuditLogWriter.cs（保留原名與原簽章）
public interface IAuditLogWriter { void Write(AuditEntry entry); }

// Bee.Definition/Logging/IAnomalyLogWriter.cs（新增）
public interface IAnomalyLogWriter { void Write(AnomalyEntry entry); }
```

需要一個 `AnomalyEntry : AuditEntry` 中間基底，`ApiAnomalyEntry` / `DbAnomalyEntry` 改繼承它。

**為什麼不讓兩個介面都收 `AuditEntry`**：那樣編譯器擋不住「拿異常寫入器去寫一筆登入記錄」，
拆介面就只是換個名字。**這一刀的價值在於把「誰能寫什麼」變成編譯期的事。**

#### D1-a：保護是**單向**的，論述與 CHANGELOG 都不得寫成雙向

`AnomalyEntry : AuditEntry` ⇒ **`IAuditLogWriter.Write(AuditEntry)` 仍然收得下
`ApiAnomalyEntry` / `DbAnomalyEntry`**。擋住的只有「拿異常寫入器寫稽核」這一向。

這在風險方向上正確（要防的就是那一向），**維持不變**，但兩件事跟著定死：

- **`IAuditLogWriter` 的型別表面完全沒有收窄。** CHANGELOG 與 ADR 不得寫「語意收窄」，
  否則下游會去找一個不存在的破壞性變更。它的破壞只在「三支建構子不再接受它」。
- `IAnomalyLogWriter` 的 XML doc 要寫明這個非對稱，讀者才不會以為兩向都擋。

要雙向就得改成 `LogEntry` 當共同基底、`AuditEntry` 與 `AnomalyEntry` 平行 —— 代價是
`IAuditLogWriteRepository.WriteBatch`（**public**）也要換型別，而且 who / company 那組共通欄
得複製到兩邊，正是下面 D1-c 明說不做的事。**不划算，不做。**

#### D1-b：五個共通欄位上提到 `AnomalyEntry`（同時解掉 S2094）

`AnomalyEntry` 若真的一個欄位都不放，它就是一個空的 `abstract class`，會被
SonarCloud **S2094（不應存在空 class）**擋（見 `.claude/rules/sonarcloud.md`）。

這條不必硬碰 —— 兩個子類**逐字重複了五個欄位**：

| 欄位 | `ApiAnomalyEntry` | `DbAnomalyEntry` | 處置 |
|---|---|---|---|
| `Kind` / `ElapsedMs` / `ThresholdMs` / `ErrorType` / `ErrorMessage` | ✅ | ✅ | **上提到 `AnomalyEntry`** |
| `Method` | ✅ | — | 留在子類 |
| `DatabaseId` / `Command` / `AffectedRows` / `ResultRows` | — | ✅ | 留在子類 |

⚠️ **只上提「屬性宣告」，`AddColumns` 的欄位發射留在各子類。** 這樣欄位輸出順序一行不變。
（順序本來就不影響正確性 —— `AuditEntry.GetColumns` 產的是具名 `AuditColumn`，
INSERT 依名稱組出來；`AuditLoggingTests` 唯一碰到 names 的那支斷言的是「不包含」而非順序。
即便如此，零順序變動仍是最小風險的做法。）

#### D1-c：who / company 那組**不下放**

`ApiAnomalyEntry` 有 session 脈絡、共通欄照填，只有 `DbAnomalyEntry` 沒有。
所以 `DbAnomalyEntry.AddCommonColumns` 那個空覆寫**原地保留**。

### D2：底下的管線一律不拆

| 型別 | 處置 | 理由 |
|---|---|---|
| `IAuditLogSink`（internal） | 不拆 | `AnomalyEntry : AuditEntry`，`WriteBatch(IReadOnlyList<AuditEntry>)` 仍然收得下 |
| `IAuditLogWriteRepository` | 不拆 | 同上；而且它的 remarks 已經說明它與讀取側分開的理由，與本次無關 |
| `AuditLogWriterService` | 同時實作兩個介面 | 佇列、批次、`TryWrite` 滿載退同步完全共用，拆了就是重複兩份 |
| `SynchronousAuditLogWriter` | 同上 | |
| `NullAuditLogWriter` | 同上 | |

⚠️ 三個實作類會因此各多一個 `Write(AnomalyEntry)` 多載（與 `Write(AuditEntry)` 並存，
內部 delegate 過去即可）。**`NullAuditLogWriter` 是 public**，新多載要進
`PublicAPI.Unshipped.txt`。**不改它的類別名** —— 改名是額外一筆破壞性變更，
換到的只是名稱貼切，改 XML doc 說明它同時是兩個介面的 no-op 就夠。

### D3：DI 註冊兩次，但 `DbAccessFactory` 那側的 gate **原地保留**

同一個實例註冊到兩個介面；`AnomalyEnabled == false` 時 `IAnomalyLogWriter` 註冊
`NullAuditLogWriter`。

⚠️⚠️ **[`BeeFrameworkServiceCollectionExtensions.cs:140`](../../src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs) 的
`audit.AnomalyEnabled ? () => sp.GetService<…>() : null` 這個三元式必須留著，只換型別名。**

理由是熱路徑：[`DbAccess.Anomaly.cs`](../../src/Bee.Db/DbAccess.Anomaly.cs) 的
`RunWithAnomalyDetection` 短路條件是 **`_anomalyWriter == null`**。若因為「反正有 NullWriter 了」
而把 gate 拿掉、無條件傳 factory，`_anomalyWriter` 會變成非 null 的 no-op，
**每一次 DB 命令都多開一個 `Stopwatch` 並繞 try/catch 分支**，寫出去的東西再被丟掉。

API 那側沒有這個問題（`JsonRpcExecutor.AnomalyEnabled` 與 `ApiServiceController.WriteApiKeyAnomaly`
各自已經檢查 `AuditLogOptions.AnomalyEnabled`），但也一併保留現行檢查，本次不動行為。

⚠️ **`Func<>` 延遲解析要留著。** 它存在的理由是建構相依環
（`IDbAccessFactory → IAnomalyLogWriter → AuditLogDbSink → IDbAccessFactory`），
不是啟用開關 —— 拆完之後環還在，只是換了中間那一段的型別名。

### D4：換參數型別的位置

**公開簽章（＝破壞性變更）：**

| 檔案 | 現行簽章片段 | 改為 |
|---|---|---|
| `Bee.Db/DbAccess.cs:50` | `IAuditLogWriter? anomalyWriter = null` | `IAnomalyLogWriter? anomalyWriter = null` |
| `Bee.Db/DbAccessFactory.cs:42` | `Func<IAuditLogWriter?>? anomalyWriterFactory = null` | `Func<IAnomalyLogWriter?>? anomalyWriterFactory = null` |
| `Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs:47` | `IAuditLogWriter? anomalyWriter = null` | `IAnomalyLogWriter? anomalyWriter = null` |

三支都在 `PublicAPI.Shipped.txt` 裡，**這是原始碼與二進位雙重破壞性變更**。

**非公開取用點（不進破壞性變更清單，但漏改就等於沒拆）：**

| 檔案 | 現行 | 改為 |
|---|---|---|
| `Bee.Api.AspNetCore/Controllers/ApiServiceController.cs:230` | `services.GetService<IAuditLogWriter>()` | `services.GetService<IAnomalyLogWriter>()` |

> 這一行現在拿得到的是「整個稽核寫入器」，`AnomalyEnabled == false` 時靠上面那行手寫
> gate 擋著。換成 `IAnomalyLogWriter` 之後，型別與 gate 會說同一件事（行為不變）。

`Bee.Db/DbAccess.cs:22`、`DbAccessFactory.cs:24`、`JsonRpcExecutor.cs:27` 三個私有欄位型別跟著換，
`DbAccessFactory.cs:38` 的相依環註解裡的型別名一併更新。

## 版號：**4.24.0 ＋ CHANGELOG 標破壞性變更**（2026-08-24 定案）

目前版號 **4.23.0**（`Version.props`）。

理由：三支換型別的都是 **optional 參數（`= null`）**，正常路徑是 DI 注入，外部直接
`new DbAccess(…)` / `new JsonRpcExecutor(…)` 並傳 anomaly writer 的機率極低；
且與本 repo 既有做法一致（v4.22.0 就在 minor 帶行為破壞性變更）。
跳 5.0.0 形式上更嚴格，但代價是所有下游 repo 的相依都要跟著跳大版，換到的只是 semver 的形式正確。

**不走 `[Obsolete]` 過渡**（那需要先發一版留舊多載，成本高於這次的破壞面）。

## 階段 1 執行紀錄（2026-08-24）

Clean Release build 零錯誤零警告；`./test.sh` 全 17 個專案通過（5,682 passed / 0 failed /
1 skipped，那筆 skip 是既有的 RSA 測試，與本次無關）。編譯期保證已實地驗證：
`IAnomalyLogWriter.Write(new LoginAuditEntry())` 擲 `CS1503`（一次性 probe，未入版控）。

⚠️⚠️ **兩件本 plan 沒預料到的事，兩件都要帶進階段 2 的 CHANGELOG。**

### 一、D1-b 的欄位上提，同時是一筆公開 API 的移動（RS0017）

把五個欄位上提到 `AnomalyEntry` 之後，PublicAPI analyzer 判定它們**不再是子類宣告的 API**：
`ApiAnomalyEntry` 與 `DbAnomalyEntry` 各掉 10 筆存取子（5 個屬性 × get/init），
共 **20 筆進 `PublicAPI.Unshipped.txt` 的 `*REMOVED*`**，同一批以 `AnomalyEntry.*` 重新出現。

**原始碼相容**（透過繼承照樣讀得到），**二進位不相容**（存取子的宣告型別換了，呼叫端要重編）。
本 plan 原本只把二進位破壞算在三支建構子上，**實際上還有這 20 筆**。
與 4.24.0 的裁定不衝突（本來就要標破壞性變更），但 **CHANGELOG 要列出來**，
否則下游只會去改建構子、不知道還得重編。

### 二、`DbAccess` 撞上 RS0026，需要具名抑制

`DbAccess` 有兩個帶 optional 參數的建構子，原本兩個都在 `Shipped` 裡、被 analyzer 放行；
把其中一個宣告成「新增」之後 RS0026 就響了（`Do not add multiple overloads with optional parameters`）。

處置：照 `src/Bee.Base/Expressions/IExpressionEvaluator.cs` 的**現成前例**，
在該建構子上加具名 `[SuppressMessage("ApiDesign", "RS0026", Justification = ...)]`，
Justification 寫明「這對多載不是新 API，兩個一直共存，且第一個參數型別互不相關，
不存在 RS0026 要防的靜默重新繫結」。**符合 `code-style.md`「抑制單行必須附說明」。**

### 範圍對帳

宣告 17 項、實際 18 檔，**多動一個**：

| 多動的檔案 | 為什麼 |
|---|---|
| `tests/Bee.Hosting.UnitTests/BeeFrameworkServiceResolutionTests.cs` | 平行路徑檢查時發現既有測試只斷言 `IAuditLogWriter` 解析得到，**新介面零覆蓋**；而本 plan 的驗收清單要求驗「`AnomalyEnabled = false` 時 `IAnomalyLogWriter` 解析得到 no-op」。補了一支 `[Theory]` 兩個 case：開著時兩個介面 `Assert.Same`（同一實例），關著時異常那一側是 `NullAuditLogWriter.Instance` 而稽核那一側不受牽動 |

宣告了但未動：`src/Bee.Db/DbAccess.Anomaly.cs` —— 它用的是 `_anomalyWriter!`，
欄位型別換了但該檔一個字都不必改。

### 平行路徑檢查結果（無漏網）

- **DI 只有一條註冊路徑**（`BeeFrameworkServiceCollectionExtensions`），samples / apps / tests 皆無自行註冊。
- **sink 與 write repository 靠 `entry.TableName` 與 `GetColumns()` 分派，不看型別** ——
  型別階層的變動對它們完全不可見。
- **entry 型別無任何序列化標註**（`XmlInclude` / `MessagePackObject` / `JsonDerivedType` 全零命中），
  沒有平行的 wire 路徑。
- 其餘仍持有 `IAuditLogWriter` 的測試替身全部在 `Bee.Business.UnitTests`，**那是稽核那一側，正確地未動**。

## 階段 2 執行紀錄（2026-08-24）

`./check-public-docs.sh` 與 `./check-xmldoc-refs.sh` 皆綠（前者只剩兩筆既有的性質說明，
不是連結）；`DefinitionDependencyGateTests` 通過；clean Release build 零錯誤零警告。

| 文件 | 改了什麼 |
|---|---|
| [adr-040](../adr/adr-040-audit-trail-taxonomy.md) | 補**決策七**：寫入介面依決策二的分界拆成兩個。含四列處置表（介面／記錄型別／寫入管線／開關）、**單向保護的警語**、who／company 為何不下放、以及「讀取側未納入本次」 |
| [`terminology.md`](../terminology.md) / [`.zh-TW`](../terminology.zh-TW.md) | 新增 `IAnomalyLogWriter`、`AnomalyEntry` 兩列；`IAuditLogWriter` 的說明收窄到「登入／異動／檢視」；`NullAuditLogWriter` 改述為同時服務兩個介面 |
| [`framework-reserved-names.md`](../framework-reserved-names.md) / [`.zh-TW`](../framework-reserved-names.zh-TW.md) | §1.3 標題加「與異常」，引言前置一段點明五張表是兩類、各走哪個介面、以及 `st_log_anomaly_db` 連觸發者都沒有 |
| [`database-settings-guide.md`](../database-settings-guide.md) / [`.zh-TW`](../database-settings-guide.zh-TW.md) | 兩處「五張＝稽核表」改為「稽核軌跡 ＋ 執行異常記錄」 |
| `CHANGELOG.md` / `.zh-TW.md` | 新增 `## [4.24.0]` 一節：破壞性 2、新增 2、變更 1、升級指引。雙語各 6 條、章節與行位逐條對齊 |

### 順手修掉的一筆文件漂移

**`AnomalyKind` 在術語表雙語都只列五個值、漏了 `Unauthorized`**（2026-07-30 那批金鑰功能帶進來的第六個值）。
與 ADR-027 D6 記的「異常五類」是同一筆漂移的另一個落點。**已補**。

### 範圍對帳

宣告 9 項、實際 **9 檔**，無超出。

### CHANGELOG 的兩點說明

- **寫進 `## [4.24.0]` 是符合本 repo 慣例的**：查過 4.23.0 那一節是由獨立 commit
  `90e44fe4`（`docs(release): 4.23.0 CHANGELOG 草稿與 ADR-039`）在 **tag 之前**寫入的，
  不是隨 tag 一起產生。
- ⚠️ **`docs/changelogs/4.24.0.md` / `.zh-TW.md` 明細檔尚未建立**，所以那一節沒有
  `📄 詳細變更與設計脈絡` 那一行。近八版都有這個連結，**發版時要補**
  —— 屆時 4.24.0 若還收了別的東西，明細檔本來就該一次寫齊。

## 刻意不做

| 項目 | 理由 |
|---|---|
| **`AuditLogOptions` 拆出 `AnomalyLogOptions`** | 那會改 `SystemSettings.xml` 的 XML 結構，是**所有既有部署的定義檔**都要跟著改的破壞性變更。開關已經分得開（`AnomalyEnabled` / `ApiSlowThresholdMs`），語意上的不一致換不到這個代價 |
| **讀取側拆分**（`LogBusinessObject` 九支方法目前全部走同一個保留 ProgId `AuditLog`） | **價值更高但範圍不同**，是另一份 plan。現況是「授一次給合規稽核員，等於連 `st_log_anomaly_db.command` 的 SQL 模板一起給」，那是權限模型的題目不是寫入介面的題目 |
| **把 who / company 下放到中間層、消掉 `DbAnomalyEntry` 的空覆寫** | 見 D1-c。那是另一個獨立的重構，而且會動到 `AuditEntry` 的公開表面 |
| **改成 `LogEntry` 平行基底以取得雙向保護** | 見 D1-a。要動 public 的 `IAuditLogWriteRepository`，且共通欄要複製兩份 |
| **`AnomalyKind` 拆成 API 與 DB 兩個列舉** | 六個值裡 `Error` / `Timeout` / `Slow` 兩邊共用，拆了要維護兩份平行列舉 |
| **`NullAuditLogWriter` 改名** | 見 D2。額外一筆破壞性變更，只換到名稱貼切 |

## 驗收

### 建置與型別

- [ ] `dotnet build --configuration Release` 零警告（`TreatWarningsAsErrors=true`）
- [ ] `IAnomalyLogWriter.Write` 收不下 `LoginAuditEntry` / `ChangeAuditEntry` / `AccessAuditEntry`（編譯期，以一支刻意編不過的樣本確認一次即可，不入版控）
- [ ] `AnomalyEntry` 不是空 class（D1-b 的五個欄位已上提），SonarCloud 無 S2094
- [ ] `AnomalyEnabled = false` 時 `IAnomalyLogWriter` 解析得到 no-op，且 `IAuditLogWriter` 不受影響
- [ ] `DbAccessFactory` 的 `anomalyWriterFactory` 在 `AnomalyEnabled = false` 時仍是 `null`（D3 的熱路徑條件）

### 測試：要跟著改的

替身實作了 `IAuditLogWriter` 而被換型別的建構子接住，**必須改成實作 `IAnomalyLogWriter`**：

- [ ] `tests/Bee.Db.UnitTests/DbAccessAnomalyTests.cs:26`（`CapturingAuditLogWriter`）
- [ ] `tests/Bee.Api.Core.UnitTests/JsonRpc/JsonRpcExecutorCoverageTests.cs:38`（`CapturingAuditLogWriter` 與 `:69` 的 helper 參數）
- [ ] `tests/Bee.Api.Core.UnitTests/` 下所有 `new JsonRpcExecutor(…)` 呼叫點（有傳 anomaly writer 的才受影響）

### 測試：預期不受影響、仍應全綠

- [ ] `AuditLogWriterServiceTests`、`AuditLogDbFactTests`、`AuditLogQueryDbFactTests`
- [ ] `FormBusinessObjectAuditTests`、`SystemBusinessObjectDeploymentAuditTests`、`SystemBusinessObjectLoginAuditIdentityTests`、`SystemBusinessObjectApiKeyLifecycleTests`（這幾支走 `IAuditLogWriter`，不動）
- [ ] `AuditLogJsonRpcRoundTripTests`、`AuditLoggingTests`
- [ ] `DefinitionDependencyGateTests`（本次不新增任何套件參考）
- [ ] `WireContractDriftTests`（entry 型別從不上 wire，只有查詢的 request / response 上）

### 出貨閘門

- [ ] 改動觸及 `src/Bee.Db/**` → **push 前跑一次完整模式 CI**（commit message 帶 `[all-db]`）
- [ ] `PublicAPI.Unshipped.txt` 申報：`Bee.Definition`（`IAnomalyLogWriter`、`AnomalyEntry`、`NullAuditLogWriter.Write(AnomalyEntry)`）、`Bee.Db`、`Bee.Api.Core`
- [ ] commit message 寫明二進位相容性判定（三支 optional 參數換型別 = 二進位破壞，取 4.24.0）

## 文件連動

| 位置 | 要改什麼 |
|---|---|
| [adr-040](../adr/adr-040-audit-trail-taxonomy.md) | 補一節「寫入介面依此分界拆成兩個」。決策二本來就說了兩者性質不同，這次是讓型別跟上。**一併寫明 D1-a 的單向保護** |
| [`docs/terminology.md`](../terminology.md) / [`.zh-TW`](../terminology.zh-TW.md) | 新增 `IAnomalyLogWriter` 與 `AnomalyEntry` 兩列（雙語同步） |
| [`docs/framework-reserved-names.md`](../framework-reserved-names.md) / [`.zh-TW`](../framework-reserved-names.zh-TW.md) | §1.3 標題是「Log 資料庫（資料軌跡）」而五張表其實分兩類，可順手把兩類的分界寫進那段引言 |
| [`docs/database-settings-guide.md`](../database-settings-guide.md) / [`.zh-TW`](../database-settings-guide.zh-TW.md) | 兩處把五張一律稱作「框架 opt-in 的稽核表」，與拆分後的分界不符 |
| `CHANGELOG` | 破壞性變更逐項列出（**三支建構子換參數型別**）。⚠️ **不要寫「`IAuditLogWriter` 語意收窄」** —— 見 D1-a，它的型別表面沒變 |

⚠️⚠️ **有一個外部死線：2026-09-10。**
鐵人賽 Day 25 那一篇的 §159 逐字寫了 `` `Func<IAuditLogWriter?>` ``，該篇 **2026-09-10 發佈後不得再改**。
框架若在那之前拆完，那一行要同步改成 `Func<IAnomalyLogWriter?>`；之後才拆就讓它停在舊識別符上
（線上版凍結與 repo 版分家本來就是預期中的）。**全系列只有這一行綁到這個識別符**，
另一處 §57 連的是 `AuditEntry.cs` 的 `AddCommonColumns`，本 plan 不動它。
