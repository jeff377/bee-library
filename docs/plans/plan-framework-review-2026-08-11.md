# 框架全面體檢（2026-08-11）

**狀態：🚧 進行中（2026-08-11）**

對 17 個 `src/` 專案做十一面向唯讀體檢，產出分級重構計畫與評分。
方法：10 個平行唯讀子代理分面向全量掃描 → 交叉去重 → P0/P1 主代理人工複驗（含實測 probe）。

- 基準版本：v4.19.0（`src/Directory.Build.props` 的 `<Version>`），HEAD `227daa70`
- 上輪體檢：2026-08-07（[plan-framework-review-2026-08-07.md](plan-framework-review-2026-08-07.md)，基準 v4.17.0）
- 期間變更：12 commit，`src/` 47 檔異動（+1,618 / −344）
- **本輪目的是發版前把關**：自 v4.19.0 累積兩筆破壞性變更，CHANGELOG 尚未撰寫
- 測試佐證：`./test.sh` 全綠 —— 16/16 專案、5,360 通過 / 1 略過（RSA）/ 0 失敗，四個 DB 容器全在

---

## 評分總表

| # | 面向 | 上輪 | **本輪** | 變化 | 主要扣分 |
|---|------|------|---------|------|---------|
| 1 | 架構分層 | 8.6 | **8.7** | ▲0.1 | A-5、A-2/A-4、A-3 |
| 2 | 相依分層 | 9.0 | **9.2** | ▲0.2 | N-1（22 處未宣告相依）、N-2 |
| 3 | 安全性 | 8.6 | **7.0** | ▼1.6 | **SEC-1（P0）**、SEC-2、SEC-3 |
| 4 | 維護性 | 8.5 | **8.5** | — | M-1、M-2（皆零進展） |
| 5 | 散落／不必要類別 | 7.5 | **7.5** | — | D-6、D-7 |
| 6 | 序列化一致性 | 9.2 | **8.8** | ▼0.4 | **GATE-2**、SEC-1 的序列化面 |
| 7 | 公開 API 表面 | 9.0 | **8.5** | ▼0.5 | **REL-1**、X-4、X-5 |
| 8 | 測試品質與覆蓋 | 8.5 | **8.2** | ▼0.3 | **GATE-1**、TEST-1/2/3、GATE-3、T-2 |
| 9 | 文件漂移 | 8.5 | **7.5** | ▼1.0 | **DOC-1**、**REL-2**、DOC-2 |
| 10 | 效能／熱路徑 | 6.8 | **6.5** | ▼0.3 | PERF-1、P-2/P-3/P-4 全未動 |
| 11 | 並行與全域狀態 | 7.8 | **7.6** | ▼0.2 | CON-1、N-5、A-4 |

> 「上輪」一欄為 2026-08-07 **修正後**的分數（該輪 plan 的〈修正後評分〉）。
> 那是只針對既有項目的增量重評、非重跑體檢，故本輪的「變化」對它比較會偏保守。

- **九面向平均：8.21**（上輪 8.60）
- **八面向平均（不含文件）：8.30**
- **十一面向平均：8.00**

> **分數下降主要不是程式碼退步。** 安全性 ▼1.6 幾乎全部來自單一既有缺陷（2026-04-12 引入）**首次被實測證實**；
> 文件 ▼1.0 的基準是「上輪修正後的增量重評」，那不是重跑體檢的分數，本來就會回落。
> 詳細歸因見〈加減分歸因拆分〉。

---

## 執行階段

| 階段 | 範圍 | 項目數 | 狀態 |
|------|------|--------|------|
| P0 | 發版阻擋項（安全 / 發版正確性） | 3 | ✅ 已完成（2026-08-11，SEC-1 / REL-1 / REL-2 全數落地並驗證） |
| P1 | 閘門可靠性與已證實的功能缺陷 | 11 | ✅ **已完成**（2026-08-11，11 項全數落地） |
| P2 | 結構、效能、一致性 | 14 | 🚧 進行中（P-2(a) / CON-2 / CON-4 / **A-4** / **N-5** ✅ 已完成；剩 9 項） |
| P3 | 文件漂移與低風險清理 | 13 | ✅ **已完成**（2026-08-11，13 項全數落地） |
| P4 | 觀察／待裁決 | 9 | 📝 擬定中（D-8 的 `MessagePackContract` 子項 ✅ 由另開 session 清除；其餘未動） |

### 已完成項目逐條（供對帳，勿只看階段狀態）

| # | 項目 | commit | 驗證 |
|---|------|--------|------|
| **SEC-1** | 型別白名單泛型繞過 —— 止血（修法 1，括號感知 parser） | `66294037` | probe 確認繞過封閉；clean build 0 警告；全套件 5,401 通過 / 0 失敗；AOT 閘門 758 通過 / 0 失敗 |
| **REL-1** | 三欄版號同步 4.20.0 + 新增 **BEE9002** 建置期閘門 | `780876d8` | 實證正反兩向：正常建置 0 警告，`-p:AssemblyVersion=1.2.3.4` 如預期紅在 BEE9002 |
| **REL-2** | CHANGELOG 雙語 + `docs/changelogs/4.20.0{,.zh-TW}.md` | `780876d8` | 雙語各 15 條 / 6 分節逐項對齊；新增文件相對連結全部可解析；舊版號殘留僅剩 3 處歷史量測 |
| **DOC-10** | `analyzer-rules` 雙語新增 BEE9xxx 一節 | `780876d8` | BEE9001 先前完全未載，一併補上 |
| **DOC-8 / DOC-9** | `docs/changelogs/4.19.0{,.zh-TW}.md` 補〈後記〉 | `780876d8` | 更正「行動端 AOT 失敗很可能是模擬假象」與已失效的 `WireMemberCount` 教學；保留原文存脈絡 |
| **D-8（子項）** | `MessagePackContract` 死碼清除 | `8f373bd5` | 另開 session 處理；複驗 `src/` / `tests/` 零殘留參照 |
| **SEC-2** | `LoginAttemptTracker` 改為有界：條目自帶到期 + 排程清掃、失敗計數視窗化、追蹤帳號數上限 | 待 commit | 新增 5 個測試，含「攻擊者永不重複 user id」這條 lazy cleanup 打不到的路徑；`LoginAttemptTracker` 測試 18 通過 |
| **SEC-3** | API key gate 失效改為可見：停用最後一把金鑰記 error、啟動檢查在非 Development 升為 error | 待 commit | **刻意未做啟動硬失敗**（見下） |
| **REL-3** | 版號抽為 repo 根 `Version.props`，`src/` 與 `tools/` 共用；`Bee.Cli` 從 4.8.0 併回 4.20.0 | `4575889e` | 雙向實證：兩個方案 clean build 0 警告，`-p:Version=9.9.9` 於 `tools/` 如預期紅在 BEE9002 |
| **GATE-1** | `WireContractDriftTests` 補防空轉斷言（閉包／註冊數下限 + 四個不同可達路徑的 canary） | 待 commit | 見下方「canary 第一版就抓到我自己的錯誤假設」 |
| **A-4** | `GlobalEvents` 只在**重新載入**時發事件；`DbConnectionManagerService` 實作 `IDisposable` 退訂 | 待 commit | **原訂修法會打斷檔案變更傳播，已更正**，見下 |
| **N-5** | `SessionCompanyBinder.Bind` 改為查詢全部完成後才寫入 `SessionInfo` | 待 commit | 窗口由「跨 3 次可能觸 DB 的呼叫」縮為「一串連續賦值」；`ClearCompanyContext` 本來就已是後者，未動 |
| **P3 其餘 9 項** | DOC-2 / DOC-4 / DOC-5 / DOC-6 / DOC-7 / DOC-11 / DOC-12 / DOC-13 / DOC-14 + Z-4～Z-7 | 待 commit | 公開文件死連結複驗 **0**；`dependency-map` 外部套件表雙語 13 列逐列一致；ADR 狀態行格式 **38/38** 統一 |
| **P-2(a)** | Unchanged 列不再攜帶兩份相同的值 | `72c5cbc6` | **比計畫記載多一處**：JSON 路徑有同樣缺陷，計畫只點名 MessagePack。連帶反轉一條把缺陷寫成規格的測試 |
| **CON-2** | `FormTable.RelationFieldReferences` 改 `Lazy<T>`（`ExecutionAndPublication`） | 待 commit | 測試以 32 執行緒並行首次讀取，斷言**拿到同一個實例**（非「都非 null」） |
| **CON-4** | `FormDefinitionLoader.GetLocalizedSchemaAsync` 空 lang 分支也 `Clone()` | 待 commit | **未加單元測試**，理由見下 |
| **PERF-1** | 運算式變數表瘦身為「只傳實際引用的變數」 | 待 commit | 前後同一 harness：`ApplyFieldExpressions` **57.2 ms → 12.2 ms（4.7×）**。**修法與計畫原本的判斷不同，見下** |
| **CON-1** | `ApiSessionContext` 承載兩個 per-session 值；`BeeApiConnectorFactory` 改 scoped | 待 commit | 新增 17 筆公開 API、**零 `*REMOVED*`**；6 個新測試驗「兩個 session 彼此不可見」 |
| **DOC-1** | `bee-serialization` skill 整份重寫（含 frontmatter description） | 待 commit | 舊版教的 `MessagePackCollectionBase` 等四個型別在 `src/` 宣告數為 **0**；改寫後逐項核對，所有引用的檔案與型別皆存在 |
| **DOC-3** | `UnitItem` XML doc 移除已退役的 BEE4004 敘述 | 待 commit | 該 doc 會進消費端 IntelliSense |
| **GATE-3** | CI AOT 閘門由 1 個專案擴到 3 個（補上 `XmlSerializer` reflection-only 那一半） | 待 commit | 本機預跑三專案：759 / 1052 / 580 全通過 0 失敗 |
| **TEST-1** | 七處以本地日斷言 UTC 產出改為 `DateTime.UtcNow.Date`，並就地註明原因 | 待 commit | 消除「本機 UTC+8 每天 00:00–08:00 必定失敗、CI 跑 UTC 永遠看不到」的失敗帶 |
| **TEST-2** | `ApiAspNetCoreTests` / `ApiKeyGateControllerTests` 改用 `SharedDbFixture` | 待 commit | 並在類別 doc 記下第三條觸發路徑（API key gate read-through）與「為何先前是綠的」 |
| **TEST-3** | `Bee.Definition.UnitTests` 新增 `ProcessWideStateCollection`，序列化三個衝突類別 | 待 commit | 該組件先前既無 `[Collection]` 也無 `DisableTestParallelization` |
| **GATE-2** | 8 個手寫 formatter 改實作 `IWireContract`，移除套套邏輯的 `WireMemberCount` | 待 commit | **實證**：在 `SortField` 加一個屬性 → drift 測試立刻紅（`型別上有但未註冊 → Probe`）。同一個 probe 在修正前不會被抓到 |

> **GATE-1 的 canary 第一版是錯的，而那正好證明了它有用。** 我原本把 `FormSchema` 列為
> 「必定在 wire 閉包內」的 canary，測試當場擋下——`FormSchema` 以 **XML 字串**夾在 wire 上傳輸，
> 不是以物件形式，因此不在閉包內。這也說明下限斷言為何不能只寫一個數字：數字擋得住「掉到只剩
> `ExtraRoots`」，擋不住「某一條可達路徑斷掉」。現行四個 canary 刻意取自不同路徑（訊息命名空間
> 的根、契約命名空間的根、掛在 `ApiMessageBase` 上每個訊息都會經過的集合、多型子型別）。

> **SEC-3 未完全照原計畫做，這是刻意的。** 計畫寫的是「非 Development 環境 `InForce==false` 由 warning
> 升為**啟動失敗**」。實作時發現 `IsApiKeyAccepted` 的 XML doc 把 presence-only 明文寫成「讓既有部署
> 跨版本仍能運作」的刻意設計——在 minor 版把它改成硬失敗，會讓每一個尚未發出金鑰的部署一升級就起不來，
> 而那正是這個退路要服務的族群。因此本輪做的是「不可能忽略」而非「直接擋死」：
> **runtime 降級的訊號**（本輪新發現、原本完全靜默的那一半）已補上，啟動檢查升為 error。
> 硬失敗應放在 major 版並附 opt-out，仍列 P1 未結。

