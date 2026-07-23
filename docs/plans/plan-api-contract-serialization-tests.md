# 計畫：API 合約完整 MessagePack + JSON 序列化單元測試

**狀態：✅ 已完成（2026-07-22，核心 Stage 1；Stage 2 為可選延伸）**

> **採用做法**：對齊 sibling repo `soarcloud-libraries` 的
> `tests/SoarCloud.Api.Core.Tests/Transformers/ApiContractSerializationTests.cs` ——
> **反射掃整個組件的合約型別 + 反射 Populate 樣本 + 保真(re-serialize 相等)比對**,
> 單一 `[Theory]` 自動涵蓋現有與未來所有合約。取代原先「手動 registry + 完整性守衛」的重做法。

## 背景

API 合約型別(wire 上的 Request/Response 與其攜帶的 DTO)同時要走兩條 wire：

- **MessagePack**(`MessagePackCodec` → `ContractlessStandardResolver`,行動端/桌面 authenticated 呼叫的 body)
- **JSON**(`JsonCodec`,System.Text.Json,JS/WASM 友善端 + JSON-RPC envelope)

目前 `tests/Bee.Api.Core.UnitTests/` 已有兩套風格的測試,但**覆蓋零散、不完整、且無法在新增合約時自動接住**：

| 既有測試 | 性質 | 問題 |
|---|---|---|
| `*MessagePackTests`、`MessagePackContractsTests` | 純 `MessagePackCodec` round-trip + 逐欄 assert | 只覆蓋部分型別(Form 全套、System 少數)；JSON 面幾乎沒有對稱測試 |
| `*JsonRpcRoundTripTests` | 走 `JsonRpcExecutor` 的 end-to-end 整合 | 重量級(需 fixture/stub/DI)、驗的是 wire→BO arg 對拷,**不是純序列化**；且非每個合約都有 |

**核心目標**:對**每一個** API 合約型別,建立 MessagePack 與 JSON 兩個方向的純序列化 round-trip 測試,使**任何合約異動(改欄位、加型別、改 [Key]/屬性名)若破壞 wire 傳遞,測試會立即紅**。並以「完整性守衛」保證新增合約型別時不會漏測。

## 範圍：哪些型別算「API 合約」

**納入**：

1. `src/Bee.Api.Core/Messages/**` 下所有具體 Request/Response 型別(`ApiMessageBase` 子類)—— 約 60 個，含 Form / System / AuditLog / ExecFunc。
2. `src/Bee.Api.Contracts/` 下可序列化 DTO：`PackageUpdateInfo`、`PackageUpdateQuery`、`RecordFieldChange`、`PackageDelivery`(enum)。這些是合約載荷。
3. 合約骨架 `ApiRequest` / `ApiResponse` / `ApiErrorInfo` / `ApiHeaders` / `ApiCallContext`(envelope 層)。

**不納入**(本計畫外)：

- `Bee.Api.Contracts` 的 `I*` 介面 —— 只是合約宣告,不參與序列化。
- BO 端 `*Args` / `*Result` —— 屬 business 層 DTO,由 `ApiInputConverter`/`ApiOutputConverter` 與 wire 型別對拷;其對拷正確性已由既有 `*JsonRpcRoundTripTests` 覆蓋。**若需一併納入,是本計畫的可選延伸(待確認)**。
- `Bee.Definition` 的定義型別(FormSchema 等)—— 已有 `Bee.Definition.UnitTests` 的序列化測試。

## 設計決策

### D1. 反射掃組件 + 反射 Populate + `[Theory]`(核心,對齊 SoarCloud)

單一 `[Theory]`,`MemberData` 產出 (serializer, type) 交叉組合：

- **型別來源**：`typeof(ApiRequest).Assembly.GetTypes()` 過濾出「具體 class 且 assignable 到 `ApiRequest` 或 `ApiResponse`」。全部 Request/Response 都繼承這兩者(已驗證)→ 自動涵蓋現有與**未來**合約,零維護。
- **樣本產生**：反射 `Populate` 以樣本非預設值填滿可寫 scalar 屬性,巢狀 class 遞歸一層,集合/字典維持預設(空)。不需手寫 registry。
- **理由**：這一招同時解決原 D1(Theory)、D2(完整性 —— 新合約自動被掃到,不需守衛)、D3 通用層(保真比對),三合一且零逐型別維護。

### D2. 保真比對：re-serialize 相等(round-trip 穩定性)

`serialize(x)` 與 `serialize(deserialize(serialize(x)))` 相等 → 值無遺失。

- MessagePack(整數 key,決定性)、JsonCodec(固定 options,決定性)輸出皆穩定,適用此法。
- 接住「欄位掉了 / `[Key]` 對調 / 屬性名改了不對稱 / 新增不支援型別」。

