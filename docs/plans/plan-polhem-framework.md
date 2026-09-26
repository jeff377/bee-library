# 計畫：bee-library 改名為 Polhem 並另開於 polhem-dev

**狀態：🚧 進行中（2026-09-26）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 0 | 啟動前置：商標重查、凍結起點、外部帳號與授權的事前確認 | ✅ 已完成（2026-09-26） |
| 1 | 本機建立 polhem：純改名（1:1 前綴替換，含編譯器抓不到的字串），diff 驗證零行為變更 | ✅ 已完成（2026-09-26） |
| 2 | agent 設定與協作文件：共用規則搬進 repo、`.claude/` 英文化、plan 慣例、LICENSE／CONTRIBUTING／CODEOWNERS | ✅ 已完成（2026-09-26） |
| 3 | 英文化：測試方法名稱與 `[DisplayName]`、程式內殘留的中文、維運文件與 gotchas | ✅ 已完成（2026-09-26） |
| 4 | 公開文件：`docs/` 改以英文為源、ADR 補英文版並納入雙語檢查、README 遷移說明 | ✅ 已完成（2026-09-26） |
| 5 | 建立 repo 與 CI：推送、SonarCloud、分支保護與 PR 工作流、auto-merge、Trusted Publishing policy | ✅ 已完成（2026-09-26） |
| 6 | polhem-connector-js 改名另開，接上新的 wire 合約 | ✅ 已完成（2026-09-26） |
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
| 階段 2～4 的提交方式 | polhem 在階段 5 建立 remote 之前，**直接 commit 本機 `main`**；`pull-request` 規則只寫目標工作流（分支 + PR + 分支保護），不寫過渡條款 | 使用者決定（2026-09-26）。repo 內不留日後要刪的暫時性文字；階段 5 推送後分支保護生效，自然轉為 PR |
| CODEOWNERS 與 CONTRIBUTING | CODEOWNERS 只列 `* @jeff377`，不預留共同維護者的寫法；CONTRIBUTING 寫給外部貢獻者 | 使用者決定（2026-09-26）。有共同維護者時再改，不預寫尚不存在的角色 |
| `check-public-docs.sh` 的 (7) | 改為**全 repo 禁止點名 plan 檔**，並新增「不得指向 `local/` 底下的檔案」；不再驗 plan 存不存在 | 使用者決定（2026-09-26）。plan 永遠不在 repo 內，CI 與其他人的 clone 也看不到 `local/`，任何點名對其他人都是死指標 |
| repo 內的 skill | 全部保留、只改名翻譯；清掉已停用 Routine 的殘留（`sonar-fix` 的 daily 模式、`.gitignore` 的 `.claude/logs/` 例外） | 使用者決定（2026-09-26） |
| 巢狀 `CLAUDE.md` | `src/Polhem.{Api.Core,Business,Definition,UI.Avalonia}/CLAUDE.md` 與 `tests/CLAUDE.md` **併入階段 2** 英文化 | 使用者決定（2026-09-26）。同屬 agent 設定，與 `.claude/rules/` 互相指路 |
| `CLAUDE.local.md` | 比照 polhem-oauth2 建立（gitignored），說明 `local/` 是私有 repo | 使用者決定（2026-09-26） |
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
     **更正（2026-09-26 實測）**：iOS head 預設 `TrimMode=partial`，連 Apple Release 也看不出來，改以單元測試把關，見階段 1 實作紀錄的待查項 4。
   - 嵌入資源：`Bee.Definition.csproj` 的 `LogicalName` 前綴 `Bee.Definition.Defaults/` 與 `Defaults.cs` 的 `ResourcePrefix`；
     DefineEditor 的 resx base name `Bee.DefineEditor.Resources.Strings`。
   - logger category（`BeeFrameworkApplicationBuilderExtensions` 的 `"Bee.Api.AspNetCore"`）與 analyzer 的 `category`。
6. **資料 key**：`DataColumnExtensions` 存進 `DataColumn.ExtendedProperties` 的 `"Bee.FieldDbType"`、
   `SerializationErrorData.FilePath` 的 `"Bee.FilePath"`（`Exception.Data` key）。
   **待查**：前者是否會隨 DataSet 的 XML 或 wire 序列化傳出去；會的話，新舊版本之間就是資料格式的差異。
   **已查（2026-09-26）**：會隨稽核 payload 落 DB，不會上 wire，見階段 1 實作紀錄的待查項 1。
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

### 實作紀錄（2026-09-26）

polhem 位於 `~/Desktop/repos/polhem`，`git init` 建立、沒有 remote。階段 1 共三個 commit，完成後另有一個：

| commit | 內容 |
|--------|------|
| `5242933` | 初始 commit：純改名，來源 `jeff377/bee-library@7d6cc9d9`，2517 個檔案 |
| `5f7c066` | 修正依賴執行順序的測試（只改測試，見「測試」） |
| `863edc4` | 補上「改錯會紅」的測試（只改測試，見「各類的檢查方式」） |
| `54fd32e` | 階段 1 完成後，主方案檔 `Polhem.Library.slnx` 改名為 `Polhem.slnx`（使用者要求，2026-09-26），引用處一併更新；下方紀錄裡的舊檔名是當時的狀態 |

`~/Desktop/repos/polhem-local` 未移動，依決策紀錄留到階段 2。

#### 替換

- 快照：以 `git archive 7d6cc9d9` 取出，刪去「不帶」的檔案後共 2517 個，與起點的 `git ls-files` 扣掉不帶的部分一致。
  `.claude/settings.local.json` 本來就沒進版控。
- 腳本（scratchpad，不進 repo）以位元組處理，保留 BOM 與編碼；binary 檔不動。內容改了 2259 個檔案，路徑以 `os.rename` 原地改名 144 處，沒有先刪再建。
  規則依序為：
  1. 保護區段不動。
  2. 特例：`https://github.com/jeff377/bee-library` → `https://github.com/polhem-dev/polhem`；`bee-library` → `polhem`；
     `Bee.NET`／`Bee.Net`／`BeeNET` → `Polhem`，`bee.net` → `polhem`；markdown 錨點裡的 `beedb`、`beeui` 等 → `polhem…`。
  3. 通則：`Bee`（後面不接小寫字母）→ `Polhem`、`bee`（前後都不是小寫字母）→ `polhem`、`BEE`（前後都不是大寫字母）→ `POLHEM`。
- 非品牌的命中只有英文的 `been`（62 處）與 `beers`（2 處），規則本來就不會改到。
- 使用者決定（2026-09-26）：
  - **指向現存外部實體的維持原樣**，屬於保護區段：bee-library 的 commit 網址、SonarCloud key `jeff377_bee-library`（含 README 徽章）、
    `bee-connector-js`、`bee-northwind-avalonia`、`bee-oauth2`／`Bee.OAuth2`。
    本機 Android AVD 名稱 `bee_pixel` 依同一原則保留，這是執行時才發現的。
  - **內文的 repo 名 `bee-library` → `polhem`**（約 45 處，含兩則執行期錯誤訊息）。
  - **`docs/repo-ops/future-work.md` 的「開放共同維護」一節整節維持原文**，留給階段 3。
- 個別處理（第 2 步）：`LICENSE.txt` 與套件中繼資料比照 polhem-oauth2，`Authors` 與 `Copyright` 都寫 `Polhem contributors`，
  `Company`／`Product` 寫 `Polhem`，`RepositoryUrl`／`PackageProjectUrl` 指 `polhem-dev/polhem`；`polhem.png` 取自 polhem-brand 的
  `png/nuget-package-icon-128.png`。`auto-merge.yml` 的 `jeff377` 與 Sonar 的 `/k:`、`/o:` 保留結構，留待階段 5。
- 環境變數、CI 容器名（`polhem-pg` 等）、測試密碼字串隨通則一併改名，**階段 5 第 2 步的 CI 改名大半已由機械替換完成**，階段 5 只需驗證。
  維護者本機的持久容器名是 `sql2025`／`pgvector-db`／`mysql8`／`oracle23ai`，不含 bee，**不需要重建**。
  本機 `.runsettings`（gitignored）由 bee-library 複製後以同一支腳本改名。

#### 零行為變更的 diff 驗證

把新樹複製一份，反向正規化（`Polhem`→`Bee`、`polhem`→`bee`、`POLHEM`→`BEE`，路徑同步反轉）後與快照 diff。
以字詞層級歸納，殘差只有以下幾類：

- 不可逆的品牌字串：`Bee.NET` 等變體 130 餘處、`bee-library` 約 45 處、repo 網址。
- `LICENSE.txt`、兩處中繼資料（`src/Directory.Build.props`、`tools/Polhem.Cli/Polhem.Cli.csproj`）與圖示。

再把這幾類不可逆的對映也套到快照上後重新比對，剩下的只有 LICENSE、中繼資料與圖示，**沒有 plan 沒預期的殘差**。
另外以同一支腳本重建一份到 scratchpad，與 polhem 比對一致（只差 Finder 產生的 `.DS_Store`），確認替換可重現。

wire fixtures 與 contracts 以 `POLHEM_REGENERATE_WIRE_*` 重生，結果與機械替換的內容逐位元組相同。

#### 各類的檢查方式（對應「編譯器抓不到的地方」）

以突變測試查證：每次只把一處改回 `Bee`，跑對應的測試專案，看會不會紅。「補測試」欄指 `863edc4` 新增或加強的測試，也都以同樣方式確認過會紅。

