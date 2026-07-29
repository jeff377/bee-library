# 計畫：SessionInfo 持久化與重建（`st_session` 種子）

**狀態：📝 擬定中（2026-07-29）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | 前置：金鑰改為可推導（`DerivedApiEncryptionKeyProvider`）＋ `st_user` 語系欄位 | 📝 待做 |
| 2 | 種子持久化：`SessionUser` 擴充、四個寫入點、Login / `CreateSession` 共用建構路徑 | 📝 待做 |
| 3 | 重建：`ICacheDataSourceProvider.GetSessionInfo` ＋ `SessionInfoCache.CreateInstance` ＋ 讀取純化 | 📝 待做 |
| 4 | 收尾：`oneTime` 明確拒絕、過期列清理排程、CHANGELOG 與文件 | 📝 待做 |

> 承接自 [plan-cache-createinstance-db-loading.md](plan-cache-createinstance-db-loading.md) 的階段 3 / 4。
> 該 plan 的階段 1 / 2（三個 DB 快取改為經 `ICacheDataSourceProvider` 自載）已完成並上線，
> 本 plan 沿用它建立的接縫。

## 背景

`SessionInfoCache.CreateInstance` 是四個 Database 快取中唯一仍回傳 `null` 的——快取一失效，
使用者就得重新登入。要讓它能重建，有兩個前置缺口：

**缺口 A —— Login 完全沒寫 DB。**
[SystemBusinessObject.Session.cs:64](../../src/Bee.Business/System/SystemBusinessObject.Session.cs)
只做 `SessionInfoService.Set(sessionInfo)`。全 repo 唯一寫 `st_session` 的是
`SessionRepository.CreateSession`（僅 `LocalOnly` 的 `SystemBO.CreateSession` 呼叫）。
即登入產生的 session 在 DB 沒有任何 row，快取一失效就真的沒了——
`ICacheDataSourceProvider.GetSessionUser` 這個既有接縫即使接上也重建不出東西。

**缺口 B —— 持久化形狀不足。** `st_session.session_user_xml` 存的是 `SessionUser`（5 欄），
`SessionInfo` 有 14 欄。缺 `ApiEncryptionKey`（`Encrypted` API 無法解密）、
`CompanyId` / `CustomizeId` / `Roles` / `UserRowId` / `EmployeeRowId` / `DeptRowId`
（EnterCompany 快照，權限與 record scope 失效）、`Culture` / `TimeZone`。

### 為何需要持久化（不只是「少登入幾次」）

三種「快取失效」成因，正解不同：

| 成因 | 現況後果 | 持久化是否解 |
|------|---------|-------------|
| 20 分鐘 sliding 逐出 | 閒置者被迫重登 | ✅ |
| 行程重啟 / 部署 | 全部使用者被登出 | ✅ |
| 多節點 | Login 打到 node A、下個請求到 node B 即 401 | ✅ |

第三項最關鍵：框架已以多節點為目標（cache-notify 整套機制存在的理由就是跨節點失效），
但 session 存在行程內記憶體，且只有 `MemoryCacheProvider` 一個實作——**session 目前沒有
跨節點的儲存體**。持久化與未來的 Redis `ICacheProvider` 是兩條可行路線，本 plan 走 DB，
理由是地端 ERP 部署要求客戶另架 Redis 的推行成本高，而資料庫是現成的。

## 核心原則：`st_session` 存的是「重建種子」，不是 `SessionInfo` 快照

不可把 `SessionInfo` 整個序列化落地——它含角色 / 權限 / 部門等**必須為當下即時值**的資料，
存成快照等於把「管理員改了權限、現存 session 不生效」這個問題固化進資料庫；且未來
`SessionInfo` 還會擴充欄位，快照格式會持續腐化。

改為：登入時寫入最小種子，快取失效時**重跑 Login / EnterCompany 的推導**還原完整 `SessionInfo`。

**種子的生命週期綁在「登入」，不是「進入公司」。** `AccessToken` 於 Login 產生後即固定，
`EnterCompany` 只覆寫 `CompanyId`（其 XML doc 已載明切換公司亦走同一方法）。因此一次登入
對應 `st_session` 一列，使用者在多家公司間切換幾次，都只是該列的 `CompanyId` 在變——
**切換公司與首次進入公司是同一個寫入點，不需額外機制**。

