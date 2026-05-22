# 計畫：Bee.UI.Maui Phase 1 觸發 — 加入 DynamicForm + FormDataObject

**狀態：✅ 已完成（2026-05-22）**

## 1. 背景

`Bee.Web.Blazor.Server` / `Bee.Web.Blazor.Wasm` 已於 [plan-blazor-web-integration.md](plan-blazor-web-integration.md) Phase 1a 完成
`DynamicForm`（layout-driven 動態表單）+ `FormDataObject`（DataSet 暫存與 GetField/SetField round-trip）。

`Bee.UI.Maui` 目前為 [plan-add-bee-ui-maui.md](plan-add-bee-ui-maui.md) §3.1 規範下的 **Phase 0 placeholder**
（plain `net10.0`、僅 `PlaceholderInfo.cs`）。本計畫**觸發 Phase 1**，把 native 端的對應元件補齊，
讓三個前端 family（Blazor Server / Blazor Wasm / MAUI）的「FormSchema-driven UI」入口同步存在。

### 1.1 與 Blazor 兩專案的對稱性

| 項目 | Blazor.Server / Blazor.Wasm | Bee.UI.Maui（本計畫） |
|------|----------------------------|----------------------|
| 動態表單元件 | `Components/DynamicForm.razor` + `.razor.cs` | `Controls/DynamicForm.cs`（C# code-only `ContentView`） |
| 資料物件 | `DataObjects/FormDataObject.cs` | `DataObjects/FormDataObject.cs`（純 C# port） |
| UI primitive | HTML `<input>` / `<select>` / `<textarea>` | MAUI `Entry` / `Picker` / `DatePicker` / `CheckBox` / `Editor` |
| 渲染檔形式 | Razor | C# 構造視覺樹（不寫 XAML，見 §4.1 決策） |
| Connector 介接 | `FormApiConnector`（Phase 1b 對接） | 同左（Phase 1b 對接） |

---

## 2. 範圍

### 2.1 動的部分

| 檔案 | 異動 |
|------|------|
| `src/Bee.UI.Maui/Bee.UI.Maui.csproj` | **整檔改寫**：plain net10.0 → multi-target MAUI（見 §3） |
| `src/Bee.UI.Maui/PlaceholderInfo.cs` | **刪除**（plan-add-bee-ui-maui.md §5.1：「第一個控制項加入時直接刪除」） |
| `src/Bee.UI.Maui/DataObjects/FormDataObject.cs` | **新增**（從 Blazor port，namespace 改為 `Bee.UI.Maui.DataObjects`） |
| `src/Bee.UI.Maui/Controls/DynamicForm.cs` | **新增**（C# code-only ContentView，依 ControlType dispatch MAUI 控制項） |
| `tests/Bee.UI.Maui.UnitTests/Bee.UI.Maui.UnitTests.csproj` | **新增**（net10.0；對齊 Blazor 兩個 test project 模式）|
| `tests/Bee.UI.Maui.UnitTests/DataObjects/FormDataObjectTests.cs` | **新增**（從 Bee.Web.Blazor.Server.UnitTests port，namespace 改寫）|
| `tests/Bee.UI.Maui.UnitTests/Controls/DynamicFormTests.cs` | **新增**（BindableProperty 結構性 smoke）|
| `tests/Bee.UI.Maui.UnitTests/Controls/DynamicFormHelperTests.cs` | **新增**（private helper reflection 測試）|
| `Bee.Library.slnx` | tests 區塊加入 `Bee.UI.Maui.UnitTests` |
| `.github/workflows/build-ci.yml` | **不變動**（見 §6 結論：net10.0 default 不需 maui workload）|
| `docs/plans/plan-add-bee-ui-maui.md` | 狀態列補註「Phase 1 已觸發（YYYY-MM-DD），實作見本計畫」 |
| `docs/dependency-map.md` + `.zh-TW.md` | 從「placeholder」描述改為「首個動態表單元件已實作」（描述句調整，mermaid 圖不變） |

### 2.2 不動的部分

