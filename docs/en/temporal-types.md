<!-- source: zh-TW/temporal-types.md blob: 63db7c10e2b48aa5ed1fdcbda06e3c03064f3978 -->
# Temporal Types: `Date`, `DateTime` and `Time`

[繁體中文](../zh-TW/temporal-types.md) · [← Docs Index](README.md)

The framework distinguishes three temporal semantics, and each one is carried differently at every
layer — database column, `DataColumn`, CLR value, and all three serialization formats. This document
is the single cross-layer reference; the linked documents go deeper on each semantic individually.

---

## 1. Choosing the right one

| Semantic | `FieldDbType` | Question it answers | Examples |
|----------|---------------|---------------------|----------|
| **Calendar day** | `Date` | *Which day?* | Birthday, invoice date, accounting period |
| **Instant** | `DateTime` | *What time on which day?* | Created-at, login timestamp, clock-in record |
| **Time of day** | `Time` | *What time (within a day)?* | Shift start/end, opening hours, reminder time |

The test: **ask whether the value needs to know which day.**

- It does → **instant**.
- It does not, and the question is *what time* → **time of day**.
- It does not, and the question is *which day* → **calendar day**.
- The question is *how long* → that is a **duration**, which the framework has no type for yet.
  Use a `Decimal` (hours). See §8.

## 2. End-to-end at a glance

| | `Date` | `DateTime` | `Time` |
|---|--------|-----------|--------|
| Declared as | `DbType="Date"` | `DbType="DateTime"` | `DbType="Time"` |
| `DataColumn.DataType` | `DateTime` | `DateTime` | **`string`** |
| Distinguishable from CLR type alone? | **No** — shares `DateTime` | No | **Yes** |
| How the semantic survives | `ExtendedProperties` marker | (default for the CLR type) | the CLR type itself |
| Read it as | `CDateOnly` → `DateOnly?` | `CDateTime` → `DateTime?` | `CTimeOnly` → `TimeOnly?` |
| Unset value | `DateTime.MinValue` → `DBNull` | `DateTime.MinValue` → `DBNull` | **empty string** |
| Time-zone converted? | **No** — provided the column carries the marker (§4) | **Yes** (UTC ↔ user zone) | **Never** |
| Default UI editor | `DateEdit` | `DateEdit` | `TimeEdit` |

The one structural difference: `Date` and `DateTime` **share a CLR type**, so the calendar-day
semantic would be lost the moment a value left the definition layer. It is preserved by an explicit
marker on the column. `Time` needs no marker — a `string` column is already unambiguous.

Declaring any of the three is the same one line, and the layout layer derives the editor from it —
no layout change is needed to get a date picker or a time input:

```xml
<DbField FieldName="hire_date"  Caption="Hire Date" DbType="Date" />
<DbField FieldName="created_at" Caption="Created"   DbType="DateTime" />
<DbField FieldName="work_start" Caption="Start"     DbType="Time" />
```

## 3. Database layer

| Database | `Date` | `DateTime` | `Time` |
|----------|--------|-----------|--------|
| SQL Server | `date` | `datetime2(7)` | `nchar(5)` |
| PostgreSQL | `date` | `timestamp` | `char(5)` |
| MySQL | `DATE` | `DATETIME(6)` | `CHAR(5)` |
| SQLite | `DATE` | `DATETIME` | `VARCHAR(5)` |
| Oracle | `DATE` | `TIMESTAMP(6)` | `VARCHAR2(5)` |

Two things worth knowing:

- **`DateTime` is stored as UTC in a naive column.** No provider stores an offset; the framework
  converts on the way out. See [Time Zones](datetime-timezone.md).
