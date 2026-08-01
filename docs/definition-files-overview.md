# Definition Files Overview

[繁體中文](definition-files-overview.zh-TW.md) · [← Docs Index](README.md)

> The map of every definition file: what each one owns, how they connect, and what changing one affects. This page is the orientation layer — each entry links to the document that covers it in depth.

Bee.NET is definition-driven: the XML under your `DefinePath` is not configuration bolted onto an application, it *is* the application's structure. The framework reads it to build SQL, render UI, enforce permissions and localise text.

---

## 1. The Full Set

Eleven definition types, enumerated as `DefineType` and all reached through `IDefineAccess`. Seven are single files at the root of `DefinePath`; four are keyed and live in subfolders.

| Definition | File path under `DefinePath` | Owns | Read in depth |
|------------|------------------------------|------|---------------|
| **FormSchema** | `FormSchema/{progId}.FormSchema.xml` | The definition hub: fields, types, relations, master-detail structure, computed fields and rules | [Architecture Overview](architecture-overview.md) |
| **TableSchema** | `TableSchema/{categoryId}/{tableName}.TableSchema.xml` | The physical table: columns, types, lengths, nullability, indexes | [Schema Upgrade](database-schema-upgrade.md) |
| **FormLayout** | `FormLayout/{layoutId}.FormLayout.xml` | How the form is arranged on screen | [Architecture Overview](architecture-overview.md) |
| **Language** | `Language/{lang}/{namespace}.Language.xml` | Localised captions and enum entries, one file per namespace × language | — |
| **SystemSettings** | `SystemSettings.xml` | Process-wide settings: master key source, payload options, debug mode | [Development Cookbook](development-cookbook.md) |
| **DatabaseSettings** | `DatabaseSettings.xml` | Physical databases and their connection strings | [Database Settings Guide](database-settings-guide.md) |
| **DbCategorySettings** | `DbCategorySettings.xml` | Which logical category each table belongs to, and which database serves it | [Database Settings Guide](database-settings-guide.md) |
| **ProgramSettings** | `ProgramSettings.xml` | The program list: progId → custom business object binding, and the navigation menu | — |
| **PermissionModels** | `PermissionModels.xml` | Permission model registry: models, actions and record-scope strategies | [Permission & Authorization](permission-authorization.md) |
| **CurrencySettings** | `CurrencySettings.xml` | Currency master: per-currency decimals and natural minor units | [Development Cookbook](development-cookbook.md) |
| **UnitSettings** | `UnitSettings.xml` | Unit-of-measure master: display decimals per unit | [Development Cookbook](development-cookbook.md) |

## 2. FormSchema Is the Hub

One `FormSchema` drives three layers at once. This is the single most important relationship in the framework:

```text
                    ┌──────────────────┐
                    │   FormSchema     │  fields · types · relations
                    │   {progId}       │  master-detail · rules
                    └────────┬─────────┘
             ┌───────────────┼───────────────┐
             ▼               ▼               ▼
      ┌────────────┐  ┌────────────┐  ┌──────────────┐
      │ FormLayout │  │ TableSchema│  │ Rules /      │
      │  (UI)      │  │ (database) │  │ Expressions  │
      └────────────┘  └────────────┘  └──────────────┘
       how it looks    where it lives   what's valid
```

- **Against the database**: the framework generates SQL per FormSchema at runtime — no ORM, no generated entity classes. See [FormMap](formmap.md).
- **Against the UI**: `FormLayout` arranges the fields a FormSchema declares; controls read the field metadata (max length, list items, read-only, relation → lookup) directly.
- **Against validation**: computed fields and `FormRule` entries live inside the FormSchema itself. See [Expressions and Rules](expression-rules.md).

The practical consequence: **ordinary CRUD requires no code**. A FormSchema, its TableSchema, a `DbCategorySettings` entry and a `ProgramSettings` item are a working form.

## 3. The Startup Trio

Three settings files are read in a fixed order during host startup, and each depends on the one before it:

```text
SystemSettings.xml          ──▶ SysInfo.Initialize + ApiServiceOptions.Initialize
   (master key, payload)         (process-wide state)
        │
        ▼
DatabaseSettings.xml        ──▶ physical databases + connection strings
   (referenced by id)            (decrypted using the master key)
        │
        ▼
DbCategorySettings.xml      ──▶ table → category → database resolution
   (common / company / log)
```

