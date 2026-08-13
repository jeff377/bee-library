# Tenant Customization

[繁體中文](customization.zh-TW.md) · [← Docs Index](README.md)

> How one company gets different behaviour from the same deployment — different field labels, a
> different form arrangement, an extra check before saving — without forking the base definitions
> or the code.

## The model in one paragraph

A deployment has one set of base definitions under `DefinePath`. A tenant that needs something
different gets a folder of its own under `CustomizePath`, holding **only what differs**. At runtime
the framework reads both and decides which wins, per lookup. A tenant with no folder resolves
against the base layer, bit for bit as if the feature were not there — which is the default, and
costs nothing.

The tenant is never chosen by the caller. It comes from the session: `st_company.customize_id` is
copied onto `SessionInfo.CustomizeId` when the session enters a company, and every server-side
consumer reads it from there and nowhere else.

## What to reach for

| You want to change | Use | Where it lives |
|---|---|---|
| Field labels, form names, messages, option text | **Language resource** | `{CustomizePath}/{customizeId}/Language/{lang}/{namespace}.Language.xml` |
| Which fields appear, their arrangement on screen | **FormLayout** | `{CustomizePath}/{customizeId}/FormLayout/{layoutId}.FormLayout.xml` |
| How the menu is grouped, ordered, labelled, and which entries are visible | **MenuSettings** | `{CustomizePath}/{customizeId}/MenuSettings.xml` |
| A program's whole behaviour — validation, workflow, AnyCode SQL | **Custom business object** | `{CustomizePath}/{customizeId}/ProgramSettings.xml` |
| How a program reads and writes its data | **Custom repository** | `{CustomizePath}/{customizeId}/ProgramSettings.xml` |
| An extra step in the existing save/delete flow | **Business plugin** | `{CustomizePath}/{customizeId}/PluginSettings.xml` |

The first three are definition-only — no code, no deployment of assemblies. The last three name types,
so they need an assembly in the host's `bin`.

**Prefer the lightest one that does the job.** A label change is a language entry, not a custom
business object. An extra validation is a plugin, not a subclass. Reaching for the heavier tool
works, but it is a class you have to maintain against every framework upgrade.

## Setting it up

Point `CustomizePath` at a directory alongside `DefinePath`:

```csharp
var paths = new PathOptions
{
    DefinePath = Path.Combine(deployRoot, "Define"),
    CustomizePath = Path.Combine(deployRoot, "Customize"),
};
```

**An empty `CustomizePath` disables the overlay entirely.** That is the default.

Then give the company a code — the value of `st_company.customize_id`. It becomes the folder name,
so it must be a valid directory name; the framework rejects one containing `..` or a path
separator. Several companies may share a code when they share a customization.

The directory need not exist. A tenant that supplies no file for a given lookup falls back to base.

## Language: labels, names and option text

The most common customization, and definition-only. Overlay is **per key**: the tenant's file holds
only the keys it changes, and every other key — including translations added to the base later —
comes from the base layer.

The namespace is the form's `ProgId`. Three sub-key shapes cover a form:

| Sub-key | Overrides |
|---|---|
| `Schema.DisplayName` | The form's own name |
| `Table.{TableName}.DisplayName` | A table's name within the form |
| `Field.{FieldName}.Caption` | A field's label |

To call the customer field "Account" for tenant `acme`, in Traditional Chinese:

```xml
<!-- Customize/acme/Language/zh-TW/Order.Language.xml -->
<LanguageResource Lang="zh-TW" Namespace="Order">
  <Items>
    <LanguageItem Key="Field.customer_id.Caption" Value="帳戶" />
  </Items>
</LanguageResource>
```

That is the whole file. Every other label on the form still comes from the base resource.

**Option sets are the exception: an enum is replaced whole.** A `LanguageEnum` of the same name in
the tenant's file supersedes the base one, so it must list every entry the option set should have —
merging entry by entry would leave both the ordering and the meaning of an omitted entry ambiguous.
A field may name an enum in another namespace by qualifying it (`Common.Gender`), in which case the
tenant's override goes in that namespace's file.

## FormLayout: what appears and where

Overlay is **whole file**. A tenant that customizes a layout owns it completely: copy the base
`{layoutId}.FormLayout.xml` into the tenant folder and edit it. The `layoutId` defaults to the
`ProgId`.

