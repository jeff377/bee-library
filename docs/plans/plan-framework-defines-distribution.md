# 計畫：框架預設定義的存放與發送機制

**狀態：✅ 已完成（2026-06-09）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | Master copies 搬到 `src/Bee.Definition/Defaults/`、`<EmbeddedResource>` 嵌入、新增 `Bee.Definition.Defaults` 公開 API、測試 fixture 對應調整 | ✅ 已完成（2026-06-09） |
| 2 | `tools/Bee.Cli/` console 專案（dotnet tool）— 框架級 CLI，`dotnet bee defines ...` 子命令呼叫 Phase 1 API | ✅ 已完成（2026-06-09） |
| 3 | DefineEditor 啟動時呼叫 `Defaults.MaterializeTo(...)`（直呼 API，不 subprocess） | ✅ 已完成（2026-06-09） |

## 背景

framework-reserved-names.md 第 3 節寫：「擴充框架表 → 在 `DefinePath` 放一份同名檔覆蓋」。但目前**沒有任何途徑取得 base XML**——這些檔案只活在 `tests/Define/`，對純 NuGet 消費者來說等於拿不到。本 plan 補上完整的「存放 + 取得」機制。

## 設計原則

### 詮釋 B（已拍板）

```
┌───────────────────────────────────────────────┐
│ Bee.Definition.Defaults.MaterializeTo(path)   │  ← 真正在做事
│   (public API in Bee.Definition NuGet)        │
└──────────────────┬────────────────────────────┘
                   │
       ┌───────────┴───────────┐
       │                       │
┌──────▼──────┐         ┌──────▼─────────┐
│  dotnet bee │         │  DefineEditor  │
│   (CLI)     │         │  (Avalonia)    │
│             │         │                │
│ thin shell  │         │ 啟動時直接呼叫 │
└─────────────┘         └────────────────┘
```

- Library API = 真正在做事
- CLI = thin shell，user-facing 文件化主介面（CI / 報錯訊息引導使用）
- DefineEditor = 直呼同一份 API，不 subprocess

### 不變項

- `IDefineStorage` / `FileDefineStorage` / `DbDefineStorage` / `LocalDefineAccess` **完全不動**
- runtime 不感知 embedded 存在，只讀 `DefinePath` 內檔案
- 消費者客製採全量覆寫（A1，先前已拍板）

## 範圍盤點：哪些是「框架自有」？

掃 `tests/Define/` 後分類：

### 框架自有（搬至 `src/Bee.Definition/Defaults/`，共 19 檔）

| 類別 | 檔案 |
|------|------|
| TableSchema/common | `st_user` / `st_session` / `st_company` / `st_user_company` / `st_cache_notify` / `st_define` |
| TableSchema/company | `st_role` / `st_role_grant` / `st_user_role` / `st_department` / `st_employee` |
| FormSchema | `Department` / `Employee` |
| FormLayout | `Department` / `Employee` |
| Language/en-US | `Department` / `Employee` |
| Language/zh-TW | `Department` / `Employee` |

### tests 自有（留在 `tests/Define/`）

| 類別 | 檔案 | 性質 |
|------|------|------|
| TableSchema/company | `ft_project` | 業務範例 |
| FormSchema | `PermGateForm` / `Project` | 權限測試 + 業務範例 |
| FormLayout | `Project` | 業務範例 |
| Language/en-US, zh-TW | `Project` | 業務範例 |
| 部署設定 | `DbCategorySettings.xml` / `SystemSettings.xml` / `DatabaseSettings.xml` | test-specific（含 `MasterKeySource.Type = Environment` 等） |

### 待確認

`DbCategorySettings.xml` 要不要算框架自有？
- 若要：框架版只列 11 張 `st_*`，消費者擴自家 `ft_*` 時新增 entry
- 若不：framework-reserved-names.md 文件指示消費者自行寫 DbCategorySettings 並參照表清單

**建議：算框架自有**（精簡版只列 `st_*`，消費者擴）。tests 自有的會擴加 `ft_project`，與框架版分離。

## Phase 1：Library API + 搬遷 + 測試 fixture 調整

### 1.1 檔案搬遷

