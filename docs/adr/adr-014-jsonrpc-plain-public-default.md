# ADR-014：JSON-RPC `Plain` 開放策略 — `Public` 為預設保護等級，HTTPS 為信任界線

## 狀態

已採納（2026-05-26）

## 背景

Bee.NET 在 v4.5 階段已為三類前端 host 完成連線抽象（[ADR-013](adr-013-frontend-api-connection-strategy.md)）：桌面端走 `Bee.UI.Core` static singleton、Blazor Server / WASM 走 `Bee.Web.*` 與 DI。三者共用 `Bee.Api.Client` 通訊層，但都假設客戶端是 .NET runtime — RSA key exchange + AES-CBC-HMAC + MessagePack 序列化 + gzip 壓縮的完整 payload pipeline。

v4.6 階段出現新的前端類型：**純 JavaScript（React / Vue / Angular / vanilla）**，沒有 .NET runtime 可承載加密管線。技術上 server 端 `PayloadFormat.Plain` 已支援 `fetch + JSON` 直送（`System.Text.Json` 已實作 `DataSet` / `DataTable` 序列化），但 BO 方法的 `ProtectionLevel` 預設值阻擋了這條路徑：

| 方法 | 原 `ProtectionLevel` | JS 影響 |
|------|--------------------|--------|
| `FormBO.GetNewData` / `GetData` / `Save` / `Delete` | `Encrypted, Authenticated` | JS 無法執行任何 CRUD |
| `SystemBO.EnterCompany` / `LeaveCompany` / `Logout` | `Encrypted, Authenticated` | JS 無法完整走 session lifecycle |
| `SystemBO.Login` / `Ping` / `GetCommonConfiguration` | `Public, Anonymous` | JS 可呼叫 |
| `<ProgId>.GetList` | `Public, Authenticated` | JS 可呼叫 |
| `System.GetDefine` / `SaveDefine` | `Public, Authenticated` | JS 可呼叫（敏感範圍由 `IsLocalCall` 守住） |

`Encrypted` 等級**強制要求加密 payload**：client 必須完成 RSA key exchange 拿到 `ApiEncryptionKey`、用 AES-CBC-HMAC 加密 request body。JS 前端若要走這條路徑，等於要在瀏覽器內實作 RSA + AES-CBC-HMAC + gzip + MessagePack — **此時改用 Blazor WASM 更實際**，整條技術路徑也失去開放給 JS 的意義。

### 反向問題：把所有 BO 方法預設為 `Encrypted` 對嗎？

回頭檢視 `Encrypted` 預設的歷史脈絡：早期 Bee.NET 部署在內網或 HTTP-only 環境，需要應用層加密守住 payload。但 v4.x 階段：

- production 部署普遍走 HTTPS（TLS 1.2+）— 傳輸層加密已是行業基線
- `AccessToken` GUID + `X-Api-Key` 雙重認證已足以阻絕未授權呼叫
- 應用層加密的真正價值是「**TLS 終止後的中介存取**」（log aggregator、APM proxy、CDN edge），這對**特定**高敏感方法（密碼修改、加密金鑰交換）有意義，**不需要**作為全方法預設

換句話說：把 `Encrypted` 當預設是**對所有方法套上「最高安全等級」的逆向預防**，但實際上多數 BO 方法的 payload（DataSet 內容、companyId 切換等）落入 TLS 加密保護圈內就足夠。

## 決策

**`Public` 為 BO 方法預設保護等級，HTTPS 為信任界線；只有開發者明確判斷需要應用層加密的特定方法才標 `Encrypted`。**

### 三項核心要點

1. **降級 7 個 BO 方法為 `Public + Authenticated`**

   | 方法 | `ProtectionLevel` 變更 |
   |------|----------------------|
   | `FormBO.GetNewData` / `GetData` / `Save` / `Delete` | `Encrypted` → `Public` |
   | `SystemBO.EnterCompany` / `LeaveCompany` / `Logout` | `Encrypted` → `Public` |

   - `AccessRequirement = Authenticated` 維持不變：身分門檻不放寬
   - Application-layer 業務權限檢查（who can edit which DataSet / who can enter which company）不在 `ProtectionLevel` 範圍，由 BO 層自行守住

2. **`ApiAccessValidator` 容許「高等級格式呼叫低等級方法」（向下相容）**

   既有 `.NET` client 走 `Encrypted` 格式呼叫降級後的方法：仍然允許。`Encrypted ≥ Public` 是合法的「**過度加密**」，不強制呼叫端配合降級。這保證：
   - 既有 desktop / Blazor 客戶端**不需要任何改動**就能繼續呼叫
   - 升級至 v4.6 不會破壞現有 deployment

