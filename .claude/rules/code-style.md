# 程式碼風格規範

## 命名規則

| 元素 | 規則 | 範例 |
|------|------|------|
| 介面 | `I` 前綴 + PascalCase | `IKeyObject`, `IApiPayloadEncryptor` |
| 類別 | PascalCase | `TraceContext`, `AesCbcHmacCryptor` |
| Attribute | PascalCase + `Attribute` 後綴 | `ApiAccessControlAttribute` |
| 屬性 / 方法 | PascalCase | `ValidateAccess`, `CreateSession` |
| 私有欄位 | `_camelCase`（底線前綴） | `_isTokenValid`, `_accessToken` |
| 參數 | camelCase | `accessToken`, `sessionId` |
| 擴充方法類別 | `<TypeName>Extensions` | `StringExtensions`, `DateTimeExtensions`, `ExceptionExtensions` |
| 名詞型 static utility | `<Domain>Utilities` 或純名詞 | `StringUtilities`, `HttpUtilities`, `ValueUtilities`, `Gzip`, `XmlCodec` |

> **`*Func` 後綴已棄用**（2026-05-01 全面移除）。現存程式碼不應再新增 `*Func` 命名的靜態類別。

## 靜態工具類別與擴充方法

新方法依其本身屬性選擇歸屬，**不要建立 grab-bag 共用類**：

| 路徑 | 適用條件 | 命名 / 位置 | 範例 |
|------|---------|-----------|------|
| **A. 直接用 BCL** | BCL 已有等價功能 | 不寫 wrapper，呼叫端直接 inline | `Guid.NewGuid()`、`ArgumentException.ThrowIfNullOrWhiteSpace`、`RandomNumberGenerator.GetInt32` |
| **B. 擴充方法** | 第一參數為 BCL 或 domain 型別，能讀為「subject 動作」 | `<TypeName>Extensions`，與目標型別同 namespace | `s.SplitLeft(...)` → `StringExtensions`、`ex.Unwrap()` → `ExceptionExtensions` |
| **C. 名詞型 static utility** | 純功能集合，無自然 domain 歸屬 | `<Domain>Utilities` 或單純名詞 | `StringUtilities.IsEqualsOr(...)`、`HttpUtilities.SendAsync(...)`、`ValueUtilities.CStr(...)` |
| **D. 移到 domain class** | 方法本質屬於某 domain object 既有職責 | 該物件的 `static` 或 instance method | `BackendInfo.GetDatabaseItem`、`FormSchema.GetListLayout` |

### 細部原則

- **不擴充 `object`**：第一參數為 `object` 的方法不轉擴充方法（會污染**所有**型別 IntelliSense）。改走 path A inline 或 path C noun-form static
- **不與 BCL instance method 同名擴充**：擴充方法會被 BCL instance method 永久覆蓋（C# member resolution 規則）。`string.StartsWith` / `string.Contains` / `string.IndexOf` 衝突時必須走 path C（`StringUtilities.StartsWith(s, prefix)`）
- **框架封裝預設值**：helper 內部處理 `CultureInfo` / `StringComparison` 等文化/大小寫預設（ERP 預設 InvariantCulture + 不區大小寫），呼叫端只在覆蓋預設時傳第二個參數。**不要把 `string.Format(CultureInfo.InvariantCulture, ...)` 散落到呼叫端**
- **0-caller 框架公開 API 保留**：作為框架對外 API surface（如 `ValueUtilities.Cxxx` 型別轉換家族），即使 prod 0 caller 仍保留；純 BCL wrapper 且 0 caller 才直接刪
- **消除純 facade**：1-line delegation wrapper 不保留 —— 公開內部 container，呼叫端直接用
- **不為假設的未來建類**：即使預期未來會新增共用方法，也應依方法屬性逐一找歸屬，現在只搬最低必要內容

### Path C 命名衝突檢查（CA1724）

選名詞型 utility 名稱時必須避開**所有** BCL namespace 末段名稱：

- Roslyn analyzer **CA1724** 對 namespace-vs-type 同名也會告警；`TreatWarningsAsErrors=true` 下會編譯失敗
- 常見地雷：`Http`（`System.Net.Http`）、`Json`（`System.Text.Json`）、`Xml`（`System.Xml`）、`Linq`、`Threading`、`Diagnostics`
- 衝突解法優先序：加 `Utilities` 後綴（`Http` → `HttpUtilities`）、加領域前綴
- **避免 `*Helper` 後綴**：在 .NET 已過時，且仍可能與舊 type 撞名

### Path D shadowing 檢查

把方法搬到 owning class 為 `private static` 前，確認 enclosing type member 不會與方法 body 引用的 type 名稱衝突（C# member lookup 規則：enclosing type member 優先於 namespace 內的 type）。

衝突時優先**改走 path C**（獨立類）—— 衝突往往是 path D 不適用的訊號。

## 檔案組織

每個套件依功能分資料夾，介面可依所屬功能就近放置，不強制集中至 `Interface/` 資料夾：
```
Bee.<Module>/
  ├── Attributes/     # 自訂 Attribute
  ├── Exception/      # 自訂例外
  └── <Feature>/      # 功能實作（含相關介面）
```

### 資料夾與命名空間一致性

資料夾結構必須對映命名空間（對應 IDE0130 規範）：
- ✅ `src/Bee.Db/Schema/` → `namespace Bee.Db.Schema`
- ❌ `src/Bee.Db/DbAccess/` → `namespace Bee.Db`（資料夾與命名空間不符）

