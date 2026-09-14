# 計畫：bee-oauth2 改名為 Polhem.OAuth2 並移至 polhem-dev

**狀態：🚧 進行中（2026-09-14）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1a | 本機建立 polhem-oauth2：純改名、拿掉 `Bee.Base`、加密移植與測試、CI、`.slnx` | ✅ 已完成（2026-09-13） |
| 1b | 導入 bee-library 現行程式碼風格設定，整理既有程式碼（英文化、例外、命名、Nullable、編碼、SDK 格式）；完成後建立 repo 並推送 | ✅ 已完成（2026-09-13） |
| 2 | state 解密加強健壯性 | ✅ 已完成（2026-09-13） |
| 3 | JSON 改用 System.Text.Json | ✅ 已完成（2026-09-13） |
| 4 | 拿掉 WebView2，改用系統預設瀏覽器 + loopback 回呼；桌面流程併入核心，Desktop／WinForms 套件移除 | ✅ 已完成（2026-09-14） |
| 5 | 目標框架 net8.0 → net10.0，核心多打 net10.0（提前到階段 4 之前） | ✅ 已完成（2026-09-13） |
| 6 | 推廣準備：README、NuGet 中繼資料、org profile | ✅ 已完成（2026-09-14）；org profile 的推送移到階段 7 發佈後 |
| 7 | 首發 `Polhem.OAuth2.*` 1.0.0 | 📝 待做 |
| 8 | 凍結舊套件與舊 repo | 📝 待做 |
| 9 | 回寫 bee-library：future-work 與演練結果 | 📝 待做 |

## 背景

[future-work.md「開放共同維護」](../repo-ops/future-work.md) 定案的方向是：bee-library **不轉移**，
而是改名另開新框架於 `polhem-dev`，舊 repo 與 `Bee.*` 套件凍結。名稱與各平台帳號已於 2026-09-12 占下。

bee-oauth2（`jeff377/bee-oauth2`）是跨平台的 OAuth2 輕量套件，先一步走同一條路，目的有二：

1. **替 bee-library 演練**改名另開、發佈到 NuGet 組織帳號、凍結舊套件這幾步。
2. **推廣 OAuth2 套件，替 Polhem 累積曝光**。舊的五個 `Bee.OAuth2.*` 合計約兩萬次下載，
   deprecated 指向新套件是主要的曝光管道。

### 現況（2026-09-13 盤點）

| 項目 | 現況 |
|------|------|
| 來源 | `jeff377/bee-oauth2` `main` @ `8042a72`，含 v2.0.0 之後未發佈的 Okta 支援（`a24acee`） |
| 套件 | `Bee.OAuth2`（netstandard2.0）、`.AspNet`（net48）、`.AspNetCore`（net8.0）、`.WinForms`（net48）、`.Desktop`（net8.0-windows） |
| 測試 | 無 |
| CI | 只有 tag 觸發的發佈 workflow；secret 直接展開在 `run:`、actions 未 pin SHA |
| `Bee.Base` 3.4.0 相依 | 核心直接參照。實際用到：`StrFunc.IsEmpty`／`IsNotEmpty`（三處）、`AesCbcHmacCryptor` + `AesCbcHmacKeyGenerator`（`OAuth2StateCryptor`） |
| 間接相依 | 七個檔案的 `JObject.Parse` 靠 `Bee.Base` 3.4.0 帶進來的 Newtonsoft.Json，csproj 沒有直接參照 |
| WebView2 | Desktop 與 WinForms 的 `AuthorizationForm` 攔截「導向 RedirectUri」的導覽取得 code，RedirectUri 不必真的有人在聽 |
| 中繼資料 | `Authors`／`Company`／`Product` 為 `Bee.NET`；`Copyright` 寫 All rights reserved；`LICENSE.txt` 著作權列仍是樣板 `[year] [fullname]`；AspNetCore 的 `PackageTags` 誤植為 `OAuth2 WinForms` |
| 舊 repo | 18 星、5 fork、1 個開放 issue（#1 Winforms usage example） |

公開 API 沒有暴露任何 JSON 型別（`UserInfo` 只有字串 `RawJson`），換 JSON 函式庫不影響使用者。

### 這次演練得到與演練不到的

對照 future-work「要一併決定的」：

| future-work 項目 | 本次 |
|------------------|------|
| 新 repo 帶完整 git 歷史 | **演練不到**。本次刻意不帶；bee-library 要帶的理由（規則與 gotchas 引用 commit hash）bee-oauth2 沒有 |
| 舊框架怎麼凍結 | 演練得到：NuGet deprecated + 替代套件、舊 repo 指路、archive |
| 型別名稱字串相容解析 | 不適用，bee-oauth2 沒有組件限定型別名稱字串 |
| 開放共同維護的缺口 | 部分：org 下的 Actions 與 secret 設定 |
| `dev-workflow` plugin | 不適用 |

另外演練得到 future-work 沒列的：NuGet 組織帳號的 API key 與發佈、org profile 首次放上實際產品。
**演練不到**：SonarCloud、分支保護與 auto-merge、wire 合約與 connector-js。

## 決策紀錄

