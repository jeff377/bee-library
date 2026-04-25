# 資料表結構升級指引

[English](database-schema-upgrade.md)

> 本文件說明 Bee.NET 應用如何維護資料庫資料表結構：定義變更後如何同步到實際資料庫、底層採用何種升級策略、以及維運上的注意事項。
> 命名規範請參閱 [資料庫命名規範](database-naming-conventions.md)，定義驅動的整體理念請參閱 [ADR-005 FormSchema-Driven](adr/adr-005-formschema-driven.md)。

## 1. 核心觀念

Bee.NET 採 **define-driven schema**：資料表結構由 FormSchema / TableSchema 的 XML 定義為唯一來源，程式啟動或維運時由框架比對 define 與實際 DB，自動產生並執行所需的升級指令。

開發者寫程式時只需要：

1. 修改定義檔（增欄位、改長度、加索引）
2. 呼叫升級 API
3. 框架負責決定要走 ALTER 還是 rebuild

### 成功契約

> **升級成功返回後，DB 必須至少包含 define 裡所有欄位，且型別／長度／nullable 與 define 一致。**

這是從應用程式視角唯一重要的事。內部走 `ALTER TABLE` 或整表 rebuild 是 library 的選擇，呼叫端不需要關心。

## 2. 入口 API

### 2.1 一般用法：`IDatabaseRepository.UpgradeTableSchema`

最簡單的呼叫方式，適用於大多數場景：

```csharp
var repo = RepositoryInfo.Get<IDatabaseRepository>();
bool upgraded = repo.UpgradeTableSchema("common", "myDb", "ft_employee");
```

回傳值代表「是否實際執行了升級」：`false` 表示 DB 與 define 已一致，無變更。

> 介面定義在 [IDatabaseRepository](../src/Bee.Repository.Abstractions/System/IDatabaseRepository.cs)，預設實作於 [DatabaseRepository](../src/Bee.Repository/System/DatabaseRepository.cs)。

### 2.2 進階用法：`TableSchemaBuilder`

需要更細的控制（dry-run、`UpgradeOptions`、結構化 diff）時，直接使用底層的 [TableSchemaBuilder](../src/Bee.Db/Schema/TableSchemaBuilder.cs)：

```csharp
var builder = new TableSchemaBuilder("common");

// 取得結構化 diff（不執行）
TableSchemaDiff diff = builder.CompareToDiff("myDb", "ft_employee");

// 取得即將執行的 SQL（不執行）
string sql = builder.GetCommandText("myDb", "ft_employee");

// 執行升級（可選傳 UpgradeOptions）
bool upgraded = builder.Execute("myDb", "ft_employee", new UpgradeOptions
{
    AllowColumnNarrowing = true,
});
```

何時要降到這層：

- 部署前需要先看 SQL（dry-run）
- 想開啟「允許欄位縮小」等特殊選項
- 維運工具需要列出所有將變動的欄位 / 索引

## 3. 升級流程：Diff → Plan → Execute

底層拆成三個階段，每一階段都可以單獨呼叫：

```
┌─────────────────────────────┐
│ 1. CompareToDiff            │  比對 define vs 實際 DB
│    → TableSchemaDiff        │  純結構化、不含 SQL
└─────────────────────────────┘
              ↓
┌─────────────────────────────┐
│ 2. Orchestrator.Plan(diff)  │  決定走 ALTER 還是 rebuild
│    → UpgradePlan            │  含分階段 SQL 與警告
└─────────────────────────────┘
              ↓
┌─────────────────────────────┐
│ 3. Orchestrator.Execute     │  實際對 DB 執行
└─────────────────────────────┘
```

### TableSchemaDiff（結構化變更）

[TableSchemaDiff](../src/Bee.Db/Schema/TableSchemaDiff.cs) 是 provider 無關的中介結果，列出每一筆 `TableChange`：

| Change 型別 | 對應變更 |
|-------------|----------|
| `AddFieldChange` | 新增欄位 |
| `AlterFieldChange` | 既有欄位定義變更（型別、長度、nullable、default） |
| `RenameFieldChange` | 欄位改名（需設定 `DbField.OriginalFieldName`） |
| `AddIndexChange` | 新增索引 |
| `DropIndexChange` | 刪除索引 |

另含 `DescriptionChanges`（MS_Description / extended property 同步）。

### UpgradePlan（執行計畫）

[UpgradePlan](../src/Bee.Db/Schema/UpgradePlan.cs) 含 `Mode`（`NoChange` / `Create` / `Alter` / `Rebuild`）、`Stages`（分階段 SQL）與 `Warnings`。可直接列印 SQL：

