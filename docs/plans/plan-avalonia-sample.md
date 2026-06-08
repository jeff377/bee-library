# 計畫：新增 Avalonia UI sample（鏡像 Maui.Demo）

**狀態：📝 擬定中**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | `src/Bee.UI.Avalonia` library 骨架（csproj + EndpointStorage + FormDataObject 重用方案） | 📝 待做 |
| 2 | `DynamicForm`（master section renderer，鏡像 MAUI 端 5 種 ControlType） | 📝 待做 |
| 3 | `DynamicGrid` + `FormView`（明細/列表 DataGrid + 含 New/Save/Delete toolbar 的容器） | 📝 待做 |
| 4 | `samples/Avalonia.Demo` 骨架（Program / App / MainWindow + Connection / Login / Employee 三 View） | 📝 待做 |
| 5 | README + `.smoke.yaml` + `Bee.Samples.slnx` 整合 + 端到端冒煙 | 📝 待做 |

---

## 背景

`samples/Maui.Demo` 已示範「同一份 `FormSchema` 在 MAUI 桌面/行動端動態渲染」，且後端走 `QuickStart.Server`（JSON-RPC, `http://localhost:5050/api`），通過 `system.ping` → `system.login` → `Employee.GetData` 三段流程。

使用者已確認新增的 Avalonia sample：
- **位置**：`samples/Avalonia.Demo`，與 `Maui.Demo` 平行
- **後端**：直接打 `QuickStart.Server`，不再拉新後端
- **渲染策略**：`FormLayout` 為控件樹唯一 source of truth，`DataTable` / `DataRowView` 只當 row 容器；Avalonia binding 系統負責雙向同步（不自己寫 observer）

## 目標

1. 補出 `src/Bee.UI.Avalonia` 作為 Avalonia 端的「`Bee.UI.Maui` 對應物」，含 `DynamicForm` / `DynamicGrid` / `FormView` / `IEndpointStorage` 實作
2. 補出 `samples/Avalonia.Demo` 作為 sample 入口，跑得通 Connection → Login → Employee 流程
3. 不動 `tools/DefineEditor`（既有 Avalonia 工具，與 sample 用途無關）
4. 與既有 MAUI / Blazor / QuickStart sample 走同一份 `samples/Define/` 定義檔，**不複製**

## 範圍

### In scope

- `src/Bee.UI.Avalonia/Bee.UI.Avalonia.csproj`（新增）
- `src/Bee.UI.Avalonia/Controls/{FormView,DynamicForm,DynamicGrid}.cs`（新增）
- `src/Bee.UI.Avalonia/Storage/{name}EndpointStorage.cs`（新增；命名見「設計決策 D2」）
- `samples/Avalonia.Demo/`：完整骨架（csproj、Program.cs、App.axaml(+cs)、MainWindow.axaml(+cs)、Views/、ViewModels/、Assets/、README、.smoke.yaml）
- `samples/Bee.Samples.slnx`：把 Avalonia.Demo 加進去
- `samples/README.md`：補一筆 Avalonia.Demo 對應行（如有總表）

### Out of scope

- `tools/DefineEditor` 任何改動（與 sample 無關，git status 中的改動屬於使用者自己的進行中工作）
- 重構 `Bee.UI.Maui.DataObjects.FormDataObject` → 共用 `Bee.UI.Core` 版本（見「設計決策 D3」，先複製、後續再共用）
- 新後端 / 新 BO（直接打 `QuickStart.Server` 既有 `Employee` BO）
- 真正的多視窗 / 多 tab IDE 風格（這次只做單視窗 + 三 View 路由）
- Release-mode trim 與打包（與 MAUI 同樣理由：debug 跑通即可，正式發布另案）

## 設計決策

