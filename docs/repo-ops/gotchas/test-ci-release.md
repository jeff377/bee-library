# 踩雷誌：測試、CI 與發佈

對應硬規則見 `.claude/rules/testing.md`、`.claude/rules/commit-verification.md`。

## CI path filter 造成的驗證死角

`.github/workflows/build-ci.yml` 的 `push` / `pull_request` 只認：
`src/**`、`tests/**`、`*.slnx`、`Directory.Build.props`、`SonarQube.Analysis.xml`、
`.github/workflows/build-ci.yml`。

**Why**：samples/ 是 demo、docs/ 是文件，兩者異動都不影響 NuGet 套件正確性，省 runner 時間。

**日常影響**：純 `samples/**` 或 `docs/**` 的 commit push 到 main 後**不要等 CI**、也不要觸發
`/ci-watch`——沒事可看。SonarCloud 也只在 CI 跑完後被觸發，samples-only 修正不會立刻反映。
（同一 commit 若既動 src/ 也動 samples/，CI 會跑。）

### ⚠️ 連帶缺口：三個方案根本沒被建過

`tools/` / `samples/` / `apps/` **既不在 `Bee.Library.slnx` 內，也不在 path filter 內**，
所以「本機 `dotnet build Bee.Library.slnx` + `./test.sh` 全綠」**不代表它們還能編譯**，CI 也不會
替你發現。

**實例**：刪除 `BackendComponents.EnterpriseObjectService` 後，`tools/DefineEditor` 的 axaml 綁定
殘留造成 AVLN2000——本機與 CI 兩邊都是綠的，直到手動建 `tools/Bee.Tools.slnx` 才爆。

**刪除或改名任何公開型別／成員時，必須額外建這三個方案**：

```bash
dotnet build samples/Bee.Samples.slnx --configuration Release
dotnet build tools/Bee.Tools.slnx --configuration Release
dotnet build apps/Bee.Northwind/Bee.Northwind.slnx --configuration Release -p:ValidateXcodeVersion=false
```

XAML／axaml 綁定尤其危險：它們是**字串綁定，grep 得到但 C# 編譯器看不到**。

## 測試 fixture 選錯被誤判為 flaky

**症狀**：`LogoutJsonRpcRoundTripTests` / `LeaveCompanyJsonRpcRoundTripTests` 在 CI 偶發紅，
斷言 `Assert.Null(response.Error)` 失敗、實際值是被遮蔽的 `-32000 Internal server error`。

**根因**：這兩個 class 用 `IClassFixture<BeeTestFixture>`，但 session 持久化落地後 `Logout` 會
DELETE、`LeaveCompany` 會 UPDATE `st_session` —— 對資料庫有真實相依。**`BeeTestFixture` 不建
schema**（只有 `SharedDbFixture` 會），所以只有在「別的測試行程剛好先把表建好」時才通過。
同資料夾的 `EnterCompanyJsonRpcRoundTripTests` 早就用 `SharedDbFixture`，正是同一個理由。

**為何被誤判為 flaky**：`gh run rerun --failed` 一次剛好轉綠（競賽條件本來就會這樣），
加上非開發模式把例外訊息遮蔽成 `Internal server error`，看不出是「表不存在」。

**心法**：**一次重跑轉綠不足以判定 flaky。** 同一組測試在不同 commit 的**首次**執行都紅，
就該當真 bug 查。重跑只用來收集證據，不是結案依據。

### 找出全部違規類別的窮盡掃描法

**別用 grep 推理代替執行。** 觸發面比想像廣：不只 `IAccessTokenValidator`，任何
`SessionInfoService.Get(未快取 token)` 都算 —— 含 BO 內部的 `GetLangText` /
`GetCurrentCustomizeId` / 查目前公司。

做法：drop 掉 `st_session`，再逐專案跑「排除所有 `SharedDbFixture` 類別」的子集 ——
建表的類別不參與，依賴該表的測試就必定現形。

```bash
docker exec sql2025 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '<pw>' -C \
  -d common -Q "DROP TABLE IF EXISTS st_session;"
# 排除該專案內所有繼承 SharedDbFixture 的類別
dotnet test tests/<Proj>/<Proj>.csproj -c Release --settings .runsettings \
  --filter "FullyQualifiedName!~.ClassA.&FullyQualifiedName!~.ClassB."
```

