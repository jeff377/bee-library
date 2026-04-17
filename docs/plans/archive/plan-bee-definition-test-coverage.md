# Bee.Definition 測試覆蓋率提升計畫書

## 1. 背景與目標

`Bee.Definition` 是 Bee.NET 框架的定義中樞層，承載 FormSchema、TableSchema、FormLayout 等跨層資料契約（DTO），並定義各類列舉、常數、系統介面與序列化集合。它被 `Bee.Business`、`Bee.Api.Core`、`Bee.Repository` 等所有上層專案相依，其 DTO 的序列化正確性、資料結構完整性、集合語意皆為框架執行時正確性的基礎。

目前測試檔僅 4 個（`DefineFuncTests`、`DefinitionSerializationTests`、`FormSchemaTests`、`SecurityKeysTests`），對應源碼超過 60 個公開型別，覆蓋顯著不足。

**目標**：

1. 補齊 `Bee.Definition` 具邏輯的 DTO／公開工具函式的單元測試，使 SonarCloud 顯示該專案的 Line Coverage 達成 **≥ 75%**。
2. 所有具備可測試邏輯的 `public` 類別（建構子驗證、方法分支、序列化契約）至少有一個對應測試檔。
3. FormSchema、TableSchema、FormLayout、Filter 等核心 DTO 的 MessagePack / XML / JSON 序列化 round-trip 路徑必須 **100%** 覆蓋。
4. 不新增任何依賴資料庫、網路或檔案系統（mutable state）的測試；檔案 I／O 測試一律使用臨時目錄（`Path.GetTempPath()`），並在測試結束後清理。
5. 安全相關類別（`EncryptionKeyProtector`、`MasterKeyProvider`）需涵蓋**錯誤路徑**（金鑰無效、來源缺失、已破壞的資料）。

## 2. 現況盤點

圖例：✅ 已完整覆蓋 / ⚠ 部分覆蓋 / ❌ 完全無測試 / n/a 不需測試（介面／純常數）

### 2.1 根目錄（Top-Level）

| 類別 | 類型 | 狀態 | 備註 |
|------|------|------|------|
| `DefineFunc` | static utility | ⚠ | 僅 `GetDefineType` 3 種情境；另 4 個方法未測 |
| `DefinePathInfo` | static utility | ❌ | 7 個路徑組合方法完全未測 |
| `SessionInfo` | DTO | ❌ | 實作 `IKeyObject`、`IUserInfo`；序列化、`ToString()` 未測 |
| `SessionUser` | DTO | ❌ | 序列化 round-trip 未測 |
| `UserInfo` | DTO | ❌ | 屬性賦值、`IUserInfo` 契約未測 |
| `BackendInfo` | static config | ❌ | `Initialize`／元件建立邏輯未測 |
| `WebsiteInfo` | static (placeholder) | n/a | 檔案為空 |
| `SortField` | DTO | ❌ | 建構子參數驗證（`ArgumentException`）未測 |
| `SortFieldCollection` | collection | ❌ | 新增／移除／序列化未測 |
| 20＋ 純 `enum` 與常數類 | — | n/a | `SysFields`、`SysProgIds`、`SystemActions` 等，不具邏輯 |
| 10＋ `I*` 介面 | — | n/a | 介面本身不測，由實作方覆蓋 |

### 2.2 Attributes

| 類別 | 狀態 | 備註 |
|------|------|------|
| `ApiAccessControlAttribute` | ❌ | 建構子、`ProtectionLevel`／`AccessRequirement` 值繫結未測 |

### 2.3 Collections

| 類別 | 狀態 | 備註 |
|------|------|------|
| `MessagePackCollectionBase` / `MessagePackCollectionItem` | ❌ | 基底類別行為（Owner、索引）未測 |
| `MessagePackKeyCollectionBase` / `MessagePackKeyCollectionItem` | ❌ | Key lookup、重複 key 行為未測 |
| `ListItem` / `ListItemCollection` | ⚠ | 僅序列化 round-trip；查詢／變更操作未測 |
| `Parameter` / `ParameterCollection` | ⚠ | 僅序列化 round-trip；`DataTable`／`DataSet` 值轉換未測 |
| `Property` / `PropertyCollection` | ❌ | 完全未測 |

