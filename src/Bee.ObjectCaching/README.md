# Bee.ObjectCaching

> Runtime caching layer that caches definition data and session information to reduce I/O operations.

[繁體中文](README.zh-TW.md)

## Architecture Position

- **Layer**: Infrastructure (caching)
- **Upstream** (dependents): Applications, `Bee.Business` (indirectly)
- **Downstream** (dependencies): `Bee.Definition`, `System.Runtime.Caching`

## Target Frameworks

- `netstandard2.0` -- broad compatibility with .NET Framework 4.6.1+ and .NET Core 2.0+
- `net10.0` -- access to modern runtime APIs and performance improvements

## Key Features

### Definition Caching

- `SystemSettingsCache` -- caches system-level configuration
- `DatabaseSettingsCache` -- caches database connection settings
- `TableSchemaCache` / `FormSchemaCache` -- caches table and form schema definitions
- `FormLayoutCache` -- caches UI layout metadata
- `ProgramSettingsCache` / `DbSchemaSettingsCache` -- caches program and DB schema settings

### Session & Runtime Caching

- `SessionInfoCache` -- caches authenticated session data to avoid repeated DB lookups
- `ViewStateCache` -- caches transient view state during user interactions

### Cache Infrastructure

- `ObjectCache<T>` -- single-object cache base class with template method hooks (`GetPolicy`, `GetKey`, `CreateInstance`)
- `KeyObjectCache<T>` -- keyed cache base class for objects identified by a string key
- `ICacheProvider` / `MemoryCacheProvider` -- pluggable cache storage provider
- `CacheItemPolicy` / `CacheTimeKind` -- expiration configuration (default: 20-minute sliding window)

### Cache Invalidation

- `HostFileChangeMonitor` integration -- evicts cache entries when underlying definition files change
- `DbChangeMonitor` -- polls the `ST_Cache` table to detect database-side invalidation signals

### Services

- `SessionInfoService` -- session lifecycle operations backed by the cache layer
- `EnterpriseObjectService` -- coordinates enterprise-scoped cached objects

## Key Public APIs

| Class / Interface | Purpose |
|-------------------|---------|
| `CacheFunc` | Static facade -- `GetSystemSettings`, `GetFormSchema`, `GetSessionInfo`, etc. |
| `CacheContainer` | Lazy singleton that manages all cache instances |
| `ObjectCache<T>` | Single-object cache base class |
| `KeyObjectCache<T>` | Keyed cache base class |
| `ICacheProvider` | Cache storage provider interface |
| `LocalDefineAccess` | `IDefineAccess` implementation that reads definitions from local cache |
| `CacheItemPolicy` | Expiration and eviction configuration |
| `CacheInfo` | Metadata descriptor for a cached entry |

## Design Conventions

- **Facade Pattern** -- `CacheFunc` exposes a flat static API, hiding `CacheContainer` and individual cache classes from callers.
- **Template Method Pattern** -- `ObjectCache<T>` subclasses override `GetPolicy`, `GetKey`, and `CreateInstance` to define caching behavior without modifying the base retrieval logic.
- **Lazy Singleton** -- `CacheContainer` uses `Lazy<T>` to defer initialization until first access.
- **Key normalization** -- all cache keys are converted to uppercase to ensure case-insensitive lookups.
- **Dual invalidation** -- file-based (`HostFileChangeMonitor`) and database-based (`DbChangeMonitor`) eviction strategies coexist.
- **Nullable reference types** enabled (`<Nullable>enable</Nullable>`).

## Directory Structure

```
Bee.ObjectCaching/
  Define/      # SystemSettingsCache, DatabaseSettingsCache, TableSchemaCache,
               # FormSchemaCache, FormLayoutCache, ProgramSettingsCache,
               # DbSchemaSettingsCache
  Database/    # SessionInfoCache
  Runtime/     # ViewStateCache
  Providers/   # ICacheProvider, MemoryCacheProvider
  Services/    # SessionInfoService, EnterpriseObjectService
  *.cs (root)  # CacheFunc, CacheContainer, ObjectCache, KeyObjectCache,
               # CacheItemPolicy, CacheTimeKind, CacheInfo,
               # LocalDefineAccess, DbChangeMonitor
```
