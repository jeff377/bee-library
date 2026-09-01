# ADR-042：API 重放防護 —— 加密封套內的 wire frame

## 狀態

**已採納（Accepted，2026-09-01）**

## 背景

一個合法的 JSON-RPC 封包被原樣重送時，伺服器過去會完整執行第二次。
[ADR-036](adr-036-wire-serialization-externalized.md) 建立的 payload 管線以
AES-CBC-HMAC 保證封包**改不了**，但不保證它**沒被送過第二次** —— 加密防的是機密性與竄改，
不是重複。

攻擊者不需要解得開封包，只要把整段 request 原樣再送一次即可。取得管道由高到低：
合法但惡意的用戶端（自己抓自己的封包重送，TLS 完全無效）、log 外洩（gateway 或 APM
記錄完整 request body）、企業 MITM proxy、以及無 TLS 的內網部署。

其中最容易被低估的是第一項 —— 攻擊者往往是**合法登入的使用者**，拿自己那張已核准單據的
封包重送數十次。傳輸層加密對這種情形毫無作用。

## 決策

### 一、frame 放進加密封套，不放 `ApiPayload` 的明文欄位

防重放需要一個攻擊者無法偽造的綁定，而綁定需要秘密。`ApiPayload` 的 `Format` 與 `TypeName`
是**明文信封**，只有 `Value` 被加密 —— 在其上新增 `Timestamp` / `Sequence` 屬性，兩個值會落在
明文層讓攻擊者任意改寫（改成當下時間、改成更大的序號），那就是一個全新的合法請求，
等於沒防。

正解是在 `Encode`（序列化 + 壓縮）之後、`Encrypt` 之前把 frame 前置到 bytes：

```
[ version(1) | timestamp(8, Unix ms) | sequence(8) ] ++ body
```

三者皆 big-endian，version 1 固定 17 bytes。實作見
[`ApiPayloadFrame`](../../src/Bee.Api.Core/JsonRpc/ApiPayloadFrame.cs) 與
[`ApiPayloadConverter`](../../src/Bee.Api.Core/JsonRpc/ApiPayloadConverter.cs)。
frame 掛在 `ApiPayload.Frame`（`[JsonIgnore]`）供呼叫端存取，但不隨信封序列化。

### 二、version 位元組的存在理由是 frame 無法自我描述長度

frame 沒有長度前綴，body 緊接其後也沒有分隔符 —— 讀取端必須在碰 body 之前就知道要吃掉
幾個 byte。日後 frame 若要加欄位，新舊長度不同而長度無從判斷，沒有 version 就只能再做一次
全體斷裂升級。

**在 version 1 這個位元組對安全沒有任何貢獻**：攻擊者可以偽造它，只是仍得過 HMAC。
它此刻唯一的作用是讓舊用戶端得到一句清楚的錯誤，而不是把 body 亂數解讀成 timestamp 後
報出離譜的時間偏差。

### 三、防護強度依 `PayloadFormat` 分級，且文件必須誠實

| Format | 有無 HMAC | 效力 |
|--------|----------|------|
| `Encrypted` | 有 | 完整 —— frame 改不動 |
| `Encoded` | 無 | 僅擋無腦原樣重送；會改封包的攻擊者可自行改 frame |
| `Plain` | 無 | 無防護，且不帶 frame |

`Encoded` 這一格是**限制而非缺陷**，但不得在任何對外描述中宣稱它防重放。

### 四、frame 的有無由部署設定決定，不由封包自述

兩端讀同一個開關 `ApiServiceOptions.RequireWireFrame`。**伺服器不「偵測」frame 在不在** ——
一旦允許「看起來沒有 frame 就當作沒有」，攻擊者只要把 frame 拿掉就能關閉防護，那正是要防的
降級攻擊。

代價是兩端設定不一致必然失敗，這是刻意的。開關預設關閉（行為與導入前完全相同），
啟用順序為：**兩端先升套件，再同時開啟兩端開關**。

### 五、序號用滑動視窗，不用 nonce 集合

nonce 集合需要無界儲存或每次資料庫往返。改用 per-session 單調遞增序號加 64-bit 位圖
（IPsec anti-replay window，RFC 6479 的做法）：每個 session 只存 `highest` 與位圖共 16 bytes，
判斷是幾個位元運算，**零資料庫往返**。實作見
[`ReplayWindow`](../../src/Bee.Api.Core/JsonRpc/ReplayWindow.cs)。

容忍亂序是必要條件而非額外好處：取號是原子的，但並行請求的送達順序不固定，
嚴格遞增會誤殺正常流量。

