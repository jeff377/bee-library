# 計畫：組織部門樹 + per-company 快取 + GetDepartmentTree API（record scope 前置）

**狀態：✅ 已完成（2026-06-05）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 部門樹三棲物件（XML+JSON+MessagePack）+ `ft_department.parent_rowid` + per-company 快取鏈（cache / service / repository） | ✅ 已完成（2026-06-05） |
| 2 | `GetDepartmentTree` API（SystemBO 跨 contract / wire / client）+ wire round-trip 測試 | ✅ 已完成（2026-06-05） |

## 背景

record scope（層二，`ScopeStrategy.Dept` / `DeptAndSub`）需要「當前 user 的部門 + 子部門」才能過濾資料範圍。前置是**組織階層**——目前 `ft_department` 平坦（只有 `manager_rowid`，無 parent），沒有任何部門樹 / 階層查詢。

組織模型已拍板 **Odoo-like 純部門樹**（討論見對話）：部門樹即組織，員工掛部門，帳號透過 company 內的 employee 身分接入。不走 SAP-like 的 Org Unit / Position。

**序列化需求（三棲）**：組織樹要傳前端（`GetDepartmentTree`），且 XML 適合存 DB（未來組織異動快照）。故 `DepartmentTree` **必須同時支援 XML + JSON + MessagePack** 三種序列化：
- **XML**（`XmlCodec`）— 存檔 / 快照
- **JSON**（`System.Text.Json`）— JS 前端友善（`JSON.parse`）
- **MessagePack** — API wire 高效傳遞

框架已有三棲樣板（`FilterNodeCollection` / `SortFieldCollection` = `MessagePackCollectionBase<T>`，有 round-trip 測試），照走即可。

## 設計

### 1. `ft_department` 加 `parent_rowid`

| 欄位 | 型別 | 說明 |
|------|------|------|
| （既有） | | `sys_no` / `sys_rowid` / `sys_id` / `sys_name` / `manager_rowid` |
| `parent_rowid` | Guid | 上級部門（指向同表 `sys_rowid`）；`Guid.Empty` = 根節點 |

純樹（單一上級），比照既有 `manager_rowid`：Guid NOT NULL、`Guid.Empty` 表「無上級」。company-category 表（per-company 各自的部門樹）。

### 2. `DepartmentTree` 三棲可序列化物件（`Bee.Definition/Organization/`）

**序列化狀態**（扁平節點清單）與**查詢索引**（parent→children + 後代集合）分離：前者三棲序列化、後者 lazy 建立不序列化。集合照 `MessagePackCollectionBase<T>` 樣板（三棲最乾淨：`Owner`/`SerializeState`/`Tag` 三標籤全跳過、無 `ItemsForSerialization` 代理；MessagePack 由 `FormatterResolver` 動態掛 `CollectionBaseFormatter`，**不需改 `MessagePackCodec`**）。

```csharp
// 節點 — 三棲（XmlAttribute + Key + JSON 自動），ICollectionItem 元素
[MessagePackObject]
public class DepartmentNode : ICollectionItem
{
    [Key(0)] [XmlAttribute] public Guid RowId { get; set; }
    [Key(1)] [XmlAttribute] public string DeptId { get; set; } = string.Empty;     // sys_id
    [Key(2)] [XmlAttribute] public string DeptName { get; set; } = string.Empty;   // sys_name
    [Key(3)] [XmlAttribute] public Guid ParentRowId { get; set; }                  // Guid.Empty = root
    [Key(4)] [XmlAttribute] public Guid ManagerRowId { get; set; }
}

// 集合走三棲 base（memory: Definition 集合走 base class）
[MessagePackObject]
public class DepartmentNodeCollection : MessagePackCollectionBase<DepartmentNode> { ... }

[MessagePackObject]
public class DepartmentTree : IKeyObject
{
    public DepartmentTree() { }                                  // 無參數 ctor（序列化要求）

    [Key(0)] [XmlAttribute] public string CompanyId { get; set; } = string.Empty;
    [Key(1)] [XmlArrayItem(typeof(DepartmentNode))]
    public DepartmentNodeCollection? Nodes { get; set; }         // 序列化的唯一狀態

    public string GetKey() => CompanyId;

    // 查詢索引：lazy thread-safe（double-check lock），三棲全跳過
    [IgnoreMember, XmlIgnore, JsonIgnore] private ...;

    // record scope DeptAndSub：自身 + 所有後代 rowid（樹中無此 dept → 空）
    public IReadOnlyList<Guid> GetSelfAndDescendants(Guid deptRowId);   // EnsureIndex()
    public bool Contains(Guid deptRowId);
    public IReadOnlyList<Guid> GetSelfAndAncestors(Guid deptRowId);     // 自身 + 祖先鏈
    public DepartmentNode? GetNode(Guid deptRowId);
    public IReadOnlyList<DepartmentNode> Roots { get; }
}
```

