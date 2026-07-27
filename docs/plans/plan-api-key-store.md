# 計畫：API Key 存放機制與預設驗證強化

**狀態：📝 擬定中（2026-07-27）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | `st_api_key` 表 + 兩段式金鑰格式 + `IApiKeyValidator` + 快取與失效，未設定時維持相容 | 📝 待做 |
| 2 | 呼叫端識別：驗證結果帶入呼叫上下文並落進稽核記錄 | 📝 待做 |
| 3 | 金鑰管理表單（框架自身 CRUD dogfooding）+ 產生 / 輪替流程與文件 | 📝 待做 |
| 4 | 用戶端存放：脫離原始碼 hardcode，samples / Northwind 遷移 | 📝 待做 |

## 背景

框架目前**沒有 API Key 的存放機制**，伺服端與用戶端都沒有。

伺服端 [`ApiAuthorizationValidator.Validate`](../../src/Bee.Api.Core/Authorization/ApiAuthorizationValidator.cs) 只做
`IsNullOrWhiteSpace` 檢查，任何非空字串通過。框架裡沒有任何位置承載合法金鑰。已有的防護是
[`UseBeeFramework` 的啟動警告](../../src/Bee.Api.AspNetCore/BeeFrameworkApplicationBuilderExtensions.cs)
與 validator 的 `<remarks>`，兩者都只是「請部署端自己寫程式覆寫」。

用戶端 [`ApiClientInfo.ApiKey`](../../src/Bee.Api.Client/ApiClientInfo.cs) 是 static 欄位，
註解寫 "typically loaded from configuration" 但框架沒提供那個 configuration；
[`IEndpointStorage`](../../src/Bee.UI.Core/IEndpointStorage.cs) 三個方法全部只處理 endpoint。
實務上就是硬寫在原始碼（`AppDefaults.ApiKey = "northwind-demo"`）。

**缺口的實質是「沒有地方放」**：就算把 validator 改成比對合法金鑰，也沒有位置可放那份金鑰。

### 目標情境決定了存放位置

驅動需求是**識別呼叫端**與**對第三方發放**：不同應用程式各持一組金鑰，log 能看出是誰在呼叫；
開放給第三方時發一組給對方即可，出事能立刻停用。

這是**營運行為**，不是部署設定——因此金鑰存 DB（`st_api_key`），不存定義檔。差別在於：

| | 定義檔 | DB |
|---|---|---|
| 新增一組給廠商 | 改檔案、可能需重啟 | 管理畫面新增一列 |
| 事故時停用 | 同上 | 改一個欄位，快取失效後即時生效 |
| 誰在呼叫 | 無從記錄 | 金鑰即實體，可帶應用名稱並落進稽核 |
| 多租戶 / 分應用 | 硬塞進設定樹 | 天然就是多列 |

### 為何 DB 的效能與相依疑慮不成立

- **熱路徑**：框架已有 [`src/Bee.ObjectCaching/Database/`](../../src/Bee.ObjectCaching/Database/)
  這條成熟路徑（`SessionInfoCache` / `CompanyInfoCache` / `CompanyRolePermissionsCache`）——
  DB 為源、`KeyObjectCache` 快取、`st_cache_notify` 跨行程失效。API Key 直接沿用同一套。
- **相依方向**：AccessToken 驗證本來就查 DB（`SessionRepository`）。API Key 查 DB 與之一致，
  不引入新的相依。

### 與既有加密金鑰機制的分界（避免誤用）

`SecurityKeySettings.ApiEncryptionKey` 名字相近但**是另一回事**：那是 payload 傳輸加密金鑰
（AES+HMAC combined key，經 master key 加密後存 base64，由 `EncryptionKeyProtector` 保護）。

兩者的密碼學需求相反：

| | 傳輸加密金鑰 | API Key |
|---|---|---|
| 伺服端要做的事 | **還原**明文金鑰來解密 | **驗證**用戶端送來的值是否正確 |
| 因此存放方式 | 可逆（master key 加密） | **不可逆（雜湊）** |

