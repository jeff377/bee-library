# MAUI 規範

本檔記 bee-library 內 MAUI 相關專案（`src/Bee.UI.Maui` + `samples/Maui.Demo`）的硬性規則與已知雷區。建立新 MAUI sample / app 時對著看。

> 配合 `maui-app-scaffold`（global skill）與 `bee-sample-add`（project skill）。樣板已把這些雷預設關掉；本檔解釋「為什麼那樣寫」。

## Sandbox 與 IO

### Mac Catalyst / iOS / 包裝 Windows app 的 `.app` bundle 是 read-only

`Bee.UI.Core.ClientInfo` 預設的 `EndpointStorage` 會把 `ClientSettings` 存在 `Path.Combine(FileUtilities.GetAssemblyPath(), "<entry>.Settings.xml")` —— 對 sandboxed app 來說這是 bundle 內，沒有寫權限。

**規則：** 任何 MAUI host 啟動時，**必須**置換 `IEndpointStorage` 為 sandbox-friendly 版本：

```csharp
// MauiProgram.CreateMauiApp 內、ClientInfo.Initialize 之前
ClientInfo.EndpointStorage = new Bee.UI.Maui.Storage.MauiPreferenceEndpointStorage();
```

`MauiPreferenceEndpointStorage` 把 endpoint 落到 `Microsoft.Maui.Storage.Preferences.Default`，平台會選擇 NSUserDefaults（Apple）/ SharedPreferences（Android）/ Registry（Windows）。

### 任何需要持久化使用者資料的場景

不要用 `FileUtilities.GetAssemblyPath()`、`AppContext.BaseDirectory`。改用：
- `FileSystem.AppDataDirectory`（per-user / per-app，最佳預設）
- `FileSystem.CacheDirectory`（可重建的快取）
- `Preferences.Default`（key-value 偏好）

## csproj 設定

### `<UseMaui>true</UseMaui>` 不再自動帶 `Microsoft.Maui.Controls`

