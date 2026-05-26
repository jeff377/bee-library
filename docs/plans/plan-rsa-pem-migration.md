# 計畫：RsaCryptor 由 XML 格式遷移至 PEM 格式 + Wasm 跳過 RSA handshake

**狀態：✅ 已完成（2026-05-26）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | XML→PEM 格式遷移（RsaCryptor 三方法 + 呼叫端 + 測試） | ✅ 已完成（2026-05-26） |
| 2 | Wasm 跳過 RSA handshake（OperatingSystem.IsBrowser() guard） | ✅ 已完成（2026-05-26） |
| 3 | Wasm 跳過 SocketsHttpHandler（HttpUtilities browser fallback） | ✅ 已完成（2026-05-26） |

> **2026-05-26 補充**：階段 1 完成後 smoke 仍踩 `PlatformNotSupportedException`，診斷確認 **RSA 金鑰生成（`RSA.Create()` + `KeySize=2048` + `Export*Pem`）本身在 browser-wasm/.NET 10 上未實作**（SubtleCrypto bridge 截至目前只 import + Encrypt/Decrypt，無 sync generateKey）。原 plan 假設 XML 是唯一原因，實證後是錯的——PEM 是必要但不充分。新增階段 2 走最小傷害路線：Wasm 端 `LoginAsync` 跳過 keypair 生成、`ClientPublicKey` 送空字串。Server 已有 fallback（[SystemBusinessObject.cs:108](src/Bee.Business/System/SystemBusinessObject.cs:108) `if (StringUtilities.IsNotEmpty(args.ClientPublicKey))`），收到空 key 就不回傳加密 `ApiEncryptionKey`。Wasm 後續 Encrypted 請求自動降級到 Encoded（[ApiConnector.cs:174](src/Bee.Api.Client/Connectors/ApiConnector.cs:174) 既有邏輯）。安全等級降級換可用性，使用者已裁示。
>
> **2026-05-26 補充 (再)**：階段 2 完成後 smoke 仍踩 PNS。診斷新 stack：[HttpUtilities.cs:30](src/Bee.Base/HttpUtilities.cs:30) `new SocketsHttpHandler { PooledConnectionLifetime = ... }` —— `SocketsHttpHandler` 在 browser-wasm 上不支援，瀏覽器 `HttpClient` 走 `BrowserHttpHandler`（內部 fetch API）。新增階段 3：`GetOrCreateClient` 在 `OperatingSystem.IsBrowser()` 為真時跳過 `SocketsHttpHandler` 自訂，回退到 `new HttpClient()` 預設 handler，連線池與 DNS 由瀏覽器自管。

## 背景

`samples/Blazor.Wasm.Demo.Host` (port 5070) 啟動後在瀏覽器以 `demo`/`demo` 登入，client-side 直接拋 `PlatformNotSupportedException`，server log 看不到 `/api` 請求進入 — 代表 request 在送出前就炸。`samples/Blazor.Server.Demo` (5055) 走 in-process 不受影響。

### Root cause

[src/Bee.Base/Security/RsaCryptor.cs](src/Bee.Base/Security/RsaCryptor.cs) 三個方法皆使用 `RSA.ToXmlString` / `RSA.FromXmlString`：

| 行 | 方法 | API |
|----|------|-----|
| 26–27 | `GenerateRsaKeyPair` | `rsa.ToXmlString(false)` / `rsa.ToXmlString(true)` |
| 41 | `EncryptWithPublicKey` | `rsa.FromXmlString(publicKeyXml)` |
| 58 | `DecryptWithPrivateKey` | `rsa.FromXmlString(privateKeyXml)` |

這兩個 API 是 `RSACryptoServiceProvider` 早期遺留 surface，在 .NET 跨平台版只剩 Windows 桌面 CSP 支援。Blazor WebAssembly runtime 的 RSA 走 SubtleCrypto-backed 實作，呼叫 `ToXmlString` / `FromXmlString` 立即拋 `PlatformNotSupportedException`。

呼叫鏈：