API Key 走可逆存放沒有必要，且 DB 一旦外洩即等於全部金鑰外洩。**故不重用
`EncryptionKeyProtector` 路徑**，改採雜湊存放。

### API Key 的定位（決定安全強度要求）

`X-Api-Key` 是**應用識別**（哪個 app / 哪個第三方在呼叫），不是使用者鑑別——使用者鑑別靠
`Bearer` AccessToken，這層不改。定位釐清後兩個推論：

1. 用戶端持有的金鑰本質上可被反編譯取出，**任何用戶端存放都不可能達到「機密」等級**。
   目標是「不寫死在原始碼、可不重編譯即更換」，不是保險箱。
2. 伺服端的比對仍須嚴謹（常數時間、不可逆存放），因為那是**伺服端**資產。

## 決策

### D1：金鑰存 `st_api_key`（common 分類）

`st_` 前綴表框架層級表，與 DB 位置正交；金鑰是全系統層級資產，故落 **common**。
欄位沿用框架慣例（`sys_rowid` 主鍵、`sys_id` 識別碼、`sys_name` 顯示名 + 既有稽核欄位）：

| 欄位 | 用途 |
|------|------|
| `sys_id` | 金鑰識別碼，**同時是明文金鑰的前段**（見 D2）；進 log 不洩密 |
| `sys_name` | 應用程式名稱（"Northwind Desktop"、"廠商 X 訂單介接"） |
| `hashed_key` | 雜湊後的 secret 段 |
| `key_type` | 內部應用 / 第三方（純標示，不承載授權語意） |
| `contact` | 第三方聯絡窗口，事故時要找得到人 |
| `enabled` | 停用而不刪除（輪替與事故處置） |
| `expired_at` | 選填到期時間，null 表不到期 |

### D2：明文金鑰採兩段式 `{sys_id}.{secret}`

雜湊存放與「用金鑰查表」看似衝突——雜湊後無法拿明文當索引。解法是把識別與驗證拆開，
業界標準做法（GitHub PAT、Stripe 皆是此形狀）：

```
bee_a7f3c2e1.xLq9Kv2mNp8wR4tZ...
└─ sys_id ─┘ └────── secret ──────┘
```

驗證流程：切出前段 → 以 `sys_id` O(1) 查快取 → 驗證後段 secret 對上 `hashed_key`。

三件事同時成立：**O(1) 查找**、**不可逆存放**、**log 得到可讀的呼叫端識別**。
逐把比對（O(n)）與無鹽索引雜湊都不需要了。

### D3：雜湊用 salt + SHA-256，**不用** `PasswordHasher`

[`PasswordHasher`](../../src/Bee.Base/Security/PasswordHasher.cs) 是 PBKDF2-SHA256、**100,000 次迭代**——
那是為「人選的低熵密碼」設計的，代價是單次驗證刻意很慢。

API Key 情境相反：**每個 request 都要驗一次**。把 100k 次迭代放進請求熱路徑等於自製 DoS 面。
且 secret 由框架產生、是 256-bit 高熵隨機值，慢 KDF 防的問題本來就不存在。

採 `salt + SHA-256(salt || secret)`，以 `CryptographicOperations.FixedTimeEquals` 比對，
儲存格式沿用 `PasswordHasher` 的版本前綴風格：`v1.{saltBase64}.{hashBase64}`。

> 這條要在程式碼註解寫明「為何不用 `PasswordHasher`」，否則日後必定有人「順手統一」
> 而把 100k 迭代搬進每個請求。

### D4：快取沿用 DB-backed cache 樣板

`ApiKeyCache : KeyObjectCache<ApiKeyInfo>`（`src/Bee.ObjectCaching/Database/`），key 為 `sys_id`，
搭配 `st_cache_notify` 失效——停用一把金鑰後，其他行程在下次 notify 檢查時同步失效。

負向快取（查無此 `sys_id`）沿用 `KeyObjectCache` 既有機制，避免以隨機 `sys_id` 掃描造成
每次都穿透到 DB。

