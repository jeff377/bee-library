# Bee.Repository / Bee.Repository.Abstractions 測試覆蓋率提升計畫書

**狀態：📝 擬定中**

## 1. 背景與目標

`Bee.Repository.Abstractions` 定義系統與表單儲存庫的介面契約，`Bee.Repository` 提供預設實作。下游被 `Bee.Business`、`Bee.Api.AspNetCore` 等業務／API 層套件依賴；`SessionRepository` 直接負責 AccessToken 的建立／查詢／失效，屬安全鏈路關鍵。

現況觀察（SonarCloud 2026-04-18 快照）：

- **`Bee.Repository`：0.0%**（81 lines uncovered）
- **`Bee.Repository.Abstractions`：0.0%**（15 lines uncovered）— 主要為 `RepositoryInfo` 的 static 初始化邏輯
- 兩個模組合計 96 行未覆蓋，行數雖少但屬 0% → SonarCloud 顯著加重項
- 既有 `tests/Bee.Repository.UnitTests/SessionRepositoryTests.cs` 僅 1 個 `[LocalOnlyFact]`，CI 全部 Skipped → 0% 覆蓋率

**目標**：

1. 在不依賴外部資料庫的前提下，補齊兩個模組所有可純邏輯測試類別與分支，使 SonarCloud 顯示的覆蓋率：
   - `Bee.Repository` Line Coverage **≥ 55%**（扣除 `SessionRepository` 必須連 DB 的部分）
   - `Bee.Repository.Abstractions` Line Coverage **≥ 70%**
2. 所有 `public` 類別至少有一個對應測試檔。
3. 不新增需連接真實資料庫的測試；既有 `[LocalOnlyFact]` 保持原樣。
4. 通過源碼掃描所列規則（`scanning.md` / `sonarcloud.md`）。

## 2. 現況盤點

圖例：✅ 已覆蓋 / ⚠ 部分覆蓋（多為 LocalOnly）／ ❌ 完全無測試 / n/a 介面或不適用

### 2.1 Bee.Repository.Abstractions

| 檔案 | 狀態 | 純邏輯可測項目 |
|------|------|--------------|
| `RepositoryInfo`（static class） | ❌ | static 建構子分支：`SysInfo.IsSingleFile=true` 提前 return；`BackendInfo.DefineAccess=null` 提前 return；正常路徑下 `SystemProvider`／`FormProvider` 由 `BackendDefaultTypes` 對應的型別建立；`CreateOrDefault<T>`（private）：configured 為空走 fallback、configured 不為空走指定型別、configured 指向不存在的型別回 null |
| `RepositoryInfo`（屬性 setter） | ❌ | `SystemProvider`／`FormProvider` 屬性可被外部覆寫（如測試替身） |
| 6 個介面 `IDataFormRepository` / `IReportFormRepository` / `IDatabaseRepository` / `ISessionRepository` / `IFormRepositoryProvider` / `ISystemRepositoryProvider` | n/a | 介面不單獨測試；由實作類別覆蓋 |

**`RepositoryInfo` static 建構子的測試挑戰**：

- static 建構子只執行一次（type initializer），無法在同一 process 重複觸發
- 既已被 `GlobalFixture` 載入，現況下執行的就是「完整初始化」分支
- 若要測試 `IsSingleFile=true` 與 `DefineAccess=null` 兩個分支，需採用以下其中一種：
  - **A**：將 `RepositoryInfo` 的 static 建構子邏輯抽出為 `internal static EnsureInitialized()` 方法，可重複呼叫並重設狀態（須加 `[InternalsVisibleTo]`）
  - **B**：以反射呼叫 `RuntimeHelpers.RunClassConstructor`，但無法重設 `SysInfo` 狀態
  - **C**：放棄測試 static ctor 的早退分支，僅測 `SystemProvider`／`FormProvider` 是否已被填值（覆蓋率上算「已執行」，但分支覆蓋仍缺）

**建議**：採 C，輔以對 `Initialize` / `CreateOrDefault` 的「等價邏輯重做」測試（不直接呼叫 private，但驗證行為意圖）。若時間允許再評估 A。

### 2.2 Bee.Repository

#### 根目錄與 Provider

| 檔案 | 狀態 | 純邏輯可測項目 |
|------|------|--------------|
| `SystemRepositoryProvider` | ❌ | 預設建構子 → `DatabaseRepository` 為 `DatabaseRepository`、`SessionRepository` 為 `SessionRepository`；屬性可被覆寫為測試替身 |
| `FormRepositoryProvider` | ❌ | `GetDataFormRepository(progId)` 回傳 `DataFormRepository` 且 `ProgId` 正確；`GetReportFormRepository(progId)` 同理；多次呼叫回傳不同實例（非快取） |