| 編號 | 決策點 | 推薦方案 | 替代方案 / 註記 |
|------|--------|----------|----------------|
| D1 | library 命名 | **`Bee.UI.Avalonia`** | 對齊 `Bee.UI.Maui` 命名規律 |
| D2 | sample 命名 | **`Avalonia.Demo`**（資料夾、csproj、namespace 一致） | 對齊 `Maui.Demo` |
| D3 | `FormDataObject` 是否共用 | **先複製到 `Bee.UI.Avalonia.DataObjects`，不動 `Bee.UI.Maui` 端**；後續再評估抽到 `Bee.UI.Core` | 移到 Core 會牽動 MAUI / Blazor，先以「鏡像」最小化爆炸半徑 |
| D4 | EndpointStorage 實作 | **`FileEndpointStorage`** — 寫 `Environment.SpecialFolder.LocalApplicationData/Bee.Avalonia.Demo/endpoint.xml`，跨平台一致；放 `src/Bee.UI.Avalonia/Storage/` | MAUI 端用 `Preferences`，Avalonia 沒對等 API；用檔案最直白 |
| D5 | Avalonia 版本 | **與 `tools/DefineEditor` 一致**（目前 `Avalonia 12.0.4`） | 避免兩個 Avalonia 專案撞版本 |
| D6 | MVVM pattern | **走 axaml + ViewModel + ReactiveUI / CommunityToolkit.Mvvm**（與 `tools/DefineEditor` 同套） | MAUI 端是純 code-behind；Avalonia 端 axaml 是慣例，跟既有專案對齊比較自然 |
| D7 | `Bee.UI.Avalonia` 目標框架 | **`net10.0`**，無條件式 TFM | MAUI 端有條件式是因為 platform TFM；Avalonia 是純桌面，單一 TFM 即可 |
| D8 | `Avalonia.Demo` 目標框架 | **`net10.0`**，`OutputType=WinExe`，加 `app.manifest`（Windows DPI awareness） | 跨平台桌面，不需 platform TFM |
| D9 | DataGrid 來源 | **`Avalonia.Controls.DataGrid` 套件**（官方獨立發佈，非預設） | 需顯式 PackageReference |
| D10 | 控件 ↔ `DataRowView` 雙向 binding 路徑 | `DataGrid.ItemsSource = DataTable.DefaultView` + `Binding "[FieldName]"`；單筆 master 用 `ContentControl.DataContext = DataRowView` + 程式碼動態建子控件 | 與「FormLayout 是 source of truth」相容；Avalonia binding 引擎讀 `DataRowView` 的 indexer + INPC 通知 |

## 階段細節

### Phase 1 — `Bee.UI.Avalonia` library 骨架

**新增檔案：**

```
src/Bee.UI.Avalonia/
├── Bee.UI.Avalonia.csproj
├── Storage/
│   └── FileEndpointStorage.cs
└── DataObjects/
    └── FormDataObject.cs              (從 Bee.UI.Maui 複製，去掉 MAUI 依賴)
```

**csproj 樣板（待 phase 1 內定案）：**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <Description>Avalonia desktop control library for Bee.NET (FormSchema-driven UI for Windows / macOS / Linux).</Description>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia" Version="12.0.4" />
    <PackageReference Include="Avalonia.Controls.DataGrid" Version="12.0.4" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Bee.UI.Core\Bee.UI.Core.csproj" />
    <ProjectReference Include="..\Bee.Api.Client\Bee.Api.Client.csproj" />
    <ProjectReference Include="..\Bee.Definition\Bee.Definition.csproj" />
  </ItemGroup>