| 決策 | 結論 | 理由 |
|------|------|------|
| 移轉方式 | 新開 repo，**不帶舊 git 歷史**；舊 repo 指路後 archive | 與 bee-library 預定路線相同（歷史一項除外，見上表） |
| repo 名稱 | `polhem-dev/polhem-oauth2` | fork 到他處仍看得出所屬家族 |
| `Bee.Base` 的加密 | repo 內自帶實作，位元組格式與 `Bee.Base` 3.4.0 相容 | 新框架只打 net10，核心卻是 netstandard2.0，等不到新 base 套件 |
| 搬移那一步的範圍 | 純改名，行為不變 | 名稱正規化後 diff 才能證明零行為變更 |
| 先改還是先搬 | **搬完再改** | 不帶歷史時，搬之前的修改會併進初始快照、看不到個別變更 |
| 首發時機 | **功能改完才首發** | 使用者只遷移一次；Polhem 名下不發出帶 Newtonsoft 的版本 |
| 首版版號 | 1.0.0 重新起算 | 使用者決定 |
| Bee 與 Polhem 的關係 | **公開，Polhem 名稱定案** | 舊套件 deprecated 是主要曝光管道 |
| 推廣範圍 | README 與 NuGet 中繼資料、org profile | 前綴保留申請與發文公告不在本 plan |
| 功能修改 | state 解密健壯性、System.Text.Json、拿掉 WebView2、net10.0 | 使用者決定 |
| 套件結構 | 五個減為三個：`Polhem.OAuth2`（含桌面登入流程）、`.AspNet`、`.AspNetCore` | 桌面流程只需 BCL，併入 netstandard2.0 核心即可跨平台。網頁套件綁 `System.Web`／`Microsoft.AspNetCore.Http` 的 cookie 與 session，無法併入；傳統 ASP.NET 版下載數（4,051）高於 AspNetCore 版（2,272），保留 |
| 程式碼風格 | 依 bee-library 現行設定導入，並整理既有程式碼（階段 1b） | 使用者決定。排在功能修改之前，後續新寫的程式碼一開始就受閘門把關 |
| 語言政策 | **共同維護的部分一律英文**：程式碼、XML doc、程式內註解、測試方法名稱與 `[DisplayName]`、`.claude/`、commit message。**公開 `.md` 文件中英雙語**：`README.md` 英文為預設，`README.zh-TW.md` 為中文版。**ADR 同樣中英雙語** | Polhem 未來開放共同維護，共同開發者要讀得懂決策。與 bee-library 現行做法（中文 `[DisplayName]`、中文 commit、只有中文的 ADR）刻意不同 |
| LICENSE 著作權人 | `Copyright (c) Polhem contributors`，不寫年份 | 使用者決定，符合開放共同維護的定位。Polhem 不是法律主體，細節見階段 1a |
| 方案與專案格式 | 方案檔用 `.slnx`；所有專案一律 SDK 格式 | 使用者決定。`.slnx` 只列專案與資料夾；全部改成 SDK 格式後，CI 可以直接建置整個方案 |
| WebForms sample | **移除** OAuthAspNet | 使用者決定。`MSBuild.SDK.SystemWeb` 把「無法用 dotnet CLI 建置」列為已知限制；補救要加 2016 年的社群套件 `MSBuild.Microsoft.VisualStudio.Web.targets` 並關掉 `MvcBuildViews`，或讓 CI 另用 Visual Studio 的 MSBuild。傳統 ASP.NET 的用法改由 README 程式碼片段說明 |
| JSON null 欄位 | 回傳 `null`，備援欄位生效 | 使用者決定。特性測試證實 Newtonsoft 對 JSON null 回傳空字串，連帶讓 Azure 的 `oid`→`sub`、Auth0 的 `name`→`nickname` 這類備援不生效。新語意與 1b 的 nullable 標註一致；差異寫進 README 遷移說明 |
| 階段順序 | 階段 5 提前到階段 4 之前 | 使用者決定。階段 4 要先在各 provider 後台登記 loopback 回呼網址、準備測試憑證；階段 5 不需要外部帳號，可以先做。代價是 Desktop 專案暫時維持 net8.0-windows，等階段 4 刪除 |
| 核心目標框架 | `netstandard2.0;net10.0`，`System.Text.Json` 套件只給 netstandard2.0 | 使用者決定。net10 的使用者（含 AspNetCore 套件）直接用框架內建的版本，這一組的相依清單是空的 |
| loopback 監聽實作 | `TcpListener`，不用 `HttpListener` | 使用者決定（2026-09-14）。Windows 上 `HttpListener` 走 http.sys：`127.0.0.1` 字首需要 URL ACL，且不支援 port 0（自動選空 port）。`TcpListener` 在一般權限下跨平台可用，實測工具已驗證 IPv4／IPv6 行為；代價是自行解析 HTTP request line |
| 階段 4 實作時機 | 與 provider 無關的部分先實作；PKCE 預設值、PKCE 下是否送 client secret、ADR 定稿，等其餘 provider 實測完再定 | 使用者決定（2026-09-14）。原訂先實測再實作，但 Google 以外的 provider 還沒登記 |
| 實測工具分支 | `claude/loopback-probe` 不單獨開 PR，與階段 4 的實作同一個 PR | 使用者決定（2026-09-14）。核心有了 loopback 流程後，工具改用核心 API、刪掉重複的監聽程式碼，一次審完 |
| Okta 實測 | 使用者提供帳號後，由 agent 建 Native app 並實測（2026-09-14 完成）；若無法取得帳號，才標示未測、不擋 ADR-004 定稿 | 使用者決定（2026-09-14）。原本決定標示未測，得知個人可申請後改為補測 |
| 桌面流程的 PKCE | `LoopbackOAuth2Client` 一律使用 PKCE，不看 `OAuth2Options.UsePkce`；web 套件不受影響 | 使用者決定（2026-09-14）。RFC 8252 要求原生應用程式使用 PKCE，且桌面 app 無法保密 client secret；實測的五家在 PKCE 下都能換到 token |
| PKCE 下的 client secret | 維持現行：PKCE 下不送 client secret，只有 Google 照舊一律送 | 使用者決定（2026-09-14）。Facebook、LINE、Entra ID、Auth0 實測都不需要 secret 就能換到 token |
| 不接受 loopback 的 provider | 不需處理 | 實測的五家都接受 loopback 回呼網址（2026-09-14），原本的待決問題不成立 |
| org profile 推送時機 | 階段 7 發佈後才推 | 使用者決定（2026-09-14）。profile 連到 nuget.org 上的 `Polhem.OAuth2`，發佈前那個連結是 404 |
| NuGet 發佈授權 | Trusted Publishing，不用 API key | 使用者決定（2026-09-14）。nuget.org 會把 API Keys 頁導向 Trusted Publishing，並註明自動化發佈強烈不建議用 API key；API key 的期限最長只剩 30 天，每次發版都要重建 key、重設 secret。glob 維持 `Polhem.OAuth2*`：policy 本來就綁單一 repo 與 workflow，放寬成 `Polhem*` 不會少建 policy，只會讓這個 workflow 能推送將來其他 Polhem 套件。**bee-library 改名另開時沿用同一做法** |