#### Form

| 檔案 | 狀態 | 純邏輯可測項目 |
|------|------|--------------|
| `DataFormRepository` | ❌ | 建構子設定 `ProgId` 屬性；目前介面無方法 → 測試僅一個 |
| `ReportFormRepository` | ❌ | 同上 |

#### System

| 檔案 | 狀態 | 純邏輯可測項目 |
|------|------|--------------|
| `DatabaseRepository`（internal） | ❌ | `TestConnection`：connection string 替換邏輯（`{@DbName}`／`{@UserId}`／`{@Password}`）— 替換正確但 `connection.Open()` 必失敗，可用「故意給錯誤連線字串 + 攔截 SqlException」驗證；`UpgradeTableSchema`：`databaseId`／`dbName`／`tableName` 空字串拋例外（由 `BaseFunc.EnsureNotNullOrWhiteSpace` 處理） — 純邏輯 |
| `SessionRepository` | ⚠ | LocalOnly | 全部方法皆需 `BackendInfo.DatabaseId` 對應的真實 DB；無純邏輯可測。保留現有 `[LocalOnlyFact]` |

**`DatabaseRepository` 是 `internal`**：需透過 `[InternalsVisibleTo("Bee.Repository.UnitTests")]` 才能直接測；或改透過 `SystemRepositoryProvider.DatabaseRepository` 屬性取得（但介面只暴露 `IDatabaseRepository`，能測的也夠）。**建議**：透過 `IDatabaseRepository` 介面 + `SystemRepositoryProvider` 取得實例測試，避免改 `internal`。

## 3. 階段規劃

### 階段 1：Bee.Repository.Abstractions（15 行未覆蓋）

**目標檔案**：1 個 / **預估測試數**：約 5–8 個

1. **`RepositoryInfoTests.cs`**（新建於 `tests/Bee.Repository.UnitTests/`，因 abstractions 無獨立測試專案）
   - `SystemProvider_AfterFixtureInit_IsNotNull`：由 `GlobalFixture` 觸發初始化後，`SystemProvider` 不為 null
   - `FormProvider_AfterFixtureInit_IsNotNull`：同上
   - `SystemProvider_DefaultType_IsSystemRepositoryProvider`：驗證型別為 `Bee.Repository.Provider.SystemRepositoryProvider`
   - `FormProvider_DefaultType_IsFormRepositoryProvider`：同上
   - `SystemProvider_CanBeReplaced`：以測試替身覆寫後讀回，再還原
   - `FormProvider_CanBeReplaced`：同上
   - **注意**：覆寫 static 屬性需 try/finally 還原，避免污染後續測試

   *如時間允許*：增測 `CreateOrDefault` 行為意圖（無法直接呼叫 private，但可寫一個小型「等價 helper」並驗證 — 或乾脆透過反射呼叫 `typeof(RepositoryInfo).GetMethod("CreateOrDefault", ...)`）

   **是否需新建 `Bee.Repository.Abstractions.UnitTests` 專案？** 不需要 — `RepositoryInfo` 的測試與 `Bee.Repository` 的初始化緊密耦合（依賴 `BackendInfo`），合併至 `Bee.Repository.UnitTests` 更合理；SonarCloud 以模組（namespace / assembly）為單位計算，覆蓋率仍歸給 `Bee.Repository.Abstractions`。

### 階段 2：Bee.Repository — Provider 與 Form 類別

**目標檔案**：4 個 / **預估測試數**：約 10 個

2. **`SystemRepositoryProviderTests.cs`**
   - `Constructor_DefaultDatabaseRepository_IsCorrectType`：透過 `IDatabaseRepository.GetType()` 比對
   - `Constructor_DefaultSessionRepository_IsCorrectType`
   - `DatabaseRepository_CanBeReplaced`：以測試替身（自製 `IDatabaseRepository` 假實作）覆寫
   - `SessionRepository_CanBeReplaced`：同上

3. **`FormRepositoryProviderTests.cs`**
   - `GetDataFormRepository_ReturnsDataFormRepositoryWithProgId`
   - `GetReportFormRepository_ReturnsReportFormRepositoryWithProgId`
   - `GetDataFormRepository_DifferentProgIds_ReturnDifferentInstances`
   - `GetDataFormRepository_SameProgId_ReturnsNewInstanceEachCall`（驗證非快取行為）

4. **`DataFormRepositoryTests.cs`**
   - `Constructor_SetsProgId`（Theory 涵蓋幾個典型值）
   - `Constructor_EmptyProgId_StoresAsIs`（目前不防護，記錄現況）

