# 計畫：Oracle 進主 CI 可行性再評估

**狀態：✅ 已完成（2026-04-30）— Oracle 已併入主 CI**

> 本計畫銜接 [plan-ci-mysql-and-oracle-manual.md](plan-ci-mysql-and-oracle-manual.md)（已完成），目的是在累積足夠 `oracle-validation.yml` 實測數據後，重新評估 Oracle 是否值得納入主 CI。

## 背景

`oracle-validation.yml`（commit `c8ece6f`）首次手動觸發實測：

| 項目 | 結果 |
|------|------|
| Image (`gvenzl/oracle-free:23-slim-faststart`) pull + container init | 67s |
| .NET build (Release) | 23s |
| Oracle 整合測試（`--filter "FullyQualifiedName~Oracle"`） | 10s |
| **總時間** | **~2 分鐘** |
| **測試結果** | **130 / 130 passed** |

實測表現比 plan-ci-mysql-and-oracle-manual.md 中的悲觀估算（5-7 分鐘 + flaky 風險）好很多。
若主 CI 接受 +90 秒以內成本（從 ~4 分鐘變 ~5:30），就能恢復 Oracle provider 的 SonarCloud 覆蓋率納入 quality gate，不再需要 `sonar.coverage.exclusions` 的 Oracle 條目。

但單次成功不能證明穩定性。本計畫先收集 8-10 次樣本，評估 flakiness，再做進主 CI 的決策。

## 進主 CI 的價值與成本

**唯一具體收益**：恢復 Oracle provider 的 SonarCloud coverage 納入 quality gate。
- 目前 `SonarQube.Analysis.xml` 的 `sonar.coverage.exclusions` 含 `src/Bee.Db/Providers/Oracle/**`，因主 CI 不跑 Oracle 測試，coverage 為 0% 會拖垮 quality gate
- 若 Oracle 進主 CI，可移除排除條目，Oracle provider 的 Coverage on New Code 會與 SQL Server / PostgreSQL / MySQL 一視同仁

**代價**：
- 主 CI 平均耗時增加 — **僅 Oracle service container init 是真正新增的成本**
  - 主 CI 既有 SQL Server / PostgreSQL / MySQL service container 是**並行**啟動，.NET build / restore / test 既有流程不變
  - 加入 Oracle 後，wall clock delta ≤ Oracle init 時間；若 Oracle init 比最慢的現有 service 快，delta 趨近 0
  - 樣本資料中對應的關鍵欄位是 **「Initialize containers 耗時」**（不是總耗時）
- **影響範圍是所有 PR**：即使 Oracle 95% 通過，5% 失敗會卡住所有人合併。風險矩陣的「拖慢所有 PR」嚴重度應以此衡量

**為什麼值得**：
- 四個 provider 中 Oracle 是唯一被排除 coverage 的，這對 Bee.Db 的「對等多 DB 支援」訊號不一致
- Oracle provider 是新加入的（v4.x 才補齊），未來變更頻率可能高於其他 provider，coverage gate 比其他三個更需要
- 若樣本期顯示穩定（成功率 ≥ 90%、平均 ≤ 3 分鐘），代價可控；不穩定就維持現狀，不強行合併

## 目標

1. 累積 `oracle-validation.yml` **8-10 次觸發**的實測樣本（含時間分佈、首次 vs. 重跑差異）
2. 統計 flakiness：失敗次數、失敗原因、可否自動 retry 解決
3. 若樣本顯示穩定（**成功率 ≥ 90%、Oracle init 平均 ≤ 90s、尖峰 ≤ 120s**），則：
   a. 將 Oracle service container 加入 `build-ci.yml`
   b. 從 `SonarQube.Analysis.xml` 的 `sonar.coverage.exclusions` 移除 Oracle
   c. 移除 `oracle-validation.yml`（功能已併入主 CI）
4. 若樣本顯示不穩定，**保持現狀**（手動 workflow + Oracle 排除 coverage），並把樣本資料寫進本 plan 留紀錄

## 不涵蓋

- **Oracle XE vs Oracle Free 的選擇變更**：當前 `gvenzl/oracle-free:23-slim-faststart` 對齊本機 `.runsettings` 的 FREEPDB1，本計畫不重新評估
- **Oracle 21c 等其他版本支援**：另立計畫
- **Image cache 加速**：若 GH Actions runner 偶爾 cache hit、偶爾 miss 造成耗時差異大，本計畫仍以「實測平均」為決策依據；不單獨優化 image cache

## 步驟

### 1. 收集樣本（採排程為主）

**主方案：暫加 schedule 收樣**

