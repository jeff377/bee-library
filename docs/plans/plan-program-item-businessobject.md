# 計畫：ProgramItem 加 BusinessObject 屬性，ProgramSettings 升格為 ProgId → BO 綁定表

**狀態：✅ 已完成（2026-05-28）**

> **2026-05-28 命名修訂**：原 plan 與初版實作使用屬性名 `TypeName`,review 後改為 `BusinessObject` 以對齊 `BackendComponents` 慣例(角色名不加 `TypeName` 後綴)。檔案也由 `plan-program-item-typename.md` 重新命名為 `plan-program-item-businessobject.md`。本文內所有 `BusinessObject` 字樣對應該決定;歷史脈絡見「實作偏離說明 §偏離 5」。

## 背景

`ProgramSettings` 目前的定位是「ProgId 註冊表」——`ProgramCategory` 載分類、`ProgramItem` 載單一功能（ProgId + DisplayName），落地為 `ProgramSettings.xml`，由 [`IDefineAccess.GetProgramSettings()`](../../src/Bee.Definition/Storage/IDefineAccess.cs) 取得。

但 ProgId 還缺了一塊資訊：**它對應的 BO 類別**。框架目前用 [`IFormBoTypeResolver`](../../src/Bee.Business/IFormBoTypeResolver.cs) 抽象「progId → BO Type」的對應，預設 [`DefaultFormBoTypeResolver`](../../src/Bee.Business/IFormBoTypeResolver.cs) 永遠回 `FormBusinessObject`。介面註解早就明寫：

> ERP applications can install a custom resolver (e.g. one backed by an XML mapping file) to dispatch progId to a specific BO subclass.

`ProgramSettings.xml` 就是那份「XML mapping file」的天然落點：它本來就是 ProgId 的註冊表，「ProgId 對應到哪個 BO」屬於同一張表的另一欄。

### 為什麼需要這層綁定

共用的 `FormBusinessObject` 只處理通用流程（FormSchema 驅動的 GetList / GetData / SaveData / Delete 等）。當特定表單需要：

- 覆寫驗證規則（例：訂單明細交叉檢查）
- 觸發領域事件（例：簽核流程啟動）
- 客製 SQL（例：報表類 ProgId 走 AnyCode Repository）

就必須繼承 `FormBusinessObject` 並覆寫對應方法。框架要能在收到 API 呼叫時，依 ProgId 解析出正確的 BO 子類別並具現化。

## 目標

1. `ProgramItem` 新增 `BusinessObject` 屬性，承載 BO 的 assembly-qualified type name
2. 新增 `ProgramSettingsFormBoTypeResolver`，從 `ProgramSettings` 查表決定 BO Type
3. 在 `Bee.Hosting` 把預設 DI 註冊由 `DefaultFormBoTypeResolver` 換成 `ProgramSettingsFormBoTypeResolver`
4. `BusinessObject` 缺省或解析失敗時，fallback 回 `FormBusinessObject`（與現行行為一致，不破壞既有部署）
5. round-trip 測試、resolver 單元測試、文件同步

## 非目標（明確排除）

- **不**在 `ProgramItem` 加 `Kind` 欄位（Form / Report / Other）。BO 種類由 `BusinessObject` 對應到的基底類別表達就夠；等選單層、權限層真的需要才補
- **不**處理 `SystemBusinessObject` 子類化（SystemBO 是 process-singleton 概念，與 progId 無關）
- **不**改 `IFormBoTypeResolver` 介面簽章；只新增一個實作
- **不**做 BO 子類別的具體案例（那是 ERP 應用層的事）
- **不**做熱重載：`ProgramSettings.xml` 異動由既有的 `ProgramSettingsCache` file watcher 處理，本計畫沿用

## 已確認的設計決議

### D1：BusinessObject 格式 — assembly-qualified

`ProgramItem.BusinessObject` 採 `"Namespace.Type, AssemblyName"` 格式，與 `Bee.Hosting` 內 `BeeFrameworkOptions.BusinessObjectFactory` / `ApiEncryptionKeyProvider` 等既有設定一致：

```xml
<ProgramItem ProgId="MFG001"
             DisplayName="工單維護"
             BusinessObject="MyErp.Business.WorkOrderBo, MyErp.Business" />
```

