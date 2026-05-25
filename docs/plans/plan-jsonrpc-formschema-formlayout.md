# 計畫：JSON-RPC 加 FormSchema / FormLayout 取得方法

**狀態：📝 擬定中**

## 背景

[plan-jsonrpc-frontend-integration.md](plan-jsonrpc-frontend-integration.md)
已開放 JS 前端透過 Plain wire format 呼叫 7 個 CRUD / Session 方法，
並落地 `samples/Web.Js.Demo`。下一步：JS 前端要做 schema-driven 渲染，
需要拿到 `FormSchema`（用於欄位 metadata / 驗證規則）與 `FormLayout`
（用於 UI 區塊與控制項結構）。

### 為什麼不直接用既有 `SystemBO.GetDefine`

`SystemBO.GetDefine` 設計上以 XML 字串回傳（`result.Xml = XmlCodec.Serialize(value)`）：

- JS 端要先解 XML、再轉物件，跨兩層序列化
- `FormLayout` 沒有實體 XML 檔（由 `FormSchema` 動態 generate），透過
  `GetDefine` 拿等於先 generate 再 XML 序列化再回傳給 JS 再解 XML 再用
- 為 JS 場景開 JSON-native 的 endpoint，server 端 Plain JSON pipeline
  自動把強型別物件展為 JS 可直接消費的 JSON tree，跳過 XML 中介

`GetDefine` 維持原樣供 .NET client 與其他既有用途，本 plan 新增的兩個
方法是 JS / JSON 場景專屬。

### 設計選擇：分兩個方法

候選：
- **A. 分兩個方法**（本 plan 採用）：`GetFormSchema(progId)` +
  `GetFormLayout(progId, layoutId?)`
- B. 合成一個 `GetFormDefinition(progId, layoutId?)` 同時回傳

選 A 理由：
- granular，與其他 FormBO / SystemBO 方法風格一致
- list view 場景可只拿 schema 不拿 layout
- JS 端 lazy-load 容易（先拉 schema 驗證，按需再拉 layout 渲染）

## 已驗證的前提

`tests/Bee.Definition.UnitTests/Layouts/FormLayoutJsonSerializationSpike.cs`
（spike，本 plan 落地時改為正式 regression）已實證：

- `FormSchema` 透過 `JsonCodec.Serialize` 產生乾淨 JSON，camelCase、
  enum-as-string、巢狀 array 結構完整
- `FormLayout` 同上
- 兩者皆無需修改類別本身的序列化標記（`[XmlIgnore, JsonIgnore]` 已正確標好）

唯一發現的小冗餘：`FormSchema` 序列化時 `tables[]` 與 `masterTable`
同時出現，後者等於 `tables[0]`，多載 ~30% payload。本 plan 順手清掉。

## 變更清單

### 1. `FormSchema.MasterTable` 加 `[JsonIgnore]`

**檔案**：`src/Bee.Definition/Forms/FormSchema.cs`

加 `[System.Text.Json.Serialization.JsonIgnore]`（**不**加 `XmlIgnore`，
因為現有 XML 序列化路徑與 .NET client MessagePack 路徑不受影響）。

### 2. 新增 `SystemBO.GetFormSchema`

`Public + Authenticated`。Args 含 `ProgId`，Result 含 `Schema`（強型別
`FormSchema` 物件，server 端 Plain pipeline 序列化為 JSON）。

實作只是 `DefineAccess.GetDefine(DefineType.FormSchema, args.ProgId)` 的
強型別 wrap，不走 Repository（讀 in-memory cache，不碰 DB）。

### 3. 新增 `SystemBO.GetFormLayout`

`Public + Authenticated`。Args 含 `ProgId` 必填、`LayoutId` 可選
（server 預設 `"default"`）。Result 含 `Layout`（`FormLayout` 物件）。

實作：先取 `FormSchema`，再呼叫 `schema.GetFormLayout(layoutId)`。

