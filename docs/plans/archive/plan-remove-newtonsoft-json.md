# 計畫：移除 Newtonsoft.Json，遷移至 System.Text.Json

## 背景

依據 [ADR-002](../adr/adr-002-newtonsoft-json.md)，所有遷移前置條件已滿足（net10.0 單一目標框架）。Newtonsoft.Json 目前僅由 `Bee.Base.csproj` 直接參考，但影響範圍透過 transitive dependency 擴及整個框架。

## 影響範圍總覽

| 分類 | 檔案數 | 複雜度 |
|------|--------|--------|
| 自訂 Converter 改寫 | 3 檔 | 高 |
| 核心序列化模組改寫 | 1 檔 | 中 |
| Attribute 替換 | ~25 檔 | 低 |
| 移除 / 清理 | 3 檔 | 低 |
| 測試更新 | 2 檔 | 低 |

---

## Phase 1：實作 STJ 自訂 Converter（高複雜度）

### 1-1. DataTableJsonConverter 改寫

- **檔案**：`src/Bee.Base/Serialization/DataTableJsonConverter.cs`
- **工作**：以 `System.Text.Json.Serialization.JsonConverter<DataTable>` 為基底類別重寫
  - `Write`：`JsonWriter` → `Utf8JsonWriter`
  - `Read`：`JsonReader` + `JsonToken` → `Utf8JsonReader` + `JsonTokenType`
  - `JsonSerializationException` → `JsonException`
- **要點**：
  - 欄位型別限制為 `FieldDbType`（13 種固定型別），型別還原透過 `switch` 對應
  - 需處理 columns metadata（name, type, allowNull, readOnly, maxLength, caption, defaultValue）
  - 需處理 rows 的 `RowState`（Added/Modified/Deleted/Unchanged）與 original vs current values
  - 需處理 `DBNull` 值

### 1-2. DataSetJsonConverter 改寫

- **檔案**：`src/Bee.Base/Serialization/DataSetJsonConverter.cs`
- **工作**：同 1-1 模式重寫
  - 序列化：dataset name、tables（委派 DataTableJsonConverter）、relations
  - `JsonSerializationException` → `JsonException`
- **要點**：DataSetJsonConverter 內部委派 DataTableJsonConverter，兩者需同步改寫

### 1-3. FilterNodeCollectionJsonConverter 改寫

- **檔案**：`src/Bee.Definition/Filters/FilterNodeCollectionJsonConverter.cs`
- **工作**：
  - `JArray` / `JObject` / `JToken` → `JsonDocument` / `JsonElement`
  - 多型反序列化：依 `"kind"` 屬性判斷 `FilterCondition` 或 `FilterGroup`
  - `JsonSerializationException` → `JsonException`
- **替代方案**：評估是否可改用 `[JsonDerivedType]` 搭配 type discriminator

### 1-4. ApiPayload Converter（新增）

- **位置**：`src/Bee.Api.Core/JsonRpc/` 新增 `ApiPayloadJsonConverter.cs`
- **工作**：
  - 利用既有 `ApiPayload.TypeName` 屬性替代 Newtonsoft 的 `$type` 自動注入
  - 白名單驗證（`SysInfo.IsTypeNameAllowed()`）移至 Converter 內
  - 僅 Plain 格式需要此機制，Encoded/Encrypted 格式已透過 `TypeName` 屬性獨立儲存

---

## Phase 2：核心序列化模組改寫（中複雜度）

### 2-1. SerializeFunc.cs 改寫

- **檔案**：`src/Bee.Base/Serialization/SerializeFunc.cs`
- **工作**：
  - `JsonConvert.SerializeObject()` → `JsonSerializer.Serialize()`
  - `JsonConvert.DeserializeObject()` → `JsonSerializer.Deserialize()`
  - `JsonSerializerSettings` → `JsonSerializerOptions`
  - 設定對應：
    | Newtonsoft | System.Text.Json |
    |------------|-----------------|
    | `DefaultValueHandling.Ignore` | `DefaultIgnoreCondition = WhenWritingDefault` |
    | `NullValueHandling.Ignore` | `DefaultIgnoreCondition = WhenWritingNull` |
    | `TypeNameHandling.Auto` | 由 ApiPayload Converter 處理 |
    | `Formatting.Indented` | `WriteIndented = true` |
    | `CamelCasePropertyNamesContractResolver` | `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` |
    | `StringEnumConverter` | `JsonStringEnumConverter` |
  - 移除 `JsonSerializationBinder` 相關邏輯
  - 註冊自訂 Converter（DataSet、DataTable、FilterNodeCollection、ApiPayload）

---

## Phase 3：Attribute 替換（低複雜度，~25 檔）

### 3-1. `[JsonIgnore]` 命名空間替換

將 `using Newtonsoft.Json;` → `using System.Text.Json.Serialization;`，屬性名稱相同無需修改。

