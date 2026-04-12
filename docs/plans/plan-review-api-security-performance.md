# API 前後端傳輸安全性與效能檢視報告

## 檢視範圍

針對 Bee.NET API 前後端傳輸管線（Serialization → Compression → Encryption），涵蓋以下元件：

| 元件 | 檔案 |
|------|------|
| 加密核心 | `Bee.Base/Security/AesCbcHmacCryptor.cs` |
| 加密封裝 | `Bee.Api.Core/Transformer/AesPayloadEncryptor.cs` |
| 壓縮核心 | `Bee.Base/Serialization/GZipFunc.cs` |
| 轉換管線 | `Bee.Api.Core/Transformer/ApiPayloadTransformer.cs` |
| 格式轉換 | `Bee.Api.Core/JsonRpc/ApiPayloadConverter.cs` |
| 伺服器端點 | `Bee.Api.AspNetCore/Controllers/ApiServiceController.cs` |
| 請求執行器 | `Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs` |
| 客戶端連接器 | `Bee.Api.Client/Connectors/ApiConnector.cs` |
| 遠端服務提供者 | `Bee.Api.Client/ApiServiceProvider/RemoteApiServiceProvider.cs` |
| HTTP 工具 | `Bee.Base/HttpFunc.cs` |
| 授權驗證 | `Bee.Api.Core/Authorization/ApiAuthorizationValidator.cs` |
| 存取控制 | `Bee.Api.Core/Validator/ApiAccessValidator.cs` |
| 登入流程 | `Bee.Api.Client/Connectors/SystemApiConnector.cs` |
| 金鑰保護 | `Bee.Definition/Security/EncryptionKeyProtector.cs` |
| 反序列化白名單 | `Bee.Definition/Serialization/SafeTypelessFormatter.cs` |

---

## 架構現況評估

### 做得好的地方

整體架構在安全性設計上已有良好基礎：

1. **加密管線完整**：Serialize → Compress → Encrypt 順序正確，解密時反向操作
2. **AES-256-CBC + HMAC-SHA256**：使用獨立金鑰，符合 Encrypt-then-MAC 模式
3. **常數時間比較**：`CompareBytes()` 使用 XOR 累積比較防止 Timing Attack
4. **隨機 IV**：每次加密生成新 IV，避免重複使用
5. **反序列化白名單**：`SafeTypelessFormatter` 雙層防護（pre/post instantiation）
6. **Zip Bomb 防護**：解壓縮限制 50MB 上限
7. **RSA 金鑰交換**：登入時使用 RSA-2048 + OAEP-SHA256 交換 Session Key
8. **Per-Session 加密金鑰**：每次登入產生獨立加密金鑰
9. **密碼雜湊**：PBKDF2-SHA256，100,000 次迭代
10. **錯誤訊息分級**：`IsUserFacingException()` 避免洩漏內部資訊

---

## 安全性問題

### S1. `ApiPayloadConverter.RestoreFrom()` 的 `Type.GetType()` 未驗證型別白名單

**嚴重度：高**

**位置**：`Bee.Api.Core/JsonRpc/ApiPayloadConverter.cs:75`

```csharp
var type = Type.GetType(payload.TypeName);
```

`TypeName` 由客戶端透過 JSON-RPC Payload 傳入，伺服器端直接呼叫 `Type.GetType()` 載入該型別，**未經任何白名單驗證**。雖然 MessagePack 層的 `SafeTypelessFormatter` 有做反序列化白名單檢查，但 `Type.GetType()` 本身就會觸發 Assembly 載入，且取得的 `type` 隨後被傳入 `Decode()` 進行反序列化——這繞過了 `SafeTypelessFormatter` 的防護層。

**風險**：攻擊者可構造惡意 `TypeName` 指向危險型別，觸發非預期的型別載入或反序列化行為。

**建議修改**：在 `RestoreFrom()` 呼叫 `Type.GetType()` 之前，加入型別白名單驗證：

