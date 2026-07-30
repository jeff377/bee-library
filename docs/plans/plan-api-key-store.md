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

### D4：快取沿用 DB-backed cache 樣板（自載，經 `ICacheDataSourceProvider`）

> **2026-07-29 修訂**：本節原本論證「`ApiKeyCache` 是框架第一個必須自己去 DB 撈的快取」，
> 並在兩條路線間選了 per-cache 載入委派。該前提已不成立——
> `CompanyInfoCache` / `CompanyRolePermissionsCache` / `DepartmentTreeCache` 已於同日改為自載
> （見 [plan-cache-createinstance-db-loading.md](archive/plan-cache-createinstance-db-loading.md)），
> 自載成為 Database 快取的既定慣例，`ApiKeyCache` 照既有樣板走即可，不需要獨有形狀。

`ApiKeyCache : KeyObjectCache<ApiKeyInfo>`（`src/Bee.ObjectCaching/Database/`），key 為 `sys_id`，
搭配 `st_cache_notify` 失效——停用一把金鑰後，其他行程在下次 notify 檢查時同步失效。

負向快取（查無此 `sys_id`）沿用 `KeyObjectCache` 既有機制，避免以隨機 `sys_id` 掃描造成
每次都穿透到 DB。**這也是必須自載的理由**：
[`KeyObjectCache.Get`](../../src/Bee.ObjectCaching/KeyObjectCache.cs) 的 miss sentinel 與
正/負向 policy 都掛在 `CreateInstance` 上，把載入寫到 service 等於繞過 base class 再手寫一份。

**載入接縫**：`ICacheDataSourceProvider`（`Bee.Definition`）新增 `ApiKeyInfo? GetApiKey(string sysId)`，
由 `Bee.Business.Providers.CacheDataSourceProvider` 實作（經 `ISystemRepositoryFactory` 取
`IApiKeyRepository`）。`ApiKeyCache.CreateInstance` 呼叫它。

三個必須遵守的形狀約束（細節見 `bee-add-cache-object` skill 的 path B）：

- **`ApiKeyInfo` 型別必須放 `Bee.Definition`**（與 `CompanyInfo` / `DepartmentTree` 同層）。
  `Bee.Repository.Abstractions` 反向相依 `Bee.Definition`，取數方法若回傳 repository 型別
  會造成專案循環參考。
- **快取持有 `Func<ICacheDataSourceProvider>` 而非實例**，第一次 miss 才解析；直接注入會閉合
  `ICacheContainer → ICacheDataSourceProvider → ISystemRepositoryFactory → IDefineAccess →
  ICacheContainer` 這條環，`AddBeeFramework` 解析即死結。
- **帶 `dataSource` 的建構式為 `internal`**（`RS0026` / `RS0027` 不允許兩個 public 多載都帶
  選擇性參數）。

**不需要 `ApiKeyService`**：載入在快取，驗證邏輯在 `IApiKeyValidator`（D6），兩者之間沒有
第三個角色可放。與其他 Database 快取不同——那些有 service 是因為介面要放 `Bee.Definition`
供上層使用，而 API Key 的上層消費者就是 validator 本身。

**TTL 須覆寫 `GetPolicy`**：預設為 sliding 20 分，對金鑰不適用——sliding 會讓熱門金鑰永遠不
重讀，`expired_at` 到期後仍在快取中存活。改為 absolute（建議 60 分）。撤銷的保證來自
cache-notify 而非過期，這也是 D5 / D10「DB 短暫不可用仍能服務」的來源；代價是 notify 失效鏈
成為撤銷的**唯一**保證，其可靠性必須有測試覆蓋。

**接線清單**（依 `bee-add-cache-object` skill）：`ICacheContainer` 新增 `ApiKey` 屬性（三處同步）、
cache group 名稱、兩個 CacheNotify 測試 stub（漏補會 CS0535 直接編不過）。

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

### D9：每請求驗證，不引入 token 交換

