---
description: 觸發完整模式 CI（四種資料庫 + SonarCloud），等待並回報結果
argument-hint: "[分支名稱，預設 main]"
---

# CI Full

對分支 `$1`（未指定則用 `main`）觸發**完整模式** CI —— 四種資料庫
（SQL Server / PostgreSQL / MySQL / Oracle）全跑 + SonarCloud 掃描，約 8 分鐘。

> `build-ci.yml` 預設是精簡模式（SQL Server + SQLite、跳過 Sonar，約 3.5 分鐘）。
> 判準與「push 前先問使用者」的條文見 `.claude/rules/testing.md` § CI 的資料庫範圍。

## 執行步驟

### 1. 先確認遠端狀態（這步不可略）

`workflow_dispatch` 跑的是**遠端該分支的最新 commit**，不是本機工作區。
本機有未 push 的 commit 或未 commit 的改動時，跑出來的結果**與你眼前的程式碼無關**。

```bash
git status --short
git log --oneline origin/$1..$1
```

有落差就停下來告訴使用者，問要先 push 還是照樣跑遠端現況。**不要默默觸發。**

### 2. 觸發

```bash
gh workflow run build-ci.yml --ref <branch> -f db_scope=all
```

### 3. 取得 run id 並等待

dispatch 不會回傳 run id，隔幾秒查最新一筆 `workflow_dispatch` 事件的 run：

```bash
gh run list --workflow=build-ci.yml -L 5 --json databaseId,event,status,createdAt \
  --jq '[.[] | select(.event=="workflow_dispatch")][0]'
```

以 `gh run watch <id> --exit-status` 背景等待（約 8 分鐘），完成後回報。

### 4. 回報

- **成功** → 回報總時長，並確認四種資料庫的測試都真的跑了
  （檢查 `Wait for extra database containers` 與 `Enable extra database connection strings`
  兩個 step 為 success 而非 skipped；skipped 代表模式判定沒生效，等於白跑一趟精簡模式）。
- **失敗** → 取 `gh run view <id> --log-failed`，依 `pull-request.md` 的「CI 失敗處理」分類：
  明確可修者直接修、commit、push；架構性或語意不明者先向使用者說明。
- **SonarCloud** → 完整模式才會上報覆蓋率。需要時銜接 `/sonar-fix`。

## 何時該用

- 改動觸及 `src/Bee.Db/Providers/**`、`src/Bee.Repository/**`、`SchemaSyntax` /
  `DbTypeMapper` / `NormalizeDbType`，或任何 SQL 產生邏輯
- **發版前**（必跑）
- 想補一次 SonarCloud 掃描（精簡模式不跑 Sonar，issue 會累積到下次完整模式才浮現）

> 另一種觸發方式：commit message（PR 則為 PR 標題）帶 `[all-db]` 標記，push 時即為完整模式。
> 適合「這次改動就是要全驗」的情況，省得事後補跑。
