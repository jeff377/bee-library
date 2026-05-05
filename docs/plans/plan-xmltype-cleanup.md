# 計畫：清理冗餘 `[XmlType("ClassName")]` 標記

**狀態：✅ 已完成（2026-05-05）**

## 背景

專案內共 **41 處** `[XmlType("ClassName")]` 標記，**全部都是「XmlType 名 = 類別名」的冗餘形式**：

```csharp
[XmlType("FormSchema")]   // 與類別名相同，純冗餘
public class FormSchema { ... }
```

`XmlSerializer` 預設就用類別名作為 XML element name，這類標記不改變任何序列化行為。

### 為什麼算冗餘

- **預設行為已足**：`XmlSerializer` 看到 `class FormSchema` 自動用 `<FormSchema>` 作為 element 名
- **違反 .NET 一般慣例**：MSDN 範例與多數開源專案，類別名 = XML 名時省略 `[XmlType]`
- **違反 DRY**：類別名與標記名雙處維護

### 為什麼現在處理

1. FormLayout 重構（`plan-formlayout-redesign.md`）採一般慣例為新類別不加 `[XmlType]`，留下既有檔案的冗餘標記會造成風格混合
2. 統一清理是低風險工作（XML 結構零變化），可獨立 PR 完成

## 目標

對齊 .NET 一般慣例：類別名 = XML 名時，**移除 `[XmlType("ClassName")]` 標記**。

## 影響範圍盤點

執行時依當前狀態重新盤點（FormLayout 重構若先完成，layouts/ 內部分檔案已不存在）。當前 41 處分佈：

### `Bee.Definition/Layouts/`（5 處）
> 註：若 FormLayout 重構先完成，此目錄已大幅變動，剩下的檔案為 `LayoutGrid`、`LayoutColumn`（其餘已刪除/重寫不加標記）

- `FormLayout.cs`
- `LayoutGroup.cs`
- `LayoutItem.cs`
- `LayoutGrid.cs`
- `LayoutColumn.cs`

### `Bee.Definition/Forms/`（4 處）
- `FormSchema.cs`
- `FormTable.cs`
- `FormField.cs`
- `FieldMapping.cs`

### `Bee.Definition/Database/`（4 處）
- `DbField.cs`
- `TableSchema.cs`
- `IndexField.cs`
- `TableSchemaIndex.cs`

### `Bee.Definition/Collections/`（2 處）
- `ListItem.cs`
- `Property.cs`

### `Bee.Definition/Settings/`（約 26 處）

| 子目錄 | 檔案 |
|--------|------|
| `ClientSettings/` | `ClientSettings.cs`, `EndpointItem.cs` |
| `DatabaseSettings/` | `DatabaseSettings.cs`, `DatabaseServer.cs`, `DatabaseItem.cs` |
| `DbSchemaSettings/` | `DbSchemaSettings.cs`, `DbSchema.cs`, `TableItem.cs` |
| `MenuSettings/` | `MenuSettings.cs`, `MenuFolder.cs`, `MenuItem.cs` |
| `ProgramSettings/` | `ProgramSettings.cs`, `ProgramCategory.cs`, `ProgramItem.cs` |
| `SystemSettings/` | `SystemSettings.cs`, `BackendConfiguration.cs`, `BackendComponents.cs`, `BackgroundServiceConfiguration.cs`, `WebsiteConfiguration.cs`, `FrontendConfiguration.cs`, `CommonConfiguration.cs`, `SecurityKeySettings.cs`, `MasterKeySource.cs`, `ApiPayloadOptions.cs`, `VersionFiles.cs` |

## 例外保留條件

**不要動**以下三類 `[XmlType]`（本次盤點未發現，但執行時若遇到要保留）：

| 條件 | 範例 | 原因 |
|------|------|------|
| XmlType 名 ≠ 類別名 | `[XmlType("OldName")]` 標在 `class NewName` | 維持 XML 向後相容，是有意的 override |
| 指定 namespace | `[XmlType(Namespace = "http://...")]` | XML schema 需要 |
| 匿名型別 | `[XmlType(AnonymousType = true)]` | 控制 schema 行為 |
| 排除 schema | `[XmlType(IncludeInSchema = false)]` | 控制 schema 行為 |

