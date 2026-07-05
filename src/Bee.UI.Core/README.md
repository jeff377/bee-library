# Bee.UI.Core

> Shared client-side foundation for the `Bee.UI.*` front-end family (Avalonia / MAUI / Blazor): connection state, API connectors, endpoint persistence, and client-side permission capability resolution.

[繁體中文](README.zh-TW.md)

## Architecture Position

- **Layer**: UI Layer (shared client foundation)
- **Downstream** (dependents): `Bee.UI.Avalonia`, `Bee.UI.Maui`, and the Blazor front ends
- **Upstream** (dependencies): `Bee.Api.Client`

## Target Framework

- `net10.0` -- access to modern runtime APIs and performance improvements

## Overview

`Bee.UI.Core` is the framework-agnostic base every `Bee.UI.*` front end builds on. It holds the
per-process client connection state (`ClientInfo`), abstracts where the service endpoint is
persisted (`IEndpointStorage`), and turns a server-issued permission snapshot into per-element UI
capability decisions (`ElementCapabilityResolver`). It carries no UI-framework types, so Avalonia,
MAUI, and Blazor can each share the same connection and permission logic while rendering their own
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

### Endpoint Persistence

- `IEndpointStorage` -- persistence contract for the configured service endpoint
  (`LoadEndpoint` / `SetEndpoint` / `SaveEndpoint`).
- `EndpointStorage` -- default implementation, backed by `ClientInfo.ClientSettings`
  (`{ExeName}.Settings.xml`). Front ends assign `ClientInfo.EndpointStorage` to a platform-specific
  implementation when the default file location is unsuitable -- e.g. `FileEndpointStorage`
  (in `Bee.UI.Avalonia`) or the sandbox-friendly `MauiPreferenceEndpointStorage` (in `Bee.UI.Maui`).

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
- **Pluggable endpoint storage** -- hosts override `ClientInfo.EndpointStorage` with a
  platform-appropriate `IEndpointStorage` implementation.
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
  IUIViewService.cs      # Host-supplied view services
  VersionInfo.cs         # Package version metadata
  Permissions/           # ElementCapabilityResolver, FieldCapability,
                         # IElementCapabilityResolver
```
