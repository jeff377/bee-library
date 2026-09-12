# 計畫：bee-oauth2 改名為 Polhem.OAuth2 並移至 polhem-dev

**狀態：🚧 進行中（2026-09-13）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1a | 本機建立 polhem-oauth2：純改名、拿掉 `Bee.Base`、加密移植與測試、CI、`.slnx` | ✅ 已完成（2026-09-13） |
| 1b | 導入 bee-library 現行程式碼風格設定，整理既有程式碼（英文化、例外、命名、Nullable、編碼、SDK 格式）；完成後建立 repo 並推送 | 🚧 進行中 |
| 2 | state 解密加強健壯性 | 📝 待做 |
| 3 | JSON 改用 System.Text.Json | 📝 待做 |
| 4 | 拿掉 WebView2，改用系統預設瀏覽器 + loopback 回呼；桌面流程併入核心，Desktop／WinForms 套件移除 | 📝 待做 |
| 5 | 目標框架 net8.0 → net10.0 | 📝 待做 |
| 6 | 推廣準備：README、NuGet 中繼資料、org profile | 📝 待做 |
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
| 方案與專案格式 | 方案檔用 `.slnx`；所有專案一律 SDK 格式，WebForms sample 用 `MSBuild.SDK.SystemWeb` | 使用者決定。`.slnx` 只列專案與資料夾；全部改成 SDK 格式後，CI 可以直接建置整個方案 |

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

macOS 只建得起 netstandard2.0 核心、AspNetCore 與測試專案。net48、net8.0-windows 與舊式 WebForms／WinForms sample
**以 windows-latest CI 為準**。repo 要到 1b 完成後才建立，所以 1a 與 1b 對這些目標的驗證都等第一次推送；
之後動到它們的階段（4、5）走分支 + PR，讓 CI 在合併前把關。

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
- OAuthAspNet（net48 WebForms）改用社群維護的 `MSBuild.SDK.SystemWeb`，只用在 sample，不進入發佈的套件。
- 兩者的 `packages.config` 改為 `PackageReference`，`Properties/AssemblyInfo.cs` 改由 SDK 產生。
- 轉換後，`build-ci.yml` 改為直接建置 `Polhem.OAuth2.slnx`、不再手動列專案；發佈 workflow 的 restore 也不會再碰到舊式專案。

### 整理既有程式碼（2026-09-13 盤點）

1. **編碼與格式**
   - 33 個 `.cs` 去除 BOM。
   - 7 個檔案含無效 UTF-8 位元組：`IStateStorage.cs`、三個 `StateStorage.cs`、Desktop 的 `AuthorizationForm.cs`、
     `samples/OAuthAspNet` 的 `AssemblyInfo.cs` 與 `Web.Release.config`。轉成 UTF-8 時逐一確認內容沒有損毀。
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
- 測試：截斷、長度欄位竄改（含極大值與負值）、HMAC 竄改、非 Base64、黃金樣本仍可解。

## 階段 3：JSON 改用 System.Text.Json

- 範圍：六個 provider 的 `ParseUserJson` 與 `OAuth2Provider.cs` 的兩處 token 解析。
- **先寫特性測試再換**：以各 provider 的樣本 JSON 對現行 Newtonsoft 實作寫測試並通過，換成 `JsonDocument` 後同一組測試仍須通過。
- 保留 `JToken?.ToString()` 的語意：欄位不存在或 null → `null`；字串 → 值；數字與布林 → 原文。
  以一個 internal helper 集中處理。
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
- 原本 net8.0-windows 的 Desktop 專案已在階段 4 移除，不需處理。
- 核心：維持 netstandard2.0（net48 使用者需要）；決定是否多打 net10.0，讓 net10 使用者不必帶 `System.Text.Json` 套件。
- AspNet（net48）不變。CI 的 setup-dotnet 補 10.0.x。

## 階段 6：推廣準備

- **根 README 中英雙語**：`README.md`（英文，預設）與 `README.zh-TW.md`，頂部互放語言切換連結，修改時兩份同步。內容：
  - 支援的 provider 一覽，補上 Okta。
  - 各平台快速開始：ASP.NET Core、ASP.NET、桌面系統瀏覽器。
  - `OAUTH2_STATE_KEY` 的產生方式：64 bytes 亂數轉 Base64。
  - 各 provider redirect URI 的登記方式，取自階段 4 實測結果。
  - 從 `Bee.OAuth2` 遷移：命名空間、桌面流程改變、redirect URI 需重新登記，
    以及 `AuthorizationResult.Exception` 不再收納非預期例外（見階段 1b）。
- **刪除各套件自己的 `README.md`**：比照 bee-library，所有套件都打包根目錄的 `README.md` 作為 NuGet 的 README。
  每個套件若各自雙語，就是每個套件兩份檔案要同步；集中後說明只有一個來源。
  NuGet 只顯示英文版，頂部的中文版連結要用 GitHub 絕對網址，因為 nuget.org 上的相對連結會失效。
- **NuGet 中繼資料**：`Description` 補齊 provider；`PackageTags` 修正 AspNetCore 的誤植，並補 `openid-connect`、`pkce` 與各 provider 名稱。
- **org profile**（`polhem-dev/.github`）：`profile/README.md` 列出已發佈的 Polhem.OAuth2，框架本身仍標示準備中。
  中文版放 `profile/README.zh-TW.md`，由英文版頂部連過去，因為組織首頁只顯示 `README.md`。

## 階段 7：首發 1.0.0

1. **（使用者操作）** NuGet 組織帳號 `Polhem` 建立 API key：push 權限，glob 限定 `Polhem.OAuth2*`。
   以 `gh secret set NUGET_API_KEY -R polhem-dev/polhem-oauth2` 自行輸入。
2. 商標：依 future-work 規定，發佈第一版前重查一次（初步檢索為 2026-09-12）。
3. 導入 `Microsoft.CodeAnalysis.PublicApiAnalyzers`（與 bee-library 相同），以 1.0.0 的公開 API 建立 `PublicAPI.Shipped.txt`。
   刻意等到首發才導入，避免功能修改期間 baseline 反覆改寫；首發之後，公開 API 的破壞性變更由它擋下。
4. 發佈前 clean build 與測試全綠；紅燈是訊號，不為發版而改測試或原始碼。
5. **推送 `v1.0.0` tag 須使用者明確同意**，發佈後無法撤回。
6. 驗證：nuget.org 上各套件的圖示、README、相依清單（不得出現 `Bee.Base`、Newtonsoft.Json）；
   在全新專案安裝並跑一次最小範例。

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
