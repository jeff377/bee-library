# 計畫：DateTime 只接受伺服端寫入，DataSet 請求方向不轉換時區

**狀態：📝 擬定中（2026-09-12）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 伺服端 Save 入口正規化 DateTime 欄，框架自動戳記 `sys_insert_time` / `sys_update_time` | 📝 待做 |
| 2 | Connector 請求方向只轉過濾條件，DataSet 保留深拷貝但不轉換；移除 `AmbiguousInstantMemory` | 📝 待做 |
| 3 | 定義檔：系統時間戳記欄（`sys_insert_time` / `sys_update_time`）一律標 `ReadOnly`，並寫進 bee-add-form 慣例 | 📝 待做 |
| 4 | 文件：修訂 ADR-032，更新 datetime-timezone / temporal-types / expression-rules（zh-TW 源文件 + en 譯本） | 📝 待做 |

## 背景

[ADR-032](../adr/adr-032-datetime-timezone.md) 採「雙向 UTC」：Connector 收到回應時 UTC → 使用者時區，
送出請求前使用者時區 → UTC。這個設計在請求方向留下一串需要逐一補洞的問題，最近三個 commit 都在補：

- DST 回撥重疊時段，一個牆上時間對應兩個 UTC 值，讀進再存回會晚一小時。
  d94f3239 以 `ConditionalWeakTable` 在 Connector 記住原 UTC 值（[AmbiguousInstantMemory.cs](../../src/Bee.Api.Core/JsonRpc/AmbiguousInstantMemory.cs)），
  但呼叫端自行複製 DataSet 時記憶就失效。
- 用戶端運算式以 `UtcNow()` 填進 `DateTime` 儲存格，送出時被再轉一次（D12 殘餘風險）。
- `DataColumn.DefaultValue` 凍結的時鐘讀數被送回伺服端（be673a00 已修）。

使用者提出新原則，從根本上讓請求方向的 DateTime 不再需要被解讀：

1. `DateTime` 值只接受伺服端寫入的值，例如系統欄位的新增時間、修改時間。
2. Connector 只在回應方向把 `DataSet` 的 `DateTime` 轉成使用者時區。
3. 請求方向 Connector 不轉換 `DataSet` 的 `DateTime`，因為伺服端預設不採用。
4. 需要由用戶端傳入 `DateTime` 的場景，由 BO 自行轉成 UTC 後寫入。

範圍只有 `FieldDbType.DateTime`。`Date`（`DateOnly`）與 `Time`（`TimeOnly`）本來就不轉時區，不受影響。

### 查證時發現的現況

| 項目 | 現況 | 位置 |
|------|------|------|
| 系統時間戳記 | **伺服端沒有寫入機制**。`SysFields.InsertTime` / `UpdateTime` 在 `src/` 零參照；表單存檔的 INSERT 與 UPDATE 寫入全部欄位，`sys_insert_time` 每次 UPDATE 都被 payload 覆寫，`sys_update_time` 從不更新 | [TableSchemaCommandBuilder.cs:59](../../src/Bee.Db/Dml/TableSchemaCommandBuilder.cs:59)、[:105](../../src/Bee.Db/Dml/TableSchemaCommandBuilder.cs:105) |
| DB 的 UTC `DEFAULT` | 表單存檔路徑永遠不生效，因為 INSERT 從不省略欄位 | 同上 |
| 明細列的 `sys_insert_time` | 只來自用戶端 `FormRowDefaults`（`UserZone` 基準） | [FormDataObject.Events.cs:62](../../src/Bee.UI.Avalonia/DataObjects/FormDataObject.Events.cs:62) |
| 稽核 DiffGram | 取 `args.DataSet.GetChanges()`，兩個版本的所有欄位都在內 | [FormBusinessObject.Write.cs:86](../../src/Bee.Business/Form/FormBusinessObject.Write.cs:86) |
| 使用者輸入的 `DateTime` 欄位 | 零個；業務時間欄位全是 `Date` | Define 檔 |
| UI 的 `DateTime` 編輯 | `Auto` 解析為 `DateEdit`，以 `yyyy-MM-dd` 寫回，改一次就截成午夜。AuditRule 版面上的 `sys_insert_time` 可編輯 | [LayoutColumnFactory.cs:69](../../src/Bee.Definition/Layouts/LayoutColumnFactory.cs:69) |
| UI 組過濾條件 | 無；只有外部呼叫 `FormApiConnector.GetListAsync(filter)` 時會帶 | — |
| in-process 呼叫端隔離 | 請求方向換算時會先深拷貝，順帶讓 `LocalApiProvider` 下伺服端改不到 UI 手上的 `DataSet` | [DateTimeZoneConverter.cs](../../src/Bee.Api.Core/JsonRpc/DateTimeZoneConverter.cs) `Convert` |
| session 時區 | 用戶端在登入時快取；伺服端快取重建時重讀使用者 locale，兩者可能分岔 | [ClientInfo.cs:460](../../src/Bee.UI.Core/ClientInfo.cs:460)、[CacheDataSourceProvider.cs:85](../../src/Bee.Business/Providers/CacheDataSourceProvider.cs:85) |

