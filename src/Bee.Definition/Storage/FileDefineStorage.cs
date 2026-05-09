using Bee.Definition.Database;
using Bee.Definition.Forms;
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
        /// <summary>
        /// Gets the database category settings.
        /// </summary>
        public DbCategorySettings? GetDbCategorySettings()
        {
            string filePath = DefinePathInfo.GetDbCategorySettingsFilePath();
            ValidateFilePath(filePath);
            return XmlCodec.DeserializeFromFile<DbCategorySettings>(filePath);
        }

        /// <summary>
        /// Saves the database category settings.
        /// </summary>
        /// <param name="settings">The database category settings.</param>
        public void SaveDbCategorySettings(DbCategorySettings settings)
        {
            string filePath = DefinePathInfo.GetDbCategorySettingsFilePath();
            XmlCodec.SerializeToFile(settings, filePath);
        }

        /// <summary>
        /// Gets the table schema for the specified category and table.
        /// </summary>
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableName">The table name.</param>
        public TableSchema? GetTableSchema(string categoryId, string tableName)
        {
            string filePath = DefinePathInfo.GetTableSchemaFilePath(categoryId, tableName);
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
            string filePath = DefinePathInfo.GetTableSchemaFilePath(categoryId, tableSchema.TableName);
            XmlCodec.SerializeToFile(tableSchema, filePath);
        }

        /// <summary>
        /// Gets the form schema for the specified program.
        /// </summary>
        /// <param name="progId">The program ID.</param>
        public FormSchema? GetFormSchema(string progId)
        {
            string filePath = DefinePathInfo.GetFormSchemaFilePath(progId);
            ValidateFilePath(filePath);
            return XmlCodec.DeserializeFromFile<FormSchema>(filePath);
        }

        /// <summary>
        /// Saves the form schema.
        /// </summary>
        /// <param name="formSchema">The form schema.</param>
        public void SaveFormSchema(FormSchema formSchema)
        {
            string filePath = DefinePathInfo.GetFormSchemaFilePath(formSchema.ProgId);
            XmlCodec.SerializeToFile(formSchema, filePath);
        }

        /// <summary>
        /// Gets the form layout for the specified layout ID.
        /// </summary>
        /// <param name="layoutId">The form layout ID.</param>
        public FormLayout? GetFormLayout(string layoutId)
        {
            string filePath = DefinePathInfo.GetFormLayoutFilePath(layoutId);
            ValidateFilePath(filePath);
            return XmlCodec.DeserializeFromFile<FormLayout>(filePath);
        }

        /// <summary>
        /// Saves the form layout.
        /// </summary>
        /// <param name="formLayout">The form layout.</param>
        public void SaveFormLayout(FormLayout formLayout)
        {
            string filePath = DefinePathInfo.GetFormLayoutFilePath(formLayout.LayoutId);
            XmlCodec.SerializeToFile(formLayout, filePath);
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
