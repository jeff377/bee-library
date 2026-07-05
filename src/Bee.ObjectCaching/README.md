# Bee.ObjectCaching

> Runtime caching layer that caches definition data and session information to reduce I/O operations.

[繁體中文](README.zh-TW.md)

## Architecture Position

- **Layer**: Infrastructure (caching)
- **Downstream** (dependents): Applications, `Bee.Business` (indirectly)
- **Upstream** (dependencies): `Bee.Definition`, `Microsoft.Extensions.Caching.Memory`

## Target Framework

- `net10.0` -- access to modern runtime APIs and performance improvements

## Key Features

### Definition Caching

- `SystemSettingsCache` -- caches system-level configuration
- `DatabaseSettingsCache` -- caches database connection settings
- `TableSchemaCache` / `FormSchemaCache` -- caches table and form schema definitions
- `FormLayoutCache` -- caches UI layout metadata
- `ProgramSettingsCache` / `DbCategorySettingsCache` -- caches program and DB category settings

### Session Caching

- `SessionInfoCache` -- caches authenticated session data to avoid repeated DB lookups

### Cache Infrastructure

- `ObjectCache<T>` -- single-object cache base class with template method hooks (`GetPolicy`, `GetKey`, `CreateInstance`)
- `KeyObjectCache<T>` -- keyed cache base class for objects identified by a string key
- `ICacheProvider` / `MemoryCacheProvider` -- pluggable cache storage provider
- `CacheItemPolicy` / `CacheTimeKind` -- expiration configuration (default: 20-minute sliding window)

### Services

- `SessionInfoService` -- session lifecycle operations backed by the cache layer
- `EnterpriseObjectService` -- coordinates enterprise-scoped cached objects

### Tenant Customization Overlay

- `ICacheContainerProvider` / `CacheContainerProvider` -- lazily builds a per-`CustomizeId` read-only override cache container (`CachePrefix=customizeId`, backed by `CustomizeOnlyStorage`), reusing the existing cache classes unchanged
- `CustomizeDefineReader` -- `ICustomizeDefineReader` implementation that reads Language / FormLayout / ProgramSettings from the per-tenant override containers; a missing override returns `null` (see [ADR-016](../../docs/adr/adr-016-multitenant-customization-overlay.md))

## Key Public APIs

| Class / Interface | Purpose |
|-------------------|---------|
| `ICacheContainer` | DI-injected contract exposing every cache instance (`SystemSettingsCache`, `FormSchemaCache`, `SessionInfoCache`, etc.) |
| `CacheContainerService` | `ICacheContainer` implementation, registered as a Singleton by `AddBeeFramework` |
| `ObjectCache<T>` | Single-object cache base class |
| `KeyObjectCache<T>` | Keyed cache base class |
| `ICacheProvider` | Cache storage provider interface |
| `CacheDefineAccess` | `IDefineAccess` implementation that reads definitions from local cache (with optional customization overlay) |
| `ICacheContainerProvider` / `CacheContainerProvider` | Per-`CustomizeId` override cache container provider |
| `CustomizeDefineReader` | Tenant customization-override reader (`ICustomizeDefineReader`) |
| `CacheItemPolicy` | Expiration and eviction configuration |
| `CacheInfo` | Metadata descriptor for a cached entry |

## Design Conventions

- **DI injection** -- consumers ctor-inject `ICacheContainer`; the `CacheContainerService` implementation is registered as a Singleton by `AddBeeFramework`, so callers reach the individual cache classes through the injected contract rather than a static facade.
- **Template Method Pattern** -- `ObjectCache<T>` subclasses override `GetPolicy`, `GetKey`, and `CreateInstance` to define caching behavior without modifying the base retrieval logic.
- **Key normalization** -- all cache keys are converted to lowercase using `ToLowerInvariant()` to ensure case-insensitive, culture-invariant lookups.
- **Backing store** -- `MemoryCacheProvider` wraps `Microsoft.Extensions.Caching.Memory.IMemoryCache`; the public `CacheItemPolicy` is mapped internally to `MemoryCacheEntryOptions`.
- **Nullable reference types** enabled (`<Nullable>enable</Nullable>`).

## Directory Structure

```
Bee.ObjectCaching/
  Define/      # SystemSettingsCache, DatabaseSettingsCache, TableSchemaCache,
               # FormSchemaCache, FormLayoutCache, ProgramSettingsCache,
               # DbCategorySettingsCache
  Database/    # SessionInfoCache
  Providers/   # ICacheProvider, MemoryCacheProvider
  Services/    # SessionInfoService, EnterpriseObjectService
  *.cs (root)  # ICacheContainer, CacheContainerService, ObjectCache, KeyObjectCache,
               # CacheItemPolicy, CacheTimeKind, CacheInfo,
               # CacheDefineAccess,
               # CacheContainerProvider, CustomizeDefineReader
```