### D5：表不存在或無啟用金鑰時維持現行行為（相容性閘門）

直接改成「金鑰不在表內即拒絕」會讓所有現存部署在升級當下全部失效——包含本 repo 的
samples 與 Northwind，以及尚未建表的既有資料庫。分兩態：

| `st_api_key` 狀態 | 行為 |
|---|---|
| 表不存在，或無任何啟用中金鑰 | 沿用現行：只檢查非空 + **啟動警告**（訊息改為指向金鑰管理） |
| 有啟用中金鑰 | **嚴格比對**：格式不符、查無、已停用、已過期一律拒絕 |

升級不破壞既有部署，而一旦建了第一把金鑰就自動獲得真正的閘門。啟動警告從「請自己覆寫
validator」改為「請建立 API 金鑰」——**從此不需要寫程式就能關上這個洞**，是本計畫最主要的體驗改善。

### D6：新增 `IApiKeyValidator`，走 DI 而非靜態

`ApiServiceOptions.AuthorizationValidator` 是 **static** 屬性，預設 validator 沒有 ctor 注入機會，
拿不到 repository / cache。

沿用框架既有先例：[`IAccessTokenValidator`](../../src/Bee.Definition/Security/IAccessTokenValidator.cs)
正是「授權驗證需要後端服務」時抽出的政策介面，放 `Bee.Definition/Security/`（依 `security.md`
的原語 / 政策分層，金鑰驗證屬政策層）。

接線點是 [`ApiServiceController.ValidateAuthorization`](../../src/Bee.Api.AspNetCore/Controllers/ApiServiceController.cs)——
它是 `protected virtual` 且能取用 `HttpContext.RequestServices`，由此解析 `IApiKeyValidator`
並帶入 `ApiAuthorizationContext`，validator 本身維持無狀態。

### D7：金鑰只在產生當下顯示一次

雜湊存放的必然結果：框架**無法**還原明文 secret。產生時回傳一次完整金鑰，之後只剩雜湊。
遺失即重新產生（輪替流程本來就要能做）。管理表單須明確呈現這個語意，不能做成
「像密碼欄一樣可以再看一次」。

### D8：用戶端存放不擴充 `IEndpointStorage`

`IEndpointStorage` 三個方法名稱都綁死 endpoint（`LoadEndpoint` / `SetEndpoint` / `SaveEndpoint`），
硬塞 ApiKey 要嘛改名（breaking，四個實作 + 所有 host）、要嘛加不對稱的方法。

改為**在既有的 `ClientSettings` 上增欄位**——`IEndpointStorage` 的實作端本來就以 `ClientSettings`
為後盾（[`EndpointStorage`](../../src/Bee.UI.Core/EndpointStorage.cs)），沿用同一份持久化載體即可，
不動介面。行動端 sandbox 情境仍走各平台既有的 storage 實作。

## 階段 1：存放模型與驗證

1. `st_api_key` TableSchema（common）+ 註冊進 `DbCategorySettings`；`ApiKeyRepository`。
2. `ApiKeyHasher`（`Bee.Base/Security/`，原語層）：`HashSecret` / `VerifySecret`，
   salt + SHA-256 + `FixedTimeEquals`，含「為何不用 `PasswordHasher`」的 WHY 註解。
3. 金鑰格式工具：產生（`RandomNumberGenerator` 256-bit，URL-safe base64）與解析
   `{sys_id}.{secret}`；格式不符即快速失敗，不進 DB 查詢。
4. `ApiKeyCache : KeyObjectCache<ApiKeyInfo>` + cache-notify 失效（依 `bee-add-cache-object` 流程，
   含 `ICacheContainer` 三處同步與兩個 CacheNotify 測試 stub）。
5. `IApiKeyValidator`（`Bee.Definition/Security/`）+ 預設實作，由 `AddBeeFramework` 註冊。
6. `ApiAuthorizationContext` 增加驗證器承載欄位；`ApiServiceController.ValidateAuthorization`
   從 DI 解析並帶入；`ApiAuthorizationValidator` 有驗證器就嚴格比對、沒有就沿用非空檢查。
