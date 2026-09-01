# 計畫：JSON-RPC 封包重放防護（Anti-Replay）

**狀態：🚧 進行中（2026-09-01）**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | Wire frame 承載 timestamp，伺服器端時窗檢查 | ✅ 已完成（2026-09-01） |
| 2 | Per-session 序號滑動視窗（零 DB），`ApiAccessControlAttribute` 第三維度 | ✅ 已完成（2026-09-01） |
| 3 | 重放事件納入 anomaly log，文件與 ADR | 📝 待做 |

## 背景

目前一個合法封包被原樣重送，伺服器會完整執行第二次。`AesCbcHmacCryptor` 的 HMAC
保證封包**改不了**，但不保證它**沒被送過第二次**——加密防機密性與竄改，不防重複。

威脅來源由高到低：合法但惡意的用戶端（自己抓自己的包重送，TLS 完全無效）、log 外洩
（APM / gateway 記錄完整 request body）、企業 MITM proxy、無 TLS 的內網部署。

## 範圍

**本計畫只處理 `Encoded` 與 `Encrypted` 兩種格式的重放防護。**

`Plain` 格式在密碼學上無法防重放（D11），而收斂 `ApiProtectionLevel.Public`
——目前允許 `Save` / `Delete` 以明文呼叫——牽涉 JS 呼叫端的可行性評估，
**另案討論，不在本計畫範圍**。D11 保留該議題的完整盤點供後續使用。

## 現況盤點

**已有的**

- `AesCbcHmacCryptor` — payload 完整性（[AesCbcHmacCryptor.cs](../../src/Bee.Base/Security/AesCbcHmacCryptor.cs)）
- `ApiAccessValidator` — AccessToken 驗證 + 加密等級強制（[ApiAccessValidator.cs](../../src/Bee.Api.Core/Validator/ApiAccessValidator.cs)）
- `SessionInfoCache` — process-wide 記憶體快取，key 為 accessToken，每次驗證本來就會取
- 一次性 session（`CreateSessionArgs.OneTime`）
- anomaly log 管線，已有 `AnomalyKind.Unauthorized` 這類「呼叫被拒」的先例

**缺的**

沒有 nonce、timestamp、序號，也沒有伺服器端去重。`JsonRpcRequest.Id` 存在但不做去重，
只是 JSON-RPC 規格的關聯 id。

## 設計決策

### D1：frame header 放進加密封套，不放 `ApiPayload` 欄位

**這推翻了初期的直覺方案。** `ApiPayload` 的 `Format` / `TypeName` 是**明文信封**，
只有 `Value` 被加密（見 [ApiPayloadConverter.cs](../../src/Bee.Api.Core/JsonRpc/ApiPayloadConverter.cs)
的 `TransformTo`）。在 `ApiPayload` 上新增 `Timestamp` / `Sequence` 屬性，這兩個值會落在
明文層、不受 HMAC 保護，攻擊者可以任意改寫——等於沒防。

正解是在 `TransformTo` **加密之前**把 frame header 前置到 `bytes`，`RestoreFrom` 解密後
再剝離：

```
[ version(1) | timestamp(8, Unix ms) | sequence(8) ] ++ body bytes
```

好處：不動任何 message 型別、不動 `WireContracts` 註冊（frame 是手工 byte 編碼，
不經 MessagePack 型別系統）、天然受 HMAC 保護、client 與 server 兩端自動對稱。

**version 位元組不是「預留擴充」的條件反射，而是因為 frame 無法自我描述長度。**
解密後拿到的是 `frame ++ body`，兩者之間沒有分隔符——解析端必須在碰 body 之前就知道要
吃掉幾個 byte。日後 frame 若要加欄位（真的需要 nonce、或 timestamp 改精度），新舊長度
不同而長度無從判斷，屆時就只能再做一次 D8 那種全體斷裂升級。1 byte 換掉未來一次全體
斷裂，這筆帳划算。

### D2：防護強度依 `PayloadFormat` 分級，且必須誠實記錄

