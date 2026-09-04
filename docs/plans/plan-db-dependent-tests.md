# T-8：71 筆「`[Fact]` 卻需要資料庫」的分類

**狀態：📋 待裁決（2026-09-04）** —— 分類已完成，尚未動任何測試碼。
上游條目見 [plan-framework-review.md](plan-framework-review.md) 的 **T-7 / T-8**。

---

## 這份文件在回答什麼

T-7 記的是 2 筆，窮盡掃描後實際是 73 筆。修掉 `WireFrameReplayTests` 那 2 筆後
仍有 **71 個 test case（67 個相異方法）**，散在 5 個組件。

問題不是「要不要全部標 `[DbFact]`」，而是**逐筆問「這個測試的主題需要資料庫嗎」**：

- **不需要** → 拆掉相依。拆完測試在**每個**環境都跑得起來，是淨賺。
- **需要** → 標 `[DbFact]`，讓它在缺環境時乾淨跳過而不是紅一片。

## 掃描手法（可重跑）

```bash
dotnet test Bee.Library.slnx --configuration Release
```

**關鍵是不要帶 `--settings .runsettings`** —— env var 全空時 `[DbFact]` 會自動跳過，
剩下還會紅的就是違規者。這是窮盡執行，不是 grep 推理。

## ⚠️ 先讀這條：`[DbFact]` 只擋一半

`DbFactAttribute` 的建構子只檢查 `BEE_TEST_CONNSTR_{DBTYPE}` **有沒有設值**，
**不檢查容器連不連得上**。而 `.runsettings` 已入版控、裡面是寫死的連線字串。所以：

| 情境 | env var | `[DbFact]` 行為 | 有效嗎 |
|------|---------|----------------|--------|
| A：`dotnet test` 沒帶 `--settings` | 空 | 跳過 | ✅ 有效（本文 71 筆的來源） |
| B：`./test.sh` 但容器沒開 | **有值** | **照跑** | ❌ 零保護，與 `[Fact]` 一樣紅 |

**「改成 `[DbFact]` 就好了」只對情境 A 成立。** 情境 B 要另想辦法（例如讓 `DbFact`
改為實際探測連線），那是獨立議題，不在本文範圍。

---

## A 類 — 意外相依（34 個方法 / 38 個 case）

**共同根因只有一個**：BO 以裸 `Guid.NewGuid()` 當 access token 建構，而 BO 的方法會
`SessionInfoService.Get(AccessToken)`（查目前公司、取語系）。該權杖不在 cache 內，
於是走 rebuild 路徑讀 `st_session`。

**正解**（已在 `WireFrameReplayTests` 驗證過）：改用
`TestSessionFactory.CreateAccessToken(fx)`，它把 SessionInfo 直接寫進 session 快取，
server 端讀得到就不必 rebuild。拆完通常整個類別也能從 `SharedDbFixture` 降為
`BeeTestFixture`。

| 測試類別 | case | 主題 | 為什麼是意外 |
|---|---|---|---|
| `Bee.Business/AuditLog/LogBusinessObjectTests` | 7 | 清單方法的 filter 透傳、DiffGram 還原、權限 gate | class doc 自己寫「**stub repository，不接實體 DB**」，用的是 `StubAuditLogRepository` + `FakeAuth` |
| `Bee.Business/Form/FormBusinessObjectPermissionGateTests` | 8 | 記錄範圍與明細歸屬的授權判斷 | 同樣是 `FakeAuth` + `FakeFactory`，資料在記憶體 `DataTable` 內建 |
| `Bee.Api.Client/ClientDefineAccessTests` | 8 | `ClientDefineAccess` 的型別化存取與快取 | 定義來自**檔案**，與資料庫無關 |
| `Bee.Business/BusinessObjectFactoryTests` | 4 | BO 建構與 `IsLocalCall` 旗標保留 | 只驗建構結果，不呼叫任何方法 |
| `Bee.Business/BusinessObjectFactoryExtensionsTests` | 4 | 同上，擴充方法版 | 同上 |
| `Bee.Business/Contracts/ReservedProgIdConstructionTests` | 6 | 每個保留 progId 都建得起來（P0-1 的閘門） | 只驗建構 |
| `Bee.Api.Core/JsonRpc/JsonRpcExecutorCoverageTests` | 1 | anomaly 記錄的欄位內容 | 已經用了 `StubSessionInfoService`，DB 是 token 驗證帶進來的 |

### 這不是 34 個獨立疏忽，是同一個權宜做法重複了六次

`LogBusinessObjectTests`、`FormBusinessObjectPermissionGateTests`、
`ClientDefineAccessTests` 三個類別的 class doc **都詳細記載了這條意外路徑**
（「裸 `Guid.NewGuid()` 權杖 → session cache miss → 走 rebuild 路徑讀 `st_session`」），
然後結論都是「所以 fixture 必須是 `SharedDbFixture`」。