解析走 [`AssemblyLoader.GetType(fullBusinessObject)`](../../src/Bee.Base/AssemblyLoader.cs)（同 codebase 慣例，會自動 load 對應 assembly 進 default load context），不直接呼叫 `Type.GetType`。

### D2：不加 Kind 欄位

`ProgramItem` 不額外開 `Kind` 列舉。理由：

- BO 種類本來就由 `BusinessObject` 對應到的基底類別（`FormBusinessObject` / 未來可能的 `ReportBusinessObject`）表達
- 選單層、權限層目前都沒有「依 Kind 篩功能」的需求
- YAGNI：等有第一個消費者再加

### D3：BusinessObject 缺省 → fallback 回 FormBusinessObject

resolver 行為：

```
1. ProgramSettings 查表找 ProgId
2. 找到 → 看 BusinessObject
   - 空字串 / 未填    → 回 typeof(FormBusinessObject)
   - 有值但解析失敗   → 寫 warning log，回 typeof(FormBusinessObject)
   - 有值且解析成功   → 驗證 typeof(FormBusinessObject).IsAssignableFrom(t)
     - 是 → 回 t
     - 否 → 寫 warning log，回 typeof(FormBusinessObject)
3. ProgramSettings 查不到 ProgId → 回 typeof(FormBusinessObject)
```

選 fallback 而非嚴格模式的理由：

- 與現行 `DefaultFormBoTypeResolver` 行為一致，既有部署升級後不需要立刻為每個 ProgId 填 BusinessObject
- ERP 端可以「只為需要客製的 ProgId 填 BusinessObject」，其他享預設
- 「解析失敗」走 warning + fallback 而非例外，避免單一 ProgId 的設定錯誤拖垮整個系統

### D4：Type 解析結果 cache

`ProgramSettingsFormBoTypeResolver` 內部用 `ConcurrentDictionary<string, Type>` 對 `(ProgId → resolved Type)` 做 process-lifetime cache，避免每次 API call 都走一次 `AssemblyLoader.GetType`。

cache invalidation：當 `ProgramSettingsCache` 因 file watcher 偵測到 `ProgramSettings.xml` 異動時，resolver 也要 reset cache。

> 實作上 resolver 在每次 `Resolve(progId)` 開頭先比對「目前的 `ProgramSettings` reference 是不是與上次 cache key 同一個 instance」——若否，整個 cache 清空。理由：`IDefineAccess.GetProgramSettings()` 在 cache miss 時會回新 instance，可直接拿 reference equality 當失效訊號，不需另外掛 file watcher 事件。

## 實作步驟

### 步驟 1 — `ProgramItem` 加 BusinessObject 屬性

檔案：[`src/Bee.Definition/Settings/ProgramSettings/ProgramItem.cs`](../../src/Bee.Definition/Settings/ProgramSettings/ProgramItem.cs)

```csharp
/// <summary>
/// Gets or sets the assembly-qualified type name of the business object
/// bound to this program. When empty, the framework falls back to the base
/// <see cref="FormBusinessObject"/>.
/// </summary>
/// <remarks>
/// Expected format: <c>"Namespace.Type, AssemblyName"</c>
/// (e.g. <c>"MyErp.Business.WorkOrderBo, MyErp.Business"</c>).
/// </remarks>
[XmlAttribute]
[Description("BO type name (assembly-qualified).")]
[DefaultValue("")]
public string BusinessObject { get; set; } = string.Empty;
```

- `[DefaultValue("")]` 讓空字串時 XmlSerializer 不輸出該 attribute（保持既有 fixture XML 不變）
- 不影響 `Key` / `ProgId` / `ToString()`

### 步驟 2 — 新增 `ProgramSettingsFormBoTypeResolver`

檔案：`src/Bee.Business/ProgramSettingsFormBoTypeResolver.cs`

依賴：`IDefineAccess`（從 ProgramSettings 查表）。

