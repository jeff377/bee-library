# 計畫:合併 bee-ui-core 至 bee-library

**狀態:✅ 已完成(2026-05-22)** — bee-library 端合併完成,測試通過;
bee-ui-core repo archive(§6)由使用者後續處理。

## 1. 背景

`bee-ui-core` 與 `bee-library` 同屬 Bee.NET framework 主幹,兩 repo 都已 100% 跨平台
(net10.0、無 WinForms 相依、VS Code 可完整開發)。

### 為何合併

- **跨 repo 協調成本高**:剛完成的 `plan-deprecate-sync-api` 需 bee-library 發版 →
  bee-ui-core 升級 → 兩端各自 PR,典型案例
- **未來 MAUI 前端**會加進 bee-library;若 UI 抽象層留在另一 repo,Web/MAUI 兩個
  前端會跨兩個 repo 引用,依賴圖混亂
- **bee-ui-core 規模極小**:1 個 csproj、5 個 .cs 檔、無測試,合併幾乎無 friction
- **單人/小團隊維護**:monorepo 內 scope 略增 < cross-repo 協調成本

### 合併後的 repo 定位

```
bee-library (monorepo,VS Code 完整開發)
├── 後端:Api.Core / Api.Client / Business / Db / Repository / ...
├── 跨平台 UI 共通:Bee.UI.Core(原 bee-ui-core)
└── 前端 RCL:Blazor.Server / Blazor.Wasm / (未來) MAUI

bee-ui-winforms 或 bee-ui-xxx (獨立 repo,VS2026 開發)
└── 消費上述 NuGet 套件
```

---

## 2. 合併範圍

### 2.1 bee-ui-core 現況盤點

| 項目 | 細節 |
|------|------|
| Project | `src/Bee.UI.Core/Bee.UI.Core.csproj`(1 個) |
| 原始檔 | `ClientInfo.cs` / `EndpointStorage.cs` / `IEndpointStorage.cs` / `IUIViewService.cs` / `VersionInfo.cs`(5 個) |
| 命名空間 | `Bee.UI.Core` |
| 目標框架 | `net10.0` |
| 套件相依 | `Bee.Api.Client 4.3.0`(PackageReference) |
| 測試 | 無(`tests/` 不存在) |
| Solution | `Bee.UI.Core.slnx`(含 1 project) |
| CI | `build-ci.yml` + `nuget-publish.yml`(獨立) |
| Build props | `Directory.Build.props`(GitHub Actions pack 加速設定) |
| docs | `docs/plans/`(2 份歷史 plan) |
| README | `README.md` + `README.zh-TW.md`(雙語) |
| Git history | 9 commits(自 `2a0f1e1 Initial commit: migrate Bee.UI.Core from bee-library`) |

### 2.2 變動項目對照

| 項目 | 處理方式 |
|------|---------|
| `src/Bee.UI.Core/*.cs` | 完整移入 `bee-library/src/Bee.UI.Core/` |
| `Bee.UI.Core.csproj` | 移入後 `Bee.Api.Client` 改 `ProjectReference`(原為 PackageReference) |
| `bee-ui-core/Directory.Build.props` | **不移入**,合併到 bee-library 既有 props(避免雙重定義);若有 bee-library 缺的 pack 加速設定,擇要納入 |
| `src/Bee.UI.Core/Directory.Build.props` | **不移入**,bee-library 由 `src/Directory.Build.props` 統一管 package metadata |
| `.github/workflows/build-ci.yml` | **不移入**,由 bee-library 既有 CI 接管 |
| `.github/workflows/nuget-publish.yml` | **不移入**,需確認 bee-library 的 nuget-publish 是否會自動把 `Bee.UI.Core` 一起 pack & push(見 §3.4) |
| `Bee.UI.Core.slnx` | **不移入**,改在 `bee-library/Bee.Library.slnx` 加入 project 條目 |
| `LICENSE.txt` | **不移入**,bee-library 已有 |
| `README*.md`(root) | **不移入**,bee-library 既有 README 補一行說明含 Bee.UI.Core |
| `docs/plans/*.md`(2 份歷史) | 移入 `bee-library/docs/archive/`(已過完成階段) |
| `bee.png` | **不移入**,bee-library 應有同檔(若無再補) |
| `nupkgs/`(歷史 nupkg) | **不移入**,純歷史 artifact |

