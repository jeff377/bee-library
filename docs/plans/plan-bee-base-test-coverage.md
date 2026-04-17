# Bee.Base 測試覆蓋率提升計畫書

## 1. 背景與目標

`Bee.Base` 是 Bee.NET 框架的基礎設施層，提供跨層通用的工具函式、安全元件、序列化、追蹤與背景服務等能力，被上層所有專案（`Bee.Definition`、`Bee.Api.Core`、`Bee.Business` …）所相依。基礎設施的缺陷會連動影響整個框架，因此測試覆蓋率的優先級最高。

**目標**：

1. 補齊 `Bee.Base` 公開 API 的單元測試，使 SonarCloud 顯示的 Line Coverage 對該專案達成 **≥ 80%**。
2. 所有 `public` 類別至少有一個對應測試檔（即使只是基本行為）。
3. 所有安全相關（Security、SysInfo 白名單）與可併發元件（BackgroundServices、Tracing）達到 **≥ 90%** 行覆蓋。
4. 不新增任何需要外部服務（DB／網路／檔案系統 mutable 狀態）的測試；必要時以臨時目錄或記憶體實作替代。

## 2. 現況盤點

下表以「資料夾分組」列出 `Bee.Base` 所有公開類別與目前測試狀態。

圖例：✅ 已完整覆蓋 / ⚠ 部分覆蓋 / ❌ 完全無測試

### 2.1 根目錄

| 類別 | 狀態 | 現有測試 |
|------|------|----------|
| `BaseFunc` | ✅ | BaseFuncTests |
| `StrFunc` | ✅ | StrFuncTests |
| `MemberPath` | ✅ | MemberPathTests |
| `IPValidator` | ✅ | IPValidatorTests |
| `SysInfo` | ⚠ | SysInfoSecurityTests 僅涵蓋 `IsTypeNameAllowed` |
| `DateTimeFunc` | ❌ | — |
| `FileFunc` | ❌ | — |
| `HttpFunc` | ❌ | — |
| `AssemblyLoader` | ❌ | — |
| `ApiException` | ❌ | — |
| `ConnectionTestResult` / `DateInterval` / `DefaultBoolean` / `NotSetBoolean` | ❌ | — |
| `IKeyObject` / `IDisplayName` / `ITagProperty` / `ISysInfoConfiguration`（介面） | n/a | 介面本身不測，由實作者覆蓋 |

### 2.2 Security（安全關鍵）

| 類別 | 狀態 | 現有測試 |
|------|------|----------|
| `AesCbcHmacCryptor` | ✅ | AesCbcHmacCryptorTests |
| `AesCbcHmacKeyGenerator` | ✅ | AesCbcHmacKeyGeneratorTests |
| `PasswordHasher` | ✅ | PasswordHasherTests |
| `RsaCryptor` | ✅ | RsaCryptorTests |
| `FileHashValidator` | ✅ | FileHashValidatorTests |

Security 區塊狀態良好，此次計畫不主動擴充，僅在發現邊界漏測時補齊。

### 2.3 Serialization

| 類別 | 狀態 | 備註 |
|------|------|------|
| `GZipFunc` | ✅ | GZipFuncTests |
| `DataSetJsonConverter` / `DataTableJsonConverter` | ✅ | JsonDataSetSerializationTests |
| `SerializeFunc` | ⚠ | 僅 JSON 路徑間接覆蓋；XML、MessagePack 分支未測 |
| `XmlSerializerCache` | ❌ | — |
| `Utf8StringWriter` | ❌ | — |
| `SerializationExtensions` | ❌ | — |
| `IObjectSerialize`（介面） | n/a | — |

### 2.4 Data

| 類別 | 狀態 | 備註 |
|------|------|------|
| `DbTypeConverter` | ✅ | DbTypeConverterTests |
| `DataSetFunc` | ⚠ | 僅間接覆蓋 |
| `DataRowExtensions` / `DataTableExtensions` / `DataSetExtensions` / `DataViewExtensions` / `DataRowViewExtensions` | ❌ | 擴充方法 |
| `DataTableComparer` | ❌ | — |

### 2.5 Collections

