# 計畫：Bee.Northwind 新增 iOS head（Avalonia）

**狀態：🚧 進行中（2026-06-25）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | iOS 工具鏈就緒（`ios` workload + 模擬器 runtime + 空殼驗證） | ✅ 已完成（2026-06-25） |
| 2 | Scaffold `Bee.Northwind.iOS` head（bootstrap + client 接線 + slnx） | ✅ 已完成（2026-06-25） |
| 3 | 模擬器 Debug 跑通 + 端到端冒煙（連線 → 登入 → 表單） | ✅ 已完成（2026-06-26）：端到端通過（連線→登入→選單→清單真實資料→開記錄響應式單欄）。修掉兩個 AOT XmlSerializer 不相容（見下） |
| 4 | 響應式佈局（FormView 依寬度：主檔欄位 2 欄↔1 欄重排 + 明細 InCell↔EditForm，**框架層 + 測試 + CI**） | ✅ 已完成（2026-06-25） |
| 5 | 行動 UX 微調（safe area / 觸控 / 方向 / EditForm 彈窗全螢幕化） | 📝 待做 |

> 階段 4 是 `src/Bee.UI.Avalonia` 框架層改動（非 iOS head），與 iOS 工具鏈解耦：**在桌面把視窗縮窄即可驗證**，不必等階段 1~3，亦可提前實作。列於此處只是敘事順序。

## 背景

`apps/Bee.Northwind` 目前是 **Avalonia 12** 架構，採「共用 UI 層 + 每平台一個 head」：

```
Bee.Northwind.UI        共用 Avalonia UI（平台中立，僅引用 Avalonia 核心 + DataGrid + Semi + Inter + CommunityToolkit）
Bee.Northwind.Desktop   classic-desktop lifetime（Avalonia.Desktop）
Bee.Northwind.Browser   single-view lifetime（Avalonia.Browser / WASM）
Bee.Northwind.Server    JSON-RPC 後端
```

本計畫新增第三個前端 head：`Bee.Northwind.iOS`（`net10.0-ios` + `Avalonia.iOS`）。
**Android 不在本計畫範圍**，待 iOS 完成後另起 `plan-northwind-android.md`，照抄本計畫已驗證的 pattern。

### 為何 iOS 先

- 開發機為 macOS（Apple Silicon），**Xcode 26.5 已安裝** —— iOS 最大前置已就緒；Android 需從零補 JDK + SDK + emulator。
- iOS 模擬器在 Mac 上是最順的迭代迴圈。
- 行動端「共同問題」（sandbox 儲存、async init、觸控 UX、Debug-first trimming）在 iOS 上解一次，Android 多為重複套用。

## 既有架構盤點（讓本計畫成本極低的關鍵）

| 觀察 | 來源 | 對 iOS 的影響 |
|------|------|--------------|
| `App.OnFrameworkInitializationCompleted` 已分支 `ISingleViewApplicationLifetime` → 掛 `MainView` | `Bee.Northwind.UI/App.axaml.cs` | iOS 同為 single-view lifetime，**UI 零改動即可開機**，與 Browser 共用同一條路徑 |
| `Bee.Northwind.UI` 不引用 `Avalonia.Desktop` | `Bee.Northwind.UI.csproj` | UI 層平台中立，iOS head 只需加 `Avalonia.iOS` |
| 每個 head 在 Avalonia 啟動前接 `ApiClientInfo` + `ClientInfo.EndpointStorage` | Desktop/Browser `Program.cs` | iOS 沿用同一份 client 契約（`Remote` connector + `ApiKey`） |
| `FileEndpointStorage` 寫於 `SpecialFolder.LocalApplicationData` | `src/Bee.UI.Avalonia/Storage/FileEndpointStorage.cs` | iOS 沙箱下該路徑落在可寫的 `Library/`，**很可能可直接重用**（階段 3 驗證） |
| 連線初始化走 `ClientInfo.InitializeAsync`（非 sync-over-async） | `[[wasm-client-async-init]]` | iOS 同樣以 async 為主，無新增風險 |

## 範圍外（明確不做）

