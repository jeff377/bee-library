# ADR-002：使用 Newtonsoft.Json 而非 System.Text.Json

## 狀態

已採納 — 維持使用中，STJ 遷移前置條件已滿足（2026-04-14 完成 net10.0 單一目標遷移）

## 背景

框架使用三種序列化格式，各有明確用途：

| 格式 | 用途 | 場景 |
|------|------|------|
| **XML** | 儲存與表示定義資料 | FormSchema、SystemSettings 等複雜型別的持久化與傳輸表示（定義資料傳輸時先序列化為 XML 字串，再經由 MessagePack 或 JSON 傳遞） |
| **MessagePack** | 內部系統前後端傳遞 | API Payload（見 [ADR-004](adr-004-messagepack-payload.md)） |
| **JSON** | 外部系統介接 | 第三方系統整合、JSON-RPC 信封、配置檔 |

在 JSON 序列化的選擇上，.NET 生態系中有兩個主要方案：

1. **Newtonsoft.Json**（Json.NET）：第三方，功能最完整
2. **System.Text.Json**（STJ）：微軟官方，內建於 .NET Core 3.0+，效能較高

## 決策

採用 `Newtonsoft.Json` 作為唯一的 JSON 序列化庫，禁止混用 `System.Text.Json`。

## 理由

- **DataSet 序列化（主要原因）**：框架以 DataSet 作為跨層 DTO（見 [ADR-001](adr-001-dataset-as-dto.md)），Newtonsoft.Json 原生支援 DataSet / DataTable 序列化，而 System.Text.Json 不支援，需要自行撰寫 Converter。這是當初選擇 Newtonsoft.Json 的最主要原因。
- **外部介接定位**：JSON 在本框架中的角色是與外部系統介接，而非內部高頻傳輸（內部由 MessagePack 負責）。因此 JSON 的效能差異對整體影響有限，功能完整性更為重要。
- ~~**netstandard2.0 相容性**：核心套件目標為 netstandard2.0，System.Text.Json 在此版本的功能有限，需要額外 NuGet 套件且缺少許多功能。~~（已不適用 — 所有專案已遷移至 net10.0，見 [ADR-006](adr-006-dual-target-framework.md)）
- **複雜序列化支援**：框架需要自訂型別繫結（`JsonSerializationBinder`）、多型序列化、特殊集合處理等進階功能，Newtonsoft.Json 支援較完整。
- **XML 互通**：定義檔使用 XML 格式儲存，Newtonsoft.Json 提供 JSON ↔ XML 轉換功能，方便定義檔處理。

## 取捨

- **效能較低**：STJ 在純序列化/反序列化效能上通常優於 Newtonsoft.Json。
- **額外相依**：需要引入 NuGet 套件，而非使用框架內建功能。
- **未來維護**：微軟已將重心轉向 STJ，Newtonsoft.Json 更新頻率降低。
- ~~**netstandard2.0 限制**~~：已不再是取捨因素 — 所有專案已遷移至 net10.0，STJ 功能完整可用。

## 未來方向：System.Text.Json 遷移路線圖

### 前置條件

~~STJ 遷移的唯一合理路徑是**先完成 net10.0+ 遷移**（見 [ADR-006](adr-006-dual-target-framework.md)）。~~ ✅ **已滿足**（2026-04-14 完成，見 [ADR-006](adr-006-dual-target-framework.md)）。所有專案已遷移至 net10.0 單一目標框架，不再有 netstandard2.0 相容需求。

### 遷移障礙分析（依嚴重程度排序）

#### 1. DataSet/DataTable Converter（可控 — 已確認型別範圍）

STJ 不支援 DataSet/DataTable 序列化，主因是任意 `System.Type` 的安全風險。但框架可將 DataSet 欄位型別限制為 `FieldDbType` 列舉（排除 `Unknown`），共 13 種固定型別，全部為 STJ 原生支援的基本型別：

| FieldDbType | .NET 型別 | STJ 支援 |
|-------------|----------|---------|
| String, Text | `string` | 原生 |
| Boolean | `bool` | 原生 |
| AutoIncrement, Short, Integer, Long | `short`/`int`/`long` | 原生 |
| Decimal, Currency | `decimal` | 原生 |
| Date, DateTime | `DateTime` | 原生 |
| Guid | `Guid` | 原生 |
| Binary | `byte[]` | 原生（Base64） |

**此限制解決了核心問題**：不需序列化 `System.Type`，型別還原透過固定的 `FieldDbType` → .NET 型別 `switch` 對應，安全且可控。

實作要點：
- 自行實作 `JsonConverter<DataSet>` + `JsonConverter<DataTable>`
- 需處理 Tables、Relations、Columns、Rows（含 RowState）、PrimaryKey
- 可參考 MessagePack 的 `DataSetFormatter` + `SerializableDataSet` 中間 DTO 模式（約 350 行）
- 三個前端 repo 皆為新建，無既有客戶端的格式相容問題

#### 2. TypeNameHandling + ISerializationBinder（非阻斷 — 已調查）

所有 JSON 序列化呼叫均使用 `includeTypeName = true`（預設值），無任何呼叫點明確設為 false。但調查後確認此障礙**可替代**：

