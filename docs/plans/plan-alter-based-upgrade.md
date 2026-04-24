# 計畫：將資料表升級策略改為 ALTER-based 增量升級

**狀態：✅ 已完成（2026-04-25）**

## 背景

目前 [SqlCreateTableCommandBuilder.GetUpgradeCommandText()](../../src/Bee.Db/Providers/SqlServer/SqlCreateTableCommandBuilder.cs#L78-L104) 採 **rebuild** 策略：任何 schema 變更都會執行「建 tmp 表 → `INSERT INTO tmp SELECT FROM original` → drop 舊表 → rename」。

此策略在大資料表（千萬筆以上）有顯著問題：
- 全表搬資料耗時（數十分鐘～小時）
- Transaction log 暴漲、tempdb 壓力
- 期間表級鎖定，業務受影響

多數 schema 變更（新增欄位、加索引、放大長度）實際上可用 `ALTER TABLE` 在秒級完成，不需搬資料。

## 成功契約

> **Execute 成功返回後，DB 必須至少包含 define 裡所有欄位，且型別／長度／nullable 與 define 一致。**

這是從使用者視角唯一重要的事。內部走 ALTER 或 rebuild 是 library 自由選擇，只要滿足此契約即可。

## 設計決策（已定案）

### D1. Comparer API 演進
- `Compare()` 保留現有行為（維持 `UpgradeAction` 欄位，現有呼叫方不動）
- 新增 `CompareToDiff()` 產出結構化 `TableSchemaDiff`
- 升級期間並存，後續 PR 才移除舊 API

### D2. 欄位改名機制
- `DbField.OriginalFieldName`（序列化屬性，非 `[XmlIgnore]`）
- 目標情境：**新模組開發期**迭代；**非**生產環境跨版本遷移
- 僅支援單次 rename（多次須逐版部署，跨版本跳躍不保證）
- 冪等性：DB 已有新名 → no-op；DB 無新名也無舊名 → 警告 no-op

### D3. Destructive 變化
- **不支援 drop column**（延續 extension field 保留政策，第三方整合可能依賴）
- `AllowColumnNarrowing`（預設 `false`）：縮小長度時要明確開啟；失敗不 fallback rebuild
- Real-only 欄位與索引一律保留，ALTER 路徑不碰

### D4. Dry-run 介面（三層）
```
Comparer.CompareToDiff()   → TableSchemaDiff    (純結構化，provider-agnostic)
Orchestrator.Plan(diff)    → UpgradePlan        (含 SQL statements + 警告)
Orchestrator.Execute(plan) → 真正執行
```

### D5. Transaction 策略
- Per-stage transaction，不整體包
- Stage 順序（orchestrator 強制）：
  ```
  Stage 1: drop-indexes       (刪除即將被改定義的索引)
  Stage 2: alter-columns      (改既有欄位型別、長度、nullable)
  Stage 3: add-columns        (新增欄位)
  Stage 4: create-indexes     (建新索引、重建先前刪除的)
  Stage 5: sync-descriptions  (extended property 同步)
  ```
- 失敗即中止，已完成 stage 不回滾；Comparer 冪等可重跑

### D6. Rebuild 定位
- **不暴露 Strategy option**，orchestrator 自動判斷
- 全部變化可 ALTER → 走 ALTER
- 任一變化需 rebuild → 整表 fallback rebuild
- 任一變化 not supported → 報錯中止
- `UpgradeOptions` 僅保留 `AllowColumnNarrowing`

### D7. Provider 抽象
- Change set 型別放 `Bee.Db.Schema.Changes`（provider-agnostic）
- `ITableAlterCommandBuilder` 介面（SqlServer 單一實作）
- 不做 per-change strategy pattern；不抽 `ITableRebuildCommandBuilder`

### D8. FK / Trigger / View
- **刻意不支援**（非暫緩）
- 架構原則：referential integrity / business rules 由 BO 處理，不依賴 DB 層
- `TableChange` 抽象基底保留一般性 extensibility，不為 FK 預留

### D9. 版本升級路徑
- Next minor：`4.1.0`
- 無 API 破壞；預設行為向後相容
- 唯一行為改變：欄位長度縮小從「靜默 truncate」→「預設失敗」（安全性改善）
- Real-only 索引不再被 rebuild 吃掉（附帶改善）

## 架構流程

```
┌──────────────────────────────────────────────────────────┐
│ 1. TableSchemaComparer (provider-agnostic)               │
│    比對 define vs real DB                                │
│    產出 TableSchemaDiff { List<TableChange> Changes }    │
└──────────────────────────────────────────────────────────┘
                         ↓
┌──────────────────────────────────────────────────────────┐
│ 2. TableUpgradeOrchestrator (provider-aware)             │
│    對每個 TableChange 詢問 ITableAlterCommandBuilder：   │
│      → Alter / Rebuild / NotSupported                    │
│    聚合結果，決定整表走 ALTER 或 rebuild                 │
│    產出 UpgradePlan { ExecutionMode, Statements }        │
└──────────────────────────────────────────────────────────┘
                         ↓
┌──────────────────────────────────────────────────────────┐
│ 3. Execute                                               │
│    依 ExecutionMode 執行對應 statements                  │
│    Per-stage transaction                                 │
└──────────────────────────────────────────────────────────┘
```

## 會觸發 Rebuild 的情境（SQL Server）

僅兩類：

### 1. 欄位型別跨 family 變更
| 從 | 到 |
|----|----|
| String / Text | Integer / Long / Short / Decimal / Currency / Date / DateTime |
| 數值型 | Date / DateTime |
| Boolean | 任何其他型別 |
| Binary | 任何非 Binary |
| Guid ↔ String | 雙向 |

### 2. AutoIncrement 狀態變更
- 一般欄位 ↔ IDENTITY 欄位
- SQL Server 的 `ALTER COLUMN` 無法改 IDENTITY 屬性

**其他變化都走 ALTER**：新增/刪除欄位（僅新增；刪除不支援）、同 family 型別變化（如 `String(50) → String(100)`、`Integer → Long`）、改 nullable、改 default、新增/刪除索引、縮小長度（需 `AllowColumnNarrowing=true`）。

## 檔案清單

### 新增

```
src/Bee.Db/Schema/
  ├── TableSchemaDiff.cs                      # 結構化 diff 結果
  ├── UpgradePlan.cs                          # 執行計畫（含 SQL）
  ├── UpgradeOptions.cs                       # AllowColumnNarrowing
  ├── ChangeExecutionKind.cs                  # enum: Alter | Rebuild | NotSupported
  ├── TableUpgradeOrchestrator.cs             # 聚合、決策、執行
  └── Changes/
      ├── TableChange.cs                      # abstract base
      ├── AddFieldChange.cs
      ├── AlterFieldChange.cs                 # 型別/長度/nullable/default
      ├── RenameFieldChange.cs
      ├── AddIndexChange.cs
      └── DropIndexChange.cs

src/Bee.Db/Providers/
  ├── ITableAlterCommandBuilder.cs            # provider 接縫
  └── SqlServer/
      ├── SqlTableAlterCommandBuilder.cs      # SqlServer 實作
      ├── SqlTableRebuildCommandBuilder.cs    # 從 SqlCreateTableCommandBuilder 抽出的 rebuild 邏輯（internal）
      └── SqlAlterCompatibilityRules.cs       # 跨型別相容性判定表
```

### 修改

```
src/Bee.Definition/Database/DbField.cs         # 加 OriginalFieldName 屬性（序列化）
src/Bee.Db/Schema/TableSchemaComparer.cs       # 新增 CompareToDiff()；舊 Compare() 不動
src/Bee.Db/Providers/SqlServer/SqlCreateTableCommandBuilder.cs  # 抽出 rebuild 邏輯後，此類只負責 CREATE
src/Bee.Db/Schema/TableSchemaBuilder.cs        # GetCommandText() 改走 orchestrator
src/Bee.Repository/System/DatabaseRepository.cs  # UpgradeTableSchema 加 UpgradeOptions 可選參數
src/Directory.Build.props                       # Version → 4.1.0
```

### 測試

**新增**：
```
tests/Bee.Db.UnitTests/Schema/
  ├── TableSchemaDiffTests.cs
  ├── Changes/                                # 各 change 型別
  │   ├── AddFieldChangeTests.cs
  │   ├── AlterFieldChangeTests.cs
  │   ├── RenameFieldChangeTests.cs
  │   └── ...
  ├── TableUpgradeOrchestratorTests.cs
  └── UpgradeOptionsTests.cs

tests/Bee.Db.UnitTests/Providers/SqlServer/
  ├── SqlTableAlterCommandBuilderTests.cs
  ├── SqlAlterCompatibilityRulesTests.cs
  └── SqlTableRebuildCommandBuilderTests.cs   # 原 rebuild 測試遷移
```

**修改**：
```
tests/Bee.Db.UnitTests/TableSchemaComparerTests.cs  # 新增 CompareToDiff 測試
```

**DbFact 整合測試**（驗證真的跑起來）：
```
tests/Bee.Db.UnitTests/Integration/
  ├── AlterUpgradeIntegrationTests.cs         # ALTER 路徑端對端
  └── RebuildFallbackIntegrationTests.cs      # Rebuild fallback 端對端
```

### 文件

```
docs/development-constraints.md    # 加「不支援 FK/trigger/view」節
docs/architecture-overview.md      # 若提及 schema 升級流程，更新
docs/adr/                          # 考慮新增 ADR 記錄此次決策
docs/plans/plan-alter-based-upgrade.md  # 完成時更新狀態
```

## PR 切分

### PR 1：Change set 型別 + Comparer 新 API（不改行為）

**範圍**：建立所有 provider-agnostic 的型別骨架，Comparer 新增第二條 API。

**檔案**：
- 新增 `src/Bee.Db/Schema/Changes/*.cs`（含 `TableChange` 基底與所有子類）
- 新增 `TableSchemaDiff.cs`、`UpgradeOptions.cs`、`ChangeExecutionKind.cs`
- 修改 `TableSchemaComparer.cs`：新增 `CompareToDiff()` 方法
- 新增 `TableSchemaDiffTests.cs`、各 Change 型別測試
- 修改 `TableSchemaComparerTests.cs`：新增 `CompareToDiff` 案例

**驗收條件**：
- 現有測試全過（舊 `Compare()` 行為不動）
- 新 API 可產出正確的 `TableSchemaDiff`
- 各 `TableChange` 子類的單元測試覆蓋建構與屬性
- Build 無警告

### PR 2：SqlTableAlterCommandBuilder 基本變化

**範圍**：SqlServer provider 實作，只支援不涉及 rename、不涉及 rebuild 的基本變化。

**檔案**：
- 新增 `src/Bee.Db/Providers/ITableAlterCommandBuilder.cs`
- 新增 `src/Bee.Db/Providers/SqlServer/SqlTableAlterCommandBuilder.cs`
- 新增 `src/Bee.Db/Providers/SqlServer/SqlAlterCompatibilityRules.cs`
- 新增對應測試

**支援範圍**：
- `AddFieldChange` → `ALTER TABLE ADD`
- `AlterFieldChange`（同 family）→ `ALTER COLUMN`
- `AddIndexChange` → `CREATE INDEX`
- `DropIndexChange` → `DROP INDEX`
- 相容性判定：跨 family 變更回傳 `Rebuild`；AutoIncrement 狀態變更回傳 `Rebuild`

**不在範圍**（後續 PR）：rename、orchestrator、rebuild fallback。

**驗收條件**：
- 每個支援的 change 型別有對應 SQL 產生測試
- 相容性判定表完整（跨所有 `FieldDbType` 組合）
- 不修改任何現有呼叫方

### PR 3：DbField.OriginalFieldName + rename 偵測

**範圍**：加上 rename 機制。

**檔案**：
- 修改 `src/Bee.Definition/Database/DbField.cs`：加 `OriginalFieldName` 屬性
- 修改 `src/Bee.Db/Schema/TableSchemaComparer.cs`：`CompareToDiff` 偵測 rename 意圖
- 新增 `src/Bee.Db/Schema/Changes/RenameFieldChange.cs`（若 PR 1 未含）
- 修改 `SqlTableAlterCommandBuilder`：支援 `RenameFieldChange` → `sp_rename`
- 新增對應測試（含冪等性）

**驗收條件**：
- 正常 rename 流程：`OriginalFieldName` 設定後產出 `RenameFieldChange`，SQL 為 `sp_rename`
- 冪等：DB 已有新名時 → 不產生 change
- 過期提示：DB 無舊名也無新名 → 警告並降級為 `AddFieldChange`（新加欄位）
- 現有 XML 定義檔反序列化相容（`OriginalFieldName` 缺失 → 空字串）

### PR 4：TableUpgradeOrchestrator + rebuild fallback

**範圍**：聚合執行流程，含 rebuild 路徑抽取。

**檔案**：
- 新增 `src/Bee.Db/Schema/UpgradePlan.cs`
- 新增 `src/Bee.Db/Schema/TableUpgradeOrchestrator.cs`
- 新增 `src/Bee.Db/Providers/SqlServer/SqlTableRebuildCommandBuilder.cs`（從 `SqlCreateTableCommandBuilder` 抽出 upgrade 邏輯）
- 修改 `src/Bee.Db/Providers/SqlServer/SqlCreateTableCommandBuilder.cs`：只保留 CREATE，移除 upgrade
- 新增 `TableUpgradeOrchestratorTests.cs`
- 新增 DbFact 整合測試（實際對 SQL Server 跑）

**驗收條件**：
- Stage 順序正確（drop-idx → alter-col → add-col → create-idx → sync-desc）
- 聚合邏輯正確：全 Alter → ALTER 路徑；任一 Rebuild → rebuild；任一 NotSupported → 報錯
- Per-stage transaction 實際驗證（故意讓某 stage 失敗，確認前面 stage 已 commit）
- Rebuild fallback 路徑跑得通且產出與舊版等效
- DbFact 端對端測試通過

### PR 5：切換預設策略 + 文件

**範圍**：讓現有使用者自動享受新策略。

**檔案**：
- 修改 `src/Bee.Repository/System/DatabaseRepository.cs`：`UpgradeTableSchema` 加選填 `UpgradeOptions` 參數，內部走 orchestrator
- 修改 `src/Bee.Db/Schema/TableSchemaBuilder.cs`：`GetCommandText` 改呼叫 orchestrator
- 修改 `src/Directory.Build.props`：Version → `4.1.0`
- 更新 `docs/development-constraints.md`
- 撰寫 `docs/plans/plan-alter-based-upgrade.md` 完成標記
- 視需要新增 ADR 文件

**驗收條件**：
- 現有整合測試（`UpgradeTableSchema`）通過
- 手動驗證大資料表升級速度（用 DbFact 新增 1M+ 筆測試表，對比前後）
- Release notes 草稿完成
- `build-ci.yml` 全綠

## Release Notes 草稿（4.1.0）

```markdown
### 新功能
- 資料表升級改為增量式（ALTER-based）策略，大資料表升級速度顯著提升，不再需要全表搬資料。
- 新增 `UpgradeOptions`，支援 `AllowColumnNarrowing` 控制是否允許縮小欄位長度。
- 新增 `DbField.OriginalFieldName`，支援新模組開發期欄位改名。

### 行為改變
- **欄位長度縮小**：舊版會靜默截斷資料，新版預設拒絕。如確認可接受，設定 `UpgradeOptions { AllowColumnNarrowing = true }`。
- **Real-only 索引**：升級時不再被刪除（先前 rebuild 策略會一併丟棄）。

### 相容性
- 既有呼叫方無需改程式碼，預設行為自動採用新策略。
- 遇到不相容的變更（型別跨 family、AutoIncrement 狀態切換）自動 fallback 到舊的 rebuild 策略。
```

## 不涵蓋

- FK / trigger / view 相依性（刻意不支援，見 D8）
- 多 schema 支援
- Online schema change（如 SQL Server online index rebuild）
- 升級失敗的自動回復（per-stage 失敗後由使用者手動重跑；Comparer 冪等保證）
- 跨 DB provider（PostgreSQL / MySQL）— 架構保留擴充點，實作不在範圍內

## 參考

- 現行 rebuild 邏輯：[SqlCreateTableCommandBuilder.GetUpgradeCommandText()](../../src/Bee.Db/Providers/SqlServer/SqlCreateTableCommandBuilder.cs#L78-L104)
- 現行 comparer：[TableSchemaComparer](../../src/Bee.Db/Schema/TableSchemaComparer.cs)
- 現行升級入口：[DatabaseRepository.UpgradeTableSchema()](../../src/Bee.Repository/System/DatabaseRepository.cs#L44-L54)
- 相關計畫：[plan-tableschema-description.md](plan-tableschema-description.md)
