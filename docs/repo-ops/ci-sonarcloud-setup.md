# SonarCloud 整合設定指南

本文件記錄將一個 GitHub Repo 接上 SonarCloud、並在 CI 上傳測試覆蓋率所需的完整設定流程。未來新專案可依此文件快速建立一致的接入方式。

## 適用情境

- .NET 專案（C#）
- GitHub Actions 作為 CI
- 使用 `coverlet.collector` 收集覆蓋率
- SonarCloud 以 Organization + Project Key 組合識別

## 為何需要 CI-based Analysis

SonarCloud 預設啟用「**Automatic Analysis**」模式，只做靜態分析，**無法接收 CI 上傳的測試覆蓋率報告**。要讓覆蓋率顯示於 SonarCloud：
- 必須關閉 Automatic Analysis
- 改由 CI 透過 SonarScanner for .NET 執行分析並上傳覆蓋率

## 整體流程（一次性設定）

1. SonarCloud 匯入專案（若尚未建立）
2. 關閉 Automatic Analysis
3. 產生 SonarCloud Token
4. 將 Token 加入 GitHub Repo Secret（名稱 `SONAR_TOKEN`）
5. 在 CI workflow 加入 SonarScanner 步驟
6. Push 觸發 CI，驗證 SonarCloud 收到覆蓋率

## 步驟 1｜匯入 SonarCloud 專案

1. 登入 https://sonarcloud.io
2. `+` → `Analyze new project` → 選擇 GitHub Repo
3. 完成後記下兩個識別：
   - **Organization Key**（個人通常為 GitHub 帳號，如 `jeff377`）
   - **Project Key**（通常為 `{org}_{repo}`，如 `jeff377_bee-library`）

## 步驟 2｜關閉 Automatic Analysis

1. 進入 SonarCloud 專案 → `Administration` → `Analysis Method`
2. 將「**Automatic Analysis**」開關**關閉（OFF）**

> 未關閉會導致 CI 上傳被拒，錯誤訊息：*"You are running CI analysis while Automatic Analysis is enabled"*

## 步驟 3｜產生 SonarCloud Token

1. SonarCloud 右上頭像 → `My Account` → `Security`
2. 於 `Generate Tokens` 輸入識別名稱（建議格式 `<repo>-ci`，例如 `bee-library-ci`）
3. `Generate` → **立刻複製 token**（只會顯示一次）

## 步驟 4｜加入 GitHub Repo Secret

1. 前往 `https://github.com/{owner}/{repo}/settings/secrets/actions`
2. `New repository secret`
   - Name：**`SONAR_TOKEN`**（全專案統一此名稱）
   - Secret：貼上步驟 3 的 token
3. `Add secret`

> 名稱固定為 `SONAR_TOKEN` 的目的：workflow 可跨專案重用，不需逐專案改引用名。

## 步驟 5｜CI Workflow 設定

以下為最小可運作的 `build-ci.yml`（Windows runner）：

```yaml
jobs:
  build:
    runs-on: windows-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      with:
        fetch-depth: 0          # SonarCloud 需要完整 git history 做 SCM blame

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 10.0.x

    - name: Setup Java (for SonarScanner)
      uses: actions/setup-java@v4
      with:
        distribution: zulu
        java-version: '17'

    - name: Cache SonarCloud packages
      uses: actions/cache@v4
      with:
        path: ~\.sonar\cache
        key: ${{ runner.os }}-sonar
        restore-keys: ${{ runner.os }}-sonar

    - name: Install SonarScanner for .NET
      run: dotnet tool install --global dotnet-sonarscanner

    - name: SonarScanner Begin
      env:
        SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
      shell: pwsh
      run: |
        dotnet sonarscanner begin `
          /k:"{ProjectKey}" `
          /o:"{OrganizationKey}" `
          /d:sonar.token="$env:SONAR_TOKEN" `
          /d:sonar.host.url="https://sonarcloud.io" `
          /d:sonar.cs.opencover.reportsPaths="**/TestResults/**/coverage.opencover.xml" `
          /d:sonar.coverage.exclusions="tests/**,samples/**"

    - name: Build
      run: dotnet build <Solution>.slnx --configuration Release

    - name: Test with coverage
      run: dotnet test <Solution>.slnx --configuration Release --no-build --verbosity normal --collect:"XPlat Code Coverage;Format=opencover"

    - name: SonarScanner End
      env:
        SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
      shell: pwsh
      run: dotnet sonarscanner end /d:sonar.token="$env:SONAR_TOKEN"
```

