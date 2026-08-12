using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Language;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Cache-backed <see cref="IDefineAccess"/> implementation that reads definition data from an
    /// <see cref="IDefineStorage"/> and caches it in an <see cref="ICacheContainer"/>; writes are
    /// persisted through the storage and invalidate the affected cache entries.
    /// </summary>
    /// <remarks>
    /// The definition accessor used by the backend (business / repository layers). It additionally
    /// encrypts configuration passwords in <c>DatabaseSettings.xml</c> when an encryption key is
    /// supplied, and overlays tenant FormLayout customizations when an
    /// <see cref="ICustomizeDefineReader"/> is provided.
    /// </remarks>
    public partial class CacheDefineAccess : IDefineAccess
    {
        private readonly IDefineStorage _storage;
        private readonly PathOptions _paths;
        private readonly ICacheContainer _cache;
        private readonly byte[] _configEncryptionKey;
        private readonly ICustomizeDefineReader? _customizeReader;

        /// <summary>
        /// Initializes a new instance of <see cref="CacheDefineAccess"/> with the supplied
        /// <see cref="PathOptions"/> for file path resolution. Constructs a default
        /// <see cref="CacheContainerService"/> internally — convenience overload for tests
        /// that don't already have an <see cref="ICacheContainer"/> on hand.
        /// </summary>
        /// <param name="storage">The define storage used for read fallback and writes.</param>
        /// <param name="paths">The path options for SaveSystemSettings / SaveDatabaseSettings / SaveProgramSettings file targets.</param>
        public CacheDefineAccess(IDefineStorage storage, PathOptions paths)
            : this(storage, paths, new CacheContainerService(storage, paths), Array.Empty<byte>())
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CacheDefineAccess"/> with an explicit
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
        public CacheDefineAccess(IDefineStorage storage, PathOptions paths, ICacheContainer cache, byte[] configEncryptionKey)
            : this(storage, paths, cache, configEncryptionKey, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CacheDefineAccess"/> with an optional tenant
        /// customization reader for the FormLayout overlay. Production DI uses this overload when
        /// customization is enabled; passing <c>null</c> disables the overlay (pure base layer).
        /// </summary>
        /// <param name="storage">The define storage used for read fallback and writes.</param>
        /// <param name="paths">The path options for SaveSystemSettings / SaveDatabaseSettings / SaveProgramSettings file targets.</param>
        /// <param name="cache">The cache container used for read/write invalidation.</param>
        /// <param name="configEncryptionKey">The 64-byte combined AES + HMAC key used to encrypt config passwords. Empty disables the crypto path.</param>
        /// <param name="customizeReader">The customization-override reader; <c>null</c> disables the FormLayout overlay.</param>
        public CacheDefineAccess(IDefineStorage storage, PathOptions paths, ICacheContainer cache, byte[] configEncryptionKey, ICustomizeDefineReader? customizeReader)
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
                case DefineType.MenuSettings:
                    return this.GetMenuSettings();
                case DefineType.PluginSettings:
                    return this.GetPluginSettings();
                case DefineType.PermissionModels:
                    return this.GetPermissionModels();
                case DefineType.DbCategorySettings:
                    return this.GetDbCategorySettings();
                case DefineType.CurrencySettings:
                    return this.GetCurrencySettings();
                case DefineType.UnitSettings:
                    return this.GetUnitSettings();
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
                case DefineType.MenuSettings:
                    this.SaveMenuSettings((defineObject as MenuSettings)!);
                    break;
                case DefineType.PluginSettings:
                    this.SavePluginSettings((defineObject as PluginSettings)!);
                    break;
                case DefineType.PermissionModels:
                    this.SavePermissionModels((defineObject as PermissionModels)!);
                    break;
                case DefineType.DbCategorySettings:
                    this.SaveDbCategorySettings((defineObject as DbCategorySettings)!);
                    break;
                case DefineType.CurrencySettings:
                    this.SaveCurrencySettings((defineObject as CurrencySettings)!);
                    break;
                case DefineType.UnitSettings:
                    this.SaveUnitSettings((defineObject as UnitSettings)!);
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
    }
}