### 2.3 不在合併範圍

- bee-ui-core 的 git remote / GitHub repo 設定(由 §6 處理 archive)
- bee-ui-core 既有 NuGet.org 上的歷史套件(保留,不 unlist)
- WinForms / MAUI / 其他前端實作專案(現在不存在;有需要再開獨立 repo)

---

## 3. 合併策略

### 3.1 保留 git history:`git subtree add`

不採直接 copy & paste(會失去 git blame 與歷史 commit 連結),改用 `git subtree`:

```bash
# 在 bee-library root
git remote add bee-ui-core /Users/jeff/Desktop/repos/bee-ui-core
git fetch bee-ui-core
git subtree add --prefix=_merge-staging/bee-ui-core bee-ui-core/main --squash
```

`--squash` 把 bee-ui-core 全部歷史 squash 成單一 merge commit(保留祖先指標但不污染
bee-library 主線歷史)。**不 squash** 也是選項(完整歷史進來),但 bee-library 既有
log 會被混入大量 bee-ui-core commit,讀 `git log --oneline` 體驗變差。

**建議採 `--squash`**:
- bee-ui-core 規模小(9 commits、5 檔)
- 完整歷史在原 repo 可查
- 主線 log 乾淨

**Subtree 落點選 `_merge-staging/`** 而非直接 `src/`,因為:
- subtree add 後立即要做 file 重排(去掉 `bee-ui-core/Bee.UI.Core.slnx` 等不需檔案)、
  csproj 改 ProjectReference、Directory.Build.props 合併
- 在 staging 區做完才搬到正式位置,避免一次大 commit 把「移檔」與「修內容」混在一起

### 3.2 落地步驟

| 步驟 | 動作 | commit 顆粒 |
|------|------|------------|
| 1 | `git subtree add --prefix=_merge-staging/bee-ui-core` | 1 commit(subtree merge) |
| 2 | `git mv _merge-staging/bee-ui-core/src/Bee.UI.Core src/Bee.UI.Core` | 1 commit |
| 3 | 刪除不需檔案(`Directory.Build.props`、`.slnx`、`LICENSE.txt`、root README、`bee.png`、`nupkgs/`、`.github/`、root `Directory.Build.props`、`docs/plans/`) | 1 commit |
| 3.5 | 把 `_merge-staging/bee-ui-core/docs/plans/*` 移到 `docs/archive/bee-ui-core/` | 1 commit |
| 4 | 刪除空 `_merge-staging/` 目錄 | (併入 step 3) |
| 5 | `Bee.UI.Core.csproj`:PackageReference Bee.Api.Client → ProjectReference | 1 commit |
| 6 | `Bee.Library.slnx` 加 `<Project Path="src/Bee.UI.Core/Bee.UI.Core.csproj" />` | 1 commit |
| 7 | (本 plan §5)修正 `ClientInfo.cs:165 / :185` 兩處 `SystemApiConnector.Initialize()` | 1 commit |
| 8 | (本 plan §4)新增 `tests/Bee.UI.Core.UnitTests/` 骨架 + 至少 smoke test | 1 commit |
| 9 | `dotnet build --configuration Release` + `./test.sh` 驗證 | (無 commit) |
| 10 | 更新 `docs/dependency-map.md` 補 UI 層 | 1 commit |
| 11 | bee-ui-core repo archive(§6) | (在原 repo 操作) |

**為何拆成多個 commit**:合併 + 重排 + 內容修正若混在單一 commit,
`git blame` 會把所有行為都記到合併日,失去歷史可追溯性。拆 commit 讓未來
讀者能分辨「這檔來自 bee-ui-core」與「合併後改了什麼」。

### 3.3 ProjectReference 改寫

`src/Bee.UI.Core/Bee.UI.Core.csproj` 從:

```xml
<ItemGroup>
  <PackageReference Include="Bee.Api.Client" Version="4.3.0" />
</ItemGroup>
```

改為:

```xml
<ItemGroup>
  <ProjectReference Include="..\Bee.Api.Client\Bee.Api.Client.csproj" />
</ItemGroup>
```

