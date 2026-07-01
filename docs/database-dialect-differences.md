[繁體中文](database-dialect-differences.zh-TW.md)

# Database Dialect Differences (DDL)

Bee.NET generates DDL (CREATE TABLE / ALTER TABLE) from a single `TableSchema` definition and hides the per-database differences behind dialect adapters under `src/Bee.Db/Providers/<Dialect>/`. Application developers usually never see these differences — the framework's own CRUD, seeding, and schema-upgrade paths handle them uniformly.

This document consolidates the DDL rules and **exceptions** that *do* leak through when you write schema definitions or hand-written SQL by hand (for example, an `INSERT` in a test helper or a migration script). It covers all five supported engines: **SQL Server, PostgreSQL, MySQL, Oracle, SQLite**.

> Related, more focused documents:
> - [database-naming-conventions.md](database-naming-conventions.md) §5 — identifier case sensitivity and quoting.
> - [database-schema-upgrade.md](database-schema-upgrade.md) §4 — ALTER-vs-rebuild decision and per-dialect ALTER capabilities.
> - [src/Bee.Db/README.md](../src/Bee.Db/README.md) — SQLite limitations and the Oracle identifier strategy.

---

## 1. Why text and numeric columns are `NOT NULL` by default

This is a deliberate framework design decision, so it is stated first because it drives most of the exceptions below.

`DbField.AllowNull` **defaults to `false`** ([DbField.cs](../src/Bee.Definition/Database/DbField.cs)). The rule is:

| Column category | Nullability | Default value |
|-----------------|-------------|---------------|
| **Text** (`String`, `Text`) | `NOT NULL` | empty string `''` |
| **Numeric** (`Short`, `Integer`, `Long`, `Decimal`, `Currency`, `Boolean`) | `NOT NULL` | `0` |
| **Date / DateTime** | `NOT NULL` unless marked otherwise | current timestamp |
| Columns that genuinely need null (e.g. an "invalid/expiry time", optional binary) | set `AllowNull="true"` **explicitly** | none |

### Rationale

Allowing `NULL` in text and numeric columns forces every consumer — application code *and* hand-written SQL — to defend against null:

- `WHERE col = ''` silently fails to match `NULL` rows; you would need `WHERE col IS NULL OR col = ''` everywhere.
- Aggregations, joins, and string operations need `COALESCE` / null guards.
- C# consumers risk `NullReferenceException` unless every read is null-checked.

By guaranteeing "text is never null, numeric is never null", the framework lets you treat an empty string and zero as the canonical "empty" values and skip null handling entirely. **Do not reflexively add `AllowNull="true"`** — only set it for columns that have a real, distinct "unknown / not-set" state that empty-string or `0` cannot represent (typically `DateTime` such as a session expiry time, or optional binary payloads).

### How the guarantee is upheld even where a database fights it