```csharp
namespace Bee.Business;

public sealed class ProgramSettingsFormBoTypeResolver : IFormBoTypeResolver
{
    private readonly IDefineAccess _defineAccess;
    private readonly ILogger<ProgramSettingsFormBoTypeResolver>? _logger;

    private readonly ConcurrentDictionary<string, Type> _typeCache = new();
    private ProgramSettings? _lastSettingsRef;

    public ProgramSettingsFormBoTypeResolver(
        IDefineAccess defineAccess,
        ILogger<ProgramSettingsFormBoTypeResolver>? logger = null)
    {
        _defineAccess = defineAccess;
        _logger = logger;
    }

    public Type Resolve(string progId)
    {
        var settings = _defineAccess.GetProgramSettings();

        // Reset the cache when the settings instance changes (e.g. after a
        // file-watcher reload from ProgramSettingsCache).
        if (!ReferenceEquals(settings, _lastSettingsRef))
        {
            _typeCache.Clear();
            _lastSettingsRef = settings;
        }

        return _typeCache.GetOrAdd(progId, key => ResolveCore(settings, key));
    }

    private Type ResolveCore(ProgramSettings settings, string progId)
    {
        var item = FindItem(settings, progId);
        if (item == null || string.IsNullOrWhiteSpace(item.BusinessObject))
            return typeof(FormBusinessObject);

        var type = AssemblyLoader.GetType(item.BusinessObject);
        if (type == null)
        {
            _logger?.LogWarning(
                "ProgramItem '{ProgId}' declares BusinessObject '{BusinessObject}' but the type cannot be resolved; falling back to FormBusinessObject.",
                progId, item.BusinessObject);
            return typeof(FormBusinessObject);
        }

        if (!typeof(FormBusinessObject).IsAssignableFrom(type))
        {
            _logger?.LogWarning(
                "ProgramItem '{ProgId}' declares BusinessObject '{BusinessObject}' which is not a FormBusinessObject; falling back to base.",
                progId, item.BusinessObject);
            return typeof(FormBusinessObject);
        }

        return type;
    }

    private static ProgramItem? FindItem(ProgramSettings settings, string progId)
    {
        if (settings.Categories == null) return null;
        foreach (var category in settings.Categories)
        {
            if (category.Items == null) continue;
            if (category.Items.TryGetItem(progId, out var item)) return item;
        }
        return null;
    }
}
```

> `KeyCollectionBase<T>` 是否有 `TryGetItem` 方法待確認（步驟 2 開工前讀一次原始碼確認 API；若無同等方法，改用 indexer + `Contains`）。

### 步驟 3 — `Bee.Hosting` 切換預設 resolver

檔案：[`src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs:125`](../../src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs)

```csharp
// 原：
services.AddSingleton<IFormBoTypeResolver, DefaultFormBoTypeResolver>();

// 改為：
services.AddSingleton<IFormBoTypeResolver, ProgramSettingsFormBoTypeResolver>();
```

`DefaultFormBoTypeResolver` 保留不刪，作為「不依賴 `IDefineAccess` 的最小 fallback」供測試/特殊情境用。

### 步驟 4 — 補測試

#### 4a — `ProgramItem` round-trip 序列化測試

擴充 [`tests/Bee.Definition.UnitTests/Settings/ProgramSettingsDataTests.cs`](../../tests/Bee.Definition.UnitTests/Settings/ProgramSettingsDataTests.cs)：

- `ProgramItem` 預設建構子 `BusinessObject` 為空字串
- `BusinessObject` 為空時序列化不輸出該 attribute（DefaultValue 行為）
- `BusinessObject` 有值時 XML round-trip 保留

#### 4b — `ProgramSettingsFormBoTypeResolver` 單元測試

新檔：`tests/Bee.Business.UnitTests/ProgramSettingsFormBoTypeResolverTests.cs`

涵蓋分支：

| Case | BusinessObject | 預期結果 |
|------|----------|---------|
| ProgId 不在 ProgramSettings | — | `typeof(FormBusinessObject)` |
| ProgId 存在、BusinessObject 空 | `""` | `typeof(FormBusinessObject)` |
| ProgId 存在、BusinessObject 解析失敗 | `"NonExistent.Type, NonExistent"` | `typeof(FormBusinessObject)` + warning |
| ProgId 存在、BusinessObject 非 FormBusinessObject 子類 | `"System.Object"` | `typeof(FormBusinessObject)` + warning |
| ProgId 存在、BusinessObject 是合法 FormBusinessObject 子類 | `"<TestFormBo>, Bee.Business.UnitTests"` | 該 type |
| Resolve 同一 ProgId 多次 | — | 第二次走 cache（透過 mock 計次 `IDefineAccess.GetProgramSettings` 呼叫被 cache 後僅一次 BusinessObject 解析） |
| ProgramSettings instance 切換 | — | cache 自動 reset |