最後一列是 ADR-032 選項 2（非對稱設計）的否決理由在今天仍然成立的具體路徑，也是過濾條件留在 Connector 轉換的主因。

## 決策（2026-09-12 定案）

### 轉換方向的原則

> **Connector 預設只轉回應方向，請求方向預設不轉。**
>
> - **回應方向**：`DataSet` / `DataTable` 的 `DateTime` 欄由 UTC 轉為使用者時區。
> - **請求方向**：`DataSet` 的 `DateTime` 不轉換，伺服端也不採用用戶端的值。
> - **例外（請求方向轉換）**：過濾條件的 `DateTime` 由使用者時區轉為 UTC。它只用於查詢、不會存進資料庫。
> - **不在轉換範圍**：強型別 DTO 屬性與 `Parameters` 兩個方向都是 UTC，由呼叫端負責。

以後出現新的請求方向轉換需求，就比照過濾條件逐案加進例外清單，不回到預設雙向。

**別把它讀成「DataSet 單向、其他雙向」。** 目前沒有任何載體是雙向轉換：過濾條件只出現在請求
（全部訊息型別只有 `GetListRequest` 帶 `FilterNode`），強型別 DTO 兩個方向都不轉。
日後接受使用者輸入的 `DateTime`，也是由 BO 在伺服端轉換，不是 Connector 雙向。

| 載體 | 回應（伺服端 → 用戶端） | 請求（用戶端 → 伺服端） |
|------|------|------|
| `DataSet` / `DataTable` 的 `DateTime` 欄 | UTC → 使用者時區 | **不轉換**（伺服端不採用） |
| `FilterCondition.Value` / `SecondValue` | 不會出現在回應 | 使用者時區 → UTC |
| 強型別 DTO 屬性（`ExpiredAt`、`FromUtc` / `ToUtc`、`ServerTime` 等） | 不轉換，一律 UTC | 不轉換，一律 UTC（呼叫端負責） |
| `Parameters` | 不轉換 | 不轉換 |

`DateOnly` 與 `TimeOnly` 在任何載體、任何方向都不轉換。

### 各面向的決定