```
tests/Define/                      src/Bee.Definition/Defaults/
├── TableSchema/                   ├── TableSchema/
│   ├── common/                    │   ├── common/
│   │   ├── st_user.*.xml          │   │   └── (6 檔)
│   │   ├── st_session.*.xml       │   ├── company/
│   │   └── ... (6 檔)             │   │   └── (5 檔，不含 ft_project)
│   └── company/                   ├── FormSchema/
│       ├── st_role.*.xml          │   ├── Department.*.xml
│       ├── st_employee.*.xml      │   └── Employee.*.xml
│       └── ft_project.*.xml       ├── FormLayout/
├── FormSchema/                    │   ├── Department.*.xml
│   ├── Department.*.xml           │   └── Employee.*.xml
│   ├── PermGateForm.*.xml         ├── Language/
│   └── Project.*.xml              │   ├── en-US/
├── ... (其他)                     │   │   ├── Department.*.xml
                                   │   │   └── Employee.*.xml
                                   │   └── zh-TW/
                                   │       └── (同上)
                                   └── DbCategorySettings.xml  ← 精簡版
```

`git mv` 直接搬，保留 history。

### 1.2 `Bee.Definition.csproj` 加 `<EmbeddedResource>`

```xml
<ItemGroup>
  <EmbeddedResource Include="Defaults\**\*.xml">
    <LogicalName>Bee.Definition.Defaults.%(RecursiveDir)%(Filename)%(Extension)</LogicalName>
  </EmbeddedResource>
</ItemGroup>
```

Resource manifest name 範例：`Bee.Definition.Defaults.TableSchema.common.st_user.TableSchema.xml`。

### 1.3 公開 API

`src/Bee.Definition/Defaults/` 內新增 C# 類別：

```csharp
namespace Bee.Definition.Defaults
{
    public sealed class MaterializeOptions
    {
        public static MaterializeOptions Default { get; } = new();

        /// 已存在的檔是否覆寫；預設 false（消費者客製不會被清掉）
        public bool Overwrite { get; init; } = false;

        /// 過濾條件；null 代表全部 materialize
        public Predicate<string>? Filter { get; init; }
    }

    public sealed record MaterializeResult(
        int WrittenCount,
        int SkippedCount,
        IReadOnlyList<string> WrittenRelativePaths,
        IReadOnlyList<string> SkippedRelativePaths);

    public static class Defaults
    {
        /// 把 embedded 框架預設物化到 definePath 下；
        /// 已存在的檔依 options.Overwrite 決定是否覆寫。
        public static MaterializeResult MaterializeTo(string definePath, MaterializeOptions? options = null);

        /// 列出框架自有的所有相對路徑（例：TableSchema/common/st_user.TableSchema.xml）
        public static IReadOnlyList<string> ListEmbedded();

        /// 取得單一 embedded 檔的 stream（供 GUI 工具預覽用）
        public static Stream OpenEmbedded(string relativePath);
    }
}
```

### 1.4 測試 fixture 調整（**最大工程量**）

目前 `BeeTestFixture` 預設 `PathOptions.DefinePath = "tests/Define"`，那是個共享 read-only 目錄。搬遷後該目錄缺了 19 檔，現有測試會炸。

#### 方案 a（推薦）：fixture init 時 materialize 到 temp，與 tests/Define 合併

```csharp
// BeeTestFixture default 路徑改成 per-class temp（已有 UseTempDefinePath builder API）
// fixture init 流程：
1. mkdtemp → tempDir
2. CopyDirectory("tests/Define/", tempDir)                     // test-specific 在前
3. Defaults.MaterializeTo(tempDir, new { Overwrite = false })  // framework 補在後，不覆蓋 test 客製
4. PathOptions.DefinePath = tempDir
```

優點：tests/Define 收斂為純 test-specific；fixture 的「合併」邏輯與 production 消費者跑 CLI materialize 行為一致（dogfooding）。
缺點：每個 `IClassFixture<BeeTestFixture>` 多一個 mkdtemp + 19 檔複製 (~10–30 ms per test class)。

#### 方案 b：tests/Define 保留實體副本（duplicate），build step 強制同步

優點：fixture 不變、零 runtime 成本。
缺點：duplication；要寫 MSBuild target 保證 `tests/Define/` 與 `src/Bee.Definition/Defaults/` 同步（不同步即 build fail）；維護負擔。

