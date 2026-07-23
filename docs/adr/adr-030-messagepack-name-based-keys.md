# ADR-030：MessagePack 合約改採 property-name key（keyAsPropertyName）

## 狀態

**提議中（Proposed，2026-07-22）** —— 本 ADR 記錄「合約鍵策略改為 name-based」的決策方向與取捨，**尚未執行**。這是對已發佈套件的 breaking wire change，執行與否由 go/no-go 閘門決定，實施步驟見 [plan-messagepack-name-based-keys.md](../plans/plan-messagepack-name-based-keys.md)。採納後本狀態轉為「已採納」並回頭修正 [ADR-004](adr-004-messagepack-payload.md)。

本 ADR 重新評估 [ADR-004](adr-004-messagepack-payload.md) 「Schema Evolution：`[Key]` 支援欄位新增/移除」一節所隱含的**整數鍵**策略，不改變「MessagePack 作為 API Payload 格式」本身的決策。

## 背景

現況（掃描於 2026-07-22）：

- `MessagePackCodec` 的 resolver 鏈以 `ContractlessStandardResolver.Instance` 為 primary，屬 **hybrid**：**90 個 `[MessagePackObject]` 型別**走整數 `[Key(n)]`，未標記型別才走 contractless（屬性名為鍵）。
- 整數鍵有**跨繼承協調**負擔：`ApiMessageBase` 用 `[Key(0)]`（`Parameters`），`LoginRequest` 等 derived 用 `[Key(100+)]` 避免與 base 撞號。
- 集合以字串鍵一致比對（key 大小寫、欄位名、ProgId 等識別碼型字串比對場景），與整數鍵的位置語意存在心智落差。

觸發重新評估的三個問題：

1. **整數鍵的位置對號 footgun**：`MessagePackCollectionItem` 子型別的參數化 ctor 參數順序若 ≠ `[Key]` 順序，wire round-trip 會**悄悄對調欄位**，XML/JSON 抓不到。此為已記錄的真實踩雷。
2. **JSON 與 MessagePack 兩套相容規則**：JSON wire 以屬性名為合約，MessagePack 以整數鍵位置為合約 —— 同一次改名對兩者的破壞方式不同，心智負擔重。
3. **行動端 AOT**：MessagePack 是行動端（iOS/Android 原生 client）authenticated wire 的必經路徑，而 `MessagePackCodec` 用 Emit-based resolver；real-device AOT round-trip 尚未驗證。若被逼上 MessagePack source generator，source-gen **需要 `[MessagePackObject]` 標記**。

## 決策

**目標**：合約 wire 鍵改為 **name-based（屬性名為鍵）**，消滅整數鍵的位置對號脆弱與跨繼承編號協調。

**實作方式**：採 **`[MessagePackObject(keyAsPropertyName: true)]`**（保留標記），**不採**「純去標記、全靠 `ContractlessStandardResolver`」的做法。

**執行條件（gated）**：這是 breaking wire change，不做獨立 breaking release；若做，綁進下一個規劃中的 major 版本，並先通過 plan 的 Phase 0（AOT 冒煙 + 範圍決定）與 go/no-go。

## 理由

- **消滅位置對號 footgun**：name-based 以屬性名對應，ctor 參數順序不再影響 wire。
- **消滅跨繼承 key 編號協調**：不再需要 base `[Key(0)]` / derived `[Key(100+)]` 的避撞規劃。
- **統一心智模型**：JSON 與 MessagePack 皆以「屬性名」為 wire 合約，一套規則。
- **保留 source generator 退路**：keyAsPropertyName 仍需 `[MessagePackObject]` 標記，日後行動端 AOT 若被逼上 source-gen，標記已在位，不必回頭全補。純去標記的 contractless 會**關掉這道門**（source-gen 需要標記），故不採。

## 取捨

- **Breaking wire change**（最大代價）：整數鍵 **array** 格式 → 字串鍵 **map** 格式，wire 不相容。框架以 NuGet 對外發佈，外部消費端若 client/server 未同版升級即破裂 —— 必須版本 bump、changelog 明標 breaking、協調升級。
- **opt-in → opt-out membership**（永久成本）：整數 `[Key]` 只序列化標鍵成員（opt-in）；keyAsPropertyName 序列化所有 public 成員（opt-out）。此後每個新增 public 屬性都須記得 `[IgnoreMember]`，否則外洩上 wire。
- **wire 變大**：字串鍵大於整數鍵，惟 payload 管線含 GZip，壓縮後淨成本不高。
- **跨型別 byte-reinterpret 對 bee 為潛在、非現行**：bee 目前 wire↔BO args 走**顯式 property-copy**（`ApiInputConverter`/`ApiOutputConverter`），未使用 byte-reinterpret，故此好處對 bee 並非現行需求。

## 未採納的替代方案

- **維持整數 `[Key]`（現況）**：位置對號 footgun 與跨繼承編號協調持續存在，且與 JSON 的名為合約規則分歧。
- **純去標記、全靠 `ContractlessStandardResolver`**：最少 boilerplate，但關掉 MessagePack source generator 退路（source-gen 需要標記）；對行動端 AOT 是不可接受的風險。

## 影響

**本 ADR（提議階段）僅新增此文件，並於 [ADR-004](adr-004-messagepack-payload.md) 加一行交叉引用。**

採納並執行時（見 plan 各 Phase）：

- `[ADR-004]` 「Schema Evolution：`[Key]` 支援欄位新增/移除」一節改為指向本 ADR 的 name-based 策略。
- 90 個 `[MessagePackObject]` 型別轉 `keyAsPropertyName: true`、移除整數 `[Key(n)]`；opt-out membership 稽核補 `[IgnoreMember]`。
- 集合容器型別（`MessagePackKeyCollectionBase<T>` 的 `ItemsForSerialization` proxy、`CollectionBaseFormatter<T>` 註冊為 array 的型別）個別處理，僅轉其 item 型別。
- 公開文件 `docs/api-bo-contract-design.md`（雙語）更新 wire 鍵描述。
- （條件式）導入 MessagePack source generator 與 `[GeneratedMessagePackResolver]`。

**回歸守衛**：`tests/Bee.Api.Core.UnitTests/Contracts/ApiContractSerializationTests.cs`（反射掃全合約、MessagePack + JSON 雙格式 round-trip 保真）為主要 regression guard —— 注意其只驗「同格式 round-trip 保真」，**不驗跨版本 wire 相容**（新舊 wire 本就不相容，屬預期的 breaking）。

## 相關

- [ADR-004：使用 MessagePack 作為 API Payload 序列化格式](adr-004-messagepack-payload.md) —— 本 ADR revisit 其整數鍵的 schema-evolution 理由。
- [ADR-025：定義型別 AOT XmlSerializer 相容](adr-025-define-types-aot-xmlserializer-compat.md) —— 行動端 AOT 序列化的相鄰脈絡。
- [plan-messagepack-name-based-keys.md](../plans/plan-messagepack-name-based-keys.md) —— 執行步驟、決策閘門與 AOT 冒煙。
