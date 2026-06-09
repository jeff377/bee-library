# Framework-Reserved Names

[繁體中文](framework-reserved-names.zh-TW.md)

> Registry of names owned by the **bee-library** framework: which `st_*` system tables exist, and which `progId`s the framework reserves.
>
> - Naming **rules** (`st_` vs `ft_` prefix, column/index conventions) live in [Database Naming Conventions](database-naming-conventions.md).
> - Full **API method reference** (action signatures, `[ApiAccessControl]`, purpose) lives in [API Method Reference](api-method-reference.md).
> - This document is the single source of truth for **which specific names** are reserved by the framework. When extending or integrating, do not collide with anything listed here.

---

## 1. System tables (`st_*`)

The `st_` prefix means "framework-owned table". It is **orthogonal to which database** the table lives in: an `st_*` table can live in the common database or the per-company database — what matters is who owns it.

### 1.1 Common database (shared globally)

| Table | Purpose |
|-------|---------|
| `st_user` | Global user master (login id, password hash, profile). |
| `st_company` | Company list (per-tenant root). |
| `st_user_company` | Which users can enter which companies. |
| `st_session` | Sessions / access tokens. |
| `st_define` | DB-backed definition storage (FormSchema / TableSchema / etc., when not stored as XML files). |
| `st_cache_notify` | Cross-node cache invalidation channel ([ADR-017](adr/adr-017-db-cache-invalidation.md)). |

### 1.2 Company database (per-tenant)

| Table | Purpose |
|-------|---------|
| `st_role` | Role definitions ([ADR-019](adr/adr-019-permission-authorization-model.md)). |
| `st_role_grant` | Role-to-resource grants (per progId / action). |
| `st_user_role` | User-to-role bindings. |
| `st_department` | Organisational departments. |
| `st_employee` | Employees (links a common-DB `st_user` to a per-company organisational position). |

> `st_department` / `st_employee` live in the company database despite their `st_` prefix — they are **framework-owned** (the record-scope and organisation tree features need them), not business data. Per-company business tables should use the `ft_` prefix.

---

## 2. Reserved `progId`s

### 2.1 System axis

- **`System`** (formalised as `Bee.Definition.SysProgIds.System`) — the singleton entry point for the system-level business object (`SystemBusinessObject`). All framework-level actions that are not form-scoped (login, ping, get-define, etc.) are dispatched here.

See [API Method Reference §Axis: System](api-method-reference.md#axis-system-systembusinessobject) for the full action list.

### 2.2 Framework-shipped forms

The framework ships these forms as part of its organisation / record-scope feature:

| progId | Backing table | Purpose |
|--------|---------------|---------|
| `Department` | `st_department` | Department maintenance form. |
| `Employee` | `st_employee` | Employee maintenance form. |

See [API Method Reference §Axis: Form](api-method-reference.md#axis-form-formbusinessobject) for the standard FormBO actions inherited by every form progId.

---

## 3. Consumer guidelines

When extending bee-library or building applications on top of it:

- **Use `ft_` for your own business tables.** Never `st_` — that prefix is reserved for the framework. (See [Database Naming Conventions](database-naming-conventions.md).)
- **Avoid reserved `progId`s** for your own forms — `System`, `Department`, `Employee` are taken. Pick distinct progIds; convention is `PascalCase`, often prefixed with your module abbreviation.
- **To extend a framework table** (e.g. add a custom column to `st_employee`): drop a same-named `.TableSchema.xml` in your application's `DefinePath`. The framework reads only from `DefinePath` at runtime; your file is the single source the framework sees. The framework's embedded defaults are not consulted at runtime — they exist only for one-shot extraction via the API below.
- **To get the base XML to start customising from** — three options, in order of typical preference:
    - **Programmatic API** (canonical): `Bee.Definition.Defaults.MaterializeTo("./Define")` writes every embedded framework default XML into the given directory. Skip-existing by default — re-runs are safe and won't clobber your customisations. See `Bee.Definition.Defaults` in [`src/Bee.Definition/Defaults.cs`](../src/Bee.Definition/Defaults.cs).
    - **CLI** (recommended for CI / setup scripts): install once with `dotnet tool install -g Bee.Cli` (upgrade later via `dotnet tool update -g Bee.Cli`), then `dotnet bee defines materialize --path ./Define` — thin shell over the same API. Run `dotnet bee defines list` to see every embedded file, `dotnet bee defines materialize --filter TableSchema/` to materialise a subset.
    - **Browse on GitHub**: every embedded default lives under [`src/Bee.Definition/Defaults/`](../src/Bee.Definition/Defaults/) in this repo — open the file you care about, copy its contents into your `DefinePath`.
- **Framework updates that change `st_*` tables** are flagged as breaking changes in the [CHANGELOG](../CHANGELOG.md). Renaming a table requires a manual `RENAME TABLE` — see [Table Schema Upgrade Guide §Renaming framework tables](database-schema-upgrade.md).

---

## See also

- [Database Naming Conventions](database-naming-conventions.md) — naming rules behind the `st_` / `ft_` split.
- [API Method Reference](api-method-reference.md) — full BO method catalogue.
- [Architecture Overview](architecture-overview.md) — how `st_*` tables fit into the broader N-tier + clean architecture.
- [ADR-019: Permission Authorisation Model](adr/adr-019-permission-authorization-model.md) — why `st_role` / `st_user_role` / `st_employee` are framework-owned.