| Format | 有無 HMAC | 防護效力 |
|--------|----------|---------|
| `Encrypted` | 有 | **完整**——frame 改不動 |
| `Encoded` | 無 | **僅擋無腦原樣重送**；會改封包的攻擊者可自行改 frame |
| `Plain` | 無 | 無防護（本來就只給 `ApiProtectionLevel.Public`） |

`Encoded` 這格是限制不是 bug，但**不得在對外文件宣稱它防重放**。

### D3：驗證時機要從「解密前」移到「解密後」

`JsonRpcExecutor.ExecuteAsyncCore` 目前刻意在解密前呼叫 `ValidateAccess`，讓未授權請求
不必付解密成本。frame 在加密封套內，因此重放檢查只能排在 `RestoreFrom` 之後，成為第二道
獨立閘門。代價是重放封包會觸發一次解密——放大倍率極小，可接受。

### D4：序號用滑動視窗，不用 nonce set

nonce set 需要無界儲存或每次打 DB。改用 per-session 單調遞增序號 + 64-bit bitmap
（IPsec anti-replay window，RFC 6479 的做法）：

- 伺服器端每個 token 只存 `highest`（目前最大序號）+ bitmap（往回 64 格的使用紀錄）
- 比 highest 大 → 移位、接受；落在視窗內且未設位 → 設位、接受；已設或超出視窗 → 拒絕
- **每 session 16 bytes、判斷是幾個位元運算、零 DB 往返**
- 視窗容忍亂序，並行請求不會被誤殺（純「嚴格遞增」做不到）

**用戶端計數器的擁有者是 `ApiSessionContext`，不是 connector。** 一個 app 通常同時持有
多個 connector（`FormApiConnector` / `SystemApiConnector` / `LogApiConnector` 都繼承
`ApiConnector`），而它們共用同一個 `Session`。計數器若掛在 connector 實例上，每個
connector 各自從 0 遞增，同一 token 就會送出重複序號，第二個 connector 的請求會被視窗
當成重放全部拒掉。判準：**server 端視窗是 per-token，client 端計數器就必須是同一粒度**，
而 `ApiSessionContext` 正是那個粒度；connector 只是取號的人。

取號用 `Interlocked.Increment(ref long)`——多個 connector 會並行取號。這也正是視窗必須
容忍亂序的原因：取號原子，但送達順序不保證。

### D5：視窗狀態不得掛進 `SessionInfo`

`rules/definition.md` 明訂 cache 取出的物件 init 後不可 mutate，而視窗每個請求都要寫。
必須另立平行的 per-token runtime 結構。塞進 `SessionInfo` 加個欄位是跨 session 洩漏的
典型踩法。

### D6：多節點會退化，但退化得溫和

`SessionInfoCache` 是 process-wide，多節點各有一份。無 LB affinity 時，同一封包重放到
另一節點會過——**重放次數上限等於節點數，不是無限**。單節點部署或 accessToken affinity
下沒有這個問題。這比「時窗內無限重放」好一個量級，不因為不完美就不做。

### D7：attribute 加第三維度要用 property，不能加建構子參數

`ApiAccessControlAttribute` 是已發佈的 public 型別。對既有 public 建構子加 optional 參數
是**二進位不相容**（`rules/commit-verification.md`）。新增獨立的 init-only property，
預設值維持現行行為。

### D8：引入 frame 本身就是斷裂變更，策略必須 fail-closed

**D1 的 version 位元組解決不了這一刻的問題**——舊 client 送的封包根本沒有那個 byte，
新 server 讀第一個 byte 會讀到 body 的內容。而框架目前**沒有任何 client / server 版本
協商機制**（[ApiConnectValidator](../../src/Bee.Api.Client/ApiConnectValidator.cs) 只驗
端點型態），所以無法靠協商繞過。

**不可用明文 flag 標示「本封包有無 frame」**：那是典型的降級攻擊面，攻擊者把 flag 改掉，
伺服器就不檢查了，整套防護歸零。

