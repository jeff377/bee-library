# Bee.Define 命名空間重構計畫

## 目標

將 Bee.Define 所有 `.cs` 檔案的命名空間由統一的 `Bee.Define` 調整為與資料夾結構對應，
同時整理下列問題：

- `Attribute/`、`Common/`、`Events/`、`Info/`、`Interface/`、`Sorts/` 解散，檔案移至根目錄
- `Define/` 中間層資料夾解散，子資料夾提升至根層級並改名
- `DefineStorage/` 改名為 `Storage/`（去除冗餘前綴）
- 修正 `SortFIeldCollection.cs` 拼字錯誤 → `SortFieldCollection.cs`

---

## 一、資料夾與命名空間對應

| 資料夾 | 命名空間 |
|--------|---------|
| 專案根目錄 | `Bee.Define` |
| `Collections/` | `Bee.Define.Collections` |
| `Database/` | `Bee.Define.Database` |
| `Filters/` | `Bee.Define.Filters` |
| `Forms/` | `Bee.Define.Forms` |
| `Layouts/` | `Bee.Define.Layouts` |
| `Logging/` | `Bee.Define.Logging` |
| `Security/` | `Bee.Define.Security` |
| `Settings/` | `Bee.Define.Settings` |
| `Storage/` | `Bee.Define.Storage` |

---

## 二、各資料夾異動明細

### 2.1 專案根目錄（`Bee.Define`）

**從 `Common/` 移入（解散）：**

| 原始路徑 | 異動 |
|---------|------|
| `Common/Common.cs` | 移至根目錄，namespace 不變 |
| `Common/DefineFunc.cs` | 移至根目錄，namespace 不變 |
| `Common/DefinePathInfo.cs` | 移至根目錄，namespace 不變 |

**從 `Attribute/` 移入（解散）：**

| 原始路徑 | 異動 |
|---------|------|
| `Attribute/ApiAccessControlAttribute.cs` | 移至根目錄，namespace 不變 |

**從 `Events/` 移入（解散）：**

| 原始路徑 | 異動 |
|---------|------|
| `Events/GlobalEvents.cs` | 移至根目錄，namespace 不變 |

**從 `Info/` 移入（解散）：**

| 原始路徑 | 異動 |
|---------|------|
| `Info/BackendInfo.cs` | 移至根目錄，namespace 不變 |
| `Info/SessionInfo.cs` | 移至根目錄，namespace 不變 |
| `Info/SessionUser.cs` | 移至根目錄，namespace 不變 |
| `Info/UserInfo.cs` | 移至根目錄，namespace 不變 |
| `Info/WebsiteInfo.cs` | 移至根目錄，namespace 不變 |

**從 `Sorts/` 移入（解散）：**

| 原始路徑 | 異動 |
|---------|------|
| `Sorts/SortField.cs` | 移至根目錄，namespace 不變 |
| `Sorts/SortFIeldCollection.cs` | 改名為 `SortFieldCollection.cs`，移至根目錄，namespace 不變 |

**從 `Interface/` 移入（解散，語意屬全域合約）：**

| 原始路徑 | 異動 |
|---------|------|
| `Interface/IBindFieldControl.cs` | 移至根目錄，namespace 不變 |
| `Interface/IBindTableControl.cs` | 移至根目錄，namespace 不變 |
| `Interface/IBusinessObjectProvider.cs` | 移至根目錄，namespace 不變 |
| `Interface/ICacheDataSourceProvider.cs` | 移至根目錄，namespace 不變 |
| `Interface/IDefineAccess.cs` | 移至根目錄，namespace 不變 |
| `Interface/IEnterpriseObjectService.cs` | 移至根目錄，namespace 不變 |
| `Interface/IExcelHelper.cs` | 移至根目錄，namespace 不變 |
| `Interface/ISessionInfoService.cs` | 移至根目錄，namespace 不變 |
| `Interface/IUIControl.cs` | 移至根目錄，namespace 不變 |
| `Interface/IUserInfo.cs` | 移至根目錄，namespace 不變 |

---

### 2.2 `Collections/`

命名空間改為 `Bee.Define.Collections`。

| 檔案 | namespace 異動 |
|------|---------------|
| `ListItem.cs` | `Bee.Define` → `Bee.Define.Collections` |
| `ListItemCollection.cs` | `Bee.Define` → `Bee.Define.Collections` |
| `MessagePackCollectionBase.cs` | `Bee.Define` → `Bee.Define.Collections` |
| `MessagePackCollectionItem.cs` | `Bee.Define` → `Bee.Define.Collections` |
| `MessagePackKeyCollectionBase.cs` | `Bee.Define` → `Bee.Define.Collections` |
| `MessagePackKeyCollectionItem.cs` | `Bee.Define` → `Bee.Define.Collections` |
| `Parameter.cs` | `Bee.Define` → `Bee.Define.Collections` |
| `ParameterCollection.cs` | `Bee.Define` → `Bee.Define.Collections` |
| `Property.cs` | `Bee.Define` → `Bee.Define.Collections` |
| `PropertyCollection.cs` | `Bee.Define` → `Bee.Define.Collections` |

---

