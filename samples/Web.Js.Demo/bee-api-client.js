// Bee.NET JSON-RPC client for pure JavaScript browsers.
// No build step, no npm — just import as an ES module.
//
// Wire format: PayloadFormat.Plain (params.format = 0, no params.type required).
// Server uses BO method reflection to determine the target args type, so JS sends
// plain JSON objects with camelCase property names (System.Text.Json on the server
// is configured case-insensitive).

const ENDPOINT = 'http://localhost:5050/api';

// The default ApiAuthorizationValidator only requires X-Api-Key to be non-empty;
// the actual value is not checked against a registry. Production hosts must
// register a stricter validator. Using "quickstart-demo" to match the value
// shown in samples/QuickStart.Server/README.md.
const API_KEY = 'quickstart-demo';

let _accessToken = null;

/** Sets the AccessToken used as Bearer for subsequent authenticated calls. */
export function setAccessToken(token) { _accessToken = token; }

/** Gets the current AccessToken, or null if not logged in. */
export function getAccessToken() { return _accessToken; }

/** Clears the AccessToken (after Logout or session expiry). */
export function clearAccessToken() { _accessToken = null; }

/** JSON-RPC error surfaced to callers with the server-side error code. */
export class RpcError extends Error {
  constructor(code, message, data) {
    super(message);
    this.name = 'RpcError';
    this.code = code;
    this.data = data;
  }
}

/**
 * Calls a JSON-RPC method on the Bee.NET backend.
 * @param {string} method   e.g. "System.Login" or "Employee.GetList"
 * @param {object} value    The args object (camelCase property names accepted).
 * @returns {Promise<object>} The result.value payload from the server.
 */
async function rpcCall(method, value) {
  const body = {
    jsonrpc: '2.0',
    method,
    params: { format: 0, value },
    id: crypto.randomUUID(),
  };
  const headers = {
    'Content-Type': 'application/json',
    'X-Api-Key': API_KEY,
  };
  if (_accessToken) headers['Authorization'] = `Bearer ${_accessToken}`;

  const res = await fetch(ENDPOINT, {
    method: 'POST',
    headers,
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    throw new RpcError(res.status, `HTTP ${res.status} ${res.statusText}`);
  }

  const data = await res.json();
  if (data.error) {
    throw new RpcError(data.error.code, data.error.message, data.error.data);
  }
  return data.result?.value ?? null;
}

export const systemApi = {
  /** Ping the backend; no AccessToken required. */
  ping: (traceId = crypto.randomUUID()) =>
    rpcCall('System.Ping', { clientName: 'Web.Js.Demo', traceId }),

  /**
   * Authenticate against the backend. ClientPublicKey is intentionally empty so
   * the server skips the RSA key exchange and the client stays in Plain format.
   */
  login: (userId, password) =>
    rpcCall('System.Login', { userId, password, clientPublicKey: '' }),

  /**
   * Enter the specified company. The default demo backend has no seeded
   * st_company / st_user_company rows, so this call returns an
   * "Company access denied" RpcError — useful for demonstrating the error path.
   */
  enterCompany: (companyId) =>
    rpcCall('System.EnterCompany', { companyId }),

  /** Leave the current company context (idempotent). */
  leaveCompany: () =>
    rpcCall('System.LeaveCompany', {}),

  /** Destroy the current session (idempotent). */
  logout: () =>
    rpcCall('System.Logout', {}),
};

/**
 * Builds a thin form-API wrapper for the given progId. Mirrors
 * FormApiConnector on the .NET side.
 * @param {string} progId  The FormSchema ProgId (e.g. "Employee").
 */
export function formApi(progId) {
  return {
    getList: (
      selectFields = 'sys_id,sys_name,hire_date,sys_rowid',
      filter = null,
      sortFields = null,
      paging = null,
    ) =>
      rpcCall(`${progId}.GetList`, { selectFields, filter, sortFields, paging }),

    getNewData: () =>
      rpcCall(`${progId}.GetNewData`, {}),

    getData: (rowId) =>
      rpcCall(`${progId}.GetData`, { rowId }),

    save: (dataSet) =>
      rpcCall(`${progId}.Save`, { dataSet }),

    delete: (rowId) =>
      rpcCall(`${progId}.Delete`, { rowId }),
  };
}

// Expose the endpoint constant for diagnostics / display.
export const apiEndpoint = ENDPOINT;
