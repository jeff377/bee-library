# ADR-044：body codec 由每個請求宣告，JSON 與 MessagePack 並存

## 狀態

**已採納（Accepted，2026-09-03）**

## 背景

框架的 wire body 一直只有 MessagePack（[ADR-004](adr-004-messagepack-payload.md)、
[ADR-036](adr-036-wire-serialization-externalized.md)），這對 .NET 與行動端都是對的選擇。
但要讓瀏覽器裡的前端直接呼叫 JSON-RPC 後端、**且維持加密**，就得在那個語言重建 client 端的
整條 payload 管線。

盤點的結果與直覺相反。**加密層對瀏覽器極為友善**：RSA 交握是 2048-bit、SPKI 公鑰、
OAEP-SHA256，對應 Web Crypto 的 `RSA-OAEP`；對稱層是 AES-256-CBC + PKCS7 加 HMAC-SHA256，
對應 `AES-CBC` 與 `HMAC`；壓縮是 gzip，對應 `DecompressionStream`。全部是原生 API，
私鑰甚至不必匯出。

**真正的障礙是 body。** 依 [ADR-037](adr-037-wire-explicit-registration.md)，wire 型別一律
顯式註冊手寫 formatter；那些 formatter 是逐 key 對位的二進位契約，只有這個框架知道。
在另一個語言鏡像它們，等於為同一份契約建立第二個權威來源，而 `WireContractDriftTests`
只守得住 .NET 那一端——跨語言的那一半**沒有任何機制會發現兩邊漂掉**。

依 `single-source` 的判準，這是結構問題而不是紀律問題。

## 決策

### 一、body codec 由每個請求在信封宣告

`ApiPayload` 增設 `Codec`，隨 `format` 與 `type` 一起走在明文信封裡。伺服端依宣告解碼，
並**以同一個 codec 回應**——客戶端協商了一種 codec，就解不動別的。

**未宣告即 MessagePack**，這是相容性的必然而非預設值的挑選：所有既有客戶端都不宣告、
也都送 MessagePack。因此未宣告 codec 的請求信封**逐位元與先前相同**。

宣告放在信封而不是 HTTP header，是因為 in-process 的本地呼叫路徑根本沒有 header；
放信封才能讓兩種 transport 走同一套判讀。

### 二、codec 不是安全屬性，所以它可以被協商

這一條要與 [ADR-042](adr-042-api-replay-protection.md) 對照著讀。那裡的 wire frame
**刻意不可協商**——讓一個請求宣告「我不帶 frame」等於開放降級攻擊，所以是否要求 frame
由部署開關決定，兩端各自讀同一個開關，不匹配就失敗。

codec 沒有這個性質：它決定 body 怎麼**拼寫**，不決定它被保護得多好。加密仍然包在外層，
HMAC 仍然覆蓋同一段位元組。在傳輸途中改寫 codec 名稱，得到的是一個解不開的 body，
而不是一個保護較弱的 body。

**兩者的差別不在「要不要相信客戶端」，而在那個欄位有沒有承載安全語意。**

名稱本身仍然當作不可信輸入處理：先驗形狀（小寫英數與連字號、長度上限）再談其他，
不合格者以固定訊息拒絕，不把任意呼叫端文字帶進錯誤訊息或記錄它的地方。

### 三、否決「在 TypeScript 鏡像 MessagePack formatter」

這是最直覺的方案，而且**完全不必動框架**——正是它誘人的地方。

否決的理由只有一條，但足夠：它把「跨語言的一次性成本」換成「跨語言的長期漂移風險」。
`Bee.Api.Core` 這端新增一個 wire 成員時，`WireContractDriftTests` 會紅；
TypeScript 那份鏡像不會，也沒有任何東西會提醒任何人。漂掉的症狀是欄位靜默消失或錯位，
不是例外。

### 四、否決「以部署設定閘門控制是否接受 JSON codec」

本決策實作過程中一度加入 `AllowedSerializers` 設定，理由是「每個可接受的 codec 都是一個
匿名呼叫者搆得到的解析器」，因此應該預設關閉、由部署明確開啟。**該前提是錯的，已移除。**

System.Text.Json 早就在匿名路徑上：JSON-RPC 信封本身就是 JSON，而 Plain 格式的 body
更是由 [`ApiInputConverter`](../../src/Bee.Api.Core/Conversion/ApiInputConverter.cs) 直接
反序列化到目標型別，`System.Login` 又是允許匿名的。JSON body codec 沒有引入新的解析器
類別，那道閘門守的是一扇早就開著的門。

代價則是實在的：瀏覽器客戶端能不能用，取決於有沒有人記得去打開一個設定；忘了開的症狀是
呼叫被拒，而錯誤訊息不會指回那個設定。

**把這一段記在這裡，是因為「多一個 codec 等於多一個攻擊面」聽起來非常合理。**
它之所以不成立，只有實際追過信封與 Plain body 的解析路徑才看得出來。

### 五、移除 `ApiPayloadOptions.Serializer`