> **CON-4 沒有加單元測試，這是刻意的。** 要替它寫測試需要替換 `ClientDefineAccess`，
> 而它是具象類別、`GetFormSchemaAsync` 非 `virtual`，建構又需要真的 `SystemApiConnector`。
> 把該方法改成 `virtual` 只為了讓測試能替身，是**為測試而改公開表面**（且 non-virtual → virtual
> 屬二進位破壞性）。改動本身是一行、語意自明（回傳 clone 而非共用實例），故以 code review 為準。
> 若日後 `ClientDefineAccess` 因其他理由抽介面，再補測試。

> **A-4 的原訂修法（「事件改由 `SaveDatabaseSettings` 發」）是錯的。** `DatabaseSettingsCache.GetPolicy`
> 掛了 `ChangeMonitorFilePaths`，因此「載入時發事件」其實在做兩件事：第一次載入是**多餘**的
> （而且會在 `DbConnectionManagerService` 自己的 `GetOrAdd` valueFactory 內清空整個連線快取），
> 但**檔案被外部編輯後的重新載入，那是唯一把變更傳播到連線快取的路徑**——搬到 `Save` 會靜默打斷它
> （外部編輯根本不經過 Save）。已改為「只在重新載入時發」，兩個目的同時滿足。

> **新增的已知 flaky（非本輪引入的缺陷，但由 TEST-2 引入的參與者）**：
> `ApiAspNetCoreTests.ExecFunc_Hello_ReturnsNotNull` 在 full suite 下偶爾以
> `ObjectResult`（錯誤）而非 `ContentResult` 失敗，隔離跑必過。**已用決定性實驗排除是本輪改動所致**——
> 暫存 A-4 / N-5 的改動後跑兩次，同樣 1/2 失敗、同一症狀。屬 `rules/testing.md` 記載的並行 DB 爭用，
> 但它是 TEST-2 把該類別改用 `SharedDbFixture` 之後才開始參與 process-wide 建 schema / seed 競爭的。
> **下輪應查根因**（嫌疑：`SharedDatabaseState` 的 seed 在多行程下的爭用），不要只當 flaky 記著。

**發版步驟進度**（依 `releasing.md`）：① CHANGELOG ✅ ② 版號 ✅ ③ `PublicAPI.Unshipped` → `Shipped` ✅（7 檔、15 筆 `*REMOVED*`，併後行數 7/7 命中預期，clean build 0 警告）④ commit ✅ / **tag ❌ 未打** ⑤ **push ❌ 未推**。

---

## 發版前把關清單（使用者指定的額外標注）

每項標「發版前是否應處理」。**判準**：(a) 會不會隨這一版發布出去且不可回收；(b) 破壞性視窗現在是開的，錯過要再開一次。

### ✅ 必辦（阻擋發版）—— 三項皆已完成（2026-08-11）

| # | 項目 | 理由 | 狀態 |
|---|------|------|------|
| **SEC-1** | 型別白名單泛型繞過（**已實測證實，未認證可達**） | 安全缺陷隨版本發布 | ✅ `66294037`（止血；根治另案） |
| **REL-1** | `AssemblyVersion` / `FileVersion` 漏升（C-5 回歸） | NuGet 發布後不可回收，已錯過一次 | ✅ `780876d8`（含 BEE9002 閘門） |
| **REL-2** | CHANGELOG 完全未寫（11 項應記載，含 **4 筆破壞性**，其中一筆是登入行為變更） | 發版必要條件 | ✅ `780876d8` |

### 🟠 強烈建議（趁破壞性視窗，錯過要再等一版）

| # | 項目 | 理由 |
|---|------|------|
| **X-4** | `SerializableData*` 5 個型別改 `internal` | 是 source-breaking；ADR-037 已把同資料夾 24/28 型別收成 internal，這 5 個是僅存破口 |
| **M-1** | `IAuthorizationService` / `TraceListener` 兩處撞名改名 | pre-stable 最後的免費窗口；CHANGELOG 本來就要寫破壞性節，邊際成本 ≈ 0 |
| **X-5** | `Login` 是否留在 `ISystemBusinessObject` 定案 | 移除即 source-breaking |
| **GATE-1** | `WireContractDriftTests` 補防空轉斷言 | 10 行；它是 ADR-037 的**唯一**自動化把關 |
| **GATE-2** | 8 個手寫 formatter 的形狀守衛 | 現行守衛在數學上不可能失敗 |

### 🟡 建議（不阻擋，但應在本版決策）

| # | 項目 | 理由 |
|---|------|------|
| **SEC-3** | API key gate presence-only（**連三輪未修**） | 與 SEC-1 有加成：gate 若 presence-only，SEC-1 即完全未認證可達 |
| **SEC-2** | `LoginAttemptTracker` 無上限（上輪修法引入的回歸） | 上輪 S-3 讓它從「零部署使用」變成「每個預設部署都在用」 |
| **CON-1** | `ApiClientInfo` per-user static | 若不修，至少在 `BeeBlazorOptions.UseRemoteProvider` 的 XML doc 標為已知限制 |
| **DOC-1** | `bee-serialization` skill 整份過期 | 不影響發版，但**主動誤導後續每一次序列化相關工作** |
| **GATE-3** | CI AOT 閘門擴到 `Bee.Definition` / `Bee.Base` | 改 2 行 yml，實測 0 失敗 |
| **TEST-1** | 七處測試以本地日斷言 UTC 產出 | 純測試端改動；**本機每天 00:00–08:00 是必定失敗帶**，會干擾發版前的本機驗證 |
| **TEST-2** | 兩個 `BeeTestFixture` 類別改 `SharedDbFixture` | 一行；消掉對 CI workflow 建 DB 步驟的隱形相依 |

### ⚪ 不需在本版處理

其餘 P2/P3/P4 項目。

---

## P0 — 發版阻擋項

### 🔴 SEC-1 型別白名單可用泛型參數繞過，未認證即可觸達（**已實測證實**）

> **✅ 止血修正已完成並驗證（2026-08-11），採修法 1。**
>
> | 驗證 | 結果 |
> |------|------|
> | probe 重跑（scratchpad，走公開入口 `ApiPayloadConverter.RestoreFrom`） | **繞過封閉**；控制組仍正確拒絕；錯誤訊息改為完整 AQN 而非截斷片段 |
> | 全方案 clean Release build（`--no-incremental`） | 0 警告 0 錯誤 |
> | `./test.sh` 全套件 | 16/16 專案、**5,401 通過 / 0 失敗 / 1 略過**（修正前 5,360，+41 為新增測試） |
> | 行動端 AOT 閘門 `-p:DynamicCodeSupport=false` | **758 通過 / 0 失敗 / 1 略過** |
>
> 改動：新增 `WireTypeWhitelist.IsAssemblyQualifiedNameAllowed`（括號感知 parser，逐一驗證外層型別、
> 每個泛型參數、陣列元素；無法解析即拒絕，fail-closed；含巢狀深度與長度上限）與
> `IsRuntimeTypeAllowed`（對已解析的 `Type` 遞迴驗證形狀）。三個呼叫點全部改用：
> `ApiPayloadConverter.ValidateTypeName`、`WireValueFormatter` 讀取端（並移除已無用的 `SimpleTypeName`）、
> `SafeMessagePackSerializerOptions.ThrowIfDeserializingTypeIsDisallowed`。
> 順帶修掉 Z-3 提到的讀寫兩端不對稱：寫入端也改走 `IsRuntimeTypeAllowed`。
> 測試：`WireTypeWhitelistTests`（新檔，含夾帶／畸形／過長／過深／pointer-byref）+
> `ApiPayloadConverterTests` 補 5 筆繞過形狀。
>
> **這只是止血。** 根治仍是本節修法 3（收斂成封閉集合查表），見〈P0 之外的後續〉。

**位置（同一缺陷、兩處實作）**
- `src/Bee.Api.Core/JsonRpc/ApiPayloadConverter.cs:113` — `typeName.IndexOf(',')`
- `src/Bee.Api.Core/MessagePack/WireValueFormatter.cs:259-263` — `SimpleTypeName`，ADR-037 新程式碼**原樣複製了同一寫法**
- 補位失效：`src/Bee.Api.Core/MessagePack/SafeMessagePackSerializerOptions.cs:47-70` — 只看 `type.FullName`，同樣不遞迴泛型參數

**根因**：白名單以「取第一個逗號之前 → 比對命名空間前綴」判定。但 assembly-qualified name 的**泛型參數自己含逗號、且排在外層逗號之前**，切點落進 `[[...]]` 內部 —— 泛型參數從未被檢查。

**現成載體**：`src/Bee.Base/Collections/Dictionary.cs:7` 的 `public class Dictionary<T> : Dictionary<string, T>`（public、具體、無參數 ctor、`T` 無約束）。`KeyCollectionBase<T>` 同理。

**實測證據**（scratchpad 獨立專案，`ProjectReference` 到 `Bee.Api.Core`，走**公開**入口 `ApiPayloadConverter.RestoreFrom`）：

```
crafted TypeName = Bee.Base.Collections.Dictionary`1[[Probe.Evil, Probe, ...]], Bee.Base
whitelist sees   = Bee.Base.Collections.Dictionary`1[[Probe.Evil      ← 切點落在 [[ 內
IsTypeNameAllowed= True
RestoreFrom      = OK
Evil.Instantiated= 1
Evil.LastPayload = 'pwned-marker'                                      ← 攻擊者控制的值進了 setter
CONTROL: plain Probe.Evil blocked -> 'Probe.Evil' is not in the allowed type whitelist.
```

**對照組是關鍵**：直接指名 `Probe.Evil` 會被正確擋下。邊界本身有效，是解析方式有洞。

**未認證可達性（逐節點驗過）**

| 環節 | 事實 |
|------|------|
| `System.Login` 在 `NoAuthMethods` | `src/Bee.Api.Core/Authorization/ApiAuthorizationValidator.cs:37` |
| `Login` 標 `(Public, Anonymous)` | `src/Bee.Business/System/SystemBusinessObject.Session.cs:25` |
| `Format = Encoded` 對 `Public` 方法放行 | `ApiAccessValidator` 只在 `ProtectionLevel > Encoded` 時擋 |
| 驗證**先於**反序列化，但 `TypeName` 不在驗證範圍 | `src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs:121,125` |
| `ContractlessStandardResolver` 仍在 resolver 鏈末端 | `src/Bee.Api.Core/MessagePack/MessagePackCodec.cs:43` —— server 是桌面 runtime，任意型別都拿得到 formatter |
| API key gate 預設 presence-only | 即 SEC-3；任何非空 `X-Api-Key` 即通過 |

**亦即：BO 方法還沒被呼叫、憑證還沒被驗證，反序列化已經發生。**

**嚴重度**：P0。未證明可達 RCE（取決於 server probing path 上的 gadget 型別），但「未認證輸入可具現任意型別並寫入其屬性」是 .NET 不安全反序列化的標準原語，且原始碼**自己**在 `WireValueFormatter.cs:234` 寫著 `a disallowed type is never even loaded` —— 那句話目前不成立。

**修法（建議 3）**
1. **括號感知 parser**：抽出所有型別段（外層 + 每個泛型參數 + 陣列元素），全部過白名單才 `Type.GetType`。改動面小、風險低。
2. **先解析再遞迴驗證**：`Type.GetType` 後對 `GetGenericArguments()` / `GetElementType()` 逐一驗。缺點是「載入」先於「檢查」，與現有註解的保證相反。
3. **收斂成封閉集合查表（根治）**：ADR-037 已把 wire 型別改為顯式註冊，`ApiPayloadConverter` 可改為只接受**已註冊 wire contract 的型別**，徹底取消 `Type.GetType(任意字串)`，並順帶關掉 `ContractlessStandardResolver` 這條 fallback 的攻擊面。

**引入時間**：`4db96d0d`（2026-04-12，v4.0.2）。**既有問題首次被證實，非 ADR-037 引入** —— 但 ADR-037 的新程式碼複製了同一形狀，所以現在有兩處。