新專案複製時需替換：
- `{ProjectKey}` → SonarCloud Project Key（如 `jeff377_bee-library`）
- `{OrganizationKey}` → SonarCloud Organization Key（如 `jeff377`）
- `<Solution>.slnx` → 實際的 solution 檔名

## 步驟 6｜驗證覆蓋率已上傳

Push 觸發 CI，等 CI 成功後用 API 確認：

```bash
curl -s "https://sonarcloud.io/api/measures/component?component={ProjectKey}&metricKeys=coverage,line_coverage,branch_coverage,ncloc" | python3 -m json.tool
```

應看到 `coverage`、`line_coverage`、`branch_coverage` 三個指標有數值。若只有 `ncloc`，代表 SonarCloud 收到分析但未收到覆蓋率 → 回去檢查：
- `coverage.opencover.xml` 是否有產生（`dotnet test` log）
- `sonar.cs.opencover.reportsPaths` 路徑 pattern 是否涵蓋實際產生位置

也可直接開啟：
```
https://sonarcloud.io/summary/new_code?id={ProjectKey}&branch=main
```

## 常見錯誤與排除

| 症狀 | 原因 | 解法 |
|------|------|------|
| `sonar.token= is invalid`（空值） | GitHub Secret 名稱與 workflow 引用不符 | 確認 secret 名稱為 `SONAR_TOKEN` |
| `You are running CI analysis while Automatic Analysis is enabled` | 未關閉 Automatic Analysis | 回步驟 2 |
| SonarCloud 只顯示 `ncloc` 無 `coverage` 指標 | Automatic Analysis 模式或覆蓋率檔路徑不符 | 關閉 Automatic Analysis、確認 `reportsPaths` pattern |
| `No coverage` badge | 同上 | 同上 |
| CI 所有測試 Skipped 導致覆蓋率異常低 | `[LocalOnlyFact]` 在 CI 環境下會跳過 | 正常現象；純邏輯測試勿使用 `LocalOnlyFact` |

## 注意事項

- **Windows runner 必要**：SonarScanner for .NET 在 Linux runner 上可運作，但本專案使用 Windows runner 與 PowerShell 語法，未跨平台驗證
- **Java 17 必要**：SonarScanner v6+ 需 Java 17 執行期
- **`fetch-depth: 0` 必要**：否則 SonarCloud 無法做 SCM blame、new-code 分析會失準
- **`--collect:"XPlat Code Coverage;Format=opencover"`**：必須指定 `Format=opencover`，否則 coverlet 預設輸出 Cobertura，SonarScanner for .NET 讀不到
- **測試結果（trx）未上傳**：本設定僅上傳覆蓋率，不上傳測試執行結果。若需 SonarCloud 顯示測試總數，需額外加 `--logger trx` 並設定 `sonar.cs.vstest.reportsPaths`
- **覆蓋率排除 `tests/` 與 `samples/`**：避免測試專案自身列入覆蓋率分母導致虛高；依專案結構調整 `sonar.coverage.exclusions`

## 參考

- [SonarScanner for .NET 官方文件](https://docs.sonarsource.com/sonarqube-cloud/advanced-setup/ci-based-analysis/sonarscanner-for-net/)
- [coverlet `--collect` 參數說明](https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/VSTestIntegration.md)
- 本專案實作：[build-ci.yml](../.github/workflows/build-ci.yml)
