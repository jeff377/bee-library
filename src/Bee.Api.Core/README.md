# Bee.Api.Core

> Core API framework handling JSON-RPC execution, payload encryption pipeline, authorization validation, and type mapping.

[繁體中文](README.zh-TW.md)

## Architecture Position

- **Layer**: API Layer (core engine)
- **Upstream** (dependents): `Bee.Api.AspNetCore`, `Bee.Api.Client`
- **Downstream** (dependencies): `Bee.Api.Contracts`, `Bee.Definition`

## Target Frameworks

- `netstandard2.0` -- broad compatibility with .NET Framework 4.6.1+ and .NET Core 2.0+
- `net10.0` -- access to modern runtime APIs and performance improvements

## Key Features

### JSON-RPC Execution

- `JsonRpcExecutor` -- parses `ProgId.Action` method identifiers, creates business objects via reflection, and invokes the target method.
- `JsonRpcRequest` / `JsonRpcResponse` / `JsonRpcError` -- standard JSON-RPC 2.0 message types.
- `ApiPayload` / `ApiPayloadConverter` -- payload wrapping and conversion for JSON-RPC transport.
- Exception sanitization -- internal error details are hidden from clients in production environments.

### Payload Security Pipeline

- `ApiPayloadTransformer` -- orchestrates the Serialize -> Compress -> Encrypt pipeline (and the reverse on inbound payloads).
- `IApiPayloadSerializer` / `MessagePackPayloadSerializer` -- pluggable serialization via MessagePack.
- `IApiPayloadCompressor` / `GZipPayloadCompressor` -- pluggable GZip compression.
- `IApiPayloadEncryptor` / `AesPayloadEncryptor` -- pluggable AES-CBC-HMAC encryption.
- `NoEncryptionEncryptor` -- bypass encryptor for testing only.
- `ApiPayloadOptionsFactory` -- creates pipeline options based on protection level.

### Authorization & Access Control

- `IApiAuthorizationValidator` / `ApiAuthorizationValidator` -- validates authorization context for incoming requests.
- `ApiAuthorizationContext` / `ApiAuthorizationResult` -- authorization input and outcome types.
- `ApiAccessValidator` -- enforces method-level protection via `ApiAccessControlAttribute`.
- `ApiCallContext` -- captures per-call metadata (token, protection level, caller identity).

### Type Mapping & Contract Registry

- `ApiContractRegistry` -- maps contract interfaces to concrete API request/response types.
- `ApiInputConverter` -- converts raw JSON-RPC parameters to strongly-typed request objects.
- `ApiHeaders` -- standard header constants for API communication.
- `PayloadFormat` -- enum defining protection levels (`Plain`, `Encoded`, `Encrypted`).

### MessagePack Infrastructure

- `SafeMessagePackSerializerOptions` -- type whitelist for deserialization to prevent untrusted-type attacks.
- `MessagePackHelper` -- utility methods for MessagePack operations.
- `FormatterResolver` -- custom resolver with formatters for ADO.NET types (`DataTable`, `DataSet`, etc.).

### Built-in System Operations

- Built-in request/response types for `Login`, `Ping`, `CreateSession`, `GetDefine`, `SaveDefine`, `ExecFunc`, and other system-level operations.

## Key Public APIs

| Class / Interface | Purpose |
|-------------------|---------|
| `JsonRpcExecutor` | Parses `ProgId.Action`, creates BO, invokes method |
| `ApiServiceOptions` | Static DI registry for pluggable components |
| `ApiPayloadTransformer` | Serialize -> Compress -> Encrypt pipeline |
| `ApiAccessValidator` | Method-level protection via `ApiAccessControlAttribute` |
| `ApiContractRegistry` | Maps contract interfaces to API types |
| `SafeMessagePackSerializerOptions` | Type whitelist for safe deserialization |
| `PayloadFormat` | Protection level enum (`Plain`, `Encoded`, `Encrypted`) |
| `ApiAuthorizationValidator` | Request authorization validation |
| `ApiCallContext` | Per-call metadata (token, protection, identity) |
| `ApiPayloadOptionsFactory` | Pipeline options based on protection level |

## Design Conventions

- **Strategy Pattern** -- serializer, compressor, and encryptor are injected via interfaces (`IApiPayloadSerializer`, `IApiPayloadCompressor`, `IApiPayloadEncryptor`), allowing each stage to be replaced independently.
- **Strict pipeline ordering** -- the payload transformer enforces Serialize -> Compress -> Encrypt on outbound and Decrypt -> Decompress -> Deserialize on inbound; the order must not be altered.
- **Type whitelist** -- `SafeMessagePackSerializerOptions` restricts deserializable types to an explicit allow-list, preventing deserialization attacks.
- **Reflection-based dispatch** -- `JsonRpcExecutor` resolves and invokes business object methods by name, decoupling the transport layer from concrete BO types.
- **Exception sanitization** -- internal exception details are stripped from responses in non-development environments to avoid information leakage.
- **Three protection levels** -- `Public` (no auth), `Encoded` (token + Base64), `Encrypted` (token + full encryption) provide graduated security via `ApiAccessControlAttribute`.
- **Nullable reference types** enabled (`<Nullable>enable</Nullable>`).

## Directory Structure

```
Bee.Api.Core/
  Authorization/    IApiAuthorizationValidator, ApiAuthorizationValidator,
                    ApiAuthorizationContext, ApiAuthorizationResult
  JsonRpc/          JsonRpcExecutor, JsonRpcRequest, JsonRpcResponse, JsonRpcError,
                    JsonRpcException, ApiPayload, ApiPayloadConverter
  MessagePack/      SafeMessagePackSerializerOptions, MessagePackHelper,
                    FormatterResolver, custom formatters for ADO.NET types
  Transformer/      IApiPayloadTransformer, ApiPayloadTransformer,
                    IApiPayloadSerializer, MessagePackPayloadSerializer,
                    IApiPayloadCompressor, GZipPayloadCompressor,
                    IApiPayloadEncryptor, AesPayloadEncryptor,
                    NoEncryptionEncryptor, ApiPayloadOptionsFactory
  Validator/        ApiAccessValidator, ApiCallContext
  System/           Built-in request/response types (Login, Ping, CreateSession,
                    GetDefine, SaveDefine, ExecFunc, etc.)
  (root)            ApiServiceOptions, ApiContractRegistry, ApiRequest, ApiResponse,
                    ApiInputConverter, ApiHeaders, PayloadFormat
```
