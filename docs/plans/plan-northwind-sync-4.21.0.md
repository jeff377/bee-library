# 計畫：Bee.Northwind 同步至 bee-northwind-avalonia（框架 4.21.0）

**狀態：✅ 已完成（2026-08-13）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 檔案同步（覆蓋 / 新增 / 刪除 / 保留清單） | ✅ 已完成（2026-08-13）：rsync 覆蓋 + 新增 15 檔、刪除 3 檔；`.gitignore` 補 `log/` 負向規則後 5 個稽核 schema 確實入版控 |
| 2 | ProjectReference → PackageReference 4.21.0（含 analyzer glob） | ✅ 已完成（2026-08-13）：Server / UI 轉套件參考；**計畫對 analyzer 的判斷修正了一半**（見下方「執行中的修正」）；三個 head 補時區屬性、兩處 bee-library 內部路徑註解改寫 |
| 3 | README 雙語逐段 port（不整份覆蓋） | ✅ 已完成（2026-08-13）：兩邊章節結構相同（截圖段亦同），故以 bee-library 版為基底重貼、再套回 4 處 standalone 差異（intro、指令路徑、layout 根目錄、結尾說明）；`Bee.Northwind.Browser/README.md` 的 3 處路徑一併修 |
| 4 | 獨立 repo build + 冒煙驗證 | ✅ 已完成（2026-08-13）：Server / UI / Desktop / Browser / Android 對 NuGet 4.21.0 全綠（0 警告 0 錯誤）；端到端冒煙通過。**iOS 卡本機 Xcode 26.6 vs .NET for iOS 26.5**，已比對 bee-library 端同樣失敗 → 環境問題 |
| 5 | commit + push 至 bee-northwind-avalonia | ✅ 已完成（2026-08-13）：commit `f73ebf4` 推上 main（47 檔、+1135 / -329），遠端已驗 `Define/TableSchema/log/` 5 檔與 4.21.0 版號 |

## 執行中的修正：analyzer 接線比計畫預期的多一步

計畫的階段 2 寫「analyzer 不需顯式引用，隨 PackageReference 遞移流入」——
**這句只對了一半，實測拆成兩件事**：

| 資產 | 是否遞移流入 | 結果 |
|------|------------|------|
| `analyzers/dotnet/cs/Bee.Analyzers.dll` | ✅ 會 | analyzer 有載入 |
| `build/Bee.Definition.targets`（注入 `AdditionalFiles`） | ❌ **不會** | analyzer 讀不到任何定義檔 |

NuGet 只對**直接** PackageReference 匯入套件的 `build/` 資料夾（要遞移得放 `buildTransitive/`）。
本 repo 的 Server 只直接引用 `Bee.Api.AspNetCore` / `Business` / `Db` / `Hosting`，
`Bee.Definition` 是遞移而來 → targets 未匯入 → `BeeDefinitionFilesGlob` 這個屬性根本沒人讀。

**實測**：改之前 `AdditionalFiles` 為 **0 筆**（bee-library 端顯式宣告則有 41 筆）。
**正解**：Server 額外直接引用 `Bee.Definition`，targets 才匯入；再配合 `BeeDefinitionFilesGlob`
指向上一層的 `Define/`。改後 `AdditionalFiles` = **41 筆 = 磁碟上定義檔數**，建置仍 0 警告。

> 這同時是一個**框架層的發現**：任何只引用 `Bee.Business` / `Bee.Db` 一類上層套件的消費者，
> 都會拿到 analyzer 卻拿不到定義檔注入 —— 規則靜默不執行，且沒有任何診斷會說。
> 屬 bee-library 的另案（`build/` → `buildTransitive/`）。

## 冒煙驗證細節

以一支 headless 臨時 client（直接引用 NuGet `Bee.Api.Client` / `Bee.UI.Core` 4.21.0）驗完整 wire 路徑，
比 GUI 點擊更能證明 MessagePack formatter 在 4.21.0 的相容性：

