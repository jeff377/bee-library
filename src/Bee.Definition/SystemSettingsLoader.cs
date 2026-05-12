using Bee.Base.Serialization;
using Bee.Definition.Settings;

namespace Bee.Definition
{
    /// <summary>
    /// Loads <see cref="SystemSettings"/> from disk during application bootstrap.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This loader has no service dependencies and is safe to call before any other
    /// framework component is initialized. It is intended for the boot-time read
    /// (one-shot, before <c>AddBeeFramework</c>).
    /// </para>
    /// <para>
    /// Runtime access (cached, file-watch-aware) continues to go through the
    /// DI-resolved <c>IDefineAccess.GetSystemSettings()</c>; this loader does not
    /// replace that path.
    /// </para>
    /// <para>
    /// Typical boot-time usage:
    /// <code>
    /// var paths = new PathOptions { DefinePath = "..." };
    /// var settings = SystemSettingsLoader.Load(paths);
    /// SysInfo.Initialize(settings.CommonConfiguration);
    /// services.AddBeeFramework(settings.BackendConfiguration, paths);
    /// </code>
    /// </para>
    /// </remarks>
    public static class SystemSettingsLoader
    {
        /// <summary>
        /// Loads <see cref="SystemSettings"/> from the file path derived from the
        /// supplied <see cref="PathOptions"/>.
        /// </summary>
        /// <param name="paths">The path options that locate <c>SystemSettings.xml</c>.</param>
        /// <returns>The deserialized <see cref="SystemSettings"/> instance.</returns>
        /// <exception cref="FileNotFoundException">
        /// Thrown when the resolved file does not exist.
        /// </exception>
        public static SystemSettings Load(PathOptions paths)
        {
            ArgumentNullException.ThrowIfNull(paths);
            return Load(paths.GetSystemSettingsFilePath());
        }

        /// <summary>
        /// Loads <see cref="SystemSettings"/> from an explicit file path.
        /// </summary>
        /// <param name="filePath">Absolute or relative path to the XML file.</param>
        /// <returns>The deserialized <see cref="SystemSettings"/> instance.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="filePath"/> is null or whitespace.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Thrown when the file does not exist.
        /// </exception>
        public static SystemSettings Load(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            if (!File.Exists(filePath))
                throw new FileNotFoundException(
                    $"SystemSettings file not found: {filePath}", filePath);

            return XmlCodec.DeserializeFromFile<SystemSettings>(filePath)!;
        }
    }
}