Two engines cannot honour "text `NOT NULL` with an empty-string default" directly. The framework still upholds the *contract* — see the two hard exceptions in [§3](#3-the-two-hard-nullability-exceptions).

---

## 2. Built-in default value expressions

For a `NOT NULL` column with no explicit `DefaultValue`, each dialect emits its own default expression. (When `AllowNull="true"`, **no** default is emitted on any dialect.)

| `FieldDbType` | SQL Server | PostgreSQL | MySQL | Oracle | SQLite |
|---------------|-----------|------------|-------|--------|--------|
| `String` / `Text` | `N''` | `''` | `''` | *(nullable — see §3)* | `''` |
| `Short`/`Integer`/`Long`/`Decimal`/`Currency`/`Boolean` | `0` | `0` | `0` | `0` | `0` |
| `Date` | `getdate()` | `CURRENT_TIMESTAMP` | `(CURRENT_DATE)` | `SYSTIMESTAMP` | `CURRENT_TIMESTAMP` |
| `DateTime` | `getdate()` | `CURRENT_TIMESTAMP` | `CURRENT_TIMESTAMP(6)` | `SYSTIMESTAMP` | `CURRENT_TIMESTAMP` |
| `Guid` | `newid()` | `gen_random_uuid()` | `(UUID())` | `SYS_GUID()` | `(hex(randomblob(16)))` |

Notes:

- **MySQL** wraps function-call defaults in parentheses (`(UUID())`, `(CURRENT_DATE)`) because MySQL only allows non-literal defaults in the parenthesised *expression* form.
- **SQLite** has no native UUID generator; `hex(randomblob(16))` is a unique-but-not-strictly-v4 surrogate, sufficient for framework-managed defaults.
- **Boolean literals**: the framework's canonical form is `"1"` / `"0"`. PostgreSQL rejects those for a `BOOLEAN` column, so the PG dialect translates them to `TRUE` / `FALSE` at the SQL-emission boundary. All other dialects accept `1` / `0`.

---

## 3. The two hard nullability exceptions

These are the rules most likely to cause a "works on 4 databases, fails on the 5th" surprise.

### 3.1 Oracle: `''` is `NULL`

Oracle has no concept of a non-null empty string — `''` **is** `NULL`. So `VARCHAR2(n) DEFAULT '' NOT NULL` is self-contradictory (`DEFAULT ''` means `DEFAULT NULL`, which conflicts with `NOT NULL`).

**How the framework handles it** ([OracleSchemaSyntax.cs](../src/Bee.Db/Providers/Oracle/OracleSchemaSyntax.cs)):

- `String` / `Text` columns are emitted **nullable** (no `NOT NULL`, no `DEFAULT ''`) on Oracle only.
- The "text is never null" contract is upheld at the C# layer: `ValueUtilities.CStr(null)` returns `""`, so callers still only ever see an empty string.
- `OracleTableSchemaProvider` reads such columns back as `AllowNull = false` to keep the schema diff stable against the definition.
- An explicit *non-empty* default is still a valid non-null literal on a nullable Oracle column, so it is preserved.
- `CLOB` / `BLOB` also reject an inline literal `DEFAULT` in the framework's `CREATE TABLE` shape.

### 3.2 MySQL: `TEXT` / `BLOB` cannot have a `DEFAULT`

MySQL forbids a `DEFAULT` clause on `TEXT` / `BLOB` columns. So a `Text` column with `AllowNull=false` is emitted as `TEXT NOT NULL` **with no default** ([MySqlSchemaSyntax.cs](../src/Bee.Db/Providers/MySql/MySqlSchemaSyntax.cs)) — the column stays `NOT NULL`, but there is no DB-side fallback value.

**Consequence for hand-written SQL:** any `INSERT` that **omits** a `NOT NULL` `Text` column fails **only on MySQL** in strict mode with:

```
Field 'x' doesn't have a default value
```

On the other four engines the same partial `INSERT` succeeds, because their `NOT NULL` text columns carry an implicit `DEFAULT ''`. The framework's own CRUD and seeding always list every column, so they are unaffected — the trap is **hand-written raw SQL** (test helpers, seed scripts, migration snippets).

> **Rule when adding a `NOT NULL` `Text` column:** keep it `NOT NULL` (do **not** switch it to nullable to work around MySQL), and make sure every hand-written `INSERT` supplies the value explicitly (an empty string is fine). This matches the framework principle in §1; the fix belongs in the INSERT, not in the column's nullability.

> **Testing caveat:** a *persistent* local MySQL container adds new columns via `ALTER TABLE ... ADD COLUMN`, which **forces the new column nullable** regardless of `AllowNull`. That masks this bug locally while CI (fresh `CREATE TABLE`) makes it `NOT NULL` and fails. To reproduce CI locally: `ALTER TABLE <t> MODIFY <col> LONGTEXT NOT NULL;` (after `UPDATE ... SET <col> = '' WHERE <col> IS NULL`).

---

## 4. Identifier quoting and case

| | Quote form | Case behaviour |
|---|-----------|----------------|
| SQL Server | `[name]` (`]` → `]]`) | quoted lowercase |
| PostgreSQL | `"name"` (`"` → `""`) | quoted lowercase |
| MySQL | `` `name` `` | quoted lowercase; case-insensitive comparison via table-level `COLLATE utf8mb4_0900_ai_ci` |
| Oracle | `"NAME"` (`"` → `""`) | **quoted UPPERCASE** — Oracle has a wide reserved-word set (`COMMENT`, `SIZE`, `LEVEL`, `SESSION`, …) so every identifier is quoted; the adapter folds to uppercase to match Oracle's native unquoted behaviour, and normalises back to lowercase on read-back |
| SQLite | `"name"` (`"` → `""`) | quoted lowercase; text columns get `COLLATE NOCASE` for case-insensitive comparison |

See [database-naming-conventions.md](database-naming-conventions.md) §5 for the full case-sensitivity matrix (identifier folding vs. data comparison).

---

## 5. AutoIncrement (identity) syntax

`AutoIncrement` maps to a different construct per engine, and some require inlining it with the primary key on the same column line:

| | Syntax | Must inline with PK? |
|---|--------|----------------------|
| SQL Server | `[int] IDENTITY(1,1)` | no |
| PostgreSQL | `GENERATED BY DEFAULT AS IDENTITY` | no |
| MySQL | `BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY` | **yes** |
| Oracle | `NUMBER(19) GENERATED BY DEFAULT AS IDENTITY` | no (PK added separately) |
| SQLite | `INTEGER PRIMARY KEY AUTOINCREMENT` | **yes** (cannot attach `AUTOINCREMENT` via an external PK constraint) |

---

## 6. ALTER vs. table rebuild

When a schema upgrade changes a column, some changes can be done with `ALTER`, others require rebuilding the table (create-new + copy + swap). The decision and the per-dialect capabilities are documented in [database-schema-upgrade.md](database-schema-upgrade.md) §4. Highlights:

- **SQLite** supports only `ADD COLUMN` / `RENAME COLUMN` / `DROP COLUMN` via `ALTER`; anything else (type change, nullability change, constraint change) requires a rebuild.
- **SQL Server** rebuilds on cross-type-family changes and on toggling AutoIncrement.
- **Oracle** `ALTER TABLE ... MODIFY` must be **diff-based** for nullability: re-issuing `NOT NULL` on an already-`NOT NULL` column raises `ORA-01442`, so the adapter only emits the `NULL` / `NOT NULL` hint when it actually changes.
- **MySQL** `ALTER ADD` of a `Guid` column with a non-deterministic default (`UUID()`) is split into two statements to stay replication-safe under statement-based binlog.
- Table rename differs too: `sp_rename` (SQL Server) vs `RENAME TABLE` (MySQL) vs `ALTER TABLE ... RENAME` (PostgreSQL / Oracle / SQLite).

---

## 7. Checklist: adding a column across all dialects

1. **Choose nullability by principle, not reflex.** Text / numeric → leave `AllowNull=false` (NOT NULL, default `''` / `0`). Only set `AllowNull="true"` for a genuine "unknown / not-set" state (e.g. a `DateTime` expiry, optional binary).
2. **Update every hand-written `INSERT`** that targets the table so it lists the new column — mandatory for a `NOT NULL` `Text` column because MySQL gives it no DB-side default (§3.2). The framework's own CRUD/seed already lists all columns.
3. **If the column is normally empty and you need Oracle**, be aware it will be physically nullable there (§3.1); the C# layer still reads it as an empty string, so no application change is needed.
4. **Do not judge correctness from a persistent local container alone.** New columns added via `ALTER` are forced nullable locally and hide the fresh-`CREATE` `NOT NULL` behaviour that CI (and production first-time setup) exercises.

---

## Reference

- Dialect implementations: `src/Bee.Db/Providers/<Dialect>/<Dialect>SchemaSyntax.cs`, `…TableSchemaProvider.cs`, `…AlterCompatibilityRules.cs`.
- Column model: [DbField.cs](../src/Bee.Definition/Database/DbField.cs).
- Related docs: [database-naming-conventions.md](database-naming-conventions.md), [database-schema-upgrade.md](database-schema-upgrade.md), [src/Bee.Db/README.md](../src/Bee.Db/README.md).
