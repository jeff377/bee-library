# 計畫 A：Bee.Definition 根層命名空間整理

**狀態：✅ 已完成（2026-04-28）**

## 背景

`Bee.Definition` 根層目前堆積 **44 份檔案**（22 enum + 9 static class + 9 interface + 4 class），形成典型 kitchen-sink 反模式。其中相當比例的型別有明顯該歸屬的子命名空間（已存在），卻被留在根層；新加入的開發者要找一個 enum 或 interface 時要在根層滾動翻找。

子命名空間使用情況：

| 子命名空間 | 既有檔案數 | 是否成熟分類 |
|-----------|-----------|-------------|
| `Database` | 9 | ✓ 已有 `DbField` / `TableSchema` 等 |
| `Filters` | 5 | ✓ 已有 `FilterCondition` / `FilterNode` 等 |
| `Forms` | 9 | ✓ 已有 `FormSchema` / `FormField` 等 |
| `Layouts` | 10 | ✓ 已有 `FormLayout` / `LayoutItem` 等 |
| `Logging` | 6 | ✓ 已有 `LogEntry` / `ILogWriter` 等 |
| `Security` | 5 | ✓ 已有 `MasterKeyProvider` / `IApiEncryptionKeyProvider` 等 |
| `Storage` | 2 | △ 既有但內容少 |
| `Collections` | 10 | ✓ 集合工具 |
| `Attributes` | 1 | ✓ 自訂 attribute |
| `Serialization` | 1 | ✓ 序列化 |
| `Settings` | 0（含子目錄） | ✓ 各 Settings 類別在子層 |

## 目標

1. 根層只保留**真正跨切面**的型別（如 `SysFields`、`SysFuncIDs`、`ApplicationType` 這類整個系統都用的常數/全域 enum）
2. 子命名空間既有分類（Database / Filters / Layouts / Logging / Security 等）擴大吸納明顯歸屬的型別
3. 所有公開型別**名稱不變**，僅 namespace 改變
4. 不異動套件版本號（沿用 4.0.x）

### 不涵蓋

- 行為變更（純命名空間調整）
- 公開型別改名 / 拆分 / 合併
- 子命名空間內既有檔案的調整
- 跨 package（`Bee.Db`、`Bee.Repository`、`Bee.Business` 等）邏輯異動 — 僅同步更新 `using`

## 搬移清單

### B1. → `Bee.Definition.Database`（4 份）

| 來源 | 目標 | 理由 |
|------|------|------|
| `DatabaseType.cs` | `Database/DatabaseType.cs` | 唯一描述 RDBMS 種類的 enum；69 份檔案使用 |
| `DbAccessAnomalyLogLevel.cs` | `Database/DbAccessAnomalyLogLevel.cs` | DB 異常 log 層級 enum |
| `DbUpgradeAction.cs` | `Database/DbUpgradeAction.cs` | DB schema 升級動作 enum |
| `FieldType.cs` | `Database/FieldType.cs` | DB 欄位型別 enum；15 份檔案使用 |

### B2. → `Bee.Definition.Filters`（3 份）

| 來源 | 目標 | 理由 |
|------|------|------|
| `FilterNodeKind.cs` | `Filters/FilterNodeKind.cs` | Filter node 種類 enum |
| `LogicalOperator.cs` | `Filters/LogicalOperator.cs` | filter 邏輯運算子 |
| `ComparisonOperator.cs` | `Filters/ComparisonOperator.cs` | filter 比對運算子 |

### B3. → `Bee.Definition.Layouts`（4 份；含 UI 控制相關）

| 來源 | 目標 | 理由 |
|------|------|------|
| `ControlType.cs` | `Layouts/ControlType.cs` | UI 控件種類 enum |
| `ColumnControlType.cs` | `Layouts/ColumnControlType.cs` | grid 欄位控件種類 |
| `GridControlAllowActions.cs` | `Layouts/GridControlAllowActions.cs` | grid 允許動作 |
| `SingleFormMode.cs` | `Layouts/SingleFormMode.cs` | 單表單模式 enum |

UI 控制介面（`IUIControl` / `IBindFieldControl` / `IBindTableControl`）視為 Layouts 的延伸：

| 來源 | 目標 | 理由 |
|------|------|------|
| `IUIControl.cs` | `Layouts/IUIControl.cs` | UI 控件抽象 |
| `IBindFieldControl.cs` | `Layouts/IBindFieldControl.cs` | 欄位繫結控件 |
| `IBindTableControl.cs` | `Layouts/IBindTableControl.cs` | 表格繫結控件 |