```csharp
// 驗證 TypeName 是否在允許的命名空間內
if (!SafeTypelessFormatter.IsTypeAllowed(payload.TypeName)
    && !SysInfo.IsTypeNameAllowed(payload.TypeName))
{
    throw new InvalidOperationException($"Type '{payload.TypeName}' is not allowed for deserialization.");
}

var type = Type.GetType(payload.TypeName);
```

注意：`TypeName` 格式為 `"Namespace.TypeName, AssemblyName"`，驗證時需解析出完整型別名稱（不含 Assembly 部分）再做比對。

---

### S2. 登入 API 缺少暴力破解防護

**嚴重度：高**

**位置**：`Bee.Api.Core/Authorization/ApiAuthorizationValidator.cs`、`Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs`

`System.Login` 被列為 `NoAuthMethods`，不需要 Bearer Token 即可呼叫。目前整個系統**沒有任何登入失敗次數限制、帳號鎖定、或速率限制機制**。

**風險**：攻擊者可無限制地嘗試登入，進行暴力破解或字典攻擊。

**建議修改**：

1. **方案 A — 應用層**：在 Login 的 Business Object 實作中加入失敗計數器，超過閾值（如 5 次）後鎖定帳號一段時間
2. **方案 B — 中介層**：在 `ApiServiceController` 或 ASP.NET Core Middleware 層加入 Rate Limiting（如 `Microsoft.AspNetCore.RateLimiting`），針對 `System.Login` 方法限制每 IP 的呼叫頻率

---

### S3. API Key 僅檢查「有無提供」，未驗證正確性

**嚴重度：中**

**位置**：`Bee.Api.Core/Authorization/ApiAuthorizationValidator.cs:46`

```csharp
if (string.IsNullOrWhiteSpace(context.ApiKey))
{
    return ApiAuthorizationResult.Fail(JsonRpcErrorCode.InvalidRequest, "Missing or invalid API key.");
}
```

驗證邏輯只檢查 API Key 是否為空白，**從未驗證 API Key 是否與伺服器端儲存的合法金鑰相符**。這意味著任何非空字串都能通過 API Key 驗證。

**風險**：API Key 形同虛設，無法達到預期的存取控制效果。

**建議修改**：`ApiAuthorizationValidator.Validate()` 應加入 API Key 的正確性比對：

```csharp
// 使用常數時間比較避免 Timing Attack
if (!FixedTimeEquals(context.ApiKey, expectedApiKey))
{
    return ApiAuthorizationResult.Fail(JsonRpcErrorCode.InvalidRequest, "Invalid API key.");
}
```

如果需要支援多組 API Key，可使用 `HashSet` 儲存合法的 Key 清單。

---

### S4. 登入密碼在 `Encoded` 模式下僅壓縮未加密

**嚴重度：中**

**位置**：`Bee.Api.Client/Connectors/SystemApiConnector.cs:192`

```csharp
var result = await ExecuteAsync<LoginResponse>(SystemActions.Login, request, PayloadFormat.Encoded);
```

`LoginRequest` 包含使用者密碼，但登入時使用 `PayloadFormat.Encoded`（序列化 + 壓縮，無加密）。這是因為登入時尚未取得 Session Key，無法使用 `Encrypted` 模式。

**風險**：若傳輸層未使用 HTTPS，密碼可被中間人攔截。即使使用 HTTPS，在網路邊界（如 Load Balancer 卸載 TLS）仍可能暴露。

**建議修改**：

客戶端已在登入時產生 RSA Key Pair 用於接收 Session Key，可以擴展此機制：使用同一組 RSA 公鑰在客戶端預先加密密碼，伺服器端用客戶端的公鑰解密——不對，方向反了，客戶端沒有伺服器公鑰。

正確做法：
1. **新增 Pre-Login 階段**：客戶端先呼叫一個不需認證的 API 取得伺服器的 RSA 公鑰，然後用此公鑰加密密碼後再發送登入請求
2. **或者強制 HTTPS**：在部署文件中明確要求所有 API 端點必須使用 HTTPS，並在伺服器端加入 HTTPS 檢查（拒絕 HTTP 請求）

