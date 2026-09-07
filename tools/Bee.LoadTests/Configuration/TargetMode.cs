namespace Bee.LoadTests.Configuration
{
    /// <summary>
    /// How the driver reaches the backend.
    /// </summary>
    public enum TargetMode
    {
        /// <summary>
        /// In-process dispatch. Measures business objects, repositories and the database
        /// without any transport cost.
        /// </summary>
        Local = 0,

        /// <summary>
        /// HTTP. Measures the whole path, including serialization, compression and encryption.
        /// </summary>
        Remote = 1
    }
}
