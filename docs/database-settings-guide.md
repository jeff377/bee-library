# DatabaseSettings & DbCategorySettings Guide

[繁體中文](database-settings-guide.zh-TW.md)

> This document explains the structure, positioning, access patterns, and runtime behavior of the two database-related settings files in the Bee.NET framework, helping developers understand the full chain of settings → connection → category routing.

## Table of Contents

1. [Overview](#1-overview)
2. [DatabaseSettings](#2-databasesettings)
3. [DbCategorySettings](#3-dbcategorysettings)
4. [Access Entry Points & Caching](#4-access-entry-points--caching)
5. [CategoryId Wiring](#5-categoryid-wiring)
6. [File Locations & Examples](#6-file-locations--examples)

---

## 1. Overview

The two settings files together support the chain of "FormSchema definition → logical category → physical connection". Their responsibilities split as follows:

| Settings File | Question Answered | Entities |
|---------------|-------------------|----------|
| **DatabaseSettings** | What "physical" database connections exist in the system? | DatabaseServer (server config) + DatabaseItem (connection items) |
| **DbCategorySettings** | What "logical" database categories exist? Which tables belong to each? | DbCategory (category) + TableItem (table registry) |

### Wiring Diagram

```text
FormSchema.CategoryId ─────┐
                           │
                           ├──► DbCategory.Id  (in DbCategorySettings)
                           │       └─ Tables  (tables registered for this category)
DatabaseItem.CategoryId ───┘

DatabaseItem.ServerId  ────► DatabaseServer.Id  (in DatabaseSettings.Servers)
```

Key concepts:
- **FormSchema.CategoryId** is used at design time: it declares which logical category the TableSchema derived from this FormSchema should belong to (determining the TableSchema output directory).
- **DatabaseItem.CategoryId** is used at deployment time: it declares which logical category the physical connection belongs to, used to derive "which tables this physical DB should contain".
- **DbCategory** is the common target of both, with `Id` as a plain string (e.g. `common` / `company` / `log`).
- ⚠️ **At runtime, connection retrieval goes entirely through `DatabaseItem.Id` and never touches CategoryId or DbCategorySettings.**

### Logical vs Physical: Mapping Model

`DbCategory` is a **pure logical abstraction** — it only defines "which tables this category contains"; it does not correspond to a number of physical databases or specify where they should be deployed. `DatabaseItem.CategoryId` and `DbCategory.Id` form a **many-to-one** relationship — the same category can have multiple DatabaseItems. Common scenarios:

- **Single physical carrier** (e.g. `common`): 1 DatabaseItem
- **Multi-tenant partitioning** (e.g. `company`): N companies have N DatabaseItems (`company001`, `company002`...), each pointing to a separate physical DB for that company
- **Time-based archival partitioning** (e.g. `log`): partitioned by year with multiple DatabaseItems (`log_2024`, `log_2025`...), each pointing to a separate physical DB for that year
- The two partitioning dimensions can stack (e.g. partition a category by both company and year)

Multiple DatabaseItems may point to the same physical DB (consolidated deployment) or to separate physical DBs (distributed or multi-carrier deployment).

Four typical deployment scenarios (all using the three logical categories common / company / log as examples):

**Scenario 1: Consolidated deployment (one physical DB contains all three categories)**

```text
DatabaseSettings.Items (3 entries)                  Physical DBs (1)
─────────────────────────────                       ────────────────
DatabaseItem  CategoryId="common"   DbName=erp ──┐
DatabaseItem  CategoryId="company"  DbName=erp ──┼──► erp (contains st_user, st_session,
DatabaseItem  CategoryId="log"      DbName=erp ──┘     st_company, st_user_company,
                                                       ft_department, ft_employee,
                                                       ft_project, log tables)
```

**Scenario 2: Distributed deployment (three physical DBs, one per logical category)**

```text
DatabaseSettings.Items (3 entries)                  Physical DBs (3)
─────────────────────────────                       ────────────────
DatabaseItem  CategoryId="common"   DbName=erp_common  ──► erp_common  (st_user, st_session, st_company, st_user_company)
DatabaseItem  CategoryId="company"  DbName=erp_company ──► erp_company (ft_department, ft_employee, ft_project)
DatabaseItem  CategoryId="log"      DbName=erp_log     ──► erp_log     (log tables)
```

**Scenario 3: Multi-tenant deployment (1 entry each for shared categories + 1 entry per company for company)**

```text
DatabaseSettings.Items (2 + N entries)              Physical DBs (2 + N)
─────────────────────────────                       ────────────────
DatabaseItem  Id="common"      CategoryId="common"   ──► erp_common
DatabaseItem  Id="company001"  CategoryId="company"  ──► company001  (ft_department, ft_employee, ft_project)
DatabaseItem  Id="company002"  CategoryId="company"  ──► company002  (ft_department, ft_employee, ft_project)
DatabaseItem  Id="company003"  CategoryId="company"  ──► company003  (ft_department, ft_employee, ft_project)
   ⋮          (one entry per company)
DatabaseItem  Id="log"         CategoryId="log"      ──► erp_log
```

The `companyXXX` physical DBs all have identical table structures (all derived from `DbCategory["company"].Tables`), but their data is separate.

**Scenario 4: log archived by year (one log entry per year)**

```text
DatabaseSettings.Items (2 + Y entries)              Physical DBs (2 + Y)
─────────────────────────────                       ────────────────
DatabaseItem  Id="common"     CategoryId="common"   ──► erp_common
DatabaseItem  Id="company"    CategoryId="company"  ──► erp_company
DatabaseItem  Id="log_2024"   CategoryId="log"      ──► log_2024  (log tables)
DatabaseItem  Id="log_2025"   CategoryId="log"      ──► log_2025  (log tables)
DatabaseItem  Id="log_2026"   CategoryId="log"      ──► log_2026  (log tables)
   ⋮          (new entry per year)
```

The `log_YYYY` physical DBs all have identical table structures (all derived from `DbCategory["log"].Tables`). The application writes using the DatabaseId for the current year, and queries can aggregate across multiple DatabaseItems. Scenarios 3 and 4 can stack (e.g. partitioning by both company and year).

The application is unaware of the deployment shape: it always retrieves connections through `IDatabaseSettingsProvider.GetItem(databaseId)` (DI ctor injected). The only difference is how the application layer decides which `databaseId` to pass:

- Scenarios 1, 2: fixed mapping (category → DatabaseId)
- Scenario 3: derived from the current tenant id (e.g. `$"company{tenantId:D3}"`)
- Scenario 4: derived from the current year (e.g. `$"log_{DateTime.UtcNow.Year}"`); cross-year queries aggregate multiple entries

**Scenarios can be freely combined**: the four above are basic patterns — actual deployments can combine them in any way to meet cost / performance / operational needs. For example, in a multi-tenant scenario, to avoid "log centralizing all tenants" becoming a performance bottleneck, you can switch to "**company and log share the same physical DB per tenant**":

```text
DatabaseSettings.Items                                Physical DBs
─────────────────────────                             ────────────
DatabaseItem  Id="common"           CategoryId="common"   ──► erp_common
DatabaseItem  Id="company001"       CategoryId="company"  ──┐
DatabaseItem  Id="log_company001"   CategoryId="log"      ──┴► company001  (contains both ft_* and log_* tables)
DatabaseItem  Id="company002"       CategoryId="company"  ──┐
DatabaseItem  Id="log_company002"   CategoryId="log"      ──┴► company002  (contains both ft_* and log_* tables)
   ⋮          (2 DatabaseItems per company, sharing the same physical DB)
```

Each company has 2 DatabaseItems declaring company and log categories respectively, but their DbName points to the same physical DB — the physical DB contains both ft_* and log_* tables. Log data is distributed across tenants, avoiding cross-tenant centralization.

Key principle: **Logical categories and physical deployment are two independent dimensions, freely combinable**. Choose the partitioning strategy based on data volume, query patterns, and operational cost; the framework places no restrictions on how they combine.

---

## 2. DatabaseSettings

Definition location: [`src/Bee.Definition/Settings/DatabaseSettings/`](../src/Bee.Definition/Settings/DatabaseSettings/)

### 2.1 Structure

```text
DatabaseSettings
├── Servers : DatabaseServerCollection   shared server configurations (connection templates)
│     └── DatabaseServer
└── Items   : DatabaseItemCollection     actual database connection entries
      └── DatabaseItem
```

### 2.2 DatabaseServer Fields

Defines a "shared server configuration" that multiple DatabaseItems can reference to share connection templates and credentials.

| Field | Type | Purpose |
|-------|------|---------|
| `Id` | string | Server identifier (Key) |
| `DisplayName` | string | Display name |
| `DatabaseType` | DatabaseType | `SQLServer` / `PostgreSQL`, etc. |
| `ConnectionString` | string | Connection string template, can contain `{@DbName}` / `{@UserId}` / `{@Password}` placeholders |
| `UserId` | string | Login id, replaces `{@UserId}` |
| `Password` | string | Login password, replaces `{@Password}`; encrypted automatically on serialization |

### 2.3 DatabaseItem Fields

Defines a "physical connection entry" — the unit that actually establishes connections at runtime.

| Field | Type | Purpose |
|-------|------|---------|
| `Id` | string | Connection identifier (Key); callers retrieve connections by this id |
| `CategoryId` | string | Logical category id this entry belongs to (corresponds to `DbCategory.Id`) |
| `DisplayName` | string | Display name |
| `DatabaseType` | DatabaseType | `SQLServer` / `PostgreSQL`, etc. |
| `ServerId` | string | Referenced DatabaseServer Id (optional) |
| `ConnectionString` | string | Standalone connection string (used when not referencing a Server) |
| `DbName` | string | Database name, replaces `{@DbName}` |
| `UserId` | string | Login id (overrides Server settings) |
| `Password` | string | Login password (overrides Server settings); encrypted automatically on serialization |

> 📌 **DatabaseItem and logical category are many-to-one**: `CategoryId` is a single string, not a collection — each DatabaseItem belongs to exactly one category; but **the same category can have multiple DatabaseItems**, with common scenarios including multi-tenant partitioning (e.g. one company per entry under company) and time-based archival partitioning (e.g. one year per entry under log). See [§1 Logical vs Physical: Mapping Model](#logical-vs-physical-mapping-model).

### 2.4 Choosing Server vs Item

Two usage patterns:

- **Reference a Server**: `DatabaseItem.ServerId` points to a Server; the connection string template comes from the Server, and the Item only overrides `DbName` and (when needed) `UserId` / `Password`. Suitable when multiple Items share the same server but use different DBs.
- **Standalone configuration**: `ServerId` is left blank; specify `ConnectionString`, `UserId`, `Password` directly on the Item. Suitable for a single connection or when connection settings vary significantly.

### 2.5 Connection String Template Substitution

The connection string can use three placeholders that the framework substitutes when establishing connections:

| Placeholder | Replacement Source |
|-------------|--------------------|
| `{@DbName}` | `DatabaseItem.DbName` |
| `{@UserId}` | `DatabaseItem.UserId` (falls back to `DatabaseServer.UserId` if empty) |
| `{@Password}` | `DatabaseItem.Password` (falls back to `DatabaseServer.Password` if empty) |

Example:
```xml
<DatabaseServer Id="sql_main" DatabaseType="SQLServer"
                ConnectionString="Server=sql.example;Database={@DbName};User ID={@UserId};Password={@Password};" />
<DatabaseItem Id="company_main" CategoryId="company" ServerId="sql_main"
              DbName="erp_company" UserId="erp_user" Password="..." />
```

### 2.6 Password Encryption

`DatabaseServer.Password` and `DatabaseItem.Password` are encrypted automatically during XML serialization:

- **Encryption**: AES-CBC-HMAC (key derived from the configured `ConfigEncryptionKey`, threaded into `LocalDefineAccess` via ctor injection at `AddBeeFramework` time)
- **Storage format**: `enc:` prefix + Base64-encoded ciphertext
- **Timing**:
  - Before serialization (`BeforeSerialize`): unencrypted Passwords are encrypted automatically
  - After deserialization (`AfterDeserialize`): Passwords starting with `enc:` are decrypted automatically
- **Behavior**: if `ConfigEncryptionKey` is empty, encryption / decryption is skipped (plaintext storage, development environments only)

Implementation: [`DatabaseSettings.cs`](../src/Bee.Definition/Settings/DatabaseSettings/DatabaseSettings.cs) — `BeforeSerialize` / `AfterDeserialize` / `DecryptPassword`.

---

## 3. DbCategorySettings

Definition location: [`src/Bee.Definition/Settings/DbCategorySettings/`](../src/Bee.Definition/Settings/DbCategorySettings/)

### 3.1 Structure

```text
DbCategorySettings
└── Categories : DbCategoryCollection
      └── DbCategory
            └── Tables : TableItemCollection
                  └── TableItem
```

### 3.2 DbCategory Fields

| Field | Type | Purpose |
|-------|------|---------|
| `Id` | string | Category identifier (Key); FormSchema / DatabaseItem map by this id |
| `DisplayName` | string | Display name (e.g. "Shared Database") |
| `Tables` | TableItemCollection | Table registry under this category |

### 3.3 TableItem Fields

| Field | Type | Purpose |
|-------|------|---------|
| `TableName` | string | Table name (Key) |
| `DisplayName` | string | Display name (e.g. "User") |

The `Tables` child node currently serves as a **documentation index of tables registered under this category**, providing a lookup entry; the actual schema authority remains with `TableSchema` and `FormSchema` files.

### 3.4 Three Default Categories

The framework uses three default logical categories:

| Category Id | Purpose | Typical Tables |
|-------------|---------|----------------|
| `common` | Shared database — system tables shared across companies | `st_user`, `st_session`, `st_company`, `st_user_company` |
| `company` | Company database — business data, separate per company | `ft_department`, `ft_employee`, `ft_project` |
| `log` | Log database — audit / operation records with frequent writes | (depends on the application) |

**`common` is a framework-mandated contract**: it must exist and `DatabaseItem.Id == CategoryId == "common"` (enforced at startup by `services.AddBeeFramework`; system services such as `SessionRepository` route through the fixed `databaseId = "common"`). See the [`DbCategoryIds`](../src/Bee.Definition/Database/DbCategoryIds.cs) constants.

`company` and `log` are default logical categories provided by the framework; usage is left to the business (a single-tenant setup may skip `log`, multi-tenant setups may add custom categories). For custom categories, the `CategoryId` in FormSchema and DatabaseItem must match a category id declared in `DbCategorySettings`.

---

## 4. Access Entry Points & Caching

### 4.1 Unified Entry

Both settings are accessed through `IDefineAccess` (DI ctor injected):

```csharp
public class MyService(IDefineAccess defineAccess)
{
    public void Demo()
    {
        // Read
        DatabaseSettings dbSettings = defineAccess.GetDatabaseSettings();
        DbCategorySettings catSettings = defineAccess.GetDbCategorySettings();

        // Write
        defineAccess.SaveDatabaseSettings(dbSettings);
        defineAccess.SaveDbCategorySettings(catSettings);
    }
}
```

`IDefineAccess` is registered as a singleton during `AddBeeFramework`, defaulting to `LocalDefineAccess` (file system); projects can configure `RemoteDefineAccess` (via API) through the XML `Components` registry.

### 4.2 Caching

Both settings are held centrally by the DI-registered [`ICacheContainer`](../src/Bee.ObjectCaching/ICacheContainer.cs) (default implementation `CacheContainerService`), with `Lazy<T>` lazy initialization:

| Cache | Holder |
|-------|--------|
| `DatabaseSettings` | `ICacheContainer.DatabaseSettings` (`Lazy<DatabaseSettingsCache>`) |
| `DbCategorySettings` | `ICacheContainer.DbCategorySettings` (`Lazy<DbCategorySettingsCache>`) |

Behavior:
- **20-second sliding expiration**: reloaded if not accessed for 20 seconds
- **File change monitoring**: cache is invalidated automatically when the underlying file changes
- **Save-then-invalidate**: calling `Save*` immediately clears the corresponding cache; the next `Get*` reloads

### 4.3 Common Lookups

```csharp
// Get a single connection entry (DI-injected IDatabaseSettingsProvider)
DatabaseItem item = dbSettingsProvider.GetItem("company_main");

// Get all tables under a category (via the indexer)
DbCategory company = catSettings.Categories!["company"];
foreach (var table in company.Tables!) { ... }
```

`IDatabaseSettingsProvider.GetItem` throws `KeyNotFoundException` when the id is not found, allowing callers to detect unknown connections.

### 4.4 API Access Restrictions

| Settings | Accessible via remote API? |
|----------|----------------------------|
| DatabaseSettings | ❌ No (contains sensitive connection info; `SystemBusinessObject.GetDefine` rejects non-local calls) |
| DbCategorySettings | ✅ Yes (no sensitive data; can be retrieved via `RemoteDefineAccess`) |

---

## 5. CategoryId Wiring

`CategoryId` is the **central correlation key** of this design, threading through three layers:

### 5.1 FormSchema Definition Phase

Every FormSchema must declare its category:

```xml
<FormSchema ProgId="EmployeeForm" CategoryId="company" ...>
  <FormTable TableName="ft_employee" ...>
    ...
  </FormTable>
</FormSchema>
```

When persisting, [`LocalDefineAccess.SaveFormSchema`](../src/Bee.ObjectCaching/LocalDefineAccess.cs) enforces that `CategoryId` is non-empty (via [`TableSchemaGenerator.GetCategoryId`](../src/Bee.Definition/Database/TableSchemaGenerator.cs)); otherwise it throws `InvalidOperationException`.

### 5.2 TableSchema Output Path

TableSchemas derived from FormSchemas are stored in directories grouped by CategoryId:

```text
<DefinePath>/TableSchema/
              ├── common/
              │     ├── st_user.TableSchema.xml
              │     ├── st_session.TableSchema.xml
              │     ├── st_company.TableSchema.xml
              │     └── st_user_company.TableSchema.xml
              ├── company/
              │     ├── ft_department.TableSchema.xml
              │     └── ft_employee.TableSchema.xml
              └── log/
```

Path resolution: [`PathOptions.GetTableSchemaFilePath(categoryId, tableName)`](../src/Bee.Definition/PathOptions.cs) (DI ctor injected).

### 5.3 Deployment Phase: Deriving the Table List for Each Physical DB

`DatabaseItem.CategoryId` is used at the schema deployment phase (when creating or upgrading physical database table structures). **The unit of computation is DatabaseItem**: for each DatabaseItem, the derivation and DDL flow runs once, applying to the physical DB pointed to by that DatabaseItem's connection.

Derivation flow for a single DatabaseItem:

1. Read `DatabaseItem.CategoryId` (e.g. `"company"`)
2. Get the registered table list from `DbCategorySettings.Categories["company"].Tables`
3. For each `TableName`, load the physical table structure from `<DefinePath>/TableSchema/company/{TableName}.TableSchema.xml`
4. Execute DDL on the physical DB through this DatabaseItem's connection (table creation or schema upgrade)

```text
[DatabaseItem]            [DbCategorySettings]              [TableSchema files]
 └─ CategoryId ─────────► DbCategory[id]                    └─ TableSchema/{cid}/{table}.xml
   ("company")             └─ Tables (tables for category)      └─ provides table structure
```

Because derivation runs independently per DatabaseItem, both deployment scenarios (single physical DB vs multiple physical DBs, see §1) are handled identically:

- **Scenario 1** (3 DatabaseItems all pointing to the same physical DB): derivation runs 3 times, all connecting to the same physical DB, building the common / company / log tables coexisting in that DB.
- **Scenario 2** (3 DatabaseItems pointing to 3 separate physical DBs): derivation runs 3 times, building tables on each respective physical DB; each physical DB only contains the tables for its category.

This design fully decouples schema definitions (which tables, what structure) from physical deployment (whether to split, how many DBs). Adding a new logical category requires only changes to `DbCategorySettings.xml` and the corresponding FormSchema; changing the physical deployment layout requires only changes to the connection info of each DatabaseItem in `DatabaseSettings.xml`, leaving the definition files untouched.

### 5.4 Runtime: Database Access

⚠️ **At runtime, retrieving a connection goes entirely through `DatabaseItem.Id` and is completely independent of `DbCategorySettings`**:

```csharp
DatabaseItem item = dbSettingsProvider.GetItem(databaseId);
// Use item.ConnectionString / DbName / UserId / Password to establish the connection and run SQL
```

Application code chooses `databaseId` based on "which logical category the data belongs to" + "the current context (tenant, time, etc.)":

| Data to Access | Category | Deployment Scenario | DatabaseId Used |
|----------------|----------|---------------------|-----------------|
| `st_user`, `st_session`, `st_company`, `st_user_company` | common | Any | Fixed string `"common"` (framework contract: `DatabaseItem.Id == "common"`) |
| `ft_employee`, `ft_department` | company | Single company | The `Id` of the entry with `CategoryId=company` |
| `ft_employee`, `ft_department` | company | Multi-tenant | The `Id` of `companyXXX` for the current tenant (e.g. `$"company{tenantId:D3}"`) |
| Log writes | log | Single DB | The `Id` of the entry with `CategoryId=log` |
| Log writes | log | Year-based archival | The `log_YYYY` for the current year (e.g. `$"log_{DateTime.UtcNow.Year}"`) |
| Log cross-year queries | log | Year-based archival | Multiple DatabaseIds for the year range, queried separately and aggregated |

Regardless of the underlying scenario, the application always uses the same entry `IDatabaseSettingsProvider.GetItem(databaseId)`; the only difference is "how to derive the databaseId string from the current context".

For bo repos (the BO-layer Repositories) the framework provides `IRepositoryDatabaseRouter` (see [ADR-010 §「後續延伸：執行時路由」](adr/adr-010-logical-database-category.md)) so that BO code does not have to derive the databaseId by hand:

| Source | How the databaseId is derived |
|--------|------------------------------|
| `DbScope.Common` | Fixed string `"common"` (no session required) |
| `DbScope.Log` | Fixed string `"log"` (no session required; `Login` / `Logout` etc. can write audit log pre-EnterCompany) |
| `DbScope.Company` | `SessionInfo.CompanyId` (set by `EnterCompany`) → `CompanyInfo.CompanyDatabaseId` (looked up from `ICompanyInfoService`) |

BO methods consume the router via `BusinessObject.ResolveDatabaseId(DbScope)` or, for FormSchema-driven CRUD, the `CreateDataFormRepository(progId)` helper which routes automatically based on the schema's `CategoryId`.

Cross-DatabaseItem aggregation (e.g. log year-range queries) and explicit archive access (e.g. reading `log_2024`) are not part of the default routing path — the application layer specifies the target databaseId directly and creates a custom bo repo via `IDbAccessFactory`.

CategoryId and DbCategorySettings are used only at the design phase (§5.1–5.2) and the deployment phase (§5.3) described above. At runtime, BO methods speak in terms of `DbScope` (the runtime access intent) rather than CategoryId strings.

---

## 6. File Locations & Examples

### 6.1 File Paths

Both settings files are located at the root of `PathOptions.DefinePath`:

| Settings | File Path | Resolution |
|----------|-----------|------------|
| DatabaseSettings | `<DefinePath>/DatabaseSettings.xml` | `PathOptions.GetDatabaseSettingsFilePath()` |
| DbCategorySettings | `<DefinePath>/DbCategorySettings.xml` | `PathOptions.GetDbCategorySettingsFilePath()` |

### 6.2 DatabaseSettings.xml Example

```xml
<?xml version="1.0" encoding="utf-8"?>
<DatabaseSettings xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                  xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <Servers>
    <DatabaseServer Id="sql_main" DisplayName="Main SQL Server"
                    DatabaseType="SQLServer"
                    ConnectionString="Server=sql.example;Database={@DbName};User ID={@UserId};Password={@Password};" />
  </Servers>
  <Items>
    <!-- common category: single physical carrier; Id matches CategoryId -->
    <DatabaseItem Id="common" CategoryId="common" DisplayName="Shared Database"
                  DatabaseType="SQLServer" ServerId="sql_main"
                  DbName="erp_common" UserId="erp_user"
                  Password="enc:base64encodeddata..." />

    <!-- company category: multi-tenant partitioning, one entry per company -->
    <DatabaseItem Id="company001" CategoryId="company" DisplayName="Company 01 Database"
                  DatabaseType="SQLServer" ServerId="sql_main"
                  DbName="company001" UserId="erp_user"
                  Password="enc:base64encodeddata..." />
    <DatabaseItem Id="company002" CategoryId="company" DisplayName="Company 02 Database"
                  DatabaseType="SQLServer" ServerId="sql_main"
                  DbName="company002" UserId="erp_user"
                  Password="enc:base64encodeddata..." />

    <!-- log category: time-based archival partitioning, one entry per year -->
    <DatabaseItem Id="log2025" CategoryId="log" DisplayName="2025 Log Database"
                  DatabaseType="SQLServer" ServerId="sql_main"
                  DbName="log2025" UserId="erp_user"
                  Password="enc:base64encodeddata..." />
    <DatabaseItem Id="log2026" CategoryId="log" DisplayName="2026 Log Database"
                  DatabaseType="SQLServer" ServerId="sql_main"
                  DbName="log2026" UserId="erp_user"
                  Password="enc:base64encodeddata..." />
  </Items>
</DatabaseSettings>
```

### 6.3 DbCategorySettings.xml Example

```xml
<?xml version="1.0" encoding="utf-8"?>
<DbCategorySettings xmlns:xsd="http://www.w3.org/2001/XMLSchema"
                    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Categories>
    <DbCategory Id="common" DisplayName="Shared Database">
      <Tables>
        <TableItem TableName="st_user" DisplayName="User" />
        <TableItem TableName="st_session" DisplayName="Session" />
        <TableItem TableName="st_company" DisplayName="Company" />
        <TableItem TableName="st_user_company" DisplayName="User-Company access" />
      </Tables>
    </DbCategory>
    <DbCategory Id="company" DisplayName="Company Database">
      <Tables>
        <TableItem TableName="ft_department" DisplayName="Department" />
        <TableItem TableName="ft_employee" DisplayName="Employee" />
        <TableItem TableName="ft_project" DisplayName="Project" />
      </Tables>
    </DbCategory>
    <DbCategory Id="log" DisplayName="Log Database">
      <Tables />
    </DbCategory>
  </Categories>
</DbCategorySettings>
```

---

## Related Documents

- [Architecture Overview](architecture-overview.md) — Definition-Driven architecture overview
- [Development Cookbook](development-cookbook.md) — framework initialization and development flow
- [Database Naming Conventions](database-naming-conventions.md) — table / column naming rules
- [ADR-005: FormSchema-Driven Architecture](adr/adr-005-formschema-driven.md)
- [ADR-010: Logical Database Category](adr/adr-010-logical-database-category.md) — why DbCategory was introduced