**涉及檔案**（Bee.Base）：
- `Collections/CollectionBase.cs`
- `Collections/CollectionItem.cs`
- `Collections/KeyCollectionBase.cs`
- `Collections/KeyCollectionItem.cs`

**涉及檔案**（Bee.Definition）：
- `Collections/MessagePackCollectionBase.cs`
- `Collections/MessagePackCollectionItem.cs`
- `Collections/MessagePackKeyCollectionBase.cs`
- `Collections/MessagePackKeyCollectionItem.cs`
- `Settings/SystemSettings/SystemSettings.cs`
- `Settings/ProgramSettings/ProgramSettings.cs`
- `Settings/MenuSettings/MenuSettings.cs`
- `Settings/DatabaseSettings/DatabaseSettings.cs`
- `Settings/ClientSettings/ClientSettings.cs`
- `Settings/DbSchemaSettings/DbSchemaSettings.cs`
- `Forms/FormLayout.cs` (Layouts)
- `Filters/FilterNode.cs`

### 3-2. `[JsonProperty("x")]` → `[JsonPropertyName("x")]`

**涉及檔案**（Bee.Api.Core）：
- `JsonRpc/JsonRpcRequest.cs` — 4 處 `[JsonProperty]` + 1 處 `[JsonIgnore]`
- `JsonRpc/JsonRpcResponse.cs` — 5 處 `[JsonProperty]` + 1 處 `[JsonIgnore]`
- `JsonRpc/JsonRpcError.cs` — 3 處 `[JsonProperty]`
- `JsonRpc/ApiPayload.cs` — 3 處 `[JsonProperty]` + 1 處 `[JsonIgnore]`
- `ApiRequest.cs` — `[JsonIgnore]`
- `ApiResponse.cs` — `[JsonIgnore]`

**涉及檔案**（Bee.Definition）：
- `Forms/FormSchema.cs` — `[JsonIgnore]`
- `Forms/FormField.cs` — `[JsonIgnore]` + `[JsonProperty]`
- `Database/TableSchema.cs` — `[JsonIgnore]`
- `Database/DbField.cs` — `[JsonProperty(DefaultValueHandling = ...)]`
- `Filters/FilterGroup.cs` — `[JsonConverter(...)]`

### 3-3. 特殊 Attribute 處理

| 原始 | 替換為 | 位置 |
|------|--------|------|
| `[JsonProperty(NullValueHandling = NullValueHandling.Include)]` | `[JsonIgnore(Condition = JsonIgnoreCondition.Never)]` 或全域設定 | JsonRpcRequest/Response（6 處）|
| `[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]` | `[JsonIgnore(Condition = JsonIgnoreCondition.Never)]` | DbField、FormField（2 處）|

---

## Phase 4：清理

### 4-1. 移除檔案

- `src/Bee.Base/Serialization/JsonSerializationBinder.cs` — 跨 runtime 名稱對應已不需要

### 4-2. 移除 NuGet 參考

- `src/Bee.Base/Bee.Base.csproj` — 移除 `<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />`

### 4-3. 更新文件

- `.claude/rules/code-style.md` — 序列化規範改為 System.Text.Json
- `docs/adr/adr-002-newtonsoft-json.md` — 狀態改為「已取代」，記錄遷移結果

### 4-4. 更新測試

- `tests/Bee.Api.Core.UnitTests/` — `JsonRpcSerializationTests.cs` 中的 `JsonConvert` / `JObject` 改用 `JsonSerializer` / `JsonDocument`
- `tests/Bee.Base.UnitTests/` — `JsonDataSetSerializationTests.cs` 應透過 `SerializeFunc` 呼叫，預期無需大幅修改
- 全部測試通過後確認無回歸

---

## 執行順序與相依性

```
Phase 1（Converter）→ Phase 2（SerializeFunc）→ Phase 3（Attribute）→ Phase 4（清理）
```

- Phase 1 與 Phase 3 可部分並行，但 Converter 必須先完成才能移除 Newtonsoft
- Phase 2 依賴 Phase 1 的 Converter 完成
- Phase 4 必須在所有程式碼遷移完成後執行
- 每個 Phase 完成後應執行 `dotnet build` 與 `dotnet test` 確認無回歸

## 風險與注意事項

1. **JSON 格式相容性**：DataSet/DataTable 的 JSON 輸出格式可能與 Newtonsoft 版本略有差異（例如 `DateTime` 格式），需確認三個前端 repo 無相容問題（ADR-002 已註明前端均為新建，無既有格式相容問題）
2. **STJ 行為差異**：STJ 預設區分大小寫、不支援 trailing comma 等，需在 `JsonSerializerOptions` 中明確設定
3. **無公開 API 破壞**：Attribute 變更為 metadata，不影響消費者的型別契約