- **`Time` is not a native database time type.** Every supported database except Oracle has one, but
  their semantics disagree (MySQL's `TIME` is a *duration* spanning ±838 hours) and the .NET
  `DataSet` cannot carry the CLR types they return. A fixed-width string sidesteps all of it and
  stays readable in a raw `SELECT`. The full measurements are in
  [ADR-033](../adr/adr-033-time-of-day-semantics.md).

### Sorting and range queries

All three sort and range-scan correctly in SQL. For `Time` this works because values are
**fixed-width and zero-padded**, making lexicographic order chronological:

```sql
SELECT * FROM ft_shift WHERE work_start BETWEEN '08:00' AND '17:00' ORDER BY work_start
```

Values written from the UI are normalised (`"8:30"` is stored as `"08:30"`): the time editor, a cell
in the Avalonia `GridControl` and `FormDataObject.SetField` all go through the single implementation
in `FormValueBinding.ToColumnValue`. **The server does not normalise again** — the database stores
whatever the `DataSet` holds — so the following writes fall outside that guarantee and must normalise
with `ValueUtilities.CTimeString` themselves:

- Assigning a `DataRow` directly (`row["work_start"] = "8:30"`).
- A column with no `Time` marker — for example a time-of-day column read back by hand-written SQL
  (`DateColumns` / `SetDateColumns` mark calendar days only). See the unmarked list in §4 for the rest.
- Data sent by a non-.NET client.
- A hand-written `INSERT` or `UPDATE`.

## 4. `DataSet` layer

```csharp
table.AddColumn("hire_date",  FieldDbType.Date);       // DataColumn.DataType == typeof(DateTime)
table.AddColumn("created_at", FieldDbType.DateTime);   // DataColumn.DataType == typeof(DateTime)
table.AddColumn("work_start", FieldDbType.Time);       // DataColumn.DataType == typeof(string)
```

When a column carries the declared-type marker, it is recoverable with:

```csharp
FieldDbType declared = column.ResolveFieldDbType();      // Date / DateTime / Time
FieldDbType? marked  = column.GetDeclaredFieldDbType();  // null when the column carries no marker
```

`ResolveFieldDbType` falls back to inferring from `DataColumn.DataType` when a column is unmarked, so
it is always safe to call — an unmarked `DateTime` column reads back as `FieldDbType.DateTime`.

Paths that currently carry the marker:

- Columns built through `AddColumn(name, FieldDbType)` — the empty table `GetNewData` returns is built
  this way.
- Schema-driven queries (`GetList`, `GetData`): the result is marked afterwards with
  `ApplyFieldDbTypes`, **covering only the fields the schema declares**.
- Tables whose command declared `DbCommandSpec.DateColumns`, or that were marked afterwards with
  `SetDateColumns` / `ApplyFieldDbTypes` (see the next section).
- Tables rebuilt after travelling as JSON or MessagePack — every column is marked, but with **the type
  the sending side declared**; see below.
- Tables produced by `Copy`, `Clone`, `DataView.ToTable` or `Merge` keep the source columns' markers,
  and `DataSet.ReadXml` restores them from XML that embeds its schema (see §6).

Cases that carry no marker:

- Results of hand-written SQL, unless declared as in the next section. Tables the framework itself
  reads with hand-written SQL (the audit-log queries, for example) are no exception.
- Columns in a schema-driven query that the schema does not declare (aggregates, expression columns).
- Columns you create yourself with `Columns.Add` or `new DataColumn`.
- Tables you read yourself with `DataTable.Load` or `DbDataAdapter.Fill`.
- `DataSet.ReadXml` on XML without a schema — every column then comes back as `string`, not merely
  unmarked.

An unmarked column that goes over the wire is declared as the type inferred from its CLR type, and the
receiving side marks the rebuilt column with that type. A calendar-day column left unmarked before
sending therefore arrives **marked as `DateTime`**, not unmarked — the marker has to be applied before
the table leaves the side that built it. A table from a non-.NET client works the same way: the marker
follows the `type` the payload declares (a JSON column that omits `type` is taken as `String`).

> **Do not write a `DateOnly` into a `DataTable`.** A calendar-day column is a `DateTime` column
> carrying a marker, and `DataColumn` rejects a `DateOnly` outright — `DateOnly` does not implement
> `IConvertible`, so the usual conversion never runs. Use `CDateTime` when writing back.

### Hand-written SQL: declare it yourself

ADO.NET reports a `date` column as `System.DateTime`, so a query the framework did not generate has
nothing to recover the semantics from. The rule is:

> **The framework marks schema-driven queries. You mark the SQL you write.**

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

Both match column names case-insensitively (result columns are canonicalized to lowercase), and both
**throw `ArgumentException` on a name that matches no column** rather than skipping it — a typo that
silently did nothing would reproduce the exact failure this mechanism exists to remove. Setting
`DateColumns` on a command kind that returns no table throws `InvalidOperationException` when the
command is created, for the same reason.

Both mark calendar days only. To mark a time-of-day column read back by hand-written SQL, use
`table.ApplyFieldDbType(FieldDbType.Time, "work_start")` — an unmarked time-of-day column is not
normalised when the UI writes to it (see §3).

If you build the table from a `FormTable` you already have, replay the whole schema instead of naming
columns one at a time:

```csharp
using Bee.Definition.Forms;

formTable.ApplyFieldDbTypes(table);   // marks every column the schema declares
```

Columns the schema does not cover are left alone (aggregates and expression columns are normal), and
fields the query did not return are skipped (partial `SELECT`s are normal).

**The failure mode this design does not remove: a calendar-day column that carries no marker, or is
marked `DateTime`.** It arises from forgetting to declare, from the unmarked paths listed above, and
from crossing the wire while still unmarked. Such a column looks like an instant to everything
downstream — most consequentially to time-zone conversion, where it can shift across a day boundary.

## 5. Code layer

```csharp
// One argument -> nullable. An unset value is a case the compiler makes you handle.
DateOnly? day     = ValueUtilities.CDateOnly(row["hire_date"]);
DateTime? instant = ValueUtilities.CDateTime(row["created_at"]);
TimeOnly? start   = ValueUtilities.CTimeOnly(row["work_start"]);

// Two arguments -> non-null, with the fallback stated at the call site.
DateTime created = ValueUtilities.CDateTime(row["created_at"], DateTime.MinValue);
```

Two properties hold across `CDateTime`, `CDateOnly` and `CTimeOnly` (the other `Cxxx` methods, such as
`CInt` and `CStr`, keep the default-value shape and are not part of this):

- **The method name matches the return type**, so a call site tells you what it yields.
- **The one-argument form returns a nullable.** Unset is then a case the compiler forces you to
  handle, rather than a sentinel you have to remember to compare against — and a leaked sentinel is
  a real failure: `0001-01-01` rendered in a report is worse than a null-reference at the boundary.

When a non-null value is wanted, pass the fallback explicitly. Stating it is the point: it makes
the choice visible instead of hiding it in an omitted default argument.

All three are **lenient about what they accept and strict about what they return**. `CTimeOnly`
takes `"8:30"`, a `DateTime` or an in-range `TimeSpan`; `CDateTime` takes Gregorian and ROC date
strings (`20150312`, `1040312`), and also numeric strings that stop at the month or the year, filling
the missing part with the first month or day (`201503` reads as 2015-03-01, `2015` as 2015-01-01).
Anything out of range or unrecognisable comes back `null`.

## 6. Serialization

All three formats can describe themselves: the column's `FieldDbType` travels with the payload, so a
consumer can tell a calendar day from an instant **without fetching the schema**. What travels is the
type the column resolves to at serialization time, so this presumes calendar-day columns are marked
before serializing (see §4); XML additionally has to be written with its schema.

The examples below are the real serializer output for these three values:
`hire_date = 2026-07-27`, `created_at = 2026-07-27 08:30:15.1234567`, `work_start = 08:30`.

### XML — `DataSet` persistence

Written with `XmlWriteMode.WriteSchema`, the declared type goes into the XSD as an `msprop` annotation,
so it survives a write/read round trip; the DiffGram and `IgnoreSchema` modes carry no schema and do
not keep it. `DataSet.ReadXml` restores the annotation as the member name in string form
rather than as a `FieldDbType` value; `GetDeclaredFieldDbType` and `ResolveFieldDbType` accept both
forms, so read the marker through them rather than from `ExtendedProperties` directly:

```xml
<xs:element name="hire_date"  msdata:DateTimeMode="Unspecified" msprop:Bee.FieldDbType="Date"     type="xs:dateTime" />
<xs:element name="created_at" msdata:DateTimeMode="Unspecified" msprop:Bee.FieldDbType="DateTime" type="xs:dateTime" />
<xs:element name="work_start"                                   msprop:Bee.FieldDbType="Time"     type="xs:string" />

<hire_date>2026-07-27T00:00:00</hire_date>
<created_at>2026-07-27T08:30:15.1234567</created_at>
<work_start>08:30</work_start>
```

`DateTimeMode="Unspecified"` is what keeps a time-zone offset out of the XML. The .NET default for a
fresh `DateTime` column is `UnspecifiedLocal`, which *does* write an offset. Tables the framework
builds itself — columns from `AddColumn`, `DbAccess` query results, tables rebuilt from JSON or
MessagePack — are set to `Unspecified`, so a persisted `DataSet` does not shift when read back
elsewhere. A table you build with `Columns.Add` or `DataTable.Load` keeps the .NET default; call
`NormalizeDateTimeMode` before persisting it as XML.

Note the full 100-nanosecond precision survives.

### JSON

The column type is emitted as the **enum name**:

```json
{
  "columns": [
    { "name": "hire_date",  "type": "Date" },
    { "name": "created_at", "type": "DateTime" },
    { "name": "work_start", "type": "Time" }
  ],
  "rows": [
    { "state": "Unchanged",
      "current": {
        "hire_date":  "2026-07-27T00:00:00",
        "created_at": "2026-07-27T08:30:15.1234567",
        "work_start": "08:30"
      } }
  ]
}
```

For a JS/TS consumer:

```js
// A calendar day and an instant look identical in the value — the column type is what separates them.
const day     = row.current.hire_date.slice(0, 10);   // "2026-07-27" — do not build a Date and format it
const instant = new Date(row.current.created_at);     // safe to convert to the user's zone
const start   = row.current.work_start;               // "08:30", or "" when unset
```

> **A calendar day must not be passed through a JS `Date` and reformatted.** The value carries no
> offset, so the browser reads it in local time and a westward zone shifts it to the previous day.
> Slice the date portion instead.

### MessagePack

The column type rides as the enum's **underlying integer** (unlike JSON, which uses the name), and
cell values are typeless: a `DateTime` cell travels as a native MessagePack timestamp, a `Time` cell
as a string. Round-tripping a table restores the CLR type *and* the marker:

```
col hire_date : clr=DateTime marker=Date     value=2026-07-27 00:00:00
col created_at: clr=DateTime marker=DateTime value=2026-07-27 08:30:15
col work_start: clr=String   marker=Time     value=08:30
```

Because the integer is positional, **new `FieldDbType` members are only ever appended** — inserting
one mid-enum would shift every later value and break existing payloads.

### Filter conditions

`FilterCondition.Value` is typeless and validated against an allow-list. `System.DateTime`,
`System.DateOnly` and `System.String` are all permitted, so filtering on any of the three semantics
works — pass a time of day as its `"HH:mm"` string:

```csharp
FilterCondition.Equal("hire_date", ValueUtilities.CDateOnly(x));   // DateOnly — allowed
FilterCondition.Equal("work_start", "08:30");                      // string — allowed
```

`System.TimeOnly` is **not** on the allow-list; convert to the string form before it reaches a
filter.

## 7. Time zones

**Only `DateTime` is ever converted.** Calendar days and times of day are wall-clock values;
shifting them by an offset produces a meaningless result — a birthday would move to the previous day
and an 08:00 shift would start at 16:00 in another zone.

What decides it is the column's resolved type: a column whose CLR type is `DateTime` is converted as
an instant unless it is marked `Date`. **An unmarked calendar-day column is therefore converted** — see
§4 for the paths that carry no marker. A time-of-day column is a `string` and never in scope.

| | Stored | Shown | Value a client saves |
|---|--------|-------|------|
| `Date` | as written | as written | written as sent |
| `DateTime` | **UTC** | converted to the session's zone | **not used** — the server writes it |
| `Time` | as written | as written | written as sent |

Details, including what hand-written SQL and non-.NET clients must do:
[Time Zones](datetime-timezone.md).

## 8. What none of them is: a duration

None of the three answers *how long*. A duration has no position on a clock or calendar — working
hours, elapsed time, a timeout. **The framework has no duration type yet; use a `Decimal` (hours).**

This matters most when you are tempted to derive a length by subtracting two `Time` values via
`TimeOnly`, whose subtraction **wraps around midnight and is always positive**:

| Expression | Result | Verdict |
|------------|--------|---------|
| `22:00` → `06:00` | 8 hours | Correct for a night shift |
| `08:00` → `08:00` | **0 hours** | Wrong — a 24-hour shift reads as zero |

In a modulo-24 world "a whole day" and "zero" are the same point. **Store a duration as its own
field rather than deriving it from two times of day.**

## 9. Common mistakes

| Mistake | What happens | Do instead |
|---------|--------------|-----------|
| Using `DateTime` for a shift definition | Carries a meaningless date, and gets time-zone shifted | `Time` |
| Using `Time` for a clock-in record | Loses which day; a night shift ending 06:00 is unrecoverable | `DateTime` |
| Writing a `DateOnly` into a `DataTable` | Throws — `DataColumn` rejects it | `CDateTime` |
| Not declaring calendar-day columns in hand-written SQL | They look like instants downstream and get time-zone shifted across a day boundary | `DateColumns` / `SetDateColumns` / `ApplyFieldDbTypes` |
| Reformatting a calendar day through a JS `Date` | Shifts a day backwards in westward zones | Slice the date portion |
| Treating `"00:00"` as "no time set" | Midnight is a legal value | Empty string means unset |
| Deriving shift length by subtracting two `Time`s | A 24-hour shift computes as 0 | Store the length |
| Inserting a new `FieldDbType` mid-enum | Breaks every existing MessagePack payload | Append |

## Related

- [ADR-031](../adr/adr-031-calendar-day-column-semantics.md) — why the calendar-day semantic needs an
  explicit marker, the alternatives rejected, and the `DataColumn`/`DateOnly` measurements behind it.
- [ADR-033](../adr/adr-033-time-of-day-semantics.md) — why `Time` is a fixed-width string rather than
  a native database time type, with the measurements behind the decision.
- [Time Zones](datetime-timezone.md) — UTC storage and conversion for instants.
  [ADR-032](../adr/adr-032-datetime-timezone.md).
- [Terminology](terminology.md) — the four-term vocabulary (calendar day / time of day / instant /
  duration).