.NET 8+ 改為要求**顯式** PackageReference，否則 build 跳 warning MA002。`TreatWarningsAsErrors=true` 下會 build 失敗：

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Maui.Controls" Version="10.0.20" />
</ItemGroup>
```

### TFM 用條件式，**不**寫死

寫死 `<TargetFrameworks>net10.0-maccatalyst</TargetFrameworks>` 會讓 Windows / Linux 開發者 `dotnet build` 直接退件。標準寫法：

```xml
<TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('osx'))">net10.0-maccatalyst</TargetFrameworks>
<TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">net10.0-windows10.0.19041.0</TargetFrameworks>
<TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('linux'))">net10.0-android</TargetFrameworks>
```

### `MauiFont` / `MauiImage` glob 空時要小心

`<MauiFont Include="Resources\Fonts\*" />` 對應的目錄空著沒事，但**呼叫端**若有：

```csharp
fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
```

runtime 會拋 `FileNotFoundException`。預設樣板**不** register 任何 .ttf，等真的要用 custom font 再把 .ttf 放進 `Resources/Fonts/` + 加 `ConfigureFonts`。

## Apple Release-mode trim 決策樹

`net10.0-maccatalyst` / `net10.0-ios` 在 Release 構建下，Mono linker 會砍未引用的反射相依。對 bee-library 來說，最常踩到的是 `System.Xml.Serialization` 的反射 fallback 被砍 → `XmlCodec.Deserialize<FormSchema>` 拋：

```
Error: XmlSerializeErrorDetails, 2, 2
```

`2, 2` 是 line 2 col 2（XML 的根節點開頭），看起來像 XML 壞掉，**實際上是 type metadata 被砍**。

### 已知不可行的「修法」

| 嘗試 | 結果 |
|------|------|
| `<PublishTrimmed>false</PublishTrimmed>` | MacCatalyst SDK 強制要求 `true`，build 退件 |
| `<MtouchLink>None</MtouchLink>` 單獨 | AOT 編譯不全 → `load_aot_module mismatch` SIGABRT |
| `<UseInterpreter>true</UseInterpreter>` 單獨 | 部分 assembly 仍走 AOT，CoreLib 版號不符 SIGABRT |
| `<MtouchLink>SdkOnly</MtouchLink>` | XmlSerializer 反射仍被砍（SdkOnly 不保護 SDK 自身） |

### 已知可行的修法（依投入成本由低到高）

1. **直接用 Debug 跑**（demo / 開發階段預設）
   - Debug 不做 trim 與 AOT 限制
   - bee-library 所有 MAUI sample 都標明跑 `-c Debug`
   - 缺點：包大、慢、不能直接 ship

2. **`Microsoft.XmlSerializer.Generator` 預編 Sgen 組件**（中度投入）
   - 在 build 期把 XmlSerializer 反射路徑展開成靜態程式碼
   - 需要為 `FormSchema` / `TableSchema` 等所有需序列化的型別加 generator pass
   - 套件名：`dotnet-xmlserializer-generator`（CLI tool）或 `Microsoft.XmlSerializer.Generator` MSBuild target

3. **補 `[DynamicallyAccessedMembers]` 註記**（最高投入但最徹底）
   - 在 `Bee.Definition.Forms` / `Bee.Definition.Tables` 等所有反射目標型別加 `[DynamicallyAccessedMembers(...)]`
   - trimmer 看到註記就保留對應 metadata
   - 影響範圍大，要審慎 review

4. **`TrimmerRootDescriptor` XML 保留特定 type**（特例使用）
   - 寫一份 `Resources/Linker/linker.xml` 把整個 namespace 標為「不砍」
   - 適合「只有少量已知 type 需要保留」的情境
   - 不適合 bee-library 因 FormSchema 子 type 過多

### 當前對策

- 所有 MAUI sample **強制 Debug 跑**（`Maui.Demo/README.md` 已記）
- 未來要 ship 任一 Bee MAUI app 時，走 #2 或 #3，留 ADR 紀錄選擇理由
- `maui-app-scaffold` 的 README 樣板已把這條雷預先寫進

## AOT 與 Interpreter

別把這些設定亂組合：
- `MtouchLink` + AOT 預設組合穩定（SDK 預設）
- `MtouchLink=None` + `UseInterpreter=true` 看起來「都解掉」但 CoreLib AOT 版本仍會不符 → 進一步壞掉
- 全用 Interpreter（`<MtouchInterpreter>-all</MtouchInterpreter>`）理論上可行，但啟動慢、性能差，不推薦

## Resources / Assets

### Icon / Splash 的 SVG 是被 MAUI build pipeline 處理過的

`<MauiIcon Include="...svg" />` 會自動產生各尺寸 `.appiconset`、Android `mipmap`、Windows tile。**不要**自己寫 `Assets.xcassets/appicon.appiconset` —— 會與 MAUI 自動生成的衝突。

### `Color` 屬性給 ASCII hex，不要中文 / 帶 alpha 的格式

```xml
<MauiIcon ... Color="#FFB300" />     ✅
<MauiIcon ... Color="#80FFB300" />   ❌  部分平台不支援 alpha
<MauiIcon ... Color="amber" />       ❌  named color 在 build pipeline 不認
```

## 平台 Stub 檔的必要性

### Mac Catalyst 三件套

每個 MAUI app **必須**有：
- `Platforms/MacCatalyst/AppDelegate.cs` — `[Register("AppDelegate")] : MauiUIApplicationDelegate`
- `Platforms/MacCatalyst/Program.cs` — `UIApplication.Main(args, null, typeof(AppDelegate))`
- `Platforms/MacCatalyst/Info.plist` — bundle id、orientation、ATS 等
- `Platforms/MacCatalyst/Entitlements.plist` — sandbox + network client（demo / 內部工具至少要 network.client）

少任何一個，build 會出奇怪錯誤（最常是「nothing found for entry point」）。

### iOS / Android

預設 `MauiAppFullPlatforms=true` 時 MAUI SDK 會自動產 `Platforms/iOS/` 與 `Platforms/Android/` 必要 stub。**只在使用者明確要這些平台時**才打開。

## 與 bee-library 整合

### 客製 `IEndpointStorage` 已備好

`Bee.UI.Maui.Storage.MauiPreferenceEndpointStorage` 已在 lib 內。MAUI host 直接：

```csharp
ClientInfo.EndpointStorage = new MauiPreferenceEndpointStorage();
```

### `Bee.UI.Maui.Controls.FormPage` 的 fallback 行為

只設 `ProgId` 時，FormPage 會從 `ClientInfo.SystemApiConnector.GetDefineAsync<FormSchema>` 取 schema、`ClientInfo.CreateFormApiConnector(progId)` 取 connector、`ClientInfo.AccessToken` 取 token。

開發 MAUI sample 想跳過 fallback 自己接 → 直接設 `FormPage.Schema` + `FormPage.FormConnector`。

### `Bee.Samples.Shared` 不能被 MAUI sample 引用

它有 `<FrameworkReference Include="Microsoft.AspNetCore.App" />`，MAUI csproj 沒有 AspNetCore framework，會 build 失敗。

要分享常數（如 `DemoCredentials.UserId = "demo"`） → MAUI sample 內 hardcode，或未來把常數類拆出去獨立 `Bee.Samples.Shared.Auth` 之類純 net10.0 lib（尚未做）。

## 參考檔案

- `src/Bee.UI.Maui/Bee.UI.Maui.csproj` — 對應 sample csproj 的「lib 端鏡像」（library 走 `net10.0` 預設、`BeeUiMauiFullPlatforms=true` 才加平台 TFM）
- `samples/Maui.Demo/Maui.Demo.csproj` — sample 端標準寫法（host 端 + Platforms 全套）
- `samples/Maui.Demo/README.md` — 跑法 + Debug 模式說明
- `src/Bee.UI.Maui/Storage/MauiPreferenceEndpointStorage.cs` — sandbox-friendly storage 實作