---

## 階段 1a：建立 polhem-oauth2（純改名）

本機位置 `~/Desktop/repos/polhem-oauth2`，以 `8042a72` 的工作樹快照為起點（不含 `.git`）。

### LICENSE

`LICENSE.txt` 的樣板 `[year] [fullname]` 改為 `Copyright (c) Polhem contributors`（2026-09-13 定案）。
不寫年份：年份沒有法律效力，寫單一年份又說不清「程式碼延續自 2025 年發表的 bee-oauth2」，寫起訖年份則每年要改卻沒有機制提醒。

「Polhem」不是法律主體，這一列標示的是權利人群體。沒有簽 CLA 時，每位貢獻者對自己的貢獻保有著作權。
將來若成立法人，再評估是否把著作權轉給法人。

### 步驟

1. **改名**
   - 資料夾、方案檔、csproj：`Bee.OAuth2*` → `Polhem.OAuth2*`；方案檔改為 `.slnx`（另成一個 commit）。
   - 命名空間與 `using`，含 samples 與各專案 `README.md`。
   - samples 的 `ProjectReference` 路徑。
2. **拿掉 `Bee.Base`**
   - `StrFunc.IsEmpty`／`IsNotEmpty` → `string.IsNullOrWhiteSpace`（它預設 `Trim()` 後才判斷，**不是** `IsNullOrEmpty`）。
     位置：`OAuth2Provider.cs` 兩處、`BaseOAuth2Client.cs` 一處。
   - 移除沒用到的 `using Bee.Base;`：AspNet 的 `OAuth2Client.cs`、`OAuth2Manager.cs`。
   - 移植加密：來源為 bee-library tag `v3.5.0` 的 `src/Bee.Base/Security/AesCbcHmacCryptor.cs` 與
     `AesCbcHmacKeyGenerator.cs`。兩檔自 2025-06-26 建立後到 v3.5.0 未變動，而 `Bee.Base` 3.4.0 發佈於 2025-09-05，格式相同。
     只移植用到的 `Encrypt`、`Decrypt`、`FromCombinedKey`，一律 `internal`（不讓 OAuth2 套件變成加密函式庫的公開表面）。
   - 補 `Newtonsoft.Json` 13.0.3 直接參照，**暫時**維持現行行為，階段 3 移除。
   - samples：刪除 `Bee.Base` 的 `<Reference>` 與 `packages.config` 條目（程式碼沒有用到）。
3. **中繼資料**：`Version` 1.0.0；`Authors`／`Company`／`Product` → Polhem；`Copyright` 與 LICENSE 一致；
   `PackageIcon` 改用 bee-library 根目錄 `bee.png`（`67bfca83` 起為三格接點），更名 `polhem.png`；
   `RepositoryUrl` → `https://github.com/polhem-dev/polhem-oauth2`。描述與 tags 留到階段 6。
4. **測試**：新增 `tests/Polhem.OAuth2.UnitTests`（xUnit、net10.0），以 `InternalsVisibleTo` 測 internal 型別。
   測試方法名稱、`[DisplayName]` 與註解從一開始就用英文（見決策紀錄）。
   - 移植 v3.5.0 的 `AesCbcHmacCryptorTests.cs`、`AesCbcHmacKeyGeneratorTests.cs`，原本的中文 XML doc 與註解改寫為英文。
   - **相容性黃金樣本**：一次性以 `Bee.Base` 3.4.0 在 scratchpad 產生「固定金鑰 + 密文 + 明文」，寫成常數。
     測試斷言新實作解得回同一明文。測試專案本身不參照 `Bee.Base`。
   - `OAuth2StateCryptor` 在型別初始化時讀環境變數（static readonly），同一行程內無法切換兩種模式，本階段不測，見階段 2。
5. **CI**
   - 新增 `build-ci.yml`（windows-latest）：restore、build 全部 src、跑測試。
   - 發佈 workflow 改專案名；secret 改由 step 的 `env:` 注入；actions pin 完整 SHA。
   - **階段 7 之前不設 `NUGET_API_KEY`**：誤推 tag 也發不出去。
6. **零行為變更驗證**：把新樹的 `Polhem.OAuth2` 正規化回 `Bee.OAuth2` 後，與 `8042a72` 快照做 diff。
   預期只剩：`Bee.Base` 移除相關、中繼資料、圖示、LICENSE、新增的測試與 CI。**本機比對通過才建立 repo 並推送。**
7. **初始 commit**：message 以英文撰寫，並註明來源 `jeff377/bee-oauth2@8042a72`。
   建立 GitHub repo 與推送**延到 1b 完成後**（使用者決定，讓外界第一眼看到的就是整理過的程式碼），見 1b 最後一步。
8. **future-work 同步**：「已占名」一節的「名字還沒正式定案」改為已定案（附日期），
   org profile 列的「刻意不提 Bee.NET」改寫為現行政策。

### 本機驗證範圍

**更正（2026-09-13 實測）**：macOS 上 net48 與 net8.0-windows 都建得起來，後者要加 `-p:EnableWindowsTargeting=true`。
原本「只有 Windows 建得起來」的推測不成立，所以本機就能建置整個方案；windows-latest CI 仍是最後一道把關。
repo 要到 1b 完成後才建立，Windows 上的第一次驗證要等第一次推送；之後動到 Windows 專用目標的階段（4、5）走分支 + PR。

---

## 階段 1b：導入程式碼風格設定並整理既有程式碼

程式碼風格依 bee-library 現行設定。1a 維持純改名、可用 diff 驗證，所以風格整理獨立成這一階段；
排在功能修改之前，階段 2 以後新寫的程式碼一開始就受閘門把關。

### 設定

