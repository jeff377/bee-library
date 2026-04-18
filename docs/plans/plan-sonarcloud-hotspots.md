# 計畫：處理 SonarCloud Security Hotspots

**狀態：🚧 進行中**

## 背景

SonarCloud 對 `jeff377_bee-library` 目前揭露 **9 個 security hotspots**（1 個已 REVIEWED/SAFE，8 個 TO_REVIEW）。本次一次性清理，避免 hotspot 堆積淹沒新掃描的訊號，並強化 GitHub Actions 供應鏈。

預期結果：

- 所有 8 個 TO_REVIEW hotspot 變更為 REVIEWED（SAFE 或 FIXED）。
- `nuget-publish.yml` 的 secrets 以 step-level `env:` 注入。
- 兩個 workflow 的第三方 actions 全部 pin 至完整 commit SHA。
- 4 個 Regex 呼叫加上 `TimeSpan.FromSeconds(1)`，從根源消除 S6444。
- `.claude/rules/sonarcloud.md` 新增本次規則（S6444 / S5766 / S7636 / S7637）。

---

## Hotspot 清冊與處置

| # | 位置 | 規則 | 類別 | 處置 |
|---|------|------|------|------|
| 1 | `src/Bee.Api.AspNetCore/Controllers/ApiServiceController.cs:32` | S5693 | DoS | ✅ 已 SAFE（`[RequestSizeLimit(10MB)]` 已生效）— 不動 |
| 2 | `src/Bee.Base/FileFunc.cs:170` | S6444 | ReDoS | 🔧 加 timeout |
| 3 | `src/Bee.Base/StrFunc.cs:160`（`Replace`） | S6444 | ReDoS | 🔧 加 timeout |
| 4 | `src/Bee.Base/StrFunc.cs:515`（`Like`） | S6444 | ReDoS | 🔧 加 timeout（風險最高，手動改通配符） |
| 5 | `src/Bee.Db/DbCommandSpec.cs:24`（`PlaceholderRegex`） | S6444 | ReDoS | 🔧 `new Regex` 加 timeout |
| 6 | `src/Bee.Base/ApiException.cs:29`（建構子） | S5766 | object-injection | 🏷️ Sonar UI 標記 SAFE |
| 7 | `src/Bee.Definition/SortField.cs:25`（建構子） | S5766 | object-injection | 🏷️ Sonar UI 標記 SAFE |
| 8 | `.github/workflows/nuget-publish.yml:68` | S7636 | secrets 展開 | 🔧 改用 step-level `env:` |
| 9 | `.github/workflows/nuget-publish.yml:84` | S7637 | 未 pin SHA | 🔧 Pin 全部第三方 actions 至 commit SHA |

---

## 實作步驟

### Step 1 — 為 4 個 Regex 呼叫加上 `TimeSpan.FromSeconds(1)`

- `src/Bee.Base/FileFunc.cs:170` — `Regex.IsMatch(input, pattern, RegexOptions.None, TimeSpan.FromSeconds(1))`
- `src/Bee.Base/StrFunc.cs:160` — `Regex.Replace(..., oOptions, TimeSpan.FromSeconds(1))`
- `src/Bee.Base/StrFunc.cs:515` — `Regex.IsMatch(source, regexPattern, options, TimeSpan.FromSeconds(1))`
- `src/Bee.Db/DbCommandSpec.cs:24` — `new Regex(..., RegexOptions.Compiled, TimeSpan.FromSeconds(1))`

4 個呼叫點散落兩個套件、數量不多，決定**就地寫 `TimeSpan.FromSeconds(1)` 不抽共用常數**（避免為 DRY 新增跨套件 API surface）。

### Step 2 — 修 `.github/workflows/nuget-publish.yml:68`（secrets env 注入）

```yaml
- name: Push to NuGet
  env:
    NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
  run: |
    $ErrorActionPreference = 'Stop'
    Get-ChildItem -Path ./nupkgs -Filter *.nupkg | ForEach-Object {
      dotnet nuget push $_.FullName --api-key "$env:NUGET_API_KEY" --source https://api.nuget.org/v3/index.json --skip-duplicate
      if ($LASTEXITCODE -ne 0) { throw "Failed to push $($_.Name)" }
    }
```

### Step 3 — Pin 所有第三方 actions 到 commit SHA

共 6 處（兩個 workflow）：

`nuget-publish.yml`：`actions/checkout@v4`、`actions/setup-dotnet@v4`、`softprops/action-gh-release@v2`
`build-ci.yml`：`actions/checkout@v4`、`actions/setup-dotnet@v4`、`actions/setup-java@v4`、`actions/cache@v4`

取 SHA：
```bash
gh api repos/actions/checkout/git/ref/tags/v4 --jq .object.sha
gh api repos/actions/setup-dotnet/git/ref/tags/v4 --jq .object.sha
gh api repos/actions/setup-java/git/ref/tags/v4 --jq .object.sha
gh api repos/actions/cache/git/ref/tags/v4 --jq .object.sha
gh api repos/softprops/action-gh-release/git/ref/tags/v2 --jq .object.sha
```

格式：`uses: <org>/<name>@<full-sha>  # <tag>`

### Step 4 — 更新 `.claude/rules/sonarcloud.md`

在「11. Reflection 與 Assembly」後新增章節：

- **§13 Regex（ReDoS 防護）**：S6444 — 所有 Regex 呼叫傳入 `TimeSpan.FromSeconds(1)`
- **§14 GitHub Actions 工作流**：S7636、S7637
- **§15 序列化**：S5766 — marker `[Serializable]` 無 `ISerializable` 時可 Sonar UI 標 SAFE

### Step 5 — 驗證

```bash
dotnet build --configuration Release
dotnet test --configuration Release --settings .runsettings
```

### Step 6 — Commit、push、建立 PR

分支：`claude/sonarcloud-hotspots`，建 PR 後 `subscribe_pr_activity` 訂閱。

### Step 7 — PR 合併後（使用者操作）

進 SonarCloud UI 把 2 個 `[Serializable]` hotspot 標 SAFE：

- `ApiException.cs:29` — 理由：非反序列化建構子；`ApiException(Exception, bool)` 僅從既有 `Exception` 複製字串與布林，類別未實作 `ISerializable`、無 `(SerializationInfo, StreamingContext)` 建構子，不經 BinaryFormatter。
- `SortField.cs:25` — 理由：非反序列化建構子；`SortField(string, SortDirection)` 建構子已驗證 `fieldName` 非空，類別僅透過 MessagePack 序列化（型別安全框架），未實作 `ISerializable`。

---

## 影響檔案

**修改**：
- `src/Bee.Base/FileFunc.cs`
- `src/Bee.Base/StrFunc.cs`
- `src/Bee.Db/DbCommandSpec.cs`
- `.github/workflows/nuget-publish.yml`
- `.github/workflows/build-ci.yml`
- `.claude/rules/sonarcloud.md`

**新增**：
- `docs/plans/plan-sonarcloud-hotspots.md`（本檔）
