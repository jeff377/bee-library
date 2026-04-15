# 修改計畫：API 安全性與效能改善（S5/S6/P1/P2/P3/P4）

## 依據

來源文件：`docs/plans/plan-review-api-security-performance.md`

---

## 修改項目總覽

| 優先級 | 編號 | 項目 | 類型 | 影響檔案 |
|--------|------|------|------|----------|
| P1 | S5 | 請求 Body 大小未限制 | 安全性 | `ApiServiceController.cs` |
| P2 | S6 | 解密長度欄位未做邊界檢查 | 安全性 | `AesCbcHmacCryptor.cs` |
| P1 | P1 | HttpClient DNS 快取問題 | 效能 | `HttpFunc.cs` |
| P2 | P2 | HttpClient 未設定 Timeout | 效能 | `HttpFunc.cs` |
| P3 | P3 | CombinedKey 重複拆解 | 效能 | `AesPayloadEncryptor.cs` |
| P3 | P4 | 同步包裝非同步的 ThreadPool 壓力 | 效能 | `RemoteApiServiceProvider.cs` |

---

## S5. 請求 Body 大小未限制

**檔案**：`src/Bee.Api.AspNetCore/Controllers/ApiServiceController.cs`

**現況**：`ReadRequestAsync()` 第 74-75 行使用 `ReadToEndAsync()` 讀取整個請求 Body，無大小限制。攻擊者可發送超大 JSON 請求導致記憶體耗盡。

**修改方式**：在 `PostAsync()` 方法加上 `[RequestSizeLimit]` Attribute，限制請求 Body 上限為 10MB。

```csharp
[HttpPost]
[RequestSizeLimit(10 * 1024 * 1024)]  // 10 MB
public async Task<IActionResult> PostAsync()
```

**選擇理由**：
- 使用 Attribute 而非 Kestrel 全域設定，保持限制只作用於 API 端點，不影響其他可能的端點（如檔案上傳）
- 10MB 與現有 GZip 解壓縮 50MB 限制搭配合理（壓縮前的 JSON 信封遠小於解壓後的 Payload）
- 繼承此 Controller 的子類別若有特殊需求，可覆寫設定

**測試**：無需新增單元測試，此為 ASP.NET Core 框架層 Attribute 行為。

---

## S6. 解密長度欄位未做邊界檢查

**檔案**：`src/Bee.Base/Security/AesCbcHmacCryptor.cs`

**現況**：`Decrypt()` 第 68-71 行直接讀取 `ivLength` 和 `cipherLength` 後呼叫 `ReadBytes()`，未檢查數值合理性。若攻擊者構造惡意資料使長度值為極大數，`ReadBytes()` 會嘗試分配巨大記憶體（雖然 HMAC 最終會驗證失敗，但記憶體分配發生在 HMAC 驗證之前）。

**修改方式**：在讀取 `ivLength` 和 `cipherLength` 後加入邊界檢查：

```csharp
public static byte[] Decrypt(byte[] encryptedData, byte[] aesKey, byte[] hmacKey)
{
    // 最小有效長度：4 (ivLength) + 16 (最小 IV) + 4 (cipherLength) + 16 (最小密文) + 32 (HMAC) = 72
    if (encryptedData == null || encryptedData.Length < 72)
        throw new CryptographicException("Invalid encrypted data.");

    using (var ms = new MemoryStream(encryptedData))
    using (var reader = new BinaryReader(ms))
    {
        int ivLength = reader.ReadInt32();
        if (ivLength < 16 || ivLength > 32)  // AES IV: 128-bit (16) 或 256-bit (32)
            throw new CryptographicException("Invalid IV length.");

        byte[] iv = reader.ReadBytes(ivLength);

        int cipherLength = reader.ReadInt32();
        if (cipherLength <= 0 || cipherLength > encryptedData.Length - ivLength - 40)
            // 40 = 4 (ivLength欄位) + 4 (cipherLength欄位) + 32 (HMAC)
            throw new CryptographicException("Invalid cipher data length.");

        byte[] cipherBytes = reader.ReadBytes(cipherLength);
        byte[] hmacBytes = reader.ReadBytes(32);
        // ... 後續 HMAC 驗證與解密不變
```

**檢查邏輯說明**：
- `ivLength`：AES-128 使用 16 bytes IV，AES-256 也使用 16 bytes IV（block size 固定 128-bit）。保守起見允許 16~32，但實際上本專案固定為 16。
- `cipherLength`：不可為負或零，且不可超過 `encryptedData.Length - ivLength - 40`（扣除兩個 Int32 欄位和 HMAC 的空間）。
- `encryptedData` 本身：加入 null 檢查和最小長度檢查。

**測試**：在 `tests/Bee.Base.UnitTests/` 新增測試案例：
- 傳入過短的 `encryptedData` → 預期拋出 `CryptographicException`
- 構造 `ivLength` 為負數的資料 → 預期拋出 `CryptographicException`
- 構造 `cipherLength` 超過實際資料的資料 → 預期拋出 `CryptographicException`

---

## P1. HttpClient DNS 快取問題

**檔案**：`src/Bee.Base/HttpFunc.cs`

**現況**：`GetOrCreateClient()` 第 31-37 行建立 `HttpClient` 後永久快取，不會因 DNS 變更而重建連線。在容器編排或藍綠部署環境中，後端 IP 可能變更。

**修改方式**：使用 `SocketsHttpHandler` 並設定 `PooledConnectionLifetime`：