金鑰每個請求驗一次，**不做**「以金鑰換短期 token、之後改驗 token」。四個理由：

1. **成本已經被 D2 / D3 設計掉**：切字串 → `sys_id` O(1) 查記憶體快取 → 一次 SHA-256（約 48 bytes）
   → `FixedTimeEquals`。框架每個需授權的請求本來就在付同級成本——
   [`AccessTokenValidator`](../../src/Bee.Business/Validator/AccessTokenValidator.cs) →
   `SessionInfoCache.Get` 命中時是純記憶體查表。
2. **交換式會多養一條 token 生命週期**：金鑰驗證與 session 是兩套獨立的失效機制，
   交換式等於把兩者綁在一起，任何一邊的過期 / 撤銷規則改動都要重新推導另一邊。
3. **撤銷語意維持單層**：停用即擋住，最壞延遲是 notify 週期。交換式須再建一條
   「key 撤銷 → 連帶撤銷其衍生 token」的失效鏈，事故處置從「改一個欄位」變成兩件事。
4. **wire 上不出現兩個生命週期不同的 token**，守住「`X-Api-Key` = 應用識別、`Bearer` = 使用者鑑別」
   的分界（也就是「不在範圍」第一條）。

> **2026-07-29 修訂**：本節原本以「session 只能由 Login 灌入、token 僅在鑄造它的行程有效」
> 作為最關鍵理由（原理由 2）。該前提正被
> [plan-session-persistence.md](archive/plan-session-persistence.md) 移除——session 將可由
> `st_session` 種子重建、跨行程一致。**結論不受影響**：其餘三個理由
> 與 session 無關，per-request 驗證仍是正解。但原理由 2 的論證已反轉（屆時交換式反而可行），
> 故改寫為與 session 實作無關的論據。同理原理由 1 的「從不查 DB」也已不成立，一併修正。

日後才需重議的觸發條件：per-key 配額 / rate limit / scope（現列不在範圍）、對第三方的
OAuth client_credentials 標準介接——屆時是**新增**一條路徑，而不是取代金鑰檢查。

### D10：`System.Ping` 免金鑰

ping 是連通性檢查，不碰 DB、不回業務資料。排除後健康檢查在 DB 不可用時仍能作答
（`待確認 1` 由此收斂）。三個落地約束：

1. **獨立豁免清單，不重用 `NoAuthMethods`**。
   [`ApiAuthorizationValidator.NoAuthMethods`](../../src/Bee.Api.Core/Authorization/ApiAuthorizationValidator.cs)
   是 **Bearer 豁免**清單（含 `System.Login` 與 `System.GetApiPayloadOptions`），與金鑰是不同軸；
   合用會一次放掉三個：`Login` 恰恰**最需要**金鑰（階段 2 要記「哪個 app 在嘗試登入」），
   `GetApiPayloadOptions` 揭露 payload / 加密協商設定、不是連通性檢查。新清單預設**只有
   `System.Ping`**，比照 `IsAuthorizationRequired` 做成 `protected virtual` 供部署端加自己的健康
   檢查方法。兩個清單並存須註解寫明「兩條軸，勿合併」。
2. **`PingResult` 增加金鑰狀態欄**（`NotProvided` / `Invalid` / `Valid`）。ping 的主要消費場景是
   連線設定畫面的「測試連線」；完全不看金鑰會讓使用者打錯金鑰仍顯示連線成功，錯誤延到第一次
   真正呼叫才在別的畫面浮出。加欄位屬 additive，adr-030 改 name-based key 後 wire 相容。
   副作用是 ping 成為「金鑰是否有效」的 oracle——以 D2 的 256-bit secret 而言無實際可利用性
   （攻擊者須先持有完整金鑰），此判斷要寫進註解，別讓後人誤以為是疏漏。
