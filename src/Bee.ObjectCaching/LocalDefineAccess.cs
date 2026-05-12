using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Local definition data access that retrieves and saves definition data via the cache.
    /// </summary>
    public class LocalDefineAccess : IDefineAccess
    {
        private readonly IDefineStorage _storage;
        private readonly byte[] _configEncryptionKey;

        /// <summary>
        /// Initializes a new instance of <see cref="LocalDefineAccess"/>.
        /// </summary>
        /// <param name="storage">The define storage used for read fallback and writes.</param>
        public LocalDefineAccess(IDefineStorage storage) : this(storage, Array.Empty<byte>())
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="LocalDefineAccess"/> with an explicit
        /// configuration encryption key for transparent encrypt/decrypt of <see cref="DatabaseSettings"/>
        /// password fields at read/save time.
        /// </summary>
        /// <param name="storage">The define storage used for read fallback and writes.</param>
        /// <param name="configEncryptionKey">
        /// The 64-byte combined AES + HMAC key used to encrypt <see cref="DatabaseServer.Password"/> /
        /// <see cref="DatabaseItem.Password"/> in <c>DatabaseSettings.xml</c>. Empty disables the crypto path.
        /// </param>
        public LocalDefineAccess(IDefineStorage storage, byte[] configEncryptionKey)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _configEncryptionKey = configEncryptionKey ?? Array.Empty<byte>();
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
                    return  this.GetProgramSettings();
                case DefineType.DbCategorySettings:
                    return this.GetDbCategorySettings();
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
            switch (defineType)
            {
                case DefineType.SystemSettings:
                    this.SaveSystemSettings((defineObject as SystemSettings)!);
                    break;
                case DefineType.DatabaseSettings:
                    this.SaveDatabaseSettings((defineObject as DatabaseSettings)!);
                    break;
                case DefineType.ProgramSettings:
                    this.SaveProgramSettings((defineObject as ProgramSettings)!);
                    break;
                case DefineType.DbCategorySettings:
                    this.SaveDbCategorySettings((defineObject as DbCategorySettings)!);
                    break;
                case DefineType.TableSchema:
                    if (keys == null || keys.Length != 1)
                        throw new ArgumentException($"{defineType} keys verification error");
                    this.SaveTableSchema(keys[0], (defineObject as TableSchema)!);
                    break;
                case DefineType.FormLayout:
                    this.SaveFormLayout((defineObject as FormLayout)!);
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
            return CacheContainer.SystemSettings.Get()!;
        }

        /// <summary>
        /// Saves the system settings.
        /// </summary>
        /// <param name="settings">The system settings.</param>
        public void SaveSystemSettings(SystemSettings settings)
        {
            // Save system settings to file
            string filePath = DefinePathInfo.GetSystemSettingsFilePath();
            XmlCodec.SerializeToFile(settings, filePath);
            // Invalidate the cache
            CacheContainer.SystemSettings.Remove();
        }

        /// <summary>
        /// Gets the database settings. <see cref="DatabaseServer.Password"/> and
        /// <see cref="DatabaseItem.Password"/> are decrypted in place on first read
        /// (subsequent cache hits see plain text and the decrypt step is an idempotent no-op).
        /// </summary>
        public DatabaseSettings GetDatabaseSettings()
        {
            var settings = CacheContainer.DatabaseSettings.Get()!;
            DatabaseSettingsCryptor.DecryptInPlace(settings, _configEncryptionKey);
            return settings;
        }

        /// <summary>
        /// Saves the database settings. Plain-text <see cref="DatabaseServer.Password"/> /
        /// <see cref="DatabaseItem.Password"/> are encrypted in place (already-prefixed
        /// <c>enc:</c> values pass through) before serializing to XML.
        /// </summary>
        /// <param name="settings">The database settings.</param>
        public void SaveDatabaseSettings(DatabaseSettings settings)
        {
            DatabaseSettingsCryptor.EncryptInPlace(settings, _configEncryptionKey);
            string filePath = DefinePathInfo.GetDatabaseSettingsFilePath();
            XmlCodec.SerializeToFile(settings, filePath);
            // Invalidate the cache
            CacheContainer.DatabaseSettings.Remove();
        }

        /// <summary>
        /// Gets the program settings.
        /// </summary>
        public ProgramSettings GetProgramSettings()
        {
            return CacheContainer.ProgramSettings.Get()!;
        }

        /// <summary>
        /// Saves the program settings.
        /// </summary>
        /// <param name="settings">The program settings.</param>
        public void SaveProgramSettings(ProgramSettings settings)
        {
            // Save program settings to file, then invalidate the cache
            string filePath = DefinePathInfo.GetProgramSettingsFilePath();
            XmlCodec.SerializeToFile(settings, filePath);
            CacheContainer.ProgramSettings.Remove();
        }

        /// <summary>
        /// Gets the database category settings.
        /// </summary>
        public DbCategorySettings GetDbCategorySettings()
        {
            return CacheContainer.DbCategorySettings.Get()!;
        }

        /// <summary>
        /// Saves the database category settings.
        /// </summary>
        /// <param name="settings">The database category settings.</param>
        public void SaveDbCategorySettings(DbCategorySettings settings)
        {
            // Save database category settings, then invalidate the cache
            _storage.SaveDbCategorySettings(settings);
            CacheContainer.DbCategorySettings.Remove();
        }

        /// <summary>
        /// Gets the table schema for the specified category and table.
        /// </summary>
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableName">The table name.</param>
        public TableSchema GetTableSchema(string categoryId, string tableName)
        {
            return CacheContainer.TableSchema.Get(categoryId, tableName)!;
        }

        /// <summary>
        /// Saves the table schema.
        /// </summary>
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableSchema">The table schema.</param>
        public void SaveTableSchema(string categoryId, TableSchema tableSchema)
        {
            // Save the table schema, then invalidate the cache
            _storage.SaveTableSchema(categoryId, tableSchema);
            CacheContainer.TableSchema.Remove(categoryId, tableSchema.TableName);
        }

        /// <summary>
        /// Gets the form schema definition for the specified program.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        public FormSchema GetFormSchema(string progId)
        {
            return CacheContainer.FormSchema.Get(progId)!;
        }

        /// <summary>
        /// Saves the form schema definition.
        /// </summary>
        /// <param name="formSchema">The form schema.</param>
        public void SaveFormSchema(FormSchema formSchema)
        {
            ArgumentNullException.ThrowIfNull(formSchema);

            // CategoryId is required: it determines which DbCategory the schema's
            // tables belong to. Reject persistence of schemas without it.
            _ = TableSchemaGenerator.GetCategoryId(formSchema);

            // Save the form schema, then invalidate the cache
            _storage.SaveFormSchema(formSchema);
            CacheContainer.FormSchema.Remove(formSchema.ProgId);
        }

        /// <summary>
        /// Gets the form layout for the specified layout identifier.
        /// </summary>
        /// <param name="layoutId">The layout identifier.</param>
        public FormLayout GetFormLayout(string layoutId)
        {
            return CacheContainer.FormLayout.Get(layoutId)!;
        }

        /// <summary>
        /// Saves the form layout.
        /// </summary>
        /// <param name="formLayout">The form layout.</param>
        public void SaveFormLayout(FormLayout formLayout)
        {
            // Save the form layout, then invalidate the cache
            _storage.SaveFormLayout(formLayout);
            CacheContainer.FormLayout.Remove(formLayout.LayoutId);
        }
    }
}
