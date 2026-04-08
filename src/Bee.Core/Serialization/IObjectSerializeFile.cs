namespace Bee.Core.Serialization
{
    /// <summary>
    /// Interface for objects that support serialization to a file.
    /// </summary>
    public interface IObjectSerializeFile : IObjectSerialize
    {
        /// <summary>
        /// Gets the serialization-bound file path.
        /// </summary>
        string ObjectFilePath { get; }

        /// <summary>
        /// Sets the serialization-bound file path.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        void SetObjectFilePath(string filePath);
    }
}