## 種子欄位盤點

依「能重建的就不要存」：

| 欄位 | 來源 | 進種子？ |
|------|------|---------|
| `AccessToken` / `UserId` / `ExpiredAt` | Login | ✅ `ExpiredAt` 是權威到期時間，不可重算 |
| `CompanyId` | EnterCompany | ✅ 無法推導使用者選了哪一家 |
| `OneTime` | CreateSession | ✅ 沿用既有欄位 |
| `UserName` / `TimeZone` | `st_user`（`ApplyUserTimeZone` 讀 `time_zone`） | ❌ 可推導 |
| `Culture` | `st_user`（本 plan 新增欄位，見 D4） | ❌ 可推導 |
| `CustomizeId` | `CompanyInfo.CustomizeId` | ❌ 可推導 |
| `Roles` / `UserRowId` / `EmployeeRowId` / `DeptRowId` | 權限表 / `EmployeeContextResolver` | ❌ 必須即時 |
| `ApiEncryptionKey` | Login 導出（見 D3） | ❌ 改為可推導 |

**種子的主體就是既有的 `SessionUser`**（存於 `st_session.session_user_xml`）——它的 XML doc
原本就寫著「retains the information needed to reconstruct a `SessionInfo`; this data is
persisted in the database」，本來就是為此而設的型別。不新增型別，擴充它即可。

實際落差極小，現有 5 欄已覆蓋多數需求：

| `SessionUser` 現有欄位 | 對應 |
|------|------|
| `AccessToken` / `UserID` | ✅ 已足夠 |
| `EndTime` | ✅ 即 `SessionInfo.ExpiredAt` |
| `OneTime` | ✅ 沿用 |
| `UserName` | ✅ 雖可由 `st_user` 推導，但既已存在且省一次查詢，保留 |

**需新增的只有 `CompanyId` 一個欄位。**

向後相容：舊格式 XML 反序列化後 `CompanyId` 為空，重建結果即「已登入但未進公司」的
session，與 `LeaveCompany` 後的狀態一致，屬合理降級。

## 四個寫入點

| 時機 | DB 操作 | 理由 |
|------|---------|------|
| Login 成功 | INSERT 種子 | token 已交付 client，DB 必須同時存在對應 row（見下） |
| EnterCompany（含公司切換） | UPDATE `CompanyId` | 否則 `CompanyId` 進種子沒有意義 |
| LeaveCompany | UPDATE `CompanyId` = `null` | 否則重建會把使用者放回已離開的公司 |
| **Logout** | **DELETE row** | 否則登出後 token 可由 DB 復活 —— 見下方安全說明 |

**為何 Login 就必須寫，不能等到 EnterCompany。**
`AccessTokenValidator` 只檢查 token 存在與未過期，**不要求已進公司**——Login 回傳的那一刻
client 手上即是有效憑證。若此時 DB 無 row：單節點下，Login 後、EnterCompany 前遇到部署或
重啟，token 立刻失效而 client 毫不知情；多節點下更糟——Login 打到 node A、選公司的請求打到
node B，快取與 DB 皆無 → 直接 401，**連 EnterCompany 都做不到**，等同「登入後必須黏在同一
節點」。Login 寫入正是讓持久化支援多節點的關鍵一步。

**Logout 必須刪除 row（安全需求，非最佳化）。**
今日 `Logout` 只做 `SessionInfoService.Remove(AccessToken)`
（[SystemBusinessObject.Session.cs:195](../../src/Bee.Business/System/SystemBusinessObject.Session.cs)），
`LeaveCompany` 同樣只清快取。重建機制一旦上線，登出只清快取將使 token 於下一個請求
由 `st_session` 重建復活，登出形同虛設。這是本 plan 引入的新漏洞，須一併處理。

### 寫入順序：先持久層，後快取

兩種不一致的後果不對稱：

- 「DB 有、快取沒有」→ 下次請求重建即可，安全降級。
- 「快取有、DB 沒有」→ 即上述多節點 401、部署即失效的狀態。

