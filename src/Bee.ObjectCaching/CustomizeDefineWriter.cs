using Bee.Base.Serialization;
using Bee.Definition;
using Bee.Definition.Settings;
using Bee.Definition.Storage;

namespace Bee.ObjectCaching
{
    /// <summary>
    /// File-backed <see cref="ICustomizeDefineWriter"/>: writes under
    /// <c>{CustomizePath}/{customizeId}/</c> and evicts that tenant's cache entry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Writes through <see cref="CustomizeOnlyPathOptions"/> rather than through
    /// <c>CustomizeOnlyStorage</c>, which stays read-only as its name promises. Both agree on where
    /// the file lives because both ask the same path options.
    /// </para>
    /// <para>
    /// <b>Single-node.</b> The file lands on the machine that served the call, so a multi-node
    /// deployment needs <see cref="PathOptions.CustomizePath"/> on shared storage — or the
    /// database-backed storage, which is shared by construction. Writing is a local-only,
    /// low-frequency maintenance operation, so this is a documented limitation rather than a
    /// blocker.
    /// </para>
    /// </remarks>
    public sealed class CustomizeDefineWriter : ICustomizeDefineWriter
    {
        private readonly ICacheContainerProvider _provider;
        private readonly PathOptions _paths;

        /// <summary>
        /// Initializes a new <see cref="CustomizeDefineWriter"/>.
        /// </summary>
        /// <param name="provider">Supplies the per-customization-code container whose entry is evicted after a write.</param>
        /// <param name="paths">The host path options; <see cref="PathOptions.CustomizePath"/> roots every tenant folder.</param>
        public CustomizeDefineWriter(ICacheContainerProvider provider, PathOptions paths)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        }

        /// <inheritdoc/>
        public void SaveCustomizePluginSettings(string customizeId, PluginSettings settings)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(customizeId);
            ArgumentNullException.ThrowIfNull(settings);
            if (string.IsNullOrEmpty(_paths.CustomizePath))
            {
                throw new InvalidOperationException(
                    "Customization is not configured: PathOptions.CustomizePath is empty, so there is nowhere to write.");
            }

            var custPaths = new CustomizeOnlyPathOptions(_paths.CustomizePath, customizeId);
            string filePath = custPaths.GetPluginSettingsFilePath();
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            XmlCodec.SerializeToFile(settings, filePath);

            // Evict before returning so the next read observes the write. The file watcher would
            // get there eventually, but "eventually" is the wrong contract for a maintenance tool
            // that reads back what it just saved.
            _provider.For(customizeId).PluginSettings.Remove();
        }
    }
}