### 4. 多層檔案實作走 `bee-add-bo-method` skill

兩個方法各需 7~8 個檔案（contract interface / API request / API response /
BO args / BO result / BO method 本體 / DI 註冊 / 兩層 round-trip test），
依 [bee-add-bo-method](../../.claude/skills/bee-add-bo-method/SKILL.md) skill
的硬性規則與層別樣板實作。`SystemBO.GetDefine` 是同軸參考樣板。

不走 Repository（規則 1：「BO 嚴禁直接存取 Bee.Db」對讀 in-memory cache
不適用，但仍要 `IDefineAccess` 注入 BO，不直接 new）。

### 5. JS client wrapper 加兩個方法

**檔案**：`samples/Web.Js.Demo/bee-api-client.js`

```js
export const systemApi = {
  // ... 既有 ...
  getFormSchema: (progId) =>
    rpcCall('System.GetFormSchema', { progId }),
  getFormLayout: (progId, layoutId = '') =>
    rpcCall('System.GetFormLayout', { progId, layoutId }),
};
```

對應 `docs/jsonrpc-frontend-integration.md`（中英版）更新：
- Method catalog 加入兩個方法
- TypeScript wrapper snippet 同步擴充

### 6. Spike test 升級為正式 regression

**檔案**：`tests/Bee.Definition.UnitTests/Layouts/FormLayoutJsonSerializationSpike.cs`
→ 改名為 `FormDefinitionJsonSerializationTests.cs`

- 移除 `Console.WriteLine`（spike 用，不留在 regression）
- 加 assertion 驗證 `FormSchema` JSON 內**沒有** `masterTable` key
  （配合變更 1 的 `[JsonIgnore]`）
- 保留 FormSchema / FormLayout 的 structure assertions

## 驗證方式

1. `dotnet build` 通過
2. `./test.sh` 全綠（含新加的 regression + 兩個方法的 round-trip test）
3. curl 模擬 JS 端呼叫，確認：
   - `System.GetFormSchema` 回傳 JSON 內**沒有** `masterTable`
   - `System.GetFormLayout` 回傳 JSON 結構與 spike 觀察到的一致
   - `params.type` 省略時兩個方法都能正確反序列化（沿用既有 Plain 驗證）

## 影響分析

- **既有 .NET client**：不受影響。`GetDefine` 不變，新增的兩個方法
  .NET 端可選擇是否使用（多一條捷徑路徑）
- **既有 JS demo**：Web.Js.Demo 不在本 plan 動，只是 wrapper 多兩個
  方法供未來 demo 區塊使用
- **`masterTable` JSON 變更**：對 Plain 路徑序列化的呼叫者是 breaking
  change（payload 不再有此 key）。盤點：
  - 既有 GetDefine 走 XML 字串，不受影響
  - 既有 JS demo / docs 內無任何引用
  - .NET client MessagePack 路徑不受影響
  - 結論：實質無外部影響

## 不在本 plan 範圍

- **`Web.Js.Demo` 加 FormDefinition-driven 渲染區塊**（用 GetFormSchema +
  GetFormLayout 串成「動態渲染 Employee 表單 → 串 GetData/Save」的 demo）：
  範圍 ~150 行 JS（form renderer + control-type dispatch + 資料綁定），
  另開 plan 處理
- **`GetTableSchema`**：JS 不需 TableSchema（已確認），不開放對應方法

## 相關連結

- [plan-jsonrpc-frontend-integration.md](plan-jsonrpc-frontend-integration.md)
  — 本 plan 的前置工作
- [docs/jsonrpc-frontend-integration.md](../jsonrpc-frontend-integration.md)
  — JS 整合指引（落地時要更新 method catalog）
- [.claude/skills/bee-add-bo-method/SKILL.md](../../.claude/skills/bee-add-bo-method/SKILL.md)
  — 多層檔案實作流程
- [samples/Web.Js.Demo](../../samples/Web.Js.Demo/) — JS demo sample
