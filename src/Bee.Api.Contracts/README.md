# Bee.Api.Contracts

> Contract interface library between the API layer and business logic layer, defining all Request/Response interfaces.

[繁體中文](README.zh-TW.md)

## Architecture Position

- **Layer**: API Layer (contracts)
- **Downstream** (dependents): `Bee.Api.Core`, `Bee.Business`
- **Upstream** (dependencies): `Bee.Definition`

## Target Frameworks

- `netstandard2.0` -- broad compatibility with .NET Framework 4.6.1+ and .NET Core 2.0+
- `net10.0` -- access to modern runtime APIs and performance improvements

## Key Features

### Authentication Contracts

- `ILoginRequest` / `ILoginResponse` -- RSA key-exchange login flow (client sends `ClientPublicKey`, server returns `ApiEncryptionKey`)
- `ICreateSessionRequest` / `ICreateSessionResponse` -- session creation after successful authentication

### Health Check

- `IPingRequest` / `IPingResponse` -- lightweight API health / connectivity check

### Definition CRUD

- `IGetDefineRequest` / `IGetDefineResponse` -- retrieve FormSchema-driven definition data
- `ISaveDefineRequest` / `ISaveDefineResponse` -- persist definition data changes

### Custom Function Execution

- `IExecFuncRequest` / `IExecFuncResponse` -- invoke server-side custom functions (AnyCode pattern)

### Configuration

- `IGetCommonConfigurationRequest` / `IGetCommonConfigurationResponse` -- retrieve shared application configuration

### Package Management

- `ICheckPackageUpdateRequest` / `ICheckPackageUpdateResponse` -- query available package updates
- `IGetPackageRequest` / `IGetPackageResponse` -- download package content
- `PackageUpdateQuery` -- query parameters for update check
- `PackageUpdateInfo` -- update metadata (version, size, SHA-256, delivery mode), serialized with MessagePack
- `PackageDelivery` -- enum defining delivery mode (`Url` or `Api`)

## Key Public APIs

| Interface / Class | Purpose |
|-------------------|---------|
| `ILoginRequest` / `ILoginResponse` | RSA key-exchange login contract |
| `ICreateSessionRequest` / `ICreateSessionResponse` | Session creation contract |
| `IPingRequest` / `IPingResponse` | Health check contract |
| `IGetDefineRequest` / `IGetDefineResponse` | Definition retrieval contract |
| `ISaveDefineRequest` / `ISaveDefineResponse` | Definition persistence contract |
| `IExecFuncRequest` / `IExecFuncResponse` | Custom function execution contract |
| `IGetCommonConfigurationRequest` / `IGetCommonConfigurationResponse` | Configuration retrieval contract |
| `ICheckPackageUpdateRequest` / `ICheckPackageUpdateResponse` | Package update check contract |
| `IGetPackageRequest` / `IGetPackageResponse` | Package download contract |
| `PackageUpdateQuery` | Update check query parameters |
| `PackageUpdateInfo` | Package update metadata (MessagePack) |
| `PackageDelivery` | Delivery mode enum (`Url` / `Api`) |

## Design Conventions

- **Pure interface definitions** -- each API operation is defined as an `IXxxRequest` / `IXxxResponse` pair; no implementation logic in this project.
- **MessagePack serialization** -- data classes such as `PackageUpdateInfo` use `[MessagePackObject]` and `[Key(n)]` attributes for high-performance binary serialization.
- **RSA-based security** -- the login contract includes `ClientPublicKey` (client-generated) and `ApiEncryptionKey` (server-generated) for secure key exchange.
- **Stable enum values** -- `PackageDelivery` members have explicit integer values; existing values must not change to preserve serialization compatibility.
- **Nullable reference types** enabled (`<Nullable>enable</Nullable>`).

## Directory Structure

```
Bee.Api.Contracts/
  ILoginRequest.cs / ILoginResponse.cs
  ICreateSessionRequest.cs / ICreateSessionResponse.cs
  IPingRequest.cs / IPingResponse.cs
  IGetDefineRequest.cs / IGetDefineResponse.cs
  ISaveDefineRequest.cs / ISaveDefineResponse.cs
  IExecFuncRequest.cs / IExecFuncResponse.cs
  IGetCommonConfigurationRequest.cs / IGetCommonConfigurationResponse.cs
  ICheckPackageUpdateRequest.cs / ICheckPackageUpdateResponse.cs
  IGetPackageRequest.cs / IGetPackageResponse.cs
  PackageUpdateQuery.cs
  PackageUpdateInfo.cs
  PackageDelivery.cs
```
