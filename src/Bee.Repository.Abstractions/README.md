# Bee.Repository.Abstractions

> Abstract interface library for the data access layer, defining Repository and Provider contracts.

[繁體中文](README.zh-TW.md)

## Architecture Position

- **Layer**: Data Access Layer (contracts)
- **Downstream** (dependents): `Bee.Repository`, `Bee.Business`
- **Upstream** (dependencies): `Bee.Definition`

## Target Framework

- `net10.0` -- access to modern runtime APIs and performance improvements

## Key Features

### Repository Contracts

- `ISessionRepository` -- session lifecycle operations: create, retrieve, and validate user sessions via access tokens
- `IDatabaseRepository` -- database administration operations: connection testing and table schema upgrades

### Provider Contracts

- `ISystemRepositoryFactory` -- aggregates system-level repositories (`ISessionRepository`, `IDatabaseRepository`)
- `IFormRepositoryFactory` -- factory for form-level repositories, resolving `IDataFormRepository` and `IReportFormRepository` by ProgId

### Form Repository Contracts

- `IDataFormRepository` -- repository interface for data form CRUD operations
- `IReportFormRepository` -- repository interface for report form query operations

### Database Routing Contract

- `IRepositoryDatabaseRouter` -- resolves the physical databaseId a repository should use for a logical `DbScope` (`Common` / `Log` / `Company`) and the current session's access token

## Key Public APIs

| Interface / Class | Purpose |
|-------------------|---------|
| `ISessionRepository` | Session create (`CreateSession`) and retrieve (`GetSession`) |
| `IDatabaseRepository` | Connection testing (`TestConnection`) and schema migration (`UpgradeTableSchema`) |
| `ISystemRepositoryFactory` | Aggregates system repositories into a single provider |
| `IFormRepositoryFactory` | Factory to resolve form repositories by ProgId |
| `IDataFormRepository` | Contract for data form data access |
| `IReportFormRepository` | Contract for report form data access |
| `IRepositoryDatabaseRouter` | Resolves the physical databaseId for a logical `DbScope` and access token |

## Design Conventions

- **Repository Pattern** -- each domain concern (session, database, form) has a dedicated repository interface.
- **Provider / Factory Pattern** -- `ISystemRepositoryFactory` aggregates repositories; `IFormRepositoryFactory` acts as a factory resolving repositories by ProgId.
- **Passive contracts, injected via DI** -- this project defines contracts only; there is no static holder or service locator. Concrete implementations are registered in the DI container and injected where needed, rather than resolved from a static entry point or read from a static `BackendConfiguration`.
- **Nullable reference types** enabled (`<Nullable>enable</Nullable>`).

## Directory Structure

```
Bee.Repository.Abstractions/
  Form/                          # IDataFormRepository, IReportFormRepository
  Factories/                     # ISystemRepositoryFactory, IFormRepositoryFactory
  System/                        # ISessionRepository, IDatabaseRepository
  IRepositoryDatabaseRouter.cs   # DB routing contract (DbScope -> databaseId)
```