| 面向 | 決定 | 理由 |
|------|------|------|
| 伺服端怎麼不採用用戶端值 | **Save 入口正規化**：依 FormSchema 找出 `DateTime` 欄。新增列清空後由伺服端補值；修改與刪除列從 DB 讀回原本的 UTC 值，覆蓋 Original 與 Current | 之後的規則、plugin、稽核、寫入都維持 UTC，D3「伺服端資料路徑為 UTC」照樣成立。另兩案：寫入層排除會讓 BO 與規則讀到使用者時區的值；Connector 送出前清空仍要讀回，還會丟失稽核原值 |
| 正規化的位置 | `FormBusinessObject.Save` 內，`EnforceWriteScope` 之後、`DoBeforeSave` 之前，抽成 `protected virtual` | 規則與覆寫都拿到 UTC。原則 4 的接縫：需要接受用戶端值的 BO 只覆寫這個方法，不必接手授權與寫入範圍檢查 |
| 系統時間戳記 | 框架在正規化時依 `SysFields` 自動戳記，只處理表單有宣告的欄位。新增列：`sys_insert_time`、`sys_update_time` = `UtcNow`；修改列：`sys_update_time` = `UtcNow`，`sys_insert_time` 保留 DB 讀回值 | 原則 1 的落點；不必每張表單各寫一次 |
| 過濾條件 | **Connector 照舊轉換**，保留 `SkipSpringForwardGap` 與 `Kind=Local` guard | 過濾條件不落庫，不牽涉「接受」與否。改由伺服端依 session 時區轉換，會踩到上表的時區分岔 |
| 使用者輸入的 `DateTime` 欄位 | **先不支援**。日後由 BO 覆寫正規化方法自行轉成 UTC；純 FormSchema 表單不支援 | 目前沒有需求，也沒有能保留時分的編輯器 |
| UI | 維持現狀，以 `FormField.ReadOnly` 讓 UI 唯讀；`sys_insert_time` 與 `sys_update_time` 在 FormSchema 一律標 `ReadOnly` | `ReadOnly` 已經由版面產生器帶到 Avalonia 的表單與表格 |
| 強型別 DTO 與 `Parameters` | 維持 UTC 約定，在 ADR 明列為原則 1、3 的例外 | `ExpiredAt`、`FromUtc` / `ToUtc` 的屬性名與 XML doc 已載明基準；改基準要改名，屬破壞性變更 |
| ADR | 修訂 ADR-032，比照 D9 撤回的前例加修訂註記 | 核心（UTC 單一來源、Connector 唯一轉換點）不變 |

範圍外：JS／TypeScript 端的時區處理之後另案實作，屆時對齊新的 wire 語意（`SaveRequest` 的 `DateTime` 儲存格不再保證是 UTC）。

## 階段 1：伺服端入口正規化與系統戳記

**必須先於階段 2 落地。** 階段 1 單獨上線時 Connector 仍送 UTC，而正規化本來就以 DB 值覆蓋，行為相容；
反過來先做階段 2 的話，伺服端會把使用者時區的值寫進資料庫。

### 做法

1. 在 `FormBusinessObject.Save` 的 `EnforceWriteScope` 與 `DoBeforeSave` 之間呼叫新的 `protected virtual` 方法（暫名 `NormalizeDateTimes(SaveContext)`）。
2. 依 `context.Schema` 找出每張表的 `FieldDbType.DateTime` 欄。
3. 新增列：
   - 系統戳記欄設 `UtcNow`（同一次 Save 取同一個讀數）。
   - 其他 `DateTime` 欄：有 `DefaultValueExpression` 的清成 `DBNull`，交給 `DoBeforeSave` 的 `ApplyFieldExpressions` 求值；
     沒有運算式的直接補 `UtcNow`，與 `GetNewData` 的 `FormRowDefaults` 語意一致，也避免以 `DBNull` 撞上 NOT NULL。
4. 修改與刪除列：以該批列的 `sys_rowid` 從 DB 讀回，把 `DateTime` 欄的 Original 與 Current 都設為讀回值；修改列再把 `sys_update_time` 設 `UtcNow`。
   - 沒有修改或刪除列時跳過讀回。
   - 讀回時找不到該列（已被同時刪除）→ 擲 `UserMessageException` 中止存檔。現況下 UPDATE 影響 0 筆本來就會擲
     `DBConcurrencyException`（`DbAccess` 未設 `ContinueUpdateOnError`），提早擋下只是讓訊息看得懂。
   - 改寫 Original 需要 `RejectChanges` / `AcceptChanges`。沿用 d94f3239 的教訓：**先擷取整列兩個版本再 `RejectChanges`**，否則非時間欄的修改會被丟掉。
   - 讀回後列狀態必須維持 `Modified` / `Deleted`。
5. 伺服端的 `Now()` 基準不動（`DateTimeBasis.Utc`），`ApplyComputed` 在正規化之後重算計算欄，結果照常寫入。

### 驗證