```csharp
var diff = builder.CompareToDiff("myDb", "ft_employee");
var plan = new TableUpgradeOrchestrator("common").Plan(diff);

Console.WriteLine($"Mode: {plan.Mode}");
foreach (var sql in plan.AllStatements)
    Console.WriteLine(sql);
```

## 4. ALTER vs Rebuild：何時走哪條路

### 走 ALTER 的變更（秒級完成）

下列變更都能用 `ALTER TABLE` 在秒級完成，不搬資料：

- 新增欄位
- 同 family 內型別變化（如 `String(50) → String(100)`、`Integer → Long`）
- 改 nullable、改 default
- 新增 / 刪除索引
- 縮小長度（需要 `AllowColumnNarrowing=true`）

### 觸發 Rebuild 的兩類情境（SQL Server）

| 類型 | 範例 |
|------|------|
| **欄位型別跨 family 變更** | `String → Integer`、數值 → `Date`、`Boolean → 任何其他`、`Binary → 非 Binary`、`Guid ↔ String` |
| **AutoIncrement 狀態切換** | 一般欄位 ↔ IDENTITY 欄位（SQL Server `ALTER COLUMN` 無法改 IDENTITY 屬性） |

**Rebuild 機制**：建臨時表 → `INSERT INTO tmp SELECT FROM original` → drop 舊表 → rename。大資料表（千萬筆）耗時可達數十分鐘到小時，期間表級鎖定。

### 自動判斷，不需開發者選

Orchestrator 會逐一檢查 `TableSchemaDiff` 內每一筆 change：

- 全部都能走 ALTER → ALTER 路徑
- 任一筆需要 rebuild → 整表走 rebuild
- 任一筆 provider 不支援 → 直接拋例外中止

> 設計上**刻意不暴露 Strategy option**：使用者不該需要決定「我這次要走 ALTER 還是 rebuild」，這由 schema 變更內容唯一決定。

## 5. UpgradeOptions

目前僅有一個選項：

```csharp
public class UpgradeOptions
{
    /// <summary>
    /// 允許縮小欄位長度／精度的 ALTER COLUMN（可能截斷資料）。
    /// 預設 false：拒絕縮小，避免靜默資料遺失。
    /// </summary>
    public bool AllowColumnNarrowing { get; set; } = false;
}
```

### `AllowColumnNarrowing` 的意義

當 define 的欄位長度／精度小於 DB 現況時：

- 預設：**直接拒絕並拋例外**，避免靜默資料截斷
- 開啟：明確同意截斷，並在 plan 的 `Warnings` 中記錄

```csharp
var options = new UpgradeOptions { AllowColumnNarrowing = true };
builder.Execute("myDb", "ft_employee", options);
```

> **何時該開啟**：你已經確認 DB 現有資料在新長度內（可預先 `SELECT MAX(LEN(col))` 驗證），或欄位剛建立還沒有資料。**何時不該開啟**：對線上業務表的縮減，先做資料修整再升級 schema。

## 6. 欄位改名（`DbField.OriginalFieldName`）

預設情況下，Bee.NET 不會自動把「DB 有但 define 沒有的欄位」當成改名 — 那會被忽略（保留為 extension 欄位，不刪除）。如果要做改名，必須在 define 端**明確標示舊名**：

```xml
<DbField FieldName="employee_no" OriginalFieldName="emp_no" Caption="員工編號" />
```

升級時 comparer 會偵測到 `emp_no` → `employee_no` 的改名意圖，產出 `RenameFieldChange`，SQL 走 `sp_rename`（保留資料）。

### 使用規則

| 情境 | 行為 |
|------|------|
| DB 有舊名 `emp_no`、無新名 | 執行 `sp_rename` |
| DB 已有新名 `employee_no` | no-op（冪等，可重跑） |
| DB 無舊名也無新名 | 警告 + 降級為新增欄位 |
| 跨多版本連續改名 | **不支援**：每版部署完成後請清掉 `OriginalFieldName` |

### 適用情境

- ✅ 新模組開發期、定義頻繁迭代
- ❌ 已部署到生產且跨多版本的累積改名 — 跳版部署不保證行為

> 改名後**下個版本就應移除** `OriginalFieldName`，避免 metadata 殘留。

## 7. 執行階段與 Transaction 行為

ALTER 路徑下，orchestrator 把 SQL 切成有序階段執行，每階段獨立 transaction：