正解是由**部署設定**決定是否使用 frame，封包不得自述。實作上兩端讀同一個開關
`ApiServiceOptions.RequireWireFrame`：

- 開啟時 client 一律寫 frame、server 一律期待 frame；關閉時兩端都不用，行為與升級前完全相同
- **兩端設定不一致必然失敗**，這是刻意的：server 端若「偵測」frame 在不在，攻擊者只要把
  frame 拿掉就能讓 server 以為本來就沒有——那正是要防的降級。回歸測試
  `RestoreFrom_FrameWrittenButNotExpected_FailsToDecode` 釘住這個行為
- 部署順序因此是：**兩端先升套件**（開關預設關閉，零行為變化），**再同時開啟兩端開關**

這也意味著**開關不能做成 per-method**（attribute 只能決定「檢不檢查序號」，不能決定
「解不解析 frame」）——frame 的有無是連線層級的事實，不是方法層級的政策。

### D9：frame 佈局與解析規則

**v1 固定 17 bytes**：`version(1) | timestamp(8, Unix ms, big-endian) | sequence(8, big-endian)`。

**位置在壓縮之後、加密之前。** `IApiPayloadTransformer.Encode` 已包含 Serialize + Compress
（見 [ApiPayloadTransformer.cs](../../src/Bee.Api.Core/Transformers/ApiPayloadTransformer.cs)），
frame 前置於 `Encode` 的輸出。因此接收端解密後可直接讀 frame，剝離後才 `Decode`。

**伺服器不偵測 frame 是否存在**——依 D8，設定要求 frame 就一律把前 17 bytes 當 frame 讀。
「看起來沒有 frame 就當作沒有」等於自建降級攻擊入口。

**version 位元組在 v1 的作用是診斷，不是安全。** 舊 client 打開了開關的新 server 時，
前 17 bytes 其實是壓縮後 body 的開頭，首 byte 是壓縮容器的 magic、不會等於 1，於是能回報
「frame 版本不符，請升級用戶端」，而不是把 body 亂數解讀成 timestamp 後報出「時間偏差
三萬年」這種完全誤導的錯誤。**這條檢查擋不住攻擊者**（version 可偽造，只是還得過 HMAC），
不得寫成安全機制。它真正的正戲在 v2：讀第一個 byte 即知要吃掉幾個 byte，不必再斷裂一次。

**frame 是雙向的。** 兩端共用同一份 converter 與同一個開關，因此 server 回應也會帶 frame，
client 剝離後丟棄、不做檢查。這是對稱設計的自然結果而非疏漏——要讓 response 不帶 frame，
就得讓 converter 知道方向，而那需要對既有 public 方法加參數（見階段 1 的實作註記）。
response 方向的 frame 目前是純開銷（17 bytes），但也預留了日後防 response 重放的位置。

**邊界條件**

- 解密後 `bytes.Length < 17` → 直接拒，不可讓它變成 index out of range
- `Plain` 格式沒有 frame：`TransformTo` 的 Plain 分支直接 return，`Value` 不是 byte[]
- 大小端固定 big-endian，不隨平台走
- **序號跳躍需設上限**：首次請求會直接把視窗 `highest` 設為收到的值，若用戶端因整數運算
  失誤送出接近 `long.MaxValue` 的序號，該 session 之後所有正常請求都落在視窗外而卡死
  （token 有效、金鑰正確、就是全被拒，極難診斷）。這不需要攻擊者，一個 bug 就夠了

**為何 sequence 佔 8 bytes**

不是因為計數會不夠——`long` 上限 9.22 × 10¹⁸，每秒百萬請求也要 29 萬年才用完，4 bytes
的 42.9 億對單一 session 早已綽綽有餘。理由是省下的 4 bytes 相對壓縮後 payload 僅約
0.5–1%，而溢位後果（wrap 回 0 → 全部落在視窗外 → session 卡死）與省下的量不成比例。
`long` + `Interlocked.Increment` 也是 .NET 最自然的寫法。