- **`nuget-publish.yml` 仍不加 `Bee.UI.Maui`**（plan-add-bee-ui-maui.md §6 已說明；publish runner 改 macos-latest 等議題留待真正發版時處理）
- **不實作 DynamicGrid / FormPage**（Blazor 端也尚未實作；對應計畫的 Phase 1b 統一觸發時再做）
- **不對接 `FormApiConnector` LoadAsync / SaveAsync**（Blazor 端為 Phase 1b 對接，本 plan 不超前）
- **不修改 `Bee.UI.Core`**（既有 `ClientInfo` 等抽象足夠）
- **不修改 `Bee.Definition.Layouts`** 內任何型別（`ControlType` 等已足夠）

---

## 3. csproj 改寫

完整改寫為以下內容（取代現有 30 行 Phase 0 placeholder）：

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!--
      Phase 1 (active): MAUI multi-target.
      net10.0 (no platform) 為 design-time / unit-test TFM，提供給 Bee.UI.Maui.UnitTests
      net10.0 test project ProjectReference 用。其餘 platform TFM 依 host OS 條件加入：
      Linux:   net10.0;net10.0-android
      macOS:   net10.0;net10.0-android;net10.0-ios;net10.0-maccatalyst
      Windows: net10.0;net10.0-android;net10.0-windows10.0.19041.0
    -->
    <TargetFrameworks>net10.0;net10.0-android</TargetFrameworks>
    <TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('osx'))">$(TargetFrameworks);net10.0-ios;net10.0-maccatalyst</TargetFrameworks>
    <TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">$(TargetFrameworks);net10.0-windows10.0.19041.0</TargetFrameworks>

    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <Nullable>enable</Nullable>

    <!-- iOS / Mac Catalyst / Android / Windows 最低支援版本 -->
    <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">15.0</SupportedOSPlatformVersion>
    <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'maccatalyst'">15.0</SupportedOSPlatformVersion>
    <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">21.0</SupportedOSPlatformVersion>
    <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">10.0.17763.0</SupportedOSPlatformVersion>
    <TargetPlatformMinVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">10.0.17763.0</TargetPlatformMinVersion>

    <Description>Cross-platform MAUI control library for Bee.NET (FormSchema-driven UI for iOS / Android / macOS / Windows).</Description>
    <PackageTags>bee.net;maui;ui;controls;mobile;desktop</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Bee.UI.Core\Bee.UI.Core.csproj" />
  </ItemGroup>

</Project>
```

### 3.1 為何採「host-OS 條件式 TFM」而非 plan-add-bee-ui-maui.md §3.2 原文（Windows 條件 Mac TFM）

原文 §3.2 用 `<TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">...$(TargetFrameworks);net10.0-windows10.0.19041.0</TargetFrameworks>`，
但**沒有為 Linux 排除 iOS/Maccatalyst**，也未保留 `net10.0` design-time TFM。本計畫修正為：

- **`net10.0` 永遠在列**（design-time / unit test 用；test project 透過此 TFM ProjectReference）
- **`net10.0-android` 永遠在列**（Linux CI 可單獨 build；最低 mobile 平台覆蓋）
- **macOS host 加入 iOS / Maccatalyst**（本機開發者環境）
- **Windows host 加入 Windows target**（無 paired Mac 也能 pack 完整 Windows TFM）

如此 ubuntu-latest CI 可順利 build `net10.0;net10.0-android`；macOS 本機開發者可 build/test 全部 Apple TFM；
Windows 開發者可 build/test Android + Windows；test project 永遠能透過 `net10.0` ProjectReference。

### 3.2 `Microsoft.Maui.Controls` PackageReference 是否需要顯式加？

`<UseMaui>true</UseMaui>` 會由 MAUI SDK 自動帶入 implicit `Microsoft.Maui.Controls` reference，
無需顯式 `<PackageReference>`。**保持 implicit 即可**，避免版本固定衝突 SDK 預設值。

### 3.3 `Bee.UI.Core` cross-TFM 相容性

`Bee.UI.Core` 目前為 `net10.0`，會被 platform-specific TFM（`net10.0-android` 等）視為相容
（.NET 10 platform TFMs 對 base `net10.0` 為 superset）。**不需動 `Bee.UI.Core`**。

### 3.4 `net10.0` TFM 下 DynamicForm 可否編譯

`UseMaui=true` + MAUI SDK 在 `net10.0`（無 platform）TFM 下提供 `Microsoft.Maui.Controls` ref assemblies，
讓 `DynamicForm : ContentView` 與 MAUI 控制項（`Entry` / `Picker` / `DatePicker` / `CheckBox` / `Editor`）
**可正常編譯**。runtime instantiate 不需 MAUI app builder（單純物件 `new` + reflection 檢查），
測試的結構性 smoke（檢查 `BindableProperty` / 物件可建立）皆可運作。

