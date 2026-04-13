# Bee.Repository

> Default implementation of repository abstractions, providing session management, database operations, and form data access.

[繁體中文](README.zh-TW.md)

## Architecture Position

- **Layer**: Data Access Layer (implementation)
- **Downstream** (dependents): Applications (via `RepositoryInfo` injection)
- **Upstream** (dependencies): `Bee.Db`, `Bee.Repository.Abstractions`

## Target Frameworks

- `netstandard2.0` -- broad compatibility with .NET Framework 4.6.1+ and .NET Core 2.0+
- `net10.0` -- access to modern runtime APIs and performance improvements

## Key Features

### Session Management

- `SessionRepository` -- persists sessions in the `st_session` table with XML-serialized `SessionUser` data
- Generates GUID-based access tokens for unpredictable session identifiers
- Supports one-time sessions that auto-delete after first retrieval
- Expired sessions are auto-cleaned on access using UTC time comparison

### Database Operations

- `DatabaseRepository` -- connection testing with parameter substitution (`{@DbName}`, `{@UserId}`, `{@Password}`)
- Schema upgrades via `TableSchemaBuilder` for FormSchema-driven table management

### Form Data Access

- `DataFormRepository` -- default implementation of `IDataFormRepository` for data form CRUD, resolved by ProgId
- `ReportFormRepository` -- default implementation of `IReportFormRepository` for report form queries, resolved by ProgId

### Provider Implementations

- `SystemRepositoryProvider` -- wires `SessionRepository` and `DatabaseRepository` into a single provider
- `FormRepositoryProvider` -- factory that creates form repository instances by ProgId

## Key Public APIs

| Class | Purpose |
|-------|---------|
| `SessionRepository` | Session CRUD against `st_session` / `st_user` tables |
| `DatabaseRepository` | Connection testing and schema migration |
| `DataFormRepository` | Data form data access implementation |
| `ReportFormRepository` | Report form data access implementation |
| `SystemRepositoryProvider` | Default `ISystemRepositoryProvider` implementation |
| `FormRepositoryProvider` | Default `IFormRepositoryProvider` implementation |

## Design Conventions

- **XML serialization for sessions** -- `SessionUser` is serialized to XML and stored in `st_session.session_user_xml`; deserialized back on retrieval.
- **Connection string parameter substitution** -- `DatabaseRepository.TestConnection` replaces `{@DbName}`, `{@UserId}`, and `{@Password}` placeholders before opening a connection.
- **One-time session auto-delete** -- when `SessionUser.OneTime` is true, the session record is deleted immediately after `GetSession` returns.
- **Expired session cleanup** -- `GetSession` compares `sys_invalid_time` against `DateTime.UtcNow` and deletes stale records transparently.
- **Parameterized queries** -- all SQL uses `DbCommandSpec` with positional parameters to prevent SQL injection.
- **Nullable reference types** enabled (`<Nullable>enable</Nullable>`).

## Directory Structure

```
Bee.Repository/
  Form/       # DataFormRepository, ReportFormRepository
  Provider/   # SystemRepositoryProvider, FormRepositoryProvider
  System/     # SessionRepository, DatabaseRepository
```
