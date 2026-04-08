using Bee.Definition.Database;
using Bee.Definition.Forms;
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
        /// Gets the database schema settings.
        /// </summary>
        DbSchemaSettings GetDbSchemaSettings();

        /// <summary>
        /// Saves the database schema settings.
        /// </summary>
        /// <param name="settings">The database schema settings.</param>
        void SaveDbSchemaSettings(DbSchemaSettings settings);

        /// <summary>
        /// Gets the table schema for the specified database and table.
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableName">The table name.</param>
        TableSchema GetTableSchema(string dbName, string tableName);

        /// <summary>
        /// Saves the table schema for the specified database.
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableSchema">The table schema.</param>
        void SaveTableSchema(string dbName, TableSchema tableSchema);

        /// <summary>
        /// Gets the form schema for the specified program.
        /// </summary>
        /// <param name="progId">The program ID.</param>
        FormSchema GetFormSchema(string progId);

        /// <summary>
        /// Saves the form schema.
        /// </summary>
        /// <param name="formSchema">The form schema.</param>
        void SaveFormSchema(FormSchema formSchema);

        /// <summary>
        /// Gets the form layout for the specified layout ID.
        /// </summary>
        /// <param name="layoutId">The form layout ID.</param>
        FormLayout GetFormLayout(string layoutId);

        /// <summary>
        /// Saves the form layout.
        /// </summary>
        /// <param name="formLayout">The form layout.</param>
        void SaveFormLayout(FormLayout formLayout);
    }
}
