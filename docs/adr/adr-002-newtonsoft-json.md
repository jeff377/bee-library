# ADR-002：JSON 序列化函式庫的選擇與遷移

## 狀態

**已取代（Superseded）** — 原始決策（採用 Newtonsoft.Json）已於 2026-04 遷移完成，現行採用 `System.Text.Json`。本 ADR 保留以記錄選型脈絡與遷移結果。

| 階段 | 函式庫 | 期間 |
|------|--------|------|
| 原始決策 | Newtonsoft.Json | 框架草創期 ~ 2026-04 |
| 現行 | System.Text.Json | 2026-04 起 |

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

## 原始決策（已取代）

採用 `Newtonsoft.Json` 作為唯一的 JSON 序列化庫。

### 當時的理由

- **DataSet 序列化（主要原因）**：框架以 DataSet 作為跨層 DTO（見 [ADR-001](adr-001-dataset-as-dto.md)），Newtonsoft.Json 原生支援 DataSet / DataTable 序列化，而 System.Text.Json 不支援，需要自行撰寫 Converter。
- **netstandard2.0 相容性**：核心套件目標為 netstandard2.0，System.Text.Json 在此版本的功能有限。
- **複雜序列化支援**：框架需要自訂型別繫結（`JsonSerializationBinder`）、多型序列化、特殊集合處理等進階功能。
- **外部介接定位**：JSON 在本框架中的角色是與外部系統介接，而非內部高頻傳輸（內部由 MessagePack 負責）。效能差異對整體影響有限，功能完整性更為重要。

### 取代原因

兩項主要障礙均已解除：

- **netstandard2.0 已棄**：所有專案於 2026-04-14 遷移至 net10.0 單一目標框架（見 [ADR-006](adr-006-dual-target-framework.md)），STJ 在 net10.0 功能完整。
- **DataSet 安全限制可控**：將 DataSet 欄位型別限制為 `FieldDbType` 列舉（13 種固定型別，全部為 STJ 原生支援的基本型別），消除了任意 `System.Type` 的安全風險。

加上 STJ 為框架內建（無第三方相依）、效能較高、官方持續投入，遷移時機成熟。

## 現行決策

採用 `System.Text.Json` 作為唯一的 JSON 序列化函式庫，禁止再引入 `Newtonsoft.Json`。

封裝點：`Bee.Base/Serialization/JsonCodec.cs`，預設 camelCase 屬性命名、indented 輸出，並掛載框架自訂 Converter：

| Converter | 用途 |
|-----------|------|
| `DataSetJsonConverter` | 還原 DataSet 結構（Tables、Relations、PrimaryKey、Rows 含 RowState） |
| `DataTableJsonConverter` | 還原 DataTable 結構（Columns 含 `FieldDbType` 對應、Rows 含 RowState） |
| `JsonStringEnumConverter` | 列舉以字串形式輸出，提升外部介接可讀性 |

## 遷移結果摘要

### Phase 1：自訂 Converter

| 項目 | 落點 |
|------|------|
| `DataSetJsonConverter` / `DataTableJsonConverter` | `src/Bee.Base/Serialization/`，採 `FieldDbType` → .NET 型別固定對應，避免序列化任意 `System.Type` |
| `ApiPayload` 型別資訊 | 改以既有 `ApiPayload.TypeName` 屬性傳遞，不再依賴 Newtonsoft.Json 的 `$type` 自動注入；白名單驗證沿用 `SysInfo.IsTypeNameAllowed()` |
| `FilterNodeCollection` Converter | 由 `JArray` / `JObject` 改寫為 `JsonDocument` / `JsonElement` |

### Phase 2：Attribute 與核心模組

| 項目 | 結果 |
|------|------|
| `[JsonIgnore]` | 命名空間由 `Newtonsoft.Json` → `System.Text.Json.Serialization` |
| `[JsonProperty("x")]` | 改為 `[JsonPropertyName("x")]` |
| `NullValueHandling.Include`（JsonRpcRequest/Response） | 改用 `[JsonIgnore(Condition = JsonIgnoreCondition.Never)]` 或全域 `DefaultIgnoreCondition` |
| `DefaultValueHandling.Include`（FormField、DbField） | 改用 `[JsonIgnore(Condition = JsonIgnoreCondition.Never)]` |
| `SerializeFunc.cs` | 重構為 `JsonCodec.cs`，內部以 `JsonSerializer` + `JsonSerializerOptions` 實作 |

### Phase 3：清理

| 項目 | 結果 |
|------|------|
| `JsonSerializationBinder.cs` | 已移除（跨 runtime 名稱對應 `mscorlib` ↔ `System.Private.CoreLib` 在 net10.0 單一目標下不再需要） |
| `Newtonsoft.Json` NuGet 相依 | 已自所有 `*.csproj` 移除 |
| `code-style.md` 序列化規範 | 已更新為「JSON 序列化使用 System.Text.Json」 |

## 影響

- 所有 JSON 操作經由 `JsonCodec` 進行；不再直接呼叫 `JsonSerializer`，以維持框架預設選項一致
- 三種序列化各司其職維持不變：XML 存定義、MessagePack 傳內部 Payload、JSON 接外部系統
- 三個前端 repo 均為新建，無既有客戶端的 JSON 格式相容問題；遷移過程未保留向後相容層
- 移除 `JsonSerializationBinder` 後，跨 runtime 型別名稱差異透過框架單一 net10.0 目標自然消解