副作用:
- Bee.UI.Core 直接拿到 monorepo 內最新的 Bee.Api.Client(已無 sync method),
  §5 的 `ClientInfo.cs` 修正必須一起做,否則 build fail
- pack 時 ProjectReference 預設會在 nuspec 內加上 Bee.Api.Client 套件相依
  (而非把 Bee.Api.Client 內容打進 Bee.UI.Core),版本號隨 bee-library 統一發版

### 3.4 NuGet Publish 設定(已盤點)

`bee-library/.github/workflows/nuget-publish.yml` 是**列舉式**(非 glob),分三個區塊都列出
csproj 清單,需要在三處加入 `Bee.UI.Core`:

1. **Build 區塊**(L36-L47):加 `dotnet build src/Bee.UI.Core/Bee.UI.Core.csproj --configuration Release --no-restore /p:Version=$ver`
2. **Pack 區塊**(L53-L64):加 `dotnet pack src/Bee.UI.Core/Bee.UI.Core.csproj --configuration Release --output ./nupkgs --no-build /p:Version=$ver`
3. **Release notes body**(L94-L105):加 `- Bee.UI.Core`

順序建議放在 `Bee.Api.Client` 之後(因為 Bee.UI.Core 依賴 Bee.Api.Client)。

> **附帶觀察**:既有 workflow 也未涵蓋已合入的 `Bee.Web.Blazor.Server` / `Bee.Web.Blazor.Wasm`。
> 不在本 plan 範圍處理(workflow 補完整可獨立 follow-up);本 plan 只新增 `Bee.UI.Core` 相關行,
> 避免 scope creep。

關於版本號:`/p:Version=$ver` 由 tag 名稱(去掉 `v`)決定,會 override `Directory.Build.props` 的
`<Version>`,符合 `~/.claude/rules/releasing.md` 規範。`Bee.UI.Core.csproj` 不需自訂 Version,
跟隨 monorepo 統一發版。

### 3.5 命名與版本

- **NuGet package id**:維持 `Bee.UI.Core`(下游消費者不需改套件名,僅升版本即可)
- **AssemblyVersion / PackageVersion**:跟 bee-library 走(目前 4.4.0)。
  下次發版時 `Bee.UI.Core` 直接從 4.3.0 跳到 bee-library 對齊的下一版(例如 4.5.0)
- **CHANGELOG**:由發版時 `/changelog-draft` 統整大綱,於條目註明「`Bee.UI.Core` 自此版起
  併入 bee-library monorepo 發佈,套件名稱與 namespace 不變」

---

## 4. 補測試骨架(Bee.UI.Core.UnitTests)

bee-ui-core 原本無測試。合併後在 bee-library 規範下,各 `src/<Module>` 必有對應
`tests/<Module>.UnitTests`(`.claude/rules/testing.md`§測試專案對應)。

### 4.1 至少需有的骨架

```
tests/Bee.UI.Core.UnitTests/
├── Bee.UI.Core.UnitTests.csproj
└── ClientInfoTests.cs(至少 1-2 條 smoke 測試)
```

### 4.2 建議的 smoke test 範圍

`ClientInfo` 多為靜態 + 與 `ApiClientInfo` / `IUIViewService` 緊耦合,深度測試需
mock UIViewService 並挑戰靜態狀態 race。本計畫**只補最低需要**:

| 測試 | 驗證點 |
|------|--------|
| `EndpointStorage_DefaultInstance_IsEndpointStorage` | `ClientInfo.EndpointStorage` 預設為 `EndpointStorage` 實例 |
| `ClientSettings_NoFile_ReturnsEmptySettings` | 沒有 `{ExeName}.Settings.xml` 時回傳新建空白 ClientSettings(不拋例外) |
| `EndpointStorage_LoadEndpoint_PersistsAndReturns`(實作測試 IEndpointStorage 預設實作的 round-trip,需用 temp 路徑) | 寫入 endpoint → 讀回 → 值相同 |

> 完整測試覆蓋(`Initialize` / `SetEndpoint` / `ApplyLoginResult` 等)留待後續獨立任務,
> **不在本 plan 範圍**。

