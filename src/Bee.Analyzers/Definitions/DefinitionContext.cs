using Microsoft.CodeAnalysis;

namespace Bee.Analyzers.Definitions
{
    /// <summary>
    /// The definition files supplied to one compilation, indexed for the cross-file rules.
    /// </summary>
    /// <remarks>
    /// Built once per analyzer invocation. Parsing itself is cached by
    /// <see cref="DefinitionDocumentLoader"/>, so rebuilding this index in each analyzer only walks
    /// already-parsed documents.
    /// </remarks>
    internal sealed class DefinitionContext
    {
        private readonly Dictionary<string, FormSchemaModel> _formSchemasByProgId;
        private readonly Dictionary<string, TableSchemaModel> _tableSchemasByKey;
        private readonly HashSet<string> _formLayoutProgIds;

        private DefinitionContext(
            List<FormSchemaModel> formSchemas,
            Dictionary<string, FormSchemaModel> formSchemasByProgId,
            Dictionary<string, TableSchemaModel> tableSchemasByKey,
            HashSet<string> formLayoutProgIds,
            List<LanguageResourceModel> languageResources,
            DbCategoryRegistry? categories)
        {
            FormSchemas = formSchemas;
            _formSchemasByProgId = formSchemasByProgId;
            _tableSchemasByKey = tableSchemasByKey;
            _formLayoutProgIds = formLayoutProgIds;
            LanguageResources = languageResources;
            Categories = categories;
        }

        /// <summary>
        /// Gets every form schema found in the additional files.
        /// </summary>
        public IReadOnlyList<FormSchemaModel> FormSchemas { get; }

        /// <summary>
        /// Gets every table schema found in the additional files.
        /// </summary>
        public IEnumerable<TableSchemaModel> TableSchemas => _tableSchemasByKey.Values;

        /// <summary>
        /// Gets every language resource found in the additional files.
        /// </summary>
        public IReadOnlyList<LanguageResourceModel> LanguageResources { get; }

        /// <summary>
        /// Gets the table-to-scope registrations, or <c>null</c> when no settings file is present.
        /// </summary>
        public DbCategoryRegistry? Categories { get; }

        /// <summary>
        /// Gets a value indicating whether any table schema file was supplied.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: Rules asserting that a table schema exists must check this first. Definitions can
        /// live in the database rather than the file system (see <c>DbDefineStorage</c>), and a consumer
        /// may also supply only a subset of their definitions as AdditionalFiles. Reporting every table
        /// as missing in those cases would be noise, not a finding.
        /// </remarks>
        public bool HasTableSchemaFiles => _tableSchemasByKey.Count > 0;

        /// <summary>
        /// Gets a value indicating whether any form layout file was supplied.
        /// </summary>
        public bool HasFormLayoutFiles => _formLayoutProgIds.Count > 0;

        /// <summary>
        /// Builds the index from the additional files of a compilation.
        /// </summary>
        /// <param name="additionalFiles">The additional files supplied to the compilation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The index, which is empty when no definition files are present.</returns>
        public static DefinitionContext Create(
            IEnumerable<AdditionalText> additionalFiles,
            CancellationToken cancellationToken)
        {
            var formSchemas = new List<FormSchemaModel>();
            var schemasByProgId = new Dictionary<string, FormSchemaModel>(StringComparer.OrdinalIgnoreCase);
            var tablesByKey = new Dictionary<string, TableSchemaModel>(StringComparer.OrdinalIgnoreCase);
            var formLayoutProgIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var languageResources = new List<LanguageResourceModel>();
            var files = additionalFiles as IReadOnlyList<AdditionalText> ?? additionalFiles.ToList();

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (DefinitionFileNames.IsFormSchema(file.Path))
                {
                    var schema = FormSchemaModel.TryCreate(file, cancellationToken);
                    if (schema is null)
                        continue;

                    formSchemas.Add(schema);
                    if (!schemasByProgId.ContainsKey(schema.ProgId))
                        schemasByProgId[schema.ProgId] = schema;
                }
                else if (DefinitionFileNames.IsTableSchema(file.Path))
                {
                    var table = TableSchemaModel.TryCreate(file, cancellationToken);
                    var key = DefinitionFileNames.GetTableSchemaKey(file.Path);
                    if (table is not null && key is not null && !tablesByKey.ContainsKey(key))
                        tablesByKey[key] = table;
                }
                else if (DefinitionFileNames.IsFormLayout(file.Path))
                {
                    formLayoutProgIds.Add(DefinitionFileNames.GetProgIdFromSidecar(
                        file.Path, DefinitionFileNames.FormLayoutSuffix));
                }
                else if (DefinitionFileNames.IsLanguage(file.Path))
                {
                    var resource = LanguageResourceModel.TryCreate(file, cancellationToken);
                    if (resource is not null)
                        languageResources.Add(resource);
                }
            }

            return new DefinitionContext(
                formSchemas,
                schemasByProgId,
                tablesByKey,
                formLayoutProgIds,
                languageResources,
                DbCategoryRegistry.TryCreate(files, cancellationToken));
        }

        /// <summary>
        /// Finds the form schema declaring the specified program identifier.
        /// </summary>
        /// <param name="progId">The program identifier to look up.</param>
        /// <returns>The schema, or <c>null</c> when no schema declares it.</returns>
        public FormSchemaModel? FindByProgId(string progId)
            => _formSchemasByProgId.TryGetValue(progId, out var schema) ? schema : null;

        /// <summary>
        /// Finds the table schema filed under the specified scope for the specified table.
        /// </summary>
        /// <param name="categoryId">The database scope, which is also the containing folder name.</param>
        /// <param name="tableName">The physical table name.</param>
        /// <returns>The table schema, or <c>null</c> when none was supplied.</returns>
        public TableSchemaModel? FindTableSchema(string categoryId, string tableName)
            => _tableSchemasByKey.TryGetValue(
                DefinitionFileNames.ComposeTableSchemaKey(categoryId, tableName), out var table)
                ? table
                : null;

        /// <summary>
        /// Determines whether a form layout exists for the specified program identifier.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        /// <returns><c>true</c> when a matching form layout file was supplied.</returns>
        public bool HasFormLayout(string progId) => _formLayoutProgIds.Contains(progId);
    }
}
