# 計畫：Bee.Northwind 新增 Web 案例（Avalonia Browser / WASM head）

**狀態：🚧 進行中（2026-06-23）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 新增 `Bee.Northwind.Browser` head 專案，重用 `Bee.Northwind.UI`，瀏覽器可開出畫面 | ✅ 已完成（2026-06-23） |
| 2 | `BrowserLocalStorageEndpointStorage`（取代 FileEndpointStorage），連線設定可持久化 | 🚧 已接線（持久化 round-trip 待連線可用後驗） |
| 2b | client 連線路徑 async 化（WASM 不可 sync-over-async；spike 發現） | ✅ 已完成（2026-06-23，live 驗證進 Login） |
| 2c | WASM 啟用 System.Text.Json 反射序列化（spike 發現） | ✅ 已完成（2026-06-23） |
| 2d | define 載入避開 sync `IDefineAccess`（FormsViewModel 改 async；spike 發現） | ✅ 已完成（2026-06-23，連線→登入→選單全通） |
| 3 | `Bee.Northwind.Server` dev CORS（跨源 5200→5100）；同源 host 留作 production | ✅ 已完成（2026-06-23，連線跑通進 Login） |
| 4 | popup Window 對話框（`LookupDialog` / `RowEditDialog`）改 overlay 疊層，WASM 可用 | 📝 待做 |
| 5 | Trimming / FormSchema 反射序列化保留設定，Release 發佈可跑 | 📝 待做 |
| 6 | README + 跑法文件，端到端冒煙 | 📝 待做 |

## 階段 1 spike 結果（2026-06-23）

- **Avalonia 12.0.4 Browser (WASM) backend 確認可渲染** — `dotnet build` 0 警告 0 錯誤（emscripten native compile + JSImport source generator + wasm-ld 連結全過）；瀏覽器載入後 `canvas` 建立、splash 自動 `splash-close`、深色 Connection 畫面完整渲染（標題 / 說明 / 端點輸入框預填 / Connect 按鈕）。`MainView` 抽取 + `ISingleViewApplicationLifetime` 接線正確。
- **環境前置**：`wasm-tools` workload 必裝（`sudo dotnet workload install wasm-tools`）。
- **boilerplate 來源**：`avalonia.xplat` 範本 scaffold 取得 .NET 10/Avalonia 12 確切 WASM 樣板（`Microsoft.NET.Sdk.WebAssembly`、`StartBrowserAppAsync("out")`、`main.js` import `./_framework/dotnet.js`）。
- **本機跑法雷**：Claude preview 面板自管 `dotnet run` 首次卡 Claude.app `disclaimer` GUID 授權；核准後正常。WasmAppHost 埠寫死在 `launchSettings.json`，`.claude/launch.json` 需 `autoPort: false`。
- **🔴 新發現（階段 2b 來源）**：點 Connect 出現 `Cannot wait on monitors on this runtime` —— WASM 單執行緒不允許 sync-over-async。連線路徑兩處 `SyncExecutor.Run(...GetAwaiter().GetResult())` 阻塞（`ApiConnectValidator.Validate` 內的 `PingAsync`、`ClientInfo.Initialize` 內的 `SystemApiConnector.InitializeAsync`）。

## 階段 2b：client 連線路徑 async 化

WASM 單執行緒 runtime 不允許 `Task.Run(asyncFunc).GetAwaiter().GetResult()`（`SyncExecutor` 的實作）阻塞 UI 執行緒。連線/ping 必須 async-all-the-way。

底層 async 方法皆已存在（`SystemApiConnector.PingAsync` / `InitializeAsync`），故為**純新增的 async sibling**，不動既有 sync API（桌面 / WinForms 維持 sync）：