**若驗證階段發現 `net10.0` 不支援 MAUI controls 編譯**（極少數 SDK 設定），fallback：
- DynamicForm.cs 包 `#if ANDROID || IOS || MACCATALYST || WINDOWS`，net10.0 編譯時排除
- 測試只覆蓋 FormDataObject，DynamicForm 結構性測試暫不加（Blazor 端 reflection-based DynamicFormTests 不可移植）
- Plan §7 註記此 fallback 已採用

---

## 4. DynamicForm 實作

### 4.1 為何 C# code-only（不寫 XAML）

`FormLayout.Sections` / `LayoutField` 在 runtime 才知道有幾個欄位、什麼型別。
XAML 在 design-time 不知道欄位結構，能放的只有 root container；實際視覺樹仍須由 code-behind 動態建立。
XAML 在此貢獻為零卻多引入 XAML compile pipeline，**直接寫 C# 構造視覺樹**最精簡，且 diff 易讀。
同時呼應 Blazor 端「foreach field → dispatch by ControlType」的程式碼結構，跨 family review 對應度高。

### 4.2 ControlType → MAUI Control 對應表

| `ControlType` | MAUI 控制項 | binding 屬性 | 備註 |
|---|---|---|---|
| `CheckEdit` | `CheckBox` | `IsChecked` | 對應 Blazor `<input type="checkbox">` |
| `DateEdit` | `DatePicker` | `Date` | 對應 Blazor `<input type="date">` |
| `YearMonthEdit` | `DatePicker`（`Format="yyyy-MM"`） | `Date`，僅顯示年月 | MAUI 無原生 month picker，用 DatePicker + Format 取代；對應 Blazor `<input type="month">` |
| `MemoEdit` | `Editor` | `Text` | multi-line；對應 Blazor `<textarea>` |
| `DropDownEdit` | `Picker` | `ItemsSource` / `SelectedItem` | 從 `FormDataObject.GetFormField(name)?.ListItems` 取選項；對應 Blazor `<select>` |
| `TextEdit` / `ButtonEdit` / `Auto` / 其他 | `Entry` | `Text` | 對應 Blazor `<input type="text">` |

### 4.3 元件 API（與 Blazor `DynamicForm` 對齊）

```csharp
namespace Bee.UI.Maui.Controls;

public class DynamicForm : ContentView
{
    // BindableProperty 名為 FormLayoutProperty 而非 LayoutProperty —— VisualElement
    // 已有 public Layout(Rect) 方法，重用名稱會強迫加 `new` 並 shadow MAUI 排版主進入點。
    public static readonly BindableProperty FormLayoutProperty =
        BindableProperty.Create(nameof(FormLayout), typeof(FormLayout), typeof(DynamicForm),
            propertyChanged: (b, o, n) => ((DynamicForm)b).Rebuild());

    public static readonly BindableProperty DataObjectProperty =
        BindableProperty.Create(nameof(DataObject), typeof(FormDataObject), typeof(DynamicForm),
            propertyChanged: (b, o, n) => ((DynamicForm)b).Rebuild());

    public FormLayout? FormLayout { get => (FormLayout?)GetValue(FormLayoutProperty); set => SetValue(FormLayoutProperty, value); }
    public FormDataObject? DataObject { get => (FormDataObject?)GetValue(DataObjectProperty); set => SetValue(DataObjectProperty, value); }

    private void Rebuild() { /* 重新依 FormLayout 建立 VerticalStackLayout/Grid + 控制項 */ }
}
```

- 與 Blazor `[Parameter] Layout` / `[Parameter] DataObject` 對應為 `BindableProperty`
- **MAUI 端 property 名為 `FormLayout`（非 `Layout`）**：`VisualElement.Layout(Rect)` 已佔用 `Layout` 名稱，避免 `new` shadow MAUI 排版方法
- 兩個 property 任一變動就重建整個視覺樹（first iteration；future optimization 可改為 diff 重建）
- `IdPrefix` Blazor 用於 DOM id；MAUI 不需要 DOM id，**捨棄此 parameter**

### 4.4 視覺樹結構

