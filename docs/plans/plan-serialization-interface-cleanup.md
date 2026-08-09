# 計畫：盤點 `IObjectSerialize*` 家族並移除孤兒介面

**狀態：✅ 已完成（2026-08-09）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| A | 移除 `IObjectSerializeProcess` 與隨之失效的 `SerializeFormat`、反序列化 lifecycle 路徑 | ✅ 已完成（2026-08-09） |
| B | 保留 `IObjectSerializeEmpty`，補上設計意圖的 XML doc（原訂移除，盤點後推翻） | ✅ 已完成（2026-08-09） |

> 4.19.0 的版號與 CHANGELOG 尚未處理，見「相容性與發版」。

## 背景

`IObjectSerializeProcess` 最初的用途是在 XML 持久化的序列化 / 反序列化過程中就地轉換資料，
實際案例是 `DatabaseSettings.Password` 的加解密。該職責已於 Phase 5（`5037c128`）抽離為
[DatabaseSettingsCryptor](../../src/Bee.Definition/Settings/DatabaseSettings/DatabaseSettingsCryptor.cs)，
由 `CacheDefineAccess` 在讀 / 存時顯式呼叫；更早之前 `SystemSettings` 也已移除該介面（`75cc267c`）。

由此順帶盤點整個 `IObjectSerialize*` 家族，判定哪些是活的抽象、哪些是沒有消費者的空殼。

## 盤點結果

掃描範圍：`src/`、`tools/`、`samples/`、`apps/`、`tests/`、`docs/`（排除 `obj/`、`bin/`）。

| 型別 | src 實作者 | 實際消費端 | 判定 |
|------|-----------|-----------|------|
| `IObjectSerializeBase` | `CommonConfiguration`、`ApiErrorInfo` 直接實作；其餘經 `IObjectSerialize` 繼承 | `SerializationExtensions.ToXml()` / `ToJson()` 的接收型別；production 呼叫點 `RemoteApiProvider.cs:49`、`SystemBusinessObject.cs:63`、`ApiServiceController.cs:264` | **保留** |
| `IObjectSerialize` | 20+（`CollectionBase`／`KeyCollectionBase`／`CollectionItem` 家族、`ApiMessageBase`、全部 Definition 定義類） | `SerializationLifecycle` 於序列化前後翻動 `SerializeState`，撐起 Definition 層 30+ 個「空集合序列化時輸出 null」的屬性 getter | **保留** |
| `IObjectSerializeFile` | 12（`FormSchema`、`TableSchema`、各 `*Settings`…） | `XmlCodec` / `JsonCodec` 的 `SetObjectFilePath`、`SerializationExtensions.Save()`；production 呼叫點 `ApiConnectValidator.cs:88,110`、`ClientInfo.cs:67`、`EndpointStorage.cs:34`、`ApiKeyStorage.cs:41`，以及 `tools/DefineEditor` 4 處 | **保留** |
| `SerializeState` | — | `SerializationUtilities.IsSerializeEmpty` 的判斷閘門（`None` 時一律回 false，確保反序列化路徑不吃掉值） | **保留** |
| `SerializationUtilities` / `SerializationExtensions` | — | 同上 | **保留** |
| **`IObjectSerializeProcess`** | **0**（唯一實作者是 `tests/Bee.Base.UnitTests/SerializationTestFixtures.cs` 中為測它而生的 payload） | `SerializationLifecycle` 的 3 個型別判斷分支 | **移除（階段 A）** |
| **`SerializeFormat`** | — | **只用來餵 `IObjectSerializeProcess` 的三個方法參數**，無其他消費者 | **隨階段 A 移除** |
| `IObjectSerializeEmpty` | 0（唯一實作者是 `SerializationUtilitiesTests` 內的 `EmptySerializeObject`） | `SerializationUtilities.IsSerializeEmpty` 的一個 `case` 分支。**設計目標是 `ExpandableObjectConverter` 巢狀複雜型別，目標型別存在但尚未接線**（見下） | **保留（階段 B 補文件）** |

`SerializationExtensions.ToXmlFile()` / `ToJsonFile()` 目前 production 0 呼叫者（僅測試用），
但**保留**：它們與有實際呼叫者的 `ToXml()` / `ToJson()` 對稱，是框架給外部使用者的公開
affordance，不是內部 facade。

### 兩個 0 實作者的介面，結論相反

兩者都是「production 0 實作者、唯一實作者在測試裡」，但成因不同，結論也不同。

