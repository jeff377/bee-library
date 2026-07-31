# Temporal Types: `Date`, `DateTime` and `Time`

[繁體中文](temporal-types.zh-TW.md)

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
| Time-zone converted? | **Never** | **Yes** (UTC ↔ user zone) | **Never** |
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
  [ADR-033](adr/adr-033-time-of-day-semantics.md).

### Sorting and range queries

All three sort and range-scan correctly in SQL. For `Time` this works because values are
**fixed-width and zero-padded**, making lexicographic order chronological:

```sql
SELECT * FROM ft_shift WHERE work_start BETWEEN '08:00' AND '17:00' ORDER BY work_start
```

The framework normalises every value written through `ToFieldValue` or the time editor, so a
hand-written `INSERT` is the only way to break that guarantee.

## 4. `DataSet` layer

```csharp
table.AddColumn("hire_date",  FieldDbType.Date);       // DataColumn.DataType == typeof(DateTime)
table.AddColumn("created_at", FieldDbType.DateTime);   // DataColumn.DataType == typeof(DateTime)
table.AddColumn("work_start", FieldDbType.Time);       // DataColumn.DataType == typeof(string)
```

Every `DataTable` the framework builds carries the declared type, recoverable with:

```csharp
FieldDbType declared = column.ResolveFieldDbType();      // Date / DateTime / Time
FieldDbType? marked  = column.GetDeclaredFieldDbType();  // null when the column carries no marker
```

`ResolveFieldDbType` falls back to inferring from `DataColumn.DataType` when a column is unmarked, so
it is always safe to call — an unmarked `DateTime` column reads back as `FieldDbType.DateTime`.

This is populated automatically for schema-driven queries (`GetList`, `GetData`, `GetNewData`) and
anything built through `AddColumn(name, FieldDbType)`. Hand-written SQL is the one exception.

> **Do not write a `DateOnly` into a `DataTable`.** A calendar-day column is a `DateTime` column
> carrying a marker, and `DataColumn` rejects a `DateOnly` outright — `DateOnly` does not implement
> `IConvertible`, so the usual conversion never runs. Use `CDateTime` when writing back.

### Hand-written SQL: declare it yourself

ADO.NET reports a `date` column as `System.DateTime`, so a query the framework did not generate has
nothing to recover the semantics from. The rule is:

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

Both match column names case-insensitively (result columns are canonicalized to lowercase), and both
**throw on a name that matches no column** rather than skipping it — a typo that silently did nothing
would reproduce the exact failure this mechanism exists to remove. Setting `DateColumns` on a command
kind that returns no table throws for the same reason.

If you build the table from a `FormTable` you already have, replay the whole schema instead of naming
columns one at a time:

```csharp
using Bee.Definition.Forms;

formTable.ApplyFieldDbTypes(table);   // marks every column the schema declares
```

Columns the schema does not cover are left alone (aggregates and expression columns are normal), and
fields the query did not return are skipped (partial `SELECT`s are normal).

**Forgetting to declare is the one failure mode this design keeps.** An unmarked calendar-day column
looks like an instant to everything downstream — most consequentially to time-zone conversion, where
it can shift across a day boundary.

## 5. Code layer

```csharp
// One argument -> nullable. An unset value is a case the compiler makes you handle.
DateOnly? day     = ValueUtilities.CDateOnly(row["hire_date"]);
DateTime? instant = ValueUtilities.CDateTime(row["created_at"]);
TimeOnly? start   = ValueUtilities.CTimeOnly(row["work_start"]);

// Two arguments -> non-null, with the fallback stated at the call site.
DateTime created = ValueUtilities.CDateTime(row["created_at"], DateTime.MinValue);
```

Two properties hold across the whole family:

- **The method name matches the return type**, so a call site tells you what it yields.
- **The one-argument form returns a nullable.** Unset is then a case the compiler forces you to
  handle, rather than a sentinel you have to remember to compare against — and a leaked sentinel is
  a real failure: `0001-01-01` rendered in a report is worse than a null-reference at the boundary.

When a non-null value is wanted, pass the fallback explicitly. Stating it is the point: it makes
the choice visible instead of hiding it in an omitted default argument.

All three are **lenient about what they accept and strict about what they return**. `CTimeOnly`
takes `"8:30"`, a `DateTime` or an in-range `TimeSpan`; `CDateTime` takes Gregorian and ROC date
strings (`20150312`, `1040312`). Anything out of range or unrecognisable comes back `null` rather
than a guess.

## 6. Serialization

All three formats are self-describing: the column's `FieldDbType` travels with the payload, so a
consumer can tell a calendar day from an instant **without fetching the schema**.

The examples below are the real serializer output for these three values:
`hire_date = 2026-07-27`, `created_at = 2026-07-27 08:30:15.1234567`, `work_start = 08:30`.

### XML — `DataSet` persistence

The declared type is written into the XSD as an `msprop` annotation, so it survives a
write/read round trip:

```xml
<xs:element name="hire_date"  msdata:DateTimeMode="Unspecified" msprop:Bee.FieldDbType="Date"     type="xs:dateTime" />
<xs:element name="created_at" msdata:DateTimeMode="Unspecified" msprop:Bee.FieldDbType="DateTime" type="xs:dateTime" />
<xs:element name="work_start"                                   msprop:Bee.FieldDbType="Time"     type="xs:string" />

<hire_date>2026-07-27T00:00:00</hire_date>
<created_at>2026-07-27T08:30:15.1234567</created_at>
<work_start>08:30</work_start>
```

`DateTimeMode="Unspecified"` is what keeps a time-zone offset out of the XML. The .NET default for a
fresh `DateTime` column is `UnspecifiedLocal`, which *does* write an offset — the framework sets
`Unspecified` everywhere so a persisted `DataSet` cannot shift when read back elsewhere.

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

| | Stored | Shown |
|---|--------|-------|
| `Date` | as written | as written |
| `DateTime` | **UTC** | converted to the session's zone |
| `Time` | as written | as written |

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

- [ADR-031](adr/adr-031-calendar-day-column-semantics.md) — why the calendar-day semantic needs an
  explicit marker, the alternatives rejected, and the `DataColumn`/`DateOnly` measurements behind it.
- [ADR-033](adr/adr-033-time-of-day-semantics.md) — why `Time` is a fixed-width string rather than
  a native database time type, with the measurements behind the decision.
- [Time Zones](datetime-timezone.md) — UTC storage and conversion for instants.
  [ADR-032](adr/adr-032-datetime-timezone.md).
- [Terminology](terminology.md) — the four-term vocabulary (calendar day / time of day / instant /
  duration).
