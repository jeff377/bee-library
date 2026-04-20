# 計畫：新建 `/sonar-fix` 品質巡檢 skill（SonarCloud Issues + Coverage）

**狀態：🚧 進行中**

## 背景

目前 `.claude/commands/ci-watch.md` 是「push 後盯 CI 通過」的通用工具，任何一次 push 都可能使用它，**不該為了新需求改寫它**。

使用者希望新增一個**常態品質巡檢流程**：每日（或手動）自動抓 SonarCloud 的 issues 與 test 覆蓋率、自動修正、補測試。這個流程完成後會 push 到 main，屬於「push 的源頭之一」，push 之後再由既有的 `/ci-watch` 接手盯 CI。

參考兩個 SonarCloud 頁面：
- [Leak period open/confirmed issues](https://sonarcloud.io/project/issues?issueStatuses=OPEN%2CCONFIRMED&sinceLeakPeriod=true&id=jeff377_bee-library)
- [Coverage by file](https://sonarcloud.io/component_measures?id=jeff377_bee-library&metric=coverage&view=list)

## 角色分工

| Skill | 職責 | 呼叫關係 |
|-------|------|---------|
| `/ci-watch`（**不動**） | push 後盯 GitHub Actions + quality gate 通過 | 被動被呼叫 |
| `/sonar-fix`（**新建**） | 查 SonarCloud → 修 issue → 補測試 → commit & push → 呼叫 `/ci-watch` | 主動觸發 |

## 使用者需求（逐字對映流程）

1. 抓 SonarCloud 的 Issue 及 test 覆蓋率
2. 有任何 issues 就啟動修正，嘗試全部修正；同一 issue 超過 3 輪無法修正就略過
3. test 覆蓋率不滿 90% 就補覆蓋率；同一目標超過 3 輪無法達標就暫停
4. 上述二個動作若產生新 issues / 覆蓋率下降時，重覆 2 及 3

## 執行環境考量（關鍵風險）

`/schedule` 建的是 **remote cron agent**，不是本機 session：

- ✅ 可執行：`gh`、`curl`（SonarCloud API）、讀寫檔、git commit/push
- ❓ 不確定：`dotnet build` / `dotnet test`（remote agent 未必有 .NET 10 SDK；即使有也沒有 SQL Server service container）

**因應策略：`/sonar-fix` 支援雙模式**

| 模式 | 觸發方式 | 職責 |
|------|---------|------|
| **A. daily（每日自動，remote cron）** | `/schedule` → `/sonar-fix --mode=daily` | 只查 SonarCloud + 比對快照；有變化 PushNotification 通知；**不改碼** |
| **B. fix（手動，本機 session）** | 使用者直接 `/sonar-fix` | 完整「查 → 修 → 補測試 → 驗證 → push → 呼叫 `/ci-watch`」閉環 |

> 若後續實測 remote agent 能完整 build + test，再合併為單一模式。

## 變更範圍

### 1. 新建 `.claude/commands/sonar-fix.md`

以 `/loop` 封裝主流程（手動模式），結構概述：

#### 1.1 共用查詢（FETCH）

```bash
# 1. Quality gate 狀態
curl -s -u "$SONAR_TOKEN:" \
  "https://sonarcloud.io/api/qualitygates/project_status?projectKey=jeff377_bee-library"

# 2. Leak period open/confirmed issues（對映第一個 URL）
curl -s -u "$SONAR_TOKEN:" \
  "https://sonarcloud.io/api/issues/search?componentKeys=jeff377_bee-library&issueStatuses=OPEN,CONFIRMED&sinceLeakPeriod=true&ps=500"

# 3. 全專案 open/confirmed issues（用於比對「新增」）
curl -s -u "$SONAR_TOKEN:" \
  "https://sonarcloud.io/api/issues/search?componentKeys=jeff377_bee-library&issueStatuses=OPEN,CONFIRMED&ps=500"

# 4. 整體 coverage + 各檔案 coverage（對映第二個 URL）
curl -s -u "$SONAR_TOKEN:" \
  "https://sonarcloud.io/api/measures/component?component=jeff377_bee-library&metricKeys=coverage"
curl -s -u "$SONAR_TOKEN:" \
  "https://sonarcloud.io/api/measures/component_tree?component=jeff377_bee-library&metricKeys=coverage&qualifier=FIL&ps=500"
```

`$SONAR_TOKEN` 本機從 `~/.zshrc` 讀取；remote agent 需在 schedule 設定時以 secret 注入。

#### 1.2 手動模式（B）主迴圈

```text
初始化：
  - 載入 .claude/state/sonar-fix-skip.json 過濾已放棄項目
  - 紀錄起點 snapshot（issue set、coverage）

LOOP（每輪）:
  1. FETCH 取得 issue list 與 coverage
  2. 決定本輪處理對象（依優先序）：
     a. BLOCKER / HIGH issue
     b. MEDIUM issue
     c. 覆蓋率 < 90% 的檔案（依缺口大小排序）
  3. 修 issue（步驟 2 對映使用者流程 2）：
     - 對應 .claude/rules/sonarcloud.md / scanning.md 規則表
     - 每個 issue 維護 attempt_count
     - 修改後本機 dotnet build + dotnet test 驗證
     - 成功 → 暫存 commit（本輪結束時統一 push）
     - 失敗 → attempt_count+1；>=3 寫入 skip list 並附原因
  4. 補覆蓋率（步驟 3 對映使用者流程 3）：
     - 依 testing.md 命名規則新增 [Fact]/[Theory]
     - 不修既有測試、不動 public API 簽章
     - 同樣 3 次重試上限；達標或略過寫入 skip list
  5. 本輪若有 commit：push 一次 → 呼叫 `/ci-watch` 盯到 CI 通過
  6. 重新 FETCH，比對前後差異（使用者流程 4）：
     - 新增 BLOCKER/HIGH issue → 下一輪回到步驟 3
     - 整體 coverage 下降 → 下一輪回到步驟 4
  7. 結束條件（任一成立即停）：
     - quality gate = PASSED 且整體 coverage >= 90% 且無未處理 HIGH+ issue
     - 所有剩餘項目都已進 skip list
     - 連續 2 輪無任何進展（避免死循環）
  8. 回報摘要（永遠輸出）：
     - 修了哪些 issue、補了哪些檔案
     - skip list 新增了什麼及原因
     - 整體 coverage 起始 → 結束
```

#### 1.3 daily 模式（A）流程

```text
1. FETCH
2. 與上次快照（.claude/state/sonar-fix-snapshot.json）比對：
   - 新增的 leak period BLOCKER/HIGH issue
   - coverage 比前日下降超過 0.5%
   - quality gate 由 PASSED 轉 FAILED
3. 有任一觸發條件：
   - 以 PushNotification 通知使用者
   - 附 issue key、檔案位置、coverage 變化
   - 建議訊息：「請本機執行 /sonar-fix 進行修正」
4. 更新快照檔、結束；**不修改任何程式碼**
```

### 2. 狀態檔

- `.claude/state/sonar-fix-skip.json`
  ```json
  {
    "issues": {
      "AZxxxx": {
        "key": "AZxxxx",
        "rule": "csharpsquid:S1234",
        "component": "src/Bee.Db/...",
        "attempts": 3,
        "reason": "修正後引發測試失敗且無法在不改 public API 下解決",
        "lastAttempt": "2026-04-20"
      }
    },
    "files": {
      "src/Bee.Db/Schema/xxx.cs": {
        "attempts": 3,
        "reason": "涉及內部 reflection 呼叫，無法以單元測試涵蓋",
        "lastAttempt": "2026-04-20"
      }
    }
  }
  ```
- `.claude/state/sonar-fix-snapshot.json`（daily 模式用）

### 3. 建立 `/schedule` cron

```
名稱：bee-sonar-fix-daily
排程：每日 09:00 Asia/Taipei
執行：/sonar-fix --mode=daily
Secret：SONAR_TOKEN
通知：PushNotification（僅當有變化）
```

### 4. 觸發方式

- **本機**：`/sonar-fix` → 進入手動模式，完整修正流程（push 後自動 `/ci-watch`）
- **遠端重跑 daily**：`/schedule run bee-sonar-fix-daily`

## 安全與限制

- 自動修復只處理 `sonarcloud.md` / `scanning.md` 已列規則的機械式修正；不碰：
  - public API 簽章
  - 加密／安全邏輯（`Bee.Base/Cryptor/*`、session 管線）
  - csproj 的相依性版本
- 補測試僅新增 `[Fact]` / `[Theory]`，不改既有測試 assertion
- 每次 commit 前本機 `dotnet build --configuration Release` + `dotnet test --settings .runsettings`
- commit 訊息遵循專案慣例（繁中、`type(scope): ...`），並於 body 標註「by /sonar-fix」
- push 後呼叫 `/ci-watch`；`/sonar-fix` 不自行實作盯 CI 邏輯
- 3 次重試上限硬性執行；連續 2 輪無進展強制停止

## 已確認決議（2026-04-20）

1. **狀態檔位置**：`docs/.sonar-fix-state/`（git 追蹤，跨機器共享）
2. **每日執行時間**：09:00 Asia/Taipei
3. **90% 覆蓋率門檻**：整體 ≥ 90% 為止損；檔案層級優先處理 < 70%
4. **通知通道**：僅日誌（不發 PushNotification / email；使用者可用 `/schedule` CLI 或手動執行查看結果）
5. **remote agent .NET 10 SDK**：暫不實測，維持雙模式（daily 只查詢）
6. **排除範圍**：`samples/` 不動；`tests/` 僅接受「新增測試」；`src/` 全納入

## 落地步驟（確認後執行）

1. 新建 `.claude/commands/sonar-fix.md`（雙模式）
2. 建立 `.claude/state/` 骨架與空的 skip list
3. 視決定加入 `.gitignore`
4. 用 `/schedule` 建立 `bee-sonar-fix-daily` trigger
5. 乾跑一次 daily 模式（僅查詢、輸出報告）驗證 API 呼叫與狀態檔
6. 手動跑一次 `/sonar-fix` 處理當前積欠的 issue（最後會 push + 呼叫 `/ci-watch`）
7. commit `.claude/` 的變更到 main

## 完成判定

- `/sonar-fix --mode=daily` 能獨立查詢、比對快照、必要時推通知
- `/sonar-fix`（手動）能跑完閉環：修 issue、補測試、push、呼叫 `/ci-watch`、回報摘要
- `/schedule list` 看到 `bee-sonar-fix-daily` 並成功跑過至少一次
- `/ci-watch` 本身未被修改