故**先寫 DB、成功後再寫快取**。先寫快取再寫 DB，一旦 DB 失敗就得回滾快取（多一條失敗路徑）；
先寫 DB 成功再寫快取，快取寫入本身幾乎不會失敗。同一原則套用於 Logout：
**先 DELETE DB，再移除快取**——破壞性操作先落地持久層，否則 DELETE 失敗會讓登出無效。

Login 的 DB 寫入失敗應**讓 Login 失敗**而非吞掉：吞掉會讓 client 取得一個隨時失效、
且多節點下不可用的 token，故障被推遲到後續請求才爆而難以診斷；common DB 故障時
整個系統本就無法運作。

## 重建流程

快取失效 → 由 `st_session` 取回種子 → 重跑 Login / EnterCompany 的推導
（`HasAccess` 檢查、`EmployeeContextResolver`、角色解析）→ 重建完整 `SessionInfo` 並回填快取。

**重建邏輯的落點**：`SessionInfoCache.CreateInstance` 不該自行做權限查詢。於
`ICacheDataSourceProvider` 新增 `SessionInfo? GetSessionInfo(Guid accessToken)`，
由 `Bee.Business.Providers.CacheDataSourceProvider` 實作。分層乾淨，並沿用快取自載那份 plan
建立的接縫。未來要換 Redis 時只需替換種子的讀寫實作，快取端與業務端不動。

**附帶的安全性提升**：重建時重跑 `HasAccess`，代表公司權限被撤銷後，
該使用者的 session 於下次重建時即失效——優於存快照。

## 決策總覽（全數定案，2026-07-29）

| 編號 | 議題 | 決策 |
|------|------|------|
| D3 | `ApiEncryptionKey` 無法重建 | 改為 HKDF 可推導，不持久化（新增 `DerivedApiEncryptionKeyProvider`） |
| D4 | `Culture` 無人設定 | 改為 `st_user` 的使用者屬性；可推導，不進種子 |
| D5 | `CreateSession` 的定位 | 非漏洞；為背景服務的「免密碼 Login」，應與 Login 共用 SessionInfo 建構路徑 |
| D6 | 讀取副作用與 `oneTime` | 讀取純化不做 DELETE；`oneTime: true` 明確擲例外，不靜默降級 |
| D7 | 過期 session 殘留 | 比照 `CacheNotifyPoller` 以 `BackgroundService` 排程清理 |

> 編號沿用原 plan（D1 / D2 屬快取自載那份），不重新編號以免交叉引用失效。

### D3：`ApiEncryptionKey` 改為可推導（`DerivedApiEncryptionKeyProvider`）

**問題**：`ApiEncryptionKey` 是 14 個欄位中唯一無法從資料庫推導的——它在 Login 由
`GenerateKeyForLogin()` 隨機產生 64-byte 組合金鑰（AES 256 + HMAC 256），存於記憶體並以
client 公鑰 RSA 加密回傳。也不能向 client 索取：伺服器若接受 client 提供的金鑰，
攻擊者即可自行指定金鑰，加密機制形同虛設。

不解決則重建後該欄位為空，使用者「帳面上仍登入」但 `Encrypted` 等級 API 全部不可用。
且失敗形式難看——[DynamicApiEncryptionKeyProvider.cs:38](../../src/Bee.Business/Providers/DynamicApiEncryptionKeyProvider.cs)
的 `sessionInfo?.ApiEncryptionKey ?? throw` 只擋 `null`，空陣列會被原樣回傳，
在加解密環節炸出密碼學例外而非乾淨的 401。

**決策：新增 `DerivedApiEncryptionKeyProvider`**，以 HKDF 由根金鑰 + `accessToken` 導出
per-session 金鑰。金鑰因此成為「可重建」資料，與角色權限同類，完全不需持久化，
且任何節點皆可算出同一把——天然適用多節點。

實作要點：

- **IKM 用 `SecurityKeySettings.ApiEncryptionKey`，不用 master key。** master key 的職責是
  解密其他金鑰、屬信任根，不應用於日常運算；`ApiEncryptionKey` 本就是「API 加密的根金鑰」，
  用途相符。Static 與 Derived 因此共用同一設定項（Static 直接使用，Derived 用以導出）。
