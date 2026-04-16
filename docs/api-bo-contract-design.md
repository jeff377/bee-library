# API Contract and BO Parameter Design Principles

[繁體中文](api-bo-contract-design.zh-TW.md)

This document describes the design architecture and usage of API Contracts (Request / Response) and BO Parameters (Args / Result) in the Bee.NET framework, intended for developers extending API methods or implementing BO logic.

---

## Core Concept

The framework separates parameter types between the API transport layer and the BO business logic layer, using **contract interfaces** to define shared properties and ensure both layers remain independent with clear separation of concerns.

```
Contract Interface (ILoginRequest / ILoginResponse)   <-- Single source of truth
     |
     +-- API Type (LoginRequest / LoginResponse)  <-- With serialization, for API transport
     |
     +-- BO Type  (LoginArgs / LoginResult)       <-- Pure POCO, for business logic
```

**Why separate layers?**

- Clients (`Bee.Api.Client`) only interact with API types, without knowing BO implementation details
- The BO layer has no dependency on API assemblies, enabling independent testing and evolution
- BOs can add internal-only properties beyond the contract without affecting the API

---

## Type Overview

### Contract Interfaces (Bee.Api.Contracts)

Define the property contracts for API method inputs and outputs. Contain only read-only properties with no serialization attributes.

```csharp
namespace Bee.Api.Contracts
{
    public interface ILoginRequest
    {
        string UserId { get; }
        string Password { get; }
        string ClientPublicKey { get; }
    }

    public interface ILoginResponse
    {
        Guid AccessToken { get; }
        DateTime ExpiredAt { get; }
        string ApiEncryptionKey { get; }
        string UserId { get; }
        string UserName { get; }
    }
}
```

### API Contract Types (Bee.Api.Core.System)

Inherit `ApiRequest` / `ApiResponse`, implement contract interfaces, and carry MessagePack serialization attributes. Clients use these types to send requests and receive responses.

```csharp
[MessagePackObject]
[Serializable]
public class LoginRequest : ApiRequest, ILoginRequest
{
    [Key(100)] public string UserId { get; set; } = string.Empty;
    [Key(101)] public string Password { get; set; } = string.Empty;
    [Key(102)] public string ClientPublicKey { get; set; } = string.Empty;
}

[MessagePackObject]
[Serializable]
public class LoginResponse : ApiResponse, ILoginResponse
{
    [Key(100)] public Guid AccessToken { get; set; } = Guid.Empty;
    [Key(101)] public DateTime ExpiredAt { get; set; }
    [Key(102)] public string ApiEncryptionKey { get; set; } = string.Empty;
    [Key(103)] public string UserId { get; set; } = string.Empty;
    [Key(104)] public string UserName { get; set; } = string.Empty;
}
```

### BO Parameter Types (Bee.Business)

Inherit `BusinessArgs` / `BusinessResult`, implement contract interfaces, and are pure POCOs. May include additional BO-specific properties beyond the contract.

```csharp
public class LoginArgs : BusinessArgs, ILoginRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ClientPublicKey { get; set; } = string.Empty;

    // BO-specific property (not in the contract interface)
    public bool IsAutoLogin { get; set; }
}
```

---

## Naming Conventions

| Purpose | Pattern | Example | Assembly |
|---------|---------|---------|----------|
| Contract interface (input) | `IXxxRequest` | `ILoginRequest` | Bee.Api.Contracts |
| Contract interface (output) | `IXxxResponse` | `ILoginResponse` | Bee.Api.Contracts |
| API input | `XxxRequest` | `LoginRequest` | Bee.Api.Core |
| API output | `XxxResponse` | `LoginResponse` | Bee.Api.Core |
| BO input | `XxxArgs` | `LoginArgs` | Bee.Business |
| BO output | `XxxResult` | `LoginResult` | Bee.Business |

---

## Three Usage Scenarios

### Scenario 1: API Method, BO Needs No Extra Properties (Most Common)

API and BO parameter properties are identical; no Args / Result types are needed.

**Types to create:** Contract interfaces + API contract types
**BO method signature:** Use contract interfaces for parameters and return types

```csharp
public ILoginResponse Login(ILoginRequest request)
{
    // Executor passes LoginRequest; BO-to-BO calls can also pass LoginRequest directly
    return new LoginResponse
    {
        AccessToken = Guid.NewGuid(),
        UserId = request.UserId
    };
}
```

### Scenario 2: API Method, BO Needs Extra Properties

BO-to-BO calls require internal properties not visible to the API.

**Types to create:** Contract interfaces + API contract types + BO parameter types

