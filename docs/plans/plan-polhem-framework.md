# 計畫：bee-library 改名為 Polhem 並另開於 polhem-dev

**狀態：🚧 進行中（2026-09-26）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 0 | 啟動前置：商標重查、凍結起點、外部帳號與授權的事前確認 | ✅ 已完成（2026-09-26） |
| 1 | 本機建立 polhem：純改名（1:1 前綴替換，含編譯器抓不到的字串），diff 驗證零行為變更 | 📝 待做 |
| 2 | agent 設定與協作文件：共用規則搬進 repo、`.claude/` 英文化、plan 慣例、LICENSE／CONTRIBUTING／CODEOWNERS | 📝 待做 |
| 3 | 英文化：測試方法名稱與 `[DisplayName]`、程式內殘留的中文、維運文件與 gotchas | 📝 待做 |
| 4 | 公開文件：`docs/` 改以英文為源、ADR 補英文版並納入雙語檢查、README 遷移說明 | 📝 待做 |
| 5 | 建立 repo 與 CI：推送、SonarCloud、分支保護與 PR 工作流、auto-merge、Trusted Publishing policy | 📝 待做 |
| 6 | polhem-connector-js 改名另開，接上新的 wire 合約 | 📝 待做 |
| 7 | 首發前健檢與修正，公開 API 定型 | 📝 待做 |
| 8 | 首發 `Polhem.*` 1.0.0，更新 org profile | 📝 待做 |
| 9 | polhem-northwind 改名另開，改用 `Polhem.*` 1.0.0 | 📝 待做 |
| 10 | 回寫 bee-library，凍結舊框架與下游 repo | 📝 待做 |

## 背景

方向的權威來源是 [future-work.md「開放共同維護」](../repo-ops/future-work.md)：bee-library **不轉移**，
而是以當時最新版為起點，在 `polhem-dev` 另開改名的新框架，命名空間全換；舊 repo 與 `Bee.*` 套件凍結。
名稱 Polhem 已定案並公開，GitHub org `polhem-dev`、NuGet 組織 `Polhem`、npm `@polhem` 都已建立。

bee-oauth2 已先走完同一條路，完整紀錄見封存的 [plan-polhem-oauth2.md](archive/plan-polhem-oauth2.md)。
本 plan 以它為結構範本，但 bee-library 的規模、相依與下游都大得多，階段是重新規劃的。

**啟動時機**：future-work 原本寫「等出現明確的共同維護者人選」。使用者於 2026-09-19 決定**排入近期執行**，
不等共同維護者。future-work 的這段與「帶完整 git 歷史」一項，於階段 10 回寫。

### 現況（2026-09-19 盤點）

| 項目 | 現況 |
|------|------|
| 來源 | `jeff377/bee-library` `main`，版號見 `Version.props`（盤點當時 4.33.0） |
| 發佈到 NuGet 的 | `src/` 下除 `Bee.Analyzers` 以外的專案（analyzer 隨 `Bee.Definition` 打包），加上 dotnet tool `Bee.Cli`（指令 `dotnet-bee`） |
| 發佈授權 | `nuget-publish.yml` 以 repo secret `NUGET_API_KEY` 推送 |
| CI | `build-ci.yml`（精簡模式 SQL Server + SQLite；`[all-db]` 跑四種資料庫與 SonarCloud）、`docs-check.yml`、`auto-merge.yml` |
| SonarCloud | 專案 key `jeff377_bee-library`、organization `jeff377`，寫在 `build-ci.yml` 的命令列 |
| 分支保護 | `main` 要求 `build` check；`enforce_admins` 關閉，維護者直接推 `main` |
| auto-merge | `auto-merge.yml` 只認 PR 作者 `jeff377`，以 `AUTOMERGE_PAT` 合併 |
| 公開文件 | `docs/zh-TW/` 為源、`docs/en/` 為譯本；`docs/adr/` 只有中文，不在 `check-docs-i18n.sh` 的範圍內 |
| agent 設定 | `.claude/` 全中文；跨專案共用規則放在使用者層 `~/.claude/rules/`，協作者讀不到；專案層 `settings.json` 宣告 `dev-workflow` plugin |
| 測試 | 方法名稱英文，`[DisplayName]` 中文 |
| 舊 repo | 11 星、5 fork、0 個開放 issue |

### 下游（2026-09-19 盤點）

盤點範圍只限本機 `~/Desktop/repos` 下的 repo（使用者決定，2026-09-26），GitHub 上沒有 clone 到本機的不納入。

