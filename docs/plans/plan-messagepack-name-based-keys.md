# 計畫：MessagePack 合約改採 property-name key（keyAsPropertyName）

**狀態：📝 擬定中（2026-07-22）— 待 go/no-go 決策**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 0 | AOT 反射-only 冒煙 + 範圍決定（阻塞後續） | 📝 待做 |
| 1 | opt-out membership 稽核（90 型別 public 屬性掃描） | 📝 待做 |
| 2 | 逐檔轉換 `[MessagePackObject]` → `keyAsPropertyName` + 移除 `[Key(n)]` | 📝 待做 |
| 3 | 回歸與相容性驗證 + changelog/版本 | 📝 待做 |
| 4 | （條件式）導入 MessagePack source generator | 📝 待評估 |

## 背景與決定脈絡

先前討論結論(見對話 + 記憶 [[messagepack-mobile-wire-aot-emit]]):

- **方向**：wire 合約改 name-based（屬性名為鍵），消滅整數-key 的對號脆弱。
- **實作**：採 **B —— `[MessagePackObject(keyAsPropertyName: true)]`**，不是純去標記的 contractless resolver。理由:保留 MessagePack source generator 退路（source-gen 需要標記；行動端 AOT 可能被逼上 source-gen），且仍可用 `[Key("name")]` 精修、`[IgnoreMember]` 排除。

現況（掃描結果）：

- **hybrid resolver**：`MessagePackCodec` 的鏈以 `ContractlessStandardResolver.Instance` 為 primary；**90 個 `[MessagePackObject]` 型別**目前走整數 `[Key(n)]`。
- **跨繼承 key 協調**：`ApiMessageBase` 用 `[Key(0)]`（`Parameters`），`LoginRequest` 等 derived 用 `[Key(100+)]` 避免與 base 撞號 —— 正是 keyAsPropertyName 要消掉的維護負擔。
- `[IgnoreMember]` 已以 `[XmlIgnore, JsonIgnore, IgnoreMember]` 合併形式保護 `Owner` / `SerializeState` / `Tag` 等基礎設施屬性。

## ⚠️ 決策閘門：這件事「該不該做」（先讀這節再決定執行）

把真實成本與好處攤開，避免當成既定事項：

### 成本

1. **Breaking wire change（最大成本）**：整數-key **array** 格式 → string-key **map** 格式，**wire 不相容**。本框架以 NuGet 對外發佈，外部消費端若 **client/server 未同版升級**（rolling deploy、舊 client 打新 server）即 wire 破裂。必須：major/minor 版本 bump、changelog 明標 breaking、且要求消費端協調升級。
2. **opt-in → opt-out membership（永久成本）**：整數 `[Key]` 是 opt-in（只有標鍵的成員上 wire）；keyAsPropertyName 是 opt-out（所有 public 成員都上 wire）。此後**每個新增 public 屬性都要記得 `[IgnoreMember]`**，否則悄悄外洩上 wire。
3. **90 檔改動 + 繼承鏈一致性**：base 與所有 derived 必須同時轉，不可部分。
4. wire 變大（string key vs array）—— gzip 壓縮緩解，淨成本不高。

### 好處（對 bee 的實際適用性）

| 好處 | 對 bee 現狀 |
|------|-----------|
| 消滅 ctor-order vs Key-order footgun | ✅ 真實、已記錄（[[messagepack-item-ctor-key-order]]） |
| 消滅跨繼承 key 編號協調（base 0 / derived 100+） | ✅ 真實 |
| JSON 與 MessagePack 統一以屬性名為合約 | ✅ 一套心智模型 |
| 跨型別 byte-reinterpret（原始動機） | ⚠️ **潛在，非現行**。bee 目前 wire↔BO args 走**顯式 property-copy**（`ApiInputConverter`/`ApiOutputConverter`），未使用 byte-reinterpret，此好處對 bee 不是現行需求 |

### 建議

這是對**已發佈套件 API surface** 的 breaking wire change，而頭號動機（跨型別 reinterpret）在 bee 未實際使用。建議：

- **不做獨立 breaking release**；若要做，**綁進下一個規劃中的 major 版本**一起發，攤平相容性衝擊。
- 執行前先跑 **Phase 0**（AOT 冒煙 + 範圍決定），再由使用者對「是否值得這個 breaking change」正式 go/no-go。
- 若暫緩：現況 hybrid 可正常運作，只需靠既有記憶與 code review 避開整數-key footgun。

## Phase 0：AOT 反射-only 冒煙 + 範圍決定（阻塞後續）

先確定「現有 Emit-based resolver 在 AOT 下是否本就可行」，因為這決定 migration 範圍：

