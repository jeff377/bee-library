# Bee.Northwind

[繁體中文](README.zh-TW.md)

A demonstration — the classic **Northwind** inventory business case — built on the [Bee.NET](../../README.md) framework to show how an application is assembled from definitions. It exists to make one argument concrete:

> **A new screen with full create / read / update / delete, list browsing, and cross-table lookups is a few XML definition files — not UI code, not CRUD code, not SQL.**

Nine forms, master-detail orders with three lookups, framework organization tables, and exactly one hand-written business object (the order rules) — everything else is definitions. The closing chapter walks you through adding a tenth form in about thirty minutes, writing zero code.

## What it demonstrates

- **Definition-driven CRUD** — `FormSchema` is the single source of truth that drives the UI form, the list view, the database table, and the validation surface.
- **Cross-table lookups with zero code** — a relation field plus a field-mapping in XML gives you a pick dialog, the foreign key, and the denormalized display columns (re-derived by a server JOIN on reload).
- **Master-detail documents** — orders carry a detail grid with per-row product lookup, saved and reloaded as one unit.
- **The pro-code boundary, drawn honestly** — order numbering, status transitions, required-field validation, and amount calculation are the *only* C# in the app, in one `OrderBO`. The README's comparison table shows exactly which behavior is definition, which is framework, and which is application code.
- **Framework system tables (`st_`) alongside business tables (`ft_`)** — `Employee` / `Department` are framework tables the app reuses and extends; `Customer` / `Product` / `Order` are business tables the app defines.

## Running the demo

Requires the **.NET 10 SDK**. The database is SQLite, created and seeded on first run — no setup.

### From VS Code (recommended)

Open the repository, pick **Run Bee.Northwind (Server + Desktop)** from the Run & Debug dropdown, and press <kbd>F5</kbd>. It builds and launches the JSON-RPC server and the desktop client together.

### From the command line

Two terminals from the repository root:

```bash
# 1. Backend (JSON-RPC on http://localhost:5100)
dotnet run --project apps/Bee.Northwind/Bee.Northwind.Server

# 2. Desktop client
dotnet run --project apps/Bee.Northwind/Bee.Northwind.Desktop
```

Then in the app: **Connect** (the endpoint is pre-filled) → **Sign in** with `demo` / `demo`.

> The first server run creates `northwind.db` next to the server project and seeds a Northwind subset. Delete that file to reseed from scratch.

## The forms

| Menu | ProgId | Table | Layer | Highlights |
|------|--------|-------|-------|-----------|
| Categories | `Category` | `ft_category` | business | plain master, zero code |
| Suppliers | `Supplier` | `ft_supplier` | business | plain master |
| Customers | `Customer` | `ft_customer` | business | plain master |
| Shippers | `Shipper` | `ft_shipper` | business | plain master |
| Products | `Product` | `ft_product` | business | **two lookups** (Supplier + Category) |
| Departments | `Department` | `st_department` | framework system | reused framework table |
| Employees | `Employee` | `st_employee` | framework system + extension | framework fields + `title` / `hire_date`; `dept` lookup carries the department manager as supervisor |
| Orders | `Order` | `ft_order` + `ft_order_detail` | business (master-detail) | **three master lookups** (Customer / Employee / Shipper) + per-row **product lookup**; the one `OrderBO` |

## Framework system tables vs business tables (`st_` / `ft_`)

The table prefix records **who owns the table, not which database it lives in**:

- **`st_` — framework / system tables.** Shipped by the framework, shared across applications, relied on by framework features (permissions, organization). `Employee` (`st_employee`) and `Department` (`st_department`) are framework tables. The app copies their definitions from the framework defaults into its own `Define/` (the same way a new project is scaffolded), keeps the standard fields, and *extends* them — `Employee` adds `title` and `hire_date`.
- **`ft_` — business tables.** Defined by this application: `Category`, `Supplier`, `Customer`, `Shipper`, `Product`, `Order`, `Order Details`.

`Order → Employee` is the interesting cross-layer edge: a business table (`ft_order`) points at a framework system table (`st_employee`) — the salesperson on an order is a framework employee.

### Which database (`common` vs `company`)

The prefix says who *owns* a table; a separate axis says which *database* it lives in. A `FormSchema`'s `CategoryId` selects the database scope:

- **`company`** — per-company business data. **All of this demo's data is company data**: the `ft_` tables *and* the org tables `st_department` / `st_employee` (an application's employees belong to that company). The router resolves company scope through the session's company to the company database.
- **`common`** — cross-company shared framework tables (sessions, the cache-notify signal). Not application data.

This demo is single-company, so it auto-enters one fixed company at login (`NorthwindCompanyInfoService` + a `CompanyId` stamped on the session — the company-context analogue of the hard-coded login) and points both the `common` and `company` databases at the same `northwind.db` file. A real multi-company deployment would give each company its own database and enter it through the full `EnterCompany` flow.

## Northwind → bee model mapping

Northwind is a normalized relational schema; bee is a `sys_rowid` (Guid) relation model. The demo borrows Northwind's business case and data, but the keys and relations follow bee conventions:

| Northwind | bee convention |
|-----------|----------------|
| text / int primary key (`CustomerID='ALFKI'`, `ProductID=17`) | `sys_id` (string business code) + `sys_rowid` (Guid relation key) + `sys_no` (auto-increment) |
| name column (`CompanyName`, `ProductName`) | `sys_name` |
| foreign key (`Orders.CustomerID`) | `customer_rowid` (Guid) + `RelationProgId="Customer"` + field mappings that fill `ref_customer_id` / `ref_customer_name` |
| composite-key detail (`Order Details`: OrderID+ProductID) | `sys_rowid` PK + `sys_master_rowid` (→ Order) + `product_rowid` (lookup → Product) + quantity / price |
| employees | framework `st_employee`: framework fields + Northwind data columns; the manager comes from the department, not a `ReportsTo` self-relation |