---

### 🔴 REL-1 `AssemblyVersion` / `FileVersion` 漏升 —— 上輪 C-5 的回歸，且 v4.19.0 已帶錯版號發布

| tag | `<Version>` | `<AssemblyVersion>` / `<FileVersion>` | |
|-----|------------|--------------------------------------|---|
| 4.13.0–4.16.0 | 同步 | 同步 | 基準行為 |
| v4.17.0 | 4.17.0 | **4.16.0.0** | 上輪 C-5 抓到 |
| v4.18.0 | 4.18.0 | 4.18.0.0 | commit `08b7c6aa` 標題明寫「並修正組件版號」 |
| **v4.19.0** | 4.19.0 | **4.18.0.0** | **又漏了** |

`git diff v4.18.0..v4.19.0 -- src/Directory.Build.props` 只有一行 `<Version>` 改動。

**判定：漏升，非刻意釘住。** 三項證據：(a) 4.13–4.16 四版皆三欄同步；(b) 4.18.0 的修正 commit 標題明白宣告意圖是同步；(c) v4.19.0 那種規模的 wire 破壞正是最不該釘住 `AssemblyVersion` 的一版 —— 釘住會讓 4.18 編譯的 client 組件在 4.19 runtime 下**綁定成功卻 wire 不相容**，把編譯期可攔的錯誤推遲成執行期解碼失敗。

**已發布的 NuGet 4.19.0 套件內組件 identity 是 `4.18.0.0`，與 4.18.0 無從區分，不可回收。**

**修法（三項，按序）**
1. 三欄同步升至 `4.20.0` / `4.20.0.0` / `4.20.0.0`。
2. CHANGELOG 明列：v4.19.0 套件內組件 identity 誤為 4.18.0.0，v4.20.0 起恢復同步。
3. **補閘門** —— 這是重點，兩次都靠人工巡檢才抓到。建議 `src/Directory.Build.targets` 加一個 `BEE9002` 斷言 `$(AssemblyVersion) == $(Version).0`：**寫錯的當下就 build 失敗，不依賴發版者記得檢查**，與 BEE9001 同一種思路。

**上輪的教訓在這裡再次被驗證**：C-5 修的是「這一次」而不是「這個機制」。

---

### 🔴 REL-2 CHANGELOG 完全未撰寫，11 項應記載

`CHANGELOG.md` / `.zh-TW.md` 最上方仍是 `## [4.19.0]`，**無 Unreleased 區段**；`docs/changelogs/` 最新為 `4.19.0`。

| # | 應記載項目 | 來源 | 分類 |
|---|-----------|------|------|
| 1 | **wire 破壞性**：`object` 值封套由型別名改判別碼，`TypelessFormatter` → `WireValueFormatter`。**client 與 server 必須同版部署** | `3bf07615`(`!`)、ADR-037 | Breaking |
| 2 | **source-breaking**：`Bee.Expressions.{IExpressionEvaluator, ExpressionPolicy, ExpressionEvaluationException}` → `Bee.Base.Expressions.*`（12 筆 `*REMOVED*`，**無 type-forward、無 `[Obsolete]` 轉接 → 直接 CS0246**） | `f114aa46`(`!`)、ADR-038 | Breaking |
| 3 | **二進位破壞性（易漏）**：`FormRuleProcessor` / `FormExpressionCalculator` / `FormLiveComputation` 三個建構子的參數型別隨命名空間改變。**`FormLiveComputation` 那個參數是 optional —— 省略它的呼叫端原始碼照樣編得過，但既有已編譯組件會 `MissingMethodException`** | 同上，4 筆 `*REMOVED*` | Breaking |
| 4 | 新增公開 API：`Bee.Base.Expressions.*` 12 筆；`LanguageEnum.Entries.set` | `f114aa46` / `5e748b08` | Added |
| 5 | 行動端修復：`LanguageEnum.Entries` 補 setter，修 iOS reflection-only `XmlSerializer` 反序列化失敗 | `5e748b08` / `fad1efaa` | Fixed |
| 6 | 新建置期診斷 **BEE9001**（`src/Directory.Build.targets`）。**且 `docs/analyzer-rules.md` 雙語完全未提它** | `f114aa46`、ADR-038 | Added |
| 7 | `Bee.Definition` nuspec 不再列 `Bee.Expressions` / `DynamicExpresso.Core` | `f114aa46` | Changed |
| 8 | `samples/Avalonia.Demo` 移除，Avalonia 端到端示範收斂到 `apps/Bee.Northwind` | `37161a15` | Changed |
| 9 | **行為變更（本輪體檢後新增）**：`SystemBusinessObject.AuthenticateUser` 的預設實作由「永遠回 `false`」改為走 `st_user`。既有部署若未覆寫，**原本無人能登入，改版後 `st_user` 內的帳號可登入** | `fd723793`(`!`) | Breaking（行為） |
| 10 | **source-breaking**：`IUserRepository` 新增介面成員 `VerifyPassword(userId, password)` —— 對外部實作者是破壞性 | `fd723793`(`!`) | Breaking |
| 11 | **已發布定義檔修正**：`st_user.password` 由 `Length=40` 放寬為 200。`PasswordHasher` 產出的雜湊實測 79 字元，SQLite 以外四家會截斷、截斷後 `VerifyPassword` 恆回 false。此缺陷先前未浮現是因為框架沒有任何地方真的把雜湊寫進 `st_user` | `fd723793` | Fixed |

**關於「第三筆破壞性變更」**：`PublicAPI.Unshipped.txt` 的 16 筆 `*REMOVED*` 全部源自 ADR-038，沒有第三個獨立來源。但缺口存在於**另一個維度** —— 上表第 1 項（wire 破壞性）**在 PublicAPI 上完全不可見**（該 commit 自述「MessagePack 相關型別皆為 internal」）。只靠 `*REMOVED*` 盤點會漏掉它。第 3 項則是 `releasing.md` 明列的「已申報但二進位不相容」。

**發版機制提醒**：5 份 `PublicAPI.Unshipped.txt` 待併入 Shipped，其中 **16 筆 `*REMOVED*`（歷來最多）**。`Bee.Expressions` 是**整檔 12 行全為 `*REMOVED*`** 的極端情形，併檔後 Shipped 應只剩 `#nullable enable` + `DynamicExpressoEvaluator` 的 5 行。直接 append 會 `RS0024` 失敗，須走 `releasing.md` 的拆分流程，併完務必跑 `dotnet build Bee.Library.slnx -c Release --no-incremental`。

---

## P1 — 閘門可靠性與已證實的功能缺陷

### GATE-1 `WireContractDriftTests` 兩條 Fact 都無防空轉斷言 —— ADR-037 的唯一自動化把關可假綠燈

**位置**：`tests/Bee.Api.Core.UnitTests/WireContractDriftTests.cs:40, 57`（全檔 206 行，只有 2 個 `[Fact]`）

- `WireTypeClosure_IsFullyRegistered`：`Assert.True(missing.Count == 0)` —— 閉包為空時**無條件通過**。
- `WireContracts_MatchTypeShape`：`foreach (... .OfType<IWireContract>())` —— 集合為空時迴圈零次，通過。

**同 repo 三個 sibling 閘門都做了這道防護，唯獨最新的這個沒有**：

| 閘門 | 防空轉斷言 |
|------|-----------|
| `ApiContractPairingTests` | ✅ `WireMessageTypes_IsNotEmpty`（註解直言「防止上面的 Theory 因反射條件寫錯而變成零案例的假綠燈」） |
| `PayloadZoneCoverageGuardTests` | ✅ |
| `DefinitionDependencyGateTests` | ✅ `DependencyClosure_IsNotVacuous` |
| **`WireContractDriftTests`** | ❌ |

**且單純 `NotEmpty` 不夠**：閉包起點是命名空間**字串比對**（`:128-129`），命名空間一改，closure 會從約 150 型別**部分萎縮**到只剩 `ExtraRoots` 那 6 個 —— 非空但幾乎什麼都不守。應比照 `DependencyClosure_IsNotVacuous`：斷言 canary 型別在閉包內（如 `GetListRequest`、`FormSchema`）+ 計數下限。

**另一半**：`WireTypeClosure_IsFullyRegistered` **只驗單向**（閉包 ⊆ 註冊）。「註冊了但閉包到不了」抓不到 —— `ApiErrorInfo` 正是實例（見 D-6）。

**發版前**：建議處理。10 行改動。

---

### GATE-2 `WireMemberCount` 斷言是套套邏輯 —— 8 個 wire 型別實際上零形狀守衛

formatter 自己寫 `writer.WriteMapHeader(WireMemberCount)`，測試再把同一個 header 讀回來斷言等於同一個常數：

```csharp
// SortFieldFormatter.cs:43
writer.WriteMapHeader(WireMemberCount);
// WireFormatterTests.cs:39
Assert.Equal(SortFieldFormatter.WireMemberCount, ReadMapMemberCount(bytes));
```

這是 `Assert.Equal(X, X)`，**在任何情況下都不可能失敗**（所有 count ≤ 6，恆走 fixmap）。

而 `WireContracts_MatchTypeShape` 只迭代 `.OfType<IWireContract>()`，**全 repo 只有 `WireObjectFormatter<T>` 實作 `IWireContract`**。結論：90 個 `WireContract.For<T>()` 型別有形狀守衛，下列 8 個手寫 formatter 的目標型別**兩道守衛都沒有**：

`SortField`、`DepartmentNode`、`NumberFormatItem`、`CashRoundingItem`、`AllowedCurrencyItem`、`Parameter`、`FilterCondition`、`FilterGroup`。

**失敗模式**：沉默丟欄位。在 `SortField` 新增一個屬性 → 編譯過、JSON/XML 正常帶、MessagePack wire **靜默不帶**、build 綠、全測試綠。**這正是 ADR-036/037 宣稱已封住的失敗** —— 其中 `Parameter` 與 `FilterCondition` 還是最常被擴充的兩個型別。

**修法**：讓這 8 支 formatter 也實作 `IWireContract`（`WireType` + `WireMemberNames`；`FilterCondition`/`FilterGroup` 把 `Kind` 列為已知額外成員），然後**刪除** `WireMemberCount` 常數與其斷言 —— 它現在只是誤導。連帶要更新 7 支 formatter 與 `WireFormatterTests.cs:15-17` 的註解（它們都宣稱有這道保證）。

**發版前**：建議處理。

---

### SEC-2 `LoginAttemptTracker._attempts` 無上限、無淘汰 —— 上輪 S-3 修法引入的回歸

**位置**：`src/Bee.Business/Security/LoginAttemptTracker.cs:25-26`、`RecordFailure`（`:100-110`）

`RecordFailure(userId)` 對**任何** client 提供的 `userId` 新增一筆項目（發生在認證失敗之後，不需有效帳號）。移除只有兩條路徑：成功登入該 userId，或「已鎖定且鎖定已過期」。**`FailedCount < MaxFailedAttempts` 的項目 `LockedUntilUtc == null`，永遠不會被移除。** 無容量上限、無背景清理、無 sliding window。

`System.Login` 匿名可達 → 隨機 `UserId` 持續呼叫，每次失敗留下一筆永久項目。

**這是上輪修法的直接後果**：`3ca48eff` 的 `TryAddSingleton<ILoginAttemptTracker, LoginAttemptTracker>()` 把這個 map **從「零部署使用」變成「每個預設部署都在用」**。修法之前 `tracker` 恆 null、`RecordFailure` 從不執行。

**修法**：改用 `MemoryCache` 並對每筆設 `AbsoluteExpiration = LockoutDuration`（未達門檻的項目也會自然過期，順帶修好「`FailedCount` 永不衰減」的語意問題）；或加容量上限 + LRU。`AttemptInfo` 補 `FirstFailureUtc` 讓失敗計數有滑動窗口。

> 附帶：目前是 process-local，多節點部署下每個節點各有 5 次額度、重啟即清空。應在文件明說，避免被誤認為部署層級的保護。

---

### SEC-3 API key gate 預設 presence-only —— **連續第三輪**

**位置**：`src/Bee.Api.Core/Authorization/ApiAuthorizationValidator.cs:147-157`，逐字未變。

