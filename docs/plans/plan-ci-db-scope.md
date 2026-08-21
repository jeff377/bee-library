# 計畫：CI 依條件決定跑哪些資料庫測試

**狀態：🚧 進行中（2026-08-21）** —— 實作已落地，待 CI 實測兩種模式各一次

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | `build-ci.yml` 條件化 + 規則與文件同步 | ✅ 已完成（2026-08-21） |
| 2 | CI 實測：精簡模式與完整模式各成功一次 | 🚧 進行中 |

## 背景

`build-ci.yml` 目前每次觸發都起四個資料庫 service container（SQL Server / PostgreSQL /
MySQL / Oracle）並跑完整測試。單人開發、日常直接 push `main` 的節奏下，多數改動與
provider dialect 無關，這筆固定成本每次都付。

### 量測基準（run 32337882346，2026-08-20，總長 479 秒）

| Step | 耗時 |
|------|------|
| Initialize containers | **104 秒** |
| Strict build (analyzer gate) | 57 秒 |
| SonarScanner Begin | 6 秒 |
| Build (for Sonar coverage) | 69 秒 |
| Test with coverage | **77 秒** |
| SonarScanner End | 74 秒 |
| Pack NuGet packages | 20 秒 |
| Mobile AOT gate | 32 秒 |
| 其餘（checkout / setup / sqlcmd…） | 約 40 秒 |

**關鍵事實：四種資料庫的全部測試合起來只花 77 秒，四個容器起來卻要 104 秒。**
因此只 gate 環境變數（讓測試 skip）省不到 30 秒 —— 要有感必須連容器一起條件化。

### 現成的機制

`[DbFact(DatabaseType.X)]` / `[DbTheory]` 的 skip 判斷只看
`BEE_TEST_CONNSTR_{DBTYPE}` 有沒有值（`../../tests/Bee.Tests.Shared/DbFactAttribute.cs`），
而 `SharedDatabaseState.EnsureRegistered` / `EnsureSchemaAndSeed` 對未設值的 DB 整條跳過註冊
與建 schema（`../../tests/Bee.Tests.Shared/SharedDatabaseState.cs`）。

**因此本計畫不需修改任何測試碼**，改動全部落在 `.github/workflows/build-ci.yml`。

各 DB 的測試分佈（`[DbFact]` / `[DbTheory]` 標註數）：
SQLServer 152、SQLite 106、PostgreSQL 48、Oracle 36、MySQL 31。

## 目標與非目標

**目標**：`main` 的日常 push 只跑 SQL Server + SQLite（精簡模式），需要時以明確指定
切換為四種資料庫全跑（完整模式）。

**非目標**：
- 不改任何測試碼、不改 `test.sh`、不改 `.runsettings`（本機行為完全不變）。
- 不拆 matrix job（Sonar coverage 收集維持單一 job，避免報告合併的複雜度）。
- 不調整 Strict build、Pack、Mobile AOT gate 三道閘門的涵蓋範圍 —— 它們與 DB 無關，兩種模式都跑。

## 設計

### 1. 模式判定

兩種指定方式（純加法，無取捨）：

| 觸發 | 判定 |
|------|------|
| `workflow_dispatch` 且 input `db_scope=all` | 完整 |
| push / PR 的 commit message 含 `[all-db]` | 完整 |
| 其餘 | 精簡 |

```yaml
on:
  workflow_dispatch:
    inputs:
      db_scope:
        description: '資料庫測試範圍'
        type: choice
        options: [all, lite]
        default: all
```

```yaml
- name: Resolve database scope
  id: scope
  env:
    DISPATCH_SCOPE: ${{ github.event.inputs.db_scope }}
    COMMIT_MSG: ${{ github.event.head_commit.message }}
  run: |
    full=false
    if [ "$DISPATCH_SCOPE" = "all" ]; then
      full=true
    elif printf '%s' "$COMMIT_MSG" | grep -qF '[all-db]'; then
      full=true
    fi
    echo "full=$full" >> "$GITHUB_OUTPUT"
    echo "Database scope: $([ "$full" = true ] && echo 'all (4 databases)' || echo 'lite (SQL Server + SQLite)')"
```

> **commit message 必須經 `env:` 注入，不可在 `run:` 內直接展開 `${{ }}`** ——
> 那是 GitHub Actions 的經典 script injection 破口（commit message 是使用者可控字串）。
> 與 `sonarcloud.md` 的 S7636 同一條理由。

### 2. 容器條件化（`services:` 不支援 `if` 的繞法）

GitHub Actions 的 `services:` 區塊**無法條件化**。做法是**依模式拆兩邊**：

- **SQL Server 留在 `services:`** —— 兩種模式都需要，永遠啟動，行為不變。
- **PostgreSQL / MySQL / Oracle 移出 `services:`，改由 step 內 `docker run` 啟動**，
  掛 `if: steps.scope.outputs.full == 'true'`。

runner 直接跑在 host 上（非 container job），`-p` 對映後 `localhost:{port}` 可達，
與 services 的行為一致。`psql` / `mysql` client 為 ubuntu-latest 預裝（現行 workflow 已在用）。

