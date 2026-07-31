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