5. **`ReportFormRepositoryTests.cs`**
   - 同 `DataFormRepository`

### 階段 3：Bee.Repository — DatabaseRepository 純邏輯部分

**目標檔案**：1 個 / **預估測試數**：約 5 個

6. **`DatabaseRepositoryTests.cs`**
   - `UpgradeTableSchema_EmptyDatabaseId_ThrowsArgumentException`（Theory：null、""、"   "）
   - `UpgradeTableSchema_EmptyDbName_ThrowsArgumentException`
   - `UpgradeTableSchema_EmptyTableName_ThrowsArgumentException`
   - `TestConnection_NullItem_ThrowsNullReferenceException`：傳入 null `DatabaseItem`（目前不防護，可記錄現況或加防護）
   - `TestConnection_UnregisteredDatabaseType_ThrowsKeyNotFoundException`：傳入未註冊的 `DatabaseType`（可在新 fixture 中暫不註冊；但 `GlobalFixture` 已註冊 SQLServer，需用其他未註冊類型如 `MySQL`）
   - **不測**：實際連線替換 `{@DbName}` 等占位符後 `Open()` — 屬整合測試，留 `[LocalOnlyFact]`

   **取得 `DatabaseRepository` 實例**：透過 `new SystemRepositoryProvider().DatabaseRepository`（介面型別）；不直接 `new DatabaseRepository()`（internal）

## 4. 預期效益

| 階段 | 涵蓋類別 | 預估新增測試數 | Bee.Repository 提升 | Abstractions 提升 |
|------|---------|---------------|--------------------|------------------|
| 階段 1 | `RepositoryInfo` | ~6 | — | 0% → 約 70% |
| 階段 2 | `SystemRepositoryProvider`、`FormRepositoryProvider`、`DataFormRepository`、`ReportFormRepository` | ~10 | 0% → 約 35% | — |
| 階段 3 | `DatabaseRepository`（純邏輯） | ~5 | 約 35% → 約 55% | — |

**對全 repo 的影響**：三階段完成後，全 repo 覆蓋率預計從 68.3% 提升約 1.0–1.3 個百分點（96 行未覆蓋中消化約 65 行）。

## 5. 不在範圍內的事項

- 既有 `[LocalOnlyFact]` 測試保持原樣，不改寫
- 不新增需連接真實 DB 的測試（如 `SessionRepository.GetSession`／`CreateSession` 的整合測試）
- 不重構 `RepositoryInfo` 的 static 建構子設計（如改為可重複呼叫的 `EnsureInitialized()`）— 屬於另一範疇的測試友善度討論
- 不為 `DatabaseRepository.TestConnection` 新增 null `DatabaseItem` 防護程式碼（除非執行時發現確實需要）；計畫僅以「現況測試」記錄行為
- 不新建 `Bee.Repository.Abstractions.UnitTests` 專案；測試合併至 `Bee.Repository.UnitTests`

## 6. 執行檢核項

每個階段完成後驗證：

- [ ] `dotnet build src/Bee.Repository/Bee.Repository.csproj --configuration Release --no-restore` 通過
- [ ] `dotnet build src/Bee.Repository.Abstractions/Bee.Repository.Abstractions.csproj --configuration Release --no-restore` 通過
- [ ] `dotnet test tests/Bee.Repository.UnitTests/Bee.Repository.UnitTests.csproj --settings .runsettings` 通過
- [ ] CI 模式（`CI=true`）下測試數＝新增數 + 1（既有 LocalOnly 仍 Skip）
- [ ] 不出現新的 SonarCloud／Roslyn 警告
- [ ] commit message 遵循專案慣例（`test(Bee.Repository): ...`）

## 7. 風險與緩解

| 風險 | 緩解 |
|------|------|
| `RepositoryInfo` 的 static 屬性被測試替身覆寫後若未還原會污染後續測試 | 一律 try/finally 還原原值；或封裝在共用 helper |
| `SystemRepositoryProvider` 的 `DatabaseRepository`／`SessionRepository` setter 同上 | 同上 |
| `DatabaseRepository` 為 internal，測試無法直接 new | 透過 `new SystemRepositoryProvider().DatabaseRepository` 取得介面型別實例 |
| `RepositoryInfo` 的 type initializer 已先於測試執行，無法測 `IsSingleFile`／`DefineAccess=null` 早退分支 | 接受該分支不覆蓋；若必要則討論是否將初始化抽出為可重複呼叫的方法 |
| `TestConnection` 的「未註冊 DbType」測試與 `GlobalFixture` 已註冊清單耦合 | 使用一個確定不會被註冊的 enum 值，並在註解標明意圖；若 fixture 未來註冊更多類型需同步調整 |
