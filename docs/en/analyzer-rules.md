# Analyzer Rules

[繁體中文](analyzer-rules.zh-TW.md) · [← Docs Index](README.md)

Bee.NET ships Roslyn analyzers that turn framework conventions into build diagnostics. They are
registered automatically by `Bee.Definition`: adding that package is all it takes, with no additional
install step and no project configuration.

The rules exist because most framework conventions fail late. A form schema pointing at the wrong
database scope, a serialised type with no parameterless constructor, a business object method with no
access-control declaration — none of these break the build, and all of them surface only when the
application runs, often far from the definition that caused them. A diagnostic moves that discovery to
build time, where the message can name both the cause and the fix.

## Rules

### Definition files — single file (BEE1xxx)

| ID | Severity | Rule |
|----|----------|------|
| BEE1001 | Error | `FormSchema/@CategoryId` must be `common`, `company` or `log` |
| BEE1002 | Error | `DbCategory/@Id` must be one of the same three scopes |
| BEE1003 | Error | `@DbType` on `FormField` and `DbField` must name a `FieldDbType` member |
| BEE1004 | Error | `ListFields` / `LookupFields` may only reference declared fields |
| BEE1005 | Error | A relation mapping's `DestinationField` must be a declared field |
| BEE1006 | Warning | A field marked `Type="RelationField"` should be populated by a mapping |
| BEE1007 | Error | A table must not declare the same field name twice |
| BEE1008 | Info | A form schema with no `PermissionModelId` is open to every authenticated caller — layer-1 authorization is a no-op for it |


> **BEE1008 is invisible by default, and that is a deliberate trade-off.** Leaving unmarked forms
> open is the framework's gradual-adoption stance (see the XML doc on `FormBusinessObject.Authorize`),
> and a warning would fail the build of every deployment that is midway through adopting permission
> models. The cost is that `Info` maps to an MSBuild message, which the default verbosity does not
> print — and every definition analyzer is a compilation-end diagnostic, so IDEs do not surface it
> live either. **The rule exists, but you will not see it.** Raise it in `.editorconfig` once a
> deployment has finished adopting permission models, and it starts doing real work:
>
> ```ini
> dotnet_diagnostic.BEE1008.severity = warning
> ```

### Definition files — cross file (BEE2xxx)

| ID | Severity | Rule |
|----|----------|------|
| BEE2001 | Warning | A form schema's table must be registered under the matching scope in `DbCategorySettings.xml` |
| BEE2002 | Warning | A table schema must exist at `TableSchema/<categoryId>/<table>.TableSchema.xml` — the folder must match the CategoryId |
| BEE2003 | Error | `@RelationProgId` must name a program identifier some form schema declares |
| BEE2004 | Error | A relation mapping's `SourceField` must be declared by the referenced schema |
| BEE2005 | Warning | A form schema must have a corresponding form layout — the runtime renders the stored layout and fails without one |
| BEE2006 | Warning | A persisted form schema field must exist as a column in the table schema |
| BEE2007 | Info | Language resources should cover the same keys across cultures |

### Coding conventions (BEE3xxx)

| ID | Severity | Rule |
|----|----------|------|
| BEE3001 | Warning | A public method on a business object must be covered by `[ApiAccessControl]` |
| BEE3002 | Warning | Definition-layer collection properties must use a framework collection type — framework-internal, does not apply to consumer projects |
| BEE3003 | Warning | A public method on an `IExecFuncHandler` implementation must be covered by `[ExecFuncAccessControl]` |

### Serialisation and wire contracts (BEE4xxx)

| ID | Severity | Rule |
|----|----------|------|
| BEE4005 | Warning | A framework collection should expose a single public `Add` |
| BEE4006 | Error | A serialised type must have a public parameterless constructor |

### Build gates (BEE9xxx)

| ID | Severity | Rule |
|----|----------|------|
| BEE9001 | Error | A locked assembly may only reference what its allowlist names |
| BEE9002 | Error | `Version`, `AssemblyVersion` and `FileVersion` must stay in step |
| BEE9003 | Error | `BeeDefinitionFilesGlob` matched no files while `BeeRequireDefinitionFiles` is set |

