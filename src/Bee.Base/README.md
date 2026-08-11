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

- `ValueUtilities` -- safe type conversions (`CInt`, `CStr`, `CBool`, …). The temporal members
  (`CDateOnly`, `CDateTime`, `CTimeOnly`) return a nullable from their one-argument form and take
  an explicit fallback in the two-argument overload — an unparseable input yields `null` rather
  than a sentinel date that could reach a report
- `FrameworkClock` -- the framework's clock. `UtcNow()` for instants; `Now(timeZoneId)` for a
  wall-clock reading in a given zone, returned as `Unspecified` (never `Local`). A blank zone id
  means UTC
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

### Expression Abstraction

- `IExpressionEvaluator` -- evaluates an expression against a named variable set. The
  DynamicExpresso-backed implementation lives in `Bee.Expressions`; the abstraction sits here so
  the definition and business layers can consume the engine without a third-party dependency
  ([ADR-038](../../docs/adr/adr-038-definition-dependency-boundary.md))
- `ExpressionPolicy` -- the shared type / null policy applied when feeding field values in, so a
  computed field yields the same result on the server and on a UI client
- `ExpressionEvaluationException` -- thrown when an expression cannot be parsed or compiled

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
| `IExpressionEvaluator` | Expression evaluation abstraction (implementation in `Bee.Expressions`) |
| `ExpressionPolicy` | Shared type / null policy for expression variables |

## Design Conventions

- **Static utility classes** -- `ValueUtilities`, `StringUtilities`, `DateTimeExtensions` expose functionality as static methods; no instance state.
- **Constant-time comparison** -- `CryptographicOperations.FixedTimeEquals` is used for HMAC / hash validation to prevent timing attacks.
- **Interface-based extensibility** -- serialization is abstracted via `IObjectSerialize`.
- **Nullable reference types** enabled (`<Nullable>enable</Nullable>`).

## Directory Structure

```
Bee.Base/
  Attributes/          # TreeNodeAttribute, TreeNodeIgnoreAttribute
  Collections/         # KeyCollectionBase<T>, StringHashSet, CollectionExtensions
  Data/                # DataTable/DataSet extensions, FieldDbType, DbTypeConverter
  Expressions/         # IExpressionEvaluator, ExpressionPolicy, ExpressionEvaluationException
  Security/            # AES, RSA, PBKDF2, file hash utilities
  Serialization/       # JSON/XML serialization, GZip compression
  Tracing/             # Tracer, TraceContext, TraceListener, ITraceWriter
  *.cs (root)          # ValueUtilities, StringExtensions, StringUtilities, DateTimeExtensions, FileUtilities, HttpUtilities,
                       # IPValidator, SysInfo, IKeyObject, etc.
```
