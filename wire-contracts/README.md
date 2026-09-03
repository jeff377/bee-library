# Wire contracts

TypeScript declarations generated from the Bee.NET message types, published so a client in another
language describes the API with the same shapes the server does.

## Why generated

A hand-written type table in another repository is a second authority for the same API contract.
When the server renames a property, that copy does not find out — and the symptom is a field
silently missing, not an error. Generating it makes the table a derivative rather than a claim.

## What is here

| File | Contents |
|------|----------|
| `messages.d.ts` | The message types as TypeScript interfaces |
| `type-names.ts` | Assembly-qualified type names, which an encoded payload must carry in its envelope |

## What these describe

The **JSON shape on the wire**, not the CLR declarations:

- `Guid` and `DateTime` are `string`, because that is what they are in JSON.
- Enums are string literal unions — the server writes them with `JsonStringEnumConverter`.
- An `object`-typed member is `WireValue`, the discriminated `[code, value]` envelope.
- `DataSet` and `DataTable` follow their custom converters, which reflection cannot see; those
  few shapes are hand-written in the generator's preamble.

## Regenerating

Generated and verified by `WireContractGeneratorTests` in `tests/Bee.Api.Core.UnitTests`. The test
fails when the message types stop matching this file, which is the point: a diff here is an API
contract change, and a renamed or removed property breaks clients that do not ship with the
framework.

```bash
BEE_REGENERATE_WIRE_CONTRACTS=1 dotnet test tests/Bee.Api.Core.UnitTests/Bee.Api.Core.UnitTests.csproj
```

Read the resulting diff before committing it.
