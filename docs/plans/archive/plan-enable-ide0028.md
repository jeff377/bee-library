# 計畫：啟用 IDE0028（使用集合初始化運算式）於編譯期檢查

**狀態：✅ 已完成（2026-04-20）**

## 背景

SonarCloud 目前在 `jeff377_bee-library` 標記 89 個 `external_roslyn:IDE0028` 的 code smell（嚴重度 INFO）——Roslyn analyzer 發現可用 collection initializer 或 collection expression 替代的寫法，例如：

```csharp
// 觸發 IDE0028：
var list = new List<int>();
list.Add(1);
list.Add(2);

// 建議：
List<int> list = [1, 2];          // collection expression (C# 12+)
// 或 collection initializer：
var list = new List<int> { 1, 2 };
```

這類修正屬於機械式 style 改善，**比靠 `/sonar-fix` 以提示詞修更穩、更快**，應仿前例 [plan-enable-ide0005.md](plan-enable-ide0005.md) 將其納入編譯期把關：一次清完現存 89 個、於 `.editorconfig` 設 `warning`，後續由 `TreatWarningsAsErrors=true` 直接當 error 阻擋。

相關脈絡：`/sonar-fix` skill（[.claude/commands/sonar-fix.md](../../.claude/commands/sonar-fix.md)，未同步至 GitHub）原本會因 `IDE0028` 不在 `.claude/rules/sonarcloud.md` 而一律寫入 skip list。編譯期把關後，這類 issue 就不會再進 SonarCloud，`/sonar-fix` 可專注於真正的 Sonar S 系列規則與覆蓋率。

## 目標

- 在編譯期偵測並失敗 `List.Add` 連寫等可 collection-initializer 的寫法
- 現有 89 個 issue 一次清完，後續由 CI 自動把關
- 不影響 `dotnet pack` 產物與現有 warning 政策
- SonarCloud 上 `IDE0028` 的 open issue 歸零（並連帶 leak period 那 29 個）

## 啟用條件

與 IDE0005 共用基礎設施，三者皆已具備，只需新增 severity 指定：

| 條件 | 現況 |
|------|------|
| `EnforceCodeStyleInBuild=true` | ✅ 已於 IDE0005 啟用時設定（src/ 與 tests/ 的 Directory.Build.props） |
| `GenerateDocumentationFile=true` | ✅ 已具備 |
| `.editorconfig` 設 `dotnet_diagnostic.IDE0028.severity = warning` | ❌ 本計畫新增 |

## 變更範圍

### 設定檔變更

- **[.editorconfig](../../.editorconfig)**：於 `[*.cs]` 區塊既有 `IDE0005` 設定下方新增：
  ```ini
  dotnet_diagnostic.IDE0028.severity = warning
  ```

### 程式碼變更

依 SonarCloud 當前資料（2026-04-20 快照），89 個 issue 分布：

| 層級 | Issue 數 | 說明 |
|------|---------|------|
| `src/` | 49 | 主要集中在 `Bee.Api.Core/MessagePack/Serializable*.cs`、`Bee.Base/Serialization/DataSetJsonConverter.cs`、`Bee.Definition/**` 等 |
| `tests/` | 40 | 主要在 `tests/Bee.*/Filters/FilterNodeCollectionJsonConverter*.cs`、`tests/Bee.*/Collections/MessagePackCollection*.cs` |

仿 IDE0005 分模組 commit，粒度便於回退。

### 工具

- 主要用 `dotnet format analyzers --severity info --diagnostic IDE0028 <proj>` 自動修正
- 若工具修後仍有殘留（例如 `dotnet format` 對某些邊界情境不動），人工補修

## 執行步驟

### Step 1：加設定檔（先不 push）

1. 修改 `.editorconfig` 加 `dotnet_diagnostic.IDE0028.severity = warning`
2. 此時全 solution `dotnet build -c Release` 會 fail（89 個 error），進入下一步

### Step 2：src/ 依相依順序逐專案清理

依 [dependency-map.md](../dependency-map.md) 由底層往上，對每個受影響專案執行：

