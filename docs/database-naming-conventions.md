# Database Naming Conventions

[繁體中文](database-naming-conventions.zh-TW.md)

This document defines the naming rules for database tables, columns, and system fields, applicable to all database object designs.
A unified naming style avoids cross-database case and semantic inconsistencies and improves maintainability.

> Looking for the actual list of `st_*` tables the framework owns? See [Framework-Reserved Names](framework-reserved-names.md).

---

## 1️⃣ Overall Database Conventions

- Use **snake_case** (lowercase + underscore).
- **Avoid** PascalCase or camelCase, since some databases (e.g. PostgreSQL, Oracle) become case-sensitive when identifiers are quoted.
- All **table** and **column** names use **lowercase letters**.
- System-level tables (settings, login, audit, etc.) use the `st_` prefix.
- Audit-trail / log tables are named `st_log_*` (e.g. `st_log_login`, `st_log_change`) and belong to the `log` category — see [Framework-Reserved Names §1.3](framework-reserved-names.md).
- Business / form-level tables use the `ft_` prefix.

---

## 2️⃣ Table and Column Naming

| Type | Format | Example | Notes |
|------|--------|---------|-------|
| **Table name** | `lowercase_underscore` | `user_accounts` | Clearly describe content; use nouns, not verbs. |
| **Column name** | `lowercase_underscore` | `user_id`, `created_at`, `dept_code` | Always lowercase; consistent abbreviations allowed; names should clearly express purpose. |
| **Primary key column** | `sys_no` | `sys_no` | Auto-incremented sequential number. |
| **Unique row identifier** | `sys_rowid` | `sys_rowid` | GUID; globally unique; immutable. |
| **Foreign key column** | `{entity}_rowid` | `manager_rowid` | References another table's `sys_rowid`; relational integrity is maintained by the application layer. |

---

## 3️⃣ Index and Default Value Naming