### 2.3 `Database/`（原 `Define/DbTable/`）

`Define/` 中間層資料夾解散，子資料夾改名為 `Database/` 並提升至根層級。
避免主類別 `DbTable` 與命名空間末段同名（CA1724），改用 `Bee.Define.Database`。

**從 `Interface/` 移入：**

| 原始路徑 | 異動 |
|---------|------|
| `Interface/IDefineField.cs` | 移至 `Database/`，namespace → `Bee.Define.Database` |

**原 `Define/DbTable/` 檔案：**

| 檔案 | namespace 異動 |
|------|---------------|
| `DbField.cs` | `Bee.Define` → `Bee.Define.Database` |
| `DbFieldCollection.cs` | `Bee.Define` → `Bee.Define.Database` |
| `DbTable.cs` | `Bee.Define` → `Bee.Define.Database` |
| `DbTableGenerator.cs` | `Bee.Define` → `Bee.Define.Database` |
| `DbTableIndex.cs` | `Bee.Define` → `Bee.Define.Database` |
| `DbTableIndexCollection.cs` | `Bee.Define` → `Bee.Define.Database` |
| `IndexField.cs` | `Bee.Define` → `Bee.Define.Database` |
| `IndexFieldCollection.cs` | `Bee.Define` → `Bee.Define.Database` |

---

### 2.4 `Filters/`

命名空間改為 `Bee.Define.Filters`。

| 檔案 | namespace 異動 |
|------|---------------|
| `FilterCondition.cs` | `Bee.Define` → `Bee.Define.Filters` |
| `FilterGroup.cs` | `Bee.Define` → `Bee.Define.Filters` |
| `FilterNode.cs` | `Bee.Define` → `Bee.Define.Filters` |
| `FilterNodeCollection.cs` | `Bee.Define` → `Bee.Define.Filters` |
| `FilterNodeCollectionJsonConverter.cs` | `Bee.Define` → `Bee.Define.Filters` |

---

### 2.5 `Forms/`（原 `Define/FormDefine/`）

`Define/` 中間層資料夾解散，子資料夾改名為 `Forms/` 並提升至根層級。
避免主類別 `FormDefine` 與命名空間末段同名（CA1724），改用 `Bee.Define.Forms`。

| 檔案 | namespace 異動 |
|------|---------------|
| `FieldMapping.cs` | `Bee.Define` → `Bee.Define.Forms` |
| `FieldMappingCollection.cs` | `Bee.Define` → `Bee.Define.Forms` |
| `FormDefine.cs` | `Bee.Define` → `Bee.Define.Forms` |
| `FormField.cs` | `Bee.Define` → `Bee.Define.Forms` |
| `FormFieldCollection.cs` | `Bee.Define` → `Bee.Define.Forms` |
| `FormTable.cs` | `Bee.Define` → `Bee.Define.Forms` |
| `FormTableCollection.cs` | `Bee.Define` → `Bee.Define.Forms` |
| `RelationFieldReference.cs` | `Bee.Define` → `Bee.Define.Forms` |
| `RelationFieldReferenceCollection.cs` | `Bee.Define` → `Bee.Define.Forms` |

---

### 2.6 `Layouts/`（原 `Define/FormLayout/`）

`Define/` 中間層資料夾解散，子資料夾改名為 `Layouts/` 並提升至根層級。
避免主類別 `FormLayout` 與命名空間末段同名（CA1724），改用 `Bee.Define.Layouts`。

| 檔案 | namespace 異動 |
|------|---------------|
| `FormLayout.cs` | `Bee.Define` → `Bee.Define.Layouts` |
| `FormLayoutGenerator.cs` | `Bee.Define` → `Bee.Define.Layouts` |
| `LayoutColumn.cs` | `Bee.Define` → `Bee.Define.Layouts` |
| `LayoutColumnCollection.cs` | `Bee.Define` → `Bee.Define.Layouts` |
| `LayoutGrid.cs` | `Bee.Define` → `Bee.Define.Layouts` |
| `LayoutGroup.cs` | `Bee.Define` → `Bee.Define.Layouts` |
| `LayoutGroupCollection.cs` | `Bee.Define` → `Bee.Define.Layouts` |
| `LayoutItem.cs` | `Bee.Define` → `Bee.Define.Layouts` |
| `LayoutItemBase.cs` | `Bee.Define` → `Bee.Define.Layouts` |
| `LayoutItemCollection.cs` | `Bee.Define` → `Bee.Define.Layouts` |

---

### 2.7 `Logging/`

命名空間改為 `Bee.Define.Logging`。（`ILogWriter.cs` 已在此資料夾，無需移動。）

| 檔案 | namespace 異動 |
|------|---------------|
| `ConsoleLogWriter.cs` | `Bee.Define` → `Bee.Define.Logging` |
| `DbAccessAnomalyLogOptions.cs` | `Bee.Define` → `Bee.Define.Logging` |
| `ILogWriter.cs` | `Bee.Define` → `Bee.Define.Logging` |
| `LogEntry.cs` | `Bee.Define` → `Bee.Define.Logging` |
| `LogOptions.cs` | `Bee.Define` → `Bee.Define.Logging` |
| `NullLogWriter.cs` | `Bee.Define` → `Bee.Define.Logging` |

