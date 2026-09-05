# 計畫：TypeScript Connector（JS 前端支援加密傳輸）

**狀態：✅ 已完成（2026-09-03）**

> 設計決策與理由已升格為 [adr-044](../../adr/adr-044-payload-codec-negotiation.md)（長效紀錄）。
> 本文件保留執行過程與階段進度。

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | Server 端：新增 JSON body codec，payload envelope 支援 per-request codec 宣告 | ✅ 已完成（2026-09-02） |
| 2 | .NET client 端支援宣告 codec，補跨 codec 的 round-trip 與安全性測試 | ✅ 已完成（2026-09-02） |
| 3 | Wire fixture 產生器：由 .NET 產出黃金樣本，供跨語言驗證 | ✅ 已完成（2026-09-03） |
| 4 | TypeScript Connector 套件（加密層 + connector API + fixture 驗證） | ✅ 已完成（2026-09-03） |

## 背景

目前 connector 只有 .NET 實作（`Bee.Api.Client`）。要讓 JS / TS 前端直接呼叫 JSON-RPC 後端，
且**必須支援加密**（自訂 BO 若宣告 `ApiProtectionLevel.Encrypted`，不加密根本呼叫不到），
就得在 TS 端重建 client 這一側的 payload 管線。

盤點後結論與直覺相反：**加密層對瀏覽器極友善，真正的障礙是 MessagePack body。**

### 加密層：瀏覽器原生 API 全數支援

| 環節 | 框架實作 | 瀏覽器對應 |
|------|---------|-----------|
| RSA 交握 | 2048-bit、SPKI PEM 公鑰、OAEP-SHA256（[RsaCryptor.cs](../../../src/Bee.Base/Security/RsaCryptor.cs)） | `crypto.subtle.importKey('spki', …)` + `RSA-OAEP` / SHA-256 |
| 對稱加密 | AES-256-CBC + PKCS7（[AesCbcHmacCryptor.cs](../../../src/Bee.Base/Security/AesCbcHmacCryptor.cs)） | `AES-CBC`，PKCS7 由 Web Crypto 自動處理 |
| 完整性 | HMAC-SHA256，覆蓋 IV + 密文 | `HMAC` / SHA-256 |
| 位元組佈局 | `[int32 ivLen][iv][int32 cipherLen][cipher][hmac32]`，little-endian | `DataView` |
| 壓縮 | gzip | `DecompressionStream('gzip')` |

私鑰不必匯出 —— `generateKey` 產生 `CryptoKeyPair`，只 export SPKI 公鑰送 server。
整個加密層約 150 行 TS，零第三方套件。

### 障礙：body 是自訂契約的 MessagePack

`Encrypted` = AES(gzip(MessagePack(obj)))，而該 MessagePack 是
[src/Bee.Api.Core/MessagePack/](../../../src/Bee.Api.Core/MessagePack/) 下 30 餘支逐 key 對位的
**手寫 formatter**。在 TS 重建鏡像等於建立第二個權威來源，且**沒有任何機制會發現它漂掉**——
`WireContractDriftTests` 只擋得住 .NET 那一端。這是結構性風險，不是靠紀律能守住的：複寫的部分每次都會漂，而編譯器不看它、測試不跑它、CI 不驗它。

## 方案評估

| 方案 | 做法 | 結論 |
|------|------|------|
| A. Plain-only | TS 走 `format: 0` 純 JSON | ❌ 不符需求（無加密，且 `Encrypted` 方法呼叫不到） |
| B. TS 重建 MessagePack 鏡像 | TS 端鏡像 30+ 支手寫 formatter | ❌ 跨語言漂移無防護，長期維護成本最高 |
| **C. JSON body codec（本計畫）** | Server 增設 JSON codec，per-request 宣告；TS 只做 JSON + gzip + AES + RSA | ✅ 採用 |
| D. BFF 代理 | 前端打自家 BFF，BFF 內部用 .NET connector | 保留選項——見「什麼情況該改走 BFF」 |

