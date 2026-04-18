# 計畫：CI 啟用 SQL Server 以執行整合測試

**狀態：✅ 已完成（2026-04-18）**

## 決策摘要（2026-04-18）

| 項目 | 決定 |
|------|------|
| Runner | `ubuntu-latest` + `mcr.microsoft.com/mssql/server:2022-latest` service container |
| DB 測試屬性 | 新增 `[DbFact]` / `[DbTheory]`（判斷 `BEE_TEST_DB_CONNSTR`，未設則 skip） |
| `LocalOnlyFact` 語意 | 回歸原本定位——判斷 `CI=true` 時 skip，僅用於需要本機基礎設施（如本機 API server）的測試 |
| `DbGlobalFixture` 搬遷 | 搬到 `Bee.Tests.Shared`，所有 DB 相依測試專案共用 |
| 需修正的跨平台測試 | `FileFuncTests.IsPathRooted_RecognizesAbsolutePaths` 的 `C:\` InlineData |
| CI 環境變數 | 直接於 workflow 注入 `BEE_TEST_DB_CONNSTR`（ephemeral container）；`BEE_TEST_FIXTURE_MASTER_KEY` 於 `GlobalFixture` 自動建立 |
| Login 測試既有狀況 | `SystemBusinessObjectTests.Login_WithRsaKeyPair_ReturnsDecryptableSessionKey` 需 `AuthenticateUser` 覆寫；base 預設永遠回傳 false。改為 `[Fact(Skip=...)]` 暫停，待後續測試用子類別建立時啟用 |

## 背景

SonarCloud 顯示專案覆蓋率有明顯缺口：

- 整體覆蓋率 71.6%
- `Bee.Repository` 僅 18.9%（`SessionRepository.cs` 為 0%）
- `Bee.Db` 61.1%（`DbAccess.cs` 12.8%、`SqlTableSchemaProvider.cs` 51.8%）
- `Bee.Api.Client/Connectors/*` 覆蓋率多為個位數

根本原因：這些類別是 DB/網路邊界薄殼，單元測試無法模擬。專案已為此設計 `[LocalOnlyFact]` / `[LocalOnlyTheory]`：CI 環境（`CI=true`）自動跳過，本機則正常執行。共計有 30+ 個這類測試散落於：

- `Bee.Db.UnitTests`（19 個）
- `Bee.Repository.UnitTests`（1 個）
- `Bee.Business.UnitTests`（2 個）
- `Bee.Api.Client.UnitTests`（2 個）

測試基礎設施已就緒：

- `DbGlobalFixture`（`Bee.Db.UnitTests`）讀 `BEE_TEST_DB_CONNSTR`，自動驗證連線、建立 `st_user` / `st_session` schema（透過 `TableSchemaBuilder`）、插入種子使用者 `001`。
- `GlobalFixture`（`Bee.Tests.Shared`）讀 `BEE_TEST_DB_CONNSTR` 並加進 DB 設定；CI 環境下自動把 `MasterKeySource` 指到 `BEE_TEST_FIXTURE_MASTER_KEY` 環境變數。
- `.runsettings.example` 已示範連線字串。

只差一步：**CI 端沒有 SQL Server**，所以測試仍被跳過。本計畫在 CI 啟動 SQL Server 並設定環境變數，讓所有 `[LocalOnlyFact]` / `[LocalOnlyTheory]` 在 CI 也能執行。

## 目標

- CI 上所有整合測試（原 `[LocalOnlyFact]`）能實際執行
- 覆蓋率指標真實提升，不靠排除清單
- 本機現有測試流程不受影響（`dotnet test --settings .runsettings` 仍可用）

## 技術選項與建議

### 選項 1：切換 runner 至 `ubuntu-latest` + service container（推薦）

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    env:
      ACCEPT_EULA: Y
      SA_PASSWORD: <強密碼>
    ports:
      - 1433:1433
    options: >-
      --health-cmd "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '<pw>' -C -Q 'SELECT 1'"
      --health-interval 10s
      --health-timeout 5s
      --health-retries 10
```

**優點：**
- GH Actions service container 原生支援，`services` 自動啟動並等就緒
- Linux runner 啟動更快、.NET build 速度一般優於 Windows
- 設定簡潔，不需手動裝 SQL Server

**缺點：**
- 更換 runner OS，需驗證所有測試在 Linux 通過（.NET 10 跨平台，風險低但仍需驗）
- 測試針對 SQL Server 行為的部分若依賴 Windows 專屬功能會受影響（本專案應無此情形）

### 選項 2：維持 `windows-latest` + SQL Server LocalDB

`windows-latest` runner 預裝 SqlLocalDB，無需安裝直接啟動：

```powershell
SqlLocalDB create "BeeTest" -s
$connStr = "Server=(localdb)\BeeTest;Database=common;Integrated Security=True;TrustServerCertificate=True;"
```

**優點：**
- 不換 runner，風險最小
- LocalDB 已預裝，啟動迅速

**缺點：**
- LocalDB 使用 Integrated Security，連線字串格式與本機 `.runsettings.example` 不同
- 需多一步建立 `common` 資料庫
- PowerShell 腳本相較 service container 較繁瑣

### 建議

**優先採選項 1**（ubuntu + service container）；若驗證時遇到 Linux 不相容問題再退回選項 2。

## 實作步驟

### A. CI 工作流（`.github/workflows/build-ci.yml`）

1. 若採選項 1：`runs-on` 改為 `ubuntu-latest`
2. 加入 `services.sqlserver` 區塊
3. 加 step 建立 `common` 資料庫（service container 預設只有 `master`）
4. 在 `Test with coverage` step 注入環境變數（step-level `env:`，遵守 S7636 規範）：
   ```yaml
   env:
     BEE_TEST_DB_CONNSTR: "Server=localhost,1433;Database=common;User Id=sa;Password=${{ secrets.TEST_DB_PASSWORD }};Encrypt=True;TrustServerCertificate=True;"
     BEE_TEST_FIXTURE_MASTER_KEY: ${{ secrets.BEE_TEST_FIXTURE_MASTER_KEY }}
   ```
   （或用隨機產生的密碼，存於 GitHub Secrets）

### B. 測試程式碼

1. 修改 [LocalOnlyFactAttribute.cs](tests/Bee.Tests.Shared/LocalOnlyFactAttribute.cs) 與 [LocalOnlyTheoryAttribute.cs](tests/Bee.Tests.Shared/LocalOnlyTheoryAttribute.cs)：

   將「CI=true 就跳」改為「`BEE_TEST_DB_CONNSTR` 空值就跳」。

   ```csharp
   public LocalOnlyFactAttribute()
   {
       if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BEE_TEST_DB_CONNSTR")))
           Skip = "Skipped – requires BEE_TEST_DB_CONNSTR (DB infrastructure)";
   }
   ```

   **新語意**：測試依賴 DB，DB 未設定就跳，不管本機或 CI。本機沒設 `.runsettings` 的人也會被自動跳過（比現在更合理）。

2. 評估 `DbGlobalFixture` 是否需搬到 `Bee.Tests.Shared`：
   - 目前 `Bee.Repository.UnitTests`、`Bee.Business.UnitTests` 使用基底 `GlobalFixture`，不會自動建 schema
   - 若這些專案獨立執行（例如 `dotnet test Bee.Repository.UnitTests.csproj`）且 DB 沒先被 `Bee.Db.UnitTests` 初始化，會因缺 `st_session` 表而失敗
   - 解法：把 `DbGlobalFixture` 的 schema/seed 邏輯搬到 `Bee.Tests.Shared`，或讓每個 DB 相依的 `GlobalCollection` 使用 `DbGlobalFixture`
   - CI 上順序是 `dotnet test Bee.Library.slnx` 會依序跑所有專案（單一命令、單一 assembly 處理順序）— 需確認 Bee.Db.UnitTests 是否先跑；若不一定，就得搬 fixture

### C. 文件更新

- [testing.md](.claude/rules/testing.md) 的 `[LocalOnlyFact]` 說明同步更新為「無 `BEE_TEST_DB_CONNSTR` 就跳」
- `.runsettings.example` 若需補充說明（本機無 DB 時仍可部分執行）
- CI 的 `BEE_TEST_FIXTURE_MASTER_KEY` 需列為必要 GitHub Secret，於 README 的 CI 設定段補一行

## 風險與取捨

| 風險 | 應對 |
|------|------|
| Linux 下 SQL Server 行為差異（如定序、字串比較） | 本機與 CI 都用 `SQL_Latin1_General_CP1_CI_AS`；`SqlTableSchemaProvider` 讀 schema 的 SQL 應為跨 OS 通用 |
| 跨 test assembly 共用同一 DB 造成資料殘留 / 衝突 | schema 建立為冪等；插入用 `IF NOT EXISTS` 模式；若需隔離，考慮每個 assembly 用不同 DB 名 |
| Service container 啟動 / 就緒時間 | 加 health check（已內建於 service container options） |
| CI 時間變長 | Linux runner 比 Windows 快，抵銷 DB 啟動；整體時間應持平或略快 |

## 預期成效

- 整體覆蓋率從 71.6% 提升至 ~85%+
- `Bee.Repository` 從 18.9% → ~70-80%
- `Bee.Db` 從 61.1% → ~75-85%
- New code coverage 閘門穩定過 80%

## 決策需求

請您確認以下三點後再動工：

1. **Runner 選擇**：ubuntu-latest（推薦）或維持 windows-latest？
2. **`LocalOnlyFact` 語意變更**：從「CI 跳過」改為「無 `BEE_TEST_DB_CONNSTR` 跳過」— 可以嗎？
3. **`DbGlobalFixture` 搬遷**：是否同意把其 schema/seed 邏輯搬到 `Bee.Tests.Shared`，讓所有 DB 相依的測試專案共用？
