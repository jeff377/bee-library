# 計畫：Bee.Northwind 同步至 bee-northwind-avalonia（框架 4.30.0）

**狀態：✅ 已完成（2026-09-10）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 0 | 「順帶發現」兩項（`OrderBO` 的 `isLocalCall` 預設值、空目錄）於 bee-library 端處理 | ✅ 已完成（2026-09-10）：commit `a042f9c5` |
| 1 | 框架版號 4.27.0 → 4.30.0（兩個 csproj、六筆參考） | ✅ 已完成（2026-09-10） |
| 2 | 雙語 README 修正一段**已經失效**的敘述（`FormLayout` 自動產生） | ✅ 已完成（2026-09-10） |
| 3 | 六個 head build ✅ / Desktop 冒煙 ⛔ 工具鏈受阻 / commit + push | ✅ 已完成（2026-09-10）：commit `9c6259c` 推上 `bee-northwind-avalonia` 的 `main` |

> 使用者已確認（2026-09-10）：照計畫做、冒煙**只跑 Desktop**、「順帶發現」兩項一併處理。

## 背景

`bee-library` 的 `apps/Bee.Northwind` 是對框架原始碼開發的案例；
[`bee-northwind-avalonia`](https://github.com/jeff377/bee-northwind-avalonia) 是它的獨立副本，
純以已發行的 `Bee.*` NuGet 套件引用。目標 repo 最後同步於 **4.27.0**，本次跟上 **4.30.0**。

本機 clone：`~/Desktop/repos/bee-northwind-avalonia`（工作區乾淨，HEAD 為 `a39ff20`）。

## 現況盤點（已實測，非推測）

以 `diff -rq`（排除 `bin` / `obj` / `.git` / `.github` / `.gitignore` / `LICENSE`）比對兩棵樹：

| 落差 | 性質 | 處置 |
|------|------|------|
| 5 個 `.csproj` | 參考型式（ProjectReference vs PackageReference）＋版號 | **只改版號**，型式差異是刻意的 |
| `README.md` / `README.zh-TW.md` | 20 行差異，其中 **1 段是失效敘述**、其餘為 standalone 措辭 | 只修那一段，其餘保留 |
| `Bee.Northwind.Browser/README.md` | 6 行，全為路徑調整 | 不動 |
| `.smoke.yaml` | 2 行路徑調整（standalone 佈局） | 不動 |
| `.vscode/` | 只在目標端 | 不動 |
| `northwind.db` | **兩邊皆 gitignore**，非版控內容 | 不同步 |
| `Define/Language/en-US/` | 只在 bee-library 端，且是**空目錄** | 不同步（另見「順帶發現」） |

**`Define/`、`Customize/` 與所有 `.cs` 逐檔相同** —— 4.28.0 與 4.29.0 期間
`apps/Bee.Northwind` 本身沒有變動，那兩版都是框架端的事。
**因此本次沒有任何應用內容要搬**，這與 4.21.0 那次（47 檔、+1135/−329）性質完全不同。

### 破壞性變更曝險：零

4.28.0 與 4.29.0 各有一批破壞性變更。逐項對兩棵樹的原始碼（排除 `bin` / `obj`）掃描：

`CheckPackageUpdate`、`GetPackage`、`ApiCallContext`、`HttpUtilities`、
`AllowColumnNarrowing`、`PluginItem`、`FormPluginStage`、`PluginSettings`、
`GetFormLayout` —— **全數零命中**。

兩筆命中經查證皆為良性：`AuditRuleBusinessObject` 只出現在 `ProgramSettings.xml` 的註解；
`isLocalCall` 是 `OrderBO` 自己的建構子參數（見下方「順帶發現」）。

## 階段 1：版號

五個 csproj 的 `Bee.*` `PackageReference` 由 `4.27.0` 改為 `4.30.0`：

```
Bee.Northwind.Server/    Bee.Definition, Bee.Api.AspNetCore, Bee.Business, Bee.Db, Bee.Hosting
Bee.Northwind.UI/        Bee.UI.Avalonia
Bee.Northwind.{Desktop,Browser,Android,iOS}/  （依實際引用）
```

**就地改版號字串，不要用 bee-library 的 csproj 覆蓋** —— 兩邊的參考型式本來就不同，
覆蓋會把 PackageReference 換回 ProjectReference，指向獨立 repo 裡不存在的路徑。

### ⚠️ 必須保住的接線（4.21.0 那次踩過）

目標端 `Bee.Northwind.Server.csproj` 目前有兩處看似多餘、實則必要的設定，**改版號時不得動到**：

1. **顯式的 `<PackageReference Include="Bee.Definition" />`** —— Server 的功能相依是
   `Bee.Api.AspNetCore` / `Business` / `Db` / `Hosting`，`Bee.Definition` 是遞移而來。
   但 NuGet **只對直接 PackageReference 匯入套件的 `build/` 資料夾**，而
   `build/Bee.Definition.targets` 正是注入 `AdditionalFiles` 的地方。少了這行，
   analyzer 會載入卻讀不到任何定義檔 —— **規則靜默不執行，沒有任何診斷會說**。
2. **`<BeeDefinitionFilesGlob>..\Define\**\*.xml</BeeDefinitionFilesGlob>`** —— 告訴上述
   targets 定義檔在哪。

**驗收方式**：建置時 `AdditionalFiles` 筆數應等於磁碟上的定義檔數，不是 0。

## 階段 2：README 的一段失效敘述

雙語 README 各有一段描述的是 **4.23.0 之前**的行為：

| | 目標端現況（錯） | 應改為（bee-library 現況） |
|---|---|---|
| zh-TW | 不需要寫 `FormLayout` 檔 —— 框架會在交付時從 `FormSchema` 自動產生版面。 | 同時要寫對應的 `FormLayout/…`。執行階段渲染的就是這個檔，**檔案不存在會失敗**；`FormLayoutGenerator` 可在設計階段產生初稿。 |
| en | There is no `FormLayout` file to write — the framework generates the layout from the `FormSchema` at delivery time. | Write the matching `FormLayout/…` as well. The runtime renders that file and **fails when it is absent**；`FormLayoutGenerator` produces a starting point at design time. |

`FormSchema.GetFormLayout` 於 4.23.0 移除，該敘述自那時起就不成立
——目標端停在 4.27.0 卻沒跟上，是上一輪同步的漏網。

**其餘 19 行差異全部保留**：CI 徽章、`https://github.com/jeff377/bee-library` 絕對連結、
`dotnet run --project Bee.Northwind.Server`（無 `apps/Bee.Northwind/` 前綴）、
目錄樹根名、結尾的「本 demo 於 bee-library 內開發」說明。這些是 standalone 副本應有的差異。

## 階段 3：驗證與推送

1. **六個 head 對 NuGet 4.30.0 建置**：Server / UI / Desktop / Browser / Android / iOS，
   要求 0 警告 0 錯誤（獨立 repo 亦為 `TreatWarningsAsErrors`）。
2. **端到端冒煙**：Desktop head 走一次登入 → 選單 → 清單真實資料 → 開記錄。
   `.smoke.yaml` 已備妥（路徑為 standalone 版）。
3. 通過後 commit + push 至 `bee-northwind-avalonia` 的 `main`。

### ⚠️ iOS 的 Xcode 版本（4.21.0 那次誤判過）

.NET for iOS SDK 要求精確對應的 Xcode 版本，不符會在 build 一開始就失敗。
**這通常不是「環境不支援」** —— 對應版本多半已側裝在 `/Applications/`。
撞到時先 `ls -d /Applications/Xcode*.app`，再以 `DEVELOPER_DIR=` 指過去建，**不要動
`xcode-select`**，也不要用 `-p:ValidateXcodeVersion=false` 硬建。
（詳見 `.claude/rules/apple-mobile-trim.md`；上次就是因為沒帶 `DEVELOPER_DIR` 而誤報成建不起來。）

## 執行結果

### 階段 1：實際是兩個 csproj、六筆參考（計畫寫「五個 csproj」有誤）

只有 `Bee.Northwind.Server`（5 筆）與 `Bee.Northwind.UI`（1 筆）直接引用 `Bee.*`；
Desktop / Browser / Android / iOS 四個 head 都是經 `ProjectReference` 指向 UI。

analyzer 接線完好（`Bee.Definition` 直接參考 + `BeeDefinitionFilesGlob` 都在）。
csproj 註解另記載了一件本計畫寫錯的事：**該直接參考自 4.22.0 起已非必要**
——targets 那時已由 `build/` 移到 `buildTransitive/`。留著無害，計畫「必須保住」的說法過強。

### 階段 3：六個 head 全綠

| head | 結果 |
|------|------|
| Server / UI / Desktop / Browser / Android | ✅ 0 警告 0 錯誤 |
| iOS | ✅ 0 錯誤，**21 個警告** |

**iOS 的警告是既有的，不是本次造成的**（完整判讀與數字不可比較的理由已收進
[gotchas/mobile-trim-aot.md](../../repo-ops/gotchas/mobile-trim-aot.md) 第四節）：全部是 `IL2026` / `IL2057`，即 trim 分析器對
`DataSet` / `DataTable` XML 序列化與 `ViewLocator` 反射的固有告警。對照組是 bee-library
自己的 iOS head —— 同樣 0 錯誤但 **67 個警告**（更多，因為 ProjectReference 的分析面比
已 trim 的 NuGet 套件大）。**計畫寫的「0 警告」對 iOS head 是訂錯了門檻**，該 head 從來
沒有、也不會是 0 警告。

iOS 如計畫預告的撞到 Xcode 版本（需 26.5、`xcode-select` 指著 26.6），
以 `DEVELOPER_DIR=/Applications/Xcode-26.5.0.app/Contents/Developer` 指過去即通過，
未動全域設定。

### ⛔ 階段 3：Desktop 冒煙無法執行（平台限制，與本次同步無關）

`.smoke.yaml` 以 `dotnet Bee.Northwind.Desktop/…/Bee.Northwind.Desktop.dll` 啟動 app
（Avalonia 桌面端在 macOS 沒有 `.app` bundle）。app 確實跑起來、視窗標題為 `Bee.Northwind`，
但 **macOS 只看到一個沒有 bundle identifier 的 `dotnet` 進程**：

- `request_access` 對 `Bee.Northwind.Desktop` / `dotnet` 皆回 `notInstalled`，對話框根本沒顯示。
- 已安裝清單裡只有 iOS 端的 `com.bee.northwind`，桌面端沒有可授權的識別碼。
- 退而求其次的全螢幕控制也不行：`request_full_control` 回
  `No applications are granted for this session`，而 display-scope 的 frontmost 閘門
  仍會要求該進程在允許清單內。

**這正是 `demo-smoke` skill 檔頭「知道的雷」列的那一條**（「以 `dotnet <dll>` 啟動時進程名是
`dotnet`」），只是那裡談的是 teardown 取名，沒談到它會讓 app 完全無法被授權。

**沒有以別的方式假裝驗過**：曾試以 `curl` 直打 `System.Ping` 取代，但手搓的請求不符
client 的信封格式（回 `NullReferenceException`），**那個錯誤不能歸因於伺服器**，
因此不採計為任何證據。

> 附帶一提，「格式不符的請求回 NRE 而非結構化錯誤」本身可能值得看一眼，但我無法在不重現
> 真正 client 協定的前提下斷定，屬 bee-library 的另案觀察，不列為本次發現。

**處置（使用者裁定，2026-09-10）**：以建置驗證交付，不補冒煙。判斷依據是本次的風險面
——應用內容零落差、破壞性變更曝險經逐項掃描為零、六個 head 全數建置通過。
**這代表本版沒有任何執行期證據**，若日後 Northwind 出現只在執行期才顯現的問題，
這一輪是第一個要回頭看的地方。

要讓冒煙日後可跑，路徑是把 Desktop 打包成 `.app` bundle（見 `avalonia-macos-bundle` skill）
並改寫 `.smoke.yaml` 的 `launch`。那會改變 demo 的啟動方式，屬另案。

## 順帶發現（不屬本次同步，供判斷是否另案）

### 1. `OrderBO` 的 `isLocalCall` 預設值保留了 4.28.0 刻意改掉的行為

`apps/Bee.Northwind/Bee.Northwind.Server/BusinessObjects/OrderBO.cs`（兩邊同碼）：

```csharp
public OrderBO(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall = true)
```

4.28.0 把**基底類別**的同名參數預設值由 `true` 改為 `false`，理由是「直接建構 BO 是唯一
繞過 `ApiAccessValidator` 的路徑，預設信任等於把四道第二線防護開在最不該信任的路上」。
`OrderBO` 自帶 `= true` 因此**沿用了舊行為**，且編譯不會有任何訊號。

案例專案的曝險有限（沒有直接建構 `OrderBO` 的呼叫端），但它是**寫給人抄的範本**。
建議另案在 bee-library 端改為 `= false`，兩邊一起。

### 2. `apps/Bee.Northwind/Define/Language/en-US/` 是空目錄

git 不追蹤空目錄，所以它只存在於本機工作區。若非刻意保留，可直接刪。

## 裁定（2026-09-10）

| 問題 | 裁定 |
|------|------|
| 計畫範圍 | 接受 |
| 冒煙程度 | 只跑 Desktop —— 後因工具鏈限制無法執行，改以建置驗證交付 |
| 「順帶發現」兩項 | 一併處理（`OrderBO` 兩邊同步、空目錄刪除） |