**`IObjectSerializeProcess` — 用途實作過，之後被判定為反模式而遷走。**
Phase 5 抽離加解密的理由寫在 `DatabaseSettingsCryptor` 的 `<remarks>`：讓 DTO 不要在序列化
過程中伸手去碰 process-wide static state。留著這個 hook 等於在公開表面上放邀請函，
請下一個人再走一次同一條路。→ **移除**。

**`IObjectSerializeEmpty` — 用途仍然有效，只是從未接線。**
設計意圖是判斷 `ExpandableObjectConverter` 標記的**巢狀複雜型別**在序列化時是否為空，
為空則不輸出該 XML 元素。目標型別在 repo 內有 13 個，全部集中在
`src/Bee.Definition/Settings/SystemSettings/` 與 `src/Bee.Definition/Logging/`：
`CommonConfiguration`、`BackendConfiguration`、`FrontendConfiguration`、`BackgroundServiceConfiguration`、
`SecurityKeySettings`、`BackendComponents`、`CacheNotifyOptions`、`SessionCleanupOptions`、
`AuditLogOptions`、`ApiPayloadOptions`、`MasterKeySource`、`LogOptions`、`DbAccessAnomalyLogOptions`。

機制的兩端都在，中間沒接上：

| 環節 | 現況 |
|------|------|
| `SerializationUtilities.IsSerializeEmpty` 的 `case IObjectSerializeEmpty` 分支 | ✅ 已備妥 |
| 13 個 Expandable 型別實作該介面 | ❌ 均未實作 |
| 容器端屬性加閘門 | ❌ `SystemSettings.CommonConfiguration` 這類是 `{ get; set; } = new X()` 純自動屬性，不像同檔的 `ExtendedProperties` 有 `if (IsSerializeEmpty(...)) return null;` |

因此現況是**全預設值的複雜型別照樣輸出整棵 XML 子樹**——正是這個介面想避免的事。
既有 30+ 個 `IsSerializeEmpty` 呼叫點傳進去的都是集合，一律走 `IList` / `IEnumerable` 分支，
所以 `case IObjectSerializeEmpty` 至今從未被命中。

移除它等於刪掉一個已設計好、目標明確的掛勾，下一個人還得重新推導；保留的成本只有一個
`case` 分支。→ **保留**，但要把意圖寫進 XML doc，否則下次盤點還會得出「孤兒 → 刪除」的結論
（本 plan 初版就是這樣判的）。

## 階段 A — 移除 `IObjectSerializeProcess` 與 `SerializeFormat`

### A1. 刪檔

- `src/Bee.Base/Serialization/IObjectSerializeProcess.cs`
- `src/Bee.Base/Serialization/SerializeFormat.cs`

### A2. `SerializationLifecycle` 收斂

[SerializationLifecycle.cs](../../src/Bee.Base/Serialization/SerializationLifecycle.cs) 移除三個
`IObjectSerializeProcess` 分支。`NotifyAfterDeserialize` 因此變成空方法，**整個刪除**；
`NotifyBefore` / `NotifyAfter` 只剩翻 `SerializeState`，`format` 參數無用一併移除：

```csharp
internal static class SerializationLifecycle
{
    public static void NotifyBefore(object? value)
    {
        if (value is IObjectSerialize os) { os.SetSerializeState(SerializeState.Serialize); }
    }

    public static void NotifyAfter(object? value)
    {
        if (value is IObjectSerialize os) { os.SetSerializeState(SerializeState.None); }
    }
}
```

類別本身保留（4 個呼叫點共用的型別判斷 + 狀態翻動，不是 1-line delegation facade）。

### A3. 兩個 codec 的呼叫點

| 檔案 | 動作 |
|------|------|
| `XmlCodec.cs:22,34` | 改呼叫無參數版本 |
| `XmlCodec.cs:77` | 刪除 `NotifyAfterDeserialize` 呼叫與其上方的 `// Post-deserialization operations` 註解 |
| `JsonCodec.cs:108,115` | 改呼叫無參數版本 |
| `JsonCodec.cs:131` | 同上刪除 |

`XmlCodec.Deserialize` 刪除該行後，區域變數 `value` 已無後續處理，可直接 `return`；
一併確認沒有留下無用的中繼變數。

### A4. XML doc 與註解的懸空引用

移除型別後，下列 `<see cref>` / `<c>` 會指向不存在的型別（`cref` 會直接編譯失敗）：

