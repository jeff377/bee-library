---
description: 巡檢 SonarCloud issues 與 test 覆蓋率，自動修正並補測試（手動模式）或產生差異報告（daily 模式）
argument-hint: "[--mode=daily|fix，預設 fix]"
---

# Sonar Fix — 品質巡檢

對 SonarCloud 專案 `jeff377_bee-library` 執行品質巡檢，依 `$1` 決定模式：
- **未指定 / `--mode=fix`**：手動模式（本機 session，完整修正閉環）
- **`--mode=daily`**：每日自動模式（只查詢 + 比對快照 + 輸出差異報告，不改碼）

相關計畫：`docs/plans/plan-sonar-fix.md`
狀態檔：`docs/.sonar-fix-state/`

## 共用：FETCH 階段

SonarCloud API 對 public project 可匿名讀取，以下查詢無需 token：

```bash
BASE="https://sonarcloud.io/api"
KEY="jeff377_bee-library"

# 1. Quality gate
curl -s "$BASE/qualitygates/project_status?projectKey=$KEY"

# 2. Leak period open/confirmed issues
curl -s "$BASE/issues/search?componentKeys=$KEY&issueStatuses=OPEN,CONFIRMED&sinceLeakPeriod=true&ps=500"

# 3. 全專案 open/confirmed issues（供比對「新增」）
curl -s "$BASE/issues/search?componentKeys=$KEY&issueStatuses=OPEN,CONFIRMED&ps=500"

# 4. 整體 coverage
curl -s "$BASE/measures/component?component=$KEY&metricKeys=coverage,ncloc,bugs,vulnerabilities,code_smells"

# 5. 各檔案 coverage（由低到高排序）
curl -s "$BASE/measures/component_tree?component=$KEY&metricKeys=coverage&qualifier=FIL&ps=500&s=metric&metricSort=coverage&asc=true"
```

> 若 API 未來加上匿名存取限制，可於 `~/.zshrc` 設 `SONAR_TOKEN`，呼叫時加 `-u "$SONAR_TOKEN:"` 即可。

---

## 模式 A：daily（每日自動，由 `/schedule` 觸發）

### 流程

1. 執行 FETCH 取得當前狀態
2. 讀取 `docs/.sonar-fix-state/snapshot.json` 前次快照
3. 比對差異：
   - **新增的 leak BLOCKER / HIGH issue**（以 issue key 比對）
   - **整體 coverage 下降超過 0.5%**（前次 vs 當前）
   - **quality gate 由 PASSED/OK 轉 FAILED/ERROR**
4. 輸出差異報告（純日誌；**不發 PushNotification/email**）：
   - 當前 quality gate、coverage、issue 總數
   - 若有觸發條件：逐項列出新增 issue key、檔案位置、嚴重度
   - 若無變化：輸出一行 `no changes since <capturedAt>`
5. 更新 `docs/.sonar-fix-state/snapshot.json` 為當前狀態；commit 到 main（訊息：`chore(sonar-fix): 更新每日快照 YYYY-MM-DD`）
6. **不改任何程式碼**。若偵測到問題，建議訊息：「請本機執行 `/sonar-fix` 進行修正」

### 執行規則

- Daily 模式只做唯讀分析 + snapshot.json commit，不跑 `dotnet build` / `dotnet test`
- 若快照 commit 失敗（例如衝突），停止並回報，不重試
- 此模式適合在 remote agent 環境執行，不依賴 .NET SDK

---

## 模式 B：fix（手動，本機 session）

手動模式用 `/loop` 反覆執行直到結束條件成立。

### 初始化

1. FETCH 取得當前 issue list 與 coverage
2. 讀取 `docs/.sonar-fix-state/skip.json`，過濾掉已放棄的 issue key 與檔案路徑
3. 紀錄起點：issue set、整體 coverage、quality gate

### 主迴圈（每輪）