image pull 並行化以免序列拉取變慢（Oracle image 約 1.5GB）：

```yaml
- name: Start extra database containers
  if: steps.scope.outputs.full == 'true'
  run: |
    docker pull postgres:17 &
    docker pull mysql:8.0 &
    docker pull gvenzl/oracle-free@sha256:c9803db54238b5be268c7f5916e3c5811ef106e58aeb2f1f9b690c135ed50672 &
    wait
    docker run -d --name bee-pg -p 5432:5432 \
      -e POSTGRES_USER=testuser -e POSTGRES_PASSWORD=testpass -e POSTGRES_DB=common postgres:17
    docker run -d --name bee-mysql -p 3306:3306 \
      -e MYSQL_ROOT_PASSWORD=BeeTest_Password123! -e MYSQL_DATABASE=common \
      -e MYSQL_USER=testuser -e MYSQL_PASSWORD=testpass mysql:8.0
    docker run -d --name bee-oracle -p 1521:1521 \
      -e ORACLE_PASSWORD=BeeTest_Password123! -e APP_USER=testuser \
      -e APP_USER_PASSWORD=testpass \
      gvenzl/oracle-free@sha256:c9803db54238b5be268c7f5916e3c5811ef106e58aeb2f1f9b690c135ed50672
```

等待就緒（取代 services 的 `--health-cmd`，沿用各 image 內建的健康檢查）：

```yaml
- name: Wait for extra database containers
  if: steps.scope.outputs.full == 'true'
  run: |
    wait_for() {
      local name="$1"; shift
      for _ in $(seq 1 90); do
        if docker exec "$name" "$@" > /dev/null 2>&1; then
          echo "$name ready"; return 0
        fi
        sleep 2
      done
      echo "$name did not become ready in time" >&2; exit 1
    }
    wait_for bee-pg     pg_isready -U testuser
    wait_for bee-mysql  mysqladmin ping -h localhost -uroot -pBeeTest_Password123!
    wait_for bee-oracle healthcheck.sh
```

> 這段等待迴圈是本計畫的主要新增複雜度，也是唯一需要在 CI 上實測調參的部分
> （Oracle 冷啟動 45~90 秒，timeout 給到 180 秒）。

### 3. 連線字串條件注入

完整模式的三條連線字串改由容器啟動 step 寫入 `$GITHUB_ENV`，精簡模式下它們**根本不存在**，
`[DbFact]` 自動 skip：

```yaml
    {
      echo 'BEE_TEST_CONNSTR_POSTGRESQL=Host=localhost;Port=5432;Database={@DbName};Username=testuser;Password=testpass'
      echo 'BEE_TEST_CONNSTR_MYSQL=Server=localhost;Port=3306;Database={@DbName};User=testuser;Password=testpass;'
      echo 'BEE_TEST_CONNSTR_ORACLE=Data Source=localhost:1521/FREEPDB1;User Id=testuser;Password=testpass;'
    } >> "$GITHUB_ENV"
```

`Test with coverage` 與 `Mobile AOT gate` 兩個 step 的 `env:` 只保留
`BEE_TEST_CONNSTR_SQLSERVER` 與 `BEE_TEST_CONNSTR_SQLITE`，其餘由 `$GITHUB_ENV` 供應。
兩個 step 因此自動一致，不會漂。

### 4. 建資料庫 step 拆分

現行的 `Create test databases (common + company + log)` 同時處理三種 DB，需拆為：

- SQL Server 那段 → 保持無條件執行。
- PostgreSQL + MySQL 兩段 → 併入完整模式，掛 `if`。

### 5. Sonar 條件化（精簡模式跳過）

`SonarScanner Begin` / `Build (for Sonar coverage)` / `SonarScanner End` 三個 step
加 `if: steps.scope.outputs.full == 'true'`，另可一併條件化 `Setup Java`、
`Cache SonarCloud packages`、`Install SonarScanner`（純節省，無行為影響）。

**跳過 `Build (for Sonar coverage)` 不影響後續 `--no-build` 的步驟** ——
`Strict build (analyzer gate)` 已用同一 `Release` configuration 建過，產物在。
該 step 的 `--no-incremental` 存在的理由是讓 SonarScanner 攔得到完整編譯，非產物需求。

coverage 收集同步條件化（精簡模式不產報告）：

```yaml
run: >-
  dotnet test Bee.Library.slnx --configuration Release --no-build --verbosity normal
  ${{ steps.scope.outputs.full == 'true' && '--collect:"XPlat Code Coverage;Format=opencover"' || '' }}
```

## 預期效果

| 模式 | 預估時長 | 對照現況 |
|------|---------|---------|
| 精簡（日常 push） | 約 200~220 秒 | 479 秒 → 省過半 |
| 完整（`[all-db]` / 手動） | 約 480 秒 | 與現況相當 |

精簡模式省下的來源：容器初始化 104 → 約 30 秒、Sonar 三步 149 → 0、測試 77 → 約 50 秒。

## 風險與取捨

