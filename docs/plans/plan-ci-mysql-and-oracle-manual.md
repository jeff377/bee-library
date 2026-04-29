# 計畫：主 CI 加入 MySQL，Oracle 走手動 workflow

**狀態：📝 擬定中**

> 本計畫銜接 commit `8c4e010`（將 Oracle/MySQL 排除在 SonarCloud coverage 計算外的短期 fix），目的是恢復「真實覆蓋率」訊號。
> MySQL 部分屬 [plan-mysql-support.md](plan-mysql-support.md) 設計策略的調整：原本「CI 不變動」的設計改為「CI 加 service container」。

## 背景

目前 SonarCloud `sonar.coverage.exclusions` 排除了 `src/Bee.Db/Providers/Oracle/**` 與 `src/Bee.Db/Providers/MySql/**`（commit `8c4e010`）。
這是當時的短期妥協，理由是 CI 沒有對應 service container，整合測試自動 skip 會把 New Coverage 拉到 79.13% 卡住 quality gate。

但長期來看：
- **MySQL** 的 service container 啟動成本極低（並行於 SQL Server，~20-30s 額外時間），值得直接進主 CI。
- **Oracle** 的 service container 啟動成本中等（image 1.5GB、啟動 45-90s，總計 +60-90s wall clock）且 `gvenzl/oracle-xe` 偶有 flaky 風險，每次 PR 承擔不划算；改為「手動／里程碑時觸發」較合理。

## 目標

1. 主 `build-ci.yml` 增加 MySQL service container，Sonar 覆蓋率含 MySQL provider 真實測試結果。
2. 新增 `oracle-validation.yml`（純手動觸發）跑 Oracle 整合測試，**不送 SonarCloud**，純粹驗證 Oracle provider 行為。
3. 主 CI 預期增加 ~20-30s（MySQL 並行於 SQL Server，幾乎不拖總時長）。
4. Oracle workflow 約 5-7 分鐘（含 image pull + container init + tests），但只在需要時手動觸發。

## 不涵蓋

- **Oracle 進主 CI**：成本與 flaky 風險不划算，留待未來 Oracle XE on GH Actions 穩定性驗證後再評估。
- **SQLite 覆蓋率偏低（69%）**：另一個獨立議題，不在本計畫範圍。
- **MySQL provider 實作完成度**：本計畫只負責 CI 整合；provider 本身的實作完成度由 [plan-mysql-support.md](plan-mysql-support.md) 負責。
- **Oracle workflow 送 SonarCloud**：刻意不送，避免兩個 workflow 互相覆蓋同一 project key 的覆蓋率快照。

## 變更項目

### 1. 主 `build-ci.yml`：加入 MySQL service container

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    # ... 既有設定
  postgresql:
    image: postgres:17
    # ... 既有設定
  mysql:
    image: mysql:8.0
    env:
      MYSQL_ROOT_PASSWORD: BeeTest_Password123!
      MYSQL_DATABASE: common
    ports:
      - 3306:3306
    options: >-
      --health-cmd "mysqladmin ping -h localhost -uroot -pBeeTest_Password123!"
      --health-interval 10s
      --health-timeout 5s
      --health-retries 10
```

並在 `Test with coverage` step 注入連線字串：

```yaml
env:
  BEE_TEST_CONNSTR_SQLSERVER: ...
  BEE_TEST_CONNSTR_POSTGRESQL: ...
  BEE_TEST_CONNSTR_SQLITE: ...
  BEE_TEST_CONNSTR_MYSQL: "Server=localhost;Port=3306;Database=common;User=root;Password=BeeTest_Password123!;"
