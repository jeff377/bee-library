# 計畫：SonarCloud HIGH 等級 10 個 issues 修正

**狀態：🚧 進行中**

## 背景

SonarCloud 掃描後仍有 10 個 HIGH 嚴重等級 open 問題（全部屬 MAINTAINABILITY 類別），見 `https://sonarcloud.io/project/issues?impactSeverities=HIGH&issueStatuses=OPEN&id=jeff377_bee-library`。規則分佈：

- 1 × **S2365** — property 不應回傳 collection 拷貝
- 9 × **S3776** — Cognitive Complexity 超標

雖 `.claude/rules/sonarcloud.md` 將 S3776 列為「重構判斷題，不納入硬性規則」，但本輪用戶明確要求修正全部 HIGH，故逐案處理。

## 修正策略

### 1. S2365 — `MessagePackKeyCollectionBase.ItemsForSerialization`

**檔案**：`src/Bee.Definition/Collections/MessagePackKeyCollectionBase.cs:143`

**處理方式**：MessagePack 序列化以 `[Key(0)]` 標記**屬性**（非方法），而 `KeyedCollection` 只能透過 `Items.ToList()` 取出內部項目；改成 method 會破壞序列化且先前已證實會破壞測試 `ItemsForSerialization_Get_ReturnsCurrentItems`。

**行動**：加上 `[SuppressMessage("Major Code Smell", "S2365")]` 並註解理由（已有說明註解，補 attribute）。

### 2. S3776 × 9 — Cognitive Complexity 重構

各方法以「萃取私有 helper」為主，不改動對外行為。

| # | 檔案 | 方法 | CC | 策略 |
|---|------|------|-----|------|
| 2 | `src/Bee.Api.Core/JsonRpc/ApiPayloadJsonConverter.cs:17` | `Read` | 23→15 | 萃取：`ReadProperty`（單一屬性解析 switch）、`ResolvePayloadValue`（format→Value 的 switch）|
| 3 | `src/Bee.Api.Core/MessagePack/SerializableDataTable.cs:133` | `ToDataTable` | 25→15 | 萃取：`BuildColumns`、`RestorePrimaryKeys`、`RestoreRow`（各 RowState 獨立函式）|
| 4 | `src/Bee.Db/DbAccess.cs:315` | `UpdateDataTable` | 21→15 | 萃取：`ValidateUpdateSpec`、`PrepareTransaction`、`AttachCommands`、`AttachTransaction`、`DisposeCommands` |
| 5 | `src/Bee.Definition/Settings/DatabaseSettings/DatabaseSettings.cs:122` | `AfterDeserialize` | 16→15 | 萃取：`DecryptPassword`（try-catch-base64-decrypt-utf8），Server/Item 兩迴圈各自呼叫 |
| 6 | `src/Bee.Base/Serialization/DataTableJsonConverter.cs:186` | `ReadColumns` | 18→15 | 萃取：`ReadColumnField`（單一欄位 switch）|
| 7 | `src/Bee.Db/Query/InternalWhereBuilder.cs:45` | `BuildCondition` | 20→15 | 萃取：`BuildNullCondition`、`BuildLikeCondition`、`BuildInCondition`、`BuildBetweenCondition`，主 switch 僅負責分派 |
| 8 | `src/Bee.Db/DbCommandSpec.cs:167` | `ResolveParameters` | 24→15 | 萃取：`ExpandParametersVariable`、`ResolveNumericKey`、`ResolveNamedKey` |
| 9 | `src/Bee.Api.Core/MessagePack/SerializableDataTable.cs:55` | `FromDataTable` | 35→15 | 萃取：`BuildColumnList`、`BuildRowData`（依 RowState 填 current/original）|
| 10 | `src/Bee.Base/Data/DataTableComparer.cs:16` | `IsEqual` | 35→15 | 萃取：`AreStructuresEqual`（TableName/Columns/PK）、`AreRowsEqual`（逐 row/col）、`AreColumnValuesEqual`（依 RowState）|

## 行為相容性保證

- 所有萃取僅是將 block 搬到 private static method，不改變邏輯、例外型別、回傳值
- 現有測試套件（含 Bee.Base / Bee.Db / Bee.Api.Core / Bee.Definition.UnitTests）跑完驗證
- 各專案公開 API 不動

## 測試驗證

每個檔案改完後：
1. `dotnet build` 確認無 warning/error
2. `dotnet test <對應專案>` 確認測試全過
3. 全修完後跑一次全套測試（`CI=true` 模擬 CI 環境）

## Commit 策略

為利回溯與審查，分 3 個 commit：
1. **commit A**：S2365 suppress + 簡單的 S3776（#5、#6、#7）
2. **commit B**：Db 相關 S3776（#4、#8）
3. **commit C**：DataTable/Payload 相關 S3776（#2、#3、#9、#10）

修完一次性 push。

## 風險

- `SerializableDataTable.FromDataTable/ToDataTable` 涉及序列化 round-trip，若測試覆蓋不足（需確認）可能漏掉邊角 case；若有疑慮可先查既有測試
- `DbCommandSpec.ResolveParameters` 為 SQL 組裝核心路徑，規則略為複雜；萃取時特別小心
