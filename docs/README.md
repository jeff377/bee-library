# Bee.NET Documentation

[繁體中文](README.zh-TW.md)

This directory contains the public-facing developer documentation for the Bee.NET framework. Every document listed below is bilingual (English + Traditional Chinese); the English version is the primary file (`xxx.md`) and the Traditional Chinese version is `xxx.zh-TW.md`.

The listing is ordered by **where you are in the journey**, not by subject. Each entry is tagged with its **kind** (Tutorial / Concept / Guide / Reference) and its **length** (Short < 150 lines, Medium 150–350, Long > 350) so you can judge the commitment before opening it. If you would rather browse by subject, see [Find by Topic](#find-by-topic) at the bottom.

**Reading paths**

- **First contact** → read the three documents under [Start Here](#1-start-here); that is enough to build something.
- **Want to understand the trade-offs** → add [Concepts](#2-concepts).
- **Stuck mid-build** → look up the matching document under [Guides](#3-guides).
- **Writing fields, naming things, calling an API** → go straight to [Reference](#4-reference).

---

## 1. Start Here

Read these three and you can build your first application.

| Document | Kind | Length | Description |
|----------|------|--------|-------------|
| [Getting Started](getting-started.md) | Tutorial | Medium | Build your first Bee.NET backend from scratch: packages, `DefinePath`, DI wiring, your first form and business object, then calling it from a client |
| [Architecture Overview](architecture-overview.md) | Concept | Long | Definition-Driven Architecture: the design philosophy and the practical patterns behind it |
| [Definition Files Overview](definition-files-overview.md) | Concept | Medium | The map of all thirteen definition files: what each one owns, how they connect, and what changing one affects |

> Unfamiliar term? Keep [Terminology](terminology.md) open in another tab.
>
> Want the bird's-eye list of what the framework already does for you? See [Framework Capabilities](framework-capabilities.md).

## 2. Concepts

Why the framework is built the way it is.

| Document | Kind | Length | Description |
|----------|------|--------|-------------|
| [FormSchema-Driven Database Access](formschema-data-access.md) | Concept | Medium | How Bee.Db generates SQL dynamically from a FormSchema, and why it is not an ORM |
| [API ↔ BO Contract Design](api-bo-contract-design.md) | Concept | Medium | Three-tier API contract separation (Contracts / API Type / BO Type) and the naming conventions that drive it |
| [Project Dependency Map](dependency-map.md) | Concept | Short | How the `src/` projects depend on each other, and the rules that keep the graph acyclic |
| [Caching](caching.md) | Concept | Long | How definition and database-backed caches work: the read path, the four invalidation signals, and the notification-table mechanism for cross-process / multi-node deployments |

## 3. Guides

How to actually do a thing.

| Document | Kind | Length | Description |
|----------|------|--------|-------------|
| [End-to-End Development Cookbook](development-cookbook.md) | Guide | Long | The core development flow from definition to API: initialization order, request pipeline, ExecFunc pattern, cache invalidation |
| [Expressions and Rules](expression-rules.md) | Guide | Short | Declarative field computation and pre-save / pre-delete validation in FormSchema, instead of hand-written BO code |
| [Tenant Customization](customization.md) | Guide | Medium | Giving one company different labels, a different form arrangement or extra behaviour, without forking the base definitions: which mechanism to reach for, how each one is written, and what cannot be customized |
| [Permission & Authorization](permission-authorization.md) | Guide | Medium | The two-layer authorization model (action gate + record scope): PermissionModels, `FormField.ScopeRole`, the role/grant tables, read filtering and authoritative write-side re-query — plus the separate deployment-level axis for installation-wide assets |
| [API Key Management](api-key-management.md) | Guide | Short | What an API key identifies (the calling application, not the user), how the gate turns itself on, who may manage keys, and the rotation procedure |
| [JSON-RPC Frontend Integration](jsonrpc-frontend-integration.md) | Guide | Long | Calling the JSON-RPC API from a JavaScript / TypeScript frontend with no .NET on the client: wire format, auth flow, TypeScript wrapper |
| [Wire Contract](../wire-contracts/README.md) | Reference | Short | The TypeScript contract generated from the message types — what a non-.NET client codes against |
| [Wire Fixtures](../wire-fixtures/README.md) | Reference | Short | Golden body samples for every wire message, to check a client implementation against |
| [DatabaseSettings & DbCategorySettings Guide](database-settings-guide.md) | Guide | Long | Structure, access patterns and runtime behaviour of the two database-related settings files |
| [Database Schema Upgrade](database-schema-upgrade.md) | Guide | Medium | Synchronising definition changes to a live database: the diff → plan → execute pipeline, ALTER vs rebuild, dry runs |

## 4. Reference

Look things up while you work.

| Document | Kind | Length | Description |
|----------|------|--------|-------------|
| [Framework Capabilities](framework-capabilities.md) | Reference | Medium | Single-page catalogue of every mechanism the framework provides, grouped by area, one line each |
| [Terminology](terminology.md) | Reference | Long | English ↔ Chinese term reference, organised by layer |
| [API Method Reference](api-method-reference.md) | Reference | Short | Every BO method exposed through JSON-RPC on one page, with its `[ApiAccessControl]` settings and purpose |
| [Framework-Reserved Names](framework-reserved-names.md) | Reference | Short | Registry of the `st_*` system tables and reserved `progId`s owned by the framework |
| [Database Naming Conventions](database-naming-conventions.md) | Reference | Medium | Naming rules for tables, columns, indexes and system fields; cross-database case-sensitivity reference |
| [Database Dialect Differences (DDL)](database-dialect-differences.md) | Reference | Medium | Cross-dialect DDL rules and exceptions (defaults, nullability, quoting, AutoIncrement); why text and numeric columns are NOT NULL |
| [Temporal Types: Date, DateTime and Time](temporal-types.md) | Reference | Medium | Choosing between the three, and how each is carried in the database, the `DataSet`, code and all three serialization formats |
| [Time Zones](datetime-timezone.md) | Reference | Short | UTC storage, where conversion happens, configuring a user's zone, and what hand-written SQL and non-.NET clients must do |
| [Analyzer Rules](analyzer-rules.md) | Reference | Short | The build diagnostics shipped with the packages: rule list, how to adjust severity, versioning policy |
| [Development Constraints and Anti-Patterns](development-constraints.md) | Reference | Medium | Framework constraints and forbidden practices; also useful as a reference for AI coding tools |

## 5. Deep Dive

| Folder | Description |
|--------|-------------|
| [`adr/`](adr/README.md) | Architecture Decision Records — the primary source for *why* a design is the way it is. The index lists every ADR with its status (accepted / superseded) |
| [`changelogs/`](changelogs/) | Per-version change detail behind the root `CHANGELOG.md` |

---

## Find by Topic

The same documents, grouped by subject. A document appearing under several topics is intentional.

| Topic | Documents |
|-------|-----------|
| **Database** | [Naming Conventions](database-naming-conventions.md) · [Reserved Names](framework-reserved-names.md) · [Settings Guide](database-settings-guide.md) · [Schema Upgrade](database-schema-upgrade.md) · [Dialect Differences](database-dialect-differences.md) · [FormSchema-Driven Access](formschema-data-access.md) |
| **Definition layer** | [Definition Files Overview](definition-files-overview.md) · [Architecture Overview](architecture-overview.md) · [Expressions and Rules](expression-rules.md) · [Reserved Names](framework-reserved-names.md) |
| **Multi-tenancy** | [Tenant Customization](customization.md) · [Definition Files Overview](definition-files-overview.md) · [Development Cookbook](development-cookbook.md) · [Caching](caching.md) |
| **Caching & performance** | [Caching](caching.md) · [Development Cookbook](development-cookbook.md) · [Development Constraints](development-constraints.md) |
| **API & frontend** | [Contract Design](api-bo-contract-design.md) · [API Method Reference](api-method-reference.md) · [JSON-RPC Frontend Integration](jsonrpc-frontend-integration.md) · [Permission & Authorization](permission-authorization.md) · [API Key Management](api-key-management.md) |
| **Types & time** | [Temporal Types](temporal-types.md) · [Time Zones](datetime-timezone.md) |
| **Quality & conventions** | [Analyzer Rules](analyzer-rules.md) · [Development Constraints](development-constraints.md) · [Naming Conventions](database-naming-conventions.md) |

---

## Other Folders

Excluded from the listing above; consult them directly when needed.

- **`plans/`** — Design and planning documents for in-progress or completed initiatives. These are point-in-time working documents, **not reference material**: an older plan may no longer describe current behaviour. Nothing outside this folder links into it — treat the documents above as the source of truth.
- **`repo-ops/`** — Operational documentation for this repository (CI / branch protection); not relevant to framework users.