**序號可以從 0 開始，不需要時間成分。** 曾考慮 Snowflake 式（高位塞 Unix ms）以應付
client 重啟後序號重來，查證後不需要：`ApiSessionContext.ApiEncryptionKey` 只活在記憶體
（[ApiSessionContext.cs](../../src/Bee.Api.Client/ApiSessionContext.cs)），client 重啟
必然重新登入換到新 accessToken，而視窗是 per-token，新 token 即全新視窗。

### D10：序號不解決重試，冪等鍵才解——這是行為變化

取號之後請求逾時，重送用同號或新號都不對：

- **同號** → server 若其實已處理成功、只是回應遺失，第二次被視窗拒 → 使用者看到失敗但資料已寫入
- **新號** → 能過視窗，但業務層確實執行了兩次

這不是序號能解的問題。序號解「拒絕重放」，冪等鍵解「安全重試」，兩者不可互相取代。
**冪等鍵不納入本計畫範圍**，但因此必須明講：開啟序號檢查後，逾時重送會失敗而非重試成功。

查證結果：框架目前**沒有自動重試**——`RemoteApiProvider` 直接呼叫 `HttpUtilities.PostAsync`，
用戶端沒有 retry 迴圈或 Polly（[RemoteApiProvider.cs](../../src/Bee.Api.Client/Providers/RemoteApiProvider.cs)），
所以開啟 anti-replay 不會打壞既有框架行為。但**應用層自己包的重試迴圈、以及使用者手動
「重新送出」都會踩到**，須寫入 D8 的升級指引。

### D11：明文路徑（Plain）不在本計畫範圍——盤點與另案交接

**明文防不了重放，這是密碼學事實而非實作限制。** 防重放需要一個攻擊者無法偽造的綁定，
綁定需要秘密。明文沒有秘密，攻擊者把 timestamp 改成現在、sequence 改成更大的值，那就是
一個全新的合法請求，不是重放。任何放在明文層的防重放欄位都是自欺。

出路本應是「Plain 只給重放無害的方法」——但**這個前提現在不成立**。`ApiProtectionLevel.Public`
的語意是**最低要求**而不是實際格式，`ApiAccessValidator` 對 Public 方法接受任何格式，包含
Plain。用戶端確實預設 `Encrypted`，但那是用戶端的自律，伺服器不強制。於是持有效 token 的
攻擊者只要改用 Plain 呼叫 `Save`，伺服器的 Plain 分支直接 return、根本不期待 frame，
D8 的 fail-closed 整套歸零。

**這是既有設計問題被 anti-replay 照出來，不是 anti-replay 引入的**——`Save` 目前就允許在
無加密的情況下傳送業務資料。

**盤點結果**

| 類別 | 方法 | 能否提升為 `Encrypted` |
|------|------|------|
| 有副作用、可加密 | `Save`、`Delete`、`ExecFunc`、`EnterCompany`、`LeaveCompany` | 技術上可以 |
| 唯讀 | `GetList`、`GetLookup`、`GetNewData`、`GetData`、`GetDefine` 系列 | 不需要（重放無害） |
| 匿名、無法加密 | `Login`、`ExecFuncAnonymous`、`Ping`、`GetCommonConfiguration`、`CreateSession` | 不可能 |

**處置（2026-08-31 定案）：不在本計畫範圍，另案討論。**

提升 `ApiProtectionLevel` 會要求呼叫端實作 MessagePack + 壓縮 + AES-CBC-HMAC 並完成
RSA 金鑰交換，**JS 呼叫端做不到這一整套**，貿然提升會直接把它們鎖死。是否提升、以及
JS 呼叫端該走哪條路，需要獨立評估，不應綁在本計畫的交付節奏上。

**繞道的實際代價比表面小。** 要利用它，行為者必須**知道封包內容**才能構造等效的 Plain
請求。而 anti-replay 的主要威脅——撿到加密封包原樣重送的攻擊者——看不懂內容，構造不出
等效的 Plain 請求，這條繞道對他無效。能走繞道的是已知內容且持有效 token 的人，而那種人
本來就能直接發任意請求，anti-replay 從來不是為了擋他。

