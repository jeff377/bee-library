---
description: 監測 main 最新 CI 狀態與 SonarCloud 掃描結果，失敗時自動分析 log 並修正
argument-hint: "[分支名稱，預設 main]"
---

# CI Watch

針對分支 `$1`（未指定則用 `main`）啟動監測流程。

## 執行步驟

使用 `/loop` 機制持續檢查，直到 CI 通過或使用者介入：

```
/loop 監測分支 $1 (未指定時用 main) 的 CI 狀態，規則如下：

1. 檢查 GitHub Actions 最新 run：
   - `gh run list -b <branch> -L 3 --json databaseId,status,conclusion,workflowName,headSha`
   - 若最新 run 狀態為 `in_progress` 或 `queued`，等待下一輪再檢查（選擇較長 delay，例如 240s，配合 build 時間）
   - 若最新 run `conclusion` 為 `success`，進入步驟 2
   - 若最新 run `conclusion` 為 `failure`：
     a. `gh run view <id> --log-failed` 取得失敗 log
     b. 分析失敗原因：分類為「編譯錯誤／測試失敗／lint／格式／環境問題」
     c. 明確可修者：直接修改程式碼、commit、push，commit message 遵循專案慣例（繁中、type(scope)）
     d. 架構性或語意不明者：先停止 loop 並向使用者說明
     e. 修復後進入下一輪，等待新的 CI run 結果

2. 檢查 SonarCloud 掃描結果（若 build-ci.yml 整合了 SonarCloud）：
   - 透過 sonarcloud API 或 gh checks 取最新 quality gate 狀態
   - 若有新增 BLOCKER / HIGH 等級 issue：
     a. 取得 issue 清單與對應程式碼位置
     b. 依 `.claude/rules/sonarcloud.md` 規範修正
     c. commit 並 push，等待下一輪驗證
   - 若僅為 MEDIUM/LOW 或 INFO，記錄並報告，不自動修（由使用者決定是否處理）

3. 通過條件（結束 loop）：
   - GitHub Actions 最新 run 為 success
   - SonarCloud quality gate 為 passed（或無新增 HIGH 以上 issue）
   - 回報摘要：修了哪些問題、剩餘觀察項目

## 規則與限制

- **不修復已知環境性失敗**（如 NuGet 還原暫時失敗、runner 超時）；建議使用者手動 re-run
- **不關閉、不忽略任何 check**
- push 時**禁止**使用 `--no-verify` 或 `--force`
- 每次修復後的 commit 須遵循 `.claude/CLAUDE.md` 的規範
- 連續 3 次同一類問題修不好時，停止並向使用者求助（避免無限迴圈）
- 若使用者當前在其他工作，回報後由使用者決定是否繼續

## 參考規則

- `.claude/rules/pull-request.md`：CI 失敗處理流程
- `.claude/rules/sonarcloud.md`：SonarCloud 規則對照
- `.claude/rules/scanning.md`：SAST 基本安全要求
```
