using Bee.Definition.Forms;

namespace Bee.Definition.Language
{
    /// <summary>
    /// Applies localized text from <see cref="LanguageResource"/> to a <see cref="FormSchema"/>
    /// instance using a fixed sub-key convention. Namespace is the schema's <see cref="FormSchema.ProgId"/>;
    /// sub-keys follow the table below.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><see cref="FormSchema.DisplayName"/> ← <c>Schema.DisplayName</c></description></item>
    /// <item><description><see cref="FormTable.DisplayName"/> ← <c>Table.{TableName}.DisplayName</c></description></item>
    /// <item><description><see cref="FormField.Caption"/> ← <c>Field.{FieldName}.Caption</c></description></item>
    /// </list>
    /// <para>
    /// Missing keys leave the existing string untouched — schema authors who do not need
    /// i18n keep working with hard-coded labels with no behaviour change.
    /// </para>
    /// <para>
    /// The localizer mutates the schema in place. Callers that share schema instances
    /// (e.g. via the <c>FormSchema</c> Define cache) must clone first; this class deliberately
    /// does <b>not</b> clone, so the same helper composes cleanly with explicit cloning at
    /// the call site (typically the BO method right before returning to the API surface).
    /// </para>
    /// </remarks>
    public sealed class FormSchemaLocalizer
    {
        /// <summary>
        /// Sub-key for the schema-level display name.
        /// </summary>
        public const string SchemaDisplayNameKey = "Schema.DisplayName";

        /// <summary>
        /// Sub-key template for a table's display name. <c>{0}</c> is the table name.
        /// </summary>
        public const string TableDisplayNameKeyFormat = "Table.{0}.DisplayName";

        /// <summary>
        /// Sub-key template for a field's caption. <c>{0}</c> is the field name.
        /// </summary>
        public const string FieldCaptionKeyFormat = "Field.{0}.Caption";

        private readonly ILanguageService _languageService;

        /// <summary>
        /// Initializes a new <see cref="FormSchemaLocalizer"/>.
        /// </summary>
        /// <param name="languageService">The language resource service used to resolve sub-keys.</param>
        public FormSchemaLocalizer(ILanguageService languageService)
        {
            _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));
        }

        /// <summary>
        /// Applies localized text in <paramref name="lang"/> to the supplied <paramref name="schema"/>.
        /// Properties whose keys are missing from the language resource are left as-is.
        /// </summary>
        /// <param name="schema">The schema to mutate. Must not be a shared cache instance — clone first.</param>
        /// <param name="lang">The BCP-47 language code (e.g. <c>"zh-TW"</c>).</param>
        public void Localize(FormSchema schema, string lang)
        {
            ArgumentNullException.ThrowIfNull(schema);
            if (string.IsNullOrWhiteSpace(lang))
                return;

            string @namespace = schema.ProgId;
            if (string.IsNullOrWhiteSpace(@namespace))
                return;

            // 1. Schema.DisplayName
            if (_languageService.TryGetLangText(lang, @namespace, SchemaDisplayNameKey, out string schemaName))
                schema.DisplayName = schemaName;

            // 2. Walk tables → fields.
            if (schema.Tables == null)
                return;

            foreach (var table in schema.Tables)
                LocalizeTable(table, lang, @namespace);
        }

        private void LocalizeTable(FormTable table, string lang, string @namespace)
        {
            if (!string.IsNullOrWhiteSpace(table.TableName))
            {
                string tableKey = string.Format(System.Globalization.CultureInfo.InvariantCulture, TableDisplayNameKeyFormat, table.TableName);
                if (_languageService.TryGetLangText(lang, @namespace, tableKey, out string tableName))
                    table.DisplayName = tableName;
            }

            if (table.Fields == null)
                return;

            foreach (var field in table.Fields)
            {
                if (string.IsNullOrWhiteSpace(field.FieldName))
                    continue;
                string fieldKey = string.Format(System.Globalization.CultureInfo.InvariantCulture, FieldCaptionKeyFormat, field.FieldName);
                if (_languageService.TryGetLangText(lang, @namespace, fieldKey, out string caption))
                    field.Caption = caption;
            }
        }
    }
}