- 用 `AppContext.SetSwitch("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", false)` 桌面重現（見 [[mobile-trim-half-a-solved]] 的方法），對現有 `MessagePackCodec` 跑既有 **`ApiContractSerializationTests`**（剛完成的 114 tests，天然可複用為 AOT 冒煙）。
- 判定：
  - **現有 Emit resolver 在 AOT-repro 下就掛** → 本 migration **必須併上 source generator**（Phase 4 升為必做）；此時 B（keyAsPropertyName 保留標記）正是 source-gen 的前提，migration 與 AOT 修復合流。
  - **通過** → source-gen 可延後，migration 純屬性轉換。

## Phase 1：opt-out membership 稽核

- 掃 90 個 `[MessagePackObject]` 型別的**所有 public instance 屬性**，標出「**無 `[Key]` 且無 `[IgnoreMember]`**」者 —— 這些在 keyAsPropertyName 下會**新外洩上 wire**。
- 逐一判定：
  - 本該上 wire（原本漏標 key）→ 補進來反而正確。
  - 不該上 wire（計算屬性、基礎設施）→ 補 `[XmlIgnore, JsonIgnore, IgnoreMember]`。
- 產出稽核清單供人工 review（此步是 migration 正確性的關鍵，不可略）。

## Phase 2：逐檔轉換

- `[MessagePackObject]` → `[MessagePackObject(keyAsPropertyName: true)]`。
- 移除 `[Key(n)]` 整數鍵（keyAsPropertyName 用屬性名；僅在需要 wire 名 ≠ 屬性名時保留 `[Key("wireName")]`）。
- `[IgnoreMember]` 保留；Phase 1 稽核結果補上。
- **特殊型別（個別評估，非普通 map 合約）**：
  - `MessagePackKeyCollectionBase<T>` / `MessagePackCollectionBase<T>`：走 `ItemsForSerialization` proxy（`[Key(0)]`）+ `IMessagePackSerializationCallbackReceiver`，序列化為內部 list。
  - `CollectionBaseFormatter<T>` 註冊的型別（`FilterNodeCollection`、`SortFieldCollection`、`CurrencySettings` 等）序列化為 **array**，不受 keyAsPropertyName 影響。
  - 這兩類「集合容器本身」保持現狀；**只轉其 item 型別**（`NumberFormatItem`、`CurrencyItem`、`FilterNode`…）的 map 表示。item 轉 name-based 正是消滅 ctor-order footgun 的所在。
- **繼承鏈一致性**：`ApiMessageBase`（base）與其所有 derived 同時轉，一次 PR 內完成，避免 base/derived 混用鍵策略。

## Phase 3：回歸、文件與相容性

- **主 regression guard**：既有 `ApiContractSerializationTests`（114 tests，雙格式 round-trip 保真）—— 轉換後應維持全綠。
  - ⚠️ 注意其**能測與不能測**：它驗「**同格式** round-trip 保真」（值無遺失），**不驗跨版本 wire 相容**。新舊 wire 本就不相容（這是預期的 breaking），此測試不會、也不應接住它。
- 補一個 **wire-shape 快照測試**（可選）：斷言轉換後某代表型別的 MessagePack 輸出為 **string-key map**（而非 array），固化格式意圖。
- 全 repo `dotnet test`（Release + .runsettings）。
- **文件與 ADR**（執行時一併）：
  - [ADR-030](../adr/adr-030-messagepack-name-based-keys.md) 狀態由「提議中」轉「已採納」。
  - [ADR-004](../adr/adr-004-messagepack-payload.md)「Schema Evolution › `[Key]`」一節改指向 ADR-030 的 name-based 策略（標 superseded-in-part）。
  - `docs/api-bo-contract-design.md` + `.zh-TW.md`（雙語同步）更新 wire 鍵描述。
  - 更新 CHANGELOG（**breaking change** 標記）、bump `Directory.Build.props` 版本。

## Phase 4（條件式）：MessagePack source generator

- 觸發條件：Phase 0 判定 AOT 需要，或行動端實機驗出 Emit resolver 不可行。
- 導入 `MessagePack.Generator` / `[GeneratedMessagePackResolver]`，resolver 鏈前置 generated resolver。B 的 keyAsPropertyName 標記使此步無需再回頭補標記。

## 對照 ADR 與記憶

- [ADR-030：MessagePack 合約改採 property-name key](../adr/adr-030-messagepack-name-based-keys.md) —— 本 plan 對應的架構決策紀錄（提議中）。
- [ADR-004：MessagePack 作為 API Payload 格式](../adr/adr-004-messagepack-payload.md) —— 本 migration revisit 其整數鍵的 schema-evolution 理由。
- [[messagepack-mobile-wire-aot-emit]]：MessagePack 是行動端 wire 必經路徑、Emit-based resolver、real-device AOT 未驗 → 決定採 B 保留 source-gen 退路。
- [[messagepack-item-ctor-key-order]]：整數 `[Key]` 的 ctor 對號 footgun —— 本 migration 的核心動機之一。