- 每家資料庫實跑（`[DbFact]`，push 帶 `[all-db]`）：
  - 用戶端送來的 `sys_insert_time` / 一般 `DateTime` 欄在 INSERT 與 UPDATE 都不落庫。
  - UPDATE 後 `sys_insert_time` 不變、`sys_update_time` 前進。
  - 修改列的非時間欄修改完整保留。
  - 稽核 DiffGram 記的是 UTC，且未變動的 `DateTime` 欄兩個版本相同。
  - 有 `DefaultValueExpression` / `ValueExpression` 的 `DateTime` 欄仍由伺服端求值寫入。
  - 新增列中沒有預設值運算式的 NOT NULL `DateTime` 欄以 `UtcNow` 落庫，不撞 NOT NULL。
  - 修改列在讀回前已被刪除時擲 `UserMessageException`，資料庫沒有任何寫入。
  - 伺服端直接呼叫 `Save`（不經 Connector）傳入的 `DateTime` 同樣不落庫。
- 覆寫正規化方法的 BO 可以採用自己轉換過的值（原則 4 接縫的回歸測試）。
- 既有 `DateTimeZoneDstSaveRoundTripTests`（讀進、改別的欄位、存回）改為驗證伺服端讀回機制，階段 2 移除 Connector 記憶後仍要綠。

## 階段 2：Connector 請求方向只轉過濾條件

1. [PayloadZoneConverter.ToUtc](../../src/Bee.Api.Core/JsonRpc/PayloadZoneConverter.cs)：`SaveRequest` 改為只做 `DataSet.Copy()` 並照舊以 swap 還原，不換算；`GetListRequest` 的過濾條件照舊轉換。
   方法名與 XML doc 要跟著改寫，它不再是「轉 UTC」而是「隔離呼叫端物件＋轉過濾條件」。
2. 移除 [AmbiguousInstantMemory.cs](../../src/Bee.Api.Core/JsonRpc/AmbiguousInstantMemory.cs)，以及 `DateTimeZoneConverter` 裡只服務請求方向 `DataSet` 的 `CellShift` 分支。
   `UserToUtc(DataSet)` / `UserToUtc(DataTable)` 一併移除。留著等於替新原則開後門；屬破壞性變更，
   PublicAPI 異動與相容性判定寫進 commit message，依 ADR-032 D11 目前沒有外部消費者。
3. `SkipSpringForwardGap` 與 `ConvertFilterValue` 保留。
4. [DateTimeWireGuard](../../src/Bee.Api.Core/JsonRpc/DateTimeWireGuard.cs)：請求方向 `DataSet` 的 `DateTimeMode` 檢查與過濾條件的 `Kind=Local` 檢查都保留；guard 在換算之前執行的順序不變。
5. [ApiConnector.ExecuteAsync](../../src/Bee.Api.Client/Connectors/ApiConnector.cs:123) 的註解改寫，不再宣稱「wire 兩個方向都是 UTC」。

### 測試調整

| 測試 | 處置 |
|------|------|
| `DateTimeZoneConverterDstTests` 的 `RoundTrip_AmbiguousInstant*`、`UserToUtc_AmbiguousLocalTime_*` | 移除（Connector 不再有請求方向 `DataSet` 換算） |
| `DateTimeZoneConverterDstTests` 的缺口前推測試 | 改為透過 `ConvertFilterValue` 驗證 |
| `DateTimeZoneConverterTests.UserToUtc_IsInverseOfUtcToUser`、`Convert_ModifiedRowWithNonInstantEdit_KeepsTheEdit(toUtc: true)` | 移除或改為只測回應方向 |
| `PayloadZoneConverterTests.ToUtc_SaveRequestFromConvertedResponse_KeepsAmbiguousInstant` | 移除 |
| `PayloadZoneConverterTests.ToUtc_SaveRequest_SwapsThenRestores` | 改為驗證「複製但不換算，且呼叫端物件不被改動」 |
| `PayloadZoneCoverageGuardTests`、`ApiConnectorDateTimeGuardTests` | 依新範圍檢視 |
| in-process 隔離 | 新增：`LocalApiProvider` 下伺服端正規化不會改到呼叫端的 `DataSet` |

## 階段 3：定義檔

**規則：FormSchema 宣告 `sys_insert_time` 或 `sys_update_time` 時，一律標 `ReadOnly="true"`。**

目前只有 AuditRule 宣告了 `sys_insert_time`，沒有任何 FormSchema 宣告 `sys_update_time`。要改的檔案：

