# 重構計畫：FormDefine → FormSchema、DbTable → TableSchema

## 背景

依架構討論決議，採方案 A 命名風格：

| 原名稱 | 新名稱 | 理由 |
|--------|--------|------|
| `FormDefine` | `FormSchema` | Define 是動詞，Schema 是業界標準名詞，語意更精確 |
| `FormLayout` | `FormLayout` | 不變 |
| `DbTable` | `TableSchema` | DbTable 不限於 Form 使用，TableSchema 通用且業界標準 |

---

## 影響範圍

### 1. 主要類別重新命名

| 原類別名稱 | 新類別名稱 | 檔案路徑 |
|-----------|-----------|---------|
| `FormDefine` | `FormSchema` | `src/Bee.Define/Forms/FormDefine.cs` → `FormSchema.cs` |
| `DbTable` | `TableSchema` | `src/Bee.Define/Database/DbTable.cs` → `TableSchema.cs` |

### 2. 連帶類別重新命名

| 原類別名稱 | 新類別名稱 | 檔案路徑 |
|-----------|-----------|---------|
| `FormDefineCache` | `FormSchemaCache` | `src/Bee.Cache/Define/FormDefineCache.cs` |
| `DbTableCache` | `TableSchemaCache` | `src/Bee.Cache/Define/DbTableCache.cs` |
| `DbTableGenerator` | `TableSchemaGenerator` | `src/Bee.Define/Database/DbTableGenerator.cs` |
| `DbTableCommandBuilder` | `TableSchemaCommandBuilder` | `src/Bee.Db/DbTableCommandBuilder.cs` |
| `DbTableIndex` | `TableSchemaIndex` | `src/Bee.Define/Database/DbTableIndex.cs` |
| `DbTableIndexCollection` | `TableSchemaIndexCollection` | `src/Bee.Define/Database/DbTableIndexCollection.cs` |
| `DbTableItem` | `TableItem` | `src/Bee.Define/Settings/DbSchemaSettings/DbTableItem.cs` |
| `DbTableItemCollection` | `TableItemCollection` | `src/Bee.Define/Settings/DbSchemaSettings/DbTableItemCollection.cs` |

### 3. Enum 值重新命名（`DefineType`）

檔案：`src/Bee.Define/Common.cs`

| 原值 | 新值 |
|------|------|
| `DefineType.FormDefine` | `DefineType.FormSchema` |
| `DefineType.DbTable` | `DefineType.TableSchema` |

### 4. Enum 對應字串（`DefineFunc`）

檔案：`src/Bee.Define/DefineFunc.cs`

| 原對應 | 新對應 |
|--------|--------|
| `{ DefineType.FormDefine, "Bee.Define.Forms.FormDefine" }` | `{ DefineType.FormSchema, "Bee.Define.Forms.FormSchema" }` |
| `{ DefineType.DbTable, "Bee.Define.Database.DbTable" }` | `{ DefineType.TableSchema, "Bee.Define.Database.TableSchema" }` |

### 5. 介面與方法簽章

檔案：`src/Bee.Define/IDefineAccess.cs`、`src/Bee.Define/Storage/IDefineStorage.cs`

| 原方法 | 新方法 |
|--------|--------|
| `GetFormDefine(string progId)` | `GetFormSchema(string progId)` |
| `SaveFormDefine(FormDefine formDefine)` | `SaveFormSchema(FormSchema formSchema)` |
| `GetDbTable(string dbName, string tableName)` | `GetTableSchema(string dbName, string tableName)` |
| `SaveDbTable(string dbName, DbTable dbTable)` | `SaveTableSchema(string dbName, TableSchema tableSchema)` |

### 6. 快取函式（`CacheFunc`、`CacheContainer`）

檔案：`src/Bee.Cache/CacheFunc.cs`、`src/Bee.Cache/CacheContainer.cs`

| 原方法/屬性 | 新方法/屬性 |
|------------|------------|
| `GetFormDefine(string progId)` | `GetFormSchema(string progId)` |
| `GetDbTable(string dbName, string tableName)` | `GetTableSchema(string dbName, string tableName)` |
| `CacheContainer.FormDefine` | `CacheContainer.FormSchema` |
| `CacheContainer.DbTable` | `CacheContainer.TableSchema` |

### 7. XmlType Attribute

| 原標註 | 新標註 |
|--------|--------|
| `[XmlType("FormDefine")]` | `[XmlType("FormSchema")]` |
| `[XmlType("DbTable")]` | `[XmlType("TableSchema")]` |

### 8. 檔案路徑與資料夾名稱

檔案：`src/Bee.Define/DefinePathInfo.cs`

| 原路徑格式 | 新路徑格式 |
|-----------|-----------|
| `FormDefine\{progId}.FormDefine.xml` | `FormSchema\{progId}.FormSchema.xml` |
| `DbTable\{dbName}\{tableName}.DbTable.xml` | `TableSchema\{dbName}\{tableName}.TableSchema.xml` |