### B4. → `Bee.Definition.Logging`（1 份）

| 來源 | 目標 | 理由 |
|------|------|------|
| `LogEntryType.cs` | `Logging/LogEntryType.cs` | log entry 類別 enum，與 `LogEntry.cs` 同層 |

### B5. → `Bee.Definition.Security`（3 份）

| 來源 | 目標 | 理由 |
|------|------|------|
| `MasterKeySourceType.cs` | `Security/MasterKeySourceType.cs` | 與 `MasterKeyProvider` 同層 |
| `ApiAccessRequirement.cs` | `Security/ApiAccessRequirement.cs` | API 存取認證需求 enum |
| `ApiProtectionLevel.cs` | `Security/ApiProtectionLevel.cs` | API 保護等級 enum |

### B6. 新增 `Bee.Definition.Identity` 子命名空間（5 份）

身份／Session 相關的 5 份檔案目前散落根層，新建 `Identity/` 子目錄收容：

| 來源 | 目標 | 理由 |
|------|------|------|
| `SessionInfo.cs` | `Identity/SessionInfo.cs` | Session 資訊類別；18 份檔案使用 |
| `SessionUser.cs` | `Identity/SessionUser.cs` | Session 中的使用者資訊 |
| `UserInfo.cs` | `Identity/UserInfo.cs` | 使用者資訊 |
| `IUserInfo.cs` | `Identity/IUserInfo.cs` | 使用者資訊抽象 |
| `ISessionInfoService.cs` | `Identity/ISessionInfoService.cs` | Session 資訊服務介面 |

> 與 `Security` 分開的理由：Security 主要是加密／認證機制；Identity 是身份模型本身。職能不同。

### B7. → `Bee.Definition.Sorting`（新建子目錄，3 份）

`SortField` / `SortFieldCollection` / `SortDirection` 三者必出雙入對（21 份檔案使用 `SortDirection`），抽成獨立子命名空間：

| 來源 | 目標 |
|------|------|
| `SortField.cs` | `Sorting/SortField.cs` |
| `SortFieldCollection.cs` | `Sorting/SortFieldCollection.cs` |
| `SortDirection.cs` | `Sorting/SortDirection.cs` |

### B8. → `Bee.Definition.Storage`（1 份）

| 來源 | 目標 | 理由 |
|------|------|------|
| `IDefineAccess.cs` | `Storage/IDefineAccess.cs` | Define 儲存讀取契約，與 Storage 子層其他兩份檔案同類 |

### B9. → `Bee.Definition.Documents`（新建子目錄，1 份）

| 來源 | 目標 | 理由 |
|------|------|------|
| `IExcelHelper.cs` | `Documents/IExcelHelper.cs` | Excel 文件操作的純抽象（Open / Save / SetCellValue / Export 等），與 Form / Database / Layouts 都無關；新建 `Documents/` 預留分類空間，未來若有 PDF / CSV / Word 等類似 helper 抽象有明確去處 |

> 雖然目前只有單一檔案進入此命名空間，但 `IExcelHelper` 屬「文件格式 helper」這一類，不適合塞入既有任何子命名空間（Forms / Storage / Database 都不對）；新建 `Documents/` 比留根層或硬塞更精準，且預留擴充空間。

### B10. 留根層（不動，13 份）

以下檔案的本質是「整個系統都需要的全域常數／全域服務／橫切型別」，留根層合理：

| 檔案 | 留根層理由 |
|------|----------|
| `ApplicationType.cs` | 整個 framework 需要區分前後端 |
| `InitializeOptions.cs` | 框架初始化選項 |
| `DefineType.cs` | 系統定義種類；跨多個 module |
| `BackendDefaultTypes.cs` | 後端預設型別常數 |
| `BackendInfo.cs` | 後端資訊 |
| `DefineFunc.cs` | 跨切面 helper |
| `DefinePathInfo.cs` | 定義檔路徑資訊 |
| `GlobalEvents.cs` | 全域事件 |
| `PropertyCategories.cs` | UI 屬性類別常數 |
| `SysFields.cs` | 系統欄位常數 |
| `SysFuncIDs.cs` | 系統功能 ID 常數 |
| `SysProgIds.cs` | 系統程式 ID 常數 |
| `SystemActions.cs` | 系統動作常數 |
| `IBusinessObjectProvider.cs` | 跨層 BO 提供者；保留根層因 Business / Repository / Api 都引用 |
| `ICacheDataSourceProvider.cs` | 跨層 cache 提供者；保留同上 |
| `IEnterpriseObjectService.cs` | 整個 framework 入口服務 |

