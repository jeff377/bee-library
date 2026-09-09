# 計畫：Bee.Northwind 同步至 bee-northwind-avalonia（框架 4.30.0）

**狀態：🚧 進行中（2026-09-10）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 0 | 「順帶發現」兩項（`OrderBO` 的 `isLocalCall` 預設值、空目錄）於 bee-library 端處理 | 🚧 進行中 |
| 1 | 五個 csproj 的框架版號 4.27.0 → 4.30.0（就地改，不覆蓋檔案） | 📝 待做 |
| 2 | 雙語 README 修正一段**已經失效**的敘述（`FormLayout` 自動產生） | 📝 待做 |
| 3 | 六個 head 對 NuGet 4.30.0 build + Desktop 冒煙，通過後 commit + push | 📝 待做 |

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

## 待確認

1. 接受本計畫範圍嗎？（重點是：**沒有應用內容要搬**，只有版號、一段 README 與驗證）
2. 階段 3 的冒煙要跑到什麼程度 —— Desktop 一輪即可，還是六個 head 都要實跑？
3. 「順帶發現」的兩項要不要納入本次，或另案處理？
