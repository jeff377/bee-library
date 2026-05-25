# 計畫：JSON-RPC 前端整合 — 開放 Plain 格式供 JS 框架呼叫

**狀態：📝 擬定中**

| 階段 | 範圍 | 狀態 |
|------|------|------|
| 1 | BO 方法 ProtectionLevel 降級為 Public | 📝 待做 |
| 2 | JS 前端整合指引文件 | 📝 待做 |

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
  "params": { "format": 0, "value": { ... }, "type": "" },
  "id": "<uuid>"
}
```

- `format: 0` = `PayloadFormat.Plain`，value 為原生 JSON 物件
- 回應的 `result.value` 也是原生 JSON 物件，直接 `response.json()` 可用

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

## 相關連結

- [ADR-013：前端 API 連線策略](../adr/adr-013-frontend-api-connection-strategy.md)
- [plan-blazor-web-integration](plan-blazor-web-integration.md)
- [plan-bo-crud-methods](plan-bo-crud-methods.md)
- [Bee.Api.Core README](../../src/Bee.Api.Core/README.md)
- [Bee.Api.Client README](../../src/Bee.Api.Client/README.md)