判斷規則：**只清純粹 `[XmlType("ClassName")]` 形式（單一參數、無其他屬性）**。

## 風險評估

- **XML 結構零變化**：移除冗餘標記後 XmlSerializer 行為與原本完全相同（用類別名做 element 名）
- **二進位相容**：純 attribute 移除，不影響 method signature、不破壞已編譯客戶端
- **既有 XML 檔可正常讀取**：包括 `tests/Define/` 內 SystemSettings/DbSchemaSettings 等 fixture 檔

## 驗證

每批次清理後執行：

```bash
dotnet build --configuration Release       # 編譯通過
./test.sh                                  # 全部測試 pass
```

關鍵測試：
- `tests/Bee.Definition.UnitTests/DtoSerializationTests.cs` — XML round-trip 測試集中地，必須全 pass
- `tests/Bee.ObjectCaching.UnitTests/LocalDefineAccessSaveTests.cs` — 涵蓋 FormSchema/SystemSettings/DbSchemaSettings 的 round-trip

## 執行步驟

建議**分 5 個 commit 依 namespace 切割**，每 commit 後跑一次測試確認：

| # | Commit 範圍 | 大致檔案數 |
|---|------------|----------|
| 1 | `Bee.Definition/Layouts/` | 視 FormLayout 重構狀態 0–5 個 |
| 2 | `Bee.Definition/Forms/` | 4 個 |
| 3 | `Bee.Definition/Database/` | 4 個 |
| 4 | `Bee.Definition/Collections/` | 2 個 |
| 5 | `Bee.Definition/Settings/` | ~26 個 |

切割理由：
- 萬一某個 namespace 出現意外（如有 fixture XML 不相容），影響範圍可隔離
- 各 commit 變更焦點清楚，code review 容易

### 每個檔案的處理

1. 移除 `[XmlType("ClassName")]` 行
2. 若該行的 `using System.Xml.Serialization;` 在移除後不再被該檔案其他地方使用，順手清掉 using
3. 確認檔案其他內容完全不動

### 機械化執行

可寫一行 sed/grep 命令批次處理，但**人工確認每個改動**（因為要排除上述例外條件）：

```bash
# 預覽（不修改）
grep -rn '\[XmlType("' --include="*.cs" src/

# 各檔案手動或半自動移除
```

## 與 FormLayout 重構 plan 的關係

本 plan **獨立執行**，不依賴 FormLayout 重構順序：

- **FormLayout 重構先做**：執行本 plan 時，layouts/ 下已沒有舊檔案的標記，盤點 step 1 範圍縮減
- **本 plan 先做**：FormLayout 重構時新類別本來就不加 `[XmlType]`，無衝突

兩個 plan 修改的檔案集合**幾乎不重疊**（FormLayout 重構動到 layouts/ 結構與 forms/FormSchema/FormField，本 plan 動到所有 [XmlType] 標記行；交集只在 LayoutGrid / LayoutColumn / FormSchema / FormField 等檔案）。為避免雙線競爭：

- 若兩 plan 並行，**FormLayout 重構先 merge**，本 plan 在其後 rebase 處理剩餘檔案
- 若想完全規避 conflict，**等 FormLayout 重構完成後再啟動本 plan**

## 不在本次 Scope（Follow-up）

1. 其他冗餘 attribute（`[Description]` 重複標、`[DefaultValue]` 與 property 初始值重複）— 屬於另一個治理工作
2. Settings/ 內某些類別的 `<TypeName>Settings` 命名是否重新審視（例如 `SecurityKeySettings` vs `SecurityKey`）— schema 命名議題

## 預期成果

- 全專案 41 處冗餘 `[XmlType]` 標記移除（扣掉 FormLayout 重構已處理的部分）
- 對齊 .NET 一般慣例
- 程式碼行數淨減約 41 行
- 後續新類別不需糾結「要不要標 `[XmlType]`」— 慣例明確：**類別名 = XML 名就不加**