**本輪新增觀察**：唯一的告警 `WarnWhenApiKeyGateIsNotInForce`（`src/Bee.Api.AspNetCore/BeeFrameworkApplicationBuilderExtensions.cs:46-71`）是 **`UseBeeFramework` 啟動時的一次性快照**。因此「輪替時先停用最後一把舊金鑰」這個**由正常維運動作觸發的降級**在 runtime 發生時**完全沒有訊號**：不重新求值、不寫 audit、不記 log。營運端無從察覺防護已倒退。

**與 SEC-1 有加成**：SEC-1 的攻擊面（匿名 `System.Login`）要先通過 API key gate。gate 若是 presence-only，SEC-1 就是完全未認證可達。

**修法**：(1) 非 Development 環境 `InForce == false` 由 warning 升為**啟動失敗**（上輪建議）；(2) **新增** —— `SetApiKeyEnabled` 偵測到 `InForce` 由 true 轉 false 時，寫一筆 deployment audit + `LogWarning`，讓 runtime 降級不再靜默。

---

### CON-1 `ApiClientInfo` per-user static —— 本輪從「理論風險」證成「已出貨組態下的功能中斷」

**位置**：`src/Bee.Api.Client/ApiClientInfo.cs:23,43,49`；寫入 `Connectors/SystemApiConnector.cs:158`

四項新證據：
1. `src/Bee.Web.Blazor.Server/Bee.Web.Blazor.Server.csproj` 明列 `ProjectReference → Bee.Api.Client`。
2. `BeeLoginPanel.razor.cs:71` → `SystemApiConnector.cs:158` `ApiClientInfo.ApiEncryptionKey = ...`：**每個使用者登入都覆寫同一個 static**。
3. `ApiConnector.cs:202,209,231` 在**每個請求**上讀它做加解密。
4. **影響邊界（上輪未釐清）**：`ApiConnector.cs:196-199` 對 `LocalApiProvider` + 非 debug 強制 `PayloadFormat.Plain` → **`BeeBlazorProviderMode.Local`（預設）不受影響**。`Remote` 模式下使用者 B 登入後，A 的下一個 `Encrypted` 請求以 B 的 key 加密 → HMAC 失敗 → **A 被鎖死到重新登入**。

`ApiClientInfo` 的 XML doc 甚至寫著 "does not hold user session state"，而 `ApiEncryptionKey` 正是 session state。

**發版前**：若不修，至少在 `BeeBlazorOptions.UseRemoteProvider` 的 XML doc（會進 NuGet IntelliSense）明寫「目前不支援單一 process 內多使用者並行 Remote 連線」，讓它成為**已知限制**而非靜默缺陷。

---

### ✅ PERF-1 運算式求值每列成本（已完成，但**計畫原本的診斷是錯的**）

> **量測推翻了本節的假設。** 本節（與後續的選項分析）都認定「建鍵字串主導」，實測結果：
>
> | 項目 | 佔完整 `Evaluate` 路徑 |
> |------|------|
> | 建鍵字串 + 雜湊 | **17.8%**（非主導） |
> | 排序 | 4.3% |
> | **只傳實際引用的 3 個變數** | **降到 15.8% ⇒ 快 6.3×** |
>
> 鍵字串實測 735 字元，非本節估的約 1.5 KB。
>
> **真因**：`Evaluate` 每次把**全部 30 個變數**包成 `Parameter[]` 交給引擎繫結，而運算式只用 3 個
> —— 成本與**欄數**成正比，與運算式複雜度無關。
>
> **採用的修法**：`FormExpressionCalculator.NarrowVariables` 以 `GetReferencedVariables`
> 把變數表縮到實際引用者（結果per expression 快取，因為該方法每次取 `_parseLock`）；
> 取不到名單時退回完整表，讓解析失敗照原路徑浮現。**不動 `IExpressionEvaluator` 介面。**
>
> 前後同一 harness：`ApplyFieldExpressions`（30 欄 / 5 計算欄 / 1000 列）
> **57.2 ms → 12.2 ms**。全套件 5,422 通過 / 0 失敗，行為未變。
>
> **教訓**：本節原本提出的修法（快取鍵最佳化）就算做到完美也只省 17.8%，
> 而真正的槓桿在旁邊。體檢方法論寫著「不報無實測支撐的臆測性微優化」——
> 這一項當初是**推算**而非實測，差點就照著推算去修錯的地方。

<details><summary>原始（未經實測的）診斷，保留供對照</summary>

### PERF-1 運算式求值每列每計算欄重建快取鍵（新發現）

**位置**：`src/Bee.Expressions/DynamicExpressoEvaluator.cs:98-119, 180-195`

`Evaluate()` 每次呼叫都做：`variables.Keys.ToArray()` → `Array.Sort` → `BuildCacheKey()`（`StringBuilder` 串接 returnType.FullName + 運算式全文 + **每個變數名及其型別 FullName**）→ 拿這條長字串去 `ConcurrentDictionary.GetOrAdd`。

**頻率證明**：`FormBusinessObject.Save` → `FormRuleProcessor.cs:35` → `FormExpressionCalculator.cs:79-88` `foreach (DataRow row in dataTable.Rows)` → `:277-303` `ApplyComputed` → 每個 computed field 一次。即 **每列 × 每計算欄**。`ValidateRules`（`:131-146`）同樣是每列 × 每規則（`When` + `Condition` 各一次）。

30 欄 / 5 計算欄 / 100 明細列 = 500 條 ~1.5KB 暫存字串 ≈ 750 KB，加上 O(len) 的雜湊成本。**實際 lambda.Invoke 在此之下是零頭。** 同一張表的 names 陣列與 cache key 在整個迴圈中**恆等**，卻逐列重算。

**附帶**：`ExpressionPolicy.CoerceValue` 的 `DefaultOf` 用 `Activator.CreateInstance(clrType)`（`src/Bee.Base/Expressions/ExpressionPolicy.cs:88`）對每個 null/DBNull 儲存格走反射建構；`BuildVariables` 每列建 N 項 Dictionary 並對**每一欄**呼叫 `CoerceValue`，即使該欄沒有任何運算式引用。

**歸因**：ADR-038 的抽象下沉**沒有引入額外間接層**（`FormExpressionCalculator` 直接持有 `IExpressionEvaluator`，一次介面呼叫），問題原本就在（`e2259ea6`）。抽象簽章 `Evaluate(expression, variables, ...)` 本身鼓勵這種寫法 —— 沒有「編譯一次、綁多列」的 API 形狀。

**修法**：把 names/cacheKey 提到列迴圈之外；或給 `IExpressionEvaluator` 加一個 `Compile(expression, signature)` 的形狀。

</details>

---

### DOC-1 `.claude/skills/bee-serialization/SKILL.md` 整份教的是已不存在的機制

實測：`MessagePackCollectionBase` / `MessagePackKeyCollectionBase` / `MessagePackCollectionItem` / `MessagePackKeyCollectionItem` 在 `src/` 的宣告數皆為 **0**。該 skill 仍以它們為「三棲首選」：

- `:3`（**description，決定觸發**）：`集合三棲(MessagePackCollectionBase + 必顯式註冊 CollectionBaseFormatter…)`
- `:26,53,61`：範例程式碼以 `[MessagePackObject]` 開頭
- `:32-33,43,56,95`：`[Key(100)]`、「`[Key(n)]` 從 100 起」
- `:62`：`public class FooNodeCollection : MessagePackCollectionBase<FooNode> { }`
- `:70,93`：以 **BEE4001** 為建置期把關（已退役）
- `:77-80`：三棲對照表整列建立在已刪除型別上
- `:129,143`：checklist 與指向不存在的檔案

**任何依此 skill 新增 wire 型別的工作都會產出編譯不過、或漏註冊而在行動端炸掉的程式碼。** 相對地 `.claude/rules/serialization.md` **已正確更新**，兩者直接衝突。

> 這是上輪 C-7 的同型問題（`.claude/` 過期內容**主動誤導後續每一次工作**），且上輪已點名這份 skill 曾誤導體檢代理自己。修對了常駐 rules、漏了按需 skill。

---

### TEST-1 七處測試以本地日斷言 UTC 產出 —— **本機每天 00:00–08:00 必定失敗，CI 永遠綠**

同一個缺陷複製在 7 個檔案：production 用 **UTC** 算「今天」（空 `timeZoneId` ⇒ UTC，`src/Bee.Base/FrameworkClock.cs:47`），測試卻斷言 **`DateTime.Today`（機器本地日）**。

| 位置 | 測試 |
|------|------|
| `tests/Bee.Base.UnitTests/FieldDbTypeExtensionsTests.cs:25` | `GetDefaultValue_ReturnsExpectedForDateAndGuidTypes` |
| `tests/Bee.Definition.UnitTests/Forms/FormRowDefaultsCoverageTests.cs:108` | `DefaultForDbType_DateTypes_ReturnTodayAndNow` |
| `tests/Bee.Definition.UnitTests/Forms/FormExpressionCalculatorTests.cs:130` | `ApplyDefaultRow_FillsOnlyEmpty` |
| `tests/Bee.Business.UnitTests/Form/FormRuleProcessorTests.cs:100` | `ApplyBeforeSave_FillsDefaultValueExpression_WhenEmpty` |
| `tests/Bee.UI.Avalonia.UnitTests/DataObjects/FormLiveComputationTests.cs:132` | `ApplyDefaults_FillsEmptyDefaultExpression` |
| `tests/Bee.UI.Avalonia.UnitTests/DataObjects/FormDataObjectTests.cs:758` | new-detail-row 預設值 |
| `tests/Bee.UI.Avalonia.UnitTests/Controls/FormViewTests.cs:701` | `NewAsync` 預設值 |

**本機是 UTC+8 ⇒ 每天本地 00:00–08:00 跑就必炸；CI 是 UTC 所以永遠綠。** 這不是隨機 flaky，是每天固定 8 小時的失敗帶 —— 也就是「本機紅、CI 綠」這個**與規則記載相反**的方向，先前沒有被記錄過。

**最便宜的修法純在測試端**：`DateTime.Today` → `DateTime.UtcNow.Date`（或傳明確 `timeZoneId` 再由 `TimeZoneInfo` 推期望值 —— `FrameworkClockTests` 已是正確樣板）。`TimeProvider` 在這裡**需要改 production**：`FrameworkClock` 是 static class 直讀 `DateTime.UtcNow`，要把時鐘穿過 `FormRowDefaults` → `FormExpressionCalculator` → `DynamicExpressoEvaluator` → `FormLiveComputation`。注意**時區本來就可注入**（`timeZoneId` 參數），缺的只有「當下瞬間」。

> 全 repo 唯一有正規時鐘接縫的 production 型別是 `src/Bee.Business/Security/LoginAttemptTracker.cs:28,68`（收 `TimeProvider`，預設 `TimeProvider.System`），其測試已用手寫 `AdvanceableTimeProvider` —— 可直接換成 `FakeTimeProvider`，零 production 改動。

---

### TEST-2 兩個 `BeeTestFixture` 類別會碰 DB —— rules 未涵蓋的**第三條**觸發路徑

`.claude/rules/testing.md` 列的兩條是「BO/repository 直接讀寫」與「未植入 cache 的 token → 讀 `st_session`」。實際還有第三條，**與 access token 完全無關**：

```
ApiServiceController.PostAsync → ValidateAuthorization (:129) → ValidateApiKey (:177)
  → ApiKeyValidator.Validate (:39)  ← 每個請求第一行，無條件
  → _cache.ApiKeyGate.GetState() → ApiKeyGateCache.CreateInstance (:79)
  → CacheDataSourceProvider.GetApiKeyGateState (:146)
  → ApiKeyRepository.GetGateState (:90-100)   ← 開 common 連線
```

`AddBeeFramework` **一律**註冊真的 `ApiKeyValidator`（`BeeFrameworkServiceCollectionExtensions.cs:312`），所以任何 `BeeTestFixture` 容器都帶著這條 read-through。守門的 `HasCommonDatabase()` 在測試環境**恆為 true**（`SharedDatabaseState.cs:119-131` 的 `EnsureFallbackCommonDatabaseItem` 保證 `common` 這個 `DatabaseItem` 一定存在）。