- **Client**：[SystemApiConnector.LoginAsync](src/Bee.Api.Client/Connectors/SystemApiConnector.cs:136) → `GenerateRsaKeyPair` (行 139) + `DecryptWithPrivateKey` (行 151)
- **Server**：[SystemBusinessObject.Login](src/Bee.Business/System/SystemBusinessObject.cs:110) → `EncryptWithPublicKey`（接 client 傳來的公鑰）
- **Tests**：[RsaCryptorTests](tests/Bee.Base.UnitTests/RsaCryptorTests.cs)、[SystemBusinessObjectLoginTests](tests/Bee.Business.UnitTests/SystemBusinessObjectLoginTests.cs)、[SystemBusinessObjectTests](tests/Bee.Business.UnitTests/SystemBusinessObjectTests.cs)

Wasm Login 第一行 `RsaCryptor.GenerateRsaKeyPair` 就炸，所以 `ExecuteAsync` 從未被呼叫，server 自然看不到 request。

## 修法方向

`RsaCryptor` 內部由 XML 格式改為 **PEM 格式**：

| 動作 | 舊 API | 新 API |
|------|--------|--------|
| 匯出公鑰 | `rsa.ToXmlString(false)` | `rsa.ExportSubjectPublicKeyInfoPem()` |
| 匯出私鑰 | `rsa.ToXmlString(true)` | `rsa.ExportRSAPrivateKeyPem()` |
| 匯入金鑰（公/私通用） | `rsa.FromXmlString(...)` | `rsa.ImportFromPem(...)` |

理由：

- `ExportSubjectPublicKeyInfoPem` / `ExportRSAPrivateKeyPem` / `ImportFromPem` 自 .NET 7 起內建於 `System.Security.Cryptography.RSA`，**全平台** managed 實作，Wasm runtime 與 net10 server 皆原生支援
- 仍維持非對稱加密的 OAEP-SHA256 padding（無變更）
- Wire payload `LoginRequest.ClientPublicKey` 仍為 `string`、schema 不動，只是內容由 XML 字串改為 PEM 字串
- 同一 process 內 client + server 同時換（兩端都用 `RsaCryptor`），不存在跨版本 round-trip 問題，無需相容層

## 修改範圍

### 1. `src/Bee.Base/Security/RsaCryptor.cs`

- 三個方法 signature 的參數名由 `publicKeyXml` / `privateKeyXml` → `publicKey` / `privateKey`（XML 字眼整批移除）
- `GenerateRsaKeyPair` body：改用 `ExportSubjectPublicKeyInfoPem()` / `ExportRSAPrivateKeyPem()`
- `EncryptWithPublicKey` / `DecryptWithPrivateKey` body：`rsa.FromXmlString(...)` → `rsa.ImportFromPem(...)`
- XML doc 同步：「XML format」字眼改為「PEM format（PKCS#1 private key / SPKI public key）」，class summary、`<param>` 描述對應更新
- 因 ImportFromPem 接 `ReadOnlySpan<char>`，呼叫直接傳 `string` 自動轉型，無需額外處理

### 2. `src/Bee.Api.Client/Connectors/SystemApiConnector.cs`

- 行 139：`out var publicKeyXml, out var privateKeyXml` → `out var publicKey, out var privateKey`
- 行 146：`ClientPublicKey = publicKeyXml` → `ClientPublicKey = publicKey`
- 行 146 行尾註解 `// Pass the RSA public key` 維持
- 行 151：`DecryptWithPrivateKey(result.ApiEncryptionKey, privateKeyXml)` → `DecryptWithPrivateKey(result.ApiEncryptionKey, privateKey)`

### 3. `src/Bee.Business/System/SystemBusinessObject.cs`

- 無變數命名需要改（只直接呼叫 `RsaCryptor.EncryptWithPublicKey(..., args.ClientPublicKey)`，欄位名稱本來就是 `ClientPublicKey`）。確認方法簽章相容後不動

### 4. 測試檔（變數重命名 + 邏輯不變）

