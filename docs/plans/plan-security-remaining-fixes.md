# 安全修復計畫（第二批：H-4 ~ L-4）

**日期**：2026-04-09
**前置作業**：H-1、H-2、H-3、L-3 已於前次提交完成

---

## 現況評估

| 項目 | 狀態 | 說明 |
|------|------|------|
| H-1 RSA OAEP | ✅ 已完成 | 前次提交 |
| H-2 MessagePack 型別白名單 | ✅ 已完成 | 前次提交 |
| H-3 AesPayloadEncryptor 空 Key | ✅ 已完成 | 前次提交 |
| L-3 RSACryptoServiceProvider 過時 | ✅ 已完成 | H-1 一併修復 |
| H-4 DateTime.Now/UtcNow 不一致 | ❌ 待修 | |
| H-5 CreateSession 無速率限制 | ⚠️ 部分修 | 僅加 ExpiresIn 上限；速率限制需框架支援 |
| M-1 NoEncryptionEncryptor 無環境限制 | ❌ 待修 | |
| M-2 PBKDF2 SHA-1 | ❌ 待修 | 需版本化格式向下相容 |
| M-3 GZip 無解壓大小限制 | ❌ 待修 | |
| M-4 ASP.NET 版本例外洩漏 | ❌ 待修 | |
| M-5 JsonRpcExecutor 例外訊息洩漏 | ❌ 待修 | |
| M-6 One-Time Token TOCTOU | ⚠️ 暫緩 | 需資料庫交易改動，列為獨立工作 |
| M-7 AccessTokenValidationProvider 無到期檢查 | ❌ 待修 | |
| M-8 AuthenticateUser 預設通過 | ❌ 待修 | |
| L-1 RSA 金鑰長度 2048 | ⚠️ 暫緩 | 資訊性建議，非立即風險 |
| L-2 ApiException 含 StackTrace | ❌ 待修 | |
| L-4 FileHashValidator 非常數時間比較 | ❌ 待修 | |

---

## 本批修復項目與說明

### 批次一：高風險（H-4、H-5 部分）

#### H-4：統一使用 `DateTime.UtcNow`
- **檔案**：`src/Bee.Repository/System/SessionRepository.cs:67`
- **變更**：`DateTime.Now` → `DateTime.UtcNow`
- **測試**：新增到期時間比較的單元測試

#### H-5（部分）：限制 `ExpiresIn` 最大值
- **檔案**：`src/Bee.Business/BusinessObjects/SystemBusinessObject.cs`、`CreateSessionArgs`
- **變更**：在 `CreateSession` 方法中加入 `ExpiresIn` 上限驗證（建議最大 86400 秒 = 24 小時）
- **說明**：完整速率限制（Rate Limiting）屬於 ASP.NET Core Middleware 層，超出本次範圍，列為後續工作

---

### 批次二：中風險（M-1、M-3、M-4、M-7、M-8）

#### M-1：`NoEncryptionEncryptor` 加入環境限制
- **檔案**：`src/Bee.Api.Core/Transformer/ApiPayloadOptionsFactory.cs`
- **變更**：在 `"none"` case 中加入 `#if DEBUG` 或透過 `SysInfo.IsDevelopment` 檢查，非開發環境時拋出 `InvalidOperationException`

#### M-3：GZip 解壓縮加入大小上限
- **檔案**：`src/Bee.Core/Serialization/GZipFunc.cs`
- **變更**：在 `Decompress` 方法中追蹤已解壓大小，超過 50 MB 時拋出 `InvalidDataException`

#### M-4：ASP.NET 版本例外訊息對齊 Core 版本
- **檔案**：`src/Bee.Api.AspNet/HttpModules/ApiServiceModule.cs`
- **變更**：加入 `HttpContext.IsDebuggingEnabled` 或環境判斷，非 Debug 模式回傳通用錯誤訊息

#### M-7：AccessTokenValidationProvider 加入到期時間檢查
- **檔案**：`src/Bee.Business/Validator/AccessTokenValidationProvider.cs`
- **變更**：加入 `if (sessionInfo.ExpiredAt < DateTime.UtcNow) throw new UnauthorizedAccessException(...)`

#### M-8：AuthenticateUser 預設行為改為拒絕
- **檔案**：`src/Bee.Business/BusinessObjects/SystemBusinessObject.cs`
- **變更**：預設回傳 `false`（框架強制子類別實作真實驗證邏輯）
- **說明**：同步更新 XML 文件說明

---

### 批次三：中風險（M-2、M-5）

#### M-2：PBKDF2 升級至 SHA-256（版本化格式）
- **檔案**：`src/Bee.Core/Security/PasswordHasher.cs`
- **設計**：
  - 雜湊字串格式由 `{iterations}.{salt}.{hash}` 升級為 `v2.{iterations}.{salt}.{hash}`（SHA-256）
  - `VerifyPassword` 自動偵測版本：無 `v2.` 前綴則以 SHA-1 驗證（向下相容舊密碼）
  - 新密碼一律用 SHA-256 產生
- **測試**：涵蓋新格式、舊格式（相容性）、錯誤格式三種情境

#### M-5：JsonRpcExecutor 區分業務例外與系統例外
- **檔案**：`src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs`
- **設計**：
  - 定義 `IUserFacingException` 標記介面（或利用現有 `BeeException` 基礎類別）
  - Catch 時：若為 `IUserFacingException` → 回傳 `rootEx.Message`；否則 → 回傳 `"Internal server error"`
  - 系統例外仍寫入 Tracer（不遺失診斷資訊）

---

### 批次四：低風險（L-2、L-4）

#### L-2：ApiException 非 Debug 模式不帶 StackTrace
- **檔案**：`src/Bee.Core/ApiException.cs`
- **變更**：`ApiException(Exception)` 建構函式改為條件式設定 StackTrace

#### L-4：FileHashValidator 改用常數時間比較
- **檔案**：`src/Bee.Core/Security/FileHashValidator.cs`
- **變更**：使用 `CryptographicOperations.FixedTimeEquals`（.NET Core）或自行實作位元 XOR 比較

---

## 暫緩項目

| 項目 | 原因 |
|------|------|
| H-5 速率限制 | 需要 ASP.NET Core Middleware 或資料庫層計數器，屬於獨立功能 |
| M-6 One-Time Token TOCTOU | 需修改資料庫交易邏輯（`DELETE … OUTPUT` 或 `SELECT FOR UPDATE`），建議獨立 PR |
| L-1 RSA 金鑰長度 | 資訊性建議，目前 2048-bit 仍符合 NIST 2030 前標準 |

---

## 執行順序

1. 批次一（H-4、H-5 部分）
2. 批次二（M-1、M-3、M-4、M-7、M-8）
3. 批次三（M-2、M-5）
4. 批次四（L-2、L-4）
5. 執行全部測試，確認通過後建立提交