| # | 類別 | 說明 |
|---|------|------|
| **B1** | `tests/Bee.Api.AspNetCore.UnitTests/ApiAspNetCoreTests.cs:16` | `ExecuteRpcAsync`(:104) 走完整 controller；`Ping_ValidRequest_ReturnsOkStatus`(:129) 與 `ExecFunc_Hello_ReturnsNotNull`(:144) 都斷言 200 |
| **B2** | `tests/Bee.Api.AspNetCore.UnitTests/ApiKeyGateControllerTests.cs:19` | 同上。**且 `Post_NoValidatorRegistered_UsesPresenceCheck`(:130) 名不副實** —— 傳 `validator: null` 只是「不加 override」，`GetService<IApiKeyValidator>()` 仍從 `_fx.Provider` 拿到框架真貨並打 DB |

**失敗症狀完全不指向真因**：`ApiServiceController.cs:186-195` 會吃掉例外轉成 `ApiKeyStatus.Invalid` → 401。測試看到的是「預期 200 拿到 401」，不是資料庫例外。

**為何現在是綠的**（三個都不是這兩個測試自己的保證）：該組件**完全沒有 `SharedDbFixture`**，所以 DB 是 CI 的 "Create test databases" 步驟或本機持久容器先建好的；而 `st_api_key` 表不存在**剛好**被 `ApiKeyRepository.cs:95-98` 的 `GetTableSchema(...) == null → InForce = false` 擋掉。任一失效即紅 —— 包含「有人在本機 `dotnet test` 該專案而未帶 `.runsettings`」。

**修法**：兩處改 `IClassFixture<SharedDbFixture>`，一行。

---

### TEST-3 `Bee.Definition.UnitTests` 完全沒有並行保護，而組件內同時有三種衝突行為

該組件**既無 `[Collection]` 也無 `DisableTestParallelization`**（全 repo 17 個測試專案中只有 4 個有後者），卻同時存在：

| ID | 衝突 |
|----|------|
| **R1** | 寫：`Security/MasterKeyProviderTests.cs:235` 把 `BEE_MASTER_KEY` 設為 `null`（:250 還原）。讀：`BeeTestFixtureSmokeTests.cs:158,159,171,172` **在測試 body 內** `new BeeTestFixture()`。在 null 窗口內建的 fixture 會走 `MasterKeyProvider.cs:146-152` 的 `autoCreate` 分支**產生一把新金鑰並寫回 env var**，接著 `finally` 又蓋回原值 → 該容器持有一把跟其他東西對不上的 master key，之後以「解密/HMAC 失敗」現形。`tests/Define/SystemSettings.xml:23-26` 把 `MasterKeySource` 釘在 `Environment`/`BEE_MASTER_KEY`，這是活路徑 |
| **R2** | `GlobalEventsTests.cs:16,19,21` 斷言 `Assert.Equal(1, invoked)`，但 `GlobalEvents.DatabaseSettingsChanged` 是**全域事件** —— 同組件平行跑的 `BeeTestFixtureSmokeTests` 每建一個容器就經 `DatabaseSettingsCache.cs:46` raise 一次，會把計數推到 2 |

**修法**：給這三個類別一個共同 `[Collection]`（最小改動），或整組件 `DisableTestParallelization`。

**同類但較輕**：`ClientInfoInitializeTests.cs:118-120` 的 `ClientInfoStringEndpointTests` 不在 `ClientInfoState` 內，註解理由是「不 mutate 任何 static」—— **判準錯了，讀也會 race**（`ClientInfo.cs:389 → :269` 會求值 `AllowGenerateSettings`）。目前良性，但那句註解等於邀請下一個人加非空 endpoint 案例。

---

### GATE-3 CI AOT 閘門只涵蓋一半

`.github/workflows/build-ci.yml:226-235` 的 `-p:DynamicCodeSupport=false` 只跑 `tests/Bee.Api.Core.UnitTests`。`rules/apple-mobile-trim.md` 明載的另一半（trim/AOT 下 `XmlSerializer` 走 reflection-only 路徑）其測試全在 `Bee.Definition.UnitTests` / `Bee.Base.UnitTests`，**從未在該旗標下跑過**。

**實測（本輪執行）**：

| 專案 | `-p:DynamicCodeSupport=false` |
|---|---|
| `Bee.Api.Core.UnitTests`（現行閘門） | 717 通過 / 1 略過 / **0 失敗** |
| `Bee.Definition.UnitTests`（未納入） | **1052 通過 / 0 失敗** |
| `Bee.Base.UnitTests`（未納入） | **580 通過 / 0 失敗** |

**擴充成本為零，且直接補上一個已實際踩過的洞**：`LanguageEnum.Entries` 的 get-only setter bug（`5e748b08`）就是這一類，而 `LanguageStorageRoundTripTests.cs:42` 正好會反序列化含 `Entries` 的 `LanguageResource` —— 那個 bug 當時是靠人工掃描發現的，不是靠 CI。

**另一個結構性盲區**：閘門跑在 CoreCLR，而 `MakeGenericType` / `MakeGenericMethod` 在 CoreCLR 上**不受該開關影響**。ADR-037 要求註冊封閉泛型具現的理由**正是** `MakeGenericType` —— 也就是說，**閘門原理上驗證不到它要保護的那一類失敗**。漏註冊一個 `List<Foo>` 只會被 `WireContractDriftTests`（靜態閉包）抓到。這兩道關卡不是互為冗餘，是各管一半，缺一不可。

---

## P2 — 結構、效能、一致性

### 相依與架構

| # | 項目 | 位置 | 說明 |
|---|------|------|------|
| **DEP-1** | **22 處「使用未宣告的組件」**（上輪 A-1 的完整版） | 11 個專案 / 340+ 條 `using` | 上輪只抓到 1 處。以「`using` 指示詞 vs csproj 宣告」全量比對後：`Bee.Api.AspNetCore`→{Api.Core, Base, Definition, **ObjectCaching**}、`Bee.Api.Client`→{Base, Definition}、`Bee.Db`→Base(94 條)、`Bee.Repository`→{Base, Definition(47)}、`Bee.Hosting`→{Base, Definition(27), RepoAbs(9)}、`Bee.UI.Core`→{**Api.Core**, Base, Definition}、`Bee.Web.Blazor.Server`→{**Api.Core**, Base, Definition} 等。**csproj 不再是相依關係的可信來源**：畫相依圖的人看到的邊，跟編譯器實際允許的邊不同。ADR-038 刪掉兩條邊沒炸是運氣（`Bee.Business` 剛好有顯式宣告），不是機制。全部引入於 2026-08-07 之前。修法：(a) 補顯式 `ProjectReference`；(b) 加測試比對每專案的 `using Bee.*` 前綴集合與其**直接** `ProjectReference` 集合 |
| **A-2** | `BackendDefaultTypes` 反射字串反指 8 個外層具象型別 | `src/Bee.Definition/BackendDefaultTypes.cs:15-53` | **仍存在，逐字未變**。Domain Core（L2）指名 `Bee.Business` ×3、`Bee.ObjectCaching` ×4、`Bee.Repository` ×1（L4），編譯期與相依圖都看不見 |
| **A-3** | `Bee.UI.Core/Permissions/` 位置錯誤 | 三檔 + `Bee.Web.Blazor.Server` 缺口 | **仍存在**。Blazor head 全 21 檔 grep `Sensitive|Capabilit|Permission` **零命中**，且其 csproj 只引用 `Bee.Api.Client`（結構上取不到）→ 同一份 FormSchema、同一組權限，Avalonia 有 per-role 降級、Blazor 沒有 |
| **A-4** | `GlobalEvents` 靜態事件 + 訂閱洩漏 + **re-entrancy 已確認為完整可達鏈路** | `src/Bee.Definition/GlobalEvents.cs:11`、`DbConnectionManagerService.cs:31` | **仍存在**。全 repo 唯一的 `public static event`；訂閱在 ctor、**無 `-=`、不實作 `IDisposable`**。re-entrancy 鏈：`GetConnectionInfo` → `_cache.GetOrAdd(id, CreateConnectionInfo)` → `_provider.Get()` → `CacheDefineAccess.GetDatabaseSettings()` → `DatabaseSettingsCache.CreateInstance` → **`GlobalEvents.RaiseDatabaseSettingsChanged()`** → `OnDatabaseSettingsChanged` → **`_cache.Clear()`，正在自己的 valueFactory 內**。不會死鎖（.NET Core 的 `GetOrAdd` 在鎖外呼叫 factory），但**每次 `DatabaseSettings` cache miss 都會連帶清光所有連線資訊**。正解：`CreateInstance` 不該在「載入」時發變更事件 —— 那不是變更，是首次載入 |
| **A-5** | Domain Core 夾帶 1,119 行檔案 IO + descriptor `preserve="all"` | `src/Bee.Definition/{Storage,Security,Defaults.cs,PathOptions.cs}` | **仍存在，數字未變** |

### 效能

| # | 項目 | 位置 | 說明 |
|---|------|------|------|
| **P-2(a)** | Unchanged 列同送 Current + Original —— **payload 與序列化 CPU 皆 2×** | `SerializableDataTable.cs:95-98` vs `:188-192` | **現象複驗成立且比上輪更確定**：`Modified` 與 `Unchanged` 併在同一 `case`，兩者都複製 Current 和 Original；而還原端 `Unchanged` 分支**只讀 `CurrentValues`**，`OriginalValues` 送過去直接丟棄。配合 `DataFormRepository.GetData` 結尾的 `AcceptChanges()`（且該行為寫進 `IGetDataResponse` 契約），**所有讀取列都是 Unchanged**。修法：拆開那個 `case`，一行 |
| **P-2(b)** | `Dictionary<string,object?>` 使欄名逐列重複上 wire | `SerializableDataRow.cs:13,18` | **仍存在，但成本結構因 ADR-037 而改善**：每格省下約 50–100 B（不再攜帶 assembly-qualified 型別名）與一次非泛型反射派發。30 欄 × 1000 列約省 1.5–3 MB。剩餘成本純粹是「欄名逐列重複」 |
| **P-3** | 單次 Save 約 6–10 次 `File.GetLastWriteTimeUtc` syscall | `FormBusinessObject.Permission.cs:32,48,73,98,138`、`Audit.cs:55`、`FormBusinessObject.cs:105,246,384`、`RepositoryFactory.cs:265` | **syscall 這條複驗成立**，三段都查證：`FormSchemaCache.GetPolicy` → `GetChangeSource` 回傳非空路徑 → `MemoryCacheProvider.CreateEntryOptions:101-107` 包成 `FileModificationToken` → `HasChanged`（`:158-165`）在未變更時**每次求值都 stat(2)**。`MemoryCache.TryGetValue` 會遍歷所有 expiration token。註解正確描述了機制，但沒推導出成本 |
| **P-4** | 權限檢查全表線性掃描 + **每次必定配置新 HashSet** | `src/Bee.Definition/Identity/CompanyRolePermissions.cs:48-62,97-111` | **仍存在**。`roleIds as ISet<string> ?? new HashSet<string>(roleIds)` —— `SessionInfo.Roles` 宣告為 `ICollection<string>` 且實際是 `List<string>`，**cast 必定失敗**。單次 Save 最多 3 次全表掃描 + 3 個 HashSet。正解：快照是不可變的，建構時預建 `Dictionary<(roleId, modelId), PermissionAction>` |
| **PERF-2** | JSON-RPC 派發每請求 4 處未快取反射 | `JsonRpcExecutor.cs:255,275,288`、`ApiAccessValidator.cs:79,86,91` | `GetType().GetMethod(action)`、`GetCustomAttribute`（每次重新具現，型別層屬性要走完三段）、`GetParameters()`、`taskType.GetProperty("Result")?.GetValue(task)`。`ApiInputConverter` / `ApiOutputConverter` 的屬性配對亦逐請求重算。修法：以 `(Type, action)` 為鍵快取一個描述子 |
| **PERF-3** | `ApiKeyCache` 負向快取 + `MemoryCache` 未設 `SizeLimit` | `MemoryCacheProvider.cs:19-20`、`ApiKeyCache.cs` | 快取鍵是呼叫端提供的 `sys_id`；未 override `GetNegativePolicy` → 沿用 5 分鐘負向快取。**`ApiKeyCache` 的 XML doc 明說負向快取是讓「沒有 rate limiting」可接受的原因，但該論證預設了快取有上界，而實際上沒有。** 對照：`SessionInfoCache.cs:70` **明確**以 `GetNegativePolicy => null` 關掉負向快取，理由寫得很清楚 —— 同一份推理沒有橫向套用 |

