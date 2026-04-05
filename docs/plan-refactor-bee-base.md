# Bee.Base 命名空間重構計畫

## 目標

將 Bee.Base 所有 `.cs` 檔案的命名空間由統一的 `Bee.Base` 調整為與資料夾結構對應，
同時移除語意不明的 `Common/`、`Exception/`、`Interface/` 資料夾，
並將 `Serialize/`、`BackgroundService/` 資料夾改名以符合命名空間。

---

## 一、資料夾與命名空間對應

| 資料夾 | 命名空間 |
|--------|---------|
| 專案根目錄 | `Bee.Base` |
| `Attributes/` | `Bee.Base.Attributes` |
| `BackgroundServices/` | `Bee.Base.BackgroundServices` |
| `Collections/` | `Bee.Base.Collections` |
| `Data/` | `Bee.Base.Data` |
| `Security/` | `Bee.Base.Security` |
| `Serialization/` | `Bee.Base.Serialization` |
| `Tracing/` | `Bee.Base.Tracing` |

---

## 二、各資料夾異動明細

### 2.1 專案根目錄（`Bee.Base`）

**從 `Common/` 移入：**

| 原始路徑 | 異動 |
|---------|------|
| `Common/AssemblyLoader.cs` | 移至根目錄，namespace 不變 |
| `Common/BaseFunc.cs` | 移至根目錄，namespace 不變 |
| `Common/ConnectionTestResult.cs` | 移至根目錄，namespace 不變 |
| `Common/DateTimeFunc.cs` | 移至根目錄，namespace 不變 |
| `Common/FileFunc.cs` | 移至根目錄，namespace 不變 |
| `Common/HttpFunc.cs` | 移至根目錄，namespace 不變 |
| `Common/IPValidator.cs` | 移至根目錄，namespace 不變 |
| `Common/MemberPath.cs` | 移至根目錄，namespace 不變 |
| `Common/StrFunc.cs` | 移至根目錄，namespace 不變 |
| `Common/SysInfo.cs` | 移至根目錄，namespace 不變 |

**從 `Common/Common.cs` 拆出（新建檔案）：**

| 型別 | 新檔名 |
|------|--------|
| `DefaultBoolean` | `DefaultBoolean.cs` |
| `NotSetBoolean` | `NotSetBoolean.cs` |
| `DateInterval` | `DateInterval.cs` |

**從 `Interface/` 移入：**

| 原始路徑 | 異動 |
|---------|------|
| `Interface/IDisplayName.cs` | 移至根目錄，namespace 不變 |
| `Interface/IKeyObject.cs` | 移至根目錄，namespace 不變 |
| `Interface/ITagProperty.cs` | 移至根目錄，namespace 不變 |
| `Interface/ISysInfoConfiguration.cs` | 移至根目錄，namespace 不變 |

**從 `Exception/` 移入（僅單一類別，不建立獨立命名空間）：**

| 原始路徑 | 異動 |
|---------|------|
| `Exception/ApiException.cs` | 移至根目錄，namespace 改為 `Bee.Base` |

---

### 2.2 `Attributes/`（`Bee.Base.Attributes`）

| 檔案 | 異動 |
|------|------|
| `Attributes/TreeNodeAttribute.cs` | namespace 改為 `Bee.Base.Attributes` |
| `Attributes/TreeNodeIgnoreAttribute.cs` | namespace 改為 `Bee.Base.Attributes` |

**從 `Common/Common.cs` 拆出（新建檔案）：**

| 型別 | 新檔名 |
|------|--------|
| `PropertyCategories` | `Attributes/PropertyCategories.cs` |

---

### 2.3 `BackgroundServices/`（`Bee.Base.BackgroundServices`，資料夾從 `BackgroundService/` 改名）

| 檔案 | 異動 |
|------|------|
| `BackgroundServices/BackgroundAction.cs` | namespace 改為 `Bee.Base.BackgroundServices` |
| `BackgroundServices/BackgroundService.cs` | namespace 改為 `Bee.Base.BackgroundServices` |
| `BackgroundServices/BackgroundServiceStatusChangedEvent.cs` | namespace 改為 `Bee.Base.BackgroundServices` |

**從 `Common/Common.cs` 拆出（新建檔案）：**

| 型別 | 新檔名 |
|------|--------|
| `BackgroundServiceStatus` | `BackgroundServices/BackgroundServiceStatus.cs` |
| `BackgroundServiceAction` | `BackgroundServices/BackgroundServiceAction.cs` |

---

### 2.4 `Collections/`（`Bee.Base.Collections`）