| repo | 關係 | 本 plan |
|------|------|---------|
| `jeff377/bee-connector-js` | wire 合約的 TypeScript 客戶端。`scripts/fetch-fixtures.mjs` 寫死 `REPO = 'jeff377/bee-library'` 抓 `wire-fixtures/`；`package.json` 標 `private`，尚未發佈到 npm | 改名另開為 `polhem-dev/polhem-connector-js`，套件名 `@polhem/connector`（階段 6） |
| `jeff377/bee-northwind-avalonia` | `apps/Bee.Northwind` 的鏡像，以 NuGet 消費 `Bee.*`，有自己的 CI | 改名另開為 `polhem-dev/polhem-northwind`（階段 9） |
| `jeff377/bee-jsonrpc-sample` | 停在 `Bee.*` 4.1.0／4.3.0，最後更新 2026-05 | 不移植，只凍結（階段 10） |
| `jeff377/claude-plugins`（`dev-workflow`） | 與框架無直接關係 | 不搬；新 repo 不宣告（見決策紀錄） |
| `jeff377/bee-blogs` | `docs/blogs/` 本身的 private repo（部落格草稿與鐵人賽工作檔），clone 在 bee-library 裡 | 更名為 `jeff377/blogs`，移出程式碼 repo，放在 `~/Desktop/repos/blogs`。**已提前完成（2026-09-26）**，見階段 0 |
| `jeff377/bee-library-private` | 以同步腳本鏡像 `docs/blogs/`、`docs/internal/` 等 gitignored 檔案的私有 repo | 由 `jeff377/polhem-local` 與獨立的 `jeff377/blogs` 取代，停止同步後 archive（階段 10） |

### 演練得到與演練不到的

| 項目 | bee-oauth2 的前例 | 本次 |
|------|------------------|------|
| 不帶 git 歷史另開 | 有 | 沿用 |
| 凍結順序、NuGet deprecated | 有 | 沿用，但套件數多、還有 dotnet tool |
| Trusted Publishing | 有，單一 glob `Polhem.OAuth2*` | 要規劃與既有 policy 不重疊的範圍，**需實測** |
| SonarCloud | **沒有** | **需實測**：org 綁定、專案建立、quality profile / gate 的 UI 設定 |
| 分支保護與 PR 工作流 | **沒有** | **需實測** |
| auto-merge | **沒有** | **需實測**：org 下的 PAT 或替代做法 |
| wire 合約與 connector-js | **沒有** | **需實測**：兩個 repo 的 CI 互相依賴 |
| 型別名稱字串、`BEE_` 識別字 | 不適用 | 本次首見，見「編譯器抓不到的地方」 |
| Apple / Android head 的 trim、AOT | 不適用 | `ILLink.Descriptors.xml` 帶組件名，**只有 Release trim 看得出來** |

## 決策紀錄