```
ContentView (DynamicForm)
└─ VerticalStackLayout
   └─ foreach (section in Layout.Sections):
      Frame (BorderColor / Padding 包住 section)
      ├─ Label (section.Caption, 條件 ShowCaption + non-empty)
      └─ Grid (ColumnDefinitions = ColumnCount, Spacing = 8)
         └─ foreach (field in section.Fields.Where(f => f.Visible)):
            VerticalStackLayout (Grid.Row / Grid.Column / Grid.RowSpan / Grid.ColumnSpan)
            ├─ Label (field.Caption)
            └─ <ControlType-specific control> (binding via TextChanged / DateSelected / CheckedChanged 等事件 → DataObject.SetField)
```

- Grid 佈局對應 Blazor 端的 `grid-template-columns: repeat(N, ...)` + `grid-row: span M / grid-column: span K`
- 控制項初始值由 `DataObject.GetField(fieldName)` 字串透過 `ConvertFromString` 解析（時間 / 布林 / 數字）
- 事件 handler 把控制項回寫的值轉回字串呼叫 `DataObject.SetField(fieldName, value)`，與 Blazor 端 round-trip 一致

---

## 5. FormDataObject port

從 [src/Bee.Web.Blazor.Server/DataObjects/FormDataObject.cs](../../src/Bee.Web.Blazor.Server/DataObjects/FormDataObject.cs) 完整複製，僅異動：

| 項目 | 原 Blazor.Server | Bee.UI.Maui port |
|---|---|---|
| `namespace` | `Bee.Web.Blazor.Server.DataObjects` | `Bee.UI.Maui.DataObjects` |
| 其餘程式碼 | — | **逐字相同**（包含 XML doc、`Phase1bMessage` 字串、Helper methods） |

**為何不抽公共 base**：與 plan-blazor-web-integration.md §「兩套元件庫各自獨立維護，不共用 UI 元件」一致。
MAUI 與 Blazor 跑在不同執行環境（mobile native vs server-rendered HTML vs browser WASM），未來各自演化的可能性高。
3 份重複的成本（< 250 行 × 3 = 750 行）小於抽象化的耦合成本。

---

## 6. CI workflow 變動

**結論：build-ci.yml 不需任何變動**（與 plan-add-bee-ui-maui.md §4.2「未來 Phase 1 需加 maui-android workload」的預期不同）。

### 6.1 為何不需 maui-android workload

`Bee.UI.Maui` 的 default TFM 已收斂為**僅 `net10.0`**（platform TFM 透過 `BeeUiMauiFullPlatforms=true`
opt-in，CI 不啟用此 property）。`net10.0` build 僅需：
- 標準 .NET 10 SDK（CI 既有）
- `Microsoft.Maui.Controls` NuGet 套件（標準 `dotnet restore` 取得，不需 workload）

`Microsoft.Maui.Controls` 在 NuGet 套件內含 `lib/net10.0/Microsoft.Maui.Controls.dll` ref assembly，
供 `net10.0`（無 platform）目標編譯使用。**無須安裝 `maui-android` workload**。

### 6.2 既有 step 全部沿用

| step | 涵蓋 Bee.UI.Maui 嗎 | 備註 |
|------|--------------------|------|
| `Strict build`（`dotnet build Bee.Library.slnx`）| ✅ | 已包含 Bee.UI.Maui net10.0 TFM |
| `SonarScanner Begin` | ✅ | 無需特殊配置 |
| `Build (for Sonar coverage)` | ✅ | 同 strict build |
| `Test with coverage`（`dotnet test Bee.Library.slnx`）| ✅ | 包含 Bee.UI.Maui.UnitTests 33 個測試 |
| `SonarScanner End` | ✅ | 無需特殊配置 |
| `Pack NuGet packages (驗證用，不推送)` | ❌（intentional）| 依 plan-add-bee-ui-maui.md §6，Bee.UI.Maui 不加入顯式 pack 列表；待真正發版時統一處理 |

### 6.3 `Microsoft.Maui.Controls` NuGet restore 在 CI 的成本

| 項目 | 估計 |
|------|------|
| 套件下載 + transitive deps（Microsoft.Maui.Controls.Core / Essentials / Graphics 等）| ~50 MB |
| 首次 restore 時間 | < 30 秒 |
| Cache 命中後（既有 `actions/cache` 或 `setup-dotnet` 內建 cache）| 幾乎 0 |