| 類別 | 狀態 | 備註 |
|------|------|------|
| `CollectionBase<T>` / `KeyCollectionBase<T>` | ❌ | 抽象基類 |
| `CollectionItem` / `KeyCollectionItem` | ❌ | — |
| `StringHashSet` / `Dictionary`（包裝） | ❌ | — |
| 介面（`ICollectionBase` 等） | n/a | — |

### 2.6 Tracing

| 類別 | 狀態 | 備註 |
|------|------|------|
| `Tracer` | ❌ | 靜態追蹤入口 |
| `TraceContext` / `TraceEvent` / `TraceListener` | ❌ | — |
| `ITraceListener` / `ITraceWriter` | n/a | 介面 |
| `TraceStatus` / `TraceEventKind` / `TraceLayer` / `TraceCategories`（enum） | ❌ | 簡單 enum |

### 2.7 BackgroundServices

| 類別 | 狀態 | 備註 |
|------|------|------|
| `BackgroundService`（基底） | ❌ | 計時、佇列、並行 |
| `BackgroundAction` | ❌ | — |
| `BackgroundServiceStatusChangedEventArgs` | ❌ | — |
| `BackgroundServiceStatus` / `BackgroundServiceAction` | ❌ | enum |

### 2.8 Attributes

| 類別 | 狀態 | 備註 |
|------|------|------|
| `TreeNodeAttribute` / `TreeNodeIgnoreAttribute` | ❌ | 標記用 Attribute，測試價值低 |

## 3. 測試缺口優先級

### P0（必補）

| 項目 | 理由 |
|------|------|
| `BackgroundService` | 含計時器、佇列、狀態機與執行緒控制，最易出錯且影響執行期穩定性 |
| `Tracer` + `TraceContext` + `TraceListener` | 追蹤是跨層診斷核心，未覆蓋會讓問題無法被發現 |
| `SysInfo`（非安全部分） | 全域設定與型別白名單；影響整個啟動流程 |

### P1（應補）

| 項目 | 理由 |
|------|------|
| `SerializeFunc`（XML / MessagePack 分支） | 公開 API，三種格式需分別驗證 |
| `FileFunc` | 檔案操作邏輯，含路徑組合與讀寫；用臨時目錄即可測 |
| `HttpFunc` | `HttpClient` 快取邏輯可單獨測試；真正發送請求的部分以 `HttpMessageHandler` fake 替代 |
| `DataSetFunc` / `DataTableComparer` | DTO 核心邏輯，跨層資料傳遞基礎 |
| `AssemblyLoader` | 影響型別解析與動態載入 |
| `DateTimeFunc` | 簡單邏輯但覆蓋成本極低 |

### P2（選補）

- `Collections` 下的基底類別與包裝集合：以一個衍生類各寫一組 CRUD 測試即可。
- `Data` 下的擴充方法：以資料驅動（`[Theory]`）批量測試。
- enum、`ApiException`、`ConnectionTestResult`、`DateInterval`、`DefaultBoolean`、`NotSetBoolean`、Attributes：僅在提升覆蓋率需要時補。
- `XmlSerializerCache`、`Utf8StringWriter`、`SerializationExtensions`：屬於 `SerializeFunc` 的實作細節，若 `SerializeFunc` 測試已走到即可，不單獨測。

## 4. 實施階段

採四階段，每階段結束後執行 `dotnet test --configuration Release --settings .runsettings` 並觀察 SonarCloud 覆蓋率變化。

### 階段 1：P0 核心（預估 3 日）

1. `BackgroundServiceTests`
   - 啟動／停止／狀態轉換（`Idle` → `Running` → `Stopping` → `Stopped`）
   - 週期任務觸發次數
   - 佇列動作的先進先出、例外不中斷服務
   - `StatusChanged` 事件發佈
   - 多執行緒壓力（短時間內大量 Enqueue）
2. `TracerTests` + `TraceContextTests` + `TraceListenerTests`
   - 追蹤段 Start/Stop、父子關聯
   - `TraceCategories` 旗標過濾
   - 多個 `ITraceListener` 同時訂閱不重複觸發
   - 無 listener 時不丟例外
3. `SysInfoTests`（擴充現有 `SysInfoSecurityTests`）
   - `Version` 取值
   - `TraceListener` 註冊／取消註冊
   - `ISysInfoConfiguration` 替換
   - 將 `SysInfoSecurityTests` 合併或重新命名為 `SysInfoTests`，並保留 `[DisplayName]` 中文描述