---

### S5. 請求 Body 大小未限制

**嚴重度：中**

**位置**：`Bee.Api.AspNetCore/Controllers/ApiServiceController.cs:75`

```csharp
var json = await reader.ReadToEndAsync();
```

`ReadToEndAsync()` 會讀取整個請求 Body 進記憶體，**沒有設定大小限制**。雖然 GZip 解壓縮有 50MB 限制，但 JSON-RPC 的外層信封（envelope）在解壓縮之前就已完整讀入記憶體。

**風險**：攻擊者可發送極大的 JSON 請求體，造成伺服器記憶體耗盡（DoS）。

**建議修改**：

在 Controller 或 Middleware 層加入請求大小限制：

```csharp
[RequestSizeLimit(10 * 1024 * 1024)]  // 10 MB
[HttpPost]
public async Task<IActionResult> PostAsync() { ... }
```

或在 `Program.cs` 全域設定：

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
});
```

---

### S6. `AesCbcHmacCryptor.Decrypt()` 解析長度欄位前未做邊界檢查

**嚴重度：低**

**位置**：`Bee.Base/Security/AesCbcHmacCryptor.cs:63-72`

```csharp
int ivLength = reader.ReadInt32();
byte[] iv = reader.ReadBytes(ivLength);
int cipherLength = reader.ReadInt32();
byte[] cipherBytes = reader.ReadBytes(cipherLength);
```

若攻擊者提供惡意構造的加密資料，`ivLength` 或 `cipherLength` 可能為極大值（如 `Int32.MaxValue`），導致 `ReadBytes()` 嘗試分配巨大記憶體。雖然 HMAC 驗證最終會失敗，但記憶體分配在 HMAC 驗證之前發生。

**建議修改**：加入合理的長度檢查：

```csharp
int ivLength = reader.ReadInt32();
if (ivLength < 0 || ivLength > 256)  // AES IV 最長 256-bit
    throw new CryptographicException("Invalid IV length.");

byte[] iv = reader.ReadBytes(ivLength);

int cipherLength = reader.ReadInt32();
if (cipherLength < 0 || cipherLength > encryptedData.Length)
    throw new CryptographicException("Invalid cipher length.");
```

---

## 效能問題

### P1. `HttpClient` 快取永不失效，無法處理 DNS 變更

**嚴重度：中**

**位置**：`Bee.Base/HttpFunc.cs:31`

```csharp
return _clientMap.GetOrAdd(cacheKey, _ =>
{
    return new HttpClient { BaseAddress = new Uri(...) };
});
```

`HttpClient` 被永久快取在 `ConcurrentDictionary` 中，不會因 DNS 變更而重新建立連線。在雲端環境中（如容器編排、藍綠部署），後端 IP 可能頻繁變更。

**建議修改**：

改用 `SocketsHttpHandler`（.NET Core 2.1+）並設定 `PooledConnectionLifetime`：

```csharp
private static HttpClient GetOrCreateClient(string fullUrl)
{
    var baseUri = new Uri(fullUrl);
    string cacheKey = $"{baseUri.Scheme}://{baseUri.Host}:{baseUri.Port}";

    return _clientMap.GetOrAdd(cacheKey, _ =>
    {
#if NETSTANDARD2_0
        return new HttpClient
        {
            BaseAddress = new Uri($"{baseUri.Scheme}://{baseUri.Host}:{baseUri.Port}/")
        };
#else
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        };
        return new HttpClient(handler)
        {
            BaseAddress = new Uri($"{baseUri.Scheme}://{baseUri.Host}:{baseUri.Port}/")
        };
#endif
    });
}
```

或者在 ASP.NET Core 環境中使用 `IHttpClientFactory` 取代手動快取。

---

### P2. `HttpClient` 未設定 Timeout

**嚴重度：中**

**位置**：`Bee.Base/HttpFunc.cs:33`

`HttpClient` 建立時未設定 `Timeout`，使用預設值 100 秒。對於 Ping 等輕量操作過長，對於大型資料傳輸可能不足。

**建議修改**：

允許呼叫端指定 Timeout，或至少設定合理預設值：

```csharp
return new HttpClient(handler)
{
    BaseAddress = new Uri($"{baseUri.Scheme}://{baseUri.Host}:{baseUri.Port}/"),
    Timeout = TimeSpan.FromSeconds(30) // 或可配置
};
```

---

### P3. `AesPayloadEncryptor` 每次加解密重複拆解 CombinedKey

**嚴重度：低**

**位置**：`Bee.Api.Core/Transformer/AesPayloadEncryptor.cs:29, 45`

```csharp
AesCbcHmacKeyGenerator.FromCombinedKey(encryptionKey, out var aesKey, out var hmacKey);
```

每次 `Encrypt()` 和 `Decrypt()` 呼叫都會重新從 CombinedKey 拆解出 AES Key 和 HMAC Key。在高頻 API 呼叫場景下，這是不必要的重複操作。

**建議修改**：

考慮在 Session 層級快取拆解後的金鑰，或讓 `AesPayloadEncryptor` 持有狀態：

```csharp
// 在 SessionInfo 或專用快取中預先拆解
public class CachedEncryptionKey
{
    public byte[] AesKey { get; }
    public byte[] HmacKey { get; }

