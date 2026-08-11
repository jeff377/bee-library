# Changelog

[繁體中文](CHANGELOG.zh-TW.md)

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added

- `Bee.Api.Client`: `ApiSessionContext` carries the per-session client state — the transmission key established at login and the signed-in user's time zone. Connectors gain constructor overloads that take one; omitting it shares `ApiSessionContext.Ambient`, which is the previous behaviour and remains correct for a single-user host.

### Fixed

- `Bee.Api.Client` / `Bee.Web.Blazor.Server`: a host serving several users from one process no longer has them overwrite each other's transmission key. `ApiClientInfo.ApiEncryptionKey` and `UserTimeZoneId` were process-wide statics, so in `BeeBlazorProviderMode.Remote` the most recent login won and earlier users' encrypted requests failed to decrypt until they signed in again. `BeeApiConnectorFactory` is now registered scoped and hands each circuit its own context. `Local` mode was never affected. `ApiClientInfo.ApiKey` stays static deliberately — it identifies the application, not the user.
- `Bee.Api.Core` / `Bee.Base`: an unchanged data row no longer carries its values twice on the wire. Both the MessagePack and the JSON writer sent Current *and* Original for `Unchanged` rows, while both readers restore that state from Current alone — and `DataFormRepository.GetData` calls `AcceptChanges()` before returning, so every row read from the database is Unchanged. Payload and serialisation cost for a read halve.
- `Bee.Definition`: expression evaluation is roughly 4.7× faster. Each evaluation handed the engine every column in the row rather than the ones the expression references, so its cost tracked the column count instead of the expression. Measured over 30 columns / 5 computed fields / 1000 rows: 57.2 ms to 12.2 ms.
- `Bee.Db`: `WhereBuilder` no longer loses `SecondValue` and `IgnoreIfNull` when it rewrites a condition's field name for a query with a `selectContext`. A `BETWEEN` lost its upper bound, and an `IgnoreIfNull` condition became `= NULL` — which is never true, so the query silently returned nothing instead of ignoring the condition.

## [4.20.0]

> This release closes a deserialization hole and finishes two decouplings. The security item: the wire's type whitelist screened only the text before the first comma of an assembly-qualified name, and for a generic type that comma sits *inside* the argument list — so a disallowed type smuggled in as a generic argument was never screened, and an unauthenticated caller could reach it. Alongside it, `object` values on the wire move from a per-value type name to a discriminated envelope, and the expression abstraction sinks into `Bee.Base` so the definition layer stops handing every consumer a dependency on DynamicExpresso. **Both wire changes require client and server to be deployed together.** Framework login also gains a default implementation, which changes behaviour for deployments that never overrode it.

📄 Full notes and design context: [docs/changelogs/4.20.0.md](docs/changelogs/4.20.0.md)

### Security

- `Bee.Business`: `LoginAttemptTracker`'s map of failed attempts is bounded. Every key in it is an attacker-chosen user id — `System.Login` is anonymous — and an entry below the lockout threshold previously never expired and was never swept, so a stream of distinct user ids grew it without limit. Entries now expire on their own, the failure count is windowed, and the number of tracked accounts is capped.
- `Bee.Business` / `Bee.Api.AspNetCore`: losing the API key gate is now reported. Disabling the last enabled key silently returns the gate to accepting any non-empty `X-Api-Key` — an ordinary step of key rotation — and the only existing signal was a one-time snapshot taken at startup. Disabling the last key now logs an error, and the startup check reports at error level outside Development.
- `Bee.Api.Core`: the type whitelist is applied to **every** type named by an assembly-qualified name — the outer type, each generic argument, and array element types — and an unparsable name is now refused instead of passed through. Previously a name such as ``Bee.Base.Collections.Dictionary`1[[Disallowed.Type, Other]], Bee.Base`` passed the check, because splitting on the first comma left a fragment that still carried an allowed namespace prefix. `System.Login` is anonymous and payload type resolution runs before the business object is invoked, so the path was reachable without authentication. Present since 4.0.2.

### Breaking Changes

- **Wire**: `object`-typed members (`Parameter.Value`, `FilterCondition.Value` / `SecondValue`, `SerializableDataColumn.DefaultValue`, and the cell values inside `SerializableDataRow`) move from a per-value assembly-qualified type name to an integer-discriminated envelope. These members ride on every request and response, so a 4.19 client cannot talk to a 4.20 server or the reverse. See [ADR-037](docs/adr/adr-037-wire-explicit-registration.md)
- `Bee.Expressions` → `Bee.Base`: `IExpressionEvaluator`, `ExpressionPolicy` and `ExpressionEvaluationException` move to `Bee.Base.Expressions`. There is no type forward — the old names do not compile. See [ADR-038](docs/adr/adr-038-definition-dependency-boundary.md)
- `Bee.Definition` / `Bee.Business` / `Bee.UI.Avalonia`: the constructors of `FormExpressionCalculator`, `FormRuleProcessor` and `FormLiveComputation` take the evaluator from its new namespace. **`FormLiveComputation`'s parameter is optional, so a caller that omits it still compiles — but an already-compiled assembly throws `MissingMethodException` and must be rebuilt.**
- `Bee.Repository.Abstractions`: `IUserRepository` gains `VerifyPassword(userId, password)` — a new interface member, so any external implementation must add it.
- `Bee.Business`: `SystemBusinessObject.AuthenticateUser` no longer returns `false` unconditionally; the default implementation verifies against `st_user`. **A deployment that never overrode it had no working login and now accepts accounts present in `st_user`.** Deployments that do override it are unaffected.

### Added

- `Bee.Base`: `Bee.Base.Expressions` — the evaluator abstraction, its policy helpers and its exception type. `Bee.Expressions` keeps only `DynamicExpressoEvaluator`.
- `Bee.Definition`: `LanguageEnum.Entries` gains a setter (see the mobile fix below).
- `Bee.Business`: `LoginAttemptTracker.MaxTrackedAccounts` and `DefaultMaxTrackedAccounts`, for hosts that want a different bound.
- Build-time diagnostics **BEE9001** (dependency boundary for `Bee.Base` / `Bee.Definition`) and **BEE9002** (the three version properties must stay in step). [Analyzer rules](docs/analyzer-rules.md)

### Fixed

- `Bee.Definition`: `LanguageEnum.Entries` was a get-only collection mapped to repeated `[XmlElement]`. The reflection-only `XmlSerializer` path used by iOS assigns rather than adds, so it threw `ArgumentException: Property set method not found`, surfacing as the misleading "There is an error in XML document". The setter clears and refills the existing instance so the owner link survives.
- `Bee.Definition`: `st_user.password` widens from 40 to 200 characters. `PasswordHasher` produces a 79-character hash; four of the five providers would have truncated it, after which verification could never succeed. The defect had not surfaced because nothing in the framework wrote a hash to `st_user` until this release.
- **Versioning**: all three version properties move to `Version.props` at the repository root, imported by both `src/` and `tools/`. `AssemblyVersion` and `FileVersion` are back in step with `Version` — the 4.19.0 packages carry assemblies stamped `4.18.0.0` and cannot be told apart from 4.18.0 by assembly identity; that release is not being re-published. **`Bee.Cli` had its own copy of the three properties and had drifted twelve minor versions**, so every published `Bee.Cli` since 4.9.0 contains an assembly stamped `4.8.0.0`; it now takes the framework version like everything else. BEE9002 fails the build on a mismatch, and the single source removes the possibility of two projects disagreeing — which no per-project check can catch.

### Changed

- `Bee.Definition`: the package no longer carries `Bee.Expressions` — and therefore no longer carries `DynamicExpresso.Core` — through its dependency chain. Definition-only consumers such as `Bee.Cli` and `DefineEditor` stop inheriting an expression engine.
- `samples`: `Avalonia.Demo` is removed; the Avalonia end-to-end example is now `apps/Bee.Northwind`.

### Upgrade

```diff
- using Bee.Expressions;
+ using Bee.Base.Expressions;
```

Deploy server and clients together — see the wire note above. Rebuild any assembly that constructs `FormLiveComputation`, even if it does not pass an evaluator. If you implement `IUserRepository` yourself, add `VerifyPassword`. If you rely on `AuthenticateUser` rejecting every login, override it.

## [4.19.0]

> This release decouples the definition layer from the transport format. `Bee.Definition` no longer references MessagePack: every wire-binding concern now lives in `Bee.Api.Core`, behind hand-written formatters. The dividing line is whether a format costs the definition layer an external package — XML and JSON are BCL vocabulary and stay, MessagePack is a technology choice and moves out. Six downstream packages that never needed MessagePack stop inheriting it, and four pairs of deliberately duplicated collection types collapse back into one. **The wire format changes for `FilterNode` and `ParameterCollection`, so client and server must be upgraded together.** Strict SemVer would call this a major; the pre-stable policy for v4.x keeps it a minor, with every break listed below.

📄 Full notes and design context: [docs/changelogs/4.19.0.md](docs/changelogs/4.19.0.md)

### Breaking Changes

- **Wire**: `FilterNode` / `FilterCondition` / `FilterGroup` move from the `[Union]` array form to a `Kind`-discriminated map, and `ParameterCollection` from a single-key map to a plain array. `ParameterCollection` rides on every request and response, so a 4.18 client cannot talk to a 4.19 server or the reverse.
- `Bee.Definition`: `Collections.MessagePackCollectionBase<T>`, `MessagePackCollectionItem`, `MessagePackKeyCollectionBase<T>` and `MessagePackKeyCollectionItem` are removed — use the `Bee.Base.Collections` equivalents, which are the same types minus the attributes.
- `Bee.Definition`: `Serialization.SafeTypelessFormatter` is removed; the typeless allow-list moves to `Bee.Api.Core` as an internal formatter.
- `Bee.Base`: `IObjectSerializeProcess`, `SerializeFormat` and `SerializationLifecycle.NotifyAfterDeserialize` are removed — the interface had no production implementer and both of its historical uses were deliberately migrated away.
- `Bee.Analyzers`: **BEE4001**–**BEE4004** are retired; the attribute mechanisms they policed no longer exist. [Analyzer rules](docs/analyzer-rules.md)

### Changed

- `Bee.Definition` / `Bee.Api.Contracts`: the `MessagePack` package reference is gone. It remains only in `Bee.Api.Core`. See [ADR-036](docs/adr/adr-036-wire-serialization-externalized.md)
- `Bee.Api.Core`: wire binding is contractless plus nine hand-written formatters. Types with no framework-managed members to exclude need no formatter at all.
- `Bee.Analyzers`: **BEE4006** now keys off the framework collection and collection-item base types rather than `[MessagePackObject]`, keeping its coverage of item types.

### Upgrade

```diff
- using Bee.Definition.Collections;
+ using Bee.Base.Collections;