- **實際需要 `$type` 的場景僅限 Plain 格式的 `ApiPayload.Value`**（宣告型別為 `object`，執行期可能是 DataSet、PingResult 等）。Encoded/Encrypted 格式已透過 `ApiPayload.TypeName` 屬性獨立儲存型別資訊，不依賴 `$type`
- **替代方案**：為 `ApiPayload` 撰寫自訂 `JsonConverter`，利用已有的 `TypeName` 屬性做型別判定，取代 Newtonsoft.Json 的 `$type` 自動注入。白名單驗證（`SysInfo.IsTypeNameAllowed()`）可在 Converter 內呼叫
- **跨 Runtime 名稱對應**（`mscorlib` ↔ `System.Private.CoreLib`）~~在放棄 netstandard2.0 後不再需要~~ 已不再需要（net10.0 單一目標），`JsonSerializationBinder` 可移除

#### 3. 確定可完成的工作

| 項目 | 說明 |
|------|------|
| Attribute 替換（25 檔，29 處） | `[JsonIgnore]`（12 處）→ STJ `[JsonIgnore]`、`[JsonProperty("x")]`（17 處）→ `[JsonPropertyName("x")]` |
| SerializeFunc.cs 改寫 | `JsonConvert` → `JsonSerializer`，`JsonSerializerSettings` → `JsonSerializerOptions` |
| JsonSerializationBinder.cs 移除 | 放棄 netstandard2.0 後跨 runtime 名稱對應不再需要，白名單驗證移至 ApiPayload Converter |
| FilterNodeCollectionJsonConverter 改寫 | `JArray`/`JObject` → `JsonDocument`/`JsonElement`，或改用 `JsonDerivedTypeAttribute` |
| NullValueHandling 處理（6 處） | JsonRpcRequest/Response 的 `NullValueHandling.Include` 改用全域 `DefaultIgnoreCondition` |
| DefaultValueHandling 處理（2 處） | FormField、DbField 的 `DefaultValueHandling.Include` 改用 `[JsonIgnore(Condition = JsonIgnoreCondition.Never)]` |

### 遷移可行性結論

所有障礙均已確認可克服，且前置條件已全部滿足：

- **DataSet Converter**：限制欄位型別為 `FieldDbType`（13 種固定型別），消除任意型別安全風險，可控工作量
- **TypeNameHandling**：利用既有 `ApiPayload.TypeName` 屬性替代 `$type` 自動注入
- ~~**前置條件**：三個前端均為 net10.0+，可放棄 netstandard2.0（見 [ADR-006](adr-006-dual-target-framework.md)）~~ ✅ 已完成（2026-04-14）

**結論：STJ 遷移可行且前置條件已滿足。**

### 遷移路線圖

#### Phase 1：實作 STJ 自訂 Converter

| 項目 | 說明 |
|------|------|
| DataSet/DataTable Converter | 實作 `JsonConverter<DataSet>` + `JsonConverter<DataTable>`，參考 MessagePack 的 `DataSetFormatter` + `SerializableDataSet` 中間 DTO 模式 |
| ApiPayload Converter | 利用 `TypeName` 屬性替代 `$type` 自動注入，白名單驗證移至 Converter 內 |
| FilterNodeCollectionJsonConverter | `JArray`/`JObject` → `JsonDocument`/`JsonElement`，或改用 `JsonDerivedTypeAttribute` |

#### Phase 2：替換 Attribute 與核心模組

| 項目 | 數量 | 說明 |
|------|------|------|
| `[JsonIgnore]` | 12 處 | 命名空間由 `Newtonsoft.Json` → `System.Text.Json.Serialization` |
| `[JsonProperty("x")]` | 17 處 | → `[JsonPropertyName("x")]` |
| `NullValueHandling.Include` | 6 處 | JsonRpcRequest/Response 改用 `[JsonIgnore(Condition = JsonIgnoreCondition.Never)]` 或全域 `DefaultIgnoreCondition` |
| `DefaultValueHandling.Include` | 2 處 | FormField、DbField 改用 `[JsonIgnore(Condition = JsonIgnoreCondition.Never)]` |
| SerializeFunc.cs 改寫 | 1 檔 | `JsonConvert` → `JsonSerializer`，`JsonSerializerSettings` → `JsonSerializerOptions` |

#### Phase 3：清理

| 項目 | 說明 |
|------|------|
| 移除 `JsonSerializationBinder.cs` | 跨 runtime 名稱對應（`mscorlib` ↔ `System.Private.CoreLib`）已不需要 |
| 移除 Newtonsoft.Json NuGet 相依 | `Bee.Base.csproj` 中的 `PackageReference` |
| 更新 `code-style.md` | 序列化規範改為 System.Text.Json |
| 更新本 ADR | 狀態改為「已取代」，記錄最終遷移結果 |

## 影響

### 現行影響

- 所有 JSON 操作統一使用 `JsonConvert`（Newtonsoft.Json），不使用 `JsonSerializer`（STJ）
- `Bee.Base/Serialization/SerializeFunc.cs` 封裝所有 JSON 序列化操作
- `.claude/rules/code-style.md` 明確規定「不混用 System.Text.Json 與 Newtonsoft.Json」
- 三種序列化各司其職：XML 存定義、MessagePack 傳內部 Payload、JSON 接外部系統

### 遷移後預期影響

- `SerializeFunc.cs` 改用 `System.Text.Json.JsonSerializer`，API 簽章不變
- 所有 `[JsonProperty]` / `[JsonIgnore]` 改用 `System.Text.Json.Serialization` 命名空間
- 移除 `Newtonsoft.Json` NuGet 套件相依，改用框架內建功能
- 三個前端 repo 均為新建，無既有客戶端的 JSON 格式相容問題
