# Bee.Business

> Business logic layer providing authentication, session management, definition access, and custom function execution framework.

[繁體中文](README.zh-TW.md)

## Architecture Position

- **Layer**: Business Logic Layer
- **Position in the dependency graph**: see [Project Dependency Map](../../docs/dependency-map.md). Not enumerated here — the csproj files are the authority, and a prose copy in every package README drifts with nothing to catch it. These did: `Bee.Hosting` was missing as a dependent from four of them for months after it was extracted.

## Target Framework

- `net10.0` -- access to modern runtime APIs and performance improvements

## Key Features

### Custom Function Execution

- `IBusinessObject` -- base interface exposing `ExecFunc` (authenticated) and `ExecFuncAnonymous` (anonymous) entry points
- `ExecFuncArgs` / `ExecFuncResult` -- input/output contracts for custom function dispatch
- `ExecFuncAccessControlAttribute` -- method-level attribute declaring authentication requirements per function

### System Operations

- `ISystemBusinessObject` -- the cross-BO contract: `Login`, `CreateSession`, `EnterCompany`, `LeaveCompany`, `Logout`. API-only methods (`Ping`, `GetFormSchema`, `GetFormLayout`, `GetLanguage`, …) are public on the concrete class with `[ApiAccessControl]` and deliberately stay off this interface
- Argument/result pairs for each operation: `LoginArgs`/`LoginResult`, `PingArgs`/`PingResult`, `CreateSessionArgs`/`CreateSessionResult`, `GetDefineArgs`/`GetDefineResult`, `SaveDefineArgs`/`SaveDefineResult`, `GetPackageArgs`/`GetPackageResult`, `GetCommonConfigurationArgs`/`GetCommonConfigurationResult`

### Form Operations

- `IFormBusinessObject` -- form-level business logic interface, extending `IBusinessObject` for FormSchema-driven operations

### Authentication & Security

- `LoginAttemptTracker` -- in-memory account lockout enforcement (default: 5 consecutive failures triggers 15-minute lockout)
- `AccessTokenValidator` -- validates access tokens for authenticated API calls
- `StaticApiEncryptionKeyProvider` / `DynamicApiEncryptionKeyProvider` -- pluggable encryption key strategies for API payload protection

### Data & Caching

- `CacheDataSourceProvider` -- provides cached data sources for business logic consumption
- `BusinessArgs` / `BusinessResult` -- base input/output types shared across business operations

## Key Public APIs

| Class / Interface | Purpose |
|-------------------|---------|
| `IBusinessObject` | Base BO interface (`ExecFunc`, `ExecFuncAnonymous`) |
| `ISystemBusinessObject` | Cross-BO system operations (API-only methods stay on the concrete class) |
| `IFormBusinessObject` | Form-level business logic interface |
| `BusinessObjectFactory` | Factory for creating BO instances |
| `LoginAttemptTracker` | Account lockout after consecutive failures |
| `AccessTokenValidator` | Access token validation |
| `StaticApiEncryptionKeyProvider` | Fixed encryption key strategy |
| `DynamicApiEncryptionKeyProvider` | Per-session encryption key strategy |
| `ExecFuncArgs` / `ExecFuncResult` | Custom function dispatch contracts |
| `ExecFuncAccessControlAttribute` | Method-level auth requirement declaration |
| `BusinessArgs` / `BusinessResult` | Base input/output types for operations |

## Design Conventions

- **Command Pattern** -- `ExecFunc` invokes methods by name via reflection, dispatching custom business logic dynamically.
- **Factory Pattern** -- `BusinessObjectFactory` creates `SystemBusinessObject` and `FormBusinessObject` instances with access token and context.
- **Template Method** -- `BusinessObject` base class defines the execution skeleton; subclasses override `DoExecFunc()` for specific logic.
- **Strategy Pattern** -- encryption key providers (`StaticApiEncryptionKeyProvider` / `DynamicApiEncryptionKeyProvider`) are interchangeable implementations.
- **Attribute-driven access control** -- `ExecFuncAccessControlAttribute` declares per-method authentication requirements, checked at dispatch time.
- **Nullable reference types** enabled (`<Nullable>enable</Nullable>`).

## Directory Structure

```
Bee.Business/
  Attributes/       # ExecFuncAccessControlAttribute
  Form/             # Form-level BO archetype (namespace Bee.Business.Form)
                    # IFormBusinessObject, FormBusinessObject, FormExecFuncHandler
  System/           # System-level BO archetype (namespace Bee.Business.System)
                    # ISystemBusinessObject, SystemBusinessObject, SystemExecFuncHandler,
                    # and Args/Result pairs for system operations
                    # (Login, Ping, CreateSession, GetDefine, SaveDefine,
                    #  GetPackage, GetCommonConfiguration)
  Providers/        # StaticApiEncryptionKeyProvider, DynamicApiEncryptionKeyProvider,
                    # CacheDataSourceProvider
  Security/         # LoginAttemptTracker (in-memory account-lockout tracker)
  Validator/        # AccessTokenValidator
  *.cs (root)       # BusinessObject, BusinessObjectFactory, IBusinessObject,
                    # IExecFuncHandler, ExecFuncArgs, ExecFuncResult,
                    # BusinessArgs, BusinessResult
```