## What is definition, what is framework, what is application code

This is the whole argument in one table.

| Behavior | Source | Where |
|----------|--------|-------|
| Form layout, field editors, labels | **definition** | `FormSchema` (layout auto-generated by the framework) |
| List columns and browsing | **definition** | `FormSchema.ListFields` |
| Database table + indexes | **definition** | `TableSchema` |
| Insert / update / delete dispatch | **framework** | `FormBusinessObject` + repository |
| Lookup dialog, foreign key write-back, JOIN reload | **definition + framework** | relation field + `RelationFieldMappings`; framework `GetLookup` |
| Master-detail save as one unit | **framework** | repository, driven by the multi-table `FormSchema` |
| Navigation menu (grouped form list) | **definition** | `ProgramSettings.xml` |
| Login / session / encryption | **framework** | `SystemBusinessObject`, API pipeline |
| **Order number, status transitions, validation, amounts** | **application code** | `OrderBO` (the only business logic in the app) |

The single C# business object, [`OrderBO`](Bee.Northwind.Server/BusinessObjects/OrderBO.cs), overrides `Save` / `GetNewData` to add what a generic form cannot express. Its pure rules are factored into [`OrderRules`](Bee.Northwind.Server/BusinessObjects/OrderRules.cs) and [`OrderDataSet`](Bee.Northwind.Server/BusinessObjects/OrderDataSet.cs), kept free of database dependencies and separate from the orchestration.

## Closing chapter: add a Region form in 30 minutes, with zero code

Northwind has a `Region` table the demo leaves out — so you can add it yourself. You will write **three XML files and one menu line, all definitions, no code**, then restart and get a fully working CRUD screen.

### 1. The table — `Define/TableSchema/company/ft_region.TableSchema.xml`

A region is business data, so it goes in the **company** category (`TableSchema/company/`), alongside the other `ft_` tables.

```xml
<?xml version="1.0" encoding="utf-8"?>
<TableSchema xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" TableName="ft_region" DisplayName="Region">
  <Fields>
    <DbField FieldName="sys_no" Caption="Sequence" DbType="AutoIncrement" />
    <DbField FieldName="sys_rowid" Caption="Row ID" DbType="Guid" />
    <DbField FieldName="sys_id" Caption="Region Code" DbType="String" Length="20" />
    <DbField FieldName="sys_name" Caption="Region Name" DbType="String" Length="50" />
  </Fields>
  <Indexes>
    <DbTableIndex Name="pk_{0}" Unique="true" PrimaryKey="true">
      <IndexFields><IndexField FieldName="sys_no" /></IndexFields>
    </DbTableIndex>
    <DbTableIndex Name="rx_{0}" Unique="true">
      <IndexFields><IndexField FieldName="sys_rowid" /></IndexFields>
    </DbTableIndex>
    <DbTableIndex Name="uk_{0}" Unique="true">
      <IndexFields><IndexField FieldName="sys_id" /></IndexFields>
    </DbTableIndex>
  </Indexes>
</TableSchema>
```

### 2. The form — `Define/FormSchema/Region.FormSchema.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<FormSchema xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" ProgId="Region" DisplayName="Region" CategoryId="company" ListFields="sys_id,sys_name">
  <Tables>
    <FormTable TableName="Region" DbTableName="ft_region" DisplayName="Region">
      <Fields>
        <FormField FieldName="sys_no" Caption="Sequence" DbType="AutoIncrement" Visible="false" />
        <FormField FieldName="sys_rowid" Caption="Row ID" DbType="Guid" Visible="false" />
        <FormField FieldName="sys_id" Caption="Region Code" DbType="String" />
        <FormField FieldName="sys_name" Caption="Region Name" DbType="String" />
      </Fields>
    </FormTable>
  </Tables>
</FormSchema>
```

There is no `FormLayout` file to write — the framework generates the layout from the `FormSchema` at delivery time.

### 3. Register the table — add to the company category in `Define/DbCategorySettings.xml`

```xml
<TableItem TableName="ft_region" DisplayName="Region" />
```

This is what makes the seeder build the table on the next start (it builds every table registered here, into the database the category maps to).

### 4. Put it on the menu — add to `Define/ProgramSettings.xml`

```xml
<ProgramItem ProgId="Region" DisplayName="Regions" />
```

`ProgramSettings.xml` *is* the app's program list — the navigation menu is built from it, so a new entry appears in the left menu. (No `BusinessObject` attribute means it uses the framework's default CRUD.)

### 5. Restart

Restart the server (it creates `ft_region`) and the desktop client. **Regions** is now in the left menu under Master Data, with working list, new, edit, delete, and a unique-code check from the `uk_` index — all from four definition edits, no compilation of your own code.

## Project layout

```
apps/Bee.Northwind/
├── Define/                       definitions — the source of truth (no project, read by the server)
│   ├── FormSchema/               one form per file
│   ├── TableSchema/company/      business tables (company/ + common/ for framework)
│   ├── DatabaseSettings.xml      the common + company databases
│   ├── DbCategorySettings.xml    which tables exist, per category (drives schema build)
│   └── ProgramSettings.xml       the program list (drives the menu + BO binding)
├── Bee.Northwind.Server/         JSON-RPC backend, OrderBO, JSON seed data
├── Bee.Northwind.UI/             Avalonia shared UI (views, view models, navigation)
└── Bee.Northwind.Desktop/        desktop entry point
```

The detailed plan and the running framework-feedback log live in [`docs/plans/plan-bee-northwind-demo.md`](../../docs/plans/plan-bee-northwind-demo.md).
