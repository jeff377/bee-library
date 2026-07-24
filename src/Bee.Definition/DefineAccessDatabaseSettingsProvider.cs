using Bee.Definition.Database;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.Definition
{
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
