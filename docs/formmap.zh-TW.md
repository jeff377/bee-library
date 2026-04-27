# FormMap：FormSchema 驅動的資料庫存取模式

[English](formmap.md)

> Form-Mapping for Definition-Driven Architecture

---

## 目錄

1. [一句話定義](#1-一句話定義)
2. [為什麼不是 ORM](#2-為什麼不是-orm)
3. [核心概念](#3-核心概念)
4. [範例](#4-範例)
5. [實作對應](#5-實作對應)
6. [適用與不適用場景](#6-適用與不適用場景)
7. [限制與設計取捨](#7-限制與設計取捨)
8. [延伸閱讀](#8-延伸閱讀)

---

## 1. 一句話定義

**FormMap** 是 Bee.Db 採用的資料庫存取模式：以 `FormSchema` 為單位描述業務實體，透過外鍵欄位（`RelationProgId`）將 `FormSchema` 串成關聯鏈，執行階段動態組出 SELECT / INSERT / UPDATE / DELETE 語法，產出資料以 `DataSet` 承載，**不依賴強型別 entity class**。

FormMap **不是 ORM 的子集或變體**，而是與 ORM 平行的另一種資料存取模式。

---

## 2. 為什麼不是 ORM

ORM（Object-Relational Mapping）解決的是「OOP 物件模型 ↔ 關聯模型」之間的 impedance mismatch；FormMap 解決的是「業務表單模型 ↔ 關聯模型」之間的對映。映射兩端不同，整體取捨自然不同。

### 2.1 核心差異對照

| 面向 | ORM | FormMap |
|---|---|---|
| 映射對象 | **物件**（編譯期類別） | **定義**（執行期 `FormSchema`） |
| 載體 | typed object graph | `DataSet` / `DataTable` |
| 模型形式 | C# class + attribute | `FormSchema` 物件（執行期快取；可由 XML 等格式持久化） |
| 關聯層級 | 資料表（Table-Level） | 表單（Form-Level） |
| 變更模型 | 改類別 → 重編 → 重佈署 | 改定義 → 重載快取 → 立即生效 |
| 查詢介面 | LINQ + Expression Tree | `FilterNode` + 欄位字串 |
| 物件實例化 | 自動 hydration 為 entity 實例 | 不實例化，欄位以 `DataRow` 承載 |
| 變更追蹤 | Identity Map / Change Tracking（追蹤 entity 屬性） | `DataRow.RowState` + `DataSet.GetChanges()`（DataSet 內建逐列狀態機） |

### 2.2 為什麼選 FormMap

ERP 系統的高頻變動會讓編譯期綁定的 ORM 付出沉重代價：

- **動態欄位**：欄位依角色／權限／公司別動態出現或隱藏，強型別類別需大量條件分支
- **客製化欄位**：不同客戶的同一張表單可能有不同欄位，類別變體會爆炸
- **多租戶**：每個租戶可能有獨立的欄位集合，編譯期綁定難以承擔
- **動態關聯來源**：同一張表單在不同情境參照不同來源（例如報價單在不同流程關聯不同客戶來源）

FormMap 把這些「變」的部分外推到執行期可重新載入的 `FormSchema` 定義，重編程式不再是日常操作的代價。

---

## 3. 核心概念

### 3.1 表單級關聯（Form-Level Relation）

FormMap 中，關聯**不是**「這張表的 FK 指向那張表的 PK」，而是「這張表單參照另一張表單」。

定義位置在 `FormField.RelationProgId`。`FormSchema` 在執行階段為**快取物件**，XML 是其常見的持久化格式之一（理論上亦可由資料庫、JSON 等其他來源載入）；下面以 XML 形式呈現便於閱讀：

```xml
<FormField FieldName="pm_rowid" RelationProgId="Employee">
  <RelationFieldMappings>
    <FieldMapping SourceField="sys_name" DestinationField="ref_pm_name"/>
  </RelationFieldMappings>
</FormField>
```

`RelationProgId="Employee"` 指向另一個 `FormSchema`，**不是**指向資料表。這層抽象讓「表單」成為業務實體單位，而非 raw table。

### 3.2 單階宣告、多階執行

**每張 `FormSchema` 只宣告自己直接參照的下一層**，不需描述更深的關聯。多層 JOIN 在執行期沿著 `FormSchema` 鏈遞迴展開。

```
Project    宣告： pm_rowid    → Employee
Employee   宣告： dept_rowid  → Department

開發者寫 Project → ref_pm_dept_name 時：
執行期自動展開： Project → Employee → Department（兩層 JOIN）
```

開發者的心智負擔等同 single-hop（每張 `FormSchema` 只看一層），但 SQL 產出可任意深。

### 3.3 在 Definition-Driven Architecture 中的位置

```
Definition-Driven Architecture（整體架構）
└── FormSchema（Single Source of Truth）
    ├── 驅動 UI    → FormLayout
    ├── 驅動 DB    → TableSchema
    ├── 驅動驗證  → ValidationRules
    └── 驅動資料存取 → FormMap（本文件）
```

FormMap 是 DDA 在資料存取層的具體模式，與 `FormLayout` / `TableSchema` 並列為 `FormSchema` 的三大投影面向。

---

## 4. 範例

下列範例使用三張 `FormSchema`（簡化版）：

- `Project`（專案）— `pm_rowid` 參照 `Employee`、`owner_dept_rowid` 參照 `Department`
- `Employee`（員工）— `dept_rowid` 參照 `Department`
- `Department`（部門）— `pm_rowid` 參照 `Employee`（部門主管）

### 範例 1：純主檔查詢（無 JOIN）

```csharp
var builder = new SqlFormCommandBuilder("Project");
var command = builder.BuildSelectCommand("Project", "sys_id,sys_name");
```

**產出 SQL：**
```sql
SELECT A.[sys_id], A.[sys_name]
FROM [ft_project] A
```

未使用任何參考欄位，FormMap 不產生 JOIN。

### 範例 2：條件觸發 JOIN

```csharp
var filter = new FilterCondition("ref_pm_name", ComparisonOperator.StartsWith, "張");
var command = builder.BuildSelectCommand("Project", "sys_id,sys_name", filter);
```

**產出 SQL：**
```sql
SELECT A.[sys_id], A.[sys_name]
FROM [ft_project] A
LEFT JOIN [ft_employee] B ON A.[pm_rowid] = B.[sys_rowid]
WHERE B.[sys_name] LIKE @p0
```

WHERE 子句用到 `ref_pm_name`（來自 `Employee`），FormMap 自動加入 `Employee` 的 JOIN。

### 範例 3：排序觸發多層 JOIN

```csharp
var sortFields = new SortFieldCollection
{
    new SortField("ref_pm_dept_name", SortDirection.Asc)
};
var command = builder.BuildSelectCommand("Project", "sys_id,sys_name", null, sortFields);
```

**產出 SQL：**
```sql
SELECT A.[sys_id], A.[sys_name]
FROM [ft_project] A
LEFT JOIN [ft_employee] B ON A.[pm_rowid] = B.[sys_rowid]
LEFT JOIN [ft_department] C ON B.[dept_rowid] = C.[sys_rowid]
ORDER BY C.[sys_name] ASC
```

`ref_pm_dept_name` 跨越 `Project → Employee → Department` 兩層，FormMap 沿 `FormSchema` 鏈遞迴展開。

### 範例 4：多參考欄位

```csharp
var command = builder.BuildSelectCommand("Project",
    "sys_id,sys_name,ref_owner_dept_name,ref_pm_dept_name");
```

**產出 SQL：**
```sql
SELECT A.[sys_id], A.[sys_name],
       B.[sys_name] AS [ref_owner_dept_name],
       D.[sys_name] AS [ref_pm_dept_name]
FROM [ft_project] A
LEFT JOIN [ft_department] B ON A.[owner_dept_rowid] = B.[sys_rowid]
LEFT JOIN [ft_employee]   C ON A.[pm_rowid]         = C.[sys_rowid]
LEFT JOIN [ft_department] D ON C.[dept_rowid]       = D.[sys_rowid]
```

兩個參考欄位走不同的 `FormSchema` 鏈，FormMap 自動建立分支 JOIN。

### 範例 5：複合條件

```csharp
var filterGroup = FilterGroup.All(
    FilterCondition.Contains("sys_name", "專案"),
    FilterCondition.Equal("ref_pm_name", "張三"));
var sortFields = new SortFieldCollection
{
    new SortField("sys_id", SortDirection.Asc)
};
var command = builder.BuildSelectCommand("Project", "sys_id,sys_name", filterGroup, sortFields);
```

**產出 SQL：**
```sql
SELECT A.[sys_id], A.[sys_name]
FROM [ft_project] A
LEFT JOIN [ft_employee] B ON A.[pm_rowid] = B.[sys_rowid]
WHERE (A.[sys_name] LIKE @p0 AND B.[sys_name] = @p1)
ORDER BY A.[sys_id] ASC
```

只有實際被引用的 `FormSchema` 會被 JOIN — FormMap 不會多 JOIN 未被使用的關聯。

---

## 5. 實作對應

| 元件 | 角色 |
|---|---|
| `Bee.Definition.Forms.FormSchema` / `FormField` | FormMap 的資料來源（業務實體與關聯定義） |
| `Bee.Db.Sql.SelectContextBuilder` | 從 `FormSchema` 鏈遞迴展開 `TableJoin` 集合與 `QueryFieldMapping` |
| `Bee.Db.Sql.SelectBuilder` | 產生 `SELECT` 子句 |
| `Bee.Db.Sql.FromBuilder` | 產生 `FROM` 子句（含 JOIN） |
| `Bee.Db.Sql.WhereBuilder` | 產生 `WHERE` 子句與參數化 |
| `Bee.Db.Sql.SortBuilder` | 產生 `ORDER BY` 子句 |
| `Bee.Db.Sql.SelectCommandBuilder` | 整合上述四個 builder，產出最終 SELECT `DbCommandSpec` |
| `Bee.Db.Sql.InsertCommandBuilder` / `UpdateCommandBuilder` / `DeleteCommandBuilder` | 從 `FormSchema` 與 `DataRow` / `FilterNode` 產出 IUD `DbCommandSpec`（單表、無 JOIN、識別子依方言 quote） |
| `Bee.Db.Providers.IFormCommandBuilder` | 各 DB 方言的入口介面（`SqlFormCommandBuilder` / `PgFormCommandBuilder`），方法 `Build{Select,Insert,Update,Delete}` 委派至上述共用核心 |

---

## 6. 適用與不適用場景

### 6.1 適用

- `FormSchema` 驅動的 CRUD 操作（NoCode / LowCode 路徑）
- ERP 動態欄位、客製化、多租戶
- UI 列表、篩選、排序場景（直接由 `FormSchema` 配置）
- 需要熱更新欄位／關聯定義的環境

### 6.2 不適用

- 報表 / 統計 / 批次匯入：走 BO + AnyCode 直接撰寫 SQL
- 複合鍵 JOIN、非等值 JOIN、子查詢、CTE
- 對效能極致敏感的熱點查詢
- 已知不需要動態欄位、且能接受 ORM 編譯成本的場景

> **雙軌策略：`FormSchema` 驅動的 CRUD 走 FormMap，任意形狀的 SQL 走 BO + AnyCode。**
> 詳見 [development-cookbook.md](development-cookbook.md)。

---

## 7. 限制與設計取捨

下列「限制」皆為對應 `FormSchema` 世界觀的合理取捨，不是缺漏：

| 限制 | 為什麼 |
|---|---|
| JOIN 必為單欄位等值（FK = PK） | `FormSchema` 的 RelationField 一律經 `RowId` 等值參照 |
| JOIN 對象必為實體表（無子查詢 / CTE / TVF） | `FormSchema` 對映實體表，沒有「子查詢式表單」的概念 |
| 主表別名固定為 `A` | 與 `SelectContextBuilder` 的別名生成器一致（`A → B → ... → Z → ZA → ZB`，跳過 SQL 保留字） |
| 不支援多層 master-detail 同時組裝 | 主檔／明細透過 `DataSet` 多 `DataTable` 組裝，由 BO 層處理 |

---

## 8. 延伸閱讀

- [架構總覽](architecture-overview.zh-TW.md)：BeeNET 的整體架構
- [ADR-005：FormSchema 定義驅動架構](adr/adr-005-formschema-driven.md)：FormMap 上位的設計決策
- [專有名詞對照表](terminology.md)：FormMap 與相關名詞的中英文對照
- [Bee.Db README](../src/Bee.Db/README.zh-TW.md)：Bee.Db 套件總覽
- [開發指引](development-cookbook.md)：FormSchema 驅動開發、雙軌策略