- **Release / trim-safe 構建**：iOS Release 強制 AOT + trim，會砍掉 `XmlSerializer`(FormSchema) 的反射路徑，重現 `XmlSerializeErrorDetails 2,2`（見 `.claude/rules/maui.md` Apple trim 決策樹）。本期一律 **Debug 跑**。要 ship 時另開 ADR 走 source-gen / `[DynamicallyAccessedMembers]`。
- **實機（device）部署 / 簽章 / TestFlight**：需 Apple Developer 帳號 + provisioning，另案。本期僅模擬器。
- **Android head**：另起 plan。
- **CI（分兩塊看）**：
  - **iOS head（`apps/`）**：`build-ci.yml` 只在 `src/ tests/ ...` 變動觸發、且不跑 iOS（需 macOS runner + workload）。iOS head **不納入 CI**，本機 Debug 驗證為唯一把關。
  - **響應式 EditMode（階段 4，`src/Bee.UI.Avalonia`）**：屬框架層，**會進 CI**，需補 `Bee.UI.Avalonia.UnitTests` 單元測試（已有 `InternalsVisibleTo`）。

---

## 階段 1：iOS 工具鏈就緒

**目標**：確認本機能 build + 跑一個空殼 Avalonia iOS app 於模擬器，排除工具鏈問題後才動 Northwind。

1. 安裝 workload：`dotnet workload install ios`
2. 確認模擬器 runtime：`xcrun simctl list runtimes | grep iOS`
   - 無 runtime → 於 Xcode 下載一個 iOS 模擬器 runtime
3. （可選但建議）用官方範本起一個 throwaway app 驗證鏈路：
   - `dotnet new install Avalonia.Templates`（若未裝）
   - `dotnet new avalonia.app`（或 ios 範本）→ `dotnet build -t:Run -f net10.0-ios`
   - 跑得起來即可刪除，確認「workload + Xcode + 模擬器」三者通

**完成準則**：空殼 Avalonia app 能在 iOS 模擬器顯示視窗。

## 階段 2：Scaffold `Bee.Northwind.iOS` head

新增專案 `apps/Bee.Northwind/Bee.Northwind.iOS/`，鏡像 Desktop/Browser head 的接線。

### 檔案清單

```
Bee.Northwind.iOS/
  Bee.Northwind.iOS.csproj
  Main.cs                          # UIApplication.Main 進入點
  AppDelegate.cs                   # AvaloniaAppDelegate<App> + CustomizeAppBuilder
  Info.plist                       # bundle id / display name / 方向 / 最低 iOS / ATS
  Entitlements.plist               # （模擬器可省，預留）
  Assets.xcassets/ 或 Resources/   # app icon / launch（用 Avalonia 範本預設值）
```

### csproj 要點

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-ios</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>Bee.Northwind.iOS</RootNamespace>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
    <!-- 不設 TreatWarningsAsErrors：iOS trim 分析會對 Bee 反射序列化噴 IL2026/2104（見頂部範圍外）-->
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia.iOS" Version="12.0.4" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Bee.Northwind.UI\Bee.Northwind.UI.csproj" />
  </ItemGroup>