| 決策 | 結論 | 理由 |
|------|------|------|
| 啟動時機 | 排入近期執行，不等共同維護者 | 使用者決定（2026-09-19） |
| 商標重查 | 列為階段 0 第一步 | 使用者決定（2026-09-19）。現在查的結果到執行時已過期 |
| git 歷史 | **不帶**，比照 polhem-oauth2 | 使用者決定（2026-09-19）。推翻 future-work 原本的「帶完整歷史」，階段 10 回寫 |
| commit hash 引用與封存 plan | `docs/plans/archive/` 不帶；`.claude/`、gotchas 等處以 hash 引用的證據，改寫為 `https://github.com/jeff377/bee-library/commit/<hash>` 完整網址 | 使用者決定（2026-09-19）。舊 repo archive 後仍可讀；封存 plan 是中文、記錄 Bee 時期的打算，與語言政策衝突 |
| 移轉方式 | 新開 repo；舊 repo 指路後 archive | future-work 已定案 |
| repo 名稱 | `polhem-dev/polhem` | 使用者決定（2026-09-19）。框架本體即家族名 |
| 名稱對映 | 1:1 前綴替換：`Bee.X` → `Polhem.X`，套件拆分不變；型別與成員名稱中的 `Bee` 一併替換（`AddBeeFramework` → `AddPolhemFramework`） | 使用者決定（2026-09-19）。搬移那一步才能以「正規化回 Bee 後 diff」證明零行為變更。命名是否要調整，交給階段 7 的健檢 |
| 先改還是先搬 | **搬完再改** | 比照 polhem-oauth2：不帶歷史時，搬之前的修改會併進初始快照、看不到個別變更 |
| 首版版號 | 1.0.0 重新起算 | 使用者決定（2026-09-19）。比照 polhem-oauth2 |
| 下游範圍 | connector-js 與 northwind 改名另開；jsonrpc-sample 只凍結 | 使用者決定（2026-09-19） |
| 下游名稱 | `polhem-connector-js`（npm `@polhem/connector`）、`polhem-northwind`；框架內的 `apps/Bee.Northwind` 對應改為 `apps/Polhem.Northwind` | 使用者決定（2026-09-19） |
| 舊框架凍結 | 比照 polhem-oauth2 的順序：新套件首發 → 舊套件全版本 deprecated → README 指路 → 處理 issue → archive；不再發 `Bee.*` 新版 | 使用者決定（2026-09-19）。分出去之後不雙邊修 |
| 型別名稱字串相容解析 | **不做**，只寫遷移說明；解析失敗的例外訊息順帶指出可能是舊的 Bee 名稱 | 使用者決定（2026-09-19）。使用者多為自有 repo |
| 其他帶 Bee 字樣的識別字 | **全部改名、不保留舊名**：`BEE_*` → `POLHEM_*`、`dotnet-bee` → `dotnet-polhem`、HKDF 標籤 `bee-api-*` → `polhem-api-*` | 使用者決定（2026-09-19）。與「不做相容解析」一致。HKDF 標籤改名只讓遷移當下的線上 session 失效（`DerivedApiEncryptionKeyProvider` 的註解已寫明），不影響持久資料 |
| 診斷代號前綴 | `BEE1001` → `POLHEM1001`，數字沿用 | 使用者決定（2026-09-19）。遷移對照最簡單，也不易與其他分析器撞名 |
| 開放共同維護的缺口 | **全部併入本 plan，首發前完成** | 使用者決定（2026-09-19）。外界第一眼看到的就是可協作的 repo |
| 語言政策 | 共同維護的部分一律英文：程式碼、XML doc、程式內註解、測試方法名稱與 `[DisplayName]`、`.claude/`、commit message、維運文件。公開 `.md` 與 ADR 中英雙語 | future-work 已定案（2026-09-13），polhem-oauth2 先行 |
| 公開文件的源語言 | `docs/<lang>/` 改以**英文為源**，ADR 一併納入雙語檢查 | 使用者決定（2026-09-19）。共同維護者改的是源文件 |
| plan 目錄 | 新 repo 的計畫目錄為 **gitignored 的 `local/plans/`**，不入版控 | 使用者決定（2026-09-19）。plan 是個人當時的打算；長效決策一律升格 ADR。bee-library 進行中的 plan 由維護者自行複製到本機 `local/plans/` |
| 個人文件 | 比照 polhem-oauth2：repo 根目錄的 `local/` 由 `.gitignore` 排除，放 plans、internal（含未修安全項的審查紀錄）與筆記；`local/` 本身是 private repo **`jeff377/polhem-local`**，直接 clone 在 `local/` 底下，不是 submodule。`docs/` 下不再有 gitignored 的內容 | 使用者決定（2026-09-20）。取代 [plan-personal-docs-directory.md](plan-personal-docs-directory.md) 原訂在 bee-library 內的搬移——舊 repo 即將凍結，先搬一次是白工 |
| 部落格草稿 | **移出程式碼 repo**：`jeff377/bee-blogs` 更名為 `jeff377/blogs`，clone 到 `~/Desktop/repos/blogs`，不放在 polhem 的 `local/`，與程式碼脫鉤 | 使用者決定（2026-09-20）。名稱不綁定 Bee 或 Polhem。部落格與框架的生命週期不同，不隨程式碼 repo 走 |
| CHANGELOG | 不帶 `CHANGELOG*` 與 `docs/changelogs/`，從 1.0.0 重起；1.0.0 一節列出相對於 `Bee.*` 最後一版的差異，遷移細節指向 README | 使用者決定（2026-09-19）。比照 polhem-oauth2 |
| `dev-workflow` plugin | 新 repo 的 `.claude/settings.json` 不宣告；屬於 repo 本身的慣例（計畫目錄、公開文件不引用 plan、發版的兩條防護欄）寫進 repo 的 `CLAUDE.md`／`CONTRIBUTING.md` | 使用者決定（2026-09-19）。plugin 與框架無直接關係，各開發者習慣不同 |
| LICENSE 著作權人 | `Copyright (c) Polhem contributors`，不寫年份 | future-work 已定案，比照 polhem-oauth2 |
| 發佈授權 | NuGet Trusted Publishing，不用 API key | future-work 已定案，比照 polhem-oauth2 |
| 本 plan 涵蓋的 repo 與套件 | 只處理本機 `~/Desktop/repos` 下的 repo；NuGet 只凍結凍結起點時 bee-library 會產出的套件，其餘 `Bee.*` 列在範圍外 | 使用者決定（2026-09-26） |
| 進行中的 plan | `plan-row-level-tenancy`、`plan-rounding-mode`、`plan-property-grid-control`、`plan-tree-view-builder` 四份**都帶到 polhem**，不在 Bee 做 | 使用者決定（2026-09-26）。四份都還沒動工；在 Bee 做完只會延後凍結 |
| 凍結起點 | `7d6cc9d9` | 使用者決定（2026-09-26）。之後的 commit 只動 `docs/plans/`，階段 1 不帶這個目錄，快照內容相同 |
| polhem-local 的暫放位置 | 階段 1 之前 clone 在 `~/Desktop/repos/polhem-local`，階段 2 第 3 步 `.gitignore` 加上 `/local/` 之後才移為 `polhem/local` | 使用者決定（2026-09-26）。先移進去的話，階段 1 的初始 commit 有可能把它收進去 |
| `docs/internal/` 的去處 | `open-source-promotion-assessment.md`、`avalonia-controls.md` 放 polhem-local 的 `internal/`（後者在階段 3 併入維運文件後從 local 刪除）；兩份 `routine-bee-*-fix.md` 與 `ai-agent-devloop-design-internal.md` 放 `internal/archive/` | 使用者決定（2026-09-26）。Routine 已停用；`bee-library-private` 的鏡像停在 2026-04-23，靠不住 |

## 編譯器抓不到的地方（2026-09-19 查證）

命名空間與套件 ID 的替換由編譯器把關，下列各類不會。future-work 原本列了第 1、2、4、6、7 類的部分，其餘是本次 grep 查到的。
階段 1 的替換腳本要逐類涵蓋，並對每一類補一個「改錯會紅」的檢查（既有測試或新增）。

1. **定義檔的組件限定型別名稱**：`ProgramSettings` 的 `BusinessObject="Bee.Business.AuditLog.LogBusinessObject, Bee.Business"`，
   分布在 `tests/Define/`、`samples/Define/`、`apps/Bee.Northwind/Define/`；消費端自己的 Define 檔也有。