```
/loop 依下列規則反覆執行：

1. 重新 FETCH（第一輪用初始化結果）

2. 檢查結束條件（任一成立即停）：
   a. quality gate = OK/PASSED 且整體 coverage >= 90% 且無未處理的 BLOCKER/HIGH issue
   b. 所有剩餘待處理項目都已在 skip.json 內
   c. 連續 2 輪無任何進展（issue 數、coverage 皆未變化）

3. 決定本輪處理對象（依優先序，每輪最多 5 項避免 diff 過大）：
   a. BLOCKER / CRITICAL / MAJOR issue（以 severity 排序）
   b. MINOR / INFO issue
   c. 檔案 coverage < 70%（按絕對缺口排序）
   d. 檔案 coverage 介於 70-90%（按絕對缺口排序，僅整體 < 90% 時才處理）

4. 修正 issue（對映使用者流程步驟 2）：
   a. 對照 .claude/rules/sonarcloud.md 與 .claude/rules/scanning.md 的規則表
      - 找不到對應規則的 issue：直接加入 skip list（原因：unknown rule）
   b. 對每個 issue 維護 attempts 計數（in-memory，本次 session 內）
   c. 套用修改後執行：
      dotnet build --configuration Release --no-restore
      dotnet test <受影響專案>.csproj --configuration Release --settings .runsettings
   d. 驗證通過 → 保留 staged change；失敗 → git restore，attempts+1
   e. attempts >= 3 → 寫入 docs/.sonar-fix-state/skip.json（issues 區塊），附原因

5. 補覆蓋率（對映使用者流程步驟 3）：
   a. 依 .claude/rules/testing.md 命名規則新增 [Fact] / [Theory]
      - 命名：<方法名>_<情境>_<預期結果>
      - 加 [DisplayName] 中文說明
      - 需 DB 用 [DbFact]；需本機服務用 [LocalOnlyFact]
   b. **嚴禁**修改既有測試 assertion、public API 簽章、csproj 相依版本
   c. 執行相同 build + test 驗證
   d. 檔案覆蓋率達目標（>= 90% 或相對提升 >= 20pp） → 成功
   e. attempts >= 3 → 寫入 skip.json（files 區塊），附原因

6. 本輪若有 staged change：
   a. commit（訊息格式：
      chore(sonar-fix): 處理 X 項 issue、補 Y 個檔案覆蓋率

      by /sonar-fix
      Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
   b. push origin main
   c. 呼叫 /ci-watch 盯 CI + quality gate 通過（不自行實作此邏輯）

7. 進入下一輪，回到步驟 1

8. 結束後輸出摘要：
   - 修了哪些 issue（key + rule + component）
   - 補了哪些檔案測試
   - 本次新增的 skip list 項目與原因
   - 整體 coverage：起始 X% → 結束 Y%
   - quality gate：起始 → 結束
```

### 安全與限制（強制）

- 自動修復只碰 `src/`，不改 `samples/`
- 補測試只在 `tests/<Module>.UnitTests/` 新增檔案或 `[Fact]`；不改既有 assertion
- **禁止**觸碰：
  - public API 簽章（method signature、class visibility）
  - 加密／session 管線（`Bee.Base/Cryptor/*`、`Bee.Api.Core/Session/*`）
  - csproj / Directory.Build.props 的相依版本
- 每次 commit 前強制 `dotnet build --configuration Release`
- 每次 commit 前強制 `dotnet test` 受影響專案（全專案測試太慢，僅跑改動檔案對映的 `tests/<Module>.UnitTests`）
- commit 訊息用繁中、`type(scope): ...`，body 標註「by /sonar-fix」
- push **禁止** `--no-verify` / `--force`

### 連續無進展處理

若 2 輪結束後 issue 總數、coverage、skip list 都無變化 → 立即停止，輸出：
```
/sonar-fix 連續 2 輪無進展，已停止。請檢查剩餘項目是否需人工介入。
```

### skip list 管理

- 每次寫入 skip.json 都包含：attempts、reason、lastAttempt（ISO 日期）
- 若使用者後續修復了某項目，可手動從 `docs/.sonar-fix-state/skip.json` 移除
- skip.json 的 commit 合併於同一次 `chore(sonar-fix): ...` 提交

---

## 參考規則

- `.claude/rules/sonarcloud.md`：SonarCloud 規則對照表
- `.claude/rules/scanning.md`：SAST 基本安全要求
- `.claude/rules/testing.md`：測試撰寫模式
- `.claude/rules/pull-request.md`：push/CI 失敗處理
- `.claude/commands/ci-watch.md`：push 後盯 CI 的下游 skill
