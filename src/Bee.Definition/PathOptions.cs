namespace Bee.Definition
{
    /// <summary>
    /// Path-related configuration. Currently holds <see cref="DefinePath"/>;
    /// may grow in later phases (e.g. for log directory, temp directory).
    /// </summary>
    /// <remarks>
    /// Installed via <see cref="DefinePathInfo.Initialize(PathOptions)"/> at host startup.
    /// </remarks>
    public class PathOptions
    {
        /// <summary>
        /// Root directory for definition data files
        /// (SystemSettings.xml, DatabaseSettings.xml, FormSchema/, TableSchema/ etc.).
        /// </summary>
        public string DefinePath { get; init; } = string.Empty;
    }
}
