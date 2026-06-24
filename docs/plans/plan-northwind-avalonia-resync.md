# 計畫：bee-northwind-avalonia 重新同步 + 新增 WASM Browser head

**狀態：📝 擬定中（2026-06-24）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 0 | bee-library 發布框架 v4.11.0（含 WASM/async src 變更） | 📝 待做 |
| 1 | avalonia repo 全量重新同步 UI/Desktop/Server/Define，套件 bump 4.10.0→4.11.0 | 📝 待做 |
| 2 | 新增 Bee.Northwind.Browser head（複製 + 加入 slnx） | 📝 待做 |
| 3 | 文件同步（top-level README 雙語加 Web 案例、wasm-tools 前置、launch 設定） | 📝 待做 |
| 4 | 驗證（三 head build + Desktop/Browser 冒煙）→ commit + push | 📝 待做 |

## 背景

- `apps/Bee.Northwind/` 是 bee-library 內走 `ProjectReference`、隨框架同步演進的 dogfooding demo。
- 2026-06-15 曾「畢業」複製一份至獨立 repo [bee-northwind-avalonia](https://github.com/jeff377/bee-northwind-avalonia)（commit `23d782b`），把 `ProjectReference` 改為 `PackageReference`（純 NuGet `4.10.0`），作為「外部使用者視角」的快照證明。**bee-library 內那份保留不刪**（見 memory `northwind-graduation-keep-source`）。
- 之後 `plan-northwind-web.md` 階段 1–6（✅ 2026-06-23）為 in-repo demo 新增了 **Avalonia Browser (WASM) head** `Bee.Northwind.Browser`，並連帶改動了框架 src 與 demo UI。
- 本計畫即「再做一次搬移」：把累積變更與新的 Browser head 同步進獨立 repo。

### 為何不是「只加 Browser 專案」

`diff -rq` 比對顯示 in-repo demo 自 4.10.0 後已分歧（非僅新增 Browser）：

- **UI**：`App.axaml.cs`、`ViewModels/ConnectionViewModel.cs`、`ViewModels/FormsViewModel.cs` 已 async 化；新增 `Views/MainView.axaml(.cs)`（WASM single-view 用）；`Views/MainWindow.axaml` 變動。
- **Server**：`Program.cs`、`Bee.Northwind.Server.csproj`、`northwind.db` 變動。

Browser head 依賴 async 化後的 UI 與框架 `OverlayDialogHost`（`OperatingSystem.IsBrowser()` 疊層分支），**舊快照 UI 無法支撐**。因此採「**全量重新同步 + 加 Browser**」。

### 為何必須先發 v4.11.0

Browser head 依賴的框架變更全部落在 `v4.10.0` 之後（`git log v4.10.0..HEAD -- src/`）：

| commit | 變更 | Browser 依賴點 |
|--------|------|---------------|
| `d9400c5a` | feat(client)!：前端↔後端統一 async、移除 SyncExecutor | WASM 不可 sync-over-async（[[wasm-client-async-init]]） |
| `ce9eb291` | feat(ui-avalonia)：對話框加 OverlayLayer 疊層（WASM 無多視窗） | LookupDialog/RowEditDialog 的 browser 分支 |
| `4cbff4a8` `0efaeeec` `e1672634` 等 | UI-Avalonia 編輯管線 / 事件補強 | async UI 行為 |

published `Bee.UI.Avalonia 4.10.0` 不含上述，獨立 repo 若維持 4.10.0 會 build 失敗。故**階段 0 先在 bee-library 發 v4.11.0**，獨立 repo 三個 head 全部 bump 至 `4.11.0`。

## 範圍與決策（已確認）

| 決策 | 結論 |
|------|------|
| 搬移範圍 | 全量重新同步 UI/Desktop/Server/Define + 新增 Browser |
| 框架套件版本 | 先發 v4.11.0（階段 0），再搬移（階段 1+） |
| bee-library 內 demo | 保留，不在此計畫處理 |

## 階段 0：bee-library 發布 v4.11.0

依 `~/.claude/rules/releasing.md` 流程：

1. `/changelog-draft` 從 `v4.10.0` 至 HEAD 整理雙語 CHANGELOG 大綱，使用者 review 後 commit。
2. 確認 `build-ci.yml` 在 main 上通過。
3. 更新 `src/Directory.Build.props` 的 `<Version>`/`<AssemblyVersion>`/`<FileVersion>` → `4.11.0`，commit & push。
4. 打 tag：`git tag v4.11.0 && git push origin v4.11.0`。
5. 確認 `nuget-publish.yml` 成功，`Bee.UI.Avalonia 4.11.0`（及其他 `Bee.*`）上架 NuGet.org。

**驗收**：NuGet.org 可見 `4.11.0`，`dotnet restore` 在獨立 repo 能解析到。

## 階段 1：avalonia repo 全量重新同步

目標 repo：`/Users/jeff/Desktop/repos/bee-northwind-avalonia`（已 clone，`origin` = GitHub repo，branch `main`）。

來源：`apps/Bee.Northwind/`（bee-library）。

1. **覆蓋複製**下列目錄（連同已分歧檔案）：
   - `Bee.Northwind.UI/`（含新增 `Views/MainView.axaml(.cs)`）
   - `Bee.Northwind.Server/`
   - `Bee.Northwind.Desktop/`
   - `Define/`
   - **排除** `bin/`、`obj/`（.gitignore 已涵蓋）。
2. **重套 ProjectReference→PackageReference 轉換**（複製會把 in-repo 的 src ProjectReference 帶回，必須改回）：
   - `Bee.Northwind.UI.csproj`：`<ProjectReference ...\src\Bee.UI.Avalonia\...>` → `<PackageReference Include="Bee.UI.Avalonia" Version="4.11.0" />`。
   - 其餘第三方套件版本維持與 in-repo 一致（Avalonia 12.0.4 等）。
3. **套件版本 bump**：repo 內所有 `Bee.*` PackageReference `4.10.0` → `4.11.0`（UI、Desktop 透過 UI 傳遞、Server 自身的 `Bee.*` 引用）。
   - 確認方式：`grep -rn 'Version="4.10.0"' --include=*.csproj`，全部改 `4.11.0`。

**驗收**：`dotnet build Bee.Northwind.Desktop`、`Bee.Northwind.Server` 在獨立 repo 通過（純 NuGet）。

## 階段 2：新增 Bee.Northwind.Browser head

1. **複製** `apps/Bee.Northwind/Bee.Northwind.Browser/` 全部（排除 `bin/`、`obj/`）：
   - `Bee.Northwind.Browser.csproj`、`Program.cs`、`runtimeconfig.template.json`
   - `Properties/launchSettings.json`
   - `Storage/BrowserLocalStorageEndpointStorage.cs`
   - `wwwroot/index.html`、`main.js`、`app.css`
   - `README.md`
2. **csproj 確認**：Browser csproj **不含** src ProjectReference（只有 `Avalonia.Browser` 套件 + `ProjectReference ..\Bee.Northwind.UI`），**可原樣移植**。`Bee.Api.Client` / `Bee.UI.Core` 經 UI 的 `Bee.UI.Avalonia` 套件傳遞，無需另加 PackageReference。
   - 必保留 `<JsonSerializerIsReflectionEnabledByDefault>true</JsonSerializerIsReflectionEnabledByDefault>` 與 `<PublishTrimmed>false</PublishTrimmed>`（[[northwind-web-avalonia-wasm]]）。
3. **加入 slnx**：在 `Bee.Northwind.slnx` 的 `/UI/` folder 加：
   ```xml
   <Project Path="Bee.Northwind.Browser/Bee.Northwind.Browser.csproj" />
   ```

**驗收**：`dotnet build Bee.Northwind.Browser`（需 `wasm-tools` workload）通過。

## 階段 3：文件同步

1. **top-level `README.md` / `README.zh-TW.md`**（雙語同步）：補上 Web (WASM) 案例段落，與 Desktop 對稱描述（`.UseBrowser` vs `.UseDesktop`）、localStorage endpoint、`PublishTrimmed=false` 包體說明。比照 in-repo `apps/Bee.Northwind/README*.md` 既有的 Web 段落改寫。
2. **wasm-tools 前置**：README「執行方式」加 `sudo dotnet workload install wasm-tools` 前置步驟，並標明 Browser head 跑 Debug。
3. **`.vscode/`**：若 in-repo `.vscode` / `launch.json` 有 northwind-server / browser 設定（commit `93372a1a` 加過），對應同步進獨立 repo（路徑改為 repo 根相對）。
4. **`.smoke.yaml`**（選配）：如要納入 Browser 冒煙，新增對應步驟；否則維持 Desktop 冒煙即可。

## 階段 4：驗證與提交

1. **Build 三 head**：`dotnet build` 通過 Desktop / Server / Browser（純 NuGet `4.11.0`）。
2. **冒煙**：
   - Desktop：啟動 Server → Desktop connect → login(demo/demo) → 開表單。
   - Browser：`dotnet run`（或 preview）→ WASM 載入 → connect → login → Forms 選單 → 開表單 → lookup overlay（[[northwind-web-avalonia-wasm]] 記載 preview headless 點擊限制，UI 互動以真實瀏覽器驗）。
3. **Commit + push**（獨立 repo 直接 main，比照畢業 commit 風格）：
   - 例：`feat: 新增 Avalonia WASM Web head + 同步至框架 4.11.0`。
   - `git push origin main`。

## 風險與注意

- **獨立 repo 無 CI build gate**：畢業 repo 是否有 `build-ci.yml` 待確認；若無，build/冒煙驗證在本機完成即提交。
- **wasm-tools workload**：本機需先裝（`sudo dotnet workload install wasm-tools`），否則 Browser build 失敗。
- **覆蓋複製勿帶回 src ProjectReference**：階段 1 步驟 2 是最易遺漏的退件點——複製 UI csproj 後務必改回 PackageReference + bump 版本。
- **bee-library 內 `apps/Bee.Northwind` 不動**：本計畫只寫獨立 repo，不在 bee-library 內 `git rm`（保留 dogfooding，見 memory）。
- **發版與搬移解耦**：階段 0 未完成（NuGet 上沒有 4.11.0）前，階段 1+ 無法 restore，順序不可顛倒。