```csharp
// BO parameter with extra properties
public class LoginArgs : BusinessArgs, ILoginRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ClientPublicKey { get; set; } = string.Empty;
    public bool IsAutoLogin { get; set; }  // BO-specific
}

// BO method uses pattern matching to access extra properties
public ILoginResponse Login(ILoginRequest request)
{
    bool isAutoLogin = request is LoginArgs args && args.IsAutoLogin;
    // ...
}
```

### Scenario 3: BO-Only Method (Not Exposed as API)

Internal methods used only within the BO layer, not published as JSON-RPC APIs.

**Types to create:** BO parameter types only (no contract interfaces, no API types)

```csharp
public class RecalcArgs : BusinessArgs
{
    public string OrderId { get; set; }
    public bool ForceRecalc { get; set; }
}
```

---

## Serialization Rules

| Layer | `[MessagePackObject]` | `[Key(n)]` | `[Serializable]` | `IObjectSerialize` |
|-------|:---:|:---:|:---:|:---:|
| Contract interface | No | No | No | No |
| API type | **Yes** | **Yes** (from 100) | **Yes** | Yes (provided by base) |
| BO type | No | No | No | No |

- `[Key(0)]` is reserved for the base class `ParameterCollection` property
- Custom property keys start at 100 to avoid conflicts with the base class

---

## Client Development

When calling APIs through `SystemApiConnector`, always use `Request` / `Response` types:

```csharp
var connector = new SystemApiConnector(endpoint, accessToken);

// Use API types, not BO types
var response = await connector.LoginAsync("admin", "password");
Console.WriteLine(response.AccessToken);
```

Clients **should not reference** and **do not need** `BusinessArgs`, `BusinessResult`, or any `XxxArgs` / `XxxResult` types.

---

## BO Development

### Method Signatures

BO method parameters and return types should use **contract interfaces**, not concrete implementation types:

```csharp
// Correct: use contract interfaces
public ILoginResponse Login(ILoginRequest request) { ... }

// Avoid: binding to concrete types
public LoginResult Login(LoginArgs args) { ... }
```

### Response Mapping

When a BO method returns a pure POCO (e.g., `LoginResult`), the framework's `ApiOutputConverter` automatically converts it to the corresponding API type (`LoginResponse`) by **naming convention** — no registration is required.

Convention:

```
{Action}Result  ──reflection lookup in Bee.Api.Core──▶  {Action}Response
```

For example, `PingResult` is automatically mapped to `PingResponse`. Reflection results are cached per BO type so each type is resolved only once.

> The convention is enforced: any BO result type that does not follow `{Action}Result` / `{Action}Response` naming cannot be auto-converted. See [ADR-007](adr/adr-007-convention-based-type-resolution.md) for background.

### ExecFunc Pattern

`ExecFunc` uses `ParameterCollection` as a generic parameter mechanism and does not follow the Request/Response layering pattern described here. It continues to use the existing approach.

---

## Steps to Add a New API Method

Using `GetOrder` as an example:

1. **Define contract interfaces** (`src/Bee.Api.Contracts/`)
   - `IGetOrderRequest.cs` — input properties
   - `IGetOrderResponse.cs` — output properties

2. **Create API contract types** (`src/Bee.Api.Core/System/` or the appropriate module directory)
   - `GetOrderRequest.cs` — inherits `ApiRequest`, implements `IGetOrderRequest`, with MessagePack
   - `GetOrderResponse.cs` — inherits `ApiResponse`, implements `IGetOrderResponse`, with MessagePack

3. **Implement BO method**
   - Method signature uses `IGetOrderRequest` / `IGetOrderResponse`
   - If BO-specific properties are needed, create `GetOrderArgs` / `GetOrderResult`
   - Naming must follow the `{Action}Args` / `{Action}Result` convention so that `ApiOutputConverter` can auto-map `GetOrderResult` → `GetOrderResponse`

4. **Update client Connector** (if needed)
   - Add a corresponding method using `GetOrderRequest` / `GetOrderResponse`

> No manual registration is required. Response mapping is resolved automatically by naming convention (see [ADR-007](adr/adr-007-convention-based-type-resolution.md)).

---

## Assembly Dependency Direction

```
Bee.Api.Contracts           <-- Contract interfaces (shared by API & BO)
    |
    +-- Bee.Api.Core        <-- API types (with serialization)
    |       |
    |       +-- Bee.Api.Client  <-- Client (uses Request / Response only)
    |
    +-- Bee.Business        <-- BO types (pure POCO) + BO interfaces
```

**Principle:** Arrows indicate dependency direction. `Bee.Api.Core` and `Bee.Business` do not depend on each other; each depends only on `Bee.Api.Contracts`.