方案 C 把「跨語言長期漂移風險」換成「一次性的框架改動」。`IApiPayloadSerializer` 這個接縫
本來就是為此存在的：[ApiPayloadOptionsFactory](../../../src/Bee.Api.Core/Transformers/ApiPayloadOptionsFactory.cs)
的 switch 目前只有 `messagepack` 一個 case。

### 什麼情況該改走 BFF（方案 D）

只有一個判準：**API key 能不能落在瀏覽器**。

JS 直連時 `X-Api-Key` 必然出現在前端，而它在框架裡是**應用身分**、不是連通性檢查——
`System.Login` 刻意保留它的要求，就是為了記錄「哪個應用嘗試登入」
（見 [ApiAuthorizationValidator.cs](../../../src/Bee.Api.Core/Authorization/ApiAuthorizationValidator.cs)
的 `s_noApiKeyMethods` 註解）。若該身分不可公開，本計畫救不了，得走 BFF。

另需明確：瀏覽器端的 payload 加密防的是**傳輸路徑上的中介**（反向代理、WAF、log），
**不防客戶端本身**——session key 在 JS 記憶體，XSS 得手即全破。

## 技術設計

### 管線：只換第一格

```
現在（.NET）  物件 → MessagePack → gzip → AES-CBC-HMAC → base64 → JSON envelope
本計畫（JS）  物件 → JSON       → gzip → AES-CBC-HMAC → base64 → JSON envelope
                     ↑ 只有這一格不同
```

`rules/security.md` 的 Serialize → Compress → Encrypt 順序不變，
`ApiPayloadFrame`（anti-replay）仍 prepend 在編碼後、加密前，與 codec 無關。

### codec 宣告放在 envelope，不是 header

`ApiPayload` 明文 envelope 增設 `codec` 欄位（預設 `messagepack`），
server 依 request 宣告解碼，並以**同一 codec** 回應——與現行 `format` 的處理方式一致
（[JsonRpcExecutor.cs](../../../src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs) 用同一個 `format` 組回應）。

不放 HTTP header 的理由：local provider 沒有 header，放 envelope 兩種 transport 一致。

**這不構成降級攻擊面**，與 `RequireWireFrame` 刻意不可協商的情況不同：codec 不是安全屬性，
加密仍在外層，改寫它只會造成解碼失敗。

### JSON codec 必須解決的三件事

1. **`object` 型別成員的判別式封套（核心風險）**
   MessagePack 端以 `WireValueCode` 判別碼保住型別
   （[WireValueFormatter.cs](../../../src/Bee.Api.Core/MessagePack/WireValueFormatter.cs)），
   JSON 沒有等價機制：`decimal 1.0` 與 `double 1.0` 在 JSON 都是 `1.0`，
   `Guid` / `DateTime` / `String` 都是字串。
   - **DataSet / DataTable 不受影響**——cell 值可由 column metadata 還原，
     [DataTableJsonConverter](../../../src/Bee.Base/Serialization/DataTableJsonConverter.cs) 已在做。
   - **無 metadata 的 object 成員**（`Parameter.Value`、`FilterCondition.Value`）**必須**
     加判別式封套，沿用 `WireValueCode` 的既有編號（那些編號已是 wire 格式的一部分，不得重編）。
2. **型別白名單**
   `WireTypeWhitelist` 對 `TypeName` 的既有檢查照舊。judgement point 在上一項的封套：
   凡從 wire 讀到型別判別碼就必須過白名單，等同
   [SafeMessagePackSerializerOptions](../../../src/Bee.Api.Core/MessagePack/SafeMessagePackSerializerOptions.cs)
   在 MessagePack 那端做的事。
3. **`MaxDepth` 顯式設定**
   不依賴 System.Text.Json 預設值，比照 MessagePack 端對不可信輸入的處理。

