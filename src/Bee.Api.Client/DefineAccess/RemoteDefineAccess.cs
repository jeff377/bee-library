using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using System;
using Bee.Base;
using Bee.Base.Collections;
using Bee.Api.Client.Connectors;
using Bee.Definition;

namespace Bee.Api.Client.DefineAccess
{
    /// <summary>
    /// Remote definition data access that retrieves and saves definition data via the API.
    /// </summary>
    public class RemoteDefineAccess : IDefineAccess
    {
        private readonly SystemApiConnector _connector;
        private readonly Dictionary<object> _list;

        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteDefineAccess"/> class.
        /// </summary>
        /// <param name="connector">The system-level API service connector.</param>
        public RemoteDefineAccess(SystemApiConnector connector)
        {
            _connector = connector;
            _list = new Dictionary<object>();
        }

        #endregion

        /// <summary>
        /// Gets the system-level API service connector.
        /// </summary>
        private SystemApiConnector Connector
        {
            get { return _connector; }
        }

        /// <summary>
        /// Gets the collection of cached definition objects.
        /// </summary>
        private Dictionary<object> List
        {
            get { return _list; }
        }

        /// <summary>
        /// Gets the cache key for a definition object.
        /// </summary>
        /// <param name="defineType">The definition data type.</param>
        /// <param name="keys">The keys used to access the definition data.</param>
        private static string GetCacheKey(DefineType defineType, string[]? keys = null)
        {
            string cacheKey = $"{defineType}";
            if (keys != null && keys.Length > 0)
            {
                cacheKey += "_";
                foreach (string value in keys)
                    cacheKey += $".{value}";
            }
            return cacheKey;
        }

        /// <summary>
        /// Gets definition data of the specified type, using the cache when available.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="defineType">The definition data type.</param>
        /// <param name="keys">The keys used to locate the definition data.</param>
        private T GetDefine<T>(DefineType defineType, string[]? keys = null)
        {
            string cacheKey = GetCacheKey(defineType, keys);
            if (!this.List.TryGetValue(cacheKey, out object? defineObject))
            {
                // Download the definition data and add it to the cache
                defineObject = this.Connector.GetDefine<T>(defineType, keys);
                this.List.Add(cacheKey, defineObject!);
            }
            return (T)defineObject!;
        }

        /// <summary>
        /// Gets definition data.
        /// </summary>
        /// <param name="defineType">The definition data type.</param>
        /// <param name="keys">The keys used to locate the definition data.</param>
        public object GetDefine(DefineType defineType, string[]? keys = null)
        {
            switch (defineType)
            {
                case DefineType.SystemSettings:
                    return this.GetSystemSettings();
                case DefineType.DatabaseSettings:
                    return this.GetDatabaseSettings();
                case DefineType.ProgramSettings:
                    return this.GetProgramSettings();
                case DefineType.DbSchemaSettings:
                    return this.GetDbSchemaSettings();
                case DefineType.TableSchema:
                    ValidateKeys(defineType, keys, 2);
                    return this.GetTableSchema(keys![0], keys[1]);
                case DefineType.FormSchema:
                    ValidateKeys(defineType, keys, 1);
                    return this.GetFormSchema(keys![0]);
                case DefineType.FormLayout:
                    ValidateKeys(defineType, keys, 1);
                    return this.GetFormLayout(keys![0]);
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
        private static void ValidateKeys(DefineType defineType, string[]? keys, int expectedLength)
        {
            if (keys == null || keys.Length != expectedLength)
                throw new ArgumentException($"{defineType} keys verification error. Input: {string.Join(",", keys ?? Array.Empty<string>())}");
        }

        /// <summary>
        /// Saves definition data.
        /// </summary>
        /// <param name="defineType">The definition data type.</param>
        /// <param name="defineObject">The definition data object.</param>
        /// <param name="keys">The keys used to locate where the definition data is saved.</param>
        public void SaveDefine(DefineType defineType, object defineObject, string[]? keys = null)
        {
            this.Connector.SaveDefine(defineType, defineObject, keys);
        }

        /// <summary>
        /// Gets the system settings.
        /// </summary>
        public SystemSettings GetSystemSettings()
        {
            return GetDefine<SystemSettings>(DefineType.SystemSettings);
        }

        /// <summary>
        /// Saves the system settings.
        /// </summary>
        /// <param name="settings">The system settings.</param>
        public void SaveSystemSettings(SystemSettings settings)
        {
            SaveDefine(DefineType.SystemSettings, settings);
        }

        /// <summary>
        /// Gets the database settings.
        /// </summary>
        public DatabaseSettings GetDatabaseSettings()
        {
            return GetDefine<DatabaseSettings>(DefineType.DatabaseSettings);
        }

        /// <summary>
        /// Saves the database settings.
        /// </summary>
        /// <param name="settings">The database settings.</param>
        public void SaveDatabaseSettings(DatabaseSettings settings)
        {
            SaveDefine(DefineType.DatabaseSettings, settings);
        }

        /// <summary>
        /// Gets the program settings.
        /// </summary>
        public ProgramSettings GetProgramSettings()
        {
            return GetDefine<ProgramSettings>(DefineType.ProgramSettings);
        }

        /// <summary>
        /// Saves the program settings.
        /// </summary>
        /// <param name="settings">The program settings.</param>
        public void SaveProgramSettings(ProgramSettings settings)
        {
            SaveDefine(DefineType.ProgramSettings, settings);
        }

        /// <summary>
        /// Gets the database schema settings.
        /// </summary>
        public DbSchemaSettings GetDbSchemaSettings()
        {
            return GetDefine<DbSchemaSettings>(DefineType.DbSchemaSettings);
        }

        /// <summary>
        /// Saves the database schema settings.
        /// </summary>
        /// <param name="settings">The database schema settings.</param>
        public void SaveDbSchemaSettings(DbSchemaSettings settings)
        {
            SaveDefine(DefineType.DbSchemaSettings, settings);
        }

        /// <summary>
        /// Gets the table schema for the specified table.
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableName">The table name.</param>
        public TableSchema GetTableSchema(string dbName, string tableName)
        {
            return GetDefine<TableSchema>(DefineType.TableSchema, new string[] { dbName, tableName });
        }

        /// <summary>
        /// Saves the table schema.
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableSchema">The table schema.</param>
        public void SaveTableSchema(string dbName, TableSchema tableSchema)
        {
            SaveDefine(DefineType.TableSchema, tableSchema, new string[] { dbName });
        }

        /// <summary>
        /// Gets the form schema definition for the specified program.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        public FormSchema GetFormSchema(string progId)
        {
            return GetDefine<FormSchema>(DefineType.FormSchema, new string[] { progId });
        }

        /// <summary>
        /// Saves the form schema definition.
        /// </summary>
        /// <param name="formSchema">The form schema.</param>
        public void SaveFormSchema(FormSchema formSchema)
        {
            SaveDefine(DefineType.FormSchema, formSchema);
        }

        /// <summary>
        /// Gets the form layout for the specified layout identifier.
        /// </summary>
        /// <param name="layoutId">The layout identifier.</param>
        public FormLayout GetFormLayout(string layoutId)
        {
            return GetDefine<FormLayout>(DefineType.FormLayout, new string[] { layoutId });
        }

        /// <summary>
        /// Saves the form layout.
        /// </summary>
        /// <param name="formLayout">The form layout.</param>
        public void SaveFormLayout(FormLayout formLayout)
        {
            SaveDefine(DefineType.FormLayout, formLayout);
        }
    }
}