- **`IApiEncryptionKeyProvider` 需新增 `GenerateKeyForLogin(Guid accessToken)`**——現有簽章
  無 token 參數，導出無從進行（breaking change）。
- **Login 的順序必須調整**：目前先產生金鑰
  （[SystemBusinessObject.Session.cs:50](../../src/Bee.Business/System/SystemBusinessObject.Session.cs)）
  才建立 `SessionInfo` 與 `AccessToken`；改為先產生 `AccessToken`，再導出金鑰。
- **client 端零影響**：client 由 `LoginResult.ApiEncryptionKey` 解出 RSA 密文存入
  `ApiClientInfo.ApiEncryptionKey`，伺服器如何產生該金鑰對其透明。
- **升級注意**：Derived provider 要求 `SecurityKeySettings.ApiEncryptionKey` 必須已設定
  （今日 Dynamic provider 不需要），註冊時應檢查並給出明確錯誤訊息。
- **`SessionInfo.ApiEncryptionKey` 欄位保留**（公開型別，移除為 breaking）；Derived provider 下
  `GetKey` 不再讀取它，重建時毋須還原。

**代價**：金鑰由「每次登入隨機」變為「對該 token 決定性」。安全性可接受——`accessToken`
為不可預測的 GUID，根金鑰僅存於伺服器端；根金鑰輪替會使所有現存 session 失效。

**範圍界定**：`StaticApiEncryptionKeyProvider` 從設定檔取固定金鑰、`GetKey` 不看 token，
本就不受 session 重建影響。續用 `DynamicApiEncryptionKeyProvider` 的部署則問題依然存在——
**session 重建需搭配 Static 或 Derived provider**，此限制須寫入文件。

> 與 API Key 無關：`X-Api-Key`（HTTP header 的應用識別）與 `SecurityKeySettings.ApiEncryptionKey`
> （payload 傳輸加密根金鑰）是兩把不同的金鑰，兩份 plan 零交集。

### D4：`Culture` 改為 `st_user` 的使用者屬性

**問題**：`SessionInfo.Culture` 是「有人讀、沒人寫」的欄位。
讀的一端是真實路徑——[BusinessObject.cs:117](../../src/Bee.Business/BusinessObject.cs)
的 `GetCurrentLang()` 讀取它並餵給 `LanguageService.GetLangText(...)`，BO 端所有多語文字
都經過這裡。寫的一端則不存在：全 repo 無任何程式碼寫入該屬性，它永遠停在宣告時的預設值
`"zh-TW"`。來源也不存在——`st_user` 只有 `time_zone` 而無語系欄位，`LoginArgs` 亦無語言參數。

結果是整套多語系機制空轉：不論部署在哪個地區，每個 session 都是 `zh-TW`。且因預設值非空，
`ILanguageService` 那條「退回系統預設語言」的路徑對已登入的呼叫**永遠走不到**。
這正是 `SessionInfo.TimeZone` 的 XML doc 檢討過的錯誤（該註解說明預設值刻意留空，
因為寫死會使 fallback 不可達並將框架綁定單一地區）——TimeZone 已修，Culture 未修。

**決策：比照 `TimeZone`，於 `st_user` 新增語系欄位。**

決定性理由：**背景服務發送通知時沒有 session**，只能直接查詢「該使用者讀什麼語言」。
語系因此必須是持久化的使用者屬性，而非僅存於 session 的狀態。

對本 plan 的直接結論：**`Culture` 可推導，不進種子**，重建時比照 `TimeZone` 重讀即可。
附帶好處是管理員於使用者維護變更語言後，session 重建即自動生效。

實作要點：

- `st_user.TableSchema.xml` 新增語系欄位，比照 `time_zone` 的形狀（`DbType="String"`）。
  **兩份都要改**：`src/Bee.Definition/Defaults/TableSchema/common/` 與
  `tests/Define/TableSchema/common/` 互為鏡像。
