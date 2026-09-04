# Wire fixtures

Golden samples of the **JSON body codec** used by the Bee.NET JSON-RPC API, published so a client
written in another language can verify it encodes and decodes bodies the way the server does.

## Why these exist

The framework's default body codec is MessagePack, assembled from hand-written per-type formatters.
Mirroring those in another language would create a second authority for the same contract with
nothing to catch the two drifting apart. The JSON codec exists so a browser client does not have to,
and these samples are what keeps the two ends honest: the .NET side generates and verifies them, and
a client uses them to check it can read what .NET writes and write what .NET reads.

## What a fixture contains

```json
{
  "case": "value-decimal",
  "description": "WireValueCode.Decimal. Quoted: a JSON number is a double …",
  "codec": "json",
  "type": "Bee.Definition.Collections.Parameter, Bee.Definition",
  "body": { "name": "v", "value": [12, "79228162514264337593543950335"] }
}
```

`body` is the payload body **before compression and encryption**. Those two layers are deliberately
not captured: gzip output is not guaranteed stable across .NET versions, and AES-CBC uses a random
IV per message, so neither can be pinned. They are standard algorithms that each language's own
library gets right; what needs pinning is the JSON shape, which only this framework defines.

## The envelope these bodies travel in

```json
{
  "jsonrpc": "2.0",
  "method": "Employee.GetList",
  "params": {
    "format": 2,
    "codec": "json",
    "type": "Bee.Api.Core.Messages.Form.GetListRequest, Bee.Api.Core",
    "value": "<base64 of AES-CBC-HMAC(gzip(body))>"
  },
  "id": "3f2a…"
}
```

- `format` is a **number**: `0` Plain, `1` Encoded (serialize + compress), `2` Encrypted
  (serialize + compress + encrypt).
- `codec` names the body codec. Omit it and the body is read as MessagePack, which is what every
  client predating codec negotiation sends. The response comes back in the same codec.
- `type` is required whenever the body is encoded; the server resolves it against an allow-list.
- The pipeline order is always serialize → compress → encrypt, and reverses on the way back.

## Rules a reader has to get right

These are the places where a plausible-looking implementation is silently wrong:

- **`object`-typed members carry a discriminator**: `[code, value]`, where the code is the
  framework's wire value code. A bare value is not accepted.
- **`decimal`, `int64` and `uint64` are JSON strings**, not numbers — in an `object`-typed member
  **and in a `DataTable` cell alike**. A JSON number is a double to every JavaScript reader, which
  holds neither a decimal's precision nor an integer past 2^53, and `JSON.parse` has already done
  the damage before your own code sees the value. The `datatable` fixture uses `decimal.MaxValue`
  and 2^53+1 for exactly this reason: read them as numbers and they will not match.
- **A null `object`-typed member is absent**, not written as `null`. Treat a missing property as
  null.
- **`DataTable` cells carry no discriminator** — their types come from the column metadata in the
  same document, so the table shape is identical to a Plain payload's. That is what makes the
  quoting rule above load-bearing here: a cell has nothing else to say "this is a decimal".
- **Enums travel as strings** (`"GreaterThan"`, `"Desc"`).
- **Date and time values are round-trip formatted** and the wire is UTC in both directions. Applying
  the user's time zone is the client's job, and nothing on the server checks it — get it wrong and
  values shift silently rather than failing.

## Regenerating

Fixtures are generated and verified by `WireFixtureTests` in `tests/Bee.Api.Core.UnitTests`. The
test fails when the current encoding stops matching a fixture, which is the point: a diff here is a
wire change, and clients in other languages parse against it.

```bash
BEE_REGENERATE_WIRE_FIXTURES=1 dotnet test tests/Bee.Api.Core.UnitTests/Bee.Api.Core.UnitTests.csproj
```

Read the resulting diff before committing it — that diff is the wire change description.
