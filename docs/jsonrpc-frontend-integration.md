# JSON-RPC Frontend Integration Guide

[繁體中文](jsonrpc-frontend-integration.zh-TW.md)

How to call the Bee.NET JSON-RPC backend from a JavaScript / TypeScript frontend
(React, Vue, Angular, Svelte, or vanilla) **without any .NET on the client**.

The whole thing fits in ~150 lines of plain JS. See the working sample at
[`samples/Web.Js.Demo/`](../samples/Web.Js.Demo/README.md) — this guide explains
*why* it works.

---

## When to use this vs the .NET client

| Frontend stack | Client to use | Wire format |
|----------------|---------------|-------------|
| Blazor Server (in-process) | `Bee.Api.Client` (Local) | Direct DI dispatch, no HTTP |
| Blazor WASM | `Bee.Api.Client` (Remote) | MessagePack + AES-CBC-HMAC over HTTP |
| .NET MAUI / WPF / WinForms | `Bee.Api.Client` (Remote) | Same as above |
| **React / Vue / Angular / vanilla JS** | **fetch + JSON-RPC** (this guide) | **Plain JSON over HTTPS** |
| TypeScript SPA / Node.js client | Same — fetch + JSON-RPC, with optional TS types | Plain JSON over HTTPS |

If the frontend can host a .NET runtime, use `Bee.Api.Client` — it gives you typed
contracts, MessagePack throughput, and payload encryption "for free". If the
frontend is JS, this guide is the path.

See [ADR-013: Frontend API connection strategy](adr/adr-013-frontend-api-connection-strategy.md)
for the broader policy.

---

## Wire format

