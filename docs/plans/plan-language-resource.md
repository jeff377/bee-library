# 計畫：多國語系資源（LanguageResource）

**狀態：✅ 已完成（2026-05-27）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | `LanguageResource` 類別（XML + JSON 雙序列化）+ `DefineType.Language` + 檔案讀寫 + `PathOptions` 路徑 | ✅ 已完成（2026-05-27） |
| 2 | `GetLangText` API + cache 走既有 Define cache + 用 `SessionInfo.Culture`（既有欄位，未新增）+ `IStringLocalizer<T>` adapter | ✅ 已完成（2026-05-27） |
| 3 | FormSchema DisplayName + Table.DisplayName + Field.Caption 自動本地化（約定 key 自動包裝） | ✅ 已完成（2026-05-27） |
| 4 | 下拉清單（`LanguageEnum`）支援、`GetLangEnum` API、ComboBox 整合 | ✅ 已完成（2026-05-27） |
| 5 | JSON-RPC `SystemBO.GetLanguage` 方法給 JS 端（對標 `GetFormSchema` / `GetFormLayout`） | ✅ 已完成（2026-05-27） |

## 背景

現行 bee-library 沒有正式的多國語系機制。需要在不引入 .NET 傳統 `.resx` 生態（與 `FormSchema` / `FormLayout` 等既有 XML 定義檔不一致）的前提下，提供：

- 以**表單為單位**（per ProgId）的語系資源檔，方便客製化、譯者按表單交付
- 與既有 Define 系統（`DefineType` / `IDefineAccess` / `FileDefineStorage`）相同的讀寫管道
- 以**檔案為單位**的小單位 cache，熱抽換時失效範圍最小
- 同時涵蓋三類語系內容：純文字、表單 metadata（Caption / Tip / ColumnHeader）、下拉清單
- **雙序列化路徑**：XML 用於持久化（檔案 + .NET client 既有管道），JSON 用於 JS / TypeScript 前端透過 JSON-RPC Plain wire format 取用（對標 `FormSchema` / `FormLayout`，見 [docs/archive/plan-jsonrpc-formschema-formlayout.md](../archive/plan-jsonrpc-formschema-formlayout.md)）

## 已確認決策

### A. 資源組織

| 項目 | 決策 |
|------|------|
| 鍵值結構 | `{Namespace}.{subKey}`，解析時**只切第一個 `.`** |
| Namespace 對應 | `Namespace = ProgId = 檔名` 嚴格 1:1 |
| 檔案命名 | `{Namespace}.Language.xml` |
| 共用檔 | `Common.Language.xml`、模組共用 `{Module}.Language.xml`（如 `Sys.Language.xml`） |
| Lang code 格式 | BCP-47 specific：`zh-TW` / `zh-CN` / `en-US`（不用 `zh-Hant` neutral form） |
| 目錄慣例 | 沿用既有單數命名（`FormSchema/` / `TableSchema/` 同套），不一併改 plural |

**N:1 對應的正確語意**：多個 form 都呼叫 `Common.{key}` 或 `{Module}.{key}` 取共用詞 —— **不是**把多個 ProgId 的 key mapping 到同一檔。後者會破壞 cache 單位。

### B. 路徑

擺在 `DefinePath` 下的 `Language/` 子目錄（與 `FormSchema/` / `FormLayout/` / `TableSchema/` 同層級）：

```
{DefinePath}/Language/{lang}/{Namespace}.Language.xml
```

範例：
```
Define/Language/zh-TW/Common.Language.xml
Define/Language/zh-TW/Sys.Language.xml
Define/Language/zh-TW/Customer.Language.xml
Define/Language/en-US/Customer.Language.xml
```

理由：與既有 Define 子目錄一致、重用 `PathOptions.DefinePath`、無需新增 config 欄位、部署單元統一。

### C. 類別形狀