### 並行

| # | 項目 | 位置 | 說明 |
|---|------|------|------|
| **N-5** | `SessionInfo` 多欄位更新不具原子性 | `SessionCompanyBinder.cs:66-82`、`SystemBusinessObject.Session.cs:184-192` | **仍存在，且窗口比上輪描述更大**：連寫 6 個欄位，中間夾著 `_rolePermissionService.Get(companyId)` 與 `_employeeContextResolver.Resolve(...)` 兩次可能觸及 DB 的呼叫。`ScopeResolver.ResolveScopes:68-71` 可讀到「新公司 + 舊角色」→ **授權決策讀到不一致狀態** |
| **N-1** | `SerializeDefine` 序列化共用快取實例 | `SystemBusinessObject.Define.cs` 6 個呼叫點 | **仍存在，已文件化並明示接受**（`:300-317` 的 `<remarks>`）。**但接受理由只涵蓋「序列化輸出不受影響」，未涵蓋併發讀者路徑**。損害本輪精確化為**輸出污染**而非 NRE：`IsSerializeEmpty` 只在 `state == Serialize` 時回 null，故裸 `!` 解參考點實務不可達；真正的損害是兩個並行 `GetDefine` 對同一快取實例，B 產生的 XML 會把空集合輸出成元素而非省略 |
| **CON-2** | `FormTable.RelationFieldReferences` 對共用 FormSchema 做無保護 lazy init | `src/Bee.Definition/Forms/FormTable.cs:143-150` | 並行鏈完整：`SelectContextBuilder.cs:80` `GetFormSchema(RelationProgId)`（process-wide 共用）→ `:118` `srcTable.RelationFieldReferences[...]`。兩個並行請求首次查詢即同時進入 getter → 重複執行 + 兩個不同實例（其一孤兒）。`CreateRelationFieldReferences()` 還會 throw，例外從一個看似純讀取的 getter 拋出。**明確違反「Cache 內的定義資料 init 後不可異動」**。修法：改 `Lazy<T>`（`ExecutionAndPublication`），單行 |
| **CON-3** | `ObjectCache<T>.Get` / `KeyObjectCache<T>.Get` 是 read-create-write，非原子 | `ObjectCache.cs:74-89`、`KeyObjectCache.cs:96-121` | 底層 `IMemoryCache` 執行緒安全，但這裡沒用 `GetOrCreate`。三個後果：(a) 冷啟時多執行緒各自反序列化整份 XML / 各查一次 DB；(b) **與 N-5 疊加** —— 兩個帶同一未快取 token 的並行請求產生兩個 `SessionInfo`，Bind 寫進其中一個 → **EnterCompany 靜默失效**；(c) `PluginSettingsResolver` / `ProgramSettingsBoTypeResolver` 用 `ReferenceEquals` 判 reload，重複建構會被誤判 → `_chainCache` 被清空 |
| **CON-4** | `FormDefinitionLoader.GetLocalizedSchemaAsync` 的 XML doc 承諾了不成立的保證 | `src/Bee.Api.Client/Definitions/FormDefinitionLoader.cs:50-62` | doc 寫 `A schema safe to mutate — the cached instance is never handed out`，但 `lang` 為空時 `return raw`（共用快取實例）。**`InvariantCulture.Name` 是空字串**，`FormPage.razor.cs:95` 以 `CultureInfo.CurrentUICulture.Name` 傳入 → 多個 Blazor circuit 拿到同一個 reference。**絕對句（"never"）比沒有文件更危險**。修法：一行 |

---

## P3 — 文件漂移與低風險清理

### 公開文件

| # | 項目 | 位置 |
|---|------|------|
| **DOC-2** | 死連結（本輪回歸，雙語）：指向已刪除的 `samples/Avalonia.Demo` | `docs/development-cookbook.md:847`、`.zh-TW.md:825`。`37161a15` 同步更新了 6 份文件與兩支 skill，**唯獨漏掉 cookbook** —— 而 cookbook 正是外部開發者的主要操作手冊 |
| **DOC-3** | XML doc 描述已退場機制（**隨 NuGet 進消費端 IntelliSense**） | `src/Bee.Definition/Settings/UnitSettings/UnitItem.cs:25-27`：三重失效（無 `keyAsPropertyName`、`UnitItem` 現由 `WireContracts.Definition.cs:61-65` 逐一具名、BEE4004 已退役），且全為現在式。`8ee1e4a9` 專門清理 BEE4004 殘留但只掃了 `.md` |
| **DOC-4** | ADR-004 頂端 superseded 註記仍給出**錯誤的實作指示** | `docs/adr/adr-004-messagepack-payload.md:7`：「新增多型階層時採整數 `[Key]` + `[Union]`」。同檔 `:43-45` 已於本輪 `3241c8ca` 補上正確註記 → **同一份公開 ADR 內部自相矛盾**，且頂端那段是讀者最先看到、還帶祈使語氣的 |
| **DOC-5** | 量化漂移：`DefineType` 12 vs 實際 13（缺 `PluginSettings`） | `docs/terminology.md:267`、`.zh-TW.md:265`。`definition-files-overview` 則正確寫 13 |
| **DOC-6** | 公開文件內部自相矛盾：`Bee.Definition` 下游 6 vs 實際 7 | `docs/dependency-map.md:121`、`.zh-TW.md:120` —— **同一份文件的 mermaid 第 82 行就畫著 `UIAvalonia --> Definition`** |
| **DOC-7** | `docs/dependency-map` 外部套件表 4 筆與 csproj 不符（雙語同錯） | `:90-99`：`Bee.ObjectCaching` 列了已移除的 `FileProviders.Physical`（幽靈相依）；`Bee.Api.Core` 記成 *(none)* 而實為 **`MessagePack 3.1.7`**（全 repo 唯一一筆、也是 ADR-036/038 論證的軸心）；`Bee.Business` / `Bee.Repository` 各漏一筆。`f114aa46` 有改這兩份文件，但只動了 mermaid 與散文，同一頁下方的表沒複核 |
| **DOC-8** | `docs/changelogs/4.19.0*.md:91` 的「已知限制」結論已被 ADR-037 推翻 | 該節把行動端 AOT 失敗判為「很可能是模擬本身的假象」；ADR-037 證實那是真缺陷（37→185 筆，NativeAOT 與真 Mono 上重現）。changelog 是長效公開紀錄，此處無更正註記 |
| **DOC-9** | `docs/changelogs/4.19.0*.md:87` 的操作指示已失效 | `publish a WireMemberCount constant and assert it in a wire test` —— 該常數已隨 ADR-037 移除 |
| **DOC-10** | `docs/analyzer-rules.md` 雙語**完全未提 BEE9001** | 新增於 `f114aa46` |

### `.claude/` 與 `repo-ops`

| # | 項目 |
|---|------|
| **REL-3** | **（2026-08-11 新發現）`tools/Bee.Cli` 的版號脫節 12 個 minor —— 與 REL-1 同類，且 BEE9002 涵蓋不到**。`tools/Bee.Cli/Bee.Cli.csproj:23-25` 自帶 `<Version>4.8.0</Version>` / `4.8.0.0` / `4.8.0.0`，而框架已到 4.20.0。**它確實會發布**（`nuget-publish.yml:79` 的 `dotnet pack tools/Bee.Cli`）：`/p:Version=$ver` 覆寫套件版號，但**不覆寫 `AssemblyVersion` / `FileVersion`** → 自 4.9.0 起每一版發布的 `Bee.Cli` 內組件標的都是 `4.8.0.0`。<br>**加重情節**：該 csproj 的註解自己就寫著 `Bump whenever bumping src/Directory.Build.props` —— 要求已經寫下來，被違反了 12 個 minor，沒有任何東西會發現。這與 REL-1 是**完全同一個形狀**（宣稱有規則、無機制），只是換一個檔案。<br>**BEE9002 涵蓋不到**：它掛在 `src/Directory.Build.targets`，而 `tools/` 不繼承該檔。<br>**修法**：`tools/` 加一份對等的 `Directory.Build.targets`（或把版號改為由單一來源匯入），讓 `tools/Bee.Cli` 也受同一道閘門管。**發版前可做**——三行改動，且做了這一版的 `Bee.Cli` 才會正確 |
| **DOC-11** | `docs/repo-ops/ci-sonarcloud-setup.md:165` 死連結（`../` 少一層） |
| **DOC-12** | `docs/repo-ops/future-work.md:63` 因 ADR-038 失準（`Bee.Cli` 已不引用 `Bee.Expressions` —— 這其實是 ADR-038 的成果證明，只是文件沒跟） |
| **DOC-13** | `rules/testing.md` 三處過時：「20 處 `[Collection]`」（實際 24 處 / 4 組）、`SysInfoStatic` / `ClientInfo` 缺 `CollectionDefinition`（已補齊）、`[LocalOnlyTheory]` 範例（該 attribute 零使用） |
| **DOC-14** | ADR 狀態行格式不一致（本輪引入）：`adr-037:3` 用 `- **狀態**：已接受`，其餘 37 份一律 `## 狀態` + `已採納` |

### 序列化／註解漂移

| # | 項目 |
|---|------|
| **Z-4** | `SafeMessagePackSerializerOptions.cs:7,9,39` 三處說「Called by `TypelessFormatter`」—— 該型別已於 ADR-037 移除，實際呼叫者是 `WireValueFormatter.cs:246` |
| **Z-5** | `FilterCondition.cs:110-112` 的 `In()` 註解說 `object[]` 是為了「round-trip through the MessagePack typeless formatter」—— 機制已換，`object[]` 現在是封閉集的 `WireValueCode.ObjectArray`(22)，理由不同 |
| **Z-6** | `CollectionBaseFormatter.cs:25-27` 提及 `[MessagePackObject]` types —— 全 repo 已無 |
| **Z-7** | `WireContracts.cs:20-22` 宣稱「Most of this file family is **generated** from the wire type closure」—— **repo 內不存在任何產生器**（`tools/` 只有 `gen-public-api.py`）。實際機制是「一次性產生 + drift test 把關」，與「持續產生」語意完全不同 |

---

## P4 — 觀察／待裁決

