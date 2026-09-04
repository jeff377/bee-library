# Bee.Api.Client

> API client connector providing a unified interface for local (in-process) and remote (network) business logic invocation.

[繁體中文](README.zh-TW.md)

## Architecture Position

- **Layer**: Frontend / Client
- **Position in the dependency graph**: see [Project Dependency Map](../../docs/dependency-map.md). Not enumerated here — the csproj files are the authority, and a prose copy in every package README drifts with nothing to catch it. These did: `Bee.Hosting` was missing as a dependent from four of them for months after it was extracted.
- Consumed by application code (WinForms, Blazor, and other heads).

## Target Framework

- `net10.0` -- access to modern runtime APIs and performance improvements

## Key Features

### Local / Remote Strategy

- `IJsonRpcProvider` abstracts the transport layer; `LocalApiProvider` invokes business logic in-process via `JsonRpcExecutor`, while `RemoteApiProvider` sends HTTP POST requests to a remote endpoint.
- The active strategy is selected at construction time via the connector's dual-constructor pattern.

### System-Level Connector

- `SystemApiConnector` exposes system operations: `LoginAsync` (RSA key-exchange authentication), `PingAsync` (health check), `CreateSessionAsync` (time-limited tokens), `InitializeAsync` (environment bootstrap), `GetDefineAsync` / `SaveDefineAsync` (definition CRUD), and `ExecFuncAsync` (custom function execution). Every operation is asynchronous and carries the `Async` suffix.

### Form-Level Connector

- `FormApiConnector` binds to a specific `ProgId` and exposes form-level business object calls (`ExecFuncAsync`, `ExecFuncAnonymousAsync`).
- Inherits the full payload pipeline (encoding, compression, encryption) from `ApiConnector`.

### Connection Validation

- `ApiConnectValidator` determines `ConnectType` (Local or Remote) from the endpoint string, validates the target, and optionally auto-generates missing settings files for local connections.
- Remote validation performs a `Ping` to verify connectivity before returning.

### Cached Definition Access

- `ClientDefineAccess` implements `IDefineAccess` over the API, caching retrieved definitions (SystemSettings, DatabaseSettings, FormSchema, FormLayout, etc.) to avoid redundant network calls.

### Application Context

- `ApiClientInfo` holds static runtime configuration: `ConnectType`, `Endpoint`, `ApiKey`, `ApiEncryptionKey`, and `SupportedConnectTypes`.

## Key Public APIs

| Class / Interface | Purpose |
|-------------------|---------|
| `ApiClientInfo` | Static runtime configuration (connection type, endpoint, keys) |
| `ApiConnector` | Abstract base connector with payload pipeline and tracing |
| `SystemApiConnector` | System-level operations (LoginAsync, PingAsync, CreateSessionAsync, InitializeAsync, Define CRUD, ExecFuncAsync) |
| `FormApiConnector` | Form-level business object calls bound to a specific ProgId |
| `IJsonRpcProvider` | Strategy interface for JSON-RPC transport |
| `LocalApiProvider` | In-process provider via `JsonRpcExecutor` |
| `RemoteApiProvider` | HTTP-based provider with API key and Bearer token headers |
| `ClientDefineAccess` | `IDefineAccess` implementation with caching over the API |
| `ApiConnectValidator` | Validates endpoints and determines connection type |
| `ConnectType` | Enum: `Local`, `Remote` |
| `SupportedConnectTypes` | Flags enum: `Local`, `Remote`, `Both` |

## Design Conventions

- **Strategy Pattern** -- `IJsonRpcProvider` with `LocalApiProvider` and `RemoteApiProvider` implementations; the connector selects the strategy at construction time.
- **Template Method** -- `ApiConnector` defines `ExecuteAsync<T>` with fixed steps (create request, transform payload, invoke provider, restore response); subclasses supply domain-specific methods.
- **Dual constructor pattern** -- each connector offers a local and a remote constructor, mirroring the two provider types: `SystemApiConnector(Guid accessToken)` / `(string endpoint, Guid accessToken)`. `FormApiConnector` takes the bound `progId` as well: `(Guid accessToken, string progId)` / `(string endpoint, Guid accessToken, string progId)`.
- **Payload format negotiation** -- requests default to `PayloadFormat.Encrypted`; the pipeline automatically downgrades to `Encoded` when no encryption key is set, or to `Plain` for local providers in non-debug mode.

## Directory Structure

```
Bee.Api.Client/
  ApiClientInfo.cs              # Static runtime configuration
  ApiConnectValidator.cs           # Endpoint validation and ConnectType detection
  ConnectType.cs                   # Local / Remote enum
  SupportedConnectTypes.cs         # Flags enum for supported connection types
  Connectors/
    ApiConnector.cs                # Abstract base connector
    SystemApiConnector.cs          # System-level operations
    FormApiConnector.cs            # Form-level business object calls
  Providers/
    IJsonRpcProvider.cs            # Transport strategy interface
    LocalApiProvider.cs     # In-process provider
    RemoteApiProvider.cs    # HTTP-based provider
  DefineAccess/
    ClientDefineAccess.cs          # Cached IDefineAccess over API
```
