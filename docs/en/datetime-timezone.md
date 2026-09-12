<!-- source: zh-TW/datetime-timezone.md blob: 265315d89fa42f4c88acd34c24a3d35e2e3bfa7a -->
# Time Zones

[繁體中文](../zh-TW/datetime-timezone.md) · [← Docs Index](README.md)

The database stores every instant in UTC; each user sees it in their own time zone. The conversion
happens in one place — the API connector on the client — so neither your business objects nor your
UI code performs it.

A `DateTime` value is only ever written by the server: on save, the `DateTime` values a client sends
are not used. The server fills them in, or keeps the value already stored in the database.

This document covers what the framework does for you, the cases where you have to act, and how to
configure a user's zone.

> Design rationale and the measurements behind it: [ADR-032](../adr/adr-032-datetime-timezone.md).
> Calendar-day versus instant semantics, and the other two temporal types:
> [Temporal Types](temporal-types.md).

---

## 1. The short version

| Question | Answer |
|----------|--------|
| Where is time converted? | In the client's `Connector`. `DateTime` values in a response move into the user's zone; a request converts only its filter values. Nowhere else. |
| What does the database hold? | UTC, in ordinary columns with no time zone (`datetime2`, `timestamp`, `DATETIME`, `TIMESTAMP`). |
| What travels on the wire? | Responses are always UTC. Filter values in a request are UTC; a `DataSet` sent for saving keeps the values shown on screen, and the server does not take its `DateTime` values. |
| Does a `DateTime` sent by a client reach the database? | No. New rows are filled in by the server, modified and deleted rows keep their stored values, and `sys_insert_time` / `sys_update_time` are stamped by the framework. See §2. |
| Which columns convert? | Those whose CLR type is `DateTime` and that are not marked `Date`. A calendar-day column carrying the `Date` marker does not convert; an unmarked one is converted as an instant. See §3. |
| What about strongly typed properties and `Parameters`? | Neither direction converts them. They are always UTC, and the caller is responsible (for example `ExpiredAt`, `FromUtc` / `ToUtc`). |
| Where does the user's zone come from? | `st_user.time_zone`, carried on the session — never the device's zone. |
| Do my business objects need changing? | No, unless they write hand-rolled SQL that filters on a date, or need to accept a `DateTime` entered by the user. See §3. |

## 2. What you get without doing anything

A `DataSet` or `DataTable` produced from a `FormSchema` carries each column's declared
`FieldDbType`, and the connector uses it:

- `DateTime` columns are shifted from UTC into the user's zone on the way in.
- `Date` columns are left alone. Shifting a calendar day would move a birthday or an invoice date
  onto the wrong day.

On save, the connector does not convert the `DataSet`, and the server's `FormBusinessObject.Save` does
not take its `DateTime` values. Before any rule runs, it replaces them with the server's own:

- New rows: `sys_insert_time`, `sys_update_time` and any `DateTime` field without a default-value
  expression receive the current UTC time; a field with a `DefaultValueExpression` is left to the
  expression.
- Modified and deleted rows: `DateTime` fields take the value stored in the database, and a modified
  row's `sys_update_time` then receives the current UTC time.

Rules, the audit trail and the write therefore all see UTC, and saving a row after editing some other
field leaves its instants unchanged — the hour a DST fall-back repeats included. The same holds when
one business object calls `Save` on another on the server.

New rows opened in the UI are seeded on the user's own day — a leave request filed from New York
against a Taipei account still defaults to the Taipei date.

Because the decision rides on the column marker rather than on a schema lookup, this also holds for
report and AnyCode results that have no `FormSchema` behind them.

## 3. What you have to do

### Hand-written SQL that returns calendar-day columns

The framework marks columns it generates from a schema. A query you write yourself must declare its
calendar-day columns, or the connector will treat them as instants and shift them across a day
boundary:

```csharp
var command = new DbCommandSpec(DbCommandKind.DataTable, sql) { DateColumns = { "invoice_date" } };
```

This is the same declaration [Temporal Types §4](temporal-types.md) describes; there is nothing
extra to do for time zones.

### Filter values

Filters are the one place a request is converted. A filter carries no column, so its value's own type
states the semantics:

```csharp
FilterCondition.Equal("invoice_date", someDateOnly);   // calendar day — never shifted
FilterCondition.Equal("created_at", someDateTime);     // instant — converted to UTC on send
```

Passing a `DateTime` where you meant a calendar day produces no error. The query simply returns the
wrong rows near midnight, which is the hardest kind of bug to notice — so prefer `DateOnly` (which
is what `ValueUtilities.CDateOnly` returns) whenever the column is a `Date`.

### Accepting a `DateTime` entered by the user

The framework does not support this by default. A `DateTime` field on a plain `FormSchema` form can only
be written by the server, so model a date the user edits as a `Date` field, and mark the system
timestamp fields `ReadOnly` in the `FormSchema` so no one edits a value that will not be saved.

When you do need it, override `FormBusinessObject.NormalizeDateTimes` in a custom business object: read
the value the user sent, call the base implementation, then write the value back converted to UTC in
the user's zone. Authorization and the write-scope checks have already run by then.

### JavaScript and other non-.NET clients

There is no connector to do the work: render a `DateTime` value by converting from UTC, and convert a
filter's `DateTime` value back to UTC before sending. A `DataSet` sent for saving needs no conversion,
since the server does not take its `DateTime` values. A `Date` value must be passed through
untouched — in particular, do not let `new Date(...)` reinterpret it in the browser's zone. Column
types arrive in the payload, so the client can tell the two apart without extra metadata; see
[jsonrpc-frontend-integration.md](jsonrpc-frontend-integration.md).

## 4. Configuring a user's time zone

`st_user.time_zone` holds an IANA id (`Asia/Taipei`, `America/New_York`). Login copies it onto the
session and returns it to the client.

A user with no value of their own falls back to `BackendConfiguration.DefaultTimeZone`, which ships
as `Asia/Taipei`. Set it to the zone your deployment actually runs in — or to an empty string to use
UTC, which is what every conversion point does with a blank zone.

The framework never falls back to the device's zone: a user travelling with a laptop would otherwise
change the meaning of the data they enter, and the value they see and the value the server stores
would come from two different sources.

There is deliberately no per-company or per-column override. When a value must be shown in some
*other* zone — an attendance record read in the employee's work-site zone, say — model it as a UTC
instant plus a time zone column of your own, because that requirement is per-row and no
column-level setting can express it.

## 5. Dates outside the `DataSet`

Dates are `DateOnly` throughout the framework. The single exception is a `DataSet` cell, where a
`DataColumn` can only hold `DateTime` — the framework converts at that boundary so you do not have
to. `Today()` in an expression yields a `DateOnly` in the user's zone; `UtcNow()` states UTC
outright. See [Expression Rules](expression-rules.md) for the full function list.

## Related

- [Temporal Types: `Date`, `DateTime` and `Time`](temporal-types.md) — the cross-layer reference:
  choosing between the three semantics, and how each is carried at every layer.
- [ADR-032](../adr/adr-032-datetime-timezone.md) — the decision itself, with the measurements behind it.
