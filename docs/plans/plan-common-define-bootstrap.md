# 計畫：common 系統定義內建化 + 新方案起始範本

**狀態：📝 擬定中（2026-05-31）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 框架自帶 common 定義（embedded resource + 回退 storage） | 📝 待做 |
| 2 | common schema 初始化公開 API（`EnsureCommonSchema`） | 📝 待做 |
| 3 | `dotnet new bee-erp` 專案範本（scaffold 應用骨架） | 📝 待做 |
| 4 | 文件（ADR + cookbook「起新方案」章節） | 📝 待做 |

## 背景

bee-library 是 ERP 底層框架，只實作共通基礎建設，不含業務邏輯。但基礎建設需要 `common` 邏輯資料庫下的四張系統表才能運作：

| 表名 | 用途 | 存取點 |
|------|------|--------|
| `st_user` | 用戶帳號 | `SessionRepository` |
| `st_session` | 連線/Token 管理 | `SessionRepository` |
| `st_company` | 公司主檔 | `CompanyRepository` |
| `st_user_company` | 用戶-公司權限 | `UserCompanyRepository` |

### 問題

這四張表的 `TableSchema` 定義目前**只存在於測試/範例 fixture**：
- [tests/Define/TableSchema/common/](../../tests/Define/TableSchema/common/)
- `samples/Define/`

也就是說，開發者要用框架起新方案時，**得手抄這四份 XML**。這帶來三個問題：

1. **散落複本**：每個方案各持一份 common 定義複本。
2. **升級不傳播**：框架某版若替 `st_session` 加欄位，既有方案的定義檔不會跟著更新。
3. **可被誤改**：common 是框架契約的一部分，但放在開發者目錄下可被任意編輯弄壞。

### 決策（已與使用者確認）

- **common 定義由框架套件擁有**（embedded resource，隨 NuGet 升級自動傳播）。
- **應用骨架由 `dotnet new bee-erp` 範本產生**（SystemSettings / DatabaseSettings / DbCategorySettings 骨架 + 空的 company/log 目錄 + bootstrap 程式）。
- 這對應 [ADR-016 多租戶客製化覆蓋層](../adr/adr-016-multitenant-customization-overlay.md) 的「framework base layer + overlay」分層思路——common 就是最底層、不可竄改的 base。

## 設計

### 核心：decorator 式 `IDefineStorage` 回退

不改 [FileDefineStorage](../../src/Bee.Definition/Storage/FileDefineStorage.cs)，而是新增一個裝飾器，包住任何 `IDefineStorage`，對 **framework-owned 定義**（`category=common` 的 TableSchema、baseline DbCategorySettings 的 common 分類）做「磁碟優先、缺則回退內嵌資源」：

```
LocalDefineAccess (cache)
    └── EmbeddedFallbackStorage  ← 新增（decorator）
            ├── inner: FileDefineStorage（磁碟，應用自訂優先）
            └── embedded: common/*.TableSchema.xml（內嵌資源，框架契約）
```

選 decorator 而非改 `FileDefineStorage` 的理由：
- `IDefineStorage` 已是可組合介面，repo 內已有 [CustomizeOnlyStorage](../../src/Bee.Definition/Storage/CustomizeOnlyStorage.cs) 等實作先例。
- `FileDefineStorage` 保持「純檔案」職責單一，回退邏輯隔離可測。
- `AddBeeFramework` 透過 `BackendDefaultTypes.DefineStorage` 反射建構 storage（見 [BackendDefaultTypes.cs:37](../../src/Bee.Definition/BackendDefaultTypes.cs)），decorator 易於插入而不破壞現有 ctor 解析鏈。

### embedded resource 放哪

放 `Bee.Definition` 專案（storage 與 schema 型別的家）：

```
src/Bee.Definition/
  Resources/Common/
    DbCategorySettings.baseline.xml      # 只含 common 分類
    TableSchema/common/
      st_user.TableSchema.xml
      st_session.TableSchema.xml
      st_company.TableSchema.xml
      st_user_company.TableSchema.xml
```

csproj 標 `<EmbeddedResource Include="Resources/Common/**/*.xml" />`。

> 這些 XML 是現有 fixture 的**正式化版本**——從 `tests/Define/TableSchema/common/` 搬來作為 single source of truth；`tests/Define` 與 `samples/Define` 後續改為「不再持有 common 複本，靠 embedded 回退」（驗證測試管線仍綠）。

### 回退邏輯邊界（重要）

- **只回退 framework-owned 定義**：`category=common` 的 TableSchema、baseline DbCategorySettings 的 common 分類。`company` / `log` / FormSchema / FormLayout / Language **一律不回退**（那是應用自訂，缺檔就該照原本語意拋 `FileNotFoundException`）。
- **磁碟優先**：若應用真的在磁碟放了 `common/st_user.TableSchema.xml`（例如想覆寫），以磁碟為準。預設情境磁碟沒有 → 用內嵌。
- **DbCategorySettings 合併語意**：應用的 `DbCategorySettings.xml` 通常自帶 company/log + 它「以為」要寫的 common。回退策略需定義清楚：
  - 選項 (a)：應用的 DbCategorySettings 完全主導，common 分類由應用自填（現狀，最不侵入）。
  - 選項 (b)：framework 注入 baseline common 分類，與應用的 company/log **合併**，common 永遠以 framework baseline 為準。
  - **傾向 (b)**——徹底達成「common 是契約、開發者不用管」的目標。需在 `DbCategorySettingsCache` 載入點做合併。**待實作前再敲定**。