#### 方案 c：BeeTestFixture default 改為 `src/Bee.Definition/Defaults`，testspecific 另外處理

優點：framework 部分零複製。
缺點：tests/Define 中 ft_project / DbCategorySettings 等需要另一個 path option chain，等於把先前否決的 storage chain 邏輯偷渡回測試端。

**推薦走 a**。fixture init overhead 在可接受範圍內，且讓 fixture 行為等同於 production 消費者，意外發現問題機率高。

### 1.5 文件更新

- `framework-reserved-names.md` 第 3 節「Consumer guidelines」加一句：「取得 base XML：跑 `dotnet bee defines materialize`（Phase 2）或開 DefineEditor（Phase 3）；或直接到 `src/Bee.Definition/Defaults/` 瀏覽」
- README 加「Getting framework defaults」一節

## Phase 2：框架 CLI `Bee.Cli`（`dotnet bee` subcommand 風格）

### 2.1 專案

新增 `tools/Bee.Cli/Bee.Cli.csproj`：
- `<OutputType>Exe</OutputType>` + `<PackAsTool>true</PackAsTool>` + `<ToolCommandName>dotnet-bee</ToolCommandName>`
- Reference `Bee.Definition`
- 用 `System.CommandLine`（或輕量 arg parser）做 subcommand 路由

> **命名考量**：`Bee.Cli` 套件名是「框架級 CLI」，預留 `defines` / `schema` / `tenant` / `samples` 等多種未來 subcommand 的成長空間；不是只給 defines 用。本 plan 只實作 `defines` 子樹，其他 subcommand 留待後續另起 plan。

### 2.2 Subcommands（本 plan 內最小集）

```bash
$ dotnet bee --version
$ dotnet bee defines materialize --path <dir> [--overwrite]
$ dotnet bee defines list                                   # 列出所有 embedded 檔
```

`defines` 是 subcommand group，下掛 `materialize` / `list`。每個 leaf command 純 arg parse + 呼叫 `Bee.Definition.Defaults.Defaults.*` API + `Console.WriteLine` + return exit code。實作 ~120 行（含 `System.CommandLine` boilerplate）。

### 2.3 未來成長空間（不在本 plan）

預留下列頂層 subcommand group 命名，未來各自獨立 plan 實作：

```bash
$ dotnet bee schema upgrade --db common              # 觸發 schema 升級
$ dotnet bee tenant init --id corp_a                 # 初始化新租戶
$ dotnet bee samples scaffold --name MySample        # scaffold 一個 sample 專案
```

本 plan **不**實作這些；只是說明「為什麼套件叫 `Bee.Cli` 而非 `Bee.Defines.Tool`」。

### 2.4 發佈 pipeline

新增 `.github/workflows/dotnet-tool-publish.yml`（或併入 `nuget-publish.yml`），tag push 時 `dotnet pack -p:PackAsTool=true` 推上 NuGet.org。

### 2.5 安裝示例

```bash
$ dotnet tool install -g Bee.Cli
$ dotnet bee defines materialize --path ./Define
Materialized 19 framework default files to ./Define
  6 TableSchema (common/)
  5 TableSchema (company/)
  2 FormSchema
  2 FormLayout
  4 Language
```

## Phase 3：DefineEditor 啟動時 materialize

### 3.1 啟動流程

`tools/DefineEditor/Program.cs` 在啟 Avalonia UI 前：

```csharp
var definePath = LoadDefinePathFromConfig();  // 既有邏輯
var result = Defaults.MaterializeTo(definePath, MaterializeOptions.Default);  // 不覆蓋
if (result.WrittenCount > 0)
{
    // 啟 UI 後在主視窗顯示通知 / 對話框：
    // "已物化 X 個框架預設檔到 {definePath}"
}
// 繼續啟動 UI
```

### 3.2 行為說明

- 預設不覆寫：消費者自家客製不會被清
- 如果消費者真的想用 framework 最新預設重置某張表 → 透過 DefineEditor menu「重新匯入框架預設」或直接刪 `DefinePath` 對應檔再啟動
- 跨 process 與 CLI 邏輯完全一致（同一個 `Defaults.MaterializeTo`）

### 3.3 不在 Phase 3 範圍