- `IUserRepository` 新增讀取方法，實作比照
  [UserRepository.GetTimeZone](../../src/Bee.Repository/System/UserRepository.cs)
  （含 Oracle 空字串即 `null` 的處理）。
  **效率考量**：Login 目前已為驗證讀一次 `st_user`、`ApplyUserTimeZone` 再讀一次；
  語系若再開第三次查詢並不划算，宜與時區合併為單次讀取。
- `BackendConfiguration` 新增 `DefaultLanguage`（對應既有的 `DefaultTimeZone`）作為 fallback。
- `SessionInfo.Culture` 預設值改為空字串，使 `ILanguageService` 的 fallback 路徑可達。
  **相容性注意**：既有部署目前隱含取得 `zh-TW`，改動後將改用 `DefaultLanguage`，
  故該設定的預設值應為 `zh-TW` 以維持現行行為。
- 背景通知服務直接讀取此欄位，不經 session。

### D5：`CreateSession` 是背景服務的「免密碼 Login」

**`CreateSession` 不是漏洞，`LocalOnly` 是真實的邊界。**
[ApiAccessValidator.cs:40](../../src/Bee.Api.Core/Validator/ApiAccessValidator.cs) 會擋下非本地
呼叫，`JsonRpcExecutor.IsLocalCall` 預設 `false`，全 repo 僅 `LocalApiProvider`（同行程 client）
會設為 `true`。遠端 HTTP 呼叫者無法觸及此方法。且對已在行程內執行的程式碼而言，簽發 token
不構成權限提升——它本就能直接寫 `st_session` 與存取整個資料庫。

**用途**：背景服務近端呼叫。當背景服務需要透過 BO 執行作業時，以此建立連線。
**正確定位**：它應該是「不需要密碼就能執行 Login / EnterCompany 的動作」，
與真實使用者建立 `SessionInfo` 的流程**完全一致**，只少了密碼驗證那一步。

**現況與此定位不符——`Login` 與 `CreateSession` 是彼此的鏡像，兩邊各只做一半：**

| | 寫快取 | 寫 DB |
|---|---|---|
| `Login` | ✅ | ❌ |
| `CreateSession` | ❌ | ✅ |

`CreateSession` 目前只呼叫 `repo.CreateSession()` 做一次 raw INSERT，**完全沒走 Login 的
`SessionInfo` 建構路徑**——不解析使用者名稱、不套用時區、不產生 `ApiEncryptionKey`、不寫快取。

**因此背景服務這條路今天走不通**：拿到 token 後若要做任何公司範圍的作業，會卡在
[RepositoryDatabaseRouter.cs:45](../../src/Bee.Repository/RepositoryDatabaseRouter.cs)——
它需要 `_sessionService.Get(accessToken)` 取出 `CompanyId` 決定連哪個公司資料庫，而快取中
無此 session，直接擲 `UnauthorizedAccessException`。
（存取控制並非卡點：`ApiAccessValidator` 第 33 行對 `IsLocalCall` 直接放行，不驗 token。
卡點在取不到公司上下文。）

**修法：抽出 Login 的 `SessionInfo` 建構路徑供兩者共用。**
`Login` = 驗證密碼 + 建構 SessionInfo；`CreateSession` = 建構 SessionInfo（略過密碼驗證）。
建構路徑一致地完成：解析使用者名稱、套用時區與語系、取得 `ApiEncryptionKey`、
寫入種子、寫入快取。背景服務隨後以該 token 呼叫 `EnterCompany` 指定公司
（會跑 `HasAccess`，故該帳號必須確實具備該公司權限），流程與真實使用者逐步一致。

**種子形狀因此統一**：`CreateSession` 與 `Login` 同樣不帶 `CompanyId`，由後續 `EnterCompany`
補上，兩條路徑無需分歧處理。

**稽核缺口**：`Login` / `Logout` 都會 `WriteLoginAudit`，`CreateSession` 目前完全不寫。
以服務身分代表某使用者建立 session 屬於需留痕的行為，應補寫稽核；
`LoginEvent` 現有四個值（`LoginSucceeded` / `LoginFailed` / `LockedOut` / `Logout`）
無合適者，需新增一個（如 `ServiceSessionCreated`），附加於列舉末端以免影響既有數值。