```csharp
public class LanguageResource
{
    public string Namespace { get; set; } = string.Empty;   // "Customer"、"Common"
    public string Lang { get; set; } = string.Empty;         // "zh-TW"
    public List<LanguageItem> Items { get; set; } = new();
    public List<LanguageEnum> Enums { get; set; } = new();

    // 內部 lazy 建 Dictionary<string, string> 加速查詢；雙序列化都要排除。
    [XmlIgnore, JsonIgnore]
    private Dictionary<string, string>? _itemIndex;
    [XmlIgnore, JsonIgnore]
    private Dictionary<string, LanguageEnum>? _enumIndex;

    public string? GetText(string subKey) { ... }
    public LanguageEnum? GetEnum(string name) { ... }
}

public class LanguageItem
{
    public string Key { get; set; } = string.Empty;    // sub-key，例 "Field.Name.Caption"
    public string Value { get; set; } = string.Empty;
}

public class LanguageEnum
{
    public string Name { get; set; } = string.Empty;
    public List<LanguageEnumEntry> Entries { get; set; } = new();
}

public class LanguageEnumEntry
{
    public string Code { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    // 未來可擴充 Description / Icon / SortOrder
}
```

XML 形式：

```xml
<LanguageResource Namespace="Common" Lang="zh-TW">
  <Items>
    <LanguageItem Key="OK" Value="確定" />
    <LanguageItem Key="Cancel" Value="取消" />
  </Items>
  <Enums>
    <LanguageEnum Name="Gender">
      <Entry Code="M" Text="男" />
      <Entry Code="F" Text="女" />
    </LanguageEnum>
  </Enums>
</LanguageResource>
```

### D. Define 整合

| 項目 | 決策 |
|------|------|
| `DefineType` 新增 | `Language` |
| `IDefineAccess` 新增 | `LanguageResource GetLanguage(string lang, string ns)` / `SaveLanguage(LanguageResource resource)` |
| `PathOptions` 新增 | `GetLanguageFilePath(string lang, string ns)` → `{DefinePath}/Language/{lang}/{ns}.Language.xml` |
| Cache key | `"{lang}:{namespace}"`（沿用既有 `Dictionary<string, object>` cache，不動底層） |
| XML 序列化 | `XmlCodec`（與 FormSchema / FormLayout 同一管道，用於檔案持久化、.NET client、`SystemBO.GetDefine`） |
| JSON 序列化 | `JsonCodec`（用於 JSON-RPC Plain wire format，給 JS / TypeScript 前端） |

### E. Server 端 Lang 來源

`SessionInfo` 新增 `Lang` 欄位（BCP-47 string），登入時寫入；BO / Repository / Validator 透過 session 取。

不走 JSON-RPC request header / per-request metadata —— 維持單一來源，避免跨層多處傳 lang 參數。

### F. MVP 範圍

| 範圍 | 含 |
|------|---|
| ✅ Phase 1 | `LanguageResource` 類別、`DefineType.Language`、`IDefineAccess.GetLanguage`、`FileDefineStorage` 讀寫、`PathOptions.GetLanguageFilePath`、XML 序列化、單元測試 |
| ✅ Phase 2 | `GetLangText(fullKey)` 與 `GetLangText(namespace, subKey)` 雙多載、cache（per `{lang}:{namespace}` 為單位，沿用 Define cache）、`SessionInfo.Lang`、`SystemSettings.DefaultLang`、`BeeStringLocalizer<T>` : `IStringLocalizer<T>` adapter、Client UI 純文字本地化（按鈕、訊息框、static label） |
| ✅ Phase 3 | FormSchema Caption / Tip / ColumnHeader 自動本地化（約定 key 自動包裝：`Field.{Name}.Caption`） |
| ✅ Phase 4 | `GetLangEnum(string fullName)` API、ComboBox 引用 LangEnum、整合 FormSchema 既有下拉欄位定義 |
| ✅ Phase 5 | JSON-RPC `SystemBO.GetLanguage(lang, namespace)` 給 JS 端、Plain wire format JSON 序列化驗證 |
| ❌ 不在 MVP | Validator 規則訊息本地化、BO `BusinessException` 訊息本地化、報表標題範本本地化 |

不含項目可在 MVP 落地、實際使用觀察後另開 plan。

### G. 補充決議（B / C 類）

