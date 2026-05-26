# Blazor.Server.Demo

**English** | [繁體中文](README.zh-TW.md)

A Blazor Server host that demonstrates how to wire `Bee.Web.Blazor.Server` components into ASP.NET Core and dispatch directly into the Bee backend via the **in-process `LocalApiProvider`** (same process, no HTTP round-trip).

## How to run

```bash
cd samples/Blazor.Server.Demo
dotnet run
# Browser opens http://localhost:5055 automatically
```

On first run:

1. Reads the master key from `BEE_MASTER_KEY`; `DemoBackend.AddBeeBackend` auto-injects a hard-coded demo value when the variable is unset (production hosts must override — see [`samples/README.md`](../README.md#master-key))
2. Creates `samples/Blazor.Server.Demo/quickstart.db` (SQLite) with `ft_employee` + `ft_employee_phone`
3. Seeds 3 demo employees (Alice / Bob / Carol)

## What you'll see

1. Landing page shows a **Sign in** panel pre-filled with the `demo / demo` hint
2. Click Sign in (sends `SystemApiConnector.LoginAsync`, handled by `DemoAuthenticatingSystemBusinessObject`)
3. After a successful login, `<FormPage ProgId="Employee" />` renders:
   - Top toolbar: `New` / `Save` / `Delete`
   - Middle: employee grid (`DynamicGrid`, columns from `FormSchema.ListFields`)
   - Bottom: edit form (`DynamicForm`) appears when a row is selected
4. Click `New` → edit fields → `Save`; the new employee appears in the grid

## What this maps to in the library

| Demo behavior | Library component |
|---------------|-------------------|
| Login form | `BeeLoginPanel` (Phase 1d) |
| AccessToken cascading | `BeeAccessTokenProvider` (Phase 1d) |
| Employee grid rendering | `DynamicGrid` + `FormSchema.ListLayout` |
| Employee edit form | `DynamicForm` + `FormSchema.FormLayout` |
| Grid + form integration | `FormPage` |
| CRUD through Bee | `FormDataObject.LoadAsync / SaveAsync / NewAsync / DeleteAsync` |
| Local in-process dispatch | `BeeApiConnectorFactory.UseLocalProvider` |
| In-process JSON-RPC | `LocalApiProvider` → `JsonRpcExecutor` → `FormBusinessObject` |

## Simplifications vs production

- **`DemoAuthenticatingSystemBusinessObject`** matches credentials with a hard-coded `demo/demo` comparison and does not query the `st_user` table — so this demo needs **no system tables** (`st_user` / `st_session` / `st_company` / `st_user_company`)
- **Single process sharing `ApiClientInfo.LocalServiceProvider` and `ApiClientInfo.ApiEncryptionKey`**: concurrent logins from multiple users would clobber each other's keys. The demo is fine for a single user at a time; production needs a per-connection scheme
- SQLite is a single file (`quickstart.db`), same as `QuickStart.Server`
