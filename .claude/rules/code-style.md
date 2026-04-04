# 程式碼風格規範

## 命名規則

| 元素 | 規則 | 範例 |
|------|------|------|
| 介面 | `I` 前綴 + PascalCase | `IKeyObject`, `IApiPayloadEncryptor` |
| 類別 | PascalCase | `TraceContext`, `AesCbcHmacCryptor` |
| Attribute | PascalCase + `Attribute` 後綴 | `ApiAccessControlAttribute` |
| 屬性 / 方法 | PascalCase | `ValidateAccess`, `CreateSession` |
| 私有欄位 | camelCase（無底線前綴） | `isTokenValid` |
| 參數 | camelCase | `accessToken`, `sessionId` |

## 檔案組織

每個套件依職責分資料夾，常見結構：
```
Bee.<Module>/
  ├── Interface/     # 公開合約（介面）
  ├── Attributes/    # 自訂 Attribute
  ├── Exception/     # 自訂例外
  └── <Feature>/     # 功能實作
```

## 語言特性

- 啟用 **Nullable Reference Types**（`<Nullable>enable</Nullable>`）
- 啟用 **Implicit Usings**（新專案）
- 目標 `netstandard2.0` + `net10.0` 的核心套件不使用僅限新版 API
- 保持 **Deterministic Builds**（`<Deterministic>true</Deterministic>`）
- 視警告為錯誤（`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`）

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

- 預設使用 **Newtonsoft.Json**（`JsonConvert`）
- 高效能場景使用 **MessagePack**
- 不混用 `System.Text.Json` 與 `Newtonsoft.Json`

## 縮排與格式

- 縮排：**4 個空格**（`.editorconfig` 定義）
- XML 檔案：**2 個空格**
- 行結尾：LF（Unix），避免混用
