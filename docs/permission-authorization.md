[繁體中文](permission-authorization.zh-TW.md)

# Permission & Authorization Guide

Bee.NET permissions span **three dimensions**:

| Dimension | Question | Enforced by | Driven by |
|-----------|----------|-------------|-----------|
| **Action** | Can this user perform this *action* on the model? | **Back end (authoritative)** | role grants (action mask) |
| **Record (row)** | On *which rows* may they do it? | **Back end (authoritative)** | per-action scope strategy + the user's identity/department |
| **Field (column)** | May they *see / edit* this sensitive field? | **Front end (UX degradation)** | sensitive category + capability snapshot |

The first two are the **security boundary** — enforced at the method layer, entirely from in-memory snapshots at request time (the database is touched only when loading the caches, at login, on `EnterCompany`, or when configuration changes). The third is a **front-end affordance**: it hides or locks sensitive fields in the standard UI so users are not shown data they lack permission for, but it is not itself a data boundary (see the caveat in [section 10](#10-enabling-capability-in-a-host-app-opt-in)).

Authorization is **orthogonal** to `ApiAccessControlAttribute` (which governs encryption level and whether login is required). See [ADR-019](adr/adr-019-permission-authorization-model.md) for the design rationale.

---

# Part 1 — Back-end enforcement (Action + Record)

The Action and Record dimensions are the authoritative gate. Both run entirely from in-memory snapshots and are decoupled from forms.

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
- An empty `PermissionModelId` makes the form **unscoped** — both back-end layers are skipped (gradual adoption / backward compatible).

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

---

# Part 2 — Front-end capability (Field permission)

The front end degrades UI elements from a per-model **capability snapshot**, so users are not shown commands or sensitive data they lack permission for. This is **UX only** — the back end (Part 1) remains the authoritative boundary.

## 6. Mark sensitive fields

The Field dimension is **opt-in**: mark only the fields that need controlling. Most fields carry no marker and always render per their layout.

```xml
<FormField FieldName="unit_cost" Caption="Unit Cost" SensitiveCategory="Cost" />
```

`SensitiveCategory` (default `None` = not controlled) is a **named, finite classification** — `Amount`, `Cost`, `PersonalData` — parallel to `ScopeRole`. The designer picks a category rather than inventing an id, so the set is validated at load time. It applies to **any field**, master or detail grid column.

## 7. Well-known category models

Each non-`None` category maps **by convention** to a permission model whose id equals the category name (`Cost` → the `"Cost"` model). These are ordinary entries in the same `PermissionModels` registry — declare and grant them like any other model. `PermissionBindingValidator` fails at load time if a marked category has no matching model.

```sql
-- Whoever may see and edit cost data, company-wide:
INSERT INTO st_role_grant (role_id, model_id, action, scope) VALUES
  ('CostViewer', 'Cost', 2 /*Read*/,   1 /*All*/),
  ('CostEditor', 'Cost', 4 /*Update*/, 1 /*All*/);
```

The category gate is **company-wide and orthogonal to the form's own model**: seeing a `Cost` column depends on `Cost.Read`, *independent* of `PurchaseOrder.Read`. A user may be permitted to read purchase orders yet still have their cost columns hidden. This matches ERP practice — cost/amount/PII visibility is a data-classification concern that should be consistent across every form, granted once.

## 8. How the capability snapshot reaches the client

On `EnterCompany`, the back end computes the per-model action mask for the session's roles (`CompanyRolePermissions.GetAllowedByModel`) and returns it on `EnterCompanyResponse.Capabilities` — a `Dictionary<modelId, PermissionAction>` — riding the existing `EnterCompany` round-trip, so there is **no extra request**. Only models the user holds a grant on appear in the map.

## 9. How the client degrades

`ClientInfo.Capabilities` caches the snapshot (nullable), and `Bee.UI.Core.Permissions.ElementCapabilityResolver` (a pure, UI-agnostic resolver) reads it:

- **`null` → capability inactive → nothing is degraded.** An app that never enters a company, or does not use permissions, renders exactly as before.
- **Non-null → active.** A model absent from the map means *no permission* on it.

Two element kinds consume the resolver:

- **Commands** (toolbar buttons). Each button is tagged at creation with the `PermissionAction` it needs (`New`→`Create`, `Save`→`Create|Update`, `Delete`→`Delete`, `View`→`Read`); the resolver's `Can(...)` checks the form's `PermissionModelId` with **any-of** semantics (`Save` shows if the user holds either `Create` or `Update`). An un-permitted button is hidden. This is the front-end **projection of the Action dimension** as UX.
- **Sensitive fields.** `ResolveField(...)` reads the field's `SensitiveCategory`, looks up the category model, and degrades: **no `Read` → hidden; `Read` but no `Update` → read-only** (hidden wins over read-only). Applied to master fields and detail grid columns alike.

> **Detail grid actions (add/edit/delete rows) are not capability-gated.** A detail belongs to the same aggregate as its master, so whether its rows can be edited follows the form's edit mode — and the permission to enter that edit mode was already enforced by the toolbar command. Only the grid's sensitive *columns* are degraded.

Degradation never mutates cached definitions: the client applies it to the per-view generated layout, narrowing visibility/editability only.

## 10. Enabling capability in a host app (opt-in)

Capability is **inert until wired**, so existing apps are unaffected. To turn it on:

1. Declare the well-known category models (`Amount` / `Cost` / `PersonalData`) in `PermissionModels` and grant them (section 7).
2. After `SystemApiConnector.EnterCompanyAsync`, hand the response to the client cache:
   ```csharp
   var response = await ClientInfo.SystemApiConnector.EnterCompanyAsync(companyId);
   ClientInfo.ApplyEnterCompanyResult(response);   // caches the capability snapshot
   ClientInfo.ResetDefineCache();                  // (existing) drop stale tenant defines
   ```
3. On `LeaveCompany`, clear it: `ClientInfo.ClearCompanyContext();`.

> **Caveat — the Field dimension is UX, not a data boundary.** `GetList` / `GetData` still return the sensitive column's value; the client merely hides or locks it. A client that bypasses the standard UI could still receive the raw value over the API. Treat field permission as *presentation*. Anything that must never leave the server belongs behind an **Action** or **Record** boundary (Part 1), or its own permission model — not solely a `SensitiveCategory`. Server-side column masking is a separate future concern (see Non-goals).

---

## Caching & invalidation

- Role/grant/user-role tables load into a per-company `CompanyRolePermissions` cache; the department tree into a per-company `DepartmentTree` cache. Both are DB-sourced and evicted via the common cache-notify poller.
- `SessionInfo` holds the request-time snapshot (`Roles`, `UserRowId`, `EmployeeRowId`, `DeptRowId`), populated at `EnterCompany`, cleared at `LeaveCompany` / `Logout`.
- The client capability snapshot (`ClientInfo.Capabilities`) is also point-in-time: populated at `EnterCompany`, cleared on `LeaveCompany` / token change. Re-enter the company to refresh it after a grant change.
- Snapshots are point-in-time: configuration changed mid-session is reflected for cache-backed checks (`Can` reads the live cache) but role/employee/department snapshots on an already-entered session update on the next `EnterCompany`.

## Non-goals

- **Declarative custom-command model** — standard toolbar commands are tagged in code (section 9); Print / Export / Approve as *data-defined* `FormLayout` elements are not modelled yet. When added, custom commands will carry their own opt-in `PermissionAction`.
- **Back-end field masking** — the Field dimension is front-end UX. Server-side masking of sensitive columns (so their values never leave the server) is not yet implemented; use an Action/Record boundary for hard data confidentiality today.
