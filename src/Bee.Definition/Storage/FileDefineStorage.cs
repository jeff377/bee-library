using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Base.Serialization;

namespace Bee.Definition.Storage
{
    /// <summary>
    /// A file-based implementation of define data read and write operations.
    /// Provides file access for database schema settings, table schema, form schema, and form layout objects.
    /// Manages persistence of all define data through XML serialization and deserialization.
    /// </summary>
    public class FileDefineStorage : IDefineStorage
    {
        /// <summary>
        /// Gets the database schema settings.
        /// </summary>
        public DbSchemaSettings? GetDbSchemaSettings()
        {
            string filePath = DefinePathInfo.GetDbTableSettingsFilePath();
            ValidateFilePath(filePath);
            return XmlCodec.DeserializeFromFile<DbSchemaSettings>(filePath);
        }

        /// <summary>
        /// Saves the database schema settings.
        /// </summary>
        /// <param name="settings">The database schema settings.</param>
        public void SaveDbSchemaSettings(DbSchemaSettings settings)
        {
            string filePath = DefinePathInfo.GetDbTableSettingsFilePath();
            XmlCodec.SerializeToFile(settings, filePath);
        }

        /// <summary>
        /// Gets the table schema for the specified database and table.
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableName">The table name.</param>
        public TableSchema? GetTableSchema(string dbName, string tableName)
        {
            string filePath = DefinePathInfo.GetTableSchemaFilePath(dbName, tableName);
            ValidateFilePath(filePath);
            return XmlCodec.DeserializeFromFile<TableSchema>(filePath);
        }

        /// <summary>
        /// Saves the table schema for the specified database.
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableSchema">The table schema.</param>
        public void SaveTableSchema(string dbName, TableSchema tableSchema)
        {
            string filePath = DefinePathInfo.GetTableSchemaFilePath(dbName, tableSchema.TableName);
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