方法同步重新命名：
- `GetFormDefineFilePath` → `GetFormSchemaFilePath`
- `GetDbTableFilePath` → `GetTableSchemaFilePath`

### 9. XML 序列化定義檔案（重新命名 + 移動）

**tests/Define/**
| 原路徑 | 新路徑 |
|--------|--------|
| `tests/Define/FormDefine/Department.FormDefine.xml` | `tests/Define/FormSchema/Department.FormSchema.xml` |
| `tests/Define/FormDefine/Employee.FormDefine.xml` | `tests/Define/FormSchema/Employee.FormSchema.xml` |
| `tests/Define/FormDefine/Project.FormDefine.xml` | `tests/Define/FormSchema/Project.FormSchema.xml` |
| `tests/Define/DbTable/Common/st_user.DbTable.xml` | `tests/Define/TableSchema/Common/st_user.TableSchema.xml` |
| `tests/Define/DbTable/Common/st_session.DbTable.xml` | `tests/Define/TableSchema/Common/st_session.TableSchema.xml` |

**samples/Define/**（同上結構）

### 10. 測試檔案

| 原檔案 | 新檔案 |
|--------|--------|
| `tests/Bee.Define.UnitTests/FormDefineTest.cs` | `FormSchemaTest.cs` |

---

## 執行順序

### 階段一：核心定義層（`Bee.Define`）

1. 重新命名類別檔案與類別本身
   - `FormDefine.cs` → `FormSchema.cs`（類別、XmlType）
   - `DbTable.cs` → `TableSchema.cs`（類別、XmlType）
   - `DbTableIndex.cs` → `TableSchemaIndex.cs`
   - `DbTableIndexCollection.cs` → `TableSchemaIndexCollection.cs`
   - `DbTableGenerator.cs` → `TableSchemaGenerator.cs`
   - `DbTableItem.cs` → `TableItem.cs`
   - `DbTableItemCollection.cs` → `TableItemCollection.cs`
2. 更新 `DefineType` enum 值
3. 更新 `DefineFunc` 對應字串
4. 更新 `DefinePathInfo` 路徑方法
5. 更新 `IDefineAccess`、`IDefineStorage` 介面方法簽章
6. 更新 `FileDefineStorage` 實作
7. 更新 `FormLayoutGenerator` 方法參數型別

### 階段二：快取層（`Bee.Cache`）

1. `FormDefineCache.cs` → `FormSchemaCache.cs`
2. `DbTableCache.cs` → `TableSchemaCache.cs`
3. 更新 `CacheContainer` 屬性名稱與型別
4. 更新 `CacheFunc` 方法名稱與型別
5. 更新 `LocalDefineAccess` 方法與 switch case

### 階段三：資料庫層（`Bee.Db`）

1. `DbTableCommandBuilder.cs` → `TableSchemaCommandBuilder.cs`
2. 更新 `TableSchemaComparer`（已是 Schema 命名，只需更新參數型別）
3. 更新各 Provider（`SqlCreateTableCommandBuilder`、`SqlTableSchemaProvider` 等）

### 階段四：連線層（`Bee.Connect`）

1. 更新 `RemoteDefineAccess` 方法名稱與參數型別

### 階段五：測試與範例

1. 重新命名測試類別與方法
2. 移動並重新命名 XML 序列化定義檔案（FormDefine → FormSchema、DbTable → TableSchema 資料夾）
3. 更新 XML 檔案內容中的根元素名稱（配合 `XmlType` 變更）

### 階段六：工具程式（`tools/`）

1. 更新 `BeeDbUpgrade` 中的 `DbTable` 相關參照

---

## 注意事項

- `FormLayout` 全程不變動，避免不必要的影響範圍
- 以下類別**本次不處理**，原因如下：
  - `DbField`、`DbFieldCollection`：`Db` 前綴語意為「資料庫欄位」，與 `DbTable` 類別命名無直接關聯
  - `DbSchema`、`DbSchemaSettings`、`DbSchemaCollection`：指資料庫結構設定，`Db` 前綴語意獨立
  - `FormTable`、`FormField` 等 Form 子類別：`Form` 前綴語意正確（表單內的 XX），不須跟著 FormDefine 更名
- `DbTableItem` → `TableItem` 的理由：職責是 `DbSchema` 中資料表的輕量描述條目，去掉 `Db` 前綴避免與 `TableSchema` 類別混淆
- XML 序列化定義檔案的根元素名稱須配合 `[XmlType]` 變更同步更新，否則反序列化會失敗
- 所有 switch/case 中的 `DefineType.FormDefine`、`DefineType.DbTable` 須全數更新
- 執行後須跑全套單元測試確認無遺漏

---

## 驗證方式

```bash
# 建置確認無編譯錯誤
dotnet build --configuration Release

# 執行全套測試
dotnet test --configuration Release

# 確認無殘留舊名稱（應無結果）
grep -r "FormDefine" src/ tests/ samples/ --include="*.cs"
grep -r "DbTable" src/ tests/ samples/ --include="*.cs"
```
