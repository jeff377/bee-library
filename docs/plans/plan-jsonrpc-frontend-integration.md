# 計畫：JSON-RPC 前端整合 — 開放 Plain 格式供 JS 框架呼叫

**狀態：📝 擬定中**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | BO 方法 ProtectionLevel 降級為 Public | 📝 待做 |
| 2 | JS 前端整合指引文件 + 可跑 demo sample（純 JS） | 📝 待做 |

## 背景

Bee.NET 的 JSON-RPC API 在 `PayloadFormat.Plain` 下直接傳輸 JSON 字串，不經過
MessagePack 序列化或加密管線。加上 `System.Text.Json` 已實作 `DataSet` / `DataTable`
的序列化支援，技術上 JS 前端（React / Vue / Angular 等）可以用原生 `fetch` + JSON
直接與後端通訊，無需任何額外依賴。

### 現行限制

Login 允許 `ClientPublicKey` 傳入空字串（跳過 RSA key exchange），差別在於 client
不會拿到 `ApiEncryptionKey`，後續只能呼叫 `ProtectionLevel.Public` 的方法。

目前 FormBO 的 CRUD 方法和 SystemBO 的公司操作標為 `Encrypted`，導致 JS 前端
（走 Plain 格式）無法呼叫：

| 方法 | 現行 ProtectionLevel | 影響 |
|------|---------------------|------|
| `EnterCompany` | Encrypted | JS 無法進入公司 |
| `LeaveCompany` | Encrypted | JS 無法離開公司 |
| `Logout` | Encrypted | JS 無法登出 |
| `GetNewData` | Encrypted | JS 無法取得空白 DataSet |
| `GetData` | Encrypted | JS 無法載入單筆資料 |
| `Save` | Encrypted | JS 無法儲存 |
| `Delete` | Encrypted | JS 無法刪除 |

### 設計原則

預設信任 HTTPS 傳輸層加密，API 方法以 `Public` 為預設保護等級。
只有特定高敏感方法（由開發者依需求判斷）才強制要求 `Encrypted`，
確保即使 HTTPS 被中間人攻擊，payload 仍有應用層加密保護。

這與 ADR-013 的 Family B（`Bee.Web.*`）策略一致：Web 前端走 `RemoteApiProvider`，
安全性由 HTTPS + `AccessToken` 認證雙重保護。

## 階段 1：BO 方法 ProtectionLevel 降級

### 變更清單

**`src/Bee.Business/Form/FormBusinessObject.cs`**

| 方法 | 行號 | 現行 | 調整為 |
|------|------|------|--------|
| `GetNewData` | 84 | `Encrypted, Authenticated` | `Public, Authenticated` |
| `GetData` | 99 | `Encrypted, Authenticated` | `Public, Authenticated` |
| `Save` | 115 | `Encrypted, Authenticated` | `Public, Authenticated` |
| `Delete` | 136 | `Encrypted, Authenticated` | `Public, Authenticated` |

**`src/Bee.Business/System/SystemBusinessObject.cs`**

| 方法 | 行號 | 現行 | 調整為 |
|------|------|------|--------|
| `EnterCompany` | 137 | `Encrypted, Authenticated` | `Public, Authenticated` |
| `LeaveCompany` | 170 | `Encrypted, Authenticated` | `Public, Authenticated` |
| `Logout` | 197 | `Encrypted, Authenticated` | `Public, Authenticated` |

### 不變更的方法

以下方法維持現行等級，不在本次調整範圍：

- `CheckPackageUpdate`（Encoded, Anonymous）— 套件更新檢查，維持 Encoded
- `GetPackage`（Encoded, Anonymous）— 套件下載，維持 Encoded

### 階段 1 完成後 JS 前端可呼叫的方法總覽

降級後 JS 前端（走 `PayloadFormat.Plain`）能呼叫的完整 API 表面，標 `*` 為本 plan 降級而開放的方法：

**Anonymous（不需 AccessToken）**

| 方法 | ProtectionLevel | 用途 |
|------|----------------|------|
| `System.Ping` | Public | 服務健康檢查 |
| `System.GetCommonConfiguration` | Public | 取得公開環境組態 |
| `System.Login` | Public | 登入（`ClientPublicKey` 傳空字串走 Plain 路徑） |
| `System.CreateSession` | Public | 建立 user session（內部 admin 用） |

**Authenticated（需 AccessToken）**

