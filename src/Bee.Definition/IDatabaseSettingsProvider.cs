using Bee.Definition.Database;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Definition
{
    /// <summary>
    /// Provides access to the current <see cref="DatabaseSettings"/> snapshot
    /// (with <see cref="DatabaseSettings.Items"/> populated). Replaces the former
    /// <c>BackendInfo.GetDatabaseItem</c> / <c>BackendInfo.ValidateDatabaseSettings</c>
    /// static helpers.
    /// </summary>
    public interface IDatabaseSettingsProvider
    {
        /// <summary>Returns the current database settings snapshot.</summary>
        DatabaseSettings Get();

        /// <summary>Looks up the database item for the given identifier.</summary>
        /// <exception cref="ArgumentNullException">When <paramref name="databaseId"/> is null or whitespace.</exception>
        /// <exception cref="KeyNotFoundException">When no matching item exists.</exception>
        DatabaseItem GetItem(string databaseId);

        /// <summary>
        /// Validates that the settings contain the framework-required
        /// <c>common</c> database item; throws if missing.
        /// </summary>
        /// <exception cref="InvalidOperationException">When the required item is missing.</exception>
        void ValidateRequired();
    }

    /// <summary>
    /// Default <see cref="IDatabaseSettingsProvider"/> backed by an
    /// <see cref="IDefineAccess"/> instance.
    /// </summary>
    public sealed class DefineAccessDatabaseSettingsProvider : IDatabaseSettingsProvider
    {
        private readonly IDefineAccess _defineAccess;

        /// <summary>
        /// Initializes a new <see cref="DefineAccessDatabaseSettingsProvider"/>.
        /// </summary>
        /// <param name="defineAccess">The define access used to load database settings.</param>
        public DefineAccessDatabaseSettingsProvider(IDefineAccess defineAccess)
        {
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
        }

        /// <inheritdoc/>
        public DatabaseSettings Get() => _defineAccess.GetDatabaseSettings();

        /// <inheritdoc/>
        public DatabaseItem GetItem(string databaseId)
        {
            if (string.IsNullOrWhiteSpace(databaseId))
                throw new ArgumentNullException(nameof(databaseId));

            var settings = Get();
            if (settings.Items == null || !settings.Items.Contains(databaseId))
                throw new KeyNotFoundException($"DatabaseItem '{databaseId}' not found.");

            return settings.Items[databaseId];
        }

        /// <inheritdoc/>
        public void ValidateRequired()
        {
            var settings = Get();
            if (settings.Items == null || !settings.Items.Contains(DbCategoryIds.Common))
                throw new InvalidOperationException(
                    $"DatabaseSettings must contain a DatabaseItem with Id='{DbCategoryIds.Common}'.");
        }
    }
}
