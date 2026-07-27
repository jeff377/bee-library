# 計畫：API Key 存放機制與預設驗證強化

**狀態：📝 擬定中（2026-07-27）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 伺服端存放模型（`ApiKeySettings`）+ `IApiKeyValidator` + 常數時間比對，未設定時維持相容 | 📝 待做 |
| 2 | 金鑰產生與管理：產生 API、一次性顯示、DefineEditor 編輯、輪替流程與文件 | 📝 待做 |
| 3 | 用戶端存放：脫離原始碼 hardcode，samples / Northwind 遷移 | 📝 待做 |

## 背景

框架目前**沒有 API Key 的存放機制**，伺服端與用戶端都沒有。

伺服端 [`ApiAuthorizationValidator.Validate`](../../src/Bee.Api.Core/Authorization/ApiAuthorizationValidator.cs) 只做
`IsNullOrWhiteSpace` 檢查，任何非空字串通過。框架裡沒有任何設定物件承載合法金鑰——
`SystemSettings`、`BackendConfiguration`、`ApiServiceOptions` 皆無對應欄位。已有的防護是
[`UseBeeFramework` 的啟動警告](../../src/Bee.Api.AspNetCore/BeeFrameworkApplicationBuilderExtensions.cs)
與 validator 的 `<remarks>`，兩者都只是「請部署端自己覆寫」。

用戶端 [`ApiClientInfo.ApiKey`](../../src/Bee.Api.Client/ApiClientInfo.cs) 是 static 欄位，
註解寫 "typically loaded from configuration" 但框架沒提供那個 configuration；
[`IEndpointStorage`](../../src/Bee.UI.Core/IEndpointStorage.cs) 三個方法全部只處理 endpoint。
實務上就是硬寫在原始碼（`AppDefaults.ApiKey = "northwind-demo"`）。

**缺口的實質是「沒有地方放」**：就算把 validator 改成比對合法金鑰，也沒有設定位置可放那份金鑰。
本計畫先補存放模型，再讓預設驗證真的成為閘門。

### 與既有加密金鑰機制的分界（避免誤用）

`SecurityKeySettings.ApiEncryptionKey` 名字相近但**是另一回事**：那是 payload 傳輸加密金鑰
（AES+HMAC combined key，經 master key 加密後存 base64，由 `EncryptionKeyProtector` 保護）。

兩者的密碼學需求相反：

| | 傳輸加密金鑰 | API Key |
|---|---|---|
| 伺服端要做的事 | **還原**明文金鑰來解密 | **驗證**用戶端送來的值是否正確 |
| 因此存放方式 | 可逆（master key 加密） | **不可逆（雜湊）** |

API Key 走可逆存放沒有必要，且設定檔外洩即等於金鑰外洩。**故不重用 `SecurityKeySettings` 的
`EncryptionKeyProtector` 路徑**，改採雜湊存放。

### API Key 的定位（決定安全強度要求）

`X-Api-Key` 是**應用識別**（哪個 app / 哪個部署在呼叫），不是使用者鑑別——使用者鑑別靠
`Bearer` AccessToken，這層沒有要改。定位釐清後兩個推論：

1. 用戶端持有的 key 本質上可被反編譯取出，**任何用戶端存放都不可能達到「機密」等級**。
   目標只是「不寫死在原始碼、可不重編譯即更換」，不是保險箱。
2. 伺服端的比對仍必須嚴謹（常數時間、不可逆存放），因為它是**伺服端**資產。

## 決策

### D1：新增 `ApiKeySettings`，支援多把金鑰

放在 `SystemSettings` 之下，與 `SecurityKeySettings` 併列（同屬 `Bee.Definition/Settings/SystemSettings/`）。

多把並存是必要的，不是 over-engineering：**金鑰輪替期間新舊必須同時有效**，否則輪替就等於一次
全體用戶端斷線。每把金鑰帶：

| 欄位 | 用途 |
|------|------|
| `Id` | 識別用短代號（記錄於 log，不含金鑰值） |
| `Description` | 人看的用途說明（如 "Northwind Desktop"） |
| `HashedKey` | 雜湊後的金鑰值 |
| `Enabled` | 停用而不刪除（輪替與事故處置） |
| `ExpiredAt` | 選填到期時間，`null` 表不到期 |

