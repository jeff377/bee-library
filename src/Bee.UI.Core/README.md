# Bee.UI.Core

> Shared client-side foundation for the `Bee.UI.*` native front-end family (Avalonia today; WinForms / WPF in future): connection state, API connectors, endpoint persistence, and client-side permission capability resolution.

[繁體中文](README.zh-TW.md)

## Architecture Position

- **Layer**: UI Layer (shared client foundation)
- **Position in the dependency graph**: see [Project Dependency Map](../../docs/dependency-map.md). Not enumerated here — the csproj files are the authority, and a prose copy in every package README drifts with nothing to catch it. These did: `Bee.Hosting` was missing as a dependent from four of them for months after it was extracted.
- The Blazor family does **not** consume `Bee.UI.Core` — see [ADR-013](../../docs/adr/adr-013-frontend-api-connection-strategy.md).

## Target Framework

- `net10.0` -- access to modern runtime APIs and performance improvements

## Overview

`Bee.UI.Core` is the framework-agnostic base every `Bee.UI.*` front end builds on. It holds the
per-process client connection state (`ClientInfo`), abstracts where the service endpoint is
persisted (`IEndpointStorage`), and turns a server-issued permission snapshot into per-element UI
capability decisions (`ElementCapabilityResolver`). It carries no UI-framework types, so Avalonia,
and Blazor can each share the same connection and permission logic while rendering their own
way.

## Key Types

### Connection State

- `ClientInfo` -- static client-side connection singleton. Owns the `AccessToken` (per-process token
  model: resetting the token clears the cached `SystemApiConnector`, `ClientDefineAccess`, and
  capability snapshot), lazily creates the `SystemApiConnector` and `ClientDefineAccess`, produces
  form-level connectors via `CreateFormApiConnector(progId)`, resolves the endpoint (local vs.
  remote) through `InitializeAsync` / `SetEndpointAsync`, and applies login / EnterCompany results
  (`ApplyLoginResult`, `ApplyEnterCompanyResult`, `ClearCompanyContext`). `ResetDefineCache` discards
  the cached definition data after a tenant switch.

### Endpoint and API Key Persistence

- `IEndpointStorage` -- persistence contract for the configured service endpoint
  (`LoadEndpoint` / `SetEndpoint` / `SaveEndpoint`).
- `EndpointStorage` -- default implementation, backed by `ClientInfo.ClientSettings`
  (`{ExeName}.Settings.xml`). Front ends assign `ClientInfo.EndpointStorage` to a platform-specific
  implementation when the default file location is unsuitable -- e.g. `FileEndpointStorage`
  (in `Bee.UI.Avalonia`).
- `IApiKeyStorage` / `ApiKeyStorage` -- the same pair for the `X-Api-Key` value
  (`LoadApiKey` / `SetApiKey` / `SaveApiKey`), assigned through `ClientInfo.ApiKeyStorage`. A host
  that replaces `EndpointStorage` because its platform cannot write beside the assembly must
  replace this too; a platform storage class may implement both interfaces and be assigned to
  each (`FileEndpointStorage` does).
- `ClientInfo.ApplyApiKey(defaultApiKey)` -- applies the stored key, seeding empty storage with the
  value the application ships. That makes the shipped constant a first-run default instead of a
  hard-coded key: from then on the stored value wins and can be changed without recompiling.
  `ClientInfo.SetApiKey` persists a new key and applies it to subsequent calls.

> An API key held by a client is not a secret in the cryptographic sense -- it can be recovered from
> the shipped application. It identifies *which application* is calling; authenticating *the user*
> remains the access token's job.

### Host Services

- `IUIViewService` -- view services supplied by the host UI framework (e.g. `ShowApiConnectAsync`
  to prompt for connection setup when the endpoint is missing or unreachable).

### Permission Capability Resolution

- `IElementCapabilityResolver` / `ElementCapabilityResolver` -- UI-agnostic, pure resolver that
  turns a per-model permission snapshot (typically `ClientInfo.Capabilities`) into element-level
  decisions: `Can(schema, action, capabilities)` for commands and
  `ResolveField(schema, fieldName, tableName, capabilities)` for sensitive fields. A `null` snapshot
  means enforcement is inactive and every element stays at full capability.
- `FieldCapability` -- the resolved capability of a single field (`Visible` / `ReadOnly`); combined
  with the field's layout state by the consuming UI. `FieldCapability.Allowed` is the unrestricted
  default.

> Client-side capability resolution is **UX degradation only**. The backend remains the
> authoritative security boundary.

## Design Conventions

- **Per-process token model** -- `ClientInfo` is a static singleton holding one access token for the
  process; changing it invalidates the cached connectors, define accessor, and capability snapshot.
- **Framework-agnostic** -- no UI-framework types leak in, so the same connection and permission
  logic serves every `Bee.UI.*` front end.
- **Pluggable endpoint / API key storage** -- hosts override `ClientInfo.EndpointStorage` and
  `ClientInfo.ApiKeyStorage` with platform-appropriate implementations.
- **Async-friendly initialization** -- `InitializeAsync` / `SetEndpointAsync` validate the endpoint
  and initialize the connector without blocking, so they are safe on single-threaded runtimes
  (browser WASM).
- **Nullable reference types** enabled (`<Nullable>enable</Nullable>`).

## Directory Structure

```
Bee.UI.Core/
  ClientInfo.cs          # Client-side connection state and connector factory
  IEndpointStorage.cs    # Endpoint persistence contract
  EndpointStorage.cs     # Default ClientSettings-backed implementation
  IApiKeyStorage.cs      # API key persistence contract
  ApiKeyStorage.cs       # Default ClientSettings-backed implementation
  IUIViewService.cs      # Host-supplied view services
  VersionInfo.cs         # Package version metadata
  Permissions/           # ElementCapabilityResolver, FieldCapability,
                         # IElementCapabilityResolver
```