| 檔案 | 變更 |
|------|------|
| `tests/Bee.Base.UnitTests/RsaCryptorTests.cs` | `publicKeyXml` / `privateKeyXml` / `publicKeyXml1` / `privateKeyXml1` / `publicKeyXml2` / `privateKeyXml2` → 對應 `publicKey` / `privateKey` / `publicKey1` 等 |
| `tests/Bee.Business.UnitTests/SystemBusinessObjectLoginTests.cs` | 行 70/79/87 變數重命名；DisplayName 維持（仍叫「RSA 加密」） |
| `tests/Bee.Business.UnitTests/SystemBusinessObjectTests.cs` | 行 52/59/70 變數重命名 |
| `tests/Bee.Api.Core.UnitTests/ApiRequestResponseTests.cs` | 行 55 `ClientPublicKey = "publicKeyXml"` 改為 `ClientPublicKey = "publicKeyPem"`（純字串值，僅為避免誤導讀者；schema 還是字串） |

### 5. 介面 / DTO 文件註解

| 檔案 | 變更 |
|------|------|
| `src/Bee.Api.Contracts/ILoginRequest.cs` | 行 19 `<summary>` 描述：`RSA public key generated by the client` 維持，無需改字面（PEM 與否屬實作細節） |
| `src/Bee.Api.Core/Messages/System/LoginRequest.cs` | 同上，描述不動 |
| `src/Bee.Business/System/LoginArgs.cs` | 同上 |

