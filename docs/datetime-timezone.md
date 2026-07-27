# Time Zones

[繁體中文](datetime-timezone.zh-TW.md)

The database stores every instant in UTC; each user sees it in their own time zone. The conversion
happens in one place — the API connector on the client — so neither your business objects nor your
UI code performs it.

This document covers what the framework does for you, the two cases where you have to act, and what
changed if you are upgrading.

> Design rationale and the measurements behind it: [ADR-032](adr/adr-032-datetime-timezone.md).
> Calendar-day versus instant semantics: [date-semantics.md](date-semantics.md).

---

## 1. The short version

| Question | Answer |
|----------|--------|
| Where is time converted? | In the client's `Connector`, both directions. Nowhere else. |
| What does the database hold? | UTC, in ordinary columns with no time zone (`datetime2`, `timestamp`, `DATETIME`, `TIMESTAMP`, `TEXT`). |
| What travels on the wire? | UTC, in **both** directions. |
| Which columns convert? | Those declared `FieldDbType.DateTime`. A `Date` column is a calendar day and never converts. |
| Where does the user's zone come from? | `st_user.time_zone`, carried on the session — never the device's zone. |
| Do my business objects need changing? | No, unless they write hand-rolled SQL that filters on a date. See §3. |
| Anything breaking? | `ValueUtilities.CDate` and the `Today()` expression helper now return `DateOnly`. See §5. |

## 2. What you get without doing anything

A `DataSet` or `DataTable` produced from a `FormSchema` carries each column's declared
`FieldDbType`, and the connector uses it:

- `DateTime` columns are shifted from UTC into the user's zone on the way in, and back to UTC on
  the way out. The two directions are exact inverses, so a value that makes a round trip is
  unchanged.
- `Date` columns are left alone. Shifting a calendar day would move a birthday or an invoice date
  onto the wrong day.

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

This is the same declaration [date-semantics.md](date-semantics.md) describes; there is nothing
extra to do for time zones.

### Filter values

A filter carries no column, so its value's own type states the semantics:

```csharp
FilterCondition.Equal("invoice_date", someDateOnly);   // calendar day — never shifted
FilterCondition.Equal("created_at", someDateTime);     // instant — converted to UTC on send
```

Passing a `DateTime` where you meant a calendar day produces no error. The query simply returns the
wrong rows near midnight, which is the hardest kind of bug to notice — so prefer `DateOnly` (which
is what `ValueUtilities.CDate` returns) whenever the column is a `Date`.

### JavaScript and other non-.NET clients

There is no connector to do the work, so the client owns both directions: render a `DateTime` value
by converting from UTC, and convert back before sending. A `Date` value must be passed through
untouched — in particular, do not let `new Date(...)` reinterpret it in the browser's zone. Column
types arrive in the payload, so the client can tell the two apart without extra metadata; see
[jsonrpc-frontend-integration.md](jsonrpc-frontend-integration.md).

## 4. Configuring a user's time zone

`st_user.time_zone` holds an IANA id (`Asia/Taipei`, `America/New_York`). Login copies it onto the
session and returns it to the client.

An empty value means UTC. The framework never falls back to the device's zone: a user travelling
with a laptop would otherwise change the meaning of the data they enter, and the value they see and
the value the server stores would come from two different sources.

There is deliberately no per-company or per-column override. When a value must be shown in some
*other* zone — an attendance record read in the employee's work-site zone, say — model it as a UTC
instant plus a time zone column of your own, because that requirement is per-row and no
column-level setting can express it.

## 5. What changed

| Change | Impact |
|--------|--------|
| `ValueUtilities.CDate` returns `DateOnly` | Call sites assigning the result to a `DateTime` need updating. Writing it into a `DataSet` cell still works — the framework widens it at that boundary. |
| `Today()` in expressions returns `DateOnly`, in the user's zone | Existing `DefaultValueExpression="Today()"` keeps working on both `Date` and `DateTime` fields. |
| `UtcNow()` added to expressions | New; use it where you want UTC stated outright. |
| `st_user.time_zone` column added | Existing rows have no value, which reads as UTC. Set it per user to enable conversion. |
| PostgreSQL `DateTime` parameters now map to `timestamp` | Previously they were sent as `timestamptz`, which let the server's zone re-express the value. No action needed; column types are unchanged. |

Dates are `DateOnly` throughout the framework. The single exception is a `DataSet` cell, where a
`DataColumn` can only hold `DateTime` — the framework converts at that boundary so you do not have
to.