### 2.4 Database

| 類別 | 狀態 | 備註 |
|------|------|------|
| `DbField` / `DbFieldCollection` | ❌ | 欄位屬性、`IDefineField` 實作未測 |
| `IndexField` / `IndexFieldCollection` | ❌ | 索引欄位設定未測 |
| `TableSchema` / `TableSchemaIndex` / `TableSchemaIndexCollection` | ❌ | DTO 結構、序列化未測 |
| `TableSchemaGenerator` | ❌ | 從 `FormSchema` 生成 `TableSchema` 的轉換邏輯未測 |
| `IDefineField`（介面） | n/a | — |

### 2.5 Filters

| 類別 | 狀態 | 備註 |
|------|------|------|
| `FilterCondition` | ⚠ | 僅在 `FilterGroup` round-trip 間接覆蓋；建構子、屬性未直接測 |
| `FilterGroup` | ⚠ | 僅序列化；`LogicalOperator`、巢狀巢套層級未測 |
| `FilterNode` | ⚠ | 間接覆蓋；`FilterNodeKind` 各分支未測 |
| `FilterNodeCollection` | ⚠ | 間接覆蓋 |
| `FilterNodeCollectionJsonConverter` | ⚠ | 僅正向 round-trip；錯誤 JSON 輸入未測 |

### 2.6 Forms

| 類別 | 狀態 | 備註 |
|------|------|------|
| `FormField` / `FormFieldCollection` | ⚠ | `FormSchemaTests` 間接覆蓋結構；屬性邊界未測 |
| `FormSchema` | ⚠ | 2 個 scenario 測試；序列化、欄位查找未測 |
| `FormTable` / `FormTableCollection` | ⚠ | 間接覆蓋 |
| `FieldMapping` / `FieldMappingCollection` | ❌ | 完全未測 |
| `RelationFieldReference` / `RelationFieldReferenceCollection` | ⚠ | 間接覆蓋 |

### 2.7 Layouts

| 類別 | 狀態 | 備註 |
|------|------|------|
| `FormLayout` | ❌ | 序列化、基本結構未測 |
| `FormLayoutGenerator` | ❌ | 從 `FormSchema` 生成 layout 的邏輯未測 |
| `LayoutColumn` / `LayoutColumnCollection` | ❌ | 欄位寬度、格式化屬性未測 |
| `LayoutGrid` | ❌ | Columns 集合新增未測 |
| `LayoutGroup` / `LayoutGroupCollection` | ❌ | 巢狀結構未測 |
| `LayoutItem` / `LayoutItemBase` / `LayoutItemCollection` | ❌ | 完全未測 |

### 2.8 Logging

| 類別 | 狀態 | 備註 |
|------|------|------|
| `LogEntry` | ❌ | 屬性、`DateTimeKind` 設定未測 |
| `LogOptions` / `DbAccessAnomalyLogOptions` | ❌ | 預設值、屬性未測 |
| `ConsoleLogWriter` | ❌ | 寫入格式、`ILogWriter` 契約未測 |
| `NullLogWriter` | ❌ | Null Object 行為未測 |

### 2.9 Security

| 類別 | 狀態 | 備註 |
|------|------|------|
| `EncryptionKeyProtector` | ⚠ | 僅正向解密（`SecurityKeysTests`）；錯誤金鑰、毀損資料未測 |
| `MasterKeyProvider` | ⚠ | 正向路徑覆蓋；`MasterKeySourceType` 各來源、缺失情境未測 |
| 3 個 `I*` 介面 | n/a | — |

### 2.10 Serialization

| 類別 | 狀態 | 備註 |
|------|------|------|
| `SafeTypelessFormatter` | ❌ | MessagePack 型別白名單／黑名單、`SysInfo` 整合未測 |

### 2.11 Settings（六個子命名空間）

| 類別 | 狀態 | 備註 |
|------|------|------|
| `SystemSettings` | ⚠ | 僅 XML round-trip；各子屬性結構未測 |
| `DatabaseSettings`、`DbSchemaSettings`、`ProgramSettings`、`ClientSettings`、`MenuSettings` | ❌ | 預設值、序列化未測 |

