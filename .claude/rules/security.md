# 安全規範

## 加密實作

### AES-CBC-HMAC（標準加密）
- AES 金鑰長度：**256-bit**
- HMAC：**SHA-256**，使用獨立 256-bit 金鑰
- 每次加密使用**隨機 IV**，不重複使用
- 驗證 HMAC 時使用 `CompareBytes`（常數時間比較），防止 Timing Attack

```csharp
// 正確：常數時間比較
private static bool CompareBytes(byte[] a, byte[] b) { ... }

// 禁止：直接比較可能洩漏時序資訊
if (hmac == expected) { ... }
```

### RSA
- 使用 `RsaCryptor` 類別，不直接操作底層 API
- 金鑰由 `AesCbcHmacKeyGenerator` 產生，不手動建構

## API 存取控制

### 三層保護等級（`ProtectionLevel`）
| 等級 | 說明 |
|------|------|
| `Public` | 不需驗證，任何人可存取 |
| `Encoded` | 需要 Token，Payload 為 Base64 編碼 |
| `Encrypted` | 需要 Token，Payload 完整加密 |

### 認證需求（`AccessRequirement`）
| 等級 | 說明 |
|------|------|
| `None` | 不需登入 |
| `Authenticated` | 需要有效 AccessToken |

### Attribute 使用
以 `[ApiAccessControl]` 宣告存取控制，優先繼承自類別：
```csharp
[ApiAccessControl(AccessRequirement.Authenticated, ProtectionLevel.Encrypted)]
public class OrderController { ... }
```

## Session 管理

- AccessToken 為 **GUID** 格式，不使用可預測值
- Token 具**到期時間**，過期後需重新驗證
- 支援一次性 Token（One-time Token）
- Session 資料存於資料庫（`st_session`, `st_user`），不存於用戶端

## Payload 安全管線

處理順序必須維持：
```
序列化（Serialize） → 壓縮（Compress） → 加密（Encrypt）
解密（Decrypt） → 解壓縮（Decompress） → 反序列化（Deserialize）
```
不可跳過或調換順序。

## 禁止事項

- 禁止在日誌或例外訊息中輸出**明文金鑰、Token 或密碼**
- 禁止使用 `MD5` 或 `SHA1` 做安全雜湊（僅 SHA-256 以上）
- 禁止硬編碼（hardcode）任何金鑰或憑證至原始碼
- 禁止在測試之外的環境使用 `NoEncryptionEncryptor`
- 禁止使用 `==` 直接比較 HMAC / 雜湊結果

## 檔案完整性

使用 `FileHashValidator` 驗證檔案完整性，不自行實作雜湊比對邏輯。