| # | 議題 | 決議 | 備註 |
|---|------|------|------|
| 1 | FormSchema 整合方式 | **約定 key 自動包裝** | namespace = ProgId、subKey 約定 `Field.{Name}.Caption` / `Field.{Name}.Tip` / `Field.{Name}.ColumnHeader`。Schema 0 侵入，查不到回退原 Caption 字面值 |
| 2 | 持久化檔案格式 | **XML 持久化 + JSON 動態產出** | XML 進檔案，JSON 由 `SystemBO.GetLanguage` 動態 serialize；不雙寫檔 |
| 3 | 客製覆寫機制 | **MVP 不做** | 多租戶架構下會有多組客製化代碼，「客製化代碼」歸屬尚未定義（要寫在 schema / session / 獨立 config 哪一層、命名與唯一性規則）；待該前置決定落地後再另開 plan 設計覆寫路徑與 cache key 擴充 |
| 4 | 缺譯行為 | **回退預設語系 → 再 miss 回 key** | 預設語系由 `SystemSettings.DefaultLang` 提供；開發階段可加 log |
| 5 | Cache invalidation | **沿用既有 Define cache 機制** | 跟 FormSchema / FormLayout 同套（Define 系統 in-memory cache、走既有 reload 介面）。未來定義檔持久化方式改抽介面層（local file / cloud storage / DB）時統一處理多租戶 / 多主機場景，不為 Language 單獨設計 watcher |
| 6 | API surface | **兩種多載並存** | `GetLangText(string fullKey)` 為主、`GetLangText(string namespace, string subKey)` 顯式版備用 |
| 7 | `IStringLocalizer<T>` adapter | **MVP 提供** | 實作 `BeeStringLocalizer<T>` : `IStringLocalizer<T>`，內部轉呼 `GetLangText`。讓 Blazor / AspNetCore 元件可 `@inject IStringLocalizer<MyPage>` 直接吃 |
| 8 | 預設語系設定來源 | **`SystemSettings.DefaultLang`** | 新增欄位，與其他系統設定一起由 Define 系統管理 |

## 階段拆解

### Phase 1：核心類別與檔案 IO

**檔案**：

- `src/Bee.Definition/Language/LanguageResource.cs` —— 主類別 + lazy dict 查詢
- `src/Bee.Definition/Language/LanguageItem.cs`
- `src/Bee.Definition/Language/LanguageEnum.cs`
- `src/Bee.Definition/Language/LanguageEnumEntry.cs`
- `src/Bee.Definition/DefineType.cs` —— 新增 `Language` 列舉值
- `src/Bee.Definition/Storage/IDefineAccess.cs` —— 新增 `GetLanguage` / `SaveLanguage`
- `src/Bee.Definition/Storage/FileDefineStorage.cs` —— 對應實作
- `src/Bee.Definition/Storage/LocalDefineAccess.cs` —— 對應實作 + cache key 規則
- `src/Bee.Definition/PathOptions.cs` —— `GetLanguageFilePath`
- `tests/Bee.Definition.UnitTests/Language/LanguageResourceTests.cs` —— **XML round-trip**（持久化路徑）、**JSON round-trip**（JS 路徑）、查詢、缺項

**範圍**：純讀寫與雙序列化，**不**含對外 `GetLangText` 公開 API（留給 Phase 2），**不**含 JSON-RPC server method（留給 Phase 5）。

**序列化驗證標準**（對齊 FormSchema 在 [docs/archive/plan-jsonrpc-formschema-formlayout.md](../archive/plan-jsonrpc-formschema-formlayout.md) 的做法）：
- XML：與 `LocalDefineAccess` round-trip 通過、檔內元素順序穩定（avoid xmlns 抖動）
- JSON：`JsonCodec.Serialize(resource)` 產出 camelCase、`enum-as-string`、`Items` / `Enums` 為 JSON array、無多餘冗餘欄位

### Phase 2：`GetLangText` API + Cache + Session 整合 + IStringLocalizer adapter

**檔案**：

- `src/Bee.<TBD>/Language/ILanguageService.cs` —— 對外查詢介面（位置進實作前再決定，候選：`Bee.Business` 或新建 `Bee.Localization`）
- `src/Bee.<TBD>/Language/LanguageService.cs` —— `GetLangText(fullKey)` 實作：拆 namespace → 取 `LanguageResource` → 查 subKey；走 `IDefineAccess` 既有 cache（per `{lang}:{namespace}`）；缺譯回退 `SystemSettings.DefaultLang` 再回 key
- `src/Bee.<TBD>/Language/BeeStringLocalizer.cs` —— `IStringLocalizer<T>` adapter，內部轉呼 `ILanguageService.GetLangText`，namespace 取 `typeof(T).Name`（或自訂 attribute）
- `src/Bee.Definition/Session/SessionInfo.cs`（或對應檔）—— 加 `Lang` 欄位（BCP-47 string）
- `src/Bee.Definition/Settings/SystemSettings.cs` —— 加 `DefaultLang` 欄位
- `tests/<...>/LanguageServiceTests.cs` —— namespace 解析、fallback 行為、cache 命中、`IStringLocalizer<T>` adapter round-trip

