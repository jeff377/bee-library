# ADR-002：使用 Newtonsoft.Json 而非 System.Text.Json

## 狀態

已採納

## 背景

框架使用三種序列化格式，各有明確用途：

| 格式 | 用途 | 場景 |
|------|------|------|
| **XML** | 儲存定義資料 | FormSchema、SystemSettings 等複雜型別的持久化 |
| **MessagePack** | 內部系統前後端傳遞 | API Payload（見 [ADR-004](adr-004-messagepack-payload.md)） |
| **JSON** | 外部系統介接 | 第三方系統整合、JSON-RPC 信封、配置檔 |

在 JSON 序列化的選擇上，.NET 生態系中有兩個主要方案：

1. **Newtonsoft.Json**（Json.NET）：第三方，功能最完整
2. **System.Text.Json**（STJ）：微軟官方，內建於 .NET Core 3.0+，效能較高

## 決策

採用 `Newtonsoft.Json` 作為唯一的 JSON 序列化庫，禁止混用 `System.Text.Json`。

## 理由

- **外部介接定位**：JSON 在本框架中的角色是與外部系統介接，而非內部高頻傳輸（內部由 MessagePack 負責）。因此 JSON 的效能差異對整體影響有限，功能完整性更為重要。
- **netstandard2.0 相容性**：核心套件目標為 netstandard2.0，System.Text.Json 在此版本的功能有限，需要額外 NuGet 套件且缺少許多功能。
- **複雜序列化支援**：框架需要自訂型別繫結（`JsonSerializationBinder`）、多型序列化、特殊集合處理等進階功能，Newtonsoft.Json 支援較完整。
- **DataSet 序列化**：Newtonsoft.Json 對 ADO.NET DataSet / DataTable 的序列化支援成熟，STJ 則需要大量自訂 Converter。
- **XML 互通**：定義檔使用 XML 格式儲存，Newtonsoft.Json 提供 JSON ↔ XML 轉換功能，方便定義檔處理。
- **既有整合深度**：框架從早期版本即採用 Newtonsoft.Json，遷移成本高且風險大。

## 取捨

- **效能較低**：STJ 在純序列化/反序列化效能上通常優於 Newtonsoft.Json。
- **額外相依**：需要引入 NuGet 套件，而非使用框架內建功能。
- **未來維護**：微軟已將重心轉向 STJ，Newtonsoft.Json 更新頻率降低。

## 影響

- 所有 JSON 操作統一使用 `JsonConvert`（Newtonsoft.Json），不使用 `JsonSerializer`（STJ）
- `Bee.Base/Serialization/SerializeFunc.cs` 封裝所有 JSON 序列化操作
- `.claude/rules/code-style.md` 明確規定「不混用 System.Text.Json 與 Newtonsoft.Json」
- 三種序列化各司其職：XML 存定義、MessagePack 傳內部 Payload、JSON 接外部系統
