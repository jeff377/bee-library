# 修復計畫：前三項高風險安全弱點

**建立日期**：2026-04-09
**對應審查**：`docs/plans/plan-security-code-review.md`
**修復項目**：H-1（RSA 填充）、H-2（MessagePack 型別白名單）、H-3（加密空 Key 靜默跳過）

---

## 一、H-2：MessagePack TypelessContractlessStandardResolver → 型別白名單

### 問題

`MessagePackHelper` 使用 `TypelessContractlessStandardResolver`，允許反序列化任意 .NET 型別，可導致 RCE。

### 修復方案

建立 `SafeTypelessFormatterResolver`，攔截 Typeless 反序列化時的型別解析，複用 `SysInfo.IsTypeNameAllowed()` 白名單。

### 變更檔案

| 檔案 | 動作 |
|------|------|
| `src/Bee.Api.Core/MessagePack/SafeTypelessFormatter.cs` | **新增** — 包裝 `TypelessFormatter`，在反序列化時驗證型別白名單 |
| `src/Bee.Api.Core/MessagePack/MessagePackHelper.cs` | **修改** — 替換 `TypelessContractlessStandardResolver` 為安全版本 |
| `src/Bee.Definition/Collections/Parameter.cs` | **修改** — 將 `TypelessFormatter` 替換為 `SafeTypelessFormatter` |
| `tests/Bee.Api.Core.UnitTests/MessagePackTests.cs` | **修改** — 新增惡意型別反序列化測試 |

### 實作細節

1. 新建 `SafeTypelessFormatter`，繼承 `IMessagePackFormatter<object>`：
   - 序列化時直接委派給 `TypelessFormatter`
   - 反序列化時，先取得型別名稱再驗證白名單
   - 允許基礎型別（`System.String`、`System.Int32`、`System.DateTime` 等）
   - 允許 `SysInfo.AllowedTypeNamespaces` 中的型別
   - 拒絕其他型別時拋出 `InvalidOperationException`

2. 在 `MessagePackHelper` 中將 `TypelessContractlessStandardResolver` 替換為 `ContractlessStandardResolver`，搭配 `SafeTypelessFormatter`

---

## 二、H-1：RSA PKCS#1 v1.5 → OAEP-SHA256

### 問題

`RsaCryptor` 使用 `RSACryptoServiceProvider` + PKCS#1 v1.5 填充，易受 Padding Oracle 攻擊。

### 修復方案

改用 `RSA.Create()` 工廠方法 + `RSAEncryptionPadding.OaepSHA256`。

### 變更檔案

| 檔案 | 動作 |
|------|------|
| `src/Bee.Core/Security/RsaCryptor.cs` | **修改** — 三個方法全部改用 `RSA.Create()` + OAEP |
| `tests/Bee.Core.UnitTests/RsaCryptorTests.cs` | **修改** — 確認現有測試通過 |

### 實作細節

```csharp
// Before
using (var rsa = new RSACryptoServiceProvider(2048))
{
    rsa.PersistKeyInCsp = false;
    rsa.FromXmlString(publicKeyXml);
    var encrypted = rsa.Encrypt(data, false);  // PKCS#1 v1.5
}

// After
using (var rsa = RSA.Create())
{
    rsa.FromXmlString(publicKeyXml);
    var encrypted = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
}
```

### 注意事項

- `RSA.Create()` 和 `rsa.FromXmlString()` 在 `netstandard2.0` 皆可用
- **破壞性變更**：使用舊版 PKCS#1 加密的密文無法用新版 OAEP 解密。此修改適用於登入密碼加密等即時場景（每次加解密成對發生），不影響持久化資料

---

## 三、H-3：AesPayloadEncryptor 空 Key 靜默跳過 → 拋出例外

### 問題

`AesPayloadEncryptor.Encrypt/Decrypt` 在 Key 為 `null` 或空時直接回傳明文，無任何警告。

### 修復方案

改為拋出 `CryptographicException`，防止靜默降級。

### 變更檔案

| 檔案 | 動作 |
|------|------|
| `src/Bee.Api.Core/Transformer/AesPayloadEncryptor.cs` | **修改** — null/empty key 時拋出例外 |
| `tests/Bee.Api.Core.UnitTests/AesPayloadEncryptorTests.cs` | **新增** — 測試空 Key 拋出例外 |

### 實作細節

```csharp
// Before
if (encryptionKey == null || encryptionKey.Length == 0) { return bytes; }

// After
if (encryptionKey == null || encryptionKey.Length == 0)
    throw new CryptographicException("Encryption key must not be null or empty.");
```

---

## 測試計畫

修改完成後執行以下測試：

```bash
dotnet test tests/Bee.Core.UnitTests/Bee.Core.UnitTests.csproj
dotnet test tests/Bee.Api.Core.UnitTests/Bee.Api.Core.UnitTests.csproj
```

確認所有既有測試通過，且新增測試覆蓋安全情境。
