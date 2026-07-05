# Bee.Base

> Cross-layer shared utility library providing type conversion, cryptography, serialization, collections, tracing, and background services.

[繁體中文](README.zh-TW.md)

## Architecture Position

- **Layer**: Infrastructure (bottom layer of the Bee.NET framework)
- **Downstream** (dependents): Almost all other `Bee.*` projects (`Bee.Definition`, `Bee.Business`, `Bee.Api.Core`, etc.)
- **Upstream** (dependencies): None (uses `System.Text.Json` built into the runtime)

## Target Framework

- `net10.0` -- access to modern runtime APIs and performance improvements

## Key Features

### Type Conversion & String Utilities

- `ValueUtilities` -- safe type conversions with fallback defaults (`CInt`, `CStr`, `CBool`, etc.)
- `StringExtensions` / `StringUtilities` -- string manipulation helpers (encoding, formatting, comparison)
- `DateTimeExtensions` -- date utilities including ROC (Minguo) calendar support

### Cryptography & Security

- `AesCbcHmacCryptor` -- AES-256-CBC encryption with HMAC-SHA256 authentication (random IV per operation)
- `RsaCryptor` -- RSA asymmetric encryption
- `PasswordHasher` -- PBKDF2-SHA256 password hashing
- `FileHashValidator` -- file integrity verification via SHA-256
- `AesCbcHmacKeyGenerator` -- cryptographic key generation

### Serialization & Compression

- `XmlCodec` / `JsonCodec` -- unified XML / JSON serialization via `System.Text.Json`
- `XmlSerializerCache` -- cached XML serializer instances to avoid repeated reflection
- `Gzip` -- Gzip compression / decompression for payload handling

### Collections

- `KeyCollectionBase<T>` -- generic keyed collection base class
- `StringHashSet` -- case-control hash set for string lookups
- `CollectionExtensions` -- LINQ-style extension methods for common collection operations

### Data Access Helpers

- `DataTable` / `DataSet` / `DataRow` extension methods for simplified ADO.NET usage
- `FieldDbType` and `DbTypeConverter` -- database type mapping utilities

### Tracing & Diagnostics

- `Tracer` / `TraceContext` -- structured diagnostic tracing
- `TraceListener` / `ITraceWriter` -- pluggable trace output targets

### Background Services

- `BackgroundService` -- base class for long-running asynchronous workers
- `BackgroundAction` -- lightweight fire-and-forget task wrapper

## Key Public APIs

| Class / Interface | Purpose |
|-------------------|---------|
| `ValueUtilities` | Safe type conversion with defaults |
| `StringExtensions` / `StringUtilities` | String encoding, formatting, comparison |
| `DateTimeExtensions` | Date utilities and ROC calendar |
| `AesCbcHmacCryptor` | Authenticated symmetric encryption |
| `PasswordHasher` | Password hashing (PBKDF2-SHA256) |
| `XmlCodec` / `JsonCodec` | XML / JSON serialization |
| `IObjectSerialize` | Serialization provider interface |
| `IKeyObject` | Keyed entity interface used across layers |
| `Tracer` | Diagnostic trace entry point |
| `BackgroundService` | Async background worker base class |

## Design Conventions

- **Static utility classes** -- `ValueUtilities`, `StringUtilities`, `DateTimeExtensions` expose functionality as static methods; no instance state.
- **Constant-time comparison** -- `CompareBytes` is used for HMAC / hash validation to prevent timing attacks.
- **Dual-framework conditional compilation** -- `#if NETSTANDARD2_0` guards are used where runtime APIs diverge.
- **Interface-based extensibility** -- serialization is abstracted via `IObjectSerialize` and `IObjectSerializeProcess`.
- **Nullable reference types** enabled (`<Nullable>enable</Nullable>`).

## Directory Structure

```
Bee.Base/
  Attributes/          # TreeNodeAttribute, TreeNodeIgnoreAttribute
  BackgroundServices/  # BackgroundService, BackgroundAction
  Collections/         # KeyCollectionBase<T>, StringHashSet, CollectionExtensions
  Data/                # DataTable/DataSet extensions, FieldDbType, DbTypeConverter
  Security/            # AES, RSA, PBKDF2, file hash utilities
  Serialization/       # JSON/XML serialization, GZip compression
  Tracing/             # Tracer, TraceContext, TraceListener, ITraceWriter
  *.cs (root)          # ValueUtilities, StringExtensions, StringUtilities, DateTimeExtensions, FileUtilities, HttpUtilities,
                       # IPValidator, SysInfo, IKeyObject, etc.
```