### `type` 欄位仍然必要

`ApiPayloadConverter.RestoreFrom` 靠 `TypeName` 決定反序列化目標並過白名單，與 body 編碼無關。
TS 端仍須維護 assembly-qualified name 字串表（`"Bee.Api.Core.Messages.Form.GetListRequest, Bee.Api.Core"`）。
差別在於：**這張表可以從 C# 型別自動產生**，MessagePack formatter 鏡像不行。

### 時區

`PayloadZoneConverter` 的 user zone ↔ UTC 轉換只在 .NET connector 做，server 端不做也不驗
（`DateTimeWireGuard` 只在 client 跑）。TS 端必須自行負責 UTC 正規化。

JSON body 對此比 MessagePack 更敏感：MessagePack 寫入時轉 UTC，JSON 則寫出 offset 由讀方
在自己的時區重新套用（可能落到別天）——見
[DateTimeWireGuard.cs](../../../src/Bee.Api.Core/JsonRpc/DateTimeWireGuard.cs) 的 remarks。
**漏了會靜默偏移，不會報錯。**

## 階段細節

### 階段 1：Server 端 JSON body codec

- 新增 `JsonPayloadSerializer`（`IApiPayloadSerializer`，`SerializationMethod => "json"`），
  含上述判別式封套與 `MaxDepth`。
- `ApiPayloadOptionsFactory.CreateSerializer` 增設 `"json"` case。
- `ApiPayload` 增設 `codec` 欄位；`ApiPayloadJsonConverter` 讀寫該欄位。
- `ApiPayloadConverter.TransformTo` / `RestoreFrom`、`ApiPayloadTransformer.Encode` / `Decode`
  改為接受 codec 參數（預設沿用 `ApiServiceOptions.PayloadSerializer`）。
- `JsonRpcExecutor` 以 request 宣告的 codec 組回應。
- **相容性**：舊 client 不送 `codec` → 預設 `messagepack` → 現有行為零變化。
- `PublicAPI.Unshipped.txt` 申報新公開成員，並判定二進位相容性。

#### 實作結果（2026-09-02）

全 solution build 0 warning / 0 error，`./test.sh` 全數通過。

- `src/Bee.Api.Core/Wire/WireValueCode.cs` —— 判別碼從 `WireValueFormatter.cs` 搬出。
  它現在是兩種 codec 共用的 wire 常數，留在 `MessagePack` 命名空間下已不符語意。
  既有的 `WireValueCodePinTests` 因此同時釘住兩條 wire。
- `src/Bee.Api.Core/Json/WireValueJsonConverter.cs` —— `object` 成員的判別式封套。
- `src/Bee.Api.Core/Transformers/JsonPayloadSerializer.cs`、`PayloadCodecNames.cs`。
- `ApiServiceOptions.ResolvePayloadSerializer`、`ApiPayload.Codec`、transformer 的
  codec-aware 多載、`JsonRpcExecutor` 沿用 request codec。
- **`ApiPayloadOptions.Serializer` 移除**（破壞性變更，見下）。
- `tests/Bee.Api.Core.UnitTests/JsonPayloadCodecTests.cs` —— 12 個測試涵蓋來回、型別保真、
  未啟用 codec 被拒、格式不合的名稱不回顯、以及跨 codec 的交叉否定。

##### 實作時定案的四個設計決定

1. **codec 由 payload 帶，不是由參數傳。** 原本規劃在 `ApiPayloadConverter.TransformTo`
   加第四個參數，但 `RS0027` 擋下——帶預設值的多載必須是參數最多的那個，加多載會逼著動
   既有簽章。改成 `ApiPayload.Codec`（public setter）後，`TransformTo` 與 `RestoreFrom`
   都從 payload 讀，反而對稱：寫入端蓋章、讀取端認章，且公開 API 表面沒有變大。