- `ApiConnectValidator.ValidateAsync(endpoint, allowGenerate)` — `await connector.PingAsync()`，回傳同 `Validate` 的 `ConnectType`。
- `ClientInfo.InitializeAsync(string endpoint)` — `await ValidateAsync(...)` + `await SystemApiConnector.InitializeAsync()`，其餘（EndpointStorage 寫入等）比照 sync 版。
- `ConnectionViewModel.ConnectAsync` — 改 `await ClientInfo.InitializeAsync(endpoint)`，移除 `Task.Run(() => ClientInfo.Initialize(endpoint))`。

跨層涉及 `Bee.Api.Client`（ValidateAsync）+ `Bee.UI.Core`（InitializeAsync）+ `Bee.Northwind.UI`（VM）。同步補對應單元測試。桌面端回歸：sync `Initialize` 維持不變。

**驗收**：WASM 下 Connect 不再拋 monitors 錯；Server 未起時回正常的連線失敗訊息（網路層），Server 起後可進 Login。

## 背景

`apps/Bee.Northwind` 目前是三層分離架構：

```
Bee.Northwind.Server   ← ASP.NET Core JSON-RPC 後端（:5100/api），定義檔 + BO + DB
        ▲ JSON-RPC over HTTP（Remote）
Bee.Northwind.Desktop  ← Avalonia.Desktop head，SupportedConnectTypes.Remote
Bee.Northwind.UI       ← 共用 Avalonia App / ViewModel / View（Library）
```

Desktop 只是一個 **Remote 瘦客戶端**，後端 `Server` 獨立存在、定義檔由 Server 載入、前端連線後用 `ClientInfo.DefineAccess` 動態取得。

**Web 案例的本質 = 再加一個前端 head，後端幾乎不動。** 因 Avalonia 有 Browser (WASM) backend，可把既有 `Bee.Northwind.UI` 編進瀏覽器跑，重用同一套 ViewModel / View / App，最大化與 Desktop 案例的對稱性。

目標結構：

```
Bee.Northwind.UI       ← 共用 Avalonia（幾乎不動）
Bee.Northwind.Desktop  ← Avalonia.Desktop head（既有）
Bee.Northwind.Browser  ← Avalonia.Browser head（新增）← 把 UI 編成 WASM
Bee.Northwind.Server   ← 後端不動（僅加靜態檔 host）
```

## 探查確認的既有條件（可重用，降低風險）

| 接線點 | 現況 | 結論 |
|--------|------|------|
| **App 類別** | 在 `Bee.Northwind.UI/App.axaml.cs`，`AppBuilder` 組裝在 Desktop head 的 `BuildAvaloniaApp()` | ✅ Browser head 照抄 builder、換 `.UseBrowser()` 即可重用同一 `App` |
| **生命週期** | `App.OnFrameworkInitializationCompleted()` 只檢查 `IClassicDesktopStyleApplicationLifetime`，無其他平台分支 | ✅ Browser 走 `ISingleViewApplicationLifetime`，需補一段 single-view 分支 |
| **HttpClient** | `src/Bee.Base/HttpUtilities.cs` 已有 `OperatingSystem.IsBrowser()` 分支，改用 `BrowserHttpHandler`（fetch），無自訂 handler / proxy / 憑證 | ✅ 框架已為 WASM 設計，零修改 |
| **Remote connector** | `RemoteApiProvider` 經 `HttpUtilities.PostAsync` 發 JSON-RPC | ✅ 直接可用 |

需要新處理的點：EndpointStorage（不能寫檔）、popup Window 對話框、Server 靜態檔 host、trimming。

### Bee.UI.Avalonia 控件 WASM 相容性（已逐檔掃描）

`Bee.Northwind.UI` 的畫面用到 `src/Bee.UI.Avalonia` 的控件家族。掃描結論：