`SystemSettings` must load before anything else because the master key it names is what decrypts the connection strings in `DatabaseSettings`. See [Development Cookbook § Framework Initialization Order](development-cookbook.md#framework-initialization-order) for the full sequence, and [Development Constraints](development-constraints.md) for what breaks when the order is violated.

### Category is a scope selector, not a free string

`CategoryId` accepts exactly three values, and picking the wrong one is the most common setup mistake:

| Category | Meaning |
|----------|---------|
| `common` | Cross-company framework tables (sessions, cache notifications, users) |
| `company` | Per-company data — **all business tables belong here**, as do the application organisation tables |
| `log` | Log and audit tables |

A table prefix (`st_` / `ft_`) indicates *who owns* the table; the category indicates *where the data lives*. They are independent axes. See [Database Settings Guide](database-settings-guide.md) and [Framework-Reserved Names](framework-reserved-names.md).

## 4. ProgramSettings Does Double Duty

`ProgramSettings.xml` is both a routing table and a menu source:

```xml
<ProgramCategory Id="transactions" DisplayName="Transactions">
  <Items>
    <ProgramItem ProgId="Customer" DisplayName="Customers" />
    <ProgramItem ProgId="Order" DisplayName="Orders"
                 BusinessObject="MyApp.Server.BusinessObjects.OrderBO, MyApp.Server" />
  </Items>
</ProgramCategory>
```

- **`BusinessObject` empty** → the progId resolves to the framework's default `FormBusinessObject`, i.e. pure definition-driven CRUD.
- **`BusinessObject` set** → that type handles the progId, for the cases declarations cannot express (cross-row aggregation, database lookups).
- **The categories and items** are also what a shell builds its navigation menu from.

Adding a form to a running application is therefore four XML edits and no code.

## 5. Change One, Change What Else

| You changed | Also update |
|-------------|-------------|
| Added a field to a **FormSchema** | The matching **TableSchema** column, then run a [schema upgrade](database-schema-upgrade.md); add it to the **FormLayout** if it should be visible; add its caption to **Language** |
| Added a **new form** | **FormSchema** + **TableSchema** + a table entry in **DbCategorySettings** + a `ProgramItem` in **ProgramSettings** |
| Added a **table** | Its **TableSchema** must sit in the `TableSchema/{categoryId}/` folder matching its `DbCategorySettings` category — the folder name *is* the category |
| Added a **database** | **DatabaseSettings** entry first, then point a category at it in **DbCategorySettings** |
| Changed a **currency or unit precision** | **CurrencySettings** / **UnitSettings**; field-level rounding follows `NumberKind`, not the raw column type |
| Added a **permission-controlled action** | **PermissionModels**, then the relevant `FormField.ScopeRole` entries — see [Permission & Authorization](permission-authorization.md) |

## 6. `DefinePath` and the `Defaults/` Scaffold

Two things that are easy to conflate:

- **`DefinePath`** is what the runtime reads. It is the only source of definitions at runtime.
- **`Defaults/`**, embedded in `Bee.Definition.dll`, is a **scaffold source** for starting a new project. `dotnet bee defines materialize` copies it into your `DefinePath` once.

> **There is no fallback.** If a definition is missing from `DefinePath`, the framework does **not** fall back to `Defaults/`. To use a framework system table in your project, materialise its definition into your `DefinePath` and extend from there — keeping the framework's standard fields, which the permission and organisation features depend on.

### Definitions are immutable after initialisation

Everything obtained through `IDefineAccess.GetX(...)` is a **process-wide cached instance** shared by every session. Mutating one at runtime leaks across sessions. Clone before modifying, and persist changes through `IDefineAccess.SaveX(...)`, which writes to storage and invalidates the cache slot.

See [Development Constraints § Definition Data Immutability After Init](development-constraints.md) for the full rule.

### Storage is pluggable

The file layout above is the default (`FileDefineStorage`). Definitions can also live in a database — see [ADR-018](adr/adr-018-db-define-storage.md). `IDefineAccess` is the same either way; only the backing store changes.

## 7. `CustomizePath` and the Tenant Customization Overlay

`DefinePath` holds the base definitions every tenant shares. `CustomizePath` is the optional second root that lets one company override parts of them without forking the base — see [ADR-016](adr/adr-016-multitenant-customization-overlay.md) for the design.

### Turning it on

The host computes it and hands it to `AddBeeFramework` alongside `DefinePath`. There is no configuration binding — `PathOptions` is constructed by the host, exactly as `DefinePath` always has been:

```csharp
var paths = new PathOptions
{
    DefinePath = definePath,
    CustomizePath = Path.Combine(deployRoot, "Customize"),
};
builder.Services.AddBeeFramework(settings.BackendConfiguration, paths);
```

**An empty `CustomizePath` disables the overlay entirely** — every consumer resolves against the base layer, bit for bit as if the feature did not exist. That is the default. `samples/Bee.Samples.Shared/DemoBackend.cs` shows the wiring.

### Layout

```
{CustomizePath}/{customizeId}/ProgramSettings.xml
{CustomizePath}/{customizeId}/FormLayout/{layoutId}.FormLayout.xml
{CustomizePath}/{customizeId}/Language/{lang}/{namespace}.Language.xml
```

The directory need not exist. A tenant that supplies no file for a given lookup falls back to the base layer.

### Only three types, at three granularities

| Type | Overlay granularity |
|------|--------------------|
| **LanguageResource** | **Per key** for text (`LanguageItem`). The customization file holds only the keys it changes; every other key comes from base — so a base translation added later propagates automatically. **A `LanguageEnum` is the exception: whole-enum.** A customization enum of the same name replaces the base one outright, so it must list every entry the option set should have |
| **ProgramSettings** | **Per progId.** A customization entry wins over the base entry of the same progId |
| **FormLayout** | **Whole file.** A customization layout replaces the base layout for that `layoutId` |

The granularities differ on purpose, and the dividing line is whether the artifact is a bag of independent values or a single composed whole.

Text keys are independent: "this label reads differently here" leaves every other key alone, so merging key by key is both cheap and obvious. A layout is one visual arrangement — sections, ordering, column spans and nesting only make sense together, and a partial merge would raise questions ("this section moved — do the fields under it follow?") with no intuitive answer. An enum sits on the layout side of that line rather than the text side: it is an ordered option set, where merging entry by entry would leave both the ordering and the meaning of an omitted entry ambiguous.

So for layouts and enums, a tenant that customizes one owns it whole, and a tenant that does not gets the base version untouched.

Owning it whole cuts both ways: a field added to the base `FormSchema` later **does not** appear on a tenant that has customized that layout, and the framework neither merges it in nor warns about the difference. This is the intent rather than a limitation — **the layout is the authority on what the screen shows**, and a schema gaining a field is not a statement that every tenant's form should now display it. Putting the new field on that tenant's form is a decision, and it is made by editing that tenant's layout file.

> **FormSchema and TableSchema are permanently excluded.** Both drive the database schema and the validation rules as well as the UI; letting them diverge per tenant would split the physical schema. This is a decision, not a gap — see ADR-016.

> The overlay is **read-only**. Customization files are produced by deployment tooling; every `SaveXxx` on the override layer throws.

### Where `customizeId` comes from

`CompanyInfo.CustomizeId` (column `st_company.customize_id`) is copied onto `SessionInfo.CustomizeId` when the session enters a company, and cleared on leave / logout. Server-side consumers read it from `SessionInfo` and nowhere else.

Two consequences worth planning around:

- **Nothing is customized before `EnterCompany`.** The login screen, the company picker, and every message on the way there resolve against the base layer, because there is no `CustomizeId` yet.
- **`SessionInfo.CustomizeId` is a snapshot, not a live value.** It is copied at the moment the session enters the company, the same as roles and the employee context. Editing `st_company.customize_id` afterwards does not move existing sessions — they pick the new value up on the next `EnterCompany`.

> **Security boundary:** the server never accepts a `customizeId` supplied by a client as the lookup key — doing so would let a caller choose which tenant's customization to read. Clients receive their own `CustomizeId` from `EnterCompany` for their own UI localization only; the server always reads `SessionInfo.CustomizeId`.

---

## Where to go next

| You want to | Read |
|-------------|------|
| See how these pieces form an architecture | [Architecture Overview](architecture-overview.md) |
| Follow the full definition → API flow | [Development Cookbook](development-cookbook.md) |
| Understand SQL generation from a FormSchema | [FormMap](formmap.md) |
| Compute and validate fields declaratively | [Expressions and Rules](expression-rules.md) |
| Know which names the framework owns | [Framework-Reserved Names](framework-reserved-names.md) |
| Set up databases and categories | [Database Settings Guide](database-settings-guide.md) |
