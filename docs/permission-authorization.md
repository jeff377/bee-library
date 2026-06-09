[繁體中文](permission-authorization.zh-TW.md)

# Permission & Authorization Guide

Bee.NET authorization is **two-layer** and decoupled from forms:

| Layer | Question | Driven by |
|-------|----------|-----------|
| **Layer 1 — action gate** | Can this user perform this *action* on the model? | role grants (action mask) |
| **Layer 2 — record scope** | On *which rows* may they do it? | per-action scope strategy + the user's identity/department |

Both run entirely from in-memory snapshots at request time — the database is touched only when loading the caches, at login, on `EnterCompany`, or when configuration changes. Authorization is **orthogonal** to `ApiAccessControlAttribute` (which governs encryption level and whether login is required).

See [ADR-019](adr/adr-019-permission-authorization-model.md) for the design rationale.

## 1. Define permission models

A **permission model** is a business entity (e.g. `PurchaseOrder`), deliberately distinct from a form's `progId`. Models live in a single registry (`PermissionModels`, `DefineType.PermissionModels`). Each model declares, per action, a default record-scope strategy:

```xml
<PermissionModels>
  <PermissionModel ModelId="PurchaseOrder" DisplayName="Purchase Order">
    <Rules>
      <PermissionRule Action="Read"   Scope="DeptAndSub" />
      <PermissionRule Action="Update" Scope="Own" />
      <PermissionRule Action="Delete" Scope="Own" />
      <PermissionRule Action="Create" Scope="All" />
      <!-- Print / Export omit Scope → inherit the model's Read scope -->
    </Rules>
  </PermissionModel>
</PermissionModels>
```

- `ModelId` is a PascalCase business entity. **One model may be consumed by many forms** (`PO001` create, `PO002` query, `PO009` report all reference `PurchaseOrder`) — granting once enables all three.
- The model is bound to neither a table nor a column. Scope strategies are pure semantics; the concrete columns come from the FormSchema (next section).

## 2. Bind a form to the model

A `FormSchema` declares which model it consumes, and marks which **master-table** columns play the owner / department role:

```xml
<FormSchema ProgId="PO001" PermissionModelId="PurchaseOrder" ...>
  <Tables>
    <FormTable TableName="PO001" ...>
      <Fields>
        <FormField FieldName="buyer_rowid" Caption="Buyer"      ScopeRole="Owner" />
        <FormField FieldName="dept_rowid"  Caption="Department" ScopeRole="Dept" />
        <!-- ... other fields ... -->
      </Fields>
    </FormTable>
  </Tables>
</FormSchema>
```

Rules:

- `ScopeRole` is **master-table only**. Marking it on a detail table is a load-time validation error (`PermissionBindingValidator`) — record scope is decided on the master record; details follow it.
- At most one `Owner` and one `Dept` column per master table.
- An empty `PermissionModelId` makes the form **unscoped** — both layers are skipped (gradual adoption / backward compatible).

## 3. Grant roles

Roles, grants and assignments live in each **company database** (`st_` framework tables, per-company config):

| Table | Columns | Meaning |
|-------|---------|---------|
| `st_role` | `sys_id`, `sys_name` | a role (a unit you assign) |
| `st_role_grant` | `role_id`, `model_id`, `action`, `scope` | one (role, model, action) the role is granted, plus its record scope |
| `st_user_role` | `user_id`, `role_id` | user ↔ role assignment (`user_id` is `st_user.sys_id`) |

`st_role_grant` is **per-action**: the presence of a row is the layer-1 grant; its `scope` (a `ScopeStrategy`) drives layer-2. This is what lets a role *read* its whole department but *update* only its own records.

```sql
-- "Buyer" can read the department-and-sub PurchaseOrders, but only update/delete its own:
INSERT INTO st_role_grant (role_id, model_id, action, scope) VALUES
  ('Buyer', 'PurchaseOrder', 2 /*Read*/,   4 /*DeptAndSub*/),
  ('Buyer', 'PurchaseOrder', 4 /*Update*/, 2 /*Own*/),
  ('Buyer', 'PurchaseOrder', 8 /*Delete*/, 2 /*Own*/);
-- scope = ScopeStrategy: Inherit=0, All=1, Own=2, Dept=3, DeptAndSub=4
-- action = PermissionAction (flags): Create=1, Read=2, Update=4, Delete=8, Print=16, Export=32
```

