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

### 命名空間 vs 型別名稱（CA1724）

- **禁止命名空間最後一段與其中的類別／介面同名**
  - ❌ `namespace Bee.Db.DbAccess` + `class DbAccess`（造成 `Bee.Db.DbAccess.DbAccess` 冗餘限定）
  - ✅ 將子命名空間合併至上層：`namespace Bee.Db` + `class DbAccess`
  - ✅ 或重新命名類別：`namespace Bee.Db.DbAccess` + `class DbAccessor`

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

## XML 文件註解

所有 `public` API 必須加 XML 文件，使用**英文**撰寫（套件公開發布於 NuGet，英文確保 IntelliSense 與外部使用者皆可閱讀）：

```csharp
/// <summary>
/// Validates the access token.
/// </summary>
/// <param name="token">The access token to validate.</param>
/// <returns>True if the token is valid; otherwise, false.</returns>
public bool ValidateToken(string token) { ... }
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