> 上述三個 DTO 的 `<summary>` 已是「RSA public key generated by the client」，沒有出現「XML」字眼，可不動。如後續要更精準可在 `RsaCryptor` 的 class 註解補一句「Keys are exchanged as PEM strings (PKCS#1 private, SPKI public).」即可。

## 變數命名規則

`publicKeyXml` → `publicKey`、`privateKeyXml` → `privateKey`。
跨檔案估計 ~12 個 identifier 需改（不含註解）。命名意圖是「以格式無關的 `publicKey` / `privateKey` 為主」，未來若再換格式（如 PKCS#8）也不必再大量改名。

## 測試策略

### A. 既有測試（必須全綠）

- [RsaCryptorTests](tests/Bee.Base.UnitTests/RsaCryptorTests.cs)：round-trip 加解密、錯誤私鑰拋例外
- [SystemBusinessObjectLoginTests.Login_WithClientPublicKey_EncryptsApiKey](tests/Bee.Business.UnitTests/SystemBusinessObjectLoginTests.cs:67)：完整 Login → RSA 加密 ApiEncryptionKey → 私鑰解密還原 base64 session key
- 其餘 `./test.sh` 全套

### B. 新增測試（可選，避免回歸）

在 `tests/Bee.Base.UnitTests/RsaCryptorTests.cs` 加：

```csharp
[Fact]
[DisplayName("GenerateRsaKeyPair 應產出 PEM 格式字串（PKCS#1 private、SPKI public）")]
public void GenerateRsaKeyPair_ReturnsPemFormattedStrings()
{
    RsaCryptor.GenerateRsaKeyPair(out var publicKey, out var privateKey);

    Assert.StartsWith("-----BEGIN PUBLIC KEY-----", publicKey);
    Assert.Contains("-----END PUBLIC KEY-----", publicKey);
    Assert.StartsWith("-----BEGIN RSA PRIVATE KEY-----", privateKey);
    Assert.Contains("-----END RSA PRIVATE KEY-----", privateKey);
}
```

此測試固定了 wire 上實際的 PEM header，防止後續有人不小心換成 PKCS#8 之類而破壞同 process round-trip 預期。

## 驗證流程

桌面環境（本機可 build + test），直接在 `main` 上修改、本機跑通後 commit & push（依 `~/.claude/rules/pull-request.md`）。

完成條件三項，**全部要通過才算結束**：

1. **`./test.sh` 全綠** — 所有單元測試（含上述新測試）pass
2. **Blazor Wasm 真實瀏覽器驗證** — `dotnet run --project samples/Blazor.Wasm.Demo.Host -c Debug` 起來後，用 computer-use 或 Chrome MCP 訪問 `http://localhost:5070/`、輸入 `demo`/`demo`、點 Sign in，確認：
   - Login 成功（不再拋 PlatformNotSupportedException）
   - 接著渲染 Employee CRUD 區塊
   - DevTools console 無錯誤
3. **回歸驗證**（不能因此打破）：
   - `samples/Blazor.Server.Demo` (5055) → 開瀏覽器、demo/demo 登入、Employee CRUD 仍可運作
   - `samples/QuickStart.Console` → 跑一次，輸出與 baseline 一致

> 在 `samples/Maui.Demo` 已有 `[LocalOnlyFact]` 等隔離手段，Login 也走同一 `SystemApiConnector`；本次不主動跑 MAUI smoke，但若桌面環境方便會順便確認。

## Release

依規劃 [v4.6.0](docs/plans/) 釋出：

- 與 master key source 預設改為 Environment 的 commit [f8e37ee0](https://github.com/jeff377/bee-library) 一同併入
- CHANGELOG 條目（雙語）以 `/changelog-draft` 在發版前統整。本 plan 不單獨新增 CHANGELOG 條目，會在發版時整批
- `Directory.Build.props` 版本號於發版 commit 一同更新

## 風險與緩解

| 風險 | 緩解 |
|------|------|
| Wasm trim 後 `ImportFromPem` 內部反射路徑被砍 | 用 Debug 構建驗證（與既有 MAUI 同策略，見 [.claude/rules/maui.md](.claude/rules/maui.md) 「Apple Release-mode trim」一節）。Wasm Release trim 若有問題另開 plan |
| 既有 Wasm client 已快取舊版（XML 格式） | 不存在 — 此格式只在同一 process 一次 Login round-trip 內使用，無快取、無 persistence |
| 同版本 client/server 不對等部署 | 不存在 — `SystemApiConnector`（client）與 `SystemBusinessObject`（server）都封裝在 bee-library，使用者升級套件時兩端一起換 |
| `ToXmlString` 被外部呼叫端拿來自製金鑰 | 已 grep 全 repo，僅 `RsaCryptor.cs` 內部使用；無公開 API leak |

## 不做的事

- 不改 `LoginRequest.ClientPublicKey` schema 與 MessagePack `[Key(102)]`
- 不動 AES-CBC-HMAC 對稱加密管線
- 不重新設計 RSA padding（仍 `OaepSHA256`）
- 不為「未來可能再換成 PKCS#8」做抽象；直接寫死 PKCS#1 private + SPKI public，符合 [code-style](.claude/CLAUDE.md) 「不為假設的未來建類」原則
- 不為向下相容保留 XML 解析 fallback（同 process round-trip，無需相容）

## 執行步驟概要（user review 通過後）

### 階段 1（已完成）

1. ✅ 改 `RsaCryptor.cs`（核心改動）
2. ✅ 改 `SystemApiConnector.cs` 變數名
3. ✅ 改三個測試檔變數名
4. ✅ 改 `ApiRequestResponseTests.cs` 行 55 字面值
5. ✅ 在 `RsaCryptorTests.cs` 加 PEM header 斷言測試（防回歸）
6. ✅ `dotnet build -c Release` + `./test.sh` 確認全綠

### 階段 2（Wasm 跳過 RSA）

7. `SystemApiConnector.LoginAsync` 加 `OperatingSystem.IsBrowser()` 分支：browser 端跳過 `GenerateRsaKeyPair` 與 `DecryptWithPrivateKey`，`ClientPublicKey` 送空字串
8. 拿掉先前為診斷臨時加的 `[DIAG]` try/catch wrapper
9. 加 `OperatingSystem.IsBrowser()` 行為的 unit test 不易（無法在非 browser 環境模擬），改在 `SystemBusinessObjectLoginTests` 確認空 `ClientPublicKey` → 空 `ApiEncryptionKey` 既有路徑仍 pass（既存測試已涵蓋，無需新增）
10. `dotnet build` + `./test.sh` 確認全綠
11. 起 Blazor.Wasm.Demo.Host，開瀏覽器跑一輪 Login + Employee CRUD
12. 回歸跑 Blazor.Server.Demo（仍要走 RSA 加密路徑）與 QuickStart.Console
13. `git commit` + `git push origin main`
14. push 後 GitHub Actions `build-ci.yml` 若失敗即時跟進
15. Plan 文件頂部狀態列改為 `**狀態：✅ 已完成（YYYY-MM-DD）**`、階段 2 表格列改為 ✅
