# 計畫：SQLite Provider 覆蓋率偏低調查

**狀態：📝 擬定中**

## 背景

SonarCloud 在 commit `a43600b` 之後的掃描中，`src/Bee.Db/Providers/Sqlite/` 的 New Coverage 為 **69.38%**（75 uncovered lines），明顯低於同期：

| Provider | New Coverage | Uncovered Lines | CI 環境 |
|----------|--------------|-----------------|---------|
| **Sqlite** | **69.38%** | **75** | ✅ in-memory（無 service container 需求） |
| MySql | 89%+（commit a43600b 後上升） | <50 | ✅ service container |
| SqlServer | 90.02% | 21 | ✅ service container |
| PostgreSql | 94.26% | 16 | ✅ service container |

預期上 SQLite 是 in-memory 配置（`Data Source=file:bee_test_sqlite?mode=memory&cache=shared`），CI 完全能跑整合測試，**覆蓋率不應低於有 service container 的同類 provider**。

## 目標

1. 找出 `src/Bee.Db/Providers/Sqlite/` 75 個 uncovered lines 集中在哪些方法／分支
2. 區分「真實未測」vs.「測試框架抓不到」兩種情境
3. 補測試或調整測試組織，將 SQLite 覆蓋率提升至 ≥85%（與其他有 service container 的 provider 量級接近）

## 不涵蓋

- **SonarCloud 整體 quality gate 調整**：當前 84.6% 已通過 80% 門檻，本計畫不影響閾值
- **其他 Provider（SqlServer / PostgreSql / MySql）的覆蓋率優化**：各別方法名／分支若有未覆蓋，獨立評估
- **新增 SQLite 功能**：本計畫只補測試，不改 provider 行為
- **重構 SQLite provider 結構**：除非測試補不上某條 dead branch，才考慮刪除無用程式碼

## 調查步驟

### 1. 取得 SQLite 未覆蓋的具體方法／行號

兩個來源任選：

**A. SonarCloud UI**
- https://sonarcloud.io/component_measures?metric=new_coverage&selected=jeff377_bee-library%3Asrc%2FBee.Db%2FProviders%2FSqlite&id=jeff377_bee-library
- 逐檔點開看哪些行未覆蓋

**B. 本機跑 coverage report**
```bash
./test.sh tests/Bee.Db.UnitTests/Bee.Db.UnitTests.csproj --collect:"XPlat Code Coverage;Format=opencover" --filter "FullyQualifiedName~Sqlite"
# 產出在 tests/Bee.Db.UnitTests/TestResults/<guid>/coverage.opencover.xml
# 用 reportgenerator 或解析 opencover XML 找 line-rate < 1.0 的方法
```

### 2. 對未覆蓋分支分類

依下列三類，每筆記錄歸屬：

| 類別 | 處理 |
|------|------|
| **真實邏輯但測試漏掉** | 補測試（純語法測試或 `[DbFact(DatabaseType.SQLite)]` 整合測試） |
| **錯誤路徑（throw / null guard）** | 加負面測試或用 `Record.Exception` 驗證 |
| **死碼／永遠不到的分支** | 移除或加 `// reachable only when X` 並標記為 false-positive on SonarCloud UI |

### 3. 補測試 / 調整程式碼

依分類執行，逐個 commit（依專案慣例 `feat`/`test`/`chore`）。

### 4. 推到 main 後驗證

- Build CI 通過
- SonarCloud 上 `Providers/Sqlite/` Coverage on New Code ≥ 85%
- New Coverage 整體不應降低（其他不變的情況下應上升 1-2%）

## 驗收標準

- [ ] `Providers/Sqlite/` Coverage on New Code ≥ 85%（從 69.38% 上升）
- [ ] 未覆蓋行數 ≤ 30（從 75 下降）
- [ ] 補上的測試在 `./test.sh` 全綠，CI 全綠
- [ ] 若有死碼移除：`Bee.Db` 對外行為不變（純語法測試與整合測試結果不變）

## 風險

| 風險 | 緩解 |
|------|------|
| 部分未覆蓋來自 `OnConfigure` 等 SQLite 特殊 lifecycle 方法，難以單測 | 接受 — 該情境可標 false-positive 並說明，但需有書面理由 |
| SQLite in-memory `cache=shared` 的測試之間會互相污染 | 沿用既有 `TempDefinePath` 模式或在每個測試開新連線；參考 `tests/Bee.Db.UnitTests/SqliteIntegrationTests.cs` |
| 補了測試後反而暴露 provider 中的真實 bug | 視 bug 嚴重性決定先修還是另立 plan |

## 參考

- 對照組：`src/Bee.Db/Providers/PostgreSql/`（94.26% coverage）、`src/Bee.Db/Providers/SqlServer/`（90.02%）的測試覆蓋模式
- SonarCloud UI 入口：https://sonarcloud.io/component_measures?metric=new_coverage&id=jeff377_bee-library
- 相關慣例：[`.claude/rules/testing.md`](../../.claude/rules/testing.md)（DbFact / 純邏輯 / shared fixture 規範）