3. **`Encrypted` 仍是合法選項，但需要明確標註**

   未來 BO 方法的設計者必須**主動評估**哪些方法需要 `Encrypted`（如密碼修改、加密金鑰生成），不再 by-default 全標。`ProtectionLevel` 不設定時 server 取既定預設（`Public`）。這把「需要應用層加密」從**全域預設**降為**個案決策**。

## 理由

### 為何信任 HTTPS 作為傳輸層基線

`Encrypted` 是 ProtectionLevel 中最強等級，承擔的是「**TLS 被中間人破解**」這個威脅模型。實務上：

- production HTTPS 配置（TLS 1.2+、HSTS、HPKP / Certificate Transparency）已是基本部署要求
- 若 TLS 被破解，加密 payload 的 AES-CBC-HMAC 金鑰也是透過 RSA key exchange 走同一條 TLS 通道交換，**同樣會洩漏**
- 真正能擋住 TLS-after-decryption 的中介存取（如 cloud APM proxy 抓 unencrypted body）才是 `Encrypted` 的價值場景，這對所有方法都套用是過度設計

### 為何不為 JS 前端做「JS 版加密管線」

評估過讓 JS 自帶 AES-CBC-HMAC + RSA 實作（Web Crypto API 已支援），但拒絕原因：

- JS 端與 .NET 端的加密管線實作要逐 byte 對齊（IV 隨機性、HMAC 對齊位元、gzip 包裝），測試成本高
- 即使做到，**降級 ProtectionLevel 的整個動機就是讓 JS 不用碰加密管線**；若 JS 仍要實作 RSA + AES-CBC-HMAC，等於白做這個 ADR — 不如改用 Blazor WASM（已有完整實作）
- JS 版加密管線會在多個前端框架（React / Vue / Angular）各自再實作一份，維護成本指數成長

JS 走 `Plain` + HTTPS + Bearer Token 的雙重保護線，與 ADR-013 Family B（`Bee.Web.*` 走 `RemoteApiProvider`）策略一致。

### 為何降級不影響既有部署

`ApiAccessValidator` 的判斷邏輯是 **「實際 payload 格式」≥「方法宣告等級」**：

| 實際格式 | 方法宣告等級 | 結果 |
|---------|------------|------|
| `Encrypted` | `Public`（降級後） | ✅ 允許（過度加密合法）|
| `Encoded` | `Public`（降級後） | ✅ 允許 |
| `Plain` | `Public`（降級後） | ✅ 允許 |
| `Plain` | `Encrypted`（未降級） | ❌ 拒絕 |

降級後 `Encrypted` 客戶端的請求仍然落在 ✅ 行，不會被 reject。

### 為何 `Login` 早已是 `Public + Anonymous`

歷史上 `Login` 是唯一在「**還沒有 AccessToken**」階段就需呼叫的方法，必須允許 anonymous 呼叫。它也是 RSA key exchange 的入口（`ClientPublicKey` 在此交換），因此自身不能要求 `Encrypted`（否則雞生蛋蛋生雞）。這個既存設計**意外地**為 JS 前端的整體開放鋪好了路：JS 走 Plain 呼叫 `Login`（`ClientPublicKey` 傳空字串），server 端短路加密協商，回傳 `AccessToken`，後續呼叫帶 `Authorization: Bearer <token>`。本 ADR 把這套既有單點機制**擴展為通用模式**。

## 替代方案（已評估後不採納）

1. **保留 `Encrypted` 預設，JS 前端用 WebCrypto 實作加密管線**
   - 拒絕原因：見〈為何不為 JS 前端做「JS 版加密管線」〉

2. **新增 `JsAuthorized` 等級（JS-only 認證模式）**
   - 拒絕原因：等於 `Public + Authenticated`，多一層命名沒有實質區別；且暗示「JS 客戶端與 .NET 客戶端走不同認證路徑」會誤導讀者

3. **預設保留 `Encrypted`，僅本次 7 個方法降級**
   - 部分採納：本次確實只降級 7 個方法。但「**未來新加方法的預設值**」是另一個獨立決策。本 ADR 明確規定**新方法預設 `Public`**，避免每加新方法就要走一次降級流程

4. **`ProtectionLevel = Encrypted` 退場（全 enum 移除）**
   - 拒絕原因：應用層加密在特定場景仍有價值（密碼修改、金鑰生成等高敏感方法），不應全面移除

5. **以 `[JsAccessible]` Attribute 表達 JS 可呼叫，不動 `ProtectionLevel`**
   - 拒絕原因：本質仍是「該方法的 payload 是否要求加密」，再加一個 attribute 表達同一個語意是冗餘；且兩個獨立 attribute 容易長期間不同步

