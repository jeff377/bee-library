using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Language;
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
        private readonly PathOptions _paths;
        private readonly ICacheContainer _cache;
        private readonly byte[] _configEncryptionKey;
        private readonly ICustomizeDefineReader? _customizeReader;

        /// <summary>
        /// Initializes a new instance of <see cref="LocalDefineAccess"/> with the supplied
        /// <see cref="PathOptions"/> for file path resolution. Constructs a default
        /// <see cref="CacheContainerService"/> internally — convenience overload for tests
        /// that don't already have an <see cref="ICacheContainer"/> on hand.
        /// </summary>
        /// <param name="storage">The define storage used for read fallback and writes.</param>
        /// <param name="paths">The path options for SaveSystemSettings / SaveDatabaseSettings / SaveProgramSettings file targets.</param>
        public LocalDefineAccess(IDefineStorage storage, PathOptions paths)
            : this(storage, paths, new CacheContainerService(storage, paths), Array.Empty<byte>())
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="LocalDefineAccess"/> with an explicit
        /// <see cref="ICacheContainer"/> + configuration encryption key. Production DI uses
        /// this overload; the per-host singleton cache is shared across all
        /// <see cref="IDefineAccess"/> consumers.
        /// </summary>
        /// <param name="storage">The define storage used for read fallback and writes.</param>
        /// <param name="paths">The path options for SaveSystemSettings / SaveDatabaseSettings / SaveProgramSettings file targets.</param>
        /// <param name="cache">The cache container used for read/write invalidation.</param>
        /// <param name="configEncryptionKey">
        /// The 64-byte combined AES + HMAC key used to encrypt <see cref="DatabaseServer.Password"/> /
        /// <see cref="DatabaseItem.Password"/> in <c>DatabaseSettings.xml</c>. Empty disables the crypto path.
        /// </param>
        public LocalDefineAccess(IDefineStorage storage, PathOptions paths, ICacheContainer cache, byte[] configEncryptionKey)
            : this(storage, paths, cache, configEncryptionKey, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="LocalDefineAccess"/> with an optional tenant
        /// customization reader for the FormLayout overlay. Production DI uses this overload when
        /// customization is enabled; passing <c>null</c> disables the overlay (pure base layer).
        /// </summary>
        /// <param name="storage">The define storage used for read fallback and writes.</param>
        /// <param name="paths">The path options for SaveSystemSettings / SaveDatabaseSettings / SaveProgramSettings file targets.</param>
        /// <param name="cache">The cache container used for read/write invalidation.</param>
        /// <param name="configEncryptionKey">The 64-byte combined AES + HMAC key used to encrypt config passwords. Empty disables the crypto path.</param>
        /// <param name="customizeReader">The customization-override reader; <c>null</c> disables the FormLayout overlay.</param>
        public LocalDefineAccess(IDefineStorage storage, PathOptions paths, ICacheContainer cache, byte[] configEncryptionKey, ICustomizeDefineReader? customizeReader)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _configEncryptionKey = configEncryptionKey ?? Array.Empty<byte>();
            _customizeReader = customizeReader;
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
                case DefineType.Language:
                    ValidateKeys(defineType, keys, 2);
                    return this.GetLanguage(keys![0], keys[1]);
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
                case DefineType.Language:
                    this.SaveLanguage((defineObject as LanguageResource)!);
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
            return _cache.SystemSettings.Get()!;
        }

        /// <summary>
        /// Saves the system settings.
        /// </summary>
        /// <param name="settings">The system settings.</param>
        public void SaveSystemSettings(SystemSettings settings)
        {
            // Save system settings to file
            string filePath = _paths.GetSystemSettingsFilePath();
            XmlCodec.SerializeToFile(settings, filePath);
            // Invalidate the cache
            _cache.SystemSettings.Remove();
        }

        /// <summary>
        /// Gets the database settings. <see cref="DatabaseServer.Password"/> and
        /// <see cref="DatabaseItem.Password"/> are decrypted in place on first read
        /// (subsequent cache hits see plain text and the decrypt step is an idempotent no-op).
        /// </summary>
        public DatabaseSettings GetDatabaseSettings()
        {
            var settings = _cache.DatabaseSettings.Get()!;
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
            string filePath = _paths.GetDatabaseSettingsFilePath();
            XmlCodec.SerializeToFile(settings, filePath);
            // Invalidate the cache
            _cache.DatabaseSettings.Remove();
        }

        /// <summary>
        /// Gets the program settings.
        /// </summary>
        public ProgramSettings GetProgramSettings()
        {
            return _cache.ProgramSettings.Get()!;
        }

        /// <summary>
        /// Saves the program settings.
        /// </summary>
        /// <param name="settings">The program settings.</param>
        public void SaveProgramSettings(ProgramSettings settings)
        {
            // Save program settings through the active storage, then invalidate the cache.
            _storage.SaveProgramSettings(settings);
            _cache.ProgramSettings.Remove();
        }

        /// <summary>
        /// Gets the database category settings.
        /// </summary>
        public DbCategorySettings GetDbCategorySettings()
        {
            return _cache.DbCategorySettings.Get()!;
        }

        /// <summary>
        /// Saves the database category settings.
        /// </summary>
        /// <param name="settings">The database category settings.</param>
        public void SaveDbCategorySettings(DbCategorySettings settings)
        {
            // Save database category settings, then invalidate the cache
            _storage.SaveDbCategorySettings(settings);
            _cache.DbCategorySettings.Remove();
        }

        /// <summary>
        /// Gets the table schema for the specified category and table.
        /// </summary>
        /// <param name="categoryId">The database category id.</param>
        /// <param name="tableName">The table name.</param>
        public TableSchema GetTableSchema(string categoryId, string tableName)
        {
            return _cache.TableSchema.Get(categoryId, tableName)!;
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
            _cache.TableSchema.Remove(categoryId, tableSchema.TableName);
        }

        /// <summary>
        /// Gets the form schema definition for the specified program.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        public FormSchema GetFormSchema(string progId)
        {
            return _cache.FormSchema.Get(progId)!;
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
            _cache.FormSchema.Remove(formSchema.ProgId);
        }

        /// <summary>
        /// Gets the form layout for the specified layout identifier.
        /// </summary>
        /// <param name="layoutId">The layout identifier.</param>
        public FormLayout GetFormLayout(string layoutId)
        {
            return _cache.FormLayout.Get(layoutId)!;
        }

        /// <summary>
        /// Gets the form layout for the specified layout identifier, applying the tenant
        /// customization overlay (whole-file selection) for the supplied customization code.
        /// </summary>
        /// <param name="customizeId">The tenant customization code; empty resolves against the base layer only.</param>
        /// <param name="layoutId">The layout identifier.</param>
        public FormLayout GetFormLayout(string customizeId, string layoutId)
        {
            // Short-circuit: no customization code (or no reader) → base layout, untouched.
            if (!string.IsNullOrEmpty(customizeId) && _customizeReader is not null)
            {
                // Whole-file selection: a customization layout wins outright; this does not merge
                // base and cust, and the base cache is never mutated.
                var custom = _customizeReader.GetCustomizeFormLayout(customizeId, layoutId);
                if (custom is not null)
                    return custom;
            }
            return GetFormLayout(layoutId);
        }

        /// <summary>
        /// Saves the form layout.
        /// </summary>
        /// <param name="formLayout">The form layout.</param>
        public void SaveFormLayout(FormLayout formLayout)
        {
            // Save the form layout, then invalidate the cache
            _storage.SaveFormLayout(formLayout);
            _cache.FormLayout.Remove(formLayout.LayoutId);
        }

        /// <summary>
        /// Gets the language resource for the specified language and namespace.
        /// </summary>
        /// <param name="lang">The BCP-47 language code.</param>
        /// <param name="ns">The resource namespace.</param>
        public LanguageResource GetLanguage(string lang, string ns)
        {
            return _cache.LanguageResource.Get(lang, ns)!;
        }

        /// <summary>
        /// Saves the language resource.
        /// </summary>
        /// <param name="resource">The language resource.</param>
        public void SaveLanguage(LanguageResource resource)
        {
            ArgumentNullException.ThrowIfNull(resource);

            // Save the language resource, then invalidate the cache
            _storage.SaveLanguage(resource);
            _cache.LanguageResource.Remove(resource.Lang, resource.Namespace);
        }
    }
}
