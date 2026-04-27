# FormMap: FormSchema-Driven Database Access

[繁體中文](formmap.zh-TW.md)

> Form-Mapping for Definition-Driven Architecture

---

## Table of Contents

1. [In One Sentence](#1-in-one-sentence)
2. [Why It's Not an ORM](#2-why-its-not-an-orm)
3. [Core Concepts](#3-core-concepts)
4. [Examples](#4-examples)
5. [Implementation Mapping](#5-implementation-mapping)
6. [When to Use / Not Use](#6-when-to-use--not-use)
7. [Limitations and Design Tradeoffs](#7-limitations-and-design-tradeoffs)
8. [Further Reading](#8-further-reading)

---

## 1. In One Sentence

**FormMap** is the database access pattern adopted by Bee.Db: it uses `FormSchema` as the unit for describing business entities, links `FormSchema` instances through foreign-key fields (`RelationProgId`) into relation chains, and dynamically composes SELECT / INSERT / UPDATE / DELETE statements at runtime, returning data through `DataSet` — **without depending on strongly-typed entity classes**.

FormMap **is not a subset or variant of ORM**, but a parallel pattern alongside it.

---

## 2. Why It's Not an ORM

ORM (Object-Relational Mapping) addresses the impedance mismatch between **OOP object models** and the relational model. FormMap addresses the mapping between **business form models** and the relational model. Different mapping endpoints lead to different design tradeoffs.

### 2.1 Key Differences

| Aspect | ORM | FormMap |
|---|---|---|
| Mapping target | **Objects** (compile-time classes) | **Definitions** (runtime `FormSchema`) |
| Carrier | typed object graph | `DataSet` / `DataTable` |
| Model form | C# class + attributes | `FormSchema` object (cached at runtime; persisted via XML or other formats) |
| Relation level | Table-level | Form-level |
| Change cycle | edit class → recompile → redeploy | edit definition → reload cache → effective immediately |
| Query interface | LINQ + Expression Tree | `FilterNode` + field name string |
| Materialization | auto-hydrate to entity instances | no instantiation; rows in `DataRow` |
| Change tracking | Identity Map / Change Tracking (per entity property) | `DataRow.RowState` + `DataSet.GetChanges()` (per-row state machine built into DataSet) |

### 2.2 Why FormMap

The high churn of ERP systems makes compile-time binding (ORM) costly:

- **Dynamic fields**: fields appear or hide based on role / permission / company; strongly-typed classes need extensive conditional branching.
- **Customization**: the same form may carry different fields for different customers; class variants explode.
- **Multi-tenancy**: each tenant may have its own field set; compile-time binding cannot scale.
- **Dynamic relation sources**: the same form may reference different sources in different scenarios (e.g., a quotation form linking different customer sources across workflows).

FormMap pushes these "moving" parts out to reloadable runtime `FormSchema` definitions, removing recompilation as a routine cost.

---

## 3. Core Concepts

### 3.1 Form-Level Relations

In FormMap, a relation is **not** "this table's FK points to that table's PK", but rather "this **form** references another **form**".

The declaration lives in `FormField.RelationProgId`. At runtime `FormSchema` is an **in-memory cached object**; XML is one of its common persistence formats (and in principle it could be loaded from a database, JSON, or other sources). The XML form is shown below for readability:

```xml
<FormField FieldName="pm_rowid" RelationProgId="Employee">
  <RelationFieldMappings>
    <FieldMapping SourceField="sys_name" DestinationField="ref_pm_name"/>
  </RelationFieldMappings>
</FormField>
```

`RelationProgId="Employee"` points to another `FormSchema`, **not** to a table. This abstraction makes "form" the unit of business entity, not raw tables.

### 3.2 Single-hop Declaration, Multi-hop Execution

**Each `FormSchema` only declares its direct (one-level-down) references.** Multi-level JOINs are resolved at runtime by recursively walking the `FormSchema` chain.

```
Project    declares:  pm_rowid    → Employee
Employee   declares:  dept_rowid  → Department

When a developer writes  Project → ref_pm_dept_name :
Runtime expands to:      Project → Employee → Department  (two-level JOIN)
```

The cognitive load on developers stays at single-hop (each `FormSchema` only looks one level down), while the resulting SQL can be arbitrarily deep.

### 3.3 Place in Definition-Driven Architecture

```
Definition-Driven Architecture (overall architecture)
└── FormSchema (Single Source of Truth)
    ├── drives UI         → FormLayout
    ├── drives DB         → TableSchema
    ├── drives validation → ValidationRules
    └── drives data access → FormMap (this document)
```

FormMap is the data-access manifestation of DDA, sitting alongside `FormLayout` and `TableSchema` as the three projection facets of `FormSchema`.

---

## 4. Examples

The following examples use three simplified `FormSchema` definitions:

- `Project` — `pm_rowid` references `Employee`, `owner_dept_rowid` references `Department`
- `Employee` — `dept_rowid` references `Department`
- `Department` — `pm_rowid` references `Employee` (department head)

### Example 1: Master-Only Query (No JOIN)

```csharp
var builder = new SqlFormCommandBuilder("Project");
var command = builder.BuildSelectCommand("Project", "sys_id,sys_name");
```

**Generated SQL:**
```sql
SELECT A.[sys_id], A.[sys_name]
FROM [ft_project] A
```

No reference fields are used — FormMap produces no JOIN.

### Example 2: WHERE Triggers a JOIN

```csharp
var filter = new FilterCondition("ref_pm_name", ComparisonOperator.StartsWith, "Chang");
var command = builder.BuildSelectCommand("Project", "sys_id,sys_name", filter);
```

**Generated SQL:**
```sql
SELECT A.[sys_id], A.[sys_name]
FROM [ft_project] A
LEFT JOIN [ft_employee] B ON A.[pm_rowid] = B.[sys_rowid]
WHERE B.[sys_name] LIKE @p0
```

The WHERE clause uses `ref_pm_name` (from `Employee`), so FormMap automatically adds the JOIN to `Employee`.

### Example 3: ORDER BY Triggers a Multi-level JOIN

```csharp
var sortFields = new SortFieldCollection
{
    new SortField("ref_pm_dept_name", SortDirection.Asc)
};
var command = builder.BuildSelectCommand("Project", "sys_id,sys_name", null, sortFields);
```

**Generated SQL:**
```sql
SELECT A.[sys_id], A.[sys_name]
FROM [ft_project] A
LEFT JOIN [ft_employee] B ON A.[pm_rowid] = B.[sys_rowid]
LEFT JOIN [ft_department] C ON B.[dept_rowid] = C.[sys_rowid]
ORDER BY C.[sys_name] ASC
```

`ref_pm_dept_name` traverses `Project → Employee → Department` (two levels). FormMap walks the `FormSchema` chain recursively.

### Example 4: Multiple Reference Fields

```csharp
var command = builder.BuildSelectCommand("Project",
    "sys_id,sys_name,ref_owner_dept_name,ref_pm_dept_name");
```

**Generated SQL:**
```sql
SELECT A.[sys_id], A.[sys_name],
       B.[sys_name] AS [ref_owner_dept_name],
       D.[sys_name] AS [ref_pm_dept_name]
FROM [ft_project] A
LEFT JOIN [ft_department] B ON A.[owner_dept_rowid] = B.[sys_rowid]
LEFT JOIN [ft_employee]   C ON A.[pm_rowid]         = C.[sys_rowid]
LEFT JOIN [ft_department] D ON C.[dept_rowid]       = D.[sys_rowid]
```

Two reference fields walk different `FormSchema` chains; FormMap creates branching JOINs automatically.

### Example 5: Composite Filter

```csharp
var filterGroup = FilterGroup.All(
    FilterCondition.Contains("sys_name", "Project"),
    FilterCondition.Equal("ref_pm_name", "Chang"));
var sortFields = new SortFieldCollection
{
    new SortField("sys_id", SortDirection.Asc)
};
var command = builder.BuildSelectCommand("Project", "sys_id,sys_name", filterGroup, sortFields);
```

**Generated SQL:**
```sql
SELECT A.[sys_id], A.[sys_name]
FROM [ft_project] A
LEFT JOIN [ft_employee] B ON A.[pm_rowid] = B.[sys_rowid]
WHERE (A.[sys_name] LIKE @p0 AND B.[sys_name] = @p1)
ORDER BY A.[sys_id] ASC
```

Only `FormSchema` definitions actually referenced are joined — FormMap never adds unused JOINs.

---

## 5. Implementation Mapping

| Component | Role |
|---|---|
| `Bee.Definition.Forms.FormSchema` / `FormField` | source of truth (business entities and relations) |
| `Bee.Db.Query.SelectContextBuilder` | recursively walks the `FormSchema` chain to produce `TableJoin` and `QueryFieldMapping` collections |
| `Bee.Db.Query.SelectBuilder` | builds the `SELECT` clause |
| `Bee.Db.Query.FromBuilder` | builds the `FROM` clause (including JOINs) |
| `Bee.Db.Query.WhereBuilder` | builds the `WHERE` clause with parameterization |
| `Bee.Db.Query.SortBuilder` | builds the `ORDER BY` clause |
| `Bee.Db.Providers.SelectCommandBuilder` | combines the four sub-builders into a final `DbCommandSpec` |
| `Bee.Db.Providers.IFormCommandBuilder` | per-dialect entry point (`SqlFormCommandBuilder` / `PgFormCommandBuilder`) |

> **Current status**: FormMap's SELECT path is fully implemented (SQL Server / PostgreSQL). The INSERT / UPDATE / DELETE interfaces (`BuildInsertCommand` / `BuildUpdateCommand` / `BuildDeleteCommand`) are declared but pending implementation; they will follow the same multi-DB architecture in a later release.

---

## 6. When to Use / Not Use

### 6.1 Use FormMap For

- `FormSchema`-driven CRUD operations (NoCode / LowCode tracks)
- ERP dynamic fields, customization, multi-tenancy
- UI list / filter / sort scenarios (directly configured by `FormSchema`)
- Environments requiring hot updates of field or relation definitions

### 6.2 Do Not Use FormMap For

- Reporting / aggregation / batch import — go through BO + AnyCode and write SQL directly
- Composite-key JOINs, non-equi JOINs, subqueries, CTEs
- Performance-critical hot paths
- Scenarios where dynamic fields aren't needed and the cost of recompiling for ORM is acceptable

> **Dual-track strategy: `FormSchema`-driven CRUD goes through FormMap; arbitrary SQL goes through BO + AnyCode.**
> See [development-cookbook.md](development-cookbook.md) (Traditional Chinese).

---

## 7. Limitations and Design Tradeoffs

The following "limitations" are deliberate tradeoffs aligned with the `FormSchema` worldview, not gaps:

| Limitation | Rationale |
|---|---|
| JOINs must be single-column equi-joins (FK = PK) | `FormSchema` RelationFields always reference `RowId` via equality |
| JOINs target physical tables only (no subquery / CTE / TVF) | `FormSchema` maps to physical tables; there is no "subquery-as-form" concept |
| Main table alias is fixed to `A` | aligned with `SelectContextBuilder`'s alias generator (`A → B → ... → Z → ZA → ZB`, skipping SQL keywords) |
| No simultaneous master-detail composition | master / detail are composed via multiple `DataTable` instances in a `DataSet`, handled at the BO layer |

---

## 8. Further Reading

- [Architecture Overview](architecture-overview.md): the overall BeeNET architecture
- [ADR-005: FormSchema-Driven Architecture](adr/adr-005-formschema-driven.md) (Traditional Chinese): the upstream design decision behind FormMap
- [Terminology Reference](terminology.md) (Traditional Chinese): EN/ZH terminology mapping
- [Bee.Db README](../src/Bee.Db/README.md): Bee.Db package overview