| 檔案 | 異動 |
|------|------|
| `Collections/CollectionBase.cs` | namespace 改為 `Bee.Base.Collections` |
| `Collections/CollectionItem.cs` | namespace 改為 `Bee.Base.Collections` |
| `Collections/Dictionary.cs` | namespace 改為 `Bee.Base.Collections` |
| `Collections/KeyCollectionBase.cs` | namespace 改為 `Bee.Base.Collections` |
| `Collections/KeyCollectionItem.cs` | namespace 改為 `Bee.Base.Collections` |
| `Collections/StringHashSet.cs` | namespace 改為 `Bee.Base.Collections` |

**從 `Interface/` 移入：**

| 原始路徑 | 異動 |
|---------|------|
| `Interface/ICollectionBase.cs` | 移至 `Collections/`，namespace 改為 `Bee.Base.Collections` |
| `Interface/ICollectionItem.cs` | 移至 `Collections/`，namespace 改為 `Bee.Base.Collections` |
| `Interface/IKeyCollectionBase.cs` | 移至 `Collections/`，namespace 改為 `Bee.Base.Collections` |
| `Interface/IKeyCollectionItem.cs` | 移至 `Collections/`，namespace 改為 `Bee.Base.Collections` |

**從 `Common/Extensions.cs` 拆出（新建檔案）：**

| 型別 | 新檔名 | 內容 |
|------|--------|------|
| `CollectionExtensions` | `Collections/CollectionExtensions.cs` | `PropertyCollection.GetValue<T>` |

---

### 2.5 `Data/`（`Bee.Base.Data`，資料夾從 `DataSet/` 改名，避免與 `System.Data.DataSet` 類別同名衝突）

| 檔案 | 異動 |
|------|------|
| `Data/DataRowExtensions.cs` | namespace 改為 `Bee.Base.Data` |
| `Data/DataRowViewExtensions.cs` | namespace 改為 `Bee.Base.Data` |
| `Data/DataSetExtensions.cs` | namespace 改為 `Bee.Base.Data` |
| `Data/DataSetFunc.cs` | namespace 改為 `Bee.Base.Data` |
| `Data/DataTableComparer.cs` | namespace 改為 `Bee.Base.Data` |
| `Data/DataTableExtensions.cs` | namespace 改為 `Bee.Base.Data` |
| `Data/DataViewExtensions.cs` | namespace 改為 `Bee.Base.Data` |
| `Data/DbTypeConverter.cs` | namespace 改為 `Bee.Base.Data` |

**從 `Common/Common.cs` 拆出（新建檔案）：**

| 型別 | 新檔名 |
|------|--------|
| `FieldDbType` | `Data/FieldDbType.cs` |

---

### 2.6 `Security/`（`Bee.Base.Security`）

| 檔案 | 異動 |
|------|------|
| `Security/AesCbcHmacCryptor.cs` | namespace 改為 `Bee.Base.Security` |
| `Security/AesCbcHmacKeyGenerator.cs` | namespace 改為 `Bee.Base.Security` |
| `Security/FileHashValidator.cs` | namespace 改為 `Bee.Base.Security` |
| `Security/RsaCryptor.cs` | namespace 改為 `Bee.Base.Security` |

**從 `Common/` 移入：**

| 原始路徑 | 異動 |
|---------|------|
| `Common/PasswordHasher.cs` | 移至 `Security/`，namespace 改為 `Bee.Base.Security` |

---

### 2.7 `Serialization/`（`Bee.Base.Serialization`，資料夾從 `Serialize/` 改名）

| 檔案 | 異動 |
|------|------|
| `Serialization/BinarySerializationBinder.cs` | namespace 改為 `Bee.Base.Serialization` |
| `Serialization/JsonSerializationBinder.cs` | namespace 改為 `Bee.Base.Serialization` |
| `Serialization/SerializeFunc.cs` | namespace 改為 `Bee.Base.Serialization` |
| `Serialization/XmlSerializerCache.cs` | namespace 改為 `Bee.Base.Serialization` |

**從 `Common/` 移入：**

| 原始路徑 | 異動 |
|---------|------|
| `Common/GZipFunc.cs` | 移至 `Serialization/`，namespace 改為 `Bee.Base.Serialization` |
| `Common/UTF8StringWriter.cs` | 移至 `Serialization/`，namespace 改為 `Bee.Base.Serialization` |

**從 `Interface/` 移入：**

