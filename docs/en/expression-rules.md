# Expressions and Rules (Field Computation and Pre-Save / Pre-Delete Validation)

[繁體中文](expression-rules.zh-TW.md) · [← Docs Index](README.md)

Use **declarative expressions** inside the `FormSchema` definition file for field computation and validation, instead of hand-written business object code. Customers and consultants can customise the behaviour at design time — no code change, no rebuild, no redeployment.

For the background and the decision itself, see [ADR-028](adr/adr-028-expression-rule-engine.md).

## Three Capabilities

| Capability | Carrier | When it runs |
|------------|---------|--------------|
| Computed field | `FormField.ValueExpression` | Before save, recomputed and written back for added / modified rows |
| Field default | `FormField.DefaultValueExpression` | On row insert, only when the field is empty |
| Validation / precondition | `FormRule` under `FormSchema` | `BeforeSave` / `BeforeDelete` |

> **The backend is authoritative.** On save, the backend always recomputes computed fields from the definition and overwrites whatever the client submitted; validation also runs on the backend. Live client-side computation (planned) is a UX preview only — correctness never depends on the client.

## Expression Syntax

- **A variable is a field name.** Write the field name directly, e.g. `unit_price * qty`. Every field on the same row is available.
- **Operators**: a subset of C# syntax (`+ - * /`, `> >= < <= == !=`, `&& || !`, the ternary `? :`, and string `==`).
- **Available functions and types** (sandbox allowlist): `Math` (`Math.Round`, `Math.Abs`, …), `Today()`, `Now()`, `UtcNow()`, `IsNullOrEmpty(s)`, `IsNullOrWhiteSpace(s)`, and `Guid` (e.g. `customer_rowid != Guid.Empty`).

  **Semantics of the time functions** (see [ADR-032](adr/adr-032-datetime-timezone.md)):

  | Function | Returns | Basis |
  |----------|---------|-------|
  | `Today()` | `DateOnly` | Today **in the user's time zone**. This is what you want for cases like defaulting a leave date to today — a user in New York filing against a Taipei company still gets the Taipei date |
  | `Now()` | `DateTime` (`Kind` is `Unspecified`) | The current moment in that same zone |
  | `UtcNow()` | `DateTime` (`Kind` is `Unspecified`) | The current UTC moment, for when UTC intent must be explicit |

  `Today()` returns `DateOnly` rather than `DateTime` because a calendar day is always expressed as `DateOnly` in the framework. The `DataSet` cell is the sole exception, since a `DataColumn` can only carry a calendar day as `DateTime`. You may write `Today()` into either a `Date` or a `DateTime` field; the framework performs the conversion when writing the cell.

  > **Confirm the intent yourself when writing `Now()` / `UtcNow()` into a `DateTime` field.** An expression evaluated on the client still has its result treated as a user-zone value and converted once more on submission. The framework neither does — nor can — determine that a given cell was filled by an expression.
- **Forbidden**: reflection, IO, and loading arbitrary types. Any identifier outside the allowlist fails at parse time as a configuration error.
- **Null handling**: an empty field (`DBNull`) is substituted with its type default (`0` for numbers, an empty string for text, `Guid.Empty`, …), so `unit_price * qty` evaluates to `0` on empty input rather than failing.

## Computed Fields: `ValueExpression`

```xml
<FormField FieldName="amount" Caption="Amount" DbType="Currency"
           NumberKind="Amount" ReadOnly="true"
           ValueExpression="quantity * unit_price * (1 - discount)" />
```

- Recomputed before save for `Added` / `Modified` rows. `Unchanged` rows are left alone, so they are never falsely marked as modified.
- **Rounding** follows the field's `NumberKind` (`Amount` → 2 decimals, `Quantity` → 0, `UnitPrice` → full precision, …; adjustable per company / currency / unit — see [ADR-026](adr/adr-026-numeric-semantics-rounding.md)). Detail rows are therefore each rounded first and only then summed (round-then-sum), so the total always reconciles.
- Computed fields are usually paired with `ReadOnly="true"`.
- Several computed fields on the same row may depend on each other: evaluation follows **declaration order**, so a later expression sees the values just computed by earlier ones.

## Field Defaults: `DefaultValueExpression`

```xml
<FormField FieldName="order_date" Caption="Order Date" DbType="Date"
           DefaultValueExpression="Today()" />
```

- Evaluated on row insert, and **only applied when the field is empty** — an existing value is never overwritten.
- When a literal `DefaultValue` is also present, the expression wins.

## Validation and Preconditions: `FormRule`

```xml
<Rules>
  <FormRule RuleId="customer_required"
            Condition="customer_rowid != Guid.Empty"
            Message="Please select a customer." />
  <FormRule RuleId="quantity_positive" TargetTable="OrderDetail"
            Condition="quantity &gt; 0"
            Message="Quantity must be greater than zero." />
  <FormRule RuleId="approved_amount"
            When="status == &quot;Approved&quot;"
            Condition="total_amount &gt; 0"
            Message="An approved order must have a positive total." />
</Rules>
```

| Attribute | Description |
|-----------|-------------|
| `Condition` | The condition that **must hold** (returns bool). A `false` result is a violation: the action is aborted and `Message` is shown |
| `When` | Optional **applicability** condition. Empty means always apply; `false` skips the whole rule (treated as passing); only `true` proceeds to check `Condition` |
| `Message` | The message shown to the user when the rule fails |
| `Trigger` | `BeforeSave` (default) or `BeforeDelete` |
| `TargetTable` | Empty targets the master table; a detail table name checks that table **row by row** |
| `Order` | Evaluation order within the same trigger (lower runs first) |
| `Enabled` | Whether the rule is active (default true) |

> **The two-part test**: `When` decides whether this rule should be checked at all right now, and `Condition` is the validation that must hold. For example, "an approved order must have a positive total" becomes `When = status == "Approved"` with `Condition = total_amount > 0`. Orders in any other status are skipped automatically.
>
> Inside XML, write `>` as `&gt;` and a string quote as `&quot;`.

## When a Business Object Is Still Required (Current Boundary)

The Phase 1 expression engine is a **per-row** model. The following cases cannot yet be expressed declaratively and require overriding `DoBeforeSave` / `DoBeforeDelete` in a custom business object:

- **Cross-row aggregation**, such as "the header total is the sum of the detail amounts" or "at least one detail row is required" — both need computation across rows.
- **Database lookups**, such as "a status transition must be checked against the state already stored" or "fetch the next number from a sequence".

`OrderBO` in `apps/Bee.Northwind` is a worked example: the detail amounts and required-field checks have been made declarative, leaving only the aggregation and database-dependent logic in `DoBeforeSave`.

### Custom Business Object Override Convention

```csharp
protected override void DoBeforeSave(SaveContext context)
{
    base.DoBeforeSave(context);   // Run the rule engine first (defaults, computed fields, BeforeSave validation).
    // Then layer on the logic that declarations cannot express, such as aggregation or database queries.
}
```

`Save` and `Delete` have been refactored into template methods: authorization, record scope and auditing are orchestrated by the framework, and you override only the part you need — `DoBeforeSave` / `DoSave` / `DoAfterSave`, and the matching Delete hooks.