- public class MyItems : MessagePackCollectionBase<MyItem> { }
+ public class MyItems : CollectionBase<MyItem> { }

- public class MyItem : MessagePackCollectionItem { }
+ public class MyItem : CollectionItem { }
```

Deploy server and clients together — see the wire note above.

## [4.18.0]

> This release is the output of a full framework review rather than a feature cycle. The headline is the **`System.ExecFunc` dispatch surface**: `UpgradeTableSchema` (which drops and rebuilds a table in whichever database the caller names) and `TestConnection` (which opens an outbound connection to whichever host the caller supplies) were reachable by any authenticated caller. Both are now local-only, the dispatcher's default for an unannotated handler flips from *allow authenticated* to **refuse**, and a new build-time analyzer makes the omission impossible to repeat. Account lockout — implemented but never registered — is on by default, and a version-numbering defect in 4.17.0 is corrected.

📄 Full notes and design context: [docs/changelogs/4.18.0.md](docs/changelogs/4.18.0.md)

### Breaking Changes

- `Bee.Business`: an `IExecFuncHandler` method with no `[ExecFuncAccessControl]` is now **refused** at dispatch instead of treated as `Authenticated`.
- `Bee.Definition` / `Bee.Api.Client`: `SystemActions.ExecFuncLocal`, `SystemApiConnector.ExecFuncLocalAsync` and `FormApiConnector.ExecFuncLocalAsync` are removed — every call has thrown `MissingMethodException` since 2025-10-03.
- `Bee.Base` / `Bee.Definition`: `ISerializableClone` and `DatabaseSettings.CreateSerializableCopy()` are removed; the in-place-encryption pipeline they guarded against never existed.
- `Bee.Db`: `DbParameterSpecCollection`'s two convenience `Add` overloads move to `DbParameterSpecCollectionExtensions` — source-compatible, binary-breaking.

### Security

- `Bee.Business`: `UpgradeTableSchema` and `TestConnection` are `LocalOnly`. Previously any valid access token could trigger destructive DDL against a caller-named database, or use the server as an outbound port scanner with connection-string injection.
- `Bee.Hosting`: account lockout is enabled by default — `AddBeeFramework` registers `LoginAttemptTracker` (5 attempts / 15 minutes) via `TryAdd`.
- `Bee.Business`: `GetDefine(DefineType.DatabaseSettings)` reads the definition file directly, so passwords stay `enc:` ciphertext instead of being served decrypted from the cache instance.

### Added

- `Bee.Business`: `ExecFuncAccessControlAttribute.LocalOnly`, plus an `InvokeExecFunc` overload taking `isLocalCall` (the original overload is kept and treated as remote).
- `Bee.Analyzers`: **BEE3003** — a public method on an `IExecFuncHandler` implementation must declare `[ExecFuncAccessControl]`. [Analyzer rules](docs/analyzer-rules.md)
- `Bee.Db`: `DbParameterSpecCollectionExtensions`.
- `Bee.Api.Contracts` / `Bee.Db`: the serialization analyzers (BEE4002–4006) now run on these projects; they were silent there before.

### Changed

- `Bee.Base`: `JsonCodec` shares `static readonly JsonSerializerOptions` instead of building one per call, and the wire path emits compact JSON (`SerializeToFile` stays indented).
- Build: `AssemblyVersion` / `FileVersion` are back in step with `Version` — see the upgrade note below.

### Fixed

- `Bee.Db` / `Bee.Api.Client`: `DbProviderRegistry`, `DbDialectRegistry` and the `ClientDefineAccess` cache use concurrent collections; all three were read concurrently through non-atomic paths.
- `Bee.Business`: `SystemBusinessObject` no longer serializes the shared cache instance directly when serving `PluginSettings`.

### Upgrade

```diff
  public class MyExecFuncHandler : IExecFuncHandler
  {
+     [ExecFuncAccessControl(ApiAccessRequirement.Authenticated)]
      public void DoSomething(ExecFuncArgs args, ExecFuncResult result) { ... }
  }
