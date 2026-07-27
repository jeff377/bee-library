# Calendar-Day vs Instant Column Semantics

[繁體中文](date-semantics.zh-TW.md)

`FieldDbType` distinguishes `Date` (a calendar day: a birthday, an invoice date, an accounting
period) from `DateTime` (an instant: when a record was created, when a user logged in). `DataSet` is
the cross-layer DTO, and a `DataColumn` can only carry a calendar day as `DateTime` (`DateOnly` is
not a usable storage type), so that distinction is easy to lose — and until v4.15 the framework did
lose it once a value reached the CLR and the wire.

This document covers what changed, what you get for free, and the one case where you have to
declare the semantics yourself.

> Design rationale and the measurements behind it: [ADR-031](adr/adr-031-calendar-day-column-semantics.md).
> How instants (as opposed to calendar days) move between UTC and a user's zone:
> [datetime-timezone.md](datetime-timezone.md).

---

## 1. The short version

| Question | Answer |
|----------|--------|
| Did `DataColumn.DataType` change? | **No.** A calendar-day column is still a `DateTime` column. Nothing about `RowFilter`, `Sort`, `Compute`, string write-back, or existing casts changed. |
| What changed? | The **declared** `FieldDbType` now travels with the column and appears correctly on the wire. It used to be inferred from the CLR type, which reported every calendar day as `DateTime`. |
| Did the payload change shape? | **No.** Both wire formats already carried a `FieldDbType` per column. Only the value got accurate. Existing clients keep working. |
| Do I have to do anything? | Only for **hand-written SQL**. Schema-driven queries mark themselves. |
| Anything breaking? | `ValueUtilities.CDate` now returns `DateOnly`. See §5. |

## 2. What you get without doing anything

Any `DataTable` the framework builds from a `FormSchema` carries the declared field types:

- `DataFormRepository` queries — `GetList`, `GetData` (master and detail).
- Skeletons built by `GetNewData`.
- Anything built through `DataTableExtensions.AddColumn(name, FieldDbType)`.

The mark survives both wire formats in both directions, so a client that deserializes a payload
sees the same semantics the server sent.

## 3. Reading the semantics

### .NET consumers

```csharp
using Bee.Base.Data;

var dbType = column.ResolveFieldDbType();          // declared type, or inferred when unmarked
if (dbType == FieldDbType.Date)
{
    // calendar day: do not apply a timezone shift, do not show a time of day
}

var declared = column.GetDeclaredFieldDbType();    // null when the column carries no mark
```

`ResolveFieldDbType` falls back to inferring from `DataColumn.DataType` when a column is unmarked,
so it is always safe to call — an unmarked `DateTime` column reads as `FieldDbType.DateTime`,
exactly as before.

### JavaScript / TypeScript consumers

The `type` field of a column was always there; it is now accurate:

```jsonc
{
  "name": "order_date",
  "type": "Date",           // was "DateTime" before v4.15
  "allowNull": false,
  // …
}
```

```ts
const isCalendarDay = (col: DataTableColumn) => col.type === 'Date';
```

Treat a `Date` column as a plain calendar date: render it without a time of day, and **do not**
convert it through the browser timezone — `new Date("2026-07-25T00:00:00")` west of UTC lands on
the previous day. See [JSON-RPC Frontend Integration](jsonrpc-frontend-integration.md) for the
surrounding wire shapes.

## 4. Hand-written SQL: declare it yourself

ADO.NET reports a `date` column as `System.DateTime`, so a query the framework did not generate
has nothing to recover the semantics from. The rule is:

> **The framework marks the SQL it generates. You mark the SQL you write.**

Two equivalent ways, sharing one implementation:

```csharp
// A. Declare next to the query — the option travels with the SQL.
var spec = new DbCommandSpec(DbCommandKind.DataTable,
    "SELECT order_date, created_at, amount FROM ft_order WHERE amount > {0}", 1000m);
spec.DateColumns.Add("order_date");
var table = dbAccess.Execute(spec).Table!;

// B. Mark afterwards — for tables you assemble yourself or receive from elsewhere.
table.SetDateColumns("order_date", "due_date");
```

Both match column names case-insensitively (result columns are canonicalized to lowercase), and
both **throw on a name that matches no column** rather than skipping it — a typo that silently did
nothing would reproduce the exact failure this mechanism exists to remove. Setting `DateColumns` on
a command kind that returns no table throws for the same reason.

If you build the table from a `FormTable` you already have, replay the whole schema instead of
naming columns one at a time:

```csharp
using Bee.Definition.Forms;

formTable.ApplyFieldDbTypes(table);   // marks every column the schema declares
```

Columns the schema does not cover are left alone (aggregates and expression columns are normal),
and fields the query did not return are skipped (partial `SELECT`s are normal).

**Forgetting to declare is the one failure mode this design keeps.** An unmarked calendar-day
column looks like an instant to everything downstream — most consequentially to timezone
conversion, where it can shift across a day boundary.

## 5. Breaking change: `ValueUtilities.CDate`

```csharp
// Before
public static DateTime CDate(object value, DateTime defaultValue = default)

// v4.15 onwards
public static DateOnly CDate(object value, DateOnly defaultValue = default)
```

A call site written as `DateTime d = ValueUtilities.CDate(x)` becomes a **compile error**, not a
runtime failure. Migrate either way:

```csharp
DateOnly day = ValueUtilities.CDate(row["order_date"]);        // want a calendar day
DateTime dt  = ValueUtilities.CDateTime(row["order_date"]);    // want a DateTime
```

`CDateTime` is unchanged and now also accepts a `DateOnly` input.

> **Do not write a `DateOnly` back into a `DataTable`.** A calendar-day column is a `DateTime`
> column carrying a mark, and `DataColumn` rejects a `DateOnly` value outright — `DateOnly` does
> not implement `IConvertible`, so the usual conversion never runs. Use `CDateTime` when the value
> is going back into a row.

## 6. Also fixed by the same mechanism

Three other `FieldDbType` pairs shared a CLR type and were equally unrecoverable. They are now
accurate on the wire too:

| Declared | CLR type | Reported before | Reported now |
|----------|----------|-----------------|--------------|
| `Date` | `DateTime` | `DateTime` | `Date` |
| `Text` | `string` | `String` | `Text` |
| `Currency` | `decimal` | `Decimal` | `Currency` |
| `AutoIncrement` | `int` | `Integer` | `AutoIncrement` |

Because `DbTypeConverter.ToType` maps each of these to the same CLR type as before, a client
rebuilding a table from the payload gets **identical column types**. Only the reported
`FieldDbType` changed.

## Related

- [ADR-031: Calendar-Day Column Semantics](adr/adr-031-calendar-day-column-semantics.md) — the decision, the rejected alternatives, and the `DataColumn`/`DateOnly` measurements behind them
- [JSON-RPC Frontend Integration](jsonrpc-frontend-integration.md) — wire shapes for JS/TS consumers
- [Database Naming Conventions](database-naming-conventions.md) — column naming and cross-DB case sensitivity
- [Development Constraints and Anti-Patterns](development-constraints.md) — framework constraints worth reading before writing AnyCode queries