| 控件 | 基底 | WASM |
|------|------|------|
| `FormView` / `ListView` / `RowEditPanel` / `LookupPanel` | UserControl | ✅ 直接可用 |
| `TextEdit` / `MemoEdit` / `DateEdit` / `YearMonthEdit` / `CheckEdit` / `DropDownEdit` | TextBox/DatePicker/CheckBox/ComboBox 子類 | ✅ 直接可用 |
| `GridControl` | ContentControl + `DataGrid` | ⚠️ 可用，需冒煙驗（複雜 template / in-cell 編輯）；優先 `GridEditMode.InCell` |
| `ButtonEdit` | TextEdit | ⚠️ 本體可用，但其 lookup 流程依賴 `LookupDialog`（見下） |
| `LookupDialog` / `RowEditDialog` | `new Window` + `ShowDialog()` | ❌ **不相容** — 瀏覽器無多視窗 → 階段 4 改 overlay |

關鍵：對話框的**內容**已抽成 `LookupPanel` / `RowEditPanel`（UserControl，相容），不相容的只有「用 Window 當外殼」。階段 4 只需換外殼，內容面板原封重用。無 P/Invoke / System.Drawing / Process / 剪貼簿 / 多執行緒同步原語。

## 階段 1：Browser head 專案

新增 `apps/Bee.Northwind/Bee.Northwind.Browser/`：

- **`Bee.Northwind.Browser.csproj`**
  - TFM：`net10.0-browser`
  - `<RuntimeIdentifier>browser-wasm</RuntimeIdentifier>`
  - `PackageReference`：`Avalonia.Browser`（對齊 repo 內 Avalonia 版號 `12.0.4`）
  - `ProjectReference`：`Bee.Northwind.UI`（取得 App / ViewModel / View）
  - 沿用 repo 既有 `Nullable` / `TreatWarningsAsErrors` 等 `Directory.Build.props` 預設

- **`Program.cs`** — 照抄 Desktop 的 `BuildAvaloniaApp()`，差異：
  ```csharp
  internal sealed partial class Program
  {
      private static Task Main(string[] args) => BuildAvaloniaApp()
          .WithInterFont()
          .StartBrowserAppAsync("out");   // out = wwwroot 內掛載點 id

      public static AppBuilder BuildAvaloniaApp()
          => AppBuilder.Configure<App>();
  }
  ```
  - `ApiClientInfo` / `EndpointStorage` 的設定移到此處（與 Desktop head 對稱）：
    `ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Remote;`
    `ClientInfo.EndpointStorage = new BrowserLocalStorageEndpointStorage("Bee.Northwind");`（階段 2）

- **`wwwroot/index.html` + `main.js`** — Avalonia.Browser 官方 bootstrap 樣板（掛載點 id `out`），參考 `samples/Blazor.Wasm.Demo/wwwroot/index.html` 的結構但改 Avalonia loader。

- **`App.axaml.cs` 補 single-view 分支**（在 `Bee.Northwind.UI`）：
  ```csharp
  if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
      singleView.MainView = new MainView { DataContext = ... };
  ```
  - 若既有畫面是 `MainWindow`（Window），需抽出 `MainView`（UserControl）讓 desktop window 與 browser single-view 共用內容。**這是階段 1 最可能要重構的點**，先讀 `MainWindow.axaml` 評估抽 View 成本。

**驗收**：`dotnet build` 過；本機 `dotnet run`（或 serve wwwroot）能在瀏覽器開出 Connection 畫面。

## 階段 2：BrowserLocalStorageEndpointStorage

`FileEndpointStorage`（`src/Bee.UI.Avalonia/Storage/`）用 `Environment.SpecialFolder.LocalApplicationData` + `File.*`，瀏覽器沙箱無法寫檔。

新增 `BrowserLocalStorageEndpointStorage : IEndpointStorage`，行為對齊既有 `MauiPreferenceEndpointStorage`（key-value 持久化），底層走瀏覽器 `localStorage`：