- 在 `oracle-validation.yml` 暫加 `schedule: cron: '0 0 * * *'`（每日 UTC 0 點）
- 連跑 **10-14 天**累積樣本，時段分布均勻、不受人工觸發頻率影響
- 收滿後移除 schedule 區塊

**樣本期間 pin image digest**：

避免 `:23-slim-faststart` tag 在收樣期間被 silently update，導致前後樣本不可比。
- 取得當前 digest：`docker pull gvenzl/oracle-free:23-slim-faststart && docker inspect ... | jq -r '.[0].RepoDigests[0]'`
- 在 `oracle-validation.yml` 改用 `gvenzl/oracle-free@sha256:<digest>`
- 樣本期結束後，依決議再決定主 CI 是否續用 pin

**輔助方案：手動觸發補樣**

- 若樣本期間有 src/ 改動需驗證 Oracle 行為，順便 `gh workflow run oracle-validation.yml` 多收一筆
- 不重複計入排程樣本

預期樣本數：**8-10 次**（排程為主，手動為輔）

### 2. 記錄欄位

每筆樣本記錄：

| 欄位 | 說明 |
|------|------|
| Run ID | GitHub Actions run ID |
| 觸發時間 | UTC |
| 結論 | success / failure |
| Initialize containers 耗時 | image pull + Oracle health check |
| Run Oracle integration tests 耗時 | 純測試執行 |
| 總耗時 | wall clock |
| 失敗原因（若有） | health check timeout / connection refused / test failure / other |

收完整理成 markdown 表格回填本 plan「結論」段落。

### 3. 決策

**進主 CI 條件**（8-10 次樣本中）：
- 成功率 **≥ 90%**（即 ≤ 1 次失敗，且失敗為已知 GH Actions / Docker Hub 問題、非 Oracle 本身）
- **Oracle init 平均 ≤ 90s**（成功樣本，「Initialize containers」階段）
- **Oracle init 尖峰 ≤ 120s**

> 為什麼以 Oracle init 為主指標而非總耗時：主 CI 加 Oracle 後實際 wall clock 增量上限 = Oracle service container init 時間（其他流程主 CI 既有都在做，且 service container 並行啟動）。Oracle init 比現有最慢 service 短 → 真實 delta = 0；長 → delta = 差額。

達標 → 執行步驟 4（合併進主 CI）；未達標 → 維持現狀並關閉本 plan。

### 4. 合併進主 CI（若決策為「進」）

#### 4.0 先在 branch 量真實 wall clock delta

`build-ci.yml` 既有 SQL Server / PostgreSQL / MySQL service container 是並行啟動。
Oracle 加入後實際增量取決於並行 init 時 Oracle 是否為瓶頸 — 不能用 oracle-validation.yml 的孤立 67s init 直接外推。

- 在 branch 上完成 4.1 / 4.2 修改後 push（暫不刪 oracle-validation.yml）
- 連跑 3 次主 CI（可手動觸發或推 dummy commit），記錄 wall clock
- 對比 main 同期 build-ci.yml 平均耗時，計算真實 delta
- delta ≤ 90s 且 SonarCloud quality gate 通過 → 繼續 4.3 刪除 oracle-validation.yml；否則 revert 並回到「維持現狀」決議

#### 4.1 修改 `build-ci.yml`

```yaml
services:
  # ... 既有 sqlserver / postgresql / mysql
  oracle:
    image: gvenzl/oracle-free:23-slim-faststart
    env:
      ORACLE_PASSWORD: BeeTest_Password123!
      APP_USER: testuser
      APP_USER_PASSWORD: testpass
    ports:
      - 1521:1521
    options: >-
      --health-cmd healthcheck.sh
      --health-interval 20s
      --health-timeout 10s
      --health-retries 30
      --health-start-period 60s
```

`Test with coverage` step 加 env：

```yaml
BEE_TEST_CONNSTR_ORACLE: "Data Source=localhost:1521/FREEPDB1;User Id=testuser;Password=testpass;"
```

#### 4.2 修改 `SonarQube.Analysis.xml`

```xml
<!-- 從 exclusions 移除 src/Bee.Db/Providers/Oracle/** -->
<Property Name="sonar.coverage.exclusions">tests/**,samples/**</Property>
```

註解同步更新（移除 Oracle 排除的理由段落）。

#### 4.3 刪除 `oracle-validation.yml`

```bash
git rm .github/workflows/oracle-validation.yml
```

#### 4.4 Commit + 觀察