```

- **ExecFunc handlers**: annotate every public method; BEE3003 fails the build otherwise. Use `LocalOnly = true` for in-process-only operations.
- **`DbParameterSpecCollection.Add`**: no source change needed; recompile assemblies built against 4.17.0 or earlier.
- **Account lockout**: to keep the previous behaviour, register a no-op `ILoginAttemptTracker` before `AddBeeFramework`.
- **Assembly identity**: the published 4.17.0 package contains assemblies stamped `4.16.0.0`. 4.18.0 jumps straight to `4.18.0.0`; `4.17.0.0` never existed and 4.17.0 is not being re-released.
- **Retroactive**: `IExcelHelper` was removed in 4.16.0 and never recorded. The entry has been added to the [4.16.0 notes](docs/changelogs/4.16.0.md).

## [4.17.0]

> The headline of this release is **business logic plugins** — attaching customer code to the existing flow of a packaged BO without replacing the whole BO class, adding only a step at a specific point. This is the fifth customization mechanism and fills the "lightweight extension" gap; it also brings the customization layer's **first write path** (`PluginSettings` is the only customization definition with a maintenance API). Two customization behaviour fixes ship alongside: `ProgramItem` override semantics move from whole-item replacement to **property-level inheritance**, and BO type resolution failure now **falls back and logs** — both surfaced only after `ProgramItem` gained its `Repository` binding in 4.16.0.

📄 Full notes and design context: [docs/changelogs/4.17.0.md](docs/changelogs/4.17.0.md)

### Added

- `Bee.Definition`: new `PluginSettings` definition type (the 13th `DefineType` member, appended to the enum) with its full read pipeline — paths, three storages, cache, reader, overlay.
- `Bee.Business`: new `FormBusinessPlugin` base and four mount points (`BeforeSave` / `AfterSave` / `BeforeDelete` / `AfterDelete`), executed after the final implementation of each `Do*` sub-method — **composable with inheritance**. [ADR-035](docs/adr/adr-035-business-logic-plugin.md)
- `Bee.Business`: new `FormPluginChain` / `FormPluginRunner` / `IFormPluginResolver` / `PluginSettingsResolver` — **the two layers add** (packaged first, tenant after), **per-operation instances**, resolution failure always throws.
- `Bee.Business`: new `SystemBO.GetCustomizePluginSettings` / `SaveCustomizePluginSettings` (`LocalOnly`), **validating every type before the write** — loadable, derives from `FormBusinessPlugin`, overrides at least one point.
- `Bee.Definition`: new `ICustomizeDefineWriter` and `CustomizeDefineWriter` — the customization layer's first write path, evicting that tenant's cache slot on write.
- `Bee.Business`: `BusinessObject` gains `protected IBeeContext Context`.
- New [tenant customization guide](docs/customization.md) (bilingual): a decision table for the five mechanisms, how-tos for language and layout, and what cannot be customized and why.

### Changed

- `Bee.Definition`: `ProgramItem` customization override moves from **whole-item replacement** to **property-level inheritance** — a customization writes only what it changes; unwritten properties inherit the packaged value. Fixes "swapping only the BO silently drops the packaged Repository". [ADR-016](docs/adr/adr-016-multitenant-customization-overlay.md)
- `Bee.Business`: BO type resolution failure for a regular progId still falls back to `FormBusinessObject`, but now **logs an error** (with progId, type name, and declaring layer).
- `Bee.Business`: `DeleteContext.Snapshot` loading now also considers whether the progId has a delete-point plugin, so its presence no longer depends on the change-audit toggle.
- `Bee.Definition` / `Bee.ObjectCaching`: `IDefineAccess` / `IDefineStorage` / `ICustomizeDefineReader` / `ICacheContainer` **gain `PluginSettings` members** — implementers of these interfaces must add them.
- `Bee.Db`: `DbDefineStorage.Write` gains a `customizeId` parameter; tenant and base rows differ only by `customize_id`, so no schema change is required.
- `FormBusinessObject`: the six `Do*` sub-methods gain `<remarks>` stating **whether they run inside the transaction**, documenting the TOCTOU window in `DoBefore*` and "throwing here leaves data committed" in `DoAfter*`. Documentation only, zero behaviour change.
- `api-bo-contract-design`: naming table gains an `XxxContext` row — `Args`/`Result` for cross-layer transport, `Context` for state shared within a flow.

## [4.16.0]

> Bee.NET remains in pre-stable evolution. This is the largest release since the changelog began: 227 commits across four themes. **ProgId becomes the framework's single addressing model** — `ProgramSettings` is now a pure type registry binding each progId to its business object *and* its repository, with the navigation menu split into its own definition [ADR-034](docs/adr/adr-034-progid-type-registry.md). **Tenant customization reaches language and layout**, with client and server sharing one overlay algorithm [ADR-016](docs/adr/adr-016-multitenant-customization-overlay.md). **Application identity gets a lifecycle** — API keys are stored, validated and managed behind a deployment-level permission axis. And **framework conventions move to build time** as 22 analyzer rules. Several changes are breaking; they ship as a minor under the pre-stable policy, as there are no external consumers yet. This entry also lists breaking changes made after `v4.15.0` whose commits were not marked `!`.

📄 Full notes & design context: [docs/changelogs/4.16.0.md](docs/changelogs/4.16.0.md)

### Added

- `Bee.Definition`: Roslyn analyzers ship with the package and turn 22 framework conventions into build diagnostics — definition file validity, wire contract shape, business object access control. See [Analyzer Rules](docs/analyzer-rules.md).
- `Bee.Definition`: `MenuSettings` — a new definition type owning the navigation menu (nested `MenuFolder` / `MenuEntry`, tree-unique `Id`, design-time `Visible`).
- `Bee.Definition`: `ProgramItem.Repository` binds a progId to its repository, alongside `BusinessObject`.
- `Bee.Repository.Abstractions`: `IRepositoryFactory` — one entry point for every repository, on two generic axes.
- Sessions survive cache eviction, restart and multi-node routing: sign-in writes a rebuild seed to `st_session` and roles / customization / record-scope are recomputed on every rebuild.
- Application identity: API keys stored in `st_api_key`, validated by `IApiKeyValidator`, managed behind `IDeploymentAuthorizationService`. See [API Key Management](docs/api-key-management.md).
- `Bee.Api.Client`: `FormDefinitionLoader` assembles runtime schema and layout from raw definitions.
- `Bee.Business`: `DerivedApiEncryptionKeyProvider` (now the default), `SessionCompanyBinder`, `BusinessObject.CreateFormRepository<T>()`.
- `Bee.Definition`: `st_user.culture`, `BackendConfiguration.DefaultLanguage` / `SessionCleanupOptions`; `Bee.Hosting`: `ExpiredSessionCleanupService`.
- `Bee.Expressions`: `UtcNow()` joins `Today()` and `Now()` in the expression sandbox. See [Expression Rules](docs/expression-rules.md).

### Changed — breaking (compile-time)

- `Bee.Definition`: `ProgramSettings` is a flat, server-only type registry; `ProgramCategory` is removed and the menu moves to `MenuSettings`. Definition files need splitting. [ADR-034](docs/adr/adr-034-progid-type-registry.md)
- `Bee.Business`: every business object resolves through the registry — `ProgId` moves to the `BusinessObject` base, `IFormBoTypeResolver` becomes `IBoTypeResolver`, the three `Create` methods collapse into `CreateBusinessObject(token, progId, isLocalCall)`, and `BackendComponents.BusinessObjectFactory` is removed.
- `Bee.Repository.Abstractions`: `ISystemRepositoryFactory` / `IFormRepositoryFactory` / `IAuditLogRepositoryFactory` and `IReportFormRepository` are removed; repositories take a uniform `(ctx, accessToken, progId)` constructor via `RepositoryBase`.
- `Bee.Api.Core` / `Bee.Api.Client` (**wire**): definition APIs serve raw definitions in an XML envelope; `SystemApiConnector.GetFormSchemaAsync` / `GetFormLayoutAsync` are removed. [ADR-016](docs/adr/adr-016-multitenant-customization-overlay.md)
- `Bee.UI.Maui` and `Bee.Web.Blazor.Wasm` are removed; the UI surface consolidates onto Avalonia + Blazor.Server.
- `Bee.Definition`: `Bee.Definition.Documents.IExcelHelper` is removed (89 lines, no implementation and no caller anywhere in the repository). *Recorded retroactively on 2026-08-07.*
- `Bee.ObjectCaching`: `IEvictableCache` and `ICacheContainer.TryEvict(string)` are removed.
- `Bee.Repository.Abstractions`: `IDataFormRepository.GetNewData()` takes `timeZoneId`; `ISessionRepository.CreateSession(...)` splits into `Insert` / `Update` / `Delete` / `DeleteExpiredSessions`; `IUserRepository.GetTimeZone` becomes `GetLocale` and gains `GetName`.
- `Bee.Business` / `Bee.Expressions`: `IFormRuleProcessor` and `IExpressionEvaluator.Evaluate` take `timeZoneId`.
- `Bee.Definition`: `IDefineStorage` gains `GetChangeSource(...)`; `IApiEncryptionKeyProvider.GenerateKeyForLogin` takes the token and the interface gains `SupportsSessionRebuild`; `ICacheDataSourceProvider.GetSessionUser` becomes `GetSessionInfo`.
- `Bee.Base`: the temporal `Cxxx` family returns nullable from its one-argument form; `CDate` is renamed `CDateOnly`; `FieldDbType.Time` is appended.
- `Bee.Api.Core` (**wire**): `SerializableData*` moves to property-name keys.
- `Bee.Business`: `SystemBO.SaveDefine` and `SystemBO.CreateSession` are `LocalOnly`.
- Dead public surface is removed: eight types (including `IEnterpriseObjectService`) and three members.

### Changed — breaking (silent, no compiler error)

- **System timestamps are UTC** — anything reading or comparing them shifts by one time-zone offset with no compilation failure.
- **Dates are `DateOnly`** — `FormRowDefaults.Apply` and `FieldDbTypeExtensions.DefaultForDbType` gain default arguments; `Today()` returns `DateOnly`.
- **Defaults changed**: `SessionInfo.TimeZone` / `Culture` default to empty (filled at login from `st_user`, falling back to `BackendConfiguration`); `BackendDefaultTypes.ApiEncryptionKeyProvider` names the derived provider, invalidating live sessions once on upgrade.
- **`SysInfo`'s deserialization allow-list** corrects `Bee.Contracts` to `Bee.Api.Contracts`.
- **`SystemBO.CreateSession` issues a usable session** and rejects `OneTime` with `NotSupportedException`.
- **Session reads have no side effects** — expired rows are filtered, not deleted; `ExpiredSessionCleanupService` reclaims them.

### Fixed

- `Bee.Hosting`: `IAuditLogWriteRepository` failed to resolve when audit logging was enabled.
- `Bee.Api.Core`: infrastructure exceptions keep their message when `IsDebugMode` is enabled; a DST spring-forward gap no longer throws.
- `Bee.Db`: a freshly created SQL Server table declares `FieldDbType.DateTime` as `datetime2(7)`, matching the ALTER and rebuild paths.
- `Bee.Definition`: `FieldDbType.Date` columns resolve to `DateEdit`.
- `Bee.Base`: `StringUtilities.Replace` uses ordinal comparison; `DataTable` JSON round-trip preserves string and decimal fidelity.

### Security

- Application identity is gated by a deployment-level permission axis that never falls back to the company-scoped one; every deployment-level operation is audited.
- Identifier-shaped string comparisons are ordinal throughout; the three hand-rolled constant-time loops are replaced by `CryptographicOperations.FixedTimeEquals`.
- `XmlCodec.Deserialize` prohibits DTD processing; definition paths reject root escapes; master key files are owner-only on Unix; session lookup failures no longer echo the user id.

### Upgrade

See [docs/changelogs/4.16.0.md](docs/changelogs/4.16.0.md#upgrade) for the definition-file split, the repository-factory migration and the list of silent changes to audit by hand.

## [4.15.0]

> Bee.NET remains in pre-stable evolution. This release is a **wire & API consolidation** ahead of stabilization. MessagePack contract serialization moves from positional integer keys to **property-name keys**, so JSON and MessagePack now share one name-based wire contract and the constructor-order / cross-inheritance key-numbering footgun is gone [ADR-030](docs/adr/adr-030-messagepack-name-based-keys.md). Separately, the API **contract interfaces are reorganized into per-axis namespaces** (`Bee.Api.Contracts.System` / `.Form` / `.AuditLog`) to match the existing implementation layers. Both changes are technically breaking — the wire format and `using` statements respectively — but ship as a minor under the pre-stable policy, as there are no external consumers yet.

📄 Full notes & design context: [docs/changelogs/4.15.0.md](docs/changelogs/4.15.0.md)

### Changed

- `Bee.Api.Core` / `Bee.Definition` (**breaking — wire**): 72 contract types (57 `Bee.Api.Core.Messages` request/response types + 15 `Bee.Definition` / `Bee.Api.Contracts` DTOs and non-`[Union]` collection items) switch from integer `[Key(n)]` to `[MessagePackObject(keyAsPropertyName: true)]`; the MessagePack payload changes from a positional array to a property-name map, matching JSON. Deliberately excluded (kept on integer keys): `[Union]` polymorphic types (`FilterNode` / `FilterCondition` / `FilterGroup`), collection containers, and `SerializableData*` DataSet/DataTable plumbing. [ADR-030](docs/adr/adr-030-messagepack-name-based-keys.md)
- `Bee.Api.Contracts` (**breaking — source**): the System / Form / AuditLog contract interfaces (and their DTOs) move out of the root namespace into `Bee.Api.Contracts.System` / `.Form` / `.AuditLog`, aligning with the already axis-split `Bee.Business.*` and `Bee.Api.Core.Messages.*` layers; the root namespace retains only the cross-BO `ExecFunc` request/response. Source-level only — serialization implementation namespaces are unchanged, so there is no wire impact.

### Upgrade

External consumers referencing the moved contract interfaces update their `using` directives to the per-axis namespace:

```diff
- using Bee.Api.Contracts;
+ using Bee.Api.Contracts.System;   // ILoginRequest, IPingRequest, …
+ using Bee.Api.Contracts.Form;     // IGetListRequest, ISaveRequest, …
+ using Bee.Api.Contracts.AuditLog; // change-axis contracts, RecordFieldChange
```

The MessagePack wire-format change requires no code change, but client and server must run the same (or a compatible) version — old positional-key payloads are not readable by the new name-based formatters.

## [4.14.0]

> Bee.NET remains in pre-stable evolution. This release adds two subsystems: a **declarative expression & rule engine** (new `Bee.Expressions` package — computed fields, before-save/delete validation rules, and Avalonia client-side live preview, all schema-driven with zero per-form code) [ADR-028](docs/adr/adr-028-expression-rule-engine.md), and an **audit-trail / log-query subsystem** (six-axis `st_log_*`: login / change / access / anomaly, with `DataSet` DiffGram change capture and a background writer) [ADR-027](docs/adr/adr-027-audit-trail.md). It also **canonicalizes in-memory `DataSet` column names to lowercase** [ADR-029](docs/adr/adr-029-lowercase-field-names.md) — a wire-visible change (JSON / MessagePack keys, e.g. `SYS_ROWID` → `sys_rowid`): external JS/TS clients must switch to lowercase keys, and the `UppercaseColumnNames` extension is renamed. .NET consumers are unaffected (column lookups are case-insensitive). Per pre-stable policy this ships as a minor although the wire/API change is technically breaking.

📄 Full notes & design context: [docs/changelogs/4.14.0.md](docs/changelogs/4.14.0.md)

### Added

- `Bee.Expressions` (new package): portable expression evaluator (`IExpressionEvaluator` / `DynamicExpressoEvaluator`, DynamicExpresso-backed, sandboxed) with compile caching, `ExpressionPolicy` type/null mapping, and dependency analysis — shared by the server and UI so a field computed on the client matches the server. [ADR-028](docs/adr/adr-028-expression-rule-engine.md)
- `Bee.Definition`: `FormField.ValueExpression` (computed field) and `DefaultValueExpression`, plus a `FormRule` / `FormRuleCollection` on `FormSchema` (`When` / `Condition` / `Message` / `Trigger` = `BeforeSave` | `BeforeDelete`); shared `FormExpressionCalculator`. [ADR-028](docs/adr/adr-028-expression-rule-engine.md)
- `Bee.Business`: `FormBusinessObject.Save` / `Delete` refactored into template methods (`DoBeforeSave` / `DoSave` / `DoAfterSave` + delete equivalents) with `IFormRuleProcessor` applying schema-driven defaults, computed fields (rounded via `NumberFormatResolver`), and validation rules — general CRUD forms need zero BO code. [ADR-028](docs/adr/adr-028-expression-rule-engine.md)
- `Bee.UI.Avalonia`: client-side live recomputation of computed fields as the user edits (`FormLiveComputation`), with a Tier 2 currency/unit rounding context and graceful degrade; `DefaultValueExpression` applied to new rows. [ADR-028](docs/adr/adr-028-expression-rule-engine.md)
- `Bee.Business` / `Bee.Repository`: audit-trail subsystem — six-axis `st_log_*` (`login` / `change` / `access` / `anomaly_api` / `anomaly_db`), `IAuditLogWriter` background writer, and `DataSet` DiffGram before/after capture on save/delete. [ADR-027](docs/adr/adr-027-audit-trail.md)
- `Bee.Business` / `Bee.Api.*`: audit log query read side — `GetChangeLog` / `GetChangeDetail` (change axis, list + detail two-stage), login / access / anomaly lists, and anomaly summary (`Summary` + Top-N). [ADR-027](docs/adr/adr-027-audit-trail.md)
- `Bee.UI.Avalonia` / `Bee.UI.Core`: front-end permission **capability** — element-level degradation (hidden without Read, read-only without Update) from the `EnterCompany` capability snapshot; `ClientInfo.Company` and `ClientDefineAccess.GetCurrencySettingsAsync` / `GetUnitSettingsAsync`.
- `Bee.Definition`: record-scope permission supports multiple Owner / Dept fields (OR union).

### Changed

- `Bee.Base` / data (**breaking — wire & public API**): in-memory `DataSet` column names are canonicalized to **lowercase** (`DataTableExtensions.AddColumn`, and `LowercaseColumnNames` at the `DbAccess` read boundary, unifying provider casing). JSON / MessagePack payload column keys change from uppercase to lowercase (e.g. `SYS_ROWID` → `sys_rowid`); the `UppercaseColumnNames` extension is renamed to `LowercaseColumnNames`. [ADR-029](docs/adr/adr-029-lowercase-field-names.md)
- `Bee.Db`: SQL Server `DateTime` columns migrated from `datetime` to `datetime2(7)` (sub-millisecond precision + pre-1753 range); the `datetime2` parameter rewrite is scoped to SQL Server only.
- `Bee.Base`: string-key case-insensitive comparisons converged to `OrdinalIgnoreCase` (culture-independent; avoids the Turkish-I hazard).

### Fixed

- `Bee.Expressions`: the variable map is keyed by the declared `FormField.FieldName`, so expressions resolve against uppercase-stored `DataColumn` names instead of failing with an unknown identifier on save; `ExpressionPolicy.CoerceValue` handles string-typed `Guid` / `byte[]` columns and maps an empty-string GUID to `Guid.Empty` (SQLite stores GUIDs as TEXT).

### Upgrade

External JS/TS clients reading `DataSet` JSON by literal column key must switch to lowercase:

```diff
- const rowId = row.current.SYS_ROWID;
+ const rowId = row.current.sys_rowid;
```

.NET callers of the renamed column-name extension:

```diff
- dataTable.UppercaseColumnNames();
+ dataTable.LowercaseColumnNames();
```

## [4.13.0]

> Bee.NET remains in pre-stable evolution. This release adds an ERP-grade numeric layer: a semantic `NumberKind` on fields drives the display format, the rounding policy, and where the decimal places come from — with **round-then-sum** totals, per-field **multi-currency** (SAP CUKY-style, JPY=0 / USD=2 / BHD=3) and **unit-of-measure** (SAP UNIT-style, KG=3 / PCS=0) decimals resolved at runtime, and an Avalonia `NumericEdit` editor. All additions are backward compatible (new members default to empty; `CompanyInfo` gains tail-appended MessagePack keys). No breaking changes. [ADR-026](docs/adr/adr-026-numeric-semantics-rounding.md)

📄 Full notes & design context: [docs/changelogs/4.13.0.md](docs/changelogs/4.13.0.md)

### Added

- `Bee.Definition`: `NumberKind` semantic (`Quantity` / `Weight` / `Amount` / `Percent` / `UnitPrice` / `Cost` / `ExchangeRate`) on `FormField` and `LayoutFieldBase`, driving display format, rounding policy, and decimals source. [ADR-026](docs/adr/adr-026-numeric-semantics-rounding.md)
- `Bee.Definition`: `NumberFormatResolver` (`ResolveDecimals` / `ResolveFormat` / `RoundByKind` / `RoundCash`) and `NumberFormatApplier.Bake` — round-then-sum totals, two-layer rounding (natural currency / unit decimals + optional cash rounding), display format baked onto a per-call schema clone (cache never mutated).
- `Bee.Definition`: `CurrencySettings` currency master (`DefineType.CurrencySettings`, curated ISO 4217, SAP TCURX-style) with per-field binding via `FormField.CurrencyField` / `FormSchema.CurrencyField`; amount decimals follow the currency.
- `Bee.Definition`: `UnitSettings` unit-of-measure master (`DefineType.UnitSettings`, SAP T006-style) with per-field binding via `FormField.UnitField`; quantity / weight decimals follow the unit.
- `Bee.Definition`: `CompanyInfo` gains `NumberFormats`, `DefaultCurrency`, `CashRounding`, `AllowedCurrencies` (`[Key(4)]`–`[Key(7)]`), backed by four new `st_company` columns; empty values fall back to framework defaults.
- `Bee.UI.Avalonia`: `NumericEdit` editor (`ControlType.NumericEdit`) — shows full precision on focus, formats per `NumberFormat` on blur, right-aligned, display rounding never written back.
- `Bee.UI.Avalonia`: `GridControl` per-cell currency / unit-aware formatting (resolves each row's `CurrencyField` / `UnitField`) and `AmountColumnSummary` mixed-currency / mixed-unit total helper.

## [4.12.1]

> Bee.NET remains in pre-stable evolution. This patch ships an embedded trimmer descriptor in `Bee.Definition` so the definition type graph survives full trim / AOT, completing the Avalonia **iOS** / **Android** Release-packaging path begun in 4.12.0 (which made the same types deserialize under the reflection-only XmlSerializer). No breaking changes.

📄 Full notes & design context: [docs/changelogs/4.12.1.md](docs/changelogs/4.12.1.md)

### Fixed

- `Bee.Definition`: ship an embedded `ILLink.Descriptors.xml` preserving the definition type graph (`Bee.Definition.*` + `Bee.Base.Collections.*`) under full trim / AOT, so the on-device `XmlCodec.Deserialize<FormSchema>` path is not stripped on trimmed iOS / Android Release builds. Auto-applied to every downstream trimmed / AOT consumer; no consumer action required.

## [4.12.0]

> Bee.NET remains in pre-stable evolution. This release makes the `Bee.UI.Avalonia` control family responsive for phone / narrow viewports and makes the `Bee.Definition` types deserialize under the AOT reflection-only XmlSerializer — together these enable the Avalonia **iOS** and **Android** heads. No breaking changes.

📄 Full notes & design context: [docs/changelogs/4.12.0.md](docs/changelogs/4.12.0.md)

### Added

- `Bee.UI.Avalonia`: `FormView` responsive layout — master fields reflow multi-column → single column and detail grids switch `InCell` → `EditForm` below `CompactWidthThreshold` (default 600 DIP).
- `Bee.UI.Avalonia`: `ListView` card layout on narrow viewports — one card per row instead of the wide column grid.
- `Bee.UI.Avalonia`: `RowEditPanel` (EditForm) reflows 1 ↔ 2 columns by host width; `RowEditDialog` desktop window is resizable.

### Fixed

- `Bee.Definition`: definition collection types deserialize under the AOT reflection-only XmlSerializer (single public `Add(T)`, parameterless constructors) — enables the iOS / Android heads. Call syntax and XML format unchanged. [ADR-025](docs/adr/adr-025-define-types-aot-xmlserializer-compat.md)
- `Bee.UI.Avalonia`: `RowEditDialog` renders through an `OverlayLayer` on single-view hosts (iOS / Android / browser) instead of a native `Window` (which crashed).
- `Bee.UI.Avalonia`: `FormView` body scrolls vertically so controls below the fold stay reachable in a narrow single-column layout.
- `Bee.UI.Avalonia`: `GridControl` lookup cells show the open-dialog magnifier icon in edit state.

## [4.11.0]

> Bee.NET remains in pre-stable evolution. The theme of this release is "front-end ↔ back-end access goes fully async": the client connection lifecycle and the typed definition cache drop their synchronous-over-asynchronous bridges (`SyncExecutor` is gone), which makes a single-window Avalonia Browser (WASM) head viable. It contains **breaking changes** confined to the client construction / connection surface of `Bee.UI.Core`, `Bee.Api.Client`, and the Avalonia / MAUI heads, plus a **security upgrade** of SQLitePCLRaw.

📄 Full notes & design context: [docs/changelogs/4.11.0.md](docs/changelogs/4.11.0.md)

### Breaking Changes

- Remove synchronous client APIs in favor of async — `ClientInfo.Initialize(string)` / `SetEndpoint`, `ApiConnectValidator.Validate`, `IUIViewService.ShowApiConnect` (use the `...Async` counterparts); `SyncExecutor` removed.
- Rename `RemoteDefineAccess` → `ClientDefineAccess` (now at the `Bee.Api.Client` root) and `LocalDefineAccess` → `CacheDefineAccess`.

### Security

- Upgrade SQLitePCLRaw to 3.x (GHSA-2m69-gcr7-jv3q), replacing the NU1903 suppression.

### Added

- `Bee.UI.Avalonia`: dialog overlay path (`OverlayLayer`) for single-window hosts, enabling lookup / row-edit dialogs in the Avalonia Browser (WASM) head.
- `Bee.UI.Avalonia`: `FormDataObject` `RowAdded` / `RowDeleted` / `IsDirtyChanged` events.

### Changed

- `Bee.UI.Avalonia`: field editors commit on leave / Enter instead of per keystroke.
- `Bee.UI.Avalonia`: field captions mark read-only (parenthesized, underline-only) and required (blue) uniformly.
- `Bee.Definition`: `FormLayoutGenerator` no longer repeats the form name on the generated main section.

### Fixed

- `Bee.UI.Avalonia`: `GridControl.Bind` self-initializes edit state on explicit bind.

### Upgrade Guide

```diff
- ClientInfo.Initialize(endpoint);
- ClientInfo.SetEndpoint(endpoint);
+ await ClientInfo.InitializeAsync(endpoint);
+ await ClientInfo.SetEndpointAsync(endpoint);
```
```diff
- RemoteDefineAccess access = ...;   // LocalDefineAccess cache = ...;
+ ClientDefineAccess access = ...;   // CacheDefineAccess  cache = ...;
```

## [4.10.0]

> Bee.NET remains in pre-stable evolution. The theme of this release is "the lookup relation mechanism lands in full": relation fields automatically become dialog-based lookup editors with composite "code - name" display, with two pick entry points (master `ButtonEdit` and detail in-cell), backed by a server-side `GetLookup`. It also splits the Avalonia single-record and list concerns into `FormView` / `ListView` (the ERP list/single separation), and consolidates DataForm persistence onto a DataTable-level `DataAdapter` path (a home-grown `SqliteDataAdapter` lets SQLite use the adapter path too). The release contains **several breaking changes**, confined to the construction surface of `Bee.UI.Avalonia` and `Bee.Db`.

📄 Full notes & design context: [docs/changelogs/4.10.0.md](docs/changelogs/4.10.0.md)

### Breaking Changes

- `Bee.UI.Avalonia`: remove `DynamicForm` / `SingleFormBase`; list duties move to new `ListView`, single-record duties consolidate in `FormView`, both move to new namespace `Bee.UI.Avalonia.Views`.
- `Bee.UI.Avalonia`: `GridControl` (with `GridControlBinder` / `GridEditMode`) moved from `Bee.UI.Avalonia.Controls.Editors` to `Bee.UI.Avalonia.Controls`.
- `Bee.Db`: remove row-by-row `InsertCommandBuilder` / `UpdateCommandBuilder` (`DeleteCommandBuilder` / `SelectCommandBuilder` remain).

### Added

- `Bee.Definition` / `Bee.Api` / `Bee.UI.Avalonia`: definition-driven dialog lookup relation mechanism — `DisplayField` / `LookupFields`, auto-resolved `ButtonEdit`, server-side `FormBusinessObject.GetLookup`, client `LookupPanel` / `LookupDialog` and `GridControl` in-cell lookup ([ADR-023](docs/adr/adr-023-lookup-relation-mechanism.md)).
- `Bee.Definition`: `FormField.ReadOnly` propagated by `FormLayoutGenerator` to `LayoutField` / `LayoutColumn`.
- `Bee.Db`: home-grown `SqliteDataAdapter` via `SqliteProviderFactory` so SQLite uses the adapter path.

### Changed

- `Bee.Db`: `DataFormRepository.Save` now uses DataTable-level IUD (`DataAdapter.Update`); no-change DataSet is a no-op returning 0 ([ADR-024](docs/adr/adr-024-dataform-save-dataadapter.md)).
- `Bee.UI.Avalonia`: `FormView` opens a list row read-only on double-click.

### Fixed

- `Bee.Db`: SQLite GUID columns get `COLLATE NOCASE` (CREATE and ALTER ADD).
- `Bee.Db`: new rows get non-null defaults from `FormSchema` via `FormRowDefaults`; master link writes the raw `sys_rowid` into detail `sys_master_rowid`.
- `GetNewData` skeleton includes `RelationField` columns.
- `SelectContextBuilder`: fix multi-relation JOIN resolution.
- `Bee.UI.Avalonia`: `ListView` list scrollbar scrolls correctly when rows exceed the visible area.

## [4.9.0]

> Bee.NET remains in pre-stable evolution. The theme of this release is "Avalonia editable forms land in full": a field editor suite mapped 1:1 to `ControlType`, a new `GridControl` with in-cell and dialog-based row editing, a form-mode lifecycle (`SingleFormBase` broadcasting `FormMode` to the whole control tree), and a definition-layer `FormEditModes` setting for per-mode editability. The release contains **one breaking change**, confined to the Avalonia family: `DynamicGrid` was removed from `Bee.UI.Avalonia` (its Blazor / MAUI counterparts are unaffected). It also ships a **security upgrade** of the MessagePack dependency.

📄 Full notes & design context: [docs/changelogs/4.9.0.md](docs/changelogs/4.9.0.md)

### Breaking Changes

- `Bee.UI.Avalonia`: removes `DynamicGrid`; `FormView` list rendering moves to `GridControl` (a `ContentControl` composite — use `GridControl.InnerGrid` for `DataGrid` members). Blazor / MAUI `DynamicGrid` unaffected.

### Security

- MessagePack: `3.1.4` → `3.1.7` (GHSA-hv8m-jj95-wg3x) — fixes LZ4 `AccessViolationException` on malicious input (NU1903 high).

### Added

- `Bee.UI.Avalonia`: field editor suite — seven editors mapped 1:1 to `ControlType` (`TextEdit` / `MemoEdit` / `ButtonEdit` / `DateEdit` / `YearMonthEdit` / `DropDownEdit` / `CheckEdit`), with `FieldEditorBinder`, `FormScope` attached properties, and `FieldEditorFactory`; `DynamicForm` renders through it.
- `Bee.UI.Avalonia`: adds `GridControl` — `LayoutGrid`-driven composite grid (`InnerGrid`) with two binding modes, `FormScope` ambient binding, in-cell editing per `LayoutColumn.ControlType`, `AllowActions` add/delete, and `AllowEdit` ([ADR-021](docs/adr/adr-021-avalonia-datagrid-editing-strategy.md)).
- `Bee.UI.Avalonia`: adds `GridEditMode` (`InCell` / `EditForm`) + `RowEditPanel` / `RowEditDialog`, backed by `FormDataObject` row-edit protocol (`BeginRowEdit` / `CommitRowEdit` / `CancelRowEdit`).
- `Bee.UI.Avalonia`: adds `SingleFormBase` owning and broadcasting `FormMode`; `FormView` inherits it with a View/Edit/Add mode lifecycle.
- `Bee.Definition`: adds `FormEditModes` `[Flags]` enum + `LayoutField.AllowEditModes` / `LayoutGrid.AllowEditModes` (default `All`); AND-composed with `ReadOnly` / `AllowActions`, defaults not serialised.
- `Bee.UI.Avalonia`: `FormDataObject` adds `FieldValueChanged` / `DataSetReplaced` events with ADO.NET bridge, plus row-overload `GetField` / `SetField`.
- `samples/Avalonia.Editors.Gallery`: native vs inherited editor comparison, in-cell editing, and `EditForm`-mode section.
- DefineEditor: Semi.Avalonia theme, Welcome tab, tab dirty markers + context menu + Save All, unsaved-changes prompt, macOS menu polish.

### Changed

- `Bee.UI.Avalonia`: `FormView` now loads records in read-only `View` mode; Edit button required to edit.

### Fixed

- `Bee.Api`: `DataTable` deserialization broken by MessagePack 3.1.5+ blocklist; `SafeMessagePackSerializerOptions` now lets the framework trust list take precedence.
- `Bee.UI.Avalonia`: `FormDataObject` async CRUD continuations now resume on the UI thread (removed `ConfigureAwait(false)`).
- `Bee.UI.Avalonia`: `DynamicForm` `DateEdit` no longer throws on non-UTC time zones.
- `Bee.UI.Avalonia`: `ComboBox` selection box now shows the selected value; `DropDownEdit` / in-cell `ComboBox` use `DisplayMemberBinding`.
- `Bee.UI.Avalonia`: `GridControl` re-realizes rows after `AddRow` / `DeleteSelectedRow`.
- `Bee.UI.Avalonia`: `ButtonEdit` read-only state now disables the embedded lookup button; icon restyled to chromeless `PathIcon`.
- Demo backend now materializes `st_cache_notify`, stopping the `CacheNotifyPoller` warning.

## [4.8.0]

> Bee.NET remains in pre-stable evolution. The theme of this release is "framework default definitions become first-class": the `st_*` system table schemas, framework-shipped `Department` / `Employee` forms, and bootstrap settings templates now ship as embedded resources inside `Bee.Definition.dll`, accessible via the new `Bee.Definition.Defaults` public API. A new `Bee.Cli` dotnet tool (`dotnet bee defines materialize ...`) and a DefineEditor auto-materialise hook turn this into a one-command first-time setup. The release contains **one breaking change**: the framework organisation tables `ft_department` / `ft_employee` were renamed to `st_department` / `st_employee` to align with the rest of the `st_*` namespace.

📄 Full notes & design context: [docs/changelogs/4.8.0.md](docs/changelogs/4.8.0.md)

### Breaking Changes

- Framework organisation tables `ft_department` / `ft_employee` renamed to `st_department` / `st_employee`; deployments must `RENAME TABLE` — see [Table Schema Upgrade Guide §Renaming framework tables](docs/database-schema-upgrade.md). FormSchema progIds, C# type names, and field names unchanged.

### Added

- `docs/framework-reserved-names.md` (bilingual): registry of framework-reserved names (`st_*` system tables, reserved `progId`s).
- `Bee.Definition`: framework default define files (11 `st_*` `TableSchema` XMLs, `Department` / `Employee` `FormSchema` / `FormLayout` / `Language`, minimal `DbCategorySettings.xml`, `SystemSettings.xml` template, empty `DatabaseSettings.xml`) now ship as embedded resources under `Bee.Definition.Defaults/{relative-path}`.
- `Bee.Definition.Defaults` API: `Defaults.MaterializeTo(path, options)` (skip-existing), `Defaults.ListEmbedded()`, `Defaults.OpenEmbedded(relativePath)`; runtime `IDefineStorage` untouched.
- `TestProcessBootstrap.SharedDefinePath`: process-wide merged define directory; `BeeTestFixture` default `DefinePath` now points here.
- `Bee.Cli` dotnet tool (`dotnet bee`): `defines materialize --path ./Define [--overwrite] [--filter <prefix>]`, `defines list`, `--version`; lock-stepped to framework version, published via `nuget-publish.yml`. Reserved subcommand groups (`schema`, `tenant`, `samples`) not yet implemented.
- DefineEditor auto-materialises framework defaults (`Defaults.MaterializeTo`, skip-existing) on folder open; status bar reports written count.

## [4.7.0]

> Bee.NET remains in pre-stable evolution. The theme of this release is "ERP permissions, i18n, and multi-tenant customisation land in full": three-phase permission model (line-A / line-B / record-scope), localisation infrastructure, multi-tenant customisation overlay, cross-node DB cache invalidation, a DB-backed define storage backend, and a third desktop platform — the new `Bee.UI.Avalonia` package. This release contains **no breaking changes** (existing public API signatures are unchanged). However, the first start-up creates several new system tables (`st_role` / `st_role_grant` / `st_user_role` / `st_cache_notify` / `st_define` / `st_user_company`); deployments that manage DDL out-of-band (instead of letting the framework auto-upgrade the schema) need to add them manually.

📄 Full notes & design context: [docs/changelogs/4.7.0.md](docs/changelogs/4.7.0.md)

### Added

- `Bee.UI.Avalonia`: new Avalonia 12 desktop control package — `DynamicForm` / `DynamicGrid` / `FormView`, `FormDataObject`, `FileEndpointStorage`; `samples/Avalonia.Demo` included. See [ADR-020](docs/adr/adr-020-avalonia-datagrid-binding-strategy.md).
- ERP permission model (line-A + line-B + record-scope): `PermissionModels` registry, `FormSchema.PermissionModelId`, `FormField.ScopeRole`, `AuthorizationService.Can`, `st_role` / `st_role_grant` / `st_user_role` data model, `EnterCompany` populating `SessionInfo.Roles`, FormBO permission gate, and `ScopeResolver` row-level filtering with authoritative `sys_rowid` re-query in `Update` / `Delete`. See [ADR-019](docs/adr/adr-019-permission-authorization-model.md).
- i18n: `LanguageResource` (XML / JSON / MessagePack), `ILanguageService` + `GetLangText`, automatic `FormSchema` localisation, `LangEnumName` enum dropdowns, and `SystemBO.GetLanguage` JSON-RPC entry point.
- Multi-tenant customisation overlay: `CustomizeId` flows through the request, define read path stacks customise overlay over base define, integrated into `IDefineAccess`, `RemoteDefineAccess` clears cache on tenant switch. See [ADR-016](docs/adr/adr-016-multitenant-customization-overlay.md).
- DB cache invalidation (cross-node): `st_cache_notify` table + `ICacheNotifyService.Touch`, `CacheNotifyPoller` background poller with static route registry, incremental polling by `sys_update_time`. See [ADR-017](docs/adr/adr-017-db-cache-invalidation.md).
- `DbDefineStorage`: `st_define` table + `DbDefineStorage` + `ICustomizeDefineReader`; defines can live in DB (XML path still works), lazy DI resolution to break the `IDbAccessFactory` cycle. See [ADR-018](docs/adr/adr-018-db-define-storage.md).
- Organisation department tree: cross-format `DepartmentTree` (nested via `DepartmentNode.Children`), per-company cache, `GetDepartmentTree` JSON-RPC API.
- `ProgramItem.BusinessObject`: a progId can bind a BO type explicitly, replacing convention-based resolution.
- `tools/define-editor`: Avalonia desktop tool for visual editing of nine define types, with live i18n, validation, single-file publishing, and a macOS `.app` bundle. Non-shipping tool.

### Changed

- `DepartmentTree`: serialisation changed from flat list to nested via `DepartmentNode.Children`.
- `st_cache_notify`: removed the `sys_` prefix from non-system columns; system columns keep it.
- `CacheNotifyPoller`: reverted to `O(1)` incremental fetch by `sys_update_time`.

### Fixed

- MySQL: `ALTER ADD Guid NOT NULL DEFAULT (UUID())` is replication-unsafe under statement-binlog; dialect now splits into `ADD COLUMN` (constant default) + `ALTER COLUMN SET DEFAULT (UUID())`.
- Oracle: `ALTER MODIFY ... NOT NULL` raised ORA-01442 on already-NOT-NULL columns; hint now emitted only when nullability changes.
- Oracle: String / Text columns now always built nullable (`''` = `NULL`, ORA-01400 on fresh `CREATE TABLE`).
- MAUI `DynamicForm`: `SetField` now idempotent, `ConvertToColumnValue` got a non-null fallback, `ReloadList` preserves `sys_rowid`.
- `ObjectCaching`: replaced `PhysicalFileProvider` with lazy `FileModificationToken` to fix a CI race (dropped `Microsoft.Extensions.FileProviders.Physical` reference).
- `DemoBusinessObjectFactory`: added missing `ILanguageService` injection.
- `RolePermissionRepository`: SQL concatenation missing space (SonarCloud S2857).

## [4.6.0]

> Bee.NET remains in pre-stable evolution. The theme of this release is "open up JSON-RPC to JavaScript frontends": seven FormBO / SystemBO CRUD / Session methods now ship with `ProtectionLevel = Public`, two new JSON-native getters (`GetFormSchema` / `GetFormLayout`) are introduced, and Plain-path `DataSet` deserialization plus a Blazor WebAssembly RSA blocker are fixed. The `MasterKeySource` default flips to `Environment`; under strict SemVer this would be a major bump, but under the pre-stable policy it ships as a minor.

📄 Full notes & design context: [docs/changelogs/4.6.0.md](docs/changelogs/4.6.0.md)

### Added

- `Bee.Business`: `SystemBO.GetFormSchema` / `GetFormLayout` — JSON-native getters returning `FormSchema` / `FormLayout`; `.NET` adds `SystemApiConnector.GetFormSchemaAsync` / `GetFormLayoutAsync`; both `Public + Authenticated`. See [ADR-014](docs/adr/adr-014-jsonrpc-plain-public-default.md).
- `docs`: new bilingual [`docs/jsonrpc-frontend-integration.md`](docs/jsonrpc-frontend-integration.md) — wire format, headers, auth flow, method catalog, `JsonRpcErrorCode` mapping, TypeScript wrapper.

### Changed

- `Bee.Definition`: `MasterKeySource` default changed to `Environment` (reads `$BEE_MASTER_KEY` instead of `Master.key`) (**breaking**); explicit `<Type>File</Type>` hosts unaffected. See [ADR-015](docs/adr/adr-015-master-key-environment-default.md).
- `Bee.Business`: seven BO methods downgraded to `ProtectionLevel = Public` — `FormBO.GetNewData` / `GetData` / `Save` / `Delete`, `SystemBO.EnterCompany` / `LeaveCompany` / `Logout` (`Encrypted` → `Public`, still `Authenticated`); backward compatible. See [ADR-014](docs/adr/adr-014-jsonrpc-plain-public-default.md).
- `Bee.Definition`: `FormSchema.MasterTable` is now `[JsonIgnore]` (XML / MessagePack unaffected); JS / TS clients read `tables[0]` instead of `masterTable`.

### Fixed

- `Bee.Base`: `RsaCryptor` exports keys in PEM (SPKI / PKCS#1) instead of XML, plus `OperatingSystem.IsBrowser()` fallback — unblocks Blazor WebAssembly login.
- `Bee.Api.Core`: `ApiInputConverter` registers Plain-path `DataTableJsonConverter` / `DataSetJsonConverter` / `JsonStringEnumConverter`, fixing empty-rows `DataSet` and "DataSet has no pending changes" on `Save`.
- `Bee.UI.Maui`: `DynamicForm` exposes public `Refresh()` driving `Rebuild()`, so the form rebuilds after in-place `DataSet` mutation (New / Save / Delete).

### Upgrade Guide

```diff
- const masterTable = formSchema.masterTable;
+ const masterTable = formSchema.tables[0];
```

## [4.5.0]

> Bee.NET remains in pre-stable evolution. This release introduces three frontend package layers (`Bee.UI.Core` cross-platform shared layer, `Bee.UI.Maui` MAUI mobile/desktop controls, `Bee.Web.Blazor.Server` / `Bee.Web.Blazor.Wasm` Blazor RCLs) and flips the API connector interfaces to async-only. Under strict SemVer the signature changes would be a major bump; under the pre-stable policy it ships as a minor.

📄 Full notes & design context: [docs/changelogs/4.5.0.md](docs/changelogs/4.5.0.md)

### Added

- `Bee.UI.Core`: new cross-platform UI shared layer (shared view models, `FormDataObject`, `SystemApiConnector`, `ClientInfo`), merged from `bee-ui-core`. See [ADR-013](docs/adr/adr-013-frontend-api-connection-strategy.md).
- `Bee.UI.Maui`: new MAUI control layer with `DynamicForm` / `DynamicGrid` / `FormPage` and `MauiPreferenceEndpointStorage`; defaults to `net10.0`, platform TFMs via `-p:BeeUiMauiFullPlatforms=true`.
- `Bee.Web.Blazor.Server` / `Bee.Web.Blazor.Wasm`: new Blazor RCLs shipping `DynamicForm` / `DynamicGrid` / `FormPage`, `BeeAccessTokenProvider`, `BeeLoginPanel`, and `AddBeeBlazor`.
- `UserMessageException` + `JsonRpcErrorCode.UserMessage`: server throws rehydrated by `ApiConnector` into client-side `UserMessageException` for direct `.Message` display.
- `FormBusinessObject`: added `GetNewData` / `GetData` / `Save` / `Delete`, completing single-row CRUD on `IFormBusinessObject`.
- `samples/`: new demo family — `QuickStart.Server` + `QuickStart.Console`, `Blazor.Server.Demo` + `Blazor.Wasm.Demo`, `Maui.Demo`; share `Bee.Samples.Shared` and ship `.smoke.yaml`.

### Changed

- `IApiConnector` / `IFormApiConnector` / `ISystemApiConnector`: now async-only; sync methods removed, use `*Async` variants.
- `ExceptionExtensions`: moved from `Bee.Base` to `Bee.Base.Exceptions`.
- `ClientInfo`: now a static class; `ClientInfo.SystemApiConnector.Initialize()` is async. See [ADR-013](docs/adr/adr-013-frontend-api-connection-strategy.md).

### Upgrade Guide

```diff
- var data = connector.GetData(progId, formData);
+ var data = await connector.GetDataAsync(progId, formData);
```

```diff
  using Bee.Base;