Owning it whole cuts both ways. A field added to the base `FormSchema` later **does not** appear on
that tenant's form, and the framework neither merges it in nor warns. This is the intent rather
than a limitation — the layout is the authority on what the screen shows, and a schema gaining a
field is not a statement that every tenant should now display it. Putting it on that tenant's form
is a decision, made by editing that tenant's layout file.

Captions are **not** part of the layout file: a UI head applies them from the localized schema after
picking the layout, so label changes belong in the language resource even for a customized layout.

> **How it reaches the screen.** The API always serves the raw definitions; the assembly happens on
> the client, in `FormDefinitionLoader`, which fetches both layers, picks the tenant's layout when
> there is one, and falls back to generating a layout from the schema when neither layer defines
> one. A UI head that does not go through `FormDefinitionLoader` will not see layout customization.

## Business object and repository: replacing a program's behaviour

Bind different types for the tenant in `ProgramSettings.xml`. Overlay is **per progId, then per
property** — write only the binding you are changing:

```xml
<!-- Customize/acme/ProgramSettings.xml -->
<ProgramSettings>
  <Items>
    <ProgramItem ProgId="Order" BusinessObject="Acme.Erp.OrderBo, Acme.Erp" />
  </Items>
</ProgramSettings>
```

The base entry's `Repository` and `DisplayName` still apply. To return a binding to the framework's
own type deliberately, name that type explicitly rather than clearing the attribute.

Writing the subclass itself is ordinary development — see
[Customising the BO for a ProgId](development-cookbook.md) in the cookbook, and
[BO Extension Points and the Transaction Boundary](development-cookbook.md) for which step to
override and what runs inside the database transaction.

## Business plugins: adding a step

When the customization is an *addition* rather than a replacement — a check before saving, a
notification afterwards — bind a plugin instead of replacing the business object:

```xml
<!-- Customize/acme/PluginSettings.xml -->
<PluginSettings>
  <Items>
    <ProgramPluginItem ProgId="Order">
      <Plugins>
        <PluginItem Type="Acme.Erp.CreditLimitPlugin, Acme.Erp" />
      </Plugins>
    </ProgramPluginItem>
  </Items>
</PluginSettings>
```

Plugins are the one artifact where the two layers **add up**: the base chain runs first, then the
tenant's. A tenant therefore cannot suppress a packaged plugin — to remove packaged behaviour,
subclass the business object and override the step.

See [Business Plugins](development-cookbook.md) in the cookbook for the four stages, the
one-instance-per-operation guarantee, and where side effects that reach other systems belong.

## Read-only, except for plugins

Customization files are produced by deployment tooling and read at runtime; every other write on
the override layer throws. **`PluginSettings` is the exception** — a deployment maintains its plugin
bindings through `SystemBO.GetCustomizePluginSettings` / `SaveCustomizePluginSettings`.

Both are `LocalOnly`: these bindings decide which code runs inside the save and delete pipelines, so
they are reachable only in-process, by a maintenance tool running on the host. Saving validates
every bound type — it must load, derive from `FormBusinessPlugin`, and override at least one stage —
and one bad entry rejects the whole definition.

With file-backed storage the write lands on the machine that served the call, so a multi-node
deployment needs `CustomizePath` on shared storage, or the database-backed storage, which is shared
by construction.

## What cannot be customized

**`FormSchema` and `TableSchema` are permanently excluded.** Both drive the database schema and the
validation rules as well as the UI; letting them diverge per tenant would split the physical schema.
This is a decision, not a gap — see [ADR-016](adr/adr-016-multitenant-customization-overlay.md).

The consequence worth planning around: a tenant cannot have an extra column. What it can have is a
different label for an existing one, a form that hides one, a business object that treats one
differently, or a plugin that fills one. If a tenant genuinely needs its own data, that is a schema
change for everyone, with the column left unused where it does not apply.

**Schema rules are not customizable either**, because they live in the `FormSchema`. Declarative
field defaults, computed fields and validation therefore apply to every tenant; per-tenant logic
goes in a plugin or a custom business object.

## Where to look next

| For | Read |
|---|---|
| The overlay mechanism: every path, the granularity of each type, how `customizeId` is resolved | [Definition Files Overview](definition-files-overview.md), §7 |
| Writing a custom business object, repository or plugin | [End-to-End Development Cookbook](development-cookbook.md) |
| Why the design is the way it is | [ADR-016](adr/adr-016-multitenant-customization-overlay.md) |
| The maintenance API's access control | [API Method Reference](api-method-reference.md) |