`scope = Inherit (0)` defers to the model's per-action default (section 1).

## 4. Link users to employees (for department scope)

Department / owner scope needs to resolve **the current user → their department**. A user (`st_user`, common DB) is linked to an employee (`st_employee`, company DB) via `st_employee.user_rowid`:

```
st_user.sys_rowid  ──(st_employee.user_rowid)──▶  st_employee  ──(dept_rowid)──▶  st_department
```

On `EnterCompany`, the framework resolves `user → employee → department` once and snapshots `UserRowId`, `EmployeeRowId`, `DeptRowId` onto the session. Scope filtering then runs zero-DB. A user without a linked employee gets empty employee/department — `Own` still matches their `UserRowId`, while `Dept`/`DeptAndSub` match nothing (fail-closed).

## 5. How enforcement behaves

### Layer 1 — action gate

`FormBusinessObject` checks `(model, action)` before running:

- `GetList` / `GetData` → `Read`
- `Save` → per row by `RowState`: `Added`→`Create`, `Modified`→`Update`, `Deleted`→`Delete`
- `Delete` → `Delete`

Multiple roles **OR-merge** (capabilities accrue). A failing check throws `ForbiddenException`.

### Layer 2 — record scope

**Reads** (`GetList`, `GetData`) AND a scope filter into the query. Out-of-scope rows are filtered out; an out-of-scope single-row fetch returns `null` (indistinguishable from "not found", so a caller cannot probe records they may not see).

**Writes** (`Update`, `Delete`) are gated by an **authoritative re-query** against the database — `WHERE sys_rowid = id AND <scope>` — *not* by evaluating the submitted payload. A forged DataSet cannot relabel its way past the boundary.

- `Save` re-checks only an existing master record (any master `RowState` other than `Added`). A details-only edit leaves the master `Unchanged` but still counts as an `Update`.
- `Delete(rowId)` returns 0 and cascades nothing when the row is out of scope.
- **`Create` is not scope-checked** — a new row has no existing scope to violate; creation is governed by the action grant.
- Scope is **master-only**: once the master passes, the whole record (details included) persists as a unit.

### Scope strategies

| Strategy | Read filter (and write re-query) |
|----------|----------------------------------|
| `All` | no restriction |
| `Own` | `ownerField IN {UserRowId, EmployeeRowId}` |
| `Dept` | `deptField = DeptRowId` **OR** Own |
| `DeptAndSub` | `deptField IN (department + descendants)` **OR** Own |
| `Inherit` | the model's per-action default (else its Read scope, else `All`) |

- `Dept` / `DeptAndSub` **implicitly include `Own`** — a user always sees records they own.
- **Multi-role merge**: if *any* role grants `All` for the action → no filter; otherwise the restrictive strategies are **OR-unioned**.
- The `Own` owner column may hold either a user row id or an employee row id (e.g. the *creator* vs the *employee on a leave form*); the `IN {UserRowId, EmployeeRowId}` set covers both, and a user need not map to an employee.

## 6. Caching & invalidation

- Role/grant/user-role tables load into a per-company `CompanyRolePermissions` cache; the department tree into a per-company `DepartmentTree` cache. Both are DB-sourced and evicted via the common cache-notify poller.
- `SessionInfo` holds the request-time snapshot (`Roles`, `UserRowId`, `EmployeeRowId`, `DeptRowId`), populated at `EnterCompany`, cleared at `LeaveCompany` / `Logout`.
- Snapshots are point-in-time: configuration changed mid-session is reflected for cache-backed checks (`Can` reads the live cache) but role/employee/department snapshots on an already-entered session update on the next `EnterCompany`.

## Non-goals

- **Element-level capability** (button → action degradation in the UI) is a separate front-end concern; the back end enforces at the method layer and does not rely on the front end.