```bash
dotnet format analyzers src/<Project>/<Project>.csproj --diagnostics IDE0028 --severity info
dotnet build src/<Project>/<Project>.csproj -c Release
# 通過後
git add src/<Project>/
git commit -m "chore(<module>): 套用集合初始化 (IDE0028)"
```

預計涉及專案（依 SonarCloud 資料初判，實際以 `dotnet format` 輸出為準）：

| 順序 | 專案 |
|------|------|
| 1 | Bee.Base |
| 2 | Bee.Definition |
| 3 | Bee.Db |
| 4 | Bee.Api.Core |
| 5 | Bee.Api.Client |
| 6 | Bee.Business（視是否觸發）|

> 若某專案執行後無變動，表示 issue 已被 `dotnet format` 合併於上游專案的修正中，跳過即可（不產生空 commit）。

### Step 3：tests/ 專案逐一清理

對 `tests/Bee.*.UnitTests`、`tests/Bee.Tests.Shared` 以相同方式處理，commit 訊息：
`chore(tests): 套用集合初始化 (IDE0028)`（或按模組拆更細）。

### Step 4：整體驗證（本機）

```bash
dotnet build --configuration Release
dotnet test --configuration Release --settings .runsettings
```

收尾 commit：`chore: 啟用 IDE0028 編譯期檢查`（設定檔）；若有零星殘留一併補上。

### Step 5：一次推送 main，監測 CI

**所有 commit 全部在本機完成並通過驗證後**，才一次 `git push origin main`；期間不做任何 push，避免中間狀態把 CI 打爆。

push 後依 [pull-request.md](../../.claude/rules/pull-request.md) 觀察 `build-ci.yml` 與 SonarCloud；失敗以後續 commit 修復。CI 通過後：

- 重新查 SonarCloud：`IDE0028` open issue 應歸零
- 若需要，以 `/sonar-fix --mode=daily`（或等 trigger 自動跑）更新 `docs/.sonar-fix-state/snapshot.json`

## 風險與對策

| 風險 | 對策 |
|------|------|
| `dotnet format` 修成 collection expression `[x, y]` 後，過載解析走到不同路徑（例如 `IEnumerable<T>` vs `params T[]`） | 每個專案修完立即 `dotnet build` + `dotnet test` 驗證，不通過就回退（`git restore`）並人工修 |
| 某些 `List.Add` 之間有條件分支或副作用，不能合併為初始化 | `dotnet format` 通常不會動這類；若動到，測試會發現，人工回退 |
| MessagePack / JsonConverter 類別對序列化行為敏感 | 上述兩類被本 plan 涵蓋較多，修後必跑對應 `tests/Bee.Api.Core.UnitTests` 與 `tests/Bee.Base.UnitTests` |
| IDE 自動修正跟本規則衝突 | 不衝突——規則一致，IDE 會提示同樣修法 |

## 驗收標準

- [ ] `dotnet build -c Release` 通過
- [ ] `dotnet test -c Release --settings .runsettings` 通過（DB 相依測試按環境跳過為正常）
- [ ] 手動於任一 `.cs` 檔寫 `var list = new List<int>(); list.Add(1);` 並 `dotnet build`，預期失敗並列出 IDE0028
- [ ] `build-ci.yml` 在 main 上通過
- [ ] SonarCloud 重新掃描後 `external_roslyn:IDE0028` open issue 數 = 0

## 回退方式

1. 若某個 commit 導致 test fail 或 runtime 行為改變：單獨 `git revert <sha>`，該專案回到修前狀態
2. 若要完全停用：revert 設定 commit（`啟用 IDE0028 編譯期檢查`），保留 code 層面的改寫（無害）

## 後續

- 落地後將「`.editorconfig` 已設 IDE0028=warning」補進 `.claude/rules/code-style.md` 的「語言特性」段
- 評估是否一併納管其他常見 IDE 規則（如 IDE0090 `new()`、IDE0300 collection expression 系列、IDE0044 make field readonly），若數量不多可合併為單一後續 plan
- `/sonar-fix` skip list 目前為空，無清理動作；未來若 sonarcloud.md 擴充 S 系列規則，才需 sweep
