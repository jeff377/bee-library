# Web.Js.Demo

[繁體中文](README.zh-TW.md)

Demonstrates calling the Bee.NET JSON-RPC API from pure JavaScript in a browser —
no `npm`, no build step, no framework. The JS frontend uses
`PayloadFormat.Plain` (the wire format opened up by the JSON-RPC frontend
integration plan), so all requests are plain JSON.

Covers all 7 methods downgraded for JS access in the plan, plus
schema-driven UI rendering using the JSON-native FormSchema / FormLayout
endpoints:

| Section | Methods |
|---------|---------|
| Login | `System.Login` |
| Ping | `System.Ping` (no auth) |
| Enter Company | `System.EnterCompany` / `System.LeaveCompany` (error path — see UI hint) |
| Employee CRUD | `Employee.GetList` / `GetData` / `GetNewData` / `Save` / `Delete` |
| FormDefinition-driven rendering | `System.GetFormSchema` / `System.GetFormLayout` → dynamic form |
| Logout | `System.Logout` |

## How to run

1. Start the backend (in another terminal):

   ```sh
   cd samples/QuickStart.Server
   dotnet run
   ```

   The server listens on `http://localhost:5050` and has CORS enabled for any
   origin (demo-only — production hosts must restrict origins).

2. Open the demo in a browser. Two options:

   - **Direct file open** — `open samples/Web.Js.Demo/index.html` (Mac) or
     equivalent on Windows / Linux. Modern browsers accept `file://` as an
     origin against an `AllowAnyOrigin` CORS policy.

   - **Static file server** — useful if your browser blocks `file://` for
     `fetch`:

     ```sh
     # Python 3
     cd samples/Web.Js.Demo
     python3 -m http.server 8080
     # or, if installed
     dotnet serve -p 8080
     ```

     Then open `http://localhost:8080/index.html`.

3. Click **Login** (default credentials `demo` / `demo`), then **Ping** —
   each result shows up in the output panel at the bottom.

## Files

| File | Purpose |
|------|---------|
| `index.html` | Minimal UI — Login form, CRUD buttons, dynamic form area, result panel. Vanilla CSS, no external dependencies. |
| `bee-api-client.js` | ES module: `rpcCall(method, value)` + `systemApi.*` + `formApi(progId)` + `RpcError` + token state. |
| `form-renderer.js` | ES module: takes a `FormLayout` JSON tree and produces a working HTML form (CSS Grid, control-type dispatch). Exposes `bindDataSet` / `collectDataSet` for two-way data binding. |
| `app.js` | UI event wiring; depends on `bee-api-client.js` and `form-renderer.js`. |
| `.smoke.yaml` | Config for the `demo-smoke` skill — launches both prerequisite servers and verifies the page loads with the expected section text. See file header for the browser-tier limitation (clicks blocked → load-only smoke). |

## Headers used

| Header | Value | Notes |
|--------|-------|-------|
| `Content-Type` | `application/json` | JSON-RPC body |
| `X-Api-Key` | `quickstart-demo` (hard-coded) | Default `ApiAuthorizationValidator` only requires the value to be non-empty. Production hosts must register a stricter validator. |
| `Authorization` | `Bearer <accessToken>` | Sent only after `Login` returns an AccessToken. Required for any method whose `[ApiAccessControl]` is `Authenticated`. |

## FormDefinition-driven rendering

Section 6 in the UI demonstrates the end-to-end JSON path for schema-driven
forms — the typical workflow a React / Vue / Angular app would build on top of:

```
GetFormSchema + GetFormLayout  (parallel) → FormLayout JSON
        ↓
renderFormLayout(layout, container)        ← produces a working HTML form
        ↓
GetNewData / GetData                        → DataSet JSON
        ↓
controller.bindDataSet(dataSet)             ← fills the form
        ↓
controller.collectDataSet()                 ← extracts form state, marks RowState=Modified
        ↓
Save                                        → server returns refreshed DataSet
```

**Design notes:**

- **No client-side validation.** The renderer trusts `Save` to fail with an
  `RpcError` and surfaces the message in the output panel. A real app may add a
  small validation layer driven by `FormSchema` field metadata (`allowNull`,
  `maxLength` etc.) — out of scope for this demo.
- **Detail tables are read-only.** Renders `LayoutGrid` as a plain `<table>`;
  adding / editing / deleting detail rows would need a more elaborate UI.
- **CSS Grid for layout.** `LayoutField.rowSpan` / `columnSpan` map directly to
  `grid-row: span N` / `grid-column: span N`, so multi-column forms work
  without a layout library.

**Porting to React / Vue / Angular:**

- Replace `renderFormLayout`'s direct DOM construction with your framework's
  component tree (a `<FormLayout layout={…} />` component that maps each
  section/field to a child component).
- Keep the control-type → component map (`TextEdit` → `<TextField>`, etc.) —
  this is the bit that varies per framework.
- Reuse `bindDataSet` / `collectDataSet` as pure-data helpers; they have no
  DOM dependency apart from the field-name → control lookup, which becomes a
  ref or state-binding in framework land.

## Why pure JavaScript

The whole point of opening up the Plain wire format is so JS frontends can talk
to the Bee.NET backend without dragging in `Microsoft.JSInterop`, MessagePack,
or the AES-CBC-HMAC pipeline. Keeping the demo dependency-free demonstrates the
minimum surface a real JS framework integration (React / Vue / Angular) needs to
build on.

If your project already uses a TypeScript toolchain, copy `bee-api-client.js`
into your `src/` and rewrite types as needed — the API surface is small enough
that hand-maintained TypeScript interfaces are cheaper than codegen at this
stage.

## Related

- Plans:
  - [plan-jsonrpc-frontend-integration](../../docs/plans/plan-jsonrpc-frontend-integration.md)
  - [plan-jsonrpc-formschema-formlayout](../../docs/plans/plan-jsonrpc-formschema-formlayout.md)
  - [plan-web-js-demo-formdef-rendering](../../docs/plans/plan-web-js-demo-formdef-rendering.md)
- Integration guide: [docs/jsonrpc-frontend-integration.md](../../docs/jsonrpc-frontend-integration.md)
- Backend host: [samples/QuickStart.Server](../QuickStart.Server/)
- Demo credentials: [samples/Bee.Samples.Shared/DemoCredentials.cs](../Bee.Samples.Shared/DemoCredentials.cs)