用 `FakeDefineAccess`（[`tests/Bee.Business.UnitTests/Fakes/FakeDefineAccess.cs`](../../tests/Bee.Business.UnitTests/Fakes/FakeDefineAccess.cs)）注入測試用 `ProgramSettings`。

#### 4c — Round-trip 整合測試影響評估

讀一次 `tests/Bee.Api.Core.UnitTests/Form/*RoundTripTests.cs`，確認用了 `_fx.GetRequiredService<IFormBoTypeResolver>()` 的測試：

- 預期：由於 fixture 的 `ProgramSettings.xml`（如有）中未填 BusinessObject，fallback 行為與 `DefaultFormBoTypeResolver` 一致，這些測試應不需改
- 若 fixture 沒有 `ProgramSettings.xml` → `IDefineAccess.GetProgramSettings()` 行為待確認（回空 instance 或拋例外？）
  - 若拋例外則需在 fixture `tests/Define/` 加最小 `ProgramSettings.xml`
  - 若回空 instance 則 resolver 對所有 ProgId fallback，無需改

### 步驟 5 — 文件同步

#### 5a — `terminology.md` / `terminology.zh-TW.md`

`ProgramSettings.xml` 條目補一句：「ProgramItem 可選擇性宣告 `BusinessObject` 綁定特定 `FormBusinessObject` 子類別；未填則走預設」。

#### 5b — `src/Bee.Definition/README.md` / `README.zh-TW.md`

`ProgramSettings` 簡介加註：「也承載 ProgId → BO 綁定資訊（`ProgramItem.BusinessObject`）」。

#### 5c — `IFormBoTypeResolver.cs` XML doc 更新

`DefaultFormBoTypeResolver` 的 remarks 補一句：「框架預設由 `Bee.Hosting` 註冊 `ProgramSettingsFormBoTypeResolver`；`DefaultFormBoTypeResolver` 僅供測試 / 最小 fallback 情境使用」。

#### 5d — `docs/development-cookbook.md`

「FormSchema 驅動開發」章節後加一小節「客製 BO 子類別」，示範：
1. 建 BO 子類別繼承 `FormBusinessObject`
2. 在 `ProgramSettings.xml` 對應 `ProgramItem` 填 `BusinessObject`
3. 重啟 / file watcher 自動生效

## 驗收條件

- [ ] `dotnet build --configuration Release` 無 warning（特別注意 `TreatWarningsAsErrors=true`）
- [ ] `./test.sh` 全綠
- [ ] 既有 `ProgramSettings.xml` fixture 不需修改（BusinessObject 為空 → 不輸出 attribute）
- [ ] 既有 RoundTripTests 不需改（fallback 行為一致）
- [ ] 新 `ProgramSettingsFormBoTypeResolverTests` 涵蓋 D3 所列全部分支
- [ ] 文件 5a-5d 全部同步更新（英文 + 繁中雙語）

## 實作偏離說明

實作過程中對原始 plan 的小調整,記錄於此供後續查閱:

### 偏離 1：D3 改為靜默 fallback,不寫 warning log

**原始 plan**：「BusinessObject 解析失敗 → warning log + fallback」。

**實作**：靜默 fallback,不寫 log。

**原因**：`Bee.Business` 目前完全不依賴 `Microsoft.Extensions.Logging.Abstractions`。為了一句 warning log 拉新 package reference 不划算;`System.Diagnostics.Trace` 在預設未設定 listener 的情境下也只是 noop。XML doc 已明確記載五種 resolution 行為,讀原始碼即可了解。未來若有正式的 diagnostic hook 設計再補回。

### 偏離 2：補上 `ProgramSettings.xml` 不存在的防線

**原始 plan**：plan 4c 提到「若 fixture 沒有 `ProgramSettings.xml` → `IDefineAccess.GetProgramSettings()` 行為待確認」,提出兩個方案(補 fixture 或 resolver 防線)。

**實作**:採 resolver 防線方案——`Resolve` 對 `IDefineAccess.GetProgramSettings()` 的 `FileNotFoundException` 做 catch,fallback 回 `FormBusinessObject`。並對 `AssemblyLoader.GetType` 拋出的 `FileNotFoundException` / `FileLoadException` / `BadImageFormatException` 一併 catch。