| Type | Format | Example | Notes |
|------|--------|---------|-------|
| **Primary key index** | `pk_table` | `pk_employee` | `sys_no`, the database primary key (sequential number) |
| **Unique row identifier index** | `rx_table` | `rx_employee` | `sys_rowid`, the system-wide unique key (globally unique) |
| **Unique data index** | `uk_table` | `uk_employee` | `sys_id`, business identifier or document number uniqueness |
| **Foreign key index** | `fk_table_column` | `fk_employee_dept` | Relational integrity (column references another table's `sys_rowid`) |
| **Generic index** | `ix_table_column` | `ix_users_email` | Query acceleration (composite or partial indexes allowed) |
| **Default value constraint** | `df_table_column` | `df_users_created_at` | Column default value definition |

---

## 4️⃣ System Field Definitions

| Field Name | Description | Notes |
|------------|-------------|-------|
| `sys_no` | System sequential number, auto-incremented | Primary key |
| `sys_rowid` | Unique identifier (GUID); immutable after insert | Globally unique identifier |
| `sys_master_rowid` | Logical foreign key from a detail row to its master record | Relations maintained by the application layer |
| `sys_id` | Record number / document number | Used by the business layer |
| `sys_name` | Record name / display name | Used by the business layer |
| `sys_valid_date` | Effective date (inclusive) | Range start |
| `sys_invalid_date` | Expiry date (exclusive) | Range end |
| `sys_insert_time` | Insert timestamp | Filled by the system |
| `sys_update_time` | Update timestamp | Filled by the system |
| `sys_insert_user_rowid` | Creator identifier (logical foreign key) | References user `sys_rowid` |
| `sys_update_user_rowid` | Updater identifier (logical foreign key) | References user `sys_rowid` |

---

## 5️⃣ Cross-Database Case Sensitivity Reference

Different databases handle the case of "identifiers" (table / column / index names) and "data values" (string comparisons) very differently.
This section lists the actual behavior of the 5 databases supported by Bee.NET, as a reference for schema design and hand-written SQL.

### 5.1 Identifiers (Table / Column / Index Names)

| Database | Unquoted Identifier | Quoted Identifier | Internal Storage |
|----------|---------------------|-------------------|------------------|
| **SQL Server** | Case-insensitive¹ | Case-insensitive¹ | Original case preserved; comparison is case-insensitive |
| **PostgreSQL** | Folded to **lowercase** | Case-sensitive, preserved as-is | Determined by the rule above |
| **MySQL** | Tables depend on OS / `lower_case_table_names`²; columns / indexes are **always** case-insensitive | Same as left | Same as left |
| **SQLite** | Case-insensitive | Case-insensitive | Original case preserved |
| **Oracle** | Folded to **UPPERCASE** | Case-sensitive, preserved as-is | Determined by the rule above |

¹ Depends on database / column collation; Bee.NET expects `_CI_` (Case-Insensitive) collations, which is the default.
² On Linux, sensitive by default (table names map to filesystem files); on Windows / macOS, insensitive by default. Can be overridden globally via `lower_case_table_names`.

#### Whether the Three Are Consistent Within a Single DB

| Database | Table / Column / Index Behavior Consistency |
|----------|---------------------------------------------|
| SQL Server | ✅ Consistent (all governed by collation) |
| PostgreSQL | ✅ Consistent (all follow "unquoted → lower, quoted → as-is") |
| **MySQL** | ❌ **Inconsistent** (tables depend on OS; columns / indexes are always case-insensitive) |
| SQLite | ✅ Consistent (all case-insensitive) |
| Oracle | ✅ Consistent (all follow "unquoted → UPPER, quoted → as-is") |

PostgreSQL and Oracle fold in **opposite directions**: the same unquoted SQL is interpreted as lowercase by PG and as UPPERCASE by Oracle.
This is the root cause of most cross-database problems involving these two DBs.

### 5.2 Data Values (String Comparison)

| Database | Default String Comparison | Bee.NET Handling |
|----------|---------------------------|------------------|
| **SQL Server** | Case-insensitive¹ | Use defaults |
| **PostgreSQL** | **Case-sensitive** (byte comparison) | Use defaults; the application must explicitly use `ILIKE` or `LOWER()` |
| **MySQL** | Case-insensitive¹ | Use defaults |
| **SQLite** | **Case-sensitive** (`BINARY`) | Add `COLLATE NOCASE` on the column if needed |
| **Oracle** | **Case-sensitive** (`BINARY`) | Set session NLS at connection startup: `NLS_COMP='LINGUISTIC'` + `NLS_SORT='BINARY_CI'`, making `=` and `LIKE` case-insensitive |

¹ Depends on column collation; the default collations (SQL Server `*_CI_*`, MySQL `*_ci`) are case-insensitive.

> **Note: identifiers and data values are two independent layers**
> - Identifier sensitivity is determined at the SQL parser stage and affects schema object lookup
> - Data value sensitivity is determined at the SQL execution stage, governed by collation / NLS / `COLLATE` clauses
> - The two layers are independent and do not affect each other

### 5.3 Bee.NET Adapter's Identifier Strategy

The framework adopts a "**uniformly quoted + each DB's most natural case**" strategy across the 5 DBs, with adapters handling case translation at the boundary:

| DB | Identifier Storage Form in DDL/DML | Quoting |
|----|------------------------------------|---------|
| SQL Server | As-is (lowercase, case-insensitive) | `[name]` |
| PostgreSQL | **Lowercase** (consistent with fold direction) | `"name"` |
| MySQL | As-is (lowercase) | `` `name` `` |
| SQLite | As-is (lowercase) | `"name"` |
| **Oracle** | **UPPERCASE** (consistent with Oracle's default fold direction) | `"NAME"` |

Oracle is the outlier: when emitting DDL/DML, the framework `.ToUpperInvariant()`s identifiers and then quotes them (via the extension method `DatabaseType.Oracle.QuoteIdentifier(...)`, defined in `Bee.Db.DatabaseTypeExtensions`), so Oracle's data dictionary stores them as UPPERCASE (e.g. `ST_USER`, `SYS_NO`, `COMMENT`).
On the read side, `OracleTableSchemaProvider` `.ToLowerInvariant()`s identifiers retrieved from views like `USER_TAB_COLUMNS` before placing them into `TableSchema` / `DbField`.

> **Abstraction consistency**: upper layers like `FormSchema`, `Bee.Repository`, and `Bee.Business` only see lowercase identifiers (e.g. `st_user`); they don't need to know that Oracle stores them as UPPERCASE internally. Case translation is fully encapsulated inside the Oracle adapter.

#### Why Oracle Uses UPPERCASE Storage

1. **Aligns with Oracle conventions**: in SQL Developer / DBeaver / sqlplus, DBAs see object names like `ST_USER`, matching the visual result of Oracle's default unquoted-fold-to-UPPER behavior.
2. **Friendly to hand-written SQL on Oracle**: `SELECT * FROM st_user` is folded by the Oracle parser to `ST_USER`, matching storage exactly — **no need to quote everywhere** (consistent with the intuitive lowercase style used on the other 4 DBs).
3. **Reserved words remain usable as columns**: because the framework still emits `"COMMENT"`, `"ORDER"` in quoted form, the Oracle parser treats them as quoted identifiers rather than reserved word tokens, so naming need not avoid reserved words.

#### Validation Side of the Naming Convention

The "all-lowercase + snake_case" naming convention in sections 1–4 corresponds to the following observations:

1. **Cross-DB abstraction is uniformly lowercase**: identifier declarations in FormSchema are always lowercase; the framework's per-DB adapters translate to that DB's storage form.
2. **Oracle's internal UPPERCASE is an encapsulated detail**: when developers write FormSchema, Repository, or BO code, they need not be aware that Oracle uses UPPERCASE — that is internal adapter behavior at the boundary.
3. **Avoiding reserved words is recommended**: although the framework's quoting can sidestep reserved word collisions (all 5 DBs exempt quoted identifiers from reserved-word rules), it's still recommended to avoid common reserved words like `comment`, `order`, `user`, `size`, `group`, `level`, `number` to reduce friction with hand-written SQL and tools.
4. **Data value comparison is case-insensitive**: the framework smooths over the differences across the 5 DBs (Oracle's session NLS makes up the gap), so hand-written `WHERE name = 'jeff'` behaves consistently across all 5 DBs.

---

## ✅ Conclusion

A unified naming convention ensures clarity and consistency in database structure, lowers communication cost during development and maintenance, and improves reliability and extensibility when integrating across systems.
