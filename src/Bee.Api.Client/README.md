# Bee.Api.Client

> API client connector providing a unified interface for local (in-process) and remote (network) business logic invocation.

[繁體中文](README.zh-TW.md)

## Architecture Position

- **Layer**: Frontend / Client
- **Downstream** (dependents): Applications (WinForms, Blazor, etc.)
- **Upstream** (dependencies): `Bee.Api.Core`

## Target Framework

- `net10.0` -- access to modern runtime APIs and performance improvements

## Key Features

### Local / Remote Strategy

- `IJsonRpcProvider` abstracts the transport layer; `LocalApiProvider` invokes business logic in-process via `JsonRpcExecutor`, while `RemoteApiProvider` sends HTTP POST requests to a remote endpoint.
- The active strategy is selected at construction time via the connector's dual-constructor pattern.

### System-Level Connector

- `SystemApiConnector` exposes system operations: `Login` (RSA key-exchange authentication), `Ping` (health check), `CreateSession` (one-time or time-limited tokens), `Initialize` (environment bootstrap), `GetDefine` / `SaveDefine` (definition CRUD), and `ExecFunc` (custom function execution).
- Every async method has a synchronous counterpart powered by `SyncExecutor`.

### Form-Level Connector

- `FormApiConnector` binds to a specific `ProgId` and exposes form-level business object calls (`ExecFunc`, `ExecFuncAnonymous`, `ExecFuncLocal`).
- Inherits the full payload pipeline (encoding, compression, encryption) from `ApiConnector`.

### Connection Validation

- `ApiConnectValidator` determines `ConnectType` (Local or Remote) from the endpoint string, validates the target, and optionally auto-generates missing settings files for local connections.
- Remote validation performs a `Ping` to verify connectivity before returning.

### Cached Definition Access

- `RemoteDefineAccess` implements `IDefineAccess` over the API, caching retrieved definitions (SystemSettings, DatabaseSettings, FormSchema, FormLayout, etc.) to avoid redundant network calls.

### Application Context

- `ApiClientInfo` holds static runtime configuration: `ConnectType`, `Endpoint`, `ApiKey`, `ApiEncryptionKey`, and `SupportedConnectTypes`.

### Async-to-Sync Bridge

- `SyncExecutor` wraps `Task.Run` + `GetAwaiter().GetResult()` to safely call async methods from synchronous contexts (constructors, WinForms event handlers) without deadlocks.

## Key Public APIs

| Class / Interface | Purpose |
|-------------------|---------|
| `ApiClientInfo` | Static runtime configuration (connection type, endpoint, keys) |
| `ApiConnector` | Abstract base connector with payload pipeline and tracing |
| `SystemApiConnector` | System-level operations (Login, Ping, CreateSession, Initialize, Define CRUD, ExecFunc) |
| `FormApiConnector` | Form-level business object calls bound to a specific ProgId |
| `IJsonRpcProvider` | Strategy interface for JSON-RPC transport |
| `LocalApiProvider` | In-process provider via `JsonRpcExecutor` |
| `RemoteApiProvider` | HTTP-based provider with API key and Bearer token headers |
| `RemoteDefineAccess` | `IDefineAccess` implementation with caching over the API |
| `ApiConnectValidator` | Validates endpoints and determines connection type |
| `ConnectType` | Enum: `Local`, `Remote` |
| `SupportedConnectTypes` | Flags enum: `Local`, `Remote`, `Both` |
| `SyncExecutor` | Async-to-sync bridge for non-async callers |

## Design Conventions

- **Strategy Pattern** -- `IJsonRpcProvider` with `LocalApiProvider` and `RemoteApiProvider` implementations; the connector selects the strategy at construction time.
- **Template Method** -- `ApiConnector` defines `ExecuteAsync<T>` with fixed steps (create request, transform payload, invoke provider, restore response); subclasses supply domain-specific methods.
- **Dual constructor pattern** -- each connector offers two constructors: `(Guid accessToken)` for local and `(string endpoint, Guid accessToken)` for remote, mirroring the two provider types.
- **Payload format negotiation** -- requests default to `PayloadFormat.Encrypted`; the pipeline automatically downgrades to `Encoded` when no encryption key is set, or to `Plain` for local providers in non-debug mode.
- **SyncExecutor for legacy callers** -- every async method has a sync wrapper using `SyncExecutor`, enabling use from WinForms constructors and synchronous APIs without deadlocks.

## Directory Structure

```
Bee.Api.Client/
  ApiClientInfo.cs              # Static runtime configuration
  ApiConnectValidator.cs           # Endpoint validation and ConnectType detection
  ConnectType.cs                   # Local / Remote enum
  SupportedConnectTypes.cs         # Flags enum for supported connection types
  SyncExecutor.cs                  # Async-to-sync bridge
  Connectors/
    ApiConnector.cs                # Abstract base connector
    SystemApiConnector.cs          # System-level operations
    FormApiConnector.cs            # Form-level business object calls
  Providers/
    IJsonRpcProvider.cs            # Transport strategy interface
    LocalApiProvider.cs     # In-process provider
    RemoteApiProvider.cs    # HTTP-based provider
  DefineAccess/
    RemoteDefineAccess.cs          # Cached IDefineAccess over API
```
