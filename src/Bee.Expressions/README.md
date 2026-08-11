# Bee.Expressions

[繁體中文](README.zh-TW.md)

The DynamicExpresso-backed implementation of the framework's expression engine. Shared by the
business layer (field computation and rule validation before save) and by UI clients (live preview
while typing), so a computed field yields the same result on both sides.

## Key Public APIs

| Type | Purpose |
|------|---------|
| `DynamicExpressoEvaluator` | The default `IExpressionEvaluator`. Parses and compiles once, caches by expression text plus parameter signature, then invokes per row |

## The abstraction lives in `Bee.Base`

`IExpressionEvaluator`, `ExpressionPolicy` and `ExpressionEvaluationException` are in
`Bee.Base.Expressions`, not here. That split keeps `Bee.Definition` and `Bee.Business` free of any
DynamicExpresso dependency — they consume the engine through the abstraction, and only a
composition root (`Bee.Hosting`, or a UI head building its own evaluator) references this package.
See [ADR-038](../../docs/adr/adr-038-definition-dependency-boundary.md).

**Reference this package when you need to pick an implementation. Reference `Bee.Base` when you
only need to accept one.**

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

`Bee.Base` (for the `IExpressionEvaluator` abstraction it implements) · DynamicExpresso
