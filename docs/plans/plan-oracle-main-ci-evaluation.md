# 計畫：Oracle 進主 CI 可行性再評估

**狀態：📝 擬定中**

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
若主 CI 接受 +90 秒成本（從 ~4 分鐘變 ~5:30），就能恢復 Oracle provider 的 SonarCloud 覆蓋率納入 quality gate，不再需要 `sonar.coverage.exclusions` 的 Oracle 條目。

但單次成功不能證明穩定性。本計畫先收集 5-10 次手動觸發樣本，評估 flakiness，再做進主 CI 的決策。

## 目標

1. 累積 `oracle-validation.yml` **至少 5 次成功觸發**的實測樣本（含時間分佈、首次 vs. 重跑差異）
2. 統計 flakiness：失敗次數、失敗原因、可否自動 retry 解決
3. 若樣本顯示穩定（成功率 ≥ 90%、平均耗時 ≤ 3 分鐘），則：
   a. 將 Oracle service container 加入 `build-ci.yml`
   b. 從 `SonarQube.Analysis.xml` 的 `sonar.coverage.exclusions` 移除 Oracle
   c. 移除 `oracle-validation.yml`（功能已併入主 CI）
4. 若樣本顯示不穩定，**保持現狀**（手動 workflow + Oracle 排除 coverage），並把樣本資料寫進本 plan 留紀錄

## 不涵蓋

- **Oracle XE vs Oracle Free 的選擇變更**：當前 `gvenzl/oracle-free:23-slim-faststart` 對齊本機 `.runsettings` 的 FREEPDB1，本計畫不重新評估
- **Oracle 21c 等其他版本支援**：另立計畫
- **Image cache 加速**：若 GH Actions runner 偶爾 cache hit、偶爾 miss 造成耗時差異大，本計畫仍以「實測平均」為決策依據；不單獨優化 image cache

## 步驟

### 1. 收集樣本（手動或排程）

兩種收法二選一：

**A. 手動觸發（推薦，反映真實使用）**
- 每次有 src/ 改動需要驗證 Oracle 行為時：`gh workflow run oracle-validation.yml`
- 每次跑完用 `gh run view <run-id> --json status,conclusion,jobs --jq ...` 記錄

**B. 暫時加 schedule（快速收樣）**
- 在 `oracle-validation.yml` 暫加 `schedule: cron: '0 0 * * *'`（每日 UTC 0 點）
- 收滿樣本後移除排程

預期樣本數：至少 **5 次成功**（含可能的 1-2 次失敗）

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

**進主 CI 條件**（5 次樣本中）：
- 成功率 ≥ 80%（即 ≤1 次失敗，且失敗為已知 GH Actions / Docker Hub 問題、非 Oracle 本身）
- 平均耗時（成功樣本）≤ 3 分鐘
- 尖峰耗時 ≤ 4 分鐘

達標 → 執行步驟 4（合併進主 CI）；未達標 → 維持現狀並關閉本 plan。

### 4. 合併進主 CI（若決策為「進」）

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
- [ ] 累積 ≥ 5 次 `oracle-validation.yml` 樣本資料
- [ ] 樣本表格回填本 plan「結論」段落

**若決議「進主 CI」：**
- [ ] `build-ci.yml` 加入 Oracle service container 並通過 5 次連續成功
- [ ] SonarCloud quality gate 通過，且 `Providers/Oracle/` Coverage on New Code ≥ 85%
- [ ] 主 CI 平均耗時 ≤ 6 分鐘（從 ~4 分鐘變 +90s 內可接受）
- [ ] `oracle-validation.yml` 已刪除

**若決議「維持現狀」：**
- [ ] 樣本資料留存於本 plan「結論」，作為未來再評估的參考
- [ ] 本 plan 標記為已完成（決議為負，但仍是完成的決策過程）

## 風險

| 風險 | 機率 | 緩解 |
|------|------|------|
| Oracle XE/Free image 偶爾因 Docker Hub 速率限制拉不到 | 中 | health-retries 已設 30；可考慮加 `actions/cache` cache layer |
| `gvenzl/oracle-free:23-slim-faststart` 標籤未來 image 異動造成 break | 低 | 必要時 pin 到具體 digest（`@sha256:...`） |
| 主 CI 加入後拖慢所有 PR | 中 | 樣本階段已估算 +90s；超過時 revert（改回手動 workflow） |
| 樣本期間都成功但合併後 flaky | 低 | 預備 revert SOP：把 build-ci.yml + SonarQube.Analysis.xml 改回上一版、重建 oracle-validation.yml |

## 參考

- 已完成的相關 plan：[plan-ci-mysql-and-oracle-manual.md](plan-ci-mysql-and-oracle-manual.md)
- 既有 Oracle workflow：[`.github/workflows/oracle-validation.yml`](../../.github/workflows/oracle-validation.yml)
- 首次成功 run（基準樣本）：https://github.com/jeff377/bee-library/actions/runs/25088730228