### 4.3 csproj 樣板(對照既有 `Bee.Api.Client.UnitTests.csproj`)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="coverlet.collector" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Bee.UI.Core\Bee.UI.Core.csproj" />
    <ProjectReference Include="..\Bee.Tests.Shared\Bee.Tests.Shared.csproj" />
  </ItemGroup>
</Project>
```

(具體 PackageReference 版本由 bee-library 既有 Central Package Management 統一)

---

## 5. 修正 `ClientInfo.cs` 兩處 `SystemApiConnector.Initialize()` 呼叫

### 5.1 背景(承 `plan-deprecate-sync-api` §3.2)

bee-library v4.x 起 `SystemApiConnector.Initialize()` 同步方法已移除,
合併後 `ClientInfo.cs` 內兩處呼叫會 build fail。本 plan 一併修正。

### 5.2 改寫 pattern(plan-deprecate-sync-api §9.2 推薦的選項 B)

維持 `ClientInfo.Initialize` 對外為同步 API(UI thread 介面不變),
在內部用 `SyncExecutor.Run` 局部包覆 async 呼叫。

**[src/Bee.UI.Core/ClientInfo.cs:165](../../src/Bee.UI.Core/ClientInfo.cs#L165)** (`SetEndpoint` 內):

```csharp
// 改寫前
SystemApiConnector.Initialize();