| 檔案 | 來源與調整 |
|------|-----------|
| `.editorconfig` | 複製 bee-library 根目錄的版本，刪除 bee-library 專屬的 `PermissionAction.cs` 段落 |
| `.gitattributes` | 複製 bee-library 的版本 |
| `src/Directory.Build.props` | 照 bee-library `src/Directory.Build.props` 的「文件」「語言特性」「健康檢查」區塊與 SourceLink；各 csproj 重複的套件中繼資料集中到這裡。另加 `Nullable` 與 `LangVersion`（netstandard2.0／net48 預設 C# 7.3，Nullable 與 global using 需要較新版本） |
| `tests/Directory.Build.props` | 套用同一組健康檢查設定 |
| `docs/adr/` | 雙語 ADR：`adr-NNN-<slug>.md`（英文）與 `adr-NNN-<slug>.zh-TW.md`，頂部互放語言切換連結；`README.md` 索引同樣雙語。文件量少，沿用 README 的後綴慣例，不採 bee-library 的 `docs/<lang>/` 資料夾結構 |
| `.claude/CLAUDE.md` | 以英文撰寫的專案層 agent 指引。寫明語言政策（見決策紀錄），並**明文覆寫**使用者層「敘述文字全部繁體中文」的預設；不寫出來，agent 會照中文寫測試 `[DisplayName]` 與 commit message。公開 `.md` 修改時兩種語言同步。程式碼風格指向 `.editorconfig`，不複寫規則 |

**不照搬**：
- `Bee.Analyzers`：BEE 系列診斷是框架專屬。
- `Version.props` 與 BEE9001／BEE9002 閘門。
- 根目錄那份只為 CI 加速的 `Directory.Build.props`。
- PublicAPI analyzer：等階段 7 首發時才導入。

設定放在 `src/` 與 `tests/` 層；`samples/` 不受建置閘門影響，只受 `.editorconfig` 的格式規則約束。

**所有 sample 改為 SDK 格式**：
- OAuthWinForms（net48）改用 `Microsoft.NET.Sdk` 加 `UseWindowsForms`。
- OAuthAspNet（net48 WebForms）**移除**，理由見決策紀錄。
- `packages.config` 改為 `PackageReference`，`Properties/AssemblyInfo.cs` 改由 SDK 產生。
- 轉換後，`build-ci.yml` 改為直接建置 `Polhem.OAuth2.slnx`、不再手動列專案；發佈 workflow 的 restore 也不會再碰到舊式專案。

### 整理既有程式碼（2026-09-13 盤點）

1. **編碼與格式**
   - 去除 BOM：不只 `.cs`，`.resx`、`.cshtml`、`.config`、`.json` 等也有，一併去除。
   - **更正（2026-09-13）**：原本記載「7 個檔案含無效 UTF-8」是 macOS `iconv` 的誤判；改用 Python 實際解碼，
     所有檔案都是合法的 UTF-8。
   - `samples/OAuthDesktop/Form1.cs` 的中文註解已損壞成替代字元（U+FFFD），依下一點重寫或刪除。
   - csproj 由 tab 改為 2 個空白。
2. **註解改英文**
   - 中文 XML doc 約 324 行，改寫為英文。
   - 程式內中文註解約 371 行，先依註解規範判讀：只重述程式碼在做什麼（WHAT）的直接刪除；說明原因（WHY）的改寫為英文。
3. **命名**
   - `PkceHelper` 改名，避開 `*Helper`、走名詞型 utility，名稱實作時定。檔名改成與型別名一致（現為 `PKCEHelper.cs`）。
   - 私有欄位依 `.editorconfig` 的 IDE1006 規則，例如 `OAuth2StateCryptor` 的 static 欄位加 `s_` 前綴。
4. **Nullable**：開啟後修正所有警告；不以 `#pragma` 大範圍壓制，測試故意傳 null 時用 `null!`。
5. **例外**（本階段唯一會改變行為的項目）
   - `throw new Exception` 改為具體型別：核心五處（`OAuth2Provider.cs` 三處、`BaseOAuth2Client.cs` 兩處）。
   - `catch (Exception)` 縮小為預期的例外型別：`BaseOAuth2Client.ValidateAuthorization`，以及 AspNet 與 AspNetCore 的
     `OAuth2Manager.ValidateAuthorization`。非預期例外改為往外拋，不再塞進 `AuthorizationResult.Exception`。
     後續階段新增的例外型別各自補進清單：階段 2 的 `CryptographicException`、階段 3 的 `JsonException`、階段 4 的逾時與取消。
   - 例外訊息不再夾帶完整 HTTP 回應內容（`scanning.md` 的敏感資訊外洩規範），只帶狀態碼。
6. **分析器警告**：在 `TreatWarningsAsErrors` 下全數修正。

Desktop 與 WinForms 兩個專案會在階段 4 刪除，本階段只做第 1 點。兩個 csproj 暫時以專案內屬性關閉
`TreatWarningsAsErrors` 與 `EnforceCodeStyleInBuild`，並附註刪除時機。
OAuthDesktop 與 OAuthWinForms 兩個 sample 會在階段 4 依新流程改寫，同樣只做第 1 點。

### ADR

本 plan 的決策紀錄放在 bee-library，共同開發者看不到。影響 polhem-oauth2 設計、而且理由不明顯的決策，要寫成該 repo 自己的雙語 ADR：

- **移除 `Bee.Base`、自帶 state 加密**：為什麼不等新框架的 base 套件（目標框架不相容），格式為何必須與 `Bee.Base` 3.4.0 相容，並附上黃金樣本測試的位置。
- **語言政策**：共同維護的部分一律英文，公開文件與 ADR 中英雙語。
- **例外語意**：只收納預期的例外，非預期例外往外拋，以及 `AuthorizationResult.Exception` 的意義。

### 驗證

build 與測試全綠，1a 的黃金樣本與加密測試不變。例外語意的改變要補測試：預期例外轉為失敗的 `AuthorizationResult`，
非預期例外往外拋。