codec 改為逐請求宣告之後，這個設定只剩下一個作用：客戶端未宣告時伺服端用哪個解。
而那個答案**只能是 MessagePack**——既有客戶端都不宣告且都送 MessagePack，把它設成 `json`
會讓它們全部解碼失敗。它已經不是部署選擇，而是相容性常數，留著只是一個容易踩的腳。

對照之下 `Compressor` 與 `Encryptor` 留在設定檔是對的：那是**部署政策**（要不要保護、
用什麼保護），而 codec 是**客戶端能力**（這個客戶端產得出哪一種 body）。
這個區分正是「只有 serializer 需要協商」的理由。

這是**破壞性變更**（見「後果」）。既有的 `SystemSettings.xml` 留著該元素無害——
`XmlSerializer` 會忽略未知元素，舊設定檔不必同步修改即可升版。

### 六、JSON 需要自己的判別式封套，且沿用同一組判別碼

[ADR-037](adr-037-wire-explicit-registration.md) 為 MessagePack 的 `object` 型別成員
建立了判別式封套。JSON 需要同一件事，理由更迫切：JSON 連基本型別都分不出來——
`decimal` 與 `double` 都是 `1.0`，`Guid`、`DateTime` 與 `string` 都是帶引號的文字，
而 System.Text.Json 把它們一律讀回 `JsonElement`。**失真的症狀是錯值，不是錯誤。**

判別碼沿用 MessagePack 端既有的那一組，因此一個碼在兩條 wire 上意義相同，
`WireValueCodePinTests` 也同時釘住兩者。

三條 JSON 專屬的規則值得寫下來，因為它們都是「不這樣做也能跑，但會錯」：

- **`decimal`、`int64`、`uint64` 以 JSON 字串上線。** JSON number 對每個 JavaScript
  讀取端都是 double，既撐不住 decimal 的精度，也撐不住 2^53 以上的整數。
  這個 codec 的主要服務對象正是那些讀取端。
- **`object` 成員為 null 時整個屬性消失**，而不是寫成 `null`。讀取端必須把「屬性不存在」
  當作 null。
- **`DataTable` 的儲存格不走封套。** 型別由同一份文件裡的 column metadata 還原，
  因此 DataTable 的 JSON 形狀與 Plain payload 完全一致，讀取端只需要理解一種形狀。

### 七、跨語言以黃金樣本驗證，且只固定 body 原文

樣本置於 repo 的 `wire-fixtures/`，由 `WireFixtureTests` 產生並驗證：現行編碼與樣本不符
即測試失敗，訊息指示重新產生並逐筆讀過 diff——**那份 diff 就是 wire 的變更說明**。

樣本覆蓋的是**編碼規則**而非逐一列舉訊息型別。逐型別會得到大量幾乎同構的檔案，
卻漏掉真正會錯的地方：判別碼、DataTable 形狀、camelCase、列舉的字串化。
訊息型別本身是屬性袋，另一語言由型別定義產生即可。

**只固定 body 原文，不固定壓縮或加密後的位元組。** gzip 的輸出跨 .NET 版本不保證一致，
AES-CBC 每則訊息用隨機 IV，本質上不可能固定。那兩層是標準演算法、各語言的 library
自己保證；需要釘住的是只有這個框架知道的 JSON 形狀。

## 後果

- 瀏覽器客戶端可以只用原生 API 實作完整的加密管線，不必鏡像任何自訂二進位契約。
- 未宣告 codec 的請求信封逐位元不變，既有客戶端行為完全不受影響。
- **破壞性變更**：`ApiPayloadOptions.Serializer` 已移除，發版時的版號判定必須把它算進去。
- 公開 API 表面增加：`ApiPayload.Codec`、`ApiServiceOptions.ResolvePayloadSerializer`、
  `PayloadCodecNames`、`JsonPayloadSerializer`、`ApiConnector.PayloadCodec`。
- 自訂的 `IApiPayloadTransformer` 若要服務協商過的 codec，必須實作新的多載；
  預設實作**明確擲回而不是退回預設 codec**——靜默退回會用呼叫端沒要求的 codec 編碼，
  而對方會用它要求的那個解碼。未宣告 codec 的呼叫仍走既有多載，因此既有實作不受影響。
- **本決策揭露了一個既有缺陷並一併修正**：宣告型別為 `FilterNode` 的成員（清單查詢的
  篩選條件）在 JSON 上只會寫出判別碼，運算子與整棵子樹靜默消失。
  `FilterGroup.Nodes` 早有 collection converter，單一節點那一半沒有，而編碼過的 body
  一直只走 MessagePack，所以缺陷只在 Plain 路徑上壞著。修正見
  [`FilterNodeJsonConverter`](../../src/Bee.Definition/Filters/FilterNodeJsonConverter.cs)，
  **必須標註在屬性上而非型別上**——標在基底型別會被子類繼承而無限遞迴至堆疊耗盡。
- 時區責任不隨 codec 轉移。[ADR-032](adr-032-datetime-timezone.md) 把轉換點定在 Connector，
  伺服端既不轉換也不檢查；另一個語言的客戶端必須自行負責 UTC 正規化，
  **漏掉的症狀是日期靜默偏移而不是報錯**。
