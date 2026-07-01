using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Base.Serialization;

namespace Bee.Definition.Storage
{
    /// <summary>
    /// A file-based implementation of define data read and write operations.
    /// Provides file access for database category settings, table schema, form schema, and form layout objects.
    /// Manages persistence of all define data through XML serialization and deserialization.
    /// </summary>
    public class FileDefineStorage : IDefineStorage
    {
        private readonly PathOptions _paths;

        /// <summary>
        /// Initializes a new instance of <see cref="FileDefineStorage"/> bound to the supplied
        /// <see cref="PathOptions"/>. All file path resolution flows through the injected instance.
        /// </summary>
        /// <param name="paths">The path options that determine where definition files live on disk.</param>
        public FileDefineStorage(PathOptions paths)
        {
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        }

        /// <summary>
        /// Gets the database category settings.
        /// </summary>
        public DbCategorySettings? GetDbCategorySettings()
        {
            string filePath = _paths.GetDbCategorySettingsFilePath();
            ValidateFilePath(filePath);
            return XmlCodec.DeserializeFromFile<DbCategorySettings>(filePath);
        }

        /// <summary>
        /// Saves the database category settings.
        /// </summary>
        /// <param name="settings">The database category settings.</param>
        public void SaveDbCategorySettings(DbCategorySettings settings)
        {
            string filePath = _paths.GetDbCategorySettingsFilePath();
            XmlCodec.SerializeToFile(settings, filePath);
        }

        /// <summary>
        /// Gets the system-level currency master. Returns <c>null</c> when the file does not exist —
        /// a missing currency master is a normal scenario (unlike <see cref="GetDbCategorySettings"/>),
        /// so callers fall back to framework-default decimals (see plan-numeric-multicurrency.md).
        /// </summary>
        public CurrencySettings? GetCurrencySettings()
        {
            string filePath = _paths.GetCurrencySettingsFilePath();
            if (!File.Exists(filePath))
                return null;
            return XmlCodec.DeserializeFromFile<CurrencySettings>(filePath);
        }

        /// <summary>
        /// Saves the system-level currency master.
        /// </summary>
        /// <param name="settings">The currency master.</param>
        public void SaveCurrencySettings(CurrencySettings settings)
        {
            string filePath = _paths.GetCurrencySettingsFilePath();
            XmlCodec.SerializeToFile(settings, filePath);
        }

        /// <summary>
        /// Gets the system-level unit-of-measure master. Returns <c>null</c> when the file does not
        /// exist — a missing unit master is a normal scenario, so callers fall back to framework
        /// defaults (see plan-numeric-uom.md).
        /// </summary>
        public UnitSettings? GetUnitSettings()
        {
            string filePath = _paths.GetUnitSettingsFilePath();
            if (!File.Exists(filePath))
                return null;
            return XmlCodec.DeserializeFromFile<UnitSettings>(filePath);
        }

        /// <summary>
        /// Saves the system-level unit-of-measure master.
        /// </summary>
        /// <param name="settings">The unit master.</param>
        public void SaveUnitSettings(UnitSettings settings)
        {
            string filePath = _paths.GetUnitSettingsFilePath();
            XmlCodec.SerializeToFile(settings, filePath);
        }

        /// <summary>
        /// Gets the program settings.
        /// </summary>
        public ProgramSettings? GetProgramSettings()
        {
            string filePath = _paths.GetProgramSettingsFilePath();
            ValidateFilePath(filePath);
            return XmlCodec.DeserializeFromFile<ProgramSettings>(filePath);
        }

        /// <summary>
        /// Saves the program settings.
        /// </summary>
        /// <param name="settings">The program settings.</param>
        public void SaveProgramSettings(ProgramSettings settings)
        {
            string filePath = _paths.GetProgramSettingsFilePath();
            XmlCodec.SerializeToFile(settings, filePath);
        }

        /// <summary>
        /// Gets the table schema for the specified category and table.
        /// </summary>
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableName">The table name.</param>
        public TableSchema? GetTableSchema(string categoryId, string tableName)
        {
            string filePath = _paths.GetTableSchemaFilePath(categoryId, tableName);
            ValidateFilePath(filePath);
            return XmlCodec.DeserializeFromFile<TableSchema>(filePath);
        }

        /// <summary>
        /// Saves the table schema for the specified category.
        /// </summary>
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableSchema">The table schema.</param>
        public void SaveTableSchema(string categoryId, TableSchema tableSchema)
        {
            string filePath = _paths.GetTableSchemaFilePath(categoryId, tableSchema.TableName);
            XmlCodec.SerializeToFile(tableSchema, filePath);
        }

        /// <summary>
        /// Gets the form schema for the specified program.
        /// </summary>
        /// <param name="progId">The program ID.</param>
        public FormSchema? GetFormSchema(string progId)
        {
            string filePath = _paths.GetFormSchemaFilePath(progId);
            ValidateFilePath(filePath);
            return XmlCodec.DeserializeFromFile<FormSchema>(filePath);
        }

        /// <summary>
        /// Saves the form schema.
        /// </summary>
        /// <param name="formSchema">The form schema.</param>
        public void SaveFormSchema(FormSchema formSchema)
        {
            string filePath = _paths.GetFormSchemaFilePath(formSchema.ProgId);
            XmlCodec.SerializeToFile(formSchema, filePath);
        }

        /// <summary>
        /// Gets the form layout for the specified layout ID.
        /// </summary>
        /// <param name="layoutId">The form layout ID.</param>
        public FormLayout? GetFormLayout(string layoutId)
        {
            string filePath = _paths.GetFormLayoutFilePath(layoutId);
            ValidateFilePath(filePath);
            return XmlCodec.DeserializeFromFile<FormLayout>(filePath);
        }

        /// <summary>
        /// Saves the form layout.
        /// </summary>
        /// <param name="formLayout">The form layout.</param>
        public void SaveFormLayout(FormLayout formLayout)
        {
            string filePath = _paths.GetFormLayoutFilePath(formLayout.LayoutId);
            XmlCodec.SerializeToFile(formLayout, filePath);
        }

        /// <summary>
        /// Gets the language resource for the specified language and namespace.
        /// Returns <c>null</c> when the file does not exist — missing translation
        /// files are a normal scenario (unlike <see cref="GetFormSchema"/>, where a
        /// missing file indicates a bug), so this path is non-throwing and
        /// negative-cacheable.
        /// </summary>
        /// <param name="lang">The BCP-47 language code.</param>
        /// <param name="ns">The resource namespace.</param>
        public LanguageResource? GetLanguage(string lang, string ns)
        {
            string filePath = _paths.GetLanguageFilePath(lang, ns);
            if (!File.Exists(filePath))
                return null;
            return XmlCodec.DeserializeFromFile<LanguageResource>(filePath);
        }

        /// <summary>
        /// Saves the language resource.
        /// </summary>
        /// <param name="resource">The language resource.</param>
        public void SaveLanguage(LanguageResource resource)
        {
            string filePath = _paths.GetLanguageFilePath(resource.Lang, resource.Namespace);
            XmlCodec.SerializeToFile(resource, filePath);
        }

        /// <summary>
        /// Validates that the specified file exists.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        private static void ValidateFilePath(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"The file {filePath} does not exist.");
        }
    }
}