**行為變化須寫入 CHANGELOG**：今日 `CreateSession` 發出的 token 是死的（`CreateInstance`
回 `null`，驗證找不到 session）——那是缺陷而非保護。本 plan 之後它才真正可用，
而這正是它本來的用途。

### D6：讀取純化；`oneTime` 先明確拒絕而非靜默降級

**現況**：`SessionRepository.GetSession` 有兩個副作用——過期列讀到即 DELETE、
`OneTime` 列讀後即 DELETE。後者正是一次性語意的唯一執行機制。

**關鍵發現：D5 的修法一落地，一次性機制即自動失效。**
`CreateSession` 改為同時寫入快取後，一次性 token 的第一次使用是**快取命中**——
DB 根本不會被讀取，delete-on-read 永遠不觸發。這不是「要不要保留副作用」的取捨，
而是現行機制與新架構本質不相容。

**且找不到涵蓋兩種呼叫路徑的消費點**：`AccessTokenValidator` 對本地呼叫直接放行
（[ApiAccessValidator.cs:33](../../src/Bee.Api.Core/Validator/ApiAccessValidator.cs)），
而本地呼叫正是 `CreateSession` 的主要用途。遠端走驗證、本地不走，一次性無處消費。

**產線無使用者**：全 repo 無任何 production 程式碼傳 `oneTime: true`；
僅測試在驗證 delete-on-read 這個行為本身。

**決策（兩半）**：

1. **讀取純化**：種子讀取以查詢條件過濾未過期的列（或讀出後判斷回 `null`），**不做 DELETE**。
   安全性不受影響——`AccessTokenValidator` 本就檢查 `ExpiredAt`；刪除責任移交 D7。
2. **一次性語意先明確拒絕**：`CreateSession` 收到 `oneTime: true` 時擲
   `NotSupportedException`。**不可靜默降級**——讓一個帶安全意味的保證無聲失效是最差的選項。
   若日後確有需求，應重新設計為「交換式 handoff token」（首次使用時換發正式 session，
   該交換即唯一且明確的消費點），屬另案；產線既無使用者，不應卡住本 plan。

### D7：過期 session 以排程清理

**問題**：目前 `st_session` 幾乎無資料（僅 `CreateSession` 會寫），過期列靠 `GetSession`
讀到時順手刪除。Login 開始寫入後每次登入都是一筆 INSERT，加上 D6 移除了 delete-on-read，
未被讀取的過期列將永久殘留。

**決策**：比照既有的 `CacheNotifyPoller : BackgroundService` 模式——以 `AddHostedService`
註冊，並由設定開關控制（見
[BeeFrameworkServiceCollectionExtensions.cs](../../src/Bee.Hosting/BeeFrameworkServiceCollectionExtensions.cs)
的 `CacheNotifyOptions.Enabled` 寫法）。依到期時間 DELETE 為冪等操作，多節點同時執行亦安全。

- 設定歸屬 `BackgroundServiceConfiguration`——該類別目前為空的佔位型別，正是此用途。
- 輔助措施：Login 時順手刪除**該使用者自己**的過期列（範圍受限、成本低），
  作為排程未啟用時的兜底。

## 已完成的前置稽核

**`IsLocalCall` 稽核 —— ✅ 已完成（2026-07-29），前提成立、不需改碼。**

`CreateSession` 的整套保護建立於 `IsLocalCall`，故於動工前查核。結果：

- **預設值 fail-safe**：`JsonRpcExecutor.IsLocalCall` 宣告即 `= false`，「忘了設」落在安全的一邊。
  已補回歸測試 `JsonRpcExecutorTests.IsLocalCall_Default_IsFalse` 釘住此預設值。
- **ASP.NET host 另行明確設 `false`**
  （[ApiServiceController.cs:146](../../src/Bee.Api.AspNetCore/Controllers/ApiServiceController.cs)），
  無條件寫死——不取自 header、設定檔或請求內容，屬 defence in depth。