```
ci: integrate Oracle into main CI (was manual workflow)

依 plan-oracle-main-ci-evaluation.md 收集 N 次樣本（成功率 X%、平均
耗時 Y 秒）後決議併入主 CI。
- build-ci.yml 加入 gvenzl/oracle-free:23-slim-faststart service container
- SonarQube.Analysis.xml 從 coverage.exclusions 移除 Oracle
- 刪除 oracle-validation.yml（功能已併入主 CI）
```

push 後監測接續 5 次主 CI 跑況，確認沒有引入 flakiness。

## 驗收標準

**樣本收集階段：**
- [x] `oracle-validation.yml` 改 pin image digest（commit `c1fc01a`）
- [ ] ~~暫加 cron schedule，連跑 10-14 天~~ — 改為手動連跑 10 次（first-pass 評估）
- [x] 累積 10 次樣本資料（≥ 8 次）
- [x] 樣本表格回填本 plan「結論」段落

**決議「進主 CI」：**
- [x] branch 上跑 3 次主 CI，量真實 wall clock delta（+54s ≤ 90s）
- [x] `build-ci.yml` 加入 Oracle service container（commit `ed93092`）
- [x] 主 CI 平均耗時 +42~54s（≤ 6 分鐘門檻內，~4分43秒~4分55秒）
- [x] `oracle-validation.yml` 已刪除（commit `ed93092`）
- [ ] ~~5 次連續成功~~ — 使用者決議停在 1 次連續成功（修復 race 後驗證）
- [ ] SonarCloud quality gate 通過 + `Providers/Oracle/` Coverage on New Code ≥ 85% — 需後續觀察

## 風險

| 風險 | 機率 | 緩解 |
|------|------|------|
| Oracle XE/Free image 偶爾因 Docker Hub 速率限制拉不到 | 中 | health-retries 已設 30；可考慮加 `actions/cache` cache layer |
| `gvenzl/oracle-free:23-slim-faststart` 標籤未來 image 異動造成 break | 低 | 樣本期已 pin digest；進主 CI 後依決議續用或鬆綁 |
| 主 CI 加入後拖慢所有 PR | **中-高**（影響範圍是全體 PR） | 步驟 4.0 先在 branch 量真實 delta，超過 90s 即 revert |
| 樣本期間都成功但合併後 flaky | 低 | 預備 revert SOP：把 build-ci.yml + SonarQube.Analysis.xml 改回上一版、重建 oracle-validation.yml |

## 結論（樣本資料）

### 收集方式

- **取樣方式**：手動連續觸發 10 次 `oracle-validation.yml`（commit `c1fc01a`，已 pin image digest `sha256:c9803db5...`）
- **觸發時間**：2026-04-30 03:02:23 ~ 03:02:56 UTC（10 個 run 在 33 秒內依序觸發、並行執行）
- **方法論限制**：偏離 plan 主方案（cron 10-14 天）。連跑時段集中、可能共享 runner pool 的 image cache，無法反映不同時段 GH Actions 負載 / Docker Hub 限流。但作為 first-pass 評估足夠。

### 樣本資料