2. **程式碼裡的組件限定型別名稱**：`src/Bee.Definition/BackendDefaultTypes.cs` 的 public const（下層組件以字串指向上層實作），
   其值也出現在 `PublicAPI.Shipped.txt`。
3. **wire 內容**：`wire-fixtures/bodies/` 的 `"type": "Bee.Definition.Collections.Parameter, Bee.Definition"`、
   `wire-contracts/type-names.ts`。兩者都是產生出來的，改名後以 `BEE_REGENERATE_WIRE_FIXTURES`／`BEE_REGENERATE_WIRE_CONTRACTS`（改名後為 `POLHEM_*`）重生，不手改。
   伺服端先以白名單過濾 envelope 的型別名稱，舊客戶端送舊名稱會被拒。
4. **非組件限定的型別全名字串**：
   - `src/Bee.Definition/DefineTypeExtensions.cs` 以 `Assembly.GetType("Bee.Definition.Settings.SystemSettings")` 取型別，漏改在執行期才擲例外。
   - **analyzer 以 metadata name 找型別**：`src/Bee.Analyzers/Serialization/SerializationTypeNames.cs`、
     `Conventions/BusinessObjectAccessControlAnalyzer.cs`、`ExecFuncAccessControlAnalyzer.cs` 等。
     **找不到型別時 analyzer 靜默不報**，建置反而是綠的，是最危險的一類。要確認 `Bee.Analyzers.UnitTests` 在名稱錯時會紅。
5. **組件名與資源名字串**：
   - `src/Bee.Base/SysInfo.cs` 的組件名清單。
   - `DefinitionCollectionPropertyAnalyzer` 的 `DefinitionAssemblyName`。
   - `src/Bee.Definition/ILLink.Descriptors.xml` 的 `<assembly fullname="Bee.Definition">`：**只在 Apple Release trim 才顯現**，桌面與測試都看不出來。
   - 嵌入資源：`Bee.Definition.csproj` 的 `LogicalName` 前綴 `Bee.Definition.Defaults/` 與 `Defaults.cs` 的 `ResourcePrefix`；
     DefineEditor 的 resx base name `Bee.DefineEditor.Resources.Strings`。
   - logger category（`BeeFrameworkApplicationBuilderExtensions` 的 `"Bee.Api.AspNetCore"`）與 analyzer 的 `category`。
6. **資料 key**：`DataColumnExtensions` 存進 `DataColumn.ExtendedProperties` 的 `"Bee.FieldDbType"`、
   `SerializationErrorData.FilePath` 的 `"Bee.FilePath"`（`Exception.Data` key）。
   **待查**：前者是否會隨 DataSet 的 XML 或 wire 序列化傳出去；會的話，新舊版本之間就是資料格式的差異。
7. **環境變數**：`BEE_MASTER_KEY`（與 `_FILE`，部署環境依賴它）、`BEE_TEST_CONNSTR_*`、`BEE_LOADTEST_CONNSTR_*`、
   `BEE_REGENERATE_WIRE_*`、`BEE_TEST_*_CONTAINER`；出現在原始碼、`test.sh`、CI workflow 與文件。
8. **診斷代號與 MSBuild 名稱**：analyzer 的 `BEE1xxx`–`BEE4xxx`、`src/Directory.Build.targets` 的 `BEE9xxx` 閘門；
   `.editorconfig` 的 severity 設定；MSBuild 屬性與項目名 `BeeAllowedDependency`、`BeeEnforceDependencyBoundary` 等
   （名稱打錯時閘門可能變成空清單而通過）。
9. **密碼學 domain label**：`DerivedApiEncryptionKeyProvider` 的 HKDF info `bee-api-session-key`、`bee-api-encryption-root-key`。依決策改名。
10. **工具、檔案系統與 UI 名稱**：`Bee.Cli` 的 `ToolCommandName`；DefineEditor 使用者設定目錄 `Bee.DefineEditor`（改名後舊設定不再被讀到）；
    暫存目錄前綴；Blazor 元件的 HTML id 前綴 `bee-form`、`bee-login-*`；`samples/Web.Js.Demo/bee-api-client.js`；
    測試容器名 `bee-mysql` 等與測試資料庫名 `bee_test_*`（維護者本機的持久容器要重建）。
11. **外部服務識別**：SonarCloud 的 `/k:`、`/o:`；`auto-merge.yml` 的 `jeff377`；connector-js 的 `REPO` 常數；
    README 徽章與 `github.com/jeff377/...` 網址（含 `.claude/rules/serialization.md`、`wire-contracts/README.md`、`wire-fixtures/README.md`）。
12. **品牌字串與圖示**：`Bee.NET`（`Company`／`Product`／`Copyright`、CLI 與壓測工具的標題文字）、根目錄 `bee.png`。
    圖示改用 polhem-brand 的 `png/nuget-package-icon-128.png`（polhem-oauth2 曾誤用舊圖示）。

## 階段 0：啟動前置

1. **商標重查**：重跑 polhem-oauth2 階段 7 的三處檢索——WIPO Global Brand Database（品牌名稱含 `POLHEM`）、
   USPTO（wordmark `polhem`）、智慧財產局（`https://cloud.tipo.gov.tw/S282/S282WV1/`，文字近似 `POLHEM`，篩第 9、42 類）。
   仍然乾淨才往下；出現衝突時名稱已隨 Polhem.OAuth2 公開，先評估影響範圍再決定是否換名。
