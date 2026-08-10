using Bee.UI.Core;

namespace Bee.UI.Avalonia.Storage
{
    /// <summary>
    /// File-backed <see cref="IEndpointStorage"/> / <see cref="IApiKeyStorage"/> implementation for
    /// desktop Avalonia hosts. Persists the service endpoint and the API key as single-line UTF-8
    /// text files under the platform's per-user local application data folder.
    /// </summary>
    /// <remarks>
    /// Hosts opt in by assigning
    /// <c>ClientInfo.EndpointStorage = new FileEndpointStorage("Bee.Northwind");</c>
    /// inside the application's bootstrap (before <see cref="ClientInfo.InitializeAsync(string)"/>
    /// or <see cref="ClientInfo.SetEndpointAsync(string)"/>).
    /// <para>
    /// Resolved storage path (per OS):
    /// <list type="bullet">
    ///   <item>Windows — <c>%LOCALAPPDATA%\&lt;appName&gt;\endpoint.txt</c></item>
    ///   <item>macOS — <c>~/Library/Application Support/&lt;appName&gt;/endpoint.txt</c></item>
    ///   <item>Linux — <c>~/.local/share/&lt;appName&gt;/endpoint.txt</c> (or <c>$XDG_DATA_HOME</c>)</item>
    /// </list>
    /// The API key sits beside it as <c>apikey.txt</c>. Separate files rather than one keyed file:
    /// each value is written independently, and a single-line file needs no format to go wrong.
    /// </para>
    /// <see cref="SetEndpoint"/> mutates an in-memory cache only; the file is touched
    /// solely by <see cref="SaveEndpoint"/> to avoid disk traffic on every keystroke
    /// of a bound input.
    /// </remarks>
    public sealed class FileEndpointStorage : IEndpointStorage, IApiKeyStorage
    {
        private readonly string _filePath;
        private readonly string _apiKeyFilePath;
        private string? _cachedEndpoint;
        private string? _cachedApiKey;

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
            _apiKeyFilePath = Path.Combine(root, appName, "apikey.txt");
        }

        /// <summary>
        /// Gets the absolute path of the backing file.
        /// </summary>
        public string FilePath => _filePath;

        /// <summary>
        /// Gets the absolute path of the file backing the API key.
        /// </summary>
        public string ApiKeyFilePath => _apiKeyFilePath;

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
            WriteFile(_filePath, endpoint);
        }

        /// <inheritdoc/>
        public string LoadApiKey()
        {
            _cachedApiKey ??= File.Exists(_apiKeyFilePath)
                ? File.ReadAllText(_apiKeyFilePath).Trim()
                : string.Empty;
            return _cachedApiKey;
        }

        /// <inheritdoc/>
        public void SetApiKey(string apiKey)
        {
            _cachedApiKey = apiKey;
        }

        /// <inheritdoc/>
        public void SaveApiKey(string apiKey)
        {
            _cachedApiKey = apiKey;
            WriteFile(_apiKeyFilePath, apiKey);
        }

        /// <summary>
        /// Writes a single-line value, creating the containing folder on first use.
        /// </summary>
        /// <param name="path">The target file path.</param>
        /// <param name="value">The value to persist.</param>
        private static void WriteFile(string path, string value)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, value);
        }
    }
}