### 建立 repo 並推送

建立 `polhem-dev/polhem-oauth2`（public），推送 1a 與 1b 的 commit。公開 repo 的 Actions 不計分鐘數，而 Windows runner 在私有 repo 以兩倍計。
第一次推送觸發的 Windows CI，同時是 net48 與 net8.0-windows 的第一次建置驗證；紅燈就在這一步修掉，不留給後面的階段。

---

## 階段 2：state 解密加強健壯性

移植來的 `Decrypt` 先讀取未經驗證的長度欄位，再依它配置陣列，之後才驗 HMAC。
形狀異常的 state 會擲出 `ArgumentException`、`OverflowException` 等非預期例外，或配置過大的陣列。

- **格式不變**：先驗證總長度與欄位一致（`ivLength` 必為 16、`cipherLength` 為 16 的正倍數、
  `8 + ivLength + cipherLength + 32 == 總長度`），再以常數時間比較 HMAC，最後才解密。
- 所有失敗一律擲 `CryptographicException`；`OAuth2StateCryptor` 的非 Base64 輸入一併收斂到同一例外型別。
- **一併修正 `BaseOAuth2Client.ValidateState`**（1b 發現）：回傳的 state 與儲存的 state 都不存在時，`null == null`
  會判定通過，削弱 CSRF 防護。改為任一方為空即失敗，並補測試。
- 測試：截斷、長度欄位竄改（含極大值與負值）、HMAC 竄改、非 Base64、黃金樣本仍可解。

## 階段 3：JSON 改用 System.Text.Json

- 範圍：六個 provider 的 `ParseUserJson` 與 `OAuth2Provider.cs` 的兩處 token 解析。
- **先寫特性測試再換**：以各 provider 的樣本 JSON 對現行 Newtonsoft 實作寫測試並通過，換成 `JsonDocument` 後同一組測試仍須通過。
- 取值語意：欄位不存在或值為 JSON null → `null`；字串 → 值；其他型別 → JSON 原文，所以數字 id 會保留原本的數字。
  以一個 internal helper 集中處理。
- **與 Newtonsoft 的差異**：特性測試證實 Newtonsoft 對 JSON null 回傳空字串，備援欄位因此不生效；改採 `null`，見決策紀錄。
- netstandard2.0 需參照 `System.Text.Json` 套件；是否另加 net10.0 target 以省掉這個相依，在階段 5 決定。
- 移除 `Newtonsoft.Json` 參照。samples 若自身用到 Newtonsoft 則保留在 sample。

## 階段 4：拿掉 WebView2，改用系統預設瀏覽器

### 新流程

依 RFC 8252 的原生應用程式做法：

1. 在 RedirectUri 指定的 loopback 位址與 port 開始監聽，只綁 loopback。
2. 以系統預設瀏覽器開啟授權 URL。
3. 等待回呼，支援逾時與取消；驗證 `state`；回應一頁「可關閉此分頁」。
4. 用授權碼換 token，後續沿用現有流程。

### 開工前的實測（結果寫進階段 6 的 README）

- 各 provider 是否接受 `http://localhost` 或 `http://127.0.0.1` 的 redirect URI，port 是否必須完全一致：
  Google、Facebook、LINE、Azure（Entra ID）、Auth0、Okta。
- 目前 sample 的三種寫法都要處理：
  - Azure 的 `https://login.microsoftonline.com/common/oauth2/nativeclient`：本機接不到，必須換成 loopback。
  - Auth0／Okta 的 `http://localhost/callback`：未寫 port 等於 80，Windows 上 `HttpListener` 綁 80 可能需要 URL ACL 或系統權限。
    需驗證；必要時要求明確寫 port，或改用 `TcpListener` 自行處理單次 HTTP 請求。
- 實測發現某 provider 不支援 loopback 時，決定是文件標示限制，還是保留內嵌瀏覽器作為另一個套件。

### 實測準備（2026-09-13 進度）

實測工具 `tools/LoopbackRedirectProbe` 已寫好並在 macOS 建置通過，位於 polhem-oauth2 的分支 `claude/loopback-probe`
（已 commit，尚未推送、尚未開 PR），用法見該資料夾的 README。它直接處理 TCP、不用 `HttpListener`，
所以上面「綁 80 port 需要 URL ACL」的疑慮在工具裡不存在；正式實作沿用這個做法（見決策紀錄）。
監聽邏輯另以 scratchpad 程式單獨驗證（2026-09-14）：IPv6 port 被占用時退回只聽 IPv4、其他路徑回 404、query 解碼、逾時、
拒絕非 loopback 位址、IPv4 port 被占用時擲 `SocketException`。尚未在沒有 IPv6 的機器與 Windows 上執行。

**要由使用者先做的事**：在各 provider 後台登記下表的回呼網址，並把 client 憑證填進
`tools/LoopbackRedirectProbe/probe.settings.json`（已 gitignore，範本是同資料夾的 `probe.settings.example.json`）。