2. **凍結起點**：選定 bee-library 的起點 commit，記下 hash。起點之後 bee-library 只收本 plan 的回寫，不再有功能變更。
   起點前先處理進行中的 plan：要在 Bee 做完的做完，其餘複製到 polhem 本機的 `local/plans/` 繼續。
3. **個人文件**：
   - 建立 private repo `jeff377/polhem-local`，`README.md` 與 `.gitignore` 比照 `jeff377/polhem-oauth2-local`（`.gitignore` 擋常見的憑證副檔名，當作第二道防線）。
   - `docs/internal/` 裡與 Polhem 相關的文件複製到 `local/internal/`；進行中的 plan 見上一步。
   - ~~`docs/blogs/` 移出 bee-library~~ **已提前完成（2026-09-26，使用者要求）**：
     - GitHub 上 `jeff377/bee-blogs` 更名為 `jeff377/blogs`（舊網址由 GitHub 轉址）。
     - 搬移前確認工作樹乾淨、沒有未推送的 commit、沒有 stash；改以 `mv` 整個目錄搬到 `~/Desktop/repos/blogs`，
       不是 clone 再刪，未追蹤的檔案也一併保留。remote 改為新網址，`git fetch` 確認與 `origin/main` 同步。
     - 使用者層 skill `ithome-publish`、`medium-publish`、`hackmd-blog` 改用新的絕對路徑；
       「確認沒開在 worktree」那道閘門改為「確認 blogs repo 的位置」，因為 blogs 已不在 bee-library 裡。
     - blogs repo 內的 `publishing-playbook.md` §0 同步改寫；`decision-log.md` 是紀錄，不改。
     - memory：部落格專用的 `medium-english-edition-decisions` 移到 `~/Desktop/repos/blogs` 的 project memory，
       `deliver-files-as-links` 兩邊各一份。
     - bee-library 的 `.gitignore`、`check-md-links.sh`、`check-public-docs.sh`、`.claude/rules/public-docs.md` 拿掉 `docs/blogs/`。
     - 未處理：排程任務 `ithome-2026-daily-post` 仍寫舊路徑（連載已完結，任務本身寫明完賽後可刪）；
       `bee-library-private` 的同步腳本仍列 `docs/blogs/`，來源不存在時只記 log、不會清鏡像，階段 10 一併處理。
   - 以 `git status` 看不出 gitignored 目錄的內容，搬完以 `ls` 與兩個 private repo 的 `git log` 確認。
4. **外部帳號與授權的事前確認**（只確認，不建立）：
   - NuGet `Polhem.` 前綴保留的申請狀態。
   - SonarCloud 能否以 `polhem-dev` 建立 organization，以及 `jeff377` org 在 UI 上的 quality profile／gate 設定，要記下來以便重建。
   - `AUTOMERGE_PAT` 在 org 下的替代做法（fine-grained PAT 需 org 核准，或改用 GitHub App）。

### 實作紀錄（2026-09-26）

**凍結起點：`7d6cc9d9`**（`docs(plans): polhem plan 階段 0 開始執行`），隨本階段的 commit 一起推上 `origin/main`。
階段 1 以這個 commit 的 `git ls-files` 為準；之後 bee-library 只收本 plan 的回寫。

1. **商標重查，未發現衝突**（非法律意見）：
   - WIPO Global Brand Database，品牌名稱含 `POLHEM`：2 筆，都是 Polhem Infra Kommanditbolag 的 POLHEM INFRA
     （歐盟註冊 018383987、018383992，第 35、36 類），與 2026-09-14 相同。
   - USPTO，wordmark `polhem`：有效（Live）與失效（Dead）案件都是 0 筆。
   - 智慧局，文字近似 `POLHEM`：共 280 筆（資料更新至 2026-09-25），篩第 9、42 類剩 30 筆，與上次筆數相同，沒有 POLHEM。
     最接近的仍是 POLEYA、ProHSM、ProEKM、PROCHEM、POLYCHEM、POLIMA，字形與讀音都有明顯差異。
2. **進行中的 plan**：四份都帶到 polhem（見決策紀錄）。複製到 polhem-local 的 `plans/`；bee-library 的原檔狀態改為「⛔ 已移交 polhem」，
   索引同步更新。原檔不刪，內文未改。
3. **個人文件**：
   - 建立 private repo `jeff377/polhem-local`，本機暫放 `~/Desktop/repos/polhem-local`（見決策紀錄），初始 commit `2dd279b`。
     `.gitignore` 與 `polhem-oauth2-local` 相同；`README.md` 比照它，另加目錄說明。
   - `docs/internal/` 五份依決策複製到 `internal/` 與 `internal/archive/`，以 `cmp` 確認與來源相同；`internal/archive/README.md` 記錄封存原因。
     bee-library 的原檔保留（gitignored）。
   - **發現**：`bee-library-private` 最後一次同步是 2026-04-23。同步用的 plist `~/Library/LaunchAgents/com.jeff377.bee-library-sync.plist`
     在本機不存在，階段 10 第 8 步的「卸載 launchd agent」在本機沒有對象。鏡像裡的兩份 routine 文件是舊版，
     `avalonia-controls.md` 與 `open-source-promotion-assessment.md` 不在鏡像裡；這五份現在以 polhem-local 為準。