```

> 連線字串格式採 `MySqlConnector` 套件（[plan-mysql-support.md](plan-mysql-support.md) 第 29 行明確指定）。

### 2. `SonarQube.Analysis.xml`：移除 MySQL 排除

```xml
<!-- 改為僅排除 Oracle，因主 CI 已能跑 MySQL 整合測試 -->
<Property Name="sonar.coverage.exclusions">tests/**,samples/**,src/Bee.Db/Providers/Oracle/**</Property>
```

註解同步更新，說明 Oracle 仍排除的理由與條件解除（手動 workflow 完成後可再評估）。

### 3. 新增 `.github/workflows/oracle-validation.yml`

```yaml
name: Oracle Validation (manual)

on:
  workflow_dispatch:

jobs:
  validate:
    runs-on: ubuntu-latest
    services:
      oracle:
        image: gvenzl/oracle-xe:21-slim-faststart
        env:
          ORACLE_PASSWORD: BeeTest_Password123!
          APP_USER: testuser
          APP_USER_PASSWORD: BeeTest_Password123!
        ports:
          - 1521:1521
        options: >-
          --health-cmd "healthcheck.sh"
          --health-interval 20s
          --health-timeout 10s
          --health-retries 30
    steps:
      - uses: actions/checkout@<sha>
      - uses: actions/setup-dotnet@<sha>
      - name: Test (Oracle integration only)
        env:
          BEE_TEST_CONNSTR_ORACLE: "Data Source=localhost:1521/XEPDB1;User Id=testuser;Password=BeeTest_Password123!;"
        run: dotnet test Bee.Library.slnx --configuration Release --filter "FullyQualifiedName~Oracle" --verbosity normal
```

要點：
- **trigger 只有 `workflow_dispatch`**：不自動觸發，避免每次 push 都拉 1.5GB image。
- **不裝 SonarScanner、不送 Sonar**：純驗證行為。
- **`--filter "FullyQualifiedName~Oracle"`**：只跑檔名／namespace 含 Oracle 的測試，避免重複跑其他 DB 的測試。
- **健康檢查重試 30 次**：應付 Oracle XE 偶爾啟動慢的情況。
- 第三方 actions 一律 pin commit SHA（依 `.claude/rules/sonarcloud.md` S7637）。

### 4. （非必要）為 `oracle-validation.yml` 加 `schedule`

延後決定。先靠手動觸發，視使用頻率與 Oracle XE flaky 程度再決定要不要 weekly schedule。

## 驗收標準

1. 主 `build-ci.yml` push 後跑通：MySQL 整合測試 `[DbFact(DatabaseType.MySQL)]` 全綠（不再 skip）。
2. SonarCloud quality gate 通過，且 New Coverage 包含 MySQL provider 的真實覆蓋（Providers/MySql 應從 48% 顯著提升）。
3. 主 CI 總時間增加不超過 60 秒（樂觀估算 +20-30s，保守 +60s）。
4. `oracle-validation.yml` 透過 GitHub UI「Run workflow」可成功觸發，Oracle 整合測試 `[DbFact(DatabaseType.Oracle)]` 跑通。
5. `oracle-validation.yml` **不**出現在 SonarCloud 上（不送 scan）。

## 風險與緩解

| 風險 | 機率 | 影響 | 緩解 |
|------|------|------|------|
| MySQL container 啟動失敗（image pull / health check） | 低 | 主 CI 紅燈 | health-retries 10 + GH Actions 預設 service 重試機制；fallback 機制：移除 MySQL service，立刻 revert |
| MySQL 整合測試本身有 bug（plan-mysql-support 未完成） | 中 | 主 CI 紅燈 | 先確認 [plan-mysql-support.md](plan-mysql-support.md) 各 Phase 進度；若 provider 未完成，本計畫延後 |
| Oracle workflow `gvenzl/oracle-xe` flaky | 中 | 手動觸發失敗，需 retry | health-retries 30 + 文件記錄 retry 慣例 |
| Oracle XE image 太大（1.5GB pull） | 低 | 手動 workflow 慢 | 接受；手動觸發本來就少跑 |
| MySQL 連線字串密碼明文寫在 workflow | 低 | 公開 repo 可見 | 同 SQL Server / PostgreSQL 既有做法（測試用密碼，無敏感性） |

## 執行順序

1. 確認 `plan-mysql-support.md` 各 Phase 已完成到 CI 整合測試能跑通的程度
   - 若未完成，本計畫延後到 MySQL provider 完成 Phase B（純語法）+ Phase C（schema）
2. 改 `build-ci.yml` 加 MySQL service container + env var
3. 改 `SonarQube.Analysis.xml` 移除 MySQL 排除
4. push 後等 CI 跑通、SonarCloud 確認 New Coverage 通過
5. 新增 `oracle-validation.yml`
6. 透過 `gh workflow run oracle-validation.yml` 試跑一次驗證
7. 將本計畫標記完成，封存

## 後續可能延伸

- **SQLite 覆蓋率調查**（69% 偏低）— 獨立 plan
- **Oracle 進主 CI 可行性再評估** — 視 `oracle-validation.yml` 使用一段時間後的穩定性而定
- **`oracle-validation.yml` 加 `schedule` 跑 weekly** — 視需求
