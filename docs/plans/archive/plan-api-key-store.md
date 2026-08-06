# 計畫：API Key 存放機制與預設驗證強化

**狀態：✅ 已完成（階段 1–4 全數落地）· 2026-08-03**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | `st_api_key` 表 + 兩段式金鑰格式 + 產生金鑰 BO 方法 + `IApiKeyValidator` + 快取與失效，未設定時維持相容 | ✅ 已完成（2026-07-30） |
| 2 | 呼叫端識別：驗證結果帶入呼叫上下文並落進稽核記錄 | ✅ 已完成（2026-07-30） |
| 3 | 金鑰生命週期的後端能力（列出 / 停用 / 設到期）+ 輪替流程與文件（雙語） | ✅ 已完成（2026-08-03） |
| 4 | 用戶端存放：脫離原始碼 hardcode，samples / Northwind 遷移 | ✅ 已完成（2026-07-30） |

## 背景

框架目前**沒有 API Key 的存放機制**，伺服端與用戶端都沒有。

伺服端 [`ApiAuthorizationValidator.Validate`](../../../src/Bee.Api.Core/Authorization/ApiAuthorizationValidator.cs) 只做
`IsNullOrWhiteSpace` 檢查，任何非空字串通過。框架裡沒有任何位置承載合法金鑰。已有的防護是
[`UseBeeFramework` 的啟動警告](../../../src/Bee.Api.AspNetCore/BeeFrameworkApplicationBuilderExtensions.cs)
與 validator 的 `<remarks>`，兩者都只是「請部署端自己寫程式覆寫」。

用戶端 [`ApiClientInfo.ApiKey`](../../../src/Bee.Api.Client/ApiClientInfo.cs) 是 static 欄位，
註解寫 "typically loaded from configuration" 但框架沒提供那個 configuration；
[`IEndpointStorage`](../../../src/Bee.UI.Core/IEndpointStorage.cs) 三個方法全部只處理 endpoint。
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