| 原始路徑 | 異動 |
|---------|------|
| `Interface/IObjectSerialize.cs` | 移至 `Serialization/`，namespace 改為 `Bee.Base.Serialization` |
| `Interface/IObjectSerializeBase.cs` | 移至 `Serialization/`，namespace 改為 `Bee.Base.Serialization` |
| `Interface/IObjectSerializeEmpty.cs` | 移至 `Serialization/`，namespace 改為 `Bee.Base.Serialization` |
| `Interface/IObjectSerializeFile.cs` | 移至 `Serialization/`，namespace 改為 `Bee.Base.Serialization` |
| `Interface/IObjectSerializeProcess.cs` | 移至 `Serialization/`，namespace 改為 `Bee.Base.Serialization` |
| `Interface/ISerializableClone.cs` | 移至 `Serialization/`，namespace 改為 `Bee.Base.Serialization` |

**從 `Common/Common.cs` 拆出（新建檔案）：**

| 型別 | 新檔名 |
|------|--------|
| `SerializeState` | `Serialization/SerializeState.cs` |
| `SerializeFormat` | `Serialization/SerializeFormat.cs` |

**從 `Common/Extensions.cs` 拆出（新建檔案）：**

| 型別 | 新檔名 | 內容 |
|------|--------|------|
| `SerializationExtensions` | `Serialization/SerializationExtensions.cs` | `ToXml`, `ToJson`, `ToBinary`, `ToXmlFile`, `ToJsonFile`, `Save` |

---

### 2.8 `Tracing/`（`Bee.Base.Tracing`）

| 檔案 | 異動 |
|------|------|
| `Tracing/ITraceListener.cs` | namespace 改為 `Bee.Base.Tracing` |
| `Tracing/ITraceWriter.cs` | namespace 改為 `Bee.Base.Tracing` |
| `Tracing/TraceContext.cs` | namespace 改為 `Bee.Base.Tracing` |
| `Tracing/TraceEvent.cs` | namespace 改為 `Bee.Base.Tracing` |
| `Tracing/TraceListener.cs` | namespace 改為 `Bee.Base.Tracing` |
| `Tracing/Tracer.cs` | namespace 改為 `Bee.Base.Tracing` |

**從 `Common/Common.cs` 拆出（新建檔案）：**

| 型別 | 新檔名 |
|------|--------|
| `TraceLayer` | `Tracing/TraceLayer.cs` |
| `TraceEventKind` | `Tracing/TraceEventKind.cs` |
| `TraceStatus` | `Tracing/TraceStatus.cs` |
| `TraceCategories` | `Tracing/TraceCategories.cs` |

---

## 三、移除的資料夾

| 資料夾 | 原因 |
|--------|------|
| `Common/` | 語意不明，內容已分散至根目錄或對應子資料夾 |
| `Exception/` | 僅單一類別，不值得獨立命名空間，`ApiException` 移至根目錄 |
| `Interface/` | 非 .NET 慣例，介面已移至實作所在的子資料夾或根目錄 |

**改名資料夾：**

| 原名 | 新名 | 原因 |
|------|------|------|
| `BackgroundService/` | `BackgroundServices/` | 與命名空間 `Bee.Base.BackgroundServices` 對齊 |
| `Serialize/` | `Serialization/` | 與命名空間 `Bee.Base.Serialization` 對齊 |

---

## 四、影響範圍

### Bee.Base 內部
- 修改命名空間宣告：~39 個 `.cs`
- 新建檔案（從 `Common.cs`、`Extensions.cs` 拆出）：~14 個
- 刪除檔案：`Common/Common.cs`、`Common/Extensions.cs`
- 移除空資料夾：`Common/`、`Exception/`、`Interface/`
- 改名資料夾：`BackgroundService/` → `BackgroundServices/`、`Serialize/` → `Serialization/`

### 下游專案（需補 `using`）
下列專案直接引用 Bee.Base，需依使用的型別補上對應 `using`：

| 專案 | 類型 |
|------|------|
| `src/Bee.Define` | 核心相依 |
| `tests/Bee.Base.UnitTests` | 測試 |
| `tests/Bee.Business.UnitTests` | 測試 |
| `tests/Bee.Define.UnitTests` | 測試 |
| `samples/JsonRpcServerAspNet` | 範例 |

> 間接引用（透過 Bee.Define 等）的專案亦需確認是否直接使用 Bee.Base 型別，
> 目前已知共約 **160 個檔案**含 `using Bee.Base;`。

---

## 五、執行順序

1. 修改 Bee.Base 內部所有命名空間宣告與檔案位置
2. 執行 `dotnet build src/Bee.Base/Bee.Base.csproj` 確認專案本身無錯誤
3. 逐一更新下游專案的 `using` 陳述式
4. 執行 `dotnet build` 全方案建置確認
5. 執行 `dotnet test` 確認測試全數通過
