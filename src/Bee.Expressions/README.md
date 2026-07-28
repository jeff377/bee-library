# Bee.Expressions

[繁體中文](README.zh-TW.md)

Portable expression evaluation engine, backed by DynamicExpresso. Shared by the business layer
(field computation and rule validation before save) and by UI clients (live preview while typing),
so a computed field yields the same result on both sides.

## Key Public APIs

| Type | Purpose |
|------|---------|
| `IExpressionEvaluator` | Evaluates an expression against a variable set. `Evaluate` returns a typed result; `GetReferencedVariables` reports which fields an expression reads |
| `DynamicExpressoEvaluator` | The default implementation. Parses and compiles once, caches by expression text plus parameter signature, then invokes per row |
| `ExpressionPolicy` | The shared type / null policy applied when feeding field values in. Both server and client route values through it so results match |
| `ExpressionEvaluationException` | Thrown when an expression fails to parse or evaluate, carrying the offending text |

## Time zone

`Evaluate` takes a `timeZoneId`. Helpers that read the clock (`Today()`, `Now()`) resolve it in
that zone, so a row created from another region still defaults to the user's own day. `UtcNow()`
states UTC outright. An empty zone id means UTC. See
[ADR-032](../../docs/adr/adr-032-datetime-timezone.md).

## Security

**This is not a sandbox.** Unregistered *type names* (`File`, `Assembly`, `Process`) fail at parse
time, but member access on a value is resolved by reflection, and `GetType()` is a public member of
`object` — any variable in scope is a starting point into the reflection API. Upstream DynamicExpresso
states the same limitation.

What keeps this safe is the *source* of the expressions, not the parser: expressions live in
definition files, and writing a definition is a deployment-time operation (`SystemBO.SaveDefine` is
`LocalOnly`). Any change that would let a remote or lower-privileged caller supply expression text
turns this into remote code execution on the server — that boundary is the control.

## AOT / trimming

`Expression.Compile` falls back to the interpreter when `IsDynamicCodeSupported` is false, so the
engine works on iOS, Android and WASM without disabling anything.

## Dependencies

`Bee.Base` · DynamicExpresso