```
Stage 1: DropIndexes        刪除即將被改定義的索引
Stage 2: AlterColumns       既有欄位 rename / 改型別 / 改長度 / 改 nullable
Stage 3: AddColumns         新增欄位
Stage 4: CreateIndexes      建新索引、重建先前刪除的
Stage 5: SyncDescriptions   extended property（MS_Description）同步
```

### 失敗行為

- 某 stage 失敗 → 該 stage 內 transaction rollback、後續 stage 不執行、例外拋出
- 已成功 commit 的前面 stage **不會回滾**
- comparer 是**冪等**的：修正失敗原因後重跑，已完成的 stage 會被視為「無變更」自動跳過

> 這個設計刻意不做整體 transaction：DDL 在多數 DB 不支援可靠的整體回滾，per-stage + 冪等重跑是更實際的策略。

## 8. 不支援的情境

下列情況**不在自動升級的範圍內**，需手動處理：

| 不支援 | 原因 / 替代方案 |
|--------|----------------|
| **Drop column** | 延續 extension field 保留政策；第三方整合可能依賴未列入 define 的欄位。確定要刪除請手動下 SQL |
| **Foreign key** | 框架原則：referential integrity 由 BO 處理，不依賴 DB 層 |
| **Trigger / View** | 同上，business rules 不放 DB |
| **跨版本多次 rename** | 單次 rename 才保證冪等；多版本累積請逐版部署 |
| **跨 DB schema** | 目前僅支援預設 schema |
| **Online schema change** | 如 SQL Server `ALTER INDEX ... WITH (ONLINE = ON)`，需自行下 SQL |

## 9. Dry-run 與部署實務

### 部署前先看 SQL

對大資料表（千萬筆以上）部署前，**強烈建議 dry-run** 先確認模式：

```csharp
var diff = builder.CompareToDiff("myDb", "ft_orders");
var plan = new TableUpgradeOrchestrator("common").Plan(diff);

if (plan.Mode == UpgradeExecutionMode.Rebuild)
{
    // 這次會走 rebuild — 排維護視窗
    Console.WriteLine("Rebuild will be triggered:");
    Console.WriteLine(builder.GetCommandText("myDb", "ft_orders"));
}
```

### 何時需要排維護視窗

| Plan.Mode | 影響 | 行動 |
|-----------|------|------|
| `NoChange` | 無 | 不需動作 |
| `Create` | 新表，無資料 | 直接執行 |
| `Alter` | 秒級，最多單欄位短暫鎖定 | 一般時段可執行 |
| `Rebuild` | 全表搬資料、表級鎖定 | **排維護視窗**，並先估算搬移時間 |

### 欄位縮小的標準流程

不要直接開 `AllowColumnNarrowing=true` 跑線上表。建議流程：

1. dry-run 確認哪些欄位被縮小
2. `SELECT COUNT(*) FROM table WHERE LEN(col) > <newLength>` 確認現有資料
3. 若有超出資料 → 先做資料修整（修剪、搬到新欄、業務溝通）
4. 確認資料都在範圍內 → 才開選項升級

### 大資料表的 rebuild 替代

如果 dry-run 顯示 rebuild、但業務不允許停機，請考慮：

- 拆解變更：把跨 family 的型別變更拆成「新增新欄位 → 業務雙寫 → backfill → 切換 → 刪舊欄」多步部署
- 手動 online schema change：依 DB provider 能力自行下 SQL，跳過 Bee.NET 自動升級
- 業務雙跑：暫不修改舊表，新功能用新表，舊表自然汰換

## 10. 參考

### 原始檔
- [TableSchemaBuilder](../src/Bee.Db/Schema/TableSchemaBuilder.cs) — 對外入口
- [TableUpgradeOrchestrator](../src/Bee.Db/Schema/TableUpgradeOrchestrator.cs) — Plan / Execute
- [TableSchemaDiff](../src/Bee.Db/Schema/TableSchemaDiff.cs) / [UpgradePlan](../src/Bee.Db/Schema/UpgradePlan.cs)
- [UpgradeOptions](../src/Bee.Db/Schema/UpgradeOptions.cs)
- [DbField.OriginalFieldName](../src/Bee.Definition/Database/DbField.cs)

### 相關文件
- [資料庫命名規範](database-naming-conventions.md)
- [架構總覽](architecture-overview.zh-TW.md)
- [開發指引](development-cookbook.md)
- [開發限制](development-constraints.md)
- [ADR-005：FormSchema-Driven](adr/adr-005-formschema-driven.md)