The Bee.NET JSON-RPC endpoint accepts the standard
[JSON-RPC 2.0](https://www.jsonrpc.org/specification) envelope with a custom
`params` shape:

### Request

```http
POST /api HTTP/1.1
Host: your.backend
Content-Type: application/json
X-Api-Key: <api-key>
Authorization: Bearer <access-token>     // omit for anonymous methods

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

- `method` — `<ProgId>.<Action>`, dispatched to the BO by reflection
- `params.format` — **always `0`** (`PayloadFormat.Plain`) from JS
- `params.value` — your args object, with **camelCase or PascalCase property names**
  (server deserializes case-insensitive)
- `id` — any client-chosen identifier echoed back in the response

### Response — success

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

### Response — error

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

Mutually exclusive: a response carries **either** `result` **or** `error`, never both.

### Why JS does not need `params.type`

The .NET client's wire format always carries `params.type` (e.g.
`"Bee.Api.Core.Messages.System.LoginRequest, Bee.Api.Core"`) because Encoded /
Encrypted formats reconstruct the MessagePack binary back into typed objects.
Plain format does not need it:

- Server walks `params.value` as a `JsonElement` (a generic JSON tree)
- Target type is resolved from `MethodInfo.GetParameters()[0].ParameterType` —
  reflection on the BO method's signature
- The `RestoreFrom` step short-circuits for Plain *before* reading `type`
- Polymorphic fields (e.g. `GetListArgs.Filter` → `FilterNode` subclasses)
  use their own `JsonConverter` with an inline discriminator, not the outer `type`

You may send `params.type` if you want (it's ignored on Plain); leaving it out
keeps payloads smaller. Regression coverage:
[`JsonRpcExecutorTests.Ping_PlainWith*`](../tests/Bee.Api.Core.UnitTests/JsonRpcExecutorTests.cs)
asserts omitted, empty, and bogus `type` values all succeed.

---

## Headers

| Header | Required | Value | Notes |
|--------|----------|-------|-------|
| `Content-Type` | Yes | `application/json` | JSON-RPC envelope |
| `X-Api-Key` | Yes | Any non-empty string by default | The default `ApiAuthorizationValidator` only checks that it is non-empty. Production hosts should register a stricter validator that checks against a registry. |
| `Authorization` | Yes for authenticated methods | `Bearer <accessToken>` | The GUID from `System.Login`'s response |

CORS must be configured on the host. The demo backend opens an `AllowAnyOrigin`
policy in [`samples/QuickStart.Server/Program.cs`](../samples/QuickStart.Server/Program.cs);
production hosts must restrict origins explicitly.

---

## Authentication flow

```
1. POST System.Login        →  { accessToken, expiredAt, userId, userName }
2. POST <Method> (with token)
   ...
3. POST System.Logout       →  {}
```

Pass `clientPublicKey: ""` to `System.Login` to skip RSA key exchange (JS Plain
path does not need an encryption key). The server returns the AccessToken
unencrypted in `result.value.accessToken`.

The token is a `Guid` string. Tokens expire (default 1 hour); after expiry the
backend rejects authenticated calls with `JsonRpcErrorCode.Unauthorized` and the
client must re-login.

Some methods require entering a company first (`System.EnterCompany`) to set
`SessionInfo.CompanyId` — this routes form CRUD to the company-specific
database. The `Employee` demo lives in the `Common` scope so it does **not**
require `EnterCompany`; methods bound to company-scoped tables will throw
`CompanyNotEntered` if you skip it.

---

## Available methods

The plan
[`docs/plans/plan-jsonrpc-frontend-integration.md`](plans/plan-jsonrpc-frontend-integration.md)
contains the authoritative method catalog. Summary:

| Category | Methods |
|----------|---------|
| Anonymous | `System.Ping`, `System.GetCommonConfiguration`, `System.Login`, `System.CreateSession` |
| Authenticated — Session | `System.EnterCompany`, `System.LeaveCompany`, `System.Logout` |
| Authenticated — Definition | `System.GetDefine`, `System.SaveDefine` (SystemSettings / DatabaseSettings are local-call-only); `System.GetFormSchema`, `System.GetFormLayout` (JSON-native, JS-preferred) |
| Authenticated — Form CRUD | `<ProgId>.GetList`, `GetNewData`, `GetData`, `Save`, `Delete` |
| **Not for JS** | `System.CheckPackageUpdate`, `System.GetPackage` (Encoded, .NET runtime use) |

**JSON-native schema / layout retrieval**

`System.GetDefine` wraps the requested definition object in an XML string
(`result.Xml`) — convenient for .NET clients (`XmlCodec.Deserialize<T>`) but
awkward from JS (two layers of parsing). Two JSON-native siblings are exposed
for the JS path:

| Method | Args | Returns |
|--------|------|---------|
| `System.GetFormSchema` | `{ progId }` | `{ schema: FormSchema }` — fields, db types, relations |
| `System.GetFormLayout` | `{ progId, layoutId? }` | `{ layout: FormLayout }` — sections, fields, controlType, row/column spans |

The `FormLayout` is generated on demand from `FormSchema`, so JS can request
either independently. For schema-driven UI rendering, both are usually
fetched together (`GetFormSchema` for validation rules, `GetFormLayout` for
the UI shape).

Method names are **case-sensitive** — `system.ping` will not dispatch.

---

## Error handling

`response.error.code` maps to [`JsonRpcErrorCode`](../src/Bee.Api.Core/JsonRpc/JsonRpcErrorCode.cs):

| Code | Name | Meaning | Typical action |
|------|------|---------|----------------|
| `-32700` | `ParseError` | Malformed JSON in the request body | Fix client serialization |
| `-32600` | `InvalidRequest` | Missing method, missing API key, or invalid Bearer format | Inspect headers |
| `-32601` | `MethodNotFound` | Unknown `progId.action` | Check method name / casing |
| `-32602` | `InvalidParams` | Args validation failed | Inspect `message` |
| `-32000` | `InternalError` | Unhandled server-side exception | Server logs; not user-facing |
| `-32001` | `Unauthorized` | Token missing, invalid, or expired | Re-login |
| `-32002` | `CompanyNotEntered` | Method needs company context | Call `System.EnterCompany` first |
| `-32003` | `CompanyAccessDenied` | User has no rights to this company | Display denial, switch company |
| `-32099` | `UserMessage` | Business-rule violation (validation, auth, domain rule) | Display `message` to user |

For anything in the user-facing range (`-32099` especially), `message` is safe to
surface verbatim to the end user. For `-32000`, never display the message — log
internally and show a generic "request failed" instead.

---

## TypeScript wrapper

Drop-in TypeScript port of [`samples/Web.Js.Demo/bee-api-client.js`](../samples/Web.Js.Demo/bee-api-client.js)
for TS projects. Standalone, no framework — bring your own state management.

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

// ---- DTO shapes (extend as needed) ----

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

// FormSchema / FormLayout shapes are deep — these stubs cover the common path.
// Extend or specialise per-app as you start consuming nested fields.
export interface FormSchema {
  progId: string;
  displayName: string;
  categoryId: string;
  listFields: string;
  tables: Array<{ tableName: string; displayName: string; fields: unknown[] }>;
}

export type ControlType =
  | 'TextEdit' | 'DateEdit' | 'YearMonthEdit'
  | 'CheckEdit' | 'MemoEdit' | 'DropDownEdit' | 'ButtonEdit';

export interface LayoutField {
  fieldName: string;
  caption: string;
  controlType: ControlType;
  rowSpan: number;
  columnSpan: number;
  visible: boolean;
}

export interface LayoutSection {
  name: string;
  caption: string;
  showCaption: boolean;
  fields: LayoutField[];
}

export interface LayoutGrid {
  tableName: string;
  caption: string;
  allowActions: string;
  columns: Array<{ fieldName: string; caption: string; controlType: ControlType; visible: boolean }>;
}

export interface FormLayout {
  layoutId: string;
  progId: string;
  caption: string;
  columnCount: number;
  sections: LayoutSection[];
  details: LayoutGrid[];
}

// ---- API surface ----

export const systemApi = {
  ping: () => rpcCall<unknown>('System.Ping', { clientName: 'app', traceId: crypto.randomUUID() }),
  login: (userId: string, password: string) =>
    rpcCall<LoginResponse>('System.Login', { userId, password, clientPublicKey: '' }),
  enterCompany: (companyId: string) =>
    rpcCall<unknown>('System.EnterCompany', { companyId }),
  logout: () => rpcCall<unknown>('System.Logout', {}),
  getFormSchema: (progId: string) =>
    rpcCall<{ schema: FormSchema }>('System.GetFormSchema', { progId }),
  getFormLayout: (progId: string, layoutId = '') =>
    rpcCall<{ layout: FormLayout }>('System.GetFormLayout', { progId, layoutId }),
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

Extend DTO interfaces as you add methods. The full set of args / result shapes
lives in `src/Bee.Api.Core/Messages/` (C# source) — if you need many DTOs
synchronized, consider running a small codegen pass instead of maintaining
them by hand.

---

## See also

- [`samples/Web.Js.Demo/README.md`](../samples/Web.Js.Demo/README.md) — runnable demo of every method above
- [`docs/plans/plan-jsonrpc-frontend-integration.md`](plans/plan-jsonrpc-frontend-integration.md) — design plan + method-by-method scope decisions
- [`docs/adr/adr-013-frontend-api-connection-strategy.md`](adr/adr-013-frontend-api-connection-strategy.md) — broader frontend connection policy
- [`src/Bee.Api.Core/README.md`](../src/Bee.Api.Core/README.md) — server-side dispatch internals
- [`src/Bee.Api.Client/README.md`](../src/Bee.Api.Client/README.md) — the .NET client this guide is the JS counterpart to