2. **兩種 codec 恆可用，不設啟用閘門。** 一度加了 `AllowedSerializers` 設定，理由是
   「每個可接受的 codec 都是匿名呼叫者搆得到的解析器」——**那個前提是錯的**。
   System.Text.Json 早就在匿名路徑上：信封本身是 JSON，而 Plain 格式的 body 更是由
   `ApiInputConverter` 直接 `JsonSerializer.Deserialize` 到目標型別，`System.Login`
   則是 Anonymous。JSON body codec 沒有引入新的解析器類別，那道閘門守的是一扇早就
   開著的門，代價卻是「瀏覽器客戶端能不能用」取決於有沒有人記得去開設定。已移除。
3. **`IApiPayloadTransformer` 的新多載採 default interface method，預設 throw 而非退回預設 codec。**
   靜默退回會用「呼叫端沒要求的 codec」編碼，而呼叫端會用它要求的那個解碼——錯得無聲。
   未宣告 codec 的呼叫仍走既有的兩參數多載，所以既有自訂 transformer 完全不受影響。
4. **codec 名稱先驗形狀再回顯。** 名稱來自 wire，只接受小寫英數與連字號、長度 ≤ 32；
   不合格者以固定訊息拒絕，不把任意呼叫端文字帶進錯誤訊息或記錄它的地方。

##### ⚠️ 破壞性變更：移除 `ApiPayloadOptions.Serializer`

**發版時的版號判定必須把這條算進去**（`PublicAPI.Shipped.txt` 已移除對應兩筆）。

理由：codec 改為每個請求宣告後，這個設定只剩「客戶端沒宣告時 server 用哪個解」一個作用，
而那個答案只能是 MessagePack——所有既有客戶端都不宣告且送 MessagePack，設成 `json`
會讓它們全部解碼失敗。它已不是部署選擇，而是相容性常數，留著只是一個容易踩的腳。

對照之下 `Compressor` 與 `Encryptor` 留在設定檔是對的：那是**部署政策**（要不要保護、
怎麼保護），而 codec 是**客戶端能力**（這個客戶端產得出哪種 body）。這個區分正是
「只有 serializer 需要協商」的原因。

既有 `SystemSettings.xml` 留著 `<Serializer>` 元素無害——`XmlSerializer` 會忽略未知元素，
所以舊設定檔不必同步修改即可升版。

### 階段 2：.NET client 支援與測試

**已完成（2026-09-02）。** `ApiConnector.PayloadCodec`（預設空 = MessagePack），
在 `TransformRequestPayload` 蓋到 `request.Params.Codec` 上；Plain 呼叫沒有編碼後的 body，
因此不標記。測試在 `tests/Bee.Api.Client.UnitTests/ApiConnectorExecuteTests.cs`（4 則，
沿用既有的 `FakeJsonRpcProvider`）與 `tests/Bee.Api.Core.UnitTests/JsonPayloadCodecTests.cs`（12 則）。

放在 connector 而非 `ApiSessionContext` 或 `ApiClientInfo`：codec 是「這個 client 產得出
哪種 body」的能力宣告，與 connector 實例一對一；session 的語意是 per-user，而
`ApiClientInfo` 是 process-wide static，一個 host 服務多個 client 時就不適用。

原規劃內容：

- `ApiConnector` 可指定 codec（預設 messagepack，不改變既有呼叫端）。
- 測試（`tests/Bee.Api.Core.UnitTests/`）：
  - 兩種 codec 各自的 Encoded / Encrypted round-trip。
  - **交叉否定測試**：以 A codec 編碼、宣告 B codec 解碼必須失敗，不得靜默產生預設值。
  - `object` 成員判別式封套的型別保真（`decimal` 不得退化為 `double`、`Guid` 不得退化為 `string`）。
  - 白名單：偽造型別判別碼必須被拒。
  - anti-replay frame 在 JSON codec 下仍生效。

### 階段 3：Wire fixture 產生器

