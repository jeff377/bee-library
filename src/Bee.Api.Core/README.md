# Bee.Api.Core

> Core API framework handling JSON-RPC execution, payload encryption pipeline, authorization validation, and type mapping.

[繁體中文](README.zh-TW.md)

## Architecture Position

- **Layer**: API Layer (core engine)
- **Downstream** (dependents): `Bee.Api.AspNetCore`, `Bee.Api.Client`
- **Upstream** (dependencies): `Bee.Api.Contracts`, `Bee.Definition`

## Target Framework

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
- `IApiPayloadCompressor` / `GzipPayloadCompressor` -- pluggable Gzip compression.
- `IApiPayloadEncryptor` / `AesPayloadEncryptor` -- pluggable AES-CBC-HMAC encryption.
- `NoEncryptionEncryptor` -- bypass encryptor for testing only.
- `ApiPayloadOptionsFactory` -- creates pipeline options based on protection level.

### Anti-Replay (optional, off by default)

- `ApiPayloadFrame` -- timestamp and sequence number carried inside the encrypted envelope, ahead of the payload body.
- `ReplayWindow` / `IReplayWindowStore` -- per-session sliding window of sequence numbers (64-slot bitmap, no database round trip).
- `ApiServiceOptions.RequireWireFrame` -- the master switch; **client and server must be set to the same value**.
- `ApiReplayProtection` -- third dimension of `ApiAccessControlAttribute`, declaring per method whether sequences are checked.

Covers `Encoded` and `Encrypted` only. `Plain` has no envelope, so any anti-replay field could be
rewritten and none is sent; `Encoded` carries a frame but has no HMAC, so it only stops a verbatim
resend. See [ADR-042](../../docs/adr/adr-042-api-replay-protection.md) for the full boundary,
rollout order and known limitations.

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

> These types are `internal`. They are documented here because they define the wire behaviour, but
> they are not part of the package's public surface — use `MessagePackPayloadSerializer` (public)
> to reach the same pipeline.

- `SafeMessagePackSerializerOptions` -- type whitelist for deserialization to prevent untrusted-type attacks.
- `MessagePackCodec` -- encoder/decoder for MessagePack serialization.
- `WireContracts` -- the explicit formatter registrations for every wire type. The contractless
  resolver is a desktop-only convenience, not the carrying mechanism: .NET for iOS turns dynamic
  code off, and an unregistered type fails there outright (see
  [ADR-037](../../docs/adr/adr-037-wire-explicit-registration.md)).
- `WireValueFormatter` -- discriminated envelope for `object`-typed members (filter values,
  parameter values, table cells).

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
  Conversion/       ApiInputConverter, ApiOutputConverter
                    (.NET object-model conversion: API type <-> BO type)
  JsonRpc/          JsonRpcExecutor, JsonRpcRequest, JsonRpcResponse, JsonRpcError,
                    JsonRpcException, ApiPayload, ApiPayloadConverter
  Messages/         ApiMessageBase, ApiRequest, ApiResponse,
                    ExecFuncRequest, ExecFuncResponse,
                    ApiHeaders, PayloadFormat, ApiErrorInfo, ApiCallContext
    System/         Built-in system-level request/response types
                    (Login, Ping, CreateSession, GetDefine, SaveDefine,
                    GetPackage, CheckPackageUpdate, GetCommonConfiguration)
  MessagePack/      SafeMessagePackSerializerOptions, MessagePackCodec,
                    WireContracts (explicit registrations), WireValueFormatter,
                    custom formatters for ADO.NET types
  Registry/         ApiContractRegistry (contract -> API type registry)
  Transformers/     IApiPayloadTransformer, ApiPayloadTransformer,
                    IApiPayloadSerializer, MessagePackPayloadSerializer,
                    IApiPayloadCompressor, GzipPayloadCompressor,
                    IApiPayloadEncryptor, AesPayloadEncryptor,
                    NoEncryptionEncryptor, ApiPayloadOptionsFactory
                    (byte-level payload pipeline; distinct from Conversion's
                    .NET object-level type mapping)
  Validator/        ApiAccessValidator
  (root)            ApiServiceOptions (user-facing startup configuration)
```

The namespace layout follows the design principles in [ADR-008](../../docs/adr/adr-008-bee-db-namespace-layout.md):
contracts grouped by responsibility (`Messages` for message types, `Conversion` for type
conversion, `Registry` for registries, etc.); the root reserved for cross-cutting
infrastructure (here, only `ApiServiceOptions`).
