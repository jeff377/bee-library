# Bee.Api.AspNetCore

> ASP.NET Core controller library providing a unified JSON-RPC 2.0 API endpoint.

[繁體中文](README.zh-TW.md)

## Architecture Position

- **Layer**: API Layer (hosting)
- **Downstream** (dependents): Applications (user inherits the controller)
- **Upstream** (dependencies): `Bee.Api.Core`

## Target Frameworks

- `net10.0` -- ASP.NET Core hosting requires the modern runtime

## Key Features

### Single POST Endpoint

- Exposes a single `POST /api` route decorated with `[ApiController]` and `[Produces("application/json")]`.
- Validates `Content-Type: application/json` before processing; returns `415 Unsupported Media Type` for other media types.

### Full Async Request Pipeline

- `PostAsync` orchestrates the complete lifecycle: read request, validate authorization, execute handler.
- Each stage is a `virtual` method that subclasses can override independently.

### JSON-RPC Request Parsing

- `ReadRequestAsync` reads the raw body, deserializes it into a `JsonRpcRequest`, and validates the `Method` field.
- Returns structured `JsonRpcException` errors for empty bodies, missing methods, and malformed JSON.

### Authorization Validation

- `ValidateAuthorization` receives the `X-Api-Key` and `Authorization` header values bound via `[FromHeader]` and delegates to the configured `ApiServiceOptions.AuthorizationValidator`.
- Returns `401 Unauthorized` with a JSON-RPC error when validation fails.

### Request Execution

- `HandleRequestAsync` creates a `JsonRpcExecutor` with the validated access token and returns the result as `application/json`.
- On unhandled exceptions, returns `500 Internal Server Error` with detailed messages in Development and empty messages in Production.

### Structured Error Responses

- `CreateErrorResponse` produces a consistent `JsonRpcResponse` with error code, message, and optional data payload, mapped to the appropriate HTTP status code.

## Key Public APIs

| Class / Member | Purpose |
|----------------|---------|
| `ApiServiceController` | Abstract base controller; inherit and register in your ASP.NET Core app |
| `PostAsync` | Entry point for all JSON-RPC requests (`POST /api`) |
| `ReadRequestAsync` | Parses and validates the JSON-RPC request body |
| `ValidateAuthorization` | Checks API key and Bearer token via `ApiAuthorizationValidator` |
| `HandleRequestAsync` | Dispatches the request to `JsonRpcExecutor` |
| `CreateErrorResponse` | Builds a standardized JSON-RPC error response |
| `IsDevelopment` | Indicates whether the host environment is Development |

## Design Conventions

- **Template Method Pattern** -- `PostAsync` defines the pipeline skeleton; `ReadRequestAsync`, `ValidateAuthorization`, `HandleRequestAsync`, and `CreateErrorResponse` are all `virtual` for selective overriding.
- **Development vs. Production error messages** -- exception details are exposed only when `IsDevelopment` is `true`, preventing information leakage in production.
- **No direct dependency on DI container** -- services are resolved from `HttpContext.RequestServices` so the controller works in any ASP.NET Core host without additional setup.
- **External dependency**: `Microsoft.AspNetCore.Mvc.Core` (v2.3.0).

## Directory Structure

```
Bee.Api.AspNetCore/
  Controllers/
    ApiServiceController.cs    # Abstract base controller (~177 lines)
```