**已完成（2026-09-03）。** 樣本在 `wire-fixtures/bodies/`（26 個），產生與驗證在
`tests/Bee.Api.Core.UnitTests/WireFixtureTests.cs`，用法與 wire 規則寫在
`wire-fixtures/README.md`（英文，樣本是給另一個語言的 client 讀的）。

##### 兩個與原規劃不同的決定

1. **覆蓋「每條編碼規則」，不是「每個 wire 型別」。** 逐型別產樣本會得到上百個幾乎同構的
   檔案，卻漏掉真正會錯的地方：判別碼、DataTable 形狀、camelCase、列舉字串化。訊息型別
   本身是屬性袋，TS 端由型別定義產生即可。目前涵蓋 22 個 `WireValueCode`、DataTable
   （含 rowState 與 Modified 的 original/current）、DataSet、以及兩個代表性訊息型別。
2. **樣本只固定 body 原文，不固定壓縮／加密後的 bytes。** gzip 輸出跨 .NET 版本不保證一致，
   AES-CBC 每次用隨機 IV，本質不可固定。那兩層是標準演算法、各語言 library 自己保證；
   需要釘住的是只有這個框架知道的 JSON 形狀。

##### ⚠️ 樣本上線當天就抓到一個既有缺陷

`GetListRequest.Filter` 的宣告型別是 `FilterNode`（抽象基底），而 System.Text.Json 綁宣告型別，
於是指派給它的 `FilterGroup` 只會寫出 `{"kind":"Group"}`——**運算子與整棵子樹靜默消失，
不擲例外**。這不是本計畫引入的：`FilterGroup.Nodes` 早有 `FilterNodeCollectionJsonConverter`，
但單一節點的那一半沒有，而編碼過的 body 一直只走 MessagePack（那端有自己的 formatter），
所以只在 Plain 路徑上壞著、沒人踩到。JSON body codec 一上線就會在最常用的清單查詢上生效。

修法是 `FilterNodeJsonConverter`（`src/Bee.Definition/Filters/`），**標在屬性上而非型別上**。
標在 `FilterNode` 型別上會被子類繼承，寫 `FilterGroup` 時再度進入同一個 converter，
無限遞迴到 stack 爆掉——那是 segfault，不是可捕捉的例外，實作時已實際踩過一次。

守護有兩層：`FilterNodeJsonConverterTests`（行為）與 `FilterNodeConverterCoverageTests`
（反射掃描，新增可寫入的 `FilterNode` 屬性卻忘了標註就會紅——「逐屬性標註」正是會漏的那種規則）。

原規劃內容：

跨語言 wire **唯一擋得住漂移的機制**是雙向 round-trip，因此由 .NET 產出黃金樣本：

- 對每個 wire 型別產出 `{型別名, 明文 JSON, 編碼後 base64}` 的 fixture 檔。
- .NET 端測試驗證 fixture 與現行實作一致（fixture 過期即紅）。
- TS 端以同一份 fixture 驗證：**TS 讀 .NET 寫的**、**TS 寫的 .NET 讀得回**。
- fixture 隨套件版本發布，TS repo 取用對應版本。

### 階段 4：TypeScript Connector 套件