- [src/Bee.Definition/Defaults/FormSchema/AuditRule.FormSchema.xml](../../src/Bee.Definition/Defaults/FormSchema/AuditRule.FormSchema.xml)
- [src/Bee.Definition/Defaults/FormLayout/AuditRule.FormLayout.xml](../../src/Bee.Definition/Defaults/FormLayout/AuditRule.FormLayout.xml)（已產生的版面不會自動跟著 FormSchema 變）
- `apps/Bee.Northwind/Define/` 的對應兩份
- `tests/Define/FormSchema/AuditRule.FormSchema.xml`（已查證沒有測試依賴該欄可編輯：`DefaultsTests` 只核對預設檔清單，`AuditRuleFormTests` 不碰這一欄）

框架沒有「由 TableSchema 產生 FormSchema」的產生器，表單定義都是手寫或照 skill 建立，所以規則要寫進
[bee-add-form skill](../../.claude/skills/bee-add-form/SKILL.md) 第 74 行一帶的 `FormField.ReadOnly` 慣例，
把系統時間戳記欄列為必須唯讀的一類。

不另設閘門測試：資料正確性由階段 1 的伺服端正規化保證，漏標 `ReadOnly` 只會讓使用者能在畫面上改一個存不進去的值，不會寫錯資料。

## 階段 4：文件

- **ADR-032 修訂**：狀態區塊加修訂註記；**D4 開頭寫入「轉換方向的原則」一節的原則與載體對照表**，
  取代原本「收到回應時 UTC → 使用者時區；送出請求前 使用者時區 → UTC」的雙向敘述；
  「考慮過的選項」補上本方案並記錄雙向 UTC 退場的理由；
  改寫 D3、D4（`DataSet` 請求方向、重疊記憶條目移除，過濾條件條目保留）、D6 請求方向 guard 的範圍、
  D12 殘餘風險、D13 的 adapter NOT NULL 註記、「後果」的 round-trip 恆等與 DST 風險條目；
  新增一條明列強型別 DTO 與 `Parameters` 是原則的例外。
- **公開文件**（zh-TW 為源文件，改完對照更新 en 並 `./check-docs-i18n.sh --stamp`）：
  - `docs/zh-TW/datetime-timezone.md`：第 19、21、30 行一帶「雙向」「兩個方向」的敘述；第 58 行過濾條件的敘述照舊成立。
  - `docs/zh-TW/temporal-types.md` §7 的方向對照表。
  - `docs/zh-TW/expression-rules.md` 第 38 行一帶的 `UtcNow()` 殘餘風險。
- `./check-public-docs.sh`、`./check-md-links.sh` 通過。
- 公開文件與 ADR **不得引用本 plan**，需要的結論寫進 ADR 本身。

## 補充決定（2026-09-12）

原列為待確認的四項，已全部定案，並寫回上方各階段：

| 項目 | 決定 | 落點 |
|------|------|------|
| 新增列中沒有預設值運算式的一般 `DateTime` 欄 | 補 `UtcNow`；有運算式的清空交給運算式 | 階段 1 做法第 3 步 |
| 讀回時找不到列 | 擲 `UserMessageException`；沒有修改或刪除列時跳過讀回 | 階段 1 做法第 4 步 |
| 伺服端 BO 之間呼叫 `Save` | **同樣不採用傳入的 `DateTime`**，原則不分呼叫來源。需要寫入 `DateTime` 的作業覆寫正規化方法或直接走 repository。要寫明在正規化方法的 XML doc（`<remarks>`）與 ADR-032 | 階段 1、階段 4 |
| `DateTimeZoneConverter.UserToUtc` | 移除（破壞性變更） | 階段 2 第 2 步 |
| `tests/Define/` 的 AuditRule | 一起補 `ReadOnly` | 階段 3 |

相容性總結（commit message 與 CHANGELOG 用）：

- **行為變更**：`FormBusinessObject.Save` 不再採用呼叫端傳入的 `DateTime` 值，含伺服端 BO 之間的呼叫；
  `sys_insert_time` / `sys_update_time` 改由框架戳記。
- **公開 API**：新增 `FormBusinessObject` 的 `protected virtual` 正規化方法；移除 `DateTimeZoneConverter.UserToUtc` 兩個多載。