| Provider | 後台位置 | 應用程式類型 | 要試的回呼網址 | 結果 |
|---|---|---|---|---|
| Google | Google Cloud Console → APIs & Services → Credentials | Desktop app | `http://127.0.0.1:0/callback`（任意 port）、`http://localhost:53682/callback` | ✅ 2026-09-14，PKCE on：兩個網址都成功導回並換到 token。Desktop app 類型有重新導向 URI 欄位，兩個網址都有登記；`:0` 那筆實際導回 `127.0.0.1:50666` 也被接受，port 不必與登記一致。完全不登記是否可行未測 |
| Microsoft Entra ID | App registrations → Authentication | Mobile and desktop applications | `http://localhost:0/callback`（任意 port）、`http://127.0.0.1:53682/callback` | ✅ 2026-09-14，PKCE on：後台登記的是 `http://localhost`（沒有 port 與路徑），改以 `http://localhost:0` 測。實際導回 `http://localhost:52247/` 被接受並換到 token，**沒送 client secret**（設定檔有填，PKCE 下不送）。Entra 忽略 `localhost` 的 port，結尾的 `/` 也不影響比對。`/callback` 這類路徑是否必須一致、`127.0.0.1` 皆未測 |
| Auth0 | Dashboard → Applications | Native | `http://127.0.0.1:53682/callback`、`http://localhost:53682/callback` | ✅ 2026-09-14，PKCE on：使用者重新申請帳號（tenant `polhem.us.auth0.com`），由 agent 在使用者已登入的 Chrome 建立 Native app「Polhem OAuth2 Loopback Probe」並登記這兩個網址。兩者都導回並換到 token，**沒送 client secret**（設定檔未填 secret）。`127.0.0.1:0` ❌ 實際導回 `127.0.0.1:53153` 時，Auth0 頁面顯示「Oops!, something went wrong」，port 必須與登記一致 |
| Okta | Admin Console → Applications | Native（PKCE） | `http://localhost:53682/callback`、`http://127.0.0.1:53682/callback` | ✅ 2026-09-14：使用者以公司信箱既有的 Integrator Free Plan 組織（`integrator-6742061.okta.com`）登入，由 agent 在 Chrome 建立 OIDC Native app（Client authentication 為 None、Require PKCE、開放組織內所有人）。第一次登入時 Okta 顯示 400「政策評估失敗」：`default` 授權伺服器沒有任何存取政策。agent 新增只套用在此 app 的政策，以及只允許 Authorization Code 的規則後，`localhost:53682` 與 `127.0.0.1:53682` 都導回並換到 token，**沒送 client secret**。`localhost:0`（實際 53867）沒有導回、逾時，port 必須與登記一致 |
| LINE | LINE Developers Console → LINE Login channel → Callback URL | — | `http://localhost:53682/callback`、`http://127.0.0.1:53682/callback` | ✅ 2026-09-14，PKCE on：後台加上 `http://localhost:53682/callback` 後，導回並換到 token，**沒送 client secret 也成功**；使用者資訊沒有 email。加上之前後台只有 `http://localhost/callback`（沒寫 port，即 80），`localhost:53682` ❌ 授權頁顯示 `400 Bad Request`「Invalid redirect_uri value」，port 必須與登記一致。`http://localhost/callback` 本身在 macOS 無法測：一般權限綁不了 port 80（`Permission denied`）。`127.0.0.1` 未登記、未測 |
| Facebook | Meta for Developers → Facebook Login → Settings → Valid OAuth Redirect URIs | — | `http://localhost:53682/callback`、`http://127.0.0.1:53682/callback` | 2026-09-14，PKCE on：`localhost:53682` ✅ 導回並換到 token，**沒送 client secret 也成功**。`127.0.0.1:53682` ❌ Facebook 登入頁顯示該應用程式「傳遞資訊所使用的網路連線並不安全」，無法登入。`localhost:0` ✅ 實際以 `localhost:51464` 導回也被接受，`localhost` 不必與登記的 port 一致。app 模式（開發／上線）未確認，上線模式下 `localhost` 是否仍放行未測。舊版 sample 用的是 `http://localhost:5000/callback` |

- 表中的應用程式類型與「任意 port」都是**待實測的假設**，不是已知事實；不接受任意 port 的 provider 要登記完全相同的 port。
- 每家都以 PKCE 測試。沒有任何一家因為 PKCE 下沒送 client secret 而換 token 失敗，`--pkce off` 從未用上；
  決定 `LoopbackOAuth2Client` 一律使用 PKCE 後，工具的 `--pkce` 選項隨之移除（2026-09-14）。
- 結果回填本表；階段 4 的 ADR 與階段 6 的 README 會引用。

### 實作（2026-09-14 完成）