**診斷正確，處方相反** —— 該做的是移除那個相依，不是遷就它。
`WireFrameReplayTests` 原本也是這樣寫的（連措辭都近似），2026-09-04 已改掉。

> 這正是「**寫下來 ≠ 解決**」的實例，與 D-1 的「Deliberately parallel / Nothing enforces this」
> 是同一種失效模式：把已知問題寫進註解，然後停在那裡。

---

## B 類 — 真的需要資料庫（33 個方法 / 33 個 case）

主題本身就是資料庫狀態，植入 session 只會把失敗往後推一步。**正解是標 `[DbFact]`。**

| 測試類別 | case | DB 觸發點 | 為什麼是真的 |
|---|---|---|---|
| `SystemBusinessObjectEnterCompanyTests` | 9 | `CompanyRepository.GetById`、測試自己 `NewDbAccess` 植公司/員工資料 | 進公司就是查公司與授權 |
| `SystemBusinessObjectLoginTests` | 4 | `UserRepository.VerifyPassword` / `GetLocale` | 登入本來就查 `st_user` |
| `SystemBusinessObjectLifecycleTests` | 4 | 同上兩者的組合 | 完整登入→進公司→登出流程 |
| `SystemBusinessObjectLogoutTests` | 3 | `SessionRepository.DeleteSession` | 登出要把 session 列刪掉，那是持久化行為 |
| `SystemBusinessObjectLeaveCompanyTests` | 2 | `SessionRepository.UpdateSession` | 同上，離開公司要寫回 |
| `EnterCompanyJsonRpcRoundTripTests` | 2 | 同 EnterCompany | 上述行為的 JSON-RPC 對照組 |
| `LogoutJsonRpcRoundTripTests` | 2 | 同 Logout | 同上 |
| `LeaveCompanyJsonRpcRoundTripTests` | 1 | 同 LeaveCompany | 同上 |
| `AccessTokenValidatorTests` | 1 | `SessionRepository.GetSession` | 要證明「這個 token **不存在**」，就得查得到那個 store |
| `DynamicApiEncryptionKeyProviderTests` | 1 | 同上 | 同上 |
| `ObjectCaching/CacheContainerTests` | 1 | 同上 | `SessionInfo.Get` 在 cache miss 時 read-through 讀 DB，**那是它的契約**；斷言「Remove 之後 Get 回 null」正是在驗這條 |
| `ObjectCaching/CacheTests` | 1 | 同上 | 同上 |
| `Bee.Api.AspNetCore/ApiKeyGateControllerTests` | 1 | 金鑰庫 presence check | 「沒註冊 validator 就退回存在性檢查」要讀得到金鑰庫 |
| `Bee.Api.AspNetCore/ApiAspNetCoreTests` | 1 | 完整 controller → BO 管線 | 端到端整合測試 |

### 判準（下次遇到時照這個問）

**這個測試如果通過了，證明的是「程式邏輯對」還是「資料庫裡的狀態對」？**

- 前者 → 資料庫是雜訊，拆掉它。
- 後者 → 標 `[DbFact]`。

輔助訊號：測試裡出現 `Stub*` / `Fake*` repository 卻仍需要 DB，**幾乎必然是 A 類**。

---

## 建議的做法

**只做 A 類，B 類維持現狀。**

- **A 類是淨賺**：拆完那 38 個 case 在每個環境都跑得起來（含沒有容器的機器、
  IDE 直接跑、未來的輕量 CI），而且執行更快。`WireFrameReplayTests` 拆完就是 10/10 全綠。
- **B 類的回報有限**：標 `[DbFact]` 只換到情境 A 的乾淨跳過，而情境 A 在 CI 不會發生
  （CI 一律有 SQL Server + SQLite），本機 `./test.sh` 也會自動起容器。加上情境 B 完全沒被
  這個改動覆蓋，投入產出比不好。

### A 類的執行順序（由易到難）

1. `BusinessObjectFactoryTests` + `BusinessObjectFactoryExtensionsTests` +
   `ReservedProgIdConstructionTests`（14 case）—— 只驗建構，換 token 即可
2. `ClientDefineAccessTests`（8 case）—— `CreateAccess()` 一處集中改
3. `JsonRpcExecutorCoverageTests`（1 case）
4. `LogBusinessObjectTests` + `FormBusinessObjectPermissionGateTests`（15 case）——
   `Bo(...)` / helper 內建構 BO 的那一行

每一步的驗證都一樣：**帶與不帶 `.runsettings` 各跑一次**，兩邊都要綠。
拆完若整個類別都不碰 DB，順手把 `SharedDbFixture` 降為 `BeeTestFixture`，
並改寫那段講「fixture 必須是 SharedDbFixture」的 class doc —— 前提已不存在，留著會誤導。

> ⚠️ **降 fixture 前務必確認整個類別都拆乾淨了**。`tests/CLAUDE.md` 記的是反向失誤
> （該用 `SharedDbFixture` 卻用了 `BeeTestFixture`），症狀是「本機綠、CI 紅」，很難查。