- 放置位置決策：
  - **建議**放 `src/Bee.UI.Avalonia/Storage/`（與 `FileEndpointStorage` 同 namespace），但需 `OperatingSystem.IsBrowser()` 守門 + JS interop。若 `Bee.UI.Avalonia` 不含 browser TFM 會編不過 → 改放 Browser head 專案內（`apps/.../Bee.Northwind.Browser/Storage/`），與 MAUI 把 storage 放 `Bee.UI.Maui` 的分層慣例一致。**預設放 head 專案**，待之後 `Bee.UI.Avalonia.Browser` 共用套件出現再上移。
- 介面（已確認）：`LoadEndpoint()` / `SetEndpoint(string)` / `SaveEndpoint(string)`。
- localStorage 存取用 `[JSImport]`（`System.Runtime.InteropServices.JavaScript`）包一層 `globalThis.localStorage.getItem/setItem`。

**驗收**：在瀏覽器輸入端點 → 重整頁面 → 端點仍在（從 localStorage 回填）。

## 階段 2c：WASM 啟用 System.Text.Json 反射序列化

**spike 真因**：點 Connect 後 ping 失敗，完整例外鏈為 `JsonSerializerIsReflectionDisabled` —— browser-wasm **預設停用 System.Text.Json 反射式 (de)serialization**。Bee 的 `JsonCodec` 走反射（訊息型別非 source-generated），在 `request.ToJson()` 序列化 JSON-RPC 請求時就拋，**請求根本沒送出**（故 server log 無紀錄；headless 手刻 body 重放才會成功 —— 那繞過了 client 的 C# 序列化）。

**修正**：`Bee.Northwind.Browser.csproj` 加 `<JsonSerializerIsReflectionEnabledByDefault>true</JsonSerializerIsReflectionEnabledByDefault>`，重新開啟反射路徑。

與 2b 同屬「WASM 連線必踩」：2b 是 sync-over-async、2c 是 STJ 反射停用。兩者修好後，WASM Connect → ping → `InitializeAsync`（含 `XmlCodec.Deserialize<CommonConfiguration>`，Debug 下 XmlSerializer 反射可用）→ 進 Login 全通（live 截圖確認）。

> XmlSerializer（FormSchema / CommonConfiguration）的反射在 **Debug 可用**，Release/publish trimming 才需保留（見階段 5）。STJ 這個開關與 trimming 無關 —— Debug 也停用，故必加。

## 階段 2d：define 載入避開 sync IDefineAccess

**spike 真因**：登入 API **成功**後，`FormsViewModel` 同步呼叫 `ClientInfo.DefineAccess.GetProgramSettings()` 載入選單。`RemoteDefineAccess` 用 `SyncExecutor.Run` 橋接 async connector（`SyncExecutor` 註解本就寫「橋接同步介面如 IDefineAccess」），在 WASM 又踩 `Cannot wait on monitors`（與 2b 同根，不同路徑）。錯誤被 `LoginViewModel` 的 catch 包成 "Login failed"（誤導 —— login 其實成功，崩在後續選單載入）。

**窄修（本計畫採用）**：`FormsViewModel` 改用 `await ClientInfo.SystemApiConnector.GetDefineAsync<ProgramSettings>(DefineType.ProgramSettings)`（與 `FormView` / Blazor `FormPage` 既有 async 慣例一致），建構子 fire-and-forget 載入、失敗顯示為一條 header。**Avalonia 開表單路徑已 async 安全**：`FormView` 走 `await GetDefineAsync<FormSchema>`，`Schema.GetFormLayout()` 是 FormSchema 本地方法（非 RemoteDefineAccess，不碰 SyncExecutor）。

**驗收**：connect → login(demo/demo) → Forms shell 選單（Master Data / Organization / Transactions 共 8 個 ProgId）完整載入（live 截圖確認）。

### 未來：框架級 async IDefineAccess（獨立工程，非本計畫）