| # | 項目 | 說明 |
|---|------|------|
| **M-1** | 消費端撞名的公開型別（**兩輪未動，pre-stable 窗口正在關閉**） | `Bee.Definition.Identity.IAuthorizationService` 撞 `Microsoft.AspNetCore.Authorization.IAuthorizationService`；`Bee.Base.Tracing.TraceListener` 撞 `System.Diagnostics.TraceListener`。撞名處必然是 `BeeFrameworkServiceCollectionExtensions.cs:298`（同時 `using` 兩邊的位置）。影響面小且封閉：10 個 `src/` 引用點 + 7 個測試檔。建議新名：`IPermissionService`、`TraceDispatcher`。**本輪另發現 3 個同類候選**：`Bee.ObjectCaching.CacheItemPolicy`（撞 `System.Runtime.Caching`，名稱與語意都相同）、`Bee.Definition.Collections.Parameter` / `Property`（過於泛用）。後三者改名成本高於 M-1，建議只列入決策紀錄 |
| **M-2** | 註解紀律 | 中文 `#region` **31 處（完全持平）**，其中 **22 處是同一個字串「建構函式」，1 次 sed 即可**；中文 in-body 註解 18 行（+2）；WHAT-only 註解 164 處（190→164，比例改善）。**中文 XML doc 0 行** ✅ |
| **M-3** | `private static readonly` 三套並行命名（新發現） | 15 處 `s_xxx` / 18 處 Roslyn 樣板 PascalCase（合法）/ **27 處無理由的 PascalCase**。`code-style.md` 只規定了實例欄位（401 處 100% 一致），**這是規範本身的缺口** —— `3bf07615` 那批新落地又添了 6 個。可用 `.editorconfig` 的 `dotnet_naming_rule` 一勞永逸 |
| **D-6** | `ApiErrorInfo` 整個型別（11 筆公開 API）已被 `JsonRpcError` 取代 | 零生產消費者（`grep "ApiErrorInfo "` 只命中宣告）；唯一存活形式正是**佔位測試**（`ApiErrorInfoTests.cs` 三個「建構子填欄位」測試，讓它在覆蓋率報告上顯示為已測試）。**且它被註冊進 wire（`WireContracts.Envelope.cs:44-48`）卻不在 wire 閉包內** —— 這是 GATE-1 單向檢查盲區裡的實例 |
| **D-7** | `AuditLogOptions.ExecEnabled` —— 已出貨的設定開關，全 repo 零讀取點 | `src/Bee.Definition/Settings/SystemSettings/AuditLogOptions.cs:79`。五個兄弟旗標各有明確消費點，只有它沒有（exec 那條軸從未實作，`st_log_exec` 只存在於已封存的 plan）。**危害不是佔空間，而是它是 `PublicAPI.Shipped.txt` 內的設定合約**：部署者讀 XML doc 會以為開關有效。連帶佐證：`docs/architecture-overview.md:422` 寫「**六軸**資料軌跡」但只列了五項 |
| **D-8** | 其他死碼／散落 | ~~`MessagePackContract.UsesNameBasedKeys`（BEE4002/4004 退役漏清，同一批清理的第三次補漏）~~ **✅ 已由另開 session 處理（2026-08-11，尚未 commit）**：`MessagePackContract.cs` 刪除、`SerializationAttributeNames.cs` → `SerializationTypeNames.cs`、`FrameworkCollectionTypes.cs` 連帶調整；複驗 `src/` / `tests/` 零殘留參照。**這是 BEE4001–4004 退役的第四次補漏，也應該是最後一次** —— 前三次分散在 `67255d22` / `5af66587` / `8ee1e4a9`，每次都少帶一個檔案，根因是缺「退役 checklist」而非缺意願；`Bee.Db.Dml` 五個單一實作零引用介面（`ISelectBuilder` 等，在 Shipped 內故移除屬破壞性）；`ApiCallContext.ShouldValidateEncoding` 零呼叫者；`ClientInfoTestScope` 零 caller **且其文件指向一個沒人用的 collection 名稱**（`[Collection("ClientInfo")]` 0 使用，14 個實際 caller 用 `ClientInfoState`）；`[DbTheory]` / `[LocalOnlyFact]` / `[LocalOnlyTheory]` 三個 attribute 零使用 |
| **D-9** | `StringUtilities.IsEmpty(object?)` 與 `ValueUtilities.IsEmpty(object)` 語意分歧 | 同組件、同名、同簽章形狀，`Guid.Empty` / `DateTime.MinValue` 結果相反。呼叫點數量差 4 倍（169 vs 41），**兩者都不會編譯錯、不會警告** |
| **T-2** | 4 處名實不符的空洞 round-trip（**連續兩輪未修**） | `DtoSerializationTests.cs:290,305`、`FileDefineStorageTests.cs:128,161`（上輪報的行號是註解列）。另 3 處同類新發現：`GetNewDataMessagePackTests.cs:18`、`GetDepartmentTreeMessagePackTests.cs:50`、`SystemSettingsLoaderTests.cs:16`。**修法成本極低**：同 repo 已有 `DefinitionSerializationTests.SerializeObject<T>` 現成 helper |
| **T-3** | 測試覆蓋缺口 | `ExpiredSessionCleanupService`（**唯一會刪資料的背景服務**）零覆蓋、`LogApiConnector`（9 個稽核 API 的 client 端）零覆蓋 —— 兩者同專案的兄弟型別都有測試，非能力缺口。三個集合成員從未帶值 round-trip（`CheckPackageUpdateRequest.Queries` / `Response.Updates` / `ListApiKeysResponse.ApiKeys`）；`WireValueCode` 的 `DateTimeOffset`(15) 是唯一無 round-trip 測試的分支，且**判別碼數值未被任何測試 pin 住**（重新編號是靜默的跨版本 wire 不相容，drift test 抓不到）；`CreateLogBO` 零測試（兄弟 `CreateFormBO`/`CreateSystemBO` 各 3 個） |
| **X-6** | `ILogBusinessObject` 的 `<remarks>` 與已發布 API 直接矛盾 | `src/Bee.Business/AuditLog/ILogBusinessObject.cs:9-12` 寫「there is no `CreateLogBO` factory extension」，但它存在（`BusinessObjectFactoryExtensions.cs:64`）且已發布（`PublicAPI.Shipped.txt:666`）。**XML doc 屬公開文件，這是一句對外的事實錯誤** |
| **N-2/N-3** | 一型別一檔（3 檔應拆）與定義層根目錄散檔 | `SnapshotLanguageService.cs:133`（**public** `LanguageLayers`）、`ReservedProgIds.cs:20`（**public** `ReservedProgIdBinding`）、`FilterNodeSubtypeFormatters.cs:18,42`（檔名不等於任何型別名）。`src/Bee.Definition/` 根目錄 37 個散檔，其中 12 個構成未成形的 `Numeric/` 家族。另：`CacheDefineAccess.cs`（523 行 / 35 方法 / 無 region 無 partial）是唯一未循 partial 慣例的大檔；**`ValueUtilities` 已從裁決當時的 501 行成長到 631 行（+26%），建議複核裁決是否仍成立** |
| **Z-3** | `ScopeResolver` 的 `List<object>` —— **landmine 實質解除** | 三處（`:142,163`，**`:187` 上輪未列**）。ADR-037 後 `List<object>` 不在封閉集 → 走逃生門 → 白名單拒絕 → **寫入端當場擲 `InvalidOperationException`**。但實際風險為零：這些 `FilterCondition` 由 server 端建構、只轉成 SQL、**從不上 wire**。且 `In()` 的 `object[]` 具現化不變式**已不再是必要條件**（`object[]` 現為 `WireValueCode.ObjectArray`(22)，有專測）。建議降 P4：三處改走 `FilterCondition.In()`，並更新 `In()` 的過時註解（Z-5） |
| **SEC-4~SEC-10** | 安全 hardening 小項 | `EXEC('...')` 內嵌 identifier 只做 quoting 未做 literal escaping（`SqlTableAlterCommandBuilder.cs:143-144`，同檔 `:132-133` 已正確 escape —— 作者知道要做，漏了這處巢狀語境；且路徑已 `LocalOnly`）；`isLocalCall` 的預設值是**寬鬆那一邊**（`IBusinessObjectFactory.cs:20` `= true`，production 呼叫點都有顯式傳值，但這是框架公開 API，host 自訂 dispatcher 是預期用法）；帳號鎖定僅以 userId 為 key（鎖定即 DoS + password spraying 不受限）；`ClientInfo.cs:342` / `MemoryCacheProvider.cs:159` / `DataTableJsonConverter.cs:383` 三處空 catch |
| **CON-5** | `ConfigureAwait(false)` 缺口與 public sync-over-async | `DbAccess.Async.cs:259,271,283`、`LocalApiProvider.cs:47`、`JsonRpcExecutor.cs:128`。後者連帶：`JsonRpcExecutor.Execute`（`:80-83`）是 public sync-over-async，production 零呼叫者但**框架使用者拿得到**，在有 SynchronizationContext 的 host 上是教科書死鎖 |
| **X-7** | `Bee.Api.Contracts` 無相依閘門 | 它目前零外部套件，且位於**每一個 UI head 的傳遞閉包**內 —— ADR-038「加在這裡會傳染給每個消費者」的論證對它幾乎同等成立。加入 `BeeEnforceDependencyBoundary`（allowlist = `Bee.Definition`）成本近乎零。另：`Bee.Base` 有 BEE9001 但無對應的閉包測試（現階段實質安全，因 allowlist 為空） |

---

## 加減分歸因拆分（方法論教訓 C）

| 面向 | 變化 | 真實改善 | 掃描深度變化 | 真回歸 |
|------|------|---------|------------|--------|
| 安全性 | ▼1.6 | +0.4（防護面廣度紮實：SQL 注入／XXE／路徑安全／加密原語／payload 順序／機密不落 log 逐項零缺口） | **−1.7**（SEC-1 自 2026-04-12 存在，首次實測證實。**程式碼沒有變差，是我們對它的認識變準了**） | −0.3（SEC-2 是上輪 S-3 修法引入的 DoS 面） |
| 文件漂移 | ▼1.0（對修正後 8.5） | +1.2（5 條 public-docs 落地檢查**全數乾淨**；ADR-038 相依圖 28 條邊雙語逐條吻合；舊命名空間在公開文件**零錯誤殘留** —— 這是預期「約六成問題來源」的項目，實際完全沒中） | −0.4（連結掃描面 1291→1926，+49%；`DefineType` 與 dependency-map 下游數是上輪未查到的既有漂移） | −0.3（DOC-2 是 `37161a15` 漏更；DOC-14 是本輪引入） |
| 序列化 | ▼0.4（對修正後 9.2） | +0.5（`object` 通道從 typeless 換成 22 個判別碼的封閉集 + 封閉泛型委派，是**能力的實質獲得**；145 筆顯式註冊使 `FormatterNotRegisteredException` 從「幾乎每個 payload 型別」降到 0） | −0.9（GATE-2 的套套邏輯是本輪才發現，那 8 個型別**從未有過**形狀守衛） | 0 |
| 效能 | ▼0.3 | +0.3（**ADR-037 對 wire 熱路徑是淨改善，不是為 AOT 付的稅**）+0.2（P-1 修得徹底） | −0.8（PERF-1 是更深一層的新發現） | 0 |
| 並行 | ▼0.2 | +0.4（ADR-037 註冊機制**零並行債**：約 30 檔、上百個註冊，全走 type-initializer + readonly，無 lazy 快取、無鎖、無 runtime 註冊路徑） | −0.5（CON-2/CON-3 是本輪新掃出的系統性缺口） | 0 |
| 相依 | ▲0.2 | +0.4（ADR-038 消除了唯一一條實際違規邊，並把判準升級為兩道**互補**的可執行閘門） | −0.2（DEP-1 從單點擴成 22 處；閘門盲區是本輪才逐項驗證的） | 0 |

**沒有任何一項是 2026-08-07 之後引入的相依或架構回歸。** 唯二的真回歸是 SEC-2（上輪修法的副作用）與 REL-1 / DOC-2（發版與文件的同步遺漏）。

---

## 掃描為乾淨的項目（供下輪回歸偵測）