- **索引 lazy + thread-safe**：查詢方法先 `EnsureIndex()`（double-check lock，從 `Nodes` 建 parent→children 與後代集合，build 一次後唯讀）。後端快取共享 → 並發第一次查詢靠 lock 保護；build 後無鎖成本。前端反序列化還原後同樣 lazy 建。**符合 memory「cache 不可 mutate」**：序列化狀態建構後不變，index 是唯讀衍生。
- **防環**：`EnsureIndex` 對 parent 鏈做訪問標記，偵測到環即停（不無限遞迴）。
- 判定/查詢全在物件方法內 → 合成節點純單元測試、不綁 DB。

### 2b. 三棲序列化驗證（照 `FilterNodeCollection` 樣板）

- **XML**：`XmlCodec`（無參數 ctor + `[XmlAttribute]` 屬性 + `Nodes` 標 `[XmlArrayItem]`；index 因 `[XmlIgnore]` 不出現）。
- **JSON**：`System.Text.Json`（`DepartmentNode` 單型別，集合直接列舉 → array；視測試結果決定是否需 `JsonConverter`，FilterNode 因多型才需，DepartmentNode 多半不需）。
- **MessagePack**：`DepartmentNode`/`DepartmentTree` 標 `[MessagePackObject]` + `[Key]`；`DepartmentNodeCollection` 靠 `FormatterResolver` 動態掛 `CollectionBaseFormatter`（序列化成 array）。
- **無敏感欄位** → 不需 `ISerializableClone`（與 GetDefine 對加密欄位的深複製保護不同）。

### 3. path B 快取鏈（Phase 1，照 `bee-add-cache-object` skill）

| # | 檔案 | 內容 |
|---|------|------|
| 1 | `src/Bee.Definition/Organization/DepartmentNode.cs` + `DepartmentNodeCollection.cs` + `DepartmentTree.cs` | 三棲物件（上述） |
| 2 | `src/Bee.ObjectCaching/Database/DepartmentTreeCache.cs` | `: KeyObjectCache<DepartmentTree>`，`CreateInstance => null` |
| 3 | `src/Bee.Definition/Organization/IDepartmentTreeService.cs` | `Get(companyId)` / `Remove(companyId)` |
| 4 | `src/Bee.ObjectCaching/Services/DepartmentTreeService.cs` | cache miss → `ICompanyInfoService` 解析 company DB → repository 載節點 → `new DepartmentTree(...)` → `Set` |
| 5 | `src/Bee.Repository.Abstractions/System/IDepartmentRepository.cs` + `src/Bee.Repository/System/DepartmentRepository.cs` | `GetDepartments(databaseId)` 讀 company DB 全部 `ft_department`；比照 `RolePermissionRepository`（company scope、ISystemRepositoryFactory） |
| 6 | `ISystemRepositoryFactory` + `SystemRepositoryFactory` | 加 `CreateDepartmentRepository()` |
| 7 | `src/Bee.ObjectCaching/ICacheContainer.cs` | 加 `DepartmentTreeCache DepartmentTree { get; }` |
| 8 | `src/Bee.ObjectCaching/CacheContainerService.cs` | 三處（init + eviction 陣列 + 屬性宣告） |
| 9 | `src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs` | DI：`IDepartmentRepository`（factory）+ `IDepartmentTreeService` |
| 10 | 兩個 CacheNotify stub | `CacheNotifyPollerUnitTests` + `CacheNotifyPollSessionUnitTests` 補 `DepartmentTree` 屬性（漏補必 CS0535） |
| (11) | cache-notify bump 點 | 部門異動時 `Touch("DepartmentTree:{companyId}", tx, dbType)`；目前無部門管理 BO → bump 點**留待**（同 `CompanyRolePermissions` 現況） |

### 4. `GetDepartmentTree` API（Phase 2，照 `bee-add-bo-method` skill）

掛在 **SystemBusinessObject**（比照 `GetFormSchema` / `GetDefine`）。回當前 session 公司的部門樹，**直接傳物件**（`Result.Tree = DepartmentTree`，三棲；wire format 由 PayloadFormat 決定 JSON 或 MessagePack，前端各取所需）。