`IDefineAccess` 整個同步介面（21 個 Get/Save）在 remote 模式靠 `SyncExecutor`，WASM 本質不可行。長期正解是加 async 全套（IDefineAccess + RemoteDefineAccess + LocalDefineAccess + BO ripple，~11 檔，中等規模）。惠及所有 WASM 前端，但**非 Northwind Web 跑通的必要條件**（窄修已足）。已另開 [plan-define-access-async.md](plan-define-access-async.md)。

## 階段 3：跨源連通（dev CORS 為主，同源 host 留 production）

實作決策：**dev 用 CORS、production 用同源**。原因 —— Avalonia WASM 的自然 dev server 是 `WasmAppHost`（`dotnet run` 起在另一個 port），讓 ASP.NET Server 同源 host 需要 publish WASM 靜態檔，dev 迭代成本高。故 dev 走兩 server + CORS，迭代快；同源 host 文件化為 production 路徑（之後 publish 時做）。

**已實作（dev CORS）** — `apps/Bee.Northwind/Bee.Northwind.Server/Program.cs`：
- `AddCors` 一個 `BeeDevWasm` policy，`SetIsOriginAllowed` 放行 `localhost` / `127.0.0.1` 任意 port（WASM dev server port 可變）。
- `app.UseCors(...)` 以 `app.Environment.IsDevelopment()` 守門，且**必須在 `UseNorthwindBackend` 之前**（讓 CORS preflight OPTIONS 先被回應，不被 API access-control middleware 擋）。

**headless 驗證（2026-06-23）** — 從 WASM origin `http://localhost:5200` 對 `http://localhost:5100/api` 跨源 fetch：POST（JSON-RPC）`corsAllowed: true`、HEAD（reachability 探針）resolved（405，`IsEndpointReachableAsync` 視任何回應為 reachable）。兩道連線關卡跨源皆通 —— 正是 WASM client 連線實際走的 fetch 路徑。

**production 同源 host（未做，留作 publish 階段）**：Server `UseBlazorFrameworkFiles` / `UseStaticFiles` + `MapFallbackToFile("index.html")`，WASM publish 輸出落 `Server/wwwroot/`，同源後不需 CORS。Avalonia WASM（`Microsoft.NET.Sdk.WebAssembly`）能否直接套 `UseBlazorFrameworkFiles` 需驗，留 ADR。

**驗收**：dev 下同時跑 `Northwind.Server`(5100) + Browser dev server(5200)，WASM 點 Connect 跨源連上、進 Login（demo/demo）。跨源 fetch 連通已 headless 驗證；UI 點擊 → Login 待真實瀏覽器確認。

## 階段 4：popup 對話框改 overlay

`src/Bee.UI.Avalonia` 的 `LookupDialog` / `RowEditDialog` 以 `new Window` + `ShowDialog(owner)` 實作，WASM 單視圖環境不支援。

**分支位置**：`ButtonEdit` 不改 —— 它只呼叫 `LookupDialog.ShowAsync(this, progId)`，不碰 Window。平台分支寫在兩個 `static` presenter（`LookupDialog` / `RowEditDialog`）內，一處改、lookup 與明細編輯兩呼叫端同時受惠。

**平台判斷**：用 runtime `OperatingSystem.IsBrowser()`（**不**用 `#if`），與框架 `HttpUtilities` 既有慣例一致，`Bee.UI.Avalonia` 維持單一 `net10.0` TFM。

**關鍵雷**：Avalonia 在 browser 連 `new Window` 都會丟例外，分支必須在「建立 Window 之前」。現有 code 先 `new Window` 再判 owner、`else` 走 `window.Show()` —— 該 fallback 在瀏覽器會炸，必須重構成「先建 panel，再依平台決定外殼」。

做法：

