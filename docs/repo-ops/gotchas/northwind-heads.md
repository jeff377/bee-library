# 踩雷誌：Northwind 四 head 與獨立 repo 同步

`apps/Bee.Northwind` 自 2026-06-26 達成 **Desktop / Browser / iOS / Android 四 head**，共用
`Bee.Northwind.UI`（Avalonia App/VM/View）、共用後端 `Bee.Northwind.Server`。
Web 案例是 **Avalonia Browser (WASM) backend**，**不是另寫 Blazor**（`.UseBrowser` vs `.UseDesktop` 對稱）。

`apps/` 不觸發 CI（見 [test-ci-release.md](test-ci-release.md)）——所以這裡壞掉不會有人通知你。

## Android head

**工具鏈**（這幾條每次換機器都要重踩）：

- `net10.0-android` 編譯 API = **36**，SDK 需裝 `platforms;android-36`（+ `build-tools;36.0.0`）。
  只有 .NET 的 `Microsoft.Android.Ref.36` ref pack **不夠**——缺 platforms 會 XA5207「找不到 android.jar」。
- `maui-android` workload 已含 `Microsoft.Android.Sdk.Darwin` → **不需** `dotnet workload install android`。
- 本機路徑：JDK `brew openjdk@17`、`ANDROID_HOME=/opt/homebrew/share/android-commandlinetools`、
  AVD `bee_pixel`。**非互動 Bash 不讀 `~/.zshrc`** → 跑 dotnet/adb 前要自帶 `JAVA_HOME` / `ANDROID_HOME`。
- emulator → 主機 loopback 是 **`10.0.2.2`**（不是 localhost）；endpoint 填 `http://10.0.2.2:5100/api`；
  AndroidManifest 需 `<application android:usesCleartextTraffic="true">`（dev only，Android 9+ 預設擋明文）。

**head 結構**：`Application.cs` 的 `[Application] class Application : AvaloniaAndroidApplication<App>`，
其 `CustomizeAppBuilder` 是**唯一**建 AppBuilder + SetupWithLifetime 的權威 hook → client 接線
（`ApiClientInfo` / `ClientInfo.EndpointStorage`）放這（對應 iOS 的 AppDelegate）。
`MainActivity : AvaloniaMainActivity`（**非泛型**）只 host view，從 Application 取 lifetime。

`FileEndpointStorage` 在 Android 沙箱可寫（`/data/data/<pkg>/files/...`），但 ConnectionView 欄位
永遠預填 `AppDefaults.Endpoint`、不回讀 storage——共用 UI 的既有行為，三 head 一致。

## iOS head

**.NET for iOS 綁死 Xcode 版本，macOS 更新 Xcode 就會打斷建置。**

```
error : This version of .NET for iOS (26.5.10284) requires Xcode 26.5.
The current version of Xcode is 26.6. Either install Xcode 26.5, or use a
different version of .NET for iOS.
```

正解是**側裝對應版本的 Xcode 並以 `DEVELOPER_DIR` 指定**，不要動 `xcode-select` ——
後者是全機設定、需 sudo，且會連帶影響其他需要新版 Xcode 的工作。本機已有
`/Applications/Xcode-26.5.0.app` 與 `/Applications/Xcode.app`（26.6）並存：

```bash
export DEVELOPER_DIR=/Applications/Xcode-26.5.0.app/Contents/Developer
```

錯誤訊息只說「裝 26.5 或換 workload」，沒提 `DEVELOPER_DIR` 這條路，所以很容易被判成
「環境壞了、只能擱置」——2026-08-13 的 4.21.0 同步就是這樣把 iOS 記成環境問題豁免掉的，
實際上兩台 Xcode 早就都在機器上。

**乾淨樹上 `-t:Run` 必須分兩段跑。**

```
error : The app must be built before the arguments to launch the app using
mlaunch can be computed.
```

症狀看起來像 mlaunch 參數或模擬器沒選對，實際是同一次 MSBuild 呼叫內 Run target
取啟動參數時 app bundle 還沒產生。先 build 再 Run：

```bash
export DEVELOPER_DIR=/Applications/Xcode-26.5.0.app/Contents/Developer
dotnet build Bee.Northwind.iOS -f net10.0-ios -c Debug
dotnet build Bee.Northwind.iOS -t:Run -f net10.0-ios -c Debug \
  -p:_DeviceName=:v2:udid=<模擬器 UDID>
```

模擬器 UDID 取自 `xcrun simctl list devices available`；省略 `_DeviceName` 時由 SDK 自選，
開著多台時未必是你要的那台。**iOS 模擬器的 endpoint 用 `http://localhost:5100/api`**
（與 Android 的 `10.0.2.2` 不同，ATS 於 dev 允許任意連線）。

兩條在 bee-library 內的 `apps/Bee.Northwind/Bee.Northwind.iOS` 同樣適用。

