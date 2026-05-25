# JSON-RPC 前端整合指引

[English](jsonrpc-frontend-integration.md)

如何從 JavaScript / TypeScript 前端（React、Vue、Angular、Svelte、vanilla）
呼叫 Bee.NET 的 JSON-RPC 後端，**client 端完全不需要 .NET**。

整套大約 150 行純 JS。可跑的範例在
[`samples/Web.Js.Demo/`](../samples/Web.Js.Demo/README.zh-TW.md)，
本文檔解釋它「為什麼這樣寫」。

---

## 何時用本指引 vs .NET client

| 前端類型 | 用哪個 client | Wire format |
|----------|-------------|-------------|
| Blazor Server（in-process） | `Bee.Api.Client`（Local） | 直接 DI 派遣，不走 HTTP |
| Blazor WASM | `Bee.Api.Client`（Remote） | HTTP + MessagePack + AES-CBC-HMAC |
| .NET MAUI / WPF / WinForms | `Bee.Api.Client`（Remote） | 同上 |
| **React / Vue / Angular / vanilla JS** | **fetch + JSON-RPC**（本指引） | **HTTPS + 純 JSON** |
| TypeScript SPA / Node.js client | 同上 — fetch + JSON-RPC，可加 TS 型別 | HTTPS + 純 JSON |

如果前端能跑 .NET runtime，請用 `Bee.Api.Client` — 你會免費拿到強型別契約、
MessagePack 效能、payload 加密。如果前端是 JS，走本指引。

整體策略見 [ADR-013：前端 API 連線策略](adr/adr-013-frontend-api-connection-strategy.md)。

---

## Wire format