3. **`Version` 改為金鑰有效才回**。[`SystemBusinessObject.Ping`](../../src/Bee.Business/System/SystemBusinessObject.cs)
   現在無條件回 `SysInfo.Version`；免金鑰後等於對全網公開框架版本（fingerprinting 起手式）。
   `Status` / `ServerTime` 對連通性檢查已足夠；監控要版本號的話本來就該帶金鑰。

## 階段 1：存放模型與驗證

1. `st_api_key` TableSchema（common）+ 註冊進 `DbCategorySettings`；`ApiKeyRepository`。
2. `ApiKeyHasher`（`Bee.Base/Security/`，原語層）：`HashSecret` / `VerifySecret`，
   salt + SHA-256 + `FixedTimeEquals`，含「為何不用 `PasswordHasher`」的 WHY 註解。
3. 金鑰格式工具：產生（`RandomNumberGenerator` 256-bit，URL-safe base64）與解析
   `{sys_id}.{secret}`；格式不符即快速失敗，不進 DB 查詢。
4. `ApiKeyCache : KeyObjectCache<ApiKeyInfo>`，依 D4：`CreateInstance` 經
   `ICacheDataSourceProvider.GetApiKey` 自載（快取持 `Func<ICacheDataSourceProvider>`、
   帶該參數的建構式為 `internal`）、覆寫 `GetPolicy` 為 absolute TTL、`ICacheContainer` 三處同步、
   cache-notify 失效與兩個 CacheNotify 測試 stub（依 `bee-add-cache-object` 流程）。
   `ApiKeyInfo` 放 `Bee.Definition`；`ICacheDataSourceProvider` 加取數方法、
   `CacheDataSourceProvider` 實作、`ISystemRepositoryFactory` 加 `CreateApiKeyRepository()`。
5. `IApiKeyValidator`（`Bee.Definition/Security/`）+ 預設實作，由 `AddBeeFramework` 註冊。
6. `ApiAuthorizationContext` 增加驗證器承載欄位；`ApiServiceController.ValidateAuthorization`
   從 DI 解析並帶入；`ApiAuthorizationValidator` 有驗證器就嚴格比對、沒有就沿用非空檢查。
   同時依 D10 新增金鑰豁免清單（`protected virtual`，預設只含 `System.Ping`），
   與既有的 Bearer 豁免 `NoAuthMethods` 並存且互不引用。
7. 依 D10 調整 ping：`PingResult` 增金鑰狀態欄、`Version` 改為金鑰有效才回。
8. 啟動警告訊息與觸發條件改依 D5。
9. 測試：命中 / 查無 / 停用 / 過期 / 格式不符 / 無啟用金鑰沿用舊行為 / 雜湊 round-trip /
   cache-notify 失效生效 / **ping 不帶金鑰仍回 `ok` 且不含 `Version`** /
   **ping 帶無效金鑰回報 `Invalid`** / **`Login` 與 `GetApiPayloadOptions` 仍需金鑰**。

**驗收**：建了金鑰的部署，錯誤金鑰被拒；沒建的部署行為與升級前一致；
DB 不可用時 ping 仍可回應，其餘方法 fail-closed。

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

1. ~~**DB 不可用時的行為**~~ —— **已決策（見 D10）**：`System.Ping` 免金鑰，健康檢查在 DB
   不可用時仍能作答；其餘方法 fail-closed（DB 掛了 API 本來就沒用）。快取 TTL 依 D4 拉長為
   absolute 60 分，DB 短暫不可用期間已在快取的金鑰仍可服務。此行為須寫進文件，避免日後被當 bug 追。
2. **金鑰是否綁 CompanyId**：DB 存放讓「一租戶一把」變得自然。但一旦金鑰帶公司語意，
   就與 session 公司情境形成兩個來源、需交叉驗證。預設**不綁**，維持金鑰只識別應用；
   有需求再另案。
3. **相容模式的長期處置**：D5 為相容保留寬鬆態。是否在某個大版本改為「無金鑰即拒絕」？
   若要，須在 CHANGELOG 標 breaking 並給遷移指引。
