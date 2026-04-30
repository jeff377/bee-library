# 計畫：將 `MessagePackCodec` 從 `public` 收緊為 `internal`

**狀態：✅ 已完成（2026-04-30）**

## 背景

接續 `plan-rename-messagepack-helper-to-codec.md` 完成後的封裝決策。
重新檢視 `Bee.Api.Core/MessagePack/` 資料夾的存取層級分布,發現
`MessagePackCodec` 是該資料夾內唯一一個「機制類別」卻標 `public` 的 outlier。

## 證據

### 1. 同資料夾存取層級 pattern

| 類別 | 角色 | 存取層級 |
|------|------|---------|
| `SafeMessagePackSerializerOptions` | 序列化機制 | `internal sealed` |
| `FormatterResolver` | 序列化機制 | `internal` |
| `DataSetFormatter` | 序列化機制 | `internal` |
| `DataTableFormatter` | 序列化機制 | `internal` |
| `CollectionBaseFormatter<,>` | 序列化機制 | `internal` |
| `SerializableDataSet` | Wire-format DTO | `public` |
| `SerializableDataTable` | Wire-format DTO | `public` |
| `SerializableDataRow` | Wire-format DTO | `public` |
| `SerializableDataColumn` | Wire-format DTO | `public` |
| `SerializableDataRelation` | Wire-format DTO | `public` |
| **`MessagePackCodec`** | **序列化機制** | **`public` ⚠️** |

`MessagePackCodec` 是「序列化機制」(包裝 options 與預設 formatter 組合,
提供統一的 Serialize/Deserialize 入口),歸類上跟其他 5 個 internal 機制
類別同掛。`public` 是該資料夾內的 outlier。

### 2. 跨專案 src 無引用

只有 `Bee.Api.Core` 內部 1 個 caller(`MessagePackPayloadSerializer`),
完全符合 `internal` 的定義。

### 3. 對外正確抽象已存在

```csharp
public class MessagePackPayloadSerializer : IApiPayloadSerializer
```

這才是用戶該看到的對外介面。`MessagePackCodec` 是它的 implementation
detail。

### 4. 安全管線考量

`security.md` 強調 payload 處理順序「序列化 → 壓縮 → 加密」必須走完整管線。
公開 `MessagePackCodec.Serialize` 等於開後門,允許繞過壓縮/加密。改
`internal` 強制用戶走 `IApiPayloadSerializer` 抽象。

## 範圍

### 唯一改動

`src/Bee.Api.Core/MessagePack/MessagePackCodec.cs:13`:
```diff
- public static class MessagePackCodec
+ internal static class MessagePackCodec
```

### 測試端零修改

`src/Bee.Api.Core/Bee.Api.Core.csproj:16` 早已設定:
```xml
<InternalsVisibleTo Include="Bee.Api.Core.UnitTests" />
```

8 個測試檔約 45 處 `MessagePackCodec.Serialize/Deserialize(...)` 呼叫
**保持原樣不需動**。同 assembly 內已有多個 internal 類別(`SafeTypelessFormatter`、
`DataSetFormatter`、`DataTableFormatter`、`CollectionBaseFormatter`)被
這些測試覆蓋,證明此機制正常運作。

### Public API breaking

框架仍在開發中、無已知外部 NuGet 消費者(同 `MessagePackHelper` 重命名
的判斷)。直接收緊,不留 alias。

## 不在本次範圍

- 同資料夾的 `Serializable*` DTO(5 個 public class)是否該也改 internal —— 
  需逐個檢視外部使用情境,獨立議題,本次不動。

## 執行步驟

1. 修改 `MessagePackCodec.cs` 將 `public` 改 `internal`
2. `dotnet build --configuration Release` —— 預期 0 警告 0 錯誤
3. `./test.sh tests/Bee.Api.Core.UnitTests/Bee.Api.Core.UnitTests.csproj` —— 
   預期 235 個全通過
4. 標記 plan 完成 → commit → push → 監測 CI

## 風險

幾乎零風險。改動是單一關鍵字,IVT 已就位,測試端零修改。
唯一要看的是 `MessagePackCodec` 是否有其他 src 專案引用 —— 已確認無,
只有 `Bee.Api.Core` 自己用。