- DefineEditor 內「show diff against framework base」功能（可作為下一個 plan）
- 把 framework base 與消費者覆寫檔的差異顯示在 UI

## 驗證計畫

### Phase 1
1. `dotnet build` 過綠（Release）；嵌入後 `Bee.Definition.dll` 增加約 50 KB（19 個 XML 嵌入）
2. 既有 ~221 測試（Bee.Business / Bee.Repository / Bee.Db / Bee.Definition）全綠
3. 新增測試：
   - `Defaults.ListEmbedded()` 回傳 19 條（精確 count）
   - `Defaults.MaterializeTo(tempDir)` 寫出 19 檔，第二次跑 `Overwrite=false` 寫 0 檔
   - `Defaults.OpenEmbedded("TableSchema/common/st_user.TableSchema.xml")` 可 deserialize 為 `TableSchema`
4. Fixture overhead 量測：跑 `Bee.Definition.UnitTests` 前後對比總時間，不應顯著增加（< 5 秒差距）

### Phase 2
1. `dotnet pack tools/Bee.Cli/Bee.Cli.csproj` 出 `.nupkg`
2. `dotnet tool install -g --add-source ./nupkgs Bee.Cli` 本機驗證
3. `dotnet bee defines materialize --path /tmp/test-out`，目視 19 檔出現
4. `dotnet bee defines list` 列出 19 條相對路徑
5. `dotnet bee --version` 顯示版本，與 `Directory.Build.props` 一致

### Phase 3
1. 開 DefineEditor，DefinePath 指向空目錄 → 開啟後該目錄出現 19 檔
2. DefinePath 指向已 materialize 過的目錄 → 跳過、不覆寫、UI 不顯示通知（或顯示 "已是最新"）
3. 客製過某張表後重啟 DefineEditor → 客製不被覆寫

## CHANGELOG

下一版 `[Unreleased]` 段：

```
### Added

- **`Bee.Definition.Defaults.MaterializeTo(path, options)`** — Public API for materialising
  the framework's default `st_*` TableSchemas plus `Department` / `Employee` FormSchema /
  FormLayout / Language resources to a target `DefinePath`. Skips existing files by default
  so consumer customisations are never overwritten.

- **`Bee.Cli` dotnet tool (`dotnet bee`)** — Framework-level CLI; ships with the `defines`
  subcommand group for materialising / listing embedded framework defaults:
  `dotnet tool install -g Bee.Cli && dotnet bee defines materialize --path ./Define`.
  Documented as the canonical way for new consumers to bootstrap their `DefinePath`.
  Reserved subcommand groups (`schema`, `tenant`, `samples`) are not implemented yet — they
  exist as a naming convention for future framework operations.

- **DefineEditor auto-materialise on startup** — When the configured `DefinePath` is missing
  framework defaults, DefineEditor materialises them on first start (via the same API as the
  CLI, in-process). Surfaces a brief notification listing what was written.
```

## Out of scope

- runtime fallback（先前討論已否決，runtime 完全不感知 embedded）
- DbDefineStorage / runtime customisation overlay 的擴充——獨立議題
- 框架升版時的 diff / merge 工具（未來 plan）
- 多語系包：目前只 ship en-US / zh-TW；其他語系等真有需求再加

## 風險

| 風險 | 緩解 |
|------|------|
| Fixture overhead 拖慢測試 | 量測後若 > 5 秒落差，改走方案 b（duplicate + build step 同步） |
| Master 搬移破壞 tests/Define 既有 hardcoded path | 19 檔搬遷 + 全 repo grep 確認沒漏 |
| DbCategorySettings 框架版與 tests 版分歧 | 兩份檔分別維護，tests 版以「framework + ft_project」描述，差異記在 plan 內 |
| `Bee.Cli` 套件名被 NuGet.org 他人占用 | 預先查；備案依序：`Bee.NET.Cli` → `Bee.Framework.Cli`。`ToolCommandName` 仍維持 `dotnet-bee`（套件名與 command 名解耦） |
| 未來 subcommand 累積後 binary 變大 / 啟動慢 | 採延遲載入 / 子命令獨立 dll 載入；初期 subcommand 少不會碰到 |
| Phase 3 啟動時意外觸發 materialize 干擾使用者 | 預設 SkipExisting，且通知對話框可關 |