- `LookupDialog` 內容已是 `LookupPanel`、`RowEditDialog` 內容已是 `RowEditPanel`（皆 UserControl）→ **內容原封重用，只換外殼**。
- 新增共用 `OverlayDialogHost.ShowAsync(host, panel, title, completion)`：用 `OverlayLayer.GetOverlayLayer(host)` 取單視圖疊層，放半透明遮罩 + 置中 Border 容納 panel，`Committed`/`Cancelled` 完成 `TaskCompletionSource` 後自移除。兩 presenter 的 browser 分支共用它，避免各刻一份 modal。
- presenter 結構：`OperatingSystem.IsBrowser()` → 走 overlay；否則走既有 `Window.ShowDialog`（桌面行為完全不變）。
- 取捨：現階段採「行內 `OperatingSystem.IsBrowser()` 分支 + 共用 overlay helper」（最少接線、與 HttpUtilities 一致）；對話框種類變多再升級成注入式 `IDialogPresenter`。連動 memory ADR-021。

**驗收**：WASM 下點 lookup 欄位 / 開明細編輯，疊層 modal 正常顯示、可選取/存檔/取消；桌面端 `Window.ShowDialog` 行為無回歸。

## 階段 5：Trimming / 序列化保留

`FormSchema` / `TableSchema` 走 `XmlCodec` → `XmlSerializerCache.Get(type)`（反射式 `XmlSerializer`，非 source-generated）。WASM Release publish 會 trim 掉反射 metadata，重演 `maui.md` 記錄的 `XmlSerializeErrorDetails, 2, 2`（看似 XML 壞，實為 type metadata 被砍）。

對策（依投入由低到高，先採低成本）：

1. **`Bee.Northwind.Browser.csproj` 保留組件**（首選，成本最低）：
   ```xml
   <ItemGroup>
     <TrimmerRootAssembly Include="Bee.Definition" />
     <TrimmerRootAssembly Include="Bee.Base" />
   </ItemGroup>
   ```
   保留承載 FormSchema / 序列化型別的組件 metadata。
2. Debug 開發期 trimming 較寬鬆，先確保 Debug 跑通，Release 再驗 trimming。
3. 若 `TrimmerRootAssembly` 不足，退而對 `FormSchema` 子型別補 `[DynamicallyAccessedMembers]`（成本高、影響廣，留 ADR）。

**驗收**：Release publish 後瀏覽器仍能正確反序列化 FormSchema、渲染表單（非 `2,2` 錯誤）。

## 階段 6：文件與冒煙

- `apps/Bee.Northwind/Bee.Northwind.Browser/README.md`：跑法（先起 Server、再 serve / 同源開啟）、Debug vs Release 注意事項、trimming 雷記錄。
- 更新 `apps/Bee.Northwind/README.md`（若有）列出三個 head（Desktop / Browser / Server）。
- 端到端冒煙：Connection → Login(demo/demo) → 開一張表單 → CRUD 一筆。

## 風險與待決

| 風險 | 影響 | 緩解 |
|------|------|------|
| `MainWindow`（Window）無法直接給 browser single-view 用 | 階段 1 卡關 | 抽 `MainView` UserControl 共用，desktop 包成 window |
| Avalonia 12.0.4 的 Browser backend 成熟度 / DataGrid 在 WASM 行為 | 表單渲染可能有差異 | 階段 1 先驗最小畫面，DataGrid 留待冒煙確認 |
| `apps/` 不在 `build-ci.yml` 觸發路徑（見 memory ci-path-filter） | Browser head 不會被 CI 自動 build | 與既有 Northwind 一致，靠本機驗；如需 CI 另議 |
| Avalonia.Browser 版號需與 repo Avalonia 對齊 | 版號不符編不過 | 鎖 `12.0.4`，由 `Directory.Packages.props`（若有 CPM）統一 |

## 不在本計畫範圍

- 不改 `Bee.Northwind.Server` 的 BO / 定義檔 / DB（後端零業務修改，只加靜態檔 host）。
- 不做 PWA / offline / service worker。
- 不抽通用 `Bee.UI.Avalonia.Browser` 共用套件（待 Avalonia 試點定稿後另案，見 memory avalonia-pilot-ui-architecture）。
