# Bee.Base.BaseFunc 測試補強計畫書

**狀態：✅ 已完成（2026-04-18）**

> 實作結果：`tests/Bee.Base.UnitTests/BaseFuncTests.cs` 從 49 行擴充至 420 行，測試數由 2 個 → 57 個，階段 1 (P0 + P1) 已全部落地。本機 coverlet 量測 `BaseFunc.cs` Line Coverage **62.0%**（127/205），超過 ≥ 60% 的次要目標。階段 2 (P2 Reflection/Exception) 視 SonarCloud Quality Gate 狀況再決定是否推進。

## 1. 背景與目標

`BaseFunc` 是 `Bee.Base` 最基礎的共用工具函式類別，提供 44 個公開靜態方法，涵蓋：空值判斷、型別轉換、Enum 處理、Reflection 存取、Guid 生成等。被整個框架大量使用。

### 現況問題

- 現有 `BaseFuncTests.cs` 僅 49 行、2 個測試（`IsNumeric`、`RndInt`）
- SonarCloud 顯示 `BaseFunc.cs` Line Coverage 僅 **22.9%**
- 近期完成的 SonarCloud code smells 修正（Batch 1-7）、Nullable 啟用改造，大量方法 signature 有變動，使原本無測試的方法重新進入 SonarCloud「new code」範圍
- 當前 `new_uncovered_lines = 18`，是拖垮 `new_coverage` 到 34.6%（門檻 80%）的主要來源之一

### 目標

1. **主要目標**：補測試使 `BaseFunc.cs` 的 `new_uncovered_lines` 降到 ≤ 3（容忍少數異常分支未覆蓋）
2. **次要目標**：整體 `BaseFunc.cs` Line Coverage 從 22.9% 提升至 **≥ 60%**
3. **不追求**：100% 覆蓋率；Reflection/Exception 類方法可依投入產出比決定是否補

## 2. 現況盤點

### 2.1 已測方法（2 個）

| 方法 | 現有測試 |
|------|---------|
| `IsNumeric` | `IsNumeric_VariousTypes_ReturnsExpectedResult` |
| `RndInt` | `RndInt_ReturnsValueWithinRange` |

### 2.2 最近被修改但無測試的方法（new code 熱點）

依 `git log` 近期 commit 推斷：

| 方法 | 近期修改內容 |
|------|-------------|
| `IsNullOrDBNull(object?)` | signature 加 `?` |
| `IsEmpty(DateTime)` | 加 `DateTimeKind.Unspecified` |
| `GetEnumName(Enum)` | 回傳改為 `string?` |
| `CStr(object)` / `CStr(object, string)` | null-forgiving 處理 |
| `ConvertToNumber` | `is bool` → `is bool b` pattern |
| `CGuid(object)` | pattern matching 改寫 |
| `CDbFieldValue` | 合併 if 語句 |
| `RndInt` | 移除 #else 分支（已測） |
| `CreateInstance` | 回傳改為 `object?` |

### 2.3 完全未測但經常使用的方法

| 分類 | 方法 |
|------|------|
| 空值判斷 | `IsDBNull`、`IsNullOrDBNull`、`IsNullOrEmpty(byte[])`、`IsEmpty`（6 個 overload） |
| 型別轉換 | `CStr` x2、`CBool` x2、`CInt`、`CDouble`、`CDecimal`、`CDateTime`、`CDate`、`CGuid` x2、`CEnum` x2 |
| Enum | `GetEnumName` |
| DB 欄位 | `CFieldValue`、`CDbFieldValue` |
| 序列化狀態 | `SetSerializeState`、`IsSerializeEmpty` |
| GUID | `NewGuid`、`NewGuidString` |
| Reflection | `CreateInstance` x2、`GetAttribute`、`GetPropertyAttribute`、`GetPropertyValue`、`SetPropertyValue`、`IsGenericType`、`CheckTypes` |
| 其他 | `GetCommandLineArgs`、`EnsureNotNullOrWhiteSpace`、`UnwrapException` |

## 3. 補測試優先級

### P0（必補 — 直接影響 Quality Gate）

**範圍**：近期被修改過的方法，對應 new_uncovered 熱點。

| 方法 | 測試設計 |
|------|---------|
| `IsNullOrDBNull` | `[Theory]` 驗證 null、DBNull、物件、字串、整數 |
| `IsEmpty(DateTime)` | `[Theory]` 驗證 MinValue、1753 之前、1753 之後、正常日期 |
| `GetEnumName` | `[Fact]` 驗證 `DateInterval.Day` → `"Day"`、已知 enum 值 |
| `CStr(object)` | `[Theory]` 驗證 null、string、Enum、DateTime、int |
| `CStr(object, string)` | `[Theory]` 驗證 null 回傳 defaultValue、非 null 轉字串 |
| `ConvertToNumber` | `[Theory]` 驗證 bool true/false → 1/0、int、string、Enum |
| `CGuid(object)` | `[Theory]` 驗證 Guid、string、null、非法型別 |
| `CDbFieldValue` | `[Fact]` 驗證 DateTime.MinValue → DBNull.Value；一般值走 `CFieldValue` |

### P1（應補 — 使用頻繁的純邏輯）