### 階段 2：P1 公開 API（預估 2~3 日）

1. `SerializeFuncTests`
   - 三種格式（JSON／XML／MessagePack）序列化 + 反序列化 round-trip
   - 不支援型別的錯誤處理
   - `null` 與空集合邊界
2. `FileFuncTests`
   - 使用 `Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())` 建暫存目錄，測後 `IDisposable` 釋放
   - 路徑組合、副檔名檢查
   - 不存在檔案的行為
3. `HttpFuncTests`
   - `HttpClient` 快取命中與替換
   - 若有發送邏輯，以 `HttpMessageHandler` fake 注入
4. `DataSetFuncTests` / `DataTableComparerTests`
   - 建表、加欄、複製、比較
5. `AssemblyLoaderTests`
   - 已載入 assembly 重複取用（快取）
   - 未知 assembly 的例外訊息
6. `DateTimeFuncTests`
   - `IsEmpty`、`IsDate` 邊界

### 階段 3：P2 補強（預估 1~2 日）

- `Collections` 擇一衍生類寫 CRUD + Key 衝突 + 找不到鍵
- `Data` 擴充方法以 `[Theory]` 大量覆蓋
- DTO／enum 建構與預設值（僅在覆蓋率仍不足時補）

### 階段 4：收斂與驗收（預估 1 日）

- 在本機產出 coverage 報告（`coverlet.collector` 已內建於測試專案）：
  ```bash
  dotnet test tests/Bee.Base.UnitTests/Bee.Base.UnitTests.csproj \
      --configuration Release \
      --settings .runsettings \
      /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
  ```
- 對照 SonarCloud 的 Bee.Base 覆蓋率，確認 ≥ 80%。
- 若有無法覆蓋的分支（`throw new NotSupportedException`、平台特定程式碼），在 PR 說明中註記。

## 5. 測試撰寫規範（遵循 `.claude/rules/testing.md`）

- 測試檔放在 `tests/Bee.Base.UnitTests/` 根目錄，檔名 `<SutClassName>Tests.cs`。
- 方法命名：`<方法>_<情境>_<預期結果>`。
- 使用 `[Fact]` / `[Theory]`；**本計畫範圍內不需要** `[LocalOnlyFact]`（`Bee.Base` 為純邏輯層，無 DB／API 依賴）。
- 每個測試一律加 `[DisplayName]` 中文描述。
- 測試傳入 `null` 驗邊界時使用 `null!`，避免編譯器警告。
- 安全相關比較使用 `Assert.Equal` 即可（常數時間比較是 SUT 的責任，不是測試的責任）。
- 測試間不共用 mutable 狀態；若需共用初始化，使用 `[Collection]` + `IClassFixture<T>`。

## 6. 風險與注意事項

1. **`BackgroundService` 的計時測試**：避免 `Thread.Sleep` 造成偶發失敗；使用可注入的時鐘／`ManualResetEventSlim` 等同步原語等待事件。
2. **`Tracer` 是靜態類**：測試間可能互相污染；每個測試在 `IDisposable.Dispose()` 中還原 listener 狀態，或以 `[Collection("Tracing")]` 序列化執行。
3. **`SysInfo` 也是靜態**：同上，注意還原現場。
4. **`HttpFunc` 若使用 `static HttpClient`**：避免實際發送請求，以 `HttpMessageHandler` fake 或將測試改為只覆蓋快取邏輯。
5. **`SerializeFunc` 的 MessagePack**：需確認被測型別有 `[MessagePackObject]`；否則以專門的 POCO fixture 進行。
6. **覆蓋率不等於品質**：避免只為提高數字而寫無斷言測試；每個測試至少有 1 個 `Assert`。

## 7. 預期成果

- Bee.Base 覆蓋率 ≥ 80%（SonarCloud 顯示）。
- 安全與可併發元件 ≥ 90%。
- 新增約 12~15 個測試檔，約 150~200 個測試案例。
- 後續 refactor 與升級（例如移除 `Newtonsoft.Json` 或更換序列化後端）具備安全網。

## 8. 執行後續

- 本計畫執行完畢且合併後，依 `CLAUDE.md` 工作流程由使用者要求才將本文件移至 `docs/plans/archive/`。
- 若階段 1 完成後發現估時偏差過大，回報並調整階段 2、3 範圍。