在 polhem-oauth2 的分支 `claude/system-browser-signin`（接在 `claude/loopback-probe` 之後）實作，經
[polhem-dev/polhem-oauth2#1](https://github.com/polhem-dev/polhem-oauth2/pull/1) 以一般 merge 合併進 `main`（merge commit `1aeca99`，2026-09-14）。內容：

- 核心新增 `LoopbackOAuth2Client`（`src/Polhem.OAuth2/Loopback/`），提供 `SignInAsync(CancellationToken)`、`Timeout`（預設 5 分鐘）與 `OpenBrowser`。
  `OpenBrowser` 為 null 時用系統預設瀏覽器，且只接受 http／https 網址。port 為 0 時，每次登入會把 `Options.RedirectUri` 改成實際綁定的 port，結束後還原。
- 與實測工具不同的行為：
  - 只有帶著本次 state 的請求才會結束等待，其他請求回 400 或 404 後忽略。
  - `localhost` 在 IPv6 上的同一 port 被其他程式占用時，直接啟動失敗（工具是略過 IPv6），避免授權碼被送到那個程式。
  - 連線逐一處理；連上後 5 秒沒送出 request line 的連線（瀏覽器預先建立的）會被放棄。
- 失敗語意：逾時轉成 `TimeoutException`、取消轉成 `OperationCanceledException`、provider 帶 error 導回轉成 `OAuth2Exception`，三者都成為失敗結果；
  `SocketException`、同一個 client 重複登入、授權網址不是 http(s) 則往外拋。
- 刪除 Desktop／WinForms 兩個專案；1b 加的閘門排除設定隨 csproj 一起刪除，全 repo 搜尋確認沒有殘留。`.claude/CLAUDE.md` 的相關說明、
  CI 與發佈 workflow 中的兩個套件與 8.0.x SDK 一併移除。
- samples：OAuthDesktop（改為 net10.0-windows）與 OAuthWinForms（net48）改用 `LoopbackOAuth2Client`，設定檔改用 System.Text.Json 讀取；新增 OAuthConsole（net10.0）。
- 實測工具改用 `LoopbackOAuth2Client`，刪除自帶的監聽程式碼。
- 雙語 ADR-004，六家的實測結果都已填入，狀態改為「已採納」（2026-09-14）。
- `LoopbackOAuth2Client` 一律使用 PKCE（`BaseOAuth2Client.UsePkce` 的 setter 改為 protected，並補測試）；實測工具的
  `RedirectUri` 改由設定檔逐家指定，`--redirect` 改為選填，`--pkce` 移除。
- 驗證：macOS 上全方案建置 0 警告，單元測試全數通過；以改寫後的工具與 OAuthConsole sample 對 Google 實際登入成功。

合併時仍未驗證：
- Windows 驗證：PR 的 Build CI（windows-latest）建置 net48 與 net10.0-windows 並跑測試已通過；監聽程式碼的 netstandard2.0 組建沒有被測試執行到。
- 兩個 WinForms sample 還沒實際登入過。

### 套件結構（已定案）

桌面流程併入核心 `Polhem.OAuth2`，`Polhem.OAuth2.Desktop` 與 `Polhem.OAuth2.WinForms` 兩個專案移除（見決策紀錄）。
核心只增加 BCL 型別、不新增相依，桌面登入因此可在 Windows、macOS、Linux 使用（含 Avalonia 與主控台程式）。
網頁套件不受影響。

### 其他

- **破壞性變更**（新套件 ID 本就需要遷移，併入同一次）：桌面專用套件整個移除。同步的 `Authorization()`、
  `Caption`／`Width`／`Height`、`AuthorizationForm`，以及桌面版的 `OAuth2Client`／`OAuth2Manager`／`StateStorage`
  隨之消失，改用核心提供的新 API。
- 桌面應用程式屬 public client：`ClientSecret` 無法保密，README 標示；評估桌面流程是否預設啟用 PKCE。
- samples：OAuthDesktop 與 OAuthWinForms 依新流程改寫；另加一個 console sample，macOS 本機即可驗證整條流程。
- 階段 1b 為 Desktop／WinForms 加的閘門排除設定隨專案一起刪除，確認沒有殘留。
- **雙語 ADR**：說明為何從 WebView2 改為系統瀏覽器加 loopback（RFC 8252），以及為何併入核心、套件由五個減為三個。
  各 provider 的 loopback 實測結果與限制也記在這份 ADR。

## 階段 5：目標框架 net8.0 → net10.0

- `Polhem.OAuth2.AspNetCore`：net8.0 → net10.0。`Microsoft.AspNetCore.Http.Abstractions` 2.3.0 套件改為
  `<FrameworkReference Include="Microsoft.AspNetCore.App" />`，少一個套件相依。
- **階段 5 提前到階段 4 之前**（見決策紀錄）：Desktop 專案與 OAuthDesktop sample 暫時維持 net8.0-windows，階段 4 刪除或改寫時處理，不另外升級。
- OAuthAspNetCore sample 參照 AspNetCore 套件，一併升 net10.0。
- 核心：`netstandard2.0;net10.0`，`System.Text.Json` 套件參照只套用在 netstandard2.0（見決策紀錄）。
- AspNet（net48）不變。CI 的 setup-dotnet 補 10.0.x。

## 階段 6：推廣準備

- **根 README 中英雙語**：`README.md`（英文，預設）與 `README.zh-TW.md`，頂部互放語言切換連結，修改時兩份同步。內容：
  - 支援的 provider 一覽，補上 Okta。
  - 各平台快速開始：ASP.NET Core、ASP.NET、桌面系統瀏覽器。
  - `OAUTH2_STATE_KEY` 的產生方式：64 bytes 亂數轉 Base64。
  - 各 provider redirect URI 的登記方式，取自階段 4 實測結果。
  - 從 `Bee.OAuth2` 遷移：命名空間、桌面流程改變、redirect URI 需重新登記，
    以及 `AuthorizationResult.Exception` 不再收納非預期例外（見階段 1b）。
  - 傳統 ASP.NET（System.Web）沒有可執行的 sample，用法以 README 裡的程式碼片段說明（見決策紀錄）。
  - 使用者資訊裡值為 JSON null 的欄位，現在對應為 `null`，Bee.OAuth2 是空字串；備援欄位也因此會生效。
- **刪除各套件自己的 `README.md`**：比照 bee-library，所有套件都打包根目錄的 `README.md` 作為 NuGet 的 README。
  每個套件若各自雙語，就是每個套件兩份檔案要同步；集中後說明只有一個來源。
  NuGet 只顯示英文版，頂部的中文版連結要用 GitHub 絕對網址，因為 nuget.org 上的相對連結會失效。
- **NuGet 中繼資料**：`Description` 補齊 provider；`PackageTags` 修正 AspNetCore 的誤植，並補 `openid-connect`、`pkce` 與各 provider 名稱。
- **org profile**（`polhem-dev/.github`）：`profile/README.md` 列出已發佈的 Polhem.OAuth2，框架本身仍標示準備中。
  中文版放 `profile/README.zh-TW.md`，由英文版頂部連過去，因為組織首頁只顯示 `README.md`。

### 實作（2026-09-14 完成）

polhem-oauth2 commit `a655b26`，直接推 `main`：

- 根 `README.md`（英文）重寫，新增 `README.zh-TW.md`，涵蓋上列各點。另外寫進去的：
  - 網頁套件的前提：state cookie 標 `Secure`，所以回呼頁面必須走 HTTPS；開啟 `UsePkce` 時必須啟用 session。
  - `OAUTH2_STATE_KEY` 在每個行程只讀一次，所以要在啟動前設定；可能收到回呼的伺服器要用同一把。產生方式列 `openssl` 與 C# 兩種。
  - 「結果與錯誤」一節說明 `AuthorizationResult.Exception` 的意義，並指向 ADR-003。
  - 回呼網址登記表註明 2026-09-14 實測，並指向 ADR-004。補三點注意事項：Okta 需要存取政策、Facebook 拒絕 `127.0.0.1`、沒寫 port 等於 port 80。
  - System.Web 範例不用 C# 8 以後的語法，因為傳統 ASP.NET 專案預設 C# 7.3。
  - 舊 README 的「Contact & Follow」個人連結沒有保留，改成一行「延續自 Bee.OAuth2」。
- 連結：英文版會打包進 NuGet，所以一律用 GitHub 絕對網址；中文版只在 GitHub 顯示，用相對連結並指向中文版 ADR。
- 刪除三個套件各自的 `README.md` 與只用來打包它們的 `src/Directory.Build.targets`，改在 `src/Directory.Build.props` 打包根目錄 README。
- `Description` 改寫並補齊 provider：Azure 改稱 Microsoft Entra ID，並補上 Okta。`PackageTags` 改為小寫、以連字號分隔，補 `openid-connect`、`pkce` 與各 provider，並移除 AspNetCore 誤植的 `WinForms`。
- 宣告範圍外多動一檔：`samples/OAuthAspNetCore/Program.cs` 刪掉一行提到舊型別名 `TOAuth2Manager` 的註解。
- 驗證：macOS clean build 0 警告 0 錯誤，112 個測試通過。三個 nupkg 都含根目錄 README 與圖示，nuspec 的 description、tags 與相依清單正確（核心的 netstandard2.0 只相依 System.Text.Json，net10.0 沒有相依）。README 的相對連結與指向 repo 內的絕對連結都對得到檔案。推送後 Windows 上的 Build CI 通過。
- org profile：英文版加上套件一節，新增中文版。組織首頁的相對連結會失效，所以兩份互連用絕對網址。本機 commit `1176df0` 放在 `~/Desktop/repos/polhem-dev-github`，**尚未推送**，見決策紀錄與階段 7 第 7 步。

## 階段 7：首發 1.0.0

1. **發佈授權改用 Trusted Publishing**（2026-09-14，見決策紀錄）：
   - nuget.org 建立 policy `polhem-oauth2-release`：Package Owner `Polhem`、repo `polhem-dev/polhem-oauth2`、workflow `nuget-publish.yml`、
     不設 environment、scope 只選 Push new packages and package versions、glob `Polhem.OAuth2*`。
     建立後列表顯示 **Active**，repository owner 與 repository 已綁定 GitHub 的數字 ID，沒有進入 7 天暫時啟用期。
   - `nuget-publish.yml` 加 `id-token: write`，推送前以 `NuGet/login`（pin v1.2.0 的 SHA）換取 1 小時有效的臨時 key；
     `user` 讀 repo secret `NUGET_USER`（值為 `jeff377`）。不設 `NUGET_API_KEY`。polhem-oauth2 commit `e700927`。
   - **未驗證**：首發是第一次實際換 key，也是這條 policy 能否建立「尚不存在的套件 ID」的第一次驗證。失敗時先看 `Log in to NuGet` 那一步的訊息。
2. 商標：依 future-work 規定，發佈第一版前重查一次（初步檢索為 2026-09-12）。
3. 導入 `Microsoft.CodeAnalysis.PublicApiAnalyzers`（與 bee-library 相同），以 1.0.0 的公開 API 建立 `PublicAPI.Shipped.txt`。
   刻意等到首發才導入，避免功能修改期間 baseline 反覆改寫；首發之後，公開 API 的破壞性變更由它擋下。
4. 發佈前 clean build 與測試全綠；紅燈是訊號，不為發版而改測試或原始碼。
5. **推送 `v1.0.0` tag 須使用者明確同意**，發佈後無法撤回。
6. 驗證：nuget.org 上各套件的圖示、README、相依清單（不得出現 `Bee.Base`、Newtonsoft.Json）；
   在全新專案安裝並跑一次最小範例。
7. **推送 org profile**：階段 6 已在本機 clone `~/Desktop/repos/polhem-dev-github` commit（`1176df0`，尚未推送）。
   確認 nuget.org 的套件頁開得到之後，才推到 `polhem-dev/.github` 的 `main`，並確認組織首頁與中文版連結。

## 階段 8：凍結舊套件與舊 repo

1. **（使用者操作，以 jeff377 身分於 nuget.org）** 五個 `Bee.OAuth2.*` 標 deprecated，原因選 Legacy，並填替代套件：
   `Bee.OAuth2`、`Bee.OAuth2.WinForms`、`Bee.OAuth2.Desktop` → `Polhem.OAuth2`；
   `Bee.OAuth2.AspNet` → `Polhem.OAuth2.AspNet`；`Bee.OAuth2.AspNetCore` → `Polhem.OAuth2.AspNetCore`。
2. 舊 repo `README.md` 頂部以中英兩種語言加上新位置說明；issue #1 回覆指向新 README 的桌面範例後關閉。
3. `jeff377/bee-oauth2` archive（可逆）。**不刪除**：fork、星數與既有連結都依附在它上面。
4. 舊 repo 的 `NUGET_API_KEY` 若只給這個 repo 用，到 nuget.org 撤銷（使用者操作）。

## 階段 9：回寫 bee-library

- `docs/repo-ops/future-work.md`：
  - 「已占名」表補上 `polhem-dev/polhem-oauth2` 與 `Polhem.OAuth2` 套件。
  - 前綴保留一列註明「首個套件已發佈」這個前提已成立。
  - 新增「bee-oauth2 演練結果」：照「這次演練得到與演練不到的」那張表回填實際結果與踩到的雷，供 bee-library 啟動時使用。
- 本 plan 標記完成，更新 `docs/plans/README.md` 索引。

## 範圍外

- 申請 NuGet `Polhem.` 前綴保留、發文公告。
- SonarCloud 掃描：已遷移為編譯期規則的部分由 `.editorconfig` 把關，其餘不導入。
- CONTRIBUTING、CODEOWNERS 等開放共同維護的文件：隨 Polhem 框架一併處理（見 future-work「開放共同維護」一節）。
- 行動端（iOS／Android）登入流程。
- 1b 發現、另案清理（程式碼已加 NOTE 註解）：
  - `AzureOAuth2Provider` 在 token 請求帶 `response_mode`，但這個參數只屬於授權請求。
  - AspNet 與 AspNetCore 的 state cookie 設成 `SameSite=None`；provider 以 GET 導回時 `Lax` 就會帶上 cookie。
