# 計畫：啟用 IDE0005（移除多餘 using）於編譯期檢查

**狀態：✅ 已完成（2026-04-20）**

## 背景

目前 IDE0005（Remove Unnecessary Using Directives）僅在 IDE 中以建議顯示，build pipeline 不會把關。新 PR 常夾帶未使用的 using，累積成 code smell。專案已啟用 `TreatWarningsAsErrors=true`，若能讓 IDE0005 在 build 時產生 warning，即可直接轉為 error 阻擋合併。

## 目標

- 在編譯期（`dotnet build`、CI）能偵測並失敗 unused using
- 現有程式碼一次清完，後續由 CI 自動把關
- 不影響 `dotnet pack` 產物與現有 warning 政策

## 啟用條件（三者缺一不可）

1. `.editorconfig` 指定 `dotnet_diagnostic.IDE0005.severity = warning`
2. csproj / Directory.Build.props 啟用 `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`
3. `<GenerateDocumentationFile>true</GenerateDocumentationFile>`（已具備，src/Directory.Build.props 已設）

## 變更範圍

### 設定檔變更

- **[.editorconfig](../.editorconfig)**：於 `[*.cs]` 區塊新增
  ```ini
  dotnet_diagnostic.IDE0005.severity = warning
  ```

- **[src/Directory.Build.props](../../src/Directory.Build.props)**：於「健康檢查」區塊新增
  ```xml
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  ```

- **tests/**：無 `Directory.Build.props`，不會自動繼承 src 設定。本計畫同步建立 `tests/Directory.Build.props` 啟用相同設定，讓測試專案也受管控。
  - tests 專案未啟用 `GenerateDocumentationFile`，需一併啟用以讓 IDE0005 在編譯期生效
  - 或改用替代規則 `CS8019`（unused using 的 compiler warning）；本計畫採 IDE0005 路線以維持一致性

### 程式碼變更

分 12 次 commit，每個專案一次，粒度便於回退。

## 執行步驟

### Step 1：加設定檔（先不 push）

1. 修改 `.editorconfig`、`src/Directory.Build.props`
2. 新增 `tests/Directory.Build.props`
3. 此時 solution 整體會 build fail，進入下一步

### Step 2：src/ 依相依順序逐專案清理

依 [dependency-map.md](../dependency-map.md) 由底層往上：

| 順序 | 專案 | 命令 |
|------|------|------|
| 1 | Bee.Base | `dotnet format src/Bee.Base/Bee.Base.csproj --diagnostics IDE0005 --severity warn` |
| 2 | Bee.Definition | 同上 |
| 3 | Bee.ObjectCaching | 同上 |
| 4 | Bee.Db | 同上 |
| 5 | Bee.Repository.Abstractions | 同上 |
| 6 | Bee.Api.Contracts | 同上 |
| 7 | Bee.Repository | 同上 |
| 8 | Bee.Business | 同上 |
| 9 | Bee.Api.Core | 同上 |
| 10 | Bee.Api.AspNetCore | 同上 |
| 11 | Bee.Api.Client | 同上 |

每個專案執行後：
1. `dotnet build <proj> -c Release` 驗證通過
2. `git add` + commit（訊息格式：`chore(<module>): 移除多餘 using 指令 (IDE0005)`）

### Step 3：tests/ 專案逐一清理

對應 9 個測試專案 + `Bee.Tests.Shared`，同樣以 `dotnet format --diagnostics IDE0005 --severity warn` 批次移除。

### Step 4：整體驗證（本機）

```bash
dotnet build --configuration Release
dotnet test --configuration Release --settings .runsettings
```

通過後分 2 個收尾 commit：
- `chore: 啟用 IDE0005 編譯期檢查`（設定檔）
- 若前面漏清的零星殘留

### Step 5：一次推送 main，監測 CI

**所有 commit 全部在本機完成並通過驗證後，才一次 `git push origin main`**；期間不做任何 push，避免中間狀態（設定已啟用但 src 尚未清完）把 CI 打爆。

push 後依 `pull-request.md` 規範觀察 `build-ci.yml` 與 SonarCloud 結果，失敗立即以後續 commit 修復。

## 風險與對策

| 風險 | 對策 |
|------|------|
| `dotnet format` 誤刪條件編譯區塊內 using | 本專案全 `net10.0` 單一 TFM，無 `#if` TFM 分支，風險低；若有需人工檢查 |
| tests 啟用 `GenerateDocumentationFile` 產生大量 CS1591（缺 XML doc 警告） | 於 tests/Directory.Build.props 加 `<NoWarn>$(NoWarn);CS1591</NoWarn>` |
| IDE 自動加 using 與本規則衝突 | 規則只檢查「未使用」，IDE 加入的 using 只要有實際引用就不會被標記 |
| 清理過程中其他開發者同時修改 | 本計畫預計單人連續完成；若被插斷，已 commit 的專案不受影響，未完成的回 rebase |

## 驗收標準

- [ ] `dotnet build -c Release` 通過
- [ ] `dotnet test -c Release --settings .runsettings` 通過（資料庫相依測試依環境跳過為正常）
- [ ] 手動於任一 `.cs` 檔加入 `using System.Text;`（未使用）並 `dotnet build`，預期 build 失敗並列出 IDE0005
- [ ] `build-ci.yml` 在 main 上通過
- [ ] SonarCloud 無新增 issue（IDE0005 為 Roslyn 規則，SonarCloud 不會重複報）

## 回退方式

若啟用後產生無法預期的副作用：

1. revert 最後一個「啟用 IDE0005」的設定 commit
2. 保留前面逐專案清 using 的 commits（那些是純粹的 code 改善，無害）

## 後續

- 規則落地後將「`.editorconfig` 已設 IDE0005=warning」加入 `.claude/rules/sonarcloud.md` 或 `code-style.md`，避免被誤改回 silent
- 觀察是否擴張到其他 IDE 規則（如 IDE0044 make field readonly、IDE0051 unused private member），評估逐一納管