| 類別 | 既有的檢查 | 補測試 |
|------|-----------|--------|
| 1 定義檔的型別名 | 無自動檢查；Northwind 四個 head 實際跑過 | `DefinitionFileTypeReferenceGateTests`：掃描各 `Define/` 目錄，型別名要能對到存在的專案與型別宣告 |
| 2 `BackendDefaultTypes` | PublicAPI analyzer（RS0016／RS0017）；連同 PublicAPI 一致地改錯時由 `BackendDefaultTypesGateTests` 擋下 | — |
| 3 wire 內容 | Wire 測試（fixture 與 `type-names.ts` 各改一處都會紅） | — |
| 4 型別全名字串 | `DefineTypeExtensions`；analyzer 11 個名稱中的 9 個 | analyzer 的 `KeyCollectionBase`1`、`KeyCollectionItem` 各補一個案例 |
| 5 組件名與資源名 | SysInfo 白名單（Api.Core 72 項紅）、`Defaults` 資源前綴、`DefinitionAssemblyName` | ILLink descriptor（`TrimmerDescriptorGateTests`）、DefineEditor resx base name、logger category |
| 6 資料 key | `FilePath`：PublicAPI | `FieldDbType` key（含寫進 XML schema 的 `msprop` 名稱） |
| 7 環境變數 | — | `POLHEM_MASTER_KEY` 預設名稱；**`POLHEM_TEST_CONNSTR_*` 移交階段 5**（見下） |
| 8 診斷代號與 MSBuild 名稱 | 診斷代號：analyzer release tracking（RS2000／RS2003）；POLHEM9001 閘門見待查項 3 | `DiagnosticIdDocumentationTests`：`docs/*/analyzer-rules.md` 與 `DiagnosticIds`、閘門 targets 一致 |
| 9 HKDF 標籤 | — | 以字面標籤獨立重算金鑰（known-answer） |
| 10 工具、檔案系統與 UI 名稱 | — | `dotnet-polhem` 與說明文字一致、DefineEditor 設定資料夾、Blazor Demo 樣式表的選擇器都對得到元件的 class |
| 11 外部服務識別 | 階段 5／6 | — |
| 12 品牌字串與圖示 | diff 驗證；nupkg 檢查在階段 7 | — |

與 plan 原文不符、實測後更正的地方：

- 第 1 類：`tests/Define/` **沒有**組件限定的型別名，只出現在 `samples/Define/` 與 `apps/Polhem.Northwind/Define/` 的 `ProgramSettings.xml`。
- 第 8 類：repo 內的 `.editorconfig` **沒有**任何 POLHEM 代號。消費端是照 `analyzer-rules.md` 抄代號進自己的 `.editorconfig`，所以檢查對象改為該文件。

#### 建置與測試

- clean Release build（`--no-incremental`）：`Polhem.Library.slnx`、`Polhem.Tools.slnx`、`Polhem.Samples.slnx` 都是 0 錯誤。
  polhem 目前每個專案有兩則 SourceLink 警告（「存放庫沒有遠端」），scratchpad 的 clone 設好 remote 之後就是 **0 警告**；階段 5 加上 remote 即消失。
- Northwind 方案（含 iOS／Android head）0 錯誤；警告全在 iOS head，種類與數量逐項和原快照相同，沒有出現 IL2007。
- `./test.sh` 四種資料庫：原快照在同一台機器上 18 個測試專案共 6293 項、略過 1 項、全數通過；
  初始 commit 的測試數與略過數逐專案相同，但 `ApiServiceOptionsTests.DefaultImplementations_AreBuiltInTypes` 穩定失敗。
  - 根因是早就存在的缺陷：`ApiServiceOptions.Initialize(serializer, compressor, encryptor)` 依序賦值，`Initialize_NullEncryptor_Throws` 在丟例外前
    已裝上 `NoCompressionCompressor` 且沒有還原。xUnit 在同一類別內依測試 ID 排序，命名空間改名後順序改變，問題才浮現。production 行為沒有變。
  - 使用者決定（2026-09-26）：初始 commit 維持純改名，修正另成 commit（`5f7c066`，只改測試）。
  - 修正後 Api.Core 870 項全過。補測試後（`863edc4`）再跑一次完整 `./test.sh`，結果見下方「最終測試」。
- DefineEditor：headless smoke 全部 OK，視窗版能開出主視窗。
- Northwind 各 head 都實際啟動：Server（Release）；iOS（Release，模擬器）、Browser（WASM）、Android（Release，emulator `bee_pixel`，以 `adb reverse` 連回 5100 埠）
  都走完連線 → 登入 → 選單 → 清單；iOS 另外開了主從明細。Desktop 行程有開出主視窗，但沒有螢幕錄製權限，截不到畫面內容。

#### 待查項目的結論

1. **`Bee.FieldDbType` 會落 DB，不會上 wire**。JSON 與 MessagePack 的 DataTable 各自帶 type 欄，不帶 ExtendedProperties 的 key。
   稽核的 `AuditDiffGram` 以 `WriteXmlSchema` 寫出 `msprop:Polhem.FieldDbType="Date"`，存進 `st_log_change.changes_xml`（以實際 payload 確認）。
   把 key 換回舊名後照 `ChangeDiffGramReader` 的讀法載入，欄位型別與新舊值都相同，只有 `GetDeclaredFieldDbType()` 由 `Date` 變成 `null`，
   而讀取端以 CLR 型別輸出文字、不使用這個標記。使用者決定照決策改名，舊稽核資料留著 `Bee.FieldDbType`，成為沒有作用的註記。**寫進階段 4 的遷移說明。**
2. **analyzer 名稱錯時，11 個中有 2 個（`KeyCollectionBase`1`、`KeyCollectionItem`）不會讓 `Polhem.Analyzers.UnitTests` 紅**，已補測試。
3. **POLHEM9001 閘門**：
   - 啟用條件的屬性名或受鎖專案名不一致時，build 閘門會靜默關閉（0 錯誤），但 `DefinitionDependencyGateTests` 會紅（4 項中 3 項）。
   - allowlist 的項目名不一致時，清單變空、把所有參考都判為違規，build 直接紅。
   - 所以改錯都有東西會紅，只是前者要到測試才會。
   - `Polhem.Definition.targets` 的消費端設定（`PolhemDefinitionFilesGlob` 等）在 repo 內沒有專案匯入，只能透過 NuGet 的 `buildTransitive` 驗證，
     留給階段 7／8 的 nupkg 實裝測試。沿用舊名 `Bee*` 的消費端設定會被靜默忽略，**寫進階段 4 的遷移說明**。
4. **ILLink descriptor 的組件名，plan 原本的「只有 iOS Release trim 看得出錯」不成立**：
   - iOS head 的 `TrimMode` 是 `partial`，Polhem 組件不會被修剪。bundle 內 `Polhem.Definition` 與未修剪前同為 310 個型別，descriptor 寫錯也看不出來。
     正確版本在模擬器上實測可用。
   - 以 `TrimMode=full` 建置時，組件名留成 `Bee.Definition` 會只剩 54 個型別，建置仍是 0 錯誤，只多一則 IL2007 警告。
   - 全 trim 下 Northwind 不論 descriptor 對錯都會在啟動時顯示 `View not found`（App 自己的 ViewLocator 被修剪），所以在模擬器上跑 App 分辨不出對錯。
   - 因此改用單元測試直接斷言 descriptor 的內容（`TrimmerDescriptorGateTests`）。

#### 移交後續階段

- **階段 2**：`check-md-links.sh` 有 7 個死連結，都指向沒帶的 `docs/plans/`、`CHANGELOG*`、`docs/changelogs/`。`check-public-docs.sh` 的 (1) 仍說明 `plans/` 資料夾。
- **階段 4**：
  - 遷移說明補三項：`FieldDbType` 舊 key、`Bee*` MSBuild 設定名、DefineEditor 設定資料夾改名後舊設定不再讀取。
  - `check-docs-i18n.sh` 在 polhem 判定全部譯本過期：蓋章與 git 歷史綁定，而新 repo 沒有歷史。改以英文為源時一併重新蓋章。
- **階段 5**：
  - `POLHEM_TEST_CONNSTR_*` 名稱對不上時，`DbFact` 會把 DB 測試全部略過而維持綠燈。這次本機有直接證據（略過數與基準相同），但 CI 上沒有機制會紅。
    使用者決定在階段 5 改 CI 時處理（見階段 5 第 2 步）。
  - SourceLink 警告在加上 remote 後消失，第一次 CI 要確認是 0 警告。
- `check-xmldoc-refs.sh` 回報的 3 筆（`DECAN`、`IXmlSerializable`、`ReadXmlDiffgram`）在 bee-library 起點就有，與改名無關。

#### 最終測試

`863edc4` 上的 `./test.sh`（四種資料庫）：18 個測試專案全數通過，共 6311 項（原快照 6293 項，加上新增的 18 項），略過 1 項，與基準相同。

## 階段 2：agent 設定與協作文件

1. **共用規則搬進 repo**：使用者層 `~/.claude/rules/` 的 `code-style`、`scanning`、`single-source`、`pull-request`、`releasing`
   以英文**複製**進 `.claude/rules/`（使用者層仍供其他 repo 使用，不刪）。`pull-request` 改為 PR + 分支保護的工作流。
2. **`.claude/` 英文化**：`CLAUDE.md`、`rules/`、`skills/`（`bee-*` 改名 `polhem-*`）、`commands/`、`hooks/` 的訊息與註解。
   `CLAUDE.md` **明文覆寫**使用者層「敘述文字全部繁體中文」的預設。巢狀的 `CLAUDE.md` 一併處理（見決策紀錄）。
3. **plan 與個人文件慣例寫進 repo**：`.gitignore` 加 `/local/` 與 `CLAUDE.local.md`，拿掉 `docs/internal/` 那條（`docs/blogs/` 已於 2026-09-26 在 bee-library 移除）；
   `.gitignore` 生效後，把暫放的 `~/Desktop/repos/polhem-local` 移為 `local/`，以 `git status` 確認它沒有出現；`CLAUDE.md` 比照 polhem-oauth2 的「Local working documents」一節，
   寫明 `local/` 的用途、計畫目錄、不 commit 也不 `git add -f`、不從已 commit 的檔案連過去、worktree 讀不到 `local/`；
   `.claude/rules/public-docs.md` 的「哪些不是」表、`check-md-links.sh` 與 `check-public-docs.sh` 對 `docs/internal/` 的排除一併改寫；
   `settings.json` 移除 `dev-workflow` 的 marketplace 與啟用宣告。`check-public-docs.sh` 的 (7) 死指標檢查改對 `local/plans/` 或移除，實作時判斷。
4. **commit hash 引用**：`.claude/`、`docs/repo-ops/` 等處以 hash 引用的證據，改寫為舊 repo 的完整 commit 網址。
5. **協作文件**：`LICENSE.txt` 改為 `Copyright (c) Polhem contributors`；新增雙語 `CONTRIBUTING.md` 與 `.github/CODEOWNERS`。
6. **ADR**：新增一份 ADR 記錄語言政策、plan 不入版控與長效決策升格 ADR 的約定（polhem-oauth2 有同類 ADR 可參考）。

### 實作紀錄（2026-09-26）

polhem 階段 2 共七個 commit，依序直接提交本機 `main`（見決策紀錄）。每個 commit 前以 `Polhem.slnx` 的 clean Release build
（`--no-incremental`）確認 0 錯誤，警告只有「存放庫沒有遠端」的 SourceLink 兩則。

| commit | 內容 |
|--------|------|
| `bdc4f31` | `.gitignore`：加 `/local/`、`CLAUDE.local.md`，拿掉 `docs/internal/` 與 `.claude/logs/` 例外，中文註解改英文 |
| `cab8260` | commit hash 引用改為 bee-library 的完整 commit 網址 |
| `2fbd2df` | `check-public-docs.sh` 改寫、`check-md-links.sh` 檔頭英文化並拿掉 `docs/plans/archive/` 排除、7 個死連結與 plan 點名、`rules/public-docs.md` 改寫 |
| `f174c6b` | ADR-045（中英兩份）與 ADR 索引 |
| `0f3382f` | `.claude/` 與五份巢狀 `CLAUDE.md` 英文化、五條共用規則搬進 `.claude/rules/`、`settings.json` 移除 `dev-workflow` |
| `b1df16b` | `CONTRIBUTING.md`／`CONTRIBUTING.zh-TW.md`、`.github/CODEOWNERS` |
| `320e3e3` | `docs/repo-ops/future-work.md` 檔頭改指 `local/plans/`（查證項 3 查到的殘留） |

#### 各步驟

1. **共用規則**：`code-style`、`scanning`、`single-source` 由使用者層翻譯後調整為本 repo 的規則（語言段落對齊語言政策、`Bee.*` 範例改 `Polhem.*`，
   事件發生地的 bee-library 保留）。`pull-request` 重寫為分支 + PR + 分支保護；`releasing` 只留兩條防護欄，原本指向 `/dev-workflow:release` 的完整流程不再引用。
   `.claude/CLAUDE.md` 以 `@` 載入這五份，並寫明「與個人或使用者層規則同主題時，以 repo 的為準」。
2. **英文化**：由平行子代理依同一份翻譯要點逐檔翻譯，主 session 驗收。驗收內容：
   - 殘留中文只剩資料（`polhem-scaffold-from-formschema` 的中文 caption 與對照表、`polhem-framework-review` 的狀態列樣本）。
   - 所有 skill 與 command 的 frontmatter 以 YAML parser 載入成功；`§` 章節引用逐一對到實際標題。
   - `[DisplayName]` 的語言：`rules/testing.md` 已寫「新測試用英文」，`polhem-scaffold-from-formschema` 的樣板一併改英文；既有測試的中文 `[DisplayName]` 仍待階段 3。
   - 個人 memory 的引用（`categoryid-is-db-scope-selector` 等）改指 `.claude/rules/database.md`／`definition.md`。
   - hook 的阻擋與提示訊息改英文；`skills/README.md` 拿掉 plugin 一節，並去掉 `polhem-add-form` 的處數（與 skill 本文不一致）。
3. **plan 與個人文件慣例**：
   - `.gitignore` 生效（`git check-ignore -v local/plans/x.md` 命中 `/local/`）後，以 `mv` 把 `~/Desktop/repos/polhem-local` 移為 `polhem/local`。
     移動前後 polhem-local 的工作樹乾淨、沒有 stash，remote 仍是 `jeff377/polhem-local`、`git log` 仍是 `2dd279b`、`git fetch` 後與 `origin/main` 同步；
     polhem 的 `git status` 看不到 `local/`（`--ignored` 列為 `!! local/`）。
   - `CLAUDE.local.md` 比照 polhem-oauth2 建立（gitignored）。`.claude/CLAUDE.md` 新增「Plan before you build」與「Local working documents」兩節。
   - `check-public-docs.sh` 改為四道：(1) 全 repo 點名 plan 檔、(2) 全 repo 指向 `local/` 底下的檔案、(3) 公開文件指向 `.claude/` 底下的檔案、(4) 公開文件以散文提到 plan（提示性質，有已知誤報）。
     (1)～(3) 有命中時 exit 1（原本一律 exit 0）。指向 bee-library 的外部網址不算命中。三道都以暫時的探針檔確認會抓到。
     (3) 原本連目錄名稱都報，改為只報指向檔案，目錄名稱用來描述慣例時放行（ADR-045 需要提到 `.claude/`）。
   - `CONTRIBUTING` 的讀者是貢獻者而非套件使用者，在 `rules/public-docs.md` 列為「非公開文件」，可以指向 `.claude/`，但維持雙語。
   - 移交項的 7 個死連結：`docs/<lang>/README.md` 的 `changelogs/` 列與 `plans/` 說明刪除；`framework-reserved-names.md` 的 CHANGELOG 連結改為純文字；
     `docs/repo-ops/` 下指向封存 plan 的連結改為釘在凍結起點的網址 `https://github.com/jeff377/bee-library/blob/7d6cc9d9/docs/plans/archive/<檔名>`（四份都確認在 `7d6cc9d9` 存在）；
     已移交 local 的 `plan-rounding-mode`、`plan-tree-view-builder` 改寫為「另案規劃」。完成後 `check-md-links.sh` 0 筆。
4. **commit hash 引用**：共 21 個不重複的 hash、28 處，其中 adr-009 的 3 處原本就是完整網址，其餘 25 處改寫
   （`.claude/rules/definition.md`、`build-ci.yml` 的註解、7 份 ADR、`docs/repo-ops/` 6 份）。
5. **協作文件**：`LICENSE.txt` 在階段 1 已是 `Copyright (c) Polhem contributors`，未再改動。
6. **ADR**：`docs/adr/adr-045-language-policy-and-local-plans.md`（英文）與 `.zh-TW.md`（中文），檔名比照 polhem-oauth2 的慣例；索引只補一列。

#### 查證項目的結論

1. **commit hash**：21 個 hash 都以 `git merge-base --is-ancestor <hash> origin/main` 確認在 bee-library 的 `main` 上，
   再以 `gh api repos/jeff377/bee-library/commits/<hash>` 確認 GitHub 能解析，逐一比對 commit 訊息與引用處的脈絡相符。
2. **hook**：從本 session（cwd 為 bee-library）以 `cd /Users/jeff/Desktop/repos/polhem && git commit ...` 的 payload 直接執行 polhem 的 hook：
   放一個語法錯誤的 `.cs` 時 exit 2 並印出英文阻擋訊息，移除後 exit 0。
   **注意**：本 session 實際執行 commit 時生效的是 bee-library 的 hook，它會跟著 `cd` 找到 polhem，但找不到 `Bee.Library.slnx` 就放行，
   所以每個 commit 前都手動跑 clean build。在 polhem 開的 session 才會由 polhem 自己的 hook 把關。
3. **`dev-workflow` 殘留**：`settings.json` 移除宣告後，全 repo 只剩 `future-work.md`「開放共同維護」一節提到 `dev-workflow`、`plan-write` 與使用者層規則，
   那一節依階段 1 的決定維持原文、留給階段 3；檔頭那句指示已修正（`320e3e3`）。

#### 移交後續階段

- **階段 3**：
  - `docs/repo-ops/`（含 `future-work.md`「開放共同維護」一節）、`test.sh`、`check-docs-i18n.sh`、`check-xmldoc-refs.sh` 的中文註解。
  - 既有測試的中文 `[DisplayName]`；`rules/testing.md` 已先寫「新測試用英文」，`tests/CLAUDE.md` 沒有指定語言。
- **階段 4**：
  - ADR 索引 `docs/adr/README.md` 仍只有中文；ADR-045 已採 `name.md` 英文、`name.zh-TW.md` 中文的檔名，其餘 ADR 補英文版時可比照。
  - CHANGELOG 建立後，`docs/*/framework-reserved-names.md` 可恢復 CHANGELOG 連結；README 可加 CONTRIBUTING 的連結。
- **階段 5**：
  - `rules/pull-request.md` 寫的是目標狀態：`main` 要求 `build` check 且 `enforce_admins` 開啟。建 repo 時要讓設定與它一致。
  - CODEOWNERS 只會發出審查請求；要不要開「Require review from Code Owners」一併決定。
  - `check-public-docs.sh` 還沒進 CI（`docs-check.yml` 只跑 `check-md-links.sh` 與 `check-docs-i18n.sh`），要不要加入一併決定。
  - `/sonar-fix` 的 SonarCloud key 仍是 `jeff377_bee-library`，與 `build-ci.yml` 一起改。
- **過渡期的已知現象**：在 polhem 開的 session 仍會載入使用者層 `~/.claude/rules/`（中文），與 repo 內的英文規則重複；
  使用者層 `pull-request` 的「桌面環境直接推 main」與 repo 的 PR 工作流衝突。`.claude/CLAUDE.md` 已寫明以 repo 規則為準。
- **翻譯時發現、未修的內容問題**（子代理回報、照原意翻譯，建議在階段 7 健檢時一併處理）。除標「未查證」者外，都已對照原始碼確認：
  - `polhem-add-bo-method`：wire DTO 樣板仍用 `[MessagePackObject(keyAsPropertyName: true)]`，沒提 `WireContracts.*.cs` 顯式註冊（adr-036／037）；
    `[Key(n)]` 與同步 wrapper 的說法前後矛盾；`[ApiAccessControl]` 的選項漏了 `LocalOnly`；有寫死行號的引用。
  - `polhem-add-cache-object`：兩個 CacheNotify 測試 stub「必補」與同檔 2026-08-06 覆核「已不存在」矛盾。
  - `polhem-add-form`：完成 checklist 寫 4 個檔案，本文寫 5 處。
  - `polhem-framework-review`：寫死 17 個專案、30 條相依邊等清點數字；「預設 wire 是 MessagePack」可能已過時（adr-044）。
  - `polhem-jsonrpc-backend`：`ApiServiceOptions.Initialize` 與 `SystemSettings.xml` 被描述為決定 payload codec，需對照 adr-044 的逐請求協商重新確認（未查證）；引用 Bee 時期的版號 4.14.0、4.12。
  - `polhem-sample-add`：自訂 TableSchema 放 `samples/Define/TableSchema/common/`，與「業務資料一律 company」的規則看似衝突，是否為 samples 刻意的設計未查證；README 樣板沒有 `README.zh-TW.md`。
  - `/sonar-fix`：列出不存在的路徑（`Polhem.Base/Cryptor/*`、`Polhem.Api.Core/Session/*`），寫死模型名稱。
  - `src/Polhem.Definition/CLAUDE.md` 提到的 `MessagePackKeyCollectionBase`／`MessagePackCollectionBase` 已不在 `src/`。
  - 已退役的 analyzer 代號 `BEE4001`–`BEE4004` 隨階段 1 機械替換成 `POLHEM4001`–`POLHEM4004`（含 `AnalyzerReleases.Shipped.md`），這些代號在 Polhem 從未存在過。

## 階段 3：英文化

1. 測試 `[DisplayName]` 改英文；`tests/CLAUDE.md` 與 `.claude/rules/testing.md` 的命名規範同步。
2. 程式內殘留的中文註解：依註解規範判讀，只重述 WHAT 的刪除，WHY 改寫為英文。
3. `docs/repo-ops/`（含 `gotchas/`、`future-work.md`）改英文。`future-work.md` 裡屬於 Bee 時期、與 Polhem 無關的段落（本 plan 所在的「開放共同維護」一節）刪除或改寫為現況。

量大，依專案分批提交；每批 build 與測試全綠。

### 實作紀錄（2026-09-26）

polhem 階段 3 共 12 個 commit，依序直接提交本機 `main`（見決策紀錄），761 個檔案。翻譯由平行子代理依同一份翻譯要點逐檔進行，主 session 驗收。

驗證方式：子代理同時在工作樹上改檔，工作樹不能拿來逐批建置。所以每一批先套到 scratchpad 裡的另一份 clone（只含已驗證的批次）並在那裡 commit：
- clean Release build（`--no-incremental`）0 錯誤，警告只有 SourceLink 的「存放庫沒有遠端」。
- 跑該批相關的測試專案，以 TRX 比對改前改後：測試數、略過數、以「類別＋方法名」計的測試清單都相同；顯示名稱（Theory 的參數以外）沒有中文。

全部完成後，確認 polhem 工作樹與 clone 的 HEAD 無差異，再以相同的檔案清單與順序提交到 polhem 的 `main`（每次都經過 polhem 自己的 pre-commit hook），
最後確認 12 個 commit 的 tree hash 與 clone 逐一相同。

| commit | 內容 | 測試（改前＝改後） |
|--------|------|------|
| `47dfd8b` | 建置設定、`.editorconfig`、`SonarQube.Analysis.xml`、三個 workflow、`test.sh`、`check-docs-i18n.sh`、`check-xmldoc-refs.sh`、`docs/.sonar-fix-state/skip.json`、`JsonRpcErrorContract` 的一處 XML doc | Api.Core 870、Analyzers 116；兩支腳本的輸出行數與改前相同 |
| `31c1948` | Avalonia.DemoCenter（註解與全部 UI 文字、`.smoke.yaml`）、DefineEditor、`tools/Directory.Build.*`、DefineEditor 測試 | DefineEditor 12；tools、samples 方案建置 0 錯誤；DefineEditor headless smoke OK |
| `c3569d7` | `docs/repo-ops/`（含 gotchas），`future-work.md` 刪去「開放共同維護」一節 | `check-md-links.sh`、`check-public-docs.sh` 0 筆 |
| `966e107` | Polhem.Definition 測試 | 1120 |
| `d71cbe7` | UI.Avalonia、UI.Core、Web.Blazor.Server 測試 | 582 |
| `958e607` | ObjectCaching、Hosting 測試 | 297 |
| `786f977` | Business 測試 | 688（略過 1） |
| `26c6c9c` | Repository、Api.Client、Api.AspNetCore 測試 | 453 |
| `7103f7b` | Db 測試 | 1422 |
| `041890b` | Api.Core 測試 | 870 |
| `4ac7025` | Base、Expressions、Cli、LoadTests、Analyzers 測試、`Polhem.Tests.Shared`、`tests/Directory.Build.props` | 867 |
| `b3aa1dc` | `tests/CLAUDE.md` 寫明 `[DisplayName]` 的寫法：英文一句、sentence case、現在式、不加句點 | — |

`rules/testing.md` 在階段 2 已寫「英文」，這次只在 `tests/CLAUDE.md` 補寫法，不重複。

#### 範圍的決定（使用者，2026-09-26）

- `future-work.md` 的「開放共同維護」一節**整節刪除**，包含名稱與帳號表、商標限制、bee-oauth2 演練結果。
- `docs/repo-ops/` 內容過時或與程式碼矛盾時，**只翻譯並記錄**，留給階段 7（見下方「未修的內容問題」）。
- samples 與 tools 裡寫死的中文 UI 文字**翻成英文**：
  - DemoCenter 全部模組，`.smoke.yaml` 同步。
  - DefineEditor 屬性明細的全形冒號改為 `: `。
  - DefineEditor `Smoke.cs` 的樣本資料視同測試資料，保留。
- 框架內建定義檔 `src/Polhem.Definition/Defaults/` 與各 `Define/` 的中文 Caption／DisplayName **保留為資料，交給階段 7**。
  那是隨 NuGet 出貨的預設顯示文字，另有 zh-TW Language 檔；基底語言用哪一種屬於產品決策。

#### 刻意保留的中文（全 repo 掃描確認）

- 掃描範圍是 git 追蹤的所有文字檔。
- 唯一不是 UTF-8 的文字檔是 `tests/Polhem.Api.Client.UnitTests/HttpSamples/jsonrpc.http`（Big5），已對 `320e3e3` 的所有 blob 確認；它唯一一行中文註解譯掉後已是 ASCII，所以以 UTF-8 掃描不會漏檔。
- `tests/`、`src/`、`tools/`、`samples/`、`apps/` 裡，字串常值以外的中文與 `[DisplayName]` 裡的中文為 0 處。
  例外是兩處刻意引用資料的英文註解（下方第 3、8 類）。

保留的類別：

1. **公開文件（階段 4）**：`docs/zh-TW/`、`docs/adr/`、`docs/README.md`、`docs/en/` 的語言切換列、各 `README*.md`、`CONTRIBUTING*.md`。
2. **zh-TW 在地化資源**：DefineEditor 的 `Strings.zh-TW.resx`；`Language/zh-TW/*.xml`（`Defaults/`、`tests/Define/`、Northwind）。
3. **語言自稱**：`繁體中文` 等（`check-docs-i18n.sh`、DefineEditor 的 `Strings.resx` 與 `App.axaml.cs`、`nuget-publish.yml` 的 release notes 連結）。
4. **定義資料**：`src/Polhem.Definition/Defaults/`、`tests/Define/`、`samples/Define/`、Northwind 的 `Define/` 與 `Customize/`（見上方使用者決定）。
5. **測試輸入與預期值**：caption、種子資料、在地化值、Unicode 與控制字元輸入、內嵌 XML、與 SQL 或例外訊息比對的字串。
6. **程式的輸入資料**：`ValueUtilities` 把 `是`、`真` 解析為 true。
7. **掃描規則本身**：`check-public-docs.sh` 第 (4) 道以中文片語比對文件。
8. **英文散文裡引用的資料**：
   - `LocalizedListView.cs` 引用的 UI 值。
   - gotchas 裡中文語系的 `ORA-00932` 原文，以及文件圖上的字面文字。
   - `future-work.md` 的多語幣別名稱範例。
9. **`.claude/skills/`**：階段 2 已驗收為資料。

#### 測試

- 改前基準：`320e3e3` 的 clone 跑 `./test.sh`（四種資料庫），18 個測試專案共 6311 項、略過 1 項、全數通過，與階段 1 的最終測試相同。
  第一次改用 git worktree 跑，`TestProcessBootstrap.FindRepoRoot` 找的是 `.git` **目錄**，而 worktree 的 `.git` 是檔案，所以大量失敗；改用 clone 後正常。
- 完成後在 polhem 的 `b3aa1dc` 跑 `./test.sh`（四種資料庫）：共 6311 項、略過 1 項、全數通過；TRX 比對，測試清單與基準逐項相同。
- 被略過的 1 項是 `SystemBusinessObjectTests` 的 `Login_WithRsaKeyPair_...`，見下方未修的內容問題。

#### 註解、`[DisplayName]` 與程式碼不符之處（已照實際行為改寫）

以下只列實質落差。另外，大量清點數字（「三個」「五家」等）依規則改為列名稱或不寫數字，不逐一列。

- **Base／Expressions／Cli／LoadTests**：
  - `DataTableJsonConverterCoverageTests` 說涵蓋所有 FieldDbType，實際是每種基本 CLR 型別各一欄。
  - `SplitMenuMigrationTests` 被加序號的是項目 Id，不是分類 Id。
  - `VirtualUserPoolTests` 說虛擬使用者索引會繞回帳號池，但 `SignInAllAsync` 遇到第一個失敗（user 0）就停，繞回從未被檢查。
  - `AnalyzerRunner` 的 `<returns>` 說診斷依 ID 與位置排序，實際沒有排序。
- **Api.Core**：
  - `WireFormatterTests` 說檢查成員數，實際沒有檢查。
  - `MessagePackTests` 用舊型別名 `TListItemCollection` 等；`DataTable_SerializeWithDbNull_PreservesValues` 說「可寫回資料庫」，實際不碰資料庫也不經 MessagePack。
  - Logout 測試只斷言 session 被移除。
  - `EnterCompanyMessagePackTests` 只檢查三個欄位，而且 formatter 是手寫的，不是靠 `[Key]`。
- **Definition**：
  - `DtoPropertyTests` 斷言 Culture 與 TimeZone 為空字串，不是 zh-TW。
  - `MasterKeyProviderTests` 傳入的是空白字串 `"   "`，不是空字串，且斷言的是擲例外。
  - `FilterGroupTests` 用的是 `Contains`，不是開頭或結尾比對。
  - `CompanyInfoTests` 只有 XML 與 JSON 兩種 round-trip。
  - 已移除的 `DefinePathInfo` 不再以 cref 引用。
- **UI**：
  - `FormLiveComputationTests` 沒有測到重入。
  - `GridControlLookupTests` 綁的是唯讀的明細 grid，不是 list 模式。
  - `LookupPanelTests` 沒有檢查錯誤是否顯示。
- **ObjectCaching／Hosting**：
  - ObjectCaching 組件設了 `DisableTestParallelization`，各類別「可與其他類別平行」的說法拿掉。
  - `ObjectCacheTests` 的 fake 其實有 override `CreateInstance`。
  - 已移除的 `CacheContainer` static facade 不再被描述為存在。
- **Business**：
  - `IsLocalCall` 預設 true 的是 fake `TestableBusinessObject`，真正的預設是 false。
  - 無變更的 Save 是 no-op，不是擲例外。
  - `BoApiSurfaceTests` 那一項只比對 baseline，比對文件的是另外兩個 Theory。
- **Repository／Api.Client／AspNetCore**：
  - `ApiConnectorFinalizeResponseTests` 用的是 `MethodNotFound`，不是 ParseError。
  - `ApiKeyRepositoryTests` 檢查的是寫入前後，清理之後沒有檢查；summary 提到的「停用列被查詢層排除」測試不存在。
  - Api.Client 的 `AssemblyInfo.cs` 引用的行號已過時，已刪除。
- **Db**：Oracle 預設值測試只涵蓋「與內建預設相同」的情況。
- **工具與腳本**：
  - `.editorconfig` 的欄位命名規則順序是 const → static → 其餘（原文寫 static readonly）。
  - `check-xmldoc-refs.sh` 成功時會印一行 OK（原文寫完全無輸出）。
  - DefineEditor `Program.cs` 引用的 `App.ConfigureNativeAppMenu` 不存在，實際是 `App.ConfigureApplicationMenu`。

**方法名稱沒有改**（翻譯要點禁止改名），以下三組名稱本身仍帶著不符的宣稱：
- `Logout_AfterEnteredCompany_ClearsThenRemoves`
- `TListItemCollection_*` 等帶舊 `T` 前綴的方法
- `DataTableJsonConverterCoverageTests` 的 `…InStringColumn`

#### 移除的 plan 指涉

- 程式內有指向 Bee 時期 plan、PR 編號或階段的註解，例如「主計畫 §範圍邊界」「PR 5.7 後」「Phase 4 之後」「決策 L7」「原則 4」「計畫 §1.3」。都保留了實質內容，拿掉指涉。
- `check-public-docs.sh` 只抓點名 plan 檔的寫法，這類散文指涉它抓不到。
- 保留一處：`docs/repo-ops/gotchas/test-ci-release.md` 說明 bee-library 時期的健檢結果放在舊 repo 的 `docs/plans/archive/`。那是描述歷史的位置，不是點名某份 plan。

#### 未修的內容問題（建議階段 7 處理，除另註明者）

**維運文件**（依使用者決定只翻譯）：
- `gotchas/serialization-and-expressions.md`：
  - 說 `AddColumn` 存大寫，實際是 `ToLowerInvariant`。
  - `[Union]` 一節與 `FilterNodeFormatter` 的現況（手寫 formatter 加 `Kind` 判別碼）矛盾。
- `gotchas/northwind-heads.md`：
  - `SyncExecutor`、`RemoteDefineAccess` 已不存在。
  - 記的 Xcode 版本已過時。
  - 「三 head 一致」與同文的四個 head 不符。
- `gotchas/mobile-trim-aot.md`：iOS 專案路徑少了 `Polhem.Northwind/` 一層。
- `gotchas/definition-and-customization.md` 與 `gotchas/README.md`：仍寫舊的 `.zh-TW.md` 後綴與 `docs/` 根目錄。
- `gotchas/test-ci-release.md`：新增套件時要改「N 個專案」的步驟，本身違反 single-source，而且那些數字已不存在。
- `gotchas/database.md`：`TryCoerceToGuid` 那段的現況不明；另有「全部 5 個 provider」這個清點數字。
- `branch-protection-setup.md`：以 `enforce_admins: false`、直接推 `main` 為前提，與 `rules/pull-request.md` 矛盾。**階段 5 設分支保護時一併改寫。**
- `ci-sonarcloud-setup.md`：寫 Windows runner 加 PowerShell，實際是 `ubuntu-latest`；token 範例名稱新舊混用。**階段 5 一併改寫。**
- `testing-patterns.md`：
  - `[DbFact]` 樣板用的 `new DbAccess("common_sqlserver")` 已不存在（現在必須傳 `IDbConnectionManager`）。
  - `[LocalOnlyTheory]` 範例用的 API 名稱不對。
- `uom-decimals-prior-art.md`：
  - `ANDEC`／`DECAN` 的說明過時。
  - 「沒綁單位的數量退到公司位數」已不成立：現在沒有 `UnitField` 會擲例外。
- `future-work.md`：外部開發者 skill 包一節寫的錨點 repo 仍是 `bee-northwind-avalonia`。

**測試**：
- **`BuildSelectTests.BuildSelect_WithFilterAndSort_ReturnsCommands` 是真的缺陷**：建了 `command3` 卻斷言 `command2`，所以 `command3` 從未被檢查。只刪了註解，測試沒動。
- `SystemBusinessObjectTests` 被略過的 `Login_WithRsaKeyPair_...` 寫著「等有測試用子類別再啟用」，但 `Fakes/TestableSystemBusinessObject.cs` 已存在，這個 Skip 看來已過時。
- `JsonRpcExecutorCoverageTests` 的 remarks 說 fixture 必須是 `SharedDbFixture`，實際用的是 `PolhemTestFixture`；是否違反 `rules/testing.md` 第 1 條待查。
- `SysInfoStaticCollection` 的說明暗示有編譯期保護，但使用端寫的是字串常值 `[Collection("SysInfoStatic")]`，正是 `tests/CLAUDE.md` 警告的寫法。
- `SystemBusinessObjectDefineTests` 的斷言失敗訊息會印出密碼值（可能牴觸 `rules/scanning.md`）。
- 測試專案的 XML doc 警告被關掉，指向已移除型別的 cref 不會讓建置失敗，例如 `FileDefineStorageTests` 約第 330 行的 `DefinePathInfo`。
  另有兩處 `<remarks>` 以 `</summary>` 結束：`FormBusinessObjectPermissionGateTests`、`CacheNotifyReaderUnitTests`。
- 原本就是英文、這次沒動的錯誤說法：
  - `CustomizeDefineReaderTests` 說可平行執行。
  - `FormLiveComputationTests` 說 `AddColumn` 存大寫。
  - `VirtualUserPoolTests` 第 55–57 行說索引會繞回。
  - `DbDefineStorageTests` 有「out of this phase」。
- Blazor 測試裡「留待 bUnit 整合測試」的說法可能已過時：專案已經在用 bUnit。
- `ApiContractSerializationTests` 指向 sibling repo 的 `SoarCloud.Api.Core.Tests/...`。

**其他**：
- `nuget-publish.yml` 的 release notes 連到 `docs/changelogs/<版號>.md`，polhem 不帶這個目錄。**階段 4 建 CHANGELOG 時一併決定。**
- `SonarQube.Analysis.xml` 對 Blazor 元件的重複偵測排除，檔案自己寫著已無依據。**階段 5 第一次完整掃描後處理。**
- 內建定義檔的中文 Caption（見上方使用者決定）。

#### 移交後續階段

- **階段 4**：
  - 公開文件的中文仍在（上方第 1 類）。
  - `nuget-publish.yml` 的 changelogs 連結。
  - `samples/Avalonia.DemoCenter/README.md`（檔名是英文版，內容仍是中文）的主題表與說明，列的是舊的中文分類名（「控件類型」「資料繫結」「FormMode 顯示狀態」等）。
    App 裡已改為 `Control Types`、`Data Binding`、`FormMode States` 等，README 要跟著改（這次沒動 README）。
- **階段 5**：`branch-protection-setup.md`、`ci-sonarcloud-setup.md` 依實際設定改寫；`SonarQube.Analysis.xml` 的 Blazor 排除。
- **階段 7**：上方「未修的內容問題」。

## 階段 4：公開文件

1. **`docs/<lang>/` 改以英文為源**：`check-docs-i18n.sh` 的源語言與蓋章方向對調，現有譯本重新蓋章；`.claude/rules/public-docs.md` 同步。
2. **ADR 補英文版**並納入 `check-docs-i18n.sh` 的檢查範圍；ADR 內指向舊 repo 的完整網址不動。
3. **README 雙語**（根目錄與各專案）：改名、徽章、安裝指令；新增「從 Bee.NET 遷移」一節，涵蓋：
   - 套件與命名空間 1:1 對照。
   - 要手動改的字串類別與 grep 指令：定義檔的 `BusinessObject`、自訂的組件限定型別名稱、`BEE_MASTER_KEY` 等環境變數、`.editorconfig` 裡的 `BEE` 診斷代號、`dotnet-bee`。
   - 遷移當下線上 session 會失效（HKDF 標籤改名）。
   - wire 的型別名稱不相容：客戶端與伺服端要一起升級。
   - 階段 1 查到的三項：舊稽核資料的 `msprop:Bee.FieldDbType` 不再被讀取（讀出的欄位值不變）；專案檔裡的 `Bee*` MSBuild 設定
     （`BeeDefinitionFilesGlob` 等）會被靜默忽略；DefineEditor 的使用者設定資料夾改名，舊設定不再讀取。
4. **CHANGELOG** 雙語 1.0.0 草稿（定稿在階段 7）。

### 實作紀錄（2026-09-26）

polhem 階段 4 共五個 commit，依序直接提交本機 `main`（見決策紀錄），每個都經過 polhem 自己的 pre-commit hook（clean Release build）。
每個 commit 前 `check-md-links.sh`、`check-public-docs.sh`（(1)～(3) 為 0 筆）、`check-docs-i18n.sh` 都通過。

| commit | 內容 |
|--------|------|
| `5c0bf45` | `docs/<lang>/` 改以英文為源：`SOURCE_LANG="en"`、`TRANSLATIONS="zh-TW:strict"`；en 拿掉譯本標頭、zh-TW 蓋章、切換列改為英文在前；`rules/public-docs.md`、`.claude/CLAUDE.md`、`rules/database.md`、`future-work.md` 對「源」的說法同步 |
| `3acdd0d` | ADR 雙語：44 份中文 ADR 與索引改名為 `.zh-TW.md`，`<name>.md` 放英文版；`check-docs-i18n.sh` 加入後綴配對；中文文件改連中文 ADR；ADR-045 補「實作演進」；`CONTRIBUTING` 雙語同步 |
| `c05307d` | README：根目錄加遷移說明、安裝指令、新徽章、ADR／CONTRIBUTING／授權連結；DemoCenter、DefineEditor 改英文並補中文版，Northwind.Browser 補中文版；中文文件改連中文 README |
| `714ac6d` | CHANGELOG 雙語 1.0.0 草稿與 `docs/changelogs/1.0.0(.zh-TW).md`；`docs/changelogs/` 納入後綴配對；恢復文件索引的 `changelogs/` 列與 `framework-reserved-names.md` 的 CHANGELOG 連結 |
| `da5240c` | 英文文件引用 ADR-010 章節時改用英文章節名（全面掃描英文公開文件殘留中文時查到） |

#### 使用者決定（2026-09-26）

- **ADR 檔案結構：後綴慣例**（`name.md` 英文、`name.zh-TW.md` 中文），與 ADR-045、polhem-oauth2 一致。docs/adr 以外約 200 處對 ADR 的引用路徑不變，只有中文文件改指 `.zh-TW.md`。
- **zh-TW 政策：strict。**
- **CHANGELOG：根目錄＋`docs/changelogs/` 逐版明細**（沿用 Bee 時期的兩層結構）。`nuget-publish.yml` 的 release notes 本來就連 `docs/changelogs/<版號>.md`，建立 `1.0.0.md` 後即可解析，workflow 未改。
- **各專案 README：補三份中文版**（DemoCenter、DefineEditor、Northwind.Browser）；`wire-contracts`、`wire-fixtures` 維持只有英文。

#### 查證項目的結論

1. **`docs/en/` 沒有落後**：
   - 在凍結起點 `7d6cc9d9` 的 clone 上跑 `check-docs-i18n.sh`，exit 0，當時全部譯本都是新鮮的。
   - 把 polhem 的兩種語言反向正規化回 Bee 後，與凍結起點逐檔比對：zh-TW 與 en 改動的檔案、行數一一對應，內容都是品牌殘留（`Bee.NET`→`Polhem`）與階段 2 拿掉 `changelogs/`／`plans/`／CHANGELOG 連結，兩邊是同一批改動。
   - 所以英文改為源之前，內容確實同步；zh-TW 以此為據蓋章。
2. **遷移說明的每一項都對照原始碼**：
   - 型別與成員改名清單取自凍結起點與 polhem 的 `PublicAPI.*.txt`（`Bee` → `Polhem` 的 11 個型別與 3 個方法）。
   - `ProgramItem` 的 `BusinessObject`／`Repository` 屬性、`SystemSettings.xml` 的 `BackendComponents`（預設值來自 `BackendDefaultTypes`）。
   - `MasterKeyProvider` 在變數名稱空白時用 `POLHEM_MASTER_KEY`；Bee 的 `Defaults/SystemSettings.xml` 明寫 `<Value>BEE_MASTER_KEY</Value>`，所以以 `materialize` 產生設定的部署會繼續讀舊變數名。
   - MSBuild 屬性取自 `buildTransitive/Polhem.Definition.targets`；Blazor CSS class 取自 `Polhem.Web.Blazor.Server`；HKDF 標籤與「衍生金鑰不儲存」取自 `DerivedApiEncryptionKeyProvider`；wire 白名單為 `SysInfo.AllowedTypeNamespaces`；`FieldDbType` key 與 `st_log_change.changes_xml` 對照階段 1 的結論；DefineEditor 設定資料夾取自 `UserSettings.DirectoryName`（凍結起點為 `Bee.DefineEditor`）。
   - 框架預設定義檔與 SQL 中沒有含 bee 的資料表或欄位名稱，所以寫「不需要資料遷移」。
   - 遷移說明附的 grep 指令：在凍結起點的樹上命中 8863 行，在 polhem 上（排除 `local/`、建置產物與 bee-library 網址）0 行；另以放了 `.editorconfig`、`ProgramSettings.xml`、`Dockerfile` 的暫存目錄確認各 `--include` 都抓得到。
   - 「套件程式碼等於 4.33.0」的依據：bee-library 在 `v4.33.0` 與 `7d6cc9d9` 之間，`src/`、`tools/Bee.Cli`、`Directory.Build.props`、`Version.props` 沒有差異（之後的 42 個 commit 只動 plan、文件、samples 與 skill）；polhem 在初始 commit 之後對 `src/` 的改動只有註解、`.editorconfig` 註解與巢狀 `CLAUDE.md`。
3. **ADR 內指向 bee-library 的 12 個 commit 網址**都以 `gh api repos/jeff377/bee-library/commits/<hash>` 解析成功、且在 `origin/main` 上；英文版原樣保留網址。

#### ADR 英文化

- 由六個平行子代理依同一份翻譯要點翻譯，主 session 驗收：
  - 每份英文版與中文版的標題數、code fence 數、表格列數逐一相同；英文版除切換列外沒有中文與全形標點。
  - 翻譯期間中文原檔的改動（改連中文 ADR／文件）逐檔比對，確認只有連結。
  - 抽查譯文品質。
- ADR-045 原本寫「先前的 ADR 只有中文、雙語同步沒有自動檢查」。依 ADR 不改寫原文的慣例，在文末加「實作演進」一節記錄現況，中英兩份都加。
- 兩處帶錨點的跨語言連結手動處理（ADR-011 中文版改連中文 cookbook 的 `#框架初始化順序`；ADR-019 英文版由子代理對到英文錨點）。
- 反引號中的 `docs/en/...` 路徑（中文 ADR 內、非連結）是當時的紀錄，保留。

#### `check-docs-i18n.sh` 的後綴配對

- 檔頭新增 `SUFFIX_DIRS="adr changelogs"`：這些資料夾內，`<name>.md` 為源、同目錄的 `<name>.<lang>.md` 為譯本，只看資料夾第一層。
- 譯本標頭一律寫「相對於 `docs/` 的源路徑」（`<!-- source: adr/adr-001-x.md blob: … -->`），與語言資料夾的 `en/caching.md` 同一規則。
- 檢查項目與語言資料夾相同（標頭、過期、缺譯本、切換列），另加：後綴為未宣告語言的檔案報錯、`SUFFIX_DIRS` 名稱像語言代碼時設定錯誤。
- 以 scratchpad 的 clone 做正反測試：只切到語言資料夾模式時結果與改前相同；孤兒譯本、缺譯本、源文件帶標頭、未宣告語言後綴、子資料夾不配對都如預期。
- ADR-045 的切換列原本是 `**English** | [繁體中文](…)`，改為腳本產生的格式。

#### README

- 英文根 README 會打包進每個套件，連結全部改為 `https://github.com/polhem-dev/polhem/(blob|tree)/main/…`；中文版維持相對連結。
- 徽章改為 Build CI（`polhem-dev/polhem` 的 `build-ci.yml`）與 SonarCloud `polhem-dev_polhem`（比照 polhem-oauth2 的 key 慣例）。**Sonar 專案要到階段 5 才建立，屆時 key 若不同要改 README 兩份。**
- 標題的 🐝 改為 🌟。個人的「Contact & Follow」一節保留，只把全形分隔號改為半形。
- 各 `src/` 套件 README 沒有徽章或安裝指令，名稱在階段 1 已改，未改動。
- DemoCenter 主題表依 `DemoModuleRegistry` 的註冊順序與各模組的 `Category`／`Title` 重建，補上原本缺的 Number formatting、Multi-currency amounts、Multi-unit quantities、Permission Capability；「FormLayout 自動產生」依現況改為設計階段由 `FormLayoutGenerator.Generate` 產生；README 指向已不存在的 `Avalonia.Demo`，改指 `apps/Polhem.Northwind`。
- 中文版的分類與案例名稱照 App 顯示的英文列出（App 介面已是英文）。

#### 與 plan 原文不符、查證後更正的地方

- 「編譯器抓不到的地方」第 7 類寫 `BEE_MASTER_KEY`（與 `_FILE`）：原始碼中**沒有** `_FILE` 版本的環境變數，遷移說明只寫 `POLHEM_MASTER_KEY`。
- 移交項「CHANGELOG 建立後恢復 `framework-reserved-names.md` 的連結」：兩種語言都已恢復，連到各自語言的 CHANGELOG。

#### 未做、移交後續階段

- **階段 5**：README 徽章的 SonarCloud key（見上）。`docs-check.yml` 未改，它跑的 `check-docs-i18n.sh` 已涵蓋 ADR 與 `docs/changelogs/`。
- **階段 6**：`wire-contracts/README.md`、`wire-fixtures/README.md` 仍連 `bee-connector-js`（原本就排在階段 6）。
- **階段 7**：
  - 決策紀錄「型別名稱字串相容解析」寫「解析失敗的例外訊息順帶指出可能是舊的 Bee 名稱」，但 `src/` 中**沒有**這樣的訊息（全 repo 找不到 `Bee` 字樣）。屬程式碼修改，不在階段 4 範圍。遷移說明沒有宣稱例外訊息會提示。
  - `Version.props` 仍是 4.33.0；1.0.0 的版號在發版時改。CHANGELOG 的 1.0.0 標為 `Unreleased`，發佈日與定稿在階段 7／8。
  - DemoCenter `MasterDetailModule` 的 `Description` 仍寫「見 Avalonia.Demo」（該專案已不存在，程式內的 UI 文字）。
  - DefineEditor README 與程式不符之處（子代理回報、照原文翻譯）：headless smoke 的預期輸出少了 `[smoke:formlayout-gen]`、`[smoke:menu]` 與 tab commands；`publish.sh` 從 `src/Directory.Build.props` 找 `<Version>`，但版號在 `Version.props`，`VERSION` 可能是空值（疑似 bug）；`.app` 結構圖少了 `Contents/Resources/AppIcon.icns`；README 說的「每個編輯器共用的工具列」不存在（儲存、驗證在 File 選單，新增、刪除在右鍵選單）。
  - 翻譯 ADR 時子代理回報的內容疑點（ADR 是紀錄，照原文翻譯；要不要加「實作演進」由階段 7 決定），例如：ADR-035 決策五與 ADR-034 2026-08-16 修訂矛盾（BO 型別載不到時退回或直接拋）；ADR-021 後果第二點與決策矛盾；ADR-017「四個核心不變式」但決策列了五個；ADR-026 參考資料列了外部讀者開不到的 agent memory 名稱；ADR-029 狀態段與決策表的遷移狀態矛盾；ADR-012 列出不存在的 `src/Polhem.Definition.Identity` 等路徑。
  - `check-public-docs.sh` 第 (4) 道的新命中都是已知誤判：ADR-045 描述慣例本身、ADR-016 的「the plan was most concerned about」（不指向文件）、ADR-023 的「另立 plan」、`database-schema-upgrade` 的 API 名稱 `plan`。

## 階段 5：建立 repo 與 CI

1. 建立 `polhem-dev/polhem`（public），推送階段 1–4 的 commit。
2. **CI 改名**：`build-ci.yml`、`docs-check.yml`、`nuget-publish.yml` 的環境變數與路徑。第一次推送以 `[all-db]` 跑完整模式。
   - 階段 1 的機械替換已改好環境變數與容器名，這一步以驗證為主。第一次 CI 確認 SourceLink 警告已消失、建置為 0 警告。
   - `POLHEM_TEST_CONNSTR_*` 名稱對不上時 DB 測試會靜默略過，要加上 CI 會紅的機制，例如 CI 上要求該跑的資料庫不得略過（使用者決定，2026-09-26）。
3. **SonarCloud**（需實測）：`polhem-dev` organization 綁定 GitHub org、建立專案、設定 `SONAR_TOKEN`，重建 quality profile／gate；
   `build-ci.yml` 的 `/k:`、`/o:` 改寫。確認 S125、S3776 等在舊專案標過的 False Positive／Won't Fix 要不要重標。
4. **分支保護與 PR 工作流**（需實測）：`main` 要求 `build` check 並開啟 `enforce_admins`；確認維護者自己的 PR 能走完。
5. **auto-merge**（需實測）：依階段 0 的結果改寫 `auto-merge.yml` 的作者判斷與權杖。
6. **Trusted Publishing policy**（需實測）：比照 polhem-oauth2 階段 7，workflow 綁 `polhem-dev/polhem`。
   glob 不能涵蓋 `Polhem.OAuth2*`（那是另一個 repo 的 policy）；確認 dotnet tool 套件也能以同一條 policy 推送。**首發前不設任何發佈用 secret。**

### 實作紀錄（2026-09-26）

repo：**https://github.com/polhem-dev/polhem**（public，GitHub repo ID `1389037817`）。

推送前在本機 `main` 直接提交四個 commit（見決策紀錄「階段 2～4 的提交方式」），第一次推送共 32 個 commit；之後改走 PR：

| commit | 內容 |
|--------|------|
| `0558429` | DB 測試不得靜默略過的閘門；`build-ci.yml` 的 PR 觸發拿掉 paths 過濾；Sonar `/k:polhem-dev_polhem`、`/o:polhem-dev` |
| `bbcf799` | `/sonar-fix` 與 `gotchas/test-ci-release.md` 的 Sonar key |
| `691c047` | `check-public-docs.sh` 加入 `docs-check.yml`；`rules/pull-request.md`、`CONTRIBUTING`（中英）改寫為 `build`＋`docs` 兩個必要 check |
| `534ce09` | `nuget-publish.yml` 改用 Trusted Publishing；刪除 `auto-merge.yml`（第一次推送的 HEAD，訊息帶 `[all-db]`） |
| `6e4cc5f` | PR [#1](https://github.com/polhem-dev/polhem/pull/1)：改寫 `branch-protection-setup.md`、`ci-sonarcloud-setup.md`，拿掉 `SonarQube.Analysis.xml` 的 Blazor 重複排除 |

推送前以 `git log --all --name-only` 掃過整段歷史：沒有 `local/`、`.runsettings`、`CLAUDE.local.md` 或憑證類副檔名。

#### 使用者決定（2026-09-26）

- **auto-merge**：刪除 `auto-merge.yml`、不設 `AUTOMERGE_PAT`，改開 repo 的原生 Allow auto-merge（以啟用者身分合併，合併後 `main` 的 workflow 正常觸發）。
- **審查**：要求 PR、0 個核准、不開 Require review from Code Owners。
- **`check-public-docs.sh`**：加入 `docs-check.yml`。
- **paths 過濾**（本階段發現：`build` 設為必要 check 後，只改文件的 PR 會永遠等不到 `build`）：`pull_request` 觸發拿掉 paths 過濾，`push` 的保留。
- **必要 check**：`build`＋`docs`。**合併方式**：只允許 squash、strict 開、合併後刪分支；squash 標題改為一律用 PR 標題（見下方實測 3）。
- **SonarCloud new code**：Previous version（沿用預設）。
- **Trusted Publishing**：逐一列出 17 個套件全名，不用 glob。

#### 各步驟

1. **建立 repo、第一次推送**：`gh repo create polhem-dev/polhem --public`，加上 remote 後本機建置的 SourceLink 警告消失。
2. **CI 改名與 DB 略過閘門**：
   - 環境變數與容器名在階段 1 已改好，CI 上確認可用（見實測 3）。
   - 閘門：`build-ci.yml` 依模式把 `POLHEM_TEST_REQUIRED_DATABASES` 寫入 `$GITHUB_ENV`（精簡 `SQLServer,SQLite`、完整五種）；
     `tests/Polhem.Db.UnitTests/RequiredTestDatabaseGateTests` 對清單中的每一種，以 `TestDbConventions.GetConnectionStringEnvVar` 推導的名稱檢查連線字串。
     `GITHUB_ACTIONS=true`（runner 自己設的）而清單為空時也會紅，所以清單變數本身改名也擋得住；本機兩者都沒設時不檢查。
   - 本機以四種情境實測：完整清單齊全 → 通過；連線字串名稱為 `BEE_TEST_CONNSTR_SQLSERVER` → 紅；CI 但沒有清單 → 紅；本機 → 通過。
   - 本機基準因此由 6311 項變為 **6315 項**（新增 4 項），略過仍為 1 項。
3. **SonarCloud**：
   - organization `polhem-dev` **已由 polhem-oauth2 於 2026-09-26 建立**（階段 0 查時還不存在），GitHub App `sonarqubecloud` 裝在 org 的全部 repo。
     quality gate 為內建 Sonar way、quality profile 全部語言為內建 Sonar way comprehensive，與 `jeff377` org 相同，**不需重建**。
   - **專案在 GitHub repo 建立後自動出現**，key 為 **`polhem-dev_polhem`**（與 README 徽章相同，中英兩份不用改），`sonar.autoscan.enabled=false`、new code 為 previous_version。沒有在 UI 上做任何變更。
   - 舊專案 `jeff377_bee-library` 的 False Positive、Won't Fix、security hotspot 都是 **0 筆**，沒有要重標的。
   - `SONAR_TOKEN` 由使用者自行產生並以 `gh secret set` 設定（agent 不經手）。
   - `SonarQube.Analysis.xml` 的 Blazor 排除：PR #1 拿掉後，main 的完整分析中 `Components/`、`DataObjects/` 共 11 個檔案的重複行數都是 0，整體重複率維持 1.0%。
4. **分支保護與 repo 設定**：
   - `main`：必要 check `build`、`docs`，strict，`enforce_admins` 開，`required_pull_request_reviews` 為 0 核准、不要求 Code Owners，禁止 force push 與刪除。
   - repo：只允許 squash、`delete_branch_on_merge`、`allow_auto_merge` 開、`squash_merge_commit_title=PR_TITLE`。
   - 設定寫在 `docs/repo-ops/branch-protection-setup.md`，`rules/pull-request.md` 指向它。
5. **auto-merge**：見使用者決定；沒有建立任何 token。
6. **Trusted Publishing**：nuget.org 建立 policy **`polhem-release`**：Package owner `Polhem`、GitHub Actions、repo `polhem-dev/polhem`、workflow `nuget-publish.yml`、不設 environment、
   scope 只選 Push new packages and package versions，套件清單為 `Polhem.Base`、`Polhem.Expressions`、`Polhem.Definition`、`Polhem.ObjectCaching`、`Polhem.Db`、`Polhem.Api.Contracts`、
   `Polhem.Business`、`Polhem.Api.Core`、`Polhem.Api.Client`、`Polhem.UI.Core`、`Polhem.UI.Avalonia`、`Polhem.Repository.Abstractions`、`Polhem.Repository`、`Polhem.Hosting`、
   `Polhem.Api.AspNetCore`、`Polhem.Web.Blazor.Server`、`Polhem.Cli`（與 `nuget-publish.yml` 的 pack 清單、`IsPackable` 逐一核對）。
   建立後列表顯示 **Active**，repository owner 與 repository 已綁定 GitHub 數字 ID（`328209386`、`1389037817`，與 `gh api` 相符）。
   `nuget-publish.yml` 比照 polhem-oauth2：publish job 加 `id-token: write`，以 `NuGet/login`（v1.2.0 SHA）換臨時 key，`user` 讀 `secrets.NUGET_USER`；release job 明寫 `contents: write`。

#### 實測結果

1. **SonarCloud**：organization 可用（已存在，見上）；第一次完整掃描（`main` 的 `534ce09`）成功：
   覆蓋率 90.5%（舊專案 90.5%）、重複率 1.0%（舊 1.1%）、bug 與 vulnerability 0、code smell 57（舊 30）。
   多出的幾乎都是根目錄 shell 腳本的 `shelldre:S7688`／`S7682`／`S7679`（階段 2～4 擴充了 `check-*.sh`），**留給階段 7**。
   第一次分析的 quality gate 為 NONE（還沒有 new code）；PR #1 與之後 main 的分析為 **OK**。PR 分析在 free plan 的 public 專案可以讀取。
2. **分支保護**：開啟後以 PR #1 實測，三個 check（`build` 完整模式、`docs`、SonarCloud Code Analysis）通過後，由 jeff377 以 squash 合併成功，分支自動刪除：**維護者自己的 PR 在 `enforce_admins` 下能走完**。
   沒有實際嘗試直推 `main`（設定以 API 回讀確認）。
3. **第一次完整模式 CI**（run `36236954807`，`534ce09`）：
   - **第一次推送沒有觸發 `build-ci.yml`**：`docs-check.yml`（沒有 paths 過濾）有跑，`build-ci.yml`（push 有 paths 過濾）沒有。改以 `workflow_dispatch`（`db_scope=all`）執行。已寫進 `branch-protection-setup.md`。
   - 建置：Strict build（閘門）**0 警告、0 錯誤**，沒有 SourceLink 警告。另外 SonarScanner 包起來的建置有 101 則，都是 Sonar 規則診斷（S1192、S3776 等），不是編譯警告。
   - 測試：18 個測試專案共 **6315 項、略過 1 項**（`SystemBusinessObjectTests.Login_WithRsaKeyPair_ReturnsDecryptableSessionKey`），失敗 0，等於本機基準 6311 加上新增的 4 項。
     CI log 只有逐專案的總數，沒有逐資料庫的分項，所以比對的是總數與略過數。
   - **DB 測試沒有被靜默略過**：五種資料庫的 `SharedDatabaseState: common_* connection verified` 都有出現，沒有任何 `requires POLHEM_TEST_CONNSTR_*` 的略過訊息，`RequiredTestDatabaseGateTests` 通過。
   - Mobile AOT gate：3 個專案共 2582 項、略過 1 項。
   - **squash 標題**：PR #1 只有一個 commit，預設的 `COMMIT_OR_PR_TITLE` 取了 commit 標題，PR 標題裡的 `[all-db]` 沒有進 `main`，main 那一輪是精簡模式。
     使用者決定改為 `PR_TITLE`，並手動 dispatch 一次完整模式（run `36238311069`，成功）。
4. **Trusted Publishing 與 `Polhem.Cli`**：policy 以套件 ID 列出，表單接受 `Polhem.Cli` 與其他套件同列，設定面沒有區分 dotnet tool。**實際能否推送 dotnet tool、能否建立尚不存在的套件 ID，要到首發才驗證**。

#### 移交後續階段

- **階段 8**：
  - **推 tag 之前要設 repo secret `NUGET_USER`**（值為 nuget.org 帳號 `jeff377`，比照 polhem-oauth2）。本階段依指示沒有設任何發佈用 secret；沒設的話 `Log in to NuGet` 那一步會失敗。
  - 首發同時驗證 Trusted Publishing 能建立 17 個新套件 ID（含 dotnet tool `Polhem.Cli`）。失敗時先看 `Log in to NuGet` 那一步。
  - 新增可發佈的套件時，`nuget-publish.yml` 的清單與 nuget.org policy 的清單要一起改（workflow 的註解已寫明）。
  - NuGet `Polhem.` 前綴保留的申請信尚未寄出（使用者動作，見第 1 步）。
- **階段 7**：code smell 中根目錄 shell 腳本的 55 則；原生 auto-merge（`gh pr merge --auto`）還沒有實際用過。
- 使用者層 `~/.claude/rules/pull-request.md` 的「桌面環境直接推 main」在 polhem 已行不通（分支保護擋下），以 repo 的規則為準（`.claude/CLAUDE.md` 已寫明）。

## 階段 6：polhem-connector-js

1. 以 `jeff377/bee-connector-js` 的追蹤檔為起點，改名另開 `polhem-dev/polhem-connector-js`：套件名 `@polhem/connector`、
   `fetch-fixtures.mjs` 的 `REPO` 改為 `polhem-dev/polhem`、型別名稱與文件改名。語言政策同框架。
2. 需實測：兩個 repo 的 CI 互相依賴的順序——框架先推新 fixtures，connector-js 再跟上；中間 connector-js 紅燈是預期的。
3. 框架端 `.claude/rules/serialization.md`、`wire-contracts/README.md`、`wire-fixtures/README.md` 的連結改指新 repo。
4. npm 發佈：現況是 `private`，本 plan 不發佈；是否發佈另行決定。

### 實作紀錄（2026-09-26）

repo：**https://github.com/polhem-dev/polhem-connector-js**（public，GitHub repo ID `1389117104`），本機位於 `~/Desktop/repos/polhem-connector-js`。

| repo | commit | 內容 |
|------|--------|------|
| polhem-connector-js | `9451601` | 初始 commit：來源 `jeff377/bee-connector-js@2dbb4cc`，不帶歷史，37 個檔案（快照 37 個，扣掉 `docs/plans/` 的 1 個，加上 `README.zh-TW.md`） |
| polhem-connector-js | `308aa92` | PR [#1](https://github.com/polhem-dev/polhem-connector-js/pull/1)：README 兩處與程式不符的說法（中英同步），同時是分支保護下的第一個 PR |
| polhem | `304239f` | PR [#2](https://github.com/polhem-dev/polhem/pull/2)：`.claude/rules/serialization.md`、`wire-contracts/README.md`、`wire-fixtures/README.md` 改指新 repo（精簡模式） |
| polhem-local | `6d09c9c` | 移交 connector-js 的 DataTable plan |

bee-connector-js 本身沒有改動，凍結在階段 10。

#### 使用者決定（2026-09-26）

- **README 中英雙語**：`README.md` 英文為源、新增 `README.zh-TW.md`，頂部互相切換。英文版連結用絕對網址（會打進 npm tarball），比照框架。
- **中文 plan `docs/plans/plan-datatable-support.md`**：新 repo 不帶，比照框架「plan 不入版控」，`.gitignore` 加 `/local/`。
  原檔以 `cmp` 確認後複製為 polhem-local 的 `plans/plan-connector-js-datatable-support.md`，內文未改（仍是 Bee 時期的名稱與行號）；polhem-local 的 `README.md` 目錄表補一句說明前綴。
  `test/wire-fixtures.test.ts` 原本點名這份 plan，改為直接寫出缺口（兩種 wire 形狀）。
- **repo 治理**：分支保護＋PR 工作流，**不接 SonarCloud**。
- **版號**：沿用 `0.0.1`，`private` 不變。
- **固定測試向量以 Polhem 重生**：`aes-cbc-hmac.test.ts`、`gzip.test.ts` 的明文原為 `Bee.NET wire compatibility vector — 跨語言驗證`，改為 `Polhem …`（保留非 ASCII），密文與壓縮文以 polhem 的實作重新產生（見實測 4）。

#### 替換

- 腳本（scratchpad，不進 repo）規則依序為：特例（`jeff377/bee-connector-js` → `polhem-dev/polhem-connector-js`、`jeff377/bee-library` → `polhem-dev/polhem`、`bee-library` → `polhem`、`Bee.NET` → `Polhem`）；
  通則 `Bee`／`bee`／`BEE` → `Polhem`／`polhem`／`POLHEM`，**前後都不能是英數字**。比階段 1 的詞界嚴格，因為 `package-lock.json` 的 integrity 雜湊裡有 `Zbee5`；lockfile 只改套件本身的 `name`。
  `src/contracts/` 不在腳本內，另以 `npm run contracts:update` 重生。
- 機械替換的結果：`BeeClient` → `PolhemClient`；環境變數 `POLHEM_FIXTURES_REF`、`POLHEM_CONTRACTS_REF`、`POLHEM_ENDPOINT`、`POLHEM_API_KEY`；`DB_NULL` 的 `Symbol.for('polhem.dbnull')`；`ping()` 預設的 `clientName` 為 `polhem-connector`。
- 個別處理：`package.json` 的 `name` 為 `@polhem/connector`、`author` 為 `Polhem contributors`、keyword `bee-net` → `polhem`；`LICENSE` 改為 `Copyright (c) Polhem contributors`；`src/index.ts` 檔頭的套件名。
  README 的安裝段拿掉「npm 上的 `bee-connector` 不是本專案發佈的」警告（`@polhem` scope 屬於本專案，沒有被冒用的問題），改為「尚未發佈」；ADR-014 改為連結。`npm pack` 的 tarball 名為 `polhem-connector-0.0.1.tgz`，與 README 相符。
- **反向正規化 diff**：新樹（不含 `src/contracts/`）反向替換後與快照比對，殘差只有：
  - 名稱、網址、中繼資料：`Bee.NET` 等品牌字串、`@polhem/connector` 套件名、repo 網址、`LICENSE`、`author`、keyword。
  - 依上述決定而生的：README 的切換列、CI 徽章、安裝段、ADR-014 連結、`README.zh-TW.md`；`.gitignore` 的 `/local/`；不帶的 `docs/plans/`；兩組測試向量；`wire-fixtures.test.ts` 那段註解。
  - **沒有預期外的殘差。**
- `src/contracts/` 兩個檔案由 `contracts:update` 從 `polhem-dev/polhem@main` 重生，與「把快照機械替換」的結果**逐位元組相同**；`type-names.ts` 扣掉同步標頭後與 polhem 的 `wire-contracts/type-names.ts` 相同。

#### 實測結果

1. **兩個 repo 的 CI 相依順序**：框架在階段 5 已推上 `Polhem.*` 的 fixtures 與 contracts，「框架先推」這一步在本階段開始前就完成了，connector-js 只需跟上。中間狀態以本機重現：
   - `REPO` 改指 polhem、但 `src/contracts/` 還是舊的：`contracts:check` **exit 1**（兩個檔案都報 differs）。
   - 同一狀態下 **`test:wire` 仍通過**（28 項）：wire 測試只看 body 的形狀，**不比對 fixture 的 `type`**。所以型別名稱漂移只有 `contracts:check` 會紅，wire 測試抓不到。
   - 重生 contracts 後兩者都通過。新 repo 第一次 CI（run `36240008783`，`9451601`）Node 20、22 兩個 job 都綠：`Contracts match polhem-dev/polhem@main`、`Fetched 27 fixtures from polhem-dev/polhem@main`。
   - PR #1 合併後 `main` 的 CI（run `36240313673`）也綠。
2. **測試**：改名前的快照在 scratchpad 跑，單元測試 43 項、wire 測試 28 項（對 bee-library）、`contracts:check` 一致；改名後單元測試 43 項、wire 測試 28 項（對 polhem，27 個 fixture）全過，`typecheck` exit 0，`build` 成功。
3. **對真實後端的 smoke**：啟動 polhem 的 `samples/QuickStart.Server`，`npm run smoke` 三項（Plain、Encoded＋`codec: json`、`SystemConnector.ping`）都通過。
   另以**改名前的 Bee 版 connector** 打同一台伺服端：Plain 通過，Encoded 以 `-32099`「Payload type 'Bee.Api.Core.Messages.System.PingRequest, Bee.Api.Core' is not in the allowed type whitelist」被拒——框架 README 遷移說明「wire 型別名稱不相容、客戶端與伺服端要一起升級」的實證。
4. **測試向量**：scratchpad 的 console 專案參考 polhem 的 `Polhem.Api.Core`，先以 `AesCbcHmacCryptor.Decrypt` 解開**舊向量**得到原明文，確認金鑰切分（前 32 位元組 AES、後 32 位元組 HMAC）與 TS 端一致，再產生新密文與 `GzipPayloadCompressor` 的壓縮文，並確認 round-trip。
5. **分支保護**：以 PR #1 實測，兩個必要 check 都有回報、`mergeStateStatus` 為 `CLEAN`，由 jeff377 以 squash 合併成功、分支自動刪除——check 名稱對得上，維護者自己的 PR 在 `enforce_admins` 下能走完。沒有實際嘗試直推 `main`（設定以 API 回讀確認）。

#### 分支保護與 repo 設定

- `main`：必要 check **`build (20)`、`build (22)`**，strict，`enforce_admins` 開，`required_pull_request_reviews` 為 0 核准、不要求 Code Owners，禁止 force push 與刪除。
- repo：只允許 squash、`squash_merge_commit_title=PR_TITLE`、`squash_merge_commit_message=COMMIT_MESSAGES`（與框架相同）、`delete_branch_on_merge`、`allow_auto_merge` 開。沒有設任何 secret。
- **必要 check 名稱是 CI matrix 產生的 job 名稱**：改 `ci.yml` 的 Node 版本清單時，分支保護的 contexts 要一起改，否則之後的 PR 會一直等不到 check。這條目前只記在這裡，新 repo 裡沒有維運文件。

#### 未做、移交後續階段

- **階段 7**：
  - connector-js 沒有 `.claude/`、`CLAUDE.md`、`CONTRIBUTING`、`CODEOWNERS`，也沒有記錄分支保護設定的維運文件（上一節的 matrix 名稱約束）。要不要比照框架補，一併決定。
  - JS client 實測（第 4 步）涵蓋 connector-js：本階段已對 QuickStart.Server 跑過 smoke，登入與加密路徑（`login`、`getList`）尚未對真實後端跑。
  - `fetch-fixtures.mjs`、`contracts.mjs` 釘在 polhem 的 `main`；註解寫「框架發版後改釘 release tag」，階段 8 首發後處理。
- **階段 10**：bee-connector-js 的 README 指路與 archive。
- 移交的 DataTable plan 內文仍是 Bee 時期的，動工前依現況改寫（polhem-local 的 README 已註明）。

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
