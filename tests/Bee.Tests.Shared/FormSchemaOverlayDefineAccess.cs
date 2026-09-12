using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// An <see cref="IDefineAccess"/> that serves one test-built <see cref="FormSchema"/>, and the table
    /// schemas generated from it, on top of the fixture's own definitions.
    /// </summary>
    /// <remarks>
    /// For tests that need a form shape <c>tests/Define</c> does not have. Writing one there is not an
    /// option, because that folder is shared by every test project. Every other call reaches the inner
    /// access unchanged, and every save call is refused for the same reason.
    /// </remarks>
    public sealed class FormSchemaOverlayDefineAccess : IDefineAccess
    {
        private readonly IDefineAccess _inner;
        private readonly FormSchema _schema;

        /// <summary>
        /// Initializes a new instance of <see cref="FormSchemaOverlayDefineAccess"/>.
        /// </summary>
        /// <param name="inner">The fixture's define access.</param>
        /// <param name="schema">The form schema to serve under its own <see cref="FormSchema.ProgId"/>.</param>
        public FormSchemaOverlayDefineAccess(IDefineAccess inner, FormSchema schema)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        /// <inheritdoc/>
        public FormSchema GetFormSchema(string progId)
            => string.Equals(progId, _schema.ProgId, StringComparison.OrdinalIgnoreCase) ? _schema : _inner.GetFormSchema(progId);

        /// <inheritdoc/>
        public TableSchema GetTableSchema(string categoryId, string tableName)
        {
            var formTable = _schema.Tables?.FirstOrDefault(t =>
                string.Equals(t.GenerateDbTable().TableName, tableName, StringComparison.OrdinalIgnoreCase));
            return formTable != null ? formTable.GenerateDbTable() : _inner.GetTableSchema(categoryId, tableName);
        }

        /// <inheritdoc/>
        public object GetDefine(DefineType defineType, string[]? keys = null) => _inner.GetDefine(defineType, keys);
        /// <inheritdoc/>
        public SystemSettings GetSystemSettings() => _inner.GetSystemSettings();
        /// <inheritdoc/>
        public DatabaseSettings GetDatabaseSettings() => _inner.GetDatabaseSettings();
        /// <inheritdoc/>
        public ProgramSettings GetProgramSettings() => _inner.GetProgramSettings();
        /// <inheritdoc/>
        public MenuSettings GetMenuSettings() => _inner.GetMenuSettings();
        /// <inheritdoc/>
        public MenuSettings GetMenuSettings(string customizeId) => _inner.GetMenuSettings(customizeId);
        /// <inheritdoc/>
        public PluginSettings GetPluginSettings() => _inner.GetPluginSettings();
        /// <inheritdoc/>
        public PermissionModels GetPermissionModels() => _inner.GetPermissionModels();
        /// <inheritdoc/>
        public CurrencySettings GetCurrencySettings() => _inner.GetCurrencySettings();
        /// <inheritdoc/>
        public UnitSettings GetUnitSettings() => _inner.GetUnitSettings();
        /// <inheritdoc/>
        public DbCategorySettings GetDbCategorySettings() => _inner.GetDbCategorySettings();
        /// <inheritdoc/>
        public FormLayout GetFormLayout(string layoutId) => _inner.GetFormLayout(layoutId);
        /// <inheritdoc/>
        public FormLayout GetFormLayout(string customizeId, string layoutId) => _inner.GetFormLayout(customizeId, layoutId);
        /// <inheritdoc/>
        public FormLayout? FindFormLayout(string customizeId, string layoutId) => _inner.FindFormLayout(customizeId, layoutId);
        /// <inheritdoc/>
        public LanguageResource GetLanguage(string lang, string ns) => _inner.GetLanguage(lang, ns);

        /// <inheritdoc/>
        public void SaveDefine(DefineType defineType, object defineObject, string[]? keys = null) => throw ReadOnly();
        /// <inheritdoc/>
        public void SaveSystemSettings(SystemSettings settings) => throw ReadOnly();
        /// <inheritdoc/>
        public void SaveDatabaseSettings(DatabaseSettings settings) => throw ReadOnly();
        /// <inheritdoc/>
        public void SaveProgramSettings(ProgramSettings settings) => throw ReadOnly();
        /// <inheritdoc/>
        public void SaveMenuSettings(MenuSettings settings) => throw ReadOnly();
        /// <inheritdoc/>
        public void SavePluginSettings(PluginSettings settings) => throw ReadOnly();
        /// <inheritdoc/>
        public void SavePermissionModels(PermissionModels models) => throw ReadOnly();
        /// <inheritdoc/>
        public void SaveCurrencySettings(CurrencySettings settings) => throw ReadOnly();
        /// <inheritdoc/>
        public void SaveUnitSettings(UnitSettings settings) => throw ReadOnly();
        /// <inheritdoc/>
        public void SaveDbCategorySettings(DbCategorySettings settings) => throw ReadOnly();
        /// <inheritdoc/>
        public void SaveTableSchema(string categoryId, TableSchema tableSchema) => throw ReadOnly();
        /// <inheritdoc/>
        public void SaveFormSchema(FormSchema formSchema) => throw ReadOnly();
        /// <inheritdoc/>
        public void SaveFormLayout(FormLayout formLayout) => throw ReadOnly();
        /// <inheritdoc/>
        public void SaveLanguage(LanguageResource resource) => throw ReadOnly();

        private static NotSupportedException ReadOnly()
            => new("FormSchemaOverlayDefineAccess is read-only: tests/Define is shared by every test project.");
    }
}
