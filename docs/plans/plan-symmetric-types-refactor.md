# 計畫：對稱型別重複行重構評估

**狀態：📝 擬定中**

## 背景

SonarCloud 在 2026-04-28 巡檢時，**leak period（v4.0.1 起）的新增重複行**為 977 行，
其中 973 行（99.6%）集中在 `Bee.Db.Providers`，是新增 Sqlite provider 後與既有
SqlServer / PostgreSql provider 形成的對稱實作。其餘對稱型別對則屬全 repo 累計
重複（不在 leak 內），但仍為長期負債。

### Leak 主來源（占 99.6%）

| 子目錄 | dup_lines | 說明 |
|---|---|---|
| `src/Bee.Db/Providers/PostgreSql` | 376 | PostgreSql 對應的 schema / builder / helper |
| `src/Bee.Db/Providers/SqlServer` | 323 | SqlServer 對應的同名類別 |
| `src/Bee.Db/Providers/Sqlite` | 274 | Sqlite 對應的同名類別 |

主要重複的類別套件（每個 provider 都有對應實作）：
- `*AlterCompatibilityRules`（SQL 方言對 ALTER TABLE 的相容性規則）
- `*SchemaHelper`（schema 元數據查詢）
- `*TableRebuildCommandBuilder`（重建表的命令產生）
- `*TypeMapping`、`*CreateTableCommandBuilder`、`*TableAlterCommandBuilder`

### 既有對稱型別對（不在 leak 內，但仍為負債）

| 型別對 | dup_lines | 領域 |
|---|---|---|
| `Bee.Definition.Layouts.LayoutItem` ↔ `LayoutColumn` | 各 68 | Form 格線 layout / Grid 欄位 layout |
| `Bee.Base.Collections.CollectionItem` ↔ `KeyCollectionItem` | 各 52 | 集合元素 base / keyed 集合元素 base |

短期已用 `sonar.cpd.exclusions=**/Providers/**` 排除 Bee.Db Providers，讓 quality
gate 過關。LayoutItem / Column / CollectionItem 等則無 exclude 必要（不在 leak）。
本計畫目標：v5.x 規劃時系統性評估是否抽共用基底，並且**不在 v4.x 內動 public API**。

---

## 已知對稱型別對

### 1. LayoutItem ↔ LayoutColumn

**位置**：
- `src/Bee.Definition/Layouts/LayoutItem.cs`（繼承 `LayoutItemBase`）
- `src/Bee.Definition/Layouts/LayoutColumn.cs`（繼承 `CollectionItem`）

**共有屬性**（重複處）：
- `FieldName`、`Caption`、`ProgId`、`ReadOnly`、`DisplayFormat`、`NumberFormat`
- `ListItems`、`ExtendedProperties`（含 `IsSerializeEmpty` 樣板）
- `SetSerializeState(...)` 同寫法
- `ToString()` 同寫法

**差異點**：
- `ControlType` 是不同 enum（`ControlType` vs `ColumnControlType`）
- `LayoutItem` 多 `RowSpan`/`ColumnSpan`（form 格線排版）
- `LayoutColumn` 多 `Visible`/`Width`（grid 欄寬可見性）
- 繼承基底不同（`LayoutItemBase` vs `CollectionItem`），無法直接合併為 abstract base class

### 2. CollectionItem ↔ KeyCollectionItem

**位置**：
- `src/Bee.Base/Collections/CollectionItem.cs`
- `src/Bee.Base/Collections/KeyCollectionItem.cs`

**待補**：尚未逐行檢視，預期共有「序列化狀態 / parent 關聯 / change tracking」骨架，差異點為 keyed 集合的 `Key` 屬性。

### 3. 其他散落對（待辨識）

`Bee.Db`、`Bee.ObjectCaching`、`Bee.Api.Client/DefineAccess` 各約 46 行 dup，尚未拆對。下次更新本 plan 時補上。

---

## 重構方向

### A. 抽 interface（推薦）

抽出 `ILayoutFieldDescriptor` 等 marker interface，集中描述「FieldName / Caption / ProgId / DisplayFormat ...」屬性合約，原類別仍保留各自繼承。

| 優點 | 缺點 |
|---|---|
| 不破壞現有繼承 | 仍各自實作（重複未消除，僅抽合約） |
| 下游 NuGet 使用者可面向 interface 寫程式 | interface 無法包含實作（C# default interface members 可解，但語意混淆） |

### B. 抽 abstract base class + interface 混合

```csharp
public interface ILayoutFieldDescriptor { ... }

// 因兩者繼承基底不同，base class 走「組合」而非「繼承」
public sealed class LayoutFieldCore : ILayoutFieldDescriptor {
    public string FieldName { get; set; } = string.Empty;
    // 其餘共有屬性
}

public class LayoutItem : LayoutItemBase, ILayoutFieldDescriptor {
    private readonly LayoutFieldCore _core = new();
    public string FieldName { get => _core.FieldName; set => _core.FieldName = value; }
    // ...
}
```

| 優點 | 缺點 |
|---|---|
| 真正消除重複實作 | 組合模式增加一層間接，序列化（XML/MessagePack）需額外處理 |
| 序列化向前相容（屬性簽章不變） | `[XmlAttribute]` 等屬性配置需人工同步至轉發 property |

### C. Source generator

用 source generator 從 marker attribute 自動生成共有屬性。

| 優點 | 缺點 |
|---|---|
| 0 重複實作 | 學習曲線高、IDE/debugger 體驗差 |
| 完全可向後相容 | 生成程式碼難審閱、CI 慢 |

---

## 暫定立場

- **v4.x**：維持 `sonar.cpd.exclusions` 治標，不動 public API
- **v5.x**：以方案 B（組合 + interface）優先評估，先做 LayoutItem/Column 一對作為 PoC，視結果決定是否擴及其他對稱對
- 拒絕方案 A（不消除實作重複）與 C（複雜度過高，效益不對等）

---

## 待辦

1. 列完所有對稱型別對（含 Bee.Db / Bee.ObjectCaching / Bee.Api.Client 散落部分）
2. v5.x 規劃時把方案 B PoC 列為 milestone 候選
3. PoC 落地後評估是否擴及其他對稱對
4. exclusion 範圍逐步收斂回 0（每完成一對重構即從 `sonar.cpd.exclusions` 移除）

## 完成定義

- 所有已知對稱型別對都已重構或被明確拒絕（記錄理由）
- `sonar.cpd.exclusions` 為空字串，dup density 自然回到門檻內
