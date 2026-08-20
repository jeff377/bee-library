using System.Globalization;
using Bee.Api.Client.Definitions;
using Bee.Api.Client.Connectors;
using Bee.Base.Exceptions;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Core;

namespace Bee.UI.Avalonia.Views
{
    /// <summary>
    /// The overridable resolution hooks: schema, layout, language, connector, access token and rounding context.
    /// </summary>
    /// <remarks>
    /// Every member here is `protected virtual` and exists for a host to replace. Grouping them means a
    /// subclass author sees the whole substitution surface at once instead of hunting through the view.
    /// </remarks>
    public partial class FormView
    {
        /// <summary>
        /// Resolves the <see cref="FormSchema"/> for <paramref name="progId"/> when the host did
        /// not pre-set <see cref="Schema"/>. Defaults to the cached <see cref="ClientInfo.DefineAccess"/>;
        /// override to supply a schema without touching the static <see cref="ClientInfo"/>.
        /// </summary>
        protected virtual async Task<FormSchema?> ResolveSchemaAsync(string progId)
            => DefinitionLoader is null
                ? await ClientInfo.DefineAccess.GetFormSchemaAsync(progId).ConfigureAwait(false)
                : await DefinitionLoader.GetLocalizedSchemaAsync(progId, ResolveLang()).ConfigureAwait(false);

        /// <summary>
        /// Resolves the <see cref="FormLayout"/> the record renders from, in three steps: the
        /// <see cref="Layout"/> the host set, else the <see cref="DefinitionLoader"/>'s assembled
        /// runtime layout, else the stored base definition fetched through
        /// <see cref="ClientInfo.DefineAccess"/>.
        /// </summary>
        /// <param name="progId">The program identifier, which doubles as the layout identifier.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no layout definition is stored under <paramref name="progId"/>. Layouts are
        /// authored at design time and saved as definition files; neither this view nor the loader
        /// generates one from the schema.
        /// </exception>
        protected virtual async Task<FormLayout> ResolveLayoutAsync(string progId)
        {
            if (Layout is not null) return Layout;

            if (DefinitionLoader is not null)
            {
                return await DefinitionLoader
                    .GetRuntimeLayoutAsync(progId, Schema!)
                    .ConfigureAwait(false);
            }

            // The define cache hands back a shared instance, and the capability applier mutates the
            // layout in place, so this path clones what it fetched. The loader path clones already.
            var definition = await ClientInfo.DefineAccess.GetFormLayoutAsync(progId).ConfigureAwait(false);
            return definition?.Clone()
                ?? throw new InvalidOperationException(
                    $"No FormLayout definition found for '{progId}'. Author one at design time and save "
                    + $"it as 'FormLayout/{progId}.FormLayout.xml' under the definition path, or set "
                    + $"{nameof(FormView)}.{nameof(Layout)} before loading the form.");
        }

        /// <summary>
        /// Gets or sets the assembler that turns the raw definitions the server serves into a
        /// localized schema and a runtime layout. <c>null</c> — the default — keeps the view purely
        /// local: the schema is fetched as stored, and the layout comes from
        /// <see cref="Layout"/> or the stored definition.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Opt-in on purpose. Assembling the runtime definitions costs API round trips (both
        /// language layers, both layout layers), and <see cref="Schema"/> is a public property, so a
        /// host may legitimately supply a schema with no backend behind it at all. Making the loader
        /// an explicit dependency keeps that case working and states plainly which forms pay for
        /// tenant customization.
        /// </para>
        /// <para>
        /// Set it to enable customized layouts, localized captions and company number formats:
        /// <c>view.DefinitionLoader = new FormDefinitionLoader(ClientInfo.DefineAccess) { CompanyAccessor = () =&gt; ClientInfo.Company }</c>.
        /// Pass the company accessor as a delegate, not a value — the entered company changes over
        /// the session's life and a captured value would keep baking the previous tenant's decimals.
        /// </para>
        /// </remarks>
        public FormDefinitionLoader? DefinitionLoader { get; set; }

        /// <summary>
        /// Resolves the language the form renders in. Defaults to the UI culture, the same source
        /// <c>BeeStringLocalizer</c> falls back to. Only consulted when
        /// <see cref="DefinitionLoader"/> is set.
        /// </summary>
        protected virtual string ResolveLang() => CultureInfo.CurrentUICulture.Name;

        /// <summary>
        /// Resolves the <see cref="FormApiConnector"/> for the load / save round-trips.
        /// Override to bypass <see cref="ClientInfo"/>.
        /// </summary>
        protected virtual FormApiConnector ResolveFormConnector(string progId)
            => ClientInfo.CreateFormApiConnector(progId);

        /// <summary>Resolves the access token. Override to plug in a different session source.</summary>
        protected virtual Guid ResolveAccessToken() => ClientInfo.AccessToken;

        /// <summary>
        /// Resolves the rounding context used to round live-preview computed fields and format
        /// amount/quantity cells (Tier 2). The default pulls the currency/unit masters through the cached
        /// <see cref="ClientInfo.DefineAccess"/> and the company from <see cref="ClientInfo.Company"/>;
        /// each part is optional and degrades to framework-default decimal places when absent. Override
        /// to supply a context without touching the static <see cref="ClientInfo"/> (the unit tests do).
        /// </summary>
        protected virtual async Task<RoundingContext> ResolveRoundingContextAsync()
        {
            return new RoundingContext
            {
                Company = ClientInfo.Company,
                CurrencySettings = await TryResolveSettingAsync(ClientInfo.DefineAccess.GetCurrencySettingsAsync)
                    .ConfigureAwait(true),
                UnitSettings = await TryResolveSettingAsync(ClientInfo.DefineAccess.GetUnitSettingsAsync)
                    .ConfigureAwait(true),
            };
        }

        // Best-effort fetch of an optional definition master: a missing master already returns null, and
        // a permission/API error must not break the form — live preview simply falls back to
        // framework-default decimals for that kind (the server still rounds authoritatively on save).
        private static async Task<T?> TryResolveSettingAsync<T>(Func<Task<T>> fetch) where T : class
        {
            try
            {
                return await fetch().ConfigureAwait(true);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
            catch (ForbiddenException)
            {
                return null;
            }
        }

        /// <summary>
        /// Called after the form mode changed and was broadcast to the scope. Refreshes the
        /// mode-dependent toolbar.
        /// </summary>
        /// <param name="formMode">The new form mode.</param>
        protected virtual void OnFormModeChanged(SingleFormMode formMode) => UpdateToolbarState();
    }
}