// 改寫後
SyncExecutor.Run(() => SystemApiConnector.InitializeAsync());
```

**[src/Bee.UI.Core/ClientInfo.cs:185](../../src/Bee.UI.Core/ClientInfo.cs#L185)** (`InitializeConnect` 內):同上改寫。

### 5.3 using 補充

`ClientInfo.cs` 已 `using Bee.Api.Client;`(line 1),`SyncExecutor` 直接可用,
無需新增 using。

### 5.4 例外語意

`SyncExecutor.Run` 透過 `Task.Run(...).GetAwaiter().GetResult()` 解包原例外,
不會包成 `AggregateException`。`InitializeConnect:188` 既有 `try/catch` 區塊
不需調整(其抓的 generic `Exception` 仍能 catch 到 `InitializeAsync` 內拋的
原例外型別)。

`SetEndpoint:165` 沒有 try/catch,例外會 propagate 給呼叫端,行為與改寫前相同。

---

## 6. bee-ui-core repo 後處置

合併 PR / commit 到 bee-library main 後:

1. `bee-ui-core/README.md` 與 `README.zh-TW.md` 頂部加 archive notice:
   ```markdown
   > **🗄️ Archived (YYYY-MM-DD)**:此 repo 已併入
   > [bee-library](https://github.com/jeff377/bee-library),套件名稱 `Bee.UI.Core` 不變,
   > 自 bee-library v?.?.? 起改由 bee-library 發佈。
   > 後續維護於 bee-library;此 repo 保留歷史參考,不再更新。
   ```
2. commit & push 到 bee-ui-core main
3. GitHub 上將 repo 設為 **archived**(Settings → Danger Zone → Archive)
4. **NuGet.org 上的歷史套件保留不 unlist**(下游消費者仍可 install 舊版)

---

## 7. 風險與緩解

### 7.1 NuGet 套件斷層

**風險**:bee-ui-core 最後一版是 4.3.0,bee-library 下一版可能是 4.5.0,
中間 4.4.0 對 `Bee.UI.Core` 不存在,造成版本不連續。

**緩解**:CHANGELOG 明確標示「v4.3.0 → v4.5.0 因 repo 合併跳號,套件內容無破壞性變更」。

### 7.2 ProjectReference 後相依升級

**風險**:`Bee.UI.Core` 改 ProjectReference 後,等同自動跟 `Bee.Api.Client` 最新版,
失去舊 PackageReference 的「版本鎖定」效果。

**緩解**:這正是合併的目的(避免跨 repo 版本不同步);發版時整個 monorepo 共享同一
`<Version>`,版本邊界清晰。

### 7.3 ClientInfo 靜態狀態 race

**風險**:`ClientInfo` 全靜態,測試時可能與其他靜態相關測試(如 `ApiClientInfo`)race。

**緩解**:§4.2 的 smoke test 不碰 `Initialize` / `SetEndpoint` 等會 mutate static state 的
路徑,只測 default getter,避免 race。深度測試的 collection 序列化由後續獨立任務處理。

### 7.4 合併過程中 build 暫時失敗

**風險**:Step 5(改 ProjectReference)後到 Step 7(修 sync 呼叫)前的中間狀態,
build 會 fail。

**緩解**:Step 5-7 在**單一 PR / 連續 commit** 內完成,不留中間態 push 到 main。
若採直接 main 工作流,本機跑完 Step 7 後才 push。

### 7.5 bee-ui-core 上有未合併的 PR / branch

**風險**:bee-ui-core 還有 in-flight 分支未合入 main,合併時錯失。

**緩解**:合併前 `cd /Users/jeff/Desktop/repos/bee-ui-core && git branch -a && git log origin/main..HEAD`
盤點本機與遠端所有 branch,確認 main 已是最終狀態;不確定的分支於 §6 archive 前明確處理。

---

## 8. 不在本 plan 範圍

- 任何**新功能**(僅做合併 + 必要的 sync→async 修正)
- `ClientInfo` 深度測試覆蓋(只補 §4.2 smoke,完整覆蓋留後續)
- `ClientInfo` 內部重構(去除靜態狀態、async 化整個 API 等)
- MAUI 前端專案新增(未來獨立 plan)
- WinForms repo 建立(需要時獨立處理)
- bee-library 既有 Directory.Build.props / CI workflow 大幅重構(只做合併必要的微調)
- CHANGELOG 撰寫(由 `/changelog-draft` 在發版時處理)

---

## 9. Checklist

實作收尾時逐項勾選:

**合併準備**
- [ ] 確認 bee-ui-core `main` 為最終狀態,無未合 branch / PR
- [ ] 確認 `bee-library/.github/workflows/nuget-publish.yml` 對新 csproj 的處理機制(§3.4)

**Subtree 合併**
- [ ] `git remote add bee-ui-core` + `git subtree add --prefix=_merge-staging/bee-ui-core --squash`
- [ ] 把 `_merge-staging/bee-ui-core/src/Bee.UI.Core/` 移到 `src/Bee.UI.Core/`
- [ ] 把 `_merge-staging/bee-ui-core/docs/plans/*.md` 移到 `docs/archive/bee-ui-core/`
- [ ] 刪除 `_merge-staging/` 內其餘檔案與目錄

**Project 設定**
- [ ] `Bee.UI.Core.csproj`:`PackageReference Bee.Api.Client` → `ProjectReference`
- [ ] 刪除 `src/Bee.UI.Core/Directory.Build.props`(若存在,讓 bee-library 統一管)
- [ ] `Bee.Library.slnx` 加 `Bee.UI.Core` project 條目(`src/` 區塊)
- [ ] `Bee.Library.slnx` 加 `Bee.UI.Core.UnitTests` project 條目(`tests/` 區塊)
- [ ] `.github/workflows/nuget-publish.yml` 三處加 `Bee.UI.Core`(build / pack / release notes)

**ClientInfo 修正(承 plan-deprecate-sync-api §3.2)**
- [ ] `src/Bee.UI.Core/ClientInfo.cs:165` `SystemApiConnector.Initialize()` → `SyncExecutor.Run(() => SystemApiConnector.InitializeAsync())`
- [ ] `src/Bee.UI.Core/ClientInfo.cs:185` 同上改寫

**測試骨架**
- [ ] 建 `tests/Bee.UI.Core.UnitTests/Bee.UI.Core.UnitTests.csproj`
- [ ] 加至少 2 條 smoke test(§4.2)
- [ ] csproj 樣板對齊既有 test project 風格

**驗證**
- [ ] `dotnet build --configuration Release` 0 警告 0 錯誤
- [ ] `./test.sh` 全部 pass
- [ ] 新測試專案有實際 pass 數(非 0)

**文件**
- [ ] 更新 `docs/dependency-map.md` 補 `Bee.UI.Core` 層
- [ ] `docs/archive/bee-ui-core/` 內歷史 plan 加 README 註明來源

**bee-ui-core repo archive**
- [ ] bee-ui-core README 加 archive notice(雙語)
- [ ] commit & push to bee-ui-core main
- [ ] GitHub 設定改 archived

**Plan 收尾**
- [ ] 本文件頂部狀態列改 `**狀態:✅ 已完成(YYYY-MM-DD)**`