- **熱路徑**：框架已有 [`src/Bee.ObjectCaching/Database/`](../../../src/Bee.ObjectCaching/Database/)
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
northwind-desktop.xLq9Kv2mNp8wR4tZ...
└──── sys_id ────┘ └────── secret ──────┘
```

驗證流程：切出前段 → 以 `sys_id` O(1) 查快取 → 驗證後段 secret 對上 `hashed_key`。

三件事同時成立：**O(1) 查找**、**不可逆存放**、**log 得到可讀的呼叫端識別**。
逐把比對（O(n)）與無鹽索引雜湊都不需要了。

**`sys_id` 由建立者人工指定**（已決策 2026-07-30），不由系統產生亂碼——符合框架
`sys_id` ＝ 人可讀識別碼的既有慣例，且 log 與稽核記錄直接看得出是哪個應用，不必再回查
`sys_name`。前段本就不機密（本節上方已論證），可被枚舉不構成風險。

框架須驗證：字元集限 `[a-z0-9-]`（**不得含 `.`**，否則切段錯誤）、長度上限、以及 `sys_id`
唯一性——三者皆為建立時的快速失敗，不是驗證熱路徑的負擔。

### D3：雜湊用 salt + SHA-256，**不用** `PasswordHasher`

[`PasswordHasher`](../../../src/Bee.Base/Security/PasswordHasher.cs) 是 PBKDF2-SHA256、**100,000 次迭代**——
那是為「人選的低熵密碼」設計的，代價是單次驗證刻意很慢。

API Key 情境相反：**每個 request 都要驗一次**。把 100k 次迭代放進請求熱路徑等於自製 DoS 面。
且 secret 由框架產生、是 256-bit 高熵隨機值，慢 KDF 防的問題本來就不存在。

採 `salt + SHA-256(salt || secret)`，以 `CryptographicOperations.FixedTimeEquals` 比對，
儲存格式沿用 `PasswordHasher` 的版本前綴風格：`v1.{saltBase64}.{hashBase64}`。

**salt 的定位要在註解寫清楚**：對 256-bit 高熵隨機 secret，salt 幾乎不提供實質防護——它防的是
彩虹表與「多人用同一組弱密碼」的碰撞，兩者在本情境都不存在。保留 salt 是為了**格式與
`PasswordHasher` 一致**、以及消除理論上的相同 secret 碰撞，不是安全必要條件。不寫明的話，
後人會把它當必要防護而不敢動，或反過來誤以為「有 salt 就等於有 KDF 強度」。

不改用 pepper（server-side keyed hash）：那會讓金鑰驗證綁上 master key，master key 輪替時
所有金鑰雜湊同時失效，而換得的離線暴力防護對 256-bit secret 本無意義。

> 這條要在程式碼註解寫明「為何不用 `PasswordHasher`」，否則日後必定有人「順手統一」
> 而把 100k 迭代搬進每個請求。

### D4：快取沿用 DB-backed cache 樣板（自載，經 `ICacheDataSourceProvider`）

> **2026-07-29 修訂**：本節原本論證「`ApiKeyCache` 是框架第一個必須自己去 DB 撈的快取」，
> 並在兩條路線間選了 per-cache 載入委派。該前提已不成立——
> `CompanyInfoCache` / `CompanyRolePermissionsCache` / `DepartmentTreeCache` 已於同日改為自載
> （見 [plan-cache-createinstance-db-loading.md](plan-cache-createinstance-db-loading.md)），
> 自載成為 Database 快取的既定慣例，`ApiKeyCache` 照既有樣板走即可，不需要獨有形狀。

`ApiKeyCache : KeyObjectCache<ApiKeyInfo>`（`src/Bee.ObjectCaching/Database/`），key 為 `sys_id`，
搭配 `st_cache_notify` 失效——停用一把金鑰後，其他行程在下次 notify 檢查時同步失效。

負向快取（查無此 `sys_id`）沿用 `KeyObjectCache` 既有機制，避免以隨機 `sys_id` 掃描造成
每次都穿透到 DB。**這也是必須自載的理由**：
[`KeyObjectCache.Get`](../../../src/Bee.ObjectCaching/KeyObjectCache.cs) 的 miss sentinel 與
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

**TTL 須覆寫 `GetPolicy`**：預設為 sliding 20 分，改為 absolute（建議 60 分）。

理由是**兜底，不是過期判定**——這點容易寫錯，故明列：`expired_at` 是快取物件**內的欄位**，
validator 每次都比對當下時間對 `expired_at`，故到期是即時生效的，與快取存活多久無關。
absolute TTL 真正解決的是**撤銷鏈斷掉時的最壞暴露窗**：撤銷（`enabled` 改 false）的唯一保證是
`st_cache_notify`，一旦 notify 鏈失效，sliding 會讓熱門金鑰**無限期**留在快取中有效，
absolute 則把暴露窗限死在一個 TTL 內。

代價與必要覆蓋：notify 失效鏈仍是撤銷的**第一線且唯一即時**保證，其可靠性必須有測試覆蓋；
absolute TTL 只是保證「最終會失效」，不能拿來當撤銷機制用。另一面的好處是 D5 / D10 的
「DB 短暫不可用期間，已在快取的金鑰仍可服務」。

**接線清單**（依 `bee-add-cache-object` skill）：`ICacheContainer` 新增 `ApiKey` 屬性（三處同步）、
cache group 名稱、兩個 CacheNotify 測試 stub（漏補會 CS0535 直接編不過）。

### D5：表不存在或無啟用金鑰時維持現行行為（相容性閘門）

直接改成「金鑰不在表內即拒絕」會讓所有現存部署在升級當下全部失效——包含本 repo 的
samples 與 Northwind，以及尚未建表的既有資料庫。分兩態：

| `st_api_key` 狀態 | 行為 |
|---|---|
| 表不存在，或無任何啟用中金鑰 | 沿用現行：只檢查非空 + **啟動警告**（訊息改為指向金鑰管理） |
| 有啟用中金鑰 | **嚴格比對**：格式不符、查無、已停用、已過期一律拒絕 |
| **查詢 gate 時 DB 連不上 / 擲例外** | **fail-closed：一律拒絕**（`System.Ping` 依 D10 免金鑰，仍可作答） |

升級不破壞既有部署，而一旦建了第一把金鑰就自動獲得真正的閘門。啟動警告從「請自己覆寫
validator」改為「請建立 API 金鑰」——**從此不需要寫程式就能關上這個洞**，是本計畫最主要的體驗改善。

#### 「表不存在」與「DB 故障」必須分開判定（已決策 2026-07-30）

第三列是本決策最容易實作錯的地方。寬鬆態的觸發條件**只有明確的 schema 訊號**（表不存在）
與明確的查詢結果（查到 0 筆啟用金鑰）；**任何例外都走 fail-closed**。

> **WARNING**：不得寫成 `try { 查 gate } catch { 回寬鬆 }`。那會讓 DB 故障自動降級成
> 「任何非空字串皆通過」——把可用性事故轉成安全洞，且外部無從察覺。

與階段 1 驗收的「其餘方法 fail-closed」一致：DB 掛了業務 API 本來就無法服務，拒絕不損失
實際可用性；能作答的健康檢查由 D10 的免金鑰 `System.Ping` 承擔。

#### gate 狀態本身需要快取與失效路徑

「是否有啟用中金鑰」不能每請求查一次 DB。此狀態與 `ApiKeyCache`（per-`sys_id`）是**不同粒度**，
D4 的樣板不直接覆蓋，須一併設計：

- 併入 `ApiKeyCache` 同一個 cache group，以保留字 key（如 `__gate__`）承載，或另立單值
  `ObjectCache`——實作時擇一，但**必須與金鑰快取共用同一個 cache group 名稱**，
  才能一次 notify 同時失效兩者。
- 金鑰的新增 / 停用 / 刪除都要發 notify，否則**建了第一把金鑰卻仍停在寬鬆態**（最壞延遲一個
  absolute TTL）。這是引導期最可能被誤判為 bug 的行為，須有測試覆蓋
  「建第一把金鑰後 → gate 轉嚴格」。
- gate 快取**不得由例外填入**（承上，例外走 fail-closed 且不寫入快取），避免把一次 DB 抽風
  固化成一個 TTL 的寬鬆態。

### D6：新增 `IApiKeyValidator`，走 DI 而非靜態

`ApiServiceOptions.AuthorizationValidator` 是 **static** 屬性，預設 validator 沒有 ctor 注入機會，
拿不到 repository / cache。

沿用框架既有先例：[`IAccessTokenValidator`](../../../src/Bee.Definition/Security/IAccessTokenValidator.cs)
正是「授權驗證需要後端服務」時抽出的政策介面，放 `Bee.Definition/Security/`（依 `security.md`
的原語 / 政策分層，金鑰驗證屬政策層）。

接線點是 [`ApiServiceController.ValidateAuthorization`](../../../src/Bee.Api.AspNetCore/Controllers/ApiServiceController.cs)——
它是 `protected virtual` 且能取用 `HttpContext.RequestServices`，由此解析 `IApiKeyValidator`。

**controller 解析後就在該處驗完，`ApiAuthorizationContext` 只承載「驗證結果」，不承載驗證器**
（已決策 2026-07-30）。結果型別帶狀態（見 D10 四態）與識別資訊（`sys_id` / `sys_name`）。
把服務實例塞進 context 會有三個壞處，故不採：

1. `ApiAuthorizationContext` 是純資料載體，塞服務即成 service locator，並讓
   `ApiAuthorizationValidator`（`Bee.Api.Core`）反過來依賴政策介面。
2. 分工會變模糊：`ApiAuthorizationValidator` 只該做**決策**（該不該放行），不該負責觸發驗證。
3. 結果可**一份三用**：授權決策、D10 的 `PingResult` 金鑰狀態欄、階段 2 的呼叫端識別，
   三者共用同一次驗證，不必各自再查一次快取。

validator 本身維持無狀態。

### D7：金鑰只在產生當下顯示一次

雜湊存放的必然結果：框架**無法**還原明文 secret。產生時回傳一次完整金鑰，之後只剩雜湊。
遺失即重新產生（輪替流程本來就要能做）。管理表單須明確呈現這個語意，不能做成
「像密碼欄一樣可以再看一次」。

### D8：用戶端存放不擴充 `IEndpointStorage`

> **2026-07-30 修訂**：本節「實作端本來就以 `ClientSettings` 為後盾」的前提**不成立**——6 個 host
> 有 5 個已改用平台專屬 storage 而繞開 `ClientSettings`。結論「不擴充 `IEndpointStorage`」保留，
> 但金鑰改走**新增的平行介面 `IApiKeyStorage`**，而非塞進 `ClientSettings` 就了事。
> 詳見階段 4 的「執行結果」。

`IEndpointStorage` 三個方法名稱都綁死 endpoint（`LoadEndpoint` / `SetEndpoint` / `SaveEndpoint`），
硬塞 ApiKey 要嘛改名（breaking，四個實作 + 所有 host）、要嘛加不對稱的方法。

改為**在既有的 `ClientSettings` 上增欄位**——`IEndpointStorage` 的實作端本來就以 `ClientSettings`
為後盾（[`EndpointStorage`](../../../src/Bee.UI.Core/EndpointStorage.cs)），沿用同一份持久化載體即可，
不動介面。行動端 sandbox 情境仍走各平台既有的 storage 實作。

### D9：每請求驗證，不引入 token 交換

金鑰每個請求驗一次，**不做**「以金鑰換短期 token、之後改驗 token」。四個理由：

1. **成本已經被 D2 / D3 設計掉**：切字串 → `sys_id` O(1) 查記憶體快取 → 一次 SHA-256（約 48 bytes）
   → `FixedTimeEquals`。框架每個需授權的請求本來就在付同級成本——
   [`AccessTokenValidator`](../../../src/Bee.Business/Validator/AccessTokenValidator.cs) →
   `SessionInfoCache.Get` 命中時是純記憶體查表。
2. **交換式會多養一條 token 生命週期**：金鑰驗證與 session 是兩套獨立的失效機制，
   交換式等於把兩者綁在一起，任何一邊的過期 / 撤銷規則改動都要重新推導另一邊。
3. **撤銷語意維持單層**：停用即擋住，最壞延遲是 notify 週期。交換式須再建一條
   「key 撤銷 → 連帶撤銷其衍生 token」的失效鏈，事故處置從「改一個欄位」變成兩件事。
4. **wire 上不出現兩個生命週期不同的 token**，守住「`X-Api-Key` = 應用識別、`Bearer` = 使用者鑑別」
   的分界（也就是「不在範圍」第一條）。

> **2026-07-29 修訂**：本節原本以「session 只能由 Login 灌入、token 僅在鑄造它的行程有效」
> 作為最關鍵理由（原理由 2）。該前提正被
> [plan-session-persistence.md](plan-session-persistence.md) 移除——session 將可由
> `st_session` 種子重建、跨行程一致。**結論不受影響**：其餘三個理由
> 與 session 無關，per-request 驗證仍是正解。但原理由 2 的論證已反轉（屆時交換式反而可行），
> 故改寫為與 session 實作無關的論據。同理原理由 1 的「從不查 DB」也已不成立，一併修正。

日後才需重議的觸發條件：per-key 配額 / rate limit / scope（現列不在範圍）、對第三方的
OAuth client_credentials 標準介接——屆時是**新增**一條路徑，而不是取代金鑰檢查。

### D10：`System.Ping` 免金鑰

ping 是連通性檢查，不碰 DB、不回業務資料。排除後健康檢查在 DB 不可用時仍能作答
（`待確認 1` 由此收斂）。三個落地約束：

1. **獨立豁免清單，不重用 `NoAuthMethods`**。
   [`ApiAuthorizationValidator.NoAuthMethods`](../../../src/Bee.Api.Core/Authorization/ApiAuthorizationValidator.cs)
   是 **Bearer 豁免**清單（含 `System.Login` 與 `System.GetApiPayloadOptions`），與金鑰是不同軸；
   合用會一次放掉三個：`Login` 恰恰**最需要**金鑰（階段 2 要記「哪個 app 在嘗試登入」），
   `GetApiPayloadOptions` 揭露 payload / 加密協商設定、不是連通性檢查。新清單預設**只有
   `System.Ping`**，比照 `IsAuthorizationRequired` 做成 `protected virtual` 供部署端加自己的健康
   檢查方法。兩個清單並存須註解寫明「兩條軸，勿合併」。
2. **`PingResult` 增加金鑰狀態欄，四態**（已決策 2026-07-30）：

   | 狀態 | 語意 |
   |---|---|
   | `NotConfigured` | 部署仍在 D5 寬鬆態（表不存在或無啟用金鑰）——**金鑰尚未成為閘門** |
   | `NotProvided` | 嚴格態，但請求未帶 `X-Api-Key` |
   | `Invalid` | 嚴格態，金鑰格式不符 / 查無 / 已停用 / 已過期 |
   | `Valid` | 嚴格態，金鑰有效 |

   ping 的主要消費場景是連線設定畫面的「測試連線」；完全不看金鑰會讓使用者打錯金鑰仍顯示連線
   成功，錯誤延到第一次真正呼叫才在別的畫面浮出。加欄位屬 additive，adr-030 改 name-based key
   後 wire 相容。

   **`NotConfigured` 是四態的關鍵**：三態設計下，寬鬆態部署會對任何非空字串回 `Valid`，
   連線測試畫面因此告訴使用者「金鑰有效」，而該部署根本沒有閘門——那是比不顯示更糟的誤導。
   這一態同時讓 D5 的啟動警告有了**對外可見面**：維運不必翻 server log 就知道閘門還沒關上。

   副作用是 ping 成為「金鑰是否有效」的 oracle——以 D2 的 256-bit secret 而言無實際可利用性
   （攻擊者須先持有完整金鑰），此判斷要寫進註解，別讓後人誤以為是疏漏。
3. **`Version` 改為金鑰有效才回**。[`SystemBusinessObject.Ping`](../../../src/Bee.Business/System/SystemBusinessObject.cs)
   現在無條件回 `SysInfo.Version`；免金鑰後等於對全網公開框架版本（fingerprinting 起手式）。
   `Status` / `ServerTime` 對連通性檢查已足夠；監控要版本號的話本來就該帶金鑰。

   **寬鬆態（`NotConfigured`）下照舊回 `Version`**——那些部署的閘門本來就未關上，扣掉版本號
   只會讓既有監控在升級當下壞掉，卻換不到任何實質收斂。此交互要寫進註解與文件，
   否則會被當成漏判。

## 階段 1：存放模型與驗證

1. `st_api_key` TableSchema（common）+ 註冊進 `DbCategorySettings`；`ApiKeyRepository`。
2. `ApiKeyHasher`（`Bee.Base/Security/`，原語層）：`HashSecret` / `VerifySecret`，
   salt + SHA-256 + `FixedTimeEquals`，含「為何不用 `PasswordHasher`」與「salt 為格式一致
   非安全必要」兩段 WHY 註解（D3）。
3. 金鑰格式工具：產生（`RandomNumberGenerator` 256-bit，URL-safe base64）與解析
   `{sys_id}.{secret}`；`sys_id` 字元集 / 長度驗證依 D2（禁 `.`）。格式不符即快速失敗，
   不進 DB 查詢——這也是「無 rate limit 仍不懼掃描」的第一道，配合負向快取讓不存在的
   `sys_id` 不穿透到 DB。
4. **產生金鑰的 BO 方法**（依 `bee-add-bo-method` 流程，回傳一次性完整金鑰）。
   置於階段 1 而非階段 3：D5 的 gate 一旦有第一把金鑰就轉嚴格態，若產生手段留在階段 3，
   階段 1 單獨上線的部署將**無法建立第一把金鑰**（框架刻意無還原路徑，只能手算
   `v1.{salt}.{hash}` 手動 INSERT）。管理表單仍留階段 3。
5. `ApiKeyCache : KeyObjectCache<ApiKeyInfo>`，依 D4：`CreateInstance` 經
   `ICacheDataSourceProvider.GetApiKey` 自載（快取持 `Func<ICacheDataSourceProvider>`、
   帶該參數的建構式為 `internal`）、覆寫 `GetPolicy` 為 absolute TTL、`ICacheContainer` 三處同步、
   cache-notify 失效與兩個 CacheNotify 測試 stub（依 `bee-add-cache-object` 流程）。
   `ApiKeyInfo` 放 `Bee.Definition`；`ICacheDataSourceProvider` 加取數方法、
   `CacheDataSourceProvider` 實作、`ISystemRepositoryFactory` 加 `CreateApiKeyRepository()`。
6. **gate 狀態快取**（D5）：「是否有啟用中金鑰」與金鑰共用同一 cache group，金鑰
   新增 / 停用 / 刪除一併發 notify；例外不寫入快取。
7. `IApiKeyValidator`（`Bee.Definition/Security/`）+ 預設實作，由 `AddBeeFramework` 註冊。
   拒絕時**對外一律回同一訊息**，不區分查無 / 停用 / 過期（避免成為 oracle）；區分只存在
   稽核記錄中。
8. `ApiAuthorizationContext` 增加**驗證結果**承載欄位（狀態 + `sys_id` / `sys_name`，
   依 D6 不承載驗證器）；`ApiServiceController.ValidateAuthorization` 從 DI 解析驗證器、
   在該處驗完並帶入結果；`ApiAuthorizationValidator` 依結果決策——嚴格態比對、
   寬鬆態沿用非空檢查、DB 異常 fail-closed。
   同時依 D10 新增金鑰豁免清單（`protected virtual`，預設只含 `System.Ping`），
   與既有的 Bearer 豁免 `NoAuthMethods` 並存且互不引用。
9. 依 D10 調整 ping：`PingResult` 增金鑰狀態欄（四態）、`Version` 改為金鑰有效才回
   （寬鬆態照舊回）。
10. 啟動警告訊息與觸發條件改依 D5。
11. **金鑰驗證失敗的稽核記錄**：明確不做 rate limit（見「不在範圍」）的前提下，失敗記錄是
    唯一的偵測手段，故階段 1 就要落地，不等階段 2。只記前段 `sys_id` 與來源，
    **絕不記完整金鑰值**（對齊 `security.md`）。
12. 測試：命中 / 查無 / 停用 / 過期 / 格式不符 / 無啟用金鑰沿用舊行為 / 雜湊 round-trip /
    cache-notify 失效生效 / **建第一把金鑰後 gate 轉嚴格** / **DB 查詢擲例外時 fail-closed
    （不得降級為寬鬆）** / **ping 不帶金鑰仍回 `ok` 且不含 `Version`** /
    **ping 帶無效金鑰回報 `Invalid`、寬鬆態回報 `NotConfigured`** /
    **`Login` 與 `GetApiPayloadOptions` 仍需金鑰** / **四種拒絕情境對外訊息一致**。

**驗收**：建了金鑰的部署，錯誤金鑰被拒；沒建的部署行為與升級前一致；
**不需等階段 3 即可產生第一把金鑰並完成引導**；DB 不可用時 ping 仍可回應，其餘方法 fail-closed。

### 執行結果（2026-07-30）

驗收條件全數達成，`dotnet build Bee.Library.slnx -c Release` 0w/0e、`./test.sh` 全綠
（新增 26 個測試，5 個 dialect 的 repository round-trip 全數執行）。與計畫的差異與追加決策：

| 項目 | 落地情況 |
|------|---------|
| `ApiKeyStatus` 為**五態** | 計畫定四態，實作多一個 `NotChecked = 0`：行程內呼叫本來就沒有 `X-Api-Key` 標頭，把它報成 `NotProvided`（嚴格態、被拒）或 `NotConfigured`（部署未設定）都是錯的敘述。授權決策上 `NotChecked` 與 `NotConfigured` 同路（沿用非空檢查） |
| `CreateApiKey` 為 `LocalOnly` | 計畫未指定保護等級。取 `SaveDefine` 的同一理由：鑄造憑證屬部署期作業，只憑「已驗證」不該能自行鑄一把金鑰。**代價**：階段 3 的遠端管理表單需要一條權限把關的路徑，屆時才處理 |
| 金鑰狀態傳遞路徑 | BO 不改建構式（會破壞所有應用子類），改以 `IApiKeyContextAware` 由 executor 在建構後賦值。此接縫階段 2 的稽核可直接沿用 |
| 表存在與否的判定 | 走 `ITableSchemaProvider.GetTableSchema` 回 `null`，不靠「查詢失敗」推論——這是 D5 第三列 fail-closed 能與「表不存在即寬鬆」分開的關鍵 |
| gate 快取 | `ApiKeyGateCache`（單一 key `[gate]`），以 `CacheGroup` 覆寫為 `ApiKeyInfo` 與金鑰快取同群；`Insert` 在同一 transaction 內 bump 兩個 notify key |
| 附帶修正 | `ApiOutputConverter.ConvertResultValue` 的讀取選項缺 `JsonStringEnumConverter`，與 `JsonCodec` 的寫入端不對稱。`PingResult.ApiKeyStatus` 是第一個 enum 型別的回應欄位，因此才浮現 |
| 既有測試的行為調整 | `PostAsync_MissingApiKey_Returns401` 原以 `System.Ping` 為樣本，Ping 依 D10 免金鑰後改用 `System.ExecFunc`；`DefaultsTests` 的嵌入檔數 29 → 30 |

尚未觸及（不屬階段 1）：samples / Northwind 的 `DbCategorySettings` 未註冊 `st_api_key`，
因此那些部署的表不存在 → 維持寬鬆態，正是 D5 設計的升級路徑。

## 階段 2：呼叫端識別

1. 驗證成功後把 `sys_id` 與 `sys_name` 帶進呼叫上下文（沿 `ApiAuthorizationResult` → executor）。
2. 落進既有稽核家族：`st_log_login` / `st_log_access` / `st_log_anomaly_api` 增記金鑰識別欄，
   讓「誰在呼叫」在既有報表即可查。
3. **只記識別碼與名稱，絕不記金鑰值**（對齊 `security.md` 禁止事項）。

**驗收**：從稽核記錄可分辨同一支 API 是哪個應用 / 哪個第三方呼叫的。

### 執行結果（2026-07-30）

驗收達成：`api_key_id` / `api_key_name` 兩欄已落進稽核並可由既有查詢取出。
build 0w/0e、`./test.sh` 全綠（新增 7 個測試）。與計畫的差異：

| 項目 | 落地情況 |
|------|---------|
| **放在 `AuditEntry` 基底而非逐表** | 計畫列 `st_log_login` / `st_log_access` / `st_log_anomaly_api` 三張表。實作放進基底的 `AddCommonColumns`，因此**四張帶 who 的表**都有（多了 `st_log_change`）。理由：這與 `user_id` / `company_id` 是同一類「誰在呼叫」資訊，分開放才是不一致；而「誰改了資料」比「誰查了資料」更需要這個欄位。`st_log_anomaly_db` **不受影響**——`DbAnomalyEntry` 早已覆寫掉整組共通欄位（它的視角是 `database_id` + `command`），新增欄自然不會出現 |
| 拒絕記錄改用正式欄位 | 階段 1 暫時把 `sys_id` 塞在 `ErrorMessage` 裡（當時無欄可放），現改為寫入 `api_key_id`，被拒的嘗試因此能與該應用的正常呼叫一起 group。`api_key_name` 留 null——金鑰被拒代表沒有識別出應用 |
| 空字串正規化為 null | 未經閘門的呼叫（行程內、或部署尚未發金鑰）寫 null 而非空字串，讓欄位語意是「不適用」；也順帶避開 Oracle 把 `''` 當 NULL 的差異 |
| 既有部署的升級 | 兩個欄位皆 nullable，落在框架自動 schema 升級（ALTER ADD）的範圍內，不需手動 DDL |
| ADR-027 D4 補記 | 去正規化 who / company 的同一決策現含呼叫端應用，已在 ADR 內註明日期與理由 |

## 階段 3：金鑰生命週期與輪替

> **✅ 受阻已解除（2026-08-03）**：[plan-deployment-admin.md](plan-deployment-admin.md) 的階段 2
> 已落地——`CreateApiKey` 從 `LocalOnly` 放寬為 `Encrypted`，遠端呼叫改由
> `IDeploymentAuthorizationService.Can(token, ManageApiKey)` 把關，本機呼叫維持直通以保住
> bootstrap 路徑。本階段新增的方法比照同一分流。
>
> <details><summary>原受阻紀錄（2026-07-30）</summary>
>
> 管理表單是遠端操作，而 `CreateApiKey` 依階段 1 的決定為 `LocalOnly`——鑄造憑證不該只憑
> 「已驗證」。放寬需要一條「遠端可用、但不是誰登入都行」的授權路徑，而框架現有的權限判定
> 寫死在公司範圍內（`AuthorizationService.Can` 無 `CompanyId` 即回 `false`），且角色資料存在
> 各公司的資料庫——用公司層權限守部署層資產，等於讓 A 公司的管理員能鑄出整個部署通用的金鑰。
>
> </details>

### 範圍重訂（2026-08-03）

原範圍是「以框架自身的 FormSchema CRUD 做管理表單（dogfooding）」。**該前提已被階段 2 的
結論推翻**，改為「後端能力 + 文件」，管理介面移出本 plan。

#### D10：不走 FormSchema CRUD（定案）

泛用 Form 路徑上**沒有位置掛部署層授權**——把關者 `IDeploymentAuthorizationService` 只接在
`SystemBO`。[FormBusinessObject.Authorize](../../../src/Bee.Business/Form/FormBusinessObject.Permission.cs)
的兩條路都是死的：

| FormSchema 怎麼標 | 結果 |
|------------------|------|
| 不標 `PermissionModelId` | 未標即 `return`，**任何已登入使用者**都能列出 / 停用整個部署的金鑰 |
| 標了 `PermissionModelId` | `IAuthorizationService.Can` 無公司脈絡即回 `false`，且角色存在各公司的資料庫——正是本 plan 與 deployment-admin 都在否定的那條路 |

要在 Form 路徑掛部署層旁路，等於把兩條刻意分開的判定又縫回去。加上原註記的「泛用 Insert
撐不住」（金鑰必須伺服器產生、雜湊存放、明文只回一次），以及 `hashed_key` 只要被列進
FormSchema 就會經 `GetList` 送到前端（讀取端沒有 `ProtectedFields` 那種機制）——三項獨立理由
指向同一結論。

#### D11：不做刪除，停用即撤銷（定案）

停用即時撤銷、到期可排程，兩者都保留 `st_api_key` 的歷史列。**刪除會讓「這把金鑰曾經存在」
從資料庫消失，而稽核裡的 `api_key_id` 從此對不到任何列**——階段 2 才剛把呼叫端識別落進四張
稽核表，刪除會反過來砍掉它的解釋能力。需要清理時由部署端自行下 SQL，框架不提供這條路。

#### D12：撤銷必須即時生效（定案）

`ApiKeyCache` 是 60 分鐘絕對過期且有負快取。停用若不主動失效快取，最壞情況下被撤銷的金鑰仍可
通行近一小時 —— 與 deployment-admin 的 D5（撤銷不該吃快取延遲）同一原則。故停用 / 啟用 /
設到期一律在**與寫入同一個交易內**失效 `ApiKeyCache`；停用最後一把還會改變閘門狀態，
`ApiKeyGateCache` 一併失效。

> **訂正（2026-08-03）**：本條初稿寫「目前沒有任何 `Touch`」，**不正確**——
> [`ApiKeyRepository.Insert`](../../../src/Bee.Repository/System/ApiKeyRepository.cs) 早已在同一交易內
> `Touch` 金鑰與閘門兩個鍵。本條的要求因此不是「補上一個缺失的機制」，而是
> **新增的寫入方法必須沿用 `Insert` 已經建立的形狀**。

### 步驟

1. **`IApiKeyRepository` 補生命週期方法**：`GetList`（不含雜湊的摘要）、`SetEnabled`、`SetExpiry`。
   寫入後失效 `ApiKeyCache` 與 `ApiKeyGateCache`（D12）。
2. **對應的 SystemBO 方法**，照 `bee-add-bo-method` 的跨層樣板走（`SystemActions` 常數 →
   contract → wire 型別 → BO → connector），把關比照 `CreateApiKey`：`IsLocalCall` 直通、
   遠端要 `Can(token, ManageApiKey)`。全部經 `WriteDeploymentAudit` 留痕
   （deployment-admin 階段 3 已備妥）。
3. **列出用的摘要型別不得帶 `HashedKey`**——`ApiKeyInfo` 是 cache 的載體、帶雜湊，不可直接上
   wire。另立摘要型別。
4. **文件（雙語）**：API Key 的定位（應用識別 ≠ 使用者鑑別）、發放與輪替流程
   （發第二把 → 用戶端逐步換 → 停用舊把）、第三方介接指引。
   `framework-reserved-names.md` 的 `st_api_key` 一列已存在，確認描述與現況一致即可。

**驗收**：可在不直接下 SQL 的前提下完成一次完整輪替（發第二把 → 換過去 → 停用舊把），
且停用後的金鑰**立即**被拒；被拒與被撤銷的動作都查得到稽核。

### 執行結果（2026-08-03）

驗收條件全數達成，`dotnet build Bee.Library.slnx -c Release --no-incremental` 0w/0e、
`./test.sh` 全綠（新增 12 個測試）。與計畫的差異與追加判斷：

| 項目 | 落地情況 |
|------|---------|
| **D12 的前提被實作推翻（見上方訂正）** | 計畫寫「目前沒有任何 `Touch`」，實際上 `Insert` 早已在同一交易內 `Touch` 金鑰與閘門兩鍵。新方法沿用該形狀，落點是 `ApiKeyRepository.UpdateColumn`（`SetEnabled` / `SetExpiry` 共用），影響零列時 rollback 並回 `false` |
| **`ApiKeySummary` 落在 `Bee.Definition.Security`** | 原想放 `Bee.Api.Contracts`（比照 `PackageUpdateInfo`），但 repository 也要產出它，而 `Bee.Repository.Abstractions` 只依賴 `Bee.Definition`。放 Definition 讓 repository / contracts / business 三層共用同一型別，不必在中間再轉一次 |
| **授權判定抽成 `RequireApiKeyManagement`** | 階段 2 的執行結果寫「只有一個呼叫端，三行留在呼叫端看得見；真的長到三處再抽」。現在是四處，照約定抽出，並在 XML doc 寫明「本機直通不是漏洞，是 bootstrap 路徑」 |
| **`SetApiKeyExpiry` 接受過去的時間** | 與 `CreateApiKey` 刻意不同：發一把出生即死的金鑰是失誤，把既有金鑰設為此刻起失效則是退役的正當手段。兩者不共用同一條驗證 |
| **稽核前值不能用 `GetEnabledById` 讀** | 它濾掉停用列，重新啟用時前值會讀成「查無」，稽核就會宣稱這把金鑰是新建的。改以 `GetList` 找（含停用列） |
| 文件落點 | 新增雙語 `docs/api-key-management.md` / `.zh-TW.md`（定位、閘門自啟、誰能管理、輪替、第三方介接、用戶端存放、留下什麼記錄），並列入 `docs/README*` 索引兩處 |

實作時才想清楚、且已寫進文件的一個坑：**只有一把金鑰的部署，停用它會讓閘門退出生效狀態**
（沒有啟用中的金鑰 → 回到「任何非空標頭皆通過」）。這是既有的相容性設計、不是缺陷，
但它讓「停用舊金鑰」成為輪替的錯誤最後一步 —— 除非新的那把已經先發出來。
`SetApiKeyEnabled` 的 XML doc 與輪替文件都明寫了這點。

> **管理介面移出本 plan**：落點（`dotnet bee apikey …` vs DefineEditor 分頁）記在
> `docs/repo-ops/future-work.md` 的「部署期作業工具」一案，與 `SetDeploymentAdmin` 的入口
> 同時決定——那個工具本來就要服務兩者，綁進本 plan 會讓更大的落點決定被單一 feature 綁架。

## 階段 4：用戶端存放

1. `ClientSettings` 增 ApiKey 欄位，`ClientInfo` 提供讀寫接縫（對齊 `GetEndpoint` / `SetEndpointAsync`）。
2. samples 與 `apps/Bee.Northwind` 從 `AppDefaults.ApiKey` 常數改為由設定讀取；
   連線設定畫面（`ConnectionViewModel`）比照 endpoint 提供輸入。
3. 各平台 storage 實作確認可承載（`FileEndpointStorage`、`MauiPreferenceEndpointStorage`、WASM）。
4. README 更新（依 `rules/public-docs.md`，不連結本 plan）。

**驗收**：更換 API Key 不需重新編譯任何用戶端。

### 執行結果（2026-07-30）

驗收達成。build 0w/0e（`Bee.Library.slnx` / `Bee.Samples.slnx` / `Bee.Northwind.slnx` 三個方案）、
`./test.sh` 全綠（新增 12 個測試）。

**D8 的前提被實測推翻，接縫因此改形狀**（已與使用者確認後定案）：D8 主張「`IEndpointStorage`
的實作端本來就以 `ClientSettings` 為後盾，金鑰加在 `ClientSettings` 即可」。實際上 6 個 host 有 5 個
**刻意繞開** `ClientSettings`——`FileEndpointStorage`（Northwind Desktop / iOS / Android、
Avalonia.Demo）與 `BrowserLocalStorageEndpointStorage`（Northwind Browser），因為那份 XML 位於組件
路徑旁，iOS bundle 唯讀、WASM 無檔案系統。照 D8 原樣做，金鑰在這 5 個 head 上無處持久化，驗收正好
在最需要的地方不成立。

| 項目 | 落地情況 |
|------|---------|
| 接縫形狀 | **新增平行的 `IApiKeyStorage`**（`Bee.UI.Core`）+ `ClientSettings`-backed 預設實作，經 `ClientInfo.ApiKeyStorage` 指派。不擴充也不改名 `IEndpointStorage`（D8 的原始顧慮成立），既有實作零破壞 |
| 平台承載 | 既有兩個平台 storage 類別**一併實作** `IApiKeyStorage`：`FileEndpointStorage` 多寫一個 `apikey.txt`、`BrowserLocalStorageEndpointStorage` 多存一個 localStorage key。head 把同一個實例指派給兩個屬性，不重複造輪子 |
| 硬編碼的去法 | `ClientInfo.ApplyApiKey(defaultApiKey)`：存放為空才以應用內建值**當種子寫入**，之後一律以存放值為準。`AppDefaults.ApiKey` 因此從「硬編碼金鑰」降格為「首次啟動預設值」，不必為了保留 out-of-the-box 體驗而留死值 |
| 連線設定畫面 | Northwind `ConnectionViewModel` 增 `ApiKey` 欄位與輸入框，**在 ping 之前套用**——金鑰打錯會當場顯示，而不是延到第一次真正呼叫才在別的畫面浮出 |
| 命令列覆寫 | `ClientInfo.InitializeAsync` 比照 `Endpoint` 支援 `ApiKey` 參數（僅記憶體、不改存放值）；註解寫明「參數列可從行程表讀取，真正的憑證不該走這條」 |
| QuickStart.Console | 只引用 `Bee.Api.Client`（無 `Bee.UI.Core`），故不走 storage：改為 `--apikey` 參數 + demo 預設值 |

命名上的取捨：`FileEndpointStorage` 實作 `IApiKeyStorage` 讀起來名不符實，但它是已發佈型別，改名即
breaking，故保留原名並在類別註解說明它承載兩個值。

## 不在範圍

- **不改 AccessToken 鑑別鏈**。本計畫只補應用識別層；使用者鑑別維持現行 Bearer token 機制。
- **不做 per-key 權限 / 配額 / rate limiting**。金鑰只有「有效 / 無效」，不承載授權語意——
  授權是 `PermissionModels` 的職責，混進來會變成第二套權限系統。`key_type` 只是標示，不影響判權。

  沒有 rate limit 為何可接受：secret 是 256-bit 高熵值，猜測不可行；成本面上，格式不符
  **不進 DB**（階段 1 第 3 點），不存在的 `sys_id` 由負向快取擋住，故亂打金鑰不會放大成
  DB 壓力，只多一次記憶體查表。代價是**偵測**——這正是階段 1 第 11 點把失敗稽核記錄
  提前落地的原因。
- **不處理 mTLS / OAuth client credentials**。那是不同層級的部署決策。

## 待確認

1. ~~**DB 不可用時的行為**~~ —— **已決策（見 D10）**：`System.Ping` 免金鑰，健康檢查在 DB
   不可用時仍能作答；其餘方法 fail-closed（DB 掛了 API 本來就沒用）。快取 TTL 依 D4 拉長為
   absolute 60 分，DB 短暫不可用期間已在快取的金鑰仍可服務。此行為須寫進文件，避免日後被當 bug 追。
2. ~~**金鑰是否綁 CompanyId**~~ —— **已決策（2026-07-30）：不綁**，維持金鑰只識別應用。
   一旦金鑰帶公司語意，就與 session 公司情境形成兩個來源、需交叉驗證。

   與 [plan-row-level-tenancy.md](../plan-row-level-tenancy.md) **正交無衝突**：`st_api_key` 是
   common scope 表，`sys_company_id` 只作用於 company 表。租賃（pooled）模式下公司情境仍來自
   session，此決策不受影響。

   重議的**唯一觸發條件**：出現**無 Bearer 的機器對機器（M2M）介接**需求——那時金鑰是唯一的
   公司來源，非綁不可。屆時比照 D9 的處理方式，是**新增一條 M2M 路徑**（金鑰可帶公司語意），
   而不是改動現行金鑰的語意。
3. **相容模式的長期處置**：D5 為相容保留寬鬆態。是否在某個大版本改為「無金鑰即拒絕」？
   若要，須在 CHANGELOG 標 breaking 並給遷移指引。