4. **外部帳號與授權**（只確認，未建立任何東西）：
   - **雲端 Routine**：`bee-coverage-fix`、`bee-sonar-fix` 都還在，但 `enabled` 為 false，最後觸發 2026-06-14。
     `auto-merge.yml` 最後一次執行也在 2026-06-14，帶 `auto-merge` label 的 PR 都是這兩個 Routine 開的。
   - **NuGet `Polhem.` 前綴**：Search API 上三個 `Polhem.OAuth2*` 套件的 `verified` 都是 false，前綴**尚未核准**。
     future-work 寫「已申請」，但 Gmail（含寄件備份）查不到寄給 `account@nuget.org` 的申請信或回覆，NuGet 的 GitHub issue 也查不到。
     使用者確認（2026-09-26）：記不得當初如何申請，**首發前重新申請**，見階段 8 第 1 步。
   - **SonarCloud**：
     - organization key `polhem-dev` 目前沒有人使用（`api/organizations/search` 回 0 筆）。GitHub org `polhem-dev` 是 free plan，jeff377 為 admin。
       以 `gh` 查 org 已安裝的 GitHub App 為 0 筆，SonarCloud 的 app 尚未安裝。實際能否建立要到階段 5 才驗證。
     - `jeff377` org 的設定（公開 API 查得）：quality gate 是內建的 **Sonar way**（預設），條件為新程式碼的 security、reliability、
       maintainability rating 劣於 A 即失敗，新程式碼覆蓋率 < 80%、重複率 > 3%、security hotspot 審查率 < 100% 失敗。
       quality profile 全部語言都是內建的 **Sonar way comprehensive**（C# 379 條規則），沒有自訂 profile。
     - 專案 `jeff377_bee-library` 唯一不是繼承來的設定是 `sonar.autoscan.enabled=false`（改由 CI 分析）。
     - 未確認：new code definition（API 查不到，要看 UI），以及新 org 的預設 profile 是否也是 Sonar way comprehensive。
   - **`AUTOMERGE_PAT` 的替代做法**：依 GitHub 文件，org 的 fine-grained PAT 預設要 owner 核准，但 **owner 自己建立的 token 不需核准**；
     jeff377 是 `polhem-dev` 的 owner。可選的做法：
     - owner 的 fine-grained PAT，resource owner 選 `polhem-dev`、只給該 repo：不必核准，但綁個人帳號、有期限。
     - GitHub App，workflow 以 `actions/create-github-app-token` 換取 token：屬於 org、不綁個人。
     - 只用 `GITHUB_TOKEN`：由它觸發的事件不會啟動新的 workflow run（GitHub 文件），合併後 `main` 的 CI 不會跑。
     現行 auto-merge 只服務已停用的 Routine；polhem 要不要 auto-merge、用哪一種，在階段 5 決定。

## 階段 1：本機建立 polhem（純改名）

本機位置 `~/Desktop/repos/polhem`，以階段 0 起點的 `git ls-files` 追蹤檔為準（不含 `.git`、gitignored 與建置產物）。

**不帶**：`docs/plans/`（含 `archive/`）、`CHANGELOG.md`／`CHANGELOG.zh-TW.md`、`docs/changelogs/`、`.claude/settings.local.json`。

### 步驟

1. **替換腳本**：以詞界比對替換 `Bee`→`Polhem`、`bee`→`polhem`、`BEE`→`POLHEM`，並涵蓋資料夾名、檔名、方案檔、csproj。
   `Bee.NET` 改為 `Polhem`。腳本放在 scratchpad，不進 repo；替換結果要逐類對照上一節清點，非品牌的命中逐一人工判讀。
   **專案檔與方案檔要原地覆寫或以 `git mv`／`mv` 改名，不要先刪再建**：開著的 VS Code 會在檔案刪除的當下，把該專案從 `.slnx` 移除（polhem-oauth2 踩過）。執行期間最好關掉開著這個 repo 的 IDE。
2. **不在替換之內、要個別處理的**：LICENSE、套件中繼資料（`RepositoryUrl` → `https://github.com/polhem-dev/polhem`）、圖示、
   SonarCloud key 與 organization、`auto-merge.yml` 的作者判斷（階段 5 定案，這一步先保留結構）。
3. **重生產生物**：wire fixtures 與 contracts 以環境變數重生；`PublicAPI.*.txt` 隨替換更新（基準的重建在階段 7）。
4. **零行為變更驗證**：把新樹正規化回 Bee（反向替換）後與起點快照 diff。預期只剩 LICENSE、中繼資料、圖示、
   不帶的檔案與階段 1 第 2 步列的項目。**本機比對通過才進下一步。**
5. **本機建置與測試**：clean Release build 0 警告；`./test.sh` 四種資料庫全跑（容器改名後重建）；
   DefineEditor、Northwind 各 head 至少啟動一次；**iOS 以 Release 建置並在模擬器跑**，驗證 `ILLink.Descriptors.xml`（見 `rules/apple-mobile-trim.md`）。
6. **初始 commit**：英文 message，註明來源 `jeff377/bee-library@<起點 hash>`。建立 GitHub repo 延到階段 5。

## 階段 2：agent 設定與協作文件

1. **共用規則搬進 repo**：使用者層 `~/.claude/rules/` 的 `code-style`、`scanning`、`single-source`、`pull-request`、`releasing`
   以英文**複製**進 `.claude/rules/`（使用者層仍供其他 repo 使用，不刪）。`pull-request` 改為 PR + 分支保護的工作流。