## Browser (WASM) head

**csproj 必加**：

```xml
<JsonSerializerIsReflectionEnabledByDefault>true</JsonSerializerIsReflectionEnabledByDefault>
```

browser-wasm 預設停用 STJ 反射，Bee `JsonCodec`（訊息非 source-gen）在 `request.ToJson()` 就拋
`JsonSerializerIsReflectionDisabled` —— **請求根本沒送出**，UI 只顯示
"Connection failed during Ping"（外層包住真因）。**與 trimming 無關，Debug 也停用。**

**Release 發佈用 `PublishTrimmed=false`**（~16M gzip）：trimming 面比預期廣，不只 FormSchema
XmlSerializer，`JsonCodec` / MessagePack / `TypeDescriptor` / `Assembly.GetType` / DataGrid 全報
IL2026（`TreatWarningsAsErrors` 直接失敗）；`TrimmerRootAssembly` 不消警告。全 trim-safe 需
source-gen 化，屬框架級議題。

**對話框**：`OverlayDialogHost`（internal），LookupDialog / RowEditDialog 以
`OperatingSystem.IsBrowser()` 分支（browser → overlay、desktop → Window）。

**連線一律 async**：`SyncExecutor.Run`（`Task.Run(...).GetAwaiter().GetResult()`）在 browser-wasm
單執行緒 runtime 會拋 **"Cannot wait on monitors on this runtime"** —— 阻塞唯一執行緒等 task，
task 完成又需同一執行緒 pump event loop → deadlock。桌面/WinForms 容忍，WASM 不容忍。
**任何 WASM head 的 client 連線一律 `await ClientInfo.InitializeAsync(endpoint)`**，禁用 sync
`Initialize` 或 `Task.Run(() => sync())` 包裝。底層 HTTP 已是 `HttpClient`（WASM 走
`BrowserHttpHandler`/fetch），async 全程安全。同理載定義要用 `connector.GetDefineAsync`，
不要走 sync `IDefineAccess`（`RemoteDefineAccess` 靠 SyncExecutor）。

**環境雷**：建 WASM 需 `sudo dotnet workload install wasm-tools`。本機跑用 Claude preview
（`.claude/launch.json` 需 `autoPort:false`）；**preview headless 注入的合成 pointer 事件進不了
Avalonia 輸入層**（canvas 上 `div.avalonia-native-host` 攔截），UI 點擊驗證要靠真實瀏覽器。

## 返回鍵（共用 UI）

`MainView` 接 `TopLevel.BackRequested` 做層級處理——記錄→回清單（`FormWorkspace.TryGoBack()`）、
清單→關分頁（`FormsView.TryHandleBack()`）、無分頁→退出 app。iOS 預測式返回與瀏覽器返回鍵同步受惠。

## 畢業與週期性同步

**畢業是「複製」不是「搬走」**（2026-06-15 使用者指示）：建獨立 repo `bee-northwind-avalonia` 時把
`apps/Bee.Northwind` 複製過去（ProjectReference → PackageReference），**但 bee-library 內的
`apps/Bee.Northwind` 保留、先不 `git rm`**。

**Why**：`Bee.UI.Avalonia` 仍在補齊控件/架構，留著走 ProjectReference 的 in-repo demo 才能在改
Avalonia 時即時 dogfooding。獨立 repo 那份是「外部視角、純 NuGet」的快照證明，兩者並存。
**`git rm` 延後到 `Bee.UI.Avalonia` 全部完成才執行。**

**同步流程**（已跑三輪）：**先發新框架版本**（in-repo 新功能依賴的 src 變更必須先上 NuGet）→
複製變更檔覆蓋 → 重套 ProjectReference→PackageReference 並 bump → 文件/launch 同步 →
本機 build + HTTP 冒煙（該 repo 無 CI）→ 直接 push main。

**三個踩過的雷**：

1. **rsync 要 `--exclude '*.csproj' --exclude 'README*.md'`**，只同步 source。csproj 個別處理、
   README 手動 port，否則會把 standalone 特製檔（root 相對路徑、NuGet 框架敘述）覆蓋掉。
   `.smoke.yaml` 同理（standalone 用 `Bee.Northwind.Server`，bee-library 用 `apps/Bee.Northwind/...`）。
2. **覆蓋複製會把 in-repo 的 src ProjectReference 帶回獨立 repo** —— 必須改回 PackageReference + bump。
   這是最容易漏的一步。
3. **`gh secret set` 語法雷**：`gh secret set <KEY值>` 會把 key 值當 secret **名稱**建出來
   （且在 UI 上洩漏 key）；正確是 `gh secret set NUGET_API_KEY --body "<值>"`，
   事後 `gh secret list` 看 Updated 時間戳確認。**發佈前先確認該 secret 是新的有效 key。**