集合依 [Definition 集合慣例](../../.claude/rules/) 繼承 `KeyCollectionBase<T>`，不用裸 `List<T>`。

### D2：雜湊用 salt + SHA-256，**不用** `PasswordHasher`

[`PasswordHasher`](../../src/Bee.Base/Security/PasswordHasher.cs) 是 PBKDF2-SHA256、**100,000 次迭代**——
那是為了讓「人選的低熵密碼」難以暴力破解，代價是單次驗證刻意很慢。

API Key 的情境相反：**每個 request 都要驗一次**。把 100k 次迭代放進請求熱路徑，等於替自己
製造 DoS 面。且 API Key 由框架產生、是高熵隨機值（256-bit），慢 KDF 防的問題本來就不存在。

採 `salt + SHA-256(salt || key)`，以 `CryptographicOperations.FixedTimeEquals` 比對：

- 快，可放熱路徑
- per-key salt 擋彩虹表（否則若有人自訂弱 key 如 `"demo"`，無鹽 SHA-256 秒破）
- 儲存格式沿用 `PasswordHasher` 的風格：`v1.{saltBase64}.{hashBase64}`，日後可換版本前綴

> 這條要在程式碼註解寫明「為何不用 `PasswordHasher`」，否則日後必定有人「順手統一」而把
> 100k 迭代搬進每個請求。

### D3：新增 `IApiKeyValidator`，走 DI 而非靜態

`ApiServiceOptions.AuthorizationValidator` 是 **static** 屬性，預設 validator 沒有 ctor 注入的機會，
拿不到 `IDefineAccess` 讀設定。

沿用框架既有的先例：[`IAccessTokenValidator`](../../src/Bee.Definition/Security/IAccessTokenValidator.cs)
就是「授權驗證需要後端服務」時抽出的政策介面，放在 `Bee.Definition/Security/`（依 `security.md`
的原語 / 政策分層，金鑰驗證屬政策層）。

接線點是 [`ApiServiceController.ValidateAuthorization`](../../src/Bee.Api.AspNetCore/Controllers/ApiServiceController.cs)——
它是 `protected virtual` 且能取用 `HttpContext.RequestServices`，由此解析 `IApiKeyValidator`
並帶入 `ApiAuthorizationContext`，validator 本身維持無狀態。

### D4：未設定任何金鑰時維持現行行為（相容性閘門）

直接改成「金鑰不在清單即拒絕」會讓**所有現存部署在升級當下全部 401**——包含本 repo 的
samples 與 Northwind。分兩態：

| `ApiKeySettings` 狀態 | 行為 |
|---|---|
| 空（未設定任何金鑰） | 沿用現行：只檢查非空 + **啟動警告**（訊息更新為指向新設定） |
| 有設定 | **嚴格比對**：不在清單、已停用、已過期一律拒絕 |

如此升級不破壞既有部署，而一旦部署端填了金鑰就自動獲得真正的閘門。啟動警告從「請自己覆寫
validator」改為「請設定 `ApiKeySettings`」——**從此不需要寫程式就能關上這個洞**，這是本計畫
最主要的體驗改善。

### D5：金鑰只在產生當下顯示一次

雜湊存放的必然結果：框架**無法**還原明文金鑰。產生時回傳一次明文，之後只剩雜湊。
遺失即重新產生（輪替流程本來就要能做）。DefineEditor 的編輯畫面須明確呈現這個語意，
不能做成「像密碼欄一樣可以再看一次」。

### D6：用戶端存放不擴充 `IEndpointStorage`

`IEndpointStorage` 的三個方法名稱都綁死 endpoint（`LoadEndpoint` / `SetEndpoint` / `SaveEndpoint`），
硬塞 ApiKey 要嘛改名（breaking，四個實作 + 所有 host）、要嘛加不對稱的方法。

改為**在既有的 `ClientSettings` 上增欄位**，`IEndpointStorage` 的實作端已經是以 `ClientSettings`
為後盾（[`EndpointStorage`](../../src/Bee.UI.Core/EndpointStorage.cs)），沿用同一份持久化載體即可，
不動介面。行動端 sandbox 情境仍走各平台既有的 storage 實作。

## 階段 1：伺服端存放模型與驗證