**None of the BEE9xxx are Roslyn analyzers**; they are MSBuild targets, listed here so the numbering
has one home. BEE9001 and BEE9002 live in `src/Directory.Build.targets` and are framework-internal —
a consumer project cannot trigger them. BEE9003 ships in the package and is opt-in; see
[Checking what the glob matched](#checking-what-the-glob-matched). BEE9001 exists because anything
added to the assemblies at the bottom of the dependency graph is inherited by every consumer of the
framework ([ADR-038](adr/adr-038-definition-dependency-boundary.md)). Which assemblies are locked is
not listed here — `src/Directory.Build.targets` declares them and nothing would catch this copy
drifting; it already did, staying at two after a third was added; BEE9002 exists because a release that
bumps only `Version` ships packages whose assemblies still claim the previous version, and a
published package cannot be recalled.

Three rules are marked framework-internal in the tables above — BEE3002 runs only inside the
framework's own `Bee.Definition` assembly, and BEE9001 / BEE9002 only inside this repository. They
are listed for completeness rather than because a consumer project can trigger them. Every other
rule applies to consumer projects, including BEE4005: a collection you derive from
`CollectionBase` or `KeyCollectionBase` yourself is checked exactly as the framework's own are.

## Where the definition file rules read from

BEE1xxx and BEE2xxx analyse XML rather than C#, which MSBuild has to hand to the compiler explicitly.
The package does that for you: `buildTransitive/Bee.Definition.targets` adds `Define\**\*.xml` to
`AdditionalFiles`, rooted at the project directory and excluding build output. This applies whether
`Bee.Definition` is referenced directly or arrives through `Bee.Business`, `Bee.Db`,
`Bee.Api.AspNetCore` or `Bee.Hosting`.

**The glob is rooted at the project directory and is not searched for above it.** The usual layout
keeps the definitions beside the solution file rather than beside the server project, and that needs
one line — on the single project that owns them, so the same findings are not reported once per
project in the solution:

```xml
<PropertyGroup>
  <BeeDefinitionFilesGlob>..\Define\**\*.xml</BeeDefinitionFilesGlob>
</PropertyGroup>
```

Point it anywhere else the same way if your definitions live outside `Define`:

```xml
<PropertyGroup>
  <BeeDefinitionFilesGlob>MyDefinitions\**\*.xml</BeeDefinitionFilesGlob>
</PropertyGroup>
```

Definitions stored in the database instead of the file system need no configuration — with no
definition files to read, the cross-file rules stay silent rather than reporting every table as
missing.

### Checking what the glob matched

A glob matching nothing looks exactly like a project that has no definitions: the rules simply stay
quiet. Two ways to tell the difference —

```bash
dotnet build MyApp.Server.csproj -v n
```

reports the count at normal verbosity (`Bee.NET: BeeDefinitionFilesGlob '…' matched N definition
file(s) …`), and a project that knows it owns definition files can turn the empty case into a build
failure:

```xml
<PropertyGroup>
  <BeeRequireDefinitionFiles>true</BeeRequireDefinitionFiles>
</PropertyGroup>
```

That raises **BEE9003** when the glob matches nothing. It is opt-in rather than the default because
most projects in a solution legitimately have no definition files of their own, and warning on all of
them would be noise — under `TreatWarningsAsErrors`, a build break.

## Adjusting severity

**IMPORTANT: which mechanism works depends on where the diagnostic is reported.**

| Rules | Reported on | Use |
|-------|-------------|-----|
| BEE1xxx, BEE2xxx | your XML definition files | `.globalconfig` |
| BEE3xxx, BEE4xxx | your C# source | `.editorconfig` or `.globalconfig` |

`.editorconfig` resolves a diagnostic's severity through the file its location belongs to. The
definition file rules report against XML supplied as `AdditionalFiles`, which is not part of the
compilation's syntax trees, so no `.editorconfig` section applies to them — not `[*.cs]`, and not
`[*.xml]` either. A `.globalconfig` is compilation-wide and therefore does apply.

Create `.globalconfig` next to your project file:

```ini
is_global = true

# Downgrade a rule
dotnet_diagnostic.BEE2001.severity = suggestion

# Turn one off entirely
dotnet_diagnostic.BEE2005.severity = none
```

For the C# rules, either file works:

```ini
# .editorconfig
[*.cs]
dotnet_diagnostic.BEE4006.severity = warning
```

Accepted values are `error`, `warning`, `suggestion`, `silent` and `none`. Note that `dotnet build`
does not print `suggestion` or `silent` diagnostics at default verbosity — they appear in IDEs only.
Downgrading a rule that far therefore removes it from build output altogether.

## Turning the definition file rules off

```xml
<PropertyGroup>
  <BeeAnalyzeDefinitionFiles>false</BeeAnalyzeDefinitionFiles>
</PropertyGroup>
```

This stops the definition files being supplied to the compiler at all, which silences every BEE1xxx
and BEE2xxx rule at once. The C# rules are unaffected; disable those individually as above.

## Versioning

Analyzer rules are part of the package's observable behaviour, so a new rule can fail a build that
previously succeeded — most visibly in projects using `TreatWarningsAsErrors`. The framework therefore
treats them as follows:

- **New rules arrive in minor versions, never in patches.** Upgrading a patch version will not
  introduce a diagnostic you have not seen before.
- **Raising an existing rule's severity is a minor-version change** and is called out in the
  changelog for that version.
- **Every rule is individually adjustable and the definition file group is collectively disableable**,
  so an upgrade never leaves you without a way to proceed.

Severity is assigned by how likely a rule is to be wrong rather than by how bad the underlying defect
is. Rules that report something necessarily broken are errors; rules with legitimate exceptions are
warnings, and may be raised to errors in a later minor version once their false positive rate has been
observed in practice.
