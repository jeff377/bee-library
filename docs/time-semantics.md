# Time-of-Day Columns

[繁體中文](time-semantics.zh-TW.md)

`FieldDbType.Time` is the framework's third time semantic, alongside `Date` (a calendar day) and
`DateTime` (an instant). A time of day is a **wall-clock position within a day**, not tied to any
date: a shift boundary, opening hours, a reminder time.

Values are stored as a fixed-width `"HH:mm"` string — in the database, in the `DataSet`, and on the
wire. In code you read them as a `TimeOnly`.

> Design rationale, including why a string rather than a native database time type:
> [ADR-033](adr/adr-033-time-of-day-semantics.md).

---

## 1. The short version

| Question | Answer |
|----------|--------|
| What CLR type is the column? | **`string`.** The cell holds `"08:30"`. |
| How do I read it as a time? | `ValueUtilities.CTimeOnly(row["work_start"])` → `TimeOnly?`. |
| What's the range? | `00:00`–`23:59`, **minute precision**. No seconds. |
| How is "not filled in" represented? | The **empty string**. Never `"00:00"` — midnight is a legal value. |
| Does it sort correctly in SQL? | **Yes.** Fixed-width zero-padded values sort lexicographically in chronological order. |
| Is it affected by time zones? | **No.** A time of day is wall-clock, like a calendar day. It is never shifted. |
| Anything breaking? | Adding an enum member is a wire change; client and server must run the same version. See §6. |

## 2. When to use it

| Data | Type | Why |
|------|------|-----|
| Shift start / end, opening hours, reminder time | **`Time`** | A declaration that repeats every day; no date attached |
| A clock-in record, an audit timestamp | `DateTime` | You must know **which day**. A night shift ending at 06:00 ends the *next* day — storing only the time loses that permanently |
| How long something took | `Decimal` (hours) | A duration is not a time of day; the framework has no duration type yet |

If you find yourself subtracting two `Time` fields to get a length, that is the signal you wanted a
duration, not a time of day. See §7.

## 3. Declaring a time-of-day field

Nothing special — declare it like any other field type:

```xml
<DbField FieldName="work_start" Caption="Start" DbType="Time" />
```

The column is created as a fixed-width 5-character column on every supported database:

| Database | Column type |
|----------|-------------|
| SQL Server | `nchar(5)` |
| PostgreSQL | `char(5)` |
| MySQL | `CHAR(5)` |
| SQLite | `VARCHAR(5)` |
| Oracle | `VARCHAR2(5)` |

The layout layer resolves a `Time` field to `ControlType.TimeEdit` automatically, so the UI gives
you a time input without any layout change.

## 4. Reading and writing

### .NET

```csharp
// Read
TimeOnly? start = ValueUtilities.CTimeOnly(row["work_start"]);
if (start is null) { /* not filled in */ }

// Write — the framework normalises on the way in, so "8:30" is stored as "08:30"
row["work_start"] = FieldDbType.Time.ToFieldValue("8:30");   // "08:30"
```

`CTimeOnly` returns **`TimeOnly?`**, not a `TimeOnly` with a default. That is deliberate: a time of
day has no spare value to mean "unset", because `default(TimeOnly)` is `00:00` — a perfectly legal
midnight. A nullable return makes the caller handle the unfilled case instead of silently reading
an empty field as midnight.

It is lenient about what it accepts (`"8:30"`, a `DateTime`, an in-range `TimeSpan`) and strict
about what it returns: anything out of range or malformed comes back `null`.

### JS / TS clients

The value is just a string — no parsing helper required:

```js
const start = row.current.work_start;   // "08:30", or "" when unset
```

The column's `FieldDbType` on the wire is `Time`, so a schema-less consumer can tell a time of day
from an arbitrary text field without fetching the schema.

## 5. Querying

Because values are fixed-width and zero-padded, ordinary string comparison is chronological:

```sql
SELECT * FROM ft_shift WHERE work_start BETWEEN '08:00' AND '17:00' ORDER BY work_start
```

This holds on every supported database and under every collation — the characters involved are
digits and a colon.

> This guarantee depends on values being zero-padded. The framework normalises everything written
> through `ToFieldValue` / the time editor, so hand-written `INSERT`s are the only way to break it.

## 6. Breaking change

`FieldDbType` gained a member. The value rides the wire as its underlying integer, so a table
containing a `Time` column cannot be deserialized by an older client — it throws rather than
silently misreading. **Client and server must run the same (or a compatible) version.**

No action is needed beyond upgrading both sides together.

## 7. What `Time` is not

**It is not a duration.** `Time` answers "what time", not "how long". The framework has no duration
type yet; use a `Decimal` (hours) for now.

This matters if you are tempted to compute a shift length by subtracting two `Time` fields via
`TimeOnly`. `TimeOnly` subtraction **wraps around midnight and is always positive**:

| Expression | Result |
|------------|--------|
| `22:00` → `06:00` | 8 hours — correct for a night shift |
| `08:00` → `08:00` | **0 hours**, not 24 |

The wrap is right for a night shift but silently wrong for a 24-hour shift, because in a modulo-24
world "a whole day" and "zero" are the same point. **Store a shift's length as its own field rather
than deriving it.**

**It has no seconds.** If you need them, you are describing an event, not a declaration — use
`DateTime`.

## Related

- [ADR-033](adr/adr-033-time-of-day-semantics.md) — why a fixed-width string rather than a native
  database time type, with the measurements behind the decision.
- [Calendar-Day vs Instant Column Semantics](date-semantics.md) — the `Date` / `DateTime`
  distinction.
- [Time Zones](datetime-timezone.md) — how instants move between UTC and a user's zone. Times of
  day, like calendar days, are never converted.
