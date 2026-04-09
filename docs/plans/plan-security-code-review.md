# Bee.NET 原始碼資訊安全審查報告

**審查日期**：2026-04-09
**審查範圍**：`src/` 目錄下所有安全相關程式碼
**審查人**：Claude Code (AI Security Review)

---

## 摘要

本次安全審查涵蓋加密實作、認證與 Session 管理、序列化/反序列化、API 存取控制、資料庫存取、錯誤處理與組態管理等面向。整體架構設計良好，但發現 **5 個高風險**、**8 個中風險** 及 **4 個低風險** 問題。

| 風險等級 | 數量 |
|---------|------|
| 🔴 高風險 (High) | 5 |
| 🟠 中風險 (Medium) | 8 |
| 🟡 低風險 (Low) | 4 |

---

## 🔴 高風險問題

### H-1：RSA 使用 PKCS#1 v1.5 填充（Padding Oracle 風險）

- **檔案**：`src/Bee.Core/Security/RsaCryptor.cs:44, 62`
- **問題**：
  ```csharp
  var encrypted = rsa.Encrypt(data, false);  // Line 44 — false = PKCS#1 v1.5
  var decrypted = rsa.Decrypt(data, false);  // Line 62 — false = PKCS#1 v1.5
  ```
  `Encrypt`/`Decrypt` 的第二個參數為 `false`，代表使用 **PKCS#1 v1.5** 填充而非 OAEP。PKCS#1 v1.5 已知容易受到 **Bleichenbacher Padding Oracle Attack**，攻擊者可透過觀察解密錯誤回應逐步還原明文。
- **影響**：登入憑證的 RSA 加密可被離線破解。
- **建議修復**：
  ```csharp
  // 改為使用 RSAEncryptionPadding.OaepSHA256
  using var rsa = RSA.Create(2048);
  rsa.ImportFromXml(publicKeyXml);
  var encrypted = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
  ```

---

### H-2：MessagePack 使用 `TypelessContractlessStandardResolver`（不安全反序列化）

- **檔案**：`src/Bee.Api.Core/MessagePack/MessagePackHelper.cs:32`
- **問題**：
  ```csharp
  var resolver = CompositeResolver.Create(
      new IMessagePackFormatter[] { ... },
      new IFormatterResolver[]
      {
          TypelessContractlessStandardResolver.Instance, // ⚠ 危險
          ...
      });
  ```
  `TypelessContractlessStandardResolver` 允許在 MessagePack 資料中嵌入**任意型別資訊**，反序列化時會自動實例化對應的 .NET 型別。攻擊者可構造惡意 payload，觸發任意程式碼執行（Remote Code Execution, RCE）。
- **影響**：API 端點接收外部 MessagePack 資料時，可能被利用進行 RCE 攻擊。
- **對照**：JSON 部分已實作 `JsonSerializationBinder` 白名單（`SysInfo.IsTypeNameAllowed`），但 MessagePack 端**完全沒有型別限制**。
- **建議修復**：
  1. 移除 `TypelessContractlessStandardResolver`，改用明確的 Resolver
  2. 若必須支援多型，實作自訂的 `IFormatterResolver`，僅允許已知安全型別
  3. 對齊 JSON 序列化已有的白名單機制

---

### H-3：`AesPayloadEncryptor` 在 Key 為空時靜默跳過加密

- **檔案**：`src/Bee.Api.Core/Transformer/AesPayloadEncryptor.cs:25, 40`
- **問題**：
  ```csharp
  public byte[] Encrypt(byte[] bytes, byte[] encryptionKey)
  {
      if (encryptionKey == null || encryptionKey.Length == 0) { return bytes; } // ⚠ 靜默跳過
      ...
  }
  ```
  當 `encryptionKey` 為 `null` 或空陣列時，加密方法**直接回傳明文**，沒有任何警告或例外。若因配置錯誤或程式 bug 導致 Key 未正確傳入，所有標示為 `Encrypted` 保護等級的 API 資料將以**明文傳輸**，形成靜默降級（Silent Downgrade）。
- **影響**：資料在傳輸過程中未加密，中間人攻擊可讀取敏感資訊。
- **建議修復**：
  ```csharp
  public byte[] Encrypt(byte[] bytes, byte[] encryptionKey)
  {
      if (encryptionKey == null || encryptionKey.Length == 0)
          throw new CryptographicException("Encryption key must not be null or empty.");
      ...
  }
  ```

---

### H-4：Session 到期驗證使用 `DateTime.Now` 與 `DateTime.UtcNow` 不一致

