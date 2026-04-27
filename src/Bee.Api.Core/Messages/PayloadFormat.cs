
namespace Bee.Api.Core.Messages
{
    /// <summary>
    /// The transport payload encoding format.
    /// </summary>
    public enum PayloadFormat
    {
        /// <summary>
        /// Plain format (not encoded or encrypted).
        /// </summary>
        Plain,

        /// <summary>
        /// Encoded format (serialized and compressed).
        /// </summary>
        Encoded,

        /// <summary>
        /// Encrypted format (serialized + compressed + encrypted).
        /// </summary>
        Encrypted
    }
}
