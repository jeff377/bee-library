using Bee.Core;
using Bee.Core.Serialization;
using Bee.ObjectCaching.Define;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using System;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Local definition data access that retrieves and saves definition data via the cache.
    /// </summary>
    public class LocalDefineAccess : IDefineAccess
    {
        /// <summary>
        /// Gets definition data.
        /// </summary>
        /// <param name="defineType">The definition data type.</param>
        /// <param name="keys">The keys used to locate the definition data.</param>
        public object GetDefine(DefineType defineType, string[] keys = null)
        {
            switch (defineType)
            {
                case DefineType.SystemSettings:
                    return this.GetSystemSettings();
                case DefineType.DatabaseSettings:
                    return this.GetDatabaseSettings();
                case DefineType.ProgramSettings:
                    return  this.GetProgramSettings();
                case DefineType.DbSchemaSettings:
                    return this.GetDbSchemaSettings();
                case DefineType.TableSchema:
                    ValidateKeys(defineType, keys, 2);
                    return this.GetTableSchema(keys[0], keys[1]);
                case DefineType.FormSchema:
                    ValidateKeys(defineType, keys, 1);
                    return this.GetFormSchema(keys[0]);
                case DefineType.FormLayout:
                    ValidateKeys(defineType, keys, 1);
                    return  this.GetFormLayout(keys[0]);
                default:
                    throw new NotSupportedException($"DefineType '{defineType}' is not supported.");
            }
        }

        /// <summary>
        /// Validates that the keys array has the expected length.
        /// </summary>
        /// <param name="defineType">The definition data type.</param>
        /// <param name="keys">The keys to validate.</param>
        /// <param name="expectedLength">The expected number of keys.</param>
        private void ValidateKeys(DefineType defineType, string[] keys, int expectedLength)
        {
            if (keys == null || keys.Length != expectedLength)
                throw new ArgumentException($"{defineType} keys verification error. Input: {string.Join(",", keys ?? new string[0])}");
        }

        /// <summary>
        /// Saves definition data.
        /// </summary>
        /// <param name="defineType">The definition data type.</param>
        /// <param name="defineObject">The definition data object.</param>
        /// <param name="keys">The keys used to locate where the definition data is saved.</param>
        public void SaveDefine(DefineType defineType, object defineObject, string[] keys = null)
        {
            switch (defineType)
            {
                case DefineType.SystemSettings:
                    this.SaveSystemSettings(defineObject as SystemSettings);
                    break;
                case DefineType.DatabaseSettings:
                    this.SaveDatabaseSettings(defineObject as DatabaseSettings);
                    break;
                case DefineType.ProgramSettings:
                    this.SaveProgramSettings(defineObject as ProgramSettings);
                    break;
                case DefineType.DbSchemaSettings:
                    this.SaveDbSchemaSettings(defineObject as DbSchemaSettings);
                    break;
                case DefineType.TableSchema:
                    if (keys == null || keys.Length != 1)
                        throw new ArgumentException($"{defineType} keys verification error");
                    this.SaveTableSchema(keys[0], defineObject as TableSchema);
                    break;
                case DefineType.FormLayout:
                    this.SaveFormLayout(defineObject as FormLayout);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Gets the system settings.
        /// </summary>
        public SystemSettings GetSystemSettings()
        {
            return CacheFunc.GetSystemSettings();
        }

        /// <summary>
        /// Saves the system settings.
        /// </summary>
        /// <param name="settings">The system settings.</param>
        public void SaveSystemSettings(SystemSettings settings)
        {
            // Save system settings to file
            string filePath = DefinePathInfo.GetSystemSettingsFilePath();
            SerializeFunc.ObjectToXmlFile(settings, filePath);
            // Invalidate the cache
            var cache = new SystemSettingsCache();
            cache.Remove();
        }

        /// <summary>
        /// Gets the database settings.
        /// </summary>
        public DatabaseSettings GetDatabaseSettings()
        {
            return CacheFunc.GetDatabaseSettings();
        }

        /// <summary>
        /// Saves the database settings.
        /// </summary>
        /// <param name="settings">The database settings.</param>
        public void SaveDatabaseSettings(DatabaseSettings settings)
        {
            DatabaseSettingsCache oCache;
            string sFilePath;

            // Save database settings to file
            sFilePath = DefinePathInfo.GetDatabaseSettingsFilePath();
            SerializeFunc.ObjectToXmlFile(settings, sFilePath);
            // Invalidate the cache
            oCache = new DatabaseSettingsCache();
            oCache.Remove();
        }

        /// <summary>
        /// Gets the program settings.
        /// </summary>
        public ProgramSettings GetProgramSettings()
        {
            return CacheFunc.GetProgramSettings();
        }

        /// <summary>
        /// Saves the program settings.
        /// </summary>
        /// <param name="settings">The program settings.</param>
        public void SaveProgramSettings(ProgramSettings settings)
        {
            ProgramSettingsCache oCache;
            string sFilePath;

            // Save program settings to file, then invalidate the cache
            sFilePath = DefinePathInfo.GetProgramSettingsFilePath();
            SerializeFunc.ObjectToXmlFile(settings, sFilePath);
            oCache = new ProgramSettingsCache();
            oCache.Remove();
        }

        /// <summary>
        /// Gets the database schema settings.
        /// </summary>
        public DbSchemaSettings GetDbSchemaSettings()
        {
            return CacheFunc.GetDbSchemaSettings();
        }

        /// <summary>
        /// Saves the database schema settings.
        /// </summary>
        /// <param name="settings">The database schema settings.</param>
        public void SaveDbSchemaSettings(DbSchemaSettings settings)
        {
            DbSchemaSettingsCache oCache;

            // Save database schema settings, then invalidate the cache
            BackendInfo.DefineStorage.SaveDbSchemaSettings(settings);
            oCache = new DbSchemaSettingsCache();
            oCache.Remove();
        }

        /// <summary>
        /// Gets the table schema for the specified database and table.
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableName">The table name.</param>
        public TableSchema GetTableSchema(string dbName, string tableName)
        {
            return CacheFunc.GetTableSchema(dbName, tableName);
        }

        /// <summary>
        /// Saves the table schema.
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableSchema">The table schema.</param>
        public void SaveTableSchema(string dbName, TableSchema tableSchema)
        {
            TableSchemaCache oCache;

            // Save the table schema, then invalidate the cache
            BackendInfo.DefineStorage.SaveTableSchema(dbName, tableSchema);
            oCache = new TableSchemaCache();
            oCache.Remove(dbName, tableSchema.TableName);
        }

        /// <summary>
        /// Gets the form schema definition for the specified program.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        public FormSchema GetFormSchema(string progId)
        {
            return CacheFunc.GetFormSchema(progId);
        }

        /// <summary>
        /// Saves the form schema definition.
        /// </summary>
        /// <param name="formSchema">The form schema.</param>
        public void SaveFormSchema(FormSchema formSchema)
        {
            FormSchemaCache oCache;

            // Save the form schema, then invalidate the cache
            BackendInfo.DefineStorage.SaveFormSchema(formSchema);
            oCache = new FormSchemaCache();
            oCache.Remove(formSchema.ProgId);
        }

        /// <summary>
        /// Gets the form layout for the specified layout identifier.
        /// </summary>
        /// <param name="layoutId">The layout identifier.</param>
        public FormLayout GetFormLayout(string layoutId)
        {
            return CacheFunc.GetFormLayout(layoutId);
        }

        /// <summary>
        /// Saves the form layout.
        /// </summary>
        /// <param name="formLayout">The form layout.</param>
        public void SaveFormLayout(FormLayout formLayout)
        {
            FormLayoutCache oCache;

            // Save the form layout, then invalidate the cache
            BackendInfo.DefineStorage.SaveFormLayout(formLayout);
            oCache = new FormLayoutCache();
            oCache.Remove(formLayout.LayoutId);
        }
    }
}