對 CI 整體時間影響極小，**不需額外 cache step**。

### 6.4 未來若要加入 platform TFM 驗證

若未來需要在 CI 加入 `net10.0-android` smoke build：
1. 加 `dotnet workload install maui-android --skip-manifest-update` step
2. CI step 加 `-p:BeeUiMauiFullPlatforms=true`
3. 加 `actions/cache` cache `~/.dotnet/sdk-manifests` + workload packs

本計畫不做，當實際需求出現時（如新增平台專屬控制項）再觸發。

---

## 7. tests/Bee.UI.Maui.UnitTests 結構

對齊 Blazor 兩個 test project 模式（[Bee.Web.Blazor.Server.UnitTests](../../tests/Bee.Web.Blazor.Server.UnitTests/) /
[Bee.Web.Blazor.Wasm.UnitTests](../../tests/Bee.Web.Blazor.Wasm.UnitTests/)）：

### 7.1 csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="8.0.1">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.4.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Bee.UI.Maui\Bee.UI.Maui.csproj" />
    <ProjectReference Include="..\Bee.Tests.Shared\Bee.Tests.Shared.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

與 [Bee.Web.Blazor.Server.UnitTests.csproj](../../tests/Bee.Web.Blazor.Server.UnitTests/Bee.Web.Blazor.Server.UnitTests.csproj) 對應，
僅 ProjectReference 由 `Bee.Web.Blazor.Server` 換成 `Bee.UI.Maui`。

### 7.2 測試檔案結構

| 檔案 | 對應 Blazor 來源 | 內容 |
|------|---------------|------|
| `DataObjects/FormDataObjectTests.cs` | [Blazor.Server FormDataObjectTests.cs](../../tests/Bee.Web.Blazor.Server.UnitTests/DataObjects/FormDataObjectTests.cs)（253 行）| 逐字 port（FormDataObject 本身也是逐字 port，測試對等同步） |
| `Controls/DynamicFormTests.cs` | [Blazor.Server DynamicFormTests.cs](../../tests/Bee.Web.Blazor.Server.UnitTests/Components/DynamicFormTests.cs)（93 行）| 結構性 smoke：`BindableProperty` 存在、可實例化、可透過 BindableProperty 設 `Layout` / `DataObject`；對應 Blazor 的 `[Parameter]` attribute 檢查 |
| `Controls/DynamicFormHelperTests.cs` | [Blazor.Server DynamicFormHelperTests.cs](../../tests/Bee.Web.Blazor.Server.UnitTests/Components/DynamicFormHelperTests.cs)（145 行）| MAUI 端 private helper（grid column / span 設定等）via reflection；對應 Blazor 的 `BuildGridStyle` / `BuildFieldStyle` |

### 7.3 為何不引入 Microsoft.Maui.Controls 顯式 PackageReference

`Bee.UI.Maui` 的 `UseMaui=true` 已 implicit 帶入 Microsoft.Maui.Controls；
test project ProjectReference Bee.UI.Maui 的 net10.0 TFM 時，MAUI controls ref assemblies 透過 transitive ProjectReference 可見。
**保持與 Blazor 端對等**（Blazor test project 不需顯式加 `Microsoft.AspNetCore.App` framework reference）。

---

## 8. 風險與緩解

### 8.1 MAUI workload install 拖慢 CI

| 項目 | 估計 |
|------|------|
| `dotnet workload install maui-android` 首次安裝 | ~1.5–3 分鐘 |
| `actions/cache` 命中後 | < 30 秒 |
| Cache miss 頻率 | 僅 .NET SDK 主版本升級時（< 1 次/季） |

**緩解**：cache key `${{ runner.os }}-maui-android-net10` 鎖在 host OS + workload identifier + .NET 主版本，
SDK patch version 升級不會 cache miss。

### 8.2 Strict build 階段對 Linux-only Android TFM 的 analyzer 覆蓋差異

**風險**：Android TFM build 時某些 analyzer rule（特別是 platform-aware analyzer）行為與 net10.0 有差異，
可能漏掉桌面 host 才會觸發的 warning。

**緩解**：
- DynamicForm 內 MAUI 控制項 API 為跨平台共通（`Entry` / `Picker` 等都是 `Microsoft.Maui.Controls.View` 子類），不會有平台分支
- FormDataObject 為純 C# / `System.Data`，不涉 platform-specific API
- 風險主要在未來新增控制項時，本 plan 範圍可控