**API 形狀**：

```csharp
public interface ILanguageService
{
    // 主要 API（fullKey 自拆 namespace）
    string GetLangText(string fullKey);
    string GetLangText(string fullKey, string fallback);
    bool TryGetLangText(string fullKey, out string text);

    // 顯式版（避免 fullKey 拼錯；namespace + subKey 分別傳）
    string GetLangText(string @namespace, string subKey);
    bool TryGetLangText(string @namespace, string subKey, out string text);
}

// .NET 既有元件介接
public class BeeStringLocalizer<T> : IStringLocalizer<T> { ... }
```

**Lang 解析優先序**：
1. `SessionInfo.Lang`（呼叫端 session 已登入）
2. `SystemSettings.DefaultLang`（fallback）
3. 寫死 `"en-US"`（兩者皆空時的最終保底）

**缺譯行為**：
- 查 `(currentLang, namespace, subKey)` miss → 查 `(DefaultLang, namespace, subKey)`
- 仍 miss → 回 `fullKey` 本身（呼叫端容易在 UI 上看出缺譯，開發階段可加 log）
- 提供 `TryGetLangText` 給呼叫端自己判斷 miss 並客製處理

### Phase 3：FormSchema 自動本地化

**目標**：使用者讀取 `FormSchema` / `FormLayout` 時，Caption / Tip / ColumnHeader 三類欄位自動帶入當前 lang 文字，不需呼叫端逐欄位手動套。

**採方案：約定 key 自動包裝**

FormSchema **不增欄位、不改 XML 格式**。`IDefineAccess.GetFormSchema(progId)` 取得 schema 後，由本 phase 新增的 `FormSchemaLocalizer`（或在現有 schema 讀取管線中插入）依約定查 LanguageResource：

| FormSchema 欄位 | 查詢 key | namespace |
|----------------|---------|-----------|
| `Field.Caption` | `Field.{FieldName}.Caption` | `{ProgId}` |
| `Field.Tip` | `Field.{FieldName}.Tip` | `{ProgId}` |
| `Field.ColumnHeader` | `Field.{FieldName}.ColumnHeader` | `{ProgId}` |

查到語系文字 → 覆蓋 schema 物件的對應屬性；查不到 → 保留 schema 內原字面值（向下相容，不強制全表單做 i18n）。

**檔案**：

- `src/Bee.<TBD>/Language/FormSchemaLocalizer.cs` —— 對 FormSchema 物件套用本地化
- `src/Bee.<TBD>/Language/Conventions.cs` —— key 約定常數（`"Field.{0}.Caption"` 等）
- `src/Bee.Business/`（或 schema 讀取管線）—— 在 `GetFormSchema` 回傳前呼叫 localizer
- `tests/<...>/FormSchemaLocalizerTests.cs` —— 有對應 key / 沒對應 key（保留原字面值）/ namespace 自動帶 ProgId

**Escape hatch（少數需跨 namespace 取共用詞的 field）**：

如果某 field 想取 `Common.Field.Generic.Name.Caption` 而非 `{ProgId}.Field.Foo.Caption`，可在 FormSchema field 上加標記欄位 `CaptionKeyOverride="Common.Field.Generic.Name.Caption"`（指定完整 fullKey）。MVP 視需求決定是否加，初版可不做。

### Phase 4：下拉清單

**檔案**：

- `LanguageService` 加 `GetLangEnum(string fullName)` / `GetLangEnumText(string fullName, string code)`
- FormSchema ComboBox 欄位定義加引用機制（`LangEnumName="OrderStatus"` 或 `LangEnumName="Common.Gender"`）
- 讀取 FormSchema 時自動把 enum entries 帶入 ComboBox 的 options

### Phase 5：JSON-RPC `GetLanguage` 給 JS 端

對標 [docs/archive/plan-jsonrpc-formschema-formlayout.md](../archive/plan-jsonrpc-formschema-formlayout.md) 的 `GetFormSchema` / `GetFormLayout`：為 JS / TypeScript 前端開 JSON-native endpoint，跳過 XML 中介。

**為什麼不直接讓 JS 走既有 `SystemBO.GetDefine`**：
- `GetDefine` 回傳 XML 字串（`result.Xml = XmlCodec.Serialize(value)`），JS 端要先解 XML 再轉物件，跨兩層序列化。
- 新方法走 Plain JSON pipeline 直接把強型別 `LanguageResource` 展為 JS 可直接消費的 JSON tree。
- `GetDefine` 維持原樣供 .NET client 與其他既有用途。

