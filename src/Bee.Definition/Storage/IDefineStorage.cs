using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
namespace Bee.Definition.Storage
{
    /// <summary>
    /// Interface for a define data storage provider.
    /// </summary>
    public interface IDefineStorage
    {
        /// <summary>
        /// Gets the database category settings.
        /// </summary>
        DbCategorySettings? GetDbCategorySettings();

        /// <summary>
        /// Saves the database category settings.
        /// </summary>
        /// <param name="settings">The database category settings.</param>
        void SaveDbCategorySettings(DbCategorySettings settings);

        /// <summary>
        /// Gets the system-level currency master.
        /// </summary>
        CurrencySettings? GetCurrencySettings();

        /// <summary>
        /// Saves the system-level currency master.
        /// </summary>
        /// <param name="settings">The currency master.</param>
        void SaveCurrencySettings(CurrencySettings settings);

        /// <summary>
        /// Gets the system-level unit-of-measure master.
        /// </summary>
        UnitSettings? GetUnitSettings();

        /// <summary>
        /// Saves the system-level unit-of-measure master.
        /// </summary>
        /// <param name="settings">The unit master.</param>
        void SaveUnitSettings(UnitSettings settings);

        /// <summary>
        /// Gets the program settings.
        /// </summary>
        ProgramSettings? GetProgramSettings();

        /// <summary>
        /// Saves the program settings.
        /// </summary>
        /// <param name="settings">The program settings.</param>
        void SaveProgramSettings(ProgramSettings settings);

        /// <summary>
        /// Gets the table schema for the specified category and table.
        /// </summary>
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableName">The table name.</param>
        TableSchema? GetTableSchema(string categoryId, string tableName);

        /// <summary>
        /// Saves the table schema for the specified category.
        /// </summary>
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableSchema">The table schema.</param>
        void SaveTableSchema(string categoryId, TableSchema tableSchema);

        /// <summary>
        /// Gets the form schema for the specified program.
        /// </summary>
        /// <param name="progId">The program ID.</param>
        FormSchema? GetFormSchema(string progId);

        /// <summary>
        /// Saves the form schema.
        /// </summary>
        /// <param name="formSchema">The form schema.</param>
        void SaveFormSchema(FormSchema formSchema);

        /// <summary>
        /// Gets the form layout for the specified layout ID.
        /// </summary>
        /// <param name="layoutId">The form layout ID.</param>
        FormLayout? GetFormLayout(string layoutId);

        /// <summary>
        /// Saves the form layout.
        /// </summary>
        /// <param name="formLayout">The form layout.</param>
        void SaveFormLayout(FormLayout formLayout);

        /// <summary>
        /// Gets the language resource for the specified language and namespace.
        /// </summary>
        /// <param name="lang">The BCP-47 language code.</param>
        /// <param name="ns">The resource namespace.</param>
        LanguageResource? GetLanguage(string lang, string ns);

        /// <summary>
        /// Saves the language resource.
        /// </summary>
        /// <param name="resource">The language resource.</param>
        void SaveLanguage(LanguageResource resource);
    }
}