1. `ApiKeySettings` + `ApiKeyItem` + `ApiKeyItemCollection`（`Bee.Definition/Settings/SystemSettings/`），
   一型別一檔；掛進 `SystemSettings`。
2. `ApiKeyHasher`（`Bee.Base/Security/`，屬原語層）：`HashKey` / `VerifyKey`，salt + SHA-256 +
   `FixedTimeEquals`，含「為何不用 `PasswordHasher`」的 WHY 註解。
3. `IApiKeyValidator`（`Bee.Definition/Security/`）+ 預設實作（讀 `SystemSettings.ApiKeySettings`，
   走既有快取、不每次讀檔），由 `AddBeeFramework` 註冊。
4. `ApiAuthorizationContext` 增加驗證器承載欄位；`ApiServiceController.ValidateAuthorization`
   從 DI 解析並帶入；`ApiAuthorizationValidator` 有驗證器就嚴格比對、沒有就沿用非空檢查。
5. 啟動警告訊息改為指向 `ApiKeySettings`；條件由「validator 是預設實作」改為「未設定任何啟用中的金鑰」。
6. 測試：命中 / 未命中 / 停用 / 過期 / 空設定沿用舊行為 / 雜湊 round-trip / 大小寫與空白邊界。

**驗收**：填了金鑰的部署，錯誤金鑰被拒；沒填的部署行為與升級前一致。

## 階段 2：金鑰產生與管理

1. 產生 API：`RandomNumberGenerator` 產 256-bit、以 URL-safe base64 呈現，回傳明文 + 已雜湊項目。
2. DefineEditor 支援編輯 `ApiKeySettings`：新增（顯示一次明文並提示不再顯示）、停用、刪除、設到期。
3. 輪替流程文件：新增第二把 → 用戶端逐步換 → 停用舊把 → 確認無流量後刪除。
4. 文件（雙語）：`docs/` 下說明 API Key 的定位（應用識別 ≠ 使用者鑑別）、設定方式、輪替流程；
   `framework-reserved-names.md` 補上新設定節點。

**驗收**：不寫任何程式碼即可完成一次完整輪替。

## 階段 3：用戶端存放

1. `ClientSettings` 增 ApiKey 欄位，`ClientInfo` 提供讀寫接縫（對齊 `GetEndpoint` / `SetEndpointAsync`）。
2. samples 與 `apps/Bee.Northwind` 從 `AppDefaults.ApiKey` 常數改為由設定讀取；
   連線設定畫面（`ConnectionViewModel`）比照 endpoint 提供輸入。
3. 各平台 storage 實作確認可承載（`FileEndpointStorage`、`MauiPreferenceEndpointStorage`、WASM）。
4. README 更新（依 `rules/public-docs.md`，不連結本 plan）。

**驗收**：更換 API Key 不需重新編譯任何用戶端。

## 不在範圍

- **不改 AccessToken 鑑別鏈**。本計畫只補應用識別層；使用者鑑別維持現行 Bearer token 機制。
- **不做 per-key 權限 / 配額 / rate limiting**。金鑰只有「有效 / 無效」，不承載授權語意——
  授權是 `PermissionModels` 的職責，混進來會變成第二套權限系統。
- **不做金鑰用量統計與稽核表**。`Id` 已可寫進既有稽核記錄，專屬報表另案。
- **不處理 mTLS / OAuth client credentials 等替代方案**。那是不同層級的部署決策。

## 待確認

1. **雜湊 vs 可逆**：D2 採雜湊 → 框架永遠無法顯示既有金鑰。若營運上需要「事後查看目前 key」
   （如支援人員要告訴客戶金鑰是什麼），則須改為可逆存放並接受設定檔外洩風險。
   預設走雜湊，需要可逆再議。
2. **空設定的長期處置**：D4 為相容而保留寬鬆模式。是否在某個大版本改為「預設拒絕」？
   若要，須在 CHANGELOG 標 breaking 並給遷移指引。
3. **金鑰是否需綁 CompanyId / 部署範圍**：目前設計是全域金鑰。多租戶下若要「一租戶一金鑰」，
   `ApiKeyItem` 需增欄位並與 session 公司情境交叉驗證——會顯著擴大範圍，預設不做。
