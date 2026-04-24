# 計畫：DisplayName／Caption 寫入 SQL Server 資料表註解（含獨立同步路徑）

**狀態：✅ 已完成（2026-04-24）** — Phase 1 + 2 實作與單元測試通過；`[DbFact]` 整合測試待本機啟動 SQL Server container 後跑過一次驗證

## 背景

`TableSchema.DisplayName` 與 `DbField.Caption` 目前存在於定義層與 XML fixture，但：

1. **建表時未寫入資料庫**：[SqlCreateTableCommandBuilder](../../src/Bee.Db/Providers/SqlServer/SqlCreateTableCommandBuilder.cs) 產 `CREATE TABLE` 時完全忽略
2. **反向讀取不對稱**：
   - 欄位層級：[ParseDbField():197](../../src/Bee.Db/Providers/SqlServer/SqlTableSchemaProvider.cs#L197) 已把 extended property 寫回 `Caption`
   - 表層級：[GetTableSchema():44-45](../../src/Bee.Db/Providers/SqlServer/SqlTableSchemaProvider.cs#L44-L45) 未讀取表的 extended property，`DisplayName` 恆為空
3. **註解異動無獨立同步路徑**：[TableSchemaComparer.CompareFields()](../../src/Bee.Db/Schema/TableSchemaComparer.cs#L62-L84) 不比對 `Caption`／`DisplayName`（刻意設計，避免大表因註解改動觸發 rebuild），但這也造成「只改註解永遠同步不到資料庫」的缺口

經討論決定：**不新增 Description 屬性**，直接以既有 `TableSchema.DisplayName` 與 `DbField.Caption` 作為資料庫註解來源。理由見先前討論（符合既有 provider 反向映射語意、fixture 內容風格吻合）。

## 範圍

本計畫分兩個 Phase 一次交付：

### Phase 1：Description 寫入 DDL（建表 / rebuild 路徑）

- `SqlCreateTableCommandBuilder.GetCreateTableCommandText()` 產出後追加 `sp_addextendedproperty` 區塊
  - 表層（若 `DisplayName` 非空）
  - 每個欄位（若 `Caption` 非空）
- CREATE 路徑：直接寫入正式表名
- UPGRADE（rebuild）路徑：寫入 tmp 表名，`sp_rename` 後 SQL Server 自動保留 extended property 至正式表
- `SqlTableSchemaProvider.GetTableSchema()` 讀取表的 extended property 寫入 `DisplayName`，讓雙向對稱

### Phase 2：Metadata-only 獨立同步路徑

- `TableSchemaComparer` 擴充偵測「註解差異」，**不影響** `UpgradeAction`：
  - 表層：比對 `DefineTable.DisplayName` vs `RealTable.DisplayName`
  - 欄位層：比對 `DefineTable.Fields[x].Caption` vs `RealTable.Fields[x].Caption`（前提是欄位同時存在於定義與實體）
  - 差異結果透過新增的 `DescriptionChanges` 屬性（或回傳結果）暴露給上層
- 新增 `SqlExtendedPropertyCommandBuilder`（檔案位置：`src/Bee.Db/Providers/SqlServer/`）：
  - 輸入：`DescriptionChanges` 清單
  - 輸出：一組 `sp_addextendedproperty` / `sp_updateextendedproperty` SQL
- 升級執行流程改為：
  - `UpgradeAction = New` → schema CREATE（已含 extended property，Phase 1 處理）
  - `UpgradeAction = Upgrade` → schema rebuild（已含 extended property，Phase 1 處理）
  - `UpgradeAction = None && 有註解差異` → **只**產 metadata DDL，**不**走 rebuild
  - `UpgradeAction = None && 無差異` → 不產任何 SQL

### 本計畫**不包含**

- 升級策略整體改 ALTER-based → 獨立計畫 [plan-alter-based-upgrade.md](plan-alter-based-upgrade.md)
- `TableSchema` / `DbField` 新增 `Description` 屬性（已決定不加）
- PostgreSQL / MySQL 的 `COMMENT ON` 實作（目前僅 SQL Server provider）
- 多 schema（`dbo` 以外）支援
- `TableSchema.Clone()` 漏複製 `UpgradeAction` 的修復 — 獨立小 issue
- fixture 誤用 `DisplayName=` 於 `DbField`（無效屬性）— 獨立小 issue

## 設計細節

### 1. SQL 產生格式（Phase 1）

**表層**（CREATE TABLE 後）：

```sql
EXEC sp_addextendedproperty
  @name=N'MS_Description', @value=N'用戶',
  @level0type=N'SCHEMA', @level0name=N'dbo',
  @level1type=N'TABLE', @level1name=N'{dbTableName}';
```

**欄位層**（每欄一條）：

```sql
EXEC sp_addextendedproperty
  @name=N'MS_Description', @value=N'用戶帳號',
  @level0type=N'SCHEMA', @level0name=N'dbo',
  @level1type=N'TABLE', @level1name=N'{dbTableName}',
  @level2type=N'COLUMN', @level2name=N'{fieldName}';
```

- 空值跳過
- 字串 escape 沿用既有 [`EscapeSqlString()`](../../src/Bee.Db/Providers/SqlServer/SqlCreateTableCommandBuilder.cs#L56-L59)
- `dbTableName`：CREATE 路徑傳正式表名、UPGRADE 路徑傳 `tmp_{tableName}`

### 2. Metadata-only 路徑（Phase 2）

**差異偵測**：`TableSchemaComparer.Compare()` 內增加一步：

```
foreach (欄位 in Define 與 Real 皆存在)
    若 Caption 不同 → 加入 DescriptionChanges

若 Define.DisplayName != Real.DisplayName → 加入 DescriptionChanges
```

不改變 `UpgradeAction` 的現有判定邏輯。

**DescriptionChanges 結構**（提案）：

```csharp
public class DescriptionChange
{
    public DescriptionLevel Level { get; set; }  // Table | Column
    public string FieldName { get; set; }        // Column 時使用，Table 時為空
    public string NewValue { get; set; }
    public bool IsNew { get; set; }              // true → Add, false → Update
}

public enum DescriptionLevel { Table, Column }
```

`TableSchemaComparer.Compare()` 回傳值或新增屬性 `List<DescriptionChange> DescriptionChanges`。

**`IsNew` 判定**：
- `Real` 中對應值為空字串 → `IsNew = true` → 產 `sp_addextendedproperty`
- `Real` 中對應值非空且與 `Define` 不同 → `IsNew = false` → 產 `sp_updateextendedproperty`
- `Real` 中對應值與 `Define` 相同 → 不加入 changes

**`Define` 為空、`Real` 有值** 的情境（使用者把註解清空）：
- **本計畫採保守策略：不移除 DB 的 extended property**
- 理由：定義檔留空常是「還沒填」而非「要刪除」，避免誤刪
- 若未來需要嚴格同步可再開 issue

### 3. 整合點

升級執行端（呼叫 `SqlCreateTableCommandBuilder` 的地方）需調整為兩階段：

```
1. 呼叫 SqlCreateTableCommandBuilder.GetCommandText(compareTable)
   → 得到 schema DDL（可能是 CREATE、rebuild、或空字串）
2. 呼叫 SqlExtendedPropertyCommandBuilder.GetCommandText(descriptionChanges)
   → 得到 metadata DDL（只在 UpgradeAction=None 且有 drift 時有內容）
3. 合併兩段 SQL 輸出
```

需要確認：目前呼叫 `SqlCreateTableCommandBuilder.GetCommandText` 的上層是誰？是否需要同步調整該層的簽章或流程。實作前先 grep。

## 實作步驟

### Phase 1

1. **`SqlCreateTableCommandBuilder.cs`**
   - 新增 `private string GetExtendedPropertyCommandText(string dbTableName)`：產生表與欄位的 `sp_addextendedproperty` 區塊
   - [`GetCreateTableCommandText()`](../../src/Bee.Db/Providers/SqlServer/SqlCreateTableCommandBuilder.cs#L166) 尾端附加上述區塊
   - 空值跳過、`EscapeSqlString` 處理

2. **`SqlTableSchemaProvider.cs`**
   - 新增 `private string GetTableDescription(string tableName)`：查 `fn_listextendedproperty` 取得表層 description
   - [`GetTableSchema()`](../../src/Bee.Db/Providers/SqlServer/SqlTableSchemaProvider.cs#L39-L63) 在 `dbTable.TableName = tableName;` 後加一行 `dbTable.DisplayName = GetTableDescription(tableName);`
   - SQL 用 `DbCommandSpec` 的 `{0}` 佔位符傳遞 tableName（符合 `scanning.md` SQL injection 防護）

3. **Phase 1 測試**
   - `SqlCreateTableCommandBuilderTests`（若不存在則新建）
     - `GetCommandText_WithDisplayNameAndCaption_IncludesExtendedProperty`
     - `GetCommandText_WithEmptyDisplayName_OmitsTableExtendedProperty`
     - `GetCommandText_WithEmptyCaption_OmitsColumnExtendedProperty`
     - `GetCommandText_WithSingleQuote_EscapesCorrectly`
     - `GetCommandText_UpgradePath_WritesExtendedPropertyToTmpTable`
   - `SqlTableSchemaProviderTests` 新增 `[DbFact]`：建表 → 寫 extended property → 讀回 → 驗證 `DisplayName`

### Phase 2

4. **`TableSchemaComparer.cs`**
   - 新增屬性 / 回傳值 `DescriptionChanges`（型別見上）
   - `Compare()` 流程末尾新增 `CompareDescriptions()` 私有方法
   - 既有 `UpgradeAction` 邏輯完全不動

5. **新增 `SqlExtendedPropertyCommandBuilder.cs`**
   - 位置：`src/Bee.Db/Providers/SqlServer/`
   - 公開方法 `string GetCommandText(IReadOnlyList<DescriptionChange> changes, string tableName)`
   - 根據 `IsNew` 產 `sp_addextendedproperty` 或 `sp_updateextendedproperty`

6. **整合**
   - 找出呼叫 `SqlCreateTableCommandBuilder.GetCommandText` 的上層（grep `GetCommandText` 在 `Bee.Db` / `Bee.Repository` 的使用）
   - 在該處加入 Phase 2 的 metadata DDL 合併邏輯
   - 如尚無統一入口，考慮新增一個 `TableUpgradeCommandBuilder`（orchestrator）串起兩個 builder

7. **Phase 2 測試**
   - `TableSchemaComparerTests` 補：
     - `Compare_OnlyDescriptionDiffers_UpgradeActionNone_DescriptionChangesPopulated`
     - `Compare_FieldTypeDiffersAndDescriptionDiffers_UpgradeActionUpgrade`
     - `Compare_EmptyDefineDescription_NoChangeGenerated`（確認保守策略）
   - `SqlExtendedPropertyCommandBuilderTests` 新建：覆蓋 add / update / 空清單 / escape
   - `[DbFact]` 整合測試：建表後只改 Caption → 執行 comparer → 產生 metadata SQL → 執行 → 讀回 → 驗證值更新

### 共通

8. **驗證**
   - `./test.sh tests/Bee.Db.UnitTests/Bee.Db.UnitTests.csproj`
   - `./test.sh tests/Bee.Definition.UnitTests/Bee.Definition.UnitTests.csproj`
   - `dotnet build --configuration Release`

## 風險與對應

| 風險 | 影響 | 對應 |
|------|------|------|
| `sp_rename` 能否保留 extended property | 中 → 已查證保留 | `[DbFact]` 整合測試驗證 |
| `sp_addextendedproperty` 重複執行會錯 | 低 | Phase 1 走 tmp 表，一定是新建；Phase 2 路徑用 `sp_updateextendedproperty` 處理既存 |
| 空值處理 | 低 | 明確跳過 |
| 特殊字元（單引號） | 低 | 沿用 `EscapeSqlString` |
| `TableSchemaComparer` 新增 API 的既有呼叫端相容 | 中 | 新增屬性 / 回傳型別要向後相容；grep 確認現有呼叫者僅依賴 `Compare()` 回傳的 `TableSchema` |
| Phase 2 整合點找不到統一入口 | 中 | 若上層散佈多處，考慮引入 `TableUpgradeCommandBuilder` orchestrator；實作前先 grep 評估 |
| SonarCloud S2077 / S3649（SQL injection） | 低 | 反向讀取的 tableName 透過 `DbCommandSpec` 佔位符；DDL 產生端的 tableName/欄位名透過 `QuoteName`、description 透過 `EscapeSqlString` |

回退方式：三個新／改動檔案可獨立 revert。資料面無變更。

## 驗收條件

- [ ] `SqlCreateTableCommandBuilder` 在 CREATE 與 UPGRADE 兩路徑都產生 `sp_addextendedproperty`
- [ ] 空 `DisplayName` / `Caption` 不輸出對應 extended property
- [ ] 單引號等特殊字元正確 escape
- [ ] `SqlTableSchemaProvider.GetTableSchema()` 回傳的 `DisplayName` 正確反映資料庫 extended property
- [ ] `TableSchemaComparer.Compare()` 額外回傳 `DescriptionChanges` 清單，不影響既有 `UpgradeAction` 判定
- [ ] `SqlExtendedPropertyCommandBuilder` 根據 changes 正確產 add / update SQL
- [ ] 升級執行流程：只改註解 → 不走 rebuild，只產 metadata DDL
- [ ] `[DbFact]` 整合測試驗證 rebuild 後 extended property 保留、metadata-only 路徑能正確更新
- [ ] 既有所有測試通過