### D3. 兩個 serializer 策略 —— bee 與 SoarCloud 的關鍵差異

SoarCloud 用對稱的兩個 `IApiPayloadSerializer`(msgpack + json)。**bee-library 沒有 `JsonPayloadSerializer`**(`ApiPayloadOptionsFactory` 只有 messagepack case;JSON wire 走 `JsonCodec`)。故測試內以一個小型 local 抽象封裝兩策略:

| 策略 | Serialize | Deserialize | 保真比對單位 |
|------|-----------|-------------|-----------|
| `msgpack` | `MessagePackCodec.Serialize(obj, type)` | `MessagePackCodec.Deserialize(bytes, type)` | `byte[]` 相等 |
| `json` | `JsonCodec.Serialize(obj)` | `JsonCodec.Deserialize<T>` 經 `MakeGenericMethod(type)` 反射呼叫(JsonCodec 無非泛型多載) | JSON `string` 相等 |

- **對齊真 wire**：msgpack 走 `MessagePackCodec`(含 `SafeMessagePackSerializerOptions` + 自訂 formatter + resolver 鏈,`InternalsVisibleTo` 已設);json 走 `JsonCodec`(含 DataSet/DataTable converter、camelCase、enum-as-string、且**內部會 dispatch `IObjectSerialize` 生命週期 hook**)。不自建 options。

### D4. bee 特有：`DataSet?` / `DataTable?` 欄位特判

9 個 Form/AuditLog response 帶 nullable `DataSet?`/`DataTable?`。盲目反射 `Populate` 遞歸進 `DataSet` 脆弱(會亂設 `EnforceConstraints` 等)。處理:

- `SampleValue` 對 `DataSet` / `DataTable` **回傳 null**(不遞歸),breadth 測試仍覆蓋該合約的其餘 scalar 欄位。
- DataSet/DataTable 的**深度** round-trip 已由既有 `Form/*MessagePackTests`、`AuditLog/*` 與 `*JsonRpcRoundTripTests` 以真實資料覆蓋 → 分工清楚,不重複。

### D5. `SerializeState` 坑(不影響本測試,但記錄)

`ApiMessageBase.Parameters` 序列化時「空回 null」,靠 `SetSerializeState` 驅動;`JsonCodec` 會 dispatch、`MessagePackCodec` 不會。但本測試的保真比對是**各格式各自**做兩次序列化(state 全程一致 None),故穩定性成立、不受影響。`Populate` 對 `ParameterCollection`(IEnumerable)回傳 null 不填,亦繞開此坑。

## 檔案結構

```
tests/Bee.Api.Core.UnitTests/Contracts/
└── ApiContractSerializationTests.cs   # 反射掃組件 + Populate + 雙格式保真比對（單檔）
```

- 既有 `*MessagePackTests` / `MessagePackContractsTests` / `*JsonRpcRoundTripTests`:**保留不動**(逐欄 assert 與 DataSet 深度覆蓋仍有價值);新測試與其並存互補。

## 分階段

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | `ApiContractSerializationTests`：反射掃描 + Populate + 雙格式保真比對 | ✅ 已完成（57 型別 × 2 格式 = 114 tests 綠;全專案 459 綠無回歸） |
| 2 | (可選延伸)把 `Bee.Api.Contracts` DTO(`PackageUpdateInfo` 等)也納入掃描 —— 它們在集合內、breadth 掃 ApiRequest/Response 時因集合留空而未被實際序列化 | 📝 待評估 |

Stage 1 為純邏輯 `[Theory]`,不需 DB、不需 fixture。

## 風險與待確認

1. **穩定性比對的決定性**：需先驗證 `JsonCodec` 對同一物件兩次序列化輸出 byte-for-byte 相同(字典序、集合序不飄)。若有不決定性欄位(如 `Dictionary` 無序),改以反序列化後結構深比或排序正規化。階段 1 第一步先做這個 spike。
2. **多型 / TypelessFormatter 型別**(如帶 `object` 欄位、DataSet)在穩定性比對下可能因型別標記位置而輸出微差 —— 這類直接歸入 D3 逐欄深比,不靠穩定性比對。
3. **樣本真實性**：registry 樣本要「填到有意義」(集合非空、巢狀有值),否則 round-trip 綠但沒測到實質欄位。完整性守衛只保證「型別有登記」,樣本品質靠 D3 深比與 code review 把關。
4. **範圍待確認**：BO 端 `*Args`/`*Result` 是否納入(見下方提問)。

## 對照記憶

- [[messagepack-item-ctor-key-order]]：整數 [Key] 的 ctor 對號 footgun —— 本測試的 D3 逐欄深比正是要接住這類 wire 欄位對調。
- [[messagepack-mobile-wire-aot-emit]]:MessagePack 是行動端 wire 的必經路徑,故合約序列化測試對行動端正確性直接相關。