## 影響面

- 移動檔案：**約 25 份**（B1–B9）
- 留根層：**約 16 份**（B10）
- 跨 package 預估更新 `using` 檔案數：**≥150 份**（`DatabaseType` 一個就 69 份；其餘累加）

### 對外 API breaking change 對應表

| 舊 namespace | 新 namespace | 影響型別 |
|-------------|-------------|---------|
| `Bee.Definition` | `Bee.Definition.Database` | `DatabaseType`、`DbAccessAnomalyLogLevel`、`DbUpgradeAction`、`FieldType` |
| `Bee.Definition` | `Bee.Definition.Filters` | `FilterNodeKind`、`LogicalOperator`、`ComparisonOperator` |
| `Bee.Definition` | `Bee.Definition.Layouts` | `ControlType`、`ColumnControlType`、`GridControlAllowActions`、`SingleFormMode`、`IUIControl`、`IBindFieldControl`、`IBindTableControl` |
| `Bee.Definition` | `Bee.Definition.Logging` | `LogEntryType` |
| `Bee.Definition` | `Bee.Definition.Security` | `MasterKeySourceType`、`ApiAccessRequirement`、`ApiProtectionLevel` |
| `Bee.Definition` | `Bee.Definition.Identity`（新） | `SessionInfo`、`SessionUser`、`UserInfo`、`IUserInfo`、`ISessionInfoService` |
| `Bee.Definition` | `Bee.Definition.Sorting`（新） | `SortField`、`SortFieldCollection`、`SortDirection` |
| `Bee.Definition` | `Bee.Definition.Storage` | `IDefineAccess` |
| `Bee.Definition` | `Bee.Definition.Documents`（新） | `IExcelHelper` |

不升版號（沿用 4.0.x），release notes 列出對應表。

## 執行步驟

1. **B1–B9 各分組逐一執行**（每組為一個邏輯單位）：
   1. `git mv` 檔案到目標子目錄（必要時建立資料夾）
   2. 改檔內 `namespace Bee.Definition` → `namespace Bee.Definition.{X}`
   3. 全 repo 找該型別所有引用方（`grep -rln`），加 `using Bee.Definition.{X};`
   4. 立即 `dotnet build` 驗證

2. **驗證**
   1. `dotnet build --configuration Release` 全綠
   2. `./test.sh` 全綠

3. **文件同步**
   1. 更新 `src/Bee.Definition/README.md` 與 `README.zh-TW.md` Directory Structure 段落
   2. 視需要在 ADR-008 補一筆「相同設計原則套用至 Bee.Definition」的補述

## 風險與權衡

| 風險 | 緩解 |
|------|------|
| 影響面廣（≥150 份檔案需更新 using），可能漏改 | 全 repo `grep` 後逐一處理；倚賴 build 失敗作為安全網 |
| `IBusinessObjectProvider` / `ICacheDataSourceProvider` / `IEnterpriseObjectService` 該不該歸到子命名空間？ | 本版選擇留根層，因為這 3 個是「跨層 service locator」性質的根入口，類比 .NET 的 `IServiceProvider` 留 `System` 根層。可後續調整 |
| 新增 `Identity` / `Sorting` 兩個全新子命名空間，破壞既有命名習慣 | 只命名空間是新的，型別名都不變；release notes 詳列即可 |
| Settings/ 子命名空間（既有）只在子目錄有檔案，根層 Settings 字面為空——本次不動 | Settings 子目錄已自成體系（`ClientSettings/` / `DatabaseSettings/` 等），不在本次範圍 |

### 不採用的替代方案

1. **更激進的搬遷**（把 `IBusinessObjectProvider` / `ICacheDataSourceProvider` / `IEnterpriseObjectService` 也搬走）— 這 3 個是跨層服務入口，留根層更符合使用者習慣
2. **保留現狀僅做文件分組** — 不解決根本問題，每個新加入的開發者仍要在根層 44 份檔案中找型別

## 驗收標準

- [ ] `dotnet build --configuration Release` 全綠且無 warning
- [ ] `./test.sh` 全綠
- [ ] `Bee.Definition` 根層檔案數 ≤ 16
- [ ] 全 repo 不再出現「該移走的型別仍在 `Bee.Definition` 根 namespace」的引用
- [ ] 公開型別名稱完全不變
- [ ] `src/Bee.Definition/README.md` Directory Structure 與實際對齊
- [ ] 套件版本號未變動（仍為 4.0.x）
- [ ] CI 通過