+ using Bee.Base.Exceptions;

  ex.Unwrap();
```

## [4.4.0]

> Bee.NET remains in pre-stable evolution; the public API surface has no external consumers yet, so minor releases are allowed to carry API moves and limited breaking changes. This release includes interface signature changes (`IFormRepositoryFactory.CreateDataFormRepository`, `IDataFormRepository.GetList`) and a property removal (`CompanyInfo.LogDatabaseId`). Under strict SemVer this would be a major bump; under the pre-stable policy it ships as a minor.

📄 Full notes & design context: [docs/changelogs/4.4.0.md](docs/changelogs/4.4.0.md)

### Added

- `Bee.Business`: `FormBO.GetList` unified list query via `IDataFormRepository` with `PagingOptions`/`PagingInfo`; `FormApiConnector.GetList`/`GetListAsync` client entry.
- `Bee.Business`: `SystemBO` adds `EnterCompany`/`LeaveCompany`/`Logout`; `SessionInfo` gains nullable `CompanyId`; `Login` declared on `ISystemBusinessObject`; new `CompanyInfo` + `ICompanyInfoService`. See [ADR-012](docs/adr/adr-012-session-company-context.md).
- `Bee.Business`: `DbScope` enum (`Common`/`Company`/`Log`) + `IRepositoryDatabaseRouter`; `BusinessObject.ResolveDatabaseId(DbScope)` and `CreateDataFormRepository(progId)` protected helpers. See [ADR-010](docs/adr/adr-010-logical-database-category.md).
- `Bee.Db`: `SelectCommandBuilder` paging (`OFFSET/FETCH` or `LIMIT/OFFSET`) across 5 dialects + new `BuildCount`.
- `Bee.ObjectCaching`: `KeyObjectCache<T>` negative caching (default 5-min absolute expiration, virtual `GetNegativePolicy` to override/disable). See [ADR-009](docs/adr/adr-009-cache-implementation.md).
- `Bee.Business`: `IBusinessObjectFactory` typed wrappers `CreateFormBO(token, progId)` / `CreateSystemBO(token)`.
- `Bee.Repository`: `st_company`/`st_user_company` system tables + `ICompanyRepository`/`IUserCompanyRepository`; default common `DbCategorySettings` includes both.
- `JsonRpcErrorCode`: new `CompanyNotEntered` (-32002, HTTP 409) and `CompanyAccessDenied` (-32003, HTTP 403).

### Changed

- `IFormRepositoryFactory.CreateDataFormRepository`: adds `Guid accessToken` parameter, routed via `IRepositoryDatabaseRouter`.
- `IDataFormRepository.GetList`: returns `DataFormListResult` (`Table` + `Paging`) and accepts optional `PagingOptions? paging`.
- `CompanyInfo.LogDatabaseId` removed: `DbScope.Log` resolves to fixed `databaseId = "log"`.
- `SelectCommandBuilder`: unknown table name now throws `InvalidOperationException` (was `KeyNotFoundException`).

### Upgrade Guide

```diff
- var repo = factory.CreateDataFormRepository("Employee");
+ var repo = factory.CreateDataFormRepository("Employee", accessToken);
```

```diff
- DataTable table = repo.GetList(filter, sortFields, fields);
+ DataFormListResult result = repo.GetList(filter, sortFields, fields, paging: null);
+ DataTable table = result.Table;
```

```diff
- var logDbId = companyInfo.LogDatabaseId;
+ var logDbId = "log";  // Fixed framework routing; cross-company isolation via row-level partitioning
```

## [4.3.0]

> Bee.NET is in pre-stable evolution; minor releases may include namespace moves while the public surface still has no external consumers. This release moves `AddBeeFramework` to a dedicated package — strictly a SemVer-major change, but treated as minor under the pre-stable policy.

📄 Full notes & design context: [docs/changelogs/4.3.0.md](docs/changelogs/4.3.0.md)

### Added

- `Bee.Hosting`: new package — framework composition root registering all backend services (`IDefineAccess`, `IDbAccessFactory`, `IBusinessObjectFactory`, `JsonRpcExecutor`, etc.) into any `IServiceCollection` without depending on ASP.NET Core.

### Changed

- `Bee.Hosting`: `BeeFrameworkServiceCollectionExtensions.AddBeeFramework` moved here from `Bee.Api.AspNetCore` (namespace `Bee.Api.AspNetCore` → `Bee.Hosting`).
- `Bee.Api.AspNetCore`: now contains only ASP.NET Core integration (`UseBeeFramework` + `ApiServiceController`); 4 previous project references consolidated under `Bee.Hosting`.

### Upgrade Guide

```diff
+ using Bee.Hosting;
  using Bee.Api.AspNetCore;

  var settings = SystemSettingsLoader.Load(pathOptions);
  services.AddBeeFramework(settings.BackendConfiguration, pathOptions);
  app.UseBeeFramework();
```

```diff
  <!-- *.csproj -->
- <PackageReference Include="Bee.Api.AspNetCore" Version="4.2.*" />
+ <PackageReference Include="Bee.Hosting" Version="4.3.*" />
```

## [4.2.0] and earlier

See git history (`git log --oneline`).