---

### 2.8 `Security/`

命名空間改為 `Bee.Define.Security`。

**從 `Interface/` 移入：**

| 原始路徑 | 異動 |
|---------|------|
| `Interface/IAccessTokenValidationProvider.cs` | 移至 `Security/`，namespace → `Bee.Define.Security` |
| `Interface/IApiEncryptionKeyProvider.cs` | 移至 `Security/`，namespace → `Bee.Define.Security` |

**原 `Security/` 檔案：**

| 檔案 | namespace 異動 |
|------|---------------|
| `EncryptionKeyProtector.cs` | `Bee.Define` → `Bee.Define.Security` |
| `MasterKeyProvider.cs` | `Bee.Define` → `Bee.Define.Security` |

---

### 2.9 `Settings/`

命名空間改為 `Bee.Define.Settings`，子資料夾不再另設子命名空間。

| 子資料夾 | 影響檔案數 | namespace 異動 |
|---------|-----------|---------------|
| `ClientSettings/` | 3 | `Bee.Define` → `Bee.Define.Settings` |
| `DatabaseSettings/` | 5 | `Bee.Define` → `Bee.Define.Settings` |
| `DbSchemaSettings/` | 5 | `Bee.Define` → `Bee.Define.Settings` |
| `MenuSettings/` | 5 | `Bee.Define` → `Bee.Define.Settings` |
| `ProgramSettings/` | 5 | `Bee.Define` → `Bee.Define.Settings` |
| `SystemSettings/` | 11 | `Bee.Define` → `Bee.Define.Settings` |

---

### 2.10 `Storage/`（原 `DefineStorage/`）

資料夾改名（去除冗餘前綴），命名空間改為 `Bee.Define.Storage`。

**從 `Interface/` 移入：**

| 原始路徑 | 異動 |
|---------|------|
| `Interface/IDefineStorage.cs` | 移至 `Storage/`，namespace → `Bee.Define.Storage` |

**原 `DefineStorage/` 檔案：**

| 檔案 | namespace 異動 |
|------|---------------|
| `FileDefineStorage.cs` | `Bee.Define` → `Bee.Define.Storage` |

---

## 三、全方案 `using` 更新

重構後，所有參考 `Bee.Define` 的專案需依下表補充對應 `using`：

| 類型 / 介面 | 原 `using` | 新增 `using` |
|------------|-----------|-------------|
| `ListItem`, `Parameter`, `Property` 等 | `Bee.Define` | `Bee.Define.Collections` |
| `DbTable`, `DbField`, `IDefineField` 等 | `Bee.Define` | `Bee.Define.Database` |
| `FilterNode`, `FilterGroup` 等 | `Bee.Define` | `Bee.Define.Filters` |
| `FormDefine`, `FormField` 等 | `Bee.Define` | `Bee.Define.Forms` |
| `FormLayout`, `LayoutGrid` 等 | `Bee.Define` | `Bee.Define.Layouts` |
| `ILogWriter`, `LogEntry` 等 | `Bee.Define` | `Bee.Define.Logging` |
| `IAccessTokenValidationProvider`, `EncryptionKeyProtector` 等 | `Bee.Define` | `Bee.Define.Security` |
| `SystemSettings`, `DatabaseSettings` 等 | `Bee.Define` | `Bee.Define.Settings` |
| `IDefineStorage`, `FileDefineStorage` | `Bee.Define` | `Bee.Define.Storage` |

---

## 四、執行順序建議

1. **建立目標資料夾結構**：新增 `Database/`、`Forms/`、`Layouts/`、`Storage/` 資料夾
2. **解散小型資料夾**：將 `Attribute/`、`Common/`、`Events/`、`Info/`、`Sorts/` 內容移至根目錄，刪除資料夾
3. **解散 `Interface/`**：依上述分配將介面移至對應資料夾，刪除 `Interface/` 資料夾
4. **解散 `Define/` 中間層**：將 `DbTable/`、`FormDefine/`、`FormLayout/` 改名提升，刪除 `Define/` 資料夾
5. **改名 `DefineStorage/` → `Storage/`**
6. **修正 `SortFIeldCollection.cs` 檔名**
7. **批次更新所有 namespace 宣告**
8. **批次更新全方案 `using` 參考**
9. **執行 `dotnet build` 確認無編譯錯誤**
10. **執行 `dotnet test` 確認測試全數通過**

---

## 五、影響評估

| 項目 | 數量 |
|------|------|
| Bee.Define 內 namespace 異動檔案 | ~100 |
| 解散資料夾 | 7（`Attribute/`、`Common/`、`Events/`、`Info/`、`Interface/`、`Define/`、`Sorts/`） |
| 改名資料夾 | 1（`DefineStorage/` → `Storage/`） |
| 新建資料夾 | 4（`Database/`、`Forms/`、`Layouts/`、`Storage/`） |
| 修正檔名（拼字） | 1（`SortFIeldCollection.cs`） |
| 需補 `using` 的外部專案 | 待 `grep` 確認（估計 100+ 處） |