- **檔案**：`src/Bee.Repository/System/SessionRepository.cs:67` vs `:102`
- **問題**：
  ```csharp
  // 建立 Session 時使用 UtcNow（Line 102）
  EndTime = DateTime.UtcNow.AddSeconds(expiresIn),

  // 驗證到期時使用 Now（Line 67）
  if (endTime < DateTime.Now)
  ```
  Session 建立時以 `DateTime.UtcNow` 設定到期時間，但驗證時以 `DateTime.Now`（本地時間）比較。
- **影響**：
  - 伺服器時區在 UTC 之後（如 UTC+8）：Session 會**延長 8 小時**才過期
  - 伺服器時區在 UTC 之前（如 UTC-5）：Session 會**提前 5 小時**過期
  - 時區差異可能被攻擊者利用，延長已失效 Token 的有效期
- **建議修復**：統一使用 `DateTime.UtcNow`。

---

### H-5：`CreateSession` API 為匿名存取且無速率限制

- **檔案**：`src/Bee.Business/BusinessObjects/SystemBusinessObject.cs:125-137`
- **問題**：
  ```csharp
  [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Anonymous)]
  public virtual CreateSessionResult CreateSession(CreateSessionArgs args)
  {
      var user = repo.CreateSession(args.UserID, args.ExpiresIn, args.OneTime);
      ...
  }
  ```
  `CreateSession` 標示為 `Public` + `Anonymous`，任何人可以不經認證呼叫。且 `CreateSessionArgs` 允許客戶端指定任意 `UserID` 和 `ExpiresIn`（到期秒數）。沒有速率限制、沒有驗證呼叫者身分。
- **影響**：
  - 攻擊者可為任意使用者建立大量 Session（資源耗盡 DoS）
  - 可指定超長 `ExpiresIn` 建立永不過期的 Token
  - 可對 `st_session` 資料表進行大量寫入（資料庫 DoS）
- **建議修復**：
  1. 加入速率限制（Rate Limiting）
  2. 限制 `ExpiresIn` 的最大值
  3. 考慮是否需要認證才能呼叫 `CreateSession`

---

## 🟠 中風險問題

### M-1：`NoEncryptionEncryptor` 可在正式環境啟用

- **檔案**：`src/Bee.Api.Core/Transformer/ApiPayloadOptionsFactory.cs:59-61`
- **問題**：
  ```csharp
  case "none":
  case "":
      return new NoEncryptionEncryptor();
  ```
  Factory 允許透過配置將加密器設定為 `"none"`，完全關閉加密。根據安全規範，`NoEncryptionEncryptor` 應**僅限測試環境使用**，但目前程式碼沒有任何環境限制。
- **建議修復**：加入環境檢查，在非 Debug/Development 模式下拋出例外或記錄警告。

---

### M-2：PBKDF2 使用 SHA-1 作為 PRF

- **檔案**：`src/Bee.Core/Security/PasswordHasher.cs:68`
- **問題**：
  ```csharp
  using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
  ```
  此建構子**預設使用 SHA-1** 作為偽隨機函數（PRF）。雖然 PBKDF2-SHA1 配合 100,000 次迭代目前仍可接受，但 NIST SP 800-132 建議使用 SHA-256 以上。
- **建議修復**：
  ```csharp
  using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
  ```

---

### M-3：GZip 解壓縮無大小限制（Zip Bomb 風險）

- **檔案**：`src/Bee.Core/Serialization/GZipFunc.cs:30-49`
- **問題**：
  ```csharp
  public static byte[] Decompress(byte[] bytes)
  {
      // ... 無限制地將解壓結果寫入 MemoryStream
      while ((count = gZipStream.Read(buffer, 0, buffer.Length)) > 0)
      {
          outputStream.Write(buffer, 0, count);
      }
      return outputStream.ToArray();
  }
  ```
  解壓縮過程沒有檢查解壓後的大小上限。攻擊者可送出極小的壓縮 payload（如 1KB）解壓後產生 GB 等級的資料（Zip Bomb / Decompression Bomb），導致記憶體耗盡（OOM）與服務中斷（DoS）。
- **建議修復**：加入最大解壓大小限制（如 50MB），超過時拋出例外。

---

### M-4：ASP.NET（非 Core）版本洩漏例外訊息

- **檔案**：`src/Bee.Api.AspNet/HttpModules/ApiServiceModule.cs:143-144`
- **問題**：
  ```csharp
  catch (Exception ex)
  {
      WriteErrorResponse(context, 500, (int)JsonRpcErrorCode.InternalError,
          "Internal server error", request.Id, ex.InnerException?.Message ?? ex.Message);
  }
  ```
  ASP.NET 版本**無條件**將例外訊息回傳給客戶端。相較之下，ASP.NET Core 版本已正確實作環境判斷（僅 Development 回傳詳細錯誤）。例外訊息可能包含資料庫結構、檔案路徑、堆疊追蹤等敏感資訊。