- **全 repo 僅一處 production 設 `true`**：
  [LocalApiProvider.cs:46](../../src/Bee.Api.Client/Providers/LocalApiProvider.cs)。
  它要求 `ApiClientInfo.LocalServiceProvider` 已指派為同行程建好的 backend service provider，
  未設即擲例外——只能由宿主程式於啟動時自行接上，遠端請求無從觸發。其餘皆為測試。
- **兩條可能的繞道均不成立**：`IsLocalCall` 位於 executor 而非 `JsonRpcRequest`，
  無法自 wire 反序列化設定；executor 為 transient、每請求一個新實例，無跨請求殘留。

## 不在範圍

### 快取過期政策維持不變（`GetPolicy` 不需覆寫）

`SessionInfoCache` 沿用 base 預設的 **20 分鐘 sliding**，而 `SessionInfo.ExpiredAt`
為 Login + 1 小時。這在**沒有重建機制的今日**確實會讓閒置逾 20 分鐘的使用者被迫重新登入，
但本 plan 落地後即自然消解：快取逐出只是記憶體回收，下次使用時由種子重建，
`ExpiredAt` 隨之取回並由 `AccessTokenValidator` 把關——**到期權威始終在種子，不在快取**。

因此不應把快取壽命釘在 `ExpiredAt`：那會讓每個 session 無論是否再被使用都佔滿一小時記憶體，
而 sliding 讓閒置者先釋放、需要時再重建，才是正確的取捨。

### 公司是 session 級而非請求級

同一個 token 於多個分頁分別進入不同公司時會互相覆寫。現行模型為「一次一家、可自由切換」。
若日後需要「同時開兩家公司作業」，需改為每請求帶公司或多 token，屬另一個設計題。

### Redis / 分散式快取

`ICacheProvider` 是既有的抽換點，補一個分散式實作可同時解決重啟與多節點——但屬另案。
本 plan 的種子讀寫刻意收斂在 `ICacheDataSourceProvider` 之後，屆時替換實作即可，
快取端與業務端不動。

## 測試

| 範圍 | 測試 |
|------|------|
| 金鑰導出（D3） | 同一 `accessToken` + 同一根金鑰導出結果一致；不同 token 導出不同；根金鑰不同則不同；長度 64 bytes |
| 語系（D4） | `st_user` 有值時取該值；無值時退回 `DefaultLanguage`；Oracle 空字串視同無值 |
| 種子 round-trip | `SessionInfo` → 種子 → 重建，欄位齊備；舊格式 XML（無 `CompanyId`）反序列化不炸、重建為未進公司狀態 |
| 四個寫入點 | Login INSERT；EnterCompany / 切換公司 UPDATE；LeaveCompany 清空；Logout DELETE |
| Logout 安全性 | 登出後以同一 token 再次請求，**不得**由 DB 重建復活 |
| 重建 | 快取清空後 `Get` 能重建；權限被撤銷後重建失敗；過期種子回 `null` |
| 讀取無副作用（D6） | 讀取種子不產生任何 DB 寫入 |
| `oneTime`（D6） | `oneTime: true` 擲 `NotSupportedException` |
| 清理排程（D7） | 過期列被刪除；未過期列保留；重複執行冪等 |

## 風險

| 風險 | 因應 |
|------|------|
| Logout 未刪 DB → 登出失效 | 列為四個寫入點之一，並有專門測試 |
| 寫入順序寫反 → 多節點 401 | 「先持久層後快取」寫進實作註解與 review checklist |
| `IApiEncryptionKeyProvider` 為 breaking change | 登錄 `PublicAPI.Unshipped.txt`；CHANGELOG 標示 |
| 續用 Dynamic provider 的部署重建後加密 API 失效 | 文件明示「重建需搭配 Static 或 Derived」；註冊時檢查並給明確錯誤 |
| `st_session` 無限成長 | D7 排程 + Login 兜底清理 |
| Login 多一次 DB 寫入的延遲 | 單筆 INSERT，與既有的稽核寫入同級；失敗即讓 Login 失敗而非吞掉 |

## 執行順序

階段 1 的兩項（D3 金鑰導出、D4 語系欄位）彼此獨立，也獨立於持久化本體，可分別 commit 驗證。
階段 2 → 3 有先後相依（先有種子才談得上重建）。階段 4 為收尾。
