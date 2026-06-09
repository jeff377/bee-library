using Bee.UI.Core;

namespace Bee.UI.Avalonia.Storage
{
    /// <summary>
    /// File-backed <see cref="IEndpointStorage"/> implementation for desktop Avalonia hosts.
    /// Persists the service endpoint as a single UTF-8 text file under the platform's
    /// per-user local application data folder.
    /// </summary>
    /// <remarks>
    /// Hosts opt in by assigning
    /// <c>ClientInfo.EndpointStorage = new FileEndpointStorage("Bee.Avalonia.Demo");</c>
    /// inside the application's bootstrap (before <see cref="ClientInfo.Initialize(string)"/>
    /// or <see cref="ClientInfo.SetEndpoint(string)"/>).
    /// <para>
    /// Resolved storage path (per OS):
    /// <list type="bullet">
    ///   <item>Windows — <c>%LOCALAPPDATA%\&lt;appName&gt;\endpoint.txt</c></item>
    ///   <item>macOS — <c>~/Library/Application Support/&lt;appName&gt;/endpoint.txt</c></item>
    ///   <item>Linux — <c>~/.local/share/&lt;appName&gt;/endpoint.txt</c> (or <c>$XDG_DATA_HOME</c>)</item>
    /// </list>
    /// </para>
    /// <see cref="SetEndpoint"/> mutates an in-memory cache only; the file is touched
    /// solely by <see cref="SaveEndpoint"/> to avoid disk traffic on every keystroke
    /// of a bound input.
    /// </remarks>
    public sealed class FileEndpointStorage : IEndpointStorage
    {
        private readonly string _filePath;
        private string? _cachedEndpoint;

        /// <summary>
        /// Initializes a new instance of <see cref="FileEndpointStorage"/>.
        /// </summary>
        /// <param name="appName">
        /// Application folder name appended to the platform's local application data path.
        /// Must be a single path segment (no separators); the constructor does not validate
        /// this beyond rejecting null / whitespace.
        /// </param>
        public FileEndpointStorage(string appName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(appName);

            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _filePath = Path.Combine(root, appName, "endpoint.txt");
        }

        /// <summary>
        /// Gets the absolute path of the backing file.
        /// </summary>
        public string FilePath => _filePath;

        /// <inheritdoc/>
        public string LoadEndpoint()
        {
            if (_cachedEndpoint is not null)
                return _cachedEndpoint;

            _cachedEndpoint = File.Exists(_filePath)
                ? File.ReadAllText(_filePath).Trim()
                : string.Empty;
            return _cachedEndpoint;
        }

        /// <inheritdoc/>
        public void SetEndpoint(string endpoint)
        {
            _cachedEndpoint = endpoint;
        }

        /// <inheritdoc/>
        public void SaveEndpoint(string endpoint)
        {
            _cachedEndpoint = endpoint;

            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(_filePath, endpoint);
        }
    }
}