</Project>
```

**`FormDataObject` 複製注意事項：**

MAUI 版（`src/Bee.UI.Maui/DataObjects/FormDataObject.cs`）只引用 `System.Data` / `Bee.Api.Client` / `Bee.Base` / `Bee.Definition`，**不依賴任何 MAUI 型別**。直接複製檔案到 `src/Bee.UI.Avalonia/DataObjects/FormDataObject.cs`、改 namespace 即可。複製不抽共用的理由：本階段以最低改動 + 最小爆炸半徑為主，後續可在獨立 plan 中把它抽到 `Bee.UI.Core`。

**`FileEndpointStorage`：**

- 存檔路徑：`Path.Combine(Environment.GetFolderPath(SpecialFolder.LocalApplicationData), "Bee.Avalonia.Demo", "endpoint.xml")`
- XML 序列化（用既有 `XmlCodec`，與框架其他地方一致）
- 實作 `IEndpointStorage` 即可，無需介面變動

### Phase 2 — `DynamicForm`

照搬 MAUI `DynamicForm` 的 `BuildSection → BuildFieldGrid → BuildFieldCell → BuildInputControl` 結構，把 MAUI 控件改成 Avalonia 對等：

| MAUI 控件 | Avalonia 對等 | 事件 |
|-----------|--------------|------|
| `Grid` + `RowDefinition/ColumnDefinition` | `Grid` + `RowDefinitions/ColumnDefinitions`（字串 syntax 也可） | — |
| `Border` | `Border` | — |
| `Label` | `TextBlock` | — |
| `VerticalStackLayout` | `StackPanel` (`Orientation="Vertical"`) | — |
| `CheckBox` | `CheckBox` | `IsCheckedChanged` |
| `DatePicker` | `DatePicker`（Avalonia 12 有內建） | `SelectedDateChanged` |
| `Editor`（multi-line） | `TextBox` with `AcceptsReturn="true"` `TextWrapping="Wrap"` | `TextChanged` |
| `Picker` | `ComboBox` | `SelectionChanged` |
| `Entry`（single-line） | `TextBox` | `TextChanged` |

binding 策略：本階段**先沿用 MAUI 的「事件驅動」風格**（控件 `TextChanged` → 呼叫 `DataObject.SetField`），確保行為與 MAUI 端一致、可直接對照。Avalonia 的 indexer binding（`{Binding [FieldName]}`）放到 phase 3 與 `DynamicGrid` 一起導入。

### Phase 3 — `DynamicGrid` + `FormView`

**`DynamicGrid`：**

- 容器：`UserControl` 包 `DataGrid`
- 兩個 BindableProperty（Avalonia 用 `StyledProperty<T>`）：`ListLayout`（型別 `LayoutGrid`）、`DataTable`
- `DataGrid.ItemsSource = DataTable.DefaultView` — 走 `DataView` 路徑
- 欄位來源：`ListLayout.Columns` 動態產 `DataGridTextColumn`（或依 `ControlType` 改用 `DataGridCheckBoxColumn` 等）
- Binding：`new Binding($"[{column.FieldName}]") { Mode = TwoWay }`
- 列選取事件：`SelectionChanged` → 取出選中 row 的 `sys_rowid` → 對外 `event EventHandler<Guid> RowSelected`

**`FormView`：**

- `UserControl` 容器，含 toolbar（`Button` × 3：New / Save / Delete） + `DynamicForm` + `DynamicGrid`
- StyledProperty：`ProgId`、`Schema`、`FormConnector`、`AccessToken`
- 沿用 MAUI `FormPage` 的 fallback 邏輯：只給 `ProgId` 時自動從 `ClientInfo.SystemApiConnector.GetDefineAsync<FormSchema>` 取 schema、`ClientInfo.CreateFormApiConnector(ProgId)` 取 connector、`ClientInfo.AccessToken` 取 token
- 內部組裝 `FormDataObject` → 接到 `DynamicForm` / `DynamicGrid`
- 三顆按鈕對應 `_dataObject.NewAsync()` / `SaveAsync()` / `DeleteAsync()`，並在動作後 `Refresh()`

### Phase 4 — `samples/Avalonia.Demo` 骨架

**檔案結構：**

```
samples/Avalonia.Demo/
├── Avalonia.Demo.csproj
├── Program.cs                         (AppBuilder.Configure<App>().UsePlatformDetect().StartWithClassicDesktopLifetime)
├── App.axaml + App.axaml.cs           (Application；OnFrameworkInitializationCompleted 開 MainWindow)
├── app.manifest                       (Windows DPI awareness)
├── ViewModels/
│   ├── MainWindowViewModel.cs         (頁面切換狀態：Connection → Login → Employee)
│   ├── ConnectionViewModel.cs
│   ├── LoginViewModel.cs
│   └── EmployeeViewModel.cs           (薄薄一層，主要交給 FormView)
├── Views/
│   ├── MainWindow.axaml(+cs)
│   ├── ConnectionView.axaml(+cs)
│   ├── LoginView.axaml(+cs)
│   └── EmployeeView.axaml(+cs)        (內含 <bee:FormView ProgId="Employee" />)
├── Assets/
│   └── (icon 等)
├── README.md
├── README.zh-TW.md
└── .smoke.yaml
```

**csproj 樣板：**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>WinExe</OutputType>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>Avalonia.Demo</RootNamespace>
    <AssemblyName>Avalonia.Demo</AssemblyName>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <ApplicationManifest>app.manifest</ApplicationManifest>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia" Version="12.0.4" />
    <PackageReference Include="Avalonia.Desktop" Version="12.0.4" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="12.0.4" />
    <PackageReference Include="Avalonia.Controls.DataGrid" Version="12.0.4" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Bee.UI.Avalonia\Bee.UI.Avalonia.csproj" />
  </ItemGroup>
</Project>
```

**`Program.cs` bootstrap：**