</Project>
```

> **linker 雷（實測）**：一度加 `<MtouchLink>None</MtouchLink>`（Debug）想關 trimming，
> 結果 app 啟動即 **`load_aot_module` SIGABRT** —— 正是 maui.md「None 單獨 → AOT 模組不符」。
> 已 revert。**預設 Debug 建置（58 個 trim 警告、不覆寫 linker）可正常啟動並渲染**；
> 預設 Debug 對 app code 不做破壞性 trim，UI 完整顯示。FormSchema/DataSet 序列化是否受
> SDK 層 trim 影響，待「Connect → 開表單」實測（若炸 `XmlSerializeErrorDetails 2,2` 再走
> TrimmerRootDescriptor / `[DynamicallyAccessedMembers]`，不可用 MtouchLink=None）。

### client 接線（沿用 Desktop/Browser 契約）

於 Avalonia 啟動前（`AppDelegate.CustomizeAppBuilder` 或 `Main` 內）設定：

```csharp
ApiClientInfo.ApiKey = AppDefaults.ApiKey;
ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Remote;
ClientInfo.EndpointStorage = new FileEndpointStorage("Bee.Northwind"); // 階段 3 驗證沙箱可寫
```

### Info.plist 關鍵欄位

- `CFBundleIdentifier`、`CFBundleDisplayName`、`MinimumOSVersion`
- `UISupportedInterfaceOrientations`（先允許直 + 橫，UX 階段再收斂）
- **ATS（App Transport Security）**：Northwind client 以使用者輸入的 endpoint 連 JSON-RPC 後端。若連 http（dev / 區網），需 ATS 例外：
  - dev 階段：`NSAppTransportSecurity` → `NSAllowsArbitraryLoads = true`（僅開發，勿 ship）
  - 或連區網 host：`NSAllowsLocalNetworking = true`
  - 正式應走 https，免 ATS 例外

### slnx 整合

- 將 `Bee.Northwind.iOS` 加入 `apps/Bee.Northwind/Bee.Northwind.slnx`

**完成準則**：`dotnet build apps/Bee.Northwind/Bee.Northwind.iOS -f net10.0-ios -c Debug` 通過。

## 階段 3：模擬器 Debug 跑通 + 端到端冒煙

> **結果（2026-06-26）**：端到端通過。連線 → 登入（demo/demo）→ 選單（ProgramSettings）→
> Categories 清單顯示真實 Northwind 資料 → 開記錄（FormView 響應式單欄）全部正常。
> 過程修掉**兩個 AOT XmlSerializer 不相容**（iOS 無 Reflection.Emit、走 reflection-only 反序列化）：
>
> 1. **多載 `Add` → `AmbiguousMatchException`**：reflection reader 用 `Type.GetMethod("Add")`（不帶參數型別）找集合 add，Bee 集合有多個 `Add` 多載即撞。**修法**：讓每個定義集合只剩一個 public instance `Add(T)` —— 基底的介面 `Add(I…CollectionItem)` 改顯式實作（4 基底）、各集合便利 `Add(...)` 改**擴充方法**（19 集合，呼叫端語法不變、`this` 可空 + `ThrowIfNull`）。
> 2. **集合無無參數建構子 → `MissingMethodException`**：reflection reader 用 `Activator.CreateInstance` 建立集合需 public 無參數建構子；owner-coupled 集合（建構子收 owner）缺之。**修法**：為 12 個這類集合補 `public Xxx() : base()`（item 型別本就有無參數建構子）。
>
> 兩者皆**格式逐字不變**（XmlSerializer 仍照常處理集合，`[XmlArray]`/多型保留）——
> Definition 723 + Base 464 round-trip 測試全綠驗證。詳見 `[[ios-xmlserializer-ambiguous-add]]`。
> **桌面/WASM 用 codegen 路徑（有 Reflection.Emit）本就沒事，此修法對其無行為影響。**

1. 部署到模擬器：`dotnet build -t:Run -f net10.0-ios -c Debug`（或從 IDE 選模擬器啟動）
2. **端點儲存驗證**：在 ConnectionView 輸入 endpoint → 重啟 app → 確認 endpoint 有持久化
   - 通過 → `FileEndpointStorage` 直接重用，**不需新 storage 類**
   - 失敗（沙箱不可寫 / 讀不回）→ 退而比照 maui.md 思路，寫 `IEndpointStorage` 行動版（如 `NSUserDefaults`-backed），放 head 內或 `Bee.UI.Avalonia` 平台分支
3. 端到端冒煙：**連線 → 登入 → 開一張表單（CRUD 讀取）** 走通一輪
4. 觀察 console，確認無 trimming / 序列化例外（Debug 不該出現；若出現代表誤開 trim）

**完成準則**：模擬器上完成「連線 → 登入 → 表單讀取」一輪，endpoint 持久化驗證通過。

## 階段 4：響應式佈局（框架層）

**需求**：小畫面（手機 / 窄視窗）下整張 `FormView` 適應 ——
（1）**主檔欄位區從多欄重排為單欄**（View 模式即可見）；
（2）明細 `GridControl` 從 inline `InCell` 改用 `EditForm`（唯讀 grid + 編輯表單）。
**決策**：採**依畫面寬度自動切**（響應式），不靠 head 旗標 —— 一次涵蓋 iOS app、手機開的 `Bee.Northwind.Browser`（網頁版）、未來 Android，且隨視窗縮放/旋轉即時反應。

### 現況（已就緒的部分）

- `GridEditMode` enum 已有 `InCell`（預設）與 `EditForm` 兩值，**`EditForm` 已完整實作**（`RowEditPanel` 就地面板、`RowEditDialog` 彈窗、`FieldEditorBinder`）。
- `FormView` 已具 `DetailEditMode`（明細偏好）與 `BuildFieldGrid` 多欄佈局（`FormLayout.ColumnCount`）。
- 目前 Northwind 無任何覆寫 → 全多欄 + `InCell`。

### 設計（實作版）

統一以 **`_isCompact` 狀態**驅動，跨門檻才**重建表單**（欄數變動需重組 Grid 結構，不能就地改屬性）：

```
isCompact = viewportWidth > 0 && viewportWidth < CompactWidthThreshold
主檔欄數    = isCompact ? 1 : FormLayout.ColumnCount
明細編輯模式 = isCompact ? EditForm : DetailEditMode
```

- 新增可調 `CompactWidthThreshold`（styled property，預設 `600`；最終值待階段 5 調校）。
- **Bounds 反應走 `OnPropertyChanged`**（非 static class handler）：`Bounds` 是繼承的 direct property，class handler 不會觸發 —— 此為早期 bug 根因，已改用 repo 既有的 `OverlayDialogHost` 模式。
- `ApplyResponsiveState` 只在跨門檻時 `Rebuild()`，band 內的 Bounds tick 僅一次比較，不 thrash。
- `DetailEditMode` 保留為「寬螢幕偏好」（預設 `InCell`），桌面行為不變。

> 門檻只看寬度可能讓手機橫向（邏輯寬度較大）落回寬版，但橫向高度小、配合鍵盤同樣侷促。是否改採「短邊 / 面積 / 方向」啟發式，留階段 5 對真機/模擬器調校決定。

### 測試（進 CI）

- `IsCompactWidth` 純函式 Theory（含邊界 = 門檻、未量測 = 0）。
- 跨寬度：窄 → 主檔單欄 + 明細 EditForm；放寬 → 回多欄 + DetailEditMode。

**完成準則**：桌面把視窗由寬縮窄，明細 grid 由 `InCell` 自動切到 `EditForm`（反之亦然）；單元測試通過、CI 綠。

## 階段 5：行動 UX 微調

共用 `MainView` 目前是桌面導向（DataGrid、側邊導覽）。本階段做最小行動適配，不重寫 UI：

- **EditForm 彈窗在 iOS 改走 overlay / 全螢幕 sheet** —— `RowEditDialog` 目前只特判 browser 用 overlay，其餘一律開 native `Window`；iOS single-view **不支援 native Window**，此分支須改成「非 desktop → overlay」（panel 欄數響應式已於階段 4 完成，見 `RowEditPanel.Compact`）
- 階段 4 `CompactWidthThreshold` 的最終值 / 是否納入方向啟發式，於此對模擬器調校定案
- safe area（瀏海 / Home indicator）內距
- 觸控目標尺寸（按鈕 / 列高）
- 方向處理（確認直/橫切換不破版）
- 導覽在窄螢幕的呈現（側欄 → 抽屜 / 漢堡，視情況；可留 follow-up）
- 主檔 `ListView` 瀏覽列在窄螢幕的呈現（橫向捲動 vs 卡片式）—— 較大重構，可留 follow-up

> 此階段以「可用」為準，不追求像素級行動設計；較大的響應式重構若有需要，另列 follow-up，不卡本計畫收斂。

**完成準則**：模擬器直/橫向皆不破版，主要操作觸控可達，明細編輯走全螢幕 EditForm。

---

## 完成後

- 在本檔頂部標記 `✅ 已完成` 與日期，階段表格逐列更新。
- 視需要補一則 ADR 記錄「iOS head 採 Debug-first、Release trim 留待 source-gen」的決策。
- 另起 `plan-northwind-android.md`，以本計畫驗證過的 head 結構為藍本。