| 方法 | 測試設計 |
|------|---------|
| `IsDBNull` | `[Theory]` DBNull vs 一般物件 |
| `IsEmpty`（string / Guid / IList / IEnumerable / byte[]） | 各 1 `[Theory]` 或合併 1 `[Fact]` 測各 overload |
| `IsNullOrEmpty(byte[])` | `[Theory]` null、空陣列、非空陣列 |
| `CBool(string)` / `CBool(object)` | `[Theory]` "true"、"1"、"0"、null、bool、int |
| `CInt` | `[Theory]` int、string、bool、null、Enum、非法字串 |
| `CDouble` / `CDecimal` | `[Theory]` 類似 CInt，含文化相依情境（使用 InvariantCulture） |
| `CDateTime` | `[Theory]` DateTime、string、null、DateTimeKind 處理 |
| `CEnum(string, Type)` / `CEnum<T>(string)` | `[Theory]` 合法、非法字串、不區分大小寫 |
| `CGuid(string)` | `[Theory]` 合法 Guid string、null、空字串、非法字串 |
| `NewGuid` / `NewGuidString` | `[Fact]` 驗證非空、格式正確、重複呼叫不同值 |
| `CFieldValue` | `[Theory]` 各 `FieldDbType` 分支 |

### P2（選補 — Reflection 與異常處理）

| 方法 | 備註 |
|------|------|
| `CreateInstance` x2 | Reflection 類，易寫但需小心測試對象型別 |
| `GetAttribute` / `GetPropertyAttribute` / `GetPropertyValue` / `SetPropertyValue` | 用測試用 POCO 類別搭配 `[DisplayName]` / `[Description]` 驗證 |
| `IsGenericType` / `CheckTypes` | 純判斷，1 個 `[Theory]` 帶過 |
| `GetCommandLineArgs` | 環境相依，須在 CI 與本機行為一致才補 |
| `EnsureNotNullOrWhiteSpace` | 驗證拋出 `ArgumentException` 與正常路徑 |
| `UnwrapException` | 驗證 `TargetInvocationException` → 內層例外、多層包裹 |
| `SetSerializeState` / `IsSerializeEmpty` | 依 `IObjectSerialize` 實作需求，可選補 |

## 4. 實施階段

採兩階段，每階段結束後於本機執行 `dotnet test --configuration Release --settings .runsettings`，觀察本地 coverlet 輸出的覆蓋率，再 push 觸發 CI 驗證 SonarCloud 指標變化。

### 階段 1：P0 + P1（主力，目標 Quality Gate 達標）

**產出**：擴充 `tests/Bee.Base.UnitTests/BaseFuncTests.cs`，預估新增 15~18 個測試方法，約 200~250 行測試碼。

**檔案組織**：
- 保留現有兩個測試，新增測試依方法分組排列，使用 `#region` 或註解區塊分段
- 一律使用 `[DisplayName]` 提供中文描述（對齊 `.claude/rules/testing.md`）
- 參數化以 `[Theory]` + `[InlineData]` 為主，避免重複

**驗證通過標準**：
- `BaseFunc.cs` Line Coverage ≥ 60%
- `new_uncovered_lines` ≤ 3
- SonarCloud Quality Gate 變綠

### 階段 2：P2（選做，視需求推進）

**產出**：視 P1 完成後的實際 Quality Gate 狀況決定。
- 若 Quality Gate 已綠：P2 轉為「日後順手補」，不排時程
- 若未綠：依殘餘未覆蓋方法優先補 Reflection 類（`GetPropertyValue`、`CreateInstance`）

## 5. 驗證方式

1. **本機驗證**：
   ```bash
   dotnet test tests/Bee.Base.UnitTests/Bee.Base.UnitTests.csproj \
     --configuration Release --settings .runsettings \
     --collect:"XPlat Code Coverage;Format=opencover"
   ```
   檢查 `TestResults/*/coverage.opencover.xml` 中 `BaseFunc.cs` 的 `sequenceCoverage`

2. **CI 驗證**：push 到 main 後於 SonarCloud 檢查
   - `https://sonarcloud.io/component_measures?id=jeff377_bee-library&metric=coverage&selected=jeff377_bee-library:src/Bee.Base/BaseFunc.cs`
   - Quality Gate 狀態：`https://sonarcloud.io/summary/new_code?id=jeff377_bee-library`

3. **API 快速驗證**：
   ```bash
   curl -s "https://sonarcloud.io/api/measures/component?component=jeff377_bee-library:src/Bee.Base/BaseFunc.cs&branch=main&metricKeys=line_coverage,new_uncovered_lines"
   ```

## 6. 風險與注意事項

1. **文化相依方法（S6580）**：`CDouble` / `CDecimal` / `CDateTime` 涉及 `Parse`，測試時需明確使用 `CultureInfo.InvariantCulture`，避免本機與 CI 時區/語系差異導致結果不一致
2. **DateTimeKind（S6562）**：`IsEmpty(DateTime)` 已改為 `DateTimeKind.Unspecified`，測試資料建立 `DateTime` 時需一致指定 Kind
3. **Enum 邊界**：`GetEnumName` 對未定義的 enum 值會回傳 `null`，測試需涵蓋此分支
4. **避免外部相依**：本計畫所有測試皆為純邏輯，不使用 `[LocalOnlyFact]`、不連 DB/網路
5. **無新增 `public` API**：僅補測試，不修改 `BaseFunc.cs` 本身；若測試過程發現行為可疑，另立 issue 不混入本 PR

## 7. 預期產出

- `tests/Bee.Base.UnitTests/BaseFuncTests.cs`：從 49 行擴充至約 250~300 行
- 一個 PR，commit message 建議：
  ```
  test(Bee.Base): 補強 BaseFunc 測試覆蓋率（P0+P1）
  ```
- 不新增其他檔案

## 8. 非目標

- 不重構 `BaseFunc.cs` 既有邏輯
- 不調整 SonarCloud Quality Gate 門檻
- 不改 New Code definition
- 不處理 `ApiConnector.cs`（28 行未覆蓋，需 `[LocalOnlyFact]`，CI 會 Skip，另案處理）