| 方法 | ProtectionLevel | 用途 |
|------|----------------|------|
| `System.EnterCompany`* | Public | 進入公司 |
| `System.LeaveCompany`* | Public | 離開公司 |
| `System.Logout`* | Public | 登出 |
| `System.GetDefine` | Public | 取得 FormSchema / TableSchema 等定義（SystemSettings / DatabaseSettings 由 `IsLocalCall` 守住） |
| `System.SaveDefine` | Public | 寫入定義（同上限制） |
| `<ProgId>.GetList` | Public | 列表查詢 |
| `<ProgId>.GetNewData`* | Public | 取得空白 DataSet |
| `<ProgId>.GetData`* | Public | 取得單筆資料 |
| `<ProgId>.Save`* | Public | 儲存（CRUD 派發） |
| `<ProgId>.Delete`* | Public | 刪除 |

**JS 不會呼叫的方法**

`CheckPackageUpdate` / `GetPackage` 維持 Encoded，因為這兩個是 .NET runtime 端的套件更新機制，JS 前端沒有對應需求。

### 影響分析

- **向下相容**：Encrypted 格式的 .NET client 呼叫這些方法時，`ApiAccessValidator`
  允許高保護格式呼叫低保護方法（Encrypted ≥ Public），不受影響
- **Blazor WASM**：`Bee.Web.Blazor.Wasm` 走 `RemoteApiProvider`，目前使用 Encoded/Encrypted
  格式，降級後仍可正常呼叫
- **桌面端**：`Bee.UI.*` 走 `ClientInfo` + 完整加密管線，不受影響
- **測試**：現有測試以 Local 呼叫為主（bypass protection level check），不需調整

### 驗證方式

1. 建置通過（`dotnet build`）
2. 現有測試全過（`./test.sh`）
3. 手動以 Plain 格式 `curl` 呼叫降級後的方法，確認回傳正常 JSON

## 階段 2：JS 前端整合指引

在 `docs/` 新增一份 JS 前端整合指引文件，內容包含：

### JS 端通訊格式

```
POST /api
Content-Type: application/json
X-Api-Key: <api-key>
Authorization: Bearer <access-token>

{
  "jsonrpc": "2.0",
  "method": "ProgId.Action",
  "params": { "format": 0, "value": { ... } },
  "id": "<uuid>"
}
```

- `format: 0` = `PayloadFormat.Plain`，value 為原生 JSON 物件
- 回應的 `result.value` 也是原生 JSON 物件，直接 `response.json()` 可用
- 屬性命名 server 端為 case-insensitive 反序列化，JS 可全程使用 camelCase
  （`userId` → `UserId`、`clientName` → `ClientName`）

**為什麼省略 `params.type`**

.NET client 的 wire format 永遠帶 `type` 欄位（Encoded / Encrypted 路徑需要型別資訊還原 MessagePack binary，
Plain 路徑為了與其他格式對齊也照寫），但 JS Plain 路徑**完全不需要送 `type`**：

- Server 端 [`ApiPayloadConverter.RestoreFrom`](../../src/Bee.Api.Core/JsonRpc/ApiPayloadConverter.cs)
  在 Plain 分支 early-return，根本沒讀 `type` 欄位
- 目標型別由 [`JsonRpcExecutor`](../../src/Bee.Api.Core/JsonRpc/JsonRpcExecutor.cs)
  從 BO 方法 `MethodInfo.GetParameters()[0].ParameterType` 反推
- 現有 API surface 沒有任何 args 類別宣告 `object` 屬性，不存在「需要 type 才能還原 polymorphic 欄位」的情境
- 多型欄位（如 `GetListArgs.Filter` → `FilterNode`）由
  [`FilterNodeCollectionJsonConverter`](../../src/Bee.Definition/Filters/FilterNodeCollectionJsonConverter.cs)
  inline 處理 discriminator，不依賴外層 `params.type`

回歸保障：`JsonRpcExecutorTests.Ping_PlainWithoutTypeField_*` 三個測試已驗證
省略 / 空字串 / 帶錯誤型別字串三種情境 server 都正常反序列化。

### 認證流程

1. 呼叫 `System.Login`，`ClientPublicKey` 傳空字串
2. 取得 `AccessToken`（GUID 字串）
3. 後續呼叫帶 `Authorization: Bearer <AccessToken>`
4. Token 有效期限由 `LoginResponse.ExpiredAt` 標示

### 可用 API 方法總覽

降級後 JS 前端可呼叫的完整方法清單（含現有 Public + 本次降級）。

### TypeScript 範例