- **對照**（正確做法）：`src/Bee.Api.AspNetCore/Controllers/ApiServiceController.cs:143-145`
  ```csharp
  string message = IsDevelopment ? rootEx.Message : string.Empty;
  ```
- **建議修復**：ASP.NET 版本也應依環境判斷是否回傳詳細錯誤。

---

### M-5：`JsonRpcExecutor` 直接回傳例外訊息給客戶端

- **檔案**：`src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs:86-88`
- **問題**：
  ```csharp
  catch (Exception ex)
  {
      var rootEx = BaseFunc.UnwrapException(ex);
      string message = rootEx.Message;
      response.Error = new JsonRpcError(-1, message);
  }
  ```
  `JsonRpcExecutor` 將解包後的例外訊息**直接**設為 JSON-RPC 錯誤回應。這意味著即使上層（ASP.NET Core Controller）有環境保護，如果錯誤發生在 `ExecuteAsyncCore` 的 try/catch 內部（大多數業務邏輯錯誤），詳細訊息仍然會洩漏。
- **建議修復**：區分業務邏輯例外（可回傳使用者友好訊息）與系統例外（僅回傳通用錯誤訊息）。

---

### M-6：One-Time Token 存在 Race Condition（TOCTOU）

- **檔案**：`src/Bee.Repository/System/SessionRepository.cs:75-76`
- **問題**：
  ```csharp
  var user = SerializeFunc.XmlToObject<SessionUser>(xml);
  if (user.OneTime) { this.Delete(accessToken); }
  return user;
  ```
  一次性 Token 的查詢與刪除不是原子操作。在高併發情境下，多個請求可同時讀取同一 Token，其中只有一個會成功刪除，其餘請求仍可使用該 Token。
- **影響**：一次性 Token 可被重複使用，違反設計意圖。
- **建議修復**：使用資料庫交易（Transaction）或 `DELETE ... OUTPUT` 確保原子性。

---

### M-7：`AccessTokenValidationProvider` 未明確檢查 Token 到期時間

- **檔案**：`src/Bee.Business/Validator/AccessTokenValidationProvider.cs:18-31`
- **問題**：
  ```csharp
  public bool ValidateAccessToken(Guid accessToken)
  {
      var sessionInfo = BackendInfo.SessionInfoService.Get(accessToken);
      if (sessionInfo == null)
          throw new UnauthorizedAccessException("Session key not found or expired.");
      return sessionInfo.AccessToken == accessToken;
  }
  ```
  驗證邏輯僅檢查 SessionInfo 是否存在和 Token 是否匹配，**沒有明確檢查 `ExpiredAt`**。依賴快取層或資料庫層隱式移除過期 Session。若快取（預設 20 分鐘滑動到期）與 Token 實際到期時間不同步，已過期 Token 可能仍被視為有效。
- **建議修復**：在 `ValidateAccessToken` 中加入 `if (sessionInfo.ExpiredAt < DateTime.UtcNow)` 檢查。

---

### M-8：Login 預設實作無條件通過驗證

- **檔案**：`src/Bee.Business/BusinessObjects/SystemBusinessObject.cs:115-119`
- **問題**：
  ```csharp
  protected virtual bool AuthenticateUser(LoginArgs args, out string userName)
  {
      userName = "Demo User";
      return true; // Default passes; override in subclasses to implement real validation
  }
  ```
  作為 Framework 的預設實作，`AuthenticateUser` 無條件回傳 `true`。雖然設計意圖是由子類別覆寫，但若開發者忘記覆寫，任何人可以用任何密碼登入系統。
- **建議修復**：預設回傳 `false` 或拋出 `NotImplementedException`，強制子類別實作驗證邏輯。

---

## 🟡 低風險問題

### L-1：RSA 金鑰長度硬編碼為 2048 位元

- **檔案**：`src/Bee.Core/Security/RsaCryptor.cs:23, 39, 57`
- **問題**：RSA 金鑰固定為 2048-bit。雖然目前仍可接受，但 NIST 建議 2030 年後使用 3072-bit 以上。
- **建議**：升級至 3072-bit 或使金鑰長度可配置。

---

### L-2：`ApiException` 包含 StackTrace 並可被序列化

- **檔案**：`src/Bee.Core/ApiException.cs:28, 43`
- **問題**：
  ```csharp
  public ApiException(Exception exception)
  {
      Message = exception.Message;
      StackTrace = exception.StackTrace; // ⚠ 堆疊追蹤
  }
  ```
  `ApiException` 是可序列化的 DTO，包含完整堆疊追蹤。若此物件被回傳給客戶端，將洩漏伺服器內部架構資訊。