因此留著這個洞損失的不是主要防護，而是「合法用戶自己重複提交」那一塊——那本來就該由
業務層冪等處理（見 D10）。**殘餘風險**：能讀到解密後 log 的內部人員可得知請求結構，
但仍需有效 token。此限制須寫入文件。

**第三類是本議題的終點。** 匿名方法在登入前金鑰尚未存在（`ApiEncryptionKey` 於 login 時
交換），沒有共享秘密就沒有 MAC，防不了重放——密碼學上無解，不是投入更多工程能改善的。
其中只有 `ExecFuncAnonymous` 有副作用，其重放防護只能推給應用層自行實作冪等，須在文件明講。

唯讀方法留 `Public` 的理由是重放無害；「明文傳輸業務資料」是**機密性**議題而非重放議題，
不在本計畫範圍內擴大處理。

**日後若重啟提升議題**，還有一個相容風險要一併處理：`ApiConnector` 在沒有傳輸金鑰時會把
`Encrypted` 自動降級為 `Encoded`（[ApiConnector.cs](../../src/Bee.Api.Client/Connectors/ApiConnector.cs)），
因此提升後任何未建立傳輸金鑰的部署，`Save` / `Delete` 會直接失敗。`IsLocalCall` 則不受影響
（`ValidateAccess` 對 local 呼叫直接 return，不檢查格式）。

## 階段拆解

### 階段 1：timestamp 時窗 ✅

單獨交付即有價值：擋掉所有陳年封包（log 外洩、離職員工的舊抓包）。

**實作結果（2026-09-01）**

- `ApiPayloadFrame` — frame 型別與 big-endian 編解碼
- `ApiPayloadConverter` — 在 `Encode` 之後、`Encrypt` 之前 prepend，解密後剝離
- `ApiServiceOptions.RequireWireFrame` / `WireFrameTimestampTolerance`（預設關閉 / 5 分鐘）
- `JsonRpcExecutor.ValidateFrameTimestamp` + `JsonRpcErrorCode.ReplayRejected`（-32005）
- `ReplayRejectedException`，於 `MapException` 映射
- 11 個測試（frame round-trip、big-endian 位移、長度不足、版本不符、三種格式行為、時窗兩側）

**與原計畫的兩處出入**

1. **frame 掛在 `ApiPayload.Frame`（`[JsonIgnore]`），不是 converter 的新多載。**
   原本要為 `TransformTo` / `RestoreFrom` 各加一個帶 frame 的多載，但 analyzer 的 `RS0027`
   擋下了——既有多載的 `encryptionKey` 帶預設值，而「有 optional 參數的 API 必須是參數最多的
   多載」。改用屬性後兩個方法簽章完全不動，`ApiConnector` 也不必改。
2. **`Bee.Api.Client` 沒有任何改動**，因為 client 與 server 走的是同一份 converter。
   原計畫預期要改 `TransformRequestPayload`。

### 階段 2：序號滑動視窗 ✅

**實作結果（2026-09-01）**

- `ReplayWindow` — `highest` + 64-bit bitmap，`MaxForwardJump` 定為 1,000,000
- `IReplayWindowStore` / `MemoryReplayWindowStore` — per-token 視窗，process-local
- `ApiReplayProtection`（`Bee.Definition.Security`）+ `ApiAccessControlAttribute.ReplayProtection`
- `ApiSessionContext.NextSequence()`（`Interlocked.Increment`）、`ApiConnector` 取號填入 frame
- `ApiAccessValidator.FindAccessControl` 公開，供 executor 讀取方法的宣告
- 22 個測試（視窗演算法 11、executor 層 3、階段 1 既有 8）

**實作期間定下的四件事**

1. **視窗存活期 = 2× timestamp 容許時窗，與 session 生命週期完全解耦。**
   用舊序號的重放，其 timestamp 必定也過期、已被階段 1 擋下，所以視窗只在時窗內有意義。
   不必掛 logout 清理，記憶體上界是「時窗內活躍的 session 數」。