    public CachedEncryptionKey(byte[] combinedKey)
    {
        AesCbcHmacKeyGenerator.FromCombinedKey(combinedKey, out var aes, out var hmac);
        AesKey = aes;
        HmacKey = hmac;
    }
}
```

但此項影響微乎其微（`FromCombinedKey` 僅為 `Array.Copy`），可視需求決定是否調整。

---

### P4. 同步方法使用 `Task.Run().GetAwaiter().GetResult()` 可能造成執行緒池壓力

**嚴重度：低**

**位置**：`Bee.Api.Client/ApiServiceProvider/RemoteApiServiceProvider.cs:56`

```csharp
return Task.Run(() => ExecuteAsync(request)).GetAwaiter().GetResult();
```

同步包裝非同步方法時使用 `Task.Run` 會佔用一個額外的 ThreadPool 執行緒。在高並發場景下可能造成 ThreadPool 飢餓。

**現況評估**：已在註解中說明此選擇是為了避免 UI 執行緒死結，是合理的折衷。但在伺服器端應盡量避免使用同步方法。

**建議**：鼓勵呼叫端優先使用 `ExecuteAsync()`，將同步方法標記為 `[Obsolete]` 或在文件中建議僅在 UI 層使用。

---

## 建議優先級

| 優先級 | 編號 | 項目 | 類型 |
|--------|------|------|------|
| **P0** | S1 | `Type.GetType()` 未驗證白名單 | 安全性 |
| **P0** | S2 | 登入缺少暴力破解防護 | 安全性 |
| **P1** | S3 | API Key 未驗證正確性 | 安全性 |
| **P1** | S4 | 密碼在 Encoded 模式下未加密 | 安全性 |
| **P1** | S5 | 請求 Body 大小未限制 | 安全性 |
| **P2** | S6 | 解密長度欄位未做邊界檢查 | 安全性 |
| **P1** | P1 | HttpClient DNS 快取問題 | 效能 |
| **P2** | P2 | HttpClient 未設定 Timeout | 效能 |
| **P3** | P3 | CombinedKey 重複拆解 | 效能 |
| **P3** | P4 | 同步包裝非同步可能的 ThreadPool 壓力 | 效能 |

---

## 總結

Bee.NET 的 API 傳輸管線在加密架構上設計良好（AES-CBC-HMAC、RSA 金鑰交換、反序列化白名單、Zip Bomb 防護），但在**應用層安全**（暴力破解防護、API Key 驗證、型別白名單一致性、請求大小限制）與**HTTP 基礎設施**（HttpClient 生命週期、Timeout）方面仍有改善空間。

建議優先處理 S1（`Type.GetType` 白名單）和 S2（登入暴力破解防護），這兩項風險最高且修改範圍明確。
