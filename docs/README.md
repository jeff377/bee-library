# Bee.NET Documentation

[繁體中文](README.zh-TW.md)

This directory contains the public-facing developer documentation for the Bee.NET framework. All documents listed below are bilingual (English + Traditional Chinese), with the English version as the primary file (`xxx.md`) and the Traditional Chinese version as `xxx.zh-TW.md`.

---

## Getting Started

| Document | Description |
|----------|-------------|
| [Architecture Overview](architecture-overview.md) | Definition-Driven Architecture: design philosophy and practical patterns for ERP systems |
| [Terminology](terminology.md) | English ↔ Chinese term reference for the framework |
| [Project Dependency Map](dependency-map.md) | Visualization of the dependencies among the 16 `src/` projects |

## Development Guides

| Document | Description |
|----------|-------------|
| [End-to-End Development Cookbook](development-cookbook.md) | Core development flow from definition to API, including initialization order, request pipeline, and ExecFunc pattern |
| [Development Constraints and Anti-Patterns](development-constraints.md) | Framework constraints and forbidden practices, useful as a reference for AI coding tools |
| [JSON-RPC Frontend Integration](jsonrpc-frontend-integration.md) | Calling the Bee.NET JSON-RPC API from JavaScript / TypeScript frontends (no .NET on the client) — wire format, auth flow, TS wrapper |
| [Permission & Authorization](permission-authorization.md) | Configuring and running the two-layer authorization (action gate + record scope): PermissionModels, FormField.ScopeRole, the role/grant tables, read filtering and authoritative write-side re-query |

## Database

| Document | Description |
|----------|-------------|
| [Database Naming Conventions](database-naming-conventions.md) | Naming rules for tables, columns, indexes, and system fields; cross-DB case sensitivity reference |
| [Framework-Reserved Names](framework-reserved-names.md) | Registry of `st_*` system tables and reserved `progId`s owned by the framework |
| [DatabaseSettings & DbCategorySettings Guide](database-settings-guide.md) | Structure, access patterns, and runtime behavior of the two database-related settings files |
| [Database Schema Upgrade](database-schema-upgrade.md) | Schema upgrade workflow and strategy |

## Design Concepts

| Document | Description |
|----------|-------------|
| [API ↔ BO Contract Design](api-bo-contract-design.md) | Three-tier API contract separation (Contracts / API Type / BO Type) |
| [API Method Reference](api-method-reference.md) | Single-page table of every BO method exposed through JSON-RPC, with `[ApiAccessControl]` settings + purpose |
| [FormMap](formmap.md) | Bee.Db's data access pattern, dynamically generating SQL from FormSchema |

---

## Other Folders

These folders are excluded from this README's main listing; consult them directly when needed:

- **`adr/`** — Architecture Decision Records (ADR-001 through ADR-015+); design decisions with their rationale
- **`plans/`** — Design / planning documents for in-progress or completed initiatives
- **`repo-ops/`** — Operational documentation for this repository (CI / branch protection); not relevant to framework users