### 階段 2：common schema 初始化公開 API

把 [DemoSchemaSeeder](../../samples/Bee.Samples.Shared/DemoSchemaSeeder.cs) 的 `EnsureSchema` 概念正式化成框架公開 API，讓新方案啟動時一行建好/升級好 common 四張表：

- 在 `IDatabaseRepository` 加 `EnsureCommonSchema(string databaseId = "common")`，內部對 common 四張表逐一跑 [TableSchemaBuilder.Execute](../../src/Bee.Db/Schema/TableSchemaBuilder.cs)（既有升級管線，能 CREATE 也能 ALTER 既有表）。
- 表清單來源：baseline DbCategorySettings 的 common 分類（不寫死在程式碼，與 embedded 定義同源）。
- 冪等：`TableSchemaBuilder` 已是 compare→plan→execute，無變更則 no-op。

### 階段 3：`dotnet new bee-erp` 範本

`dotnet new` template package，scaffold 出可跑的最小後端方案：

```
MyErp.Server/
  Define/
    SystemSettings.xml           # 範本預填（debug、序列化選項骨架）
    DatabaseSettings.xml         # 預填 common DatabaseItem（連線字串留 placeholder）
    DbCategorySettings.xml       # 預填 company/log 空分類（common 由框架注入）
    FormSchema/                  # 空
    TableSchema/company/         # 空
  Program.cs                     # AddBeeFramework + EnsureCommonSchema + UseBeeBackend
  README.md                      # 填連線字串 → 跑起來 的 3 步驟
```

開發者體驗：`dotnet new bee-erp -n MyErp` → 填連線字串 → `dotnet run`，框架自動建好 common 四張表，開發者只需專心定義自己的 company 業務表。

> 範本內容物可大量沿用既有 [QuickStart.Server](../../samples/QuickStart.Server/) 與 `DemoBackend` 的 wiring；差異在於 common 不再進範本（走 embedded）、連線字串改 placeholder、移除 demo 業務表/seed。

### 階段 4：文件

- **ADR**：`docs/adr/adr-017-common-define-ownership.md`——記錄「common 定義由框架擁有 + embedded 回退 + 範本起始流程」的決策與被否決的方案（複本式範本）。
- **cookbook**：在 [development-cookbook.md](../development-cookbook.md) 加「起一個新方案」章節，串接範本 → 連線設定 → EnsureCommonSchema → 第一張業務表。

## 影響檔案（預估）

| 檔案 | 動作 |
|------|------|
| `src/Bee.Definition/Resources/Common/**` | 新增（embedded common 定義，從 fixture 正式化） |
| `src/Bee.Definition/Storage/EmbeddedFallbackStorage.cs` | 新增（decorator） |
| `src/Bee.Definition/Bee.Definition.csproj` | 加 `<EmbeddedResource>` |
| `src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs` | storage 建構鏈包入 decorator |
| `src/Bee.ObjectCaching/Define/DbCategorySettingsCache.cs` | common 分類合併語意（若採選項 b） |
| `src/Bee.Repository.Abstractions/System/IDatabaseRepository.cs` | 加 `EnsureCommonSchema` |
| `src/Bee.Repository/System/DatabaseRepository.cs` | 實作 `EnsureCommonSchema` |
| `tests/Bee.Definition.UnitTests/...` | 新增 decorator 回退測試（磁碟優先、common-only 邊界、缺檔不回退非 common） |
| `tests/Define/`, `samples/Define/` | 移除 common 複本，驗證走回退仍綠 |
| `templates/bee-erp/**` | 新增 dotnet new 範本 + `.template.config/template.json` |
| `docs/adr/adr-017-*.md`, `docs/development-cookbook.md` | 新增/更新 |

## 已敲定決策（2026-05-31）

1. **DbCategorySettings 合併語意**：採 **(b) framework 注入 baseline common 並與應用的 company/log 合併**，common 永遠以 framework baseline 為準。在 `DbCategorySettingsCache` 載入點做合併。
2. **磁碟覆寫 common**：**保留逃生門**——應用在磁碟放 `common/*.TableSchema.xml` 即覆蓋 embedded（磁碟優先）。方便框架未發版前的 hotfix / 客製租戶。
3. **範本發佈管道**：**獨立 `Bee.Templates` NuGet 套件**，與 runtime 套件解耦。

## PR 切分

- 階段 1+2（框架側：embedded + decorator + `EnsureCommonSchema`）→ 一個 PR。
- 階段 3（`Bee.Templates` 範本）→ 獨立 PR。
- 階段 4（ADR-017 + cookbook）→ 隨階段 1 PR 附帶（決策需先落地）。

## 驗證

- 階段 1+2：新增 decorator 單元測試；`./test.sh` 全綠（含移除 fixture common 複本後仍能載入）；`[DbFact]` common 表測試在各 DB 容器上 CREATE 成功。
- 階段 3：`dotnet new bee-erp -n Probe` 後 `dotnet build` 通過，填 SQLite 連線後 `dotnet run` 能建好 common 四張表。
