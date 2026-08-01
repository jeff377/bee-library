using Bee.Definition.Customization;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;

namespace Bee.Api.Client.Definitions
{
    /// <summary>
    /// Assembles the runtime form definitions a UI needs — a localized <see cref="FormSchema"/> and
    /// a <see cref="FormLayout"/> — out of the raw definitions the server serves.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The APIs hand out definitions exactly as stored: no localization, no customization overlay,
    /// no generation. Assembling them is the caller's job, and doing it here rather than in each UI
    /// head means every head performs the same steps in the same order:
    /// </para>
    /// <list type="number">
    ///   <item><description>fetch the raw schema;</description></item>
    ///   <item><description>fetch both language layers for every namespace the schema references, and localize the schema through <c>FormSchemaLocalizer</c>;</description></item>
    ///   <item><description>fetch both layout layers, pick between them with <c>CustomizeOverlay</c>, and generate from the schema when neither exists;</description></item>
    ///   <item><description>take the layout's captions from the localized schema, so a layout file describes structure only.</description></item>
    /// </list>
    /// <para>
    /// Which tenant's customization arrives is never asked for here — the server derives it from the
    /// session. This class only decides how the two layers combine.
    /// </para>
    /// </remarks>
    public sealed class FormDefinitionLoader
    {
        private readonly ClientDefineAccess _defineAccess;
        private readonly string _defaultLang;

        /// <summary>
        /// Initializes a new <see cref="FormDefinitionLoader"/>.
        /// </summary>
        /// <param name="defineAccess">The client define access used to fetch raw definitions.</param>
        /// <param name="defaultLang">The system default language used for the localization fall-back hop; empty disables it.</param>
        public FormDefinitionLoader(ClientDefineAccess defineAccess, string defaultLang = "")
        {
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
            _defaultLang = defaultLang ?? string.Empty;
        }

        /// <summary>
        /// Fetches the raw schema for <paramref name="progId"/> and returns a localized copy.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        /// <param name="lang">The BCP-47 language code; empty returns the schema unlocalized.</param>
        /// <returns>A schema safe to mutate — the cached instance is never handed out.</returns>
        public async Task<FormSchema> GetLocalizedSchemaAsync(string progId, string lang)
        {
            var raw = await _defineAccess.GetFormSchemaAsync(progId).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(lang))
                return raw;

            // The client define cache hands back a shared instance; localization mutates, so clone.
            var schema = raw.Clone();
            var languageService = await BuildLanguageServiceAsync(schema, lang).ConfigureAwait(false);
            new FormSchemaLocalizer(languageService).Localize(schema, lang);
            return schema;
        }

        /// <summary>
        /// Returns the runtime layout for <paramref name="progId"/>: the tenant's layout definition
        /// when it has one, else the base definition, else one generated from the schema — with the
        /// captions taken from <paramref name="localizedSchema"/> in every case.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        /// <param name="localizedSchema">The localized schema, from <see cref="GetLocalizedSchemaAsync"/>.</param>
        /// <param name="layoutId">The layout identifier; empty resolves to <paramref name="progId"/>.</param>
        public async Task<FormLayout> GetRuntimeLayoutAsync(string progId, FormSchema localizedSchema, string layoutId = "")
        {
            ArgumentNullException.ThrowIfNull(localizedSchema);

            string effectiveLayoutId = string.IsNullOrWhiteSpace(layoutId) ? progId : layoutId;
            var customize = await _defineAccess.GetCustomizeFormLayoutAsync(progId, effectiveLayoutId).ConfigureAwait(false);
            var @base = await _defineAccess.GetFormLayoutAsync(effectiveLayoutId).ConfigureAwait(false);

            var definition = CustomizeOverlay.PickFormLayout(customize, @base);
            if (definition is null)
            {
                // No layout definition at all is the common case: derive one from the schema, which
                // already carries localized captions.
                return localizedSchema.GetFormLayout(effectiveLayoutId);
            }

            // Definitions come from the client define cache, so clone before the applier mutates.
            var layout = definition.Clone();
            FormLayoutCaptionApplier.Apply(layout, localizedSchema);
            return layout;
        }

        /// <summary>
        /// Fetches both language layers for every namespace <paramref name="schema"/> reads from and
        /// wraps them in a synchronous service.
        /// </summary>
        /// <remarks>
        /// The namespaces are the schema's own <c>ProgId</c> plus any namespace named by a
        /// fully-qualified <c>FormField.LangEnumName</c> (<c>"Common.Gender"</c>), which is how a
        /// schema borrows a shared option set. Each is fetched in the requested language and, when
        /// different, the default language too, so the service can apply the same fall-back the
        /// server does.
        /// </remarks>
        private async Task<SnapshotLanguageService> BuildLanguageServiceAsync(FormSchema schema, string lang)
        {
            var namespaces = CollectNamespaces(schema);
            var languages = new List<string> { lang };
            if (!string.IsNullOrEmpty(_defaultLang)
                && !string.Equals(lang, _defaultLang, StringComparison.OrdinalIgnoreCase))
            {
                languages.Add(_defaultLang);
            }

            var snapshot = new Dictionary<string, LanguageLayers>(StringComparer.Ordinal);
            foreach (string language in languages)
            {
                foreach (string ns in namespaces)
                {
                    var customize = await _defineAccess.GetCustomizeLanguageAsync(language, ns).ConfigureAwait(false);
                    var @base = await SafeGetLanguageAsync(language, ns).ConfigureAwait(false);
                    snapshot[SnapshotLanguageService.BuildKey(language, ns)] = new LanguageLayers(@base, customize);
                }
            }
            return new SnapshotLanguageService(snapshot, _defaultLang);
        }

        private static HashSet<string> CollectNamespaces(FormSchema schema)
        {
            var namespaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(schema.ProgId))
                namespaces.Add(schema.ProgId);

            if (schema.Tables == null)
                return namespaces;

            foreach (var table in schema.Tables)
            {
                if (table.Fields == null) continue;
                foreach (var field in table.Fields)
                {
                    string name = field.LangEnumName;
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    int dot = name.IndexOf('.');
                    // A bare name resolves against the schema's own namespace, already added above.
                    if (dot > 0) namespaces.Add(name.Substring(0, dot));
                }
            }
            return namespaces;
        }

        /// <summary>
        /// A namespace with no base resource is normal (a schema may be translated only in the
        /// customization layer, or not at all), so a missing file is an answer rather than a fault.
        /// </summary>
        private async Task<LanguageResource?> SafeGetLanguageAsync(string lang, string ns)
        {
            try
            {
                return await _defineAccess.GetLanguageAsync(lang, ns).ConfigureAwait(false);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }
    }
}