**API 形狀**：

```
SystemBO.GetLanguage
  Args:   { Lang: string, Namespace: string }
  Result: { Resource: LanguageResource }   // Plain JSON pipeline 自動序列化
  ProtectionLevel: Public + Authenticated   // 與 GetFormSchema 對齊
```

實作 = `IDefineAccess.GetLanguage(args.Lang, args.Namespace)` 的強型別 wrap，不走 Repository（讀 in-memory cache，不碰 DB）。

**檔案**（依 `.claude/skills/bee-add-bo-method/SKILL.md` 樣板，7~8 個檔）：

- `src/Bee.Definition/SystemActions.cs` —— 新增 `GetLanguage` 常數
- `src/Bee.Definition/Contracts/Language/GetLanguageArgs.cs` / `GetLanguageResult.cs`
- `src/Bee.Api.Core/Wire/.../GetLanguageRequest.cs` / `GetLanguageResponse.cs`
- `src/Bee.Business/SystemBO.cs` —— 新增 `GetLanguage` BO 方法
- `src/Bee.Api.Client/.../SystemApiConnector.cs` —— 對應 client method
- DI 註冊 + 兩層 round-trip 測試（contract / wire / BO 各一）

**注意**：路徑與檔名最終由 `bee-add-bo-method` skill 落地時決定，上述為示意。

## 不在本 plan 範圍

- **客製覆寫機制**（雙目錄 / sub-key merge）：MVP 不做。前置障礙是**多租戶下「客製化代碼」歸屬尚未定義** —— 同一個 deployment 可能有多個 tenant 各帶不同客製化代碼，覆寫路徑與 cache key 都要納入該維度（例如 `{DefinePath}/Language.Custom/{tenantCode}/{lang}/{namespace}.Language.xml`、cache key 從 `{lang}:{namespace}` 變 `{tenantCode}:{lang}:{namespace}`）。等多租戶客製化代碼定義落地後另開 plan 設計
- **多租戶 / 多主機 storage 介面層**：未來 Define 系統定義檔持久化抽象化（local file / cloud storage / DB）時統一處理；屆時 Language cache invalidation 隨 Define 系統的 invalidation 介面走，不為 Language 單獨設計
- Validator 規則訊息本地化（屬資料驗證層，獨立 plan）
- BO `BusinessException` 訊息本地化（要先決定 server 拋出策略：error code vs localized text）
- 報表標題範本本地化（屬報表引擎，獨立模組）
- 既有資料夾 `FormSchema/` → `FormSchemas/` 改複數的全 repo rename（breaking change，須隨 major bump 一起做）
- 翻譯工作流（譯者使用什麼工具匯入匯出 XML、是否提供 CLI dump tool）

## 設計取捨備忘

### 為什麼不分 `LanguagePath` 獨立目錄

考慮過 `PathOptions.LanguagePath` 與 `DefinePath` 平行（譯者交付、熱抽換更獨立）。最終選 `DefinePath/Language/` 子目錄：

- 部署單元統一，運維只管一個根目錄
- 重用既有 `PathOptions.DefinePath`，不引入新 config 欄位
- 與 `FormSchema/` / `FormLayout/` / `TableSchema/` 一致；schema 與其語系本來就應放在一起
- 客製覆寫議題（待議 #3）若採雙目錄方案，可以在 `Languages.Custom/`（仍在 `DefinePath` 下）疊放，不需動 `PathOptions`

### 為什麼下拉清單分離 `Enums` 而非 flat `Items`

詳見對話設計過程：保留順序、結構性查詢、可擴充欄位、避免 key naming convention 綁架四點。flat key-value `Items` 撐不住下拉的「ordered + multi-attribute per entry」需求。

### 為什麼用 BCP-47 specific 而非 .NET CultureInfo neutral

`zh-TW` / `zh-CN` / `en-US` 與 web 生態（HTML lang attr、Accept-Language、URL slug、CLDR）一致；`zh-Hant` / `zh-Hans` 雖是 .NET 原生 neutral culture，但對譯者與一般使用者直觀度低。`CultureInfo.GetCultureInfo("zh-TW")` 在 .NET 也能正常解析，沒有相容性損失。
