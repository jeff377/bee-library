# 源碼掃描規範

本規範涵蓋常見 SAST 工具（CodeQL、SonarQube、Roslyn Analyzers）均會檢查的安全編碼要求。

## SQL 注入防護

- **禁止**字串拼接或 `string.Format` 組合 SQL 語句
- 一律使用 `DbCommandSpec` 的 `{0}` 佔位符傳遞參數，由框架負責參數化

```csharp
// ✅ 正確：使用佔位符
var cmd = new DbCommandSpec(DbCommandKind.Scalar,
    "SELECT COUNT(*) FROM st_user WHERE sys_id = {0}", userId);

// ❌ 禁止：字串拼接
var cmd = new DbCommandSpec(DbCommandKind.Scalar,
    $"SELECT COUNT(*) FROM st_user WHERE sys_id = '{userId}'");
```

## 資源釋放（IDisposable）

- 所有實作 `IDisposable` 的物件必須以 `using` 或 `await using` 管理生命週期
- 禁止手動呼叫 `.Dispose()` 散落在程式碼中（容易因例外而漏釋放）

```csharp
// ✅ 正確
using var conn = DbConnectionManager.CreateConnection("common");
conn.Open();

// ❌ 禁止
var conn = DbConnectionManager.CreateConnection("common");
conn.Open();
// ... 可能因例外而未執行 Dispose
conn.Dispose();
```

## 例外處理

- **禁止** catch `Exception` 或 `SystemException` 等基底型別（會遮蔽非預期錯誤）
- **禁止**空 catch block（吞掉例外，讓錯誤靜默消失）
- catch 後若需重新拋出，使用 `throw;`（保留堆疊），不用 `throw ex;`

```csharp
// ✅ 正確：只捕捉預期的例外型別
try { ... }
catch (SqlException ex) { ... }

// ✅ 正確：重新拋出保留堆疊
catch (IOException ex)
{
    _logger.LogError(ex, "...");
    throw;
}

// ❌ 禁止：過於寬泛
catch (Exception) { }

// ❌ 禁止：空 catch（吞掉例外）
catch { }
```

## XML 安全（XXE 防護）

- 解析 XML 時必須停用 DTD 與外部實體，防止 XML External Entity（XXE）攻擊
- 使用 `XmlReaderSettings` 明確設定安全選項

```csharp
// ✅ 正確
var settings = new XmlReaderSettings
{
    DtdProcessing = DtdProcessing.Prohibit,
    XmlResolver = null
};
using var reader = XmlReader.Create(stream, settings);

// ❌ 禁止：使用預設設定直接解析不受信任的 XML
var doc = new XmlDocument();
doc.Load(untrustedStream);
```

## 亂數安全

- **安全用途**（Token、IV、加密金鑰、驗證碼）一律使用 `RandomNumberGenerator`
- **禁止**將 `System.Random` 用於任何安全相關場景（可預測）

```csharp
// ✅ 正確：安全亂數
var iv = RandomNumberGenerator.GetBytes(16);

// ❌ 禁止：用於安全用途
var rng = new Random();
var iv = new byte[16];
rng.NextBytes(iv);
```

## 路徑安全（Path Traversal 防護）

- 操作使用者提供的檔案路徑前，必須驗證路徑不超出允許的根目錄
- 使用 `Path.GetFullPath()` 正規化後，確認是否在預期目錄範圍內

```csharp
// ✅ 正確
var fullPath = Path.GetFullPath(Path.Combine(allowedRoot, userInput));
if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
    throw new UnauthorizedAccessException("Path traversal detected.");

// ❌ 禁止：直接使用使用者輸入作為路徑
var content = File.ReadAllText(userInput);
```

## 敏感資訊外洩

- API 回應與例外訊息中**禁止**包含堆疊追蹤（stack trace）、內部路徑或系統細節
- Log 中禁止輸出完整 SQL 語句（可能含參數值）
- 已在 `security.md` 中定義的禁止事項（金鑰、Token、密碼）同樣適用
