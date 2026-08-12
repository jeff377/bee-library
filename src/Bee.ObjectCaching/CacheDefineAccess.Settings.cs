using Bee.Base.Serialization;
using Bee.Definition.Customization;
using Bee.Definition.Settings;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// Typed accessors for the settings family (system, database, program, menu, plugin, permission, category, currency, unit).
    /// </summary>
    public partial class CacheDefineAccess
    {
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
        /// Gets the base-layer menu definition.
        /// </summary>
        public MenuSettings GetMenuSettings()
        {
            return _cache.MenuSettings.Get()!;
        }

        /// <summary>
        /// Gets the menu definition for the supplied customization code; the customization menu
        /// replaces the base menu outright.
        /// </summary>
        /// <param name="customizeId">The tenant customization code; empty resolves against the base layer only.</param>
        public MenuSettings GetMenuSettings(string customizeId)
        {
            var custom = !string.IsNullOrEmpty(customizeId) && _customizeReader is not null
                ? _customizeReader.GetCustomizeMenuSettings(customizeId)
                : null;
            // Which layer wins is decided by CustomizeOverlay — the same class a client runs over
            // the two copies it fetched, so both ends select identically.
            return CustomizeOverlay.PickMenuSettings(custom, GetMenuSettings())!;
        }

        /// <summary>
        /// Saves the menu definition.
        /// </summary>
        /// <param name="settings">The menu definition.</param>
        public void SaveMenuSettings(MenuSettings settings)
        {
            // Save the menu through the active storage, then invalidate the cache.
            _storage.SaveMenuSettings(settings);
            _cache.MenuSettings.Remove();
        }

        /// <summary>
        /// Gets the base-layer business plugin bindings.
        /// </summary>
        public PluginSettings GetPluginSettings()
        {
            return _cache.PluginSettings.Get()!;
        }

        /// <summary>
        /// Saves the business plugin bindings.
        /// </summary>
        /// <param name="settings">The plugin bindings.</param>
        public void SavePluginSettings(PluginSettings settings)
        {
            // Save through the active storage, then invalidate the cache.
            _storage.SavePluginSettings(settings);
            _cache.PluginSettings.Remove();
        }

        /// <summary>
        /// Gets the permission model registry.
        /// </summary>
        public PermissionModels GetPermissionModels()
        {
            return _cache.PermissionModels.Get()!;
        }

        /// <summary>
        /// Saves the permission model registry.
        /// </summary>
        /// <param name="models">The permission model registry.</param>
        public void SavePermissionModels(PermissionModels models)
        {
            // Save the permission model registry to file, then invalidate the cache.
            string filePath = _paths.GetPermissionModelsFilePath();
            XmlCodec.SerializeToFile(models, filePath);
            _cache.PermissionModels.Remove();
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
        /// Gets the system-level currency master.
        /// </summary>
        public CurrencySettings GetCurrencySettings()
        {
            return _cache.CurrencySettings.Get()!;
        }

        /// <summary>
        /// Saves the system-level currency master.
        /// </summary>
        /// <param name="settings">The currency master.</param>
        public void SaveCurrencySettings(CurrencySettings settings)
        {
            // Save the currency master through the active storage, then invalidate the cache.
            _storage.SaveCurrencySettings(settings);
            _cache.CurrencySettings.Remove();
        }

        /// <summary>
        /// Gets the system-level unit-of-measure master.
        /// </summary>
        public UnitSettings GetUnitSettings()
        {
            return _cache.UnitSettings.Get()!;
        }

        /// <summary>
        /// Saves the system-level unit-of-measure master.
        /// </summary>
        /// <param name="settings">The unit master.</param>
        public void SaveUnitSettings(UnitSettings settings)
        {
            // Save the unit master through the active storage, then invalidate the cache.
            _storage.SaveUnitSettings(settings);
            _cache.UnitSettings.Remove();
        }
    }
}
