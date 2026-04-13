# ADR-002：使用 Newtonsoft.Json 而非 System.Text.Json

## 狀態

已採納

## 背景

.NET 生態系中有兩個主要 JSON 序列化庫：

1. **Newtonsoft.Json**（Json.NET）：第三方，功能最完整
2. **System.Text.Json**（STJ）：微軟官方，內建於 .NET Core 3.0+，效能較高

框架需要選擇一個作為預設 JSON 序列化方案。

## 決策

採用 `Newtonsoft.Json` 作為唯一的 JSON 序列化庫，禁止混用 `System.Text.Json`。

## 理由

- **netstandard2.0 相容性**：核心套件目標為 netstandard2.0，System.Text.Json 在此版本的功能有限，需要額外 NuGet 套件且缺少許多功能。
- **複雜序列化支援**：框架需要自訂型別繫結（`JsonSerializationBinder`）、多型序列化、特殊集合處理等進階功能，Newtonsoft.Json 支援較完整。
- **DataSet 序列化**：Newtonsoft.Json 對 ADO.NET DataSet / DataTable 的序列化支援成熟，STJ 則需要大量自訂 Converter。
- **XML 互通**：框架的定義檔（FormSchema、SystemSettings 等）使用 XML 格式儲存，Newtonsoft.Json 提供 JSON ↔ XML 轉換功能，方便定義檔處理。
- **既有整合深度**：框架從早期版本即採用 Newtonsoft.Json，遷移成本高且風險大。

## 取捨

- **效能較低**：STJ 在純序列化/反序列化效能上通常優於 Newtonsoft.Json。
- **額外相依**：需要引入 NuGet 套件，而非使用框架內建功能。
- **未來維護**：微軟已將重心轉向 STJ，Newtonsoft.Json 更新頻率降低。

## 影響

- 所有 JSON 操作統一使用 `JsonConvert`（Newtonsoft.Json），不使用 `JsonSerializer`（STJ）
- `Bee.Base/Serialization/SerializeFunc.cs` 封裝所有 JSON 序列化操作
- `.claude/rules/code-style.md` 明確規定「不混用 System.Text.Json 與 Newtonsoft.Json」
- API Payload 的高效能序列化由 MessagePack 負責，JSON 僅用於定義檔與配置