`--filter` 的值務必用雙引號包住（含 `&`，否則 shell 會吃掉）。
表由下一次 `SharedDatabaseState.EnsureSchemaAndSeed` 重建，對其他測試無殘留。

> 2026-08-04 用此法一次掃出 4 個違規類別（`ClientDefineAccessTests`、
> `JsonRpcExecutorCoverageTests`、`LogBusinessObjectTests`、`CacheTests`），
> 而先前僅以 grep 推理只找到第 1 個。

## 快取自載 DB 時假設「該 DB 一定有設定」

**症狀**：本機全綠、CI 紅。

**根因**：`DbConnectionManagerService.GetConnectionInfo` 對未登錄的 databaseId 會擲
`KeyNotFoundException`（KeyedCollection 的 indexer 先炸，程式碼裡那句 `InvalidOperationException`
是**死路徑**），而呼叫端往往只想知道「這筆資料在不在」。

**本機測不出來**：`tests/Define` 的 DatabaseSettings 有 common，本機 fixture 一律連得到；
`Bee.ObjectCaching.UnitTests` 等不掛 DB 的 fixture 只有在 CI 才會走到未設定路徑。

**正解**：自載前先確認該 DB 已設定
（`IDatabaseSettingsProvider.Get().Items.GetOrDefault(id)`），未設定即視為「沒有這個資料來源」
回 null，**不要吞例外**。2026-07-30 於 SessionInfoCache 重建踩到並修正（`caf45975`）。

## `.gitignore` 的 `[Ll]og/` 吃掉原始碼資料夾

**症狀**：在 `src/**/Log/` 或 `tests/**/Log/` 新增 `.cs`，`git status` **完全看不到**、
`git add -A` 不會加入 → commit 缺檔、CI 缺檔編譯失敗。
`git check-ignore -v <file>` 會指向 `.gitignore:<n> [Ll]og/`。

**Why**：`[Ll]og/`（VS 範本預設）本意是忽略 log 輸出目錄，但會忽略**任何**名為 `Log/` 或 `log/`
的資料夾，包含命名空間資料夾。IDE0130 又強制資料夾對映命名空間，無法只改資料夾名保留 `.Log`
命名空間；`!` negation 對「被規則匹配到的目錄」下的檔案再包含也**無效**。

**正解**：稽核查詢讀取側一律用 `AuditLog/`（命名空間 `...AuditLog`），**不要**用 `Log/`。
`AuditLog` 也對齊 progId / 軸名。未來任何要用 `Log` 當資料夾名的情況同理改別名
（`AuditLog` / `Logging` 皆可，`Logging` 未被忽略）。

## 死碼掃描：attribute 必須 grep 簡寫

**症狀**：把某個 `*Attribute` 判為「零使用」，刪除後 build 立刻失敗。

**根因**：C# 的 attribute 使用端幾乎一律用**去 `Attribute` 後綴的簡寫**（`[TreeNodeIgnore]`），
只 grep 全名 `TreeNodeIgnoreAttribute` 會得到假的「零使用」結論。

**實例**：2026-07-28 框架體檢的死碼清單把 `TreeNodeIgnoreAttribute` 列為零使用，實際有 7 處
生產用途（`CollectionItem` / `KeyCollectionItem` / `FormField` / `FormRule` / `FormSchema` /
`MessagePackCollectionItem` / `MessagePackKeyCollectionItem`）。

**正解**：對 `*Attribute` 型別用 `grep -rn "TypeName"`（不加 `Attribute` 後綴、不加尾界）。
同理適用於任何有語法糖簡寫的型別。

## 新增 src 套件時最容易漏的一步

`.github/workflows/nuget-publish.yml` 與 `build-ci.yml` 的 pack step 是**逐一
`dotnet pack src/Bee.X/...` 列舉，不是 glob**。漏了新套件：

- **nuget-publish**：該套件**不會被推上 NuGet**，但 workflow 仍**顯示 success**
  （它只推 `./nupkgs` 內既有的）。消費端 restore 依賴此新套件的其他 4.x 套件時會失敗。
