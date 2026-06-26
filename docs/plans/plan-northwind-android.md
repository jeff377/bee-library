# 計畫：Bee.Northwind 新增 Android head（Avalonia）

**狀態：📝 擬定中（2026-06-26）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | Android 工具鏈就緒（JDK + Android SDK + AVD + `android` workload） | 📝 待做 |
| 2 | Scaffold `Bee.Northwind.Android` head（bootstrap + client 接線 + slnx） | 📝 待做 |
| 3 | 模擬器 Debug 跑通 + 端到端冒煙（連線 → 登入 → 表單） | 📝 待做 |
| 4 | 行動 UX 驗證 + Android 平台行為（返回鍵 / display cutout / 系統列） | 📝 待做 |

## 背景

`apps/Bee.Northwind` 目前支援 **Desktop / Browser / iOS** 三 head（Avalonia 12，共用 `Bee.Northwind.UI`）。本計畫新增第四個前端 head：`Bee.Northwind.Android`（`net10.0-android` + `Avalonia.Android`）。

本計畫是 **iOS 移植（`plan-northwind-ios.md`，已完成）的第二棒**。iOS 那次把大量基礎工作做在框架層，Android 直接繼承，因此本計畫顯著較輕。

### Android 直接繼承的既有成果（不需重做）

| 已就緒（框架層 / app 共用） | 由哪次落地 |
|------|------|
| 共用 UI（`Bee.Northwind.UI`，single-view lifetime） | iOS 已驗證 single-view 路徑（`App.OnFrameworkInitializationCompleted`） |
| AOT reflection XmlSerializer 相容（單一 `Add(T)` + 無參數建構子，見 ADR-025） | iOS 階段 3 |
| 響應式佈局（FormView 欄位 2↔1、明細 InCell↔EditForm、表單垂直捲動） | iOS 階段 4 |
| EditForm 改 overlay（`RowEditDialog` 以「top-level 非 Window」判定 → **已涵蓋 Android single-view**，非僅 iOS） | iOS 階段 5 |
| ListView 窄螢幕卡片化 | iOS 階段 5 |
| safe area（`MainView` 用 `TopLevel.InsetsManager.SafeAreaPadding` → **Android 的 display cutout / 系統列 inset 同樣適用**） | iOS 階段 5 |
| 窄螢幕選單點選自動收起 pane | iOS 階段 5 |

> 結論：Android 的程式工作主要是 **head bootstrap + 工具鏈**，UX/序列化幾乎不必新寫，多為**照抄 iOS 結構 + 驗證繼承行為**。

## Android 與 iOS 的關鍵差異（務必先讀）

1. **工具鏈從零**：iOS 有 Xcode 現成；Android 本機**無 JDK、無 Android SDK、無 emulator**（只有 `maui-android` workload，非獨立 `android`）。最省事是裝 **Android Studio**（一次帶齊 JDK + SDK + emulator）。Apple Silicon 用 **arm64 系統映像**（原生加速）。
2. **模擬器 → 主機網路（最易踩）**：Android emulator 的 `localhost` 指**模擬器自己**，主機要用特殊別名 **`10.0.2.2`**。所以連線 endpoint 必須填 **`http://10.0.2.2:5100/api`**（不是 `localhost`）。`AppDefaults.Endpoint` 預設是 localhost → 模擬器連不到，需在 ConnectionView 改填 `10.0.2.2`。
3. **Cleartext HTTP（iOS ATS 的對應）**：Android 9+ 預設**禁止明文 HTTP**。dev 需在 `AndroidManifest.xml` 的 `<application>` 加 `android:usesCleartextTraffic="true"`（僅開發；正式走 HTTPS 移除）。
4. **AOT/trim 行為不同**：Android **Debug 走 JIT（有 `Reflection.Emit`）**，故 iOS 的「reflection-only XmlSerializer」雷在 Android Debug **預期不會出現**；且框架已修（ADR-025）→ 無論如何安全。Release Android 才 AOT/trim（同 iOS 採 **Debug-first**，Release trim-safe 另案）。`AndroidEnableProfiledAot=false`（Debug）。
5. **返回鍵**：Android 有硬體/手勢返回鍵；需確認行為（關閉目前表單/分頁 vs 退出 app），列入階段 4 驗證。

## 範圍外（明確不做）

- **Release / trim-safe / Play Store 簽章 / AAB 上架**：Debug-first，另案。
- **實機部署**：本期僅模擬器。
- **觸控目標尺寸等更細美化**：與 iOS 同，列日後選用。

---

## 階段 1：Android 工具鏈就緒

1. 安裝 **JDK**（Microsoft OpenJDK 或 Android Studio 內建）。
2. 安裝 **Android SDK** + **arm64 系統映像** + 建一個 **AVD**（最省事：裝 Android Studio，SDK Manager / Device Manager 一站搞定）。
3. `dotnet workload install android`。
4. 驗證：`dotnet workload list` 有 `android`；`adb devices` / `emulator -list-avds` 看得到 AVD；`java -version` 有輸出。
5. （可選）用官方範本 `dotnet new avalonia.xplat` 起 throwaway，`dotnet build App.Android -f net10.0-android` + 部署到 emulator 驗證工具鏈通。

**完成準則**：空殼 Avalonia Android app 能在 emulator 顯示。

## 階段 2：Scaffold `Bee.Northwind.Android` head

新增 `apps/Bee.Northwind/Bee.Northwind.Android/`，鏡像 iOS head 與 `avalonia.xplat` Android 範本。