```csharp
private static HttpClient GetOrCreateClient(string fullUrl)
{
    var baseUri = new Uri(fullUrl);
    string cacheKey = $"{baseUri.Scheme}://{baseUri.Host}:{baseUri.Port}";

    return _clientMap.GetOrAdd(cacheKey, _ =>
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        };
        return new HttpClient(handler)
        {
            BaseAddress = new Uri($"{baseUri.Scheme}://{baseUri.Host}:{baseUri.Port}/")
        };
    });
}
```

**說明**：
- 專案已全面遷移至 `net10.0`，不需要 `#if NETSTANDARD2_0` 條件編譯
- `PooledConnectionLifetime = 5 分鐘`：連線池中的連線超過 5 分鐘後會被回收並重新建立，自動解析最新 DNS
- `HttpClient` 實例本身仍然永久快取（避免 Socket Exhaustion），只有底層連線會定期更新

**測試**：無需新增單元測試，此為 `SocketsHttpHandler` 框架層行為。

---

## P2. HttpClient 未設定 Timeout

**檔案**：`src/Bee.Base/HttpFunc.cs`

**現況**：`HttpClient` 使用預設 Timeout（100 秒），對於一般 API 呼叫過長。

**修改方式**：與 P1 合併修改，在建立 `HttpClient` 時設定 Timeout：

```csharp
return new HttpClient(handler)
{
    BaseAddress = new Uri($"{baseUri.Scheme}://{baseUri.Host}:{baseUri.Port}/"),
    Timeout = TimeSpan.FromSeconds(30)
};
```

**說明**：
- 30 秒對一般 JSON-RPC API 呼叫已足夠
- 若後續有需要更長 Timeout 的場景（如大型檔案傳輸），可考慮在 `PostAsync`/`GetAsync` 方法加入 `CancellationToken` 參數讓呼叫端自行控制，但目前不在本次修改範圍

**測試**：無需新增單元測試。

---

## P3. CombinedKey 重複拆解

**檔案**：`src/Bee.Api.Core/Transformer/AesPayloadEncryptor.cs`

**現況**：每次 `Encrypt()` 和 `Decrypt()` 都呼叫 `AesCbcHmacKeyGenerator.FromCombinedKey()` 拆解金鑰。`FromCombinedKey()` 實際上只做兩次 `Buffer.BlockCopy`，效能影響極微。

**修改方式**：不修改。

**理由**：
1. `FromCombinedKey()` 僅為兩次 `Buffer.BlockCopy`（共 64 bytes），開銷可忽略
2. `AesPayloadEncryptor` 實作 `IApiPayloadEncryptor` 介面，方法簽章為 `Encrypt(byte[] bytes, byte[] encryptionKey)` — 金鑰由呼叫端傳入，Encryptor 本身是無狀態的設計
3. 若要快取拆解結果，需要改變介面契約或讓 Encryptor 變為有狀態，增加複雜度但收益極低
4. 原始檢視報告也提到「此項影響微乎其微，可視需求決定是否調整」

---

## P4. 同步包裝非同步的 ThreadPool 壓力

**檔案**：`src/Bee.Api.Client/ApiServiceProvider/RemoteApiServiceProvider.cs`

**現況**：第 56 行使用 `Task.Run(() => ExecuteAsync(request)).GetAwaiter().GetResult()`，在高並發時可能造成 ThreadPool 壓力。但程式碼註解已說明此選擇是為了避免 UI 執行緒死結。

**修改方式**：為 `Execute()` 方法加上 `[Obsolete]` 標記，引導呼叫端使用 `ExecuteAsync()`：

```csharp
/// <summary>
/// Executes an API method synchronously.
/// </summary>
/// <param name="request">The JSON-RPC request model.</param>
/// <remarks>
/// Prefer <see cref="ExecuteAsync"/> for better performance and resource utilization.
/// This synchronous wrapper uses <c>Task.Run</c> to avoid UI thread deadlocks,
/// but may cause ThreadPool pressure under high concurrency on server-side scenarios.
/// </remarks>
[Obsolete("Use ExecuteAsync for better performance. This synchronous method is retained only for UI thread compatibility.")]
public JsonRpcResponse Execute(JsonRpcRequest request)
{
    return Task.Run(() => ExecuteAsync(request)).GetAwaiter().GetResult();
}
```

**說明**：
- `RemoteApiServiceProvider` 是客戶端元件（`Bee.Api.Client`），主要用於 WinForms/MAUI 等 UI 應用程式
- 在 UI 場景下 `Task.Run` 是避免死結的合理折衷，不應移除
- 加上 `[Obsolete]` 可在編譯時產生警告，提醒開發者優先使用非同步版本
- 同時需確認 `IJsonRpcProvider` 介面是否定義了 `Execute()` 方法 — 若有，需評估是否也標記 `[Obsolete]`

**前置確認**：需檢查 `IJsonRpcProvider` 介面定義，確認 `Execute()` 是否為介面方法。若是，則 `[Obsolete]` 需加在介面層級。

**測試**：無需新增單元測試。

---

## 執行順序

1. **S5** + **P1** + **P2**（P1 和 P2 在同一檔案，一起修改）
2. **S6**（含新增測試）
3. **P4**（確認介面後加 Obsolete）
4. **P3**（不修改）

## 不在範圍內

- S1（Type.GetType 白名單）、S2（暴力破解防護）、S3（API Key 驗證）、S4（登入密碼加密）— 這些項目不在本次修改範圍