**原因**:既保留漸進採用的彈性(production 部署 `ProgramSettings.xml` 之前不會炸),也不需動到既有測試 fixture。新增測試 case 涵蓋此防線。

### 偏離 3:resolver test 內定義專用 fake,不沿用 `Fakes/FakeDefineAccess`

**原始 plan**:「用 `FakeDefineAccess` 注入測試用 `ProgramSettings`」。

**實作**:在 `ProgramSettingsFormBoTypeResolverTests.cs` 內定義兩個 private sealed 子 class(`ProgramSettingsDefineAccess` / `ThrowingDefineAccess`),不修改共用 `FakeDefineAccess`。

**原因**:resolver 需要能在測試中切換 `GetProgramSettings()` 回傳值與拋例外,語意特殊化過頭。獨立 fake 不污染共用 `FakeDefineAccess` 的 API surface,避免後續其他測試誤用。

### 偏離 5:屬性名由 `TypeName` 改為 `BusinessObject`

**原始 plan**:`ProgramItem.TypeName`(assembly-qualified type name)。

**實作後修訂**(2026-05-28):rename 為 `ProgramItem.BusinessObject`,跨 9 個檔同步更新(含 plan 檔名)。

**原因**:plan 與初版實作完成後,review 階段發現 `TypeName` **反 codebase 慣例**。`BackendComponents.cs` 內所有「儲存 assembly-qualified type name 的 string 屬性」(`BusinessObjectFactory`、`SessionInfoService`、`DefineAccess` 等共 12 個)**都不加 `TypeName` 後綴**,直接以角色名命名。`TypeName` 後綴除了違反慣例外,在 XML 中也太通用——讀者看 `TypeName="..."` 看不出是哪個 type。`BusinessObject` 完全對齊現有慣例,且讀 XML 時語意自然(「這個 ProgramItem 的 BusinessObject 是 X」)。

**評估替代方案**:`BoType`(對齊 `IFormBoTypeResolver` 的 `Bo` 簡稱但加 Type 後綴,半違反慣例)、`FormBoType`(鎖死 Form 前綴,雖明確但失彈性)。最終選 `BusinessObject` 因「完全對齊 codebase 慣例」比「短一兩字」重要。

### 偏離 4:cookbook 章節標題

**原始 plan**:「客製 BO 子類別」。

**實作**:中英雙語標題使用「客製化 ProgId 對應的 BO」/「Customising the BO for a ProgId」。

**原因**:讀者更容易透過搜尋「ProgId」找到該章節,且「為 ProgId 客製」比「客製子類別」更貼近實際操作場景。

## 風險與注意事項

1. **`ProgramSettings.xml` schema 變更相容性**：新增 attribute 屬向後相容（舊 XML 不含 `BusinessObject` 仍可正常 deserialize），但需在測試確認舊 fixture 不需改
2. **`AssemblyLoader.GetType` 載入失敗的副作用**：第一次失敗會被 cache 起來；若 ERP 端先部署了壞掉的 `ProgramSettings.xml` 又熱修正 `BusinessObject`，需要重啟 process 或觸發 file watcher reload 才會重試（接受此行為，因 `ProgramSettingsCache` 本來就會 reload 整份 settings）
3. **assembly 還沒 load 的情境**：`AssemblyLoader.LoadAssembly` 走 default load context，需要 ERP 端把 BO assembly 放在 server 可探索的位置（通常已是；若有 plugin folder 機制需另議——本計畫不處理）
4. **與 SonarCloud S125 互動**：上述程式碼範例中的英文註解已使用完整句子 + `.` 結尾，避開 S125 啟發式誤判

## 參考

- [`src/Bee.Business/IFormBoTypeResolver.cs`](../../src/Bee.Business/IFormBoTypeResolver.cs) — 抽象介面與目前預設實作
- [`src/Bee.Business/BusinessObjectFactory.cs`](../../src/Bee.Business/BusinessObjectFactory.cs) — resolver 的消費者
- [`src/Bee.Base/AssemblyLoader.cs`](../../src/Bee.Base/AssemblyLoader.cs) — 型別解析工具
- [`src/Bee.ObjectCaching/Define/ProgramSettingsCache.cs`](../../src/Bee.ObjectCaching/Define/ProgramSettingsCache.cs) — file watcher 重載機制