2. **`.claude/` 英文化**：`CLAUDE.md`、`rules/`、`skills/`（`bee-*` 改名 `polhem-*`）、`commands/`、`hooks/` 的訊息與註解。
   `CLAUDE.md` **明文覆寫**使用者層「敘述文字全部繁體中文」的預設。
3. **plan 與個人文件慣例寫進 repo**：`.gitignore` 加 `/local/` 與 `CLAUDE.local.md`，拿掉 `docs/internal/` 那條（`docs/blogs/` 已於 2026-09-26 在 bee-library 移除）；
   `.gitignore` 生效後，把暫放的 `~/Desktop/repos/polhem-local` 移為 `local/`，以 `git status` 確認它沒有出現；`CLAUDE.md` 比照 polhem-oauth2 的「Local working documents」一節，
   寫明 `local/` 的用途、計畫目錄、不 commit 也不 `git add -f`、不從已 commit 的檔案連過去、worktree 讀不到 `local/`；
   `.claude/rules/public-docs.md` 的「哪些不是」表、`check-md-links.sh` 與 `check-public-docs.sh` 對 `docs/internal/` 的排除一併改寫；
   `settings.json` 移除 `dev-workflow` 的 marketplace 與啟用宣告。`check-public-docs.sh` 的 (7) 死指標檢查改對 `local/plans/` 或移除，實作時判斷。
4. **commit hash 引用**：`.claude/`、`docs/repo-ops/` 等處以 hash 引用的證據，改寫為舊 repo 的完整 commit 網址。
5. **協作文件**：`LICENSE.txt` 改為 `Copyright (c) Polhem contributors`；新增雙語 `CONTRIBUTING.md` 與 `.github/CODEOWNERS`。
6. **ADR**：新增一份 ADR 記錄語言政策、plan 不入版控與長效決策升格 ADR 的約定（polhem-oauth2 有同類 ADR 可參考）。

## 階段 3：英文化

1. 測試 `[DisplayName]` 改英文；`tests/CLAUDE.md` 與 `.claude/rules/testing.md` 的命名規範同步。
2. 程式內殘留的中文註解：依註解規範判讀，只重述 WHAT 的刪除，WHY 改寫為英文。
3. `docs/repo-ops/`（含 `gotchas/`、`future-work.md`）改英文。`future-work.md` 裡屬於 Bee 時期、與 Polhem 無關的段落（本 plan 所在的「開放共同維護」一節）刪除或改寫為現況。

量大，依專案分批提交；每批 build 與測試全綠。

## 階段 4：公開文件

1. **`docs/<lang>/` 改以英文為源**：`check-docs-i18n.sh` 的源語言與蓋章方向對調，現有譯本重新蓋章；`.claude/rules/public-docs.md` 同步。
2. **ADR 補英文版**並納入 `check-docs-i18n.sh` 的檢查範圍；ADR 內指向舊 repo 的完整網址不動。
3. **README 雙語**（根目錄與各專案）：改名、徽章、安裝指令；新增「從 Bee.NET 遷移」一節，涵蓋：
   - 套件與命名空間 1:1 對照。
   - 要手動改的字串類別與 grep 指令：定義檔的 `BusinessObject`、自訂的組件限定型別名稱、`BEE_MASTER_KEY` 等環境變數、`.editorconfig` 裡的 `BEE` 診斷代號、`dotnet-bee`。
   - 遷移當下線上 session 會失效（HKDF 標籤改名）。
   - wire 的型別名稱不相容：客戶端與伺服端要一起升級。
4. **CHANGELOG** 雙語 1.0.0 草稿（定稿在階段 7）。

## 階段 5：建立 repo 與 CI

1. 建立 `polhem-dev/polhem`（public），推送階段 1–4 的 commit。
2. **CI 改名**：`build-ci.yml`、`docs-check.yml`、`nuget-publish.yml` 的環境變數與路徑。第一次推送以 `[all-db]` 跑完整模式。
3. **SonarCloud**（需實測）：`polhem-dev` organization 綁定 GitHub org、建立專案、設定 `SONAR_TOKEN`，重建 quality profile／gate；
   `build-ci.yml` 的 `/k:`、`/o:` 改寫。確認 S125、S3776 等在舊專案標過的 False Positive／Won't Fix 要不要重標。
4. **分支保護與 PR 工作流**（需實測）：`main` 要求 `build` check 並開啟 `enforce_admins`；確認維護者自己的 PR 能走完。
5. **auto-merge**（需實測）：依階段 0 的結果改寫 `auto-merge.yml` 的作者判斷與權杖。
6. **Trusted Publishing policy**（需實測）：比照 polhem-oauth2 階段 7，workflow 綁 `polhem-dev/polhem`。
   glob 不能涵蓋 `Polhem.OAuth2*`（那是另一個 repo 的 policy）；確認 dotnet tool 套件也能以同一條 policy 推送。**首發前不設任何發佈用 secret。**

## 階段 6：polhem-connector-js

1. 以 `jeff377/bee-connector-js` 的追蹤檔為起點，改名另開 `polhem-dev/polhem-connector-js`：套件名 `@polhem/connector`、
   `fetch-fixtures.mjs` 的 `REPO` 改為 `polhem-dev/polhem`、型別名稱與文件改名。語言政策同框架。
