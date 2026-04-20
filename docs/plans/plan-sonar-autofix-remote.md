# 計畫：遠端 SonarCloud 自動修正（走 PR 流程）

**狀態：✅ 已完成（2026-04-20）**

## 背景

目前遠端 trigger `bee-sonar-fix-daily` 僅做唯讀巡檢（更新 `docs/.sonar-fix-state/snapshot.json`），實際修正仰賴本機 `/sonar-fix`。

希望新增另一個遠端 trigger，自動挑選**低風險**的 Sonar issues 進行修正，透過 **branch + PR** 流程讓 `build-ci.yml` 與人工 review 把關，而不是直接 push main。

## 範圍

### 納入自動修正的規則（白名單）

僅以下格式／死碼／純結構類規則允許自動改：

| Rule | 說明 | 修正風險 |
|------|------|---------|
| **S125** | 移除註解掉的程式碼 | 低 |
| **S1066** | 合併巢狀 if 為 `&&` | 低 |
| **S1116** | 移除多餘空 statement（`;`） | 低 |
| **S1118 / S3442** | static holder 類別加 `sealed` 與 private ctor | 低 |
| **S2094** | 移除空 class | 低 |
| **S2344** | 移除 enum 多餘的 `: int` | 低 |
| **S3260** | 未被繼承的 private nested class 加 `sealed` | 低 |
| **S3604** | 移除已由 ctor 賦值欄位的 inline initializer | 低 |
| **S3878** | 去除 `params` 的明確 array 包裝 | 低 |
| **S4144** | 合併相同實作的方法 | 低 |
| **S6444** | Regex 補 timeout | 低 |
| **S4487 / IDE0052** | 移除未使用 private 成員 | 低 |

### 排除（永遠走本機 `/sonar-fix`）

- 任何安全分類（Vulnerability、Security Hotspot）
- Bug 分類（需理解邏輯，風險高）
- Cognitive Complexity（S3776）等需重構的
- 涉及公開 API 簽章改動
- DB 相關（`[DbFact]` 在遠端跳過，無法驗證 regression）

## 流程

1. 同 daily prompt 抓 SonarCloud issues（帶 `SONAR_TOKEN` auth）
2. 以白名單 rule 過濾，再以 severity（BLOCKER/HIGH/MAJOR 優先）與 effort 排序，**最多取前 N 筆**（建議 N=20，避免單 PR 過大）
3. 建立分支：`claude/sonar-autofix-YYYYMMDD`（若同日重跑，附 `-<hhmm>` 後綴避重）
4. **逐 rule 修正**：
   - Read 目標檔 → Edit → 記錄修改
   - 所有 issue 修完後**一次**執行 `dotnet build --configuration Release`
   - build 失敗 → 停止，回滾分支，log 失敗原因後結束
5. 執行 `dotnet test --configuration Release --settings .runsettings`（`[DbFact]` 會自動 skip）
   - test 失敗 → 停止，回滾，log 失敗測試後結束
6. **commit 策略**：每個 rule 一個 commit，訊息：
   ```
   fix(sonar-autofix): 修正 <Rule> <Rule 描述>（共 N 處）

   by remote sonar-autofix trigger

   Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
   ```
7. `git push origin claude/sonar-autofix-YYYYMMDD`
8. `gh pr create` 建 PR 至 `main`，body 含：
   - 本次修正的 rules 清單與計數
   - 建議 reviewer 檢查要點
   - `@jeff377` tag
9. **不自動 merge**

## 遠端環境需求

新建 environment（不與 daily snapshot 共用，避免變動影響現有運作）：

- **Env vars**
  - `SONAR_TOKEN`（同 daily env 的 token 可共用）
- **Setup script**
  - 安裝 .NET 10 SDK：`curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --version latest --channel 10.0`
  - `dotnet --info` 驗證
- **Network allowlist**
  - `sonarcloud.io`
  - `api.nuget.org`、`*.nuget.org`
  - `dot.net`、`dotnetcli.azureedge.net`、`dotnetbuilds.azureedge.net`（SDK 下載用）
  - `github.com`、`api.github.com`（push + `gh pr create`）
- **Git credentials**
  - 確認 `gh` CLI 在遠端已登入（claude.ai 通常預注入 GitHub App OAuth）；若無則需 PAT

## 排程

- **預設**：每週一 09:30 本地時間（錯開 daily 09:05）
- cron: `30 9 * * 1`
- 或改為 **manual-only**（不設 cron），需要時按 Run

## 硬性限制

- 修正範圍僅限白名單 rules
- build 或 test 失敗 → 停止、回滾、不 push、不建 PR
- 禁止 `--no-verify`、`--force`、`--force-with-lease`
- 禁止直接 push main
- 禁止修改 `src/Directory.Build.props` 版本號（發佈用）
- 禁止動 `.github/workflows/`（CI 流程）
- 禁止動 `.claude/`、`docs/`（除 snapshot.json 外）

## 最終決策

1. **排程頻率**：每週一 08:00 本地時間自動執行（cron `0 8 * * 1`）
2. **白名單 rules**：採用上表列的 12 條規則（全部）
3. **單次 PR 上限**：20 個 issue；PR 結構採 **選項 A**（1 PR / 多 commit，依 rule 分 commit）
4. **GitHub 認證**：遠端 environment 已連 GitHub OAuth（可列舉 repo），直接用 `gh` CLI 即可 push / 建 PR，不需額外 PAT

## 實作步驟（Plan 通過後執行）

1. 建立新 environment（名稱 `bee-sonar-autofix-env`），設 env vars / allowlist / setup script
2. 用 `RemoteTrigger create` 建立新 trigger `bee-sonar-autofix`，掛上該 environment
3. 本機手動觸發 run 驗證：
   - 環境 setup 成功（`dotnet --info` 輸出正確）
   - 能讀取 Sonar issues
   - 能建分支、push、開 PR
4. 驗證成功後正式啟用排程