### 檔案清單

```
Bee.Northwind.Android/
  Bee.Northwind.Android.csproj
  Application.cs            # AvaloniaAndroidApplication<App> + CustomizeAppBuilder（client 接線放這）
  MainActivity.cs          # [Activity(MainLauncher=true)] AvaloniaMainActivity
  Properties/AndroidManifest.xml   # INTERNET + dev usesCleartextTraffic
  Resources/               # values/styles.xml, colors.xml, splash_screen.xml, Icon.png（範本預設）
```

### csproj 要點

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0-android</TargetFramework>
    <SupportedOSPlatformVersion>23</SupportedOSPlatformVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>Bee.Northwind.Android</RootNamespace>
    <ApplicationId>com.bee.northwind</ApplicationId>
    <ApplicationVersion>1</ApplicationVersion>
    <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
    <AndroidPackageFormat>apk</AndroidPackageFormat>
    <AndroidEnableProfiledAot>false</AndroidEnableProfiledAot>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
    <!-- 不設 TreatWarningsAsErrors：Release trim 分析會對 Bee 反射序列化噴 IL2026 等（Debug-first，同 iOS）-->
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia.Android" Version="12.0.4" />
    <PackageReference Include="Xamarin.AndroidX.Core.SplashScreen" Version="..." />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Bee.Northwind.UI\Bee.Northwind.UI.csproj" />
  </ItemGroup>
</Project>
```

### client 接線（沿用 Desktop/iOS 契約，放 `Application.CustomizeAppBuilder`）

```csharp
ApiClientInfo.ApiKey = AppDefaults.ApiKey;
ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Remote;
ClientInfo.EndpointStorage = new FileEndpointStorage("Bee.Northwind"); // 階段 3 驗證沙箱可寫
```

### AndroidManifest.xml 要點

```xml
<application android:label="Bee.Northwind" android:icon="@drawable/Icon"
             android:usesCleartextTraffic="true">  <!-- DEV ONLY：明文 HTTP 連 dev server -->
  ...
</application>
<uses-permission android:name="android.permission.INTERNET" />
```

### slnx + VSCode

- 加入 `apps/Bee.Northwind/Bee.Northwind.slnx`。
- （可選）`.vscode/launch.json` 加「Run Bee.Northwind.Android」+ prep task（沿用既有 `free-northwind-port` / `wait-for-northwind-server`）。

**完成準則**：`dotnet build apps/Bee.Northwind/Bee.Northwind.Android -f net10.0-android -c Debug` 通過。

## 階段 3：模擬器 Debug 跑通 + 端到端冒煙

1. 啟動 AVD + Server（:5100）。
2. 部署：`dotnet build -t:Run -f net10.0-android -c Debug`（或 IDE 選 emulator）。
3. **連線 endpoint 改 `http://10.0.2.2:5100/api`**（emulator → 主機 loopback；localhost 連不到）。
4. **端點儲存驗證**：輸入 endpoint → 重啟 app → 確認持久化（`FileEndpointStorage` → app 私有目錄，預期可寫）。
5. 端到端冒煙：**連線 → 登入（demo/demo）→ 開表單（CRUD 讀取）→ 明細 EditForm（overlay）** 走一輪。
6. 觀察 logcat：Debug 走 JIT，預期**無** iOS 的 AOT XmlSerializer 例外；若見 cleartext 被擋，補 manifest `usesCleartextTraffic`。

**完成準則**：emulator 上完成「連線 → 登入 → 清單(真實資料) → 開記錄」一輪，endpoint 持久化驗證通過。

## 階段 4：行動 UX 驗證 + Android 平台行為

繼承自框架的 UX **預期直接生效**，本階段主要**驗證**而非新寫：

- 響應式（主檔單欄 / 明細 EditForm）、ListView 卡片、EditForm overlay、選單自動收起 —— 逐一在 emulator 確認。
- **safe area**：Android 的 display cutout（瀏海）/ 狀態列 / 導覽列 inset 是否經 `InsetsManager.SafeAreaPadding` 正確套用（`MainView` 既有邏輯）；若 Android 回報方式不同需微調。
- **返回鍵**：Android 硬體/手勢返回鍵行為 —— 確認是否該關閉目前分頁/表單而非直接退出；必要時於 `MainActivity` 處理 `OnBackPressed` 或 Avalonia 的 BackRequested。
- 方向直/橫 reflow（`ConfigChanges` 已含 Orientation）。

> 純美化（觸控目標尺寸等）與 iOS 同，列日後選用，不阻擋完成。

**完成準則**：emulator 直/橫向皆不破版，繼承的行動 UX 正常，返回鍵行為合理。

---

## 注意事項（沿用 iOS 經驗）

- **AOT 增量建置雷**：改 code 後若啟動崩在 `load_aot_module`（Android Release 才 AOT；Debug 較不會），先 `rm -rf bin obj` clean 重建。
- **AOT XmlSerializer 已修**：ADR-025 的修正對 Android Release 同樣有效，無需另做。
- **多為照抄**：head bootstrap 對照 `Bee.Northwind.iOS` + `avalonia.xplat` Android 範本；UX 不必新寫。

## 完成後

- 在本檔頂部標記 `✅ 已完成` 與日期，階段表格逐列更新。
- `apps/Bee.Northwind` 達成 Desktop / Browser / iOS / Android 四 head。
- 視需要回寫一則 reference 記憶或更新 ADR-025（若 Android 揭露新平台差異）。