**維持乾淨（逐一重驗仍成立）**
- 28 條 consumer-facing 相依邊**零循環**（DFS 三色標記無 back edge）；四條硬約束全綠（BO 無 `Bee.Db`、後端無 `Bee.Api.Client`、Repository 抽象未被繞過、Contracts 零實作污染）
- mermaid 相依圖與 csproj **28/28 逐條吻合，雙語各一份**（上輪 30/30；差額 2 條正是 ADR-038 刪除的兩條邊）
- **ADR-038 落實度全綠**：`Bee.Definition` 傳遞閉包實測無 DynamicExpresso；`Bee.Expressions` 是唯一持有者且只剩 `DynamicExpressoEvaluator` 一個型別；`Bee.Base.csproj` 零 `PackageReference`、零 `ProjectReference`；`Bee.Cli` / `DefineEditor` 閉包中已無 `Bee.Expressions`
- **BEE9001 的 `PrivateAssets` 大小寫比對有效**（props 寫 `"All"`、target 比 `'all'`，MSBuild `WithMetadataValue` 為 case-insensitive）
- `*Func` 靜態類 0、`*Helper`/`*Util`/`*Mgr` 型別 0、Newtonsoft 0、`[Obsolete]` 0、空 class 0、grab-bag 0
- **`CurrentCultureIgnoreCase` / `StringComparison.CurrentCulture*` / 無 Invariant 的 `ToLower/ToUpper` 全部 0**（983 檔）；19 處無 `StringComparison` 的 `StartsWith`/`Contains`/`IndexOf` 逐一驗證後 **0 違規**；擴充 `object` 0；與 BCL instance method 同名的擴充方法 0（44 個 `*Extensions` 逐一比對）
- S125 真陽性 **0**（29 筆疑似命中全為折行英文散文）；`#pragma warning disable` 0；4 處 `SuppressMessage` 全附完整 Justification
- 資料夾↔命名空間：53 處全落在 `Settings/` 明文例外，例外以外 **0**；36 個 partial 檔命名 100% 合規
- **SQL 注入 0**（`src/Bee.Db` 124 處內插字串全量掃：值一律 `{0}` 佔位符、識別符一律 `QuoteIdentifier`；`SelectBuilder.cs:36-37` 另以 `formTable.Fields` 白名單擋下不存在欄位）；**XXE 0**（4 個解析入口全設 `DtdProcessing.Prohibit` + `XmlResolver=null`）；`new Random(` **0**；硬編碼機密 **0**；MD5 **0**；裸手動 `Dispose` **0**（9 處全在 `finally` 或 `Dispose()` 內）；`throw ex;` **0**；TLS 憑證驗證繞過 **0**
- 加密原語全綠：AES-256-CBC + 隨機 IV + **encrypt-then-MAC** + `CryptographicOperations.FixedTimeEquals`；PBKDF2-SHA256 / 100k / 16-byte 鹽；**全 repo 零 `==` 比較 HMAC/雜湊**
- payload 管線順序正確且**存取驗證在解密之前**（`JsonRpcExecutor.cs:121` 先於 `:124-125`，有註解說明為刻意）
- 未標註 = 拒絕（fail-closed）：`ApiAccessValidator.cs:31-36` + BEE3001 + BEE3003 雙重把關；**上輪 S-1/S-2 的 `LocalOnly` 鏈路逐節點複驗通過，遠端無路徑可讓 `isLocalCall` 為真**
- 契約軸 100% 對齊；契約↔wire **雙向零孤兒**；public 可變欄位 0；**XML doc 0 行含中日韓字元 / 25,778 行**；`<param>` 名稱不符 0
- **ADR-037 公開表面增量 0 行**（改動 28 個型別、重寫整條 wire 綁定，`Bee.Api.Core` 的 `PublicAPI` 本輪未動）
- 漏註冊的 wire 型別／封閉泛型具現 **0**（獨立核對 enum 14／`Nullable<T>` 4／`List<T>` 5／`Dictionary<K,V>` 3／`string[]`／`DataTable`/`DataSet`／`byte[]` 全對應）
- **`[XmlElement]` 標註的 get-only 集合屬性 0**（全 repo 重掃，2026-08-10 的 `LanguageEnum.Entries` 修正確實成立）
- 自訂 formatter 內非泛型 `Serialize(Type, ref writer, …)` 僅 2 處（ADR 明載的逃生門）
- **DI captive dependency 0**（46 `AddSingleton` + 1 `TryAddSingleton` + 1 `AddTransient`，**零 Scoped**，結構上不可能）；`async void` 0；`lock(this)`/`lock(typeof)` 0；唯一的 DCL（`DepartmentTree.EnsureIndex`）帶正確 `volatile` 且先建後發佈；唯一的 `[ThreadStatic]` 有完整作用窗論證
- `KeyCollectionBase<T>` 仍是真 O(1)（`base(StringComparer.OrdinalIgnoreCase)` → 隱含 `dictionaryCreationThreshold = 0`）；**零處 per-row 線性欄位查找**；N+1（讀取路徑）0；`XmlSerializer` / `HttpClient` / `Regex` / 加密器皆已快取或池化
- **S2699 0 違規 / 4133 個測試方法**（195 個無 direct assert 者逐一 1-hop 解析到 asserting helper）；`[DisplayName]` **100%**；零案例 Theory 0；fixture 污染 0（58 個 `SaveXxx` 呼叫點全隔離）；`Thread.Sleep` / `SpinWait` / `WaitOne` / `[Fact(Timeout=)]` 在 `tests/` **完全不存在**，`Task.Delay` 僅 2 處且皆為帶 deadline 的輪詢
- 上輪 N-6（`[Collection]` 讀寫不對稱）**已根治**（三個專案改 `DisableTestParallelization`，註解留下 CI build 編號與 `0x1F` gzip magic 的完整推理鏈）；24 處 `[Collection]` / 4 個名稱 **0 個孤兒**（無打錯字造成隱式分組）
- `AppContext.SetSwitch` / `AppDomain.SetData` / `Directory.SetCurrentDirectory` / `CultureInfo.DefaultThreadCurrentCulture` 在 `tests/` **零出現**

> ⚠️ **上輪「牆鐘 flaky 0」與「`SharedDbFixture` 誤用 0」兩項判定，本輪經深掃後推翻** —— 見 TEST-1、TEST-2、TEST-3。
> 兩者都屬「掃描單位決定盲區」：前者只掃了 `Thread.Sleep` / `Task.Delay` 這類**顯式等待**，
> 沒掃「local 時區 vs production UTC」這種**無等待卻仍與牆鐘綁定**的斷言；
> 後者只掃了 rules 明列的兩條觸發路徑，漏了第三條（API key gate）。
- 死連結 **0 / 1926**（268 份 `.md`，較上輪擴大 49%）；CHANGELOG 雙語 19 版條目數全等；66 對雙語配對語言切換連結零缺漏；ADR 索引 38/38 雙向吻合；analyzer 19/19、reserved names 17/17、運算式函式 5/5、README 套件表 16/16
- **公開文件零 `docs/plans/` 引用**（5 條落地檢查：(2)(4)(5) 完全無輸出，(1)(3) 只剩規範自己記載的固定誤報）
- `Bee.Base.Expressions` 測試已正確分家（`ExpressionPolicyTests` 137 行移入 `Bee.Base.UnitTests`）；`rules/serialization.md` 要求的大小寫解耦回歸測試到位（`FormExpressionCalculatorTests.cs:139,169` 用大寫欄名建 DataTable）

**閘門機制盤點（下輪應確認仍存在且有效）**
- BEE9001（建置期相依鎖）+ `DefinitionDependencyGateTests`（傳遞閉包，**自帶 `DependencyClosure_IsNotVacuous`**）—— 互補設計正確
- `WireContractDriftTests` —— 設計正確但**缺防空轉斷言且只驗單向**（GATE-1）
- CI `-p:DynamicCodeSupport=false` —— **有效但只涵蓋一半**（GATE-3）；且**原理上驗證不到 `MakeGenericType` 那一類**（CoreCLR 不受該開關影響）
- `BoApiSurfaceTests` / `ApiContractPairingTests` / `PayloadZoneCoverageGuardTests` / public API 快照 / BEE3001 / BEE3003 / BEE4005 / BEE4006 —— 全部在位
- **BEE4004 退役乾淨**（`DiagnosticIds` 無殘留、`AnalyzerReleases.Unshipped.md` 只剩 4005/4006、`docs/analyzer-rules*.md` 雙語同步），唯 `MessagePackContract.cs` 死碼未清

---

## 建議執行順序

1. **SEC-1**（型別白名單泛型繞過）—— 唯一實測證實、未認證可達的安全缺陷
2. **REL-1 + REL-2**（版號三欄同步 + CHANGELOG）—— 發版必要條件；REL-1 順帶補 `BEE9002` 閘門
3. **GATE-1 + GATE-2**（兩道閘門的可靠性）—— 合計約 60 行，關閉「宣稱有把關但實際沒有」
4. **X-4 + M-1 + X-5**（趁破壞性視窗的三項收斂）—— 錯過要再開一次破壞性視窗
5. **GATE-3**（CI AOT 閘門擴到兩個專案）—— 改 2 行 yml，實測 0 失敗
6. **SEC-2 + SEC-3 + CON-1**（安全與並行的已知缺陷）
7. **DOC-1**（`bee-serialization` skill）—— 不影響發版但主動誤導後續每一次工作
8. **P-2(a) + CON-2 + CON-4**（三個一行修法，零行為風險）
9. **PERF-1 + P-3 + P-4 + PERF-2**（效能，需實測佐證）
10. P2 其餘 + P3 文件 + P4 裁決

---

## 方法論教訓（下輪沿用）

1. **上輪的「已修」要複驗的是機制，不是那一次。** REL-1 是 C-5 的完整重演：上輪把版號改對了，但沒有加閘門，於是下一版又漏。判別法：**修完問一次「下次還會不會發生？」** 會，就代表只修了症狀。

2. **「新程式碼複製舊程式碼的錯誤形狀」是缺陷擴散的主要途徑。** SEC-1 的 `IndexOf(',')` 寫法從 2026-04-12 的 `ApiPayloadConverter` 原樣複製到 2026-08-10 的 `WireValueFormatter`。下輪掃描時，對每個確認的缺陷追問：**這個形狀在 repo 裡還有幾份？**

3. **P0 值得付出實測成本，而且實測會改變結論。** SEC-1 由代理評 P1（「未能證明可達 RCE」）。主代理建 scratchpad probe 走公開入口實跑，證實**非白名單型別被具現化且屬性 setter 收到攻擊者控制的值**，並以「直接指名同一型別會被擋」作對照組排除誤判 —— 這才把它從推論升格為事實。**對照組是關鍵**：沒有它，無法區分「邊界被繞過」與「邊界本來就沒生效」。

4. **閘門的「防空轉斷言」應列為驗收條件。** 本輪三個新閘門中，兩個有、一個沒有，而那個沒有的正是最新、也最關鍵的（ADR-037 的唯一把關）。同 repo 已有三個正確範式 —— 這是**一致性缺口而非知識缺口**。下輪看到任何新閘門，第一個問題固定是：**它在什麼都沒掃到時會不會通過？**

5. **「文件宣稱有保證」比沒有文件更危險，且本輪出現五次。** `WireContracts` 的「generated」、`BoApiSurfaceTests` 的 DisplayName、`ILogBusinessObject` 的 remarks、`FormDefinitionLoader` 的 `"never"`、`WireValueFormatter` 的 `"never even loaded"`、`ApiKeyCache` 的「負向快取讓沒有 rate limiting 可接受」。**共同形狀**：註解描述了**意圖**，而讀者把它當成**事實**。下輪對每一句帶絕對語氣（never / always / 唯一 / 已由 X 把關）的註解，都要求指出對應的執行路徑。

6. **掃描單位決定盲區，要求代理自述。** 本輪明確要求散落面向自述盲區，得到六條（成員級死碼靠 analyzer 代管而未實跑驗證、識別符索引是「名字」而非「符號」、XAML/Razor 字串式引用、反射慣例式消費者、NuGet 外部消費者不可見）。這比上輪事後才發現「掃的是型別所以漏了 attribute」有用得多。

7. **「乾淨」清單上的項目，要問的是「用什麼掃的」而不是「上輪說乾淨」。** 本輪推翻了兩項基準：
   「牆鐘 flaky 0」只掃了 `Thread.Sleep` / `Task.Delay` 這類**顯式等待**，漏掉「local 時區 vs production UTC」
   這種**無等待卻仍與牆鐘綁定**的斷言（TEST-1，本機每天 8 小時失敗帶）；「`SharedDbFixture` 誤用 0」
   只掃了 rules 明列的兩條觸發路徑，漏了第三條（TEST-2，API key gate）。
   **基準清單應該記「用什麼方法掃的」，而不只是記結論** —— 否則下一輪會用同一個盲區重新確認同一個結論。

8. **「本機紅、CI 綠」這個方向先前沒有被記錄過。** `rules/testing.md` 整節都在講「本機綠、CI 紅」
   （本機環境更完整）。TEST-1 是反方向：**CI 跑 UTC 所以永遠綠，本機 UTC+8 所以每天有 8 小時紅**。
   規則應補上這個對稱情形。

9. **「上輪修法」本身要當成新的攻擊面掃一次。** SEC-2 是 S-3 的直接後果：把一個無淘汰的 map 從「零部署使用」變成「每個預設部署都在用」。下輪固定加一步：**列出上輪所有已落地的修正，逐一問「這個修正引入了什麼新東西？」**