- `XmlCodec.cs:8`、`JsonCodec.cs:9` 的類別層 `<summary>`
- `SerializationLifecycle.cs` 的類別與方法 `<summary>`（隨 A2 改寫）
- `DatabaseSettingsCryptor.cs:16` 的 `<remarks>`：改寫為不點名該介面，例如
  「Phase 5 把加解密職責從 `DatabaseSettings` 的序列化回呼抽出，讓 DTO 不再觸碰 process-wide
  static state」——保留 WHY，拿掉指標。

### A5. 公開 API 申報

`src/Bee.Base/PublicAPI.Shipped.txt` 移除 6 行（171–174 的 `IObjectSerializeProcess` 4 行、
178–180 的 `SerializeFormat` 3 行中對應項），並於 `PublicAPI.Unshipped.txt` 補 `*REMOVED*` 條目：

```
*REMOVED*Bee.Base.Serialization.IObjectSerializeProcess
*REMOVED*Bee.Base.Serialization.IObjectSerializeProcess.AfterDeserialize(Bee.Base.Serialization.SerializeFormat serializeFormat) -> void
*REMOVED*Bee.Base.Serialization.IObjectSerializeProcess.AfterSerialize(Bee.Base.Serialization.SerializeFormat serializeFormat) -> void
*REMOVED*Bee.Base.Serialization.IObjectSerializeProcess.BeforeSerialize(Bee.Base.Serialization.SerializeFormat serializeFormat) -> void
*REMOVED*Bee.Base.Serialization.SerializeFormat
*REMOVED*Bee.Base.Serialization.SerializeFormat.Json = 1 -> Bee.Base.Serialization.SerializeFormat
*REMOVED*Bee.Base.Serialization.SerializeFormat.Xml = 0 -> Bee.Base.Serialization.SerializeFormat
```

### A6. 文件（雙語同步）

`src/Bee.Base/README.md:79` 與 `README.zh-TW.md:77` 的「介面導向擴充」條目點名了
`IObjectSerializeProcess`，改為只列 `IObjectSerialize`（該行的型別表格 `IObjectSerialize` 那列不動）。

**不動 `docs/changelogs/4.18.0*.md`** —— 那是已發布版本的歷史紀錄，其中對
`IObjectSerializeProcess` 的敘述在當時正確，改寫等於竄改紀錄。

### A7. 測試調整

`SerializationTestPayload` 移除介面實作與 `Events` 清單後，`XmlCodecTests` /
`JsonCodecTests` 的 lifecycle 測試會失去斷言對象。**不是直接刪掉這兩個測試**——
序列化期間 `SerializeState` 被翻起、結束後歸零，是 Definition 層整套
`IsSerializeEmpty` 機制的地基，必須保留覆蓋。改法是讓 payload 記錄狀態轉換：

```csharp
public class SerializationTestPayload : IObjectSerializeBase, IObjectSerialize, IObjectSerializeFile
{
    // ...
    [XmlIgnore, JsonIgnore]
    public List<SerializeState> StateChanges { get; } = [];

    public void SetSerializeState(SerializeState serializeState)
    {
        _state = serializeState;
        StateChanges.Add(serializeState);
    }
}
```

| 檔案 | 動作 |
|------|------|
| `SerializationTestFixtures.cs:44` | 移除 `IObjectSerializeProcess`、3 個 callback 與 `Events`；改記 `StateChanges` |
| `XmlCodecTests.cs:8-9,40-52` | 移除 `s_xmlSerializeEvents` / `s_xmlDeserializeEvents`；測試改名為驗證狀態轉換（如 `Xml_Serialize_FlipsSerializeStateAndResets`），斷言 `StateChanges` 為 `[Serialize, None]` 且結束後 `SerializeState == None` |
| `JsonCodecTests.cs:8-24` | 同上 |

反序列化路徑本來就沒有狀態翻動（`NotifyAfterDeserialize` 只服務被移除的介面），
故反序列化端只保留既有的 round-trip 值斷言。

## 階段 B — 保留 `IObjectSerializeEmpty`，補上設計意圖

階段 A 與 B 互相獨立，可分別出貨。B 不動任何公開表面與行為，純文件修正，零風險。

### B1. 改寫 XML doc

[IObjectSerializeEmpty.cs](../../src/Bee.Base/Serialization/IObjectSerializeEmpty.cs) 現有的
summary 只寫「determining whether an object has empty data during serialization」，讀不出
它是為誰設計的——這正是它反覆被誤判為孤兒的原因。改為明確記載目標與現況：