### 2.12 Storage

| 類別 | 狀態 | 備註 |
|------|------|------|
| `FileDefineStorage` | ❌ | 讀取／寫入／不存在／權限錯誤情境未測 |
| `IDefineStorage`（介面） | n/a | — |

## 3. 優先級分類

依「影響面 × 邏輯複雜度」分三級，依序執行。

### P0 — 關鍵路徑（必做，影響所有上層專案）

1. **`DefineFunc`**：`GetNumberFormatString`、`ToColumnControlType`（internal，需 `InternalsVisibleTo`）、`ToLayoutColumn`、`GetListLayout`、`GetDefineType` 的無效值分支。
2. **`DefinePathInfo`**：7 個路徑方法 × 至少一個 assert，確認路徑組合格式（使用臨時 `BackendInfo.DefinePath`）。
3. **核心 DTO 序列化 round-trip**（XML + JSON + MessagePack 三軌並行）：
   - `SortField` / `SortFieldCollection`
   - `FormSchema`（完整巢狀：Tables → Fields）
   - `TableSchema`（含 Indexes）
   - `FormLayout`（含 Grid / Group / Column 巢狀）
   - `DatabaseSettings`、`DbSchemaSettings`、`ProgramSettings`、`ClientSettings`
4. **`SortField` 建構子**：空字串、`null`、純空白應拋 `ArgumentException`。
5. **`FilterGroup` / `FilterCondition` / `FilterNode`**：各 `FilterNodeKind`、`ComparisonOperator`、`LogicalOperator` 的分支；深度巢狀（≥ 3 層）序列化。

### P1 — 高價值邏輯（次做）

6. **`TableSchemaGenerator`**：從 `FormSchema` 生成 `TableSchema` 的欄位映射、主鍵推導、型別轉換。
7. **`FormLayoutGenerator`**：從 `FormSchema` 生成預設 layout 的欄位順序、分組規則。
8. **`MessagePackKeyCollectionBase`**：Key 查找、重複 key（預期例外或覆蓋）、`TryGetValue` 語意。
9. **`EncryptionKeyProtector` 錯誤路徑**：金鑰長度錯誤、HMAC 驗證失敗、Base64 格式錯誤。
10. **`MasterKeyProvider`**：`MasterKeySourceType.EnvironmentVariable` 與 `File` 各來源的讀取、缺失時的例外型別。
11. **`FileDefineStorage`**：讀取不存在的檔案、寫入後回讀、序列化格式一致性（XML only）。
12. **`SafeTypelessFormatter`**：允許的型別可 round-trip、`SysInfo` 黑名單型別被拒絕並拋出預期例外。

### P2 — 補完覆蓋（完成後達標）

13. **集合類完整操作**：`ListItemCollection`、`ParameterCollection`、`PropertyCollection`、`DbFieldCollection`、`FieldMappingCollection` 的 `Add` / `Remove` / `Clear` / `Contains` / 索引器。
14. **DTO 屬性測試**：`SessionInfo`、`SessionUser`、`UserInfo`、`LogEntry`、`LogOptions`、`DbAccessAnomalyLogOptions` 的預設值、屬性 setter / getter、`ToString()`。
15. **`ApiAccessControlAttribute`**：建構子組合（4 種 `ProtectionLevel` × 2 種 `AccessRequirement`）。
16. **`ConsoleLogWriter` / `NullLogWriter`**：用 `StringWriter` 重導 `Console.Out` 驗證輸出；`NullLogWriter` 不產生副作用。

## 4. 執行步驟

1. **建立測試檔骨架**：依資料夾鏡射新增測試檔，命名為 `<類別名>Tests.cs`。例：
   - `tests/Bee.Definition.UnitTests/DefinePathInfoTests.cs`
   - `tests/Bee.Definition.UnitTests/Collections/ListItemCollectionTests.cs`
   - `tests/Bee.Definition.UnitTests/Filters/FilterGroupTests.cs`