2. 需實測：兩個 repo 的 CI 互相依賴的順序——框架先推新 fixtures，connector-js 再跟上；中間 connector-js 紅燈是預期的。
3. 框架端 `.claude/rules/serialization.md`、`wire-contracts/README.md`、`wire-fixtures/README.md` 的連結改指新 repo。
4. npm 發佈：現況是 `private`，本 plan 不發佈；是否發佈另行決定。

## 階段 7：首發前健檢與修正

polhem-oauth2 的教訓：**健檢排在建立 PublicAPI 基準與打 tag 之前**，公開 API 一發佈就鎖定。

1. 以 `bee-framework-review` skill（改名後為 `polhem-framework-review`）做全面體檢；報告放在維護者本機、不入版控（含弱點細節）。
2. 修正範圍與批次由使用者依報告決定；1.0.0 未發佈，此時改公開 API 不算破壞性變更。
3. 重建 `PublicAPI.Shipped.txt`（1.0.0 的公開 API），Unshipped 清空。
4. **每一種 client 類型都要實測**：桌面 Avalonia、iOS、Android、Browser（WASM）、Blazor Server、JS（`Web.Js.Demo` 與 connector-js），
   以及 Local 與 Remote 兩種連線。
5. **打開 nupkg 檢查**：圖示、README、nuspec 的作者／授權／repository、相依清單不得出現任何 `Bee.*`。
6. CHANGELOG 1.0.0 定稿。

## 階段 8：首發 1.0.0

1. **NuGet `Polhem.` 前綴保留重新申請**（使用者決定，2026-09-26）：寄信到 `account@nuget.org`，**由使用者寄出**。
   核准不是首發的前提，未核准也能發佈，只是套件不會顯示 verified；核准需要時間，所以提早在階段 5 前後寄出較好。
2. clean build 與完整模式 CI 全綠；紅燈是訊號，不為發版而改測試或原始碼。
3. **推送 `v1.0.0` tag 須使用者明確同意**，發佈後無法撤回。
4. 驗證 nuget.org 上各套件頁、相依清單；在全新專案安裝並跑最小範例（含 `dotnet tool install Polhem.Cli`）。
5. 更新 org profile（`polhem-dev/.github`）：框架由「準備中」改為已發佈；中英互連用絕對網址，發佈後才推送。

## 階段 9：polhem-northwind

以 `jeff377/bee-northwind-avalonia` 的追蹤檔為起點，改名另開 `polhem-dev/polhem-northwind`，
`PackageReference` 改為 `Polhem.*` 1.0.0；框架內 `apps/Polhem.Northwind` 與鏡像的同步方式沿用 `docs/repo-ops/gotchas/northwind-heads.md`。
CI 綠燈，各 head 至少啟動一次。

## 階段 10：回寫 bee-library 並凍結

**回寫要在 archive 之前**，archive 後 repo 唯讀。

1. **回寫 bee-library**：`docs/repo-ops/future-work.md` 的「開放共同維護」一節改為結果摘要，指向新 repo；
   「要等什麼」與「帶完整 git 歷史」兩處依本 plan 的決策更正。本 plan 標記完成、更新索引。
2. **（使用者操作或經同意後由 agent 操作）** 凍結起點時 bee-library 會產出的 `Bee.*` 套件（含 `Bee.Cli`）全版本 deprecated：原因 Legacy，替代套件為對應的 `Polhem.*`，版本選 Latest。
   清單以凍結起點時 `src/` 與 `tools/Bee.Cli` 的可打包專案為準，不以 NuGet 上 `jeff377` 名下的套件清單為準。
3. 舊 repo README 頂部加中英雙語停止維護提醒與新舊套件對照表：`bee-library`、`bee-connector-js`、`bee-northwind-avalonia`、`bee-jsonrpc-sample`。
4. 仍開著的 issue 回覆指路後關閉。
5. 最後才 archive 四個舊 repo。不刪除：fork、星數與既有連結都依附在它上面。
6. 舊的 `NUGET_API_KEY`：**撤銷前先向使用者確認**，secret 的設定日期看不出 key 是否相同，也看不出還有誰在用。
7. 舊 SonarCloud 專案的處置：詢問使用者。
8. `jeff377/bee-library-private`：卸載同步用的 launchd agent（`bootstrap/install-sync-agent.sh` 裝的那個；2026-09-26 查過，本機沒有安裝），確認 `docs/internal/` 與部落格都已在新位置、兩個 private repo 都有最新 commit 後，archive。

## 範圍外

- connector-js 發佈到 npm。
- 發文公告。
- 套件重組與命名調整：階段 1 刻意不做，由階段 7 健檢提出。
- 本機 `~/Desktop/repos` 以外的 `bee-*` repo（使用者決定，2026-09-26）。
- 不由現行 bee-library 產出的 `Bee.*` 套件，是否 deprecated、替代套件指向哪裡，另行決定：
  舊版的舊名（例如停在 3.6.2 的 `Bee.Define`、`Bee.Cache`）、已從 repo 移除的套件、由其他 repo 發佈的套件，以及與框架無關的套件。
  2026-09-26 查 NuGet，`jeff377` 名下有這幾類；`Bee.OAuth2.*` 已由 polhem-oauth2 處理。