7. 啟動警告訊息與觸發條件改依 D5。
8. 測試：命中 / 查無 / 停用 / 過期 / 格式不符 / 無啟用金鑰沿用舊行為 / 雜湊 round-trip /
   cache-notify 失效生效。

**驗收**：建了金鑰的部署，錯誤金鑰被拒；沒建的部署行為與升級前一致。

## 階段 2：呼叫端識別

1. 驗證成功後把 `sys_id` 與 `sys_name` 帶進呼叫上下文（沿 `ApiAuthorizationResult` → executor）。
2. 落進既有稽核家族：`st_log_login` / `st_log_access` / `st_log_anomaly_api` 增記金鑰識別欄，
   讓「誰在呼叫」在既有報表即可查。
3. **只記識別碼與名稱，絕不記金鑰值**（對齊 `security.md` 禁止事項）。

**驗收**：從稽核記錄可分辨同一支 API 是哪個應用 / 哪個第三方呼叫的。

## 階段 3：金鑰管理與輪替

1. 以**框架自身的 FormSchema CRUD** 做管理表單（dogfooding，不需為此擴充 DefineEditor）：
   新增（顯示一次完整金鑰並提示不再顯示）、停用、設到期、刪除。
2. 產生動作走 BO 方法（依 `bee-add-bo-method` 流程），不由前端組金鑰。
3. 輪替流程文件：發第二把 → 用戶端逐步換 → 停用舊把 → 確認無流量後刪除。
4. 文件（雙語）：API Key 的定位（應用識別 ≠ 使用者鑑別）、發放與輪替流程、第三方介接指引；
   `framework-reserved-names.md` 補 `st_api_key`。

**驗收**：不寫任何程式碼即可完成「發一組給第三方」與一次完整輪替。

## 階段 4：用戶端存放

1. `ClientSettings` 增 ApiKey 欄位，`ClientInfo` 提供讀寫接縫（對齊 `GetEndpoint` / `SetEndpointAsync`）。
2. samples 與 `apps/Bee.Northwind` 從 `AppDefaults.ApiKey` 常數改為由設定讀取；
   連線設定畫面（`ConnectionViewModel`）比照 endpoint 提供輸入。
3. 各平台 storage 實作確認可承載（`FileEndpointStorage`、`MauiPreferenceEndpointStorage`、WASM）。
4. README 更新（依 `rules/public-docs.md`，不連結本 plan）。

**驗收**：更換 API Key 不需重新編譯任何用戶端。

## 不在範圍

- **不改 AccessToken 鑑別鏈**。本計畫只補應用識別層；使用者鑑別維持現行 Bearer token 機制。
- **不做 per-key 權限 / 配額 / rate limiting**。金鑰只有「有效 / 無效」，不承載授權語意——
  授權是 `PermissionModels` 的職責，混進來會變成第二套權限系統。`key_type` 只是標示，不影響判權。
- **不處理 mTLS / OAuth client credentials**。那是不同層級的部署決策。

## 待確認

1. **DB 不可用時的行為**：金鑰驗證在請求最前端，連 `System.Ping` 都需通過。DB 掛掉時
   API 全數拒絕——判斷是可接受（DB 掛了 API 本來就沒用），但要明確載明，避免日後被當 bug 追。
   若要讓 ping 在 DB 掛時仍可回應（健康檢查用途），需把 ping 排除在金鑰檢查外，另議。
2. **金鑰是否綁 CompanyId**：DB 存放讓「一租戶一把」變得自然。但一旦金鑰帶公司語意，
   就與 session 公司情境形成兩個來源、需交叉驗證。預設**不綁**，維持金鑰只識別應用；
   有需求再另案。
3. **相容模式的長期處置**：D5 為相容保留寬鬆態。是否在某個大版本改為「無金鑰即拒絕」？
   若要，須在 CHANGELOG 標 breaking 並給遷移指引。