**已完成（2026-09-03）。** 套件在 [bee-connector-js](https://github.com/jeff377/bee-connector-js)，
每一層都以跨語言證據驗證，而非同語言的 round-trip：

| 層 | 驗證方式 |
|----|---------|
| AES-CBC-HMAC | 解開 .NET `AesCbcHmacCryptor` 實際產生的密文 |
| gzip | 解開 .NET `GzipPayloadCompressor` 的輸出 |
| RSA 交握 | 單一向量雙向驗證（金鑰對由 Web Crypto 產、密文由 .NET 產） |
| JSON body codec | 本 repo 發布的 26 個 wire 樣本，逐一 round-trip |
| API 合約型別 | CI 每次建置比對，漂了就紅 |
| 整條路徑 | 對 `samples/QuickStart.Server` 的端到端 smoke |

##### 三個與原規劃不同的決定

1. **合約型別由產生器輸出，不手寫。** 原規劃只說「由 `Messages/**` 產生 `.d.ts`」，
   實作時擴為 `wire-contracts/`（`messages.d.ts` + `type-names.ts`），由
   `WireContractGeneratorTests` 釘住。型別名對映特別重要：編碼過的 payload 必須在信封
   指名 assembly-qualified type，手抄的話搬一次命名空間就指向解析不到的型別，而症狀是
   執行期被拒、不是編譯錯誤。
2. **樣本不入版控，合約入版控。** 兩者角色不同：樣本只餵測試，合約是**建置的輸入**——
   一個離線編不起來的套件，比一份受 CI 檢查的衍生檔更糟。
3. **未做 anti-replay frame。** `ApiServiceOptions.RequireWireFrame` 預設關閉，且**客戶端
   無從查詢伺服端是否開啟**（它不在 `CommonConfiguration` 裡）。TS 端要支援它，得先補上
   能力宣告——見「後續」。

##### 實作中踩到、值得記住的三個「不報錯的錯」

- **`FilterNode` 多型靜默丟失整棵篩選子樹**（既有缺陷，本計畫揭露並修正，詳見階段 3）。
- **`WireValue` 撞名被靜默丟棄。** 合約的 `WireValue`（wire 上的信封）與 codec 的
  `WireValue`（信封拆開後的值）同名，而 TypeScript 對 `export *` 的名稱衝突不報錯、
  是把該名稱排除。源頭改名為 `WireValueEnvelope`，消費端再以 namespace 匯出。
- **GitHub raw CDN 讓防漂閘門報出不成立的「一致」。** push 後一段時間內 raw 會回舊版，
  於是 `--check` 拿舊合約比對而通過。改走 contents API 直接取 blob。

另有一個誤導性很強的 CI 失敗：matrix 兩個 job 從同一 IP 各抓一次全套樣本，撞爆未認證
GitHub API 的 60 次/小時上限，症狀卻是「某個隨機樣本檔 403」且只有一個 Node 版本紅——
看起來像下載不穩或 Node 版本差異。解法是注入 `GITHUB_TOKEN`。

##### 後續（不屬於本計畫）

- **wire 能力宣告**：`RequireWireFrame` 應可被客戶端查詢，否則不隨框架發版的前端無從得知
  伺服端是否要求 frame，而它一開啟就會拒絕所有舊客戶端。
- **收斂到 tag**：合約與樣本目前釘 `main`，待本 repo 發版後改為對應 tag。
- **npm 佔名**：`bee-connector` 尚未發布，unscoped 名稱先搶先贏。

原規劃內容：


**落腳與命名（已決，2026-09-03）**

| 項目 | 值 |
|------|-----|
| repo | `bee-connector-js`（另立 repo，見「待決事項」1） |
| npm 套件 | `bee-connector`（unscoped） |
| demo | 放 `bee-connector-js`，後端沿用 bee-library 的 `samples/QuickStart.Server` |

- `connector` 沿用框架既有的概念名（`ApiConnector` / `FormApiConnector` /
  `SystemApiConnector`），對既有使用者零學習成本。
- repo 加 `-js` 後綴而套件不加：repo 名要能與 `bee-library`（.NET）並排時一眼分出生態，
  套件名活在 npm 裡、上下文已隱含。用 `-js` 而非 `-ts` 是因為發佈的是 JS + `.d.ts`，
  後綴描述消費端比描述實作語言有用。
- **npm 不是 NuGet**：兩個生態各自獨立的 registry、帳號與發佈流程，該 repo 會有自己的
  一套 CI 與發佈設定，與 bee-library 完全不共用。
- 名稱查核（2026-09-03）：`bee-connector` 於 npm 未被佔用；`bee-client` 已被佔用。
  unscoped 名稱先搶先贏，**要用就趁早發一個 `0.0.1` 佔名**（該動作由維護者自行執行）。

**demo 不放 bee-library**：`samples/Web.Js.Demo` 已經存在，定位是「零建置、零 npm 的
純 JS」——那個定位本身有價值（示範最低門檻），加進 npm 套件與 build step 會毀掉它。
TS 套件的 demo 是該套件的門面，跟著套件走；後端則指示開發者起 `samples/QuickStart.Server`，
bee-library 因此維持純 dotnet、TS repo 維持純 npm。

**與既有 JS 支援的關係**：本計畫**不取代** [adr-014](../../adr/adr-014-jsonrpc-plain-public-default.md)
的 Plain 路徑，它仍是 JS 前端的預設。兩條路徑的分工與 ADR-014 三條拒絕理由的逐條對照，
見 [adr-044](../../adr/adr-044-payload-codec-negotiation.md)。

**版本對應**：TS 套件有自己的發佈節奏，不硬跟 bee-library 的版號走，但必須說得出它對應
哪一版的 wire。最輕的做法是在套件內記一個相容的 fixture 版本，CI 以該 tag 的
`wire-fixtures/` 驗證——wire 一變，TS 端 CI 就紅，而不是等使用者踩到。



- 加密層：RSA-OAEP-SHA256 交握、AES-CBC-HMAC（含 little-endian 佈局）、gzip。
- Connector API：對齊 .NET 的 `SystemApiConnector` / `FormApiConnector` 形狀。
- 型別定義由 `Bee.Api.Core/Messages/**` 產生 `.d.ts`，避免手抄漂掉。
- DataSet / DataTable 的 TS 型別（`{dataSetName, tables[], relations[]}` + rowState）。
- **FormSchema / FormLayout 是 XML 字串夾在 JSON 裡**，要 metadata-driven UI 就得在 TS parse XML。
- 以階段 3 的 fixture 做 CI 驗證。

## 待決事項

1. ~~TS 套件落腳處~~ →  **已決（2026-09-02）：另立 repo，命名見階段 4。**
   框架改動留在 bee-library，fixture 由 bee-library 產生並隨版本發布，TS repo 取用對應版本。
   bee-library 的 CI 維持純 dotnet，不引入 npm。
2. 是否同時提供 `Encoded`（不加密）給 JS——同一條路少一步，建議提供。

## 順帶處理

`System.GetApiPayloadOptions` 出現在 `ApiAuthorizationValidator` 的 `s_noAuthMethods` 清單與
`s_noApiKeyMethods` 的 XML doc 說明中（"discloses payload and encryption negotiation settings"），
但**全 repo 無任何實作**——目前 client 是靠 `System.GetCommonConfiguration` 跟隨 server 設定，
不是協商。本計畫剛好可以復活這個名字來承載 codec 協商；若不採用則應清掉該死條目。

## 風險

| 風險 | 影響 | 因應 |
|------|------|------|
| `object` 成員型別退化 | 金額變 double、Guid 變字串，靜默錯誤 | 判別式封套 + 階段 2 的型別保真測試 |
| TS 端漏做 UTC 正規化 | 全站日期靜默偏移 | fixture 納入跨時區案例；TS 端集中在單一 boundary 處理 |
| codec 分歧造成兩套 wire 行為 | 兩條路徑行為不一致 | 階段 2 的交叉否定測試 + 共用同一組 fixture |
| API key 落在瀏覽器 | 應用身分外洩 | 由使用者決定是否改走 BFF（方案 D） |

## 驗證

- 階段 1、2 完成後跑完整模式 CI（`[all-db]`）——雖不觸及 SQL 產生邏輯，但動到 wire 契約。
- 階段 4 的 TS 套件以 fixture 做 CI 驗證，不依賴跑起來的 server。
