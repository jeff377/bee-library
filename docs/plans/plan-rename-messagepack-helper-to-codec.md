# 計畫：將 `MessagePackHelper` 重新命名為 `MessagePackCodec`

**狀態：✅ 已完成（2026-04-30）**

## 背景

`Bee.Api.Core/MessagePack/MessagePackHelper.cs` 是 Bee 框架專屬的 MessagePack
序列化包裝層,提供 `Serialize<T>`、`Serialize(object, Type)`、`Deserialize<T>`、
`Deserialize(byte[], Type)` 4 個方法,並預載一套客製 formatters/resolvers
(DataTable、DataSet、CollectionBase、SafeTypeless 等)。

`Helper` 後綴跟 `*SchemaHelper` 同樣是 weak naming —— 沒有提供任何資訊,讀者
必須翻檔才知道在做什麼。

`Codec`(Encoder + Decoder)是業界通用詞彙,精準對應 4 個方法的本質
(編碼/解碼),比 `Helper` 進步一個抽象層次。

## 為什麼不選其他候選

- `SafeMessagePackSerializer` —— `Safe` 是 `SafeMessagePackSerializerOptions`
  的 implementation detail(type-blocking),不是這個類別對外的定位
- `BeeMessagePackSerializer` —— `Bee.*` namespace 內加 `Bee` 前綴冗餘
- `MessagePackSerializerEx` —— `Ex` 後綴是過時風格

## 範圍

### 重新命名

| 目前 | 新名稱 |
|------|--------|
| `src/Bee.Api.Core/MessagePack/MessagePackHelper.cs` | `MessagePackCodec.cs` |
| `public static class MessagePackHelper` | `public static class MessagePackCodec` |

### 連動更新

#### Source 引用(1 處)

| 檔案 | 呼叫點數 |
|------|---------|
| `src/Bee.Api.Core/Transformers/MessagePackPayloadSerializer.cs` | 2 |

#### 測試引用(8 個檔,約 45 處)

| 檔案 | 呼叫點數 |
|------|---------|
| `tests/Bee.Api.Core.UnitTests/TestFunc.cs` | 2 |
| `tests/Bee.Api.Core.UnitTests/MessagePackContractsTests.cs` | 8 |
| `tests/Bee.Api.Core.UnitTests/MessagePackTests.cs` | ~15 |
| `tests/Bee.Api.Core.UnitTests/SafeTypelessFormatterTests.cs` | 4 |
| `tests/Bee.Api.Core.UnitTests/MessagePackNullFormatterTests.cs` | 8 |
| `tests/Bee.Api.Core.UnitTests/ApiRequestResponseTests.cs` | 6 |
| `tests/Bee.Api.Core.UnitTests/SerializableDataSetTests.cs` | 2 |

中文註解中的「使用 MessagePackHelper」也會被 sed 一併替換為「使用
MessagePackCodec」,對應後仍語意正確。

## 不保留舊名

框架仍在開發中,當前無已知外部 NuGet 消費者。直接 rename,**不留
`Obsolete` alias**,保持 codebase 乾淨。

## 不在本次範圍

- `public` → `internal` —— 雖然只有 1 個 src 內部 caller,但測試端用 8 個檔,
  改 internal 需要 `InternalsVisibleTo` 設定。這是獨立的封裝決策,留作另一個
  plan(若有需要)。
- 資料夾 `MessagePack/` 命名 —— 維持不動。資料夾以「技術名稱」分類是合理的
  慣例(類似 `Bee.Db/Providers/SqlServer/`)。

## 執行步驟

1. **重命名類別檔**
   - `git mv src/Bee.Api.Core/MessagePack/MessagePackHelper.cs
     src/Bee.Api.Core/MessagePack/MessagePackCodec.cs`
   - 更新類別名與 XML doc 中的引用

2. **批次替換引用**
   - `find src/Bee.Api.Core tests/Bee.Api.Core.UnitTests -type f -name "*.cs"
     -exec sed -i '' 's/MessagePackHelper/MessagePackCodec/g' {} +`
   - 字串 `MessagePackHelper` 唯一,不會誤中其他識別字

3. **驗證**
   - `dotnet build --configuration Release src/Bee.Api.Core/Bee.Api.Core.csproj`
   - `./test.sh tests/Bee.Api.Core.UnitTests/Bee.Api.Core.UnitTests.csproj`
   - 預期全部通過(這條路徑沒有 DB 相依)

4. **Plan 標記完成 → commit → push → 監測 CI**

## 風險與注意事項

- **無 DB 相依** —— Bee.Api.Core 測試純邏輯,本機可完整驗證,不像
  `*SchemaHelper` 那次需要 docker container
- **無 alias 過渡** —— 直接 rename,內部 caller 必須一次清乾淨,sed 全域
  替換可達成
- **Public API breaking** —— 已確認框架仍在開發中、無外部消費者,可接受