2. **匿名呼叫（`Guid.Empty`）不做序號檢查。** 序號是 per session 的，匿名呼叫全共用一個
   token，若也檢查，不同用戶端會互相把對方的序號用掉而大量誤拒。這也呼應 D11——匿名路徑
   本來就防不了重放。
3. **標記範圍就是「遠端可達且有副作用」的完整集合**：`Save`、`Delete`、`ExecFunc`、
   `EnterCompany`、`LeaveCompany`。其餘寫入方法（`SaveDefine`、`SaveCustomizePluginSettings`、
   `SetDeploymentAdmin`）全為 `LocalOnly`，遠端呼叫不到；`Define.cs` 的 `Public` 方法全為唯讀。
4. **`IReplayWindowStore` 做成介面，是 D6 多節點退化的出路。** 預設 process-local；需要跨節點
   強一致的部署可換上共享實作，不必改框架。

**已知不涵蓋**：`Public` 等級方法若以 Plain 呼叫則無 frame、不受檢查（D11 的降級繞道）。
`ApiReplayProtection.UniqueSequence` 的 XML doc 已明載此限制。

### 階段 3：可觀測性與文件

1. `AnomalyKind` 新增 `Replay`（列舉加值為相容變更），比照 `Unauthorized` 的處理方式
2. ADR 記錄 D1（為何不放 `ApiPayload` 明文層）與 D2（Encoded 的效力邊界）
3. `docs/development-constraints.md` 補上防護邊界；README 據 D2 誠實描述
4. 升級指引寫明 D10 的行為變化（逾時重送會失敗），並指出需要安全重試的場景應自行實作冪等鍵
5. 文件寫明 D11 的限制：`Public` 等級方法可經格式降級繞過重放防護，`ExecFuncAnonymous` 的冪等由應用層自負

## 測試

- frame round-trip：三種 `PayloadFormat` 各自編解碼還原
- 重放拒絕：同一封包送第二次應被拒
- 亂序接受：視窗內亂序到達的序號全部接受
- 視窗邊界：超出 64 格的舊序號應被拒
- 時窗邊界：時鐘偏差恰好落在容忍值內外的兩側
- 相容性：既有測試在預設設定下應全綠（預設不改變現行行為）

## 已定案決策（2026-08-31）

| # | 問題 | 決定 |
|---|------|------|
| A | 只做階段 1，還是做到階段 2？ | 兩層都做——序號視窗零 DB 成本，只做 timestamp 會留下「時窗內無限重放」的縫，而合法惡意用戶端正好落在那個縫裡 |
| B | 序號檢查全域套用，還是 attribute 宣告？ | attribute 宣告。查詢類方法重放無害，全套上去只是徒增判斷 |
| C | timestamp 時窗預設長度 | ±300 秒。要容忍 NTP 沒設好的用戶端桌機，壓到 60 秒以下實務上會誤殺 |
| D | 拒絕時回傳專屬 error code，還是併入既有的未授權碼？ | 專屬碼，讓用戶端能區分「重試會成功」與「不該重試」 |
| E | `Encoded` 格式要不要也套用 | 要，但依 D2 不得宣稱其為防重放 |
| F | frame 開關的預設值與升級指引 | 預設關閉。開啟是斷裂變更（D8），需在 CHANGELOG 標明並給部署順序 |
| G | `Public` 等級允許明文呼叫 `Save` / `Delete`，繞過 anti-replay | **移出本計畫範圍，另案討論**（D11）——提升會鎖死 JS 呼叫端，需獨立評估。本計畫僅在文件記錄此限制 |

## 附帶觀察（不在本計畫範圍）

- **業務層守門仍應獨立存在**：狀態機檢查（已核准的單不能再核准）、樂觀鎖版本號。它同時
  擋掉使用者連按兩下送出這種非攻擊情境，不因為有了 anti-replay 就可以省。
- **`StaticApiEncryptionKeyProvider` 下所有 session 共用金鑰**。本計畫的序號綁定 accessToken，
  不受影響，但該 provider 的其他重放面向值得另案檢視。
