# 計畫：抽出 `Bee.Hosting` 套件作為 DI Composition Root

**狀態：✅ 已完成（2026-05-13）**

## 背景

`AddBeeFramework`（`IServiceCollection` 擴充）目前定義於 `Bee.Api.AspNetCore.BeeFrameworkServiceCollectionExtensions`。
[src/Bee.Api.AspNetCore/BeeFrameworkServiceCollectionExtensions.cs:38](../../src/Bee.Api.AspNetCore/BeeFrameworkServiceCollectionExtensions.cs#L38) 方法本體只用 `IServiceCollection` 與自家 Bee.* 組件，**完全不使用任何 ASP.NET Core 型別**。
但因該方法所在的組件還包含 `IApplicationBuilder.UseBeeFramework` 與 `ApiServiceController`，整個 `Bee.Api.AspNetCore` 強制透過 `<FrameworkReference Include="Microsoft.AspNetCore.App" />` 拖入整個 ASP.NET Core 框架。

連帶造成下列問題：

1. **非 ASP.NET Core 宿主**（WinForms / WPF / Console / Worker Service / 整合測試）無法呼叫 `AddBeeFramework`，除非接受拖入 ASP.NET Core 框架（不合理）
2. **bee-ui-core 的近端連線場景**：bee-ui-core 本身只 ref `Bee.Api.Client`，不能 ref 後端組件；改由「宿主應用」呼叫 `AddBeeFramework` 並把 `IServiceProvider` 透過 [ApiClientInfo.LocalServiceProvider](../../src/Bee.Api.Client/ApiClientInfo.cs#L48) 注入。當宿主是 WinForms 桌面應用時，「為了呼叫 `AddBeeFramework` 而 ref `Bee.Api.AspNetCore`」明顯不對
3. **`tests/Bee.Tests.Shared`** 測試基礎建設目前透過 ref `Bee.Api.AspNetCore` 拿 `AddBeeFramework`，導致純單元測試也背 ASP.NET Core

## 目標

把「DI 註冊」從「ASP.NET Core 整合」切開：

- 新增 `Bee.Hosting` 套件作為 **composition root**，只負責 `IServiceCollection.AddBeeFramework`，不依賴 ASP.NET Core
- `Bee.Api.AspNetCore` 改 ref `Bee.Hosting`，保留 `UseBeeFramework` middleware hook 與 `ApiServiceController`，繼續對 web host 提供完整整合
- 非 ASP.NET Core 宿主直接 ref `Bee.Hosting` 即可，不再被 ASP.NET Core 牽連
- 不更動 `Bee.Api.Client` / `ApiClientInfo.LocalServiceProvider` 反射機制，bee-ui-core 與宿主之間的契約完全不變

## 範圍

### 包含

- 新增 `src/Bee.Hosting/` 專案
- 搬 `BeeFrameworkServiceCollectionExtensions.cs` 至新組件，namespace 改 `Bee.Hosting`
- `Bee.Api.AspNetCore` 改 ref `Bee.Hosting`，砍掉 4 個原有 ProjectReference
- 新增 `tests/Bee.Hosting.UnitTests/`，搬 `BeeFrameworkServiceCollectionExtensionsTests.cs` 過去
- `tests/Bee.Tests.Shared` 改 ref `Bee.Hosting`（不再透過 `Bee.Api.AspNetCore`）
- 更新 XML 註解、README、ADR 中所有指向 `Bee.Api.AspNetCore.BeeFrameworkServiceCollectionExtensions` 的字串
- bump 版本至 **5.0.0**，CHANGELOG 標 breaking change

### 不包含

- 不動 `Bee.Api.Client` / `LocalApiProvider` / `ApiClientInfo`（既有近端連線機制不變）
- 不動 `UseBeeFramework`、`ApiServiceController`（仍在 Bee.Api.AspNetCore）
- 不為 `AddBeeFramework` 在 `Bee.Api.AspNetCore` 保留 thin facade（採方案 A：直接 break）
- 不引入 `IHostedService` / `IHostBuilder` 整合（未來如需要再評估）

## 設計

### 相依關係

```
Bee.Hosting （新增）
  ├── Bee.Api.Core
  ├── Bee.Business
  ├── Bee.Repository
  └── Bee.ObjectCaching
      （Bee.Definition / Bee.Base / Bee.Db / Bee.Repository.Abstractions / Bee.Api.Contracts
        皆由上述 4 個 transitively 帶入）

Bee.Api.AspNetCore （改）
  ├── FrameworkReference: Microsoft.AspNetCore.App
  └── ProjectReference:
        └── Bee.Hosting           ← 取代原本的 4 個 ProjectReference
```

### 4 個 ProjectReference 的依據

| ProjectReference | 直接帶入 namespace | Transitive 帶入 |
|------------------|---------------------|------------------|
| Bee.Api.Core | `Bee.Api.Core.JsonRpc` (JsonRpcExecutor) | Bee.Api.Contracts、Bee.Definition、Bee.Base |
| Bee.Business | `Bee.Business`、`Bee.Business.Providers` | Bee.Repository.Abstractions、Bee.Api.Contracts、Bee.Definition |
| Bee.Repository | （編譯期無 using，但執行期反射要載入 `SystemRepositoryFactory` / `FormRepositoryFactory`） | Bee.Db、Bee.Repository.Abstractions、Bee.Definition |
| Bee.ObjectCaching | `Bee.ObjectCaching` | Bee.Definition |

Bee.Repository 看似無編譯期 using，但 [BeeFrameworkServiceCollectionExtensions.cs:122-126](../../src/Bee.Api.AspNetCore/BeeFrameworkServiceCollectionExtensions.cs#L122-L126) 透過 `AssemblyLoader.GetType()` 反射載入其內具體型別 —— 必須保證 `Bee.Repository.dll` 出現在下游輸出目錄，所以列為 ProjectReference 而非依賴呼叫端自行補。

## 詳細步驟

### Step 1：建立 `src/Bee.Hosting/`

新增檔案：

- `src/Bee.Hosting/Bee.Hosting.csproj`
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <TargetFramework>net10.0</TargetFramework>
      <Nullable>enable</Nullable>
      <ImplicitUsings>enable</ImplicitUsings>
      <Description>Composition root for the Bee.NET framework. Registers backend services in any IServiceCollection — usable from ASP.NET Core, WinForms/WPF, Console, Worker Service, and integration test hosts.</Description>
      <PackageTags>bee.net;di;composition;hosting;backend</PackageTags>
    </PropertyGroup>

    <ItemGroup>
      <ProjectReference Include="..\Bee.Api.Core\Bee.Api.Core.csproj" />
      <ProjectReference Include="..\Bee.Business\Bee.Business.csproj" />
      <ProjectReference Include="..\Bee.Repository\Bee.Repository.csproj" />
      <ProjectReference Include="..\Bee.ObjectCaching\Bee.ObjectCaching.csproj" />
    </ItemGroup>

    <ItemGroup>
      <InternalsVisibleTo Include="Bee.Hosting.UnitTests" />
    </ItemGroup>
  </Project>
  ```
- `src/Bee.Hosting/README.md` + `README.zh-TW.md`（簡述用途、`AddBeeFramework` 使用方式、與 `Bee.Api.AspNetCore` 的關係）

### Step 2：搬 `BeeFrameworkServiceCollectionExtensions.cs`

從 `src/Bee.Api.AspNetCore/BeeFrameworkServiceCollectionExtensions.cs` 搬到 `src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs`：

- namespace `Bee.Api.AspNetCore` → **`Bee.Hosting`**
- 內容不動（含所有 private helper：`ResolveDefineAccess`、`CreateDefineStorage`、`CreateConfigurableService`、`CreateApiEncryptionKeyProvider`、`CreateBusinessObjectFactory`、`CreateOrDefault`、`DecryptSecurityKeys`、`SecurityKeys` record）
- 刪除 `src/Bee.Api.AspNetCore/BeeFrameworkServiceCollectionExtensions.cs`

### Step 3：精簡 `Bee.Api.AspNetCore.csproj`

```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="..\Bee.Hosting\Bee.Hosting.csproj" />
  <!-- 原本 4 個 ref 全刪：Bee.Api.Core、Bee.Business、Bee.Db、Bee.ObjectCaching、Bee.Repository -->
</ItemGroup>
```

剩餘檔案：
- `BeeFrameworkApplicationBuilderExtensions.cs`（`UseBeeFramework`）
- `Controllers/ApiServiceController.cs`
- `README.md` / `README.zh-TW.md`

### Step 4：建立 `tests/Bee.Hosting.UnitTests/`

新增 `tests/Bee.Hosting.UnitTests/Bee.Hosting.UnitTests.csproj`，相依：

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\Bee.Hosting\Bee.Hosting.csproj" />
  <ProjectReference Include="..\Bee.Tests.Shared\Bee.Tests.Shared.csproj" />
</ItemGroup>
```

把 `tests/Bee.Api.AspNetCore.UnitTests/BeeFrameworkServiceCollectionExtensionsTests.cs` 整檔搬到 `tests/Bee.Hosting.UnitTests/`：

- namespace 改 `Bee.Hosting.UnitTests`
- using 加 `using Bee.Hosting;`
- 3 個 `[Fact]` 方法內容不動

### Step 5：`tests/Bee.Tests.Shared` 改 ref `Bee.Hosting`

[Bee.Tests.Shared.csproj](../../tests/Bee.Tests.Shared/Bee.Tests.Shared.csproj)：

- 移除：`<ProjectReference Include="..\..\src\Bee.Api.AspNetCore\..." />`
- 新增：`<ProjectReference Include="..\..\src\Bee.Hosting\Bee.Hosting.csproj" />`
- `BeeTestFixtureBuilder.cs:95` 與 `TestProcessBootstrap.cs:77` 加 `using Bee.Hosting;`

驗證：`Bee.Tests.Shared` 編譯後不再 transitive 載入 `Microsoft.AspNetCore.App`。

### Step 6：`tests/Bee.Api.AspNetCore.UnitTests` 維持其它測試

保留：
- `ApiAspNetCoreTests.cs`
- `ApiServiceControllerIsDevelopmentTests.cs`
- `ApiServiceControllerTests.cs`
- `BeeFrameworkApplicationBuilderExtensionsTests.cs`

刪除：
- `BeeFrameworkServiceCollectionExtensionsTests.cs`（已搬至 Bee.Hosting.UnitTests）

### Step 7：更新文件與註解引用

逐檔把 `Bee.Api.AspNetCore.BeeFrameworkServiceCollectionExtensions.AddBeeFramework` 字串改為 `Bee.Hosting.BeeFrameworkServiceCollectionExtensions.AddBeeFramework`（XML 註解 `<see cref="..."/>` 同步調整）：

- `src/Bee.Api.Client/ApiClientInfo.cs:6`
- `src/Bee.Api.AspNetCore/BeeFrameworkApplicationBuilderExtensions.cs:7`
- `src/Bee.Db/Manager/DbConnectionManagerService.cs:16`
- `src/Bee.Db/Manager/IDbConnectionManager.cs:12`
- `src/Bee.Definition/SystemSettingsLoader.cs:13,26`
- `src/Bee.Definition/Database/DbCategoryIds.cs:23`
- `src/Bee.Definition/README.md` + `README.zh-TW.md`（行 29 / 55）
- `src/Bee.ObjectCaching/CacheContainerService.cs`、`CacheInfo.cs`、`ICacheContainer.cs`
- `docs/adr/adr-011-di-replaces-service-locator.md:19`
- `docs/plans/plan-backendinfo-to-di-migration.md:257`（不動 archive，保留歷史脈絡）

### Step 8：bump 版本至 5.0.0

`src/Directory.Build.props`：

```xml
<Version>5.0.0</Version>
<AssemblyVersion>5.0.0.0</AssemblyVersion>
<FileVersion>5.0.0.0</FileVersion>
```

### Step 9：CHANGELOG / Migration Guide

`CHANGELOG.md`（若不存在則新建）寫明：

```markdown
## 5.0.0

### Breaking Changes

- **`AddBeeFramework` 已從 `Bee.Api.AspNetCore` 搬移至新套件 `Bee.Hosting`。**
  - 命名空間從 `Bee.Api.AspNetCore` 改為 `Bee.Hosting`
  - ASP.NET Core 宿主：透過 `Bee.Api.AspNetCore` transitively 取得，需在啟動程式加上 `using Bee.Hosting;`
  - 非 ASP.NET Core 宿主（WinForms / WPF / Console / Worker）：直接 ref `Bee.Hosting` 即可，不再需要拖入 `Microsoft.AspNetCore.App`

### Migration

- web host：
  ```csharp
  using Bee.Hosting;          // ← 新增
  using Bee.Api.AspNetCore;

  services.AddBeeFramework(cfg, paths);
  app.UseBeeFramework();
  ```
- desktop / console host：
  ```csharp
  // 將 PackageReference 從 Bee.Api.AspNetCore 改為 Bee.Hosting
  using Bee.Hosting;

  var services = new ServiceCollection();
  services.AddBeeFramework(cfg, paths);
  var sp = services.BuildServiceProvider();

  // 透過 ApiClientInfo 注入給 bee-ui-core 近端連線
  ApiClientInfo.LocalServiceProvider = sp;
  ApiClientInfo.ConnectType = ConnectType.Local;
  ```
```

## 驗收標準

- [ ] `dotnet build --configuration Release` 全 src 編譯通過（含 `Bee.Hosting`、修改後的 `Bee.Api.AspNetCore`）
- [ ] `./test.sh` 全測試通過（含新 `Bee.Hosting.UnitTests` 與其餘專案）
- [ ] `Bee.Tests.Shared` 編譯後輸出目錄**不含** `Microsoft.AspNetCore.*.dll`（驗證測試基礎建設已脫離 ASP.NET Core）
- [ ] `Bee.Hosting` 編譯後輸出目錄**不含** `Microsoft.AspNetCore.*.dll`
- [ ] `Bee.Hosting` 編譯後輸出目錄**包含** `Bee.Repository.dll`（保證執行期反射載入）
- [ ] `dotnet pack` 產出 `Bee.Hosting.5.0.0.nupkg` 與所有現有套件 5.0.0
- [ ] `grep -rn "Bee.Api.AspNetCore.BeeFrameworkServiceCollectionExtensions" src/ docs/adr/ docs/plans/plan-backendinfo-to-di-migration.md`（不含 docs/archive/） 0 個結果
- [ ] ADR-011 文字更新為 `Bee.Hosting` 提供入口

## 風險與緩解

| 風險 | 影響 | 緩解 |
|------|------|------|
| 外部 host 升級漏改 `using Bee.Hosting;` | 編譯錯誤 `AddBeeFramework not found` | 5.0 主版本 bump + CHANGELOG 明示，IDE「Add using」會主動提示 |
| `Bee.Repository.dll` 在某些 host 部署環境漏 copy | 反射載入 `SystemRepositoryFactory` 失敗 | Bee.Hosting 透過 ProjectReference 強制帶入，MSBuild 自動 copy；驗收標準明列 |
| bee-ui-core 既有用戶誤以為要 ref Bee.Hosting | 不必要的相依擴散 | README 寫清楚「bee-ui-core 只 ref Bee.Api.Client；`AddBeeFramework` 由宿主呼叫」 |
| ADR-011 / 公開文件殘留舊 namespace | 開發者誤導 | Step 7 一次性 sweep；驗收標準 `grep` 0 結果 |

## 後續可能擴充（不在本計畫範圍）

- 若日後需要 `IHostedService`（cache 預熱、scheduled job、graceful shutdown）整合，可在 `Bee.Hosting` 內新增 `IHostBuilder.UseBeeFramework()` 擴充
- 若 bee-ui-core 提供「ConfigureLocalConnection(IServiceProvider)」之類的便利 helper，由 bee-ui-core 自己加，不需動 bee-library
