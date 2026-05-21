# Bee.UI.Core

[繁體中文](README.zh-TW.md)

[![NuGet](https://img.shields.io/nuget/v/Bee.UI.Core.svg)](https://www.nuget.org/packages/Bee.UI.Core/)
[![Build CI](https://github.com/jeff377/bee-ui-core/actions/workflows/build-ci.yml/badge.svg)](https://github.com/jeff377/bee-ui-core/actions/workflows/build-ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.txt)

Client-side bridging layer for [Bee.NET](https://github.com/jeff377/bee-library) UI applications. `Bee.UI.Core` wraps `Bee.Api.Client` connectors with a single static entry point (`ClientInfo`) that manages connection settings, login state, and definition-data access — letting WinForms / WPF / Avalonia hosts focus on views instead of plumbing.

Targets **`net10.0`**.

## ✨ Features

- **One-line bootstrap**: `ClientInfo.Initialize(...)` resolves the saved endpoint, validates it, falls back to a connection setup view when missing, and primes the API connector.
- **Local & remote connection abstraction**: Switch between local (file-based define path) and remote (JSON-RPC URL) by changing the endpoint string; the rest of the API surface is unchanged.
- **Connector caching**: `SystemApiConnector` and `IDefineAccess` are cached per token; resetting the access token automatically invalidates them.
- **Pluggable persistence**: Replace the default `EndpointStorage` (XML-backed `<exe>.Settings.xml`) with your own `IEndpointStorage` implementation.
- **Host-supplied UI**: Connection-settings dialogs are abstracted behind `IUIViewService`, leaving the choice of UI framework to the consumer.

## 📦 Installation

```bash
dotnet add package Bee.UI.Core
```

Or in `.csproj`:

```xml
<PackageReference Include="Bee.UI.Core" Version="4.3.0" />
```

This package depends on:

| Package | Version |
|---|---|
| `Bee.Api.Client` | `4.3.0` |

(Transitively pulls in `Bee.Base`, `Bee.Definition`, `Bee.Api.Core`, `Bee.Api.Contracts`.)

## 🚀 Quick Start

### 1. Implement the host UI service

`IUIViewService` is invoked when the saved endpoint is missing or unreachable, prompting the user to configure a new one.

```csharp
using Bee.UI.Core;

public class MainFormUIViewService : IUIViewService
{
    public bool ShowApiConnect()
    {
        using var dialog = new ApiConnectDialog();
        return dialog.ShowDialog() == DialogResult.OK;
    }
}
```

### 2. Initialize on startup

```csharp
using Bee.UI.Core;
using Bee.Api.Client;

// At application startup:
if (!ClientInfo.Initialize(new MainFormUIViewService(), SupportedConnectTypes.Both))
{
    // User cancelled the connection setup — exit.
    return;
}
```

Or directly with a known endpoint (no UI fallback):

```csharp
ClientInfo.Initialize("http://localhost:5000/jsonrpc/api");
```

### 3. Apply the login result

After a successful login through `SystemApiConnector`, hand the response to `ClientInfo` to populate the access token and user info:

```csharp
var response = await ClientInfo.SystemApiConnector.LoginAsync(userId, password);
ClientInfo.ApplyLoginResult(response);
```

### 4. Use the connectors

```csharp
// System-level connector (cached per token):
var system = ClientInfo.SystemApiConnector;

// Form-level connector for a specific program:
var employee = ClientInfo.CreateFormApiConnector("Employee");

// Definition data access:
var formSchema = ClientInfo.DefineAccess.GetFormSchema("Employee");
```

### 5. (Optional) Local connection: register backend services

For **remote** connections (JSON-RPC URL endpoint) the steps above are sufficient — the API connectors talk to the server over HTTP.

For **local** connections (in-process, no HTTP), the host application must build a Bee.NET service container and hand it to `ApiClientInfo.LocalServiceProvider` before `ClientInfo.Initialize(...)`. The composition root lives in the [`Bee.Hosting`](https://www.nuget.org/packages/Bee.Hosting/) package, which is a **host-side dependency** — `Bee.UI.Core` itself does not (and must not) reference it.

Install `Bee.Hosting` in the host project (WinForms / WPF / Avalonia / Console):

```bash
dotnet add package Bee.Hosting
```

Then wire it up at startup:

```csharp
using Bee.Api.Client;          // ApiClientInfo
using Bee.Hosting;             // AddBeeFramework
using Bee.UI.Core;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddBeeFramework(options =>
{
    // Configure backend options (define path, database, etc.) as needed.
});
var serviceProvider = services.BuildServiceProvider();

ApiClientInfo.LocalServiceProvider = serviceProvider;

if (!ClientInfo.Initialize(new MainFormUIViewService(), SupportedConnectTypes.Local))
{
    return;
}
```

`ClientInfo` resolves the in-process executor through `ApiClientInfo.LocalServiceProvider` without taking a direct dependency on the backend types, preserving the UI-layer / backend-layer separation.

> Remote-only hosts do **not** need `Bee.Hosting`. Skip this step if your application only connects via JSON-RPC URL.

## 🔧 Customization

### Replace the endpoint storage

The default `EndpointStorage` persists to `<ExeName>.Settings.xml` next to the executable. To use a different backend (registry, environment variable, in-memory, etc.) supply your own implementation before `Initialize`:

```csharp
public class EnvVarEndpointStorage : IEndpointStorage
{
    public string LoadEndpoint() => Environment.GetEnvironmentVariable("API_ENDPOINT") ?? "";
    public void SetEndpoint(string endpoint) { /* ... */ }
    public void SaveEndpoint(string endpoint) => Environment.SetEnvironmentVariable("API_ENDPOINT", endpoint);
}

ClientInfo.EndpointStorage = new EnvVarEndpointStorage();
ClientInfo.Initialize(uiViewService, SupportedConnectTypes.Both);
```

### Override the endpoint via command line

`Initialize(IUIViewService, SupportedConnectTypes)` parses `Key=Value` pairs from the command line. Pass `Endpoint=...` to override the saved endpoint at launch:

```bash
MyApp.exe Endpoint=http://staging.example.com/jsonrpc/api
```

The parsed dictionary is exposed through `ClientInfo.Arguments`.

## 🧩 Public API at a glance

| Member | Purpose |
|---|---|
| `ClientInfo.Initialize(IUIViewService, SupportedConnectTypes)` | Bootstrap from saved settings with UI fallback |
| `ClientInfo.Initialize(string endpoint)` | Bootstrap with an explicit endpoint |
| `ClientInfo.SetEndpoint(string)` / `GetEndpoint()` | Switch the active endpoint at runtime |
| `ClientInfo.ApplyLoginResult(LoginResponse)` | Apply the login response (token + user info) |
| `ClientInfo.SystemApiConnector` | System-level connector (auto-cached) |
| `ClientInfo.CreateFormApiConnector(progId)` | Form-level connector for a specific program |
| `ClientInfo.DefineAccess` | Remote definition-data accessor |
| `ClientInfo.UserInfo` / `AccessToken` | Authenticated user state |
| `IUIViewService` | Contract for the host application's view service |
| `IEndpointStorage` / `EndpointStorage` | Endpoint persistence contract & default impl |
| `VersionInfo` | Version / product metadata of the entry assembly |

## 🌐 Bee.NET Ecosystem

| Repository | Role |
|---|---|
| [bee-library](https://github.com/jeff377/bee-library) | Core framework (`Bee.Base`, `Bee.Definition`, `Bee.Api.*`, `Bee.Business`, …) |
| [bee-jsonrpc-sample](https://github.com/jeff377/bee-jsonrpc-sample) | End-to-end JSON-RPC server / client samples |
| **bee-ui-core** (this repo) | UI-side connection state & connector plumbing |

## 📬 Contact

[Facebook](https://www.facebook.com/profile.php?id=61574839666569) ｜ [HackMD](https://hackmd.io/@jeff377) ｜ [GitHub](https://github.com/jeff377) ｜ [NuGet](https://www.nuget.org/profiles/jeff377)

## 📄 License

[MIT](LICENSE.txt) © Bee.NET
