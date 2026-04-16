namespace Bee.Definition
{
    /// <summary>
    /// The source type of the master key.
    /// </summary>
    public enum MasterKeySourceType
    {
        /// <summary>
        /// Load the master key from a file.
        /// </summary>
        File,
        /// <summary>
        /// Load the master key from an environment variable.
        /// </summary>
        Environment
    }
}