## 後果

### JS 前端可呼叫的完整 API 表面

降級後，**Authenticated（需 AccessToken）** 區內 JS 前端透過 `PayloadFormat.Plain` 可呼叫：

| 方法 | `ProtectionLevel` | 用途 |
|------|------------------|------|
| `System.EnterCompany`* | Public | 進入公司 |
| `System.LeaveCompany`* | Public | 離開公司 |
| `System.Logout`* | Public | 登出 |
| `System.GetDefine` | Public | 取得 FormSchema / TableSchema 等定義 |
| `System.SaveDefine` | Public | 寫入定義（`IsLocalCall` 守住） |
| `System.GetFormSchema`† | Public | JSON-friendly 取得 FormSchema |
| `System.GetFormLayout`† | Public | JSON-friendly 取得 FormLayout |
| `<ProgId>.GetList` | Public | 列表查詢 |
| `<ProgId>.GetNewData`* | Public | 取得空白 DataSet |
| `<ProgId>.GetData`* | Public | 取得單筆資料 |
| `<ProgId>.Save`* | Public | CRUD 儲存 |
| `<ProgId>.Delete`* | Public | 刪除 |

`*` 為本 ADR 降級而開放、`†` 為配套新增的 JSON-native getter。

**Anonymous（不需 AccessToken）** 區：`System.Ping` / `GetCommonConfiguration` / `Login` / `CreateSession` 既有皆為 Public，不在本 ADR 範圍。

### 不變更的方法

`CheckPackageUpdate` / `GetPackage` 維持 `Encoded`，因為這兩個是 `.NET` runtime 端的套件更新機制，JS 前端沒有對應需求。

### 安全模型

| 攻擊向量 | 防護線 |
|---------|-------|
| 未認證呼叫 | `AccessRequirement.Authenticated` 守 `AccessToken` 有效性 |
| Token 竊取（網路嗅探） | HTTPS / TLS 1.2+ |
| Token 竊取（client-side） | client 自負（瀏覽器 localStorage 對 XSS、桌面端 process 內對 memory dump）|
| 跨來源呼叫 | CORS 設定（QuickStart.Server 已加，production 須限制 origin） |
| Payload 中介存取（TLS 終止後） | 特定高敏感方法仍標 `Encrypted`（不在本 ADR 降級清單） |
| 應用層權限失效 | BO 層業務檢查（如 `EnterCompany` 的公司權限驗證、Repository 的資料範圍過濾） |

### 對開發者的義務

- **新增 BO 方法時預設 `Public + Authenticated`**，不再 by-default 標 `Encrypted`
- 若該方法 payload 屬於「**TLS 終止後不能洩漏**」的敏感資料（password、金鑰、PII 等高敏感），**主動標 `Encrypted`** 並在 PR 內說明理由
- 部署 production host 時 **HTTPS 強制**為前置條件（HTTP-only 部署不再被視為合法配置）

## 相關連結

- [ADR-013：前端 API 連線策略](adr-013-frontend-api-connection-strategy.md) — Family B（`Bee.Web.*`）的 HTTPS + Bearer Token 安全模型與本 ADR 一致
- [計畫：JSON-RPC 前端整合 — 開放 Plain 格式供 JS 框架呼叫](../archive/plan-jsonrpc-frontend-integration.md) — 階段 1 降級實作與階段 2 整合指引（已封存）
- [計畫：JSON-RPC 加 FormSchema / FormLayout 取得方法](../archive/plan-jsonrpc-formschema-formlayout.md) — 配套新增的 JSON-native getter（已封存）
- [JSON-RPC 前端整合指引](../jsonrpc-frontend-integration.md) — 對外公開的 JS / TS 開發者文件
- [Bee.Api.Core README](../../src/Bee.Api.Core/README.md) — `ApiAccessValidator` 等級判斷邏輯
- `samples/Web.Js.Demo/` — 純 JS demo，端到端驗證 Plain 路徑完整 CRUD

## 不在範圍

- **`ProtectionLevel = Encrypted` 全 enum 退場** — 應用層加密在特定場景仍有價值，保留為 `Public` 之上的 opt-in 等級
- **JS 版加密管線實作** — 見〈替代方案 1〉拒絕原因；若未來真有需求，視為獨立 ADR
- **DTO codegen / TypeScript 自動產生** — 屬工具鏈議題，與 `ProtectionLevel` 決策無關
- **跨來源呼叫的 CORS 預設值** — host 各自決定，與 BO 方法保護等級獨立
- **NPM 套件化** — 升級路徑見 plan 內三階段表（純 JS sample → TS + Vite → NPM 套件），觸發條件未到不啟動