### 8.3 dependency-map.md 描述需同步調整

**風險**：dependency-map 仍寫 Bee.UI.Maui 為「placeholder skeleton」，與本計畫實作不一致。

**緩解**：本 plan §2.1 已列入 dependency-map.md / .zh-TW.md 同步更新（描述句調整為「動態表單元件首版已實作；尚未對外發版」），mermaid 圖不變。

### 8.4 與 Bee.UI.Core 的雙向相容

**風險**：未來 Bee.UI.Core 若改 multi-target（例如為支援 platform-specific service abstraction），
Bee.UI.Maui 的 ProjectReference 可能 break。

**緩解**：Bee.UI.Core 目前明確為 net10.0 純抽象層（`ClientInfo` / `IEndpointStorage` / `IUIViewService`），
無 platform-specific 需求。若未來真有需求，屆時統一升級兩邊 TFM 即可，不在本 plan 範圍。

---

## 9. 不在本計畫範圍

- **`Bee.UI.Maui` NuGet publish 配置**（`nuget-publish.yml`）— 待真正發版時統一處理（含 macos-latest runner 改造）
- **`DynamicGrid` / `FormPage` 元件**— Blazor 端 Phase 1b 統一觸發時跟進
- **`FormApiConnector` LoadAsync / SaveAsync 對接**— Blazor 端 Phase 1b 才會做，本 plan 同步不超前
- **`Bee.UI.Core` 重構**（`ClientInfo` static state DI 化等）— 獨立任務
- **iOS / Maccatalyst CI 驗證**— 留給 macOS host 本機驗證；CI 僅做 Android smoke build
- **CHANGELOG 撰寫**— 由 `/changelog-draft` 在發版時統一處理

---

## 10. Checklist

實作收尾時逐項勾選：

**csproj + 原始碼**
- [x] `src/Bee.UI.Maui/Bee.UI.Maui.csproj` 改寫為 §3 multi-target MAUI 版本（含 `net10.0`）
- [x] 刪除 `src/Bee.UI.Maui/PlaceholderInfo.cs`
- [x] 新增 `src/Bee.UI.Maui/DataObjects/FormDataObject.cs`（§5）
- [x] 新增 `src/Bee.UI.Maui/Controls/DynamicForm.cs`（§4）

**測試專案**
- [x] 新增 `tests/Bee.UI.Maui.UnitTests/Bee.UI.Maui.UnitTests.csproj`（§7.1）
- [x] 新增 `tests/Bee.UI.Maui.UnitTests/DataObjects/FormDataObjectTests.cs`（從 Blazor.Server port）
- [x] 新增 `tests/Bee.UI.Maui.UnitTests/Controls/DynamicFormTests.cs`（BindableProperty 結構性 smoke）
- [x] 新增 `tests/Bee.UI.Maui.UnitTests/Controls/DynamicFormHelperTests.cs`（private helper reflection）
- [x] `Bee.Library.slnx` tests 區塊加入 `Bee.UI.Maui.UnitTests`

**CI workflow**
- [x] **不動 `.github/workflows/build-ci.yml`**（§6 結論：net10.0 default 不需 maui workload）
- [x] 不動 `Pack NuGet packages` step（不加 Bee.UI.Maui pack 行）
- [x] 不動 `nuget-publish.yml`

**文件**
- [x] `docs/plans/plan-add-bee-ui-maui.md` 狀態列補註 Phase 1 已觸發
- [x] `docs/dependency-map.md` 描述句更新（mermaid 圖不變）
- [x] `docs/dependency-map.zh-TW.md` 對應雙語同步

**驗證**
- [x] 本機（macOS）：`dotnet workload install maui-android maui-ios maui-maccatalyst`（如未裝）
- [x] 本機：`dotnet build Bee.Library.slnx --configuration Release` 通過（0 警告 0 錯誤）
- [x] 本機：既有 `./test.sh` 全部 pass（FormDataObject port 程式碼為逐字 copy，行為應一致；DynamicForm 無單元測試）
- [x] 直接改 main 流程（per `~/.claude/rules/pull-request.md`）：commit + push 到 main 後監看 build-ci.yml 通過

**plan 收尾**
- [x] 本文件頂部狀態列改 `**狀態：✅ 已完成（YYYY-MM-DD）**`