三項衍生決定：

- **視窗存活期 = 2× 時間戳容許時窗。** 用舊序號的重放其時間戳必定也過期、已被時窗檢查擋下，
  因此視窗只在該期間內有意義。這讓清理與 session 生命週期完全解耦，記憶體上界是
  「時窗內活躍的 session 數」。
- **前跳設上限（`MaxForwardJump`）。** 沒有上限的話，用戶端一次整數運算失誤送出接近
  `long.MaxValue` 的序號，該 session 之後所有正常請求都落在視窗外而卡死 —— token 有效、
  金鑰正確卻全部失敗，幾乎無法診斷。
- **匿名呼叫不檢查序號。** 序號是 per session 的，匿名呼叫共用同一個空 token，
  若也檢查，不同用戶端會互相把對方的序號用掉而大量誤拒。

### 六、逐方法宣告，不全域套用

`ApiAccessControlAttribute` 新增第三維度
[`ApiReplayProtection`](../../src/Bee.Definition/Security/ApiReplayProtection.cs)，
預設 `None`。查詢類方法重放無害，全面套用只是徒增每次呼叫的判斷。

目前宣告 `UniqueSequence` 的是 `Save`、`Delete`、`ExecFunc`、`EnterCompany`、`LeaveCompany`
—— 這是「遠端可達且有副作用」的完整集合。其餘寫入方法（`SaveDefine`、
`SaveCustomizePluginSettings`、`SetDeploymentAdmin`）皆為 `LocalOnly`，遠端呼叫不到。

新維度是**屬性而非建構子參數**：對已發佈的公開建構子加上選擇性參數是二進位破壞性變更。

### 七、多節點退化可接受，並留下出路

`IReplayWindowStore` 的預設實作是 process-local。多節點且無 token affinity 時每個節點各持
一份視窗，**重放次數上限等於節點數，而非無限** —— 比「時窗內無限重放」好一個量級。
需要跨節點強一致的部署可替換為共享實作，不必改框架。

## 明確不納入

- **冪等鍵。** 序號解的是「拒絕重放」，冪等鍵解的是「安全重試」，兩者不可互相取代。
  取號後若請求逾時，重送用同號會被視窗拒（即使伺服器其實已處理成功、只是回應遺失），
  用新號則業務層執行兩次 —— 兩條都不對，因為這不是序號能解的問題。
  **啟用序號檢查後，逾時重送會失敗而非重試成功**；需要安全重試的場景應自行實作冪等鍵。
  框架目前沒有自動重試機制，因此啟用不會打壞既有框架行為，但應用層自己包的重試迴圈、
  以及使用者手動「重新送出」都會踩到。
- **收斂 `ApiProtectionLevel.Public`。** `Save` / `Delete` / `ExecFunc` 目前允許以 `Plain`
  呼叫，該路徑不帶 frame、不受檢查，是一條降級繞道。提升保護等級會要求呼叫端實作
  MessagePack、壓縮、AES-CBC-HMAC 與 RSA 金鑰交換，JS 呼叫端做不到這一整套，
  貿然提升會直接把它們鎖死。此議題需獨立評估，另案處理。

  **繞道的實際代價小於表面**：要利用它，行為者必須知道封包內容才能構造等效的 `Plain` 請求，
  而主要威脅 —— 撿到加密封包原樣重送的攻擊者 —— 看不懂內容，構造不出來。
  能走繞道的是已知內容且持有效 token 的人，那種人本來就能直接發任意請求。
- **業務層守門不因此省略。** 狀態機檢查（已核准的單不能再核准）與樂觀鎖版本號仍應獨立存在，
  它們同時擋掉使用者連按兩下送出這類非攻擊情形。

## 後果 / 影響

- 開關預設關閉，導入本身**零行為變化**。
- 啟用後每個 Encoded / Encrypted 請求多 17 bytes；回應方向也帶 frame（兩端共用同一份
  converter 的自然結果），用戶端剝離後丟棄不檢查，該方向目前是純開銷。
- 重放拒絕回傳專屬錯誤碼 `JsonRpcErrorCode.ReplayRejected`（-32005），讓呼叫端能區分
  「重試不會成功」與「憑證無效」；並記為
  [`AnomalyKind.Replay`](../../src/Bee.Definition/Logging/AnomalyKind.cs) 而非泛用 `Error`
  —— 折進 `Error` 的話，「某 session 連續被拒」這個訊號就看不見了，而那正是判別用戶端
  時鐘偏移或有人重送封包的依據。
- 本機呼叫（`IsLocalCall`）不受影響。