```csharp
public static void Main(string[] args)
{
    ApiClientInfo.ApiKey = "avalonia-demo";
    ApiClientInfo.SupportedConnectTypes = SupportedConnectTypes.Remote;
    ClientInfo.EndpointStorage = new FileEndpointStorage("Bee.Avalonia.Demo");

    BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
}

public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
```

### Phase 5 — README / `.smoke.yaml` / `.slnx` / 冒煙

- `samples/Avalonia.Demo/README.md` + `README.zh-TW.md`：照 `Maui.Demo` 樣板，列前置條件（QuickStart.Server 要先跑）、啟動指令（`dotnet run -c Debug --project samples/Avalonia.Demo`）、預期畫面
- `samples/Avalonia.Demo/.smoke.yaml`：對齊 `Maui.Demo` 規格（demo-smoke skill 讀取）
- `samples/Bee.Samples.slnx`：加 `<Folder Name="/Avalonia/">` + `<Project Path="Avalonia.Demo/Avalonia.Demo.csproj" />`
- 跑一次 `./test.sh src/Bee.UI.Avalonia` + `dotnet build samples/Avalonia.Demo`，確認綠
- （可選）跑 `demo-smoke` 對 Avalonia.Demo 做一次 UI 點擊冒煙

## 風險與雷區

| 風險 | 機率 | 影響 | 對策 |
|------|------|------|------|
| Avalonia DataGrid 對 `DataRowView` 的 indexer binding 行為與 WPF 略有差異 | 中 | 中 | Phase 3 先在 spike 階段跑通最小例子；若 indexer 路徑失靈，退而用 code-behind 串 `CellEditEnded` 事件手動寫回 `DataRowView`（與 MAUI 同款做法） |
| `Avalonia.Controls.DataGrid` 套件與 `Avalonia` 主套件版本必須一致 | 中 | 低 | csproj 用相同 `Version` 字串；CI build 會抓到 |
| `tools/DefineEditor` 與 `Bee.UI.Avalonia` 都依 Avalonia，版本不一致 | 中 | 中 | D5 鎖在 `12.0.4`；後續若 DefineEditor 升版，同步升 `Bee.UI.Avalonia` |
| Release 模式 trim 把 `System.Xml.Serialization` 反射砍掉（MAUI 已知雷） | 高 | 低（本 plan 不在 scope） | README 註明只支援 Debug 跑；正式發布另案 |
| `Bee.Samples.slnx` 載入 Avalonia.Demo 在無 Avalonia workload 的環境（無此事，Avalonia 純 NuGet 套件） | 低 | 低 | 與 MAUI 不同，Avalonia 無 workload；any net10.0 SDK 都能還原 |
| `FormDataObject` 複製造成兩份維護成本 | 中 | 低 | 已標於「Out of scope」；後續抽到 `Bee.UI.Core` 走獨立 plan |
| `MauiPreferenceEndpointStorage` 與新的 `FileEndpointStorage` 路徑不同，跨 sample 不會共用 endpoint 設定 | 低 | 低 | 預期行為（每個 sample 是獨立 user-scope 設定），README 註明 |

## 已拍板的決策（2026-06-08）

| 編號 | 決策 |
|------|------|
| D2 | sample 命名 **`Avalonia.Demo`**（與 `Maui.Demo` / `Blazor.Server.Demo` 同樣以 framework 命名） |
| D3 | `FormDataObject` **先複製**到 `Bee.UI.Avalonia.DataObjects`，不動 MAUI 端；抽到 `Bee.UI.Core` 走獨立 plan |
| D6 | MVVM 採 **axaml + ViewModel + CommunityToolkit.Mvvm**（與 `tools/DefineEditor` 對齊、Avalonia 慣例） |

## 驗收

- [ ] `dotnet build src/Bee.UI.Avalonia/Bee.UI.Avalonia.csproj -c Release` 通過
- [ ] `dotnet build samples/Avalonia.Demo/Avalonia.Demo.csproj -c Debug` 通過
- [ ] 啟動 `QuickStart.Server`，再跑 `dotnet run --project samples/Avalonia.Demo`：能完成 Connection → Login → Employee 三頁流程，至少能看到 `Employee` 列表的 3 列 seed data（Alice / Bob / Carol）
- [ ] `samples/Bee.Samples.slnx` 包含 Avalonia.Demo 且能被 `dotnet sln list`（或 slnx 對等指令）讀到
- [ ] README 中英雙語齊備
- [ ] CI（`build-ci.yml`）通過：CI 路徑 filter 含 `src/` `samples/`（待確認，見 memory「CI path filter」）