- **build-ci**：pack 驗證漏測該套件。

**2026-07-09 實例**：4.14.0 發佈 `Bee.Expressions`（新套件），兩個 workflow 的 pack 清單都漏列
→ 首次 publish 成功但 NuGet 上沒有 `Bee.Expressions.4.14.0`，而 Bee.Business / Definition /
UI.Avalonia 都依賴它。修法：補兩個 workflow 的 pack 行，commit，**刪除並重推 tag** 到含修正的
commit 觸發 publish，`--skip-duplicate` 讓已發佈的跳過、只補推新套件。

**排查徵兆**：publish workflow 綠、但
`curl https://api.nuget.org/v3-flatcontainer/<pkg-lowercase>/index.json` 回 BlobNotFound
（且非索引延遲）。確認 push 步驟 log 有無 `Pushing <Pkg>.nupkg... Your package was pushed.`
——沒有就是漏列。

**同時要同步**（雙語文件必須兩份都改）：

- `docs/dependency-map.md` + `.zh-TW.md`：mermaid 加節點 + 相依邊、外部套件表加一列、
  Architectural Notes、開頭「N 個 src/ 專案」數字 +1。
- `README.md` + `.zh-TW.md`：Shared / Backend / Frontend 套件表擇一加一列。
- `.claude/CLAUDE.md`：「N 個專案」數字 +1。

## 框架體檢（`bee-framework-review`）的方法論

各次結果與分級計畫每輪一份，落在 `docs/plans/`，完成後封存為
`docs/plans/archive/plan-framework-review-<YYYY-MM-DD>.md`（入版控）。以下是**跨體檢沿用**的方法：

1. **「分數下降」多半是掃描深度提升，不是回歸——但必須逐項用 git 驗證才能這樣說。**
   2026-07-28 那輪多數降幅來自把 `PackageReference`、`git show` 歷史比對、**實際執行驗證**納入掃描；
   各代理用 git 逐項查了問題引入時間，確認多為長期既有。不能憑感覺說「這不是回歸」。
2. **體檢基準不可寫「死碼 0」這類無從驗證的斷言，要寫具體型別清單。**
   上一輪基準宣稱「空 class 0、死碼 0」，下一輪查出至少 15 個零使用型別且全部早於上次——
   判定過於樂觀（很可能只掃了完全無引用的檔案，未追到「宣告 + DI 註冊」或「宣告 + 佔位測試」
   這類假陽性存活的型別）。**佔位測試會讓死碼在覆蓋率報告上呈現為已測試。**
3. **P0 級發現值得付實測成本，把「理論推斷」釘成「已知失敗模式」。**
   序列化面向的 P0（定義類 response 在 MessagePack wire 上內容全滅）原本只是推斷，在 scratchpad
   建獨立 console 專案 ProjectReference 到 `Bee.Api.Core`、走公開的 `MessagePackPayloadSerializer`
   實測後，釘死失敗模式為**沉默空殼**——修法選擇取決於這個答案。

> **已關閉的流程缺口**：public API 把關曾連兩輪漏標 breaking（`IExcelHelper`、`IEvictableCache`
> 移除都沒標 `!`），根因是「commit 前綴是 changelog 唯一來源，卻無機制檢查 public surface 有刪改」。
> 已引入 `PublicApiAnalyzers` 的 `PublicAPI.Shipped.txt` / `Unshipped.txt`（見
> `docs/repo-ops/public-api-baseline.md`），漏標即 build 失敗；分析器看不到的「已申報但二進位
> 不相容」由 pre-commit hook 攤開提示。
>
> **但「gate 關閉」不等於「舊帳清完」**（2026-08-07 補）：那兩個案例的下場不同——
> `IEvictableCache` 的 CHANGELOG **有**記到，`IExcelHelper` 則連 CHANGELOG 都沒有，
> 直到 2026-08-07 的體檢查出才回溯補進 4.16.0 明細檔。導入機制擋的是「以後」，
> 先前漏出去的要人工回補。**下次引入任何 gate 時，同時列一份「gate 之前已經漏掉什麼」的清單。**
