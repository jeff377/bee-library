# ADR-004：使用 MessagePack 作為 API Payload 序列化格式

## 狀態

已採納

## 背景

框架使用三種序列化格式，各有明確用途：

| 格式 | 用途 | 場景 |
|------|------|------|
| **XML** | 儲存與表示定義資料 | FormSchema、SystemSettings 等複雜型別的持久化與傳輸表示（定義資料傳輸時先序列化為 XML 字串，再經由 MessagePack 或 JSON 傳遞） |
| **MessagePack** | 內部系統前後端傳遞 | API Payload |
| **JSON** | 外部系統介接 | 第三方系統整合、JSON-RPC 信封（見 [ADR-002](adr-002-newtonsoft-json.md)） |

針對內部系統前後端之間的 API Payload 傳輸，需要選擇一個高效的序列化格式。選項包括：

1. **JSON**（Newtonsoft.Json）：文字格式，可讀性高，但體積大、速度慢
2. **MessagePack**：二進位格式，體積小、速度快
3. **Protobuf**：二進位格式，需要 .proto 定義檔

## 決策

採用 `MessagePack` 作為內部系統前後端 API Payload 的序列化格式。JSON-RPC 信封使用 JSON（符合 JSON-RPC 2.0 規範），Payload 欄位使用 MessagePack 編碼後以 Base64 嵌入。

## 理由

- **效能**：MessagePack 的序列化/反序列化速度顯著優於 JSON，適合高頻 API 呼叫場景。
- **體積**：二進位格式的 Payload 體積通常為 JSON 的 50-70%，搭配 GZip 壓縮後更小。
- **與加密管線整合**：Payload 經過 Serialize → Compress → Encrypt 三階段管線處理，二進位格式天然適合後續的壓縮與加密操作。
- **Schema Evolution**：MessagePack 的 `[Key]` 屬性支援欄位新增/移除，不會破壞向後相容性。
- **無需 .proto 檔案**：相較於 Protobuf，MessagePack 使用 C# Attribute 定義 Schema，不需要額外的定義檔與程式碼產生步驟。

## 取捨

- **可讀性差**：二進位格式無法直接閱讀，除錯時需要工具解碼。
- **學習成本**：開發者需要了解 `[MessagePackObject]`、`[Key]` 等屬性的用法。
- **型別白名單**：為防止反序列化攻擊，框架實作了 `SafeTypelessFormatter` 和 `SafeMessagePackSerializerOptions`，新增 API 型別時必須同步註冊。

## 影響

- `Bee.Api.Core/Transformer/MessagePackPayloadSerializer.cs`：預設 Payload 序列化器
- `Bee.Api.Core/MessagePack/`：自訂 Formatter（DataSet、DataTable 等 ADO.NET 型別）
- `Bee.Api.Core/MessagePack/SafeMessagePackSerializerOptions.cs`：型別白名單機制
- `Bee.Api.Core/ApiContractRegistry.cs`：API 型別註冊
- `Bee.Definition` 的集合型別（FilterCondition、PackageUpdateQuery 等）也使用 MessagePack 序列化
- API Payload 格式分三級：Plain（無編碼）、Encoded（MessagePack + GZip）、Encrypted（MessagePack + GZip + AES）