```
ping        : ok
initialize  : ok                     ← RSA 金鑰交換
login       : ok (token issued=True) ← 框架 st_user 驗證
getlist     : Order -> 5 row(s)      ← 含 ref_customer_name / ref_employee_name（lookup JOIN 回填）
getdata     : 2 table(s) -> Order(1), OrderDetail(2)  ← 主從
```

另驗：Server 首次啟動建 21 張表（含 log 類別 5 張）、seed 資料就緒
（`ft_order` 5 / `ft_order_detail` 12 / `ft_product` 15 / `st_employee` 5）、
登入紀錄如期寫入 `st_log_login`、`st_cache_notify.TableSchema.xml` 於啟動時重新落地
並被新的 ignore 規則正確排除。

---


## 背景

`apps/Bee.Northwind` 已於 2026-06-26 畢業至獨立 repo
[`bee-northwind-avalonia`](https://github.com/jeff377/bee-northwind-avalonia)（見
`archive/plan-northwind-avalonia-graduation.md`）。目標 repo 最後同步於 **2026-06-27
（commit `243fde9`，框架 4.12.1）**，此後 bee-library 端已推進至 **4.21.0**，落後 9 個 minor。

本計畫把目標 repo 同步至現況。**本機已有 clone**：`~/Desktop/repos/bee-northwind-avalonia`
（工作目錄乾淨、與 origin/main 同步）。

### 已確認的決策

| 項目 | 決定 |
|------|------|
| bee-library 的 `apps/Bee.Northwind` | **保留不刪**（維持 `[[northwind-graduation-keep-source]]`：續作 dogfooding） |
| 同步方式 | **直接覆蓋 + 單一 commit**（沿用前兩次畢業同步作法，不移植 git 歷史） |
| 框架套件版本 | **4.21.0**（已查證 NuGet 上 `Bee.Api.AspNetCore` / `Bee.Business` / `Bee.Db` / `Bee.Hosting` / `Bee.UI.Avalonia` / `Bee.Definition` 皆有 4.21.0） |

### 範圍外

- bee-library 端**任何改動**（本計畫只寫入目標 repo；本檔本身除外）。
- 目標 repo 的 CI workflow —— 仍未建立，維持選用 follow-up。
- iOS / Android 的 Release / trim-safe / 上架簽章 —— 同 bee-library，Debug-first，另案。

---

## 階段 1：檔案同步

同步基準為 bee-library `apps/Bee.Northwind/` 的**版控內容**（`git ls-files`），不含 bin / obj /
`northwind.db` / 空的 `Define/Language/{en-US,zh-TW}` 目錄。

### 1a. 新增（bee-library 有、目標 repo 無）

| 檔案 | 說明 |
|------|------|
| `Bee.Northwind.Server/NorthwindSystemBusinessObject.cs` | 取代目標 repo 的 `NorthwindAuthenticatingSystemBusinessObject.cs` |
| `Bee.Northwind.Server/Repositories/IOrderRepository.cs` | ADR-034 progId 型別註冊（自訂 Repository 介面） |
| `Bee.Northwind.Server/Repositories/OrderRepository.cs` | 同上 |
| `Define/MenuSettings.xml` | 選單自 `ProgramSettings` 分離（ADR-034） |
| `Define/TableSchema/common/st_api_key.TableSchema.xml` | 以下 6 檔為框架表 schema |
| `Define/TableSchema/common/st_company.TableSchema.xml` | |
| `Define/TableSchema/common/st_define.TableSchema.xml` | |
| `Define/TableSchema/common/st_session.TableSchema.xml` | |
| `Define/TableSchema/common/st_user.TableSchema.xml` | |
| `Define/TableSchema/common/st_user_company.TableSchema.xml` | |
| `Define/TableSchema/log/st_log_access.TableSchema.xml` | 以下 5 檔為稽核 log 表 schema |
| `Define/TableSchema/log/st_log_anomaly_api.TableSchema.xml` | |
| `Define/TableSchema/log/st_log_anomaly_db.TableSchema.xml` | |
| `Define/TableSchema/log/st_log_change.TableSchema.xml` | |
| `Define/TableSchema/log/st_log_login.TableSchema.xml` | |

> ⚠️ **`log/` 會被通用 `.gitignore` 規則吃掉。** 目標 repo 的 `.gitignore` 含 `[Ll]og/`，
> 而 TableSchema 的資料夾名 = CategoryId，恰好叫 `log`。bee-library 已為此加了負向規則
> （`.gitignore:69-70`），**目標 repo 也必須補**：
>
> ```gitignore
> # Exception: framework audit-log TableSchema lives in a "log/" folder named after the
> # CategoryId, which the generic [Ll]og/ rule above would otherwise ignore.
> !Define/TableSchema/log/
> !Define/TableSchema/log/**
> ```
>
> 漏補的症狀是 `git add` 靜默不收這 5 個檔，而 build 仍會過（seeder 讀檔案系統不讀 git）。

### 1b. 刪除（目標 repo 有、已被取代）

| 檔案 | 原因 |
|------|------|
| `Bee.Northwind.Server/NorthwindAuthenticatingSystemBusinessObject.cs` | 由 `NorthwindSystemBusinessObject.cs` 取代 |
| `Bee.Northwind.Server/NorthwindBusinessObjectFactory.cs` | 已改由 ADR-034 的 progId 型別註冊機制承擔 |
| `Define/TableSchema/common/st_cache_notify.TableSchema.xml` | **執行期產物** —— 由 `NorthwindBackend` 啟動時自 `Bee.Definition` 內嵌預設落地。bee-library 已將其 gitignore（`.gitignore:458`）；目標 repo 應一併刪檔並補上對應 ignore 規則 |

### 1c. 覆蓋（兩邊皆有但內容已漂移）

以 bee-library 版本直接覆蓋：

- `Bee.Northwind.Server/`：`NorthwindBackend.cs`、`NorthwindCompanyInfoService.cs`、
  `NorthwindCredentials.cs`、`NorthwindSchemaSeeder.cs`、`BusinessObjects/{OrderBO,OrderDataSet,OrderRules}.cs`
- `Bee.Northwind.UI/`：`ViewModels/{ConnectionViewModel,FormsViewModel}.cs`、`Views/ConnectionView.axaml`
- `Bee.Northwind.Desktop/Program.cs`、`Bee.Northwind.Browser/{Program.cs,README.md,Storage/BrowserLocalStorageEndpointStorage.cs}`、
  `Bee.Northwind.Android/Application.cs`、`Bee.Northwind.iOS/AppDelegate.cs`
- `Define/`：`DatabaseSettings.xml`、`DbCategorySettings.xml`、`ProgramSettings.xml`、
  `SystemSettings.xml`、`FormSchema/Order.FormSchema.xml`

> `Bee.Northwind.slnx` 兩邊完全相同，不需改。

### 1d. 保留（目標 repo 專屬，**不得覆蓋**）

| 檔案 | 原因 |
|------|------|
| `.gitignore` | standalone 版（僅需依 1a / 1b 增補兩條規則） |
| `.vscode/{launch.json,tasks.json}` | standalone 路徑 |
| `LICENSE` | 目標 repo 專屬 |
| `.smoke.yaml` | 路徑為 standalone 版（`cwd: Bee.Northwind.Server` 而非 `apps/Bee.Northwind/...`） |
| `README.md` / `README.zh-TW.md` | 見階段 3 |
| 全部 `*.csproj` | 見階段 2 |

**完成準則**：目標 repo 工作目錄的原始碼與定義檔內容等同 bee-library 現況，1d 清單各檔未被動到。

---

## 階段 2：ProjectReference → PackageReference 4.21.0

只有 `Server` 與 `UI` 兩個專案碰 `src/`；四個 head 只內部引用 `Bee.Northwind.UI`，**不需轉換**。

### 2a. `Bee.Northwind.Server.csproj`

bee-library 版的這一段整段**不可照抄**：

```xml
<!-- bee-library 內的寫法 —— 目標 repo 不適用 -->
<ProjectReference Include="..\..\..\src\Bee.Analyzers\Bee.Analyzers.csproj" ... />
<AdditionalFiles Include="..\Define\**\*.xml" />
<ProjectReference Include="..\..\..\src\Bee.Api.AspNetCore\..." />（等 4 條）
```

改為：

```xml
<ItemGroup>
  <PackageReference Include="Bee.Api.AspNetCore" Version="4.21.0" />
  <PackageReference Include="Bee.Business" Version="4.21.0" />
  <PackageReference Include="Bee.Db" Version="4.21.0" />
  <PackageReference Include="Bee.Hosting" Version="4.21.0" />
</ItemGroup>
```

**analyzer 不需顯式引用** —— 它嵌在 `Bee.Definition` 套件的 `analyzers/dotnet/cs/` 下，
隨 PackageReference 遞移流入。

**但 `AdditionalFiles` 需要一行設定。** 套件內的 `build/Bee.Definition.targets` 會自動注入
定義檔 glob，預設值是 `Define\**\*.xml`，**以專案目錄為根**（刻意如此，避免多專案方案重複掃描）。
本 repo 的 `Define/` 在**方案根、專案的上一層**，預設 glob 掃不到 → 定義層規則（BEE1xxx /
BEE2xxx）會靜默不作用。正解是覆寫該 targets 提供的屬性：

```xml
<PropertyGroup>
  <!-- Definitions live at the solution root, one level above this project; the package's
       default glob is rooted at the project directory and would find nothing. -->
  <BeeDefinitionFilesGlob>..\Define\**\*.xml</BeeDefinitionFilesGlob>
</PropertyGroup>
```

> 這一條是**本次新增**：上次同步（4.12.1）時目標 repo 的 Server csproj 根本沒有 analyzer /
> AdditionalFiles 段落，等於一直沒跑到定義層規則。

`Microsoft.Data.Sqlite` / `SQLitePCLRaw.bundle_e_sqlite3` 兩條維持不變（版本兩邊已一致）。

### 2b. `Bee.Northwind.UI.csproj`

```xml
<!-- 移除 -->
<ProjectReference Include="..\..\..\src\Bee.UI.Avalonia\Bee.UI.Avalonia.csproj" />
<!-- 改為（放回原本的 PackageReference ItemGroup 內） -->
<PackageReference Include="Bee.UI.Avalonia" Version="4.21.0" />
```

Avalonia / Semi / CommunityToolkit 五條套件版本兩邊已一致，不動
（依 `rules/avalonia.md`：**不無腦升核心**，Semi 停在 12.0.3 就整組停在該版本線）。

### 2c. 四個 head 的 csproj

只需 port bee-library 端新增的內容：

1. **`Browser` / `iOS` / `Android` 補時區屬性**（Desktop 不需要，bee-library 端也沒有）：

   ```xml
   <PropertyGroup>
     <InvariantGlobalization>false</InvariantGlobalization>
     <InvariantTimezone>false</InvariantTimezone>
   </PropertyGroup>
   ```

   連同 bee-library 那段解釋 WHY 的註解一起帶過去（ADR-032 D4：框架每個時刻都經
   `TimeZoneInfo` 轉換，缺 tz data 會在第一次轉換時擲例外，桌面 build 與測試都攔不到）。

2. **`iOS` / `Android` 的註解修正 bee-library 內部路徑**：兩邊目前都寫
   `see .claude/rules/maui.md Apple trim decision tree`，該檔在 bee-library 已更名為
   `apple-mobile-trim.md`，而**目標 repo 根本沒有 `.claude/rules/`**。改寫為不依賴 bee-library
   內部路徑的自足說明，例如：

   ```
   Release / trim-safe packaging is a separate follow-up.
   ```

**完成準則**：全 repo grep 無 `..\..\..\src\` 殘留；`Bee.*` PackageReference 全為 `4.21.0`。

```bash
grep -rn 'src\\\|src/Bee\|Version="4\.1[0-9]' --include="*.csproj" .
```

---

## 階段 3：README 雙語逐段 port

**不可整份覆蓋。** 目標 repo 的 README 有 standalone 專屬內容：

- 開頭與結尾說明「純以 NuGet 套件引用」「於 bee-library 內開發、此處為獨立副本」
- 四個 head 的執行畫面（外連 `raw.githubusercontent.com/jeff377/blog-images`，commit `fa762ae`）
- 相對路徑以 repo 根為基準（bee-library 版以 `apps/Bee.Northwind/` 為基準）

作法：diff 兩邊 README，**只把 bee-library 端新增的實質內容（新表單、新機制、執行方式變更）
port 過去**，standalone 專屬段落原樣保留。兩份 README 都要改（`rules/public-docs.md` 雙語同步）。

同時檢查：

- 是否出現 `docs/plans/` 連結 —— 公開文件不得引用 plan（`rules/public-docs.md`）。
  目標 repo 已於 `fa762ae` 清過一次，本次覆蓋後需複檢。
- 是否有寫死的框架版號 —— 目前兩份 README 皆無（已查證），**不要新增**
  （`single-source.md`：版號的權威來源是 csproj）。

**完成準則**：雙語 README 內容與現況一致、standalone 專屬段落完整、無 plan 連結、無寫死版號。

---

## 階段 4：獨立 repo build + 冒煙驗證

於 `~/Desktop/repos/bee-northwind-avalonia`（純 NuGet，不依賴 bee-library）：

```bash
dotnet restore
dotnet build Bee.Northwind.Server/Bee.Northwind.Server.csproj -c Debug
dotnet build Bee.Northwind.UI/Bee.Northwind.UI.csproj -c Debug
dotnet build Bee.Northwind.Desktop/Bee.Northwind.Desktop.csproj -c Debug
dotnet build Bee.Northwind.Browser/Bee.Northwind.Browser.csproj -c Debug   # 需 wasm-tools
dotnet build Bee.Northwind.Android/Bee.Northwind.Android.csproj -c Debug   # 需 Android SDK + JDK
dotnet build Bee.Northwind.iOS/Bee.Northwind.iOS.csproj -c Debug           # 需 ios workload + Xcode
```

- Server / UI / Desktop / Browser 設 `TreatWarningsAsErrors=true` → **任何警告即失敗**。
  這是 4.21.0 相對 4.12.1 的 API 漂移最可能顯現的地方（`[Obsolete]` 標註會直接變 error）。
- iOS / Android 刻意未設該旗標（trim 分析的 IL2026 / IL2104 為預期）。
- **iOS 若卡本機 Xcode workload 不匹配，比照上次處理**：先確認 bee-library 端的 iOS head
  是否同樣失敗；同樣失敗即為環境問題、非同步 regression，記錄後不阻擋。

冒煙（最低）：Server 起得來 + 建表；Desktop 連線 → 登入 → 清單真實資料 → 開一筆訂單。
可用 `/demo-smoke`（目標 repo 自帶 standalone 版 `.smoke.yaml`）。

**完成準則**：Server + 四 head build 通過（iOS 得以環境問題豁免）；Desktop 端到端冒煙通過。

---

## 階段 5：commit + push

1. 在目標 repo 單一 commit，訊息：`feat: 同步至框架 4.21.0`，繁中 body 摘要
   （progId 型別註冊 / MenuSettings 分離 / 框架表與 log schema 補齊 / 時區屬性 / analyzer glob）。
2. push 到 `bee-northwind-avalonia` 的 `main`。
3. 確認 GitHub 上內容正確（特別是 `Define/TableSchema/log/` 5 檔真的有進版控）。

**完成準則**：`bee-northwind-avalonia` main 為框架 4.21.0 的完整四 head，與 bee-library
`apps/Bee.Northwind` 內容一致（差異僅 ProjectReference vs PackageReference、
與階段 1d 的 standalone 專屬檔）。

## 完成後

- 本檔頂部標記 ✅ 與日期，階段表格逐列更新。
- bee-library `apps/Bee.Northwind` **保留不動**，本 repo 零改動（本計畫文件除外）。
- 由使用者要求時再移至 `archive/`。