提供輕量的 JSON-RPC client wrapper 範例程式碼，涵蓋：
- `rpcCall<T>()` 泛型呼叫函式
- Login / GetList / GetData / Save / Delete 的使用範例
- 錯誤處理（JSON-RPC error code 對應）

### 客戶端封裝形式

採用**純 JS sample + 文件指引雙軌**：JS client 本體放在 sample 內作為 single source of truth，
整合指引文件從 sample 引用程式碼，避免 docs / sample 兩邊不同步。

**檔案配置**

```
samples/
  └── Web.Js.Demo/                    # 新增
      ├── README.md / README.zh-TW.md
      ├── index.html                  # demo 頁面：Login → Enter Company → CRUD
      ├── bee-api-client.js           # JS client wrapper（rpcCall + 認證 + error mapping）
      └── app.js                      # demo 互動邏輯
docs/
  ├── jsonrpc-frontend-integration.md     # 整合指引（含 wire format、認證流程、API 總覽）
  └── jsonrpc-frontend-integration.zh-TW.md
```

backend host 重用現有 `samples/QuickStart.Server`，加 `app.UseCors()` 開放本機 file:// 與
localhost 來源。不另建新 backend。

**工具鏈：純 JS + ES modules，無 npm**

- 開瀏覽器直接跑（`open samples/Web.Js.Demo/index.html` 或透過 dotnet-served static file）
- 與其他 sample 風格一致（全部 `dotnet run` 即跑，無 npm / node_modules）
- 型別檢查靠 JSDoc 註解（在 docs snippet 內可改寫為 TypeScript 供 TS 專案 copy-paste）
- `demo-smoke` skill 可直接整合，用 computer-use 跑端到端 UI 測試

**範圍限制：Plain-only**

JS client 只實作 `PayloadFormat.Plain` 路徑，不實作 Encoded / Encrypted。理由：
- 階段 1 降級 ProtectionLevel 的整個動機就是讓 JS 不用碰加密管線
- 若 JS 仍要實作 AES-CBC-HMAC + RSA + 壓縮，等於白做階段 1 — 不如改用 Blazor WASM
- 安全性靠 HTTPS + Bearer Token 雙重保護（與 ADR-013 Family B 策略一致）

**對應 .NET 端的方法表面**

JS wrapper 應提供與 `SystemApiConnector` / `FormApiConnector` 對應的 method 簽名（語法對應，不做型別轉換）：

| .NET | JS |
|------|----|
| `SystemApiConnector.LoginAsync(userId, password)` | `systemApi.login(userId, password)` |
| `SystemApiConnector.EnterCompanyAsync(companyId)` | `systemApi.enterCompany(companyId)` |
| `SystemApiConnector.LogoutAsync()` | `systemApi.logout()` |
| `FormApiConnector(progId).GetListAsync(...)` | `formApi(progId).getList(...)` |
| `FormApiConnector(progId).SaveAsync(dataSet)` | `formApi(progId).save(dataSet)` |

**未實作的對應**

- `LocalApiProvider`（in-process）— JS 無 .NET runtime，不適用
- `ApiPayloadConverter`（Encoded / Encrypted 變換）— Plain-only 不需要
- DTO codegen — sample 內手寫 JS object，docs snippet 可額外提供 TypeScript interface 供 TS 專案 copy-paste

**升級路徑（不在本 plan 範圍內，遇到觸發條件再啟動）**

預期演進路徑為三階段：

| 階段 | 形式 | 觸發升級條件 |
|------|------|------------|
| **現在（本 plan）** | 純 JS sample + docs snippet | — |
| **下一階段** | TypeScript + Vite sample（仍在 `samples/`） | sample 邏輯複雜到 JSDoc 不夠；或內部 TS 專案要直接複製 sample 起手 |
| **再下一階段** | 獨立 NPM 套件（如 `@bee/api-client`） | JS 前端 ≥ 3 個專案使用、外部開發者直接整合、或 DTO 型別手動同步出包 ≥ 2 次（此時連帶評估 codegen） |

升級時前一階段的程式碼可直接搬入下一階段，無架構改寫成本。

## 相關連結

- [ADR-013：前端 API 連線策略](../adr/adr-013-frontend-api-connection-strategy.md)
- [plan-blazor-web-integration](plan-blazor-web-integration.md)
- [plan-bo-crud-methods](plan-bo-crud-methods.md)
- [Bee.Api.Core README](../../src/Bee.Api.Core/README.md)
- [Bee.Api.Client README](../../src/Bee.Api.Client/README.md)