**唯一例外**：某資料夾下集中了同一父類別的大量子類別，可用資料夾做邏輯分組而不建立對應子命名空間。
- 例：`src/Bee.Definition/Settings/` 下有許多 `*Settings` 子類別，命名空間維持 `Bee.Definition.Settings`

> 此規則無法以 `.editorconfig` 硬性化（IDE0130 是全域規則，無法針對個別資料夾開例外），由 prompt 層把關。

## 文件語言規則

- 未指定雙語版時，文件只有 `xxx.md`，內容以**繁體中文**撰寫
- 若有雙語版本（如 `README.md` / `README.zh-TW.md`），修改內容時**必須同步更新兩份文件**：
  - 預設檔名為英文版（如 `architecture-overview.md`）
  - 繁體中文版以 `.zh-TW.md` 後綴命名（如 `architecture-overview.zh-TW.md`）
  - 兩份文件頂部需有語言切換連結（`[繁體中文](xxx.zh-TW.md)` / `[English](xxx.md)`）

## 語言特性

- 啟用 **Nullable Reference Types**（`<Nullable>enable</Nullable>`）
- 啟用 **Implicit Usings**（新專案）
- 保持 **Deterministic Builds**（`<Deterministic>true</Deterministic>`）
- 視警告為錯誤（`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`）

### 警告處理原則

程式碼中的編譯器警告必須盡量修正，不允許保留未處理的警告：
- **Nullable 警告**（CS8600–CS8670）：使用 `!`（null-forgiving）、加 null 檢查、或重構為非 nullable，依情境選擇最能表達意圖的方式
- 測試中故意傳入 `null` 驗證邊界行為時，使用 `null!` 明確表示這是有意為之
- 不得以 `#pragma warning disable` 大範圍壓制警告；若需抑制單行，必須加上說明註解

## 註解規範

預設**不寫註解** — 命名清楚的識別符已能說明 WHAT，註解只在 WHY 非顯而易見時才有價值。
若移除註解後讀者不會困惑，就不該寫。

### 說明性註解（in-body）

寫註解時遵循以下原則：

- **寫 WHY 不寫 WHAT** — 解釋意圖、限制、權衡，不重複程式碼已表達的內容
- **使用結構化前綴**標示意圖：

  | 前綴 | 用途 |
  |------|------|
  | `WARNING:` / `IMPORTANT:` | 改動會破壞功能或安全性 |
  | `NOTE:` | 非顯而易見的設計考量 |
  | `HACK:` | 已知技術債，需附原因 |
  | `TODO:` | 待辦（建議帶 issue 連結） |

- **長篇背景外送至 `docs/adr/`**，原始碼只留結論並引用 ADR 編號
- **跨檔案約束**雙向都寫註解；可能的話以編譯期手段（source generator、測試斷言）取代註解
- **絕不註解掉舊程式碼**（對應 SonarCloud S125，見 `sonarcloud.md` 第 9 節）。需保留歷史用 `git log`

```csharp
// ✅ 寫 WHY：解釋非顯而易見的限制
// PostgreSQL 預設 statement_timeout 為 30 秒；超過會被 server 中斷，
// 此處對齊以便 client 端先收到 TimeoutException 而非 NpgsqlException。
client.Timeout = TimeSpan.FromSeconds(30);

// ✅ WARNING 前綴：標示安全相關約束
// WARNING: 此處的順序不可調換 — 必須先序列化再壓縮再加密。
// 反過來會破壞 AesCbcHmac 的 IV 隨機性保證（見 docs/adr/0007-payload-pipeline.md）。
var serialized = Serialize(payload);
var compressed = Compress(serialized);
var encrypted = Encrypt(compressed);

// ❌ 寫了等於沒寫：重複程式碼已表達的內容
// 設定 timeout 為 30 秒
client.Timeout = TimeSpan.FromSeconds(30);

// ❌ 註解掉的舊程式碼：用 git log 保留歷史，不留在原始碼
// var legacy = oldRepo.GetUser(id);
var user = repo.GetUser(id);
```

### XML 文件註解（公開 API）

XML 文件使用**英文**撰寫（套件公開發布於 NuGet，英文確保 IntelliSense 與外部使用者皆可閱讀）：

```csharp
/// <summary>
/// Validates the access token.
/// </summary>
/// <param name="token">The access token to validate.</param>
/// <returns>True if the token is valid; otherwise, false.</returns>
public bool ValidateToken(string token) { ... }
```

API 級的警告或前置條件寫進 `<remarks>`，呼叫端會在 IDE IntelliSense 看到：

```csharp
/// <summary>
/// Renews the access token.
/// </summary>
/// <remarks>
/// This method must be called within an active transaction.
/// Calling without a transaction will leave the session table in an
/// inconsistent state if a network error occurs mid-call.
/// </remarks>
public void RenewAccessToken(Guid sessionId) { ... }
```

## 序列化

- JSON 序列化使用 **System.Text.Json**（`JsonSerializer`）
- 高效能場景使用 **MessagePack**
- 不使用 `Newtonsoft.Json`

## 縮排與格式

- 檔案編碼：**UTF-8**（不帶 BOM），避免中文亂碼
- 縮排：**4 個空格**（`.editorconfig` 定義）
- XML 檔案（`.csproj`、`.props`）：**2 個空格**
- 行結尾：**LF**（Unix），由 `.gitattributes` 強制