| # | 檔案 | 內容 |
|---|------|------|
| 1 | `src/Bee.Api.Contracts/IGetDepartmentTreeRequest.cs` + `IGetDepartmentTreeResponse.cs` | 純介面 |
| 2 | `src/Bee.Api.Core/Messages/System/GetDepartmentTreeRequest.cs` + `GetDepartmentTreeResponse.cs` | `[MessagePackObject]`；`Response.Tree` `[Key(100)]` = `DepartmentTree?` |
| 3 | `src/Bee.Definition/SystemActions.cs` | 加 `GetDepartmentTree` 常數 |
| 4 | `src/Bee.Business/System/GetDepartmentTreeArgs.cs` + `GetDepartmentTreeResult.cs` | `Result.Tree = DepartmentTree?`（無參數 args，用 session 公司） |
| 5 | `src/Bee.Business/System/SystemBusinessObject.cs` | `GetDepartmentTree`：`session.CompanyId` → `IDepartmentTreeService.Get` → `Result.Tree`；`[ApiAccessControl(Authenticated)]` |
| 6 | `src/Bee.Business/System/ISystemBusinessObject.cs` | 加方法簽名 |
| 7 | `src/Bee.Api.Client/Connectors/SystemApiConnector.cs` | `GetDepartmentTreeAsync` + 同步 wrapper |

- **不需 Repository CRUD 層**（layer 9）：資料走 `IDepartmentTreeService`（Phase 1 已建），BO 只取快照。
- 未 `EnterCompany`（`CompanyId` 空）→ 回空樹或 `null`（依既有慣例，比照 `CompanyNotEntered`）。

## 測試

| 層 | 測什麼 |
|----|--------|
| POCO 純單元（`tests/Bee.Definition.UnitTests/Organization/DepartmentTreeTests.cs`） | 合成多層節點：`GetSelfAndDescendants`（多層後代）、`Contains`、`GetSelfAndAncestors`、`Roots`、單/未知節點邊界、**防環** |
| **三棲 round-trip**（`DepartmentTreeSerializationTests.cs`） | **XML**（`XmlCodec`）、**JSON**（`System.Text.Json`）、**MessagePack**（`MessagePackCodec`）三種各 round-trip：還原後 `Nodes` 完整、查詢一致、index 正確重建；空樹/單節點邊界 |
| Service（`tests/Bee.ObjectCaching.UnitTests/Services/DepartmentTreeServiceTests.cs`） | fake repository + fake `ICompanyInfoService`：cache miss 載入 + cache hit 短路（兩次只載一次）、未知 company 回 null |
| Repository（`tests/Bee.Repository.UnitTests/DepartmentRepositoryTests.cs`） | `[DbFact]` 5 DB round-trip：insert `ft_department`（含 parent 連結）→ `GetDepartments` 查回正確 |
| API（Phase 2，`tests/Bee.Api.Core.UnitTests/System/GetDepartmentTree*Tests.cs`） | wire round-trip（`DepartmentTree` 跨 MessagePack/JSON）、executor dispatch（`System.GetDepartmentTree` 派發、stub service） |

## 非目標（明確劃線）

- **user↔employee 連結**、「當前登入者 → 部門」解析 → record-scope enforcement plan。
- **record scope 實際過濾**（`ScopeResolver`、把 `GetSelfAndDescendants` 接進查詢 WHERE）→ record-scope enforcement plan。
- **員工匯報樹**（employee.parent，直屬主管，與部門樹正交）→ 非當前需求。
- **矩陣組織 / Position / 組織時間版本**（SAP-like）→ 不做；未來需要時 `ft_department` 加 `unit_type` 漸進演進。
- **部門管理 BO/UI**（新增/異動部門）→ 另案；屆時補 cache-notify bump 點。
- **XML 存組織異動快照的實際落地**（存哪、何時存）→ 本計畫只確保 `DepartmentTree` 可 XML 序列化，不建快照儲存流程。

## 驗收

- `ft_department` 加 `parent_rowid`（5 DB fresh CREATE 正常）。
- slnx build 0w/0e（含兩個 CacheNotify stub）。
- `DepartmentTree` 純單元測試全綠（含防環）。
- **XML + JSON + MessagePack 三棲 round-trip 測試全綠**（還原後查詢一致、index 正確重建）。
- `DepartmentTreeService` cache miss/hit 綠；`DepartmentRepository` 5 DB round-trip 綠。
- Phase 2：`GetDepartmentTree` wire round-trip + executor dispatch 綠。
- 本機全套 + CI 全 5 DB + SonarCloud quality gate passed。