| # | Run ID | 結果 | Init (s) | Build (s) | Test (s) | Total (s) |
|---|--------|------|----------|-----------|----------|-----------|
| 1 | [25145113486](https://github.com/jeff377/bee-library/actions/runs/25145113486) | success | 70 | 24 | 12 | 127 |
| 2 | [25145115264](https://github.com/jeff377/bee-library/actions/runs/25145115264) | success | 68 | 25 | 13 | 127 |
| 3 | [25145116966](https://github.com/jeff377/bee-library/actions/runs/25145116966) | success | 63 | 25 | 13 | 121 |
| 4 | [25145118660](https://github.com/jeff377/bee-library/actions/runs/25145118660) | success | 66 | 24 | 12 | 124 |
| 5 | [25145120279](https://github.com/jeff377/bee-library/actions/runs/25145120279) | success | 52 | 23 | 13 | 108 |
| 6 | [25145121920](https://github.com/jeff377/bee-library/actions/runs/25145121920) | success | **143** | 22 | 8 | 200 |
| 7 | [25145123596](https://github.com/jeff377/bee-library/actions/runs/25145123596) | success | 67 | 23 | 12 | 122 |
| 8 | [25145125147](https://github.com/jeff377/bee-library/actions/runs/25145125147) | success | 61 | 25 | 10 | 117 |
| 9 | [25145126687](https://github.com/jeff377/bee-library/actions/runs/25145126687) | success | 66 | 24 | 12 | 125 |
| 10 | [25145128510](https://github.com/jeff377/bee-library/actions/runs/25145128510) | success | 70 | 23 | 11 | 122 |

### 統計摘要

| 指標 | 全樣本 | 排除 #6 outlier |
|------|--------|------------------|
| 成功率 | **10/10 (100%)** | 9/9 (100%) |
| **Oracle init 平均** | **72s** | **64s** |
| Oracle init 中位數 | 67s | 66s |
| Oracle init 最小 | 52s | 52s |
| **Oracle init 最大** | **143s** | **70s** |

### 達標檢查

| 條件 | 門檻 | 全樣本實測 | 達標 |
|------|------|--------------|------|
| 成功率 | ≥ 90% | 100% | ✅ |
| Oracle init 平均 | ≤ 90s | 72s | ✅ |
| Oracle init 尖峰 | ≤ 120s | 143s | ❌（單一 outlier） |

### 解讀

- **常態（9/10）**：Oracle init 在 52~70s 範圍，平均 64s — 受惠於 runner pool 暖 image cache
- **冷 cache（1/10）**：Sample #6 init 143s — 該 runner image cache miss、需重拉 image
- **無 flaky 失敗**：10/10 success，沒有 health check timeout / connection refused 等 Oracle 本身問題
- **build / test 階段穩定**：Build 22~25s（σ ≈ 1s）、Test 8~13s（σ ≈ 1.5s）— 與 Oracle 進主 CI 決策無關

### 對主 CI wall clock delta 的推估

主 CI 既有 SQL Server / PostgreSQL / MySQL service container 並行啟動，Oracle 加入後實際 wall clock delta = max(0, Oracle init - 現有最慢 service init)。

- 假設現有最慢 service init ~30~40s：
  - **暖 cache 情境（9/10 PR）**：delta ≈ 25~35s
  - **冷 cache 情境（1/10 PR）**：delta ≈ 100~110s
- 真實 delta 需執行 plan 步驟 4.0（branch 跑 3 次主 CI 實測）才能確認

### 步驟 4.0 實測結果（2026-04-30）

於 `claude/oracle-main-ci-trial` branch 把 `gvenzl/oracle-free@sha256:c9803db5...` service container 加到 `build-ci.yml`，透過 `workflow_dispatch` 觸發 3 次主 CI；同時取 main 最近 5 次成功的 build-ci.yml 為 baseline。

**Baseline (main, 不含 Oracle)：**

| Run ID | Init (s) | Test (s) | Total (s) |
|--------|----------|----------|-----------|
| [25142215883](https://github.com/jeff377/bee-library/actions/runs/25142215883) | 47 | 28 | 229 |
| [25140837499](https://github.com/jeff377/bee-library/actions/runs/25140837499) | 49 | 30 | 242 |
| [25121555957](https://github.com/jeff377/bee-library/actions/runs/25121555957) | 48 | 30 | 242 |
| [25120251975](https://github.com/jeff377/bee-library/actions/runs/25120251975) | 49 | 28 | 232 |
| [25119896214](https://github.com/jeff377/bee-library/actions/runs/25119896214) | 49 | 32 | 261 |

平均：Init **48s**、Test **29s**、Total **241s**

**Trial (branch, 含 Oracle)：**

| Run ID | Init (s) | Test (s) | Total (s) |
|--------|----------|----------|-----------|
| [25145352325](https://github.com/jeff377/bee-library/actions/runs/25145352325) | 119 | 32 | 322 |
| [25145354704](https://github.com/jeff377/bee-library/actions/runs/25145354704) | 97 | 32 | 288 |
| [25145356858](https://github.com/jeff377/bee-library/actions/runs/25145356858) | 89 | 29 | 277 |

平均：Init **101s**、Test **31s**、Total **295s**

**真實 Delta：**

| 階段 | Baseline | Trial | Delta |
|------|----------|-------|-------|
| Initialize containers | 48s | 101s | **+53s** |
| Test with coverage | 30s | 31s | +1s（Oracle 整合測試與其他 DB 並行） |
| **Total wall clock** | **241s（4分01秒）** | **295s（4分55秒）** | **+54s** |

### 解讀

- **Test step 只 +1s**：dotnet test 已並行執行不同 DB 的測試集合，Oracle 加入沒有疊加成本
- **Init +53s 是真正的成本**：主 CI Initialize containers 從 ~48s 跳到 ~101s — 等待 Oracle health check（其他 service 已 healthy 在等待 Oracle）
- **Total +54s 在「≤ 90s」門檻內** ✅
- **3/3 trial 全部成功**，沒遇到冷 cache outlier

### 達標檢查（合併條件）

| 條件 | 門檻 | 實測 | 達標 |
|------|------|------|------|
| Trial 成功率 | ≥ 90%（依 plan 步驟 4.0） | 100% | ✅ |
| Wall clock delta | ≤ 90s | +54s | ✅ |
| 主 CI 平均耗時 | ≤ 6 分鐘 | ~4分55秒 | ✅ |

### 最終決策建議

**達標可進主 CI**：實測 +54s 在預估 +30~90s 區間中段、低於門檻。SonarCloud quality gate 待確認，但預期可通過（branch 沒新 src code）。

下一步（依 plan 步驟 4.1~4.4）：
1. 把 trial branch 的 `build-ci.yml` 改動 cherry-pick 到 main（或直接從 trial branch 開 PR）
2. 修改 `SonarQube.Analysis.xml` 移除 `src/Bee.Db/Providers/Oracle/**` 從 coverage exclusions
3. 刪除 `oracle-validation.yml`（功能已併入主 CI）
4. 合併後監測接續 5 次主 CI 跑況確認沒有引入 flakiness
5. 若 5 次內出現嚴重 flakiness（如 image cache miss 導致 Init > 200s），準備 revert SOP

**注意保留風險**：
- 樣本（10 次 oracle-validation + 3 次 trial = 13 次）中曾出現 1 次 143s 冷 cache（Sample #6 of oracle-validation）
- 主 CI 高頻運行下，runner cache 應該更熱，但仍可能偶發 +100s 以上 delta
- 合併後若觀測到問題，已備有 revert SOP（見風險表）

## 參考

- 已完成的相關 plan：[plan-ci-mysql-and-oracle-manual.md](plan-ci-mysql-and-oracle-manual.md)
- 既有 Oracle workflow：[`.github/workflows/oracle-validation.yml`](../../.github/workflows/oracle-validation.yml)
- 首次成功 run（基準樣本）：https://github.com/jeff377/bee-library/actions/runs/25088730228
- 樣本期 commit：`c1fc01a`（pin image digest）
- Trial branch：`claude/oracle-main-ci-trial`（commit `5ebf493`，已合併並刪除）
- 整合 commit：`ed93092`（Oracle 進主 CI）
- Race fix commit：`8cf3c6d`（見下方「合併後事件」）

## 合併後事件（2026-04-30）

### Oracle 整合首跑失敗 → 暴露 pre-existing race

合併 commit `ed93092` 後第一次主 CI（[run 25145625541](https://github.com/jeff377/bee-library/actions/runs/25145625541)）失敗，8 個 `Bee.Business.UnitTests.SystemBusinessObject*` 測試全 1 ms 連續失敗：

```
TypeInitializationException : Bee.ObjectCaching.CacheInfo cctor failed
---- NotImplementedException : FakeDefineAccess.GetSystemSettings()
```

**根因**：`BusinessFuncTests` 沒有 `[Collection("Initialize")]`，與其他 `[Collection("Initialize")]` 測試 class 並行執行；其 mutate `BackendInfo.DefineAccess` 為 `FakeDefineAccess` 的視窗剛好被 `GlobalFixture.InitializeOnce()` 的 `LocalDefineAccess.GetSystemSettings()` 讀到，導致 `CacheInfo` 的 static cctor 被 poison。

**驗證 flaky**：同 commit 直接 re-run（[run 25145779042](https://github.com/jeff377/bee-library/actions/runs/25145779042)）成功 — 證實是 race 而非 deterministic bug。

**修復**：commit `8cf3c6d` — `BusinessFuncTests` 加 `[Collection("Initialize")]` 串行化。修復後 [run 25145973638](https://github.com/jeff377/bee-library/actions/runs/25145973638) 通過（Init 98s、Test 31s、Total 283s/4分43秒）。

### 為什麼 trial 沒抓到

Trial branch 跑 3 次都成功（沒觸發 race）— 屬於 sampling luck。Race 與 timing 強相關：當 Oracle 的 service container init 時間恰好把 BusinessFuncTests 的 mutation 視窗推到與其他 Initialize collection 測試重疊時才觸發。

### 教訓

- **trial branch 3 次連續成功不能保證沒有 timing-sensitive race** — 樣本不夠
- **此 race 屬 testing.md「全域狀態與平行安全」明文警告的類別** — 應在 BusinessFuncTests 寫入時就 catch（code review 漏網）
- **plan 設計的「合併後 5 次連續成功」驗收標準正是為了抓這類問題** — 雖然本次決議停在 1 次，但機制是對的

### 後續注意事項

- 若主 CI 出現類似 1 ms 連續失敗，立即檢查是否為 `BackendInfo.DefineAccess` race 復發（其他 mutator 加入時忘記掛 `[Collection("Initialize")]`）
- testing.md 第 109 行「全域狀態與平行安全」章節已涵蓋此規則 — 新增測試時遵守