Bee.NET 的 JSON-RPC endpoint 接受標準
[JSON-RPC 2.0](https://www.jsonrpc.org/specification) 信封，配上自訂的
`params` 結構：

### Request

```http
POST /api HTTP/1.1
Host: your.backend
Content-Type: application/json
X-Api-Key: <api-key>
Authorization: Bearer <access-token>     // anonymous 方法可省略

{
  "jsonrpc": "2.0",
  "method": "System.Login",
  "params": {
    "format": 0,
    "value": { "userId": "demo", "password": "demo", "clientPublicKey": "" }
  },
  "id": "<uuid>"
}
```

- `method` — `<ProgId>.<Action>`，server 用 reflection 派遣到對應 BO
- `params.format` — JS 端**永遠 `0`**（`PayloadFormat.Plain`）
- `params.value` — args 物件，**camelCase 或 PascalCase 屬性名都可以**
  （server 反序列化 case-insensitive）
- `id` — client 任選的識別字串，response 會原樣回傳

### Response — 成功

```json
{
  "jsonrpc": "2.0",
  "method": "System.Login",
  "result": {
    "format": 0,
    "value": {
      "accessToken": "f32bcd07-be16-44b9-be4a-db2bc669a6c2",
      "expiredAt": "2026-05-25T15:53:47.408399Z",
      "userId": "demo",
      "userName": "Demo User"
    },
    "type": ""
  },
  "id": "<echoed>"
}
```

### Response — 錯誤

```json
{
  "jsonrpc": "2.0",
  "method": "System.Login",
  "error": {
    "code": -32099,
    "message": "Invalid username or password.",
    "data": null
  },
  "id": "<echoed>"
}
```

互斥：response 一定**只有** `result` 或 `error`，不會兩者並存。

### 為什麼 JS 不需要 `params.type`

.NET client 的 wire format 永遠帶 `params.type`（例如
`"Bee.Api.Core.Messages.System.LoginRequest, Bee.Api.Core"`），因為
Encoded / Encrypted 格式需要型別資訊把 MessagePack binary 還原為強型別物件。
Plain 格式不需要：

- Server 把 `params.value` 當 `JsonElement`（通用 JSON 樹）處理
- 目標型別來自 `MethodInfo.GetParameters()[0].ParameterType`，靠 reflection
  從 BO 方法簽名拿
- `RestoreFrom` 步驟在 Plain 分支 early-return，根本沒讀 `type`
- 多型欄位（例如 `GetListArgs.Filter` → `FilterNode` 多個 subclass）由
  自訂 `JsonConverter` inline 處理 discriminator，不依賴外層 `type`

你想送 `params.type` 也可以（Plain 路徑會忽略），省略則 payload 較小。
回歸保障：[`JsonRpcExecutorTests.Ping_PlainWith*`](../tests/Bee.Api.Core.UnitTests/JsonRpcExecutorTests.cs)
驗證了省略 / 空字串 / 帶錯誤型別字串三種情境都會成功。

---

## Headers

| Header | 必要 | 值 | 說明 |
|--------|------|-----|------|
| `Content-Type` | 是 | `application/json` | JSON-RPC 信封 |
| `X-Api-Key` | 是 | 預設為任意非空字串 | 預設的 `ApiAuthorizationValidator` 只檢查非空。Production host 應註冊更嚴格的 validator 比對註冊清單。 |
| `Authorization` | Authenticated 方法必要 | `Bearer <accessToken>` | 從 `System.Login` 拿到的 GUID |

Host 必須設好 CORS。Demo 後端在
[`samples/QuickStart.Server/Program.cs`](../samples/QuickStart.Server/Program.cs)
開了 `AllowAnyOrigin` 政策；production host 必須明確限制 origin。

---

## 認證流程

```
1. POST System.Login        →  { accessToken, expiredAt, userId, userName }
2. POST <Method>（帶 token）
   ...
3. POST System.Logout       →  {}
```

`System.Login` 的 `clientPublicKey` 傳空字串，server 會跳過 RSA key exchange
（JS Plain 路徑不需要加密金鑰），AccessToken 直接以明文回在
`result.value.accessToken`。

Token 是 `Guid` 字串。Token 有效期限預設 1 小時；過期後 backend 會以
`JsonRpcErrorCode.Unauthorized` 拒絕 authenticated 呼叫，client 必須重新登入。

某些方法需要先進入公司（`System.EnterCompany`）以設定 `SessionInfo.CompanyId`，
讓 form CRUD 路由到公司專屬資料庫。Demo 的 `Employee` 屬於 `Common` scope，
**不需要 EnterCompany 也能跑**；綁在公司 scope 的方法若跳過 EnterCompany 會
回 `CompanyNotEntered`。

---

## 可呼叫的方法

權威方法清單見
[`docs/plans/plan-jsonrpc-frontend-integration.md`](plans/plan-jsonrpc-frontend-integration.md)。
摘要：

| 類別 | 方法 |
|------|------|
| Anonymous | `System.Ping`、`System.GetCommonConfiguration`、`System.Login`、`System.CreateSession` |
| Authenticated — Session | `System.EnterCompany`、`System.LeaveCompany`、`System.Logout` |
| Authenticated — Definition | `System.GetDefine`、`System.SaveDefine`（SystemSettings / DatabaseSettings 限 local call） |
| Authenticated — Form CRUD | `<ProgId>.GetList`、`GetNewData`、`GetData`、`Save`、`Delete` |
| **JS 不會用到** | `System.CheckPackageUpdate`、`System.GetPackage`（Encoded，.NET runtime 用） |

方法名稱**大小寫敏感** — `system.ping` 不會派遣到 `System.Ping`。

---

## 錯誤處理

`response.error.code` 對應 [`JsonRpcErrorCode`](../src/Bee.Api.Core/JsonRpc/JsonRpcErrorCode.cs)：

| Code | Name | 意義 | 對應動作 |
|------|------|------|---------|
| `-32700` | `ParseError` | request body 不是合法 JSON | 修 client 序列化 |
| `-32600` | `InvalidRequest` | 缺 method、缺 API key、Bearer 格式錯 | 檢查 headers |
| `-32601` | `MethodNotFound` | 找不到 `progId.action` | 檢查方法名稱 / 大小寫 |
| `-32602` | `InvalidParams` | args 驗證失敗 | 看 `message` 內容 |
| `-32000` | `InternalError` | 未處理的 server 端例外 | 看 server log；訊息不適合對使用者顯示 |
| `-32001` | `Unauthorized` | Token 缺、無效、過期 | 重新登入 |
| `-32002` | `CompanyNotEntered` | 方法需要公司 context | 先呼叫 `System.EnterCompany` |
| `-32003` | `CompanyAccessDenied` | 使用者沒有此公司權限 | 顯示拒絕、切換公司 |
| `-32099` | `UserMessage` | 業務規則違反（驗證、權限、領域規則） | 直接把 `message` 顯示給使用者 |

User-facing 範圍（特別是 `-32099`）的 `message` 可以原樣顯示給終端使用者。
`-32000` 絕對不要直接顯示 — 內部 log 記下，UI 顯示通用「請求失敗」訊息。

---

## TypeScript wrapper

[`samples/Web.Js.Demo/bee-api-client.js`](../samples/Web.Js.Demo/bee-api-client.js)
的 TypeScript 版本，可直接複製到 TS 專案。獨立檔案、無框架依賴，
state 管理請自行接（Zustand / Redux / Context 都可）。

```typescript
// bee-api-client.ts
const ENDPOINT = '/api';
const API_KEY = 'your-api-key';

let _accessToken: string | null = null;

export const setAccessToken = (t: string | null) => { _accessToken = t; };
export const getAccessToken = () => _accessToken;

export class RpcError extends Error {
  constructor(public code: number, message: string, public data?: unknown) {
    super(message);
    this.name = 'RpcError';
  }
}

interface JsonRpcResponse<T> {
  jsonrpc: '2.0';
  method: string;
  result?: { format: number; value: T; type: string };
  error?: { code: number; message: string; data?: unknown };
  id: string;
}

async function rpcCall<T>(method: string, value: object): Promise<T> {
  const body = {
    jsonrpc: '2.0',
    method,
    params: { format: 0, value },
    id: crypto.randomUUID(),
  };
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    'X-Api-Key': API_KEY,
  };
  if (_accessToken) headers.Authorization = `Bearer ${_accessToken}`;

  const res = await fetch(ENDPOINT, { method: 'POST', headers, body: JSON.stringify(body) });
  if (!res.ok) throw new RpcError(res.status, `HTTP ${res.status} ${res.statusText}`);

  const data: JsonRpcResponse<T> = await res.json();
  if (data.error) throw new RpcError(data.error.code, data.error.message, data.error.data);
  return data.result!.value;
}

// ---- DTO 型別（依需要擴充）----

export interface LoginResponse {
  accessToken: string;
  expiredAt: string;
  userId: string;
  userName: string;
}

export interface DataTableColumn {
  name: string;
  type: string;
  allowNull: boolean;
  readOnly: boolean;
  maxLength: number;
  caption: string;
  defaultValue: unknown;
}

export type RowState = 'Unchanged' | 'Added' | 'Modified' | 'Deleted';

export interface DataRow {
  state: RowState;
  current: Record<string, unknown>;
  original?: Record<string, unknown>;
}

export interface DataTable {
  tableName: string;
  columns: DataTableColumn[];
  primaryKeys: string[];
  rows: DataRow[];
}

export interface DataSet {
  dataSetName: string;
  tables: DataTable[];
  relations: unknown[];
}

// ---- API surface ----

export const systemApi = {
  ping: () => rpcCall<unknown>('System.Ping', { clientName: 'app', traceId: crypto.randomUUID() }),
  login: (userId: string, password: string) =>
    rpcCall<LoginResponse>('System.Login', { userId, password, clientPublicKey: '' }),
  enterCompany: (companyId: string) =>
    rpcCall<unknown>('System.EnterCompany', { companyId }),
  logout: () => rpcCall<unknown>('System.Logout', {}),
};

export const formApi = (progId: string) => ({
  getList: (selectFields = 'sys_id,sys_name,sys_rowid') =>
    rpcCall<{ table: DataTable }>(`${progId}.GetList`,
      { selectFields, filter: null, sortFields: null, paging: null }),
  getNewData: () =>
    rpcCall<{ dataSet: DataSet }>(`${progId}.GetNewData`, {}),
  getData: (rowId: string) =>
    rpcCall<{ dataSet: DataSet }>(`${progId}.GetData`, { rowId }),
  save: (dataSet: DataSet) =>
    rpcCall<{ dataSet: DataSet; affectedRows: number }>(`${progId}.Save`, { dataSet }),
  delete: (rowId: string) =>
    rpcCall<{ rowsAffected: number }>(`${progId}.Delete`, { rowId }),
});
```

需要新方法就擴充 DTO 介面。完整 args / result 型別在
`src/Bee.Api.Core/Messages/`（C# source）— 如果要同步的 DTO 變多，
比起手工維護，可以考慮跑一個小型 codegen。

---

## 相關連結

- [`samples/Web.Js.Demo/README.zh-TW.md`](../samples/Web.Js.Demo/README.zh-TW.md) — 上述所有方法的可跑 demo
- [`docs/plans/plan-jsonrpc-frontend-integration.md`](plans/plan-jsonrpc-frontend-integration.md) — 設計計畫 + 逐方法範圍決策
- [`docs/adr/adr-013-frontend-api-connection-strategy.md`](adr/adr-013-frontend-api-connection-strategy.md) — 前端連線策略全景
- [`src/Bee.Api.Core/README.md`](../src/Bee.Api.Core/README.md) — server 端派遣內部細節
- [`src/Bee.Api.Client/README.md`](../src/Bee.Api.Client/README.md) — 本指引對應的 .NET client