| 風險 | 判定 |
|------|------|
| **日常 push 不再被 Sonar 掃描**，issue 延後到下次全跑才發現 | 已於 2026-08-21 確認接受。`/sonar-fix`、`/ci-watch` 仍可手動觸發完整模式補掃 |
| **PG / MySQL / Oracle dialect 的迴歸延後發現** | 改到 `src/Bee.Db/Providers/**` 時記得帶 `[all-db]`；發版前必跑一次完整模式（列入發版前置） |
| 等待迴圈取代 services health-check，可能不穩 | 需在 CI 實測完整模式至少一次；SQL Server 的等待迴圈早已是自己寫的，同一套做法 |
| 忘記帶 `[all-db]` 就 push 了 dialect 改動 | 可接受：補一個空 commit 或手動 dispatch 即可重跑 |

**明確不採用**：依變更路徑自動判定（如「動到 `src/Bee.Db/**` 才全跑」）。
判定規則會與實際相依關係漂移（改 `Bee.Repository` 一樣可能踩 dialect），
而誤判方向是「該跑卻沒跑」—— 靜默失效，比忘記加標記更糟。

## 執行步驟

1. `build-ci.yml` 加 `workflow_dispatch` 的 `db_scope` input。
2. 加 `Resolve database scope` step（置於 checkout 之後、其餘 step 之前）。
3. `services:` 移除 postgresql / mysql / oracle 三個區塊，只留 sqlserver。
4. 新增 `Start extra database containers` + `Wait for extra database containers` 兩個條件 step，
   並在其中寫入三條連線字串到 `$GITHUB_ENV`。
5. 拆分 `Create test databases`：SQL Server 無條件、PG + MySQL 條件化。
6. Sonar 相關 step（含 Java / cache / scanner 安裝）掛 `if`。
7. `Test with coverage` 的 `env:` 縮為兩條，`--collect` 參數條件化。
8. `Mobile AOT gate` 的 `env:` 同步縮為兩條。
9. 順手移除 workflow 內指向 `docs/plans/plan-oracle-main-ci-evaluation.md` 的註解引用
   —— 該檔已於封存清理時刪除，指標已失效。
10. `.claude/rules/testing.md` 新增「CI 的資料庫範圍」一節（內容見下方「規則落點」）。

## 驗證

1. **精簡模式**：一般 push（commit message 不帶標記）。確認
   （a）Initialize containers 只起一個容器；（b）測試摘要中 PG / MySQL / Oracle 的
   `[DbFact]` 全數 skipped 且**無 failed**；（c）SQL Server + SQLite 測試照常通過；
   （d）Sonar 三步 skipped；（e）總時長 < 4 分鐘。
2. **完整模式**：手動 `workflow_dispatch`（`db_scope=all`）。確認
   （a）四個容器都起來且等待迴圈通過；（b）測試通過數與現行 baseline 一致（無新增 skip）；
   （c）SonarCloud 收到 coverage 且數值與前次全跑相當。
3. 兩種模式各成功一次後才視為完成。

## 規則落點（決定於 2026-08-21）

workflow 支援 `[all-db]` 標記還不夠 —— **沒有常駐規則說明它存在，agent 在後續 session
不會知道要用它，機制形同不存在**。因此在 `../../.claude/rules/testing.md`（已常駐，
定位正是「動筆前必須知道的決定」）新增一節，擬定內容：

```markdown
## CI 的資料庫範圍：push 前先問使用者

`build-ci.yml` 預設只跑 **SQL Server + SQLite**（精簡模式，約 3.5 分鐘）。
四種資料庫全跑需明確指定：commit message 帶 `[all-db]`，或手動
`workflow_dispatch` 選 `db_scope=all`。

**準備 commit / push 到 `main` 前，一律先問使用者本次要不要跑完整模式。**
不要自行決定 —— 該不該全跑取決於使用者對這次改動的風險判斷，不是從 diff 看得出來的。

需要主動建議「應該全跑」的訊號：改動觸及 `src/Bee.Db/Providers/**`、
`src/Bee.Repository/**`、`SchemaSyntax` / `DbTypeMapper` / `NormalizeDbType`，
或任何 SQL 產生邏輯。發版前必須跑一次完整模式。

漏跑的補救成本很低（手動 dispatch 重跑一次即可），所以這條是條文而非 hook 強制 ——
不值得讓每次 push 都多一輪來回。
```

> 強度採**條文**而非 hook：`PreToolUse` hook 只能 allow/deny、無法互動提問，
> 要強制就得每次 push 都擋一輪，而漏跑的補救成本遠低於那個摩擦。

## 文件同步

- `../../.claude/rules/testing.md` → 新增上節的「CI 的資料庫範圍」。
- `../../tests/CLAUDE.md` 第 15~16 行「CI 走 service container、env vars 由 workflow 注入」
  → 需改為反映兩種模式（SQL Server 走 service container，其餘依模式由 step 啟動）。
- `../repo-ops/gotchas/test-ci-release.md` → 檢查是否有「CI 一定會跑四種 DB」的敘述需更新。
- `../repo-ops/ci-sonarcloud-setup.md` → 該文件是通用接入指引，預期不需改；仍需確認一次。
- 「發版前必須跑一次完整模式」→ 已納入上節條文（發版 skill 為跨 repo 共用，不寫進去）。