```csharp
/// <summary>
/// Lets a nested complex type report itself as empty during serialization so the
/// containing property can omit its XML element entirely.
/// </summary>
/// <remarks>
/// The intended targets are the settings types marked with
/// <c>[TypeConverter(typeof(ExpandableObjectConverter))]</c>, whose XML subtree is
/// currently written out in full even when every value is at its default.
/// Collections do not need this interface — `SerializationUtilities.IsSerializeEmpty`
/// already handles `IList` and `IEnumerable` directly.
///
/// No production type implements this yet. Wiring it up requires both an
/// implementation on the complex type and a null-returning gate on the containing
/// property, in the same shape as `SystemSettings.ExtendedProperties`.
/// </remarks>
```

註解語言依 [code-style.md](../../.claude/rules/code-style.md) 用英文（公開 repo + 隨 NuGet 發佈
進消費端 IntelliSense）；避免行尾 `;` 與連續英文識別字，降低 SonarCloud S125 誤判。

### B2. 不做的事

- **不刪檔、不改 `SerializationUtilities` 的 `case` 分支、不動 `PublicAPI.*.txt`**。
- **不改測試**：`SerializationUtilitiesTests` 的 `EmptySerializeObject` 與
  `IsSerializeEmpty_ObjectSerializeEmpty_ReflectsProperty` 保留——那是該分支唯一的覆蓋來源。

### B3. 接線屬於獨立 feature，不在本 plan 範圍

若日後要真的讓 13 個 Expandable 型別在全預設值時不輸出，需要：

1. 每個型別實作 `IsSerializeEmpty`（判定「所有欄位皆為預設值」）；
2. 容器端把 `{ get; set; } = new X()` 改為帶 backing field 與閘門的屬性，並標 `[DefaultValue(null)]`。

代價是每次替這些型別加欄位，都要同步維護該「全預設」判定，漏改會靜默少輸出設定。
收益是設定檔 XML 大幅精簡。這是有取捨的功能決策，**另立 plan 評估**，不夾帶在本次清理。

## 相容性與發版

**只有階段 A** 涉及 shipped public API 的移除（`IObjectSerializeProcess` 與 `SerializeFormat`），
屬 source-breaking 與 binary-breaking。階段 B 純文件，不影響公開表面。

- 依 [releasing.md](../../.claude/rules/releasing.md) 的 pre-stable 例外，v4.x 且無外部消費者，
  允許在 minor 版內移除，但**必須在 CHANGELOG 明列，不可靜默**。
- 下一版為 **4.19.0**：更新 `src/Directory.Build.props`、根 `CHANGELOG.md` / `CHANGELOG.zh-TW.md`
  各一行 WHAT，明細寫進 `docs/changelogs/4.19.0.md` / `.zh-TW.md`（雙語條目數一致）。
- 明細需交代 WHY：`IObjectSerializeProcess` 的唯一 production 用途已在 Phase 5 遷出，
  且該模式（DTO 於序列化過程觸碰 process-wide static state）被判定為反模式。
- 若外部確有實作者需要 post-deserialize hook，替代做法是在自己的型別內處理，
  或改用 MessagePack 既有的 `IMessagePackSerializationCallbackReceiver`。
- 不需新增 ADR：這是移除未使用的抽象，沒有新的設計決策要長期記錄；WHY 寫在 changelog 明細即可。

## 驗證

```bash
dotnet build Bee.Library.slnx -c Release --no-incremental
```

analyzer 通過即證明公開表面申報一致（`RS0016` 等會擋未申報變更；A5 若漏改會直接失敗）。
`git commit` 前的 hook 也會跑同一條指令。

```bash
./test.sh tests/Bee.Base.UnitTests/Bee.Base.UnitTests.csproj
./test.sh
```

先跑 Bee.Base 單測確認 A7 的改寫成立，再跑全量確認 Definition 層 30+ 個
`IsSerializeEmpty` 屬性的序列化輸出沒有變化（XML round-trip 測試會抓到空集合輸出方式的改變）。

額外人工確認殘留引用：

```bash
grep -rn "IObjectSerializeProcess\|SerializeFormat" src tools samples apps tests docs --include="*.cs" --include="*.md" | grep -v "/obj/"
```

預期只剩 `docs/changelogs/4.18.0.md` / `.zh-TW.md` 的歷史敘述（該兩檔刻意不動，見 A6）。
`IObjectSerializeEmpty` 保留，不列入本檢查。