- **建議**：非 Debug 模式下不設定 `StackTrace` 屬性。

---

### L-3：`RSACryptoServiceProvider` 已被標記為過時

- **檔案**：`src/Bee.Core/Security/RsaCryptor.cs:23, 39, 57`
- **問題**：使用已標記為 `[Obsolete]` 的 `RSACryptoServiceProvider`。現代 .NET 建議使用 `RSA.Create()` 工廠方法。
- **建議**：遷移至 `RSA.Create()` 以獲得更好的跨平台支援與演算法支援。

---

### L-4：`FileHashValidator.VerifySha256` 未使用常數時間比較

- **檔案**：`src/Bee.Core/Security/FileHashValidator.cs:28`
- **問題**：
  ```csharp
  return string.Equals(actualHex, expectedSha256Hex, StringComparison.OrdinalIgnoreCase);
  ```
  使用 `string.Equals` 比較雜湊值，可能產生時序差異（Timing Side-Channel）。不過檔案完整性驗證通常是離線操作，實際被利用的風險很低。
- **建議**：改用固定時間比較，與專案其他安全實作保持一致。

---

## ✅ 正面發現（設計良好之處）

| 元件 | 評估 |
|------|------|
| AES-256-CBC + HMAC-SHA256 | ✅ 正確實作 Encrypt-then-MAC，隨機 IV，常數時間比較 |
| `AesCbcHmacKeyGenerator` | ✅ 使用 `RandomNumberGenerator`（CSPRNG），獨立的 AES/HMAC 金鑰 |
| `PasswordHasher` | ✅ PBKDF2 + 100,000 次迭代 + 128-bit 隨機鹽 + 常數時間比較 |
| JSON 序列化白名單 | ✅ `JsonSerializationBinder` + `SysInfo.IsTypeNameAllowed` 限制可反序列化型別 |
| SQL 參數化查詢 | ✅ `DbCommandSpec` 使用 `{0}` 佔位符自動轉為資料庫參數（`@p0`），非字串串接 |
| API 存取控制 | ✅ 三層保護等級 + `ApiAccessValidator` 強制驗證 + 缺少 Attribute 時預設拒絕 |
| Session 管理 | ✅ GUID Token（不可預測）+ 到期時間 + 一次性 Token 支援 |
| Master Key 管理 | ✅ `.gitignore` 已排除 `Master.key`，支援環境變數載入 |
| 金鑰加密儲存 | ✅ `EncryptionKeyProtector` 使用 Master Key 加密其他金鑰 |
| ASP.NET Core 錯誤處理 | ✅ 僅在 Development 環境回傳例外訊息 |

---

## 修復優先順序建議

| 優先順序 | 項目 | 工作量評估 |
|---------|------|-----------|
| 1 | H-2：MessagePack Typeless Resolver → 型別白名單 | 中（需設計 Resolver） |
| 2 | H-1：RSA PKCS#1 → OAEP + RSA.Create() | 小（改 2 行 + 測試） |
| 3 | H-3：AesPayloadEncryptor 空 Key 改為拋出例外 | 小（改 2 行 + 測試） |
| 4 | H-4：DateTime.Now → DateTime.UtcNow 統一 | 小（改 1 行 + 測試） |
| 5 | H-5：CreateSession 加入速率限制與 ExpiresIn 上限 | 中 |
| 6 | M-8：AuthenticateUser 預設改為 false | 小（改 1 行） |
| 7 | M-7：AccessTokenValidationProvider 加入到期檢查 | 小（加 2 行） |
| 8 | M-6：One-Time Token 原子性修復 | 中（需改 DB 交易） |
| 9 | M-1：NoEncryptionEncryptor 加入環境限制 | 小 |
| 10 | M-3：GZip 解壓加入大小上限 | 小 |
| 11 | M-4：ASP.NET 版本錯誤處理對齊 Core 版本 | 小 |
| 12 | M-5：JsonRpcExecutor 區分業務/系統例外 | 中 |
| 13 | M-2：PBKDF2 SHA-1 → SHA-256 | 小（需考慮既有密碼相容） |
| 14 | L-1 ~ L-4：低風險項目 | 小 |

---

## 備註

- DDL 建構器（`SqlCreateTableCommandBuilder`）使用字串內插組合 SQL，但 Table/Column 名稱來源為內部 `TableSchema` 定義物件，非外部使用者輸入，故不構成 SQL Injection 風險。
- `Parameter.Value` 使用 `[MessagePackFormatter(typeof(TypelessFormatter))]` 標記，同屬 H-2 問題範圍。
- 本報告為靜態程式碼審查，建議後續搭配動態測試（Penetration Testing）進行驗證。