2. **共用 fixture**：在 `Bee.Tests.Shared` 或本專案內新增 `DefinitionTestData` 靜態類，提供最小可用的 `FormSchema`、`TableSchema`、`FormLayout` 建構 helper，避免每個測試重複 30 行物件建構。
3. **序列化 round-trip helper**：若 `Bee.Tests.Shared` 尚未有，新增 `SerializationRoundtripHelper`，泛型 API：`AssertXmlRoundtrip<T>(T)`、`AssertJsonRoundtrip<T>(T)`、`AssertMessagePackRoundtrip<T>(T)`。使用 `FluentAssertions` 或 `Assert.Equivalent` 比較結構。
4. **Internal 成員可測**：確認 `Bee.Definition.csproj` 已設定 `InternalsVisibleTo("Bee.Definition.UnitTests")`；若無則新增於 `AssemblyInfo.cs` 或 csproj `<ItemGroup>`。
5. **檔案 I/O 測試**：一律使用 `Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())` 建立隔離目錄，以 `IDisposable` fixture（`IAsyncLifetime`）清理；**不得**汙染 repo 的 `definition/` 目錄。
6. **CI 相容**：涉及本機 DB／服務的測試用 `[LocalOnlyFact]`；本計畫目標應 **無** 此類測試（純 DTO／邏輯）。
7. **執行驗證**：每批 P0／P1／P2 完成後執行：
   ```bash
   dotnet test tests/Bee.Definition.UnitTests/Bee.Definition.UnitTests.csproj \
     --configuration Release --settings .runsettings \
     --collect:"XPlat Code Coverage"
   ```
   回報新增測試數、當前行覆蓋率。

## 5. 命名與品質要求

- 測試方法命名：`<方法名>_<情境>_<預期結果>`（遵循 `.claude/rules/testing.md`）。
- 所有測試加 `[DisplayName]` 中文描述。
- `[Theory]` 優先於多個重複的 `[Fact]`，降低樣板碼。
- 每個測試只驗證一個行為；避免多重 `Assert` 混驗。
- 遵循 `sonarcloud.md` S2701：`Assert.True/False` 不可傳字面值。
- 遵循 `code-style.md`：4 空格縮排、UTF-8 無 BOM、LF 行結尾。
- 新增的 helper 類如設計為純 static，須加 `private` 建構子或改為 `static class`（S1118／S3442）。

## 6. 驗收條件

1. `dotnet test` 全部通過（本機 + CI）。
2. `Bee.Definition` 專案 SonarCloud Line Coverage ≥ 75%。
3. 所有列為 P0、P1 的類別至少一個測試檔存在。
4. 無 `#pragma warning disable`、無跳過（`Skip=...`）測試（本機專屬情境除外）。
5. 新增測試不引入對 `Bee.Repository`、`Bee.Api.*` 的相依（保持 `Bee.Definition` 測試獨立於上層）。

## 7. 預估工作量

| 階段 | 新增測試檔 | 測試方法數（概估） | 工時 |
|------|-----------|-------------------|------|
| P0 | 8–10 | 60–80 | 1.0 人日 |
| P1 | 6–8 | 40–60 | 0.8 人日 |
| P2 | 10–12 | 50–70 | 0.7 人日 |
| **合計** | **24–30** | **150–210** | **2.5 人日** |

## 8. 風險與備註

- **MessagePack `[Serializable]` 結合**：部分 DTO（如 `SortField`）同時標記 `[MessagePackObject]` 與 `[Serializable]`，序列化測試需涵蓋三條獨立路徑，注意 `[Key]` 編號變更會破壞相容性。
- **`InternalsVisibleTo`**：`DefineFunc.ToColumnControlType`、`ToLayoutColumn`、`GetListLayout` 為 `internal`；若未設定，需補設定或透過 `GetListLayout` 的公開入口間接測試。
- **`BackendInfo` 全域狀態**：`BackendInfo.DefinePath` 為 process-wide 可變狀態，測試需使用 `[Collection("Initialize")]` 串接 `GlobalFixture` 避免平行測試相互干擾，或改以可注入的方式（若需重構，另案處理）。
- **SonarCloud 覆蓋率計算**：以 `coverlet.collector` 產出的 `coverage.cobertura.xml` 為準；純 enum 與介面檔不計入行覆蓋率分母，實際達成 75% 的可行性高。
